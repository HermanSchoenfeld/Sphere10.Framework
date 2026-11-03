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
/// AutoSizeMode (Flags)
/// </summary>
[Flags]
public enum AutoSizeMode {
	None = 0,

	/// <summary>
	/// Enable the AutoSize
	/// </summary>
	EnableAutoSize = 1,

	/// <summary>
	/// Enable the AutoSize only for visible view
	/// </summary>
	EnableAutoSizeView = 8 | EnableAutoSize,

	/// <summary>
	/// Enable Stretch operation
	/// </summary>
	EnableStretch = 2,

	/// <summary>
	/// If this flag is selected the Measure function returns always the minimum column/row size and don't calculate the real required size. This flag can be used to don't consider the content of a column/row
	/// </summary>
	MinimumSize = 4,

	/// <summary>
	/// Default: EnableAutoSize, EnableStretch
	/// </summary>
	Default = EnableAutoSize | EnableStretch
}
