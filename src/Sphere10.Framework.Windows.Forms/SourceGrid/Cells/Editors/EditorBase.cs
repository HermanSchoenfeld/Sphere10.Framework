// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors;

/// <summary>
/// Represents the base class of a DataModel. This DataModel support conversion but doesn't provide any user interface editor.
/// </summary>
[System.ComponentModel.ToolboxItem(false)]
public class EditorBase : Sphere10.Framework.ComponentModel.TypeConverterValidator {
	private event ValidatingCellEventHandler ValidatingHandlers;

	private event CellContextEventHandler ValidatedHandlers;

	/// <summary>
	/// Validating event
	/// </summary>
	[Obsolete("You should use the Control.Validating event")]
	public event ValidatingCellEventHandler Validating {
		add {
			ValidatingHandlers += value;
		}
		remove {
			ValidatingHandlers -= value;
		}
	}

	/// <summary>
	/// Validated event
	/// </summary>
	[Obsolete("You should use the Control.Validated event")]
	public event CellContextEventHandler Validated {
		add {
			ValidatedHandlers += value;
		}
		remove {
			ValidatedHandlers -= value;
		}
	}

	/// <summary>
	/// Event fired when an exception is throw in the Validated event or in an editing method
	/// </summary>
	public event ExceptionEventHandler EditException;

	// Edit coordinates
	//Queste variabili vengono usato durante le fasi di editing
	private CellContext _editCellContext = CellContext.Empty;

	// ErrorString
	/// <summary>
	/// Error rappresentation
	/// </summary>
	private string _errorString = "#ERROR!";

	// Editable settings
	private bool _enableCellDrawOnEdit = true;
	private bool _useCellViewProperties = true;

	// Constructor
	/// <summary>
	/// Construct a Model. Based on the Type specified the constructor populate StringEditor property
	/// </summary>
	/// <param name="type">The type of this model</param>
	public EditorBase(Type type)
		: base(type) {
	}

	/// <summary>
	/// Cell in editing, if null no cell is in editing state
	/// </summary>
	public CellContext EditCellContext => _editCellContext;

	/// <summary>
	/// Cell in editing, if null no cell is in editing state
	/// </summary>
	public Cells.ICellVirtual EditCell => _editCellContext.Cell;

	/// <summary>
	/// Cell in editing, if Empty no cell is in editing state
	/// </summary>
	public Position EditPosition => _editCellContext.Position;

	/// <summary>
	/// String used when error occurred
	/// </summary>
	public string ErrorString {
		get => _errorString;
		set => _errorString = value;
	}

	/// <summary>
	/// Enable or disable the cell editor (if disable no visual edit is allowed)
	/// </summary>
	public bool EnableEdit { get; set; } = true;

	/// <summary>
	/// Mode to edit the cell.
	/// </summary>
	public EditableMode EditableMode { get; set; } = EditableMode.Default;

	/// <summary>
	/// Indicates if the draw of the cell when in editing mode is enabled.
	/// </summary>
	public virtual bool EnableCellDrawOnEdit {
		get => _enableCellDrawOnEdit;
		set => _enableCellDrawOnEdit = value;
	}

	/// <summary>
	/// Gets or sets if the editor must assign to the editor control the default view properties: ForeColor, BackColor, Font. This can be disabled if you want to manually assign these properties to the control.
	/// </summary>
	public virtual bool UseCellViewProperties {
		get => _useCellViewProperties;
		set => _useCellViewProperties = value;
	}

	// StartEdit/EndEdit/IsEditing/ApplyEdit/GetEditedValue
	/// <summary>
	/// Indicates if the current editor is in editing state
	/// </summary>
	public bool IsEditing => (EditCell != null);

	/// <summary>
	/// Returns true if the string passed is equal to the error string rappresentation
	/// </summary>
	/// <param name="text"></param>
	/// <returns></returns>
	public bool IsErrorString(string text) {
		if (text == ErrorString)
			return true;
		return false;
	}

	/// <summary>
	/// Apply edited value
	/// </summary>
	/// <returns></returns>
	public virtual bool ApplyEdit() {
		return true;
	}

	/// <summary>
	/// Returns the new value edited with the custom control
	/// </summary>
	/// <returns></returns>
	public virtual object GetEditedValue() {
		throw new SourceGridException("No valid cell editor found");
	}

	// ClearCell/SetCellValue
	/// <summary>
	/// Clear the value of the cell using the default value
	/// </summary>
	/// <param name="cellContext"></param>
	public virtual void ClearCell(CellContext cellContext) {
		SetCellValue(cellContext, DefaultValue);
	}

