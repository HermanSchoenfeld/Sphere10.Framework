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
/// A model that contains the value of cell. Usually used for a Real Cell or cells with a static text.
/// </summary>
public class ValueModel : IValueModel {
	private object _value;

	public ValueModel() {
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="val">value to set for this model</param>
	public ValueModel(object val) {
		_value = val;
	}

	// IModel Members
	public object GetValue(CellContext cellContext) {
		return _value;
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="cellContext"></param>
	/// <param name="newValue">new value of this model</param>
	public void SetValue(CellContext cellContext, object newValue) {
		if (IsNewValueEqual(newValue) == true)
			return;
		var valArgs = new ValueChangeEventArgs(_value, newValue);
		if (cellContext.Grid != null)
			cellContext.Grid.Controller.OnValueChanging(cellContext, valArgs);
		_value = valArgs.NewValue;
		if (cellContext.Grid != null)
			cellContext.Grid.Controller.OnValueChanged(cellContext, EventArgs.Empty);
	}

	public bool IsNewValueEqual(object newValue) {
		if (newValue == _value)
			return true;
		var valueModel = _value;
		if (valueModel == null)
			valueModel = string.Empty;
		if (newValue == null)
			newValue = string.Empty;

		return newValue.Equals(valueModel);
	}
}
