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

namespace Sphere10.Framework;

/// <summary>
/// Implements page caching and lifecycle management for paged lists that keep only a subset of pages resident in memory.
/// Eviction, loading, and persistence hooks are surfaced via events so consumers can observe paging behavior.
/// </summary>
public abstract class MemoryPagedListBase<TItem> : PagedListBase<TItem>, IMemoryPagedList<TItem> {

	/// <inheritdoc />
	public event EventHandlerEx<object, IMemoryPage<TItem>> PageLoading;
	/// <inheritdoc />
	public event EventHandlerEx<object, IMemoryPage<TItem>> PageLoaded;
	/// <inheritdoc />
	public event EventHandlerEx<object, IMemoryPage<TItem>> PageSaving;
	/// <inheritdoc />
	public event EventHandlerEx<object, IMemoryPage<TItem>> PageSaved;
	/// <inheritdoc />
	public event EventHandlerEx<object, IMemoryPage<TItem>> PageUnloading;
	/// <inheritdoc />
	public event EventHandlerEx<object, IMemoryPage<TItem>> PageUnloaded;

	private readonly ICache<long, IMemoryPage<TItem>> _loadedPages;

	protected bool Disposing;

	protected MemoryPagedListBase(long pageSize, long maxMemory, bool autoLoad = false) 
		: base(false) {
		Guard.ArgumentInRange(pageSize, 1, int.MaxValue, nameof(pageSize));
		Guard.ArgumentInRange(maxMemory, pageSize, long.MaxValue, nameof(maxMemory));
		PageSize = pageSize;
		FlushOnDispose = false;
		Disposing = false;
		Pages = InternalPages.AsReadOnly().WithProjection(p => (IMemoryPage<TItem>)p);
		_loadedPages = new PageCache(this, maxMemory);

		_loadedPages.ItemRemoved += (page, key) => {
			var memPage = (IMemoryPage<TItem>)InternalPages[page];

			// Uncaching a deleted page, do nothing
			if (memPage.State == PageState.Deleted)
				return;

			NotifyPageAccessing(memPage);
			NotifyPageUnloading(memPage);
			memPage.Unload();
			NotifyPageUnloaded(memPage);
			NotifyPageAccessed(memPage);
			if (memPage.State == PageState.Deleting)
				memPage.Dispose();
		};

		if (autoLoad && RequiresLoad)
			Load();
	}

	/// <summary>
	/// Exposes the pages managed by this list, including those that may be currently unloaded.
	/// </summary>
	public new IReadOnlyList<IMemoryPage<TItem>> Pages { get; }

	/// <summary>
	/// Maximum logical size of each page in items (not bytes).
	/// </summary>
	public long PageSize { get; }

	/// <summary>
	/// Number of pages currently cached in memory.
	/// </summary>
	public long CurrentOpenPages => _loadedPages.ItemCount;

	/// <summary>
	/// Indicates whether the list has been disposed.
	/// </summary>
	public bool Disposed { get; protected set; }

	/// <summary>
	/// Maximum number of bytes that can be occupied by loaded pages.
	/// </summary>
	public long MaxMemory => _loadedPages.MaxCapacity;

	/// <summary>
	/// Returns <c>true</c> when any page in the list is dirty.
	/// </summary>
	public virtual bool Dirty => InternalPages.Any(p => p.Dirty);

	/// <summary>
	/// When set, calls to <see cref="Dispose"/> automatically flush dirty pages and unload cached ones.
	/// </summary>
	public bool FlushOnDispose { get; set; }

	/// <summary>
	/// Calculates the aggregate size of every page, respecting implementations that measure size differently.
	/// </summary>
	public long CalculateTotalSize() => InternalPages.Sum(p => p.Size);

	/// <inheritdoc />
	public sealed override IDisposable EnterOpenPageScope(IPage<TItem> page) {
		// dont need to do much since cache manages life-cycle of page
		CheckNotDisposed();
		var cachedItem = _loadedPages.Get(page.Number); // ensures page is fetched from streams if not cached
		var canPurge = cachedItem.CanPurge;
		cachedItem.CanPurge = false;
		return new ActionScope(() => cachedItem.CanPurge = canPurge);
	}

	/// <summary>
	/// Saves all dirty pages and unloads everything currently cached.
	/// </summary>
	public virtual void Flush() {
		// Causes all dirty pages to be saved
		// and all loaded pages to be unloaded
		_loadedPages.Purge();
	}

	/// <summary>
	/// Disposes the list, optionally flushing dirty pages first and clearing page metadata.
	/// </summary>
	public virtual void Dispose() {
		if (FlushOnDispose)
			Flush();
		Disposing = true;
		if (!RequiresLoad)
			Clear();
		Disposed = true;
	}

