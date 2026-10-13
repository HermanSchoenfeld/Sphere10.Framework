// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;

namespace Sphere10.Framework;

/// <summary>
/// Reaper that manages one cache's capacity without depending on its key or value types.
/// </summary>
public class IsolatedCacheReaper : CacheReaperBase {

	private ICacheBase _cache;

	public override void Register(ICacheBase cache) {
		Guard.ArgumentNotNull(cache, nameof(cache));
		Guard.Ensure(Interlocked.CompareExchange(ref _cache, cache, null) == null, "A cache was already registered.");
	}

	public override void Deregister(ICacheBase cache) {
		Guard.ArgumentNotNull(cache, nameof(cache));
		var registered = GetRegisteredCache();
		Guard.Ensure(ReferenceEquals(cache, registered), "Attempting to deregister an unregistered cache.");
		Guard.Ensure(ReferenceEquals(Interlocked.CompareExchange(ref _cache, null, registered), registered), "Cache was not registered.");
	}

	public override long AvailableSpace() {
		var cache = GetRegisteredCache();
		return cache.MaxCapacity - cache.CurrentSize;
	}

	public override long AvailableSpace(ICacheBase requestingCache) {
		Guard.ArgumentNotNull(requestingCache, nameof(requestingCache));
		var cache = GetRegisteredCache();
		Guard.Argument(ReferenceEquals(requestingCache, cache), nameof(requestingCache), "Not a registered cache.");
		return cache.MaxCapacity - cache.CurrentSize;
	}

	public override long MakeSpace(ICacheBase requestingCache, long requestedSpace) {
		Guard.ArgumentNotNull(requestingCache, nameof(requestingCache));
		Guard.Argument(requestedSpace >= 0, nameof(requestedSpace), "Requested release must not be negative.");
		var cache = GetRegisteredCache();
		Guard.Argument(ReferenceEquals(requestingCache, cache), nameof(requestingCache), "Not a registered cache.");
		Guard.Ensure(requestedSpace <= cache.MaxCapacity, "Cache capacity insufficient for requested space.");
		return cache.Reap(requestedSpace);
	}

	private ICacheBase GetRegisteredCache() {
		var cache = Volatile.Read(ref _cache);
		Guard.Ensure(cache != null, "No cache was registered.");
		return cache;
	}

}
