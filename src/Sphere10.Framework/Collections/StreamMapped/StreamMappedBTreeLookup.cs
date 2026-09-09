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
using System.IO;
using System.Linq;

namespace Sphere10.Framework;

/// <summary>
/// A stream-mapped one-to-many lookup (multimap) backed by a <see cref="StreamMappedBTreePlus{K,V}"/>
/// with compound keys. Each logical entry <c>(key, value)</c> is stored as
/// <c>CompoundKey(key, groupIndex) → value</c> in the B+ tree, where <c>groupIndex</c> is a
/// zero-based ordinal within the key's group.
/// </summary>
/// <typeparam name="K">Lookup key type. Must have a constant-size serializer.</typeparam>
/// <typeparam name="V">Lookup value type. Must have a constant-size serializer.</typeparam>
public class StreamMappedBTreeLookup<K, V> : IBTreeLookup<K, V>, IDisposable {
	private readonly StreamMappedBTreePlus<CompoundKey<K, int>, V> _btree;
	private readonly BTreeLookup<K, V> _lookup;

	/// <summary>
	/// Creates a new stream-mapped B+ tree lookup or opens an existing one from the given stream.
	/// </summary>
	/// <param name="order">B+ tree order (minimum 3).</param>
	/// <param name="stream">Backing stream for persistent storage.</param>
	/// <param name="keySerializer">Constant-size serializer for lookup keys.</param>
	/// <param name="valueSerializer">Constant-size serializer for lookup values.</param>
	/// <param name="keyComparer">Comparer for lookup keys.</param>
	/// <param name="endianness">Byte order for all binary encoding.</param>
	public StreamMappedBTreeLookup(
		int order,
		Stream stream,
		IItemSerializer<K> keySerializer,
		IItemSerializer<V> valueSerializer,
		IComparer<K> keyComparer,
		Endianness endianness = Sphere10FrameworkDefaults.Endianness) {
		Guard.ArgumentNotNull(stream, nameof(stream));
		Guard.ArgumentNotNull(keySerializer, nameof(keySerializer));
		Guard.ArgumentNotNull(valueSerializer, nameof(valueSerializer));
		Guard.ArgumentNotNull(keyComparer, nameof(keyComparer));

		var CompoundKeySerializer = new ConstantSizeCompoundKeySerializer<K, int>(keySerializer, PrimitiveSerializer<int>.Instance);
		var CompoundComparer = new CompoundKeyComparer<K, int>(keyComparer, Comparer<int>.Default);

		_btree = new StreamMappedBTreePlus<CompoundKey<K, int>, V>(
			order,
			stream,
			CompoundKeySerializer,
			valueSerializer,
			CompoundComparer,
			endianness
		);

		_lookup = new BTreeLookup<K, V>(_btree, keyComparer);
	}

	/// <summary>
	/// The total number of entries (including duplicate keys with different values).
	/// </summary>
	public int TotalCount => _lookup.TotalCount;

	/// <summary>
	/// The number of distinct keys in the lookup.
	/// </summary>
	public int DistinctKeyCount => _lookup.DistinctKeyCount;

	/// <summary>
	/// The underlying B-tree stream.
	/// </summary>
	public Stream Stream => _btree.Stream;

	/// <summary>
	/// Adds a key-value pair to the lookup. Multiple values may be associated with the same key.
	/// </summary>
	public void Add(K key, V value) => _lookup.Add(key, value);

	/// <summary>
	/// Removes a specific key-value pair from the lookup. Returns true if the pair was found and removed.
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
	/// Returns all values associated with the given key. Returns empty if key not found.
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
	/// Validates the integrity of the underlying B+ tree.
	/// </summary>
	public bool Validate(out string error) => _lookup.Validate(out error);

	#region Explicit ILookup / IEnumerable

	int ILookup<K, V>.Count => DistinctKeyCount;

	IEnumerable<V> ILookup<K, V>.this[K key] => GetValues(key);

	bool ILookup<K, V>.Contains(K key) => ContainsKey(key);

	IEnumerator<IGrouping<K, V>> IEnumerable<IGrouping<K, V>>.GetEnumerator() => EnumerateGroupings().GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => EnumerateGroupings().GetEnumerator();

	#endregion

	public void Dispose() {
		_btree?.Dispose();
	}
}
