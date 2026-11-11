// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>Stores its data as a list fetched asynchronously; reads search, sort and page that list and committed mutations notify consumers.</summary>
/// <remarks>
/// The dual of <see cref="ListDataSource{TItem}"/> on the asynchronous lineage. Construct it over a list or rows (already loaded), or derive from it and
/// override <see cref="FetchAsync"/> so the list is fetched on load. Its synchronous members wait on the asynchronous ones
/// (<see cref="AsyncDataSourceBase{TItem}"/>). Reads load an unloaded list first.
/// </remarks>
public class AsyncListDataSource<TItem> : AsyncBatchDataSourceBase<TItem>, ILoadableDataSource<TItem> {
	// NOTE: ListDataSource is the dual of this class on the synchronous lineage. Since multiple inheritance is disallowed in C#,
	// the list storage below mirrors it; keep both in step.

	public event EventHandlerEx<object> Loading;
	public event EventHandlerEx<object> Loaded;

	private readonly SemaphoreSlim _loadGate = new(1, 1);
	private IExtendedList<TItem> _list;
	private volatile bool _loaded;

	/// <summary>Serves the supplied list; nothing is fetched.</summary>
	public AsyncListDataSource(IExtendedList<TItem> list) {
		Guard.ArgumentNotNull(list, nameof(list));
		_list = list;
		_loaded = true;
	}

	/// <summary>Serves a list copied from the rows; nothing is fetched.</summary>
	public AsyncListDataSource(IEnumerable<TItem> rows)
		: this(ToList(rows)) {
	}

	/// <summary>Fetches the list on load through <see cref="FetchAsync"/>.</summary>
	protected AsyncListDataSource() {
	}

	public string DefaultSortProperty { get; set; }

	public SortDirection DefaultSortDirection { get; set; } = SortDirection.Ascending;

	public bool RequiresLoad => !_loaded;

	public override Task<long> CountAsync => ReadCountAsync();

	public override Task<DataSourceCapabilities> CapabilitiesAsync => Task.FromResult(Capabilities);

	public override DataSourceCapabilities Capabilities => DataSourceCapabilities.Default | DataSourceCapabilities.CanNotify;

	/// <summary>The stored list, loaded synchronously first when it has not been loaded. Asynchronous members await <see cref="GetListAsync"/> instead.</summary>
	protected IExtendedList<TItem> List {
		get {
			if (RequiresLoad)
				Load();
			return _list;
		}
	}

	public void Load() {
		if (!RequiresLoad)
			return;
		Debug.AssertNotMainThread();
		Task.ExecuteSyncOverAsync(() => LoadAsync());
	}

	/// <summary>Fetches the list on a worker. Concurrent callers wait for the first caller's fetch; cancellation ends this caller's wait and the fetch it started.</summary>
	public async Task LoadAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		if (!RequiresLoad)
			return;

