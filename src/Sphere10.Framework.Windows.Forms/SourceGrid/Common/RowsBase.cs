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
/// Abstract base class for manage rows informations.
/// </summary>
public abstract class RowsBase : IRows {
	public event RowVisibilityChangedHandler RowVisibilityChanged;

	// Layout
	private int _suspendedCount = 0;

	protected IHiddenRowCoordinator HiddenRowCoordinator = null;

	public RowsBase(GridVirtual grid) {
		Grid = grid;

		HiddenRowCoordinator = new StandardHiddenRowCoordinator(this);
	}

	public IHiddenRowCoordinator HiddenRowsCoordinator => HiddenRowCoordinator;

	public GridVirtual Grid { get; }

	// Abstract methods
	public abstract int Count {
		get;
	}

	/// <summary>
	/// Returns the first visible scrollable row.
	/// Return null if there isn't a visible row.
	/// </summary>
	/// <returns></returns>
	public int? FirstVisibleScrollableRow {
		get {
			var firstVisible = HiddenRowsCoordinator.ConvertScrollbarValueToRowIndex(Grid.CustomScrollPosition.Y) + Grid.FixedRows;

			if (firstVisible >= Count)
				return null;
			return firstVisible;
		}
	}

	/// <summary>
	/// Returns the last visible scrollable row.
	/// Return null if there isn't a visible row.
	/// </summary>
	/// <returns></returns>
	public int? LastVisibleScrollableRow {
		get {
			var first = FirstVisibleScrollableRow;
			if (first == null)
				return null;

			var scrollableArea = Grid.GetScrollableArea();

			var bottom = GetTop(first.Value);
			var r = first.Value;
			for (; r < Count; r++) {
				bottom += GetHeight(r);

				if (bottom >= scrollableArea.Bottom)
					return r;
			}

			return r - 1;
		}
	}

	/// <summary>
	/// Calculate the Row that have the Top value smaller or equal than the point p_Y, or -1 if not found found.
	/// </summary>
	/// <param name="y">Y Coordinate to search for a row</param>
	/// <returns></returns>
	public int? RowAtPoint(int y) {
		var list = RowsInsideRegion(y, 1);
		if (list.Count == 0)
			return null;
		return list[0];
	}

	public void AutoSize(bool useColumnWidth) {
		AutoSize(useColumnWidth, 0, Grid.Columns.Count - 1);
	}

	/// <summary>
	/// Auto size all the rows with the max required height of all cells.
	/// </summary>
	/// <param name="useColumnWidth">True to fix the column width when calculating the required height of the row.</param>
	/// <param name="startCol">Start column to measure</param>
	/// <param name="endCol">End column to measure</param>
	public void AutoSize(bool useColumnWidth, int startCol, int endCol) {
		SuspendLayout();
		for (var index = 0; index < Count; index++)
			AutoSizeRow(index, useColumnWidth, startCol, endCol);
		ResumeLayout();
	}

	public void AutoSizeRow(int row) {
		var minColumn = 0;
		var maxColumn = Grid.Columns.Count - 1;

		if ((GetAutoSizeMode(row) & AutoSizeMode.EnableAutoSizeView) == AutoSizeMode.EnableAutoSizeView) {
			var isRowVisible = this.Grid.GetVisibleRows(true).Contains(row);
			if (isRowVisible == false)
				return;
			var visibleColumns = Grid.GetVisibleColumns(true);
			visibleColumns.Sort();
			if (visibleColumns.Count == 0)
				return;
			minColumn = visibleColumns[0];
			maxColumn = visibleColumns[visibleColumns.Count - 1];
		}
		AutoSizeRow(row, true, minColumn, maxColumn);
	}

	public void AutoSizeRow(int row, bool useColumnWidth, int startCol, int endCol) {
		if ((GetAutoSizeMode(row) & AutoSizeMode.EnableAutoSize) == AutoSizeMode.EnableAutoSize &&
			IsRowVisible(row))
			SetHeight(row, MeasureRowHeight(row, useColumnWidth, startCol, endCol));
	}

