// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Models;

/// <summary>
/// CheckBox model.
/// </summary>
public class CheckBox : ICheckBox {
	private string _caption = null;

	public string Caption {
		get => _caption;
		set => _caption = value;
	}

	// ICheckBox Members
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="cellContext"></param>
	/// <returns></returns>
	public CheckBoxStatus GetCheckBoxStatus(CellContext cellContext) {
		var enableEdit = false;
		if (cellContext.Cell.Editor != null && cellContext.Cell.Editor.EnableEdit)
			enableEdit = true;

		var val = cellContext.Cell.Model.ValueModel.GetValue(cellContext);
		if (val == null)
			return new CheckBoxStatus(enableEdit, Sphere10.Framework.Windows.Forms.Drawing.CheckBoxState.Undefined, _caption);
		if (val is bool)
			return new CheckBoxStatus(enableEdit, (bool)val, _caption);
		throw new SourceGridException("Cell value not supported for this cell. Expected bool value or null.");
	}

	/// <summary>
	/// Set the checked value
	/// </summary>
	/// <param name="cellContext"></param>
	/// <param name="isChecked"></param>
	public void SetCheckedValue(CellContext cellContext, bool? isChecked) {
		if (cellContext.Cell.Editor != null && cellContext.Cell.Editor.EnableEdit)
			cellContext.Cell.Editor.SetCellValue(cellContext, isChecked);
	}
}
