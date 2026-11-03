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
/// EditorNumericUpDown editor class.
/// </summary>
[System.ComponentModel.ToolboxItem(false)]
public class NumericUpDown : EditorControlBase {
	/// <summary>
	/// Create a model of type Decimal
	/// </summary>
	public NumericUpDown()
		: base(typeof(decimal)) {
	}

	public NumericUpDown(Type cellType, decimal maximum, decimal minimum, decimal increment)
		: base(cellType) {
		if (cellType == null || cellType == typeof(int) ||
			cellType == typeof(long) || cellType == typeof(decimal)) {
			Control.Maximum = maximum;
			Control.Minimum = minimum;
			Control.Increment = increment;
		} else
			throw new SourceGridException("Invalid CellType expected long, int or decimal");
	}

	/// <summary>
	/// Gets the control used for editing the cell.
	/// </summary>
	public new System.Windows.Forms.NumericUpDown Control {
		get {
			return (System.Windows.Forms.NumericUpDown)base.Control;
		}
	}

	/// <summary>
	/// Set the specified value in the current editor control.
	/// </summary>
	/// <param name="editValue"></param>
	public override void SetEditValue(object editValue) {
		decimal dec;
		if (editValue is decimal)
			dec = (decimal)editValue;
		else if (editValue is long)
			dec = (decimal)((long)editValue);
		else if (editValue is int)
			dec = (decimal)((int)editValue);
		else if (editValue == null)
			dec = Control.Minimum;
		else
			throw new SourceGridException("Invalid value, expected Decimal, Int or Long");

		//.NET BUG:  First I must get the value, otherwise seems that the control don't work properly (when I hit the Escape so I never getthe value so the control use always the previous value also if I manually set the value)
		var oldValue = Control.Value;

		Control.Value = dec;
	}

	/// <summary>
	/// Returns the value inserted with the current editor control
	/// </summary>
	/// <returns></returns>
	public override object GetEditedValue() {
		if (ValueType == null)
			return Control.Value;
		if (ValueType == typeof(decimal))
			return Control.Value;
		if (ValueType == typeof(int))
			return (int)Control.Value;
		if (ValueType == typeof(long))
			return (long)Control.Value;

		throw new SourceGridException("Invalid type of the cell expected decimal, long or int");
	}

	// Edit Control
	/// <summary>
	/// Create the editor control
	/// </summary>
	/// <returns></returns>
	protected override Control CreateControl() {
		var control = new System.Windows.Forms.NumericUpDown();
		control.BorderStyle = System.Windows.Forms.BorderStyle.None;
		return control;
	}

	protected override void OnSendCharToEditor(char key) {
		//No implementation
	}
}
