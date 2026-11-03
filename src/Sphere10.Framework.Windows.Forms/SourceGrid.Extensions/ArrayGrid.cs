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
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Models;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// This class derive from GridVirtual and create a grid bound to an array.
/// </summary>
[System.ComponentModel.ToolboxItem(true)]
public class ArrayGrid : GridVirtual {
	private Array _dataSource = null;

	public ArrayGrid()
		: base() {
	}

	public override bool EnableSort {
		get; set;
	}

	[System.ComponentModel.Browsable(false)]
	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public new ArrayRows Rows => (ArrayRows)base.Rows;

	[System.ComponentModel.Browsable(false)]
	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public new ArrayColumns Columns => (ArrayColumns)base.Columns;

	/// <summary>
	/// Gets or sets the data source array used to bind the grid.
	/// </summary>
	[System.ComponentModel.Browsable(false)]
	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public Array DataSource {
		get {
			return _dataSource;
		}
		set {
			if (value != null && value.Rank != 2)
				throw new SourceGridException("Array dimension not valid, must be an array with 2 dimensions");

			_dataSource = value;
			Bind();
		}
	}

	/// <summary>
	/// Gets or sets the cell used for the column headers.  Only used when FixedRows is greater than 0.
	/// </summary>
	[System.ComponentModel.Browsable(false)]
	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public ICellVirtual ColumnHeader { get; set; } = new ArrayColumnHeader();

	/// <summary>
	/// Gets or sets the cell used for the row headers. Only used when FixedColumns is greater than 0.
	/// </summary>
	[System.ComponentModel.Browsable(false)]
	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public ICellVirtual RowHeader { get; set; } = new ArrayRowHeader();

	/// <summary>
	/// Gets or sets the cell used for the left top position header. Only used when FixedRows and FixedColumns are greater than 0.
	/// </summary>
	[System.ComponentModel.Browsable(false)]
	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public ICellVirtual Header { get; set; } = new ArrayHeader();

	[System.ComponentModel.Browsable(false)]
	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public ICellVirtual ValueCell { get; set; }

	public override ICellVirtual GetCell(int row, int column) {
		if (row < FixedRows && column < FixedColumns)
			return Header;
		if (row < FixedRows)
			return ColumnHeader;
		if (column < FixedColumns)
			return RowHeader;
		return ValueCell;
	}

	protected override RowsBase CreateRowsObject() {
		return new ArrayRows(this);
	}

	protected override ColumnsBase CreateColumnsObject() {
		return new ArrayColumns(this);
	}

	protected virtual void Bind() {
		ValueCell = null;
		if (_dataSource != null) {
			ValueCell = new SourceGrid.Cells.Virtual.CellVirtual();
			ValueCell.Model.AddModel(new ArrayValueModel());
			ValueCell.Editor = Cells.Editors.Factory.Create(_dataSource.GetType().GetElementType());
		}

		Rows.RowsChanged();
		Columns.ColumnsChanged();
	}
}
