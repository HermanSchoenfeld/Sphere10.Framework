// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;

namespace Sphere10.Framework;

/// <summary>
/// A stream-mapped B+ tree dictionary that persists all nodes as fixed-size byte arrays in a
/// <see cref="StreamPagedList{TItem}"/>. Each node is serialized as a contiguous record with
/// a fixed layout determined by the tree order and key/value serializer sizes.
///
/// <para>
/// In a B+ tree data entries (K+V) are stored only in leaf nodes, which are linked for efficient
/// sequential access. Internal nodes hold separator keys (K only) that guide searches.
/// </para>
///
/// <para>
/// Stream Layout:
/// <code>
/// [Header: 32 bytes]
/// [StreamPagedList of byte[] node records...]
/// </code>
/// </para>
///
/// <para>
/// Header Format (32 bytes):
/// <code>
/// [RootIndex:     8 bytes (long)]  — index of root node, -1 if empty
/// [Count:         4 bytes (int)]   — total number of key-value pairs
/// [FreeListCount: 4 bytes (int)]   — number of recycled node indices
/// [Reserved:     16 bytes]         — reserved for future use
/// </code>
/// </para>
///
/// <para>
/// Node Record Format (unified fixed size for both leaf and internal nodes):
/// <code>
/// [IsLeaf:      1 byte]
/// [KeyCount:    4 bytes (int)]    — leaf: entry count, internal: separator key count
/// [ChildCount:  4 bytes (int)]    — internal: child count, leaf: 0
/// [NextLeaf:    8 bytes (long)]   — leaf: next leaf index, internal: NoNode
/// [Entries:     (order-1) × (keySize + valueSize) bytes]
///     Leaf nodes use full K+V slots; internal nodes use only the keySize portion.
/// [Children:    order × 8 bytes (long indices)]
///     Used by internal nodes; unused by leaf nodes.
/// </code>
/// </para>
/// </summary>
/// <typeparam name="K">Key type. Must have a constant-size serializer.</typeparam>
/// <typeparam name="V">Value type. Must have a constant-size serializer.</typeparam>
public class StreamMappedBTreePlus<K, V> : BTreePlus<K, V, long>, IDisposable {
	public const int HeaderSize = 32;

	/// <summary>
	/// Sentinel value representing "no node". Equal to <c>default(long)</c> so that
	/// <see cref="BTreePlus{K,V,TNode}"/> can use <c>default(TNode)</c> to represent a null node.
	/// Node handles exposed to the base class are 1-based; the underlying store uses 0-based indices.
	/// </summary>
	public const long NoNode = 0L;

	// Node record field offsets
	private const int IsLeafOffset = 0;
	private const int KeyCountOffset = 1;
	private const int ChildCountOffset = 5;
	private const int NextLeafOffset = 9;
	private const int EntriesOffset = 17;

	private readonly Stream _stream;
	private readonly EndianBitConverter _bitConverter;
	private readonly IItemSerializer<K> _keySerializer;
	private readonly IItemSerializer<V> _valueSerializer;
	private readonly Endianness _endianness;
	private readonly int _keySize;
	private readonly int _valueSize;
	private readonly int _entrySize;
	private readonly int _childrenOffset;
	private readonly int _nodeSize;
	private readonly StreamPagedList<byte[]> _nodeStore;
	private readonly Stack<long> _freeList;
	private long _rootIndex;
	private bool _disposed;

