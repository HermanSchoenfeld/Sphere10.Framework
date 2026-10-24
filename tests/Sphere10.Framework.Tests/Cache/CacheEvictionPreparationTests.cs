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
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace Sphere10.Framework.Tests.Cache;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CacheEvictionPreparationTests {

	[Test]
	public void FailedPreparationRetainsTheEntryAndCanBeRetried([Values] RemovalOperation operation) {
		using var cache = new TestCache(1);
		cache.Set(0, 1);
		var original = cache.Get(0);
		var failure = new IOException("Injected preparation failure");
		var preparations = 0;
		var removals = 0;
		cache.PreparingRemoval = (_, _) => {
			if (++preparations == 1)
				throw failure;
		};
		cache.ItemRemoved += (_, _) => removals++;
		Action remove = operation switch {
			RemovalOperation.Remove => () => cache.Remove(0),
			RemovalOperation.Purge => cache.Purge,
			RemovalOperation.Reap => () => cache.Reap(1),
			RemovalOperation.Admission => () => cache.Set(1, 1),
			_ => throw new ArgumentOutOfRangeException(nameof(operation))
		};

		Assert.That(remove, Throws.Exception.SameAs(failure));
		Assert.That(cache.Get(0), Is.SameAs(original));
		Assert.That(original.Value, Is.EqualTo(1));
		Assert.That(original.Traits.HasFlag(CachedItemTraits.Purged), Is.False);
		Assert.That(original.CanPurge, Is.True);
		Assert.That(cache.CurrentSize, Is.EqualTo(1));
		Assert.That(cache.ItemCount, Is.EqualTo(1));
		Assert.That(cache.ContainsCachedItem(1), Is.False);
		Assert.That(removals, Is.Zero);

		remove();
		Assert.That(preparations, Is.EqualTo(2));
		Assert.That(removals, Is.EqualTo(1));
		Assert.That(cache.ContainsCachedItem(0), Is.False);
		Assert.That(original.Traits.HasFlag(CachedItemTraits.Purged), Is.True);
		Assert.That(cache.CurrentSize, Is.EqualTo(operation == RemovalOperation.Admission ? 1 : 0));
		Assert.That(cache.ItemCount, Is.EqualTo(operation == RemovalOperation.Admission ? 1 : 0));
		Assert.That(cache.ContainsCachedItem(1), Is.EqualTo(operation == RemovalOperation.Admission));
	}

	[Test]
	public void PreparationObservesTheLiveEntryAndRemovalObservesCommittedEviction() {
		using var cache = new TestCache(2);
		cache.Set(0, 2);
		var original = cache.Get(0);
		var calls = new List<string>();
		cache.PreparingRemoval = (key, item) => {
			calls.Add("preparing");
			Assert.That(cache.ContainsCachedItem(key), Is.True);
			Assert.That(cache.Get(key), Is.SameAs(original));
			Assert.That(item, Is.SameAs(original));
			Assert.That(item.Traits.HasFlag(CachedItemTraits.Purged), Is.False);
			Assert.That(item.CanPurge, Is.True);
			Assert.That(cache.CurrentSize, Is.EqualTo(2));
			Assert.That(cache.ItemCount, Is.EqualTo(1));
		};
		cache.ItemRemoved += (key, item) => {
			calls.Add("removed");
			Assert.That(cache.ContainsCachedItem(key), Is.False);
			Assert.That(item, Is.SameAs(original));
			Assert.That(item.Traits.HasFlag(CachedItemTraits.Purged), Is.True);
			Assert.That(item.CanPurge, Is.False);
			Assert.That(cache.CurrentSize, Is.Zero);
			Assert.That(cache.ItemCount, Is.Zero);
		};

		cache.Remove(0);
		Assert.That(calls, Is.EqualTo(new[] { "preparing", "removed" }));
	}

	[Test]
	public void NestedRemovePurgeAndReapDoNotPrepareTheSameEntryRecursively() {
		using var cache = new TestCache(1);
		cache.Set(0, 1);
		var preparations = 0;
		var removals = 0;
		cache.PreparingRemoval = (key, _) => {
			Assert.That(++preparations, Is.EqualTo(1), "A pending removal must not be prepared recursively.");
			cache.Remove(key);
			cache.Purge();
			Assert.That(cache.Reap(1), Is.Zero);
			Assert.That(cache.ContainsCachedItem(key), Is.True);
			Assert.That(cache.CurrentSize, Is.EqualTo(1));
		};
		cache.ItemRemoved += (_, _) => removals++;

		cache.Remove(0);
		Assert.That(preparations, Is.EqualTo(1));
		Assert.That(removals, Is.EqualTo(1));
		Assert.That(cache.CurrentSize, Is.Zero);
		Assert.That(cache.ItemCount, Is.Zero);
	}

	[Test]
	public void NestedPurgeRemovesOtherEntriesWhileRetainingTheEntryBeingPrepared() {
		using var cache = new TestCache(2);
		cache.Set(0, 1);
		cache.Set(1, 1);
		var preparations = new List<int>();
		var removals = new List<int>();
		cache.PreparingRemoval = (key, _) => {
			preparations.Add(key);
			Assert.That(preparations.Count, Is.LessThanOrEqualTo(2), "Nested purge must skip removals already in progress.");
			if (key != 0)
				return;
			cache.Purge();
			Assert.That(cache.ContainsCachedItem(0), Is.True);
			Assert.That(cache.ContainsCachedItem(1), Is.False);
			Assert.That(cache.CurrentSize, Is.EqualTo(1));
		};
		cache.ItemRemoved += (key, _) => removals.Add(key);

		cache.Remove(0);
		Assert.That(preparations, Is.EqualTo(new[] { 0, 1 }));
		Assert.That(removals, Is.EqualTo(new[] { 1, 0 }));
		Assert.That(cache.CurrentSize, Is.Zero);
		Assert.That(cache.ItemCount, Is.Zero);
	}

	[Test]
	public void ReentrantAdmissionCannotUseCapacityBelongingToTheEntryBeingPrepared() {
		using var cache = new TestCache(1);
		cache.Set(0, 1);
		var original = cache.Get(0);
		var preparations = 0;
		cache.PreparingRemoval = (_, _) => {
			Assert.That(++preparations, Is.EqualTo(1), "Admission must not recursively evict the pending victim.");
			cache.Set(1, 1);
		};

		Assert.That(() => cache.Remove(0), Throws.InvalidOperationException);
		cache.PreparingRemoval = null;
		Assert.That(cache.Get(0), Is.SameAs(original));
		Assert.That(cache.ContainsCachedItem(1), Is.False);
		Assert.That(cache.CurrentSize, Is.EqualTo(1));
		Assert.That(cache.ItemCount, Is.EqualTo(1));

		cache.Set(1, 1);
		Assert.That(cache.ContainsCachedItem(0), Is.False);
		Assert.That(cache.Get(1).Value, Is.EqualTo(1));
		Assert.That(cache.CurrentSize, Is.EqualTo(1));
	}

	[Test]
	public void ReentrantAdmissionCanReapAnotherEntryWithoutEvictingThePendingVictim() {
		using var cache = new TestCache(2);
		cache.Set(0, 1);
		cache.Set(1, 1);
		var preparations = new List<int>();
		var removals = new List<int>();
		cache.PreparingRemoval = (key, _) => {
			preparations.Add(key);
			Assert.That(preparations.Count, Is.LessThanOrEqualTo(2));
			if (key != 0)
				return;
			cache.Set(2, 1);
			Assert.That(cache.ContainsCachedItem(0), Is.True);
			Assert.That(cache.ContainsCachedItem(1), Is.False);
			Assert.That(cache.ContainsCachedItem(2), Is.True);
			Assert.That(cache.CurrentSize, Is.EqualTo(2));
		};
		cache.ItemRemoved += (key, _) => removals.Add(key);

		cache.Remove(0);
		cache.PreparingRemoval = null;
		Assert.That(preparations, Is.EqualTo(new[] { 0, 1 }));
		Assert.That(removals, Is.EqualTo(new[] { 1, 0 }));
		Assert.That(cache.ContainsCachedItem(2), Is.True);
		Assert.That(cache.CurrentSize, Is.EqualTo(1));
		Assert.That(cache.ItemCount, Is.EqualTo(1));
	}

	[Test]
	public void FailedPreparationPreservesLegitimateCallbackAdmissionsAndBothEntriesRemainAccountedFor() {
		using var cache = new TestCache(2);
		cache.Set(0, 1);
		var original = cache.Get(0);
		var failure = new IOException("Failure after admitting another entry");
		cache.PreparingRemoval = (_, _) => {
			cache.Set(1, 1);
			throw failure;
		};

		Assert.That(() => cache.Remove(0), Throws.Exception.SameAs(failure));
		cache.PreparingRemoval = null;
		Assert.That(cache.Get(0), Is.SameAs(original));
		Assert.That(cache.Get(1).Value, Is.EqualTo(1));
		Assert.That(cache.CurrentSize, Is.EqualTo(2));
		Assert.That(cache.ItemCount, Is.EqualTo(2));

		cache.Remove(0);
		Assert.That(cache.ContainsCachedItem(1), Is.True);
		Assert.That(cache.CurrentSize, Is.EqualTo(1));
	}

	[Test]
	public void ReplacingTheEntryDuringPreparationIsRejectedAndCanBeRetriedAfterward([Values] bool bulkLoad) {
		using var cache = new TestCache(2);
		cache.Set(0, 1);
		var original = cache.Get(0);
		cache.PreparingRemoval = (key, _) => {
			if (bulkLoad)
				cache.BulkLoad(new[] { new KeyValuePair<int, int>(key, 2) });
			else
				cache.Set(key, 2);
		};

		Assert.That(() => cache.Remove(0), Throws.InvalidOperationException);
		cache.PreparingRemoval = null;
		Assert.That(cache.Get(0), Is.SameAs(original));
		Assert.That(original.Value, Is.EqualTo(1));
		Assert.That(original.Traits.HasFlag(CachedItemTraits.Purged), Is.False);
		Assert.That(cache.CurrentSize, Is.EqualTo(1));

		cache.Set(0, 2);
		Assert.That(cache.Get(0), Is.SameAs(original));
		Assert.That(original.Value, Is.EqualTo(2));
		Assert.That(cache.CurrentSize, Is.EqualTo(2));
	}

	[Test]
	public void StartingDisposalDuringPreparationIsRejectedWithoutDisposingTheCache() {
		using var cache = new TestCache(2);
		cache.Set(0, 1);
		var original = cache.Get(0);
		cache.PreparingRemoval = (_, _) => cache.Dispose();

		Assert.That(() => cache.Remove(0), Throws.InvalidOperationException);
		cache.PreparingRemoval = null;
		Assert.That(cache.Get(0), Is.SameAs(original));
		Assert.That(cache.CurrentSize, Is.EqualTo(1));
		Assert.That(cache.ItemCount, Is.EqualTo(1));
		cache.Set(1, 1);
		Assert.That(cache.ItemCount, Is.EqualTo(2));

		cache.Dispose();
		Assert.That(cache.CurrentSize, Is.Zero);
		Assert.That(cache.ItemCount, Is.Zero);
		Assert.That(() => cache.Get(0), Throws.TypeOf<ObjectDisposedException>());
	}

	[Test]
	public void RecursiveDisposalDuringOrdinaryDisposalRemainsANoOp() {
		using var cache = new TestCache(2);
		cache.Set(0, 1);
		cache.Set(1, 1);
		var preparations = 0;
		var removals = 0;
		cache.PreparingRemoval = (_, _) => {
			preparations++;
			cache.Dispose();
		};
		cache.ItemRemoved += (_, _) => removals++;

		cache.Dispose();
		Assert.That(preparations, Is.EqualTo(2));
		Assert.That(removals, Is.EqualTo(2));
		Assert.That(cache.CurrentSize, Is.Zero);
		Assert.That(cache.ItemCount, Is.Zero);
	}

	[Test]
	public void PurgeRetainsEntriesAddedDuringPreparationOutsideItsOriginalSnapshot() {
		using var cache = new TestCache(3);
		cache.Set(0, 1);
		cache.Set(1, 1);
		var preparations = new List<int>();
		cache.PreparingRemoval = (key, _) => {
			preparations.Add(key);
			if (key == 0)
				cache.Set(2, 1);
		};

		cache.Purge();
		cache.PreparingRemoval = null;
		Assert.That(preparations, Is.EqualTo(new[] { 0, 1 }));
		Assert.That(cache.ContainsCachedItem(2), Is.True);
		Assert.That(cache.ItemCount, Is.EqualTo(1));
		Assert.That(cache.CurrentSize, Is.EqualTo(1));
	}

	[Test]
	public void PurgeRetainsAnotherCandidateReplacedDuringPreparation() {
		using var cache = new TestCache(3);
		cache.Set(0, 1);
		cache.Set(1, 1);
		var originalOther = cache.Get(1);
		CachedItem<int> replacement = null;
		cache.PreparingRemoval = (key, _) => {
			if (key != 0)
				return;
			cache.Remove(1);
			cache.Set(1, 2);
			replacement = cache.Get(1);
		};

		cache.Purge();
		cache.PreparingRemoval = null;
		Assert.That(replacement, Is.Not.SameAs(originalOther));
		Assert.That(cache.Get(1), Is.SameAs(replacement));
		Assert.That(replacement.Value, Is.EqualTo(2));
		Assert.That(replacement.Traits.HasFlag(CachedItemTraits.Purged), Is.False);
		Assert.That(cache.CurrentSize, Is.EqualTo(2));
		Assert.That(cache.ItemCount, Is.EqualTo(1));
	}

	[Test]
	public void ReapingStillRespectsPinsAndExplicitRemovalStillBypassesThem([Values] bool purge) {
		using var cache = new TestCache(1);
		cache.Set(0, 1);
		var original = cache.Get(0);
		original.CanPurge = false;
		var preparations = 0;
		cache.PreparingRemoval = (_, _) => preparations++;

		Assert.That(cache.Reap(1), Is.Zero);
		Assert.That(preparations, Is.Zero);
		Assert.That(cache.Get(0), Is.SameAs(original));
		if (purge)
			cache.Purge();
		else
			cache.Remove(0);
		Assert.That(preparations, Is.EqualTo(1));
		Assert.That(cache.ItemCount, Is.Zero);
		Assert.That(cache.CurrentSize, Is.Zero);
	}

	[Test]
	public void PostRemovalFailureLeavesEvictionCommitted() {
		using var cache = new TestCache(1);
		cache.Set(0, 1);
		var original = cache.Get(0);
		var failure = new IOException("Injected post-removal notification failure");
		var preparations = 0;
		cache.PreparingRemoval = (_, _) => preparations++;
		cache.ItemRemoved += (_, _) => throw failure;

		Assert.That(() => cache.Remove(0), Throws.Exception.SameAs(failure));
		Assert.That(preparations, Is.EqualTo(1));
		Assert.That(original.Traits.HasFlag(CachedItemTraits.Purged), Is.True);
		Assert.That(cache.ContainsCachedItem(0), Is.False);
		Assert.That(cache.CurrentSize, Is.Zero);
		Assert.That(cache.ItemCount, Is.Zero);
	}

	[Test]
	public void PostRemovalCallbackCanRepopulateTheSameKey() {
		using var cache = new TestCache(2);
		cache.Set(0, 1);
		var original = cache.Get(0);
		EventHandlerEx<int, CachedItem<int>> repopulate = (key, _) => cache.Set(key, 2);
		cache.ItemRemoved += repopulate;

		cache.Remove(0);
		cache.ItemRemoved -= repopulate;
		Assert.That(cache.Get(0), Is.Not.SameAs(original));
		Assert.That(cache.Get(0).Value, Is.EqualTo(2));
		Assert.That(cache.Get(0).Traits.HasFlag(CachedItemTraits.Purged), Is.False);
		Assert.That(original.Traits.HasFlag(CachedItemTraits.Purged), Is.True);
		Assert.That(cache.ItemCount, Is.EqualTo(1));
		Assert.That(cache.CurrentSize, Is.EqualTo(2));
	}

	[Test]
	public void InvalidatedEntryCanBeReadDuringPreparationWithoutRecursivelyFetchingIt() {
		using var cache = new TestCache(1);
		cache.Set(0, 1);
		var original = cache.Get(0);
		cache.Invalidate(0);
		var preparations = 0;
		cache.PreparingRemoval = (key, _) => {
			Assert.That(++preparations, Is.EqualTo(1), "Reading the invalidated entry during preparation must not start another removal.");
			Assert.That(cache.Get(key), Is.SameAs(original));
			Assert.That(cache.FetchCount, Is.Zero);
			Assert.That(cache.CurrentSize, Is.EqualTo(1));
		};

		var refreshed = cache.Get(0);
		cache.PreparingRemoval = null;
		Assert.That(preparations, Is.EqualTo(1));
		Assert.That(refreshed, Is.Not.SameAs(original));
		Assert.That(original.Traits.HasFlag(CachedItemTraits.Purged), Is.True);
		Assert.That(cache.FetchCount, Is.EqualTo(1));
		Assert.That(cache.Get(0), Is.SameAs(refreshed));
		Assert.That(cache.FetchCount, Is.EqualTo(1));
		Assert.That(cache.CurrentSize, Is.EqualTo(1));
	}

	[Test]
	public void FailedReentrantAdmissionRetainsPendingAndPinnedCapacityForRetry([Values] bool pooled) {
		var reaper = pooled ? new PooledCacheReaper(2) : null;
		using var cache = new TestCache(2, reaper);
		using var otherCache = pooled ? new TestCache(2, reaper) : null;
		var pinnedCache = otherCache ?? cache;
		cache.Set(0, 1);
		pinnedCache.Set(99, 1);
		var original = cache.Get(0);
		var pinned = pinnedCache.Get(99);
		pinned.CanPurge = false;
		var preparations = 0;
		cache.PreparingRemoval = (_, _) => {
			Assert.That(++preparations, Is.EqualTo(1), "The pending victim and pinned entries are unavailable to nested admission.");
			cache.Set(1, 1);
		};

		Assert.That(() => cache.Remove(0), Throws.InvalidOperationException);
		cache.PreparingRemoval = null;
		Assert.That(cache.Get(0), Is.SameAs(original));
		Assert.That(pinnedCache.Get(99), Is.SameAs(pinned));
		Assert.That(pinned.CanPurge, Is.False);
		Assert.That(cache.ContainsCachedItem(1), Is.False);
		Assert.That(cache.CurrentSize, Is.EqualTo(pooled ? 1 : 2));
		Assert.That(reaper?.CurrentSize ?? cache.CurrentSize, Is.EqualTo(2));

		cache.Set(1, 1);
		Assert.That(cache.ContainsCachedItem(0), Is.False);
		Assert.That(cache.ContainsCachedItem(1), Is.True);
		Assert.That(pinnedCache.Get(99), Is.SameAs(pinned));
		Assert.That(cache.CurrentSize, Is.EqualTo(pooled ? 1 : 2));
		Assert.That(reaper?.CurrentSize ?? cache.CurrentSize, Is.EqualTo(2));
	}

	[Test]
	public void FailedPreparationAfterReapingAnotherCachePreservesTheSharedBudgetAndCanBeRetried() {
		var reaper = new PooledCacheReaper(2);
		using var cache = new TestCache(2, reaper);
		using var otherCache = new TestCache(2, reaper);
		cache.Set(0, 1);
		otherCache.Set(99, 1);
		var original = cache.Get(0);
		var other = otherCache.Get(99);
		var failure = new IOException("Preparation failed after a shared-budget admission");
		var preparations = 0;
		cache.PreparingRemoval = (_, _) => {
			Assert.That(++preparations, Is.EqualTo(1), "Reaping the shared budget must skip the pending victim.");
			cache.Set(1, 1);
			Assert.That(otherCache.ItemCount, Is.Zero);
			Assert.That(cache.CurrentSize, Is.EqualTo(2));
			Assert.That(reaper.CurrentSize, Is.EqualTo(2));
			throw failure;
		};

		Assert.That(() => cache.Remove(0), Throws.Exception.SameAs(failure));
		cache.PreparingRemoval = null;
		Assert.That(cache.Get(0), Is.SameAs(original));
		Assert.That(cache.ContainsCachedItem(1), Is.True);
		Assert.That(other.Traits.HasFlag(CachedItemTraits.Purged), Is.True);
		Assert.That(otherCache.ItemCount, Is.Zero);
		Assert.That(cache.ItemCount, Is.EqualTo(2));
		Assert.That(reaper.CurrentSize, Is.EqualTo(2));

		cache.Remove(0);
		Assert.That(cache.ContainsCachedItem(1), Is.True);
		Assert.That(cache.CurrentSize, Is.EqualTo(1));
		Assert.That(reaper.CurrentSize, Is.EqualTo(1));
		Assert.That(reaper.TotalItemCount, Is.EqualTo(1));
	}

	[Test]
	public void PinChangesPreserveLifecycleFlagsThroughProjectedEntries([Values] bool invalidated, [Values] bool purged) {
		using var cache = new TestCache(1);
		cache.Set(0, 1);
		var source = cache.Get(0);
		CachedItemTraits lifecycleFlags = 0;
		if (invalidated) {
			cache.Invalidate(0);
			lifecycleFlags |= CachedItemTraits.Invalidated;
		}
		if (purged) {
			cache.Remove(0);
			lifecycleFlags |= CachedItemTraits.Purged;
		}
		var projected = new ProjectedCachedItem<int, int>(source, value => value, value => value);

		projected.CanPurge = false;
		Assert.That(source.Traits, Is.EqualTo(lifecycleFlags));
		Assert.That(projected.CanPurge, Is.False);
		Assert.That(source.CanPurge, Is.False);
		projected.CanPurge = true;
		Assert.That(source.Traits, Is.EqualTo(lifecycleFlags | CachedItemTraits.CanPurge));
		Assert.That(projected.CanPurge, Is.True);
		Assert.That(source.CanPurge, Is.True);
		source.CanPurge = false;
		Assert.That(projected.Traits, Is.EqualTo(lifecycleFlags));
		Assert.That(projected.CanPurge, Is.False);
	}

	[Test]
	public void WarmedPinChangesDoNotAllocateManagedMemory() {
		using var cache = new TestCache(1);
		cache.Set(0, 1);
		var item = cache.Get(0);
		cache.Invalidate(0);
		cache.Remove(0);
		for (var warmup = 0; warmup < 32; warmup++)
			ChangePins(item, 128);
		GC.GetAllocatedBytesForCurrentThread();

		var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
		var observedPinValues = ChangePins(item, 10_000);
		var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

		Assert.That(observedPinValues, Is.EqualTo(20_000));
		Assert.That(allocatedBytes, Is.Zero, "Repeated cache page pinning must not box enum values or inspect their attributes.");
		Assert.That(item.CanPurge, Is.True);
		Assert.That(item.Traits, Is.EqualTo(CachedItemTraits.Invalidated | CachedItemTraits.Purged | CachedItemTraits.CanPurge));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static int ChangePins(CachedItem<int> item, int iterations) {
		var observedPinValues = 0;
		for (var iteration = 0; iteration < iterations; iteration++) {
			item.CanPurge = false;
			if (!item.CanPurge)
				observedPinValues++;
			item.CanPurge = true;
			if (item.CanPurge)
				observedPinValues++;
		}
		return observedPinValues;
	}

	public enum RemovalOperation {
		Remove,
		Purge,
		Reap,
		Admission
	}

	private sealed class TestCache : CacheBase<int, int> {
		public TestCache(long capacity, ICacheReaper reaper = null)
			: base(reapStrategy: CacheReapPolicy.Oldest, maxCapacity: capacity, reaper: reaper) {
		}

		public Action<int, CachedItem<int>> PreparingRemoval { get; set; }

		public int FetchCount { get; private set; }

		protected override long EstimateSize(int value) => value;

		protected override int Fetch(int key) {
			FetchCount++;
			return 1;
		}

		protected override bool CheckStaleness(int key, CachedItem<int> item) => false;

		protected override void OnItemRemoving(int key, CachedItem<int> item) => PreparingRemoval?.Invoke(key, item);
	}
}
