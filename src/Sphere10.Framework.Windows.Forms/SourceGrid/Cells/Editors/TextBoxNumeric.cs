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
/// An editor that use a TextBoxTypedNumeric for editing support. You can customize the Control.NumericCharStyle property to enable char validation.
/// </summary>
[System.ComponentModel.ToolboxItem(false)]
public class TextBoxNumeric : TextBox {
	// Constructor
	/// <summary>
	/// Construct a Model. Based on the Type specified the constructor populate AllowNull, DefaultValue, TypeConverter, StandardValues, StandardValueExclusive
	/// </summary>
	/// <param name="type">The type of this model</param>
	public TextBoxNumeric(Type type)
		: base(type) {
	}

	/// <summary>
	/// Gets the control used for editing the cell.
	/// </summary>
	public new Sphere10.Framework.Windows.Forms.TextBoxEx Control => (Sphere10.Framework.Windows.Forms.TextBoxEx)base.Control;

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
}
