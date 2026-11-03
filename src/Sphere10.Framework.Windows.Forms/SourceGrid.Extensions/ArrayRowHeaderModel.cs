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

public class ArrayRowHeaderModel : Cells.Models.IValueModel {
	public ArrayRowHeaderModel() {
	}

	// IValueModel Members
	public virtual object GetValue(CellContext cellContext) {
		return cellContext.Position.Row - cellContext.Grid.FixedRows;
	}

	public virtual void SetValue(CellContext cellContext, object value) {
		throw new ApplicationException("Not supported");
	}
}
