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
using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// The main grid control with static data.
/// </summary>
[System.ComponentModel.ToolboxItem(true)]
public class Grid : GridVirtual {
	private ISpannedCellRangesController _spannedCellReferences = null;
	private CellOptimizeMode _optimizeMode = CellOptimizeMode.ForRows;

	// AllowOverlappingCells
	private bool _allowOverlappingCells = false;

	// Row/Col Span
	[Obsolete] private static int _maxSpan = 100;

	// PaintCell
	/// <summary>
	/// List of the ranges already drawn
	/// </summary>
	private RangeCollection _drawnRange = new RangeCollection();

	// Sort
	private bool _customSort = false;

	// Constructor
	/// <summary>
	/// Constructor
	/// </summary>
	public Grid() {
		this.SuspendLayout();
		this.Name = "Grid";
		_spannedCellReferences = new SpannedCellRangesController(
this, new QuadTreeRangesList(CellRange.From(new Position(0, 0), 4, 4)));

		this.ResumeLayout(false);
	}

	#region properties

	/// <summary>
	/// Allows to check what spanned cells we have.
	/// Do not use this property directly from client code
	/// </summary>
	public ISpannedCellRangesController SpannedCellReferences => _spannedCellReferences;

	#endregion

	#region Rows/Columns

	/// <summary>
	/// Gets or Sets the number of columns
	/// </summary>
	[DefaultValue(0)]
	public int ColumnsCount {
		get => Columns.Count;
		set => Columns.SetCount(value);
	}

	/// <summary>
	/// Gets or Sets the number of rows
	/// </summary>
	[DefaultValue(0)]
	public int RowsCount {
		get => Rows.Count;
		set => Rows.SetCount(value);
	}

	/// <summary>
	/// Returns the maximum number of rows that can fit without a vertical scrollbar
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int RowsPerPage => base.VerticalPage;

	/// <summary>
	/// RowsCount informations
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public new GridRows Rows {
		get => (GridRows)base.Rows;
	}

	/// <summary>
	/// Columns informations
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public new GridColumns Columns {
		get => (GridColumns)base.Columns;
	}

	/// <summary>
	/// Gets or sets the optimize mode. Default is ForRows
	/// </summary>
	public CellOptimizeMode OptimizeMode {
		get => _optimizeMode;
		set => _optimizeMode = value;
	}

	#endregion

	/// <summary>
	/// Indicates that overlapping cells are not allowed and will be thrown an exception.
	/// This is needed due to the fact that when there are two overlapping spanned cells,
	/// SourceGrid can not reliably draw them, since the call to function GetCell(x,y)
	/// returns random results. However, some users epxressed the need to allow such
	/// behaviour. So this flag forces Grid to not check for overlapping cells
	/// </summary>
	[DefaultValue(false)]
	public bool AllowOverlappingCells {
		get => _allowOverlappingCells;
		set => _allowOverlappingCells = value;
	}

	/// <summary>
	/// Gets the maximum rows or columns number to search when using Row/Col Span.
	/// This is a static property.
	/// This value is automatically calculated based on the current cells. Do not change this value manually.
	/// Default is 100.
	/// </summary>
	[Obsolete("This property is not needed anymore. It has completely no effect")]
	public static int MaxSpan {
		get => _maxSpan;
		set => _maxSpan = value;
	}

	/// <summary>
	/// Gets or sets if when calling SortRangeRows method use a custom sort or an automatic sort. Default = false (automatic)
	/// </summary>
	[DefaultValue(false)]
	public bool CustomSort {
		get => _customSort;
		set => _customSort = value;
	}

	public override bool EnableSort {
		get; set;
	}

	public Cells.ICell this[Position position] {
		get {
			var cell = DirectGetCell(position);
			if (cell != null)
				return cell;
			return GetSpannedCell(position);
		}
		set => InsertCell(position.Row, position.Column, value);
	}

	/// <summary>
	/// Returns or set a cell at the specified row and col.
	/// If you get a ICell position occupied by a row/col span cell,
	/// and EnableRowColSpan is true, this method returns the cell with Row/Col span.
	/// </summary>
	public Cells.ICell this[int row, int column] {
		get => this[new Position(row, column)];
		set => this[new Position(row, column)] = value;
	}

