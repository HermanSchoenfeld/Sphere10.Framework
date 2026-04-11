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
/// A stream-mapped B+ tree dictionary that persists all nodes as fixed-size records written
/// directly to the backing <see cref="Stream"/>. Each node is serialized as a contiguous record
/// with a fixed layout determined by the tree order and key/value serializer sizes.
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
/// [Node 0: nodeSize bytes]
/// [Node 1: nodeSize bytes]
/// ...
/// </code>
/// </para>
///
/// <para>
/// Header Format (32 bytes):
/// <code>
/// [RootIndex:     8 bytes (long)]  — index of root node, 0 if empty (1-based handles)
/// [Count:         8 bytes (long)]  — total number of key-value pairs
/// [FreeListCount: 4 bytes (int)]   — number of recycled node indices
/// [Reserved:     12 bytes]         — reserved for future use
/// </code>
/// </para>
///
/// <para>
/// Node Record Format (unified fixed size for both leaf and internal nodes):
/// <code>
/// [IsLeaf:      1 byte]
/// [KeyCount:    4 bytes (int)]    — leaf: entry count, internal: separator key count
/// [ChildCount:  4 bytes (int)]    — internal: child count, leaf: 0
/// [NextLeaf:    8 bytes (long)]   — leaf: next leaf handle, internal: NoNode
/// [Entries:     (order) × (keySize + valueSize) bytes]
///     Leaf nodes use full K+V slots; internal nodes use only the keySize portion.
/// [Children:    (order + 1) × 8 bytes (long handles)]
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
	private readonly EndianBinaryReader _reader;
	private readonly EndianBinaryWriter _writer;
	private readonly IItemSerializer<K> _keySerializer;
	private readonly IItemSerializer<V> _valueSerializer;
	private readonly Endianness _endianness;
	private readonly int _keySize;
	private readonly int _valueSize;
	private readonly int _entrySize;
	private readonly int _childrenOffset;
	private readonly int _nodeSize;
	private readonly Stack<long> _freeList;
	private long _rootIndex;
	private long _nodeCount;
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
		_reader = new EndianBinaryReader(_bitConverter, stream);
		_writer = new EndianBinaryWriter(_bitConverter, stream);
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

		if (stream.Length == 0) {
			// Initialize a new tree
			_rootIndex = NoNode;
			_nodeCount = 0;
			Count = 0;
			WriteHeader();
		} else {
			// Load existing tree from stream
			ReadHeader();
			_nodeCount = (_stream.Length - HeaderSize) / _nodeSize;
		}
	}

	#region Root Management

	public Stream Stream => _stream;

	public IItemSerializer<K> KeySerializer => _keySerializer;

	public IItemSerializer<V> ValueSerializer => _valueSerializer;

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
		_stream.Seek(GetNodeOffset(node) + IsLeafOffset, SeekOrigin.Begin);
		return _reader.ReadByte() != 0;
	}

	protected override int GetKeyCount(long node) {
		_stream.Seek(GetNodeOffset(node) + KeyCountOffset, SeekOrigin.Begin);
		return _reader.ReadInt32();
	}

	protected override int GetChildCount(long node) {
		_stream.Seek(GetNodeOffset(node) + ChildCountOffset, SeekOrigin.Begin);
		return _reader.ReadInt32();
	}

	#endregion

	#region Leaf Entry Operations

	protected override KeyValuePair<K, V> GetLeafEntry(long node, int index) {
		_stream.Seek(GetNodeOffset(node) + EntriesOffset + (index * _entrySize), SeekOrigin.Begin);
		var KeyBytes = _reader.ReadBytes(_keySize);
		var ValueBytes = _reader.ReadBytes(_valueSize);
		return new KeyValuePair<K, V>(
			_keySerializer.DeserializeBytes(KeyBytes, _endianness),
			_valueSerializer.DeserializeBytes(ValueBytes, _endianness));
	}

	protected override void SetLeafEntry(long node, int index, KeyValuePair<K, V> entry) {
		var KeyBytes = _keySerializer.SerializeToBytes(entry.Key, _endianness);
		var ValueBytes = _valueSerializer.SerializeToBytes(entry.Value, _endianness);
		_stream.Seek(GetNodeOffset(node) + EntriesOffset + (index * _entrySize), SeekOrigin.Begin);
		_writer.Write(KeyBytes, 0, _keySize);
		_writer.Write(ValueBytes, 0, _valueSize);
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
		var KeyCount = GetKeyCount(node);
		var NodeOffset = GetNodeOffset(node);
		var KeyBytes = _keySerializer.SerializeToBytes(entry.Key, _endianness);
		var ValueBytes = _valueSerializer.SerializeToBytes(entry.Value, _endianness);
		_stream.Seek(NodeOffset + EntriesOffset + (KeyCount * _entrySize), SeekOrigin.Begin);
		_writer.Write(KeyBytes, 0, _keySize);
		_writer.Write(ValueBytes, 0, _valueSize);
		_stream.Seek(NodeOffset + KeyCountOffset, SeekOrigin.Begin);
		_writer.Write(KeyCount + 1);
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
		_stream.Seek(GetNodeOffset(node) + EntriesOffset + (index * _entrySize), SeekOrigin.Begin);
		var KeyBytes = _reader.ReadBytes(_keySize);
		return _keySerializer.DeserializeBytes(KeyBytes, _endianness);
	}

	protected override void SetInternalKey(long node, int index, K key) {
		var KeyBytes = _keySerializer.SerializeToBytes(key, _endianness);
		_stream.Seek(GetNodeOffset(node) + EntriesOffset + (index * _entrySize), SeekOrigin.Begin);
		_writer.Write(KeyBytes, 0, _keySize);
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
		var KeyCount = GetKeyCount(node);
		var NodeOffset = GetNodeOffset(node);
		var KeyBytes = _keySerializer.SerializeToBytes(key, _endianness);
		_stream.Seek(NodeOffset + EntriesOffset + (KeyCount * _entrySize), SeekOrigin.Begin);
		_writer.Write(KeyBytes, 0, _keySize);
		_stream.Seek(NodeOffset + KeyCountOffset, SeekOrigin.Begin);
		_writer.Write(KeyCount + 1);
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
		_stream.Seek(GetNodeOffset(node) + _childrenOffset + (index * sizeof(long)), SeekOrigin.Begin);
		return _reader.ReadInt64();
	}

	protected override void SetChild(long node, int index, long child) {
		_stream.Seek(GetNodeOffset(node) + _childrenOffset + (index * sizeof(long)), SeekOrigin.Begin);
		_writer.Write(child);
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
		var ChildCount = GetChildCount(node);
		var NodeOffset = GetNodeOffset(node);
		_stream.Seek(NodeOffset + _childrenOffset + (ChildCount * sizeof(long)), SeekOrigin.Begin);
		_writer.Write(child);
		_stream.Seek(NodeOffset + ChildCountOffset, SeekOrigin.Begin);
		_writer.Write(ChildCount + 1);
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
		_stream.Seek(GetNodeOffset(leafNode) + NextLeafOffset, SeekOrigin.Begin);
		return _reader.ReadInt64();
	}

	protected override void SetNextLeaf(long leafNode, long nextLeaf) {
		_stream.Seek(GetNodeOffset(leafNode) + NextLeafOffset, SeekOrigin.Begin);
		_writer.Write(nextLeaf);
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
		_nodeCount = 0;
		_stream.SetLength(HeaderSize);
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
			_writer.Flush();
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

	private long GetNodeOffset(long handle) => HeaderSize + (HandleToIndex(handle) * _nodeSize);

	private byte[] ReadNode(long handle) {
		_stream.Seek(GetNodeOffset(handle), SeekOrigin.Begin);
		return _reader.ReadBytes(_nodeSize);
	}

	private void WriteNode(long handle, byte[] record) {
		_stream.Seek(GetNodeOffset(handle), SeekOrigin.Begin);
		_writer.Write(record, 0, _nodeSize);
	}

	private long AllocateNode(byte[] record) {
		if (_freeList.Count > 0) {
			var Recycled = _freeList.Pop();
			WriteNode(Recycled, record);
			return Recycled;
		}
		var NewHandle = IndexToHandle(_nodeCount);
		_nodeCount++;
		WriteNode(NewHandle, record);
		return NewHandle;
	}

	#endregion

	#region Private — Serialization Helpers

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
	/// Serializes only the key (K) into an entry slot in the record. Used for internal node separator keys.
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
		_writer.Write(_rootIndex);
		_writer.Write((long)Count);
		_writer.Write(_freeList.Count);
		_writer.Write(0L);
		_writer.Write(0);
		_writer.Flush();
	}

	private void ReadHeader() {
		if (_stream.Length < HeaderSize)
			throw new InvalidDataFormatException("Stream is too short to contain a valid StreamMappedBTreePlus header.");
		_stream.Seek(0, SeekOrigin.Begin);
		_rootIndex = _reader.ReadInt64();
		Count = (int)_reader.ReadInt64();
		// Free list count is read but the in-memory free list starts empty on load,
		// since we cannot persist an unbounded free list in a fixed header.
		// Deleted node slots are reclaimed only during the current session.
	}

	#endregion
}
