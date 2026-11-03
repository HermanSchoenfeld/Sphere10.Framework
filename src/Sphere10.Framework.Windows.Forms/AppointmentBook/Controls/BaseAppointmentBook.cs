// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Sphere10.Framework.Drawing;
using Sphere10.Framework.Windows.Forms.Drawing;

namespace Sphere10.Framework.Windows.Forms.AppointmentBook;

[ToolboxItem(false)]
public partial class BaseAppointmentBook : UserControlEx {
	public event EventHandlerEx<BaseAppointmentBook, int, int, MouseEventArgs> MouseUpEvent;

	public event EventHandlerEx<BaseAppointmentBook, int, int, MouseEventArgs> MouseDownEvent;

	public event EventHandlerEx<BaseAppointmentBook, int, int, MouseEventArgs> MouseOverEvent;

	public event EventHandlerEx<BaseAppointmentBook, int, int> ClickEvent;

	public event EventHandlerEx<BaseAppointmentBook, int, int> DoubleClickEvent;

	public event EventHandlerEx<BaseAppointmentBook, int, int> SelectionStarted;

	public event EventHandlerEx<BaseAppointmentBook, int, int> Selecting;

	public event EventHandlerEx<BaseAppointmentBook, int, int> SelectionChanged;

	public event EventHandlerEx<BaseAppointmentBook, int, int, int, int> SelectionFinished;

	public event EventHandlerEx<BaseAppointmentBook> SelectionCleared;

	private BaseAppointmentBookViewModel _viewModel;
	private Tuple<int, int, DateTime, Point> _lastMouseDown;
	private Tuple<int, int, DateTime, Point> _lastMouseUp;
	private Tuple<int, int, DateTime, Point> _lastMouseOver;
	private Tuple<int, int, DateTime, Point> _lastMouseClick;
	private Tuple<int, int, DateTime, Point> _lastMouseDoubleClick;
	internal Tuple<int, int> SelectionStart;
	internal Tuple<int, int> SelectionEnd;
	internal GridSelectingState SelectState;
	private readonly IDictionary<Tuple<int, int>, Tuple<SourceGrid.Cells.Cell, CellViewModel>> _cellDisplays;

	public BaseAppointmentBook() {
		CanResize = true;
		HasRowHeaders = true;
		HasColHeaders = true;
		HorizontalBorderLine = new BorderLine {
			Color = Color.Lavender,
			DashStyle = System.Drawing.Drawing2D.DashStyle.Dash,
			Padding = 0,
			Width = 1
		};

		SelectedAppointmentBorderLine = new BorderLine {
			Color = Color.Black,
			DashStyle = System.Drawing.Drawing2D.DashStyle.Solid,
			Padding = 0,
			Width = 2
		};

		AppointmentBorderLine = new BorderLine {
			Color = Color.Black,
			DashStyle = System.Drawing.Drawing2D.DashStyle.Solid,
			Padding = 0,
			Width = 1
		};

		MaxColumnWidth = 400;
		MinColumnWidth = 100;
		RowHeaderColumnWidth = 32;
		DistanceMovedToInitiateDragging = 12;
		InitializeComponent();

		_grid.Cursor = null;
		_viewModel = null;
		_cellDisplays = new Dictionary<Tuple<int, int>, Tuple<SourceGrid.Cells.Cell, CellViewModel>>();
		ClearSelection();
	}

	// Properties
	internal BaseAppointmentBookViewModel ViewModel {
		get {
			return _viewModel;
		}
		set {
			_viewModel = value;
			if (_viewModel != null && !base.DesignMode) {
				ClearSelection();
				BindToViewModel();
			}
		}
	}

	public float DistanceMovedToInitiateDragging {
		get; set;
	}

	public bool HasRowHeaders {
		get; set;
	}

	public bool HasColHeaders {
		get; set;
	}

