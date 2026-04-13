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
/// A <see cref="ClusteredStreamsAttachmentBase"/> that exposes a stream-mapped B-tree-backed one-to-many
/// lookup as an <see cref="ILookup{TKey, TValue}"/>. This is the ILookup analog of
/// <see cref="BTreeStorageAttachment{TKey,TValue}"/>.
/// </summary>
public class BTreeLookupStorageAttachment<TKey, TValue> : BTreeLookupStorageAttachmentBase<TKey, TValue>, ILookup<TKey, TValue> {

	public BTreeLookupStorageAttachment(
		ClusteredStreams streams,
		string attachmentID,
		int order,
		IItemSerializer<TKey> keySerializer,
		IItemSerializer<TValue> valueSerializer,
		IComparer<TKey> keyComparer,
		IComparer<TValue> valueComparer,
		TValue minValue,
		TValue maxValue
	) : base(streams, attachmentID, order, keySerializer, valueSerializer, keyComparer, valueComparer, minValue, maxValue) {
	}

	public new StreamMappedBTreeLookup<TKey, TValue> BTreeLookup => base.BTreeLookup;

	#region Count

	public int Count {
		get {
			CheckAttached();
			using var _ = Streams.EnterAccessScope();
			return BTreeLookup.DistinctKeyCount;
		}
	}

	#endregion

	#region Contains

	public bool Contains(TKey key) {
		CheckAttached();
		using var _ = Streams.EnterAccessScope();
		return BTreeLookup.ContainsKey(key);
	}

	#endregion

	#region Indexer

	public IEnumerable<TValue> this[TKey key] {
		get {
			CheckAttached();
			using var _ = Streams.EnterAccessScope();
			return BTreeLookup.GetValues(key).ToArray();
		}
	}

	#endregion

	#region Add / Remove

	public void Add(TKey key, TValue value) {
		CheckAttached();
		using var _ = Streams.EnterAccessScope();
		BTreeLookup.Add(key, value);
	}

	public bool Remove(TKey key, TValue value) {
		CheckAttached();
		using var _ = Streams.EnterAccessScope();
		return BTreeLookup.Remove(key, value);
	}

	public void RemoveAll(TKey key) {
		CheckAttached();
		using var _ = Streams.EnterAccessScope();
		BTreeLookup.RemoveAll(key);
	}

	#endregion

	#region Clear

	public void Clear() {
		CheckAttached();
		using var _ = Streams.EnterAccessScope();
		BTreeLookup.Clear();
	}

	#endregion

	#region GetEnumerator

	public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator() {
		CheckAttached();
		var Scope = Streams.EnterAccessScope();
		try {
			return
				BTreeLookup
					.EnumerateGroupings()
					.GetEnumerator()
					.OnDispose(Scope.Dispose);
		} catch {
			Scope.Dispose();
			throw;
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	#endregion
}
