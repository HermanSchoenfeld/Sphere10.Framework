// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Virtual;

/// <summary>
/// A cell that rappresent a header of a table, with 3D effect. This cell override IsSelectable to false. Default use VisualModels.VisualModelHeader.Style1
/// </summary>
public class ColumnHeader : CellVirtual {
	/// <summary>
	/// Constructor
	/// </summary>
	public ColumnHeader() {
		View = Views.ColumnHeader.Default;
		Model.AddModel(new Models.SortableHeader());
		AddController(Controllers.Unselectable.Default);
		AddController(Controllers.MouseInvalidate.Default);
		ResizeEnabled = true;
		AutomaticSortEnabled = true;
	}

	/// <summary>
	/// Gets or sets if enable the resize of the width of the column. This property internally use the Controllers.Resizable.ResizeWidth.
	/// </summary>
	public bool ResizeEnabled {
		get {
			return FindController(typeof(Controllers.Resizable)) == Controllers.Resizable.ResizeWidth;
		}
		set {
			if (value == ResizeEnabled)
				return;

			if (value)
				AddController(Controllers.Resizable.ResizeWidth);
			else
				RemoveController(Controllers.Resizable.ResizeWidth);
		}
	}

	/// <summary>
	/// Gets or sets if enable the automatic sort features of the column. This property internally use the Controllers.SortableHeader.Default.
	/// </summary>
	public bool AutomaticSortEnabled {
		get {
			return FindController(typeof(Controllers.SortableHeader)) == Controllers.SortableHeader.Default;
		}
		set {
			if (value == AutomaticSortEnabled)
				return;

			if (value)
				AddController(Controllers.SortableHeader.Default);
			else
				RemoveController(Controllers.SortableHeader.Default);
		}
	}
}
