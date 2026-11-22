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
public abstract class VisualGridReferenceBindingBase {
	public const DataSourceCapabilities ReadOnlyCapabilities = DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage;

	protected VisualGridReferenceBindingBase(IEnumerable<IVisualGridColumn> gridBindings) {
		Guard.ArgumentNotNull(gridBindings, nameof(gridBindings));
		GridBindings = gridBindings.ToArray();
	}

	public IVisualGridColumn[] GridBindings { get; set; }

	public bool AllowNull { get; set; } = true;

	/// <summary>The configured dropdown content size in device pixels, limited by the available screen space.</summary>
	public Size MaximumDropDownSize { get; set; } = new(760, 380);

	/// <summary>Compatibility alias for MaximumDropDownSize.</summary>
	public Size DropDownSize {
		get => MaximumDropDownSize;
		set => MaximumDropDownSize = value;
	}

	public abstract string GetDisplayText(object entity);

	public abstract Task BindAsync(VisualGrid grid);

	protected void ConfigureGrid(VisualGrid grid) {
		Guard.ArgumentNotNull(grid, nameof(grid));
		grid.AllowCellEditing = false;
		grid.RightClickForContextMenu = false;
		grid.LeftClickToDeselect = false;
		grid.SelectOnMouseUp = true;
		grid.UseEntityReferenceForLookup = true;
		grid.AutoPageSize = true;
		grid.GridBindings = GridBindings.Where(column => column.DisplayType is not (VisualGridCellDisplayType.Button or VisualGridCellDisplayType.EditCommand or VisualGridCellDisplayType.DeleteCommand)).ToArray();
	}
}
