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
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>Stores its data as a list; reads search, sort and page that list and committed mutations notify consumers.</summary>
/// <remarks>
/// Construct it over a list or rows (already loaded), or derive from it and override <see cref="Fetch"/> so the list is fetched synchronously on load.
/// Its asynchronous members run the synchronous ones on a worker (<see cref="SyncDataSourceBase{TItem}"/>). The dual for an asynchronous backend is
/// <see cref="AsyncListDataSource{TItem}"/>. Reads load an unloaded list first.
/// </remarks>
public class ListDataSource<TItem> : SyncBatchDataSourceBase<TItem>, ILoadableDataSource<TItem> {
	// NOTE: AsyncListDataSource is the dual of this class on the asynchronous lineage. Since multiple inheritance is disallowed in C#,
	// the list storage below is mirrored there; keep both in step.

	public event EventHandlerEx<object> Loading;
	public event EventHandlerEx<object> Loaded;

	private readonly object _loadLock = new();
	private IExtendedList<TItem> _list;
	private volatile bool _loaded;

	/// <summary>Serves the supplied list; nothing is fetched.</summary>
	public ListDataSource(IExtendedList<TItem> list) {
		Guard.ArgumentNotNull(list, nameof(list));
		_list = list;
		_loaded = true;
	}

	/// <summary>Serves a list copied from the rows; nothing is fetched.</summary>
	public ListDataSource(IEnumerable<TItem> rows)
		: this(ToList(rows)) {
	}

	/// <summary>Fetches the list on load through <see cref="Fetch"/>.</summary>
	protected ListDataSource() {
	}

	public string DefaultSortProperty { get; set; }

	public SortDirection DefaultSortDirection { get; set; } = SortDirection.Ascending;

	public bool RequiresLoad => !_loaded;

	public override long Count => List.Count;

	public override DataSourceCapabilities Capabilities => DataSourceCapabilities.Default | DataSourceCapabilities.CanNotify;

	/// <summary>The stored list, fetched first when it has not been loaded.</summary>
	protected IExtendedList<TItem> List {
		get {
			if (RequiresLoad)
				Load();
			return _list;
		}
	}

	public void Load() {
		// Concurrent callers wait for the first caller's fetch rather than fetch twice
		lock (_loadLock) {
			if (!RequiresLoad)
				return;
			NotifyLoading();
			var rows = Fetch();
			Guard.Ensure(rows != null, "A fetch must return its complete list.");
			_list = ToList(rows);
			_loaded = true;
		}
		NotifyLoaded();
	}

	/// <summary>Runs <see cref="Load"/> on a worker. Cancellation ends this caller's wait; a fetch already running completes for other callers.</summary>
	public Task LoadAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		if (!RequiresLoad)
			return Task.CompletedTask;
		return Task.Run(Load).WaitAsync(cancellationToken);
	}

	public override IEnumerable<TItem> NewRange(int count) => Enumerable.Range(0, count).Select(_ => NewMethod());

	public override void CreateRange(IEnumerable<TItem> entities) {
		var rows = entities as TItem[] ?? entities.ToArray();
		if (rows.Length == 0)
			return;
		var list = List;
		var count = list.Count;
		Tools.Exceptions.ExecuteCapturingException(() => list.AddRange(rows), out var error);
		CompleteMutation(count != list.Count, error);
	}

	public override DataSourceItems<TItem> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection)
		=> Tools.DataSources.Query(List, searchTerm, pageLength, page, sortProperty, sortDirection, FormatValue, DefaultSortProperty, DefaultSortDirection);

	/// <summary>Refreshes each entity through <see cref="RefreshMethod"/>, storing a changed row in the list and removing a row whose refresh returns null.</summary>
	public override void RefreshRange(TItem[] entities) {
		var list = List;
		var changed = false;
		for (var index = 0; index < entities.Length; index++) {
			var entity = entities[index];
			var refreshed = RefreshMethod(entity);
			entities[index] = refreshed;
			changed |= StoreRefreshed(list, entity, refreshed);
		}
		if (changed)
			NotifyChanged();
	}

	public override void UpdateRange(IEnumerable<TItem> entities) {
		var rows = entities as TItem[] ?? entities.ToArray();
		if (rows.Length == 0)
			return;
		var list = List;
		var changed = false;
		Tools.Exceptions.ExecuteCapturingException(() => {
			foreach (var (index, row) in list.IndexOfRange(rows).Zip(rows)) {
				list.Update(index, row);
				changed = true;
			}
		}, out var error);
		CompleteMutation(changed, error);
	}

	public override void DeleteRange(IEnumerable<TItem> entities) {
		var rows = entities as TItem[] ?? entities.ToArray();
		if (rows.Length == 0)
			return;
		var list = List;
		var count = list.Count;
		Tools.Exceptions.ExecuteCapturingException(() => list.RemoveRange(rows), out var error);
		CompleteMutation(count != list.Count, error);
	}

	public override Result ValidateRange(IEnumerable<(TItem entity, CrudAction action)> actions) => Result.Default;

	/// <summary>The synchronous acquisition of the complete list, run by <see cref="Load"/>. A fetching leaf overrides it.</summary>
	protected virtual IEnumerable<TItem> Fetch() => throw new NotSupportedException("Supply the list or override Fetch to acquire it.");

	/// <summary>Formats a property value for searching and sorting.</summary>
	protected virtual string FormatValue(object value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

	protected virtual TItem NewMethod() => TypeActivator.Activate<TItem>();

	protected virtual TItem RefreshMethod(TItem entity) => entity;

	protected virtual void OnLoading() {
	}

	protected virtual void OnLoaded() {
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
