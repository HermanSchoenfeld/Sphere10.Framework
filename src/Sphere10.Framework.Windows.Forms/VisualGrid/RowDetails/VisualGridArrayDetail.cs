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

/// <summary>Binds prepared child rows through an array store and the ordinary IDataSource paging/query path.</summary>
public sealed class VisualGridArrayDetail<T> : VisualGridDetail {
	private readonly ArrayDataSource<T> _source;
	private readonly VisualGridDataSourceController<T> _controller;
	private readonly Func<T, object> _identity;
	private readonly Action<T[]> _changed;
	private readonly Action<VisualGrid, T[]> _beforePublish;
	private readonly Func<object, T[]> _readPreparedRows;

	/// <param name="source">An optional array store supplying the child query policy, such as value formatting; by default an ordinary array source.</param>
	public VisualGridArrayDetail(VisualGrid grid, Func<T, object> identity, Action<T[]> changed = null, ArrayDataSource<T> source = null,
		Action<VisualGrid, T[]> beforePublish = null, Func<object, T[]> readPreparedRows = null)
		: base(grid) {
		Guard.ArgumentNotNull(identity, nameof(identity));
		_identity = identity;
		_changed = changed;
		_beforePublish = beforePublish;
		_readPreparedRows = readPreparedRows;
		_source = source ?? new ArrayDataSource<T>();
		_controller = new VisualGridDataSourceController<T>(grid, _source,
			selected => _source.Data.FirstOrDefault(row => Equals(identity(row), identity(selected))));
		_controller.VisibleSelectionIdentity = identity;
	}

	public override object CaptureSelectedIdentity() => Grid.SelectedEntity is T selected ? _identity(selected) : null;

	public override void RestoreSelectedIdentity(object identity) {
		if (identity != null)
			Grid.SelectedEntity = _source.Data.FirstOrDefault(row => Equals(_identity(row), identity));
	}

	protected override async Task ApplyCoreAsync(object prepared, CancellationToken cancellationToken) {
		// A caller can retain capture metadata with its rows and present that metadata on this same UI publication.
		var rows = _readPreparedRows == null ? Guard.ArgumentCast<T[]>(prepared, nameof(prepared)) : _readPreparedRows(prepared);
		Guard.ArgumentNotNull(rows, nameof(prepared));
		_beforePublish?.Invoke(Grid, rows);
		_source.ReplaceData(rows);
		await _controller.RefreshAsync(cancellationToken);
		if (!Grid.IsDisposed && !cancellationToken.IsCancellationRequested)
			_changed?.Invoke(_source.Data);
	}
}
