// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Models;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A retained SourceGrid value model with a silent accepted-snapshot update.</summary>
internal sealed class CrudGridPreparedValueModel : IValueModel {
	private object _value;

	public object GetValue(CellContext cellContext) => _value;

	public void SetValue(CellContext cellContext, object value) {
		// Preserve SourceGrid's ordinary null/empty equality and external value-change callbacks.
		if (ReferenceEquals(value, _value) || (value ?? string.Empty).Equals(_value ?? string.Empty))
			return;
		var change = new ValueChangeEventArgs(_value, value);
		if (cellContext.Grid != null)
			cellContext.Grid.Controller.OnValueChanging(cellContext, change);
		_value = change.NewValue;
		if (cellContext.Grid != null)
			cellContext.Grid.Controller.OnValueChanged(cellContext, EventArgs.Empty);
	}

	public void ReplaceValue(object value) => _value = value;
}
