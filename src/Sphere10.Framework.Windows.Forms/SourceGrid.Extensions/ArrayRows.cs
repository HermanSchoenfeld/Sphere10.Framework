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
	public ArrayRows(ArrayGrid grid)
		: base(grid) {
	}

	public new ArrayGrid Grid => (ArrayGrid)base.Grid;

	public override int Count => Grid.DataSource == null ? Grid.FixedRows : Grid.DataSource.GetLength(0) + Grid.FixedRows;

	public AutoSizeMode AutoSizeMode { get; set; } = AutoSizeMode.Default;

	public override AutoSizeMode GetAutoSizeMode(int row) {
		return AutoSizeMode;
	}
}
