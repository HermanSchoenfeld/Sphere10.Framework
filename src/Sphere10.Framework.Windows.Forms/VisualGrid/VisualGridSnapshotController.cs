// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Collects typed snapshots, binds them to a VisualGrid and retains row selection by caller-defined identity.</summary>
public sealed class VisualGridSnapshotController<T> : IVisualGridSnapshotController {
	private readonly VisualGrid _grid;
	private readonly SnapshotDataSource<T> _source;
	private readonly Func<T, object> _identity;
	private readonly Func<CancellationToken, Task<T[]>> _collect;
	private readonly Action<T[]> _changed;
	private readonly AsyncSynchronizedGate _refresh = new();
	private readonly int _ownerThreadId;
	private bool _bound;

	/// <summary>Binds collection and identity callbacks to a grid on its creating UI thread.</summary>
	public VisualGridSnapshotController(VisualGrid grid, Func<T, object> identity, Func<CancellationToken, Task<T[]>> collect, Action<T[]> changed = null,
				Func<object, string> formatValue = null)
		: this(grid, new SnapshotDataSource<T>(formatValue), identity, collect, changed) {
	}

	/// <summary>Binds collection and identity callbacks to a grid on its creating UI thread.</summary>
	public VisualGridSnapshotController(VisualGrid grid, SnapshotDataSource<T> source, Func<T, object> identity, Func<CancellationToken, Task<T[]>> collect,
				Action<T[]> changed = null) {
		Guard.ArgumentNotNull(grid, nameof(grid));
		Guard.ArgumentNotNull(source, nameof(source));
		Guard.ArgumentNotNull(identity, nameof(identity));
		Guard.ArgumentNotNull(collect, nameof(collect));
		_grid = grid;
		_source = source;
		_identity = identity;
		_collect = collect;
		_changed = changed;
		_ownerThreadId = Environment.CurrentManagedThreadId;
	}

	/// <summary>The number of rows in the currently published snapshot.</summary>
	public long Count {
		get {
			return _source.Count;
		}
	}

	/// <summary>The currently published typed source snapshot.</summary>
	public T[] Rows {
		get {
			return _source.Snapshot;
		}
	}

	/// <summary>Call on the grid's UI thread with its dispatcher context. Collection is caller-supplied; refreshes serialize through a disposable admission scope.</summary>
	public async Task RefreshAsync(CancellationToken cancellationToken = default) {
		EnsureOwnerThread();
		using var admission = await _refresh.EnterScopeAsync(cancellationToken);
		if (_grid.IsDisposed)
			return;
		var rows = await _collect(cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		EnsureOwnerThread();
		if (_grid.IsDisposed)
			return;
		Guard.Ensure(rows != null, "Snapshot collection must return an array.");
		_source.ReplaceSnapshot(rows);
		_grid.NotifyDataSourceChanged();
		if (!_bound) {
			await _grid.SetDataSource(_source, _source.Capabilities);
			_bound = true;
		}
		// Map the latest selected instance as part of the native publication, before geometry work can yield to input.
		_grid.ResolvePublishedEntity = ResolvePublishedEntity;
		await _grid.RefreshGrid();
		if (cancellationToken.IsCancellationRequested || _grid.IsDisposed)
			return;
		_changed?.Invoke(_source.Snapshot);
	}

	/// <summary>Configures the source's fallback sorting on the owner thread.</summary>
	public void SetDefaultSort(string property, SortDirection direction) {
		EnsureOwnerThread();
		_source.DefaultSortProperty = property;
		_source.DefaultSortDirection = direction;
	}

	/// <summary>Returns the typed snapshot through the nonvisual controller contract.</summary>
	public object CaptureSnapshot() {
		return Rows;
	}

	/// <summary>Finds the selected entity's matching identity in the latest published snapshot.</summary>
	private object ResolvePublishedEntity(object selected) {
		if (selected is not T entity)
			return null;
		var identity = _identity(entity);
		if (identity != null)
			foreach (var row in _source.Snapshot)
				if (Equals(identity, _identity(row)))
					return row;
		return null;
	}

	/// <summary>Requires configuration and native publication on the creating UI thread.</summary>
	private void EnsureOwnerThread() {
		Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThreadId, "Refresh and configure the grid snapshot controller on its creating UI thread.");
	}
}
