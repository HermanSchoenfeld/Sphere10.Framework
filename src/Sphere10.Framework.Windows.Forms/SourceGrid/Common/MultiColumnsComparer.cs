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
using System.Runtime.Serialization;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// A comparer used to sort more than one columns.
/// </summary>
public class MultiColumnsComparer : IComparer {
	private IComparer _defaultComparer = new SourceGrid.ValueCellComparer();
	private int[] _secondarySortColumns;

	public MultiColumnsComparer(params int[] secondarySortColumns) {
		_secondarySortColumns = secondarySortColumns;
	}

	public virtual System.Int32 Compare(System.Object x, System.Object y) {
		var result = _defaultComparer.Compare(x, y);
		if (result == 0) {
			var grid = ((SourceGrid.Cells.ICell)x).Grid;
			var rowX = ((SourceGrid.Cells.ICell)x).Range.Start.Row;
			var rowY = ((SourceGrid.Cells.ICell)y).Range.Start.Row;

			var indexOrder = 0;
			while (indexOrder < _secondarySortColumns.Length) {
				var otherX = grid.GetCell(rowX, _secondarySortColumns[indexOrder]);
				var otherY = grid.GetCell(rowY, _secondarySortColumns[indexOrder]);

				var subRet = _defaultComparer.Compare(otherX, otherY);
				if (subRet != 0)
					return subRet;

				indexOrder++;
			}

			return 0;
		}
		return result;
	}
}
