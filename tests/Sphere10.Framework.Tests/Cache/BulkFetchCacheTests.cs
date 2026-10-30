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
using NUnit.Framework;

namespace Sphere10.Framework.Tests.Cache;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BulkFetchCacheTests {

	[TestCase(NullValuePolicy.CacheNormally)]
	[TestCase(NullValuePolicy.ReturnButDontCache)]
	[TestCase(NullValuePolicy.Throw)]
	public void Get_InitialMissingKey_LoadsOneBatch(NullValuePolicy nullValuePolicy) {
		var fetches = 0;
		using var cache = new BulkFetchActionCache<int, string>(() => {
			fetches++;
			return new Dictionary<int, string> { [1] = "one", [2] = "two" };
		}, nullValuePolicy: nullValuePolicy);

		if (nullValuePolicy == NullValuePolicy.Throw)
			Assert.That(() => cache.Get(404), Throws.TypeOf<SoftwareException>());
		else
			Assert.That(cache.Get(404).Value, Is.Null);

		Assert.That(fetches, Is.EqualTo(1));
		Assert.That(cache[1], Is.EqualTo("one"));
		Assert.That(cache[2], Is.EqualTo("two"));
		Assert.That(fetches, Is.EqualTo(1));
	}

	[TestCase(NullValuePolicy.CacheNormally)]
	[TestCase(NullValuePolicy.ReturnButDontCache)]
	[TestCase(NullValuePolicy.Throw)]
	public void Get_MissingKeyAfterInitialLoad_LoadsOneNewBatch(NullValuePolicy nullValuePolicy) {
		var fetches = 0;
		using var cache = new BulkFetchActionCache<int, string>(() => {
			fetches++;
			return new Dictionary<int, string> { [1] = fetches.ToString() };
		}, nullValuePolicy: nullValuePolicy);
		Assert.That(cache[1], Is.EqualTo("1"));

		if (nullValuePolicy == NullValuePolicy.Throw)
			Assert.That(() => cache.Get(404), Throws.TypeOf<SoftwareException>());
		else
			Assert.That(cache.Get(404).Value, Is.Null);

		Assert.That(fetches, Is.EqualTo(2));
		Assert.That(cache[1], Is.EqualTo("2"));
		Assert.That(fetches, Is.EqualTo(2));
	}

	[Test]
	public void Get_UncachedMissingKey_LoadsOneBatchPerRequest() {
		var fetches = 0;
		using var cache = new BulkFetchActionCache<int, string>(() => {
			fetches++;
			return new Dictionary<int, string>();
		}, nullValuePolicy: NullValuePolicy.ReturnButDontCache);

		for (var request = 1; request <= 3; request++) {
			Assert.That(cache.Get(404).Value, Is.Null);
			Assert.That(fetches, Is.EqualTo(request));
			Assert.That(cache.ItemCount, Is.Zero);
		}
	}

	[Test]
	public void Get_ValidHit_DoesNotRefetch() {
		var fetches = 0;
		using var cache = new BulkFetchActionCache<int, string>(() => {
			fetches++;
			return new Dictionary<int, string> { [1] = "one", [2] = "two" };
		});

		var item = cache.Get(1);
		Assert.That(cache.Get(1), Is.SameAs(item));
		Assert.That(cache[2], Is.EqualTo("two"));
		Assert.That(item.AccessedCount, Is.EqualTo(2));
		Assert.That(fetches, Is.EqualTo(1));
	}

	[TestCase("Expired")]
	[TestCase("Invalidated")]
	[TestCase("Stale")]
	public void Get_RefreshesWholeSnapshotOnce(string reason) {
		var fetches = 0;
		var stale = false;
		using var cache = new BulkFetchActionCache<int, string>(() => {
			fetches++;
			return new Dictionary<int, string> { [1] = $"one-{fetches}", [2] = $"two-{fetches}" };
		}, expirationStrategy: ExpirationPolicy.SinceFetchedTime, expirationDuration: TimeSpan.FromHours(1),
			staleValuePolicy: StaleValuePolicy.CheckStaleOnDemand, stalenessChecker: (_, _) => stale);
		var first = cache.Get(1);
		if (reason == "Expired")
			first.FetchedOn = DateTime.Now.AddHours(-2);
		else if (reason == "Invalidated")
			cache.Invalidate(1);
		else
			stale = true;

		Assert.That(cache[1], Is.EqualTo("one-2"));
		Assert.That(fetches, Is.EqualTo(2));
		Assert.That(cache.CachedItems.Select(item => item.Value), Is.EquivalentTo(new[] { "one-2", "two-2" }));
	}

