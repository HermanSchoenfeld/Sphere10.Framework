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

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Controllers;

//TODO I can now use the VisualElement GetElementsAtPoint method to check the checkbox only when clicking on the check and not when clicking on the header. This can also be used with the HeaderCheckBox cell (to enable the sort when clicking outside the check and to select all when clicking the check box.

/// <summary>
/// Summary description for BehaviorModelCheckBox. This behavior can be shared between multiple cells.
/// </summary>
public class CheckBox : ControllerBase {
	/// <summary>
	/// An event similar to Windows.Forms.CheckBox.CheckedChanged.
	/// Fired when value changed
	/// </summary>
	public event EventHandler CheckedChanged;

	/// <summary>
	/// I mantain the last mouse button pressed here to simulate exactly the behavior of the standard system CheckBox.
	///
	/// Here are the events executed on a system checkbox:
	///
	/// [status checked = false]
	/// MouseDown [status checked = false]
	/// CheckedChanged [status checked = true]
	/// Click [status checked = true]
	/// MouseUp [status checked = true]
	///
	/// Consider that I can use this member varialbes because also if you have multiple grid or multiple threads there is only one mouse that can fire the events.
	/// Consider also that I cannot use the Click event because in that event I don't have informations about the button pressed.
	/// </summary>
	private MouseButtons _lastButton = MouseButtons.None;
	private bool _autoChangeValueOfSelectedCells = false;

	/// <summary>
	/// Default behavior checkbox
	/// </summary>
	public readonly static CheckBox Default = new CheckBox();

	/// <summary>
	/// Constructor
	/// </summary>
	public CheckBox() {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="autoChangeValueOfSelectedCells">Indicates if this cells when checked or uncheck must change also the value of the selected cells of type CellCheckBox.</param>
	public CheckBox(bool autoChangeValueOfSelectedCells) {
		_autoChangeValueOfSelectedCells = autoChangeValueOfSelectedCells;
	}

	/// <summary>
	/// Indicates if this cells when checked or uncheck must change also the value of the selected cells of type CellCheckBox. Default is false
	/// </summary>
	public bool AutoChangeValueOfSelectedCells {
		get {
			return _autoChangeValueOfSelectedCells;
		}
	}

	// Controller Events
	public override void OnKeyPress(CellContext sender, KeyPressEventArgs e) {
		base.OnKeyPress(sender, e);

		if (e.KeyChar == ' ')
			UIChangeChecked(sender, e);
	}

	public override void OnMouseDown(CellContext sender, MouseEventArgs e) {
		base.OnMouseDown(sender, e);

		_lastButton = e.Button;
	}

	public override void OnClick(CellContext sender, EventArgs e) {
		base.OnClick(sender, e);

		if (_lastButton == MouseButtons.Left)
			UIChangeChecked(sender, e);
	}

	/// <summary>
	/// Toggle the value of the current cell and if AutoChangeValueOfSelectedCells is true of all the selected cells.
	/// Simulate an edit operation.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private void UIChangeChecked(CellContext sender, EventArgs args) {
		var checkModel = (Models.ICheckBox)sender.Cell.Model.FindModel(typeof(Models.ICheckBox));
		;
		if (checkModel == null)
			throw new SourceGrid.SourceGridException("Models.ICheckBox not found");

		var checkStatus = checkModel.GetCheckBoxStatus(sender);
		if (checkStatus.CheckEnable) {
			var newVal = true;
			if (checkStatus.Checked != null)
				newVal = !checkStatus.Checked.Value;

			sender.StartEdit();
			try {
				checkModel.SetCheckedValue(sender, newVal);
				sender.EndEdit(false);
				OnCheckedChanged(EventArgs.Empty);
			} catch (Exception exception) {
				sender.EndEdit(true);
				throw new Exception(string.Empty, exception);
			}

			//change the status of all selected control
			if (AutoChangeValueOfSelectedCells)
				AutoChangeValues(sender, newVal);
		}
	}

	protected virtual void OnCheckedChanged(EventArgs e) {
		if (CheckedChanged != null)
			CheckedChanged(this, e);
	}

	private void AutoChangeValues(CellContext sender, bool newVal) {
		foreach (var position in sender.Grid.Selection.GetSelectionRegion().GetCellsPositions()) {
			var c = sender.Grid.GetCell(position);
			Models.ICheckBox check;
			if (c != this && c != null &&
				(check = (Models.ICheckBox)c.Model.FindModel(typeof(Models.ICheckBox))) != null) {
				var context = new CellContext(sender.Grid, position, c);
				context.StartEdit();
				try {
					check.SetCheckedValue(context, newVal);
					context.EndEdit(false);
				} catch (Exception) {
					context.EndEdit(true);
					throw;
				}
			}
		}
	}
}
