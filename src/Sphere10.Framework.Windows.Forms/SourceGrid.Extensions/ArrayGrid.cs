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

	private ICellVirtual _columnHeader = new ArrayColumnHeader();

	private ICellVirtual _rowHeader = new ArrayRowHeader();

	private ICellVirtual _header = new ArrayHeader();

	private ICellVirtual _valueCell;

	public ArrayGrid()
		: base() {
	}

	public override bool EnableSort {
		get; set;
	}

	[System.ComponentModel.Browsable(false)]
	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public new ArrayRows Rows {
		get {
			return (ArrayRows)base.Rows;
		}
	}

	[System.ComponentModel.Browsable(false)]
	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public new ArrayColumns Columns {
		get {
			return (ArrayColumns)base.Columns;
		}
	}

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
	public ICellVirtual ColumnHeader {
		get {
			return _columnHeader;
		}
		set {
			_columnHeader = value;
		}
	}

	/// <summary>
	/// Gets or sets the cell used for the row headers. Only used when FixedColumns is greater than 0.
	/// </summary>
	[System.ComponentModel.Browsable(false)]
	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public ICellVirtual RowHeader {
		get {
			return _rowHeader;
		}
		set {
			_rowHeader = value;
		}
	}

	/// <summary>
	/// Gets or sets the cell used for the left top position header. Only used when FixedRows and FixedColumns are greater than 0.
	/// </summary>
	[System.ComponentModel.Browsable(false)]
	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public ICellVirtual Header {
		get {
			return _header;
		}
		set {
			_header = value;
		}
	}

	[System.ComponentModel.Browsable(false)]
	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public ICellVirtual ValueCell {
		get {
			return _valueCell;
		}
		set {
			_valueCell = value;
		}
	}

	public override ICellVirtual GetCell(int row, int column) {
		if (row < FixedRows && column < FixedColumns)
			return _header;
		if (row < FixedRows)
			return _columnHeader;
		if (column < FixedColumns)
			return _rowHeader;
		return _valueCell;
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
			_valueCell = new SourceGrid.Cells.Virtual.CellVirtual();
			_valueCell.Model.AddModel(new ArrayValueModel());
			_valueCell.Editor = Cells.Editors.Factory.Create(_dataSource.GetType().GetElementType());
		}

		Rows.RowsChanged();
		Columns.ColumnsChanged();
	}
}
