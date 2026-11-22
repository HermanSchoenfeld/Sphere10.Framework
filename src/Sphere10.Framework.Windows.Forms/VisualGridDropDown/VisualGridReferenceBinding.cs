// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

public class VisualGridReferenceBinding<TEntity> : VisualGridReferenceBindingBase {
	public VisualGridReferenceBinding(IDataSource<TEntity> dataSource, IEnumerable<IVisualGridColumn> gridBindings, Func<TEntity, string> displayText = null)
		: base(gridBindings) {
		Guard.ArgumentNotNull(dataSource, nameof(dataSource));
		DataSource = dataSource;
		DisplayText = displayText;
	}

	public IDataSource<TEntity> DataSource { get; }

	public Func<TEntity, string> DisplayText { get; set; }

	public override string GetDisplayText(object entity) => entity == null ? "(none)" : DisplayText?.Invoke((TEntity)entity) ?? entity.ToString() ?? string.Empty;

	public override async Task BindAsync(VisualGrid grid) {
		ConfigureGrid(grid);
		await grid.SetDataSource(DataSource, ReadOnlyCapabilities);
		if (grid.IsDisposed)
			return;
		await grid.RefreshGrid();
	}
}
