// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells;

/// <summary>
/// Represents a Cell in a grid, with Cell.Value support and row/col span. Support also ToolTipText, ContextMenu and Cursor
/// </summary>
public class Cell : Virtual.CellVirtual, ICell {
	private Grid _grid;
	private GridColumn _column;
	private GridRow _row;

	/// <summary>
	/// Object to put additional info for this cell
	/// </summary>
	private object _tag = null;
	private int _columnSpan = 1;
	private int _rowSpan = 1;

	// Constructors
	/// <summary>
	/// Constructor
	/// </summary>
	public Cell()
		: this(null) {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="cellValue"></param>
	public Cell(object cellValue) {
		Model = new SourceGrid.Cells.Models.ModelContainer();
		Model.ValueModel = new Models.ValueModel();

		Model.AddModel(new Models.ToolTip());
		Model.AddModel(new Models.Image());

		Value = cellValue;
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="cellValue"></param>
	/// <param name="type"></param>
	public Cell(object cellValue, Type type)
		: this(cellValue) {
		Editor = Editors.Factory.Create(type);
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="cellValue"></param>
	/// <param name="editor"></param>
	public Cell(object cellValue, Editors.EditorBase editor)
		: this(cellValue) {
		Editor = editor;
	}

	/// <summary>
	/// The Grid object
	/// </summary>
	public Grid Grid {
		get {
			return _grid;
		}
	}

	/// <summary>
	/// Gets the column of the specified cell
	/// </summary>
	public GridColumn Column {
		get {
			return _column;
		}
	}

	/// <summary>
	/// Gets the row of the specified cell
	/// </summary>
	public GridRow Row {
		get {
			return _row;
		}
	}

	/// <summary>
	/// Gets the range of the cell
	/// </summary>
	public CellRange Range {
		get {
			if (Grid == null)
				return CellRange.Empty;

			var column = Column.Index;
			var row = Row.Index;
			return new CellRange(row, column, row + RowSpan - 1, column + ColumnSpan - 1);
		}
	}

	// Cell Data (Value, DisplayText, Tag)
	//ATTENTION: is reccomanded that all the actions fired by the user interface does not modify this property
	// instead call the CellEditor.ChangeCellValue to preserve data consistence

	/// <summary>
	/// The string representation of the Cell.Value property (default Value.ToString())
	/// </summary>
	public virtual string DisplayText {
		get {
			return GetContext().DisplayText;
		}
	}

	/// <summary>
	/// Value of the cell
	/// </summary>
	public virtual object Value {
		get {
			return Model.ValueModel.GetValue(GetContext());
		}
		set {
			Model.ValueModel.SetValue(GetContext(), value);
		}
	}

	/// <summary>
	/// Object to put additional info for this cell
	/// </summary>
	public virtual object Tag {
		get {
			return _tag;
		}
		set {
			_tag = value;
		}
	}

	/// <summary>
	/// ColSpan for merge operation
	/// </summary>
	public int ColumnSpan {
		get {
			return _columnSpan;
		}
		set {
			if (value < 1)
				throw new ArgumentOutOfRangeException("ColumnSpan");

			SetSpan(this.RowSpan, value);
		}
	}

	/// <summary>
	/// RowSpan for merge operation
	/// </summary>
	public int RowSpan {
		get {
			return _rowSpan;
		}
		set {
			if (value < 1)
				throw new ArgumentOutOfRangeException("RowSpan");

			SetSpan(value, this.ColumnSpan);
		}
	}

	/// <summary>
	/// Gets or sets the tool tip text of the cell. Internally use the Models.ToolTip class.
	/// </summary>
	public string ToolTipText {
		get {
			return ToolTipModel.ToolTipText;
		}
		set {
			ToolTipModel.ToolTipText = value;
		}
	}

	/// <summary>
	/// Gets or sets the Image associeted with the Cell. Internally use a Models.Image class.
	/// </summary>
	public System.Drawing.Image Image {
		get {
			return ImageModel.ImageValue;
		}
		set {
			ImageModel.ImageValue = value;
		}
	}

	// ToolTipText
	private Models.ToolTip ToolTipModel {
		get {
			return (Models.ToolTip)Model.FindModel(typeof(Models.ToolTip));
		}
	}

	// Image
	private Models.Image ImageModel {
		get {
			return (Models.Image)Model.FindModel(typeof(Models.Image));
		}
	}

	// LinkToGrid
	/// <summary>
	/// Link the cell at the specified grid.
	/// For internal use only.
	/// </summary>
	/// <param name="grid"></param>
	/// <param name="position"></param>
	public virtual void BindToGrid(Grid grid, Position position) {
		_grid = grid;
		_row = Grid.Rows[position.Row];
		_column = Grid.Columns[position.Column] as GridColumn;
	}

	/// <summary>
	/// Remove the link of the cell from the grid.
	/// For internal use only.
	/// </summary>
	public virtual void UnBindToGrid() {
		_grid = null;
		_column = null;
		_row = null;
	}

	/// <summary>
	/// ToString method
	/// </summary>
	/// <returns></returns>
	public override string ToString() {
		return DisplayText;
	}

	// Row/Col Span
	public void SetSpan(int rowSpan, int colSpan) {
		var oldColSpan = ColumnSpan;
		var oldRowSpan = _rowSpan;

		try {
			var update = false;
			if ((_columnSpan > 1) || (_rowSpan > 1))
				update = true;
			_columnSpan = colSpan;
			_rowSpan = rowSpan;

			if (_grid != null) {
				if (_columnSpan != 1 || _rowSpan != 1) {
					if (update)
						_grid.UpdateSpannedArea(this.Row.Index, this.Column.Index, this);
					else
						_grid.OccupySpannedArea(this.Row.Index, this.Column.Index, this);
				}
			}
		} catch (OverlappingCellException exception) {
			_columnSpan = oldColSpan;
			_rowSpan = oldRowSpan;
			throw new OverlappingCellException("Can not change span", exception);
		}
	}

	protected CellContext GetContext() {
		return new CellContext(Grid, Range.Start, this);
	}
}
