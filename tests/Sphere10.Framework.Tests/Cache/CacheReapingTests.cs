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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests.Cache;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CacheReapingTests {

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(-0.1)]
	[TestCase(1.1)]
	[TestCase(double.NaN)]
	[TestCase(double.PositiveInfinity)]
	[TestCase(double.NegativeInfinity)]
	public void ReapLevel_RejectsValuesOutsideTheFiniteOpenInterval(double reapLevel) {
		Assert.That(() => new ActionCache<int, int>(key => key, reapLevel: reapLevel), Throws.ArgumentException);
	}

	[TestCase(0.01)]
	[TestCase(0.25)]
	[TestCase(0.999)]
	[TestCase(double.Epsilon)]
	public void ReapLevel_AcceptsFiniteFractions(double reapLevel) {
		using var cache = new ActionCache<int, int>(key => key, reapLevel: reapLevel);
		Assert.That(cache.ReapLevel, Is.EqualTo(reapLevel));
	}

	[Test]
	public void CapacityPressure_ReleasesABatchAndReusesTheFreedCapacity() {
		using var cache = new ActionCache<int, int>(key => 10, value => value, CacheReapPolicy.ASAP, maxCapacity: 100, reapLevel: 0.25);
		for (var key = 0; key < 10; key++)
			cache.Set(key, 10);
		var removed = 0;
		cache.ItemRemoved += (key, item) => removed++;

		cache.Set(10, 10);
		Assert.That(removed, Is.EqualTo(3));
		Assert.That(cache.CurrentSize, Is.EqualTo(80));
		cache.Set(11, 10);
		cache.Set(12, 10);
		Assert.That(removed, Is.EqualTo(3));
		Assert.That(cache.CurrentSize, Is.EqualTo(100));
		cache.Set(13, 10);
		Assert.That(removed, Is.EqualTo(6));
		Assert.That(cache.CurrentSize, Is.EqualTo(80));
	}

	[Test]
	public void CapacityPressure_UsesEstimatedSizeRatherThanEntryCount() {
		using var cache = new ActionCache<int, int>(key => key, value => value, CacheReapPolicy.Largest, maxCapacity: 100, reapLevel: 0.25);
		cache.Set(0, 60);
		for (var key = 1; key <= 4; key++)
			cache.Set(key, 10);
		var removed = new List<int>();
		cache.ItemRemoved += (key, item) => removed.Add(key);

		cache.Set(5, 1);
		Assert.That(removed, Is.EqualTo(new[] { 0 }));
		Assert.That(cache.CurrentSize, Is.EqualTo(41));
		Assert.That(cache.ItemCount, Is.EqualTo(5));
	}

	[Test]
	public void CapacityPressure_ReleasesAtLeastTheAdmissionDeficit() {
		using var cache = new ActionCache<int, int>(key => key, value => value, CacheReapPolicy.Smallest, maxCapacity: 100, reapLevel: 0.01);
		for (var key = 0; key < 10; key++)
			cache.Set(key, 10);
		var removed = 0;
		cache.ItemRemoved += (key, item) => removed++;

		cache.Set(10, 70);
		Assert.That(removed, Is.EqualTo(7));
		Assert.That(cache.CurrentSize, Is.EqualTo(100));
	}

	[Test]
	public void RawReap_DoesNotApplyThePressureFraction() {
		using var cache = new ActionCache<int, int>(key => 10, value => value, CacheReapPolicy.ASAP, maxCapacity: 100, reapLevel: 0.75);
		for (var key = 0; key < 10; key++)
			cache.Set(key, 10);

		Assert.That(cache.Reap(10), Is.EqualTo(10));
		Assert.That(cache.CurrentSize, Is.EqualTo(90));
		Assert.That(cache.Reap(0), Is.Zero);
		Assert.That(cache.ItemCount, Is.EqualTo(9));
		Assert.That(() => cache.Reap(-1), Throws.ArgumentException);
	}

	[TestCase(CacheReapPolicy.LeastUsed, 0)]
	[TestCase(CacheReapPolicy.Largest, 2)]
	[TestCase(CacheReapPolicy.Smallest, 0)]
	[TestCase(CacheReapPolicy.LongestIdle, 2)]
	[TestCase(CacheReapPolicy.Oldest, 1)]
	[TestCase(CacheReapPolicy.ASAP, 1)]
	public void Reap_SelectsLiveVictimsAccordingToTheConfiguredPolicy(CacheReapPolicy policy, int expectedVictim) {
		using var cache = new ActionCache<int, int>(key => key, value => value, policy, maxCapacity: 10);
		cache.Set(1, 3);
		cache.Set(0, 2);
		cache.Set(2, 5);
		cache.Get(2);
		cache.Get(2);
		cache.Get(1);
		cache.Get(1);
		cache.Get(0);
		var removed = new List<int>();
		cache.ItemRemoved += (key, item) => removed.Add(key);

		cache.Reap(1);
		Assert.That(removed, Is.EqualTo(new[] { expectedVictim }));
	}

	[Test]
	public void NonePolicy_DoesNotReapLiveEntries() {
		using var cache = new ActionCache<int, int>(key => 1, value => value, CacheReapPolicy.None, maxCapacity: 2);
		cache.Set(0, 1);
		cache.Set(1, 1);
		Assert.That(cache.Reap(1), Is.Zero);
		Assert.That(() => cache.Set(2, 1), Throws.InvalidOperationException);
		Assert.That(cache.CurrentSize, Is.EqualTo(2));
	}

	[TestCase(CacheReapPolicy.ASAP)]
	[TestCase(CacheReapPolicy.None)]
	public void ExpiredPinnedEntries_AreNotReaped(CacheReapPolicy policy) {
		using var cache = new ActionCache<int, int>(key => 1, value => value, policy, maxCapacity: 1);
		var entry = cache.Get(0);
		entry.CanPurge = false;
		cache.Invalidate(0);
		Assert.That(cache.Reap(1), Is.Zero);
		Assert.That(entry.Traits.HasFlag(CachedItemTraits.Purged), Is.False);
		Assert.That(cache.CurrentSize, Is.EqualTo(1));
	}

	[Test]
	public void RawReap_ReportsBestEffortWhenTheRequestExceedsAvailableSize() {
		using var cache = new ActionCache<int, int>(key => 1, value => value, CacheReapPolicy.ASAP, maxCapacity: 10);
		cache.Set(0, 3);
		cache.Set(1, 7);
		Assert.That(cache.Reap(long.MaxValue), Is.EqualTo(10));
		Assert.That(cache.ItemCount, Is.Zero);
	}

	[TestCase(CacheReapPolicy.LeastUsed)]
	[TestCase(CacheReapPolicy.Largest)]
	[TestCase(CacheReapPolicy.Smallest)]
	[TestCase(CacheReapPolicy.LongestIdle)]
	[TestCase(CacheReapPolicy.Oldest)]
	[TestCase(CacheReapPolicy.ASAP)]
	[TestCase(CacheReapPolicy.None)]
	public void Reap_ExpiredItemsPrecedeLiveVictims(CacheReapPolicy policy) {
		using var cache = new ActionCache<string, int>(key => 0, value => value, policy, maxCapacity: 30, reapLevel: 0.1);
		cache.Set("live", 20);
		cache.Set("expired", 5);
		cache.Invalidate("expired");
		var removed = new List<string>();
		cache.ItemRemoved += (key, item) => removed.Add(key);

		cache.Set("incoming", 10);
		Assert.That(removed, Is.EqualTo(new[] { "expired" }));
		Assert.That(cache.ContainsCachedItem("live"), Is.True);
		Assert.That(cache.CurrentSize, Is.EqualTo(30));
	}

	[Test]
	public void NonePolicy_ReapsInvalidatedEntriesWithNoncomparableKeys() {
		using var cache = new ActionCache<object, int>(key => 1, value => value, CacheReapPolicy.None, maxCapacity: 2);
		var invalidatedKey = new object();
		var liveKey = new object();
		var incomingKey = new object();
		cache.Set(invalidatedKey, 1);
		cache.Set(liveKey, 1);
		cache.Invalidate(invalidatedKey);

		cache.Set(incomingKey, 1);
		Assert.That(cache.ContainsCachedItem(invalidatedKey), Is.False);
		Assert.That(cache.ContainsCachedItem(liveKey), Is.True);
		Assert.That(cache.ContainsCachedItem(incomingKey), Is.True);
	}

	[TestCase(CacheReapPolicy.LeastUsed)]
	[TestCase(CacheReapPolicy.ASAP)]
	public void PinnedItems_CanLimitTheSoftBatchTargetWhenAdmissionStillFits(CacheReapPolicy policy) {
		using var cache = new ActionCache<string, int>(key => 0, value => value, policy, maxCapacity: 10, reapLevel: 0.75);
		cache.Set("pinned", 9);
		cache.Set("victim", 1);
		var pinned = cache.Get("pinned");
		pinned.CanPurge = false;

		cache.Set("incoming", 1);
		Assert.That(cache.CurrentSize, Is.EqualTo(10));
		Assert.That(cache.Get("pinned"), Is.SameAs(pinned));
		Assert.That(pinned.Traits.HasFlag(CachedItemTraits.Purged), Is.False);
	}

	[TestCase(CacheReapPolicy.LeastUsed)]
	[TestCase(CacheReapPolicy.ASAP)]
	public void PinnedItems_PreventAdmissionBeyondHardCapacity(CacheReapPolicy policy) {
		using var cache = new ActionCache<int, int>(key => 1, value => value, policy, maxCapacity: 1);
		var pinned = cache.Get(0);
		pinned.CanPurge = false;
		Assert.That(() => cache.Set(1, 1), Throws.InvalidOperationException);
		Assert.That(cache.CurrentSize, Is.EqualTo(1));
		Assert.That(cache.Get(0), Is.SameAs(pinned));
	}

	[Test]
	public void ZeroSizedCandidates_DoNotSatisfyAReleaseRequest() {
		using var cache = new ActionCache<int, int>(key => key, value => value, CacheReapPolicy.ASAP, maxCapacity: 1);
		for (var key = 0; key < 20; key++)
			cache.Set(key, 0);
		cache.Set(20, 1);
		Assert.That(cache.Reap(1), Is.EqualTo(1));
		Assert.That(cache.CurrentSize, Is.Zero);
	}

	[Test]
	public void ZeroCapacity_AllowsZeroSizedEntriesAndRejectsPositiveEntries() {
		using var cache = new ActionCache<int, int>(key => key, value => value, CacheReapPolicy.ASAP, maxCapacity: 0);
		cache.Set(0, 0);
		Assert.That(() => cache.Set(1, 1), Throws.InvalidOperationException);
		Assert.That(cache.ItemCount, Is.EqualTo(1));
		Assert.That(cache.CurrentSize, Is.Zero);
	}

	[Test]
	public void NegativeSize_IsRejectedWithoutChangingStoredEntries() {
		using var cache = new ActionCache<int, int>(key => key, value => value, CacheReapPolicy.ASAP, maxCapacity: 10);
		cache.Set(0, 5);
		Assert.That(() => cache.Set(1, -1), Throws.InvalidOperationException);
		Assert.That(() => cache.Set(0, -1), Throws.InvalidOperationException);
		Assert.That(cache.CurrentSize, Is.EqualTo(5));
		Assert.That(cache.Get(0).Value, Is.EqualTo(5));
	}

	[Test]
	public void GrowingAReplacement_ProtectsTheExistingEntryAndAccountsForItsSizeDelta() {
		using var cache = new ActionCache<string, int>(key => 0, value => value, CacheReapPolicy.Largest, maxCapacity: 10);
		cache.Set("replacement", 6);
		cache.Set("victim", 4);
		var original = cache.Get("replacement");
		var removed = new List<string>();
		cache.ItemRemoved += (key, item) => removed.Add(key);

		cache.Set("replacement", 8);
		Assert.That(cache.Get("replacement"), Is.SameAs(original));
		Assert.That(original.Value, Is.EqualTo(8));
		Assert.That(original.Size, Is.EqualTo(8));
		Assert.That(removed, Is.EqualTo(new[] { "victim" }));
		Assert.That(cache.CurrentSize, Is.EqualTo(8));
	}

	[Test]
	public void ShrinkingAReplacement_ReleasesCapacityWithoutEviction() {
		using var cache = new ActionCache<string, int>(key => 0, value => value, CacheReapPolicy.ASAP, maxCapacity: 10);
		cache.Set("replacement", 6);
		cache.Set("other", 4);
		cache.Invalidate("replacement");
		var removed = 0;
		cache.ItemRemoved += (key, item) => removed++;

		cache.Set("replacement", 2);
		Assert.That(cache.CurrentSize, Is.EqualTo(6));
		Assert.That(cache.ContainsCachedItem("replacement"), Is.True);
		Assert.That(removed, Is.Zero);
	}

	[Test]
	public void OversizedReplacement_IsRejectedBeforeEvictingOtherEntries() {
		using var cache = new ActionCache<string, int>(key => 0, value => value, CacheReapPolicy.ASAP, maxCapacity: 10);
		cache.Set("replacement", 6);
		cache.Set("other", 4);
		var original = cache.Get("replacement");
		var removed = 0;
		cache.ItemRemoved += (key, item) => removed++;

		Assert.That(() => cache.Set("replacement", 11), Throws.InvalidOperationException);
		Assert.That(original.Value, Is.EqualTo(6));
		Assert.That(cache.CurrentSize, Is.EqualTo(10));
		Assert.That(removed, Is.Zero);
	}

	[Test]
	public void RemovalCallback_ObservesPurgedStateEvenWhenItThrows() {
		using var cache = new ActionCache<int, int>(key => 1, value => value, CacheReapPolicy.ASAP, maxCapacity: 1);
		var item = cache.Get(0);
		EventHandlerEx<int, CachedItem<int>> handler = (key, removed) => {
			Assert.That(removed.Traits.HasFlag(CachedItemTraits.Purged), Is.True);
			Assert.That(removed.CanPurge, Is.False);
			throw new ApplicationException("Removal failed after observing the purged entry.");
		};
		cache.ItemRemoved += handler;
		Assert.That(() => cache.Remove(0), Throws.TypeOf<ApplicationException>());
		cache.ItemRemoved -= handler;
		Assert.That(item.Traits.HasFlag(CachedItemTraits.Purged), Is.True);
		Assert.That(cache.CurrentSize, Is.Zero);
		Assert.That(cache.ItemCount, Is.Zero);
	}

	[Test]
	public void RemovalCallback_RefillingTheCacheDoesNotBreakHardCapacity() {
		using var cache = new ActionCache<int, int>(key => 1, value => value, CacheReapPolicy.ASAP, maxCapacity: 3);
		for (var key = 0; key < 3; key++)
			cache.Set(key, 1);
		cache.ItemRemoved += (key, item) => {
			if (key == 0)
				cache.Set(100, 1);
		};

		cache.Set(3, 1);
		Assert.That(cache.CurrentSize, Is.EqualTo(3));
		Assert.That(cache.ItemCount, Is.EqualTo(3));
		Assert.That(cache.ContainsCachedItem(100), Is.True);
		Assert.That(cache.ContainsCachedItem(3), Is.True);
	}

	[Test]
	public void RemovalCallback_ReplacingASnapshottedCandidatePreservesTheNewEntry() {
		using var cache = new ActionCache<int, int>(key => 1, value => value, CacheReapPolicy.ASAP, maxCapacity: 3);
		for (var key = 0; key < 3; key++)
			cache.Set(key, 1);
		CachedItem<int> replacement = null;
		EventHandlerEx<int, CachedItem<int>> handler = (key, item) => {
			if (key != 0)
				return;
			cache.Remove(1);
			cache.Set(1, 1);
			replacement = cache.Get(1);
			cache.Set(100, 1);
		};
		cache.ItemRemoved += handler;

		cache.Set(3, 1);
		cache.ItemRemoved -= handler;
		Assert.That(cache.Get(1), Is.SameAs(replacement));
		Assert.That(replacement.Traits.HasFlag(CachedItemTraits.Purged), Is.False);
		Assert.That(cache.CurrentSize, Is.EqualTo(3));
	}

	[Test]
	public void Purge_CallbackAddedEntriesRemainStoredAndAccountedFor() {
		using var cache = new ActionCache<int, int>(key => 1, value => value, CacheReapPolicy.ASAP, maxCapacity: 3);
		cache.Set(0, 1);
		cache.Set(1, 1);
		cache.ItemRemoved += (key, item) => {
			if (key == 0)
				cache.Set(100, 1);
		};

		cache.Purge();
		Assert.That(cache.ContainsCachedItem(100), Is.True);
		Assert.That(cache.ItemCount, Is.EqualTo(1));
		Assert.That(cache.CurrentSize, Is.EqualTo(1));
	}

	[Test]
	public void Dispose_RejectsCallbackAdditionsAndCleansAllOriginalEntries() {
		using var cache = new ActionCache<int, int>(key => 1, value => value, CacheReapPolicy.ASAP, maxCapacity: 3);
		cache.Set(0, 1);
		cache.Set(1, 1);
		var removed = 0;
		cache.ItemRemoved += (key, item) => {
			removed++;
			Assert.That(() => cache.Set(100, 1), Throws.TypeOf<ObjectDisposedException>());
		};

		cache.Dispose();
		Assert.That(removed, Is.EqualTo(2));
		Assert.That(cache.ItemCount, Is.Zero);
		Assert.That(cache.CurrentSize, Is.Zero);
	}

	[TestCase(double.Epsilon, 2)]
	[TestCase(0.9999999999999999, 1)]
	public void LongCapacity_RoundsTheBatchWithoutOverflow(double reapLevel, int expectedCount) {
		using var cache = new ActionCache<int, long>(key => 1, value => value, CacheReapPolicy.Smallest, maxCapacity: long.MaxValue, reapLevel: reapLevel);
		cache.Set(0, long.MaxValue - 1);
		cache.Set(1, 1);
		cache.Set(2, 1);
		Assert.That(cache.CurrentSize, Is.LessThanOrEqualTo(long.MaxValue));
		Assert.That(cache.CurrentSize, Is.GreaterThanOrEqualTo(0));
		Assert.That(cache.ItemCount, Is.EqualTo(expectedCount));
	}

	[Test]
	public void ConcurrentSameKeyMisses_FetchOnceAndKeepMetadataCounts() {
		var fetches = 0;
		using var cache = new ActionCache<int, int>(key => Interlocked.Increment(ref fetches), value => 1, maxCapacity: 1);
		Parallel.For(0, 2000, index => Assert.That(cache.Get(42).Value, Is.EqualTo(1)));
		Assert.That(fetches, Is.EqualTo(1));
		Assert.That(cache.ItemCount, Is.EqualTo(1));
		Assert.That(cache.CurrentSize, Is.EqualTo(1));
		Assert.That(cache.TotalAccesses, Is.EqualTo(2000));
		Assert.That(cache.CachedItems.Single().AccessedCount, Is.EqualTo(2000));
	}

}
