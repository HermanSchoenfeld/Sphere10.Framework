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
/// A one-to-many lookup (multimap) backed by an <see cref="IBTree{K,V}"/> with compound keys.
/// Each logical entry <c>(key, value)</c> is stored as <c>CompoundKey(key, groupIndex) → value</c>
/// in the tree, where <c>groupIndex</c> is a zero-based ordinal within the key's group.
/// This eliminates the need for min/max sentinel values and works with any type.
/// </summary>
/// <typeparam name="K">Lookup key type.</typeparam>
/// <typeparam name="V">Lookup value type.</typeparam>
public class BTreeLookup<K, V> : IExtendedBTreeLookup<K, V> {
	private readonly IBTree<CompoundKey<K, int>, V> _btree;
	private readonly IComparer<K> _keyComparer;
	private readonly IEqualityComparer<V> _valueEqualityComparer;
	private int _distinctKeyCount;

	public BTreeLookup(BTreeBase<CompoundKey<K, int>, V> btree, IComparer<K> keyComparer, IEqualityComparer<V> valueEqualityComparer = null)
		: this((IBTree<CompoundKey<K, int>, V>)btree, keyComparer, valueEqualityComparer) {
	}

	public BTreeLookup(IBTree<CompoundKey<K, int>, V> btree, IComparer<K> keyComparer, IEqualityComparer<V> valueEqualityComparer = null) {
		Guard.ArgumentNotNull(btree, nameof(btree));
		Guard.ArgumentNotNull(keyComparer, nameof(keyComparer));
		_btree = btree;
		_keyComparer = keyComparer;
		_valueEqualityComparer = valueEqualityComparer ?? EqualityComparer<V>.Default;
		_distinctKeyCount = ComputeDistinctKeyCount();
	}

	/// <summary>
	/// The total number of entries (including duplicate keys with different values).
	/// </summary>
	public int TotalCount => _btree.Count;

	/// <summary>
	/// The number of distinct keys in the lookup.
	/// </summary>
	public int DistinctKeyCount => _distinctKeyCount;

	/// <summary>
	/// Adds a key-value pair to the lookup.
	/// </summary>
	public void Add(K key, V value) {
		var wasPresent = ContainsKey(key);
		var nextIndex = TryFindMaxGroupIndex(key, out var maxIndex) ? maxIndex + 1 : 0;
		_btree.Add(CompoundKey.From(key, nextIndex), value);
		if (!wasPresent)
			_distinctKeyCount++;
	}

	/// <summary>
	/// Removes a specific key-value pair from the lookup.
	/// </summary>
	public bool Remove(K key, V value) {
		// Scan the group to find the entry with the matching value and determine max index
		var foundIndex = -1;
		var maxIndex = -1;
		for (var index = 0; _btree.TryGetValue(CompoundKey.From(key, index), out var entryValue); index++) {
			maxIndex = index;
			if (foundIndex < 0 && _valueEqualityComparer.Equals(entryValue, value))
				foundIndex = index;
		}

		if (foundIndex < 0)
			return false;

		// Remove the found entry
		_btree.Remove(CompoundKey.From(key, foundIndex));

		// Re-index subsequent entries to keep indices contiguous
		for (var index = foundIndex + 1; index <= maxIndex; index++) {
			var entryValue = _btree[CompoundKey.From(key, index)];
			_btree.Remove(CompoundKey.From(key, index));
			_btree.Add(CompoundKey.From(key, index - 1), entryValue);
		}

		if (!ContainsKey(key))
			_distinctKeyCount--;

		return true;
	}

	/// <summary>
	/// Removes all values associated with the given key.
	/// </summary>
	public void RemoveAll(K key) {
		if (!TryFindMaxGroupIndex(key, out var maxIndex))
			return;

		for (var index = maxIndex; index >= 0; index--)
			_btree.Remove(CompoundKey.From(key, index));

		_distinctKeyCount--;
	}

	/// <summary>
	/// Returns whether the lookup contains any entries for the given key.
	/// </summary>
	public bool ContainsKey(K key) {
		return _btree.ContainsKey(CompoundKey.From(key, 0));
	}

	/// <summary>
	/// Returns all values associated with the given key.
	/// </summary>
	public IEnumerable<V> GetValues(K key) {
		for (var index = 0; _btree.TryGetValue(CompoundKey.From(key, index), out var value); index++)
			yield return value;
	}

	/// <summary>
	/// Returns whether the lookup contains a specific key-value pair.
	/// </summary>
	public bool ContainsEntry(K key, V value) {
		for (var index = 0; _btree.TryGetValue(CompoundKey.From(key, index), out var entryValue); index++)
			if (_valueEqualityComparer.Equals(entryValue, value))
				return true;
		return false;
	}

	/// <summary>
	/// Removes all entries from the lookup.
	/// </summary>
	public void Clear() {
		_btree.Clear();
		_distinctKeyCount = 0;
	}

	/// <summary>
	/// Enumerates all entries grouped by key.
	/// </summary>
	public IEnumerable<IGrouping<K, V>> EnumerateGroupings() {
		K currentKey = default;
		List<V> currentValues = null;
		var hasCurrent = false;

		foreach (var entry in _btree) {
			var entryKey = entry.Key.Key1;
			var entryValue = entry.Value;

			if (!hasCurrent || _keyComparer.Compare(currentKey, entryKey) != 0) {
				if (hasCurrent)
					yield return new Grouping<K, V>(currentKey, currentValues);

				currentKey = entryKey;
				currentValues = new List<V>();
				hasCurrent = true;
			}

			currentValues.Add(entryValue);
		}

		if (hasCurrent)
			yield return new Grouping<K, V>(currentKey, currentValues);
	}

	/// <summary>
	/// Validates the integrity of the underlying B-tree.
	/// </summary>
	public bool Validate(out string error) => _btree.Validate(out error);

	// Preserve the class enumeration mapping when subclasses add another enumerable interface.
	IEnumerator IEnumerable.GetEnumerator() => EnumerateGroupings().GetEnumerator();

	private bool TryFindMaxGroupIndex(K key, out int maxIndex) {
		maxIndex = -1;
		for (var index = 0; _btree.TryGetValue(CompoundKey.From(key, index), out _); index++)
			maxIndex = index;
		return maxIndex >= 0;
	}

	private int ComputeDistinctKeyCount() {
		K previousKey = default;
		var count = 0;
		var first = true;
		foreach (var entry in _btree) {
			if (first || _keyComparer.Compare(previousKey, entry.Key.Key1) != 0) {
				count++;
				previousKey = entry.Key.Key1;
				first = false;
			}
		}
		return count;
	}
}