	/// <summary>
	/// Creates a new stream-mapped B+ tree or opens an existing one from the given stream.
	/// </summary>
	/// <param name="order">B+ tree order (minimum 3).</param>
	/// <param name="stream">Backing stream for persistent storage.</param>
	/// <param name="keySerializer">Constant-size serializer for keys.</param>
	/// <param name="valueSerializer">Constant-size serializer for values.</param>
	/// <param name="keyComparer">Optional key comparer; defaults to <see cref="Comparer{K}.Default"/>.</param>
	/// <param name="endianness">Byte order for all binary encoding.</param>
	public StreamMappedBTreePlus(
		int order,
		Stream stream,
		IItemSerializer<K> keySerializer,
		IItemSerializer<V> valueSerializer,
		IComparer<K> keyComparer = null,
		Endianness endianness = Sphere10FrameworkDefaults.Endianness)
		: base(order, keyComparer) {
		Guard.ArgumentNotNull(stream, nameof(stream));
		Guard.ArgumentNotNull(keySerializer, nameof(keySerializer));
		Guard.ArgumentNotNull(valueSerializer, nameof(valueSerializer));
		Guard.Argument(keySerializer.IsConstantSize, nameof(keySerializer), "Key serializer must be constant-size.");
		Guard.Argument(valueSerializer.IsConstantSize, nameof(valueSerializer), "Value serializer must be constant-size.");

		_stream = stream;
		_keySerializer = keySerializer;
		_valueSerializer = valueSerializer;
		_endianness = endianness;
		_bitConverter = EndianBitConverter.For(endianness);
		_keySize = (int)keySerializer.ConstantSize;
		_valueSize = (int)valueSerializer.ConstantSize;
		_entrySize = _keySize + _valueSize;
		_freeList = new Stack<long>();

		// Compute derived layout values
		// During splits, a node temporarily holds up to (order) keys/entries and (order + 1) children,
		// so we allocate one extra slot for entries and children beyond the steady-state maximum.
		var MaxEntrySlots = order;           // order - 1 steady-state + 1 overflow during split
		var MaxChildSlots = order + 1;       // order steady-state + 1 overflow during split
		_childrenOffset = EntriesOffset + (MaxEntrySlots * _entrySize);
		_nodeSize = _childrenOffset + (MaxChildSlots * sizeof(long));

		// The StreamPagedList occupies the stream region after the header.
		var NodeStream = new BoundedStream(stream, HeaderSize, long.MaxValue - HeaderSize) {
			UseRelativeAddressing = true,
			AllowInnerResize = true
		};

		if (stream.Length == 0) {
			// Initialize a new tree
			_rootIndex = NoNode;
			Count = 0;
			WriteHeader();
			_nodeStore = new StreamPagedList<byte[]>(
				new ConstantSizeByteArraySerializer(_nodeSize),
				NodeStream,
				_endianness,
				includeListHeader: true,
				autoLoad: false);
		} else {
			// Load existing tree from stream
			ReadHeader();
			_nodeStore = new StreamPagedList<byte[]>(
				new ConstantSizeByteArraySerializer(_nodeSize),
				NodeStream,
				_endianness,
				includeListHeader: true,
				autoLoad: true);
		}
	}

	#region Root Management

	protected override bool HasRoot => _rootIndex != NoNode;

	protected override long Root => _rootIndex;

	protected override void SetRoot(long node) {
		_rootIndex = node;
		WriteHeader();
	}

	protected override void ClearRoot() {
		_rootIndex = NoNode;
		WriteHeader();
	}

	#endregion

	#region Node Lifecycle

	protected override long CreateLeafNode() {
		var Record = new byte[_nodeSize];
		Record[IsLeafOffset] = 1;
		// KeyCount = 0, ChildCount = 0 are already zero
		// NextLeaf = NoNode
		Buffer.BlockCopy(_bitConverter.GetBytes(NoNode), 0, Record, NextLeafOffset, sizeof(long));
		// Initialize all children slots to NoNode (unused for leaves, but keeps the record clean)
		for (var I = 0; I < Order + 1; I++)
			Buffer.BlockCopy(_bitConverter.GetBytes(NoNode), 0, Record, _childrenOffset + (I * sizeof(long)), sizeof(long));
		return AllocateNode(Record);
	}

	protected override long CreateInternalNode() {
		var Record = new byte[_nodeSize];
		Record[IsLeafOffset] = 0;
		// KeyCount = 0, ChildCount = 0 are already zero
		// NextLeaf = NoNode (not used for internal nodes)
		Buffer.BlockCopy(_bitConverter.GetBytes(NoNode), 0, Record, NextLeafOffset, sizeof(long));
		// Initialize all children slots to NoNode
		for (var I = 0; I < Order + 1; I++)
			Buffer.BlockCopy(_bitConverter.GetBytes(NoNode), 0, Record, _childrenOffset + (I * sizeof(long)), sizeof(long));
		return AllocateNode(Record);
	}

	protected override void DeleteNode(long node) {
		_freeList.Push(node);
	}

	#endregion

	#region Node Properties

	protected override bool IsLeaf(long node) {
		var Record = ReadNode(node);
		return Record[IsLeafOffset] != 0;
	}

	protected override int GetKeyCount(long node) {
		var Record = ReadNode(node);
		return _bitConverter.ToInt32(Record, KeyCountOffset);
	}

	protected override int GetChildCount(long node) {
		var Record = ReadNode(node);
		return _bitConverter.ToInt32(Record, ChildCountOffset);
	}

