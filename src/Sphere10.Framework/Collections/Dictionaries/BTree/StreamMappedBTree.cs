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
/// A stream-mapped B-tree dictionary that persists all nodes as fixed-size records written
/// directly to the backing <see cref="Stream"/>. Each node is serialized as a contiguous record with
/// a fixed layout determined by the tree order and key/value serializer sizes.
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
/// [RootIndex:     8 bytes (long)]  — index of root node, -1 if empty
/// [Count:         8 bytes (long)]  — total number of key-value pairs
/// [FreeListCount: 4 bytes (int)]   — number of recycled node indices
/// [Reserved:     12 bytes]         — reserved for future use
/// </code>
/// </para>
///
/// <para>
/// Node Record Format (fixed size per order):
/// <code>
/// [IsLeaf:     1 byte]
/// [KeyCount:   4 bytes (int)]
/// [ChildCount: 4 bytes (int)]
/// [Keys:       (order) × (keySize + valueSize) bytes]
/// [Children:   (order + 1) × 8 bytes (long indices)]
/// </code>
/// </para>
/// </summary>
/// <typeparam name="K">Key type. Must have a constant-size serializer.</typeparam>
/// <typeparam name="V">Value type. Must have a constant-size serializer.</typeparam>
public class StreamMappedBTree<K, V> : BTree<K, V, long>, IDisposable {
	public const int HeaderSize = 32;
	public const long NoNode = -1L;

	// Node record field offsets
	private const int IsLeafOffset = 0;       // 1 byte
	private const int KeyCountOffset = 1;     // 4 bytes (int)
	private const int ChildCountOffset = 5;   // 4 bytes (int)
	private const int KeysOffset = 9;         // keys region starts here

	// Header field offsets and lengths
	private const int RootIndexOffset = 0;
	private const int RootIndexLength = sizeof(long);
	private const int CountOffset = RootIndexOffset + RootIndexLength;
	private const int CountLength = sizeof(long);
	private const int FreeListCountOffset = CountOffset + CountLength;
	private const int FreeListCountLength = sizeof(int);

	private readonly Stream _stream;
	private readonly EndianBitConverter _bitConverter;
	private readonly EndianBinaryReader _reader;
	private readonly EndianBinaryWriter _writer;
	private readonly IItemSerializer<K> _keySerializer;
	private readonly IItemSerializer<V> _valueSerializer;
	private readonly Endianness _endianness;
	private readonly int _keySize;
	private readonly int _valueSize;
	private readonly int _keyEntrySize;
	private readonly int _maxKeySlots;
	private readonly int _childrenOffset;
	private readonly int _nodeSize;
	private readonly Stack<long> _freeList;
	private readonly StreamMappedProperty<long> _rootIndexProperty;
	private readonly StreamMappedProperty<long> _countProperty;
	private readonly StreamMappedProperty<int> _freeListCountProperty;
	private long _nodeCount;
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
		_reader = new EndianBinaryReader(_bitConverter, stream);
		_writer = new EndianBinaryWriter(_bitConverter, stream);
		_keySize = (int)keySerializer.ConstantSize;
		_valueSize = (int)valueSerializer.ConstantSize;
		_keyEntrySize = _keySize + _valueSize;
		_freeList = new Stack<long>();

		// Node layout: 1 (IsLeaf) + 4 (KeyCount) + 4 (ChildCount) + keys + children
		// During splits, a node temporarily holds up to (order) keys and (order + 1) children,
		// so we allocate one extra slot for keys and children beyond the steady-state maximum.
		_maxKeySlots = order;             // order - 1 steady-state + 1 overflow during split
		var MaxChildSlots = order + 1;    // order steady-state + 1 overflow during split
		_childrenOffset = KeysOffset + (_maxKeySlots * _keyEntrySize);
		_nodeSize = _childrenOffset + (MaxChildSlots * sizeof(long));

