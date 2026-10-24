// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
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
	private readonly PagedListStorageAttachment<TKey> _pagedListStore;
	private readonly BTreeStorageAttachment<TKey, long> _btreeStore;

	public UniqueKeyStorageAttachment(ClusteredStreams streams, string attachmentID, IItemSerializer<TKey> keySerializer, IEqualityComparer<TKey> keyComparer)
		: this(streams, attachmentID, keySerializer, keyComparer, Sphere10FrameworkDefaults.IndexBTreeOrder) {
	}

	public UniqueKeyStorageAttachment(
		ClusteredStreams streams,
		string attachmentID,
		IItemSerializer<TKey> keySerializer,
		IEqualityComparer<TKey> keyComparer,
		int bTreeOrder = Sphere10FrameworkDefaults.IndexBTreeOrder
	)
		: this(streams, attachmentID, keySerializer, keyComparer, new BTreeSpec(BTreeType.BTree, bTreeOrder)) {
	}

	public UniqueKeyStorageAttachment(
		ClusteredStreams streams,
		string attachmentID,
		IItemSerializer<TKey> keySerializer,
		IEqualityComparer<TKey> keyComparer,
		BTreeSpec bTreeSpec
	)
		: this(streams, attachmentID, keySerializer, Comparer<TKey>.Default, bTreeSpec) {
		Guard.ArgumentNotNull(keyComparer, nameof(keyComparer));
	}

	public UniqueKeyStorageAttachment(ClusteredStreams streams, string attachmentID, IItemSerializer<TKey> keySerializer, IComparer<TKey> keyComparer)
		: this(streams, attachmentID, keySerializer, keyComparer, Sphere10FrameworkDefaults.IndexBTreeOrder) {
	}

	public UniqueKeyStorageAttachment(
		ClusteredStreams streams,
		string attachmentID,
		IItemSerializer<TKey> keySerializer,
		IComparer<TKey> keyComparer,
		int bTreeOrder = Sphere10FrameworkDefaults.IndexBTreeOrder
	)
		: this(streams, attachmentID, keySerializer, keyComparer, new BTreeSpec(BTreeType.BTree, bTreeOrder)) {
	}

	public UniqueKeyStorageAttachment(
		ClusteredStreams streams,
		string attachmentID,
		IItemSerializer<TKey> keySerializer,
		IComparer<TKey> keyComparer,
		BTreeSpec bTreeSpec
	)
		: this(
			streams,
			attachmentID,
			new PagedListStorageAttachment<TKey>(streams, attachmentID + ".pagedList", keySerializer),
			new BTreeStorageAttachment<TKey, long>(streams, attachmentID + ".btree", keySerializer, PrimitiveSerializer<long>.Instance, keyComparer ?? Comparer<TKey>.Default, bTreeSpec)
		) {
		Guard.ArgumentNotNull(keySerializer, nameof(keySerializer));
		Guard.Argument(keySerializer.IsConstantSize, nameof(keySerializer), "Key serializer must be a constant-length serializer.");
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

	public BTreeSpec Spec => _btreeStore.Spec;

	public int Count => IsAttached ? _btreeStore.Tree.Count : 0;

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
			_btreeStore.Tree.Add(key, index);
			_pagedListStore.Add(key);
		}
	}

	public void Update(long index, TKey key) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			var OldKey = _pagedListStore.Read(index);
			_btreeStore.Tree.Remove(OldKey);
			_btreeStore.Tree.Add(key, index);
			_pagedListStore.Update(index, key);
		}
	}

	public void Insert(long index, TKey key) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			Guard.Ensure(!_btreeStore.Tree.ContainsKey(key));
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
			_btreeStore.Tree.Remove(OldKey);
		}
	}

	public void Clear() {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			_btreeStore.Tree.Clear();
			_pagedListStore.Clear();
		}
	}

	public IEnumerator<KeyValuePair<TKey, long>> GetEnumerator() {
		CheckAttached();
		using (Streams.EnterAccessScope())
			return _btreeStore.Tree.ToList().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}

	public long this[TKey key] {
		get {
			CheckAttached();
			using (Streams.EnterAccessScope())
				return _btreeStore.Tree[key];
		}
	}

	public override void Attach() {
		base.Attach();
		using (Streams.EnterAccessScope()) {
			if (_btreeStore.Tree.Count == 0 && _pagedListStore.Count > 0)
				RebuildBTree();
		}
	}

	private void RebuildBTree() {
		using var _ = Streams.EnterAccessScope();
		_btreeStore.Tree.Clear();
		var Reserved = Streams.Header.ReservedStreams;
		for (var I = 0L; I < _pagedListStore.Count; I++) {
			if (Streams.FastReadStreamDescriptorTraits(I + Reserved).HasFlag(ClusteredStreamTraits.Reaped))
				continue;
			var Key = _pagedListStore.Read(I);
			_btreeStore.Tree.Add(Key, I);
		}
	}
}
