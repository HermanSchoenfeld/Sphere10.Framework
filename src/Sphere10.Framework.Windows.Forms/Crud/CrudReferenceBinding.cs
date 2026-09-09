// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Describes the existing records offered when editing an entity reference.</summary>
public abstract class CrudReferenceBinding {
	public const DataSourceCapabilities ReadOnlyCapabilities = DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage;

	protected CrudReferenceBinding(IEnumerable<ICrudGridColumn> GridBindings) {
		Guard.ArgumentNotNull(GridBindings, nameof(GridBindings));
		this.GridBindings = GridBindings;
	}

	public IEnumerable<ICrudGridColumn> GridBindings { get; set; }

	public bool AllowNull { get; set; } = true;

	/// <summary>The configured dropdown content size in device pixels, limited by the available screen space.</summary>
	public Size MaximumDropDownSize { get; set; } = new(760, 380);

	/// <summary>Compatibility alias for MaximumDropDownSize.</summary>
	public Size DropDownSize {
		get => MaximumDropDownSize;
		set => MaximumDropDownSize = value;
	}

	public abstract string GetDisplayText(object? Entity);

	public abstract Task BindAsync(CrudGrid Grid);

	protected void ConfigureGrid(CrudGrid Grid) {
		Guard.ArgumentNotNull(Grid, nameof(Grid));
		Grid.AllowCellEditing = false;
		Grid.RightClickForContextMenu = false;
		Grid.LeftClickToDeselect = false;
		Grid.SelectOnMouseUp = true;
		Grid.UseEntityReferenceForLookup = true;
		Grid.AutoPageSize = true;
		Grid.GridBindings = GridBindings.Where(Column => Column.DisplayType is not (CrudCellDisplayType.Button or CrudCellDisplayType.EditCommand or CrudCellDisplayType.DeleteCommand));
	}
}

public class CrudReferenceBinding<TEntity> : CrudReferenceBinding {
	public CrudReferenceBinding(IDataSource<TEntity> DataSource, IEnumerable<ICrudGridColumn> GridBindings, Func<TEntity, string>? DisplayText = null)
		: base(GridBindings) {
		Guard.ArgumentNotNull(DataSource, nameof(DataSource));
		this.DataSource = DataSource;
		this.DisplayText = DisplayText;
	}

	public IDataSource<TEntity> DataSource { get; }

	public Func<TEntity, string>? DisplayText { get; set; }

	public override string GetDisplayText(object? Entity) => Entity == null ? "(none)" : DisplayText?.Invoke((TEntity)Entity) ?? Entity.ToString() ?? string.Empty;

	public override async Task BindAsync(CrudGrid Grid) {
		ConfigureGrid(Grid);
		await Grid.SetDataSource(DataSource, ReadOnlyCapabilities);
		if (Grid.IsDisposed)
			return;
		await Grid.RefreshGrid();
	}
}
