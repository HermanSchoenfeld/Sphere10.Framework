// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.DataGrid;

public class Cell : Cells.Virtual.CellVirtual {
	public Cell() => Model.AddModel(new DataGridValueModel());

	public static Cells.ICellVirtual Create(Type type, bool editable) {
		Cells.ICellVirtual cell;

		if (type == typeof(bool))
			cell = new SourceGrid.Cells.DataGrid.CheckBox();
		else {
			cell = new SourceGrid.Cells.DataGrid.Cell();
			cell.Editor = SourceGrid.Cells.Editors.Factory.Create(type);
		}

		if (cell.Editor != null) { //Can be null for special DataType like Object
			//The columns now support always DbNull values because the validation is done at row level by the DataTable itself.
			cell.Editor.AllowNull = true;
			cell.Editor.EnableEdit = editable;
		}

		return cell;
	}
}
