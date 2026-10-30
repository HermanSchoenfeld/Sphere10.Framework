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
			// Prepare one whole snapshot; a missing key in that snapshot must not trigger another source fetch.
			if (FetchCount == 0 || !_fetchOnceOnly && (!InternalStorage.TryGetValue(key, out var item) || !item.IsRemoving && base.RequiresFetch(key, item, DateTime.Now)))
				ForceRefresh();
			return base.Get(key);
		}
	}

	// Get has already checked the snapshot; fetch-once entries must also survive expiry and invalidation.
	protected sealed override bool RequiresFetch(TKey key, CachedItem<TValue> item, DateTime now) => false;

	public override bool ContainsCachedItem(TKey key) {
		if (key is null)
			Guard.ArgumentNotNull(key, nameof(key));
		using (EnterWriteScope()) {
			if (FetchCount == 0)
				ForceRefresh();
			return base.ContainsCachedItem(key) || _fetchOnceOnly && InternalStorage.ContainsKey(key);
		}
	}

	/// <summary>
	/// Replaces the whole snapshot immediately. A failed source fetch leaves the cache empty.
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

	public override long Reap(long minimumRelease) {
		Guard.Argument(minimumRelease >= 0, nameof(minimumRelease), "The requested release must be nonnegative.");
		// Whole snapshots have zero-sized entries: per-entry reaping releases no capacity and would leave partial data.
		// The base zero-release path preserves synchronized disposed validation without fetching or removing entries.
		return base.Reap(0);
	}

	protected sealed override long EstimateSize(TValue value) {
		// Size limiting not applied to bulk-fetched cache
		return 0L;
	}

	protected abstract IDictionary<TKey, TValue> BulkFetch();

}

