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
/// A stream-mapped B-tree dictionary that persists all nodes as fixed-size byte arrays in a
/// <see cref="StreamPagedList{TItem}"/>. Each node is serialized as a contiguous record with
/// a fixed layout determined by the tree order and key/value serializer sizes.
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
/// Node Record Format (fixed size per order):
/// <code>
/// [IsLeaf:     1 byte]
/// [KeyCount:   4 bytes (int)]
/// [ChildCount: 4 bytes (int)]
/// [Keys:       (order - 1) × (keySize + valueSize) bytes]
/// [Children:   order × 8 bytes (long indices)]
/// </code>
/// </para>
/// </summary>
/// <typeparam name="K">Key type. Must have a constant-size serializer.</typeparam>
/// <typeparam name="V">Value type. Must have a constant-size serializer.</typeparam>
public class StreamMappedBTree<K, V> : BTree<K, V, long>, IDisposable {
	public const int HeaderSize = 32;
	public const long NoNode = -1L;

	private readonly Stream _stream;
	private readonly EndianBitConverter _bitConverter;
	private readonly IItemSerializer<K> _keySerializer;
	private readonly IItemSerializer<V> _valueSerializer;
	private readonly Endianness _endianness;
	private readonly int _keySize;
	private readonly int _valueSize;
	private readonly int _keyEntrySize;
	private readonly int _maxKeySlots;
	private readonly int _childrenOffset;
	private readonly int _nodeSize;
	private readonly StreamPagedList<byte[]> _nodeStore;
	private readonly Stack<long> _freeList;
	private long _rootIndex;
	private bool _disposed;

