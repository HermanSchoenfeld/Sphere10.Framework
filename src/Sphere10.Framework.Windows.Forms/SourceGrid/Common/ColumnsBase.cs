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
using System.Drawing;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// Abstract base class for manage columns informations.
/// </summary>
public abstract class ColumnsBase {
	private GridVirtual _grid;

	// Layout
	private int _suspendedCount = 0;

	public ColumnsBase(GridVirtual grid) => _grid = grid;

	public GridVirtual Grid => _grid;

	// Abstract methods
	public abstract int Count {
		get;
	}

	/// <summary>
	/// Returns the first visible scrollable column.
	/// Return null if there isn't a visible column.
	/// </summary>
	/// <returns></returns>
	public int? FirstVisibleScrollableColumn {
		get {
			var firstVisible = Grid.CustomScrollPosition.X + Grid.FixedColumns;

			if (firstVisible >= Count)
				return null;
			return firstVisible;
		}
	}

	/// <summary>
	/// Returns the last visible scrollable column.
	/// Return null if there isn't a visible column.
	/// </summary>
	/// <returns></returns>
	public int? LastVisibleScrollableColumn {
		get {
			var first = FirstVisibleScrollableColumn;
			if (first == null)
				return null;

			var scrollableArea = Grid.GetScrollableArea();

			var right = GetLeft(first.Value);
			var c = first.Value;
			for (; c < Count; c++) {
				right += GetWidth(c);

				if (right >= scrollableArea.Right)
					return c;
			}

			return c - 1;
		}
	}

	public void AutoSize(bool useRowHeight) {
		SuspendLayout();
		for (var index = 0; index < Count; index++)
			AutoSizeColumn(index, useRowHeight);

		ResumeLayout();
	}

	/// <summary>
	/// Auto size all the columns with the max required width of all cells.
	/// </summary>
	/// <param name="useRowHeight">True to fix the row height when measure the column width.</param>
	/// <param name="startRow">Start row to measure</param>
	/// <param name="endRow">End row to measure</param>
	public void AutoSize(bool useRowHeight, int startRow, int endRow) {
		SuspendLayout();
		for (var index = 0; index < Count; index++)
			AutoSizeColumn(index, useRowHeight, startRow, endRow);

		ResumeLayout();
	}

	/// <summary>
	/// Autosize column using default auto size mode
	/// </summary>
	/// <param name="column"></param>
	public void AutoSizeColumn(int column) => AutoSizeColumn(column, true);

	/// <summary>
	/// Autosize column using default auto size mode
	/// </summary>
	public void AutoSizeColumn(int column, bool useRowHeight) {
		var minRow = 0;
		var maxRow = Grid.Rows.Count - 1;

		if ((GetAutoSizeMode(column) & AutoSizeMode.EnableAutoSizeView) == AutoSizeMode.EnableAutoSizeView) {
			var isColumnVisible = this.Grid.GetVisibleColumns(true).Contains(column);
			if (isColumnVisible == false)
				return;
			var visibleRows = Grid.GetVisibleRows(true);
			visibleRows.Sort();
			if (visibleRows.Count == 0)
				return;
			minRow = visibleRows[0];
			maxRow = visibleRows[visibleRows.Count - 1];
		}
		AutoSizeColumn(column, useRowHeight, minRow, maxRow);
	}

	public void AutoSizeColumn(int column, bool useRowHeight, int startRow, int endRow) {
		if ((GetAutoSizeMode(column) & AutoSizeMode.EnableAutoSize) == AutoSizeMode.EnableAutoSize &&
			IsColumnVisible(column))
			SetWidth(column, MeasureColumnWidth(column, useRowHeight, startRow, endRow));
	}