	/// <summary>
	/// Change the value of the cell applying the rule of the current editor. Is recommend to use this method to simulate a edit operation and to validate the cell value using the current model.
	/// Doesn't call the StartEdit and EndEdit but change directly the cell value. Use the CellContext.Start edit to begin an edit operation.
	/// </summary>
	/// <param name="cellContext">Cell to change the value</param>
	/// <param name="newValue"></param>
	/// <returns>returns true if the value passed is valid and has been applied to the cell</returns>
	public virtual bool SetCellValue(CellContext cellContext, object newValue) {
		if (EnableEdit) {
			if (cellContext.Cell == null)
				throw new SourceGridException("Invalid CellContext, cell is null");

			var cancelEvent = new ValidatingCellEventArgs(cellContext, newValue);
			OnValidating(cancelEvent);

			//check if cancel == true
			if (cancelEvent.Cancel == false) {
				var prevValue = cellContext.Cell.Model.ValueModel.GetValue(cellContext);
				try {
					cellContext.Cell.Model.ValueModel.SetValue(cellContext, ObjectToValue(cancelEvent.NewValue));
					OnValidated(new CellContextEventArgs(cellContext));
				} catch (Exception exception) {
					OnEditException(new ExceptionEventArgs(exception));
					cellContext.Cell.Model.ValueModel.SetValue(cellContext, prevValue);
					cancelEvent.Cancel = true; //di fatto è fallita la validazione del dato
				}
			}

			return (cancelEvent.Cancel == false);
		}
		return false;
	}

	// Send Keys
	/// <summary>
	/// Used to send some keyboard keys to the active editor. It is only valid when there is an active edit operations.
	/// </summary>
	/// <param name="key"></param>
	public virtual void SendCharToEditor(char key) {
		//No Action
	}

	// Minimum Size
	/// <summary>
	/// Calculate the minimum required size for the specified editor cell.
	/// </summary>
	/// <param name="cellContext"></param>
	/// <returns></returns>
	public virtual System.Drawing.Size GetMinimumSize(CellContext cellContext) {
		return System.Drawing.Size.Empty;
	}

	/// <summary>
	/// Set the current editing cell, for an editor only one cell can be in editing state
	/// </summary>
	/// <param name="cellContext"></param>
	protected void SetEditCell(CellContext cellContext) {
		_editCellContext = cellContext;
	}

	// Validating
	//TODO: Remove OnValidated and OnValidating methods. For a better user experience (focus, validating, ...) must be used the validating event of the Control. The cell simply check if the value is valid but cannot handle correctly the user interface.

	/// <summary>
	/// Functions used when the validating operation is finished
	/// </summary>
	/// <param name="e"></param>
	protected void OnValidated(CellContextEventArgs e) {
		if (ValidatedHandlers != null)
			ValidatedHandlers(this, e);
	}

	/// <summary>
	/// Validating the value of the cell.
	/// </summary>
	/// <param name="e"></param>
	protected void OnValidating(ValidatingCellEventArgs e) {
		if (ValidatingHandlers != null)
			ValidatingHandlers(this, e);
	}

	/// <summary>
	/// Event fired when an exception is throw in the Validated event or in an editing method
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnEditException(ExceptionEventArgs e) {
#if DEBUG
		System.Diagnostics.Debug.WriteLine("Exception on editing cell: " + e.Exception.ToString());
#endif

		if (EditException != null)
			EditException(this, e);
	}

	/// <summary>
	/// Start editing the cell passed. Do not call this method for start editing a cell, you must use CellContext.StartEdit.
	/// </summary>
	/// <param name="cellContext">Cell to start edit</param>
	internal virtual void InternalStartEdit(CellContext cellContext) {
		if (cellContext.Cell == null)
			throw new ArgumentNullException("cellContext.Cell");
		if (cellContext.Grid == null)
			throw new ArgumentNullException("cellContext.Grid");
		if (cellContext.Grid.Selection.ActivePosition != cellContext.Position)
			throw new SourceGridException("Cell must have the focus");
		//no edit supported for this editor
	}

	/// <summary>
	/// Cancel the edit action. Do not call this method directly, use the CellContext.EndEdit instead.
	/// </summary>
	/// <param name="cancel">True to cancel the editing and return to normal mode, false to call automatically ApplyEdit and terminate editing</param>
	/// <returns>Returns true if the cell terminate the editing mode</returns>
	internal virtual bool InternalEndEdit(bool cancel) {
		return true;
	}
}
