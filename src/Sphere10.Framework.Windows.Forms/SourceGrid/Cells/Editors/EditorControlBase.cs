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

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors;

/// <summary>
/// The base class for all the editors that have a control
/// </summary>
[System.ComponentModel.ToolboxItem(false)]
public abstract class EditorControlBase : EditorBase {
	/// <summary>
	/// KeyPress event is fired in 2 cases: when the Control.KeyPress event is executed and when the user start to editing the cell by pressing a key (calling SendCharToEditor)
	/// Set the e.Handled property to true to block the event
	/// </summary>
	public event KeyPressEventHandler KeyPress;

	// Editor Control
	private Control _control;
	private GridVirtual _grid;
	private LinkedControlValue _linkedControl;

	/// <summary>
	/// Variable that indicate if the InternalEndEdit method is already called. Is used because the InternalEndEdit can be called by a the Control_Validated or directly by the user.
	/// </summary>
	private bool _isInsideEndEdit = false;

	#region Constructor

	/// <summary>
	/// Construct a Model. Based on the Type specified the constructor populate AllowNull, DefaultValue, TypeConverter, StandardValues, StandardValueExclusive
	/// </summary>
	/// <param name="type">The type of this model</param>
	public EditorControlBase(Type type)
		: base(type) {
		_control = CreateControl();
		if (Control == null)
			throw new SourceGridException("Control cannot be null");
		Control.Hide();
	}

	#endregion

	/// <summary>
	/// The Control used to edit the Cell.
	/// </summary>
	public Control Control => _control;

	/// <summary>
	/// The grid used by this editor. Null if the editor is not attached on a grid.
	/// </summary>
	public GridVirtual Grid => _grid;

	// Send Keys
	/// <summary>
	/// Used to send some keyboard keys to the active editor. It is only valid when there is an active edit operations.
	/// </summary>
	/// <param name="key"></param>
	public override sealed void SendCharToEditor(char key) {
		//if (IsEditing && Control.ContainsFocus)
		//{
		//    //Sphere10.Framework.Windows.Forms.SourceGrid.DevAgeControls.SendCharExact.Send(key); //Doesn't work well on french keyboard or in response to CAPS LOCK/shift keys combination
		//}

		var arg = new KeyPressEventArgs(key);
		OnKeyPress(arg);

		if (arg.Handled)
			return;

		OnSendCharToEditor(arg.KeyChar);
	}

	#region Minimum Size

	/// <summary>
	/// Calculate the minimum required size for the specified editor cell.
	/// </summary>
	/// <param name="cellContext"></param>
	/// <returns></returns>
	public override System.Drawing.Size GetMinimumSize(CellContext cellContext) => Control.GetPreferredSize(System.Drawing.Size.Empty);

	#endregion

	/// <summary>
	/// Apply edited value without stop the editing.
	/// </summary>
	/// <returns></returns>
	public override bool ApplyEdit() {
		//Note: I don't use SetFocusCells on this method because this methed can be called also inside some event of the control like TextChanged.

		bool bSuccess;

		if (IsEditing) {
			try {
				bSuccess = SetCellValue(EditCellContext, GetEditedValue());
			} catch (Exception exception) {
				OnEditException(new ExceptionEventArgs(exception));
				bSuccess = false;
			}
		} else
			bSuccess = true;

		return bSuccess;
	}

	/// <summary>
	/// Undo the edit value of the control to the initial value of the cell, usually used when pressing Esc key or terminate the edit operation with Cancel true.
	/// </summary>
	public virtual void UndoEditValue() {
		if (EditCell == null)
			throw new SourceGridException("Not in edit state");

		SafeSetEditValue(EditCell.Model.ValueModel.GetValue(EditCellContext));
	}

	/// <summary>
	/// Set the specified value in the current editor control calling the SetEditValue method. If an exception is throwed calls the OnUserException method and set the default value.
	/// </summary>
	/// <param name="editValue"></param>
	public void SafeSetEditValue(object editValue) {
		try {
			//Can throw an exception if the original value is not valid for the editor
			SetEditValue(editValue);
		} catch (Exception exception) {
			EditCellContext.Grid.OnUserException(new ExceptionEventArgs(new EditingCellException(exception)));
			//On exception try to put the default value
			try {
				SetEditValue(DefaultValue);
			} catch {
			}
		}
	}

	/// <summary>
	/// Returns the value inserted with the current editor control
	/// </summary>
	/// <returns></returns>
	public override abstract object GetEditedValue();

	/// <summary>
	/// Set the specified value in the current editor control.
	/// </summary>
	/// <param name="editValue"></param>
	public abstract void SetEditValue(object editValue);

	/// <summary>
	/// Start editing the cell passed. Do not call this method for start editing a cell, you must use CellContext.StartEdit.
	/// </summary>
	/// <param name="cellContext">Cell to start edit</param>
	internal override void InternalStartEdit(CellContext cellContext) {
		base.InternalStartEdit(cellContext);

		if (Control == null)
			throw new SourceGridException("Control cannot be null");

		if (IsEditing == false && EnableEdit) {
			//verifico di non avere ancora una cella associata
			if (EditCell != null)
				throw new SourceGridException("There is already a Cell in edit state");

			if (IsControlAttached() == false)
				AttachControl(cellContext.Grid);

			_linkedControl.Position = cellContext.Position;

			//aggiorno la posizione
			cellContext.Grid.ArrangeLinkedControls();

			OnStartingEdit(cellContext, Control);

			//With this method the edit start
			SetEditCell(cellContext);

			//Set the control value
			SafeSetEditValue(cellContext.Cell.Model.ValueModel.GetValue(cellContext));

			//Show the control
			ShowControl(Control);
		}
	}

