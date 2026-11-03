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

public class GridColumns : ColumnInfoCollection {
	public GridColumns(Grid grid)
		: base(grid) {
	}

	public new Grid Grid => base.Grid as Grid;

	/// <summary>
	/// Insert a column at the specified position
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
	/// Insert the specified number of Columns at the specified position
	/// </summary>
	public void InsertRange(int startIndex, int count) {
		var columns = new GridColumn[count];
		for (var index = 0; index < columns.Length; index++)
			columns[index] = CreateColumn();

		InsertRange(startIndex, columns);
		this.Grid.SpannedCellReferences.MoveRightSpannedRanges(startIndex, count);
		this.Grid.SpannedCellReferences.ExpandSpannedColumns(startIndex, count);
	}

	public override void RemoveRange(int startIndex, int count) {
		this.Grid.SpannedCellReferences.RemoveSpannedCellReferencesInColumns(startIndex, count);
		base.RemoveRange(startIndex, count);
		this.Grid.SpannedCellReferences.ShrinkOrRemoveSpannedColumns(startIndex, count);
		this.Grid.SpannedCellReferences.MoveLeftSpannedRanges(startIndex, count);
	}

	protected GridColumn CreateColumn() {
		return new GridColumn((Grid)Grid);
	}
}