	/// <summary>
	/// Measures the current row when drawn with the specified cells.
	/// </summary>
	/// <param name="row"></param>
	/// <param name="useColumnWidth">True to fix the column width when calculating the required height of the row.</param>
	/// <param name="startCol">Start column to measure</param>
	/// <param name="endCol">End column to measure</param>
	/// <returns>Returns the required height</returns>
	public int MeasureRowHeight(int row, bool useColumnWidth, int startCol, int endCol) {
		var min = Grid.MinimumHeight;

		if ((GetAutoSizeMode(row) & AutoSizeMode.MinimumSize) == AutoSizeMode.MinimumSize)
			return min;

		for (var c = startCol; c <= endCol; c++) {
			var cell = Grid.GetCell(row, c);
			if (cell != null) {
				var cellPosition = new Position(row, c);

				var maxLayout = Size.Empty;
				//Use the width of the actual cell (considering spanned cells)
				if (useColumnWidth)
					maxLayout.Width = Grid.RangeToSize(Grid.PositionToCellRange(cellPosition)).Width;

				var cellContext = new CellContext(Grid, cellPosition, cell);
				var cellSize = cellContext.Measure(maxLayout);
				if (cellSize.Height > min)
					min = cellSize.Height;
			}
		}
		return min;
	}

	/// <summary>
	/// stretch the rows height to always fit the available space when the contents of the cell is smaller.
	/// </summary>
	public virtual void StretchToFit() {
		SuspendLayout();

		var displayRect = Grid.DisplayRectangle;

		if (Count > 0 && displayRect.Height > 0) {
			var visibleIndex = RowsInsideRegion(displayRect.Y, displayRect.Height);

			//Continue only if the rows are all visible, otherwise this method cannot shirnk the rows
			if (visibleIndex.Count >= Count) {
				int? current = GetBottom(Count - 1);
				if (current != null && displayRect.Height > current.Value) {
					//Calculate the columns to stretch
					var countToStretch = 0;
					for (var index = 0; index < Count; index++) {
						if ((GetAutoSizeMode(index) & AutoSizeMode.EnableStretch) == AutoSizeMode.EnableStretch &&
							IsRowVisible(index))
							countToStretch++;
					}

					if (countToStretch > 0) {
						var deltaPerRow = (displayRect.Height - current.Value) / countToStretch;
						for (var i = 0; i < Count; i++) {
							if ((GetAutoSizeMode(i) & AutoSizeMode.EnableStretch) == AutoSizeMode.EnableStretch &&
								IsRowVisible(i))
								SetHeight(i, GetHeight(i) + deltaPerRow);
						}
					}
				}
			}
		}

		ResumeLayout();
	}

	public abstract AutoSizeMode GetAutoSizeMode(int row);

	/// <summary>
	/// Gets the rows index inside the specified display area.
	/// </summary>
	public List<int> RowsInsideRegion(int y, int height) {
		return RowsInsideRegion(y, height, true, true);
	}

	/// <summary>
	/// Gets the rows index inside the specified display area.
	/// The list returned is ordered by the index.
	/// Note that this method returns also invisible rows.
	/// </summary>
	/// <param name="y"></param>
	/// <param name="height"></param>
	/// <param name="returnsPartial">True to returns also partial rows</param>
	/// <param name="returnsFixedRows"></param>
	public List<int> RowsInsideRegion(int y, int height, bool returnsPartial, bool returnsFixedRows) {
		var bottom = y + height;

		var list = new List<int>();

		//Add the fixed rows
		// Loop until the currentHeight is smaller then the requested displayRect
		for (var fr = 0; fr < Grid.FixedRows && fr < Count; fr++) {
			var topDisplay = GetTop(fr);
			var bottomDisplay = topDisplay + GetHeight(fr);

			//If the row is inside the view
			if (bottom >= topDisplay && y <= bottomDisplay &&
				(returnsPartial || (bottomDisplay <= bottom && topDisplay >= y))) {
				if (returnsFixedRows)
					list.Add(fr);
			}

			if (bottomDisplay > bottom)
				break;
		}

		var relativeRow = FirstVisibleScrollableRow;

		if (relativeRow != null) {
			//Add the standard rows
			for (var r = relativeRow.Value; r < Count; r++) {
				var topDisplay = GetTop(r);
				var bottomDisplay = topDisplay + GetHeight(r);

				//If the row is inside the view
				if (bottom >= topDisplay && y <= bottomDisplay &&
					(returnsPartial || (bottomDisplay <= bottom && topDisplay >= y)))
					list.Add(r);

				if (bottomDisplay > bottom)
					break;
			}
		}

		return list;
	}

	public CellRange GetRange(int row) {
		return new CellRange(row, 0, row, Grid.Columns.Count - 1);
	}

