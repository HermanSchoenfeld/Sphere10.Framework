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

public class SortableHeader : ISortableHeader {
	private SortStatus _sortStatus = new SortStatus(Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle.None, null);

	public SortStatus SortStatus {
		get => _sortStatus;
		set => _sortStatus = value;
	}

	// ISortableHeader Members
	public SortStatus GetSortStatus(CellContext cellContext) => _sortStatus;

	public void SetSortMode(CellContext cellContext, Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle style) => _sortStatus.Style = style;
}
