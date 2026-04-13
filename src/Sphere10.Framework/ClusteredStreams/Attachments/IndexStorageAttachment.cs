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
/// A scalable index storage attachment that persists the reverse index (data → positions) in
/// a stream-mapped B-tree lookup rather than an in-memory lookup. The forward mapping (position → data)
/// is stored in a <see cref="StreamPagedList{TData}"/>.
///
/// This is a <see cref="CompositeStorageAttachmentBase"/> that composes two child attachments:
/// a <see cref="PagedListStorageAttachment{TData}"/> for the paged list (forward mapping)
/// and a <see cref="BTreeLookupStorageAttachment{TKey,TValue}"/> for the B-tree lookup (reverse mapping).
///
/// This is the <see cref="ILookup{TKey,TElement}"/> analog of <see cref="UniqueKeyStorageAttachment{TKey}"/>.
/// </summary>
public class IndexStorageAttachment<TData> : CompositeStorageAttachmentBase, ILookup<TData, long> {
	private const int BTreeOrder = 64;

	private readonly PagedListStorageAttachment<TData> _pagedListStore;
	private readonly BTreeLookupStorageAttachment<TData, long> _btreeLookupStore;

	public IndexStorageAttachment(ClusteredStreams streams, string attachmentID, IItemSerializer<TData> datumSerializer, IEqualityComparer<TData> datumComparer)
		: this(
			streams,
			attachmentID,
			new PagedListStorageAttachment<TData>(streams, attachmentID + ".pagedList", datumSerializer),
			new BTreeLookupStorageAttachment<TData, long>(streams, attachmentID + ".btreeLookup", BTreeOrder, datumSerializer, PrimitiveSerializer<long>.Instance, Comparer<TData>.Default)
		) {
		Guard.ArgumentNotNull(datumSerializer, nameof(datumSerializer));
		Guard.Argument(datumSerializer.IsConstantSize, nameof(datumSerializer), "Data serializer must be a constant-length serializer.");
		Guard.ArgumentNotNull(datumComparer, nameof(datumComparer));
	}

	private IndexStorageAttachment(
		ClusteredStreams streams,
		string attachmentID,
		PagedListStorageAttachment<TData> pagedListStore,
		BTreeLookupStorageAttachment<TData, long> btreeLookupStore
	) : base(streams, attachmentID, pagedListStore, btreeLookupStore) {
		_pagedListStore = pagedListStore;
		_btreeLookupStore = btreeLookupStore;
	}

	public int Count => IsAttached ? _btreeLookupStore.BTreeLookup.DistinctKeyCount : 0;

	public bool Contains(TData key) {
		CheckAttached();
		using var _ = Streams.EnterAccessScope();
		return _btreeLookupStore.BTreeLookup.ContainsKey(key);
	}

	public IEnumerable<long> this[TData key] {
		get {
			CheckAttached();
			using var _ = Streams.EnterAccessScope();
			return _btreeLookupStore.BTreeLookup.GetValues(key).ToArray();
		}
	}

	public TData Read(long index) {
		CheckAttached();
		using var _ = Streams.EnterAccessScope();
		return _pagedListStore.Read(index);
	}

	public byte[] ReadBytes(long index) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			_pagedListStore.ReadItemBytes(index, 0, null, out var Bytes);
			return Bytes;
		}
	}

	public void Add(long index, TData data) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			Guard.Argument(index == _pagedListStore.Count, nameof(index), "Index mismatches with expected index in store");
			_btreeLookupStore.BTreeLookup.Add(data, index);
			_pagedListStore.Add(data);
		}
	}

	public void Update(long index, TData data) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			var OldData = _pagedListStore.Read(index);
			_btreeLookupStore.BTreeLookup.Remove(OldData, index);
			_btreeLookupStore.BTreeLookup.Add(data, index);
			_pagedListStore.Update(index, data);
		}
	}

	public void Insert(long index, TData data) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			_pagedListStore.Insert(index, data);
			RebuildBTreeLookup();
		}
	}

	public void Remove(long index) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			_pagedListStore.RemoveAt(index);
			RebuildBTreeLookup();
		}
	}

	public void Reap(long index) {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			var OldData = _pagedListStore.Read(index);
			_btreeLookupStore.BTreeLookup.Remove(OldData, index);
		}
	}

	public void Clear() {
		CheckAttached();
		using (Streams.EnterAccessScope()) {
			_btreeLookupStore.BTreeLookup.Clear();
			_pagedListStore.Clear();
		}
	}

	public IEnumerator<IGrouping<TData, long>> GetEnumerator() {
		CheckAttached();
		using (Streams.EnterAccessScope())
			return _btreeLookupStore.BTreeLookup.EnumerateGroupings().ToList().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}

	public override void Attach() {
		base.Attach();
		using (Streams.EnterAccessScope()) {
			if (_btreeLookupStore.BTreeLookup.TotalCount == 0 && _pagedListStore.Count > 0)
				RebuildBTreeLookup();
		}
	}

	private void RebuildBTreeLookup() {
		using var _ = Streams.EnterAccessScope();
		_btreeLookupStore.BTreeLookup.Clear();
		var Reserved = Streams.Header.ReservedStreams;
		for (var I = 0L; I < _pagedListStore.Count; I++) {
			if (Streams.FastReadStreamDescriptorTraits(I + Reserved).HasFlag(ClusteredStreamTraits.Reaped))
				continue;
			var Data = _pagedListStore.Read(I);
			_btreeLookupStore.BTreeLookup.Add(Data, I);
		}
	}
}

