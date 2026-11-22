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
/// Implement the mouse resize features of a cell. This behavior can be shared between multiple cells.
/// </summary>
public class Resizable : ControllerBase {
	private MouseCursor _widthCursor = new MouseCursor(System.Windows.Forms.Cursors.VSplit, false);
	private MouseCursor _heightCursor = new MouseCursor(System.Windows.Forms.Cursors.HSplit, false);
	private CellResizeMode _resizeMode = CellResizeMode.Both;

	//		private Cursor m_ModelCursor = new Cursor();

	//Queste variabili indicano lo stato del resize (essendo usate però in un contesto di MouseEnter e MouseLeave possono essere tranquillamente condivise tra più cello o griglie, visto che il mouse in un dato momento sarà solo in una cella particolare, di un thread particolare, ...). Questo è un motivo in più per non poter usare questo controllo in multi thread (cosa che nessun controllo windows forms può fare ...)
	private bool _isWidthResize = false;
	private bool _isHeightResize = false;
	private float _distanceFromBorder = 0;
	private int _resizeColumn = -1;
	private int _grabOffset;
	private bool _endedResize;

	/// <summary>
	/// Resize both width nd height behavior
	/// </summary>
	public readonly static Resizable ResizeBoth = new Resizable(CellResizeMode.Both);

	/// <summary>
	/// Resize width behavior
	/// </summary>
	public readonly static Resizable ResizeWidth = new Resizable(CellResizeMode.Width);

	/// <summary>
	/// Resize height behavior
	/// </summary>
	public readonly static Resizable ResizeHeight = new Resizable(CellResizeMode.Height);

	/// <summary>
	/// Border used to calculate the region where the resize is enabled.
	/// </summary>
	public Sphere10.Framework.Drawing.RectangleBorder LogicalBorder = new Sphere10.Framework.Drawing.RectangleBorder(new Sphere10.Framework.Drawing.BorderLine(System.Drawing.Color.Black, 4), new Sphere10.Framework.Drawing.BorderLine(System.Drawing.Color.Black, 4));

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="mode"></param>
	public Resizable(CellResizeMode mode) => _resizeMode = mode;

	/// <summary>
	/// Resize mode of the cell
	/// </summary>
	public CellResizeMode ResizeMode => _resizeMode;

	/// <summary>
	/// Indicates if the behavior is currently resizing a cell width
	/// </summary>
	public bool IsWidthResizing => _isWidthResize;

	/// <summary>
	/// Indicates if the behavior is currently resizing a cell height
	/// </summary>
	public bool IsHeightResizing => _isHeightResize;

	/// <summary>Whether the last mouse release finished a resize, so other controllers of the same cell (a sortable header, say) do not
	/// also treat it as a click.</summary>
	public bool EndedResize => _endedResize;

	/// <summary>How far either side of a boundary the pointer grabs it, in device pixels: four logical pixels at the grid's DPI.</summary>
	public static int GetGrabWidth(GridVirtual grid) => Math.Max(4, (int)Math.Round(4 * grid.DeviceDpi / 96D));

	#region IBehaviorModel Members

	public override void OnMouseDown(CellContext sender, MouseEventArgs e) {
		base.OnMouseDown(sender, e);

		_isHeightResize = false;
		_isWidthResize = false;
		_endedResize = false;
		_resizeColumn = -1;

		var cellRect = sender.Grid.PositionToRectangle(sender.Position);
		var mousePoint = new Point(e.X, e.Y);

		if ((ResizeMode & CellResizeMode.Width) == CellResizeMode.Width && TryGetWidthBoundary(sender, cellRect, mousePoint, out var column, out var boundary)) {
			_isWidthResize = true;
			_resizeColumn = column;
			_grabOffset = boundary - mousePoint.X;
			return;
		}
		var partType = GetBorder(sender.Grid).GetPointPartType(cellRect, mousePoint, out _distanceFromBorder);
		if (((ResizeMode & CellResizeMode.Height) == CellResizeMode.Height) &&
					partType == Sphere10.Framework.Drawing.RectanglePartType.BottomBorder)
			_isHeightResize = true;
	}

	public override void OnMouseUp(CellContext sender, MouseEventArgs e) {
		base.OnMouseUp(sender, e);

		_endedResize = _isWidthResize || _isHeightResize;
		_isWidthResize = false;
		_isHeightResize = false;
	}

	public override void OnMouseMove(CellContext sender, MouseEventArgs e) {
		base.OnMouseMove(sender, e);

		var cellRect = sender.Grid.PositionToRectangle(sender.Position);
		if (cellRect.IsEmpty)
			return;

		var mousePoint = new Point(e.X, e.Y);

		float dummy;
		var partType = GetBorder(sender.Grid).GetPointPartType(cellRect, mousePoint, out dummy);

		// Already resizing: the boundary follows the pointer, wherever on the line it was grabbed.
		if (sender.Grid.MouseDownPosition == sender.Position && (_isWidthResize || _isHeightResize)) {
			if (_isWidthResize) {
				var columnLeft = sender.Grid.Columns.GetLeft(_resizeColumn) - (sender.Grid.Columns.GetLeft(sender.Position.Column) - cellRect.Left);
				var newWidth = mousePoint.X + _grabOffset - columnLeft;

				if (newWidth > 0)
					sender.Grid.Columns.SetWidth(_resizeColumn, newWidth);

				_widthCursor.ApplyCursor(sender, e);
				_heightCursor.ResetCursor(sender, e);
			} else if (_isHeightResize) {
				var newHeight = mousePoint.Y - cellRect.Top;

				if (newHeight > 0)
					SetHeight(sender.Grid, sender.Position, (int)(newHeight + _distanceFromBorder));

				_heightCursor.ApplyCursor(sender, e);
				_widthCursor.ResetCursor(sender, e);
			}
		} else {
			if ((ResizeMode & CellResizeMode.Width) == CellResizeMode.Width && TryGetWidthBoundary(sender, cellRect, mousePoint, out _, out _))
				_widthCursor.ApplyCursor(sender, e);
			else if (partType == Sphere10.Framework.Drawing.RectanglePartType.BottomBorder && (ResizeMode & CellResizeMode.Height) == CellResizeMode.Height)
				_heightCursor.ApplyCursor(sender, e);
			else {
				_widthCursor.ResetCursor(sender, e);
				_heightCursor.ResetCursor(sender, e);
			}
		}
	}

