// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using Sphere10.Framework.Windows.Forms.SourceGrid.Selection;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

public class StandardHiddenRowCoordinator : IHiddenRowCoordinator {
	protected RowsBase GridRows = null;
	protected int TotalHiddenRows = 0;

	/// <summary>
	/// This will help us track which rows are hidden, and which are not
	/// </summary>
	protected RangeMergerByRows HiddenRowRanges = new RangeMergerByRows();

	public StandardHiddenRowCoordinator(RowsBase rows) {
		this.GridRows = rows;

		rows.RowVisibilityChanged += delegate (int rowIndex, bool becameVisible) {
			var range = new CellRange(rowIndex, 0, rowIndex, 1);
			if (becameVisible)
				HiddenRowRanges.RemoveRange(range);
			else
				HiddenRowRanges.AddRange(range);
		};

		rows.RowVisibilityChanged += delegate (int rowIndex, bool becameVisible) {
			if (becameVisible == true)
				TotalHiddenRows -= 1;
			else
				TotalHiddenRows += 1;
			if (TotalHiddenRows < 0)
				throw new SourceGridException("Total hidden rows becamse less than 0. This indicates a bug");
		};
	}

	public RowsBase Rows => GridRows;

	public int GetTotalHiddenRows() => TotalHiddenRows;

	public int ConvertScrollbarValueToRowIndex(int scrollBarValue) {
		//return scrollBarValue;
		var processedAllRows = 0;
		var processedVisibleRows = 0;
		foreach (var range in HiddenRowRanges.LoopAllRanges()) {
			var deltaAllRows = range.End.Row - processedAllRows + 1;
			var deltaVisibleRows = range.Start.Row - processedAllRows;

			// break if we have enough
			if (processedVisibleRows > scrollBarValue)
				break;

			// cap current range, if we are near our needed range
			if (processedVisibleRows + deltaVisibleRows > scrollBarValue)
				deltaAllRows = deltaVisibleRows - (processedVisibleRows + deltaVisibleRows - scrollBarValue);

			processedAllRows += deltaAllRows;
			processedVisibleRows += deltaVisibleRows;
		}
		if (processedVisibleRows < scrollBarValue)
			processedAllRows += scrollBarValue - processedVisibleRows;

		return processedAllRows;
	}

	/// <summary>
	/// Returns a sequence of row indexes which are visible only.
	/// Correctly handles invisible rows.
	/// </summary>
	/// <param name="scrollBarValue">The value of the vertical scroll bar. Note that
	/// this does not directly relate to row number. If there are no hidden rows at all,
	/// then scroll bar value directly relates to row index number. However,
	/// if some rows are hidden, then this value is different.
	/// Basically, it says how many visible rows must be scrolled down</param>
	/// <param name="numberOfRowsToProduce">How many visible rows to return</param>
	/// <returns>Can return less rows than requested. This might occur
	/// if you request to return visible rows in the end of the grid,
	/// and all the rows would be hidden. In that case no indexes would be returned
	/// at all, even though specific amount of rows was requested</returns>
	public IEnumerable<int> LoopVisibleRows(int scrollBarValue, int numberOfRowsToProduce) {
		var producedRows = 0;
		var currentRow = scrollBarValue;
		if (Rows.IsRowVisible(scrollBarValue) == false) {
			var next = GetNextVisibleRow(currentRow);
			if (next == null)
				yield break;
			currentRow = next.Value;
		}

		// produce speicifc number of visible row indexes
		while (producedRows <= numberOfRowsToProduce) {
			yield return currentRow;
			producedRows++;

			var nextRow = GetNextVisibleRow(currentRow);
			if (nextRow == null)
				break;
			currentRow = nextRow.Value;
		}
	}

	private int? GetNextVisibleRow(int startFrom) {
		var index = startFrom + 1;
		while (index < Rows.Count) {
			if (Rows.IsRowVisible(index) == true)
				return index;
			index++;
		}
		return null;
	}
}