	/// <summary>
	/// Terminate the edit action. Do not call this method directly, use the CellContext.EndEdit instead.
	/// </summary>
	/// <param name="cancel">True to cancel the editing and return to normal mode, false to call automatically ApplyEdit and terminate editing</param>
	/// <returns>Returns true if the cell terminate the editing mode</returns>
	internal override bool InternalEndEdit(bool cancel) {
		if (IsEditing == false)
			return true;

		if (_isInsideEndEdit)
			return false;

		_isInsideEndEdit = true;
		try {
			var bSuccess = true;

			if (cancel)
				UndoEditValue();

			if (Control.ContainsFocus) {
				//Change the focus to force a validate of the editor Control
				if (EditCellContext.Grid.Focus() == false)
					bSuccess = false;
			}

			//Apply edit value
			if (bSuccess && cancel == false)
				bSuccess = ApplyEdit();

			if (bSuccess) {
				HideControl();
				_linkedControl.Position = Position.Empty;

				//di fatto mettendo questa property a null termina logicamente l'edit
				SetEditCell(CellContext.Empty);
			} else {
				//editing failed

				if (Control.ContainsFocus == false)
					Control.Focus();
			}

			return bSuccess;
		} finally {
			_isInsideEndEdit = false;
		}
	}

	/// <summary>Removes the native editing surface from its grid without disposing this editor's control.</summary>
	internal void DetachControl() {
		// Detach callbacks before changing parentage or disposing the editing surface.
		_control.Validated -= InnerControlValidated;
		_control.KeyPress -= InnerControlKeyPress;
		SetEditCell(CellContext.Empty);
		if (_grid != null && _linkedControl != null)
			_grid.LinkedControls.Remove(_linkedControl);
		_grid = null;
		_linkedControl = null;
	}

	/// <summary>Detaches the editor and disposes the native control created by this owner.</summary>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			DetachControl();
			_control.Dispose();
		}
		base.Dispose(disposing);
	}

	/// <summary>
	/// Abstract method that must create a new Control used for the editing operations.
	/// </summary>
	/// <returns></returns>
	protected abstract Control CreateControl();

	/// <summary>
	/// This method is called just before the edit start. You can override this method to customize the editor with the cell informations.
	/// </summary>
	/// <param name="cellContext"></param>
	/// <param name="editorControl"></param>
	protected virtual void OnStartingEdit(CellContext cellContext, Control editorControl) {
		//Default properties
		if (UseCellViewProperties) {
			editorControl.BackColor = cellContext.Cell.View.BackColor;
			editorControl.ForeColor = cellContext.Cell.View.ForeColor;
			editorControl.Font = cellContext.Cell.View.Font;
		}
	}

	protected virtual void ShowControl(Control editorControl) {
		editorControl.Show();
		editorControl.BringToFront();
		editorControl.Focus();
	}

	/// <summary>
	/// Method used to precess the key sended.
	/// Abstract method that must be override in the derived class
	/// </summary>
	/// <param name="key"></param>
	protected abstract void OnSendCharToEditor(char key);

	/// <summary>
	/// Check if the Control is attached to a grid.
	/// </summary>
	private bool IsControlAttached() => _grid != null;

	/// <summary>
	/// Add the Control to the specified grid. Consider that a Control can only be a child of one Container.
	/// </summary>
	private void AttachControl(GridVirtual grid) {
		_grid = grid;
		_linkedControl = new LinkedControlValue(Control, Position.Empty);
		Control.Hide();
		grid.LinkedControls.Add(_linkedControl);
		Control.Validated += new EventHandler(InnerControlValidated);
		Control.KeyPress += new KeyPressEventHandler(InnerControlKeyPress);
	}

	private void HideControl() {
		try {
			// invoke "Hide", method with some reflection stuff.
			// See test SourceGrid.Tests.net_2_0.TestOverride_With_New
			// why this is necessary
			Control.GetType().GetMethod("Hide").Invoke(Control, null);
		} catch {
			// if something goes wrong, just call Hide normally
			Control.Hide();
		}
	}

	/// <summary>
	/// Validated of the editor control
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private void InnerControlValidated(object sender, EventArgs args) {
		try {
			if (IsEditing)
				EditCellContext.EndEdit(false);
		} catch (Exception exception) {
			OnEditException(new ExceptionEventArgs(exception));
		}
	}

	private void InnerControlKeyPress(object sender, KeyPressEventArgs args) {
		if (args.Handled)
			return;

		OnKeyPress(args);
	}

	protected virtual void OnKeyPress(KeyPressEventArgs e) {
		if (e.Handled)
			return;

		if (KeyPress != null)
			KeyPress(this, e);
	}
}
