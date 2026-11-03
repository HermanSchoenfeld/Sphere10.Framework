// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Extensions.PingGrids;

public class PingGridColumns : ColumnInfoCollection {
	public PingGridColumns(PingGrid grid)
		: base(grid) {
	}

	public new PingGrid Grid => (PingGrid)base.Grid;

	public new PingGridColumn this[int index] => base[index] as PingGridColumn;

	/// <summary>
	/// Return the DataColumn object for a given grid column index. Return null if not applicable, for example if the column index requested is a FixedColumns of an unbound column
	/// </summary>
	/// <param name="gridColumnIndex"></param>
	/// <returns></returns>
	[Obsolete]
	public System.ComponentModel.PropertyDescriptor IndexToPropertyColumn(int gridColumnIndex) {
		return Grid.Columns[gridColumnIndex].PropertyColumn;
	}

	/// <summary>
	/// Returns the index for a given DataColumn. -1 if not valid.
	/// </summary>
	/// <returns></returns>
	[Obsolete]
	public int DataSourceColumnToIndex(System.ComponentModel.PropertyDescriptor propertyColumn) {
		for (var index = 0; index < Grid.Columns.Count; index++) {
			if (Grid.Columns[index].PropertyColumn == propertyColumn)
				return index;
		}

		return -1;
	}

	// Add Helper methods
	public PingGridColumn Add(string property, string caption, Type propertyType) {
		var cell = SourceGrid.Extensions.PingGrids.Cells.Cell.Create(propertyType, true);

		return Add(property, caption, cell);
	}

	public PingGridColumn Add(string property, string caption, EditorBase editor) {
		var cell = new SourceGrid.Extensions.PingGrids.Cells.Cell();
		cell.Editor = editor;

		return Add(property, caption, cell);
	}

	public PingGridColumn Add(string property, string caption, ICellVirtual cell) {
		var column = new PingGridColumn(Grid, new SourceGrid.Extensions.PingGrids.Cells.ColumnHeader(caption), cell, property);
		Insert(Count, column);

		return column;
	}
}
