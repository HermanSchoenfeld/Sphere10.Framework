// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

public class SortRangeRowsEventArgs : EventArgs {
	private CellRange _range;

	public SortRangeRowsEventArgs(CellRange range, int keyColumn, bool ascending, IComparer cellComparer) {
		_range = range;
		KeyColumn = keyColumn;
		Ascending = ascending;
		CellComparer = cellComparer;
	}

	public CellRange Range => _range;

	public int KeyColumn { get; }

	public bool Ascending { get; }

	public IComparer CellComparer { get; }
}
