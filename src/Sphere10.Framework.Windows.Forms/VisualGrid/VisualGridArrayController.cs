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

/// <summary>Binds typed rows to a VisualGrid through an array store and retains row selection by caller-defined identity. Rows come from a borrowed source or from a collector callback.</summary>
public sealed class VisualGridArrayController<T> : IVisualGridArrayController {
	public event EventHandler SelectionReconciled {
		add {
			_binding.SelectionReconciled += value;
		}
		remove {
			_binding.SelectionReconciled -= value;
		}
	}
	private readonly VisualGrid _grid;
	private readonly ArrayDataSource<T> _projectionSource;
	private readonly IDataSource<T> _captureSource;
	private readonly Func<T, object> _identity;
	private readonly Func<CancellationToken, Task<T[]>> _collect;
	private readonly Action<T[]> _changed;
	private readonly AsyncSynchronizedGate _refresh = new();
	private readonly int _ownerThreadId;
	private readonly VisualGridDataSourceController<T> _binding;
	private object _publishedIdentity;

	/// <summary>Binds collection and identity callbacks to a grid on its creating UI thread.</summary>
	public VisualGridArrayController(VisualGrid grid, Func<T, object> identity, Func<CancellationToken, Task<T[]>> collect, Action<T[]> changed = null)
		: this(grid, new ArrayDataSource<T>(), identity, collect, changed) {
	}

	/// <summary>Binds collection and identity callbacks to a grid on its creating UI thread.</summary>
	public VisualGridArrayController(VisualGrid grid, ArrayDataSource<T> source, Func<T, object> identity, Func<CancellationToken, Task<T[]>> collect,
				Action<T[]> changed = null)
		: this(grid, source, identity, collect, changed, null) {
	}

	/// <summary>Projects a borrowed source's published rows into an existing array query source, retaining its specialized filtering, hierarchy and row-refresh behavior.</summary>
	public VisualGridArrayController(VisualGrid grid, IDataSource<T> captureSource, ArrayDataSource<T> projectionSource,
		Func<T, object> identity, Action<T[]> changed = null
	)
		: this(grid, projectionSource, identity, null, changed, captureSource) {
	}

	private VisualGridArrayController(VisualGrid grid, ArrayDataSource<T> source, Func<T, object> identity, Func<CancellationToken, Task<T[]>> collect,
		Action<T[]> changed, IDataSource<T> captureSource
	) {
		Guard.ArgumentNotNull(grid, nameof(grid));
		Guard.ArgumentNotNull(source, nameof(source));
		Guard.ArgumentNotNull(identity, nameof(identity));
		if (captureSource == null)
			Guard.ArgumentNotNull(collect, nameof(collect));
		_captureSource = captureSource;
		_grid = grid;
		_projectionSource = source;
		_identity = identity;
		_collect = collect;
		_changed = changed;
		_ownerThreadId = Environment.CurrentManagedThreadId;
		_binding = new VisualGridDataSourceController<T>(grid, source, ResolvePublishedEntity);
	}

	/// <summary>The number of currently published rows.</summary>
	public long Count => _projectionSource.Count;

	/// <summary>The currently published typed rows.</summary>
	public T[] Rows => _projectionSource.Data;

	/// <summary>Optional owner-thread publication stamp. A changed value rejects prepared rows without replacing the published rows.</summary>
	public Func<object> PublicationIdentity {
		get;
		set {
			EnsureOwnerThread();
			field = value;
			_binding.PublicationIdentity = value;
			_grid.AcceptSourcePublication = value == null ? null : () => Equals(_publishedIdentity, value());
		}
	}

	/// <summary>Rejects an in-flight grid query through its existing publication version; committed rows stay interactive.</summary>
	public void Invalidate() { EnsureOwnerThread(); _binding.Invalidate(); }

	/// <summary>Optional visible-page selection identity; null preserves the controller's existing selection behavior.</summary>
	public Func<T, object> VisibleSelectionIdentity {
		get => _binding.VisibleSelectionIdentity;
		set => _binding.VisibleSelectionIdentity = value;
	}

	/// <summary>Refreshes on the grid's UI thread through a disposable admission scope. A source-backed controller only presents rows the source has already loaded; a collector-backed controller first invokes its collection callback.</summary>
	public async Task RefreshAsync(CancellationToken cancellationToken = default) {
		EnsureOwnerThread();
		using var admission = await _refresh.EnterScopeAsync(cancellationToken);
		if (_grid.IsDisposed)
			return;
		// Presentation never triggers the first acquisition; the source owner loads it.
		if (_captureSource is ILoadable { RequiresLoad: true })
			return;
		var publication = PublicationIdentity?.Invoke();
		var rows = _captureSource == null ? await _collect(cancellationToken) : await Tools.DataSources.ReadAllAsync(_captureSource, cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		EnsureOwnerThread();
		if (_grid.IsDisposed)
			return;
		if (PublicationIdentity != null && !Equals(publication, PublicationIdentity()))
			return;
		Guard.Ensure(rows != null, "Row collection must return an array.");
		_publishedIdentity = publication;
		_projectionSource.ReplaceData(rows);
		// Map the latest selected instance as part of the native publication, before geometry work can yield to input.
		var accepted = false;
		EventHandler observeAcceptance = (_, _) => accepted = true;
		if (PublicationIdentity != null)
			_grid.DataBindingComplete += observeAcceptance;
		using var observe = Tools.Scope.ExecuteOnDispose(() => _grid.DataBindingComplete -= observeAcceptance);
		await _binding.RefreshAsync(cancellationToken);
		if (PublicationIdentity != null && !accepted)
			return;
		if (cancellationToken.IsCancellationRequested || _grid.IsDisposed)
			return;
		_changed?.Invoke(Rows);
	}

	/// <summary>Configures the store's fallback sorting on the owner thread.</summary>
	public void SetDefaultSort(string property, SortDirection direction) {
		EnsureOwnerThread();
		_projectionSource.DefaultSortProperty = property;
		_projectionSource.DefaultSortDirection = direction;
	}

	/// <summary>Returns the typed rows through the nonvisual controller contract.</summary>
	public object CaptureRows() => Rows;

	/// <summary>Finds the selected entity's matching identity in the latest published rows.</summary>
	private object ResolvePublishedEntity(T entity) {
		var identity = _identity(entity);
		if (identity != null)
			foreach (var row in Rows)
				if (Equals(identity, _identity(row)))
					return row;
		return null;
	}

	/// <summary>Requires configuration and native publication on the creating UI thread.</summary>
	private void EnsureOwnerThread()
		=> Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThreadId, "Refresh and configure the grid array controller on its creating UI thread.");
}