	[Test]
	public void Get_InitialSnapshotAlreadyStale_LoadsOneBatch() {
		var fetches = 0;
		using var cache = new BulkFetchActionCache<int, string>(() => {
			fetches++;
			return new Dictionary<int, string> { [1] = fetches.ToString() };
		}, staleValuePolicy: StaleValuePolicy.CheckStaleOnDemand, stalenessChecker: (_, _) => true);

		Assert.That(cache[1], Is.EqualTo("1"));
		Assert.That(fetches, Is.EqualTo(1));
	}

	[TestCase("Expired")]
	[TestCase("Invalidated")]
	[TestCase("Stale")]
	public void FetchOnceOnly_GetRetainsSnapshotUntilForceRefresh(string reason) {
		var fetches = 0;
		var stale = false;
		using var cache = new BulkFetchActionCache<int, string>(() => {
			fetches++;
			return new Dictionary<int, string> { [1] = $"one-{fetches}", [2] = $"two-{fetches}" };
		}, expirationStrategy: ExpirationPolicy.SinceFetchedTime, expirationDuration: TimeSpan.FromHours(1),
			fetchOnceOnly: true, staleValuePolicy: StaleValuePolicy.CheckStaleOnDemand, stalenessChecker: (_, _) => stale);
		var first = cache.Get(1);
		if (reason == "Expired")
			first.FetchedOn = DateTime.Now.AddHours(-2);
		else if (reason == "Invalidated")
			cache.Invalidate(1);
		else
			stale = true;

		Assert.That(cache.ContainsCachedItem(1), Is.True);
		Assert.That(cache.Get(1), Is.SameAs(first));
		Assert.That(cache[1], Is.EqualTo("one-1"));
		Assert.That(cache[2], Is.EqualTo("two-1"));
		Assert.That(fetches, Is.EqualTo(1));
		cache.ForceRefresh();
		Assert.That(fetches, Is.EqualTo(2));
		Assert.That(cache[1], Is.EqualTo("one-2"));
		Assert.That(cache[2], Is.EqualTo("two-2"));
		Assert.That(fetches, Is.EqualTo(2));
	}

	[TestCase(NullValuePolicy.CacheNormally)]
	[TestCase(NullValuePolicy.ReturnButDontCache)]
	[TestCase(NullValuePolicy.Throw)]
	public void FetchOnceOnly_MissingKey_DoesNotRefetch(NullValuePolicy nullValuePolicy) {
		var fetches = 0;
		using var cache = new BulkFetchActionCache<int, string>(() => {
			fetches++;
			return new Dictionary<int, string> { [1] = "one" };
		}, fetchOnceOnly: true, nullValuePolicy: nullValuePolicy);

		for (var request = 0; request < 2; request++) {
			if (nullValuePolicy == NullValuePolicy.Throw)
				Assert.That(() => cache.Get(404), Throws.TypeOf<SoftwareException>());
			else
				Assert.That(cache.Get(404).Value, Is.Null);
			Assert.That(fetches, Is.EqualTo(1));
			Assert.That(cache[1], Is.EqualTo("one"));
		}
	}

	[Test]
	public void ForceRefresh_FailedFetch_PurgesPreviousSnapshotAndAllowsRetry() {
		var fetches = 0;
		var fail = false;
		using var cache = new BulkFetchActionCache<int, string>(() => {
			fetches++;
			if (fail)
				throw new InvalidOperationException("Source unavailable");
			return new Dictionary<int, string> { [1] = fetches.ToString() };
		}, fetchOnceOnly: true);
		Assert.That(cache[1], Is.EqualTo("1"));

		fail = true;
		Assert.That(() => cache.ForceRefresh(), Throws.TypeOf<InvalidOperationException>());
		Assert.That(cache.ItemCount, Is.Zero);
		Assert.That(fetches, Is.EqualTo(2));
		fail = false;
		Assert.That(cache[1], Is.EqualTo("3"));
		Assert.That(fetches, Is.EqualTo(3));
	}

