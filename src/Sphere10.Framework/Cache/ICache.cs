// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>Object-key/value specialization. Use a projection to opt a typed cache into this contract.</summary>
public interface ICache : ICache<object, object> {
}

/// <summary>A synchronized typed cache with expiration and capacity management.</summary>
public interface ICache<TKey, TValue> : ICacheBase {

	/// <summary>Raised immediately before a cache miss triggers a fetch.</summary>
	event EventHandlerEx<TKey> ItemFetching;

	/// <summary>Raised after a fetched value has been stored.</summary>
	event EventHandlerEx<TKey, TValue> ItemFetched;

	/// <summary>Raised before committing an entry's removal; ordinary removal remains retryable if a callback fails.</summary>
	event EventHandlerEx<TKey, CachedItem<TValue>> ItemRemoving;

	/// <summary>Raised when an entry is removed explicitly or through reaping.</summary>
	event EventHandlerEx<TKey, CachedItem<TValue>> ItemRemoved;

	/// <summary>A snapshot of entry membership, including expired entries. Entry metadata remains live.</summary>
	IEnumerable<CachedItem<TValue>> CachedItems { get; }

	bool ContainsCachedItem(TKey key);

	CachedItem<TValue> Get(TKey key);

	void Set(TKey key, TValue value);

	/// <summary>Stores pre-fetched values without invoking fetch callbacks.</summary>
	void BulkLoad(IEnumerable<KeyValuePair<TKey, TValue>> bulkLoadedValues);

	void Invalidate(TKey key);

	void Remove(TKey key);

	TValue this[TKey index] { get; set; }
}
