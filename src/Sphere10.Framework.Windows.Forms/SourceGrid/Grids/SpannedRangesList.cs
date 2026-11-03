// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// A is simple List of Ranges.
/// Uses simple iterating over list to find
/// required range
/// </summary>
public class SpannedRangesList : List<CellRange>, ISpannedRangesCollection {
	public void Update(CellRange oldRange, CellRange newRange) {
		var index = base.IndexOf(oldRange);
		if (index < 0)
			throw new RangeNotFoundException();
		this[index] = newRange;
	}

	public void Redim(int rowCount, int colCount) {
		// just do nothing, nothing needed
	}

	public new void Remove(CellRange range) {
		var index = base.IndexOf(range);
		if (index < 0)
			throw new RangeNotFoundException();
		base.RemoveAt(index);
	}

	/// <summary>
	/// Returns first intersecting region
	/// </summary>
	/// <param name="pos"></param>
	public CellRange? GetFirstIntersectedRange(Position pos) {
		for (var index = 0; index < this.Count; index++) {
			var range = this[index];
			if (range.Contains(pos))
				return range;
		}
		return null;
	}

	public List<CellRange> GetRanges(CellRange range) {
		var result = new List<CellRange>();
		for (var index = 0; index < this.Count; index++) {
			var r = this[index];
			if (r.Contains(range))
				result.Add(r);
		}
		return result;
	}

	public CellRange? FindRangeWithStart(Position start) {
		for (var index = 0; index < this.Count; index++) {
			var range = this[index];
			if (range.Start.Equals(start))
				return range;
		}
		return null;
	}
}
