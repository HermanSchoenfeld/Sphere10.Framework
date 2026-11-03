// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Selection;

/// <summary>
/// Contains a list of ranges which differ only by row, that is vertically.
/// This the reason why the class contains a word "row" in its name.
///
/// "Merger" means that it will merge adjancent ranges into single big range.
/// So, for example, if you have 100 ranges from first row to second, that
/// equals to one big range from row 1 to 100. If Row 50 is not selected,
/// then this class splits one big range into two smaller. One from first row
/// to row number 49, and anoter range from row 51 to row 100
///
/// Although some functions work with Range structure, which has also a horizontal
/// span, that information is actually not needed, and used as dummy only.
///
///
/// </summary>
public class RangeMergerByRows {
	private List<CellRange> _ranges = new List<CellRange>();
	private int _columnStart = 0;
	private int _columnEnd = 0;

	public RangeMergerByRows() {
	}

	/// <summary>
	/// Loops via all ranges. Ranges are guaranteed to be ordered
	/// from lowest row to highest.
	/// </summary>
	/// <returns></returns>
	public IEnumerable<CellRange> LoopAllRanges() {
		foreach (var row in _ranges) {
			yield return row;
		}
	}

	/// <summary>
	/// Returns a list of selected ranges
	/// </summary>
	/// <param name="startColumn">start column, usually 0</param>
	/// <param name="endColumn">end column, usually equals to column count - 1</param>
	/// <returns></returns>
	public List<CellRange> GetSelectedRowRegions(int startColumn, int endColumn) {
		if (startColumn > endColumn)
			throw new ArgumentException("end column can not be less than startColumn");
		var ranges = new List<CellRange>();
		foreach (var range in _ranges)
			ranges.Add(new CellRange(range.Start.Row, startColumn, range.End.Row, endColumn));

		return ranges;
	}

	/// <summary>
	/// Add a range to collection. If the range can be added (merged)
	/// to existing range, it will be added so. This will guarantee
	/// that the number of different ranges is kept to minimal.
	/// In theory only if user selects every second row, this would produce
	/// RowCount / 2 number of ranges. In practice, there are rarely
	/// more than 3 - 5 different selection regions
	/// </summary>
	/// <param name="rangeToAdd">columns values are ignored, so simply
	/// put 0 as start colum and 1 as end column. Only row values matter</param>
	/// <returns></returns>
	public RangeMergerByRows AddRange(CellRange rangeToAdd) {
		rangeToAdd = NormalizeRange(rangeToAdd);
		Merge(rangeToAdd);
		JoinAdjanced();
		return this;
	}

	/// <summary>
	/// As opposed to Adding a range, this will remove a range.
	/// It might happend that removing a range is dividing one bigger range into two
	/// smaller ones. So practically the number of ranges might increase.
	///
	/// It might be that this method might carry more exact meaning with "Exclude", instead
	/// of "RemoveRange"
	/// </summary>
	/// <param name="rangeToRemove"></param>
	/// <returns></returns>
	public RangeMergerByRows RemoveRange(CellRange rangeToRemove) {
		while (RemoveRangeRecursive(rangeToRemove)) {
		}
		return this;
	}

	/// <summary>
	/// Returns a list of indexes of rows
	/// </summary>
	/// <returns></returns>
	public IList<int> GetRowsIndex() {
		var rowIndex = new List<int>();
		foreach (var range in _ranges) {
			for (var r = range.Start.Row; r <= range.End.Row; r++)
				rowIndex.Add(r);
		}
		return rowIndex;
	}

	public bool IsEmpty() {
		return _ranges.Count == 0;
	}

	public bool IsSelectedRow(int rowIndex) {
		var p = new Position(rowIndex, 0);
		foreach (var range in _ranges) {
			if (range.Contains(p) == true)
				return true;
		}
		return false;
	}

	public RangeMergerByRows Clear() {
		_ranges.Clear();
		return this;
	}

	/// <summary>
	/// </summary>
	/// <param name="rangeToMerge"></param>
	/// <returns></returns>
	private void Merge(CellRange rangeToMerge) {
		while (true) {
			rangeToMerge = MergeRecursive(rangeToMerge);
			if (rangeToMerge == CellRange.Empty)
				return;
		}
	}

	/// <summary>
	/// Merges given range with one of the intersecting range in m_ranges
	/// if no intersecting ranges is found, then rangeToMerge is
	/// added to m_ranges
	/// </summary>
	/// <param name="rangeToMerge"></param>
	/// <returns></returns>
	private CellRange MergeRecursive(CellRange rangeToMerge) {
		for (var index = 0; index < _ranges.Count; index++) {
			var range = _ranges[index];
			if (range.IntersectsWith(rangeToMerge)) {
				_ranges.Remove(range);
				return MergeByRow(rangeToMerge, range);
			}
		}
		_ranges.Add(rangeToMerge);
		return CellRange.Empty;
	}

	private void JoinAdjanced() {
		while (JoinAjdancedRecursive()) {
		}
	}

	/// <summary>
	/// Returns true if at least two ranges were joined
	/// </summary>
	/// <returns></returns>
	private bool JoinAjdancedRecursive() {
		SortList();
		for (var index = 0; index < _ranges.Count - 1; index++) {
			var first = _ranges[index];
			var second = _ranges[index + 1];
			if (first.End.Row + 1 >= second.Start.Row) {
				var newRange = new CellRange(first.Start.Row, _columnStart, second.End.Row, _columnEnd);
				_ranges.Remove(first);
				_ranges.Remove(second);
				_ranges.Add(newRange);
				return true;
			}
		}
		return false;
	}

	private void SortList() {
		_ranges.Sort(new RangeComparerByRows());
	}

	/// <summary>
	/// Returns new range witch is the max of both ranges in row axis
	/// </summary>
	/// <param name="first"></param>
	/// <param name="second"></param>
	/// <returns></returns>
	private CellRange MergeByRow(CellRange first, CellRange second) {
		var x = first.Start.Row;
		if (x > second.Start.Row)
			x = second.Start.Row;

		var x1 = first.End.Row;
		if (x1 < second.End.Row)
			x1 = second.End.Row;
		return new CellRange(x, _columnStart, x1, _columnEnd);
	}

	private bool RemoveRangeRecursive(CellRange rangeToRemove) {
		rangeToRemove = NormalizeRange(rangeToRemove);
		for (var index = 0; index < _ranges.Count; index++) {
			var range = _ranges[index];
			if (range.IntersectsWith(rangeToRemove) == false)
				continue;
			var intersection = range.Intersect(rangeToRemove);
			_ranges.Remove(range);
			var excludedRanges = range.Exclude(intersection);
			InternalAdd(excludedRanges);
			return true;
		}
		return false;
	}

	private void InternalAdd(RangeRegion rangeRegion) {
		foreach (var range in rangeRegion)
			_ranges.Add(range);
	}

	private CellRange NormalizeRange(CellRange rangeToNormalize) {
		return new CellRange(rangeToNormalize.Start.Row, _columnStart, rangeToNormalize.End.Row, _columnEnd);
	}
}
