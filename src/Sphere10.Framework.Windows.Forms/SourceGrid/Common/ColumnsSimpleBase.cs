// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// This class implements a RowsBase class using always the same Height for all rows. Using this class you must only implement the Count method.
/// </summary>
public abstract class ColumnsSimpleBase : ColumnsBase {
	private int _columnWidth;

	public ColumnsSimpleBase(GridVirtual grid)
		: base(grid) {
		_columnWidth = grid.DefaultWidth;
	}

	public int ColumnWidth {
		get {
			return _columnWidth;
		}
		set {
			if (_columnWidth != value) {
				_columnWidth = value;
				PerformLayout();
			}
		}
	}

	public override int GetWidth(int column) {
		return ColumnWidth;
	}

	public override void SetWidth(int column, int width) {
		ColumnWidth = width;
	}

	public override bool IsColumnVisible(int column) {
		return true;
	}

	public override void HideColumn(int column) {
		throw new NotSupportedException("ColumnsSimpleBase does not support column hiding");
	}

	public override void ShowColumn(int column) {
		return;
	}
}
