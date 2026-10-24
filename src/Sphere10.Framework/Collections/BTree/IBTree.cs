// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>A sorted dictionary contract shared by B-tree variants and their storage backends.</summary>
public interface IBTree<K, V> : IDictionary<K, V> {

	int Order { get; }

	IComparer<K> Comparer { get; }

	/// <summary>Controls the traversal used by dictionary enumeration.</summary>
	TreeTraversalType TraversalType { get; set; }

	/// <summary>Inserts an entry, or replaces an existing value when <paramref name="overwriteIfExists"/> is true.</summary>
	void Set(K key, V value, bool overwriteIfExists);

	/// <summary>Enumerates the inclusive key range in comparer order, independently of <see cref="TraversalType"/>.</summary>
	IEnumerable<KeyValuePair<K, V>> FindRange(K lowerInclusive, K upperInclusive);

	/// <summary>Checks tree invariants and reports the first failure, if any.</summary>
	bool Validate(out string error);
}
