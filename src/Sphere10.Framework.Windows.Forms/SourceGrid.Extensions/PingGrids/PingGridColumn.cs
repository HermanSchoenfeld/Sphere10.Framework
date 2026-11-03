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

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Extensions.PingGrids;

/// <summary>
/// A ColumnInfo derived class used to store column informations for a PingGrid control.
/// Mantains the cell used on this grid and manage the binding to the DataSource using a DataGridValueModel class.
/// </summary>
public class PingGridColumn : ColumnInfo {
	[Obsolete] private PropertyDescriptor _propertyColumn = null;
	[Obsolete] private List<Conditions.ICondition> _conditions = new List<Conditions.ICondition>();

	[Obsolete] private Dictionary<Conditions.ICondition, ICellVirtual> _conditionalCells = new Dictionary<Conditions.ICondition, ICellVirtual>();

	/// <summary>
	/// Constructor. Create a DataGridColumn class.
	/// </summary>
	/// <param name="grid"></param>
	public PingGridColumn(PingGrid grid)
		: base(grid) {
		HeaderCell = new SourceGrid.Extensions.PingGrids.Cells.ColumnHeader(string.Empty);
		DataCell = new SourceGrid.Extensions.PingGrids.Cells.Cell();
	}

	/// <summary>
	/// Constructor. Create a DataGridColumn class.
	/// </summary>
	public PingGridColumn(PingGrid grid, ICellVirtual headerCell, ICellVirtual dataCell, string propertyName)
		: base(grid) {
		PropertyName = propertyName;
		HeaderCell = headerCell;
		DataCell = dataCell;
	}

	public new PingGrid Grid => (PingGrid)base.Grid;

	public string PropertyName {
		get {
			return field;
		}
		set {
			field = value;
			//mPropertyColumn = null;
		}
	}

	/// <summary>
	/// Gets the property column. Can be null if not bound to a datasource Column.
	/// This field is used for example to support sorting.
	/// </summary>
	[Obsolete]
	public PropertyDescriptor PropertyColumn {
		get {
			//if (mPropertyColumn == null && Grid.DataSource != null)
			//	mPropertyColumn = Grid.DataSource.GetItemProperty(PropertyName, StringComparison.InvariantCultureIgnoreCase);

			return _propertyColumn;
		}
	}

	/// <summary>
	/// Gets or sets the header cell for this column.
	/// Typically is an instance of SourceGrid.Cells.DataGrid.ColumnHeader
	/// </summary>
	public ICellVirtual HeaderCell { get; set; }

	/// <summary>
	/// Gets or sets the cell used for this column for all the rows to disply the data
	/// Typically is an instance of SourceGrid.Cells.DataGrid.Cell or other classes of the same namespace
	/// </summary>
	public ICellVirtual DataCell { get; set; }

	/// <summary>
	/// Gets the conditions used to returns different cell based on the data of the row.
	/// </summary>
	[Obsolete]
	public List<Conditions.ICondition> Conditions => _conditions;

	/// <summary>
	/// Gets the ICellVirtual for the current column and the specified row.
	/// Override this method to provide custom cells, based on the row informations.
	/// </summary>
	/// <param name="gridRow"></param>
	/// <returns></returns>
	public virtual ICellVirtual GetDataCell(int gridRow) {
		//object itemRow = Grid.Rows.IndexToDataSourceRow(gridRow);

		//Cells.ICellVirtual cell;
		//if (mConditionalCells.TryGetValue(con, out cell) == false)
		//{
		//	cell = con.ApplyCondition(DataCell);
		//	mConditionalCells.Add(con, cell);
		//}
		//
		//return cell;

		return DataCell;
	}

	/// <summary>
	/// Create a DataGridColumn with special cells used for RowHeader, usually used when FixedColumns is 1 for the first column.
	/// </summary>
	/// <param name="grid"></param>
	/// <returns></returns>
	public static PingGridColumn CreateRowHeader(PingGrid grid) {
		return new PingGridColumn(grid, new SourceGrid.Extensions.PingGrids.Cells.Header(), new SourceGrid.Extensions.PingGrids.Cells.RowHeader(), null);
	}

	/// <summary>
	/// Clears any associated data with DataGridView.
	/// <remarks>PropertyColumn binds to DataTable, calling invalidate will remove
	/// this link</remarks>
	/// </summary>
	[Obsolete]
	public void Invalidate() {
		//mPropertyColumn = null;
	}
}
