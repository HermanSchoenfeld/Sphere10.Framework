// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Models;

public class ToolTip : IToolTipText {
	public string ToolTipText { get; set; }

	// IToolTipText Members
	public string GetToolTipText(CellContext cellContext) {
		if (string.IsNullOrEmpty(ToolTipText) && !cellContext.IsEmpty())
			return cellContext.DisplayText;
		return ToolTipText;
	}
}
