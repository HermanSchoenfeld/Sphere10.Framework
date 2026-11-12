// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors;

/// <summary>
/// An editor that use a RichTextBoxTyped for editing support.
/// </summary>
[System.ComponentModel.ToolboxItem(false)]
public class RichTextBox : EditorControlBase {
	#region Constructor

	/// <summary>
	/// Constructor
	/// </summary>
	public RichTextBox()
		: base(typeof(Sphere10.Framework.Windows.Forms.RichText)) {
		TypeConverter = new Sphere10.Framework.Windows.Forms.RichTextTypeConverter();
	}

	#endregion

	/// <summary>
	/// Gets the control used for editing the cell.
	/// </summary>
	public new Sphere10.Framework.Windows.Forms.RichTextBoxEx Control {
		get => (Sphere10.Framework.Windows.Forms.RichTextBoxEx)base.Control;
	}

	/// <summary>
	/// Set the specified value in the current editor control.
	/// </summary>
	/// <param name="editValue"></param>
	public override void SetEditValue(object editValue) {
		Control.Value = editValue as Sphere10.Framework.Windows.Forms.RichText;
		Control.SelectAll();
	}

	/// <summary>
	/// Returns the value inserted with the current editor control
	/// </summary>
	/// <returns></returns>
	public override object GetEditedValue() => Control.Value;

	#region Edit Control

	/// <summary>
	/// Create the editor control
	/// </summary>
	/// <returns></returns>
	protected override Control CreateControl() {
		var editor = new Sphere10.Framework.Windows.Forms.RichTextBoxEx();
		editor.BorderStyle = BorderStyle.None;
		editor.AutoSize = false;
		editor.Validator = this;
		return editor;
	}

	/// <summary>
	/// This method is called just before the edit start.
	/// You can use this method to customize the editor with the cell informations.
	/// </summary>
	/// <param name="cellContext"></param>
	/// <param name="editorControl"></param>
	protected override void OnStartingEdit(CellContext cellContext, Control editorControl) {
		base.OnStartingEdit(cellContext, editorControl);

		var rchTxtBox = (Sphere10.Framework.Windows.Forms.RichTextBoxEx)editorControl;
		rchTxtBox.WordWrap = cellContext.Cell.View.WordWrap;

		// to set the scroll of the textbox to the initial position
		//(otherwise the richtextbox uses the previous scroll position)
		rchTxtBox.SelectionStart = 0;
		rchTxtBox.SelectionLength = 0;
	}

	/// <summary>
	/// Override content of cell with sent character
	/// </summary>
	/// <param name="key"></param>
	protected override void OnSendCharToEditor(char key) {
		Control.Text = key.ToString();
		if (Control.Text != null)
			Control.SelectionStart = Control.Text.Length;
	}

	#endregion
}
