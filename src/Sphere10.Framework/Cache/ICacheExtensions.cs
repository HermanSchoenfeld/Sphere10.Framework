// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework;

/// <summary>
/// Convenience extensions for working with cache implementations.
/// </summary>
public static class ICacheExtensions {

	/// <summary>Creates an explicit object view that borrows the typed cache and shares its live entry metadata.</summary>
	public static ObjectCacheProjection<TKey, TValue> AsObjectCache<TKey, TValue>(this ICache<TKey, TValue> cache) => new(cache);

	/// <summary>Creates a borrowed cache view with explicit forward and inverse key/value conversions.</summary>
	public static ProjectedCache<TKey, TValue, TProjectedKey, TProjectedValue> AsProjection<TKey, TValue, TProjectedKey, TProjectedValue>(
		this ICache<TKey, TValue> cache,
		Func<TKey, TProjectedKey> keyProjection,
		Func<TProjectedKey, TKey> inverseKeyProjection,
		Func<TValue, TProjectedValue> valueProjection,
		Func<TProjectedValue, TValue> inverseValueProjection
	) => new(cache, keyProjection, inverseKeyProjection, valueProjection, inverseValueProjection);

	/// <summary>
	/// Returns all currently cached values, forcing an initial bulk fetch when applicable.
	/// </summary>
	public static IEnumerable<V> GetAllCachedValues<K, V>(this ICache<K, V> cache) {
		if (cache is BulkFetchCacheBase<K, V> bulkFetchCache && bulkFetchCache.FetchCount == 0) {
			bulkFetchCache.ForceRefresh();
		}
		using (cache.EnterReadScope()) {
			return cache.CachedItems.Select(c => c.Value).ToArray();
		}
	}

}