	// GetCell methods
	/// <summary>
	/// Set the specified cell int he specified position. Abstract method of the GridVirtual control
	/// </summary>
	/// <param name="row"></param>
	/// <param name="column"></param>
	/// <param name="cell"></param>
	public virtual void SetCell(int row, int column, Cells.ICellVirtual cell) {
		if (cell is Cells.ICell)
			InsertCell(row, column, (Cells.ICell)cell);
		else if (cell == null)
			InsertCell(row, column, null);
		else
			throw new SourceGridException("Expected ICell class");
	}

	/// <summary>
	/// Set the specified cell int he specified position. This method calls SetCell(int p_iRow, int p_iCol, Cells.ICellVirtual p_Cell)
	/// </summary>
	/// <param name="position"></param>
	/// <param name="cell"></param>
	public void SetCell(Position position, Cells.ICellVirtual cell) => SetCell(position.Row, position.Column, cell);

	/// <summary>
	/// Return the Cell at the specified Row and Col position.
	/// </summary>
	/// <param name="row"></param>
	/// <param name="column"></param>
	/// <returns></returns>
	public override Cells.ICellVirtual GetCell(int row, int column) => this[row, column];

	public void UpdateSpannedArea(int row, int column, ICell cell) {
		if (cell == null)
			throw new ArgumentNullException();
		if (cell.RowSpan == 1 && cell.ColumnSpan == 1)
			throw new ArgumentException("Cell is not spanned! Can not update it!. " +
										"You should delete it manually, and ensure that foreach statement" +
										" will not be broken");
		EnsureDestinationSpannedAreaIsEmptyExceptOriginalCell(row, column, cell);
		_spannedCellReferences.Update(cell.Range);
	}

	/// <summary>
	/// This method converts a Position to the real range of the cell. This is usefull when RowSpan or ColumnSpan is greater than 1.
	/// </summary>
	/// <returns></returns>
	public override CellRange RangeToCellRange(CellRange range) {
		var x = range.Start.Column;
		var x1 = range.End.Column;
		var y = range.Start.Row;
		var y1 = range.End.Row;
		for (var x2 = range.Start.Column; x2 <= range.End.Column; x2++) {
			for (var y2 = range.Start.Row; y2 <= range.End.Row; y2++) {
				var p = new Position(y2, x2);
				var range2 = PositionToCellRange(p);
				if (range2.IsEmpty())
					range2 = new CellRange(p, p);
				if (range2.Start.Column < x)
					x = range2.Start.Column;
				if (range2.End.Column > x1)
					x1 = range2.End.Column;

				if (range2.Start.Row < y)
					y = range2.Start.Row;
				if (range2.End.Row > y1)
					y1 = range2.End.Row;
			}
		}
		return new CellRange(y, x, y1, x1);
	}

	#region InvalidateCell

	/// <summary>
	/// Force a redraw of the specified cell
	/// </summary>
	/// <param name="cell"></param>
	public virtual void InvalidateCell(Cells.ICell cell) {
		if (cell != null)
			base.InvalidateRange(cell.Range);
	}

	/// <summary>
	/// Force a cell to redraw. If ColSpan or RowSpan is greater than 0 this function invalidate the complete range with InvalidateRange
	/// </summary>
	/// <param name="position"></param>
	public override void InvalidateCell(Position position) {
		var cell = this[position.Row, position.Column];
		if (cell == null || (cell.Range.ColumnsCount == 1 && cell.Range.RowsCount == 1))
			base.InvalidateCell(position);
		else
			InvalidateRange(cell.Range);
	}

	#endregion

	/// <summary>
	/// Method used to create the rows object, in this class of type RowInfoCollection.
	/// </summary>
	protected override RowsBase CreateRowsObject() => new GridRows(this);

	/// <summary>
	/// Method used to create the columns object, in this class of type ColumnInfoCollection.
	/// </summary>
	protected override ColumnsBase CreateColumnsObject() => new GridColumns(this);

	protected override void OnRangePaint(RangePaintEventArgs e) {
		_drawnRange.Clear();

		base.OnRangePaint(e);
	}

