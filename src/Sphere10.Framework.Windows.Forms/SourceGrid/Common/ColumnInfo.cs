// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// Column Information
/// </summary>
public class ColumnInfo {
	private int _width;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="grid"></param>
	public ColumnInfo(GridVirtual grid) {
		Grid = grid;
		_width = Grid.DefaultWidth;
	}

	/// <summary>
	/// Maximal width of the current Column, -1 if unlimited
	/// </summary>
	public int MaximalWidth {
		get {
			return field;
		}
		set {
			// Maximal width can not be smaller than minimal width
			if (value < MinimalWidth)
				value = MinimalWidth;
			if (value != field && value >= -1) {
				field = value;
				if (this.Width > field && field > -1)
					this.Width = field;
			}
		}
	} = -1;

	/// <summary>
	/// Minimal width of the current Column
	/// </summary>
	public int MinimalWidth {
		get {
			return field;
		}
		set {
			if (value < 0)
				value = 0;
			// Minimal width can not be bigger than maximal width
			if (value > MaximalWidth && MaximalWidth > -1)
				value = MaximalWidth;
			if (value != field) {
				field = value;
				if (this.Width < field && this.Visible)
					this.Width = field;
			}
		}
	} = 0;

	/// <summary>
	/// Width of the current Column
	/// </summary>
	public int Width {
		get {
			return _width;
		}
		set {
			if (value < MinimalWidth)
				value = MinimalWidth;
			if (value > MaximalWidth && MaximalWidth > -1)
				value = MaximalWidth;

			if (_width != value) {
				_width = value;
				if (Visible)
					((ColumnInfoCollection)Grid.Columns).OnColumnWidthChanged(new ColumnInfoEventArgs(this));
			}
		}
	}

	/// <summary>
	/// Index of the current Column
	/// </summary>
	public int Index => ((ColumnInfoCollection)Grid.Columns).IndexOf(this);

	/// <summary>
	/// Attached Grid
	/// </summary>
	[Browsable(false)]
	public GridVirtual Grid { get; }

	public CellRange Range => Grid == null ? throw new SourceGridException("Invalid Grid object") : new CellRange(0, Index, Grid.Rows.Count - 1, Index);

	/// <summary>
	/// A property that the user can use to insert custom informations associated to a specific column
	/// </summary>
	[Browsable(false)]
	public object Tag { get; set; }

	/// <summary>
	/// Flags for autosize and stretch
	/// </summary>
	public AutoSizeMode AutoSizeMode { get; set; } = AutoSizeMode.Default;

	/// <summary>
	/// Gets or sets if the column is visible.
	/// Internally set the width to 0 to hide a column.
	/// </summary>
	public bool Visible {
		get {
			return field;
		}
		set {
			if (value != field) {
				field = value;
				((ColumnInfoCollection)Grid.Columns).OnColumnWidthChanged(new ColumnInfoEventArgs(this));
			}
		}
	} = true;
}
