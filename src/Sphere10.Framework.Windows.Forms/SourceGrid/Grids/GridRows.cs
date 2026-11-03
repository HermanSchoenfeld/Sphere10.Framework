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

public class GridRows : RowInfoCollection {
	public GridRows(Grid grid)
		: base(grid) {
	}

	public new Grid Grid {
		get {
			return base.Grid as Grid;
		}
	}

	public new GridRow this[int index] {
		get {
			return (GridRow)base[index];
		}
	}

	public override void Swap(int rowIndex1, int rowIndex2) {
		base.Swap(rowIndex1, rowIndex2);
		this.Grid.SpannedCellReferences.Swap(rowIndex1, rowIndex2);
	}

	/// <summary>
	/// Insert a row at the specified position
	/// </summary>
	/// <param name="index"></param>
	public void Insert(int index) {
		InsertRange(index, 1);
	}

	public void SetCount(int value) {
		this.Grid.GrowGrid();
		if (Count < value)
			InsertRange(Count, value - Count);
		else if (Count > value)
			RemoveRange(value, Count - value);
	}

	/// <summary>
	/// Insert the specified number of rows at the specified position
	/// </summary>
	public void InsertRange(int startIndex, int count) {
		var rows = new RowInfo[count];
		for (var index = 0; index < rows.Length; index++)
			rows[index] = CreateRow();

		base.InsertRange(startIndex, rows);
		// Ensure that grid grows when rows are inserted
		this.Grid.GrowGrid();

		this.Grid.SpannedCellReferences.MoveDownSpannedRanges(startIndex, count);
		this.Grid.SpannedCellReferences.ExpandSpannedRows(startIndex, count);
	}

	public override void RemoveRange(int startIndex, int count) {
		this.Grid.SpannedCellReferences.RemoveSpannedCellReferencesInRows(startIndex, count);
		base.RemoveRange(startIndex, count);
		this.Grid.SpannedCellReferences.ShrinkOrRemoveSpannedRows(startIndex, count);
		this.Grid.SpannedCellReferences.MoveUpSpannedRanges(startIndex, count);
	}

	protected GridRow CreateRow() {
		return new GridRow((Grid)Grid);
	}
}