	protected override void PaintCell(Sphere10.Framework.Drawing.GraphicsCache graphics, CellContext cellContext, RectangleF drawRectangle) {
		var cellRange = PositionToCellRange(cellContext.Position);
		if (cellRange.ColumnsCount == 1 && cellRange.RowsCount == 1)
			base.PaintCell(graphics, cellContext, drawRectangle);
		else {
			//Row/Col Span > 1

			// I draw the merged cell only if not already drawn otherwise
			// drawing the same cell can cause some problem when using
			// special drawing code (for example semi transparent background)
			if (_drawnRange.Contains(cellRange) == false) {
				var spanRect = RangeToRectangle(cellRange);
				base.PaintCell(graphics, cellContext, spanRect);

				_drawnRange.Add(cellRange);
			}
		}
	}

	/// <summary>
	/// This method converts a Position to the real range of the cell. This is usefull when RowSpan or ColumnSpan is greater than 1.
	/// For example suppose to have at grid[0,0] a cell with ColumnSpan equal to 2. If you call this method with the position 0,0 returns 0,0-0,1 and if you call this method with 0,1 return again 0,0-0,1.
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public override CellRange PositionToCellRange(Position position) {
		if (position.IsEmpty())
			return CellRange.Empty;

		var cell = this[position.Row, position.Column];
		if (cell == null)
			return CellRange.Empty;
		return cell.Range;
	}

	/// <summary>
	/// Fired when calling SortRangeRows method. If the range contains all the columns this method move directly the row object otherwise move each cell.
	/// </summary>
	/// <param name="e"></param>
	protected override void OnSortingRangeRows(SortRangeRowsEventArgs e) {
		base.OnSortingRangeRows(e);

		if (CustomSort)
			return;

		if (e.KeyColumn > e.Range.End.Column && e.KeyColumn < e.Range.Start.Column)
			throw new ArgumentException("Invalid range", "e.KeyColumn");

		var cellComparer = e.CellComparer;
		if (cellComparer == null)
			cellComparer = new ValueCellComparer();

		//Sort all the columns (in this case I move directly the row object)
		if (e.Range.ColumnsCount == ColumnsCount) {
			var rowInfoToSort = new RowInfo[e.Range.End.Row - e.Range.Start.Row + 1];
			var cellKeys = new Cells.ICell[e.Range.End.Row - e.Range.Start.Row + 1];

			var zeroIndex = 0;
			for (var r = e.Range.Start.Row; r <= e.Range.End.Row; r++) {
				cellKeys[zeroIndex] = this[r, e.KeyColumn];

				rowInfoToSort[zeroIndex] = Rows[r];
				zeroIndex++;
			}

			Array.Sort(cellKeys, rowInfoToSort, 0, cellKeys.Length, cellComparer);

			//Apply sort
			if (e.Ascending) {
				for (zeroIndex = 0; zeroIndex < rowInfoToSort.Length; zeroIndex++)
					Rows.Swap(rowInfoToSort[zeroIndex].Index, e.Range.Start.Row + zeroIndex);
			} else {
				//desc

				for (zeroIndex = rowInfoToSort.Length - 1; zeroIndex >= 0; zeroIndex--)
					Rows.Swap(rowInfoToSort[zeroIndex].Index, e.Range.End.Row - zeroIndex);
			}
		} else {
			//sort only the specified range

			var rangeSort = new Cells.ICell[e.Range.End.Row - e.Range.Start.Row + 1][];
			var cellsKeys = new Cells.ICell[e.Range.End.Row - e.Range.Start.Row + 1];

			var zeroRowIndex = 0;
			for (var r = e.Range.Start.Row; r <= e.Range.End.Row; r++) {
				cellsKeys[zeroRowIndex] = this[r, e.KeyColumn];

				var zeroColIndex = 0;
				rangeSort[zeroRowIndex] = new Cells.ICell[e.Range.End.Column - e.Range.Start.Column + 1];
				for (var c = e.Range.Start.Column; c <= e.Range.End.Column; c++) {
					rangeSort[zeroRowIndex][zeroColIndex] = this[r, c];
					zeroColIndex++;
				}
				zeroRowIndex++;
			}

			Array.Sort(cellsKeys, rangeSort, 0, cellsKeys.Length, cellComparer);

			//Apply sort
			zeroRowIndex = 0;
			if (e.Ascending) {
				for (var r = e.Range.Start.Row; r <= e.Range.End.Row; r++) {
					var zeroColIndex = 0;
					for (var c = e.Range.Start.Column; c <= e.Range.End.Column; c++) {
						RemoveCell(r, c); //rimuovo qualunque cella nella posizione corrente
						var temporary = rangeSort[zeroRowIndex][zeroColIndex];

						if (temporary != null && temporary.Grid != null && temporary.Range.Start.Row >= 0 && temporary.Range.Start.Column >= 0) //verifico che la cella sia valida
							RemoveCell(temporary.Range.Start.Row, temporary.Range.Start.Column); //la rimuovo dalla posizione precedente

						this[r, c] = temporary;
						zeroColIndex++;
					}
					zeroRowIndex++;
				}
			} else {
				//desc

				for (var r = e.Range.End.Row; r >= e.Range.Start.Row; r--) {
					var zeroColIndex = 0;
					for (var c = e.Range.Start.Column; c <= e.Range.End.Column; c++) {
						RemoveCell(r, c); //rimuovo qualunque cella nella posizione corrente
						var tmp = rangeSort[zeroRowIndex][zeroColIndex];

						if (tmp != null && tmp.Grid != null && tmp.Range.Start.Row >= 0 && tmp.Range.Start.Column >= 0) //verifico che la cella sia valida
							RemoveCell(tmp.Range.Start.Row, tmp.Range.Start.Column); //la rimuovo dalla posizione precedente

						this[r, c] = tmp;
						zeroColIndex++;
					}
					zeroRowIndex++;
				}
			}
		}
	}

