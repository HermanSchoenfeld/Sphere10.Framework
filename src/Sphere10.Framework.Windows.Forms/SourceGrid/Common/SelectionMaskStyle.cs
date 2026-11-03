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
/// SelectionmaskStyle, used to customize the visual style of the selection mask. (Flags)
/// </summary>
[Flags]
public enum SelectionMaskStyle {
	/// <summary>
	/// None. No special flags
	/// </summary>
	None = 0,

	/// <summary>
	/// Used to draw only initialized cells. If you have uninitialized cell (you don't create a cell for a specific position: grid[0,0] = null;) the selection mask is not drawed on this cell. Consider that with this flags the drawing method is more complex and slow.If you use this flag the border can only be set to FocusCell or None.
	/// </summary>
	DrawOnlyInitializedCells = 1,

	/// <summary>
	/// Used to draw the selection over the the cells, usually used with a transparent backcolor drawed over the normal cell. If not set, each cells (View) draw the selection or focus backcolor inside the normal drawing code.
	/// </summary>
	DrawSeletionOverCells = 2,

	/// <summary>
	/// Default value: None
	/// </summary>
	Default = DrawSeletionOverCells
}
