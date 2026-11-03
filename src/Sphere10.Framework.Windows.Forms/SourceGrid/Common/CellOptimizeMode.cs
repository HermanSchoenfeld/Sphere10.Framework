// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// Optimize mode used when constructing the grid control.
/// </summary>
public enum CellOptimizeMode {
	/// <summary>
	/// Optimize the grid for many rows
	/// </summary>
	ForRows = 1,

	/// <summary>
	/// Optimize the grid for many columns
	/// </summary>
	ForColumns = 2
}
