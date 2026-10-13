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
public class CacheTypedEventTests {
	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	public void ObjectEventsPreserveDuplicateMulticastAndComponentRemoval(int eventIndex) {
		using var cache = new ActionCache<int, int>(key => key);
		using var projection = cache.AsObjectCache();
		var calls = new List<string>();
		var first = Handler(eventIndex, () => calls.Add("first"));
		var second = Handler(eventIndex, () => calls.Add("second"));
		var combined = Delegate.Combine(first, second);
		Change(projection, eventIndex, true, null);
		Change(projection, eventIndex, false, first);
		Change(projection, eventIndex, true, combined);
		Change(projection, eventIndex, true, combined);
		Change(projection, eventIndex, false, second);
		Trigger(cache, eventIndex, 1);
		Assert.That(calls, Is.EqualTo(new[] { "first", "second", "first" }));

		calls.Clear();
		Change(projection, eventIndex, false, combined);
		Trigger(cache, eventIndex, 2);
		Assert.That(calls, Is.EqualTo(new[] { "first" }));

		calls.Clear();
		Change(projection, eventIndex, false, first);
		Change(projection, eventIndex, false, first);
		Change(projection, eventIndex, false, null);
		Trigger(cache, eventIndex, 3);
		Assert.That(calls, Is.Empty);
	}

	[Test]
	public void ProjectionMulticastRemovalRequiresTheLastContiguousSubsequence() {
		using var cache = new ActionCache<int, int>(key => key);
		using var projection = cache.AsObjectCache();
		var calls = new List<string>();
		EventHandlerEx<object, object> first = (_, _) => calls.Add("first");
		EventHandlerEx<object, object> second = (_, _) => calls.Add("second");
		EventHandlerEx<object, object> middle = (_, _) => calls.Add("middle");
		projection.ItemFetched += first;
		projection.ItemFetched += middle;
		projection.ItemFetched += second;
		projection.ItemFetched -= first + second;
		cache.Get(1);
		Assert.That(calls, Is.EqualTo(new[] { "first", "middle", "second" }));

		calls.Clear();
		projection.ItemFetched += first + second;
		projection.ItemFetched -= first + second;
		cache.Get(2);
		Assert.That(calls, Is.EqualTo(new[] { "first", "middle", "second" }));
	}

	[Test]
	public void ContravariantTypedHandlersAndProjectionSubscriptionsStayIndependent() {
		using var cache = new ActionCache<string, string>(key => key);
		using var projection = cache.AsObjectCache();
		var calls = new List<string>();
		var projectedCalls = new List<string>();
		EventHandlerEx<object, object> broad = (_, _) => calls.Add("broad");
		EventHandlerEx<string, string> narrow = (_, _) => calls.Add("narrow");
		EventHandlerEx<object, object> projectedHandler = (_, _) => projectedCalls.Add("projected");
		cache.ItemFetched += broad;
		cache.ItemFetched += narrow;
		projection.ItemFetched += projectedHandler;
		cache.Get("first");
		Assert.That(calls, Is.EqualTo(new[] { "broad", "narrow" }));
		Assert.That(projectedCalls, Is.EqualTo(new[] { "projected" }));

		calls.Clear();
		cache.ItemFetched -= broad;
		cache.Get("second");
		Assert.That(calls, Is.EqualTo(new[] { "narrow" }));
		Assert.That(projectedCalls, Is.EqualTo(new[] { "projected", "projected" }));
		calls.Clear();
		projection.ItemFetched -= projectedHandler;
		cache.Get("third");
		Assert.That(calls, Is.EqualTo(new[] { "narrow" }));
		Assert.That(projectedCalls, Has.Count.EqualTo(2));
	}

	[Test]
	public void ObjectSpecializationSharesItsTypedAndObjectEventSlot() {
		using var cache = new ObjectCache();
		ICache<object, object> typed = cache;
		ICache objects = cache;
		var calls = 0;
		EventHandlerEx<object, object> handler = (_, _) => calls++;
		typed.ItemFetched += handler;
		objects.ItemFetched -= handler;
		cache.Get(1);
		Assert.That(calls, Is.Zero);
		objects.ItemFetched += handler;
		typed.ItemFetched -= handler;
		cache.Get(2);
		Assert.That(calls, Is.Zero);
	}

	[Test]
	public void ExplicitObjectViewKeepsEntryValueAndPinningLive() {
		using var cache = new ActionCache<int, int>(key => key, value => 1, CacheReapPolicy.Oldest, maxCapacity: 2);
		using var projection = cache.AsObjectCache();
		var item = cache.Get(1);
		var objectItem = projection.Get(1);
		Assert.That(objectItem, Is.Not.SameAs(item));
		Assert.That(projection.Get(1), Is.SameAs(objectItem));
		Assert.That(objectItem.Value, Is.EqualTo(1));
		objectItem.CanPurge = false;
		projection.Set(1, 17);
		Assert.That(cache.Get(1), Is.SameAs(item));
		Assert.That(item.Value, Is.EqualTo(17));
		Assert.That(objectItem.Value, Is.EqualTo(17));
		Assert.That(objectItem.FetchedOn, Is.EqualTo(item.FetchedOn));
		Assert.That(objectItem.AccessedCount, Is.EqualTo(item.AccessedCount));
		Assert.That(objectItem.LastAccessedOn, Is.EqualTo(item.LastAccessedOn));
		Assert.That(objectItem.Size, Is.EqualTo(item.Size));
		Assert.That(item.CanPurge, Is.False);
		cache.Set(2, 2);
		cache.Set(3, 3);
		Assert.That(cache.ContainsCachedItem(1), Is.True);
		Assert.That(cache.CachedItems.Any(candidate => ReferenceEquals(candidate, item)), Is.True);
		Assert.That(projection.CachedItems.Single(candidate => (int)candidate.Value == 17), Is.SameAs(objectItem));
		cache.Invalidate(1);
		Assert.That(objectItem.Traits, Is.EqualTo(item.Traits));
		Assert.That(objectItem.Traits.HasFlag(CachedItemTraits.Invalidated), Is.True);
	}