	public void SuspendLayout() {
		_suspendedCount++;
	}

	public void ResumeLayout() {
		if (_suspendedCount > 0)
			_suspendedCount--;

		PerformLayout();
	}

	/// <summary>
	/// Fired when the numbers of rows changed.
	/// </summary>
	public void RowsChanged() {
		PerformLayout();
	}

	public void PerformLayout() {
		if (_suspendedCount == 0)
			OnLayout();
	}

	public int GetAbsoluteBottom(int row) {
		var top = GetAbsoluteTop(row);
		return top + GetHeight(row);
	}

	// Top/Bottom
	public int GetAbsoluteTop(int row) {
		if (row < 0)
			throw new ArgumentException("Must be a valid index");

		var top = 0;

		var index = 0;
		while (index < row) {
			top += GetHeight(index);

			index++;
		}

		return top;
	}

	/// <summary>
	/// Gets the row bottom position. GetTop + GetHeight.
	/// </summary>
	public int GetBottom(int row) {
		var top = GetTop(row);
		return top + GetHeight(row);
	}

	/// <summary>
	/// Gets the row top position.
	/// The Top is relative to the specified start position.
	/// Calculate the top using also the FixedRows if present.
	/// </summary>
	public int GetTop(int row) {
		if (row < 0)
			throw new ArgumentNullException("Row is less than 0");
		var actualFixedRows = Math.Min(Grid.FixedRows, Count);

		var top = 0;

		//Calculate fixed top cells
		for (var index = 0; index < actualFixedRows; index++) {
			if (index == row)
				return top;

			top += GetHeight(index);
		}

		var relativeRow = FirstVisibleScrollableRow;
		if (relativeRow == null)
			relativeRow = Count;

		if (relativeRow == row)
			return top;
		if (relativeRow < row) {
			top += GetTopPositive(relativeRow.Value, row);
			return top;
		}
		if (relativeRow > row) {
			top += GetTopNegative(relativeRow.Value, row);
			return top;
		}

		throw new IndexOutOfRangeException(string.Format("row value is {0}", row));
	}

	/// <summary>
	/// Hide the specified row (set the height to 0)
	/// </summary>
	/// <param name="row"></param>
	public void HideRow(int row) {
		ShowRow(row, false);
	}

	/// <summary>
	/// Show a row (set the height to default height)
	/// </summary>
	/// <param name="row"></param>
	public void ShowRow(int row) {
		ShowRow(row, true);
	}

	/// <summary>
	/// Makes row visible or hidden.
	/// Fires OnRowVisibilityChanged event only if row visibility is changed
	/// </summary>
	/// <param name="row"></param>
	/// <param name="isVisible"></param>
	public void ShowRow(int row, bool isVisible) {
		if (isVisible == true && IsRowVisible(row) == false) {
			SetHeight(row, Grid.DefaultHeight);
			OnRowVisibilityChanged(row, isVisible);
		} else if (isVisible == false && IsRowVisible(row)) {
			SetHeight(row, 0);
			OnRowVisibilityChanged(row, isVisible);
		}
	}

	/// <summary>
	/// Sets the height of the specified row.
	/// </summary>
	/// <param name="row"></param>
	/// <param name="height"></param>
	public abstract void SetHeight(int row, int height);

	/// <summary>
	/// Returns true if the specified row is visible
	/// </summary>
	/// <param name="row"></param>
	/// <returns></returns>
	public bool IsRowVisible(int row) {
		return GetHeight(row) > 0;
	}

	protected virtual void OnRowVisibilityChanged(int rowIndex, bool becameVisible) {
		if (RowVisibilityChanged != null)
			RowVisibilityChanged(rowIndex, becameVisible);
	}

	protected virtual void OnLayout() {
		Grid.OnCellsAreaChanged();
	}

	private int GetTopPositive(int relativeRow, int row) {
		var top = 0;
		for (var index = relativeRow; index < row; index++)             /*if (i == row)
					return top;*/

			top += GetHeight(index);

		return top;
	}

	private int GetTopNegative(int relativeRow, int row) {
		var top = 0;
		for (var index = relativeRow - 1; index >= row; index--) {
			top -= GetHeight(index);
			/*if (i == row)
					return top;*/
		}
		return top;
	}

	/// <summary>
	/// Gets the height of the specified row.
	/// </summary>
	/// <param name="row"></param>
	/// <returns></returns>
	public abstract int GetHeight(int row);
}
