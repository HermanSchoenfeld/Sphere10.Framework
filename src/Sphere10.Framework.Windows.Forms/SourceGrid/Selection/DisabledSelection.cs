// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Selection;

public class DisabledSelection : SelectionBase {
	public override bool IsSelectedColumn(int column) => false;

	public override void SelectRow(int row, bool @select) {
	}

	public override bool IsSelectedCell(Position position) => false;

	public override bool IsEmpty() => true;

	public override RangeRegion GetSelectionRegion() => new RangeRegion();

	public override bool IntersectsWith(CellRange rng) => false;

	public override void SelectRange(CellRange range, bool @select) {
	}

	public override void SelectCell(Position position, bool @select) {
	}

	public override bool IsSelectedRange(CellRange range) => false;

	public override void SelectColumn(int column, bool @select) {
	}

	public override bool IsSelectedRow(int row) => false;

	protected override void OnResetSelection() {
	}
}
