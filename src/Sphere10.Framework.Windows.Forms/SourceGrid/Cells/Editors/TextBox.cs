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
/// An editor that use a TextBoxTyped for editing support.
/// </summary>
[System.ComponentModel.ToolboxItem(false)]
public class TextBox : EditorControlBase {
	// Constructor
	/// <summary>
	/// Construct a Model. Based on the Type specified the constructor populate AllowNull, DefaultValue, TypeConverter, StandardValues, StandardValueExclusive
	/// </summary>
	/// <param name="type">The type of this model</param>
	public TextBox(Type type)
		: base(type) {
	}

	/// <summary>
	/// Gets the control used for editing the cell.
	/// </summary>
	public new Sphere10.Framework.Windows.Forms.TextBoxEx Control => (Sphere10.Framework.Windows.Forms.TextBoxEx)base.Control;

	/// <summary>
	/// Set the specified value in the current editor control.
	/// </summary>
	/// <param name="editValue"></param>
	public override void SetEditValue(object editValue) {
		Control.Value = editValue;
		Control.SelectAll();
	}

	/// <summary>
	/// Returns the value inserted with the current editor control
	/// </summary>
	/// <returns></returns>
	public override object GetEditedValue() {
		return Control.Value;
	}

	// Edit Control
	/// <summary>
	/// Create the editor control
	/// </summary>
	/// <returns></returns>
	protected override Control CreateControl() {
		var editor = new Sphere10.Framework.Windows.Forms.TextBoxEx();
		editor.BorderStyle = BorderStyle.None;
		editor.AutoSize = false;
		editor.Validator = this;
		return editor;
	}

	/// <summary>
	/// This method is called just before the edit start. You can use this method to customize the editor with the cell informations.
	/// </summary>
	/// <param name="cellContext"></param>
	/// <param name="editorControl"></param>
	protected override void OnStartingEdit(CellContext cellContext, Control editorControl) {
		base.OnStartingEdit(cellContext, editorControl);

		var txtBox = (Sphere10.Framework.Windows.Forms.TextBoxEx)editorControl;
		txtBox.WordWrap = cellContext.Cell.View.WordWrap;
		txtBox.TextAlign = Tools.WinForms.ContentToHorizontalAlignment(cellContext.Cell.View.TextAlignment);

		//to set the scroll of the textbox to the initial position (otherwise the textbox use the previous scroll position)
		txtBox.SelectionStart = 0;
		txtBox.SelectionLength = 0;
	}

	protected override void OnSendCharToEditor(char key) {
		Control.Text = key.ToString();
		if (Control.Text != null)
			Control.SelectionStart = Control.Text.Length;
	}
}
