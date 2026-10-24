// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework;

/// <summary>
/// An in-memory one-to-many lookup (multimap) backed by a configured B-tree
/// with compound keys. Each logical entry <c>(key, value)</c> is stored as
/// <c>CompoundKey(key, groupIndex) → value</c> in the tree.
/// </summary>
/// <typeparam name="K">Lookup key type.</typeparam>
/// <typeparam name="V">Lookup value type.</typeparam>
public class InMemoryBTreeLookup<K, V> : IExtendedBTreeLookup<K, V> {
	private readonly IBTree<CompoundKey<K, int>, V> _btree;
	private readonly BTreeLookup<K, V> _lookup;

	public InMemoryBTreeLookup(IComparer<K> keyComparer)
		: this(Sphere10FrameworkDefaults.BTreeLookupOrder, keyComparer) {
	}

	public InMemoryBTreeLookup(int order, IComparer<K> keyComparer)
		: this(new BTreeSpec(BTreeType.BTreePlus, order), keyComparer) {
	}

	public InMemoryBTreeLookup(BTreeSpec bTreeSpec, IComparer<K> keyComparer) {
		Guard.ArgumentNotNull(bTreeSpec, nameof(bTreeSpec));
		Guard.ArgumentNotNull(keyComparer, nameof(keyComparer));
		Spec = bTreeSpec;
		var compoundComparer = new CompoundKeyComparer<K, int>(keyComparer, Comparer<int>.Default);
		_btree = Spec.Type == BTreeType.BTree
			? new InMemoryBTree<CompoundKey<K, int>, V>(Spec.Order, compoundComparer)
			: new InMemoryBTreePlus<CompoundKey<K, int>, V>(Spec.Order, compoundComparer);
		_lookup = new BTreeLookup<K, V>(_btree, keyComparer);
	}

	/// <summary>The order of the underlying B-tree.</summary>
	public int Order => _btree.Order;

	public BTreeSpec Spec { get; }

	/// <summary>
	/// The total number of entries (including duplicate keys with different values).
	/// </summary>
	public int TotalCount => _lookup.TotalCount;

	/// <summary>
	/// The number of distinct keys in the lookup.
	/// </summary>
	public int DistinctKeyCount => _lookup.DistinctKeyCount;

	/// <summary>
	/// Adds a key-value pair to the lookup.
	/// </summary>
	public void Add(K key, V value) => _lookup.Add(key, value);

	/// <summary>
	/// Removes a specific key-value pair from the lookup.
	/// </summary>
	public bool Remove(K key, V value) => _lookup.Remove(key, value);

	/// <summary>
	/// Removes all values associated with the given key.
	/// </summary>
	public void RemoveAll(K key) => _lookup.RemoveAll(key);

	/// <summary>
	/// Returns whether the lookup contains any entries for the given key.
	/// </summary>
	public bool ContainsKey(K key) => _lookup.ContainsKey(key);

	/// <summary>
	/// Returns all values associated with the given key.
	/// </summary>
	public IEnumerable<V> GetValues(K key) => _lookup.GetValues(key);

	/// <summary>
	/// Returns whether the lookup contains a specific key-value pair.
	/// </summary>
	public bool ContainsEntry(K key, V value) => _lookup.ContainsEntry(key, value);

	/// <summary>
	/// Removes all entries from the lookup.
	/// </summary>
	public void Clear() => _lookup.Clear();

	/// <summary>
	/// Enumerates all entries grouped by key.
	/// </summary>
	public IEnumerable<IGrouping<K, V>> EnumerateGroupings() => _lookup.EnumerateGroupings();

	/// <summary>
	/// Validates the integrity of the underlying B-tree.
	/// </summary>
	public bool Validate(out string error) => _lookup.Validate(out error);

	// Preserve the class enumeration mapping when subclasses add another enumerable interface.
	IEnumerator IEnumerable.GetEnumerator() => EnumerateGroupings().GetEnumerator();
}
