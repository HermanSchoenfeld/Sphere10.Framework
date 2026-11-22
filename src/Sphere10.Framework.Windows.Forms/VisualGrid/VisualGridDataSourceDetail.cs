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

/// <summary>Displays a hierarchy's child query in an owned VisualGrid.</summary>
/// <remarks>
/// Return this adapter from the parent grid's <see cref="VisualGrid.CreateDetail"/> callback to show a nested table.
/// Each update binds <see cref="VisualGridDetailContext.ChildDataSource"/>, invokes the optional presentation callback
/// and refreshes the table. Selection is retained by node key across updates. The base adapter owns the grid and applied context.
/// An update that fails or is canceled returns the table to the context it applied last, so it never reads a released child source.
/// </remarks>
public sealed class VisualGridDataSourceDetail : VisualGridDetailBase {
	private readonly Action<VisualGrid, VisualGridDetailContext> _beforePublish;
	private VisualGridDetailContext _context;

	public VisualGridDataSourceDetail(VisualGrid grid, Action<VisualGrid, VisualGridDetailContext> beforePublish = null)
		: base(grid) => _beforePublish = beforePublish;

	public override object CaptureSelectedIdentity()
		=> !Grid.HasDetailSelection && Grid.SelectedEntity is { } selected ? _context?.ChildDataSource.GetNodeKey(selected) : null;

	public override void RestoreSelectedIdentity(object identity) {
		if (identity != null && _context != null)
			Grid.SelectedEntity = Grid.VisibleEntities.FirstOrDefault(node => Equals(_context.ChildDataSource.GetNodeKey(node), identity));
	}

	protected override async Task ApplyCoreAsync(object prepared, CancellationToken cancellationToken) {
		var context = Guard.ArgumentCast<VisualGridDetailContext>(prepared, nameof(prepared));
		Guard.ArgumentNotNull(context.ChildDataSource, nameof(context.ChildDataSource));
		var selected = CaptureSelectedIdentity();
		var previous = _context;
		Bind(context);
		try {
			await Grid.RefreshDataSourceAsync(cancellationToken);
		} catch {
			// The base adapter releases the context of a failed update, so the table returns to the one it still owns.
			if (!Grid.IsDisposed && !Grid.Disposing)
				Bind(previous);
			throw;
		}
		RestoreSelectedIdentity(selected);
	}

	private void Bind(VisualGridDetailContext context) {
		_context = context;
		if (context == null) {
			Grid.ClearDataSource();
			return;
		}
		Grid.ConfigureDataSource(context.ChildDataSource, visibleSelectionIdentity: context.ChildDataSource.GetNodeKey);
		_beforePublish?.Invoke(Grid, context);
	}
}
