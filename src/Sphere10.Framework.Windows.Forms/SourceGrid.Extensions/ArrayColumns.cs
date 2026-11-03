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

public class ArrayColumns : ColumnInfoCollection {
	public ArrayColumns(ArrayGrid grid)
		: base(grid) {
	}

	public new ArrayGrid Grid {
		get {
			return (ArrayGrid)base.Grid;
		}
	}

	/*public override int Count
	{
		get
		{
			if (Grid.DataSource == null)
				return Grid.FixedColumns;
			else
			{
				return Grid.DataSource.GetLength(1) + Grid.FixedColumns;
			}
		}
	}*/
	/*
			private AutoSizeMode mAutoSizeMode = AutoSizeMode.Default;
			public AutoSizeMode AutoSizeMode
			{
				get { return mAutoSizeMode; }
				set { mAutoSizeMode = value; }
			}

			public override AutoSizeMode GetAutoSizeMode(int row)
			{
				return mAutoSizeMode;
			}*/
}