	public override void OnMouseLeave(CellContext sender, EventArgs e) {
		base.OnMouseLeave(sender, e);

		_widthCursor.ResetCursor(sender, e);
		_heightCursor.ResetCursor(sender, e);
		_isWidthResize = false;
		_isHeightResize = false;
	}

	public override void OnDoubleClick(CellContext sender, EventArgs e) {
		base.OnDoubleClick(sender, e);

		var currentPoint = sender.Grid.PointToClient(System.Windows.Forms.Control.MousePosition);
		var cellRect = sender.Grid.PositionToRectangle(sender.Position);

		float distance;
		var partType = GetBorder(sender.Grid).GetPointPartType(cellRect, currentPoint, out distance);

		if ((ResizeMode & CellResizeMode.Width) == CellResizeMode.Width && TryGetWidthBoundary(sender, cellRect, currentPoint, out var column, out _))
			sender.Grid.Columns.AutoSizeColumn(column);
		else if ((ResizeMode & CellResizeMode.Height) == CellResizeMode.Height &&
							partType == Sphere10.Framework.Drawing.RectanglePartType.BottomBorder)
			sender.Grid.Rows.AutoSizeRow(sender.Position.Row);
	}

	#endregion

	// The boundary a column is resized by is grabbed from either side of the line: inside this cell's right edge it is this cell's last column,
	// inside its left edge the column before it. People aim at the line itself, so a grab on one side only is too narrow to find.
	private static bool TryGetWidthBoundary(CellContext sender, Rectangle cellRect, Point point, out int column, out int boundary) {
		var grab = GetGrabWidth(sender.Grid);
		var range = sender.Grid.PositionToCellRange(sender.Position);
		column = -1;
		boundary = 0;
		if (point.X >= cellRect.Right - grab && point.X <= cellRect.Right + 1) {
			column = range.End.Column;
			boundary = cellRect.Right;
			return true;
		}
		if (point.X < cellRect.Left + grab && range.Start.Column > 0 && sender.Grid.Columns.IsColumnVisible(range.Start.Column - 1)) {
			column = range.Start.Column - 1;
			boundary = cellRect.Left;
			return true;
		}
		return false;
	}

	private Sphere10.Framework.Drawing.RectangleBorder GetBorder(GridVirtual grid) {
		var grab = GetGrabWidth(grid);
		return grab == 4 ? LogicalBorder
			: new Sphere10.Framework.Drawing.RectangleBorder(new Sphere10.Framework.Drawing.BorderLine(System.Drawing.Color.Black, grab), new Sphere10.Framework.Drawing.BorderLine(System.Drawing.Color.Black, grab));
	}

	private void SetWidth(GridVirtual grid, Position position, int width) {
		var range = grid.PositionToCellRange(position);
		var widthForCol = width / range.ColumnsCount;
		for (var c = range.Start.Column; c <= range.End.Column; c++)
			grid.Columns.SetWidth(c, widthForCol);
	}

	private void SetHeight(GridVirtual grid, Position position, int height) {
		var range = grid.PositionToCellRange(position);
		var heightForCol = height / range.RowsCount;
		for (var r = range.Start.Row; r <= range.End.Row; r++)
			grid.Rows.SetHeight(r, heightForCol);
	}
	//		#region Support Functions
	//		/// <summary>
	//		///
	//		/// </summary>
	//		/// <param name="p_CellRectangle">A grid relative rectangle</param>
	//		/// <param name="p"></param>
	//		/// <returns></returns>
	//		public static bool IsInResizeHorRegion(Rectangle p_CellRectangle, Point p)
	//		{
	//			if (p.X >= p_CellRectangle.Right-c_MouseDelta && p.X <= p_CellRectangle.Right)
	//				return true;
	//			else
	//				return false;
	//		}
	//
	//		/// <summary>
	//		///
	//		/// </summary>
	//		/// <param name="p_CellRectangle">A grid relative rectangle</param>
	//		/// <param name="p"></param>
	//		/// <returns></returns>
	//		public static bool IsInResizeVerRegion(Rectangle p_CellRectangle, Point p)
	//		{
	//			if (p.Y >= p_CellRectangle.Bottom-c_MouseDelta && p.Y <= p_CellRectangle.Bottom)
	//				return true;
	//			else
	//				return false;
	//		}
	//
	//		private const int c_MouseDelta = 4;
	//
	//		#endregion
}
