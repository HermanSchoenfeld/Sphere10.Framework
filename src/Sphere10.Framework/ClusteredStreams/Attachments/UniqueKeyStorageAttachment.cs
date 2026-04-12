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
/// This is a <see cref="CompositeStorageAttachmentBase"/> that composes two child attachments:
/// a <see cref="PagedListStorageAttachmentBase{TData}"/> for the paged list (forward mapping)
/// and a <see cref="BTreeStorageAttachment{TKey,TValue}"/> for the B-tree (reverse mapping).
/// </summary>
public class UniqueKeyStorageAttachment<TKey> : CompositeStorageAttachmentBase, IReadOnlyDictionary<TKey, long> {
	private const int BTreeOrder = 64;

	private readonly PagedListStorageAttachment<TKey> _pagedListStore;
	private readonly BTreeStorageAttachment<TKey, long> _btreeStore;

	public UniqueKeyStorageAttachment(ClusteredStreams streams, string attachmentID, IItemSerializer<TKey> keySerializer, IEqualityComparer<TKey> keyComparer)
		: this(
			streams,
			attachmentID,
			new PagedListStorageAttachment<TKey>(streams, attachmentID + ".pagedList", keySerializer),
			new BTreeStorageAttachment<TKey, long>(streams, attachmentID + ".btree", BTreeOrder, keySerializer, PrimitiveSerializer<long>.Instance, Comparer<TKey>.Default)
		) {
		Guard.ArgumentNotNull(keySerializer, nameof(keySerializer));
		Guard.Argument(keySerializer.IsConstantSize, nameof(keySerializer), "Key serializer must be a constant-length serializer.");
		Guard.ArgumentNotNull(keyComparer, nameof(keyComparer));
	}

	private UniqueKeyStorageAttachment(
		ClusteredStreams streams,
		string attachmentID,
		PagedListStorageAttachment<TKey> pagedListStore,
		BTreeStorageAttachment<TKey, long> btreeStore
	) : base(streams, attachmentID, pagedListStore, btreeStore) {
		_pagedListStore = pagedListStore;
		_btreeStore = btreeStore;
	}

	public int Count => IsAttached ? _btreeStore.BTree.Count : 0;

	public IEnumerable<TKey> Keys {
		get {
			CheckAttached();
			return _btreeStore.Select(Kvp => Kvp.Key).ToArray();
		}
	}

	public IEnumerable<long> Values {
		get {
			CheckAttached();
			return _btreeStore.Select(Kvp => Kvp.Value).ToArray();
		}
	}

	public bool ContainsKey(TKey key) {
		CheckAttached();
		return _btreeStore.ContainsKey(key);
	}

	public bool TryGetValue(TKey key, out long value) {
		CheckAttached();
		return _btreeStore.TryGetValue(key, out value);
	}

	public TKey Read(long index) {
		CheckAttached();
		return _pagedListStore.Read(index);
	}

	public byte[] ReadBytes(long index) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			_pagedListStore.ReadItemBytes(index, 0, null, out var Bytes);
			return Bytes;
		}
	}

	public void Add(long index, TKey key) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			Guard.Argument(index == _pagedListStore.Count, nameof(index), "Index mismatches with expected index in store");
			_btreeStore.BTree.Add(key, index);
			_pagedListStore.Add(key);
		}
	}

	public void Update(long index, TKey key) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			var OldKey = _pagedListStore.Read(index);
			_btreeStore.BTree.Remove(OldKey);
			_btreeStore.BTree.Add(key, index);
			_pagedListStore.Update(index, key);
		}
	}

	public void Insert(long index, TKey key) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			Guard.Ensure(!_btreeStore.BTree.ContainsKey(key));
			_pagedListStore.Insert(index, key);
			RebuildBTree();
		}
	}

	public void Remove(long index) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			_pagedListStore.RemoveAt(index);
			RebuildBTree();
		}
	}

	public void Reap(long index) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			var OldKey = _pagedListStore.Read(index);
			_btreeStore.BTree.Remove(OldKey);
		}
	}

	public void Clear() {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			_btreeStore.BTree.Clear();
			_pagedListStore.Clear();
		}
	}

	public IEnumerator<KeyValuePair<TKey, long>> GetEnumerator() {
		CheckAttached();
		using (Streams.EnterAccessScope())
			return _btreeStore.BTree.ToList().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}

	public long this[TKey key] {
		get {
			CheckAttached();
			using (Streams.EnterAccessScope())
				return _btreeStore.BTree[key];
		}
	}

	public override void Attach() {
		base.Attach();
		using (Streams.EnterAccessScope()) {
			if (_btreeStore.BTree.Count == 0 && _pagedListStore.Count > 0)
				RebuildBTree();
		}
	}

	private void RebuildBTree() {
		using var _ = Streams.EnterAccessScope();
		_btreeStore.BTree.Clear();
		var Reserved = Streams.Header.ReservedStreams;
		for (var I = 0L; I < _pagedListStore.Count; I++) {
			if (Streams.FastReadStreamDescriptorTraits(I + Reserved).HasFlag(ClusteredStreamTraits.Reaped))
				continue;
			var Key = _pagedListStore.Read(I);
			_btreeStore.BTree.Add(Key, I);
		}
	}
}
