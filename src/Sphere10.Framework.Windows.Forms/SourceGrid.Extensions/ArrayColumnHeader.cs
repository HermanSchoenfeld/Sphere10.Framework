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

// Cells
/// <summary>
/// A cell header used for the columns. Usually used in the HeaderCell property of a DataGridColumn.
/// </summary>
public class ArrayColumnHeader : Cells.Virtual.ColumnHeader {
	public ArrayColumnHeader() {
		Model.AddModel(new ArrayColumnHeaderModel());
		AutomaticSortEnabled = false;
		ResizeEnabled = false;
		//            ColumnSelectorEnabled = true;
		//            ColumnFocusEnabled = true;
	}
}