	/// <summary>
	/// Overriden to return a custom accessibility object instance.
	/// </summary>
	/// <returns>AccessibleObject for the grid control.</returns>
	protected override AccessibleObject CreateAccessibilityInstance() => new GridAccessibleObject(this);

	/// <summary>
	/// Check if a cell exists in spanned cells.
	/// If yes, returns. If no, returns null
	/// </summary>
	private Cells.ICell GetSpannedCell(Position position) {
		var range = _spannedCellReferences.SpannedRangesCollection.GetFirstIntersectedRange(position);
		if (range == null)
			return null;
		var cellPos = range.Value.Start;
		var cell = DirectGetCell(cellPos);
		if (cell == null)
			throw new ArgumentException(string.Format(
"Invalid grid state. Grid should contain a spanned cell at position {0}, " +
				" but apparently it does not!", cellPos));
		return cell;
	}

	/// <summary>
	/// Insert the specified cell
	/// </summary>
	private void InsertCell(int row, int column, ICell cell) {
		RemoveCell(row, column);

		if (cell != null && cell.Grid != null)
			throw new ArgumentException("This cell already have a linked grid", "p_cell");

		if (cell != null)
			EnsureDestinationSpannedAreaisCompletelyEmpty(row, column, cell.RowSpan, cell.ColumnSpan);

		if (cell != null)
			cell.BindToGrid(this, new Position(row, column));

		if ((cell != null) && ((cell.RowSpan > 1) || (cell.ColumnSpan > 1)))            // occupy space if it is spanned cell
			OccupySpannedArea(row, column, cell);

		DirectSetCell(new Position(row, column), cell);
	}

	public void OccupySpannedArea(int row, int column, ICell cell) {
		if (cell == null)
			throw new ArgumentNullException();
		EnsureDestinationSpannedAreaIsEmptyExceptOriginalCell(row, column, cell);
		_spannedCellReferences.UpdateOrAdd(cell.Range);
	}

	/// <summary>
	/// Remove the specified cell
	/// </summary>
	/// <param name="row"></param>
	/// <param name="column"></param>
	private void RemoveCell(int row, int column) {
		var cell = DirectGetCell(new Position(row, column));

		if (cell == null)
			return;
		cell.UnBindToGrid();

		RemoveSpannedCell(new Position(row, column));
		DirectSetCell(new Position(row, column), null);
	}

	private void DirectSetCell(Position position, Cells.ICell cell) {
		if (OptimizeMode == CellOptimizeMode.ForRows) {
			var row = Rows[position.Row];
			if (position.Column >= Columns.Count)
				throw new ArgumentException(string.Format(
					"Grid has only {0} columns, you tried to insert cell into position {1}." +
					"You should probably call Redim on grid to increase it's column size",
					Columns.Count,
					position.ToString()));

			row[Columns[position.Column] as GridColumn] = cell;
		} else if (OptimizeMode == CellOptimizeMode.ForColumns) {
			var column = Columns[position.Column] as GridColumn;

			column[Rows[position.Row]] = cell;
		} else
			throw new SourceGridException("Invalid OptimizeMode");
	}

