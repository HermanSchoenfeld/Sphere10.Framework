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
/// FocusStyle (Flags). Used to customize the style of the focus.
/// </summary>
[Flags]
public enum FocusStyle {
	None = 0,

	/// <summary>
	/// Remove the focus cell when the grid lost the focus
	/// </summary>
	RemoveFocusCellOnLeave = 1,

	/// <summary>
	/// Remove the selection when the grid lost the focus
	/// </summary>
	RemoveSelectionOnLeave = 2,

	/// <summary>
	/// Set the focus on the first cell when the grid receive the focus and there isnt' an active cell. Use the FocusFirstCell method.
	/// </summary>
	FocusFirstCellOnEnter = 4,

	/// <summary>
	/// The default value for this flags: FocusStyle.FocusFirstCellOnEnter | FocusStyle.RemoveFocusCellOnLeave
	/// </summary>
	Default = FocusStyle.FocusFirstCellOnEnter | FocusStyle.RemoveFocusCellOnLeave
}
