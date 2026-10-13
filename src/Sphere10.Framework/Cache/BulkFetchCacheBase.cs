// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>
/// Base class for caches that populate all entries in a single bulk fetch operation.
/// </summary>
public abstract class BulkFetchCacheBase<TKey, TValue> : CacheBase<TKey, TValue> {
	private readonly bool _fetchOnceOnly;
	internal uint FetchCount;

	protected BulkFetchCacheBase(ExpirationPolicy expirationStrategy, TimeSpan? expirationDuration = null, bool fetchOnceOnly = false, NullValuePolicy nullValuePolicy = NullValuePolicy.CacheNormally,
	                             StaleValuePolicy staleValuePolicy = StaleValuePolicy.AssumeNeverStale, IEqualityComparer<TKey> keyComparer = null)
		: base(CacheReapPolicy.None, expirationStrategy, uint.MaxValue, expirationDuration, nullValuePolicy, staleValuePolicy, keyComparer) {
		_fetchOnceOnly = fetchOnceOnly;
		FetchCount = 0;
	}

	protected sealed override TValue Fetch(TKey key) {
		TValue result;
		using (EnterWriteScope()) {
			if (!_fetchOnceOnly || FetchCount == 0) {
				ForceRefresh();
			}

			if (InternalStorage.TryGetValue(key, out var item)) {
				result = item.Value;
			} else {
				switch (NullValuePolicy) {
					case NullValuePolicy.Throw:
						throw new SoftwareException("BulkCache does not have key with id {0}", key);
					case NullValuePolicy.ReturnButDontCache:
					case NullValuePolicy.CacheNormally:
					default:
						result = default;
						break;
				}
			}
		}
		return result;
	}

	public override CachedItem<TValue> Get(TKey key) {
		if (key is null)
			Guard.ArgumentNotNull(key, nameof(key));
		using (EnterWriteScope()) {
			if (FetchCount == 0)
				ForceRefresh();
			return base.Get(key);
		}
	}

	public override bool ContainsCachedItem(TKey key) {
		if (key is null)
			Guard.ArgumentNotNull(key, nameof(key));
		using (EnterWriteScope()) {
			if (FetchCount == 0)
				ForceRefresh();
			return base.ContainsCachedItem(key);
		}
	}

	/// <summary>
	/// Immediately refetches all items and resets fetch counters.
	/// </summary>
	public void ForceRefresh() {
		using (EnterWriteScope()) {
			Purge();
			BulkLoad(BulkFetch());
			FetchCount++;
		}
	}

	public override void Purge() {
		base.Purge();
		FetchCount = 0;
	}

	public override void Remove(TKey key) {
		throw new NotSupportedException("Items cannot be manually removed from a bulk fetch cache");
	}

	protected sealed override long EstimateSize(TValue value) {
		// Size limiting not applied to bulk-fetched cache
		return 0L;
	}

	protected abstract IDictionary<TKey, TValue> BulkFetch();

}

