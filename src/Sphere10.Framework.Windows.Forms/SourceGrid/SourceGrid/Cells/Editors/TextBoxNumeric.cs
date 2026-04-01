// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Dev Age
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;


namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors;

/// <summary>
/// An editor that use a TextBoxTypedNumeric for editing support. You can customize the Control.NumericCharStyle property to enable char validation.
/// </summary>
[System.ComponentModel.ToolboxItem(false)]
public class TextBoxNumeric : TextBox {

	#region Constructor

	/// <summary>
	/// Construct a Model. Based on the Type specified the constructor populate AllowNull, DefaultValue, TypeConverter, StandardValues, StandardValueExclusive
	/// </summary>
	/// <param name="p_Type">The type of this model</param>
	public TextBoxNumeric(Type p_Type) : base(p_Type) {
	}

	#endregion

	#region Edit Control

	/// <summary>
	/// Create the editor control
	/// </summary>
	/// <returns></returns>
	protected override Control CreateControl() {
		Sphere10.Framework.Windows.Forms.SourceGrid.DevAgeControls.DevAgeTextBox editor = new Sphere10.Framework.Windows.Forms.SourceGrid.DevAgeControls.DevAgeTextBox();
		editor.BorderStyle = BorderStyle.None;
		editor.AutoSize = false;
		editor.Validator = this;
		return editor;
	}

	/// <summary>
	/// Gets the control used for editing the cell.
	/// </summary>
	public new Sphere10.Framework.Windows.Forms.SourceGrid.DevAgeControls.DevAgeTextBox Control {
		get { return (Sphere10.Framework.Windows.Forms.SourceGrid.DevAgeControls.DevAgeTextBox)base.Control; }
	}

	#endregion

}