	public virtual bool CanResize {
		get; set;
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
	public BorderLine HorizontalBorderLine {
		get; set;
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
	public BorderLine SelectedAppointmentBorderLine {
		get; set;
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
	public BorderLine AppointmentBorderLine {
		get; set;
	}

	public int MaxColumnWidth {
		get; set;
	}

	public int MinColumnWidth {
		get; set;
	}

	public int RowHeaderColumnWidth {
		get; set;
	}

	public virtual bool IsCellSelected(int col, int row) {
		if (!SelectState.IsIn(GridSelectingState.None, GridSelectingState.Dragging)) {
			if ((SelectionStart.Item1 <= col && col <= SelectionEnd.Item1 &&
				 SelectionStart.Item2 <= row && row <= SelectionEnd.Item2) ||
				(SelectionEnd.Item1 <= col && col <= SelectionStart.Item1 &&
				 SelectionEnd.Item2 <= row && row <= SelectionStart.Item2))
				return true;
		}
		return false;
	}

	public virtual void EndDragging() {
		SelectState = GridSelectingState.None;
	}

	/// <summary>Reports completed appointment-book gestures instead of the internal grid's intermediate selection.</summary>
	protected override bool ShouldObserveChildStateChanges(Control child) {
		return false;
	}

	// Binding
	protected virtual void BindToViewModel() {
		using var update = EnterUpdateScope(FinishedUpdateBehaviour.DoNothing);
		ClearSelection();
		if (ViewModel == null) {
			_grid.Redim(0, 0);
			return;
		}
		var dataSourceRowCount = ViewModel.GetRowCount();
		var dataSourceColCount = ViewModel.GetColumnCount();
		var colIndexOffset = HasRowHeaders ? 2 : 0;
		var rowIndexOffset = HasColHeaders ? 1 : 0;

		_grid.Redim(dataSourceRowCount + rowIndexOffset, dataSourceColCount + colIndexOffset);
		for (var index = 0; index < _grid.ColumnsCount; index++) {
			if (this.MaxColumnWidth > 0)
				_grid.Columns[index].MaximalWidth = this.MaxColumnWidth;
			if (this.MinColumnWidth > 0)
				_grid.Columns[index].MinimalWidth = this.MinColumnWidth;
			_grid.Columns[index].AutoSizeMode = SourceGrid.AutoSizeMode.EnableStretch;
		}
		var rowHeaders = (
			from rowNumber in Enumerable.Range(0, dataSourceRowCount)
			select ViewModel.GetRowHeaderText(rowNumber)
		).ToLookup(x => x.Item1, x => x.Item2);

		if (HasRowHeaders) {
			for (var i = 0; i < _grid.RowsCount; i++)
				_grid[i, 0] = _grid[i, 1] = null;

			_grid[0, 0] = new SourceGrid.Cells.Header("Time");
			_grid[0, 0].ColumnSpan = 2;

			_grid.Columns[0].AutoSizeMode = SourceGrid.AutoSizeMode.None;
			_grid.Columns[0].MinimalWidth = 0;
			_grid.Columns[0].MaximalWidth = RowHeaderColumnWidth;
			_grid.Columns[0].Width = RowHeaderColumnWidth;

			_grid.Columns[1].AutoSizeMode = SourceGrid.AutoSizeMode.None;
			_grid.Columns[1].MinimalWidth = 0;
			_grid.Columns[1].MaximalWidth = RowHeaderColumnWidth;
			_grid.Columns[1].Width = RowHeaderColumnWidth;

			var rowIndex = rowIndexOffset;
			foreach (var rowHeader in rowHeaders) {
				_grid[rowIndex, 0] = new SourceGrid.Cells.RowHeader(rowHeader.Key) { RowSpan = rowHeader.Count() };
				foreach (var secondRowHeader in rowHeader) {
					var secondRowHeaderCell = new SourceGrid.Cells.RowHeader(secondRowHeader);
					secondRowHeaderCell.View = new SuperscriptRowHeaderCellView();
					_grid[rowIndex, 1] = secondRowHeaderCell;
					rowIndex++;
				}
			}
		}

		if (HasColHeaders) {
			for (var i = 0 + colIndexOffset; i < _grid.ColumnsCount; i++)
				_grid[0, i] = null;

			for (var dataColIndex = 0; dataColIndex < dataSourceColCount; dataColIndex++)
				_grid[0, dataColIndex + colIndexOffset] = new SourceGrid.Cells.Header(_viewModel.GetColumnHeaderText(dataColIndex));
		}

		for (var row = 0; row < dataSourceRowCount; row++) {
			for (var col = 0; col < dataSourceColCount; col++) {
				var cellDisplay = _viewModel.GetCellDisplay(col, row);
				var cell = new SourceGrid.Cells.Cell(cellDisplay.Text);
				if (cellDisplay.Traits.HasFlag(CellTraits.Empty))
					BindEmptyCell(cell, cellDisplay, col, row);
				else
					BindCellWithCellDisplay(cell, cellDisplay, col, row);
				_cellDisplays[Tuple.Create(col, row)] = Tuple.Create(cell, cellDisplay);
				cellDisplay.GridCellObject = cell;
				var gridRow = row;
				var gridCol = col;
				TransformModelToGrid(ref gridCol, ref gridRow);
				_grid[gridRow, gridCol] = cell;
			}
		}
		_grid.PerformResize();
	}

	protected virtual void BindEmptyCell(SourceGrid.Cells.Cell cell, CellViewModel cellDisplay, int col, int row) {
		cell.View = new EmptyCellView(this);
		cell.AddController(new EmptyCellController(this));
	}

	protected virtual void BindCellWithCellDisplay(SourceGrid.Cells.Cell cell, CellViewModel cellDisplay, int col, int row) {
		cell.View = new CellView(this, cellDisplay);
	}

	// Cell Methods
	internal void RedrawCell(int col, int row) {
		TransformModelToGrid(ref col, ref row);
		_grid.InvalidateCell(new SourceGrid.Position(row, col));
	}

	internal void RedrawCell(SourceGrid.Cells.Cell cell) {
		var gridCol = cell.Column.Index;
		var gridRow = cell.Row.Index;
		TransformGridToModel(ref gridCol, ref gridRow);
		RedrawCell(gridCol, gridRow);
	}

	internal Tuple<SourceGrid.Cells.Cell, CellViewModel> GetCell(int col, int row) {
		return _cellDisplays[Tuple.Create(col, row)];
	}

	// Event Triggers
	internal void FireMouseDown(int col, int row, MouseEventArgs mouseEvent) {
		var now = DateTime.Now;
		OnCellMouseDown(col, row, mouseEvent);
		if (MouseDownEvent != null)
			MouseDownEvent(this, col, row, mouseEvent);
		_lastMouseDown = Tuple.Create(col, row, now, mouseEvent.Location);
	}

	// Mouse
	protected virtual async void OnCellMouseDown(int col, int row, MouseEventArgs mouseEvent) {
		try {
			switch (SelectState) {
				case GridSelectingState.None:
				case GridSelectingState.Selected:
					if (CanSelect(col, row)) {
						SelectState = GridSelectingState.Selecting;
						SelectionStart = Tuple.Create(col, row);
						SelectionEnd = SelectionStart;
						FireSelectingStarted(col, row);
						FireSelecting(col, row);
					}
					break;
				case GridSelectingState.Selecting:
				default:
					break;
			}
		} catch (Exception error) {
			SystemLog.Exception(error);
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	internal void FireMouseUp(int col, int row, MouseEventArgs mouseEvent) {
		var now = DateTime.Now;
		OnCellMouseUp(col, row, mouseEvent);
		if (MouseUpEvent != null)
			MouseUpEvent(this, col, row, mouseEvent);
		_lastMouseUp = Tuple.Create(col, row, now, mouseEvent.Location);
	}

	protected virtual async void OnCellMouseUp(int col, int row, MouseEventArgs mouseEvent) {
		try {
			const bool allowMultiColumnSelect = false;
			switch (SelectState) {
				case GridSelectingState.Selecting:
					col = allowMultiColumnSelect ? col : SelectionStart.Item1;
					if (CanSelect(col, row))
						SelectionEnd = Tuple.Create(col, row);
					if (AcceptSelection(SelectionStart, SelectionEnd)) {
						SelectState = GridSelectingState.Selected;
						FireSelectionChanged(col, row);
						FireSelectionFinished(SelectionStart.Item1, SelectionStart.Item2, col, row);
					} else {
						ClearSelection();
						FireSelectionFinished(SelectionStart.Item1, SelectionStart.Item2, col, row);
					}
					break;
				case GridSelectingState.Selected:
				case GridSelectingState.None:
				default:
					break;
			}
		} catch (Exception error) {
			SystemLog.Exception(error);
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	// Selection
	public virtual void ClearSelection() {
		var changed = SelectState != GridSelectingState.None || SelectionStart != null || SelectionEnd != null;
		SelectState = GridSelectingState.None;
		SelectionStart = null;
		SelectionEnd = null;
		_grid.Selection.ResetSelection(false);
		FireSelectionCleared();
		if (changed)
			NotifyStateChanged();
	}

	public virtual bool AcceptSelection(Tuple<int, int> selectionStart, Tuple<int, int> selectionEnd) {
		return true;
	}

	internal void FireMouseEnter(int col, int row) {
	}

	internal void FireMouseLeave(int col, int row) {
	}

	internal void FireMouseMove(int col, int row, MouseEventArgs mouseEvent) {
		var now = DateTime.Now;
		OnCellMouseMoved(col, row, mouseEvent);
		if (MouseOverEvent != null)
			MouseOverEvent(this, col, row, mouseEvent);
		_lastMouseOver = Tuple.Create(col, row, now, mouseEvent.Location);
	}

	protected virtual async void OnCellMouseMoved(int col, int row, MouseEventArgs mouseEvent) {
		try {
			const bool allowMultiColumnSelect = false;
			switch (SelectState) {
				case GridSelectingState.Selecting:
					col = allowMultiColumnSelect ? col : SelectionStart.Item1;
					if (CanSelect(col, row))
						SelectionEnd = Tuple.Create(col, row);
					FireSelecting(SelectionEnd.Item1, SelectionEnd.Item2);
					break;
				default:
					if (_lastMouseDown != null) {
						if (mouseEvent.Button.HasFlag(MouseButtons.Left) &&
							_grid.PointToClient(Cursor.Position).DistanceTo(_lastMouseDown.Item4) >= DistanceMovedToInitiateDragging &&
							CanDragCell(_lastMouseDown.Item1, _lastMouseDown.Item2)
						   )
							BeginDragging(_lastMouseDown.Item1, _lastMouseDown.Item2, _lastMouseDown.Item4);
					}
					break;
			}
		} catch (Exception error) {
			SystemLog.Exception(error);
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	public new virtual bool CanSelect(int col, int row) {
		if (col < 0 || row < 0)
			return false;

		switch (SelectState) {
			case GridSelectingState.Selecting:
				// check1 = can only select empty cells
				// check2 = can only select cells within a single column
				// check3 = selection must be continuous

				var check1 = ViewModel.GetCellDisplay(col, row).Traits.HasFlag(CellTraits.Empty);
				var check2 = SelectionEnd.Item1 == col;
				var check3 = Math.Abs(SelectionEnd.Item2 - row) == 1;

				return check1 && check2 && check3;

			default:
				// check1 = can only select empty cells
				return ViewModel.GetCellDisplay(col, row).Traits.HasFlag(CellTraits.Empty);
		}
	}

	// Drag-n-drop
	public virtual bool CanDragCell(int col, int row) {
		return false;
	}

	public virtual void BeginDragging(int col, int row, Point mouseLocation) {
	}

	internal void FireClick(int col, int row) {
		OnCellClick(col, row);
		if (ClickEvent != null)
			ClickEvent(this, col, row);
		_lastMouseClick = _lastMouseDown;
	}

	protected virtual void OnCellClick(int col, int row) {
	}

	internal void FireDoubleClick(int col, int row) {
		OnCellDoubleClick(col, row);
		if (DoubleClickEvent != null)
			DoubleClickEvent(this, col, row);
		_lastMouseDoubleClick = _lastMouseUp;
	}

	protected virtual void OnCellDoubleClick(int col, int row) {
	}

	internal void FireSelectingStarted(int col, int row) {
		OnSelectingStarted(col, row);
		if (SelectionStarted != null)
			SelectionStarted(this, col, row);
	}

	protected virtual void OnSelectingStarted(int col, int row) {
	}

	internal void FireSelecting(int col, int row) {
		OnSelecting(col, row);
		if (Selecting != null)
			Selecting(this, col, row);
	}

	protected virtual void OnSelecting(int col, int row) {
		int startGridCol = SelectionStart.Item1, startGridRow = SelectionStart.Item2, endGridCol = SelectionEnd.Item1, endGridRow = SelectionEnd.Item2;
		TransformModelToGrid(ref startGridCol, ref startGridRow);
		TransformModelToGrid(ref endGridCol, ref endGridRow);
		_grid.Selection.ResetSelection(false);
		_grid.Selection.SelectRange(
new SourceGrid.CellRange(startGridRow, startGridCol, endGridRow, endGridCol), true);
	}

	internal void FireSelectionChanged(int col, int row) {
		OnSelectionChanged(col, row);
		if (SelectionChanged != null)
			SelectionChanged(this, col, row);
	}

	protected virtual void OnSelectionChanged(int col, int row) {
	}

	internal void FireSelectionFinished(int startCol, int startRow, int endCol, int endRow) {
		using var update = EnterUpdateScope(FinishedUpdateBehaviour.NotifyStateChanged);
		OnSelectingFinished(startCol, startRow, endCol, endRow);
		if (SelectionFinished != null)
			SelectionFinished(this, startCol, startRow, endCol, endRow);
		NotifyStateChanged();
	}

	protected virtual void OnSelectingFinished(int startCol, int startRow, int endCol, int endRow) {
	}

	internal void FireSelectionCleared() {
		OnSelectionCleared();
		if (SelectionCleared != null)
			SelectionCleared(this);
	}

	protected virtual void OnSelectionCleared() {
	}

	internal void TransformGridToModel(ref int gridCol, ref int gridRow) {
		if (HasColHeaders)
			gridRow = gridRow - 1;
		if (HasRowHeaders)
			gridCol = gridCol - 2;
	}

	internal void TransformModelToGrid(ref int col, ref int row) {
		if (HasColHeaders)
			row = row + 1;
		if (HasRowHeaders)
			col = col + 2;
	}
}
