// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
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

	/// <summary>Creates a new tree in an empty stream using the framework's recommended order.</summary>
	/// <remarks>Existing trees must be reopened with their original explicit order and serializer layout.</remarks>
	public StreamMappedBTree(
		Stream stream,
		IItemSerializer<K> keySerializer,
		IItemSerializer<V> valueSerializer,
		IComparer<K> keyComparer = null,
		Endianness endianness = Sphere10FrameworkDefaults.Endianness)
		: this(Sphere10FrameworkDefaults.StreamMappedBTreeOrder, RequireEmptyStream(stream), keySerializer, valueSerializer, keyComparer, endianness) {
	}

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
		Guard.Argument(stream.CanSeek && stream.CanRead, nameof(stream), "Tree storage must support reading and seeking.");
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
		_keySize = checked((int)keySerializer.ConstantSize);
		_valueSize = checked((int)valueSerializer.ConstantSize);
		_keyEntrySize = checked(_keySize + _valueSize);
		_freeList = new Stack<long>();

		// Node layout: 1 (IsLeaf) + 4 (KeyCount) + 4 (ChildCount) + keys + children
		// During splits, a node temporarily holds up to (order) keys and (order + 1) children,
		// so we allocate one extra slot for keys and children beyond the steady-state maximum.
		_maxKeySlots = order;             // order - 1 steady-state + 1 overflow during split
		var MaxChildSlots = checked(order + 1);    // order steady-state + 1 overflow during split
		_childrenOffset = checked(KeysOffset + (_maxKeySlots * _keyEntrySize));
		_nodeSize = checked(_childrenOffset + (MaxChildSlots * sizeof(long)));

		ValidateExistingStorage();

		// Header properties — cached, write-through stream-mapped values
		_rootIndexProperty = StreamMappedProperty.From(_stream, RootIndexOffset, RootIndexLength, PrimitiveSerializer<long>.Instance, _reader, _writer, useCache: true);
		_countProperty = StreamMappedProperty.From(_stream, CountOffset, CountLength, PrimitiveSerializer<long>.Instance, _reader, _writer, useCache: true);
		_freeListCountProperty = StreamMappedProperty.From(_stream, FreeListCountOffset, FreeListCountLength, PrimitiveSerializer<int>.Instance, _reader, _writer, useCache: true);

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
		using BorrowArrayScope<byte>? rental = _nodeSize <= Sphere10FrameworkDefaults.StackBufferSize ? null : new BorrowArrayScope<byte>(_nodeSize);
		Span<byte> record = rental.HasValue ? rental.Value.Span : stackalloc byte[_nodeSize];
		record.Clear();
		record[IsLeafOffset] = 1; // IsLeaf = true
		// KeyCount = 0 and ChildCount = 0 are already zero after clear
		return AllocateNode(record);
	}

	protected override long CreateInternalNode() {
		using BorrowArrayScope<byte>? rental = _nodeSize <= Sphere10FrameworkDefaults.StackBufferSize ? null : new BorrowArrayScope<byte>(_nodeSize);
		Span<byte> record = rental.HasValue ? rental.Value.Span : stackalloc byte[_nodeSize];
		record.Clear();
		record[IsLeafOffset] = 0; // IsLeaf = false
		// Initialize all children slots to NoNode
		for (var index = 0; index < Order + 1; index++)
			_bitConverter.WriteTo(NoNode, record, _childrenOffset + (index * sizeof(long)));
		return AllocateNode(record);
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

	protected override K GetKeyValue(long node, int index) {
		_stream.Seek(GetNodeOffset(node) + KeysOffset + (index * _keyEntrySize), SeekOrigin.Begin);
		// The generic serializer needs an owned array; avoid a scratch span followed by its ToArray copy.
		var keyBytes = new byte[_keySize];
		_stream.ReadExactly(keyBytes);
		return _keySerializer.DeserializeBytes(keyBytes, _endianness);
	}

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
		using BorrowArrayScope<byte>? rental = _nodeSize <= Sphere10FrameworkDefaults.StackBufferSize ? null : new BorrowArrayScope<byte>(_nodeSize);
		Span<byte> record = rental.HasValue ? rental.Value.Span : stackalloc byte[_nodeSize];
		ReadNode(node, record);
		var keyCount = _bitConverter.ToInt32(record, KeyCountOffset);

		// Shift keys right to make room
		if (index < keyCount)
			record.Slice(KeysOffset + (index * _keyEntrySize), (keyCount - index) * _keyEntrySize)
				.CopyTo(record.Slice(KeysOffset + ((index + 1) * _keyEntrySize)));

		SerializeKeyEntry(record, index, entry);
		// Increment key count
		_bitConverter.WriteTo(keyCount + 1, record, KeyCountOffset);
		WriteNode(node, record);
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
		using BorrowArrayScope<byte>? rental = _nodeSize <= Sphere10FrameworkDefaults.StackBufferSize ? null : new BorrowArrayScope<byte>(_nodeSize);
		Span<byte> record = rental.HasValue ? rental.Value.Span : stackalloc byte[_nodeSize];
		ReadNode(node, record);
		var keyCount = _bitConverter.ToInt32(record, KeyCountOffset);

		// Shift keys left
		if (index < keyCount - 1)
			record.Slice(KeysOffset + ((index + 1) * _keyEntrySize), (keyCount - index - 1) * _keyEntrySize)
				.CopyTo(record.Slice(KeysOffset + (index * _keyEntrySize)));

		// Zero the vacated slot
		record.Slice(KeysOffset + ((keyCount - 1) * _keyEntrySize), _keyEntrySize).Clear();
		// Decrement key count
		_bitConverter.WriteTo(keyCount - 1, record, KeyCountOffset);
		WriteNode(node, record);
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
		using BorrowArrayScope<byte>? rental = _nodeSize <= Sphere10FrameworkDefaults.StackBufferSize ? null : new BorrowArrayScope<byte>(_nodeSize);
		Span<byte> record = rental.HasValue ? rental.Value.Span : stackalloc byte[_nodeSize];
		ReadNode(node, record);
		var childCount = _bitConverter.ToInt32(record, ChildCountOffset);

		// Shift children right
		if (index < childCount)
			record.Slice(_childrenOffset + (index * sizeof(long)), (childCount - index) * sizeof(long))
				.CopyTo(record.Slice(_childrenOffset + ((index + 1) * sizeof(long))));

		_bitConverter.WriteTo(child, record, _childrenOffset + (index * sizeof(long)));
		// Increment child count
		_bitConverter.WriteTo(childCount + 1, record, ChildCountOffset);
		WriteNode(node, record);
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
		using BorrowArrayScope<byte>? rental = _nodeSize <= Sphere10FrameworkDefaults.StackBufferSize ? null : new BorrowArrayScope<byte>(_nodeSize);
		Span<byte> record = rental.HasValue ? rental.Value.Span : stackalloc byte[_nodeSize];
		ReadNode(node, record);
		var childCount = _bitConverter.ToInt32(record, ChildCountOffset);

		// Shift children left
		if (index < childCount - 1)
			record.Slice(_childrenOffset + ((index + 1) * sizeof(long)), (childCount - index - 1) * sizeof(long))
				.CopyTo(record.Slice(_childrenOffset + (index * sizeof(long))));

		// Write NoNode into the vacated slot
		_bitConverter.WriteTo(NoNode, record, _childrenOffset + ((childCount - 1) * sizeof(long)));
		// Decrement child count
		_bitConverter.WriteTo(childCount - 1, record, ChildCountOffset);
		WriteNode(node, record);
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

	#region Private — Storage Validation

	private static Stream RequireEmptyStream(Stream stream) {
		Guard.ArgumentNotNull(stream, nameof(stream));
		Guard.Argument(stream.CanSeek, nameof(stream), "Tree storage must support seeking.");
		Guard.Argument(stream.Length == 0, nameof(stream), "Opening an existing tree requires its original explicit order. Default-order construction is only supported for an empty stream.");
		return stream;
	}

	private void ValidateExistingStorage() {
		if (_stream.Length == 0)
			return;
		if (_stream.Length < HeaderSize)
			throw new InvalidDataFormatException("Stream is too short to contain a valid StreamMappedBTree header.");
		if ((_stream.Length - HeaderSize) % _nodeSize != 0)
			throw new InvalidDataFormatException("Tree storage is not aligned with the supplied order and serializer sizes. Reopen with the original layout.");

		var position = _stream.Position;
		try {
			_stream.Position = 0;
			var root = _reader.ReadInt64();
			var count = _reader.ReadInt64();
			var freeCount = _reader.ReadInt32();
			var nodeCount = (_stream.Length - HeaderSize) / _nodeSize;
			if (count < 0 || count > int.MaxValue || count > nodeCount * (Order - 1L))
				throw new InvalidDataFormatException("Tree header contains an invalid entry count.");
			if (freeCount < 0 || freeCount > nodeCount)
				throw new InvalidDataFormatException("Tree header contains an invalid recycled-node count.");
			if ((count == 0) != (root == NoNode))
				throw new InvalidDataFormatException("Tree root and entry count disagree.");
			if (root != NoNode) {
				if (root < 0 || root >= nodeCount)
					throw new InvalidDataFormatException("Tree root is outside the supplied storage layout.");
				_stream.Position = HeaderSize + ((root) * _nodeSize);
				var isLeaf = _reader.ReadByte();
				var keyCount = _reader.ReadInt32();
				var childCount = _reader.ReadInt32();
				if (isLeaf > 1 || keyCount < 1 || keyCount >= Order || childCount < 0 || childCount > Order
					|| (isLeaf == 1 ? childCount != 0 : childCount != keyCount + 1))
					throw new InvalidDataFormatException("Tree root record is inconsistent with the supplied layout.");
			}
		} finally {
			_stream.Position = position;
		}
	}

	#endregion

	#region Private — Node I/O

	private long GetNodeOffset(long index) => HeaderSize + (index * _nodeSize);

	private void ReadNode(long index, Span<byte> record) {
		_stream.Seek(GetNodeOffset(index), SeekOrigin.Begin);
		_stream.ReadExactly(record[.._nodeSize]);
	}

	private void WriteNode(long index, ReadOnlySpan<byte> record) {
		_stream.Seek(GetNodeOffset(index), SeekOrigin.Begin);
		_stream.Write(record[.._nodeSize]);
	}

	private long AllocateNode(ReadOnlySpan<byte> record) {
		if (_freeList.Count > 0) {
			var index = _freeList.Pop();
			WriteNode(index, record);
			return index;
		}
		var newIndex = _nodeCount;
		_nodeCount++;
		WriteNode(newIndex, record);
		return newIndex;
	}

	#endregion

	#region Private — Key Entry Serialization

	private void SerializeKeyEntry(Span<byte> record, int keyIndex, KeyValuePair<K, V> entry) {
		var offset = KeysOffset + (keyIndex * _keyEntrySize);
		var keyBytes = _keySerializer.SerializeToBytes(entry.Key, _endianness);
		keyBytes.AsSpan(0, _keySize).CopyTo(record.Slice(offset, _keySize));
		var valueBytes = _valueSerializer.SerializeToBytes(entry.Value, _endianness);
		valueBytes.AsSpan(0, _valueSize).CopyTo(record.Slice(offset + _keySize, _valueSize));
	}

	#endregion
}
