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

/// <summary>Extends the mutable lookup with named primitives and shared adapters to the LINQ lookup contract.</summary>
public interface IExtendedBTreeLookup<K, V> : IBTreeLookup<K, V> {
	/// <summary>Gets the number of distinct keys in the lookup.</summary>
	int DistinctKeyCount { get; }

	/// <summary>Returns whether the lookup contains the key.</summary>
	bool ContainsKey(K key);

	/// <summary>Returns the values associated with the key, or an empty sequence.</summary>
	IEnumerable<V> GetValues(K key);

	/// <summary>Enumerates each distinct key and its associated values.</summary>
	IEnumerable<IGrouping<K, V>> EnumerateGroupings();

	#region Base Interface Bridges

	int ILookup<K, V>.Count => DistinctKeyCount;

	IEnumerable<V> ILookup<K, V>.this[K key] => GetValues(key);

	bool ILookup<K, V>.Contains(K key) => ContainsKey(key);

	IEnumerator<IGrouping<K, V>> IEnumerable<IGrouping<K, V>>.GetEnumerator() => EnumerateGroupings().GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => EnumerateGroupings().GetEnumerator();

	#endregion
}
