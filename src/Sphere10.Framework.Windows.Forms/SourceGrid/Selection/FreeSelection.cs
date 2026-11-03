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
/// A selection class that support free selection of cells (ranges)
/// </summary>
public class FreeSelection : SelectionBase {
	private RangeRegion _region = new RangeRegion();
	private Decorators.DecoratorSelection _decorator;

	public FreeSelection() {
		_region.Changed += delegate (object sender, RangeRegionChangedEventArgs args) {
			OnSelectionChanged(args);
		};
	}

	public override void BindToGrid(GridVirtual grid) {
		base.BindToGrid(grid);

		_decorator = new Decorators.DecoratorSelection(this);
		Grid.Decorators.Add(_decorator);
	}

	public override void UnBindToGrid() {
		Grid.Decorators.Remove(_decorator);

		base.UnBindToGrid();
	}

	public override bool IsSelectedColumn(int column) {
		return _region.ContainsColumn(column);
	}

	public override void SelectColumn(int column, bool select) {
		var rng = Grid.Columns.GetRange(column);

		SelectRange(rng, select);
	}

	public override bool IsSelectedRow(int row) {
		return _region.ContainsRow(row);
	}

	public override void SelectRow(int row, bool select) {
		var rng = Grid.Rows.GetRange(row);

		SelectRange(rng, select);
	}

	public override bool IsSelectedCell(Position position) {
		return _region.Contains(position);
	}

	public override void SelectCell(Position position, bool select) {
		SelectRange(Grid.PositionToCellRange(position), select);
	}

	public override bool IsSelectedRange(CellRange range) {
		return _region.Contains(range);
	}

	public override void SelectRange(CellRange range, bool select) {
		var newRange = Grid.RangeToCellRange(range);
		if (select)
			_region.Add(ValidateRange(newRange));
		else
			_region.Remove(newRange);
	}

	/// <summary>
	/// Returns true if the selection is empty
	/// </summary>
	/// <returns></returns>
	public override bool IsEmpty() {
		return _region.IsEmpty();
	}

	/// <summary>
	/// Returns the selected region.
	/// </summary>
	/// <returns></returns>
	public override RangeRegion GetSelectionRegion() {
		return new RangeRegion(_region);
	}

	/// <summary>
	/// Returns true if the specified selection intersect with the range
	/// </summary>
	/// <param name="rng"></param>
	/// <returns></returns>
	public override bool IntersectsWith(CellRange rng) {
		return _region.IntersectsWith(rng);
	}

	protected override void OnResetSelection() {
		_region.Clear();
	}
}
