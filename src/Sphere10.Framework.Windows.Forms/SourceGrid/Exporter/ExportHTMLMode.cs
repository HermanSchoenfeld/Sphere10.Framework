// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Exporter;

/// <summary>
/// Flags for the export html features (Flags)
/// </summary>
[Flags]
public enum ExportHTMLMode {
	None = 0,
	HTMLAndBody = 1,
	GridBackColor = 2,
	CellBackColor = 4,
	RectangleBorder = 8,
	CellForeColor = 16,
	CellImages = 32,
	Default = (HTMLAndBody | GridBackColor | CellBackColor | RectangleBorder | CellForeColor | CellImages)
}
