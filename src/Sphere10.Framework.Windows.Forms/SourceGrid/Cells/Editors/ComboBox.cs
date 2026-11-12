// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors;

/// <summary>
/// Editor for a ComboBox (using ComboBoxEx control)
/// </summary>
[System.ComponentModel.ToolboxItem(false)]
public class ComboBox : EditorControlBase {
	#region Constructor

	/// <summary>
	/// Construct a Model. Based on the Type specified the constructor populate AllowNull, DefaultValue, TypeConverter, StandardValues, StandardValueExclusive
	/// </summary>
	/// <param name="type">The type of this model</param>
	public ComboBox(Type type)
		: base(type) {
	}

	/// <summary>
	/// Construct a Model. Based on the Type specified the constructor populate AllowNull, DefaultValue, TypeConverter, StandardValues, StandardValueExclusive
	/// </summary>
	/// <param name="type">The type of this model</param>
	/// <param name="standardValues"></param>
	/// <param name="standardValueExclusive">True to not allow custom value, only the values specified in the standardvalues collection are allowed.</param>
	public ComboBox(Type type, ICollection standardValues, bool standardValueExclusive)
		: base(type) {
		StandardValues = standardValues;
		StandardValuesExclusive = standardValueExclusive;
	}

	#endregion

	/// <summary>
	/// Gets the control used for editing the cell.
	/// </summary>
	public new Sphere10.Framework.Windows.Forms.ComboBoxEx Control {
		get => (Sphere10.Framework.Windows.Forms.ComboBoxEx)base.Control;
	}

	/// <summary>
	/// Set the specified value in the current editor control.
	/// </summary>
	/// <param name="editValue"></param>
	public override void SetEditValue(object editValue) {
		if (editValue is string && IsStringConversionSupported() &&
			Control.DropDownStyle == ComboBoxStyle.DropDown) {
			Control.SelectedIndex = -1;
			Control.Text = (string)editValue;
			Control.SelectionLength = 0;
			if (Control.Text != null)
				Control.SelectionStart = Control.Text.Length;
			else
				Control.SelectionStart = 0;
		} else {
			Control.SelectedIndex = -1;
			Control.Value = editValue;
			Control.SelectAll();
		}
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
		var editor = new Sphere10.Framework.Windows.Forms.ComboBoxEx();
		//editor.FlatStyle = FlatStyle.System;
		editor.Validator = this;

		//NOTE: I have changed a little the ArrangeLinkedControls to support ComboBox control

		return editor;
	}

	protected override void OnSendCharToEditor(char key) {
		if (Control.DropDownStyle == ComboBoxStyle.DropDown) {
			Control.Text = key.ToString();
			if (Control.Text != null)
				Control.SelectionStart = Control.Text.Length;
		}
	}

	#endregion
}
