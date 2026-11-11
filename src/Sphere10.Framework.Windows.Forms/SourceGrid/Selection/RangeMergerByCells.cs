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
/// Similar to <see cref="RangeMergerByRows"/>, this class merge adjancent
/// ranges if they are differ only by row or column count.
///
/// See TestRangeMergerByCells for unit tests, which show how this class works
/// </summary>
public class RangeMergerByCells {
	private List<CellRange> _ranges = new List<CellRange>();

	public RangeMergerByCells AddRange(CellRange rangeToAdd) {
		var rangeRegionToAdd = IntersectWithCurrentRanges(new RangeRegion(rangeToAdd));
		foreach (var range in rangeRegionToAdd)
			_ranges.Add(range);

		JoinAdjanced();
		return this;
	}

	public List<CellRange> GetSelectedRowRegions() => _ranges;

	private RangeRegion IntersectWithCurrentRanges(RangeRegion rangeRegionToIntersect) {
		while (IntersectWithCurrentRangesRecursive(rangeRegionToIntersect)) {
		}
		return rangeRegionToIntersect;
	}

	private bool IntersectWithCurrentRangesRecursive(RangeRegion rangeRegionToIntersect) {
		// check against each range
		foreach (var range in _ranges) {
			RangeRegion excludedResults = null;
			var remove = CellRange.Empty;
			var excluded = false;
			// loop through our new ranges
			foreach (var rangeToIntersect in rangeRegionToIntersect) {
				// if we intersect with at least one range
				// get the exclusion range.
				// mark a flag, that we need to remove our
				// range, and add an excluded range
				if (range.IntersectsWith(rangeToIntersect)) {
					excludedResults = rangeToIntersect.Exclude(range);
					excluded = true;
					remove = rangeToIntersect;
					break;
				}
			}
			if (excluded) {
				rangeRegionToIntersect.Remove(remove);
				rangeRegionToIntersect.Add(excludedResults);
				return true;
			}
		}
		return false;
	}

	private void JoinAdjanced() {
		while (JoinAdjancedRecursive()) {
		}
	}

	/// <summary>
	/// Repeat this function until it returns false to recursively merge all ranges
	/// </summary>
	/// <returns>true, if at least two ranges were joined into single</returns>
	private bool JoinAdjancedRecursive() {
		var cloneRanges = new List<CellRange>(_ranges);
		foreach (var clonedRange in cloneRanges) {
			foreach (var rangeToTest in _ranges) {
				if (rangeToTest.Equals(clonedRange))
					continue;
				if ((rangeToTest.Start.Row == clonedRange.Start.Row) &&
					(rangeToTest.End.Row == clonedRange.End.Row)) {
					if (Math.Abs(rangeToTest.Start.Column - clonedRange.End.Column) == 1) {
						Join(rangeToTest, clonedRange);
						return true;
					}
				}

				if ((rangeToTest.Start.Column == clonedRange.Start.Column) &&
					(rangeToTest.End.Column == clonedRange.End.Column)) {
					if (Math.Abs(rangeToTest.Start.Row - clonedRange.End.Row) == 1) {
						Join(rangeToTest, clonedRange);
						return true;
					}
				}
			}
		}
		return false;
	}

	private void Join(CellRange range1, CellRange range2) {
		var result = CellRange.GetBounds(range1, range2);
		_ranges.Remove(range1);
		_ranges.Remove(range2);
		_ranges.Add(result);
	}
}
