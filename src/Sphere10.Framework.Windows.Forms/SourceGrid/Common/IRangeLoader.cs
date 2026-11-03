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
/// Interface that rappresent a range of the grid. (RangeGridNoFixedRows, RangeFullGridNoFixedCols, RangeFixedRows, RangeFixedCols, Range)
/// This class is used to calculate a real Range structure at runtime.
/// </summary>
public interface IRangeLoader {
	/// <summary>
	/// Rectangle that contains the range.
	/// </summary>
	CellRange GetRange(GridVirtual grid);
}
