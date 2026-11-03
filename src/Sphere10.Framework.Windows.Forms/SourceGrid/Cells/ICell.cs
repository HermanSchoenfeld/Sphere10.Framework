// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells;

/// <summary>
/// Represents a Cell to use with Grid control.
/// </summary>
public interface ICell : ICellVirtual {
	// Value, DisplayText, ToolTipText, Image, Tag
	/// <summary>
	/// Gets the string representation of the Cell.Value property (default Value.ToString())
	/// </summary>
	string DisplayText {
		get;
	}

	/// <summary>
	/// Gets or sets the value of the cell
	/// </summary>
	object Value {
		get; set;
	}

	/// <summary>
	/// Gets or sets additional info for this cell
	/// </summary>
	object Tag {
		get; set;
	}

	/// <summary>
	/// Gets or sets the ToolTipText
	/// </summary>
	string ToolTipText {
		get; set;
	}

	/// <summary>
	/// Gets or set the image of the cell.
	/// </summary>
	System.Drawing.Image Image {
		get; set;
	}

	// LinkToGrid
	/// <summary>
	/// The Grid object
	/// </summary>
	Grid Grid {
		get;
	}

	/// <summary>
	/// Gets the column of the specified cell
	/// </summary>
	GridColumn Column {
		get;
	}

	/// <summary>
	/// Gets the row of the specified cell
	/// </summary>
	GridRow Row {
		get;
	}

	/// <summary>
	/// Gets the range of the cell
	/// </summary>
	CellRange Range {
		get;
	}

	// Row/Col Span
	/// <summary>
	/// ColSpan for merge operation, calculated using the current range.
	/// </summary>
	int ColumnSpan {
		get; set;
	}

	/// <summary>
	/// RowSpan for merge operation, calculated using the current range.
	/// </summary>
	int RowSpan {
		get; set;
	}

	/// <summary>
	/// Link the cell at the specified grid.
	/// For internal use only.
	/// </summary>
	/// <param name="grid"></param>
	/// <param name="position"></param>
	void BindToGrid(Grid grid, Position position);

	/// <summary>
	/// Remove the link of the cell from the grid.
	/// For internal use only.
	/// </summary>
	void UnBindToGrid();

	/// <summary>
	/// Setting a col/row spann is a costly operation, so it's better
	/// if you set these two at the same time.
	/// Prefer this method to <c>RowSpan</c> and <c>ColSpan</c> property setters
	/// </summary>
	/// <param name="rowSpan"></param>
	/// <param name="colSpan"></param>
	void SetSpan(int rowSpan, int colSpan);
}
