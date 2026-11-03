// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Models;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

// Models
public class ArrayValueModel : IValueModel {
	// IValueModel Members
	public virtual object GetValue(CellContext cellContext) {
		var array = ((ArrayGrid)cellContext.Grid).DataSource;
		return array.GetValue(cellContext.Position.Row - cellContext.Grid.FixedRows, cellContext.Position.Column - cellContext.Grid.FixedColumns);
	}

	public virtual void SetValue(CellContext cellContext, object value) {
		var array = ((ArrayGrid)cellContext.Grid).DataSource;
		var oldValue = array.GetValue(cellContext.Position.Row - cellContext.Grid.FixedRows, cellContext.Position.Column - cellContext.Grid.FixedColumns);
		var changeArgs = new ValueChangeEventArgs(oldValue, value);
		if (cellContext.Grid != null)
			cellContext.Grid.Controller.OnValueChanging(cellContext, changeArgs);

		array.SetValue(value, cellContext.Position.Row - cellContext.Grid.FixedRows, cellContext.Position.Column - cellContext.Grid.FixedColumns);

		if (cellContext.Grid != null)
			cellContext.Grid.Controller.OnValueChanged(cellContext, EventArgs.Empty);
	}
}