	/// <summary>
	/// Measures the current column when drawn with the specified cells.
	/// </summary>
	/// <param name="column"></param>
	/// <param name="useRowHeight">True to fix the row height when measure the column width.</param>
	/// <param name="startRow">Start row to measure</param>
	/// <param name="endRow">End row to measure</param>
	/// <returns>Returns the required width</returns>
	public int MeasureColumnWidth(int column, bool useRowHeight, int startRow, int endRow) {
		var min = Grid.MinimumWidth;

		if ((GetAutoSizeMode(column) & AutoSizeMode.MinimumSize) == AutoSizeMode.MinimumSize)
			return min;

		for (var r = startRow; r <= endRow; r++) {
			var cell = Grid.GetCell(r, column);
			if (cell != null) {
				var cellPosition = new Position(r, column);

				var maxLayout = Size.Empty;
				//Use the width of the actual cell (considering spanned cells)
				if (useRowHeight)
					maxLayout.Height = Grid.RangeToSize(Grid.PositionToCellRange(cellPosition)).Height;

				var cellContext = new CellContext(Grid, cellPosition, cell);
				var cellSize = cellContext.Measure(maxLayout);
				if (cellSize.Width > min)
					min = cellSize.Width;
			}
		}
		return min;
	}

	/// <summary>
	/// stretch the columns width to always fit the available space when the contents of the cell is smaller.
	/// </summary>
	public virtual void StretchToFit() {
		SuspendLayout();

		var displayRect = Grid.DisplayRectangle;

		if (Count > 0 && displayRect.Width > 0) {
			var visibleIndex = ColumnsInsideRegion(displayRect.X, displayRect.Width);

			//Continue only if the columns are all visible, otherwise this method cannot shirnk the columns
			if (visibleIndex.Count >= Count) {
				int? current = GetRight(Count - 1);
				if (current != null && displayRect.Width > current.Value) {
					//Calculate the columns to stretch
					var countToStretch = 0;
					for (var index = 0; index < Count; index++) {
						if ((GetAutoSizeMode(index) & AutoSizeMode.EnableStretch) == AutoSizeMode.EnableStretch &&
							IsColumnVisible(index))
							countToStretch++;
					}

					if (countToStretch > 0) {
						var deltaPerColumn = (displayRect.Width - current.Value) / countToStretch;
						for (var i = 0; i < Count; i++) {
							if ((GetAutoSizeMode(i) & AutoSizeMode.EnableStretch) == AutoSizeMode.EnableStretch &&
								IsColumnVisible(i))
								SetWidth(i, GetWidth(i) + deltaPerColumn);
						}
					}
				}
			}
		}

		ResumeLayout();
	}

	/// <summary>
	/// Sets the width of the specified column.
	/// </summary>
	/// <param name="column"></param>
	/// <param name="width"></param>
	public abstract void SetWidth(int column, int width);

	public abstract AutoSizeMode GetAutoSizeMode(int column);

	/// <summary>
	/// Calculate the Column that have the Left value smaller or equal than the point p_X, or -1 if not found found.
	/// </summary>
	/// <param name="x">X Coordinate to search for a column</param>
	/// <returns></returns>
	public int? ColumnAtPoint(int x) {
		var list = ColumnsInsideRegion(x, 1);
		if (list.Count == 0)
			return null;
		return list[0];
	}

	/// <summary>
	/// Gets the columns index inside the specified display area.
	/// </summary>
	/// <returns></returns>
	public List<int> ColumnsInsideRegion(int x, int width) => ColumnsInsideRegion(x, width, true, true);

