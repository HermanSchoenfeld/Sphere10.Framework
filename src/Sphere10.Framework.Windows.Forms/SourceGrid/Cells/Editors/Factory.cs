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

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors;

public static class Factory {
	#region DataModel Utility

	/// <summary>
	/// Construct an EditorBase for the specified type. You can set the value returned in the Editor property.
	/// If the Type support a UITypeEditor returns a EditorUITypeEditor else if the type has a StandardValues list return a EditorComboBox else if the type support string conversion returns a EditorTextBox otherwise returns null.
	/// </summary>
	/// <param name="type">Type to edit</param>
	/// <returns></returns>
	public static Cells.Editors.EditorBase Create(Type type) {
		var typeConverter = System.ComponentModel.TypeDescriptor.GetConverter(type);
		ICollection standardValues = null;
		var standardValuesExclusive = false;
		if (typeConverter != null) {
			standardValues = typeConverter.GetStandardValues();
			if (standardValues != null && standardValues.Count > 0)
				standardValuesExclusive = typeConverter.GetStandardValuesExclusive();
			else
				standardValuesExclusive = false;
		}
		var uiTypeEditor = System.ComponentModel.TypeDescriptor.GetEditor(type, typeof(System.Drawing.Design.UITypeEditor));
		if (uiTypeEditor != null) //UITypeEditor founded
			return new Cells.Editors.TextBoxUITypeEditor(type);
		if (standardValues != null) //combo box
			return new Cells.Editors.ComboBox(type, standardValues, standardValuesExclusive);
		if (typeConverter != null && typeConverter.CanConvertFrom(typeof(string))) //txtbox
			return new Cells.Editors.TextBox(type);
		//no editor found
		return null;
	}

	/// <summary>
	/// Construct a CellEditor for the specified type
	/// </summary>
	/// <param name="type">Cell Type</param>
	/// <param name="defaultValue">Default value of the editor</param>
	/// <param name="allowNull">Allow null</param>
	/// <param name="standardValues">List of available values or null if there is no available values list</param>
	/// <param name="standardValueExclusive">Indicates if the p_StandardValue are the unique values supported</param>
	/// <param name="typeConverter">Type converter used for conversion for the specified type</param>
	/// <param name="uiTypeEditor">UITypeEditor if null must be populated the TypeConverter</param>
	/// <returns></returns>
	public static Cells.Editors.EditorBase Create(

	#endregion

Type type,
object defaultValue,
bool allowNull,
System.Collections.ICollection standardValues,
bool standardValueExclusive,
System.ComponentModel.TypeConverter typeConverter,
System.Drawing.Design.UITypeEditor uiTypeEditor
) {
		Cells.Editors.EditorBase editor;
		if (uiTypeEditor == null) {
			if (standardValues != null) {
				var editCombo = new Cells.Editors.ComboBox(type);
				editor = editCombo;
			} else if (typeConverter != null && typeConverter.CanConvertFrom(typeof(string))) {
				//txtbox

				var editTextBox = new Cells.Editors.TextBox(type);
				editor = editTextBox;
			} else //if no editor no edit support
				editor = null;
		} else {
			//UITypeEditor supported

			var txtBoxUITypeEditor = new Cells.Editors.TextBoxUITypeEditor(type);
			txtBoxUITypeEditor.Control.UITypeEditor = uiTypeEditor;
			editor = txtBoxUITypeEditor;
		}

		if (editor != null) {
			editor.DefaultValue = defaultValue;
			editor.AllowNull = allowNull;
			//l_Editor.CellType = p_Type;
			editor.StandardValues = standardValues;
			editor.StandardValuesExclusive = standardValueExclusive;
			editor.TypeConverter = typeConverter;
		}

		return editor;
	}
}
