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

namespace Sphere10.Framework;

public class BTreeStorageAttachment<TKey, TValue> : BTreeStorageAttachmentBase<TKey, TValue>, IDictionary<TKey, TValue> {

	public BTreeStorageAttachment(
		ClusteredStreams streams,
		string attachmentID,
		int order,
		IItemSerializer<TKey> keySerializer,
		IItemSerializer<TValue> valueSerializer,
		IComparer<TKey> keyComparer
	) : this(streams, attachmentID, keySerializer, valueSerializer, keyComparer, new BTreeSpec(BTreeType.BTree, order)) {
	}

	public BTreeStorageAttachment(
		ClusteredStreams streams,
		string attachmentID,
		IItemSerializer<TKey> keySerializer,
		IItemSerializer<TValue> valueSerializer,
		IComparer<TKey> keyComparer,
		BTreeSpec bTreeSpec
	) : base(streams, attachmentID, keySerializer, valueSerializer, keyComparer, bTreeSpec) {
	}

	public new StreamMappedBTree<TKey, TValue> BTree => base.BTree;

	/// <summary>The selected ordered dictionary, independent of its node layout.</summary>
	public new BTreeBase<TKey, TValue> Tree => base.Tree;

	#region IsReadOnly

	public bool IsReadOnly {
		get {
			CheckAttached();
			using var _ = Streams.EnterAccessScope();
			return Tree.IsReadOnly;
		}
	}

	#endregion

	#region Count

	public int Count {
		get {
			CheckAttached();
			using var _ = Streams.EnterAccessScope();
			return Tree.Count;
		}
	}

	#endregion

	#region Keys / Values

	public ICollection<TKey> Keys {
		get {
			CheckAttached();
			using var _ = Streams.EnterAccessScope();
			return Tree.Keys;
		}
	}

	public ICollection<TValue> Values {
		get {
			CheckAttached();
			using var _ = Streams.EnterAccessScope();
			return Tree.Values;
		}
	}

	#endregion

	#region Indexer

	public TValue this[TKey key] {
		get {
			CheckAttached();
			using var _ = Streams.EnterAccessScope();
			return Tree[key];
		}
		set {
			CheckAttached();
			using var _ = Streams.EnterAccessScope();
			Tree[key] = value;
		}
	}

	#endregion

	#region Add

	public void Add(TKey key, TValue value) {
		CheckAttached();
		using var _ = Streams.EnterAccessScope();
		Tree.Add(key, value);
	}

	public void Add(KeyValuePair<TKey, TValue> item) {
		CheckAttached();
		using var _ = Streams.EnterAccessScope();
		Tree.Add(item);
	}

	#endregion

	#region ContainsKey / Contains

	public bool ContainsKey(TKey key) {
		CheckAttached();
		using var _ = Streams.EnterAccessScope();
		return Tree.ContainsKey(key);
	}

	public bool Contains(KeyValuePair<TKey, TValue> item) {
		CheckAttached();
		using var _ = Streams.EnterAccessScope();
		return Tree.Contains(item);
	}

	#endregion

	#region TryGetValue

	public bool TryGetValue(TKey key, out TValue value) {
		CheckAttached();
		using var _ = Streams.EnterAccessScope();
		return Tree.TryGetValue(key, out value);
	}

	#endregion

	#region Remove

	public bool Remove(TKey key) {
		CheckAttached();
		using var _ = Streams.EnterAccessScope();
		return Tree.Remove(key);
	}

	public bool Remove(KeyValuePair<TKey, TValue> item) {
		CheckAttached();
		using var _ = Streams.EnterAccessScope();
		return Tree.Remove(item);
	}

	#endregion

	#region Clear

	public void Clear() {
		CheckAttached();
		using var _ = Streams.EnterAccessScope();
		Tree.Clear();
	}

	#endregion

	#region CopyTo

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
		CheckAttached();
		using var _ = Streams.EnterAccessScope();
		Tree.CopyTo(array, arrayIndex);
	}

	#endregion

	#region GetEnumerator

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
		CheckAttached();
		var scope = Streams.EnterAccessScope();
		try {
			return
				Tree
					.GetEnumerator()
					.OnDispose(scope.Dispose);
		} catch {
			scope.Dispose();
			throw;
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	#endregion

}