	/// <summary>
	/// Creates a new stream-mapped B-tree or opens an existing one from the given stream.
	/// </summary>
	/// <param name="order">B-tree order (minimum 3).</param>
	/// <param name="stream">Backing stream for persistent storage.</param>
	/// <param name="keySerializer">Constant-size serializer for keys.</param>
	/// <param name="valueSerializer">Constant-size serializer for values.</param>
	/// <param name="keyComparer">Optional key comparer; defaults to <see cref="Comparer{K}.Default"/>.</param>
	/// <param name="endianness">Byte order for all binary encoding.</param>
	public StreamMappedBTree(
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
		_keyEntrySize = _keySize + _valueSize;
		_freeList = new Stack<long>();

		// Node layout: 1 (IsLeaf) + 4 (KeyCount) + 4 (ChildCount) + keys + children
		// During splits, a node temporarily holds up to (order) keys and (order + 1) children,
		// so we allocate one extra slot for keys and children beyond the steady-state maximum.
		_maxKeySlots = order;             // order - 1 steady-state + 1 overflow during split
		var MaxChildSlots = order + 1;    // order steady-state + 1 overflow during split
		_childrenOffset = 1 + 4 + 4 + (_maxKeySlots * _keyEntrySize);
		_nodeSize = _childrenOffset + (MaxChildSlots * sizeof(long));

		// The StreamPagedList occupies the stream region after the BTree header.
		// A BoundedStream with relative addressing maps position 0 to HeaderSize.
		var NodeStream = new BoundedStream(stream, HeaderSize, long.MaxValue - HeaderSize) {
			UseRelativeAddressing = true,
			AllowInnerResize = true
		};

		if (stream.Length == 0) {
			// Initialize a new tree — write the header and set up the node store
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
		Record[0] = 1; // IsLeaf = true
		// KeyCount = 0 and ChildCount = 0 are already zero in fresh array
		return AllocateNode(Record);
	}

	protected override long CreateInternalNode() {
		var Record = new byte[_nodeSize];
		Record[0] = 0; // IsLeaf = false
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
		return Record[0] != 0;
	}

	protected override int GetKeyCount(long node) {
		var Record = ReadNode(node);
		return _bitConverter.ToInt32(Record, 1);
	}

	protected override int GetChildCount(long node) {
		var Record = ReadNode(node);
		return _bitConverter.ToInt32(Record, 5);
	}

	#endregion

	#region Key Operations

	protected override KeyValuePair<K, V> GetKey(long node, int index) {
		var Record = ReadNode(node);
		return DeserializeKeyEntry(Record, index);
	}

	protected override void SetKey(long node, int index, KeyValuePair<K, V> entry) {
		var Record = ReadNode(node);
		SerializeKeyEntry(Record, index, entry);
		WriteNode(node, Record);
	}

	protected override void InsertKey(long node, int index, KeyValuePair<K, V> entry) {
		var Record = ReadNode(node);
		var KeyCount = _bitConverter.ToInt32(Record, 1);
		var KeysOffset = 1 + 4 + 4;

		// Shift keys right to make room
		if (index < KeyCount) {
			Buffer.BlockCopy(
				Record, KeysOffset + (index * _keyEntrySize),
				Record, KeysOffset + ((index + 1) * _keyEntrySize),
				(KeyCount - index) * _keyEntrySize);
		}

		SerializeKeyEntry(Record, index, entry);

		// Increment key count
		Buffer.BlockCopy(_bitConverter.GetBytes(KeyCount + 1), 0, Record, 1, 4);
		WriteNode(node, Record);
	}

	protected override void AddKey(long node, KeyValuePair<K, V> entry) {
		var Record = ReadNode(node);
		var KeyCount = _bitConverter.ToInt32(Record, 1);
		SerializeKeyEntry(Record, KeyCount, entry);
		Buffer.BlockCopy(_bitConverter.GetBytes(KeyCount + 1), 0, Record, 1, 4);
		WriteNode(node, Record);
	}

	protected override void RemoveKeyAt(long node, int index) {
		var Record = ReadNode(node);
		var KeyCount = _bitConverter.ToInt32(Record, 1);
		var KeysOffset = 1 + 4 + 4;

		// Shift keys left
		if (index < KeyCount - 1) {
			Buffer.BlockCopy(
				Record, KeysOffset + ((index + 1) * _keyEntrySize),
				Record, KeysOffset + (index * _keyEntrySize),
				(KeyCount - index - 1) * _keyEntrySize);
		}

		// Zero the vacated slot
		Array.Clear(Record, KeysOffset + ((KeyCount - 1) * _keyEntrySize), _keyEntrySize);

		// Decrement key count
		Buffer.BlockCopy(_bitConverter.GetBytes(KeyCount - 1), 0, Record, 1, 4);
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
		var ChildCount = _bitConverter.ToInt32(Record, 5);

		// Shift children right
		if (index < ChildCount) {
			Buffer.BlockCopy(
				Record, _childrenOffset + (index * sizeof(long)),
				Record, _childrenOffset + ((index + 1) * sizeof(long)),
				(ChildCount - index) * sizeof(long));
		}

		Buffer.BlockCopy(_bitConverter.GetBytes(child), 0, Record, _childrenOffset + (index * sizeof(long)), sizeof(long));

		// Increment child count
		Buffer.BlockCopy(_bitConverter.GetBytes(ChildCount + 1), 0, Record, 5, 4);
		WriteNode(node, Record);
	}

	protected override void AddChild(long node, long child) {
		var Record = ReadNode(node);
		var ChildCount = _bitConverter.ToInt32(Record, 5);
		Buffer.BlockCopy(_bitConverter.GetBytes(child), 0, Record, _childrenOffset + (ChildCount * sizeof(long)), sizeof(long));
		Buffer.BlockCopy(_bitConverter.GetBytes(ChildCount + 1), 0, Record, 5, 4);
		WriteNode(node, Record);
	}

	protected override void RemoveChildAt(long node, int index) {
		var Record = ReadNode(node);
		var ChildCount = _bitConverter.ToInt32(Record, 5);

		// Shift children left
		if (index < ChildCount - 1) {
			Buffer.BlockCopy(
				Record, _childrenOffset + ((index + 1) * sizeof(long)),
				Record, _childrenOffset + (index * sizeof(long)),
				(ChildCount - index - 1) * sizeof(long));
		}

		// Write NoNode into the vacated slot
		Buffer.BlockCopy(_bitConverter.GetBytes(NoNode), 0, Record, _childrenOffset + ((ChildCount - 1) * sizeof(long)), sizeof(long));

		// Decrement child count
		Buffer.BlockCopy(_bitConverter.GetBytes(ChildCount - 1), 0, Record, 5, 4);
		WriteNode(node, Record);
	}

	#endregion

	#region Overrides

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

	private byte[] ReadNode(long index) {
		return _nodeStore.Read(index);
	}

	private void WriteNode(long index, byte[] record) {
		_nodeStore.Update(index, record);
	}

	private long AllocateNode(byte[] record) {
		if (_freeList.Count > 0) {
			var Index = _freeList.Pop();
			WriteNode(Index, record);
			return Index;
		}
		_nodeStore.Add(record);
		return _nodeStore.Count - 1;
	}

	#endregion

	#region Private — Key Entry Serialization

	private KeyValuePair<K, V> DeserializeKeyEntry(byte[] record, int keyIndex) {
		var Offset = 1 + 4 + 4 + (keyIndex * _keyEntrySize);

		var KeyBytes = new byte[_keySize];
		Buffer.BlockCopy(record, Offset, KeyBytes, 0, _keySize);
		var Key = _keySerializer.DeserializeBytes(KeyBytes, _endianness);

		var ValueBytes = new byte[_valueSize];
		Buffer.BlockCopy(record, Offset + _keySize, ValueBytes, 0, _valueSize);
		var Value = _valueSerializer.DeserializeBytes(ValueBytes, _endianness);

		return new KeyValuePair<K, V>(Key, Value);
	}

	private void SerializeKeyEntry(byte[] record, int keyIndex, KeyValuePair<K, V> entry) {
		var Offset = 1 + 4 + 4 + (keyIndex * _keyEntrySize);

		var KeyBytes = _keySerializer.SerializeToBytes(entry.Key, _endianness);
		Buffer.BlockCopy(KeyBytes, 0, record, Offset, _keySize);

		var ValueBytes = _valueSerializer.SerializeToBytes(entry.Value, _endianness);
		Buffer.BlockCopy(ValueBytes, 0, record, Offset + _keySize, _valueSize);
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
			throw new InvalidDataFormatException("Stream is too short to contain a valid StreamMappedBTree header.");
		_rootIndex = _bitConverter.ToInt64(Header, 0);
		Count = _bitConverter.ToInt32(Header, 8);
		// Free list count is read but the in-memory free list starts empty on load,
		// since we cannot persist an unbounded free list in a fixed header.
		// Deleted node slots are reclaimed only during the current session.
	}

	#endregion
}
