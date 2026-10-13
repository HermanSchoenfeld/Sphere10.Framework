// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests.Cache;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class PooledCacheReaperTests {

	[Test]
	public void Constructor_RejectsNegativeCapacity() {
		Assert.That(() => new PooledCacheReaper(-1), Throws.ArgumentException);
	}

	[Test]
	public void IsolatedReaper_RawReleaseDoesNotApplyTheAdmissionFractionAgain() {
		var reaper = new IsolatedCacheReaper();
		using var cache = new ActionCache<int, int>(key => 1, value => value, CacheReapPolicy.ASAP, maxCapacity: 10, reaper: reaper, reapLevel: 0.75);
		for (var key = 0; key < 10; key++)
			cache.Set(key, 1);

		Assert.That(reaper.MakeSpace(cache, 1), Is.EqualTo(1));
		Assert.That(cache.CurrentSize, Is.EqualTo(9));
		Assert.That(reaper.AvailableSpace(), Is.EqualTo(1));
	}

	[Test]
	public void IsolatedReaper_RejectsMissingOrDuplicateRegistration() {
		var reaper = new IsolatedCacheReaper();
		Assert.That(() => reaper.AvailableSpace(), Throws.InvalidOperationException);
		using var cache = new ActionCache<int, int>(key => 1, value => value, maxCapacity: 10, reaper: reaper);
		Assert.That(() => reaper.Register(cache), Throws.InvalidOperationException);
		cache.Dispose();
		Assert.That(() => reaper.AvailableSpace(), Throws.InvalidOperationException);
		Assert.That(() => reaper.Deregister(cache), Throws.InvalidOperationException);
	}

	[Test]
	public void IsolatedReaper_MembershipUsesReferenceIdentity() {
		var reaper = new IsolatedCacheReaper();
		using var first = new EquivalentCache(reaper);
		using var other = new EquivalentCache(new IsolatedCacheReaper());
		first.Set(0, 1);
		Assert.That(first.Equals(other), Is.True);

		Assert.That(() => reaper.Deregister(other), Throws.InvalidOperationException);
		Assert.That(() => reaper.MakeSpace(other, 1), Throws.ArgumentException);
		Assert.That(first.CurrentSize, Is.EqualTo(1));
		Assert.That(reaper.AvailableSpace(), Is.EqualTo(9));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void MakeSpace_RejectsInvalidTargetsBeforeReaping(bool pooled) {
		ICacheReaper reaper = pooled ? new PooledCacheReaper(10) : new IsolatedCacheReaper();
		using var cache = new ActionCache<int, int>(key => 1, value => value, CacheReapPolicy.ASAP, maxCapacity: 10, reaper: reaper);
		cache.Set(0, 1);

		Assert.That(() => reaper.MakeSpace(cache, -1), Throws.ArgumentException);
		Assert.That(() => reaper.MakeSpace(cache, 11), Throws.InvalidOperationException);
		Assert.That(reaper.MakeSpace(cache, 0), Is.Zero);
		Assert.That(cache.ContainsCachedItem(0), Is.True);
		Assert.That(cache.CurrentSize, Is.EqualTo(1));
	}

	[Test]
	public void Registration_RejectsDuplicateOrMissingMembers() {
		var pool = new PooledCacheReaper(10);
		using var member = new ActionCache<int, int>(key => 1, value => value, maxCapacity: 10, reaper: pool);
		using var outsider = new ActionCache<string, int>(key => 1, value => value, maxCapacity: 10);
		member.Set(0, 1);

		Assert.That(() => pool.Register(member), Throws.InvalidOperationException);
		Assert.That(() => pool.Deregister(outsider), Throws.InvalidOperationException);
		Assert.That(() => pool.MakeSpace(outsider, 1), Throws.InvalidOperationException);
		Assert.That(member.ContainsCachedItem(0), Is.True);
		Assert.That(pool.CurrentSize, Is.EqualTo(1));
	}

	[Test]
	public void PooledReaper_RawReleaseDoesNotApplyMemberFractionsAgain() {
		var pool = new PooledCacheReaper(10);
		using var requester = new ActionCache<int, int>(key => 1, value => value, CacheReapPolicy.ASAP, maxCapacity: 10, reaper: pool, reapLevel: 0.75);
		using var donor = new ActionCache<string, int>(key => 1, value => value, CacheReapPolicy.ASAP, maxCapacity: 10, reaper: pool, reapLevel: 0.75);
		requester.Set(0, 1);
		for (var key = 0; key < 9; key++)
			donor.Set(key.ToString(), 1);

		Assert.That(pool.MakeSpace(requester, 2), Is.EqualTo(2));
		Assert.That(requester.ItemCount, Is.Zero);
		Assert.That(donor.ItemCount, Is.EqualTo(8));
		Assert.That(pool.CurrentSize, Is.EqualTo(8));
	}

	[Test]
	public void PooledReaper_UsesOnlyTheRepresentationIndependentContract() {
		var pool = new PooledCacheReaper(10);
		var requester = new MetadataCache(10);
		var donor = new MetadataCache(10);
		pool.Register(requester);
		pool.Register(donor);
		try {
			requester.Populate(2);
			donor.Populate(3);
			Assert.That(pool.MakeSpace(requester, 4), Is.EqualTo(5));
			Assert.That(requester.LastReapTarget, Is.EqualTo(4));
			Assert.That(donor.LastReapTarget, Is.EqualTo(2));
			Assert.That(pool.CurrentSize, Is.Zero);
			Assert.That(pool.TotalItemCount, Is.Zero);
		} finally {
			pool.Deregister(requester);
			pool.Deregister(donor);
		}
	}

	[Test]
	public void Disposal_AttemptsAllRemovalsAndRetainsDeregistrationFailures() {
		var removalFailure = new ApplicationException("A removal callback failed.");
		var deregistrationFailure = new InvalidOperationException("The reaper failed after deregistration.");
		var reaper = new FailingDeregistrationReaper(deregistrationFailure);
		using var cache = new ActionCache<int, int>(key => 1, value => value, maxCapacity: 2, reaper: reaper);
		cache.Set(0, 1);
		cache.Set(1, 1);
		var removals = 0;
		cache.ItemRemoved += (key, item) => {
			removals++;
			throw removalFailure;
		};

		Assert.That(() => cache.Dispose(), Throws.TypeOf<AggregateException>()
			.With.Property(nameof(AggregateException.InnerExceptions)).EqualTo(new Exception[] { removalFailure, removalFailure, deregistrationFailure }));
		Assert.That(removals, Is.EqualTo(2));
		Assert.That(cache.CurrentSize, Is.Zero);
		Assert.That(cache.ItemCount, Is.Zero);
		Assert.That(reaper.Deregistrations, Is.EqualTo(1));
		Assert.That(() => cache.Set(2, 1), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(() => cache.Dispose(), Throws.Nothing);
		Assert.That(reaper.Deregistrations, Is.EqualTo(1));
	}

	[Test]
	public void DeregisteredMember_CannotAdmitValuesOutsideTheSharedBudget() {
		var pool = new PooledCacheReaper(10);
		var cache = new ActionCache<int, int>(key => 1, value => value, maxCapacity: 10, reaper: pool);
		cache.Set(0, 10);
		var original = cache.Get(0);
		pool.Deregister(cache);
		try {
			Assert.That(() => cache.Set(1, 5), Throws.InvalidOperationException);
			Assert.That(cache.Get(0), Is.SameAs(original));
			Assert.That(original.Value, Is.EqualTo(10));
			Assert.That(cache.ContainsCachedItem(1), Is.False);
			Assert.That(cache.CurrentSize, Is.EqualTo(10));
			Assert.That(cache.ItemCount, Is.EqualTo(1));
			Assert.That(pool.CurrentSize, Is.Zero);
		} finally {
			Assert.That(() => cache.Dispose(), Throws.TypeOf<AggregateException>());
		}
		Assert.That(cache.CurrentSize, Is.Zero);
		Assert.That(cache.ItemCount, Is.Zero);
		Assert.That(() => cache.Dispose(), Throws.Nothing);
	}

	[Test]
	public void Pressure_TargetIsCalculatedOnceForTheCombinedCapacity() {
		var pool = new PooledCacheReaper(100);
		using var requester = new ActionCache<int, int>(key => 10, value => value, CacheReapPolicy.ASAP, maxCapacity: 100, reaper: pool, reapLevel: 0.25);
		using var firstDonor = new ActionCache<string, int>(key => 10, value => value, CacheReapPolicy.ASAP, maxCapacity: 100, reaper: pool, reapLevel: 0.75);
		using var secondDonor = new ActionCache<long, int>(key => 10, value => value, CacheReapPolicy.ASAP, maxCapacity: 100, reaper: pool, reapLevel: 0.75);
		requester.Set(0, 10);
		for (var key = 0; key < 4; key++)
			firstDonor.Set(key.ToString(), 10);
		for (var key = 0; key < 5; key++)
			secondDonor.Set(key, 10);
		firstDonor.LastAccessedOn = DateTime.Now - TimeSpan.FromHours(2);
		secondDonor.LastAccessedOn = DateTime.Now - TimeSpan.FromHours(1);
		var removed = 0;
		requester.ItemRemoved += (key, item) => removed++;
		firstDonor.ItemRemoved += (key, item) => removed++;
		secondDonor.ItemRemoved += (key, item) => removed++;

		requester.Set(1, 1);
		Assert.That(removed, Is.EqualTo(3));
		Assert.That(pool.CurrentSize, Is.EqualTo(71));
		Assert.That(firstDonor.CurrentSize, Is.EqualTo(20));
		Assert.That(secondDonor.CurrentSize, Is.EqualTo(50));
		Assert.That(pool.TotalItemCount, Is.EqualTo(8));
	}

	[Test]
	public void PinnedMembers_CanLimitTheSoftBatchWhenAdmissionFits() {
		var pool = new PooledCacheReaper(10);
		using var requester = new ActionCache<int, int>(key => key, value => value, CacheReapPolicy.ASAP, maxCapacity: 10, reaper: pool, reapLevel: 0.75);
		using var donor = new ActionCache<string, int>(key => 0, value => value, CacheReapPolicy.ASAP, maxCapacity: 10, reaper: pool);
		requester.Set(0, 8);
		requester.Get(0).CanPurge = false;
		donor.Set("victim", 2);

		requester.Set(1, 1);
		Assert.That(pool.CurrentSize, Is.EqualTo(9));
		Assert.That(requester.ContainsCachedItem(0), Is.True);
		Assert.That(requester.ContainsCachedItem(1), Is.True);
		Assert.That(donor.ItemCount, Is.Zero);
	}

	[Test]
	public void PinnedMembers_KeepTheCombinedHardLimit() {
		var pool = new PooledCacheReaper(10);
		using var requester = new ActionCache<int, int>(key => key, value => value, CacheReapPolicy.ASAP, maxCapacity: 10, reaper: pool);
		using var donor = new ActionCache<string, int>(key => 0, value => value, CacheReapPolicy.ASAP, maxCapacity: 10, reaper: pool);
		requester.Set(0, 8);
		requester.Get(0).CanPurge = false;
		donor.Set("pinned", 2);
		donor.Get("pinned").CanPurge = false;

		Assert.That(() => requester.Set(1, 1), Throws.InvalidOperationException);
		Assert.That(pool.CurrentSize, Is.EqualTo(10));
		Assert.That(pool.TotalItemCount, Is.EqualTo(2));
	}

	[Test]
	public void Callback_PopulatingAnotherMemberCannotConsumeUncountedCapacity() {
		var pool = new PooledCacheReaper(3);
		using var requester = new ActionCache<int, int>(key => 1, value => value, CacheReapPolicy.ASAP, maxCapacity: 3, reaper: pool);
		using var donor = new ActionCache<string, int>(key => 1, value => value, CacheReapPolicy.ASAP, maxCapacity: 3, reaper: pool);
		requester.Set(0, 1);
		donor.Set("first", 1);
		donor.Set("second", 1);
		requester.ItemRemoved += (key, item) => {
			if (key == 0)
				donor.Set("callback", 1);
		};

		requester.Set(1, 1);
		Assert.That(pool.CurrentSize, Is.EqualTo(3));
		Assert.That(pool.CurrentSize, Is.EqualTo(requester.CurrentSize + donor.CurrentSize));
		Assert.That(requester.ContainsCachedItem(1), Is.True);
		Assert.That(donor.ContainsCachedItem("callback"), Is.True);
	}

	[Test]
	public void MemberDisposal_RemovesCapacityAndPreservesOpenScopeLockIdentity() {
		var pool = new PooledCacheReaper(10);
		using var disposedMember = new ActionCache<int, int>(key => key, value => value, CacheReapPolicy.ASAP, maxCapacity: 10, reaper: pool);
		using var remainingMember = new ActionCache<string, int>(key => 1, value => value, CacheReapPolicy.ASAP, maxCapacity: 10, reaper: pool);
		disposedMember.Set(0, 8);
		remainingMember.Set("remaining", 2);
		var originalLock = disposedMember.ThreadLock;

		using (disposedMember.EnterWriteScope()) {
			disposedMember.Dispose();
			Assert.That(disposedMember.ParentSyncObject, Is.SameAs(pool));
			Assert.That(disposedMember.ThreadLock, Is.SameAs(originalLock));
			Assert.That(pool.CurrentSize, Is.EqualTo(2));
			Assert.That(pool.TotalItemCount, Is.EqualTo(1));
		}
		Assert.That(originalLock.IsWriteLockHeld, Is.False);
		remainingMember.Set("new", 8);
		Assert.That(pool.CurrentSize, Is.EqualTo(10));
	}

	[Test]
	public void Registration_RejectsCachesThatHaveAlreadyBeenUsedOrParented() {
		var pool = new PooledCacheReaper(10);
		using var nonempty = new ActionCache<int, int>(key => 1, value => value, maxCapacity: 10);
		nonempty.Set(0, 1);
		Assert.That(() => pool.Register(nonempty), Throws.ArgumentException);
		using var parented = new ActionCache<int, int>(key => 1, value => value, maxCapacity: 10);
		parented.ParentSyncObject = new SynchronizedObject();
		Assert.That(() => pool.Register(parented), Throws.ArgumentException);
		Assert.That(pool.CurrentSize, Is.Zero);
	}

	[Test]
	public void Membership_UsesReferenceIdentityForCachesWithEquivalentValues() {
		var pool = new PooledCacheReaper(10);
		using var first = new EquivalentCache(pool);
		using var second = new EquivalentCache(pool);
		Assert.That(first.Equals(second), Is.True);
		first.Set(0, 1);
		second.Set(0, 1);
		Assert.That(pool.CurrentSize, Is.EqualTo(2));
		Assert.That(pool.TotalItemCount, Is.EqualTo(2));
		first.Dispose();
		Assert.That(pool.CurrentSize, Is.EqualTo(1));
		Assert.That(second.ContainsCachedItem(0), Is.True);
	}

	[Test]
	public void LongCapacity_IsSharedWithoutNarrowingToInt32() {
		var pool = new PooledCacheReaper(long.MaxValue);
		using var first = new ActionCache<int, long>(key => key, value => value, CacheReapPolicy.ASAP, maxCapacity: long.MaxValue, reaper: pool);
		using var second = new ActionCache<string, long>(key => 0, value => value, CacheReapPolicy.ASAP, maxCapacity: long.MaxValue, reaper: pool);
		first.Set(0, long.MaxValue - 1);
		second.Set("one", 1);
		Assert.That(pool.CurrentSize, Is.EqualTo(long.MaxValue));
		Assert.That(pool.AvailableSpace(), Is.Zero);
		second.Set("new", 1);
		Assert.That(pool.CurrentSize, Is.EqualTo(1));
		Assert.That(pool.TotalItemCount, Is.EqualTo(1));
	}

	[Test]
	public void ConcurrentMembers_EnforceCombinedCapacityAfterEveryFetch() {
		var pool = new PooledCacheReaper(80);
		var caches = Enumerable.Range(0, 4).Select(index => new ActionCache<int, int>(key => 1, value => value, CacheReapPolicy.ASAP, maxCapacity: 80, reaper: pool)).ToArray();
		var fetches = 0;
		foreach (var cache in caches)
			cache.ItemFetched += (key, value) => {
				Interlocked.Increment(ref fetches);
				Assert.That(pool.CurrentSize, Is.LessThanOrEqualTo(80));
			};
		try {
			Parallel.For(0, caches.Length, index => {
				for (var key = 0; key < 300; key++)
					Assert.That(caches[index].Get(key).Value, Is.EqualTo(1));
			});
			Assert.That(fetches, Is.EqualTo(1200));
			Assert.That(pool.CurrentSize, Is.LessThanOrEqualTo(80));
			Assert.That(pool.CurrentSize, Is.EqualTo(caches.Sum(cache => cache.CurrentSize)));
			Assert.That(pool.TotalItemCount, Is.EqualTo(caches.Sum(cache => (long)cache.ItemCount)));
		} finally {
			foreach (var cache in caches)
				cache.Dispose();
		}
		Assert.That(pool.CurrentSize, Is.Zero);
		Assert.That(pool.TotalItemCount, Is.Zero);
	}

	private sealed class EquivalentCache : CacheBase<int, int> {

		public EquivalentCache(ICacheReaper reaper)
			: base(reapStrategy: CacheReapPolicy.ASAP, maxCapacity: 10, reaper: reaper) {
		}

		public override bool Equals(object obj) => obj is EquivalentCache;

		public override int GetHashCode() => 0;

		protected override int Fetch(int key) => 1;

		protected override long EstimateSize(int value) => value;

		protected override bool CheckStaleness(int key, CachedItem<int> item) => false;

	}

	private sealed class MetadataCache : SynchronizedObject, ICacheBase {

		private long _currentSize;

		public MetadataCache(long maxCapacity) {
			MaxCapacity = maxCapacity;
		}

		public int ItemCount => CurrentSize == 0 ? 0 : 1;

		public long CurrentSize {
			get {
				using (EnterReadScope())
					return _currentSize;
			}
		}

		public long MaxCapacity { get; }

		public double ReapLevel => 0.25;

		public DateTime LastUpdateOn => DateTime.MinValue;

		public DateTime LastAccessedOn => DateTime.MinValue;

		public long TotalAccesses => 0;

		public TimeSpan ExpirationDuration => TimeSpan.MaxValue;

		public ExpirationPolicy ExpirationPolicy => ExpirationPolicy.None;

		public CacheReapPolicy ReapPolicy => CacheReapPolicy.ASAP;

		public NullValuePolicy NullValuePolicy => NullValuePolicy.CacheNormally;

		public long LastReapTarget { get; private set; }

		public void Populate(long size) {
			using (EnterWriteScope())
				_currentSize = size;
		}

		public long Reap(long minimumRelease) {
			using (EnterWriteScope()) {
				LastReapTarget = minimumRelease;
				var released = minimumRelease == 0 ? 0 : _currentSize;
				_currentSize -= released;
				return released;
			}
		}

		public void Purge() => Populate(0);

	}

	private sealed class FailingDeregistrationReaper : ICacheReaper {

		private readonly IsolatedCacheReaper _inner = new();
		private readonly Exception _deregistrationFailure;

		public FailingDeregistrationReaper(Exception deregistrationFailure) {
			_deregistrationFailure = deregistrationFailure;
		}

		public int Deregistrations { get; private set; }

		public void Register(ICacheBase cache) => _inner.Register(cache);

		public void Deregister(ICacheBase cache) {
			Deregistrations++;
			_inner.Deregister(cache);
			throw _deregistrationFailure;
		}

		public long AvailableSpace() => _inner.AvailableSpace();

		public long MakeSpace(ICacheBase requestingCache, long requestedSpace) => _inner.MakeSpace(requestingCache, requestedSpace);

	}

}
