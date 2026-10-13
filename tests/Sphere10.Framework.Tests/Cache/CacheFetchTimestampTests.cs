// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using NUnit.Framework;

namespace Sphere10.Framework.Tests.Cache;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CacheFetchTimestampTests {

	[TestCase(false)]
	[TestCase(true)]
	public void FetchedTimestampBeginsAfterFetchCompletes(bool uncachedNull) {
		var fetchCompletedOn = default(DateTime);
		using var cache = new ActionCache<int, string>(_ => {
			fetchCompletedOn = CompletionMarker();
			return uncachedNull ? null : "value";
		}, nullValuePolicy: uncachedNull ? NullValuePolicy.ReturnButDontCache : NullValuePolicy.CacheNormally);

		var item = cache.Get(1);

		Assert.That(item.Value, Is.EqualTo(uncachedNull ? null : "value"));
		Assert.That(item.FetchedOn, Is.GreaterThanOrEqualTo(fetchCompletedOn));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void LastAccessTimestampsFollowFetchedNotification(bool uncachedNull) {
		var notificationCompletedOn = default(DateTime);
		using var cache = new ActionCache<int, string>(_ => uncachedNull ? null : "value",
			nullValuePolicy: uncachedNull ? NullValuePolicy.ReturnButDontCache : NullValuePolicy.CacheNormally);
		cache.ItemFetched += (_, _) => notificationCompletedOn = CompletionMarker();

		var item = cache.Get(1);

		Assert.That(item.Value, Is.EqualTo(uncachedNull ? null : "value"));
		Assert.That(item.LastAccessedOn, Is.GreaterThanOrEqualTo(notificationCompletedOn));
		Assert.That(cache.LastAccessedOn, Is.GreaterThanOrEqualTo(notificationCompletedOn));
		Assert.That(item.FetchedOn, Is.LessThan(notificationCompletedOn), "Notification time must not restart expiration measured from fetch completion.");
	}

	private static DateTime CompletionMarker() {
		var startedOn = DateTime.Now;
		Assert.That(SpinWait.SpinUntil(() => DateTime.Now > startedOn, TimeSpan.FromSeconds(1)), Is.True,
			"The wall clock must advance within the bounded callback before timestamp ordering can be checked.");
		return DateTime.Now;
	}
}
