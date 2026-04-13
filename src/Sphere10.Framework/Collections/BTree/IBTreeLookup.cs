// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;

namespace Sphere10.Framework;

/// <summary>
/// A mutable one-to-many lookup (multimap) that extends <see cref="ILookup{TKey,TElement}"/>
/// with mutation operations and B-tree integrity validation.
/// </summary>
/// <typeparam name="K">Lookup key type.</typeparam>
/// <typeparam name="V">Lookup value type.</typeparam>
public interface IBTreeLookup<K, V> : ILookup<K, V> {

	/// <summary>
	/// The total number of entries (including duplicate keys with different values).
	/// </summary>
	int TotalCount { get; }

	/// <summary>
	/// Adds a key-value pair to the lookup.
	/// </summary>
	void Add(K key, V value);

	/// <summary>
	/// Removes a specific key-value pair from the lookup.
	/// </summary>
	bool Remove(K key, V value);

	/// <summary>
	/// Removes all values associated with the given key.
	/// </summary>
	void RemoveAll(K key);

	/// <summary>
	/// Returns whether the lookup contains a specific key-value pair.
	/// </summary>
	bool ContainsEntry(K key, V value);

	/// <summary>
	/// Removes all entries from the lookup.
	/// </summary>
	void Clear();

	/// <summary>
	/// Validates the integrity of the underlying B-tree.
	/// </summary>
	bool Validate(out string error);
}
