// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>
/// Compares <see cref="CompoundKey{TKey1, TKey2}"/> instances by comparing <see cref="CompoundKey{TKey1, TKey2}.Key1"/>
/// first and, when equal, comparing <see cref="CompoundKey{TKey1, TKey2}.Key2"/> as a tiebreaker.
/// </summary>
public class CompoundKeyComparer<TKey1, TKey2> : IComparer<CompoundKey<TKey1, TKey2>> {
	private readonly IComparer<TKey1> _key1Comparer;
	private readonly IComparer<TKey2> _key2Comparer;

	public CompoundKeyComparer(IComparer<TKey1> key1Comparer, IComparer<TKey2> key2Comparer) {
		Guard.ArgumentNotNull(key1Comparer, nameof(key1Comparer));
		Guard.ArgumentNotNull(key2Comparer, nameof(key2Comparer));
		_key1Comparer = key1Comparer;
		_key2Comparer = key2Comparer;
	}

	public int Compare(CompoundKey<TKey1, TKey2> x, CompoundKey<TKey1, TKey2> y) {
		var Cmp = _key1Comparer.Compare(x.Key1, y.Key1);
		if (Cmp != 0)
			return Cmp;
		return _key2Comparer.Compare(x.Key2, y.Key2);
	}
}
