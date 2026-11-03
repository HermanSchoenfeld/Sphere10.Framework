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
/// ContextMenuStyle (Flags)
/// </summary>
[Flags]
public enum ContextMenuStyle {
	None = 0,
	ColumnResize = 1,
	RowResize = 2,
	AutoSize = 4,
	ClearSelection = 8,
	CopyPasteSelection = 16,
	CellContextMenu = 32
}
