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

public class LargeCollection<TItem> : CollectionDecorator<TItem>, IDisposable {
	public event EventHandlerEx<object, IMemoryPage<TItem>> PageLoading { add => InternalCollection.PageLoading += value; remove => InternalCollection.PageLoading -= value; }
	public event EventHandlerEx<object, IMemoryPage<TItem>> PageLoaded { add => InternalCollection.PageLoaded += value; remove => InternalCollection.PageLoaded -= value; }
	public event EventHandlerEx<object, IMemoryPage<TItem>> PageUnloading { add => InternalCollection.PageUnloading += value; remove => InternalCollection.PageUnloading -= value; }
	public event EventHandlerEx<object, IMemoryPage<TItem>> PageUnloaded { add => InternalCollection.PageUnloaded += value; remove => InternalCollection.PageUnloaded -= value; }

	protected new MemoryPagedList<TItem> InternalCollection; 

	/// <summary>Creates a collection whose items are measured by the default Framework serializer for <typeparamref name="TItem"/>.</summary>
	public LargeCollection(int pageSize, long maxMemory, CacheReapPolicy pageReapPolicy = Sphere10FrameworkDefaults.PageCacheReapPolicy)
		: this(new MemoryPagedList<TItem>(pageSize, maxMemory, SerializerFactory.Default.GetSerializer<TItem>(), pageReapPolicy)) {
	}

	/// <summary>Creates a collection whose items are measured by <paramref name="itemSizer"/>.</summary>
	public LargeCollection(int pageSize, long maxMemory, Func<TItem, long> itemSizer, CacheReapPolicy pageReapPolicy = Sphere10FrameworkDefaults.PageCacheReapPolicy)
		: this(new MemoryPagedList<TItem>(pageSize, maxMemory, itemSizer, pageReapPolicy)) {
	}

	private LargeCollection(MemoryPagedList<TItem> list)
		: base(list) {
		InternalCollection = list;
		Pages = list.Pages;
	}

	public IReadOnlyList<IMemoryPage<TItem>> Pages { get; }

	/// <summary>The order in which loaded pages are evicted when the memory budget is reached.</summary>
	public CacheReapPolicy PageReapPolicy => InternalCollection.PageReapPolicy;

	public void Dispose() {
		InternalCollection.Dispose();
	}

}

