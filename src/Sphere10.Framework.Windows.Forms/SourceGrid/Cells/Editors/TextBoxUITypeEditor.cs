// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing.Design;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors;

/// <summary>
///  An editor that use a UITypeEditor to edit the cell.
/// </summary>
[System.ComponentModel.ToolboxItem(false)]
public class TextBoxUITypeEditor : TextBoxButton {
	#region Constructor

	/// <summary>
	/// Construct a Model. Based on the Type specified the constructor populate AllowNull, DefaultValue, TypeConverter, StandardValues, StandardValueExclusive
	/// </summary>
	/// <param name="type">The type of this model</param>
	public TextBoxUITypeEditor(Type type)
		: base(type) {
	}

	#endregion

	/// <summary>
	/// Gets the control used for editing the cell.
	/// </summary>
	public new Sphere10.Framework.Windows.Forms.TextBoxUITypeEditor Control {
		get => (Sphere10.Framework.Windows.Forms.TextBoxUITypeEditor)base.Control;
	}

	#region Edit Control

	/// <summary>
	/// Create the editor control
	/// </summary>
	/// <returns></returns>
	protected override Control CreateControl() {
		var editor = new Sphere10.Framework.Windows.Forms.TextBoxUITypeEditor();
		editor.BorderStyle = Sphere10.Framework.Windows.Forms.Drawing.BorderStyle.None;
		editor.Validator = this;

		var objEditor = System.ComponentModel.TypeDescriptor.GetEditor(base.ValueType, typeof(System.Drawing.Design.UITypeEditor));
		UITypeEditor uiEditor = null;
		if (editor != null)
			uiEditor = (System.Drawing.Design.UITypeEditor)objEditor;

		editor.UITypeEditor = uiEditor;
		return editor;
	}

	#endregion
}