	#endregion

	#region Leaf Entry Operations

	protected override KeyValuePair<K, V> GetLeafEntry(long node, int index) {
		var Record = ReadNode(node);
		return DeserializeEntry(Record, index);
	}

	protected override void SetLeafEntry(long node, int index, KeyValuePair<K, V> entry) {
		var Record = ReadNode(node);
		SerializeEntry(Record, index, entry);
		WriteNode(node, Record);
	}

	protected override void InsertLeafEntry(long node, int index, KeyValuePair<K, V> entry) {
		var Record = ReadNode(node);
		var KeyCount = _bitConverter.ToInt32(Record, KeyCountOffset);

		// Shift entries right to make room
		if (index < KeyCount) {
			Buffer.BlockCopy(
				Record, EntriesOffset + (index * _entrySize),
				Record, EntriesOffset + ((index + 1) * _entrySize),
				(KeyCount - index) * _entrySize);
		}

		SerializeEntry(Record, index, entry);
		Buffer.BlockCopy(_bitConverter.GetBytes(KeyCount + 1), 0, Record, KeyCountOffset, 4);
		WriteNode(node, Record);
	}

	protected override void AddLeafEntry(long node, KeyValuePair<K, V> entry) {
		var Record = ReadNode(node);
		var KeyCount = _bitConverter.ToInt32(Record, KeyCountOffset);
		SerializeEntry(Record, KeyCount, entry);
		Buffer.BlockCopy(_bitConverter.GetBytes(KeyCount + 1), 0, Record, KeyCountOffset, 4);
		WriteNode(node, Record);
	}

	protected override void RemoveLeafEntryAt(long node, int index) {
		var Record = ReadNode(node);
		var KeyCount = _bitConverter.ToInt32(Record, KeyCountOffset);

		// Shift entries left
		if (index < KeyCount - 1) {
			Buffer.BlockCopy(
				Record, EntriesOffset + ((index + 1) * _entrySize),
				Record, EntriesOffset + (index * _entrySize),
				(KeyCount - index - 1) * _entrySize);
		}

		// Zero the vacated slot
		Array.Clear(Record, EntriesOffset + ((KeyCount - 1) * _entrySize), _entrySize);
		Buffer.BlockCopy(_bitConverter.GetBytes(KeyCount - 1), 0, Record, KeyCountOffset, 4);
		WriteNode(node, Record);
	}

	#endregion

	#region Internal Key Operations

	protected override K GetInternalKey(long node, int index) {
		var Record = ReadNode(node);
		return DeserializeKey(Record, index);
	}

	protected override void SetInternalKey(long node, int index, K key) {
		var Record = ReadNode(node);
		SerializeKey(Record, index, key);
		WriteNode(node, Record);
	}

	protected override void InsertInternalKey(long node, int index, K key) {
		var Record = ReadNode(node);
		var KeyCount = _bitConverter.ToInt32(Record, KeyCountOffset);

		// Shift entry slots right (full entrySize slots to preserve fixed layout)
		if (index < KeyCount) {
			Buffer.BlockCopy(
				Record, EntriesOffset + (index * _entrySize),
				Record, EntriesOffset + ((index + 1) * _entrySize),
				(KeyCount - index) * _entrySize);
		}

		SerializeKey(Record, index, key);
		Buffer.BlockCopy(_bitConverter.GetBytes(KeyCount + 1), 0, Record, KeyCountOffset, 4);
		WriteNode(node, Record);
	}

	protected override void AddInternalKey(long node, K key) {
		var Record = ReadNode(node);
		var KeyCount = _bitConverter.ToInt32(Record, KeyCountOffset);
		SerializeKey(Record, KeyCount, key);
		Buffer.BlockCopy(_bitConverter.GetBytes(KeyCount + 1), 0, Record, KeyCountOffset, 4);
		WriteNode(node, Record);
	}

	protected override void RemoveInternalKeyAt(long node, int index) {
		var Record = ReadNode(node);
		var KeyCount = _bitConverter.ToInt32(Record, KeyCountOffset);

		// Shift entry slots left (full entrySize slots)
		if (index < KeyCount - 1) {
			Buffer.BlockCopy(
				Record, EntriesOffset + ((index + 1) * _entrySize),
				Record, EntriesOffset + (index * _entrySize),
				(KeyCount - index - 1) * _entrySize);
		}

		// Zero the vacated slot
		Array.Clear(Record, EntriesOffset + ((KeyCount - 1) * _entrySize), _entrySize);
		Buffer.BlockCopy(_bitConverter.GetBytes(KeyCount - 1), 0, Record, KeyCountOffset, 4);
		WriteNode(node, Record);
	}

