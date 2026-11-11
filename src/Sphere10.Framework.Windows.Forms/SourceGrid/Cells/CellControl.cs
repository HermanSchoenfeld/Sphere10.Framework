// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells;

/// <summary>
/// The CellControl class is used to create a cell with a Windows Forms Control inside.
/// The CellControl class requires a new Windows Forms control for each cell. Unfortunately Winwods Forms control requires a lot of system resources and with many cells this can cause system fault or out of memory conditions.
/// Basically I suggest to use CellControl with no more than 50 cells and only if necessary, usually it is better to use standard cells.
/// Another problem with the CellControl class is that it is not integrated well with the rest of the grid (control borders, cell navigation, ...)
/// Finally a cell of type CellControl cannot be moved, for example you cannot use sort, move the columns, ... when using a CellControl.
/// </summary>
[Obsolete("I will soon remove this class. If you need to add a user control to the grid I suggest to manually add or remove it using the Grid.LinkedControls collection.")]
public class CellControl : Cell {
	private Control _control;
	private LinkedControlScrollMode _scrollMode = LinkedControlScrollMode.BasedOnPosition;
	private bool _useCellBorder = true;

	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="control">Control to insert inside the grid</param>
	public CellControl(Control control)
		: base(null) {
		_control = control;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="control">Control to insert inside the grid</param>
	/// <param name="scrollMode"></param>
	/// <param name="useCellBorder"></param>
	public CellControl(Control control, LinkedControlScrollMode scrollMode, bool useCellBorder)
		: this(control) {
		_control = control;
		_scrollMode = scrollMode;
		_useCellBorder = useCellBorder;
	}

	/// <summary>
	/// Gets the control associated with this cell.
	/// </summary>
	public Control Control => _control;

	public override void BindToGrid(Grid grid, Position position) {
		base.BindToGrid(grid, position);

		var linkedValue = new LinkedControlValue(_control, Range.Start);
		linkedValue.ScrollMode = _scrollMode;
		linkedValue.UseCellBorder = _useCellBorder;
		Grid.LinkedControls.Add(linkedValue);

		Grid.ArrangeLinkedControls();
	}

	public override void UnBindToGrid() {
		if (Grid.LinkedControls.GetByControl(_control) != null)
			Grid.LinkedControls.Remove(Grid.LinkedControls.GetByControl(_control));

		base.UnBindToGrid();
	}
}
