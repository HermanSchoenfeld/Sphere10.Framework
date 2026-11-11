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
public class RowHeader : CellVirtual {
	/// <summary>
	/// Constructor
	/// </summary>
	public RowHeader() {
		View = Views.RowHeader.Default;
		AddController(Controllers.Unselectable.Default);
		AddController(Controllers.MouseInvalidate.Default);
		ResizeEnabled = true;
	}

	/// <summary>
	/// Gets or sets if enable the resize of the height, using a Resizable controller. Default is true.
	/// </summary>
	public bool ResizeEnabled {
		get => FindController(typeof(Controllers.Resizable)) == Controllers.Resizable.ResizeHeight;
		set {
			if (value == ResizeEnabled)
				return;

			if (value)
				AddController(Controllers.Resizable.ResizeHeight);
			else
				RemoveController(Controllers.Resizable.ResizeHeight);
		}
	}
}
