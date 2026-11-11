// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Selection;

public class RowSelection : SelectionBase {
	private Decorators.DecoratorSelection _decorator;
	private RangeMergerByRows _list = new RangeMergerByRows();

	public RowSelection() {
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

	public override bool IsSelectedColumn(int column) => false;

	public override void SelectColumn(int column, bool select) => throw new Exception("The method or operation is not implemented.");

	public override bool IsSelectedCell(Position position) => IsSelectedRow(position.Row);

	public override void SelectCell(Position position, bool select) => SelectRow(position.Row, select);

	public override void SelectRow(int row, bool select) {
		var rowRange = Grid.Rows.GetRange(row);
		if (select && _list.IsSelectedRow(row) == false) {
			// if multi selection is false, remove all previously selected rows
			var activePosition = this.ActivePosition;
			if (this.EnableMultiSelection == false)
				this.Grid.Selection.ResetSelection(false);
			// continue with adding selection
			_list.AddRange(rowRange);
			this.ActivePosition = activePosition;
			OnSelectionChanged(new RangeRegionChangedEventArgs(rowRange, CellRange.Empty));
		} else if (!select && _list.IsSelectedRow(row)) {
			_list.RemoveRange(rowRange);
			OnSelectionChanged(new RangeRegionChangedEventArgs(CellRange.Empty, rowRange));
		}
	}

	public override bool IsSelectedRange(CellRange range) {
		for (var r = range.Start.Row; r <= range.End.Row; r++) {
			if (IsSelectedRow(r) == false)
				return false;
		}

		return true;
	}

	public override void SelectRange(CellRange range, bool select) {
		var normalizedRange = NormalizeRange(range);
		if (select)
			_list.AddRange(normalizedRange);
		else
			_list.RemoveRange(normalizedRange);
		OnSelectionChanged(new RangeRegionChangedEventArgs(normalizedRange, CellRange.Empty));
	}

	public override bool IsEmpty() => _list.IsEmpty();

	public override bool IntersectsWith(CellRange rng) {
		for (var r = rng.Start.Row; r <= rng.End.Row; r++) {
			if (IsSelectedRow(r))
				return true;
		}

		return false;
	}

	public override bool IsSelectedRow(int row) => _list.IsSelectedRow(row);

	protected override void OnResetSelection() {
		var prevRange = GetSelectionRegion();

		_list.Clear();

		OnSelectionChanged(new RangeRegionChangedEventArgs(null, prevRange));
	}

	public override RangeRegion GetSelectionRegion() {
		var region = new RangeRegion();

		if (Grid.Columns.Count == 0)
			return region;
		foreach (var selectedRange in _list.GetSelectedRowRegions(0, Grid.Columns.Count))
			region.Add(ValidateRange(selectedRange));

		return region;
	}

	private CellRange NormalizeRange(CellRange range) => new CellRange(range.Start.Row, 0, range.End.Row, Grid.Columns.Count - 1);
}
