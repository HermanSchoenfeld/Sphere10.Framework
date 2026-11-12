// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Models;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Extensions.PingGrids;

/// <summary>
/// A Model of type IValueModel used for binding the value to a specified property of the bound object.
/// Used for the DataGrid control.
/// </summary>
public class PingGridValueModel : IValueModel {
	/// <summary>
	/// Constructor
	/// </summary>
	public PingGridValueModel() {
	}

	#region IValueModel Members

	public object GetValue(CellContext cellContext) {
		// Resolve the source row and property represented by this cell.
		var grid = cellContext.Grid as PingGrid;
		var propertyName = grid.Columns[cellContext.Position.Column].PropertyName;
		var dataIndex = grid.Rows.IndexToDataSourceIndex(cellContext.Position.Row);

		//Check if the row is not outside the valid range (for example to handle the new row)
		if (dataIndex >= grid.DataSource.Count)
			return null;
		return grid.DataSource.GetItemValue(dataIndex, propertyName);
	}

	public void SetValue(CellContext cellContext, object value) {
		throw new NotImplementedException();
		/*DataGrid grid = (DataGrid)cellContext.Grid;
		PropertyDescriptor prop = grid.Columns[cellContext.Position.Column].PropertyColumn;
		object oldValue = GetValue(cellContext);
		
		ValueChangeEventArgs valArgs = new ValueChangeEventArgs(oldValue, value);
		if (cellContext.Grid != null)
			cellContext.Grid.Controller.OnValueChanging(cellContext, valArgs);

		

		

		grid.DataSource.SetEditValue(prop, valArgs.NewValue);

		if (cellContext.Grid != null)
			cellContext.Grid.Controller.OnValueChanged(cellContext, EventArgs.Empty);
			*/
	}

	#endregion
}
