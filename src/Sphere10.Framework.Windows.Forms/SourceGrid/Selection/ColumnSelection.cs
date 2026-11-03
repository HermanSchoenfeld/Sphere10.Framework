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

public class ColumnSelection : SelectionBase {
	private Decorators.DecoratorSelection _decorator;
	private List<int> _list = new List<int>();

	public ColumnSelection() {
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

	public override bool IsSelectedRow(int row) {
		return false;
	}

	public override void SelectRow(int row, bool select) {
		throw new Exception("The method or operation is not implemented.");
	}

	public override bool IsSelectedCell(Position position) {
		return IsSelectedColumn(position.Column);
	}

	public override void SelectCell(Position position, bool select) {
		SelectColumn(position.Column, select);
	}

	public override bool IsSelectedRange(CellRange range) {
		for (var c = range.Start.Column; c <= range.End.Column; c++) {
			if (IsSelectedColumn(c) == false)
				return false;
		}

		return true;
	}

	public override void SelectRange(CellRange range, bool select) {
		for (var c = range.Start.Column; c <= range.End.Column; c++)
			SelectColumn(c, select);
	}

	public override void SelectColumn(int column, bool select) {
		if (select && _list.Contains(column) == false) {
			_list.Add(column);

			OnSelectionChanged(new RangeRegionChangedEventArgs(Grid.Columns.GetRange(column), CellRange.Empty));
		} else if (!select && _list.Contains(column)) {
			_list.Remove(column);

			OnSelectionChanged(new RangeRegionChangedEventArgs(CellRange.Empty, Grid.Columns.GetRange(column)));
		}
	}

	public override bool IsEmpty() {
		return _list.Count == 0;
	}

	public override bool IntersectsWith(CellRange rng) {
		for (var c = rng.Start.Column; c <= rng.End.Column; c++) {
			if (IsSelectedColumn(c))
				return true;
		}

		return false;
	}

	public override bool IsSelectedColumn(int column) {
		return _list.Contains(column);
	}

	protected override void OnResetSelection() {
		var prevRange = GetSelectionRegion();

		_list.Clear();

		OnSelectionChanged(new RangeRegionChangedEventArgs(null, prevRange));
	}

	public override RangeRegion GetSelectionRegion() {
		var region = new RangeRegion();

		if (Grid.Rows.Count > 0) {
			foreach (var column in _list)
				region.Add(ValidateRange(new CellRange(Grid.FixedRows, column, Grid.Rows.Count - 1, column)));
		}

		return region;
	}
}
