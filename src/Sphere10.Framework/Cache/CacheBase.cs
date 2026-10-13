// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>Object-key/value specialization of the typed cache engine.</summary>
public abstract class CacheBase : CacheBase<object, object>, ICache {
	protected CacheBase(
		IEqualityComparer<object> keyComparer,
		CacheReapPolicy reapStrategy = CacheReapPolicy.None,
		ExpirationPolicy expirationStrategy = ExpirationPolicy.None,
		long maxCapacity = int.MaxValue,
		TimeSpan? expirationDuration = null,
		NullValuePolicy nullValuePolicy = NullValuePolicy.CacheNormally,
		StaleValuePolicy staleValuePolicy = StaleValuePolicy.AssumeNeverStale,
		ICacheReaper reaper = null,
		double reapLevel = Sphere10FrameworkDefaults.CacheReapLevel
	) : base(reapStrategy, expirationStrategy, maxCapacity, expirationDuration, nullValuePolicy, staleValuePolicy, ValidateComparer(keyComparer), reaper, reapLevel) {
	}

	public override CachedItem Get(object key) => (CachedItem)base.Get(key);

	protected override CachedItem NewCachedItem(object key, object value) => new() { Value = value };

	protected sealed override bool CheckStaleness(object key, CachedItem<object> item) => CheckStaleness(key, (CachedItem)item);

	protected abstract bool CheckStaleness(object key, CachedItem item);

	protected override void OnItemRemoved(object key, CachedItem<object> item) => OnItemRemoved(key, (CachedItem)item);

	protected virtual void OnItemRemoved(object key, CachedItem item) {
	}

	private static IEqualityComparer<object> ValidateComparer(IEqualityComparer<object> keyComparer) {
		Guard.ArgumentNotNull(keyComparer, nameof(keyComparer));
		return keyComparer;
	}
}
