// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework;

/// <summary>
/// An in-memory one-to-many lookup (multimap) backed by an <see cref="InMemoryBTree{K,V}"/>
/// with compound keys. Each logical entry <c>(key, value)</c> is stored as a unique compound key
/// <c>KeyValuePair&lt;K, V&gt;</c> inside the B-tree, with a dummy <see cref="byte"/> as the tree value.
/// </summary>
/// <typeparam name="K">Lookup key type.</typeparam>
/// <typeparam name="V">Lookup value type.</typeparam>
public class InMemoryBTreeLookup<K, V> {
	private readonly InMemoryBTree<KeyValuePair<K, V>, byte> _btree;
	private readonly IComparer<K> _keyComparer;
	private readonly V _minValue;
	private readonly V _maxValue;
	private int _distinctKeyCount;

	public InMemoryBTreeLookup(
		int order,
		IComparer<K> keyComparer,
		IComparer<V> valueComparer,
		V minValue,
		V maxValue) {
		Guard.ArgumentNotNull(keyComparer, nameof(keyComparer));
		Guard.ArgumentNotNull(valueComparer, nameof(valueComparer));
		_keyComparer = keyComparer;
		_minValue = minValue;
		_maxValue = maxValue;
		var CompoundComparer = new KeyValuePairComparer<K, V>(keyComparer, valueComparer);
		_btree = new InMemoryBTree<KeyValuePair<K, V>, byte>(order, CompoundComparer);
		_distinctKeyCount = 0;
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
		_btree.Add(new KeyValuePair<K, V>(key, value), 0);
		if (!WasPresent)
			_distinctKeyCount++;
	}

	/// <summary>
	/// Removes a specific key-value pair from the lookup.
	/// </summary>
	public bool Remove(K key, V value) {
		var Result = _btree.Remove(new KeyValuePair<K, V>(key, value));
		if (Result && !ContainsKey(key))
			_distinctKeyCount--;
		return Result;
	}

	/// <summary>
	/// Returns whether the lookup contains any entries for the given key.
	/// </summary>
	public bool ContainsKey(K key) {
		var Lower = new KeyValuePair<K, V>(key, _minValue);
		var Upper = new KeyValuePair<K, V>(key, _maxValue);
		return _btree.FindRange(Lower, Upper).Any();
	}

	/// <summary>
	/// Returns all values associated with the given key.
	/// </summary>
	public IEnumerable<V> GetValues(K key) {
		var Lower = new KeyValuePair<K, V>(key, _minValue);
		var Upper = new KeyValuePair<K, V>(key, _maxValue);
		return _btree.FindRange(Lower, Upper).Select(E => E.Key.Value);
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
			var EntryKey = Entry.Key.Key;
			var EntryValue = Entry.Key.Value;

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
}
