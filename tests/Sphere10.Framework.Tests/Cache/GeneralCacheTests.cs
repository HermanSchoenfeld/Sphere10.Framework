// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
namespace Sphere10.Framework.Tests.Cache;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class GeneralCacheTests {

	[Test]
	public void PackedEventHandlerRemovedCorrectly() {
		var cache = new ActionCache<int, int>(x => x);
		var handlerInvocations = 0;
		cache.ItemFetched += Handler;
		var val = cache[0];
		Assert.That(handlerInvocations, Is.EqualTo(1));
		cache.ItemFetched -= Handler;
		val = cache[1];
		Assert.That(handlerInvocations, Is.EqualTo(1));  // shouldnt of have been called again

		void Handler(int arg1, int arg2) {
			handlerInvocations++;
		}
	}

	[Test]
	public void MaxCapacity_1() {
		var cache = new ActionCache<int, int>(x => x, sizeEstimator: x => 1, maxCapacity: 1, reapStrategy: CacheReapPolicy.LeastUsed);
		var val = cache[0];
		Assert.That(cache.ItemCount, Is.EqualTo(1));
		val = cache[1];
		Assert.That(cache.ItemCount, Is.EqualTo(1));
	}

	[Test]
	public void MaxCapacity_2() {
		var cache = new ActionCache<int, int>(x => x, sizeEstimator: x => 1, maxCapacity: 3, reapStrategy: CacheReapPolicy.LeastUsed);
		var val = cache[0];
		val = cache[1];
		val = cache[2];
		Assert.That(cache.ItemCount, Is.EqualTo(3));
		for (var i = 3; i < 1000; i++) {
			val = cache[i];
			Assert.That(cache.ItemCount, Is.EqualTo(3));
		}
	}


	[Test]
	public void ItemRemoved_1() {
		var removed = new List<int>();
		var cache = new ActionCache<int, int>(x => x, sizeEstimator: x => 1, maxCapacity: 1, reapStrategy: CacheReapPolicy.LeastUsed);
		cache.ItemRemoved += (i, item) => removed.Add(item.Value);

		var val = cache[0];
		Assert.That(removed.Count, Is.EqualTo(0));

		val = cache[1];
		Assert.That(removed.Count, Is.EqualTo(1));
		Assert.That(removed[0], Is.EqualTo(0));
	}

	[Test]
	public void ItemRemoved_2() {
		var removed = new List<int>();
		var cache = new ActionCache<int, int>(x => x, sizeEstimator: x => 1, maxCapacity: 1, reapStrategy: CacheReapPolicy.LeastUsed);
		cache.ItemRemoved += (i, item) => removed.Add(item.Value);

		for (var i = 0; i < 1000; i++) {
			var val = cache[i];
		}

		Assert.That(removed.Count, Is.EqualTo(999));
		removed.Sort();
		Assert.That(removed.ToArray(), Is.EqualTo(Enumerable.Range(0, 999).ToArray()));
	}

	[Test]
	public void BulkTest_Simple_1() {
		var cache = new BulkFetchActionCache<int, string>(
			() => new Dictionary<int, string>() {
				{ 1, "one" }, { 2, "two" }, { 3, "three" }
			});
		Assert.That(cache[1], Is.EqualTo("one"));
		Assert.That(cache.CachedItems.Select(x => x.Value).ToArray(), Is.EqualTo(new[] { "one", "two", "three" }));
	}

	[Test]
	public void ExpirationTest_Simple_1() {
		var val = "first";
		var cache = new ActionCache<int, string>(
			(x) => val,
			reapStrategy: CacheReapPolicy.LeastUsed,
			expirationStrategy: ExpirationPolicy.SinceFetchedTime,
			expirationDuration: TimeSpan.FromMilliseconds(100)
		);
		Assert.That(cache[1], Is.EqualTo("first"));
		val = "second";
		Assert.That(cache[1], Is.EqualTo("first"));
		Assert.That(cache[1], Is.EqualTo("first"));
		Thread.Sleep(111);
		Assert.That(cache[1], Is.EqualTo("second"));
		Assert.That(cache[1], Is.EqualTo("second"));
		Assert.That(cache[1], Is.EqualTo("second"));
	}

	[Test]
	public void SizeTest_Simple_1() {
		var cache = new ActionCache<int, string>(
			(x) => x.ToString(),
			reapStrategy: CacheReapPolicy.LeastUsed,
			expirationStrategy: ExpirationPolicy.SinceFetchedTime,
			expirationDuration: TimeSpan.FromMilliseconds(100),
			sizeEstimator: long.Parse,
			maxCapacity: 100
		);

		Assert.Throws<InvalidOperationException>(() => {
			var x = cache[101];
		});
		Assert.That(cache[98], Is.EqualTo("98"));
		Assert.That(cache.InternalStorage.Count, Is.EqualTo(1));
		Assert.That(cache[2], Is.EqualTo("2"));
		Assert.That(cache.InternalStorage.Count, Is.EqualTo(2));
		Assert.That(cache[1], Is.EqualTo("1"));
		Assert.That(cache.InternalStorage.Count, Is.EqualTo(2)); // should have purged first item
		Assert.That(cache.GetAllCachedValues().ToArray(), Is.EqualTo(new[] { "1", "2" }));
		Assert.That(cache[100], Is.EqualTo("100"));
		Assert.That(cache.InternalStorage.Count, Is.EqualTo(1)); // should have purged everything 
	}

	[Test]
	public void ContainsCachedItem_1() {
		var called = false;
		var cache = new ActionCache<int, string>(
			(x) => {
				if (x != 1)
					throw new Exception("test only allows key with value 1");
				if (called)
					throw new Exception("item 1 has been requested more than once");
				called = true;
				return "value";
			},
			reapStrategy: CacheReapPolicy.LeastUsed,
			expirationStrategy: ExpirationPolicy.SinceFetchedTime,
			expirationDuration: TimeSpan.FromMilliseconds(100)
		);
		Assert.That(cache.ContainsCachedItem(1), Is.False);
		var val = cache[1];
		Assert.That(cache.ContainsCachedItem(1), Is.True);
		Thread.Sleep(111);
		Assert.That(cache.ContainsCachedItem(1), Is.False);
	}


	[Test]
	public void TestEmptySize() {
		var cache = new ActionCache<int, string>(
			_ => string.Empty,
			s => s.Length,
			0
		);
		for (var i = 0; i < 100; i++) {
			var item = cache[i];
			Assert.That(cache.ItemCount, Is.EqualTo(i + 1));
			Assert.That(cache.CurrentSize, Is.EqualTo(0));
		}
	}

	[Test]
	public void TestOutOfSpace_1() {
		string[] items = { "", "1", "22", "333" };

		var cache = new ActionCache<int, string>(
			(x) => items[x],
			s => s.Length,
			CacheReapPolicy.Smallest,
			ExpirationPolicy.None,
			5
		);

		var x = cache[0]; // item ""
		var y = cache[1]; // item "1"
		var z = cache[2]; // item "22"
		cache.Get(0).CanPurge = false;
		cache.Get(1).CanPurge = false;
		cache.Get(2).CanPurge = false;
		string d = default;
		Assert.Throws<InvalidOperationException>(() => d = cache[3]);
	}


}