	#endregion

	#region Child Operations

	protected override long GetChild(long node, int index) {
		var Record = ReadNode(node);
		return _bitConverter.ToInt64(Record, _childrenOffset + (index * sizeof(long)));
	}

	protected override void SetChild(long node, int index, long child) {
		var Record = ReadNode(node);
		Buffer.BlockCopy(_bitConverter.GetBytes(child), 0, Record, _childrenOffset + (index * sizeof(long)), sizeof(long));
		WriteNode(node, Record);
	}

	protected override void InsertChild(long node, int index, long child) {
		var Record = ReadNode(node);
		var ChildCount = _bitConverter.ToInt32(Record, ChildCountOffset);

		// Shift children right
		if (index < ChildCount) {
			Buffer.BlockCopy(
				Record, _childrenOffset + (index * sizeof(long)),
				Record, _childrenOffset + ((index + 1) * sizeof(long)),
				(ChildCount - index) * sizeof(long));
		}

		Buffer.BlockCopy(_bitConverter.GetBytes(child), 0, Record, _childrenOffset + (index * sizeof(long)), sizeof(long));
		Buffer.BlockCopy(_bitConverter.GetBytes(ChildCount + 1), 0, Record, ChildCountOffset, 4);
		WriteNode(node, Record);
	}

	protected override void AddChild(long node, long child) {
		var Record = ReadNode(node);
		var ChildCount = _bitConverter.ToInt32(Record, ChildCountOffset);
		Buffer.BlockCopy(_bitConverter.GetBytes(child), 0, Record, _childrenOffset + (ChildCount * sizeof(long)), sizeof(long));
		Buffer.BlockCopy(_bitConverter.GetBytes(ChildCount + 1), 0, Record, ChildCountOffset, 4);
		WriteNode(node, Record);
	}

	protected override void RemoveChildAt(long node, int index) {
		var Record = ReadNode(node);
		var ChildCount = _bitConverter.ToInt32(Record, ChildCountOffset);

		// Shift children left
		if (index < ChildCount - 1) {
			Buffer.BlockCopy(
				Record, _childrenOffset + ((index + 1) * sizeof(long)),
				Record, _childrenOffset + (index * sizeof(long)),
				(ChildCount - index - 1) * sizeof(long));
		}

		// Write NoNode into the vacated slot
		Buffer.BlockCopy(_bitConverter.GetBytes(NoNode), 0, Record, _childrenOffset + ((ChildCount - 1) * sizeof(long)), sizeof(long));
		Buffer.BlockCopy(_bitConverter.GetBytes(ChildCount - 1), 0, Record, ChildCountOffset, 4);
		WriteNode(node, Record);
	}

	#endregion

	#region Leaf Link Operations

	protected override long GetNextLeaf(long leafNode) {
		var Record = ReadNode(leafNode);
		return _bitConverter.ToInt64(Record, NextLeafOffset);
	}

	protected override void SetNextLeaf(long leafNode, long nextLeaf) {
		var Record = ReadNode(leafNode);
		Buffer.BlockCopy(_bitConverter.GetBytes(nextLeaf), 0, Record, NextLeafOffset, sizeof(long));
		WriteNode(leafNode, Record);
	}

	#endregion

	#region Overrides — Persist Header After Mutations

	public override void Set(K key, V value, bool overwriteIfExists) {
		base.Set(key, value, overwriteIfExists);
		WriteHeader();
	}

	public override bool Remove(K key) {
		var Result = base.Remove(key);
		if (Result)
			WriteHeader();
		return Result;
	}

	public override void Clear() {
		base.Clear();
		_freeList.Clear();
		WriteHeader();
	}

	#endregion

