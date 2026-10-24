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
using System.Diagnostics;

namespace Sphere10.Framework;

/// <summary>
/// Abstract base for all B-tree family dictionaries (B-tree, B+ tree, etc.).
/// Provides the common <see cref="IBTree{K, V}"/> contract and abstract operations
/// that subclasses implement for specific tree variants.
/// <see cref="BTree{K, V, TNode}"/> and <see cref="BTreePlus{K, V, TNode}"/> supply distinct
/// node algorithms; their storage subclasses supply the node representation.
/// </summary>
/// <typeparam name="K">Key type.</typeparam>
/// <typeparam name="V">Value type.</typeparam>
public abstract class BTreeBase<K, V> : IBTree<K, V> {
	private readonly int _order;
	private readonly IComparer<K> _comparer;

	protected BTreeBase(int order, IComparer<K> keyComparer = null) {
		if (order < 3)
			throw new ArgumentOutOfRangeException(nameof(order), "B-tree order must be at least 3.");

		_order = order;
		_comparer = keyComparer ?? Comparer<K>.Default;
	}

	public int Count { get; protected set; }

	public TreeTraversalType TraversalType { get; set; } = TreeTraversalType.InOrder;

	public bool IsReadOnly => false;

	public ICollection<K> Keys {
		get {
			var list = new List<K>(Count);
			foreach (var entry in this)
				list.Add(entry.Key);
			return list;
		}
	}

	public ICollection<V> Values {
		get {
			var list = new List<V>(Count);
			foreach (var entry in this)
				list.Add(entry.Value);
			return list;
		}
	}

	public V this[K key] {
		get {
			if (TryGetValue(key, out var value))
				return value;
			throw new KeyNotFoundException();
		}
		set => Set(key, value, overwriteIfExists: true);
	}

	public int Order => _order;

	public IComparer<K> Comparer => _comparer;

	public void Add(K key, V value) => Set(key, value, overwriteIfExists: false);

	public void Add(KeyValuePair<K, V> item) => Add(item.Key, item.Value);

	public abstract void Set(K key, V value, bool overwriteIfExists);

	public abstract bool Remove(K key);

	public bool Remove(KeyValuePair<K, V> item) {
		if (!TryGetValue(item.Key, out var current))
			return false;
		if (!EqualityComparer<V>.Default.Equals(current, item.Value))
			return false;
		return Remove(item.Key);
	}

	public abstract void Clear();

	public bool ContainsKey(K key) => TryGetValue(key, out _);

	public abstract bool TryGetValue(K key, out V value);

	public bool Contains(KeyValuePair<K, V> item)
		=> TryGetValue(item.Key, out var current) && EqualityComparer<V>.Default.Equals(current, item.Value);

	public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex) {
		if (array == null)
			throw new ArgumentNullException(nameof(array));
		if (arrayIndex < 0 || arrayIndex > array.Length)
			throw new ArgumentOutOfRangeException(nameof(arrayIndex));
		if (array.Length - arrayIndex < Count)
			throw new ArgumentException("Destination array is too small.", nameof(array));

		foreach (var entry in this)
			array[arrayIndex++] = entry;
	}

	public abstract IEnumerator<KeyValuePair<K, V>> GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public abstract IEnumerable<KeyValuePair<K, V>> FindRange(K lowerInclusive, K upperInclusive);

	public abstract bool Validate(out string error);

	protected int Compare(K x, K y) => _comparer.Compare(x, y);

	[Conditional("DIAGNOSTIC")]
	protected void DebugValidate() {
		if (!Validate(out var error))
			throw new InvalidOperationException("B-tree invariant violation: " + error);
	}
}
