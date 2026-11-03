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
/// Special keys that the grid can handle. You can change this enum to block or allow some special keys function. (Flags)
/// </summary>
[Flags]
public enum GridSpecialKeys {
	/// <summary>
	/// No keys
	/// </summary>
	None = 0,

	//		/// <summary>
	//		/// Ctrl+C for Copy selection operation
	//		/// </summary>
	//		Ctrl_C = 1,
	//		/// <summary>
	//		/// Ctrl+V for paste selection operation
	//		/// </summary>
	//		Ctrl_V = 2,
	//		/// <summary>
	//		/// Ctrl+X for cut selection operation
	//		/// </summary>
	//		Ctrl_X = 4,
	//		/// <summary>
	//		/// Delete key, for Clear selection operation
	//		/// </summary>
	//		Delete = 8,
	/// <summary>
	/// Arrows keys, for moving focus cell operation
	/// </summary>
	Arrows = 16,

	/// <summary>
	/// Tab and Shift+Tab keys, for moving focus cell operation
	/// </summary>
	Tab = 32,

	/// <summary>
	/// PageDown and PageUp keys, for page operation
	/// </summary>
	PageDownUp = 64,

	/// <summary>
	/// Enter key, for apply editing operation
	/// </summary>
	Enter = 128,

	/// <summary>
	/// Escape key, for cancel editing operation
	/// </summary>
	Escape = 256,

	/// <summary>
	/// Control key, for selection operations. Enables the selection of non adjacent cells
	/// </summary>
	Control = 512,

	/// <summary>
	/// Shift key, for selection operations. Enables the selection of the range from the focused cell to the selected cells.
	/// </summary>
	Shift = 1024,

	/// <summary>
	/// Default: Arrows|Tab|PageDownUp|Enter|Escape|Control|Shift
	/// </summary>
	Default = Arrows | Tab | PageDownUp | Enter | Escape | Control | Shift
}