	#region IDisposable

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing) {
		if (_disposed)
			return;
		if (disposing) {
			WriteHeader();
			_stream.Flush();
		}
		_disposed = true;
	}

	#endregion

	#region Private — Node I/O

	/// <summary>
	/// Converts a 1-based node handle to a 0-based store index.
	/// </summary>
	private static long HandleToIndex(long handle) => handle - 1;

	/// <summary>
	/// Converts a 0-based store index to a 1-based node handle.
	/// </summary>
	private static long IndexToHandle(long index) => index + 1;

	private byte[] ReadNode(long handle) {
		return _nodeStore.Read(HandleToIndex(handle));
	}

	private void WriteNode(long handle, byte[] record) {
		_nodeStore.Update(HandleToIndex(handle), record);
	}

	private long AllocateNode(byte[] record) {
		if (_freeList.Count > 0) {
			var Handle = _freeList.Pop();
			WriteNode(Handle, record);
			return Handle;
		}
		_nodeStore.Add(record);
		return IndexToHandle(_nodeStore.Count - 1);
	}

	#endregion

	#region Private — Serialization Helpers

	/// <summary>
	/// Deserializes a full K+V entry from an entry slot in the record. Used for leaf nodes.
	/// </summary>
	private KeyValuePair<K, V> DeserializeEntry(byte[] record, int entryIndex) {
		var Offset = EntriesOffset + (entryIndex * _entrySize);
		var KeyBytes = new byte[_keySize];
		Buffer.BlockCopy(record, Offset, KeyBytes, 0, _keySize);
		var Key = _keySerializer.DeserializeBytes(KeyBytes, _endianness);
		var ValueBytes = new byte[_valueSize];
		Buffer.BlockCopy(record, Offset + _keySize, ValueBytes, 0, _valueSize);
		var Value = _valueSerializer.DeserializeBytes(ValueBytes, _endianness);
		return new KeyValuePair<K, V>(Key, Value);
	}

	/// <summary>
	/// Serializes a full K+V entry into an entry slot in the record. Used for leaf nodes.
	/// </summary>
	private void SerializeEntry(byte[] record, int entryIndex, KeyValuePair<K, V> entry) {
		var Offset = EntriesOffset + (entryIndex * _entrySize);
		var KeyBytes = _keySerializer.SerializeToBytes(entry.Key, _endianness);
		Buffer.BlockCopy(KeyBytes, 0, record, Offset, _keySize);
		var ValueBytes = _valueSerializer.SerializeToBytes(entry.Value, _endianness);
		Buffer.BlockCopy(ValueBytes, 0, record, Offset + _keySize, _valueSize);
	}

	/// <summary>
	/// Deserializes only the key (K) from an entry slot. Used for internal node separator keys.
	/// </summary>
	private K DeserializeKey(byte[] record, int entryIndex) {
		var Offset = EntriesOffset + (entryIndex * _entrySize);
		var KeyBytes = new byte[_keySize];
		Buffer.BlockCopy(record, Offset, KeyBytes, 0, _keySize);
		return _keySerializer.DeserializeBytes(KeyBytes, _endianness);
	}

	/// <summary>
	/// Serializes only the key (K) into an entry slot. Used for internal node separator keys.
	/// The value portion of the slot is left untouched (padding).
	/// </summary>
	private void SerializeKey(byte[] record, int entryIndex, K key) {
		var Offset = EntriesOffset + (entryIndex * _entrySize);
		var KeyBytes = _keySerializer.SerializeToBytes(key, _endianness);
		Buffer.BlockCopy(KeyBytes, 0, record, Offset, _keySize);
	}

	#endregion

	#region Private — Header I/O

	private void WriteHeader() {
		_stream.Seek(0, SeekOrigin.Begin);
		var Header = new byte[HeaderSize];
		Buffer.BlockCopy(_bitConverter.GetBytes(_rootIndex), 0, Header, 0, 8);
		Buffer.BlockCopy(_bitConverter.GetBytes(Count), 0, Header, 8, 4);
		Buffer.BlockCopy(_bitConverter.GetBytes(_freeList.Count), 0, Header, 12, 4);
		// Bytes 16..31 are reserved (zeroed)
		_stream.Write(Header, 0, HeaderSize);
		_stream.Flush();
	}

	private void ReadHeader() {
		_stream.Seek(0, SeekOrigin.Begin);
		var Header = new byte[HeaderSize];
		var BytesRead = _stream.Read(Header, 0, HeaderSize);
		if (BytesRead < HeaderSize)
			throw new InvalidDataFormatException("Stream is too short to contain a valid StreamMappedBTreePlus header.");
		_rootIndex = _bitConverter.ToInt64(Header, 0);
		Count = _bitConverter.ToInt32(Header, 8);
		// Free list count is read but the in-memory free list starts empty on load,
		// since we cannot persist an unbounded free list in a fixed header.
		// Deleted node slots are reclaimed only during the current session.
	}

	#endregion
}