		// Header properties — cached, write-through stream-mapped values
		_rootIndexProperty = new StreamMappedProperty<long>(_stream, RootIndexOffset, RootIndexLength, PrimitiveSerializer<long>.Instance, _reader, _writer);
		_countProperty = new StreamMappedProperty<long>(_stream, CountOffset, CountLength, PrimitiveSerializer<long>.Instance, _reader, _writer);
		_freeListCountProperty = new StreamMappedProperty<int>(_stream, FreeListCountOffset, FreeListCountLength, PrimitiveSerializer<int>.Instance, _reader, _writer);

		if (stream.Length == 0) {
			// Initialize a new tree
			_stream.SetLength(HeaderSize);
			_rootIndexProperty.Value = NoNode;
			_countProperty.Value = 0;
			_freeListCountProperty.Value = 0;
			Count = 0;
			_nodeCount = 0;
		} else {
			// Load existing tree from stream
			if (_stream.Length < HeaderSize)
				throw new InvalidDataFormatException("Stream is too short to contain a valid StreamMappedBTree header.");
			Count = (int)_countProperty.Value;
			_nodeCount = (_stream.Length - HeaderSize) / _nodeSize;
		}
	}

	#region Root Management

	public Stream Stream => _stream;

	public IItemSerializer<K> KeySerializer => _keySerializer;

	public IItemSerializer<V> ValueSerializer => _valueSerializer;

	protected override bool HasRoot => _rootIndexProperty.Value != NoNode;

	protected override long Root => _rootIndexProperty.Value;

	protected override void SetRoot(long node) {
		_rootIndexProperty.Value = node;
	}

	protected override void ClearRoot() {
		_rootIndexProperty.Value = NoNode;
	}

	#endregion

	#region Node Lifecycle

	protected override long CreateLeafNode() {
		var Record = new byte[_nodeSize];
		Record[IsLeafOffset] = 1; // IsLeaf = true
		// KeyCount = 0 and ChildCount = 0 are already zero in fresh array
		return AllocateNode(Record);
	}

	protected override long CreateInternalNode() {
		var Record = new byte[_nodeSize];
		Record[IsLeafOffset] = 0; // IsLeaf = false
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

	#region Key Operations

	protected override KeyValuePair<K, V> GetKey(long node, int index) {
		_stream.Seek(GetNodeOffset(node) + KeysOffset + (index * _keyEntrySize), SeekOrigin.Begin);
		var KeyBytes = _reader.ReadBytes(_keySize);
		var ValueBytes = _reader.ReadBytes(_valueSize);
		return new KeyValuePair<K, V>(
			_keySerializer.DeserializeBytes(KeyBytes, _endianness),
			_valueSerializer.DeserializeBytes(ValueBytes, _endianness));
	}

	protected override void SetKey(long node, int index, KeyValuePair<K, V> entry) {
		var KeyBytes = _keySerializer.SerializeToBytes(entry.Key, _endianness);
		var ValueBytes = _valueSerializer.SerializeToBytes(entry.Value, _endianness);
		_stream.Seek(GetNodeOffset(node) + KeysOffset + (index * _keyEntrySize), SeekOrigin.Begin);
		_writer.Write(KeyBytes, 0, _keySize);
		_writer.Write(ValueBytes, 0, _valueSize);
	}

	protected override void InsertKey(long node, int index, KeyValuePair<K, V> entry) {
		var Record = ReadNode(node);
		var KeyCount = _bitConverter.ToInt32(Record, KeyCountOffset);

		// Shift keys right to make room
		if (index < KeyCount) {
			Buffer.BlockCopy(
				Record, KeysOffset + (index * _keyEntrySize),
				Record, KeysOffset + ((index + 1) * _keyEntrySize),
				(KeyCount - index) * _keyEntrySize);
		}

		SerializeKeyEntry(Record, index, entry);

		// Increment key count
		Buffer.BlockCopy(_bitConverter.GetBytes(KeyCount + 1), 0, Record, KeyCountOffset, 4);
		WriteNode(node, Record);
	}

	protected override void AddKey(long node, KeyValuePair<K, V> entry) {
		var KeyCount = GetKeyCount(node);
		var NodeOffset = GetNodeOffset(node);
		var KeyBytes = _keySerializer.SerializeToBytes(entry.Key, _endianness);
		var ValueBytes = _valueSerializer.SerializeToBytes(entry.Value, _endianness);
		_stream.Seek(NodeOffset + KeysOffset + (KeyCount * _keyEntrySize), SeekOrigin.Begin);
		_writer.Write(KeyBytes, 0, _keySize);
		_writer.Write(ValueBytes, 0, _valueSize);
		_stream.Seek(NodeOffset + KeyCountOffset, SeekOrigin.Begin);
		_writer.Write(KeyCount + 1);
	}

	protected override void RemoveKeyAt(long node, int index) {
		var Record = ReadNode(node);
		var KeyCount = _bitConverter.ToInt32(Record, KeyCountOffset);

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

		// Increment child count
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

		// Decrement child count
		Buffer.BlockCopy(_bitConverter.GetBytes(ChildCount - 1), 0, Record, ChildCountOffset, 4);
		WriteNode(node, Record);
	}

	#endregion

	#region Overrides

	public override void Set(K key, V value, bool overwriteIfExists) {
		base.Set(key, value, overwriteIfExists);
		_countProperty.Value = Count;
		_freeListCountProperty.Value = _freeList.Count;
	}

	public override bool Remove(K key) {
		var Result = base.Remove(key);
		if (Result) {
			_countProperty.Value = Count;
			_freeListCountProperty.Value = _freeList.Count;
		}
		return Result;
	}

	public override void Clear() {
		base.Clear();
		_freeList.Clear();
		_nodeCount = 0;
		_stream.SetLength(HeaderSize);
		_countProperty.Value = Count;
		_freeListCountProperty.Value = 0;
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
			_countProperty.Value = Count;
			_freeListCountProperty.Value = _freeList.Count;
			_writer.Flush();
		}
		_disposed = true;
	}

	#endregion

	#region Private — Node I/O

	private long GetNodeOffset(long index) => HeaderSize + (index * _nodeSize);

	private byte[] ReadNode(long index) {
		_stream.Seek(GetNodeOffset(index), SeekOrigin.Begin);
		return _reader.ReadBytes(_nodeSize);
	}

	private void WriteNode(long index, byte[] record) {
		_stream.Seek(GetNodeOffset(index), SeekOrigin.Begin);
		_writer.Write(record, 0, _nodeSize);
	}

	private long AllocateNode(byte[] record) {
		if (_freeList.Count > 0) {
			var Index = _freeList.Pop();
			WriteNode(Index, record);
			return Index;
		}
		var NewIndex = _nodeCount;
		_nodeCount++;
		WriteNode(NewIndex, record);
		return NewIndex;
	}

	#endregion

	#region Private — Key Entry Serialization

	private KeyValuePair<K, V> DeserializeKeyEntry(byte[] record, int keyIndex) {
		var Offset = KeysOffset + (keyIndex * _keyEntrySize);

		var KeyBytes = new byte[_keySize];
		Buffer.BlockCopy(record, Offset, KeyBytes, 0, _keySize);
		var Key = _keySerializer.DeserializeBytes(KeyBytes, _endianness);

		var ValueBytes = new byte[_valueSize];
		Buffer.BlockCopy(record, Offset + _keySize, ValueBytes, 0, _valueSize);
		var Value = _valueSerializer.DeserializeBytes(ValueBytes, _endianness);

		return new KeyValuePair<K, V>(Key, Value);
	}

	private void SerializeKeyEntry(byte[] record, int keyIndex, KeyValuePair<K, V> entry) {
		var Offset = KeysOffset + (keyIndex * _keyEntrySize);

		var KeyBytes = _keySerializer.SerializeToBytes(entry.Key, _endianness);
		Buffer.BlockCopy(KeyBytes, 0, record, Offset, _keySize);

		var ValueBytes = _valueSerializer.SerializeToBytes(entry.Value, _endianness);
		Buffer.BlockCopy(ValueBytes, 0, record, Offset + _keySize, _valueSize);
	}

	#endregion
}
