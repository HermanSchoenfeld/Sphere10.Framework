// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Dev Age
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Models;

/// <summary>
/// Interface for informations about a cechkbox
/// </summary>
public interface ICheckBox : IModel {
	/// <summary>
	/// Get the status of the checkbox at the current position
	/// </summary>
	/// <param name="cellContext"></param>
	/// <returns></returns>
	CheckBoxStatus GetCheckBoxStatus(CellContext cellContext);

	/// <summary>
	/// Set the checked value
	/// </summary>
	/// <param name="cellContext"></param>
	/// <param name="pChecked">True, False or Null</param>
	void SetCheckedValue(CellContext cellContext, bool? pChecked);
}


/// <summary>
/// Status of the CheckBox
/// </summary>
public struct CheckBoxStatus {
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="checkEnable"></param>
	/// <param name="bChecked"></param>
	/// <param name="caption"></param>
	public CheckBoxStatus(bool checkEnable, bool? bChecked, string caption) {

		CheckEnable = checkEnable;
		Caption = caption;

		mCheckState = Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.CheckBoxState.Undefined;
		Checked = bChecked;
	}
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="checkEnable"></param>
	/// <param name="checkState"></param>
	/// <param name="caption"></param>
	public CheckBoxStatus(bool checkEnable, Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.CheckBoxState checkState, string caption) {
		CheckEnable = checkEnable;
		mCheckState = checkState;
		Caption = caption;
	}

	private Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.CheckBoxState mCheckState;

	/// <summary>
	/// Gets or sets the state of the check box.
	/// </summary>
	public Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.CheckBoxState CheckState {
		get { return mCheckState; }
		set { mCheckState = value; }
	}

	/// <summary>
	/// Enable or disable the checkbox
	/// </summary>
	public bool CheckEnable;

	/// <summary>
	/// Gets or set Checked status. Return true for Checked, false for Uncheck and null for Undefined
	/// </summary>
	public bool? Checked {
		get {
			if (CheckState == Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.CheckBoxState.Checked)
				return true;
			else if (CheckState == Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.CheckBoxState.Unchecked)
				return false;
			else
				return null;
		}
		set {
			if (value == null)
				CheckState = Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.CheckBoxState.Undefined;
			else if (value.Value)
				CheckState = Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.CheckBoxState.Checked;
			else
				CheckState = Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.CheckBoxState.Unchecked;
		}
	}

	/// <summary>
	/// Caption of the CheckBox
	/// </summary>
	public string Caption;
}

