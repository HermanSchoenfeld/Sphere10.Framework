// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;
using Sphere10.Framework.Windows.Forms.SourceGrid.Selection;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// A abstract Grid control to support large virtual data. You must override: GetCell, CreateRowsObject, CreateColumnsObject
/// </summary>
[System.ComponentModel.ToolboxItem(false)]
public abstract class GridVirtual : CustomScrollControl {
	/// <summary>
	/// Fired when the selection eith the mouse is finished
	/// </summary>
	/// <param name="e"></param>
	public event EventHandler MouseSelectionFinished;

	public event RangePaintEventHandler RangePaint;

	/// <summary>
	/// Fired when calling SortRangeRows method
	/// </summary>
	[Browsable(true)]
	public event SortRangeRowsEventHandler SortingRangeRows;

	/// <summary>
	/// Fired after calling SortRangeRows method
	/// </summary>
	[Browsable(true)]
	public event SortRangeRowsEventHandler SortedRangeRows;

	// Exception
	/// <summary>
	/// Event fired when an exception is throw in some method that require a notification to the user.
	/// </summary>
	public event ExceptionEventHandler UserException;

	// Default Cell Width/Height
	private int _defaultHeight = 20;
	private int _defaultWidth = 50;
	private int _minimumHeight = 0;
	private int _minimumWidth = 0;
	private bool _autoStretchColumnsToFitWidth = false;
	private bool _autoStretchRowsToFitHeight = false;

	// Drag Fields
	/// <summary>
	/// Indica la cella che ha subito l'ultimo evento di DragEnter
	/// </summary>
	private Position _dragCellPosition = Position.Empty;
	private IGridSelection _selection;
	private GridSelectionMode _selectionMode;
	private CellRange _oldMouseSelectionRange = CellRange.Empty;
	private CellRange _mouseSelectionRange = CellRange.Empty;

	// Special Keys and command keys
	/// <summary>
	/// Stores the active position when user presses shift
	/// </summary>
	private Position _firstCellShiftSelected;
	private bool _overrideCommonCmdKey = true;
	private GridSpecialKeys _gridSpecialKeys = GridSpecialKeys.Default;
	private bool _acceptsInputChar = true;

	// Keyboard
	/// <summary>
	/// Remember the active position, when the key was pressed.
	/// Events are called in this order:
	/// KeyDown
	/// KeyPress
	/// KeyUp
	///
	/// During these three events, the same active position must be kept,
	/// regardless of whether it is changed in any of these events
	///
	/// http://msdn.microsoft.com/en-us/library/system.windows.forms.control.keydown(VS.80).aspx
	/// </summary>
	private Position _keybordActivePosition;

	// Focus
	private bool _autoFocusFirstCellOnEnter = true;

	// Controls linked
	private LinkedControlsList _linkedControls;

	// Rows, Columns
	private int _fixedRows = 0;
	private int _fixedCols = 0;
	private RowsBase _rows;
	private ColumnsBase _columns;

	// Right To Left Support
	//With Visual Studio 2005 probably I can use the new RightToLeftLayout and TextRenderer class
	// se also
	//		http://www.microsoft.com/middleeast/msdn/visualstudio2005.aspx
	//		http://www.microsoft.com/middleeast/msdn/mirror.aspx
	//      http://msdn2.microsoft.com/en-us/library/fh376txk.aspx
	//      http://msdn2.microsoft.com/en-us/library/7d3337xw.aspx
	//      "How to: Create Mirrored Windows Forms and Controls"  http://msdn2.microsoft.com/en-us/library/xwbz5ws0.aspx

	private bool _mirrored = false;

	// ToolTip
	private System.Windows.Forms.ToolTip _toolTip;

	// Decorator List
	private Decorators.DecoratorList _decorators = new Decorators.DecoratorList();

	// Controllers
	private Cells.Controllers.ControllerContainer _controller = new SourceGrid.Cells.Controllers.ControllerContainer();

	// Clipboard
	private ClipboardMode _clipboardMode = ClipboardMode.None;
	private bool _clipboardUseOnlyActivePosition = false;

	// Mouse cell positions and mouse selection
	/// <summary>
	/// Represents the cell that receive the mouse down event
	/// </summary>
	protected Position MouseDownCellPosition = Position.Empty;
	protected Position MouseOverCellPosition = Position.Empty;

