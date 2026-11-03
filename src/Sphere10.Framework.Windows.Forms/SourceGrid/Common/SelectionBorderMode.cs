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
/// SelectionBorderMode. Used with Grid.Selection.BorderMode property
/// </summary>
public enum SelectionBorderMode {
	/// <summary>
	/// Don't draw a border around each selection range
	/// </summary>
	None = 0,

	/// <summary>
	/// Draw a border around the range that contains the focus.
	/// </summary>
	FocusRange = 1,

	/// <summary>
	/// Draw a border around the focusl cell
	/// </summary>
	FocusCell = 2,

	/// <summary>
	/// Draw a border around the selection range only is there is only one range selected
	/// </summary>
	UniqueRange = 3,

	/// <summary>
	/// Like the UniqueRange enum but when there is more then one range set the range only on the FocusCell
	/// </summary>
	Auto = 4
}
