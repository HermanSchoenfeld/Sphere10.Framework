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
using System.IO;
using System.Linq;

namespace Sphere10.Framework;

/// <summary>
/// A stream-mapped one-to-many lookup (multimap) backed by a <see cref="StreamMappedBTree{K,V}"/>
/// with compound keys. Each logical entry <c>(key, value)</c> is stored as a unique compound key
/// <c>KeyValuePair&lt;K, V&gt;</c> inside the B-tree, with a dummy <see cref="byte"/> as the tree value.
/// The compound comparer sorts by key first, then by value, ensuring all values for a given key
/// are contiguous in the sorted order and can be retrieved efficiently via range queries.
/// </summary>
/// <typeparam name="K">Lookup key type. Must have a constant-size serializer.</typeparam>
/// <typeparam name="V">Lookup value type. Must have a constant-size serializer.</typeparam>
public class StreamMappedBTreeLookup<K, V> : IDisposable {
	private readonly StreamMappedBTree<KeyValuePair<K, V>, byte> _btree;
	private readonly IComparer<K> _keyComparer;
	private readonly V _minValue;
	private readonly V _maxValue;
	private int _distinctKeyCount;

	/// <summary>
	/// Creates a new stream-mapped B-tree lookup or opens an existing one from the given stream.
	/// </summary>
	/// <param name="order">B-tree order (minimum 3).</param>
	/// <param name="stream">Backing stream for persistent storage.</param>
	/// <param name="keySerializer">Constant-size serializer for lookup keys.</param>
	/// <param name="valueSerializer">Constant-size serializer for lookup values.</param>
	/// <param name="keyComparer">Comparer for lookup keys.</param>
	/// <param name="valueComparer">Comparer for lookup values (used as tiebreaker in compound key).</param>
	/// <param name="minValue">Minimum possible value of <typeparamref name="V"/> (e.g. <see cref="long.MinValue"/>).</param>
	/// <param name="maxValue">Maximum possible value of <typeparamref name="V"/> (e.g. <see cref="long.MaxValue"/>).</param>
	/// <param name="endianness">Byte order for all binary encoding.</param>
	public StreamMappedBTreeLookup(
		int order,
		Stream stream,
		IItemSerializer<K> keySerializer,
		IItemSerializer<V> valueSerializer,
		IComparer<K> keyComparer,
		IComparer<V> valueComparer,
		V minValue,
		V maxValue,
		Endianness endianness = Sphere10FrameworkDefaults.Endianness) {
		Guard.ArgumentNotNull(stream, nameof(stream));
		Guard.ArgumentNotNull(keySerializer, nameof(keySerializer));
		Guard.ArgumentNotNull(valueSerializer, nameof(valueSerializer));
		Guard.ArgumentNotNull(keyComparer, nameof(keyComparer));
		Guard.ArgumentNotNull(valueComparer, nameof(valueComparer));

		_keyComparer = keyComparer;
		_minValue = minValue;
		_maxValue = maxValue;

		var CompoundSerializer = new ConstantSizeKeyValuePairSerializer<K, V>(keySerializer, valueSerializer);
		var CompoundComparer = new KeyValuePairComparer<K, V>(keyComparer, valueComparer);

		_btree = new StreamMappedBTree<KeyValuePair<K, V>, byte>(
			order,
			stream,
			CompoundSerializer,
			PrimitiveSerializer<byte>.Instance,
			CompoundComparer,
			endianness
		);

		// Compute distinct key count from existing data
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
	/// The underlying B-tree stream.
	/// </summary>
	public Stream Stream => _btree.Stream;

	/// <summary>
	/// Adds a key-value pair to the lookup. Multiple values may be associated with the same key.
	/// </summary>
	public void Add(K key, V value) {
		var WasPresent = ContainsKey(key);
		_btree.Add(new KeyValuePair<K, V>(key, value), 0);
		if (!WasPresent)
			_distinctKeyCount++;
	}

	/// <summary>
	/// Removes a specific key-value pair from the lookup. Returns true if the pair was found and removed.
	/// </summary>
	public bool Remove(K key, V value) {
		var Result = _btree.Remove(new KeyValuePair<K, V>(key, value));
		if (Result && !ContainsKey(key))
			_distinctKeyCount--;
		return Result;
	}

	/// <summary>
	/// Removes all values associated with the given key.
	/// </summary>
	public void RemoveAll(K key) {
		var Values = GetValues(key).ToArray();
		foreach (var Val in Values)
			_btree.Remove(new KeyValuePair<K, V>(key, Val));
		if (Values.Length > 0)
			_distinctKeyCount--;
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
	/// Returns all values associated with the given key. Returns empty if key not found.
	/// </summary>
	public IEnumerable<V> GetValues(K key) {
		var Lower = new KeyValuePair<K, V>(key, _minValue);
		var Upper = new KeyValuePair<K, V>(key, _maxValue);
		return _btree.FindRange(Lower, Upper).Select(E => E.Key.Value);
	}

	/// <summary>
	/// Returns whether the lookup contains a specific key-value pair.
	/// </summary>
	public bool ContainsEntry(K key, V value) {
		return _btree.ContainsKey(new KeyValuePair<K, V>(key, value));
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

	public void Dispose() {
		_btree?.Dispose();
	}

	private int ComputeDistinctKeyCount() {
		K PrevKey = default;
		var Count = 0;
		var First = true;
		foreach (var Entry in _btree) {
			if (First || _keyComparer.Compare(PrevKey, Entry.Key.Key) != 0) {
				Count++;
				PrevKey = Entry.Key.Key;
				First = false;
			}
		}
		return Count;
	}
}