	/// <summary>
	/// Gets the columns index inside the specified display area.
	/// The list returned is ordered by the index.
	/// Note that this method returns also invisible rows.
	/// </summary>
	/// <param name="returnsPartial">True to returns also partial columns</param>
	/// <param name="x"></param>
	/// <param name="width"></param>
	/// <param name="returnsFixedColumns"></param>
	/// <returns></returns>
	public List<int> ColumnsInsideRegion(int x, int width, bool returnsPartial, bool returnsFixedColumns) {
		var right = x + width;

		var list = new List<int>();

		//Add the fixed columns
		// Loop until the currentHeight is smaller then the requested displayRect
		for (var fr = 0; fr < Grid.FixedColumns && fr < Count; fr++) {
			var leftDisplay = GetLeft(fr);
			var rightDisplay = leftDisplay + GetWidth(fr);

			//If the column is inside the view
			if (right >= leftDisplay && x <= rightDisplay &&
				(returnsPartial || (rightDisplay <= right && leftDisplay >= x))) {
				if (returnsFixedColumns)
					list.Add(fr);
			}

			if (rightDisplay > right)
				break;
		}

		var relativeCol = FirstVisibleScrollableColumn;

		if (relativeCol != null) {
			//Add the standard columns
			for (var r = relativeCol.Value; r < Count; r++) {
				var leftDisplay = GetLeft(r);
				var rightDisplay = leftDisplay + GetWidth(r);

				//If the column is inside the view
				if (right >= leftDisplay && x <= rightDisplay &&
					(returnsPartial || (rightDisplay <= right && leftDisplay >= x)))
					list.Add(r);

				if (rightDisplay > right)
					break;
			}
		}

		return list;
	}

	public CellRange GetRange(int column) => new CellRange(0, column, Grid.Rows.Count - 1, column);

	public void SuspendLayout() => _suspendedCount++;

	public void ResumeLayout() {
		if (_suspendedCount > 0)
			_suspendedCount--;

		PerformLayout();
	}

	/// <summary>
	/// Fired when the numbes of columns changed.
	/// </summary>
	public void ColumnsChanged() => PerformLayout();

	public void PerformLayout() {
		if (_suspendedCount == 0)
			OnLayout();
	}

	public int GetAbsoluteRight(int column) {
		var left = GetAbsoluteLeft(column);
		return left + GetWidth(column);
	}

	#region Left/Right

	public int GetAbsoluteLeft(int column) {
		if (column < 0)
			throw new ArgumentException("Must be a valid index");

		var left = 0;

		var index = 0;
		while (index < column) {
			left += GetWidth(index);

			index++;
		}

		return left;
	}

	/// <summary>
	/// Gets the column right position. GetLeft + GetWidth.
	/// </summary>
	public int GetRight(int column) {
		var left = GetLeft(column);
		return left + GetWidth(column);
	}

	#endregion

	/// <summary>
	/// Gets the column left position.
	/// The Left is relative to the specified start position.
	/// Calculate the left using also the FixedColumn if present.
	/// </summary>
	public int GetLeft(int column) {
		var actualFixedColumns = Math.Min(Grid.FixedColumns, Count);

		var left = 0;

		//Calculate fixed left cells
		for (var index = 0; index < actualFixedColumns; index++) {
			if (index == column)
				return left;

			left += GetWidth(index);
		}

		var relativeColumn = FirstVisibleScrollableColumn;
		if (relativeColumn == null)
			relativeColumn = Count;

		if (relativeColumn == column)
			return left;
		if (relativeColumn < column) {
			for (var i = relativeColumn.Value; i < Count; i++) {
				if (i == column)
					return left;

				left += GetWidth(i);
			}
		} else if (relativeColumn > column) {
			for (var i = relativeColumn.Value - 1; i >= 0; i--) {
				left -= GetWidth(i);

				if (i == column)
					return left;
			}
		}

		throw new IndexOutOfRangeException();
	}

	/// <summary>
	/// Gets the width of the specified column.
	/// </summary>
	/// <param name="column"></param>
	/// <returns></returns>
	public abstract int GetWidth(int column);

	/// <summary>
	/// Show a column (set the width to default width)
	/// </summary>
	/// <param name="column"></param>
	public abstract void ShowColumn(int column);

	/// <summary>
	/// Hide the specified column (set the width to 0)
	/// </summary>
	/// <param name="column"></param>
	public abstract void HideColumn(int column);

	/// <summary>
	/// Returns true if the specified column is visible
	/// </summary>
	/// <param name="column"></param>
	/// <returns></returns>
	public abstract bool IsColumnVisible(int column);

	protected virtual void OnLayout() => Grid.OnCellsAreaChanged();
}
