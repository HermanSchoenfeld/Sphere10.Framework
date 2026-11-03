// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Controllers;

/// <summary>
/// A cell controller used to handle mouse selection
/// </summary>
public class MouseSelection : ControllerBase {
	private Timer _scrollTimer;
	private GridVirtual _capturedGrid;
	private CellContext _capturedMouseMoveSender = CellContext.Empty;
	private MouseEventArgs _capturedMouseMoveEventArgs = null;

	public static MouseSelection Default = new MouseSelection();

	public MouseSelection() {
		MouseButtons = MouseButtons.Left;
	}

	/// <summary>
	/// Controls which mouse buttons invoke mouse selection.
	/// Default is MouseButtons.Left
	/// </summary>
	public MouseButtons MouseButtons {
		get; set;
	}

	public override void OnMouseDown(CellContext sender, System.Windows.Forms.MouseEventArgs e) {
		base.OnMouseDown(sender, e);

		var pressed = e.Button & MouseButtons;
		if (pressed == MouseButtons.None)
			return;

		var grid = sender.Grid;

		//Check the control and shift key status
		var controlPress = ((Control.ModifierKeys & Keys.Control) == Keys.Control &&
								(grid.SpecialKeys & GridSpecialKeys.Control) == GridSpecialKeys.Control);

		var shiftPress = ((Control.ModifierKeys & Keys.Shift) == Keys.Shift &&
							(grid.SpecialKeys & GridSpecialKeys.Shift) == GridSpecialKeys.Shift);

		//Default click handler
		if (shiftPress == false ||
			grid.Selection.EnableMultiSelection == false) {
			//Handle Control key
			var mantainSelection = grid.Selection.EnableMultiSelection && controlPress;

			//If the cell is already selected and the user has the ctrl key pressed then deselect the cell
			if (controlPress && grid.Selection.IsSelectedCell(sender.Position) && grid.Selection.ActivePosition != sender.Position)
				grid.Selection.SelectCell(sender.Position, false);
			else
				grid.Selection.Focus(sender.Position, !mantainSelection);
		} else {
			//handle shift key

			grid.Selection.ResetSelection(true);

			var rangeToSelect = new CellRange(grid.Selection.ActivePosition, sender.Position);
			grid.Selection.SelectRange(rangeToSelect, true);
		}

		// begin scroll tracking only if mouse was clicked
		// in scrollable area
		var scrollRect = grid.GetScrollableArea();
		if (scrollRect.Contains(e.Location))
			BeginScrollTracking(grid);
	}

	public override void OnMouseUp(CellContext sender, MouseEventArgs e) {
		base.OnMouseUp(sender, e);

		if (e.Button != MouseButtons.Left)
			return;

		sender.Grid.MouseSelectionFinish();

		EndScrollTracking();
	}

	/// <summary>
	/// Ends scroll tracking on double click
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public override void OnDoubleClick(CellContext sender, EventArgs e) {
		base.OnDoubleClick(sender, e);

		EndScrollTracking();
	}

	/// <summary>
	/// Start the timer to scroll the visible area
	/// </summary>
	/// <param name="grid"></param>
	private void BeginScrollTracking(GridVirtual grid) {
		//grid.Capture = true;
		_capturedGrid = grid;

		if (_scrollTimer == null) {
			_scrollTimer = new Timer();
			_scrollTimer.Interval = 100;
			_scrollTimer.Tick += this.ScrollTimerTick;
		}
		_scrollTimer.Enabled = true;
	}

	private void ScrollTimerTick(object sender, EventArgs args) {
		if (_capturedGrid == null)
			return;
		if (_capturedGrid.IsDisposed == true) {
			EndScrollTracking();
			return;
		}

		//If grid has lost focus end scroll tracking
		if (!_capturedGrid.Focused) {
			EndScrollTracking();
			return;
		}

		//Scroll the view if required
		var mousePoint = _capturedGrid.PointToClient(Control.MousePosition);
		_capturedGrid.ScrollOnPoint(mousePoint);
		// If we are scrolling view fire mouse move event to change also selection
		if (_capturedMouseMoveEventArgs != null)
			this.OnMouseMove(_capturedMouseMoveSender,
				new MouseEventArgs(_capturedMouseMoveEventArgs.Button,
					_capturedMouseMoveEventArgs.Clicks,
					mousePoint.X,
					mousePoint.Y,
					_capturedMouseMoveEventArgs.Delta));
	}

	/// <summary>
	/// Used for mouse multi selection and mouse scrolling
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public override void OnMouseMove(CellContext sender, MouseEventArgs e) {
		if (_capturedGrid != null && _capturedMouseMoveEventArgs != null) {
			_capturedMouseMoveSender = CellContext.Empty;
			_capturedMouseMoveEventArgs = null;
		}

		base.OnMouseMove(sender, e);

		//First check if the multi selection is enabled and the active position is valid
		if (sender.Grid.Selection.EnableMultiSelection == false ||
			sender.Grid.MouseDownPosition.IsEmpty() ||
			sender.Grid.MouseDownPosition != sender.Grid.Selection.ActivePosition)
			return;

		// preprare helper variables
		var lastVisibleRow = sender.Grid.Rows.LastVisibleScrollableRow;
		var lastVisibleColumn = sender.Grid.Columns.LastVisibleScrollableColumn;
		var firstVisibleRow = sender.Grid.Rows.FirstVisibleScrollableRow;
		var firstVisibleColumn = sender.Grid.Columns.FirstVisibleScrollableColumn;

		//Check if the mouse position is valid and try to fix it if it's not
		var row = sender.Grid.Rows.RowAtPoint(e.Y);
		var column = sender.Grid.Columns.ColumnAtPoint(e.X);
		if (!row.HasValue) {
			if (e.Y < 0)
				row = firstVisibleRow;
			else
				row = lastVisibleRow;
		}
		if (!column.HasValue) {
			if (e.X < 0)
				column = firstVisibleColumn;
			else
				column = lastVisibleColumn;
		}

		if (!column.HasValue || !row.HasValue)
			return;

		// Fix mouse position so it does not go out of visible range
		if (lastVisibleRow.HasValue && row.Value > lastVisibleRow.Value)
			row = lastVisibleRow;
		if (lastVisibleColumn.HasValue && column.Value > lastVisibleColumn.Value)
			column = lastVisibleColumn;
		if (firstVisibleRow.HasValue && row < firstVisibleRow.Value)
			row = firstVisibleRow;
		if (firstVisibleColumn.HasValue && column < firstVisibleColumn.Value)
			column = firstVisibleColumn;

		var mousePosition = new Position(row.Value, column.Value);

		//If the position type is different I don't continue
		// bacause this can cause problem for example when selection the fixed rows when the scroll is on a position > 0
		// that cause all the rows to be selected
		if (sender.Grid.GetPositionType(mousePosition) !=
			sender.Grid.GetPositionType(sender.Grid.Selection.ActivePosition))
			return;

		sender.Grid.ChangeMouseSelectionCorner(mousePosition);
		if (_capturedGrid != null) {
			_capturedMouseMoveSender = sender;
			_capturedMouseMoveEventArgs = e;
		}
	}

	/// <summary>
	/// Stop the timer
	/// </summary>
	private void EndScrollTracking() {
		//grid.Capture = false;
		if (_scrollTimer != null)
			_scrollTimer.Enabled = false;
		_capturedGrid = null;
		_capturedMouseMoveEventArgs = null;
		_capturedMouseMoveSender = CellContext.Empty;
	}
}