	protected override void OnAccessing() {
		base.OnAccessing();
		CheckNotDisposed();
	}

	#region Events

	protected override void OnPageDeleting(IPage<TItem> page) {
		base.OnPageDeleting(page);
		if (_loadedPages.ContainsCachedItem(page.Number))
			_loadedPages.Remove(page.Number);
	}

	protected virtual void OnPageLoading(IMemoryPage<TItem> page) {
	}

	protected virtual void OnPageLoaded(IMemoryPage<TItem> page) {
	}

	protected virtual void OnPageSaving(IMemoryPage<TItem> page) {
	}

	protected virtual void OnPageSaved(IMemoryPage<TItem> page) {
	}

	protected virtual void OnPageUnloading(IMemoryPage<TItem> page) {
	}

	protected virtual void OnPageUnloaded(IMemoryPage<TItem> page) {
	}

	private void NotifyPageLoading(IMemoryPage<TItem> page) {
		OnPageLoading(page);
		PageLoading?.Invoke(this, page);
	}

	private void NotifyPageLoaded(IMemoryPage<TItem> page) {
		OnPageLoaded(page);
		PageLoaded?.Invoke(this, page);
	}

	private void NotifyPageSaving(IMemoryPage<TItem> page) {
		OnPageSaving(page);
		PageSaving?.Invoke(this, page);
	}

	private void NotifyPageSaved(IMemoryPage<TItem> page) {
		OnPageSaved(page);
		PageSaved?.Invoke(this, page);
	}

	private void NotifyPageUnloading(IMemoryPage<TItem> page) {
		OnPageUnloading(page);
		PageUnloading?.Invoke(this, page);
	}

	private void NotifyPageUnloaded(IMemoryPage<TItem> page) {
		OnPageUnloaded(page);
		PageUnloaded?.Invoke(this, page);
	}

	#endregion

	#region Private methods

	private void CheckNotDisposed() {
		if (Disposed)
			throw new InvalidOperationException("Disposing or disposed");
	}

	#endregion

	private sealed class PageCache : CacheBase<long, IMemoryPage<TItem>> {
		private readonly MemoryPagedListBase<TItem> _owner;

		public PageCache(MemoryPagedListBase<TItem> owner, long maxMemory)
			: base(reapStrategy: CacheReapPolicy.LeastUsed, expirationStrategy: ExpirationPolicy.SinceLastAccessedTime, maxCapacity: maxMemory) {
			_owner = owner;
		}

		protected override IMemoryPage<TItem> Fetch(long key) {
			Guard.ArgumentInRange(key, 0, _owner.InternalPages.Count - 1, nameof(key), "Page not contained in list");
			var page = (IMemoryPage<TItem>)_owner.InternalPages[key];
			EnsureCapacity(_owner.PageSize);
			if (_owner.PageSize > Reaper.AvailableSpace(this))
				throw new InvalidOperationException("Cache capacity insufficient to open the requested page.");

			// Reserve before loading: callbacks may open other pages, but cannot spend this page's capacity.
			CurrentSize += _owner.PageSize;
			try {
				_owner.NotifyPageAccessing(page);
				_owner.NotifyPageLoading(page);
				page.Load();
				return page;
			} finally {
				CurrentSize -= _owner.PageSize;
			}
		}

		protected override long EstimateSize(IMemoryPage<TItem> value) => _owner.PageSize;

		protected override bool CheckStaleness(long key, CachedItem<IMemoryPage<TItem>> item) => false;

		protected override void OnItemFetched(long key, IMemoryPage<TItem> value) {
			var item = InternalStorage[key];
			var canPurge = item.CanPurge;
			item.CanPurge = false;
			try {
				_owner.NotifyPageLoaded(value);
				_owner.NotifyPageAccessed(value);
			} finally {
				item.CanPurge = canPurge;
			}
			if (value.State != PageState.Loaded || !InternalStorage.TryGetValue(key, out var current) || !ReferenceEquals(current, item))
				throw new InvalidOperationException("The page was unloaded or reopened by a load notification. Retry the operation to reopen it.");
		}

		protected override void OnItemRemoving(long key, CachedItem<IMemoryPage<TItem>> item) {
			// Uncaching a dirty page, ensure saved
			var page = item.Value;
			if (!page.Dirty || page.State is PageState.Deleting or PageState.Deleted)
				return;
			_owner.NotifyPageSaving(page);
			page.Save();
			_owner.NotifyPageSaved(page);
			if (page.Dirty)
				throw new InvalidOperationException("The page was modified by a save notification. Retry the operation to save those changes.");
		}
	}

}