	/// <summary>
	/// Removes a spanned cell internal reference.
	/// This effectively removes any data in spannedCellReferences collection
	/// </summary>
	/// <param name="position"></param>
	private void RemoveSpannedCell(Position position) {
		var range = _spannedCellReferences.SpannedRangesCollection.GetFirstIntersectedRange(position);
		if (range == null)
			return;
		_spannedCellReferences.SpannedRangesCollection.Remove(range.Value);
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="row"></param>
	/// <param name="column"></param>
	/// <param name="cell">except this cell</param>
	private void EnsureDestinationSpannedAreaIsEmptyExceptOriginalCell(int row, int column, Cells.ICell cell) {
		if (AllowOverlappingCells)
			return;

		EnsureNoSpannedCellsExist(row, column, cell);
		EnsureNoOtherCellsExist(row, column, cell);
		/*
		for (int y = row; y < row + p_cell.RowSpan; y++)
		{
			for (int x = col; x < col + p_cell.ColumnSpan; x++)
			{
				if (x == col && y == row)
					continue;
				Cells.ICell existingSpannedCell = this[y, x];
				if (existingSpannedCell != null)
					if (existingSpannedCell != p_cell)
						throw new OverlappingCellException(string.Format(
							"Given cell at position ({0}, {1}), " +
							"intersects with another cell " +
							"at position ({2}, {3}) '{4}'",
							row, col,
							existingSpannedCell.Row.Index,
							existingSpannedCell.Column.Index,
							existingSpannedCell.DisplayText));
			}
		}*/
	}

	#region Spanned Cells support

	private void EnsureNoSpannedCellsExist(int row, int column, Cells.ICell cell) {
		var spanRange = new CellRange(row, column, row + cell.RowSpan - 1, column + cell.ColumnSpan - 1);
		var ranges = _spannedCellReferences.SpannedRangesCollection.GetRanges(

	#endregion

spanRange);
		if (ranges.Count == 0)
			return;

		var start = new Position(row, column);
		foreach (var range in ranges) {
			if (start.Equals(range.Start) == true)
				continue;
			var existingSpannedCell = this[range.Start];
			if (existingSpannedCell == null)
				throw new ArgumentException("internal error. please report this bug to developers");
			throw new OverlappingCellException(string.Format(
				"Given cell at position ({0}, {1}), " +
				"intersects with another cell " +
				"at position ({2}, {3}) '{4}'",
				row,
				column,
				existingSpannedCell.Row.Index,
				existingSpannedCell.Column.Index,
				existingSpannedCell.DisplayText));
		}
	}

	/// <summary>
	/// Check
	/// </summary>
	private void EnsureNoOtherCellsExist(int row, int column, Cells.ICell cell) {
		for (var y = row; y < row + cell.RowSpan; y++) {
			for (var x = column; x < column + cell.ColumnSpan; x++) {
				if (x == column && y == row)
					continue;
				var existingSpannedCell = this.DirectGetCell(y, x);
				if (existingSpannedCell != null)
					if (existingSpannedCell != cell)
						throw new OverlappingCellException(string.Format(
							"Given cell at position ({0}, {1}), " +
							"intersects with another cell " +
							"at position ({2}, {3}) '{4}'",
							row,
							column,
							existingSpannedCell.Row.Index,
							existingSpannedCell.Column.Index,
							existingSpannedCell.DisplayText));
			}
		}
	}

	private Cells.ICell DirectGetCell(int row, int column) => DirectGetCell(new Position(row, column));

	private Cells.ICell DirectGetCell(Position position) {
		if (IsInGrid(position) == false)
			return null;
		if (OptimizeMode == CellOptimizeMode.ForRows) {
			var row = Rows[position.Row];
			if (row == null)
				return null;

			return row[Columns[position.Column] as GridColumn];
		}
		if (OptimizeMode == CellOptimizeMode.ForColumns) {
			var column = Columns[position.Column] as GridColumn;
			if (column == null)
				return null;

			return column[Rows[position.Row]];
		}
		throw new SourceGridException("Invalid OptimizeMode");
	}

	private bool IsInGrid(Position position) {
		if (position.Column < 0)
			return false;
		if (position.Row < 0)
			return false;
		if (position.Column >= this.Columns.Count)
			return false;
		if (position.Row >= this.Rows.Count)
			return false;
		return true;
	}

	private void EnsureDestinationSpannedAreaisCompletelyEmpty(int row, int column, int rowSpan, int colSpan) {
		if (AllowOverlappingCells)
			return;

		for (var y = row; y < row + rowSpan; y++) {
			for (var x = column; x < column + colSpan; x++) {
				var existingSpannedCell = this[y, x];
				if (existingSpannedCell != null)
					throw new OverlappingCellException(string.Format(
						"Given cell at position ({0}, {1}), " +
						"intersects with another cell " +
						"at position ({2}, {3}) '{4}'",
						row,
						column,
						(existingSpannedCell.Row != null) ? existingSpannedCell.Row.Index : -1,
						(existingSpannedCell.Column != null) ? existingSpannedCell.Column.Index : -1,
						existingSpannedCell.DisplayText));
			}
		}
	}

	#region AddRow/Col, RemoveRow/Col

	/// <summary>
	/// Call this method to update internal grid structures.
	/// This is used to update QuadTree space at the moment.
	/// </summary>
	internal void GrowGrid() => _spannedCellReferences.SpannedRangesCollection.Redim(RowsCount, ColumnsCount);

	/// <summary>
	/// Set the number of columns and rows
	/// </summary>
	public void Redim(int rows, int columns) {
		base.SuspendLayout();
		RowsCount = rows;
		ColumnsCount = columns;
		base.ResumeLayout();
		GrowGrid();
	}

	#endregion

	/// <summary>
	/// Provides information about the grid control in the acessibility tree.
	/// </summary>
	public class GridAccessibleObject : Control.ControlAccessibleObject {
		private Grid _grid;

		/// <summary>
		/// Creates a new GridAccessibleObject
		/// </summary>
		/// <param name="owner">Owner</param>
		public GridAccessibleObject(Grid owner)
			: base(owner) {
			_grid = owner;
		}

		/// <summary>
		/// Returns the role of the grid control
		/// </summary>
		public override AccessibleRole Role => AccessibleRole.Table;

		/// <summary>
		/// Retrieves the accessible name of the grid control
		/// </summary>
		public override string Name => _grid.Name;

		/// <summary>
		/// Retrieves the accessibility object of a row in the grid.
		/// </summary>
		/// <param name="index">Index of child</param>
		/// <returns>AccessibleObject of a child element</returns>
		public override AccessibleObject GetChild(int index) => new GridRowAccessibleObject(_grid.Rows[index], this);

		/// <summary>
		/// Returns the number of children
		/// </summary>
		/// <returns>The number of rows in the grid.</returns>
		public override int GetChildCount() => _grid.RowsCount;
	}

	/// <summary>
	/// Provides for information about grid rows in the acessibility tree.
	/// </summary>
	public class GridRowAccessibleObject : AccessibleObject {
		private GridRow _gridRow;
		private GridAccessibleObject _parent;

		/// <summary>
		/// Creates a new GridRowAccessibleObject
		/// </summary>
		/// <param name="gridRow">The grid row</param>
		/// <param name="parent">The grid row's parent grid</param>
		public GridRowAccessibleObject(GridRow gridRow, GridAccessibleObject parent)
			: base() {
			_gridRow = gridRow;
			_parent = parent;
		}

		/// <summary>
		/// Gets the bounding rectangle in screen coordinates.
		/// </summary>
		public override System.Drawing.Rectangle Bounds {
			get {
				// Return empty rectangle if row is not visible
				if (!_gridRow.Visible)
					return System.Drawing.Rectangle.Empty;

				var vScroll = 0;
				if (_gridRow.Grid.VerticalScroll.Enabled)
					vScroll = _gridRow.Grid.VScrollBar.Value;

				// Check if the row is before the current scroll position
				if (_gridRow.Index < vScroll)
					return System.Drawing.Rectangle.Empty;

				// Get the row height
				var rowHeight = _gridRow.Height;

				var hScroll = 0;
				if (_gridRow.Grid.HorizontalScroll.Enabled)
					hScroll = _gridRow.Grid.HScrollBar.Value;

				// Calculate row width
				var rowWidth = 0;
				for (var index = hScroll; index < _gridRow.Grid.Columns.Count; index++) {
					var cols = (GridColumns)_gridRow.Grid.Columns;
					rowWidth += cols[index].Width;
				}

				// Get the row x position
				var x = _parent.Bounds.X;

				// Calculate the row y position
				var y = _parent.Bounds.Top;
				for (var i = vScroll; i < _gridRow.Index; i++) {
					var rows = (GridRows)_gridRow.Grid.Rows;
					y += rows[i].Height;
				}

				var rowRect = new System.Drawing.Rectangle(x, y, rowWidth, rowHeight);

				// Double check if row is inside current grid view
				if (_parent.Bounds.IntersectsWith(rowRect))
					return rowRect;
				return System.Drawing.Rectangle.Empty;
			}
		}

		/// <summary>
		/// Returns the name of the grid row.
		/// </summary>
		public override string Name => "Row " + _gridRow.Index;

		/// <summary>
		/// Returns the role of the grid row.
		/// </summary>
		public override AccessibleRole Role => AccessibleRole.Row;

		/// <summary>
		/// Returns the parent grid of the grid row.
		/// </summary>
		public override AccessibleObject Parent => _parent;

		/// <summary>
		/// Returns a child cell of the row.
		/// </summary>
		/// <param name="index">Index of child</param>
		/// <returns>AccessibleObject of a child element</returns>
		public override AccessibleObject GetChild(int index) {
			var cells = _gridRow.Grid.GetCellsAtRow(_gridRow.Index);
			if (index < cells.Length) {
				var cell = (Cell)cells[index];

				if (cell == null)
					return null;

				return new GridRowCellAccessibleObject(cell, this);
			}
			var lastCell = (Cell)cells[cells.Length - 1];
			return new GridRowCellAccessibleObject(lastCell, this);
		}

		/// <summary>
		/// Returns the number of children.
		/// </summary>
		/// <returns>Number of cells in a row</returns>
		public override int GetChildCount() => _gridRow.Grid.GetCellsAtRow(_gridRow.Index).Length;
	}

	/// <summary>
	/// Provides information about grid cells in the accessibility tree.
	/// </summary>
	public class GridRowCellAccessibleObject : AccessibleObject {
		Cell _cell;
		GridRowAccessibleObject _parent;

		/// <summary>
		/// Creates a new GridRowCellAccessibleObject
		/// </summary>
		/// <param name="cell">The grid cell</param>
		/// <param name="parent">The parent row of the cell</param>
		public GridRowCellAccessibleObject(Cell cell, GridRowAccessibleObject parent)
			: base() {
			_cell = cell;
			_parent = parent;
		}

		/// <summary>
		/// Gets the bounding rectange in screen coordinates.
		/// </summary>
		public override System.Drawing.Rectangle Bounds {
			get {
				// If the parent row isn't visible, neither is the cell
				if (_parent.Bounds == System.Drawing.Rectangle.Empty)
					return System.Drawing.Rectangle.Empty;

				var hScroll = 0;
				if (_cell.Grid.HorizontalScroll.Enabled)
					hScroll = _cell.Grid.HScrollBar.Value;

				// If we scrolled horizontally past the cell it won't be visible anymore
				if (_cell.Column.Index < hScroll)
					return System.Drawing.Rectangle.Empty;

				var cellWidth = _cell.Column.Width;
				var cellHeight = _cell.Row.Height;

				var x = _parent.Bounds.X;

				for (var index = hScroll; index < _cell.Column.Index; index++) {
					var cols = (GridColumns)_cell.Grid.Columns;
					x += cols[index].Width;
				}

				var y = _parent.Bounds.Y;

				var cellRect = new System.Drawing.Rectangle(x, y, cellWidth, cellHeight);

				// Double check cell if cell is inside current grid view
				if (_parent.Bounds.IntersectsWith(cellRect))
					return cellRect;
				return System.Drawing.Rectangle.Empty;
			}
		}

		/// <summary>
		/// Returns the role of the grid cell.
		/// </summary>
		public override AccessibleRole Role => AccessibleRole.Cell;

		/// <summary>
		/// Returns the name of the cell.
		/// </summary>
		/// <remarks>
		/// If display text not found, returns a default name containing the column index.
		/// </remarks>
		public override string Name {
			get {
				if (_cell.DisplayText != null)
					return _cell.DisplayText;
				return "Column " + _cell.Column.Index;
			}
		}

		/// <summary>
		/// Set or get the value of the cell.
		/// </summary>
		public override string Value {
			get => _cell.DisplayText;
			set => _cell.Value = value;
		}

		/// <summary>
		/// Returns the parent row of the cell.
		/// </summary>
		public override AccessibleObject Parent => _parent;
	}
}