	[Test]
	public void ProjectedEntryDisposalUpdatesTheTypedDisposableValue() {
		using var cache = new ActionCache<int, DisposableStruct>(_ => new DisposableStruct());
		using var projection = cache.AsObjectCache();
		var sourceItem = cache.Get(1);
		var projectedItem = projection.Get(1);
		projectedItem.Dispose();
		Assert.That(sourceItem.Value.Disposed, Is.True);
		Assert.That(((DisposableStruct)projectedItem.Value).Disposed, Is.True);
	}

	[Test]
	public void DisposalBorrowsValuesAndExplicitEntryDisposalUpdatesValueTypes() {
		var value = new DisposableValue();
		using (var cache = new ActionCache<int, DisposableValue>(_ => value)) {
			cache.Get(1);
			cache.Purge();
			Assert.That(value.Disposals, Is.Zero);
		}
		Assert.That(value.Disposals, Is.Zero);
		var entry = new CachedItem<DisposableStruct> { Value = new DisposableStruct() };
		entry.Dispose();
		Assert.That(entry.Value.Disposed, Is.True);
	}

	[TestCase("Fetching")]
	[TestCase("Fetch")]
	[TestCase("Fetched")]
	public void FetchCallbacksCannotAdmitIntoADisposedCache(string phase) {
		ActionCache<int, int> cache = null;
		cache = new ActionCache<int, int>(key => {
			if (phase == "Fetch")
				cache.Dispose();
			return key;
		}, value => 1, CacheReapPolicy.Oldest, maxCapacity: 2);
		using (cache) {
			cache.ItemFetching += _ => {
				if (phase == "Fetching")
					cache.Dispose();
			};
			cache.ItemFetched += (_, _) => {
				if (phase == "Fetched")
					cache.Dispose();
			};
			Assert.That(() => cache.Get(1), Throws.TypeOf<ObjectDisposedException>());
			Assert.That(cache.ItemCount, Is.Zero);
			Assert.That(cache.CurrentSize, Is.Zero);
		}
	}

	[Test]
	public void ReturnButDontCacheNullsDoNotRetainInvalidEntries() {
		var fetches = 0;
		using var cache = new ActionCache<int, string>(_ => { fetches++; return null; }, nullValuePolicy: NullValuePolicy.ReturnButDontCache);
		Assert.That(cache.Get(1).Value, Is.Null);
		Assert.That(cache.Get(1).Value, Is.Null);
		Assert.That(cache.ItemCount, Is.Zero);
		Assert.That(cache.ContainsCachedItem(1), Is.False);
		Assert.That(fetches, Is.EqualTo(2));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ReadNullPolicyAlsoAppliesToPrefetchedEntries(bool bulk) {
		using var throwing = new ActionCache<int, string>(_ => "fetched", nullValuePolicy: NullValuePolicy.Throw);
		using var uncached = new ActionCache<int, string>(_ => "fetched", nullValuePolicy: NullValuePolicy.ReturnButDontCache);
		if (bulk) {
			throwing.BulkLoad(new[] { new KeyValuePair<int, string>(1, null) });
			uncached.BulkLoad(new[] { new KeyValuePair<int, string>(1, null) });
		} else {
			throwing.Set(1, null);
			uncached.Set(1, null);
		}
		Assert.That(() => throwing.Get(1), Throws.InvalidOperationException);
		Assert.That(uncached.Get(1).Value, Is.Null);
		Assert.That(uncached.ContainsCachedItem(1), Is.False);
		Assert.That(uncached.ItemCount, Is.Zero);
	}

	private static Delegate Handler(int eventIndex, Action action)
		=> eventIndex switch {
			0 => new EventHandlerEx<object>(_ => action()),
			1 => new EventHandlerEx<object, object>((_, _) => action()),
			_ => new EventHandlerEx<object, CachedItem<object>>((_, _) => action())
		};

	private static void Change(ICache cache, int eventIndex, bool add, Delegate handler) {
		switch (eventIndex) {
			case 0:
				if (add) cache.ItemFetching += (EventHandlerEx<object>)handler;
				else cache.ItemFetching -= (EventHandlerEx<object>)handler;
				break;
			case 1:
				if (add) cache.ItemFetched += (EventHandlerEx<object, object>)handler;
				else cache.ItemFetched -= (EventHandlerEx<object, object>)handler;
				break;
			default:
				if (add) cache.ItemRemoved += (EventHandlerEx<object, CachedItem<object>>)handler;
				else cache.ItemRemoved -= (EventHandlerEx<object, CachedItem<object>>)handler;
				break;
		}
	}

	private static void Trigger(ActionCache<int, int> cache, int eventIndex, int key) {
		cache.Get(key);
		if (eventIndex == 2)
			cache.Remove(key);
	}

	private sealed class DisposableValue : IDisposable {
		public int Disposals { get; private set; }

		public void Dispose() => Disposals++;
	}

	private struct DisposableStruct : IDisposable {
		public bool Disposed { get; private set; }

		public void Dispose() => Disposed = true;
	}

	private sealed class ObjectCache : CacheBase {
		public ObjectCache()
			: base(EqualityComparer<object>.Default) {
		}

		protected override object Fetch(object key) => key;

		protected override long EstimateSize(object value) => 0;

		protected override bool CheckStaleness(object key, CachedItem item) => false;
	}
}
