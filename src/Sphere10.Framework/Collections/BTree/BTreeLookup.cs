// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework;

/// <summary>
/// A one-to-many lookup (multimap) backed by a <see cref="BTreeBase{K,V}"/> with compound keys.
/// Each logical entry <c>(key, value)</c> is stored as <c>CompoundKey(key, groupIndex) → value</c>
/// in the tree, where <c>groupIndex</c> is a zero-based ordinal within the key's group.
/// This eliminates the need for min/max sentinel values and works with any type.
/// </summary>
/// <typeparam name="K">Lookup key type.</typeparam>
/// <typeparam name="V">Lookup value type.</typeparam>
public class BTreeLookup<K, V> : IBTreeLookup<K, V> {
	private readonly BTreeBase<CompoundKey<K, int>, V> _btree;
	private readonly IComparer<K> _keyComparer;
	private readonly IEqualityComparer<V> _valueEqualityComparer;
	private int _distinctKeyCount;

	public BTreeLookup(BTreeBase<CompoundKey<K, int>, V> btree, IComparer<K> keyComparer, IEqualityComparer<V> valueEqualityComparer = null) {
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
		var WasPresent = ContainsKey(key);
		var NextIndex = TryFindMaxGroupIndex(key, out var MaxIndex) ? MaxIndex + 1 : 0;
		_btree.Add(new CompoundKey<K, int>(key, NextIndex), value);
		if (!WasPresent)
			_distinctKeyCount++;
	}

	/// <summary>
	/// Removes a specific key-value pair from the lookup.
	/// </summary>
	public bool Remove(K key, V value) {
		// Scan the group to find the entry with the matching value and determine max index
		var FoundIndex = -1;
		var MaxIndex = -1;
		for (var I = 0; _btree.TryGetValue(new CompoundKey<K, int>(key, I), out var Val); I++) {
			MaxIndex = I;
			if (FoundIndex < 0 && _valueEqualityComparer.Equals(Val, value))
				FoundIndex = I;
		}

		if (FoundIndex < 0)
			return false;

		// Remove the found entry
		_btree.Remove(new CompoundKey<K, int>(key, FoundIndex));

		// Re-index subsequent entries to keep indices contiguous
		for (var J = FoundIndex + 1; J <= MaxIndex; J++) {
			var Val = _btree[new CompoundKey<K, int>(key, J)];
			_btree.Remove(new CompoundKey<K, int>(key, J));
			_btree.Add(new CompoundKey<K, int>(key, J - 1), Val);
		}

		if (!ContainsKey(key))
			_distinctKeyCount--;

		return true;
	}

	/// <summary>
	/// Removes all values associated with the given key.
	/// </summary>
	public void RemoveAll(K key) {
		if (!TryFindMaxGroupIndex(key, out var MaxIndex))
			return;

		for (var I = MaxIndex; I >= 0; I--)
			_btree.Remove(new CompoundKey<K, int>(key, I));

		_distinctKeyCount--;
	}

	/// <summary>
	/// Returns whether the lookup contains any entries for the given key.
	/// </summary>
	public bool ContainsKey(K key) {
		return _btree.ContainsKey(new CompoundKey<K, int>(key, 0));
	}

	/// <summary>
	/// Returns all values associated with the given key.
	/// </summary>
	public IEnumerable<V> GetValues(K key) {
		for (var I = 0; _btree.TryGetValue(new CompoundKey<K, int>(key, I), out var Val); I++)
			yield return Val;
	}

	/// <summary>
	/// Returns whether the lookup contains a specific key-value pair.
	/// </summary>
	public bool ContainsEntry(K key, V value) {
		for (var I = 0; _btree.TryGetValue(new CompoundKey<K, int>(key, I), out var Val); I++)
			if (_valueEqualityComparer.Equals(Val, value))
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
		K CurrentKey = default;
		List<V> CurrentValues = null;
		var HasCurrent = false;

		foreach (var Entry in _btree) {
			var EntryKey = Entry.Key.Key1;
			var EntryValue = Entry.Value;

			if (!HasCurrent || _keyComparer.Compare(CurrentKey, EntryKey) != 0) {
				if (HasCurrent)
					yield return new Grouping<K, V>(CurrentKey, CurrentValues);

				CurrentKey = EntryKey;
				CurrentValues = new List<V>();
				HasCurrent = true;
			}

			CurrentValues.Add(EntryValue);
		}

		if (HasCurrent)
			yield return new Grouping<K, V>(CurrentKey, CurrentValues);
	}

	/// <summary>
	/// Validates the integrity of the underlying B-tree.
	/// </summary>
	public bool Validate(out string error) => _btree.Validate(out error);

	#region Explicit ILookup / IEnumerable

	int ILookup<K, V>.Count => DistinctKeyCount;

	IEnumerable<V> ILookup<K, V>.this[K key] => GetValues(key);

	bool ILookup<K, V>.Contains(K key) => ContainsKey(key);

	IEnumerator<IGrouping<K, V>> IEnumerable<IGrouping<K, V>>.GetEnumerator() => EnumerateGroupings().GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => EnumerateGroupings().GetEnumerator();

	#endregion

	private bool TryFindMaxGroupIndex(K key, out int maxIndex) {
		maxIndex = -1;
		for (var I = 0; _btree.TryGetValue(new CompoundKey<K, int>(key, I), out _); I++)
			maxIndex = I;
		return maxIndex >= 0;
	}

	private int ComputeDistinctKeyCount() {
		K PrevKey = default;
		var Count = 0;
		var First = true;
		foreach (var Entry in _btree) {
			if (First || _keyComparer.Compare(PrevKey, Entry.Key.Key1) != 0) {
				Count++;
				PrevKey = Entry.Key.Key1;
				First = false;
			}
		}
		return Count;
	}
}