	[Test]
	public void Purge_FetchOnceOnly_LoadsNewSnapshotOnNextGet() {
		var fetches = 0;
		using var cache = new BulkFetchActionCache<int, string>(() => {
			fetches++;
			return new Dictionary<int, string> { [1] = fetches.ToString() };
		}, fetchOnceOnly: true);
		Assert.That(cache[1], Is.EqualTo("1"));
		cache.Purge();
		Assert.That(cache[1], Is.EqualTo("2"));
		Assert.That(fetches, Is.EqualTo(2));
	}

	[Test]
	public void PreloadedCache_ForceRefresh_ReadsTheBorrowedSourceDictionary() {
		var source = new Dictionary<int, string> { [1] = "one" };
		using var cache = new PreloadedCache<int, string>(source);
		Assert.That(cache[1], Is.EqualTo("one"));
		source[1] = "updated";
		source[2] = "two";
		cache.Invalidate(1);
		Assert.That(cache[1], Is.EqualTo("one"));
		Assert.That(cache.ContainsCachedItem(2), Is.False);
		cache.ForceRefresh();
		Assert.That(cache[1], Is.EqualTo("updated"));
		Assert.That(cache[2], Is.EqualTo("two"));
	}

	[Test]
	public void GetAllCachedValues_InitialRequest_LoadsWholeSnapshotOnce() {
		var fetches = 0;
		using var cache = new BulkFetchActionCache<int, string>(() => {
			fetches++;
			return new Dictionary<int, string> { [1] = "one", [2] = "two" };
		});

		Assert.That(cache.GetAllCachedValues(), Is.EquivalentTo(new[] { "one", "two" }));
		Assert.That(cache.GetAllCachedValues(), Is.EquivalentTo(new[] { "one", "two" }));
		Assert.That(fetches, Is.EqualTo(1));
	}

	[TestCase(true, "Expired")]
	[TestCase(true, "Invalidated")]
	[TestCase(false, "Expired")]
	[TestCase(false, "Invalidated")]
	public void Reap_ThroughBaseReference_PreservesCompleteSnapshot(bool fetchOnceOnly, string reason) {
		var fetches = 0;
		using var cache = new BulkFetchActionCache<int, string>(() => {
			fetches++;
			return new Dictionary<int, string> { [1] = $"one-{fetches}", [2] = $"two-{fetches}" };
		}, expirationStrategy: ExpirationPolicy.SinceFetchedTime, expirationDuration: TimeSpan.FromHours(1), fetchOnceOnly: fetchOnceOnly);
		var original = cache.Get(1);
		if (reason == "Expired")
			original.FetchedOn = DateTime.Now.AddHours(-2);
		else
			cache.Invalidate(1);
		CacheBase<int, string> baseReference = cache;

		Assert.That(baseReference.Reap(1), Is.Zero);
		Assert.That(cache.ItemCount, Is.EqualTo(2));
		Assert.That(cache.CachedItems, Does.Contain(original));
		Assert.That(fetches, Is.EqualTo(1), "Reaping must neither discard a snapshot entry nor refetch the source.");
		Assert.That(cache[1], Is.EqualTo(fetchOnceOnly ? "one-1" : "one-2"));
		Assert.That(fetches, Is.EqualTo(fetchOnceOnly ? 1 : 2));
	}

	[Test]
	public void Reap_NegativeRequest_IsRejectedWithoutInitializingSnapshot() {
		var fetches = 0;
		using var cache = new BulkFetchActionCache<int, string>(() => {
			fetches++;
			return new Dictionary<int, string>();
		});
		CacheBase<int, string> baseReference = cache;

		Assert.That(() => baseReference.Reap(-1), Throws.InstanceOf<ArgumentException>());
		Assert.That(baseReference.Reap(0), Is.Zero);
		Assert.That(fetches, Is.Zero);
	}

	[TestCase(0L)]
	[TestCase(1L)]
	public void Reap_DisposedCache_IsRejected(long minimumRelease) {
		var cache = new BulkFetchActionCache<int, string>(() => new Dictionary<int, string>());
		CacheBase<int, string> baseReference = cache;
		cache.Dispose();

		Assert.That(() => baseReference.Reap(minimumRelease), Throws.TypeOf<ObjectDisposedException>());
	}
}
