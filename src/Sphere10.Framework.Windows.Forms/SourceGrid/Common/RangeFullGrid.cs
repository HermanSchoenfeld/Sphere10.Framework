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
/// Represents a range that contains all the grid
/// </summary>
public class RangeFullGrid : IRangeLoader {
	/// <summary>
	/// Constructor
	/// </summary>
	public RangeFullGrid() {
	}

	/// <summary>
	/// Returns the Range struct from the specific instance
	/// </summary>
	/// <param name="grid"></param>
	/// <returns></returns>
	public CellRange GetRange(GridVirtual grid) => grid.CompleteRange;
}
