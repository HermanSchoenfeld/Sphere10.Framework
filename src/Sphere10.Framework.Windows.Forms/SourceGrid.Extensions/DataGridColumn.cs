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

	private PropertyDescriptor _propertyColumn;

	private Cells.ICellVirtual _headerCell;

	private Cells.ICellVirtual _dataCell;

	private List<Conditions.ICondition> _conditions = new List<Conditions.ICondition>();

	private Dictionary<Conditions.ICondition, Cells.ICellVirtual> _conditionalCells = new Dictionary<Conditions.ICondition, Cells.ICellVirtual>();

	/// <summary>
	/// Constructor. Create a DataGridColumn class.
	/// </summary>
	/// <param name="grid"></param>
	public DataGridColumn(DataGrid grid)
		: base(grid) {
		_headerCell = new Cells.DataGrid.ColumnHeader(string.Empty);
		_dataCell = new Cells.DataGrid.Cell();
	}

	/// <summary>
	/// Constructor. Create a DataGridColumn class.
	/// </summary>
	public DataGridColumn(DataGrid grid, Cells.ICellVirtual headerCell, Cells.ICellVirtual dataCell, string propertyName)
		: base(grid) {
		_propertyName = propertyName;
		_headerCell = headerCell;
		_dataCell = dataCell;
	}

	public new DataGrid Grid {
		get => (DataGrid)base.Grid;
	}

	public string PropertyName {
		get => _propertyName;
		set {
			_propertyName = value;
			_propertyColumn = null;
		}
	}

	/// <summary>
	/// Gets the property column. Can be null if not bound to a datasource Column.
	/// This field is used for example to support sorting.
	/// </summary>
	public PropertyDescriptor PropertyColumn {
		get {
			if (_propertyColumn == null && Grid.DataSource != null)
				_propertyColumn = Grid.DataSource.GetItemProperty(PropertyName, StringComparison.InvariantCultureIgnoreCase);

			return _propertyColumn;
		}
	}

	/// <summary>
	/// Gets or sets the header cell for this column.
	/// Typically is an instance of SourceGrid.Cells.DataGrid.ColumnHeader
	/// </summary>
	public Cells.ICellVirtual HeaderCell {
		get => _headerCell;
		set => _headerCell = value;
	}

	/// <summary>
	/// Gets or sets the cell used for this column for all the rows to disply the data
	/// Typically is an instance of SourceGrid.Cells.DataGrid.Cell or other classes of the same namespace
	/// </summary>
	public Cells.ICellVirtual DataCell {
		get => _dataCell;
		set => _dataCell = value;
	}

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
	public static DataGridColumn CreateRowHeader(DataGrid grid) => new DataGridColumn(grid, new Cells.DataGrid.Header(), new Cells.DataGrid.RowHeader(), null);

	/// <summary>
	/// Clears any associated data with DataGridView.
	/// <remarks>PropertyColumn binds to DataTable, calling invalidate will remove
	/// this link</remarks>
	/// </summary>
	public void Invalidate() => _propertyColumn = null;
}
