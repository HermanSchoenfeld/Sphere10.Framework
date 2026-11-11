// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Models;

/// <summary>
/// Status of the CheckBox
/// </summary>
public struct CheckBoxStatus {
	private Sphere10.Framework.Windows.Forms.Drawing.CheckBoxState _checkState;

	/// <summary>
	/// Enable or disable the checkbox
	/// </summary>
	public bool CheckEnable;

	/// <summary>
	/// Caption of the CheckBox
	/// </summary>
	public string Caption;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="checkEnable"></param>
	/// <param name="bChecked"></param>
	/// <param name="caption"></param>
	public CheckBoxStatus(bool checkEnable, bool? bChecked, string caption) {

		CheckEnable = checkEnable;
		Caption = caption;

		_checkState = Sphere10.Framework.Windows.Forms.Drawing.CheckBoxState.Undefined;
		Checked = bChecked;
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="checkEnable"></param>
	/// <param name="checkState"></param>
	/// <param name="caption"></param>
	public CheckBoxStatus(bool checkEnable, Sphere10.Framework.Windows.Forms.Drawing.CheckBoxState checkState, string caption) {
		CheckEnable = checkEnable;
		_checkState = checkState;
		Caption = caption;
	}

	/// <summary>
	/// Gets or sets the state of the check box.
	/// </summary>
	public Sphere10.Framework.Windows.Forms.Drawing.CheckBoxState CheckState {
		get => _checkState;
		set => _checkState = value;
	}

	/// <summary>
	/// Gets or set Checked status. Return true for Checked, false for Uncheck and null for Undefined
	/// </summary>
	public bool? Checked {
		get {
			if (CheckState == Sphere10.Framework.Windows.Forms.Drawing.CheckBoxState.Checked)
				return true;
			if (CheckState == Sphere10.Framework.Windows.Forms.Drawing.CheckBoxState.Unchecked)
				return false;
			return null;
		}
		set {
			if (value == null)
				CheckState = Sphere10.Framework.Windows.Forms.Drawing.CheckBoxState.Undefined;
			else if (value.Value)
				CheckState = Sphere10.Framework.Windows.Forms.Drawing.CheckBoxState.Checked;
			else
				CheckState = Sphere10.Framework.Windows.Forms.Drawing.CheckBoxState.Unchecked;
		}
	}
}