		// Concurrent callers wait for the first caller's fetch rather than fetch twice
		await _loadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
		try {
			if (!RequiresLoad)
				return;
			NotifyLoading();
			// Acquisition runs on a worker even when the fetch has a synchronous prefix
			var rows = await Task.Run(() => FetchAsync(cancellationToken), cancellationToken).ConfigureAwait(false);
			Guard.Ensure(rows != null, "A fetch must return its complete list.");
			_list = ToList(rows);
			_loaded = true;
		} finally {
			_loadGate.Release();
		}
		NotifyLoaded();
	}

	public override IEnumerable<TItem> NewRange(int count) => Enumerable.Range(0, count).Select(_ => NewMethod());

	public override async Task CreateRangeAsync(IEnumerable<TItem> entities) {
		var rows = entities as TItem[] ?? entities.ToArray();
		if (rows.Length == 0)
			return;
		var list = await GetListAsync().ConfigureAwait(false);
		var count = list.Count;
		Tools.Exceptions.ExecuteCapturingException(() => list.AddRange(rows), out var error);
		CompleteMutation(count != list.Count, error);
	}

	public override async Task<DataSourceItems<TItem>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		var list = await GetListAsync().ConfigureAwait(false);
		return Tools.DataSources.Query(list, searchTerm, pageLength, page, sortProperty, sortDirection, FormatValue, DefaultSortProperty, DefaultSortDirection);
	}

	/// <summary>Refreshes each entity through <see cref="RefreshMethodAsync"/>, storing a changed row in the list and removing a row whose refresh returns null.</summary>
	public override async Task RefreshRangeAsync(TItem[] entities) {
		var list = await GetListAsync().ConfigureAwait(false);
		var changed = false;
		for (var index = 0; index < entities.Length; index++) {
			var entity = entities[index];
			var refreshed = await RefreshMethodAsync(entity).ConfigureAwait(false);
			entities[index] = refreshed;
			changed |= StoreRefreshed(list, entity, refreshed);
		}
		if (changed)
			NotifyChanged();
	}

	public override async Task UpdateRangeAsync(IEnumerable<TItem> entities) {
		var rows = entities as TItem[] ?? entities.ToArray();
		if (rows.Length == 0)
			return;
		var list = await GetListAsync().ConfigureAwait(false);
		var changed = false;
		Tools.Exceptions.ExecuteCapturingException(() => {
			foreach (var (index, row) in list.IndexOfRange(rows).Zip(rows)) {
				list.Update(index, row);
				changed = true;
			}
		}, out var error);
		CompleteMutation(changed, error);
	}

	public override async Task DeleteRangeAsync(IEnumerable<TItem> entities) {
		var rows = entities as TItem[] ?? entities.ToArray();
		if (rows.Length == 0)
			return;
		var list = await GetListAsync().ConfigureAwait(false);
		var count = list.Count;
		Tools.Exceptions.ExecuteCapturingException(() => list.RemoveRange(rows), out var error);
		CompleteMutation(count != list.Count, error);
	}

	public override Task<Result> ValidateRangeAsync(IEnumerable<(TItem entity, CrudAction action)> actions) => Task.FromResult(Result.Default);

	/// <summary>The asynchronous acquisition of the complete list, run by <see cref="LoadAsync"/>. A fetching leaf overrides it.</summary>
	protected virtual Task<IEnumerable<TItem>> FetchAsync(CancellationToken cancellationToken)
		=> throw new NotSupportedException("Supply the list or override FetchAsync to acquire it.");

	/// <summary>Formats a property value for searching and sorting.</summary>
	protected virtual string FormatValue(object value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

	protected virtual TItem NewMethod() => TypeActivator.Activate<TItem>();

	protected virtual Task<TItem> RefreshMethodAsync(TItem entity) => Task.FromResult(entity);

	protected virtual void OnLoading() {
	}

	protected virtual void OnLoaded() {
	}

	/// <summary>The stored list, fetched first when it has not been loaded.</summary>
	protected async Task<IExtendedList<TItem>> GetListAsync() {
		if (RequiresLoad)
			await LoadAsync().ConfigureAwait(false);
		return _list;
	}

	/// <summary>Copies rows into a growable list; an array-wrapped list would be fixed at the array's capacity.</summary>
	protected static IExtendedList<TItem> ToList(IEnumerable<TItem> rows) {
		Guard.ArgumentNotNull(rows, nameof(rows));
		var list = new ExtendedList<TItem>();
		list.AddRange(rows);
		return list;
	}

	/// <summary>Stores a refreshed row in place of its original, or removes the original when the refresh returned null. Returns whether the list changed.</summary>
	private static bool StoreRefreshed(IExtendedList<TItem> list, TItem entity, TItem refreshed) {
		if (ReferenceEquals(refreshed, entity) || entity == null)
			return false;
		var position = list.IndexOfL(entity);
		if (position < 0)
			return false;
		if (refreshed == null)
			list.RemoveAt(position);
		else
			list.Update(position, refreshed);
		return true;
	}

	private async Task<long> ReadCountAsync() {
		var list = await GetListAsync().ConfigureAwait(false);
		return list.Count;
	}

	private void NotifyLoading() {
		Tools.Exceptions.Execute(OnLoading, ReportHookFailure);
		Tools.Exceptions.Execute(() => Loading?.Invoke(this), ReportHookFailure);
	}

	private void NotifyLoaded() {
		Tools.Exceptions.Execute(OnLoaded, ReportHookFailure);
		Tools.Exceptions.Execute(() => Loaded?.Invoke(this), ReportHookFailure);
	}

	private static void ReportHookFailure(Exception error)
		=> Tools.Exceptions.ExecuteIgnoringException(() => SystemLog.Logger.Exception(error, "Data source load notification failed."));

	/// <summary>Notifies the changes a nontransactional list actually committed before rethrowing its failure.</summary>
	private void CompleteMutation(bool changed, Exception error) {
		if (changed)
			NotifyChanged();
		if (error != null)
			ExceptionDispatchInfo.Capture(error).Throw();
	}
}
