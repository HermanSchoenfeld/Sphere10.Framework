// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Controllers;

/// <summary>
/// Allow to customize the cursor of a cell. The cell must implement ICellCursor. This behavior can be shared between multiple cells.
/// </summary>
public class MouseCursor : ControllerBase {
	private bool _applyOnMouseEnter = false;
	private System.Windows.Forms.Cursor _cursor = null;

	public readonly static MouseCursor Default = new MouseCursor(System.Windows.Forms.Cursors.Default, true);
	public readonly static MouseCursor Hand = new MouseCursor(System.Windows.Forms.Cursors.Hand, true);

	// Constructor
	public MouseCursor(System.Windows.Forms.Cursor cursor, bool applyOnMouseEnter) {
		_applyOnMouseEnter = applyOnMouseEnter;
		_cursor = cursor;
	}

	public System.Windows.Forms.Cursor Cursor {
		get {
			return _cursor;
		}
		set {
			_cursor = value;
		}
	}

	// IBehaviorModel Members
	public override void OnMouseEnter(CellContext sender, EventArgs e) {
		base.OnMouseEnter(sender, e);

		if (_applyOnMouseEnter)
			ApplyCursor(sender, e);
	}

	public override void OnMouseLeave(CellContext sender, EventArgs e) {
		base.OnMouseLeave(sender, e);

		if (_applyOnMouseEnter)
			ResetCursor(sender, e);
	}

	/// <summary>
	/// Change the cursor with the cursor of the cell
	/// </summary>
	public virtual void ApplyCursor(CellContext sender, EventArgs e) {
		if (Cursor != null)
			sender.Grid.Cursor = Cursor;
	}

	/// <summary>
	/// Reset the original cursor
	/// </summary>
	public virtual void ResetCursor(CellContext sender, EventArgs e) {
		if (Cursor != null) {
			if (sender.Grid.Cursor == Cursor)
				sender.Grid.Cursor = null;
		}
	}
}
