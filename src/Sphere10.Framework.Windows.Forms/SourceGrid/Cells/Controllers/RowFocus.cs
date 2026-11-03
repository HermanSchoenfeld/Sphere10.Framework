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
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Controllers;

/// <summary>
/// RowFocus controller overrides the OnFocusEntering method and set the Focus on the Row. This controller is usually used on the Row Header. This controller also add an arror Cursor when the mouse is over the cell.
/// </summary>
public class RowFocus : ControllerBase {
	private MouseCursor _selectionCursor = new MouseCursor(Tools.WinForms.LoadRawCursor(Resources.CursorRightArrow), false);

	/// <summary>
	/// Default controller to select all the row
	/// </summary>
	public readonly static RowFocus Default = new RowFocus();

	/// <summary>
	/// Border used to calculate the region to enable the row selection
	/// </summary>
	public Sphere10.Framework.Drawing.RectangleBorder LogicalBorder = new Sphere10.Framework.Drawing.RectangleBorder(new Sphere10.Framework.Drawing.BorderLine(System.Drawing.Color.Black, 4), new Sphere10.Framework.Drawing.BorderLine(System.Drawing.Color.Black, 4));

	public RowFocus() {
	}

	//Non uso questo evento altrimenti non verrebbero applicate le regole di selezione del tasto Shift
	//		public override void OnClick(PositionEventArgs e)
	//		{
	//			base.OnClick (e);
	//
	//			sender.Grid.Rows[sender.Position.Row].Focus();
	//		}

	public override void OnMouseMove(CellContext sender, MouseEventArgs e) {
		base.OnMouseMove(sender, e);

		var cellRect = sender.Grid.PositionToRectangle(sender.Position);
		var mousePoint = new Point(e.X, e.Y);

		float distance;
		var partType = LogicalBorder.GetPointPartType(cellRect, mousePoint, out distance);

		if (partType == Sphere10.Framework.Drawing.RectanglePartType.ContentArea)
			_selectionCursor.ApplyCursor(sender, e);
	}

	public override void OnMouseLeave(CellContext sender, EventArgs e) {
		base.OnMouseLeave(sender, e);

		_selectionCursor.ResetCursor(sender, e);
	}

	public override void OnFocusEntering(CellContext sender, System.ComponentModel.CancelEventArgs e) {
		base.OnFocusEntering(sender, e);

		sender.Grid.Selection.FocusRow(sender.Position.Row);
	}
}
