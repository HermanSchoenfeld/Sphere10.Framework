// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

public class DataGridColumns : ColumnInfoCollection {
	public DataGridColumns(DataGrid grid)
		: base(grid) {
	}

	public new DataGrid Grid {
		get {
			return (DataGrid)base.Grid;
		}
	}

	public new DataGridColumn this[int index] {
		get {
			return base[index] as DataGridColumn;
		}
	}

	/// <summary>
	/// Return the DataColumn object for a given grid column index. Return null if not applicable, for example if the column index requested is a FixedColumns of an unbound column
	/// </summary>
	/// <param name="gridColumnIndex"></param>
	/// <returns></returns>
	public System.ComponentModel.PropertyDescriptor IndexToPropertyColumn(int gridColumnIndex) {
		return Grid.Columns[gridColumnIndex].PropertyColumn;
	}

	/// <summary>
	/// Returns the index for a given DataColumn. -1 if not valid.
	/// </summary>
	/// <returns></returns>
	public int DataSourceColumnToIndex(System.ComponentModel.PropertyDescriptor propertyColumn) {
		for (var index = 0; index < Grid.Columns.Count; index++) {
			if (Grid.Columns[index].PropertyColumn == propertyColumn)
				return index;
		}

		return -1;
	}

	// Add Helper methods
	public DataGridColumn Add(string property, string caption, Type propertyType) {
		var cell = SourceGrid.Cells.DataGrid.Cell.Create(propertyType, true);

		return Add(property, caption, cell);
	}

	public DataGridColumn Add(string property, string caption, EditorBase editor) {
		var cell = new SourceGrid.Cells.DataGrid.Cell();
		cell.Editor = editor;

		return Add(property, caption, cell);
	}

	public DataGridColumn Add(string property, string caption, ICellVirtual cell) {
		var column = new DataGridColumn(Grid, new SourceGrid.Cells.DataGrid.ColumnHeader(caption), cell, property);
		Insert(Count, column);

		return column;
	}
}
