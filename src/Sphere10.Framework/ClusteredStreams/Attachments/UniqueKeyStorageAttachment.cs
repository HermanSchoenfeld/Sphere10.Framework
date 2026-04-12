// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework;

/// <summary>
/// A scalable unique key storage attachment that persists the reverse index (key → position) in
/// a stream-mapped B-tree rather than an in-memory dictionary. The forward mapping (position → key)
/// is stored in a <see cref="StreamPagedList{TData}"/>.
///
/// This is a <see cref="CompositeStorageAttachment"/> that uses two reserved streams:
/// stream 0 for the paged list (forward mapping) and stream 1 for the B-tree (reverse mapping).
/// </summary>
public class UniqueKeyStorageAttachment<TKey> : CompositeStorageAttachment, IReadOnlyDictionary<TKey, long> {
	private const int BTreeOrder = 64;

	private readonly IItemSerializer<TKey> _keySerializer;
	private readonly IEqualityComparer<TKey> _keyComparer;
	private StreamPagedList<TKey> _pagedList;
	private StreamMappedBTree<TKey, long> _btree;

	public UniqueKeyStorageAttachment(ClusteredStreams streams, string attachmentID, IItemSerializer<TKey> keySerializer, IEqualityComparer<TKey> keyComparer)
		: base(streams, attachmentID, 2) {
		Guard.ArgumentNotNull(keySerializer, nameof(keySerializer));
		Guard.Argument(keySerializer.IsConstantSize, nameof(keySerializer), "Key serializer must be a constant-length serializer.");
		Guard.ArgumentNotNull(keyComparer, nameof(keyComparer));
		_keySerializer = keySerializer;
		_keyComparer = keyComparer;
	}

	public int Count => _btree?.Count ?? 0;

	protected StreamPagedList<TKey> PagedList => _pagedList;

	public IEnumerable<TKey> Keys {
		get {
			CheckAttached();
			using (Streams.EnterAccessScope())
				return _btree.Select(Kvp => Kvp.Key).ToArray();
		}
	}

	public IEnumerable<long> Values {
		get {
			CheckAttached();
			using (Streams.EnterAccessScope())
				return _btree.Select(Kvp => Kvp.Value).ToArray();
		}
	}

	public bool ContainsKey(TKey key) {
		CheckAttached();
		using (Streams.EnterAccessScope())
			return _btree.ContainsKey(key);
	}

	public bool TryGetValue(TKey key, out long value) {
		CheckAttached();
		using (Streams.EnterAccessScope())
			return _btree.TryGetValue(key, out value);
	}

	public TKey Read(long index) {
		CheckAttached();
		using (Streams.EnterAccessScope())
			return _pagedList.Read(index);
	}

	public byte[] ReadBytes(long index) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			_pagedList.ReadItemBytes(index, 0, null, out var bytes);
			return bytes;
		}
	}

	public void Add(long index, TKey key) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			Guard.Argument(index == _pagedList.Count, nameof(index), "Index mismatches with expected index in store");
			_btree.Add(key, index);
			_pagedList.Add(key);
		}
	}

	public void Update(long index, TKey key) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			var oldKey = _pagedList.Read(index);
			_btree.Remove(oldKey);
			_btree.Add(key, index);
			_pagedList.Update(index, key);
		}
	}

	public void Insert(long index, TKey key) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			Guard.Ensure(!_btree.ContainsKey(key));
			_pagedList.Insert(index, key);
			RebuildBTree();
		}
	}

	public void Remove(long index) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			_pagedList.RemoveAt(index);
			RebuildBTree();
		}
	}

	public void Reap(long index) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			var oldKey = _pagedList.Read(index);
			_btree.Remove(oldKey);
		}
	}

	public void Clear() {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			_btree.Clear();
			_pagedList.Clear();
		}
	}

	public IEnumerator<KeyValuePair<TKey, long>> GetEnumerator() {
		CheckAttached();
		using (Streams.EnterAccessScope())
			return _btree.ToList().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}

	public long this[TKey key] {
		get {
			CheckAttached();
			using (Streams.EnterAccessScope())
				return _btree[key];
		}
	}

	protected override void AttachInternal() {
		// Stream 0: paged list (forward mapping position → key)
		_pagedList = new StreamPagedList<TKey>(
			_keySerializer,
			GetAttachmentStream(0),
			Streams.Endianness,
			false,
			true
		);

		// Stream 1: B-tree (reverse mapping key → position)
		_btree = new StreamMappedBTree<TKey, long>(
			BTreeOrder,
			GetAttachmentStream(1),
			_keySerializer,
			PrimitiveSerializer<long>.Instance,
			Comparer<TKey>.Default
		);
		if (_btree.Count == 0 && _pagedList.Count > 0)
			RebuildBTree();
	}

	protected override void VerifyIntegrity() {
	}

	protected override void DetachInternal() {
		_btree?.Dispose();
		_btree = null;
		_pagedList = null;
	}

	public override void Flush() {
		base.Flush();
	}

	private void RebuildBTree() {
		using var _ = Streams.EnterAccessScope();
		_btree.Clear();
		var Reserved = Streams.Header.ReservedStreams;
		for (var I = 0L; I < _pagedList.Count; I++) {
			if (Streams.FastReadStreamDescriptorTraits(I + Reserved).HasFlag(ClusteredStreamTraits.Reaped))
				continue;
			var Key = _pagedList.Read(I);
			_btree.Add(Key, I);
		}
	}
}

