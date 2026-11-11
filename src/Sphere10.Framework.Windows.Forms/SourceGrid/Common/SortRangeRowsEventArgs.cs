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
	private int _keyColumn;
	private bool _ascending;
	private IComparer _cellComparer;

	public SortRangeRowsEventArgs(CellRange range, int keyColumn, bool ascending, IComparer cellComparer) {
		_range = range;
		_keyColumn = keyColumn;
		_ascending = ascending;
		_cellComparer = cellComparer;
	}

	public CellRange Range => _range;

	public int KeyColumn => _keyColumn;

	public bool Ascending => _ascending;

	public IComparer CellComparer => _cellComparer;
}
