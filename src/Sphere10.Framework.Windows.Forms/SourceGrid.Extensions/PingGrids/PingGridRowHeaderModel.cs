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

public class PingGridRowHeaderModel : IValueModel {
	public PingGridRowHeaderModel() {
	}

	// IValueModel Members
	public object GetValue(CellContext cellContext) {
		var dataGrid = (DataGrid)cellContext.Grid;
		if (dataGrid.DataSource != null && dataGrid.DataSource.AllowNew && cellContext.Position.Row == (dataGrid.Rows.Count - 1))
			return "*";
		return null;
	}

	public void SetValue(CellContext cellContext, object value) {
		throw new ApplicationException("Not supported");
	}
}
