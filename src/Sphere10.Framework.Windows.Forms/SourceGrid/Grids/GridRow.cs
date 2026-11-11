// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

public class GridRow : RowInfo {
	private Dictionary<GridColumn, Cells.ICell> _cells = new Dictionary<GridColumn, Cells.ICell>();

	public GridRow(Grid grid)
		: base(grid) {
	}

	public Cells.ICell this[GridColumn column] {
		get {
			Cells.ICell cell;
			if (_cells.TryGetValue(column, out cell))
				return cell;
			return null;
		}
		set => _cells[column] = value;
	}
}
