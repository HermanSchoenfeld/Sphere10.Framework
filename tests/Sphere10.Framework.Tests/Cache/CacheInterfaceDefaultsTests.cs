// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CacheProjectionTests {
	[Test]
	public void TypedContractsDoNotRequireTheObjectView() {
		Assert.That(typeof(ICache).IsAssignableFrom(typeof(ICache<string, int>)), Is.False);
		Assert.That(typeof(ICacheBase).IsAssignableFrom(typeof(ICache<string, int>)), Is.True);
		Assert.That(typeof(ICache<object, object>).IsAssignableFrom(typeof(ICache)), Is.True);
		Assert.That(typeof(CachedItem).IsAssignableFrom(typeof(CachedItem<int>)), Is.False);
		Assert.That(typeof(CachedItem<object>).IsAssignableFrom(typeof(CachedItem)), Is.True);
		using var cache = new ActionCache<object, object>(key => key);
		Assert.That(cache, Is.Not.InstanceOf<ICache>());
	}

	[Test]
	public void DirectTypedCacheImplementsOnlyTheTypedContract() {
		using var implementation = new DirectCache<string, int>(key => key.Length);
		ICache<string, int> cache = implementation;
		Assert.That(implementation, Is.Not.InstanceOf<ICache>());

		cache.Set("first", 11);
		Assert.That(cache.ContainsCachedItem("first"), Is.True);
		Assert.That(cache.Get("first").Value, Is.EqualTo(11));
		cache["second"] = 22;
		Assert.That(cache["second"], Is.EqualTo(22));
		cache.BulkLoad(new[] { new KeyValuePair<string, int>("third", 33) });
		Assert.That(cache.CachedItems.Select(item => item.Value), Is.EquivalentTo(new[] { 11, 22, 33 }));

		cache.Invalidate("first");
		Assert.That(cache.ContainsCachedItem("first"), Is.False);
		Assert.That(cache.Get("first").Value, Is.EqualTo(5));
		cache.Remove("second");
		Assert.That(cache.ContainsCachedItem("second"), Is.False);
		Assert.That(implementation.GetCalls, Is.EqualTo(2));
	}

	[Test]
	public void ObjectKeysRemainTypedWithoutCompatibilityDispatch() {
		using var implementation = new DirectCache<object, int>(_ => 17);
		ICache<object, int> cache = implementation;
		var key = new object();

		cache.Set(key, 12);
		Assert.That(cache.ContainsCachedItem(key), Is.True);
		Assert.That(cache.Get(key).Value, Is.EqualTo(12));
		cache[key] = 19;
		Assert.That(cache[key], Is.EqualTo(19));
		cache.BulkLoad(new[] { new KeyValuePair<object, int>("bulk", 23) });
		Assert.That(cache.CachedItems.Select(item => item.Value), Is.EquivalentTo(new[] { 19, 23 }));
		cache.Invalidate(key);
		Assert.That(cache.Get(key).Value, Is.EqualTo(17));
		cache.Remove(key);
		Assert.That(cache.ContainsCachedItem(key), Is.False);
		Assert.That(implementation.GetCalls, Is.EqualTo(2));
	}

	[Test]
	public void ExplicitObjectProjectionSharesOperationsWithTheTypedSource() {
		using var cache = new ActionCache<object, int>(_ => 31);
		using var projection = cache.AsObjectCache();
		ICache<object, int> typed = cache;
		ICache objects = projection;
		var key = new object();

		cache.Set(key, 7);
		Assert.That(cache.Get(key).Value, Is.EqualTo(7));
		Assert.That(typed.Get(key).Value, Is.EqualTo(7));
		Assert.That(objects.Get(key).Value, Is.EqualTo(7));
		objects[key] = 9;
		Assert.That(cache[key], Is.EqualTo(9));
		Assert.That(objects[key], Is.EqualTo(9));
		objects.BulkLoad(new[] { new KeyValuePair<object, object>("bulk", 11) });
		Assert.That(cache.CachedItems.Select(item => item.Value), Is.EquivalentTo(new[] { 9, 11 }));
		objects.Invalidate(key);
		Assert.That(typed.ContainsCachedItem(key), Is.False);
		Assert.That(cache.Get(key).Value, Is.EqualTo(31));
		objects.Remove(key);
		Assert.That(cache.ContainsCachedItem(key), Is.False);
	}

	[Test]
	public void ObjectProjectionAlsoAdaptsACustomTypedOnlyImplementation() {
		using var implementation = new DirectCache<string, int>(key => key.Length);
		using var projection = implementation.AsObjectCache();
		projection.Set("value", 17);
		Assert.That(projection.Get("value").Value, Is.EqualTo(17));
		Assert.That(implementation.Get("value").Value, Is.EqualTo(17));
		Assert.That(projection.ItemCount, Is.EqualTo(1));
		Assert.That(projection.TotalAccesses, Is.EqualTo(implementation.TotalAccesses));
		projection.Purge();
		Assert.That(implementation.ItemCount, Is.Zero);
	}

	[Test]
	public void ObjectSpecializationRetainsItsNongenericEntryAndSubclassHooks() {
		using var implementation = new ObjectCache();
		ICache cache = implementation;
		CachedItem original = implementation.Get("key");
		Assert.That(original.Value, Is.EqualTo(1));
		implementation.NeedsRefresh = true;
		var refreshed = cache.Get("key");
		Assert.That(refreshed, Is.InstanceOf<CachedItem>());
		Assert.That(refreshed, Is.Not.SameAs(original));
		Assert.That(refreshed.Value, Is.EqualTo(2));
		Assert.That(implementation.LastStaleCheck, Is.SameAs(original));
		Assert.That(implementation.LastRemoved, Is.SameAs(original));
		cache.Set("key", 19);
		Assert.That(implementation.Get("key").Value, Is.EqualTo(19));
		cache.Remove("key");
		Assert.That(implementation.LastRemoved, Is.SameAs(refreshed));
		Assert.That(cache.ItemCount, Is.Zero);
	}

	[Test]
	public void ProjectionReusesItsEntryViewAndPreservesTheTypedNullKeyGuard() {
		using var implementation = new DirectCache<string, int>(_ => 13);
		using var projection = implementation.AsObjectCache();
		var sourceItem = implementation.Get("key");
		var view = projection.Get("key");
		Assert.That(view, Is.Not.SameAs(sourceItem));
		Assert.That(projection.Get("key"), Is.SameAs(view));
		Assert.That(projection.CachedItems.Single(), Is.SameAs(view));
		Assert.That(view.AccessedCount, Is.EqualTo(sourceItem.AccessedCount));
		Assert.That(view.LastAccessedOn, Is.EqualTo(sourceItem.LastAccessedOn));
		Assert.That(() => implementation.Get(null), Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("key"));
	}

	[TestCase("Get")]
	[TestCase("SetKey")]
	[TestCase("SetValue")]
	[TestCase("Contains")]
	[TestCase("Invalidate")]
	[TestCase("Remove")]
	[TestCase("BulkLoad")]
	public void ObjectProjectionRejectsIncompatibleInputsWithoutChangingSourceMembership(string operation) {
		using var source = new ActionCache<int, int>(key => key);
		using var projection = source.AsObjectCache();
		source.Set(1, 17);
		Action action = operation switch {
			"Get" => () => projection.Get("1"),
			"SetKey" => () => projection.Set("1", 2),
			"SetValue" => () => projection.Set(1, "2"),
			"Contains" => () => projection.ContainsCachedItem("1"),
			"Invalidate" => () => projection.Invalidate("1"),
			"Remove" => () => projection.Remove("1"),
			_ => () => projection.BulkLoad(new[] { new KeyValuePair<object, object>("1", 2) })
		};
		Assert.That(action, Throws.TypeOf<InvalidCastException>());
		Assert.That(source.ItemCount, Is.EqualTo(1));
		Assert.That(source.Get(1).Value, Is.EqualTo(17));
	}

	[Test]
	public void ArbitraryProjectionConvertsReadsLookupsAndWritesInTheirOwnDirections() {
		using var source = new ActionCache<int, int>(key => key * 10);
		using var projection = CreateProjection(source);
		Assert.That(projection.Get("key:3").Value, Is.EqualTo(3m));
		Assert.That(source.Get(3).Value, Is.EqualTo(30));
		projection.Set("key:1", 2.5m);
		Assert.That(source.Get(1).Value, Is.EqualTo(25));
		Assert.That(projection.ContainsCachedItem("key:1"), Is.True);
		projection["key:2"] = 4.5m;
		Assert.That(projection["key:2"], Is.EqualTo(4.5m));
		projection.BulkLoad(new[] { new KeyValuePair<string, decimal>("key:4", 6.5m) });
		Assert.That(source.Get(4).Value, Is.EqualTo(65));
		Assert.That(projection.CachedItems.Select(item => item.Value), Is.EquivalentTo(new[] { 3m, 2.5m, 4.5m, 6.5m }));
		projection.Invalidate("key:1");
		Assert.That(source.ContainsCachedItem(1), Is.False);
		Assert.That(projection.Get("key:1").Value, Is.EqualTo(1m));
		projection.Remove("key:2");
		Assert.That(source.ContainsCachedItem(2), Is.False);
		Assert.That(projection.ItemCount, Is.EqualTo(source.ItemCount));
		Assert.That(projection.CurrentSize, Is.EqualTo(source.CurrentSize));
	}

	[Test]
	public void ArbitraryProjectionConvertsEventsAndReturnsTheExistingRemovalView() {
		using var source = new ActionCache<int, int>(key => key * 10);
		using var projection = CreateProjection(source);
		var fetching = new List<string>();
		var fetched = new List<(string, decimal)>();
		CachedItem<decimal> removedItem = null;
		string removedKey = null;
		projection.ItemFetching += key => fetching.Add(key);
		projection.ItemFetched += (key, value) => fetched.Add((key, value));
		projection.ItemRemoved += (key, item) => { removedKey = key; removedItem = item; };
		var view = projection.Get("key:7");
		projection.Remove("key:7");
		Assert.That(fetching, Is.EqualTo(new[] { "key:7" }));
		Assert.That(fetched, Is.EqualTo(new[] { ("key:7", 7m) }));
		Assert.That(removedKey, Is.EqualTo("key:7"));
		Assert.That(removedItem, Is.SameAs(view));
		Assert.That(removedItem.Value, Is.EqualTo(7m));
	}

	[Test]
	public void ProjectionSupportsStructKeysAndReferenceValuesOverStringKeysAndIntegers() {
		using var source = new ActionCache<string, int>(key => int.Parse(key, CultureInfo.InvariantCulture) * 10);
		using var projection = source.AsProjection<string, int, CacheKey, ReferenceValue>(
			key => new CacheKey(int.Parse(key, CultureInfo.InvariantCulture)), key => key.Value.ToString(CultureInfo.InvariantCulture),
			value => new ReferenceValue(value), value => value.Number);
		var fetching = new List<CacheKey>();
		var fetched = new List<(CacheKey Key, int Number)>();
		var removed = new List<(CacheKey Key, CachedItem<ReferenceValue> Item)>();
		projection.ItemFetching += key => fetching.Add(key);
		projection.ItemFetched += (key, value) => fetched.Add((key, value.Number));
		projection.ItemRemoved += (key, item) => removed.Add((key, item));
		var firstKey = new CacheKey(1);
		var firstView = projection.Get(firstKey);
		Assert.That(firstView.Value.Number, Is.EqualTo(10));
		projection.Set(firstKey, new ReferenceValue(25));
		Assert.That(source.Get("1").Value, Is.EqualTo(25));
		source.Set("1", 26);
		Assert.That(firstView.Value.Number, Is.EqualTo(26));
		Assert.That(projection.Get(firstKey), Is.SameAs(firstView));
		projection[new CacheKey(2)] = new ReferenceValue(45);
		Assert.That(projection[new CacheKey(2)].Number, Is.EqualTo(45));
		Assert.That(source.Get("2").Value, Is.EqualTo(45));
		projection.BulkLoad(new[] { new KeyValuePair<CacheKey, ReferenceValue>(new CacheKey(3), new ReferenceValue(65)) });
		Assert.That(source.Get("3").Value, Is.EqualTo(65));
		Assert.That(projection.CachedItems.Select(item => item.Value.Number), Is.EquivalentTo(new[] { 26, 45, 65 }));
		projection.Invalidate(firstKey);
		Assert.That(projection.ContainsCachedItem(firstKey), Is.False);
		Assert.That(projection.Get(firstKey).Value.Number, Is.EqualTo(10));
		var secondView = projection.Get(new CacheKey(2));
		projection.Remove(new CacheKey(2));
		Assert.That(source.ContainsCachedItem("2"), Is.False);
		Assert.That(fetching, Is.EqualTo(new[] { firstKey, firstKey }));
		Assert.That(fetched, Is.EqualTo(new[] { (firstKey, 10), (firstKey, 10) }));
		Assert.That(removed.Select(item => item.Key), Is.EqualTo(new[] { firstKey, new CacheKey(2) }));
		Assert.That(removed[0].Item, Is.SameAs(firstView));
		Assert.That(removed[1].Item, Is.SameAs(secondView));
	}

	[Test]
	public void ProjectionSupportsStringKeysAndIntegersOverStructKeysAndReferenceValues() {
		using var source = new ActionCache<CacheKey, ReferenceValue>(key => new ReferenceValue(key.Value * 10));
		using var projection = source.AsProjection<CacheKey, ReferenceValue, string, int>(
			key => key.Value.ToString(CultureInfo.InvariantCulture), key => new CacheKey(int.Parse(key, CultureInfo.InvariantCulture)),
			value => value.Number, value => new ReferenceValue(value));
		var fetching = new List<string>();
		var fetched = new List<(string Key, int Number)>();
		var removed = new List<(string Key, CachedItem<int> Item)>();
		projection.ItemFetching += key => fetching.Add(key);
		projection.ItemFetched += (key, value) => fetched.Add((key, value));
		projection.ItemRemoved += (key, item) => removed.Add((key, item));
		var firstView = projection.Get("1");
		Assert.That(firstView.Value, Is.EqualTo(10));
		projection.Set("1", 25);
		Assert.That(source.Get(new CacheKey(1)).Value.Number, Is.EqualTo(25));
		source.Get(new CacheKey(1)).Value.Number = 26;
		Assert.That(firstView.Value, Is.EqualTo(26));
		Assert.That(projection.Get("1"), Is.SameAs(firstView));
		projection["2"] = 45;
		Assert.That(projection["2"], Is.EqualTo(45));
		Assert.That(source.Get(new CacheKey(2)).Value.Number, Is.EqualTo(45));
		projection.BulkLoad(new[] { new KeyValuePair<string, int>("3", 65) });
		Assert.That(source.Get(new CacheKey(3)).Value.Number, Is.EqualTo(65));
		Assert.That(projection.CachedItems.Select(item => item.Value), Is.EquivalentTo(new[] { 26, 45, 65 }));
		projection.Invalidate("1");
		Assert.That(projection.ContainsCachedItem("1"), Is.False);
		Assert.That(projection.Get("1").Value, Is.EqualTo(10));
		var secondView = projection.Get("2");
		projection.Remove("2");
		Assert.That(source.ContainsCachedItem(new CacheKey(2)), Is.False);
		Assert.That(fetching, Is.EqualTo(new[] { "1", "1" }));
		Assert.That(fetched, Is.EqualTo(new[] { ("1", 10), ("1", 10) }));
		Assert.That(removed.Select(item => item.Key), Is.EqualTo(new[] { "1", "2" }));
		Assert.That(removed[0].Item, Is.SameAs(firstView));
		Assert.That(removed[1].Item, Is.SameAs(secondView));
	}

	[Test]
	public void DisposingProjectionDetachesEventsAndBorrowsItsSource() {
		using var source = new ActionCache<int, int>(key => key);
		var projection = source.AsObjectCache();
		var sourceCalls = 0;
		var projectedCalls = 0;
		source.ItemFetched += (_, _) => sourceCalls++;
		projection.ItemFetched += (_, _) => projectedCalls++;
		projection.Get(1);
		projection.Dispose();
		projection.Dispose();
		Assert.That(source.Get(2).Value, Is.EqualTo(2));
		Assert.That(sourceCalls, Is.EqualTo(2));
		Assert.That(projectedCalls, Is.EqualTo(1));
		Assert.That(() => projection.Get(3), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(source.ContainsCachedItem(3), Is.False);
	}

	[Test]
	public void DisposingProjectionAttemptsEverySubscribedRemovalAndAggregatesFailures() {
		using var source = new DirectCache<string, int>(key => key.Length, throwAfterEventRemoval: true);
		using var projection = source.AsObjectCache();
		var calls = 0;
		projection.ItemFetching += _ => calls++;
		projection.ItemFetched += (_, _) => calls++;
		projection.ItemRemoved += (_, _) => calls++;
		AggregateException failure = null;
		try {
			projection.Dispose();
		} catch (AggregateException error) {
			failure = error;
		}
		Assert.That(failure, Is.Not.Null);
		Assert.That(failure.InnerExceptions.Select(error => error.Message), Is.EquivalentTo(new[] { "Removal 0", "Removal 1", "Removal 2" }));
		Assert.That(source.EventRemovalAttempts, Is.EqualTo(new[] { 1, 1, 1 }));
		Assert.That(source.Get("after").Value, Is.EqualTo(5));
		source.Remove("after");
		Assert.That(calls, Is.Zero);
		Assert.That(projection.Dispose, Throws.Nothing);
		Assert.That(source.EventRemovalAttempts, Is.EqualTo(new[] { 1, 1, 1 }));
	}

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	public void DisposingProjectionDoesNotRemoveForwardersThatWereNeverSubscribed(int eventIndex) {
		using var source = new DirectCache<string, int>(key => key.Length, throwAfterEventRemoval: true);
		using var projection = source.AsObjectCache();
		switch (eventIndex) {
			case 0: projection.ItemFetching += _ => { }; break;
			case 1: projection.ItemFetched += (_, _) => { }; break;
			default: projection.ItemRemoved += (_, _) => { }; break;
		}
		Assert.That(projection.Dispose, Throws.TypeOf<AggregateException>());
		var attempts = new int[3];
		attempts[eventIndex] = 1;
		Assert.That(source.EventRemovalAttempts, Is.EqualTo(attempts));
		Assert.That(source.Get("alive").Value, Is.EqualTo(5));
		Assert.That(projection.Dispose, Throws.Nothing);
		Assert.That(source.EventRemovalAttempts, Is.EqualTo(attempts));
	}

	[TestCase("Fetching", false)]
	[TestCase("Fetching", true)]
	[TestCase("Fetched", false)]
	[TestCase("Fetched", true)]
	public void DisposingProjectionDuringFetchDoesNotReturnThroughTheDisposedView(string phase, bool indexer) {
		using var source = new ActionCache<int, int>(key => key);
		using var projection = source.AsObjectCache();
		projection.ItemFetching += _ => {
			if (phase == "Fetching")
				projection.Dispose();
		};
		projection.ItemFetched += (_, _) => {
			if (phase == "Fetched")
				projection.Dispose();
		};
		Assert.That(() => {
			if (indexer)
				_ = projection[1];
			else
				_ = projection.Get(1);
		}, Throws.TypeOf<ObjectDisposedException>());
		Assert.That(source.ContainsCachedItem(1), Is.True);
		Assert.That(source.Get(2).Value, Is.EqualTo(2));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void DisposingProjectionDuringKeyConversionDoesNotReturnThroughTheDisposedView(bool indexer) {
		using var source = new ActionCache<int, int>(key => key);
		ProjectedCache<int, int, int, int> projection = null;
		projection = new ProjectedCache<int, int, int, int>(source, key => key, key => { projection.Dispose(); return key; }, value => value, value => value);
		using (projection) {
			Assert.That(() => {
				if (indexer)
					_ = projection[1];
				else
					_ = projection.Get(1);
			}, Throws.TypeOf<ObjectDisposedException>());
			Assert.That(source.Get(2).Value, Is.EqualTo(2));
		}
	}

	[Test]
	public void DisposingProjectionDuringValueConversionDoesNotReturnAnIndexerValue() {
		using var source = new ActionCache<int, int>(key => key);
		ProjectedCache<int, int, int, int> projection = null;
		projection = new ProjectedCache<int, int, int, int>(source, key => key, key => key, value => { projection.Dispose(); return value; }, value => value);
		using (projection) {
			Assert.That(() => _ = projection[1], Throws.TypeOf<ObjectDisposedException>());
			Assert.That(source.Get(1).Value, Is.EqualTo(1));
		}
	}

	[Test]
	public void ObjectProjectionRejectsAMissingSource() {
		Assert.That(() => new ObjectCacheProjection<int, int>(null), Throws.TypeOf<ArgumentNullException>());
	}

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	[TestCase(3)]
	public void ArbitraryProjectionRejectsMissingConversionFunctions(int missing) {
		using var source = new ActionCache<int, int>(key => key);
		Func<int, string> keyProjection = missing == 0 ? null : key => key.ToString(CultureInfo.InvariantCulture);
		Func<string, int> inverseKeyProjection = missing == 1 ? null : key => int.Parse(key, CultureInfo.InvariantCulture);
		Func<int, decimal> valueProjection = missing == 2 ? null : value => value;
		Func<decimal, int> inverseValueProjection = missing == 3 ? null : value => checked((int)value);
		Assert.That(() => new ProjectedCache<int, int, string, decimal>(source, keyProjection, inverseKeyProjection, valueProjection, inverseValueProjection),
			Throws.TypeOf<ArgumentNullException>());
	}

	private static ProjectedCache<int, int, string, decimal> CreateProjection(ICache<int, int> source)
		=> source.AsProjection<int, int, string, decimal>(key => $"key:{key}", key => int.Parse(key.Substring(4), CultureInfo.InvariantCulture), value => value / 10m, value => checked((int)(value * 10)));

	private readonly record struct CacheKey(int Value);

	private sealed class ReferenceValue {
		public ReferenceValue(int number) => Number = number;

		public int Number { get; set; }
	}

	private sealed class ObjectCache : CacheBase {
		private int _fetchCount;

		public ObjectCache()
			: base(EqualityComparer<object>.Default, staleValuePolicy: StaleValuePolicy.CheckStaleOnDemand) {
		}

		public bool NeedsRefresh { get; set; }

		public CachedItem LastStaleCheck { get; private set; }

		public CachedItem LastRemoved { get; private set; }

		protected override object Fetch(object key) => ++_fetchCount;

		protected override long EstimateSize(object value) => 1;

		protected override bool CheckStaleness(object key, CachedItem item) {
			LastStaleCheck = item;
			var result = NeedsRefresh;
			NeedsRefresh = false;
			return result;
		}

		protected override void OnItemRemoved(object key, CachedItem item) => LastRemoved = item;
	}

	private sealed class DirectCache<TKey, TValue> : SynchronizedObject, ICache<TKey, TValue>, IDisposable {
		public event EventHandlerEx<TKey> ItemFetching {
			add => Internal.ItemFetching += value;
			remove {
				Internal.ItemFetching -= value;
				RecordEventRemoval(0);
			}
		}

		public event EventHandlerEx<TKey, TValue> ItemFetched {
			add => Internal.ItemFetched += value;
			remove {
				Internal.ItemFetched -= value;
				RecordEventRemoval(1);
			}
		}

		public event EventHandlerEx<TKey, CachedItem<TValue>> ItemRemoved {
			add => Internal.ItemRemoved += value;
			remove {
				Internal.ItemRemoved -= value;
				RecordEventRemoval(2);
			}
		}

		private readonly ActionCache<TKey, TValue> _internal;
		private readonly bool _throwAfterEventRemoval;
		private readonly int[] _eventRemovalAttempts = new int[3];

		public DirectCache(Func<TKey, TValue> fetch, bool throwAfterEventRemoval = false) {
			_internal = new ActionCache<TKey, TValue>(fetch);
			_throwAfterEventRemoval = throwAfterEventRemoval;
		}

		public int GetCalls { get; private set; }

		public int[] EventRemovalAttempts => (int[])_eventRemovalAttempts.Clone();

		public int ItemCount => Internal.ItemCount;
		public long CurrentSize => Internal.CurrentSize;
		public long MaxCapacity => Internal.MaxCapacity;
		public double ReapLevel => Internal.ReapLevel;
		public DateTime LastUpdateOn => Internal.LastUpdateOn;
		public DateTime LastAccessedOn => Internal.LastAccessedOn;
		public long TotalAccesses => Internal.TotalAccesses;
		public TimeSpan ExpirationDuration => Internal.ExpirationDuration;
		public ExpirationPolicy ExpirationPolicy => Internal.ExpirationPolicy;
		public CacheReapPolicy ReapPolicy => Internal.ReapPolicy;
		public NullValuePolicy NullValuePolicy => Internal.NullValuePolicy;
		public TValue this[TKey index] { get => Internal[index]; set => Internal[index] = value; }
		public IEnumerable<CachedItem<TValue>> CachedItems => Internal.CachedItems;

		private ActionCache<TKey, TValue> Internal => _internal;

		public void Dispose() {
			Internal.Dispose();
			ThreadLock.Dispose();
		}

		public bool ContainsCachedItem(TKey key) => Internal.ContainsCachedItem(key);

		public CachedItem<TValue> Get(TKey key) {
			GetCalls++;
			return Internal.Get(key);
		}

		public void Set(TKey key, TValue value) => Internal.Set(key, value);

		public void BulkLoad(IEnumerable<KeyValuePair<TKey, TValue>> bulkLoadedValues) => Internal.BulkLoad(bulkLoadedValues);

		public void Invalidate(TKey key) => Internal.Invalidate(key);

		public void Remove(TKey key) => Internal.Remove(key);

		public long Reap(long minimumRelease) => Internal.Reap(minimumRelease);

		public void Purge() => Internal.Purge();

		private void RecordEventRemoval(int eventIndex) {
			_eventRemovalAttempts[eventIndex]++;
			if (_throwAfterEventRemoval)
				throw new InvalidOperationException($"Removal {eventIndex}");
		}
	}
}
