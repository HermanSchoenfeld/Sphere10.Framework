// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sphere10.Framework.Utils.Profiler.BTrees;

internal interface IBTreeBenchmarkIndex : IDisposable {
	int Count { get; }
	void Add(int key, long value);
	void Set(int key, long value);
	bool Remove(int key, long value);
	bool Contains(int key, long value);
	bool TryGet(int key, out long value);
	long Scan();
	long Range(int first, int last);
	long Group(int key);
	bool Validate(out string error);
}

internal sealed class BTreeIndex<TKey> : IBTreeBenchmarkIndex {
	private readonly TKey[] _keys;
	private readonly BTreeBase<TKey, long> _tree;
	private readonly IExtendedBTreeLookup<TKey, long> _lookup;
	private readonly Func<TKey, TKey, IEnumerable<KeyValuePair<TKey, long>>> _range;

	public BTreeIndex(BTreeProfile profile, TKey[] keys, IItemSerializer<TKey> serializer, IComparer<TKey> comparer, Stream stream) {
		_keys = keys;
		if (profile.Family == BTreeFamily.Lookup) {
			_lookup = profile.Storage == BTreeStorage.ManagedNodes ? new InMemoryBTreeLookup<TKey, long>(profile.Order, comparer)
				: new StreamMappedBTreeLookup<TKey, long>(profile.Order, stream, serializer, PrimitiveSerializer<long>.Instance, comparer, profile.Endianness);
			return;
		}
		if (profile.Family == BTreeFamily.BTree) {
			if (profile.Storage == BTreeStorage.ManagedNodes) {
				var tree = new InMemoryBTree<TKey, long>(profile.Order, comparer);
				_tree = tree;
				_range = tree.FindRange;
			} else {
				var tree = new StreamMappedBTree<TKey, long>(profile.Order, stream, serializer, PrimitiveSerializer<long>.Instance, comparer, profile.Endianness);
				_tree = tree;
				_range = tree.FindRange;
			}
		} else {
			if (profile.Storage == BTreeStorage.ManagedNodes) {
				var tree = new InMemoryBTreePlus<TKey, long>(profile.Order, comparer);
				_tree = tree;
				_range = tree.FindRange;
			} else {
				var tree = new StreamMappedBTreePlus<TKey, long>(profile.Order, stream, serializer, PrimitiveSerializer<long>.Instance, comparer, profile.Endianness);
				_tree = tree;
				_range = tree.FindRange;
			}
		}
	}

	public int Count => _tree?.Count ?? _lookup.TotalCount;

	public void Add(int key, long value) {
		if (_tree != null)
			_tree.Add(_keys[key], value);
		else
			_lookup.Add(_keys[key], value);
	}

	public void Set(int key, long value) => _tree[_keys[key]] = value;

	public bool Remove(int key, long value) => _tree?.Remove(_keys[key]) ?? _lookup.Remove(_keys[key], value);

	public bool Contains(int key, long value) => _tree?.TryGetValue(_keys[key], out var stored) == true ? stored == value
		: _tree == null && _lookup.ContainsEntry(_keys[key], value);

	public bool TryGet(int key, out long value) {
		if (_tree != null)
			return _tree.TryGetValue(_keys[key], out value);
		var values = _lookup.GetValues(_keys[key]);
		using var iterator = values.GetEnumerator();
		var found = iterator.MoveNext();
		value = found ? iterator.Current : 0;
		return found;
	}

	public long Scan() => _tree != null ? _tree.Sum(entry => entry.Value) : _lookup.EnumerateGroupings().Sum(group => group.Sum());

	public long Range(int first, int last) => _range(_keys[first], _keys[last]).Sum(entry => entry.Value);

	public long Group(int key) => _lookup.GetValues(_keys[key]).Sum();

	public bool Validate(out string error) => _tree != null ? _tree.Validate(out error) : _lookup.Validate(out error);

	public void Dispose() {
		(_tree as IDisposable)?.Dispose();
		(_lookup as IDisposable)?.Dispose();
	}
}
