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

/// <summary>
/// A ColumnInfo derived class used to store column informations for a DataGrid control.
/// Mantains the cell used on this grid and manage the binding to the DataSource using a DataGridValueModel class.
/// </summary>
public class DataGridColumn : ColumnInfo {
	private string _propertyName;
	private List<Conditions.ICondition> _conditions = new List<Conditions.ICondition>();

	private Dictionary<Conditions.ICondition, Cells.ICellVirtual> _conditionalCells = new Dictionary<Conditions.ICondition, Cells.ICellVirtual>();

	/// <summary>
	/// Constructor. Create a DataGridColumn class.
	/// </summary>
	/// <param name="grid"></param>
	public DataGridColumn(DataGrid grid)
		: base(grid) {
		HeaderCell = new Cells.DataGrid.ColumnHeader(string.Empty);
		DataCell = new Cells.DataGrid.Cell();
	}

	/// <summary>
	/// Constructor. Create a DataGridColumn class.
	/// </summary>
	public DataGridColumn(DataGrid grid, Cells.ICellVirtual headerCell, Cells.ICellVirtual dataCell, string propertyName)
		: base(grid) {
		_propertyName = propertyName;
		HeaderCell = headerCell;
		DataCell = dataCell;
	}

	public new DataGrid Grid => (DataGrid)base.Grid;

	public string PropertyName {
		get {
			return _propertyName;
		}
		set {
			_propertyName = value;
			PropertyColumn = null;
		}
	}

	/// <summary>
	/// Gets the property column. Can be null if not bound to a datasource Column.
	/// This field is used for example to support sorting.
	/// </summary>
	public PropertyDescriptor PropertyColumn {
		get {
			if (field == null && Grid.DataSource != null)
				field = Grid.DataSource.GetItemProperty(PropertyName, StringComparison.InvariantCultureIgnoreCase);

			return field;
		}
		private set {
			field = value;
		}
	}

	/// <summary>
	/// Gets or sets the header cell for this column.
	/// Typically is an instance of SourceGrid.Cells.DataGrid.ColumnHeader
	/// </summary>
	public Cells.ICellVirtual HeaderCell { get; set; }

	/// <summary>
	/// Gets or sets the cell used for this column for all the rows to disply the data
	/// Typically is an instance of SourceGrid.Cells.DataGrid.Cell or other classes of the same namespace
	/// </summary>
	public Cells.ICellVirtual DataCell { get; set; }

	/// <summary>
	/// Gets the conditions used to returns different cell based on the data of the row.
	/// </summary>
	public List<Conditions.ICondition> Conditions => _conditions;

	/// <summary>
	/// Gets the ICellVirtual for the current column and the specified row.
	/// Override this method to provide custom cells, based on the row informations.
	/// </summary>
	/// <param name="gridRow"></param>
	/// <returns></returns>
	public virtual Cells.ICellVirtual GetDataCell(int gridRow) {
		var itemRow = Grid.Rows.IndexToDataSourceRow(gridRow);

		foreach (var condition in Conditions) {
			if (condition.Evaluate(this, gridRow, itemRow)) {
				Cells.ICellVirtual cell;
				if (_conditionalCells.TryGetValue(condition, out cell) == false) {
					cell = condition.ApplyCondition(DataCell);
					_conditionalCells.Add(condition, cell);
				}

				return cell;
			}
		}

		return DataCell;
	}

	/// <summary>
	/// Create a DataGridColumn with special cells used for RowHeader, usually used when FixedColumns is 1 for the first column.
	/// </summary>
	/// <param name="grid"></param>
	/// <returns></returns>
	public static DataGridColumn CreateRowHeader(DataGrid grid) {
		return new DataGridColumn(grid, new Cells.DataGrid.Header(), new Cells.DataGrid.RowHeader(), null);
	}

	/// <summary>
	/// Clears any associated data with DataGridView.
	/// <remarks>PropertyColumn binds to DataTable, calling invalidate will remove
	/// this link</remarks>
	/// </summary>
	public void Invalidate() {
		PropertyColumn = null;
	}
}
