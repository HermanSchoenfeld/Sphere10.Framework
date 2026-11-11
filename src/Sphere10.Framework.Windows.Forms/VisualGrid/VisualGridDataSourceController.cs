// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Binds a ranged source without materializing its entire dataset. Snapshot collection remains a separate responsibility.</summary>
public sealed class VisualGridDataSourceController<T> {
	public event EventHandler SelectionReconciled;
	private readonly VisualGrid _grid;
	private readonly IDataSource<T> _source;
	private readonly Func<T, object> _resolvePublishedEntity;
	private readonly AsyncSynchronizedGate _refresh = new();
	private readonly int _ownerThread;
	private bool _bound;
	private Func<T, object> _visibleSelectionIdentity;

	public VisualGridDataSourceController(VisualGrid grid, IDataSource<T> source, Func<T, object> resolvePublishedEntity = null) {
		Guard.ArgumentNotNull(grid, nameof(grid));
		Guard.ArgumentNotNull(source, nameof(source));
		_grid = grid;
		_source = source;
		_resolvePublishedEntity = resolvePublishedEntity;
		_ownerThread = Environment.CurrentManagedThreadId;
		_grid.DataBindingComplete += GridDataBindingComplete;
	}

	/// <summary>Optional stable identity for reconciling selection with the accepted visible page. Null preserves the existing selection policy.</summary>
	/// <remarks>Runs on the owning dispatcher over published rows; it must not collect native resources.</remarks>
	public Func<T, object> VisibleSelectionIdentity {
		get => _visibleSelectionIdentity;
		set {
			Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThread, "Configure grid selection on its owning UI dispatcher.");
			_visibleSelectionIdentity = value;
			if (_bound)
				_grid.PublishedSelectionIdentity = PublishedIdentity;
		}
	}

	/// <summary>Optional resource-publication stamp captured before a ranged query and checked again before native publication.</summary>
	public Func<object> PublicationIdentity {
		get => _grid.SourcePublicationIdentity;
		set {
			Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThread, "Configure publication identity on its owning UI dispatcher.");
			_grid.SourcePublicationIdentity = value;
		}
	}

	/// <summary>Call after publishing source changes, on the owning dispatcher. Rows stay interactive during the asynchronous read.</summary>
	public void Invalidate() {
		Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThread, "Invalidate the grid query on its owning UI dispatcher.");
		if (!_grid.IsDisposed)
			_grid.NotifyDataSourceChanged();
	}

	/// <summary>Call after publishing source changes, on the owning dispatcher. Rows stay interactive during the asynchronous read.</summary>
	public async Task RefreshAsync(CancellationToken cancellationToken = default) {
		Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThread, "Refresh the grid on its owning UI dispatcher.");
		using var admission = await _refresh.EnterScopeAsync(cancellationToken);
		if (_grid.IsDisposed)
			return;
		_grid.NotifyDataSourceChanged();
		if (!_bound) {
			await _grid.SetDataSource(_source);
			_bound = true;
		}
		cancellationToken.ThrowIfCancellationRequested();
		if (_grid.IsDisposed)
			return;
		_grid.ResolvePublishedEntity = Resolve;
		_grid.PublishedSelectionIdentity = PublishedIdentity;
		await _grid.RefreshGrid();
	}

	/// <summary>The owner's visible-selection identity over published rows, applied inside the publication; null when no identity is configured.</summary>
	private object PublishedIdentity(object entity) => _visibleSelectionIdentity != null && entity is T typed ? _visibleSelectionIdentity(typed) : null;

	private object Resolve(object selected) {
		if (selected is not T typed)
			return null;
		var resolved = _resolvePublishedEntity == null ? selected : _resolvePublishedEntity(typed);
		Guard.Ensure(resolved == null || resolved is T, "A published grid selection must have the source's item type.");
		return resolved;
	}

	private void GridDataBindingComplete(object sender, EventArgs args) {
		if (_visibleSelectionIdentity == null)
			return;
		if (_grid.HasDetailSelection) {
			SelectionReconciled?.Invoke(this, EventArgs.Empty);
			return;
		}
		if (_grid.SelectedEntity is T selected) {
			var identity = _visibleSelectionIdentity(selected);
			var published = _grid.VisibleEntities.FirstOrDefault(row => row is T typed && Equals(identity, _visibleSelectionIdentity(typed)));
			if (!ReferenceEquals(published, selected))
				_grid.SelectedEntity = published;
		}
		SelectionReconciled?.Invoke(this, EventArgs.Empty);
	}
}
