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
/// Position type of the cell. Look at the .vsd diagram for details.
/// </summary>
public enum CellPositionType {
	/// <summary>
	/// Empty Cell
	/// </summary>
	Empty = 0,

	/// <summary>
	/// Fixed Top+Left Cell
	/// </summary>
	FixedTopLeft = 1,

	/// <summary>
	/// Fixed Top Cell
	/// </summary>
	FixedTop = 2,

	/// <summary>
	/// Fixed Left cell
	/// </summary>
	FixedLeft = 3,

	/// <summary>
	/// Scrollable Cell
	/// </summary>
	Scrollable = 4
}
