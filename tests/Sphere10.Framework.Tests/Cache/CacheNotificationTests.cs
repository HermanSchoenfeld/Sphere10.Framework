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
using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Tests.Cache;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CacheNotificationTests {
	[Test]
	public void EveryNotificationCallsItsHookBeforeItsEventOnce([Values] bool objectSpecialization) {
		var calls = new List<string>();
		using var cache = CreateTrackingCache(objectSpecialization, calls);
		cache.ItemFetching += _ => calls.Add("event fetching");
		cache.ItemFetched += (_, _) => {
			calls.Add("event fetched");
			Assert.That(cache.ContainsCachedItem("entry"), Is.True);
			Assert.That(cache.CurrentSize, Is.EqualTo(1));
		};
		var original = cache.Get("entry");
		Assert.That(cache.Get("entry"), Is.SameAs(original));
		cache.ItemRemoving += (key, item) => {
			calls.Add("event removing");
			Assert.That(cache.Get(key), Is.SameAs(original));
			Assert.That(item, Is.SameAs(original));
			Assert.That(item.Traits.HasFlag(CachedItemTraits.Purged), Is.False);
			Assert.That(cache.ItemCount, Is.EqualTo(1));
			Assert.That(cache.CurrentSize, Is.EqualTo(1));
		};
		cache.ItemRemoved += (key, item) => {
			calls.Add("event removed");
			Assert.That(cache.ContainsCachedItem(key), Is.False);
			Assert.That(item, Is.SameAs(original));
			Assert.That(item.Traits.HasFlag(CachedItemTraits.Purged), Is.True);
			Assert.That(cache.ItemCount, Is.Zero);
			Assert.That(cache.CurrentSize, Is.Zero);
		};

		cache.Remove("entry");
		cache.Remove("missing");
		Assert.That(calls, Is.EqualTo(new[] {
			"hook fetching", "event fetching", "fetch", "hook fetched", "event fetched",
			"hook removing", "event removing", "hook removed", "event removed"
		}));
	}

	[TestCase("Remove", false)]
	[TestCase("Remove", true)]
	[TestCase("Purge", false)]
	[TestCase("Purge", true)]
	[TestCase("Reap", false)]
	[TestCase("Reap", true)]
	[TestCase("Admission", false)]
	[TestCase("Admission", true)]
	public void RemovingEventFailurePreservesMembershipAndAllowsRetry(string operation, bool projected) {
		using var cache = new ActionCache<int, int>(_ => 1, value => value, CacheReapPolicy.Oldest, maxCapacity: 1);
		using var projection = cache.AsObjectCache();
		cache.Set(0, 1);
		var original = cache.Get(0);
		var failure = new IOException("Injected removing-event failure");
		var preparations = 0;
		var removals = 0;
		void Prepare() {
			if (++preparations == 1)
				throw failure;
		}
		if (projected)
			projection.ItemRemoving += (_, _) => Prepare();
		else
			cache.ItemRemoving += (_, _) => Prepare();
		cache.ItemRemoved += (_, _) => removals++;
		Action remove = operation switch {
			"Remove" => () => cache.Remove(0),
			"Purge" => cache.Purge,
			"Reap" => () => cache.Reap(1),
			_ => () => cache.Set(1, 1)
		};

		Assert.That(remove, Throws.Exception.SameAs(failure));
		Assert.That(cache.Get(0), Is.SameAs(original));
		Assert.That(original.Traits.HasFlag(CachedItemTraits.Purged), Is.False);
		Assert.That(cache.ContainsCachedItem(1), Is.False);
		Assert.That(cache.ItemCount, Is.EqualTo(1));
		Assert.That(cache.CurrentSize, Is.EqualTo(1));
		Assert.That(removals, Is.Zero);

		remove();
		Assert.That(preparations, Is.EqualTo(2));
		Assert.That(removals, Is.EqualTo(1));
		Assert.That(cache.ContainsCachedItem(0), Is.False);
		Assert.That(original.Traits.HasFlag(CachedItemTraits.Purged), Is.True);
		Assert.That(cache.ItemCount, Is.EqualTo(operation == "Admission" ? 1 : 0));
		Assert.That(cache.CurrentSize, Is.EqualTo(operation == "Admission" ? 1 : 0));
	}

	[Test]
	public void RemovingEventRunsInsideTheReentrantRemovalAndCapacityGuards() {
		using var cache = new ActionCache<int, int>(_ => 1, value => value, CacheReapPolicy.Oldest, maxCapacity: 1);
		cache.Set(0, 1);
		var original = cache.Get(0);
		var preparations = 0;
		cache.ItemRemoving += (key, item) => {
			Assert.That(++preparations, Is.EqualTo(1));
			cache.Remove(key);
			cache.Purge();
			Assert.That(cache.Reap(1), Is.Zero);
			Assert.That(cache.Get(key), Is.SameAs(item));
			Assert.That(() => cache.Set(key, 1), Throws.InvalidOperationException.With.Message.Contains("replace an entry"));
			Assert.That(() => cache.Set(1, 1), Throws.InvalidOperationException);
			Assert.That(cache.Dispose, Throws.InvalidOperationException);
			Assert.That(cache.ContainsCachedItem(key), Is.True);
			Assert.That(cache.ContainsCachedItem(1), Is.False);
			Assert.That(cache.ItemCount, Is.EqualTo(1));
			Assert.That(cache.CurrentSize, Is.EqualTo(1));
		};

		cache.Remove(0);
		Assert.That(preparations, Is.EqualTo(1));
		Assert.That(original.Traits.HasFlag(CachedItemTraits.Purged), Is.True);
		Assert.That(cache.ItemCount, Is.Zero);
		Assert.That(cache.CurrentSize, Is.Zero);
	}

	[Test]
	public void DisposalAttemptsEveryRemovingEventAndAggregatesFailures() {
		using var cache = new ActionCache<int, int>(key => key, _ => 1);
		cache.Set(0, 0);
		cache.Set(1, 1);
		var preparations = new List<int>();
		var removals = 0;
		cache.ItemRemoving += (key, _) => {
			preparations.Add(key);
			throw new IOException($"Removal {key} failed");
		};
		cache.ItemRemoved += (_, _) => removals++;

		Assert.That(cache.Dispose, Throws.TypeOf<AggregateException>().With.Property("InnerExceptions").Property("Count").EqualTo(2));
		Assert.That(preparations, Is.EquivalentTo(new[] { 0, 1 }));
		Assert.That(removals, Is.Zero);
		Assert.That(cache.ItemCount, Is.Zero);
		Assert.That(cache.CurrentSize, Is.Zero);
		Assert.That(cache.Dispose, Throws.Nothing);
	}

	[Test]
	public void ProjectionForwardsTheLiveRemovingViewAndDetachesWithoutDisposingSource() {
		using var source = new ActionCache<int, int>(key => key, value => 1);
		using var projection = source.AsObjectCache();
		var view = projection.Get(7);
		var sourceCalls = 0;
		var projectedCalls = 0;
		CachedItem<object> removed = null;
		source.ItemRemoving += (_, _) => sourceCalls++;
		projection.ItemRemoving += (key, item) => {
			projectedCalls++;
			Assert.That(key, Is.EqualTo(7));
			Assert.That(item, Is.SameAs(view));
			Assert.That(projection.Get(key), Is.SameAs(view));
			Assert.That(projection.CachedItems.Single(), Is.SameAs(view));
			Assert.That(item.Value, Is.EqualTo(17));
			Assert.That(item.Traits.HasFlag(CachedItemTraits.Purged), Is.False);
			Assert.That(source.CurrentSize, Is.EqualTo(1));
		};
		projection.ItemRemoved += (_, item) => removed = item;
		source.Set(7, 17);
		source.Remove(7);
		Assert.That(removed, Is.SameAs(view));
		Assert.That(view.Traits.HasFlag(CachedItemTraits.Purged), Is.True);
		Assert.That(projectedCalls, Is.EqualTo(1));

		projection.Dispose();
		source.Set(8, 8);
		source.Remove(8);
		Assert.That(projectedCalls, Is.EqualTo(1));
		Assert.That(sourceCalls, Is.EqualTo(2));
		Assert.That(source.Get(9).Value, Is.EqualTo(9));
	}

	[Test]
	public void RemovingAndRemovedKeepContravariantSubscriptionsIndependent() {
		using var cache = new ActionCache<string, string>(key => key);
		var calls = new List<string>();
		EventHandlerEx<object, object> broad = (_, _) => calls.Add("broad");
		EventHandlerEx<string, CachedItem<string>> narrow = (_, _) => calls.Add("narrow");
		cache.ItemRemoving += broad;
		cache.ItemRemoving += narrow;
		cache.ItemRemoved += broad;
		cache.Get("first");
		cache.Remove("first");
		Assert.That(calls, Is.EqualTo(new[] { "broad", "narrow", "broad" }));

		calls.Clear();
		cache.ItemRemoving -= broad;
		cache.Get("second");
		cache.Remove("second");
		Assert.That(calls, Is.EqualTo(new[] { "narrow", "broad" }));

		calls.Clear();
		cache.ItemRemoved -= broad;
		cache.Get("third");
		cache.Remove("third");
		Assert.That(calls, Is.EqualTo(new[] { "narrow" }));
	}

	private static CacheBase<object, object> CreateTrackingCache(bool objectSpecialization, List<string> calls)
		=> objectSpecialization ? new ObjectTrackingCache(calls) : new TypedTrackingCache(calls);

	private sealed class TypedTrackingCache : CacheBase<object, object> {
		private readonly List<string> _calls;

		public TypedTrackingCache(List<string> calls) {
			_calls = calls;
		}

		protected override object Fetch(object key) {
			_calls.Add("fetch");
			return key;
		}

		protected override long EstimateSize(object value) => 1;

		protected override bool CheckStaleness(object key, CachedItem<object> item) => false;

		protected override void OnItemFetching(object key) => _calls.Add("hook fetching");

		protected override void OnItemFetched(object key, object value) => _calls.Add("hook fetched");

		protected override void OnItemRemoving(object key, CachedItem<object> item) => _calls.Add("hook removing");

		protected override void OnItemRemoved(object key, CachedItem<object> item) => _calls.Add("hook removed");
	}

	private sealed class ObjectTrackingCache : CacheBase {
		private readonly List<string> _calls;

		public ObjectTrackingCache(List<string> calls)
			: base(EqualityComparer<object>.Default) {
			_calls = calls;
		}

		protected override object Fetch(object key) {
			_calls.Add("fetch");
			return key;
		}

		protected override long EstimateSize(object value) => 1;

		protected override bool CheckStaleness(object key, CachedItem item) => false;

		protected override void OnItemFetching(object key) => _calls.Add("hook fetching");

		protected override void OnItemFetched(object key, object value) => _calls.Add("hook fetched");

		protected override void OnItemRemoving(object key, CachedItem item) => _calls.Add("hook removing");

		protected override void OnItemRemoved(object key, CachedItem item) => _calls.Add("hook removed");
	}
}
