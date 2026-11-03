// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Models;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

// Rows and Columns
public class ArrayRows : RowsSimpleBase {
	private AutoSizeMode _autoSizeMode = AutoSizeMode.Default;

	public ArrayRows(ArrayGrid grid)
		: base(grid) {
	}

	public new ArrayGrid Grid {
		get {
			return (ArrayGrid)base.Grid;
		}
	}

	public override int Count {
		get {
			if (Grid.DataSource == null)
				return Grid.FixedRows;
			return Grid.DataSource.GetLength(0) + Grid.FixedRows;
		}
	}

	public AutoSizeMode AutoSizeMode {
		get {
			return _autoSizeMode;
		}
		set {
			_autoSizeMode = value;
		}
	}

	public override AutoSizeMode GetAutoSizeMode(int row) {
		return _autoSizeMode;
	}
}
