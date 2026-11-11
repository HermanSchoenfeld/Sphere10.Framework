// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// This class implements a RowsBase class using always the same Height for all rows. Using this class you must only implement the Count method.
/// </summary>
public abstract class RowsSimpleBase : RowsBase {
	private int _rowHeight;

	public RowsSimpleBase(GridVirtual grid)
		: base(grid) {
		_rowHeight = grid.DefaultHeight;
	}

	public int RowHeight {
		get => _rowHeight;
		set {
			if (_rowHeight != value) {
				_rowHeight = value;
				PerformLayout();
			}
		}
	}

	public override int GetHeight(int row) => RowHeight;

	public override void SetHeight(int row, int height) => RowHeight = height;
}
