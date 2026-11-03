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
/// EditableMode Cell mode (Flags)
/// </summary>
[Flags]
public enum EditableMode {
	/// <summary>
	/// No edit support
	/// </summary>
	None = 0,

	/// <summary>
	/// Edit the cell with F2 key ( 1 )
	/// </summary>
	F2Key = 1,

	/// <summary>
	/// Edit the cell with a double click (2)
	/// </summary>
	DoubleClick = 2,

	/// <summary>
	/// Edit a cell with a single Key (4)
	/// </summary>
	SingleClick = 4,

	/// <summary>
	/// Edit the cell pressing any keys (8 + F2Key)
	/// </summary>
	AnyKey = 9,

	/// <summary>
	/// Edit the cell when it receive the focus (16)
	/// </summary>
	Focus = 16,

	/// <summary>
	/// DoubleClick + F2Key
	/// </summary>
	Default = DoubleClick | F2Key | AnyKey
}
