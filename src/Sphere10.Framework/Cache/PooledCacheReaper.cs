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
/// Coordinates differently typed caches under one capacity budget in their common size-estimator units.
/// </summary>
/// <remarks>
/// Members register before use and borrow this reaper's synchronization lock. Every member has the same maximum
/// capacity as the reaper. Members are borrowed; deregistration retains their parent so open or waiting scopes
/// preserve lock identity. The requester applies ReapLevel once before asking the reaper to release capacity.
/// </remarks>
public class PooledCacheReaper : CacheReaperBase {

	private readonly List<ICacheBase> _caches;

	public PooledCacheReaper(int memoryPoolSize)
		: this((long)memoryPoolSize) {
	}

	public PooledCacheReaper(long memoryPoolSize) {
		Guard.Argument(memoryPoolSize >= 0, nameof(memoryPoolSize), "Capacity must not be negative.");
		MaxCapacity = memoryPoolSize;
		_caches = new List<ICacheBase>();
	}

	public long MaxCapacity { get; }

	public long CurrentSize {
		get {
			using (EnterReadScope()) {
				var size = 0L;
				foreach (var cache in _caches)
					size = checked(size + cache.CurrentSize);
				return size;
			}
		}
	}

	public long TotalItemCount {
		get {
			using (EnterReadScope()) {
				var count = 0L;
				foreach (var cache in _caches)
					count = checked(count + cache.ItemCount);
				return count;
			}
		}
	}

	public override long AvailableSpace() => MaxCapacity - CurrentSize;

	public override long AvailableSpace(ICacheBase requestingCache) {
		Guard.ArgumentNotNull(requestingCache, nameof(requestingCache));
		using (EnterReadScope()) {
			Guard.Ensure(IsRegistered(requestingCache), "Requesting cache was not registered.");
			Guard.Ensure(ReferenceEquals(requestingCache.ParentSyncObject, this), "A pooled cache's synchronization parent must remain its reaper.");
			return MaxCapacity - CurrentSize;
		}
	}

	public override void Register(ICacheBase cache) {
		Guard.ArgumentNotNull(cache, nameof(cache));
		Guard.Argument(cache.MaxCapacity == MaxCapacity, nameof(cache), "Cache must have the same capacity as its reaper.");
		using (EnterWriteScope()) {
			Guard.Ensure(!IsRegistered(cache), "Cache was already registered.");
			Guard.Argument(cache.ParentSyncObject == null, nameof(cache), "A pooled cache must register before borrowing another synchronization parent.");
			Guard.Argument(cache.CurrentSize == 0 && cache.ItemCount == 0, nameof(cache), "A pooled cache must register before use.");
			cache.ParentSyncObject = this;
			_caches.Add(cache);
		}
	}

	public override void Deregister(ICacheBase cache) {
		Guard.ArgumentNotNull(cache, nameof(cache));
		using (EnterWriteScope()) {
			var index = IndexOf(cache);
			Guard.Ensure(index >= 0, "Cache was not registered.");
			_caches.RemoveAt(index);
			// Retain the parent: changing it would make waiting or open scopes release a different lock.
		}
	}

	public override long MakeSpace(ICacheBase requestingCache, long requestedSpace) {
		Guard.ArgumentNotNull(requestingCache, nameof(requestingCache));
		Guard.Argument(requestedSpace >= 0, nameof(requestedSpace), "Requested release must not be negative.");
		Guard.Ensure(requestedSpace <= MaxCapacity, "Cache capacity insufficient for requested space.");
		using (EnterWriteScope()) {
			Guard.Ensure(IsRegistered(requestingCache), "Requesting cache was not registered.");
			Guard.Ensure(ReferenceEquals(requestingCache.ParentSyncObject, this), "A pooled cache's synchronization parent must remain its reaper.");
			if (requestedSpace == 0)
				return 0;

			// Reap the requester first and measure actual net release after its callbacks.
			var originalSize = CurrentSize;
			requestingCache.Reap(requestedSpace);
			var released = Math.Max(0, originalSize - CurrentSize);
			if (released >= requestedSpace)
				return released;

			// Use a finite snapshot: removal callbacks may dispose or populate other members.
			var otherCaches = new List<(ICacheBase cache, DateTime lastAccessedOn, int registrationOrder)>(_caches.Count);
			for (var index = 0; index < _caches.Count; index++) {
				var cache = _caches[index];
				if (!ReferenceEquals(cache, requestingCache))
					otherCaches.Add((cache, cache.LastAccessedOn, index));
			}
			otherCaches.Sort((left, right) => {
				var comparison = left.lastAccessedOn.CompareTo(right.lastAccessedOn);
				return comparison != 0 ? comparison : left.registrationOrder.CompareTo(right.registrationOrder);
			});
			foreach (var candidate in otherCaches) {
				var cache = candidate.cache;
				if (!IsRegistered(cache))
					continue;
				Guard.Ensure(ReferenceEquals(cache.ParentSyncObject, this), "A pooled cache's synchronization parent must remain its reaper.");
				cache.Reap(requestedSpace - released);
				released = Math.Max(0, originalSize - CurrentSize);
				if (released >= requestedSpace)
					break;
			}
			return released;
		}
	}

	private bool IsRegistered(ICacheBase cache) => IndexOf(cache) >= 0;

	private int IndexOf(ICacheBase cache) {
		for (var index = 0; index < _caches.Count; index++)
			if (ReferenceEquals(_caches[index], cache))
				return index;
		return -1;
	}

}
