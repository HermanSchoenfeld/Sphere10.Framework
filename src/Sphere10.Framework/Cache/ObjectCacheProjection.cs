// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>An explicitly requested object view of a typed cache. The source cache remains borrowed.</summary>
public sealed class ObjectCacheProjection<TKey, TValue> : ProjectedCache<TKey, TValue, object, object>, ICache {

	public ObjectCacheProjection(ICache<TKey, TValue> source)
		: base(source, key => key, key => (TKey)key, value => value, value => (TValue)value) {
	}
}
