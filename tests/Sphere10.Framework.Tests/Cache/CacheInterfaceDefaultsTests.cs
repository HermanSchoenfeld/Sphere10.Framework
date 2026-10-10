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

namespace Sphere10.Framework.UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CacheInterfaceDefaultsTests {
	[Test]
	public void DirectTypedDefaultsProjectToUntypedCache() {
		using var implementation = new DirectCache<string, int>(key => key.Length);
		var cache = (ICache<string, int>)implementation;

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
	public void ObjectKeyDefaultsUseExplicitUntypedSlotWithoutRecursion() {
		using var implementation = new DirectCache<object, int>(_ => 17);
		var cache = (ICache<object, int>)implementation;
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
	public void ExistingObjectKeyClassRetainsTypedAndUntypedApis() {
		using var cache = new ActionCache<object, int>(_ => 31);
		var typed = (ICache<object, int>)cache;
		var untyped = (ICache)cache;
		var key = new object();

		cache.Set(key, 7);
		Assert.That(cache.Get(key).Value, Is.EqualTo(7));
		Assert.That(typed.Get(key).Value, Is.EqualTo(7));
		Assert.That(untyped.Get(key).Value, Is.EqualTo(7));
		typed[key] = 9;
		Assert.That(cache[key], Is.EqualTo(9));
		Assert.That(untyped[key], Is.EqualTo(9));
		typed.BulkLoad(new[] { new KeyValuePair<object, int>("bulk", 11) });
		Assert.That(cache.CachedItems.Select(item => item.Value), Is.EquivalentTo(new[] { 9, 11 }));
		typed.Invalidate(key);
		Assert.That(typed.ContainsCachedItem(key), Is.False);
		Assert.That(cache.Get(key).Value, Is.EqualTo(31));
		untyped.Remove(key);
		Assert.That(cache.ContainsCachedItem(key), Is.False);
	}

	[Test]
	public void DefaultGetReturnsSameTypedEntryAndPreservesNullKeyGuard() {
		using var implementation = new DirectCache<string, int>(_ => 13);
		var typed = (ICache<string, int>)implementation;
		var untyped = (ICache)implementation;

		Assert.That(typed.Get("key"), Is.SameAs(untyped.Get("key")));
		Assert.That(() => typed.Get(null), Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("key"));
	}

	private sealed class DirectCache<TKey, TValue> : SynchronizedObject, ICache<TKey, TValue>, IDisposable {
		public event EventHandlerEx<TKey, TValue> ItemFetched { add => Internal.ItemFetched += value; remove => Internal.ItemFetched -= value; }
		public event EventHandlerEx<TKey, CachedItem<TValue>> ItemRemoved { add => Internal.ItemRemoved += value; remove => Internal.ItemRemoved -= value; }
		event EventHandlerEx<object> ICache.ItemFetching { add => Untyped.ItemFetching += value; remove => Untyped.ItemFetching -= value; }
		event EventHandlerEx<object, object> ICache.ItemFetched { add => Untyped.ItemFetched += value; remove => Untyped.ItemFetched -= value; }
		event EventHandlerEx<object, CachedItem> ICache.ItemRemoved { add => Untyped.ItemRemoved += value; remove => Untyped.ItemRemoved -= value; }

		private readonly ActionCache<TKey, TValue> _internal;

		public DirectCache(Func<TKey, TValue> fetch) => _internal = new ActionCache<TKey, TValue>(fetch);

		public int GetCalls { get; private set; }

		private ActionCache<TKey, TValue> Internal => _internal;

		private ICache Untyped => _internal;

		IEnumerable<CachedItem> ICache.CachedItems => Untyped.CachedItems;
		int ICache.ItemCount => Untyped.ItemCount;
		long ICache.CurrentSize => Untyped.CurrentSize;
		long ICache.MaxCapacity => Untyped.MaxCapacity;
		DateTime ICache.LastUpdateOn => Untyped.LastUpdateOn;
		DateTime ICache.LastAccessedOn => Untyped.LastAccessedOn;
		long ICache.TotalAccesses => Untyped.TotalAccesses;
		TimeSpan ICache.ExpirationDuration => Untyped.ExpirationDuration;
		ExpirationPolicy ICache.ExpirationPolicy => Untyped.ExpirationPolicy;
		CacheReapPolicy ICache.ReapPolicy => Untyped.ReapPolicy;
		NullValuePolicy ICache.NullValuePolicy => Untyped.NullValuePolicy;
		object ICache.this[object index] { get => Untyped[index]; set => Untyped[index] = value; }

		public void Dispose() {
			Internal.Dispose();
			ThreadLock.Dispose();
		}

		bool ICache.ContainsCachedItem(object key) => Untyped.ContainsCachedItem(key);

		CachedItem ICache.Get(object key) {
			GetCalls++;
			return Untyped.Get(key);
		}

		void ICache.Set(object key, object value) => Untyped.Set(key, value);

		void ICache.BulkLoad(IEnumerable<KeyValuePair<object, object>> bulkLoadedValues) => Untyped.BulkLoad(bulkLoadedValues);

		void ICache.Invalidate(object key) => Untyped.Invalidate(key);

		void ICache.Remove(object key) => Untyped.Remove(key);

		void ICache.Purge() => Untyped.Purge();
	}
}