	// Constructor
	/// <summary>
	/// Grid constructor
	/// </summary>
	public GridVirtual() {
		SetStyle(ControlStyles.Selectable, true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		SetStyle(ControlStyles.DoubleBuffer, true);
		SetStyle(ControlStyles.SupportsTransparentBackColor, true);
		SetStyle(ControlStyles.ContainerControl, true);

		TabStop = true;
		EnableSort = true;

		_rows = CreateRowsObject();
		_columns = CreateColumnsObject();

		SelectionMode = GridSelectionMode.Cell;

		//Create the Controller list for the Cells (the order is important because is the same order that the events will be dispatched)
		Controller.AddController(Cells.Controllers.StandardBehavior.Default);
		Controller.AddController(Cells.Controllers.MouseSelection.Default);
		Controller.AddController(Cells.Controllers.CellEventDispatcher.Default);

		_linkedControls = new LinkedControlsList(this);

		//ToolTip
		_toolTip = new System.Windows.Forms.ToolTip();
		ToolTipText = "";
	}

	/// <summary>
	/// Indicates the default height of new row
	/// </summary>
	[DefaultValue(20)]
	public int DefaultHeight {
		get {
			return _defaultHeight;
		}
		set {
			_defaultHeight = value;
		}
	}

	/// <summary>
	/// Indicates the default width of new column
	/// </summary>
	[DefaultValue(50)]
	public int DefaultWidth {
		get {
			return _defaultWidth;
		}
		set {
			_defaultWidth = value;
		}
	}

	/// <summary>
	/// Indicates the minimum height of the rows
	/// </summary>
	[DefaultValue(0)]
	public int MinimumHeight {
		get {
			return _minimumHeight;
		}
		set {
			_minimumHeight = value;
		}
	}

	/// <summary>
	/// Indicates the minimum width of the columns
	/// </summary>
	[DefaultValue(0)]
	public int MinimumWidth {
		get {
			return _minimumWidth;
		}
		set {
			_minimumWidth = value;
		}
	}

	/// <summary>
	/// True to auto stretch the columns width to always fit the available space, also when the contents of the cell is smaller.
	/// False to leave the original width of the columns
	/// </summary>
	[DefaultValue(false)]
	public bool AutoStretchColumnsToFitWidth {
		get {
			return _autoStretchColumnsToFitWidth;
		}
		set {
			_autoStretchColumnsToFitWidth = value;
		}
	}

	/// <summary>
	/// True to auto stretch the rows height to always fit the available space, also when the contents of the cell is smaller.
	/// False to leave the original height of the rows
	/// </summary>
	[DefaultValue(false)]
	public bool AutoStretchRowsToFitHeight {
		get {
			return _autoStretchRowsToFitHeight;
		}
		set {
			_autoStretchRowsToFitHeight = value;
		}
	}

	/// <summary>
	/// The last cell that has received a DragEnter event.
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Position DragCellPosition {
		get {
			return _dragCellPosition;
		}
	}

	/// <summary>
	/// Gets or sets the Selection object that represents the selected cells and the active cell.
	/// Use the SelectionMode property to set the type of selection (free, by row, by column).
	/// Override the CreateSelectionObject method to create a custom Selection class for special needs.
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IGridSelection Selection {
		get {
			return _selection;
		}
		protected set {
			if (_selection != null)
				_selection.UnBindToGrid();

			_selection = value;

			if (_selection != null)
				_selection.BindToGrid(this);
		}
	}

	/// <summary>
	/// Gets or sets the selection mode. Changing this property cause the Selection object to be recreated, for this reason remember to set this property at the beginning of your code.
	/// </summary>
	public GridSelectionMode SelectionMode {
		get {
			return _selectionMode;
		}
		set {
			_selectionMode = value;
			Selection = CreateSelectionObject();
			Invalidate(true);
		}
	}

	/// <summary>
	/// Represents the cell that have received the MouseDown event. You can use this cell for contextmenu logic. Can be null.
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Position MouseDownPosition {
		get {
			return MouseDownCellPosition;
		}
	}

	/// <summary>
	/// The cell position currently under the mouse cursor (row, col).
	/// If you MouseDown on a cell this cell is the MouseCellPosition until an MouseUp is fired
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Position MouseCellPosition {
		get {
			return MouseOverCellPosition;
		}
	}

	/// <summary>
	/// Returns the cells that are selected with the mouse. Range.Empty if no cells are selected. Consider that this method returns valid cells only during the mouse down operations, when release the mouse the cells are selected and you can read them using Grid.Selection object.
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual CellRange MouseSelectionRange {
		get {
			return _mouseSelectionRange;
		}
	}

	/// <summary>
	/// True to override with the ProcessCmdKey the common Command Key: Enter, Escape, Tab
	/// </summary>
	[DefaultValue(true)]
	public bool OverrideCommonCmdKey {
		get {
			return _overrideCommonCmdKey;
		}
		set {
			_overrideCommonCmdKey = value;
		}
	}

	/// <summary>
	/// Special keys that the grid can handle. You can change this enum to block or allow some special keys function. For example to disable Ctrl+C Copy operation remove from this enum the GridSpecialKeys.Ctrl_C.
	/// </summary>
	[DefaultValue(GridSpecialKeys.Default)]
	public GridSpecialKeys SpecialKeys {
		get {
			return _gridSpecialKeys;
		}
		set {
			_gridSpecialKeys = value;
		}
	}

	/// <summary>
	/// True accepts input char when the grid has the focus. Used for example to start the edit operation without processing the char.
	/// If you set this property to false when the character is sended to the windows forms handler and can be used for example to execute directly some access key (shortcut keys).
	/// Default is true.
	/// </summary>
	[DefaultValue(true)]
	public bool AcceptsInputChar {
		get {
			return _acceptsInputChar;
		}
		set {
			_acceptsInputChar = value;
		}
	}

	/// <summary>
	/// List of controls that are linked to a specific cell position.
	/// For example is used for editors controls. Key=Control, Value=Position.
	/// The controls are automatically removed from the list when they are removed from the Grid.Controls
	/// collection
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public LinkedControlsList LinkedControls {
		get {
			return _linkedControls;
		}
	}

	// Sort Range
	/// <summary>
	/// Tells if grid has sorting enabled.
	/// Note that you can still call manually functions to sort data,
	/// such as SortRangeRows, or even sort the data
	/// in your own data source. This property is for UI only
	/// </summary>
	public abstract bool EnableSort {
		get; set;
	}

	/// <summary>
	/// Gets or Sets how many rows are not scrollable
	/// </summary>
	[DefaultValue(0)]
	public int FixedRows {
		get {
			return _fixedRows;
		}
		set {
			if (_fixedRows != value) {
				_fixedRows = value;
				OnCellsAreaChanged();
			}
		}
	}

	public int ActualFixedRows {
		get {
			if (FixedRows > Rows.Count)
				return Rows.Count;
			return FixedRows;
		}
	}

	/// <summary>
	/// Gets or Sets how many cols are not scrollable
	/// </summary>
	[DefaultValue(0)]
	public int FixedColumns {
		get {
			return _fixedCols;
		}
		set {
			if (_fixedCols != value) {
				_fixedCols = value;
				OnCellsAreaChanged();
			}
		}
	}

	public int ActualFixedColumns {
		get {
			if (FixedColumns > Columns.Count)
				return Columns.Count;
			return FixedColumns;
		}
	}

	/// <summary>
	/// RowsCount informations
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public RowsBase Rows {
		get {
			return _rows;
		}
	}

	/// <summary>
	/// Columns informations
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ColumnsBase Columns {
		get {
			return _columns;
		}
	}

	/// <summary>
	/// Returns a Range that represents the complete cells of the grid
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public CellRange CompleteRange {
		get {
			if (Rows.Count > 0 && Columns.Count > 0)
				return new CellRange(0, 0, Rows.Count - 1, Columns.Count - 1);
			return CellRange.Empty;
		}
	}

	[Description("Change the right-to-left layout."), DefaultValue(false),
											Localizable(true), Category("Appearance"), Browsable(true)]
	public bool Mirrored {
		get {
			return _mirrored;
		}
		set {
			if (_mirrored != value) {
				_mirrored = value;
				base.OnRightToLeftChanged(EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Hide the RightToLeft property and returns always No.
	/// </summary>
	[Browsable(false)]
	[DefaultValue(RightToLeft.No)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override RightToLeft RightToLeft {
		get {
			return RightToLeft.No;
		}
		set {
		}
	}

	/// <summary>
	/// ToolTip text
	/// </summary>
	public string ToolTipText {
		get {
			return _toolTip.GetToolTip(this);
		}
		set {
			_toolTip.SetToolTip(this, value);
		}
	}

	/// <summary>
	/// Gets the tooltip control used when showing tooltip text.
	/// </summary>
	public ToolTip ToolTip {
		get {
			return _toolTip;
		}
	}

	/// <summary>
	/// Grid decorators, used to draw additiona contents on the drawing surface
	/// </summary>
	public Decorators.DecoratorList Decorators {
		get {
			return _decorators;
		}
	}

	public Cells.Controllers.ControllerContainer Controller {
		get {
			return _controller;
		}
	}

	/// <summary>
	/// Gets or sets the clipboard operation mode. Default is none
	/// </summary>
	[DefaultValue(ClipboardMode.None)]
	public ClipboardMode ClipboardMode {
		get {
			return _clipboardMode;
		}
		set {
			_clipboardMode = value;
		}
	}

	/// <summary>
	/// Gets or sets if only active cell should be used for clipboard operation. Default is whole selection
	/// </summary>
	[DefaultValue(false)]
	public bool ClipboardUseOnlyActivePosition {
		get {
			return _clipboardUseOnlyActivePosition;
		}
		set {
			_clipboardUseOnlyActivePosition = value;
		}
	}

	protected override CreateParams CreateParams {
		get {
			CreateParams cP;
			cP = base.CreateParams;
			if (Mirrored)
				cP.ExStyle = cP.ExStyle | WinAPI.USER32.WS_EX_LAYOUTRTL;
			return cP;
		}
	}

	// AutoSize
	/// <summary>
	/// Auto size the columns and the rows speified
	/// </summary>
	/// <param name="rangeToAutoSize"></param>
	public virtual void AutoSizeCells(CellRange rangeToAutoSize) {
		SuspendLayout();
		if (rangeToAutoSize.IsEmpty() == false) {
			Rows.SuspendLayout();
			Columns.SuspendLayout();
			try {
				for (var c = rangeToAutoSize.End.Column; c >= rangeToAutoSize.Start.Column; c--)
					Columns.AutoSizeColumn(c, false, rangeToAutoSize.Start.Row, rangeToAutoSize.End.Row);
				for (var r = rangeToAutoSize.End.Row; r >= rangeToAutoSize.Start.Row; r--)
					Rows.AutoSizeRow(r, false, rangeToAutoSize.Start.Column, rangeToAutoSize.End.Column);
			} finally {
				//aggiorno top e left
				Rows.ResumeLayout();
				Columns.ResumeLayout();
			}

			//Call this method after calculated Bottom and Right
			if (AutoStretchColumnsToFitWidth)
				Columns.StretchToFit();
			if (AutoStretchRowsToFitHeight)
				Rows.StretchToFit();
		}
		ResumeLayout(false);
	}

	/// <summary>
	/// Auto size all the columns and all the rows with the required width and height
	/// </summary>
	public virtual void AutoSizeCells() {
		AutoSizeCells(CompleteRange);
	}

	/// <summary>
	/// Indicates if the specified cell is visible.
	/// </summary>
	/// <param name="position"></param>
	/// <param name="partial">True to returns also partial visible cells</param>
	/// <returns></returns>
	public bool IsCellVisible(Position position, bool partial) {
		Point scrollPosition;
		return !(GetScrollPositionToShowCell(position, partial, out scrollPosition));
	}

	/// <summary>
	/// Scroll the view to show the cell passed. Ensure that if the cell if invisible or partial visible it will be totally visible
	/// </summary>
	/// <param name="position"></param>
	/// <param name="ignorePartial">true to ignore and consider already visible partial visible cells</param>
	/// <returns>Returns true if the Cell passed was already visible, otherwise false</returns>
	public bool ShowCell(Position position, bool ignorePartial) {
		Point newCustomScrollPosition;
		if (GetScrollPositionToShowCell(position, ignorePartial, out newCustomScrollPosition)) {
			CustomScrollPosition = newCustomScrollPosition;
			//il problema di refresh si verifica solo in caso di FixedRows e ColumnsCount maggiori di 0
			if (FixedRows > 0 || FixedColumns > 0)
				Invalidate();

			return false;
		}
		return true;
	}

	/// <summary>
	/// Force a cell to redraw.
	/// </summary>
	/// <param name="position"></param>
	public virtual void InvalidateCell(Position position) {
		InvalidateRange(new CellRange(position));
	}

	/// <summary>
	/// Force a range of cells to redraw.
	/// </summary>
	/// <param name="range"></param>
	public void InvalidateRange(CellRange range) {
		if (range.IsEmpty())
			return;
		var types = new CellPositionType[] {
			CellPositionType.FixedLeft,
			CellPositionType.FixedTop,
			CellPositionType.FixedTopLeft,
			CellPositionType.Scrollable
		};

		// invalidate eaach position type individually
		// cliping against given range
		foreach (var type in types) {
			var visibleRange = RangeAtArea(type);
			// clip our range with visible range
			// so that we wount loop through thousands of rows
			var clippedRange = CellRange.Intersect(range, visibleRange);
			if (clippedRange.IsEmpty() == false) {
				var gridRectangle = RangeToRectangle(clippedRange);
				if (gridRectangle.IsEmpty == false)
					Invalidate(gridRectangle, true);
			}
		}
	}

	/// <summary>
	/// Returns a visible area range, which is a bit
	/// expanded to the left and top
	/// </summary>
	/// <param name="areaType"></param>
	/// <returns></returns>
	public CellRange RangeAtAreaExpanded(CellPositionType areaType) {
		var range = RangeAtArea(areaType);
		// decrease visible row by 1, so that selection top row would not be drawn
		// if selection start somewhere outside the screen
		if (range.IsEmpty() == false) {
			var startRow = range.Start.Row > 0 ? range.Start.Row - 1 : 0;
			var startCol = range.Start.Column > 0 ? range.Start.Column - 1 : 0;
			range = new CellRange(
new Position(startRow, startCol), range.End);
		}
		return range;
	}

	/// <summary>
	/// Move the scrollbars to the direction specified by the point specified.
	/// Method used by the Mouse multi selection (MouseSelection.cs).
	/// Scroll the grid only if the specified location is outside the visible area.
	/// </summary>
	/// <param name="mousePoint"></param>
	public void ScrollOnPoint(Point mousePoint) {
		//Scroll if necesary
		var scrollRect = GetScrollableArea();

		var last = this.Columns.LastVisibleScrollableColumn;
		if (mousePoint.X > scrollRect.Right && (!last.HasValue ||
												last.Value < this.Columns.Count - 1 || this.Columns.GetRight(last.Value) > scrollRect.Right))
			CustomScrollLineRight();
		last = this.Rows.LastVisibleScrollableRow;
		if (mousePoint.Y > scrollRect.Bottom && (!last.HasValue ||
													last.Value < this.Rows.Count - 1 || this.Rows.GetBottom(last.Value) > scrollRect.Bottom))
			CustomScrollLineDown();
		if (mousePoint.X < scrollRect.Left)
			CustomScrollLineLeft();
		if (mousePoint.Y < scrollRect.Top)
			CustomScrollLineUp();
	}

	public Rectangle GetFixedTopLeftArea() {
		var rect = GetScrollableArea();

		return new Rectangle(0, 0, rect.Left, rect.Top);
	}

	public Rectangle GetFixedTopArea() {
		var rect = GetScrollableArea();

		return new Rectangle(rect.Left, 0, rect.Width, rect.Top);
	}

	public Rectangle GetFixedLeftArea() {
		var rect = GetScrollableArea();

		return new Rectangle(0, rect.Top, rect.Left, rect.Height);
	}

	public Rectangle GetScrollableArea() {
		var rect = DisplayRectangle;

		var fixedRows = ActualFixedRows;

		var fixedColumns = ActualFixedColumns;

		if (fixedRows > 0)
			rect.Y = Rows.GetAbsoluteBottom(fixedRows - 1);
		else
			rect.Y = 0;
		rect.Height -= rect.Y;

		if (fixedColumns > 0)
			rect.X = Columns.GetAbsoluteRight(fixedColumns - 1);
		else
			rect.X = 0;
		rect.Width -= rect.X;

		return rect;
	}

	// Row/Column Span
	/// <summary>
	/// This method converts a Position to the real range of the cell. This is usefull when RowSpan or ColumnSpan is greater than 1.
	/// For example suppose to have at grid[0,0] a cell with ColumnSpan equal to 2. If you call this method with the position 0,0 returns 0,0-0,1 and if you call this method with 0,1 return again 0,0-0,1.
	/// </summary>
	/// <returns></returns>
	public virtual CellRange RangeToCellRange(CellRange range) {
		return new CellRange(range.Start, range.End);
	}

	/// <summary>
	/// Fired when the cell in the drag events change. For internal use only.
	/// </summary>
	/// <param name="cell"></param>
	/// <param name="dragEventArgs"></param>
	public virtual void ChangeDragCell(CellContext cell, DragEventArgs dragEventArgs) {
		if (cell.Position != _dragCellPosition) {
			if (_dragCellPosition.IsEmpty() == false)
				Controller.OnDragLeave(new CellContext(this, _dragCellPosition, GetCell(_dragCellPosition)), dragEventArgs);

			if (cell.Position.IsEmpty() == false)
				Controller.OnDragEnter(cell, dragEventArgs);

			_dragCellPosition = cell.Position;
		}
	}

	/// <summary>
	/// Fired when the mouse selection finish. See also MouseSelectionRange.
	/// </summary>
	///
	public void MouseSelectionFinish() {
		if (_mouseSelectionRange != CellRange.Empty)
			OnMouseSelectionFinish(new RangeEventArgs(_oldMouseSelectionRange));

		_mouseSelectionRange = CellRange.Empty;
	}

	/// <summary>
	/// Fired when the corner of the mouse selection change. For internal use only.
	/// </summary>
	/// <param name="corner"></param>
	public virtual void ChangeMouseSelectionCorner(Position corner) {
		var newMouseSelection = new CellRange(Selection.ActivePosition, corner);

		var bChange = false;
		if (_mouseSelectionRange != newMouseSelection) {
			_mouseSelectionRange = newMouseSelection;
			bChange = true;
		}

		if (bChange)
			OnMouseSelectionChange(EventArgs.Empty);
	}

	/// <summary>
	/// Force to recalculate scrollbars and panels location. Used usually after changing width and height of the columns / rows, or after adding or removing rows and columns.
	/// </summary>
	public virtual void OnCellsAreaChanged() {
		SuspendLayout();
		SetScrollArea();
		CheckPositions();
		ArrangeLinkedControls();
		ResumeLayout(true);
	}

	// CheckPositions
	/// <summary>
	/// Check if the positions saved are still valid, for example if all the selected cells are still valid positions, if not the selection are removed without calling any other methods.
	/// </summary>
	public virtual void CheckPositions() {
		var complete = CompleteRange;

		if (MouseOverCellPosition.IsEmpty() == false &&
			CompleteRange.Contains(MouseOverCellPosition) == false)
			MouseOverCellPosition = Position.Empty;

		if (MouseDownCellPosition.IsEmpty() == false &&
			CompleteRange.Contains(MouseDownCellPosition) == false)
			MouseDownCellPosition = Position.Empty;

		if (_dragCellPosition.IsEmpty() == false &&
			CompleteRange.Contains(_dragCellPosition) == false)
			_dragCellPosition = Position.Empty;

		//If the selection contains some invalid cells reset the selection state.
		var completeRegion = new RangeRegion(complete);
		if (
			(Selection.ActivePosition.IsEmpty() == false && complete.Contains(Selection.ActivePosition) == false) ||
			(Selection.IsEmpty() == false && completeRegion.Contains(Selection.GetSelectionRegion()) == false)
		)
			Selection.ResetSelection(false);
	}

	/// <summary>
	/// Sort a range of the grid
	/// </summary>
	/// <param name="rangeToSort">Range to sort</param>
	/// <param name="keyColumn">Index of the column relative to the grid to use as sort keys, must be between start and end col of the range</param>
	/// <param name="ascending">Ascending true, Descending false</param>
	/// <param name="cellComparer">CellComparer, if null the default comparer will be used</param>
	public void SortRangeRows(IRangeLoader rangeToSort, int keyColumn, bool ascending, IComparer cellComparer) {
		var range = rangeToSort.GetRange(this);
		SortRangeRows(range, keyColumn, ascending, cellComparer);
	}

	/// <summary>
	/// Sort a range of the grid.
	/// </summary>
	/// <param name="range"></param>
	/// <param name="keyColumn">Index of the column relative to the grid to use as sort keys, must be between start and end col</param>
	/// <param name="ascending">Ascending true, Descending false</param>
	/// <param name="cellComparer">CellComparer, if null the default ValueCellComparer comparer will be used</param>
	public void SortRangeRows(CellRange range, int keyColumn, bool ascending, IComparer cellComparer) {
		var eventArgs = new SortRangeRowsEventArgs(range, keyColumn, ascending, cellComparer);

		if (SortingRangeRows != null)
			SortingRangeRows(this, eventArgs);

		OnSortingRangeRows(eventArgs);

		if (SortedRangeRows != null)
			SortedRangeRows(this, eventArgs);

		OnSortedRangeRows(eventArgs);
	}

	/// <summary>
	/// Returns all the cells at specified row position
	/// </summary>
	/// <param name="rowIndex"></param>
	/// <returns></returns>
	public virtual Cells.ICellVirtual[] GetCellsAtRow(int rowIndex) {
		var cells = new Cells.ICellVirtual[Columns.Count];
		for (var c = 0; c < Columns.Count; c++)
			cells[c] = GetCell(rowIndex, c);

		return cells;
	}

	/// <summary>
	/// Returns all the cells at specified column position
	/// </summary>
	/// <param name="columnIndex"></param>
	/// <returns></returns>
	public virtual Cells.ICellVirtual[] GetCellsAtColumn(int columnIndex) {
		var cells = new Cells.ICellVirtual[Rows.Count];
		for (var r = 0; r < Rows.Count; r++)
			cells[r] = GetCell(r, columnIndex);

		return cells;
	}

	/// <summary>
	/// Event fired when an exception is throw in some method that require a notification to the user.
	/// If not handled by the user (Handled property = false) a MessageBox is used to display the exception.
	/// </summary>
	/// <param name="e"></param>
	public virtual async void OnUserException(ExceptionEventArgs e) {
#if DEBUG
		System.Diagnostics.Debug.WriteLine("Exception on editing cell: " + e.Exception.ToString());
#endif

		if (UserException != null)
			UserException(this, e);

		if (e.Handled == false) {
			e.Handled = true;
			await ExceptionDialog.ShowAsync(this, "Error", e.Exception);
		}
	}

	// Dispose
	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	protected override void Dispose(bool disposing) {
		if (disposing) {
		}
		base.Dispose(disposing);
	}

	//MICK(13)
	protected override int GetActualFixedRows() {
		return ActualFixedRows;
	}

	/// <summary>
	/// Invalidate the cells
	/// </summary>
	protected override void InvalidateScrollableArea() {
		Invalidate(true);
	}

	/// <summary>
	/// Processes a command key.
	/// </summary>
	/// <param name="msg"></param>
	/// <param name="keyData"></param>
	/// <returns></returns>
	protected override bool ProcessCmdKey(
ref Message msg, Keys keyData) {
		// if shift pressed, remember which cell is selected
		if (keyData == (Keys.Shift | Keys.ShiftKey))
			_firstCellShiftSelected = Selection.ActivePosition;

		if ((keyData == Keys.Enter ||
				keyData == Keys.Escape ||
				keyData == Keys.Tab ||
				keyData == (Keys.Tab | Keys.Shift)) &&
			OverrideCommonCmdKey) {
			var args = new KeyEventArgs(keyData);
			OnKeyDown(args);
			if (args.Handled)
				return true;
			return base.ProcessCmdKey(ref msg, keyData);
		}
		return base.ProcessCmdKey(ref msg, keyData);
	}

	/// <summary>
	/// Allow the grid to handle specials keys like Arrows and Tab. See also Grid.SpecialKeys
	/// </summary>
	/// <param name="keyData"></param>
	/// <returns></returns>
	protected override bool IsInputKey(Keys keyData) {
		//Handle arrows and tab keys only if OverrideCommonCmdKey
		if (OverrideCommonCmdKey) {
			if ((SpecialKeys & GridSpecialKeys.Arrows) == GridSpecialKeys.Arrows) {
				switch (keyData) {
					case Keys.Up:
					case Keys.Down:
					case Keys.Left:
					case Keys.Right:
					//altrimenti venivano (per qualche strana ragione) selezionate le scrollbars
					case (Keys.Up | Keys.Shift):
					case (Keys.Down | Keys.Shift):
					case (Keys.Left | Keys.Shift):
					case (Keys.Right | Keys.Shift):
						return true;
				}
			}

			if ((SpecialKeys & GridSpecialKeys.Tab) == GridSpecialKeys.Tab) {
				switch (keyData) {
					case Keys.Tab:
					case (Keys.Tab | Keys.Shift):
						return true;
				}
			}
		}

		return base.IsInputKey(keyData);
	}

	/// <summary>
	/// IsInputChar method.
	/// </summary>
	/// <param name="charCode"></param>
	/// <returns></returns>
	protected override bool IsInputChar(char charCode) {
		return AcceptsInputChar;
	}

	// Paint
	protected override void OnPaint(PaintEventArgs e) {
		base.OnPaint(e);

		//NOTE: For now I draw all the visible cells (not only the invalidated cells).
		using (var grCache = new Sphere10.Framework.Drawing.GraphicsCache(e.Graphics, e.ClipRectangle)) {
			foreach (var rng in GetVisibleRegion())
				OnRangePaint(new RangePaintEventArgs(this, grCache, rng));
		}
	}

	// Mouse
	protected override void OnMouseMove(MouseEventArgs e) {
		base.OnMouseMove(e);

		var mousePosition = PositionAtPoint(new Point(e.X, e.Y));
		var mouseCell = GetCell(mousePosition);

		//Call MouseMove on the cell that receive tha MouseDown event
		if (MouseDownPosition.IsEmpty() == false) {
			var mouseDownCell = GetCell(MouseDownPosition);
			if (mouseDownCell != null)
				Controller.OnMouseMove(new CellContext(this, MouseDownPosition, mouseDownCell), e);
		} else {
			//se non ho nessuna cella attualmente che ha ricevuto un mousedown, l'evento di MouseMove viene segnalato sulla cella correntemente sotto il Mouse

			// se non c'è nessuna cella MouseDown cambio la cella corrente sotto il Mouse
			ChangeMouseCell(mousePosition); //in ogni caso cambio la cella corrente

			if (mousePosition.IsEmpty() == false && mouseCell != null)              // I call MouseMove on the current cell only if there aren't any cells under the mouse
				Controller.OnMouseMove(new CellContext(this, mousePosition, mouseCell), e);
		}
	}

	protected override void OnMouseDown(MouseEventArgs e) {
		base.OnMouseDown(e);

		//verifico che l'eventuale edit sia terminato altrimenti esco
		if (Selection.ActivePosition.IsEmpty() == false) {
			var focusCell = new CellContext(this, Selection.ActivePosition);
			if (focusCell.Cell != null && focusCell.IsEditing()) {
				if (focusCell.EndEdit(false) == false)
					return;
			}
		}

		//scateno eventi di MouseDown
		var position = PositionAtPoint(new Point(e.X, e.Y));
		if (position.IsEmpty() == false) {
			var cellMouseDown = GetCell(position);
			if (cellMouseDown != null) {
				ChangeMouseDownCell(position, position);

				//Cell.OnMouseDown
				var cellContext = new CellContext(this, position, cellMouseDown);
				Controller.OnMouseDown(cellContext, e);
			}
		} else
			ChangeMouseDownCell(Position.Empty, Position.Empty);
	}

	protected override void OnMouseUp(MouseEventArgs e) {
		base.OnMouseUp(e);

		if (MouseDownPosition.IsEmpty() == false) {
			var mouseDownCell = GetCell(MouseDownPosition);
			if (mouseDownCell != null)
				Controller.OnMouseUp(new CellContext(this, MouseDownPosition, mouseDownCell), e);

			ChangeMouseDownCell(Position.Empty, PositionAtPoint(new Point(e.X, e.Y)));
		}
	}

	/// <summary>
	/// Change the cell currently under the mouse
	/// </summary>
	/// <param name="mouseDownCell"></param>
	/// <param name="mouseCell"></param>
	public virtual void ChangeMouseDownCell(Position mouseDownCell, Position mouseCell) {
		MouseDownCellPosition = mouseDownCell;

		ChangeMouseCell(mouseCell);
	}

	protected override void OnMouseClick(MouseEventArgs e) {
		base.OnMouseClick(e);

		var clickPosition = PositionAtPoint(PointToClient(Control.MousePosition));

		//Se ho precedentemente scatenato un MouseDown su una cella
		// e se questa corrisponde alla cella sotto il puntatore del mouse (non posso usare MouseCellPosition perchè questa viene aggiornata solo quando non si ha una cella come MouseDownPosition
		if (MouseDownPosition.IsEmpty() == false &&
			MouseDownPosition == clickPosition) {
			var mouseDownCell = GetCell(MouseDownPosition);
			if (mouseDownCell != null)
				Controller.OnClick(new CellContext(this, MouseDownPosition, mouseDownCell), e);
		}
	}

	protected override void OnMouseDoubleClick(MouseEventArgs e) {
		var clickPosition = PositionAtPoint(PointToClient(Control.MousePosition));
		HandleMouseDoubleClick(clickPosition, e);
		base.OnMouseDoubleClick(e);
	}

	/// <summary>
	/// Returns the cell at the specified grid view relative point (the point must be relative to the grid display region), SearchInFixedCells = true. Return Position.Empty if no valid cells are found.
	/// </summary>
	/// <param name="point">Point relative to the DisplayRectangle area.</param>
	/// <returns></returns>
	public virtual Position PositionAtPoint(Point point) {
		var row = Rows.RowAtPoint(point.Y);
		if (row == null)
			return Position.Empty;

		var column = Columns.ColumnAtPoint(point.X);
		if (column == null)
			return Position.Empty;

		var realPos = new Position(row.Value, column.Value);

		//Returns the logical position (using row/col span if present)
		return PositionToStartPosition(realPos);
	}

	/// <summary>
	/// This method converts a Position to the real start position of cell. This is usefull when RowSpan or ColumnSPan is greater than 1.
	/// For example suppose to have at grid[0,0] a cell with ColumnSpan equal to 2. If you call this method with the position 0,0 returns 0,0 and if you call this method with 0,1 return again 0,0.
	/// Get the real position for the specified position. For example when p_Position is a merged cell this method returns the starting position of the merged cells.
	/// Usually this method returns the same cell specified as parameter. This method is used for processing arrow keys, to find a valid cell when the focus is in a merged cell.
	/// For this class returns always p_Position.
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public Position PositionToStartPosition(Position position) {
		return PositionToCellRange(position).Start;
	}

	/// <summary>
	/// Fired when a user scroll with the mouse wheel
	/// </summary>
	/// <param name="e"></param>
	protected override void OnMouseWheel(MouseEventArgs e) {
		base.OnMouseWheel(e);

		CustomScrollWheel(e.Delta);
	}

	protected override void OnKeyDown(KeyEventArgs e) {
		base.OnKeyDown(e);
		_keybordActivePosition = Selection.ActivePosition;

		if (_keybordActivePosition.IsEmpty() == false) {
			var focusCell = GetCell(_keybordActivePosition);
			if (focusCell != null)
				Controller.OnKeyDown(new CellContext(this, _keybordActivePosition, focusCell), e);
		}

		if (e.Handled == false)
			ProcessSpecialGridKey(e);
	}

	/// <summary>
	/// Process Delete, Ctrl+C, Ctrl+V, Up, Down, Left, Right, Tab keys
	/// </summary>
	/// <param name="e"></param>
	public virtual void ProcessSpecialGridKey(KeyEventArgs e) {
		if (e.Handled)
			return;

		bool enableArrows, enableTab, enablePageDownUp;
		enableArrows = enableTab = enablePageDownUp = false;

		if ((SpecialKeys & GridSpecialKeys.Arrows) == GridSpecialKeys.Arrows)
			enableArrows = true;
		if ((SpecialKeys & GridSpecialKeys.PageDownUp) == GridSpecialKeys.PageDownUp)
			enablePageDownUp = true;
		if ((SpecialKeys & GridSpecialKeys.Tab) == GridSpecialKeys.Tab)
			enableTab = true;

		var enableEscape = false;
		if ((SpecialKeys & GridSpecialKeys.Escape) == GridSpecialKeys.Escape)
			enableEscape = true;
		var enableEnter = false;
		if ((SpecialKeys & GridSpecialKeys.Enter) == GridSpecialKeys.Enter)
			enableEnter = true;

		// Processing keys
		//Escape
		if (e.KeyCode == Keys.Escape && enableEscape) {
			var focusCellContext = new CellContext(this, Selection.ActivePosition);
			if (focusCellContext.Cell != null && focusCellContext.IsEditing()) {
				if (focusCellContext.EndEdit(true))
					e.Handled = true;
			}
		}

		//Enter
		if (e.KeyCode == Keys.Enter && enableEnter) {
			var focusCellContext = new CellContext(this, Selection.ActivePosition);
			if (focusCellContext.Cell != null && focusCellContext.IsEditing()) {
				focusCellContext.EndEdit(false);

				e.Handled = true;
			}
		}

		//Tab
		if (e.KeyCode == Keys.Tab && enableTab) {
			var focusCellContext = new CellContext(this, Selection.ActivePosition);
			if (focusCellContext.Cell != null && focusCellContext.IsEditing()) {
				//se l'editing non riesce considero il tasto processato
				// altrimenti no, in questo modo il tab ha effetto anche per lo spostamento
				if (focusCellContext.EndEdit(false) == false) {
					e.Handled = true;
					return;
				}
			}
		}

		// Navigate keys: arrows, tab and PgDown/Up
		var shiftPressed = e.Modifiers == Keys.Shift;
		var resetSelection = shiftPressed == false;
		if (e.KeyCode == Keys.Down && enableArrows) {
			Selection.MoveActiveCell(1, 0, resetSelection);
			e.Handled = true;
		} else if (e.KeyCode == Keys.Up && enableArrows) {
			Selection.MoveActiveCell(-1, 0, resetSelection);
			e.Handled = true;
		} else if (e.KeyCode == Keys.Right && enableArrows) {
			Selection.MoveActiveCell(0, 1, resetSelection);
			e.Handled = true;
		} else if (e.KeyCode == Keys.Left && enableArrows) {
			Selection.MoveActiveCell(0, -1, resetSelection);
			e.Handled = true;
		} else if (e.KeyCode == Keys.Tab && enableTab) {
			//If the tab failed I automatically select the next control in the form (SelectNextControl)

			if (e.Modifiers == Keys.Shift) {
				// backward

				if (Selection.MoveActiveCell(0, -1, -1, int.MaxValue) == false)
					FindForm().SelectNextControl(this, false, true, true, true);
				e.Handled = true;
			} else {
				//forward

				if (Selection.MoveActiveCell(0, 1, 1, int.MinValue) == false)
					FindForm().SelectNextControl(this, true, true, true, true);
				e.Handled = true;
			}
		} else if ((e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown)
					&& enablePageDownUp) {
			//MICK(18): I commented out this part - focusing is done within my CustomScrollPageDown() and CustomScrollPageUp()
			//Point focusPoint = PositionToRectangle(Selection.ActivePosition).Location;
			//focusPoint.Offset(1, 1); //in modo da entrare nella cella

			if (e.KeyCode == Keys.PageDown)
				CustomScrollPageDown();
			else if (e.KeyCode == Keys.PageUp)
				CustomScrollPageUp();

			//MICK(18): I commented out this part - focusing is done within my CustomScrollPageDown() and CustomScrollPageUp()
			//Position newPosition = PositionAtPoint(focusPoint);
			//if (Selection.CanReceiveFocus(newPosition))
			//    Selection.Focus(newPosition, true);

			e.Handled = true;
		}
		if (shiftPressed) {
			Selection.ResetSelection(true);
			Selection.SelectRange(new CellRange(_firstCellShiftSelected, Selection.ActivePosition), true);
		}

		// Clipboard
		var selRegion = (ClipboardUseOnlyActivePosition ? new RangeRegion(Selection.ActivePosition) : Selection.GetSelectionRegion());

		//Paste
		if (e.Control && e.KeyCode == Keys.V) {
			PerformPaste(selRegion);
			e.Handled = true;
		} else if (e.Control && e.KeyCode == Keys.C) {
			//Copy

			PerformCopy(selRegion);
			e.Handled = true;
		} else if (e.Control && e.KeyCode == Keys.X) {
			//Cut

			PerformCut(selRegion);
			e.Handled = true;
		} else if (e.KeyCode == Keys.Delete) {
			//Delete

			PerformDelete(selRegion);
			e.Handled = true;
		}
	}

	/// <summary>
	/// Checks if cut mode is enabled, and copies
	/// data into clipaborad
	/// </summary>
	/// <param name="selRegion"></param>
	public void PerformCut(RangeRegion selRegion) {
		var cutEnabled = (ClipboardMode & ClipboardMode.Cut) == ClipboardMode.Cut;
		if (cutEnabled == false || selRegion.IsEmpty() == true)
			return;
		var rng = selRegion[0];

		var data = RangeData.LoadData(this, rng, CutMode.CutImmediately);
		RangeData.ClipboardSetData(data);
	}

	/// <summary>
	/// Checks if paste mode is enabled and writes
	/// data into grid from clipboard
	/// </summary>
	/// <param name="selRegion"></param>
	public void PerformPaste(RangeRegion selRegion) {
		var pasteEnabled = (ClipboardMode & ClipboardMode.Paste) == ClipboardMode.Paste;
		if (pasteEnabled == false || selRegion.IsEmpty() == true)
			return;
		var rngData = RangeData.ClipboardGetData();

		if (rngData == null)
			return;
		var rng = selRegion[0];
		var data = rngData.SourceRange;
		var destinationRange = new CellRange(
			new Position(rng.Start.Row, rng.Start.Column),
			new Position(rng.Start.Row + (data.End.Row - data.Start.Row), rng.Start.Column + (data.End.Column - data.Start.Column)));

		rngData.WriteData(this, rng.Start);

		Selection.ResetSelection(true);
		Selection.SelectRange(destinationRange, true);
	}

	/// <summary>
	/// Checks if copy mode is enabled and copies data into clipboard
	/// </summary>
	/// <param name="selRegion"></param>
	public void PerformCopy(RangeRegion selRegion) {
		var copyEnabled = (ClipboardMode & ClipboardMode.Copy) == ClipboardMode.Copy;
		if (copyEnabled == false || selRegion.IsEmpty() == true)
			return;
		var rng = selRegion[0];

		var data = RangeData.LoadData(this, rng, CutMode.None);
		RangeData.ClipboardSetData(data);
	}

	/// <summary>
	/// Checks if delete is enabled, and clears values from
	/// given destination by invoking ClearValues(selRegion)
	/// </summary>
	/// <param name="selRegion"></param>
	public void PerformDelete(RangeRegion selRegion) {
		var deleteEnabled = (ClipboardMode & ClipboardMode.Delete) == ClipboardMode.Delete;
		if (deleteEnabled == false || selRegion.IsEmpty() == true)
			return;
		ClearValues(selRegion);
	}

	// Clear
	/// <summary>
	/// Clear all the selected cells with a valid Model.
	/// </summary>
	public virtual void ClearValues(RangeRegion region) {
		var positions = region.GetCellsPositions();
		foreach (var position in positions) {
			var cellContext = new CellContext(this, position);
			if (cellContext.Cell != null) {
				if (cellContext.Cell.Editor != null)
					cellContext.Cell.Editor.ClearCell(cellContext);
			}
		}
	}

	// MICK(1)
	// Scroll PageDown/Up/Right/Left/LineUp/Down/Right/Left
	// NewScrollDown
	public override void CustomScrollPageDown() {
		var needActualScrolling = false;
		var newProposedTopRow = 0;
		var newProposedFocusedRow = 0;

		var rngFocusedCell = PositionToCellRange(Selection.ActivePosition);

		//if there is no selection here (ActiveRow is undefined),
		//put selection in the first possible cell
		//confusing NAMING: Rows.IsRowVisible -> Rows.NotHidden - because it is not hidden,
		//                  may be confused with visible on screen within visible page

		if (rngFocusedCell.IsEmpty()) {
			Selection.MoveActiveCell(1, 0);
			return;
		}

		var rows = GetVisibleRows(false);

		if (rows.Count <= ActualFixedRows + 1) {
			Selection.MoveActiveCell(1, 0);
			return;
		}
		;

		var firstVisibleRow = rows[ActualFixedRows];
		var lastVisibleRow = rows[rows.Count - 1];

		if (IsThisRowVisibleOnScreen(rngFocusedCell.Start.Row)) {
			//focus is on the screen

			var bottommostFocusableRow = GetBottommostFocusableRowFromRange(firstVisibleRow, lastVisibleRow);
			if (rngFocusedCell.ContainsRow(bottommostFocusableRow)) {
				//need actual scrolling, ie. displaying new page
				var findFirstFocusableRow = FindFirstFocusableRowInDownDirection(lastVisibleRow);
				if (findFirstFocusableRow != -1) {
					needActualScrolling = true;
					newProposedTopRow = lastVisibleRow;
					var newProposedBottomRow = GetBottomVisibleRowFromTopRow(newProposedTopRow);
					newProposedFocusedRow = GetBottommostFocusableRowFromRange(newProposedTopRow, newProposedBottomRow);

					if (newProposedFocusedRow == -1) {
						//There is no Focusable Row in the proposed page, so there need to be new page and focus found
						newProposedFocusedRow = FindFirstFocusableRowInDownDirection(newProposedBottomRow);
						newProposedTopRow = GetTopVisibleRowFromBottomRow(newProposedFocusedRow);
					}
				} else {
					//no focusable cells below -
					//in this case we might scroll down page, leaving focus unchanged
					//or move focus to the side like in arrow down handler

					//this is only temporary
					//MICK(23)
					Selection.MoveActiveCell(1, 0);
					return;
				}
			} else {
				//this time only move focus, wihout actual scrolling

				var newPosition = new Position(bottommostFocusableRow, Selection.ActivePosition.Column);
				Selection.Focus(newPosition, true);
			}
		} else {
			// focus is not on the screen, so actual scrolling is done always
			needActualScrolling = true;
			newProposedTopRow = rngFocusedCell.Start.Row;
			var offscreenBottomRow = GetBottomVisibleRowFromTopRow(newProposedTopRow);
			newProposedFocusedRow = GetBottommostFocusableRowFromRange(newProposedTopRow, offscreenBottomRow);
		}

		if (needActualScrolling) {
			var newTopRow = 0;
			var newFocusedRow = 0;

			GetValidScrollDown(newProposedTopRow, newProposedFocusedRow, out newTopRow, out newFocusedRow);
			newTopRow = ExtendTopRowByHiddenRows(newTopRow);

			base.CustomScrollPageToLine(newTopRow - ActualFixedRows);

			if (rngFocusedCell.ContainsRow(newFocusedRow) == false) {
				var newPosition = new Position(newFocusedRow, Selection.ActivePosition.Column);
				Selection.Focus(newPosition, true);
			}
		}
	}

	// NewScrollUp
	public override void CustomScrollPageUp() {
		var needActualScrolling = false;
		var newProposedTopRow = 0;
		var newProposedFocusedRow = 0;

		//confusing NAMING: ActivePosition -> FocusedPosition - it would be better
		var focusedRow = Selection.ActivePosition.Row;
		//TODO: if there is no selection here (ActiveRow is undefined),
		//put selection in the first possible cell
		//confusing NAMING: Rows.IsRowVisible -> Rows.NotHidden - because it is not hidden,
		//                  may be confused with visible on screen within visible page

		if (focusedRow == -1) {
			Selection.MoveActiveCell(-1, 0);
			return;
		}

		var rows = GetVisibleRows(false);

		//TODO: if Rows.height<displayheight
		if (rows.Count <= ActualFixedRows + 1) {
			Selection.MoveActiveCell(-1, 0);
			return;
		}
		;

		var firstVisibleRow = rows[ActualFixedRows];
		var lastVisibleRow = rows[rows.Count - 1];

		//another workaround: if (focusedrow==firstVisibleRow)
		//if the row above has height larger than displayheight it is also like one row scrolling-
		//so workaround is needed like the one above
		if (focusedRow == firstVisibleRow)
			if (focusedRow > ActualFixedRows) {
				var displayHeight = DisplayRectangle.Height;

				//Remove the fixed rows from the scrollable area
				for (var f = 0; f < ActualFixedRows; f++)
					displayHeight -= Rows.GetHeight(f);

				if (displayHeight <= Rows.GetHeight(focusedRow - 1)) {
					Selection.MoveActiveCell(-1, 0);
					return;
				}
			}

		if (IsThisRowVisibleOnScreen(focusedRow)) {
			//focus is on the screen

			var topmostFocusableRow = GetTopmostFocusableRowFromRange(firstVisibleRow, lastVisibleRow);
			if (topmostFocusableRow == focusedRow) {
				//need actual scrolling, ie. displaying new page
				var findFirstFocusableRow = FindFirstFocusableRowInUpDirection(firstVisibleRow);
				if (findFirstFocusableRow != -1) {
					needActualScrolling = true;
					var newProposedBottomRow = firstVisibleRow;
					newProposedTopRow = GetTopVisibleRowFromBottomRow(newProposedBottomRow);
					newProposedFocusedRow = GetTopmostFocusableRowFromRange(newProposedTopRow, newProposedBottomRow);

					if (newProposedFocusedRow == -1) {
						//There is no Focusable Row in the proposed page, so there need to be new page and focus found
						newProposedFocusedRow = FindFirstFocusableRowInUpDirection(newProposedTopRow);
						newProposedTopRow = newProposedFocusedRow;
					}
				} else {
					//no focusable cells below -
					//in this case we might scroll down page, leaving focus unchanged
					//or move focus to the side like in arrow down handler

					//this is only temporary
					//MICK(24)
					Selection.MoveActiveCell(-1, 0);
					return;
				}
			} else {
				//this time only move focus, wihout actual scrolling

				var newPosition = new Position(topmostFocusableRow, Selection.ActivePosition.Column);
				Selection.Focus(newPosition, true);
			}
		} else {
			// focus is not on the screen, so actual scrolling is done always
			needActualScrolling = true;

			var offscreenBottomRow = focusedRow;
			newProposedTopRow = GetTopVisibleRowFromBottomRow(offscreenBottomRow);
			newProposedFocusedRow = GetTopmostFocusableRowFromRange(newProposedTopRow, offscreenBottomRow);
			//NewProposedTopRow = FocusedRow;
			//NewProposedFocusedRow = GetBottomVisibleRowFromTopRow(NewProposedTopRow);
		}

		if (needActualScrolling) {
			var newTopRow = 0;
			var newFocusedRow = 0;

			GetValidScrollUp(newProposedTopRow, newProposedFocusedRow, out newTopRow, out newFocusedRow);
			newTopRow = ExtendTopRowByHiddenRows(newTopRow);

			base.CustomScrollPageToLine(newTopRow - ActualFixedRows);

			if (newFocusedRow != focusedRow) {
				var newPosition = new Position(newFocusedRow, Selection.ActivePosition.Column);
				Selection.Focus(newPosition, true);
			}
		}
	}

	protected override void OnKeyUp(KeyEventArgs e) {
		base.OnKeyUp(e);

		if (_keybordActivePosition.IsEmpty() == false) {
			var focusCell = GetCell(_keybordActivePosition);
			if (focusCell != null)
				Controller.OnKeyUp(new CellContext(this, _keybordActivePosition, focusCell), e);
		}
	}

	protected override void OnKeyPress(KeyPressEventArgs e) {
		base.OnKeyPress(e);

		//Che if is not Tab, Enter or Copy/Paste
		if (_keybordActivePosition.IsEmpty() || e.KeyChar == '\t' || e.KeyChar == 13 ||
			e.KeyChar == 3 || e.KeyChar == 22 || e.KeyChar == 24) {
		} else {
			var focusCell = GetCell(_keybordActivePosition);
			if (focusCell != null)
				Controller.OnKeyPress(new CellContext(this, _keybordActivePosition, focusCell), e);
		}
	}

	protected override void OnEnter(EventArgs e) {
		base.OnEnter(e);

		//Move the focus on the first cell if there isn't an active cell and the FocusStyle FocusFirstCellOnEnter is on.
		if ((Selection.FocusStyle & FocusStyle.FocusFirstCellOnEnter) == FocusStyle.FocusFirstCellOnEnter &&
			_autoFocusFirstCellOnEnter && Selection.ActivePosition.IsEmpty())
			Selection.FocusFirstCell(false);
	}

	protected override void OnValidated(EventArgs e) {
		base.OnValidated(e);

		//NOTE: I use OnValidated and not OnLostFocus because is not called when the focus is on another child control (for example an editor control) or OnLeave because before Validating event and so the validation can still be stopped

		if ((Selection.FocusStyle & FocusStyle.RemoveFocusCellOnLeave) == FocusStyle.RemoveFocusCellOnLeave)
			Selection.Focus(Position.Empty, false);

		if ((Selection.FocusStyle & FocusStyle.RemoveSelectionOnLeave) == FocusStyle.RemoveSelectionOnLeave)
			Selection.ResetSelection(true);
	}

	/// <summary>
	/// Set the Focus on the grid with an optional behaviour.
	/// If selectFirstCell is true and the FocusStyle is equal to FocusStyle.FocusFirstCellOnEnter then
	/// if there isn't a cell selected the first cell of the grid receive became the active cell.
	/// Otherwise the focus is not changed.
	/// The Default is true.
	/// </summary>
	/// <param name="selectFirstCell"></param>
	public virtual bool Focus(bool selectFirstCell) {
		bool result;
		try {
			_autoFocusFirstCellOnEnter = selectFirstCell;
			result = Focus();
		} finally {
			_autoFocusFirstCellOnEnter = true;
		}

		return result;
	}

	/// <summary>
	/// OnHScrollPositionChanged
	/// </summary>
	/// <param name="e"></param>
	protected override void OnHScrollPositionChanged(ScrollPositionChangedEventArgs e) {
		base.OnHScrollPositionChanged(e);

		ArrangeLinkedControls();
	}

	/// <summary>
	/// OnVScrollPositionChanged
	/// </summary>
	/// <param name="e"></param>
	protected override void OnVScrollPositionChanged(ScrollPositionChangedEventArgs e) {
		base.OnVScrollPositionChanged(e);

		ArrangeLinkedControls();
	}

	/// <summary>
	/// Refresh the linked controls bounds
	/// </summary>
	public virtual void ArrangeLinkedControls() {
		SuspendLayout();
		foreach (var linked in _linkedControls) {
			if (linked.Position.IsEmpty())
				continue;

			var control = linked.Control;
			var cell = GetCell(linked.Position);

			var rect = PositionToRectangle(linked.Position);

			if (cell != null && linked.UseCellBorder)
				rect = Rectangle.Round(cell.View.Border.GetContentRectangle(rect));

			control.Bounds = rect;

			// Hide all linked controls which are not in the fixed area
			// and fall outside the scrollable area
			if ((linked.Position.Row >= FixedRows) && (linked.Position.Column >= FixedColumns)) {
				var scrollableRange = RangeAtArea(CellPositionType.Scrollable);
				var scrollableRectangle = RangeToRectangle(scrollableRange);

				control.Visible = !Rectangle.Intersect(rect, scrollableRectangle).IsEmpty;
			}
		}
		ResumeLayout(false);
	}

	// Position methods
	/// <summary>
	/// Get the rectangle of the cell respect to the client area visible, the grid DisplayRectangle.
	/// Returns Rectangle.Empty if the Position is empty or if is not valid.
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public Rectangle PositionToRectangle(Position position) {
		return RangeToRectangle(PositionToCellRange(position));
	}

	/// <summary>
	/// This method converts a Position to the real range of the cell. This is usefull when RowSpan or ColumnSpan is greater than 1.
	/// For example suppose to have at grid[0,0] a cell with ColumnSpan equal to 2. If you call this method with the position 0,0 returns 0,0-0,1 and if you call this method with 0,1 return again 0,0-0,1.
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public virtual CellRange PositionToCellRange(Position position) {
		if (position.IsEmpty())
			return CellRange.Empty;

		var cell = this.GetCell(position.Row, position.Column);
		if (cell == null)
			return CellRange.Empty;
		return new CellRange(position);
		//return new CellRange(pPosition);
	}

	protected override void OnResize(EventArgs e) {
		base.OnResize(e);

		PerformResize();
	}

	public void PerformResize() {
		SuspendLayout();
		SetScrollArea();
		PerformStretch();
		ResumeLayout(true);
	}

	/// <summary>
	/// Abstract method used to create the rows object.
	/// </summary>
	protected abstract RowsBase CreateRowsObject();

	/// <summary>
	/// Abstract method used to create the columns object.
	/// </summary>
	protected abstract ColumnsBase CreateColumnsObject();

	///// <summary>
	///// Indicates if the specified range is visible
	///// </summary>
	///// <param name="range"></param>
	///// <param name="partial">True to return also partial visible cells</param>
	///// <returns></returns>
	//public bool IsRangeVisible(CellRange range, bool partial)
	//{
	//    List<int> rows = GetVisibleRows(partial);
	//    List<int> columns = GetVisibleColumns(partial);

	//    if (rows.Count == 0 || columns.Count == 0)
	//        return false;

	//    //All the fixed rows are considered to be visible
	//    bool isRowVisible = false;
	//    if (range.Start.Row < FixedRows)
	//        isRowVisible = true;
	//    else if (range.Start.Row <= rows[rows.Count - 1] &&
	//                range.End.Row >= rows[0])
	//        isRowVisible = true;

	//    bool isColVisible = false;
	//    if (range.Start.Column < FixedColumns)
	//        isColVisible = true;
	//    else if (range.Start.Column <= columns[columns.Count - 1] &&
	//                range.End.Column >= columns[0])
	//        isColVisible = true;

	//    return isColVisible && isRowVisible;
	//}

	/// <summary>
	/// Return the scroll position that must be set to show a specific cell.
	/// </summary>
	/// <param name="position"></param>
	/// <param name="partial">True to consider also partial visible cells</param>
	/// <param name="newScrollPosition"></param>
	/// <returns>Return false if the cell is already visible, return true is the cell is not currently visible.</returns>
	protected virtual bool GetScrollPositionToShowCell(Position position, bool partial, out Point newScrollPosition) {
		var displayRectangle = DisplayRectangle;

		var rows = GetVisibleRows(partial);
		var columns = GetVisibleColumns(partial);

		if (rows.Contains(position.Row) && columns.Contains(position.Column)) {
			newScrollPosition = CustomScrollPosition;
			return false;
		}
		var posType = GetPositionType(position);
		var isFixedTop = false;
		if (posType == CellPositionType.FixedTop || posType == CellPositionType.FixedTopLeft)
			isFixedTop = true;
		var isFixedLeft = false;
		if (posType == CellPositionType.FixedLeft || posType == CellPositionType.FixedTopLeft)
			isFixedLeft = true;

		int x;
		if (columns.Contains(position.Column)) //Is x visible
			x = CustomScrollPosition.X;
		else {
			if (isFixedLeft)
				x = 0;
			else
				x = position.Column - FixedColumns;

			//Check if the scrollable positioin if not outside the valid area
			var maxX = GetScrollColumns(displayRectangle.Width);
			if (x > maxX)
				x = maxX;
		}

		int y;
		if (rows.Contains(position.Row)) {
			//Is y visible

			y = CustomScrollPosition.Y;
			y = ExtendTopRowByHiddenRows(y);
		} else {
			//MICK(15): add direction dependent procedure of finding y (and x should be too).
			//It means that when you go from bottom to top, the selected item will be at top,
			//              (this already happens, so this part is not changed)
			//              when you go from top to bottom, the selected item will be at bottom,
			// y (and x) value is adjusted accordingly
			if (CustomScrollPosition.Y + ActualFixedRows > position.Row) {
				//MICK(16): direction bottom to top
				if (isFixedTop)
					y = 0;
				else
					y = position.Row - FixedRows;

				//Check if the scrollable positioin if not outside the valid area
				var maxY = GetScrollRows(displayRectangle.Height);
				if (y > maxY)
					y = maxY;
				//MICK(21)
				y = ExtendTopRowByHiddenRows(y);
			} else {
				//MICK(17): direction from top to bottom
				y = GetTopVisibleRowFromBottomRow(position.Row) - ActualFixedRows;
				y = ExtendTopRowByHiddenRows(y);
			}
		}

		newScrollPosition = new Point(x, y);

		return true;
	}

	/// <summary>
	/// Returns the type of a cell position
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public CellPositionType GetPositionType(Position position) {
		if (position.IsEmpty())
			return CellPositionType.Empty;
		if (position.Row < FixedRows && position.Column < FixedColumns)
			return CellPositionType.FixedTopLeft;
		if (position.Row < FixedRows)
			return CellPositionType.FixedTop;
		if (position.Column < FixedColumns)
			return CellPositionType.FixedLeft;
		return CellPositionType.Scrollable;
	}

	///// <summary>
	///// Returns the logical scroll size (usually Rows and Columns) for the specified display area.
	///// </summary>
	///// <param name="displayRectangle"></param>
	///// <returns></returns>
	//protected override Size GetVisibleScrollArea(Rectangle displayRectangle)
	//{
	//    List<int> rows = GetVisibleRows(displayRectangle, false);
	//    List<int> columns = GetVisibleColumns(displayRectangle, false);

	//    return new Size(columns.Count, rows.Count);
	//}

	///// <summary>
	///// Returns true if the vertical scrollbar is required
	///// </summary>
	///// <returns></returns>
	//protected override bool RequireVerticalScroll(Size displaySize)
	//{
	//    int currentHeight = 0;

	//    for (int r = 0; r < Rows.Count; r++)
	//    {
	//        currentHeight += Rows.GetHeight(r);

	//        if (currentHeight > displaySize.Height)
	//            return true;
	//    }

	//    return false;
	//}

	///// <summary>
	///// Returns true if the horizontal scrollbar is required
	///// </summary>
	///// <returns></returns>
	//protected override bool RequireHorizontalScroll(Size displaySize)
	//{
	//    int currentWidth = 0;

	//    for (int c = 0; c < Columns.Count; c++)
	//    {
	//        currentWidth += Columns.GetWidth(c);

	//        if (currentWidth > displaySize.Width)
	//            return true;
	//    }

	//    return false;
	//}

	/// <summary>
	/// Calculate the number of rows to scroll. 0 to disable the scrollbar.
	/// The returned value is independent from the current scrolling position, must be a fixed value
	/// calculated based on the total number of rows and the available area.
	/// </summary>
	protected override int GetScrollRows(int displayHeight) {
		if (displayHeight < 0)
			return 0;

		var currentHeight = 0;
		var scrollRows = 0;

		//Remove the fixed rows from the scrollable area
		for (var f = 0; f < ActualFixedRows; f++)
			displayHeight -= Rows.GetHeight(f);

		//Calculate the rows to be scrolled
		for (var r = Rows.Count - 1; r >= ActualFixedRows; r--) {
			if (Rows.IsRowVisible(r) == false)
				continue;
			currentHeight += Rows.GetHeight(r);

			if (currentHeight > displayHeight)
				return Rows.Count - scrollRows - GetTotalHiddenRows(r);

			scrollRows++;
		}

		return 0;
	}

	/*
										//MICK(14)
										protected int GetHiddenRows()
										{
											int HiddenRows=0;

											for (int r = 0; r < Rows.Count; r++)
											{
												//IsRowVisible - this function name is confusing - is it visible on the screen? - no.
												//Also FirstVisibleScrollableRow - is it a bug - it does not consider hidden rows?
												if (!Rows.IsRowVisible(r)) HiddenRows++;
											}

											return HiddenRows;
										}
										 */

	/// <summary>
	/// Calculate the number of columns to scroll. 0 to disable the scrollbar.
	/// The returned value is independent from the current scrolling position, must be a fixed value
	/// calculated based on the total number of columns and the available area.
	/// </summary>
	protected override int GetScrollColumns(int displayWidth) {
		if (displayWidth < 0)
			return 0;
		var currentWidth = 0;
		var scrollCols = 0;

		//Remove the fixed columns from the scrollable area
		for (var f = 0; f < ActualFixedColumns; f++)
			displayWidth -= Columns.GetWidth(f);

		//Calculate the columns to be scrolled
		for (var c = Columns.Count - 1; c >= ActualFixedColumns; c--) {
			currentWidth += Columns.GetWidth(c);

			if (currentWidth > displayWidth)
				return Columns.Count - scrollCols;

			scrollCols++;
		}

		return 0;
	}

	// Selection
	/// <summary>
	/// Virtual factory method used to create the SelectionBase derived object.
	/// The base method create a different object based on the value of SelectionMode property.
	/// </summary>
	/// <returns></returns>
	protected virtual Selection.SelectionBase CreateSelectionObject() {
		switch (SelectionMode) {
			case GridSelectionMode.None:
				return new Selection.DisabledSelection();
			case GridSelectionMode.Cell:
				return new Selection.FreeSelection();
			case GridSelectionMode.Column:
				return new Selection.ColumnSelection();
			case GridSelectionMode.Row:
				return new Selection.RowSelection();
			default:
				throw new ArgumentException("SelectionMode not valid", "SelectionMode");
		}
	}

	protected virtual void OnMouseSelectionFinish(RangeEventArgs e) {
		_oldMouseSelectionRange = CellRange.Empty;
		if (MouseSelectionFinished != null)
			MouseSelectionFinished(this, e);
	}

	/// <summary>
	/// Fired when the mouse selection change. See also MouseSelectionRange.
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnMouseSelectionChange(EventArgs e) {
		var mouseRange = MouseSelectionRange;

		OnUndoMouseSelection(new RangeEventArgs(_oldMouseSelectionRange));

		OnApplyMouseSelection(new RangeEventArgs(mouseRange));

		_oldMouseSelectionRange = mouseRange;
	}

	/// <summary>
	/// Fired when the mouse selection must be canceled. See also MouseSelectionRange.
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnUndoMouseSelection(RangeEventArgs e) {
		Selection.SelectRange(e.Range, false);
	}

	/// <summary>
	/// Fired when the mouse selection is succesfully finished. See also MouseSelectionRange.
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnApplyMouseSelection(RangeEventArgs e) {
		Selection.SelectRange(e.Range, true);
	}

	protected virtual void OnRangePaint(RangePaintEventArgs e) {
		var drawRectangle = RangeToRectangle(e.DrawingRange);

		var state = e.GraphicsCache.Graphics.Save();

		try {
			e.GraphicsCache.Graphics.SetClip(drawRectangle);

			var top = drawRectangle.Top;
			int width;
			int height;
			foreach (var r in _rows.HiddenRowsCoordinator.LoopVisibleRows(e.DrawingRange.Start.Row, e.DrawingRange.End.Row - e.DrawingRange.Start.Row)) {
				//for (int r = e.DrawingRange.Start.Row; r <= e.DrawingRange.End.Row; r++)

				if (Rows.IsRowVisible(r) == false)
					throw new SourceGridException("Incorrect internal state. This rows must have been visible");
				height = Rows.GetHeight(r);

				var left = drawRectangle.Left;
				for (var c = e.DrawingRange.Start.Column; c <= e.DrawingRange.End.Column; c++) {
					width = Columns.GetWidth(c);
					if (Columns.IsColumnVisible(c) == false)
						continue;

					var position = new Position(r, c);

					var cell = GetCell(position);
					if (cell != null) {
						var drawRect = new Rectangle(left, top, width, height);

						//Draw the cell only if inside the ClipRectangle
						if (e.GraphicsCache.ClipRectangle.IsEmpty ||
							e.GraphicsCache.ClipRectangle.IntersectsWith(drawRect)) {
							var cellContext = new CellContext(this, position, cell);
							PaintCell(e.GraphicsCache, cellContext, drawRect);
						}
					}

					left += width;
				}

				top += height;
			}

			//Draw the decorators
			foreach (var dec in Decorators) {
				if (dec.IntersectWith(e.DrawingRange))
					dec.Draw(e);
			}

			if (RangePaint != null)
				RangePaint(this, e);
		} finally {
			e.GraphicsCache.Graphics.Restore(state);
		}
	}

	/// <summary>
	/// Returns the relative rectangle to the current scrollable area of the specified Range.
	/// Returns a Rectangle.Empty if the Range is not valid.
	/// </summary>
	public Rectangle RangeToRectangle(CellRange range) {
		if (range.IsEmpty())
			return Rectangle.Empty;

		if (range.Start.Column < 0)
			throw new ArgumentOutOfRangeException(string.Format("range.Start.Column was less than zero: {0}", range.Start.Column));
		var x = Columns.GetLeft(range.Start.Column);
		if (range.Start.Row < 0)
			throw new ArgumentOutOfRangeException(string.Format("range.Start.Row was less than zero: {0}", range.Start.Row));
		var y = Rows.GetTop(range.Start.Row);

		var size = RangeToSize(range);
		if (size.IsEmpty)
			return Rectangle.Empty;

		return new Rectangle(new Point(x, y), size);
	}

	// Range methods
	public Size RangeToSize(CellRange range) {
		if (range.IsEmpty())
			return Size.Empty;

		var width = 0;
		for (var c = range.Start.Column; c <= range.End.Column; c++)
			width += Columns.GetWidth(c);

		var height = 0;
		for (var r = range.Start.Row; r <= range.End.Row; r++)
			height += Rows.GetHeight(r);

		return new Size(width, height);
	}

	/// <summary>
	/// Draw the specified Cell
	/// </summary>
	protected virtual void PaintCell(Sphere10.Framework.Drawing.GraphicsCache graphics, CellContext cellContext, RectangleF drawRectangle) {
		if (drawRectangle.Width > 0 && drawRectangle.Height > 0 &&
			cellContext.CanBeDrawn())
			cellContext.Cell.View.DrawCell(cellContext, graphics, drawRectangle);
	}

	/// <summary>
	/// Fired when calling SortRangeRows method
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnSortingRangeRows(SortRangeRowsEventArgs e) {
	}

	/// <summary>
	/// Fired after calling SortRangeRows method
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnSortedRangeRows(SortRangeRowsEventArgs e) {
	}

	///// <summary>
	///// Get the range for the specific area type and rectangle
	///// </summary>
	//public CellRange RangeAtRectangle(CellPositionType areaType, Rectangle clipRectangle)
	//{
	//    if (areaType == CellPositionType.FixedTopLeft)
	//    {
	//    }
	//    else if (areaType == CellPositionType.FixedLeft)
	//    {
	//    }
	//    else if (areaType == CellPositionType.FixedTop)
	//    {
	//    }
	//    else if (areaType == CellPositionType.Scrollable)
	//    {
	//    }
	//    else
	//        throw new SourceGridException("Invalid areaType");
	//}

	/// <summary>
	/// Get the visible ranges. Returns a list of Range, one for each area.
	/// </summary>
	/// <returns></returns>
	private IEnumerable<CellRange> GetVisibleRegion() {
		CellRange rng;

		rng = RangeAtArea(CellPositionType.FixedTopLeft);
		if (rng.IsEmpty() == false)
			yield return rng;
		rng = RangeAtArea(CellPositionType.FixedTop);
		if (rng.IsEmpty() == false)
			yield return rng;
		rng = RangeAtArea(CellPositionType.FixedLeft);
		if (rng.IsEmpty() == false)
			yield return rng;
		rng = RangeAtArea(CellPositionType.Scrollable);
		if (rng.IsEmpty() == false)
			yield return rng;
	}

	/// <summary>
	/// Get the range of cells at the specified dispaly area.
	/// This method consider only the visible cells using the current scroll position.
	/// Returns a single Range for the specified grid area (scrollable, fixedtop, fixedleft, fixedtopleft).
	/// Returns Range.Empty if there isn't a valid range in the specified area.
	/// </summary>
	public CellRange RangeAtArea(CellPositionType areaType) {
		if (areaType == CellPositionType.FixedTopLeft) {
			if (FixedRows > 0 && Rows.Count >= FixedRows &&
				FixedColumns > 0 && Columns.Count >= FixedColumns)
				return new CellRange(0, 0, FixedRows - 1, FixedColumns - 1);
			return CellRange.Empty;
		}
		if (areaType == CellPositionType.FixedLeft) {
			var actualFixed = FixedColumns;
			if (actualFixed > Columns.Count)
				actualFixed = Columns.Count;

			if (actualFixed <= 0)
				return CellRange.Empty;

			var firstRow = Rows.FirstVisibleScrollableRow;
			var lastRow = Rows.LastVisibleScrollableRow;

			if (firstRow == null || lastRow == null)
				return CellRange.Empty;

			return new CellRange(firstRow.Value, 0, lastRow.Value, actualFixed - 1);
		}
		if (areaType == CellPositionType.FixedTop) {
			var actualFixed = FixedRows;
			if (actualFixed > Rows.Count)
				actualFixed = Rows.Count;

			if (actualFixed <= 0)
				return CellRange.Empty;

			var firstCol = Columns.FirstVisibleScrollableColumn;
			var lastCol = Columns.LastVisibleScrollableColumn;

			if (firstCol == null || lastCol == null)
				return CellRange.Empty;

			return new CellRange(0, firstCol.Value, actualFixed - 1, lastCol.Value);
		}
		if (areaType == CellPositionType.Scrollable) {
			var firstRow = Rows.FirstVisibleScrollableRow;
			var lastRow = Rows.LastVisibleScrollableRow;

			var firstCol = Columns.FirstVisibleScrollableColumn;
			var lastCol = Columns.LastVisibleScrollableColumn;

			if (firstRow == null || firstCol == null ||
				lastRow == null || lastCol == null)
				return CellRange.Empty;

			return new CellRange(firstRow.Value, firstCol.Value, lastRow.Value, lastCol.Value);
		}
		throw new SourceGridException("Invalid areaType");
	}

	private int GetTotalHiddenRows(int from) {
		return Rows.HiddenRowsCoordinator.GetTotalHiddenRows();
		/*var hidden = 0;
		for (int x = from; x >= ActualFixedRows; x--)
		{
			if (Rows.IsRowVisible(x) == false)
				hidden ++;
		}
		return hidden;*/
	}

	private void HandleMouseDoubleClick(Position clickPosition, EventArgs args) {
		if (clickPosition.IsEmpty() == true)
			return;
		var mouseDownCell = GetCell(clickPosition);
		if (mouseDownCell != null)
			Controller.OnDoubleClick(new CellContext(this, clickPosition, mouseDownCell), args);
	}

	// GetCell methods
	/// <summary>
	/// Return the Cell at the specified Row and Col position. Abstract, must be implemented in the derived class.
	/// </summary>
	/// <param name="row"></param>
	/// <param name="column"></param>
	/// <returns></returns>
	public abstract Cells.ICellVirtual GetCell(int row, int column);

	/// <summary>
	/// Return the Cell at the specified Row and Col position. This method is called for sort operations and for Move operations. If position is Empty return null. This method calls GetCell(int p_iRow, int p_iCol)
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public Cells.ICellVirtual GetCell(Position position) {
		if (position.IsEmpty())
			return null;
		return GetCell(position.Row, position.Column);
	}

	protected override void OnDoubleClick(EventArgs e) {
		// just pass up this event.
		// we do not have a method for this at the moment.
		// And it is different from MouseDoubleClick.
		base.OnDoubleClick(e);
	}

	// Layout
	/// <summary>
	/// Recalculate the scrollbar position and value based on the current cells, scroll client area, linked controls and more. If redraw == false this method has not effect.
	/// </summary>
	private void PerformStretch() {
		if (AutoStretchColumnsToFitWidth || AutoStretchRowsToFitHeight) {
			if (AutoStretchColumnsToFitWidth && AutoStretchRowsToFitHeight) {
				Rows.AutoSize(false);
				Columns.AutoSize(false);
				Columns.StretchToFit();
				Rows.StretchToFit();
			} else if (AutoStretchColumnsToFitWidth) {
				Columns.AutoSize(true);
				Columns.StretchToFit();
			} else if (AutoStretchRowsToFitHeight) {
				Rows.AutoSize(true);
				Rows.StretchToFit();
			}
		}
	}

	private void SetScrollArea() {
		//MICK(19): original version is obviously a bug - when vertPage is calculated in this manner
		//the thumb height is not right
		//and scrolling is not continuous (not all items are displayed when scrolling)
		//ORIG: int vertPage = Math.Max(Rows.Count / 10, 1);
		//ORIG: int horPage = Math.Max(Columns.Count / 10, 1);

		//MICK(20)
		var vertPage = Math.Max(GetVisibleRows(false).Count - ActualFixedRows, 1);
		var horPage = Math.Max(GetVisibleColumns(false).Count - ActualFixedColumns, 1);

		LoadScrollArea(vertPage, horPage);
	}

	public List<int> GetVisibleColumns(bool returnsPartial) {
		return GetVisibleColumns(DisplayRectangle, returnsPartial);
	}

	public List<int> GetVisibleColumns(Rectangle displayRectangle, bool returnsPartial) {
		return Columns.ColumnsInsideRegion(displayRectangle.X, displayRectangle.Width, returnsPartial, true);
	}

	int ExtendTopRowByHiddenRows(int topRow) {
		//Simple (though not the best) way to handle hidden rows

		if (topRow == 0)
			return 0;
		for (var k = topRow; k >= ActualFixedRows; k--) {
			if (Rows.IsRowVisible(k))
				return k;
		}
		return ActualFixedRows;
	}

	int FindFirstFocusableRowInDownDirection(int topRow) {
		for (var k = topRow; k < Rows.Count; k++) {
			var newPosition = new Position(k, Selection.ActivePosition.Column);
			if (Selection.CanReceiveFocus(newPosition))
				return k;
		}
		return -1;
	}

	int GetBottommostFocusableRowFromRange(int firstRow, int lastRow) {
		for (var k = lastRow; k >= firstRow; k--) {
			var newPosition = new Position(k, Selection.ActivePosition.Column);
			if (Selection.CanReceiveFocus(newPosition))
				return k;
		}
		return -1;
	}

	bool IsThisRowVisibleOnScreen(int row) {
		var rows = GetVisibleRows(false);
		var firstVisibleRow = rows[ActualFixedRows];
		var lastVisibleRow = rows[rows.Count - 1];
		if ((row <= lastVisibleRow) && (row >= firstVisibleRow))
			return true;
		return false;
	}

	// Rectangle, range and scrollable areas methods
	public List<int> GetVisibleRows(bool returnsPartial) {
		return GetVisibleRows(DisplayRectangle, returnsPartial);
	}

	public List<int> GetVisibleRows(Rectangle displayRectangle, bool returnsPartial) {
		var allRows = Rows.RowsInsideRegion(displayRectangle.Y, displayRectangle.Height, returnsPartial, true);
		var onlyVisible = new List<int>(allRows.Count);
		foreach (var index in allRows) {
			if (Rows.IsRowVisible(index) == true)
				onlyVisible.Add(index);
		}
		return onlyVisible;
	}

	int GetBottomVisibleRowFromTopRow(int topRow) {
		var displayHeight = DisplayRectangle.Height;
		var currentHeight = 0;
		//int CurrentRows = 0;

		//Remove the fixed rows from the scrollable area
		for (var f = 0; f < ActualFixedRows; f++)
			displayHeight -= Rows.GetHeight(f);

		var r = 0;
		for (r = topRow; r < Rows.Count; r++) {
			currentHeight += Rows.GetHeight(r);

			if (currentHeight == displayHeight)
				return r;

			if (currentHeight > displayHeight) {
				if (r == topRow)
					return r;
				return r - 1;
			}
		}
		return Rows.Count - 1;
	}

	void GetValidScrollDown(int newProposedTopRow, int newProposedFocusedRow, out int newTopRow, out int newFocusedRow) {
		//if scrool down is too far down, it needs to be corrected
		newTopRow = newProposedTopRow;
		newFocusedRow = newProposedFocusedRow;

		var validLastTopRow = GetTopVisibleRowFromBottomRow(Rows.Count - 1);

		if (newProposedTopRow > validLastTopRow)
			newTopRow = validLastTopRow;

		if (newProposedFocusedRow > Rows.Count - 1)
			newFocusedRow = Rows.Count - 1;
	}

	int GetTopVisibleRowFromBottomRow(int bottomRow) {
		var displayHeight = DisplayRectangle.Height;
		var currentHeight = 0;
		//int CurrentRows = 0;

		//Remove the fixed rows from the scrollable area
		for (var f = 0; f < ActualFixedRows; f++)
			displayHeight -= Rows.GetHeight(f);

		var r = 0;
		for (r = bottomRow; r >= ActualFixedRows; r--) {
			currentHeight += Rows.GetHeight(r);

			if (currentHeight == displayHeight)
				return r;

			if (currentHeight > displayHeight) {
				if (r == bottomRow)
					return r;
				return r + 1;
			}
		}
		return 0;
	}

	int FindFirstFocusableRowInUpDirection(int bottomRow) {
		for (var k = bottomRow; k > ActualFixedRows; k--) {
			var newPosition = new Position(k, Selection.ActivePosition.Column);
			if (Selection.CanReceiveFocus(newPosition))
				return k;
		}
		return -1;
	}

	int GetTopmostFocusableRowFromRange(int firstRow, int lastRow) {
		for (var k = firstRow; k <= lastRow; k++) {
			var newPosition = new Position(k, Selection.ActivePosition.Column);
			if (Selection.CanReceiveFocus(newPosition))
				return k;
		}
		return -1;
	}

	void GetValidScrollUp(int newProposedTopRow, int newProposedFocusedRow, out int newTopRow, out int newFocusedRow) {
		//if scrool down is too far down, it needs to be corrected
		newTopRow = newProposedTopRow;
		newFocusedRow = newProposedFocusedRow;

		var validFirstTopRow = ActualFixedRows;

		if (newProposedTopRow < validFirstTopRow)
			newTopRow = validFirstTopRow;

		if (newProposedFocusedRow < validFirstTopRow)
			newFocusedRow = validFirstTopRow;
	}

	/// <summary>
	/// Fired when the cell under the mouse change. For internal use only.
	/// </summary>
	/// <param name="cell"></param>
	public virtual void ChangeMouseCell(Position cell) {
		if (MouseOverCellPosition != cell) {
			//se la cella che sta perdento il mouse è anche quella che ha ricevuto un eventuale evento di MouseDown non scateno il MouseLeave (che invece verrà scatenato dopo il MouseUp)
			if (MouseOverCellPosition.IsEmpty() == false &&
				MouseOverCellPosition != MouseDownCellPosition)
				Controller.OnMouseLeave(new CellContext(this, MouseOverCellPosition), EventArgs.Empty);

			MouseOverCellPosition = cell;
			if (MouseOverCellPosition.IsEmpty() == false)
				Controller.OnMouseEnter(new CellContext(this, MouseOverCellPosition), EventArgs.Empty);
		}
	}

	protected override void OnMouseLeave(EventArgs e) {
		base.OnMouseLeave(e);

		ChangeMouseCell(Position.Empty);
	}
}
