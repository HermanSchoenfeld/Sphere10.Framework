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

/// <summary>Coordinates one grid-owned borrowed source without collecting or owning its data.</summary>
internal sealed class VisualGridDataSourceController {
	private readonly VisualGrid _grid;
	private readonly IDataSource<object> _source;
	private readonly Type _itemType;
	private readonly Func<object, object> _resolvePublishedEntity;
	private readonly int _assignmentVersion;
	private readonly DataSourceCapabilities _capabilities;
	private readonly AsyncSynchronizedGate _refresh = new();

	internal VisualGridDataSourceController(VisualGrid grid, IDataSource<object> source, Type itemType, Func<object, object> resolvePublishedEntity, DataSourceCapabilities capabilities, int assignmentVersion) {
		_grid = grid;
		_source = source;
		_itemType = itemType;
		_resolvePublishedEntity = resolvePublishedEntity;
		_assignmentVersion = assignmentVersion;
		_capabilities = capabilities;
	}

	internal bool IsBound { get; private set; }

	internal async Task BindAsync() {
		using (await _refresh.EnterScopeAsync())
			if (await BindCoreAsync())
				ApplySelectionBinding();
	}

	internal async Task RefreshAsync(CancellationToken cancellationToken) {
		using (await _refresh.EnterScopeAsync(cancellationToken)) {
			if (!_grid.OwnsDataSourceController(this))
				return;
			_grid.InvalidateDataSource();
			if (!await BindCoreAsync())
				return;
			cancellationToken.ThrowIfCancellationRequested();
			if (!_grid.OwnsDataSourceController(this))
				return;
			ApplySelectionBinding();
			await _grid.RefreshGrid();
		}
	}

	internal void ReconcileSelection(VisualGrid sender) {
		if (!IsBound || !_grid.OwnsDataSourceController(this))
			return;
		var identity = _grid.VisibleSelectionIdentity;
		if (identity == null)
			return;
		if (!_grid.HasDetailSelection && _itemType.IsInstanceOfType(_grid.SelectedEntity)) {
			var selected = _grid.SelectedEntity;
			var key = identity(selected);
			var published = _grid.VisibleEntities.FirstOrDefault(row => _itemType.IsInstanceOfType(row) && Equals(key, identity(row)));
			if (!ReferenceEquals(published, selected))
				_grid.SelectedEntity = published;
		}
		if (_grid.OwnsDataSourceController(this))
			_grid.NotifySelectionReconciled();
	}

	private async Task<bool> BindCoreAsync() {
		if (!_grid.OwnsDataSourceController(this))
			return false;
		if (!IsBound) {
			if (!await _grid.BindDataSourceAsync(_source, _itemType, _capabilities, _assignmentVersion) || !_grid.OwnsDataSourceController(this))
				return false;
			IsBound = true;
		}
		return true;
	}

	private void ApplySelectionBinding() {
		_grid.ResolvePublishedEntity = _resolvePublishedEntity;
		_grid.PublishedSelectionIdentity = _grid.VisibleSelectionIdentity;
	}

}
