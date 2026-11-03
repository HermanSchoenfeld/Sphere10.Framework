// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// Represents a range that contains only fixed cols
/// </summary>
public class RangeFixedCols : IRangeLoader {
	/// <summary>
	/// Constructor
	/// </summary>
	public RangeFixedCols() {
	}

	/// <summary>
	/// Returns the Range struct from the specific instance
	/// </summary>
	/// <param name="grid"></param>
	/// <returns></returns>
	public CellRange GetRange(GridVirtual grid) {
		if (grid.Columns.Count >= grid.FixedColumns)
			return new CellRange(0, 0, grid.Rows.Count - 1, grid.FixedColumns);
		return CellRange.Empty;
	}
}
