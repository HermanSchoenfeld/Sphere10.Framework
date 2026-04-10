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
/// is still stored in the base <see cref="PagedListStorageAttachmentBase{TKey}"/>.
///
/// A companion <see cref="ClusteredStreamsAttachmentBase"/> is registered on the same
/// <see cref="ClusteredStreams"/> to provide a dedicated stream for the B-tree.
/// </summary>
public class UniqueKeyStorageAttachment<TKey> : PagedListStorageAttachmentBase<TKey>, IReadOnlyDictionary<TKey, long> {
	private const int BTreeOrder = 64;

	private readonly IEqualityComparer<TKey> _keyComparer;
	private readonly BTreeCompanionAttachment _btreeCompanion;
	private StreamMappedBTree<TKey, long> _btree;

	public UniqueKeyStorageAttachment(ClusteredStreams streams, string attachmentID, IItemSerializer<TKey> keySerializer, IEqualityComparer<TKey> keyComparer)
		: base(streams, attachmentID, keySerializer) {
		Guard.ArgumentNotNull(keyComparer, nameof(keyComparer));
		_keyComparer = keyComparer;

		// Register a companion attachment to hold the BTree's persistent storage
		_btreeCompanion = new BTreeCompanionAttachment(streams, attachmentID + ".btree");
		streams.RegisterAttachment(_btreeCompanion);
	}

	public int Count => _btree?.Count ?? 0;

	public IEnumerable<TKey> Keys {
		get {
			CheckAttached();
			return _btree.Select(Kvp => Kvp.Key);
		}
	}

	public IEnumerable<long> Values {
		get {
			CheckAttached();
			return _btree.Select(Kvp => Kvp.Value);
		}
	}

	public bool ContainsKey(TKey key) {
		CheckAttached();
		return _btree.ContainsKey(key);
	}

	public bool TryGetValue(TKey key, out long value) {
		CheckAttached();
		return _btree.TryGetValue(key, out value);
	}

	public TKey Read(long index) {
		CheckAttached();
		return PagedList.Read(index);
	}

	public byte[] ReadBytes(long index) {
		CheckAttached();
		PagedList.ReadItemBytes(index, 0, null, out var bytes);
		return bytes;
	}

	public void Add(long index, TKey key) {
		CheckAttached();
		Guard.Argument(index == PagedList.Count, nameof(index), "Index mismatches with expected index in store");
		_btree.Add(key, index);
		PagedList.Add(key);
	}

	public void Update(long index, TKey key) {
		CheckAttached();
		var OldKey = PagedList.Read(index);
		_btree.Remove(OldKey);
		_btree.Add(key, index);
		PagedList.Update(index, key);
	}

	public void Insert(long index, TKey key) {
		CheckAttached();
		Guard.Ensure(!_btree.ContainsKey(key));
		PagedList.Insert(index, key);
		RebuildBTree();
	}

	public void Remove(long index) {
		CheckAttached();
		PagedList.RemoveAt(index);
		RebuildBTree();
	}

	public void Reap(long index) {
		CheckAttached();
		var OldKey = PagedList.Read(index);
		_btree.Remove(OldKey);
	}

	public void Clear() {
		CheckAttached();
		_btree.Clear();
		PagedList.Clear();
	}

	public IEnumerator<KeyValuePair<TKey, long>> GetEnumerator() {
		CheckAttached();
		return _btree.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}

	public long this[TKey key] {
		get {
			CheckAttached();
			return _btree[key];
		}
	}

	protected override void AttachInternal() {
		base.AttachInternal();
		_btree = new StreamMappedBTree<TKey, long>(
			BTreeOrder,
			_btreeCompanion.Stream,
			DatumSerializer,
			PrimitiveSerializer<long>.Instance,
			Comparer<TKey>.Default
		);
		if (_btree.Count == 0 && PagedList.Count > 0)
			RebuildBTree();
	}

	protected override void DetachInternal() {
		_btree?.Dispose();
		_btree = null;
		base.DetachInternal();
	}

	public override void Flush() {
		base.Flush();
		_btreeCompanion.Flush();
	}

	private void RebuildBTree() {
		_btree.Clear();
		using var _ = Streams.EnterAccessScope();
		var Reserved = Streams.Header.ReservedStreams;
		for (var I = 0L; I < PagedList.Count; I++) {
			if (Streams.FastReadStreamDescriptorTraits(I + Reserved).HasFlag(ClusteredStreamTraits.Reaped))
				continue;
			var Key = PagedList.Read(I);
			_btree.Add(Key, I);
		}
	}

	/// <summary>
	/// A lightweight companion attachment that provides a dedicated stream for the B-tree storage.
	/// It holds no logic — only exposes its <see cref="Stream"/> for the parent to use.
	/// </summary>
	private sealed class BTreeCompanionAttachment : ClusteredStreamsAttachmentBase {
		public BTreeCompanionAttachment(ClusteredStreams streams, string attachmentID)
			: base(streams, attachmentID) {
		}

		public System.IO.Stream Stream => AttachmentStream;

		protected override void AttachInternal() {
			// No-op: the parent attachment manages the BTree lifecycle
		}

		protected override void VerifyIntegrity() {
		}

		protected override void DetachInternal() {
		}
	}
}

