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
	private int _maximalWidth = -1;
	private int _minimalWidth = 0;
	private int _width;
	private GridVirtual _grid;
	private object _tag;
	private AutoSizeMode _autoSizeMode = AutoSizeMode.Default;
	private bool _visible = true;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="grid"></param>
	public ColumnInfo(GridVirtual grid) {
		_grid = grid;
		_width = Grid.DefaultWidth;
	}

	/// <summary>
	/// Maximal width of the current Column, -1 if unlimited
	/// </summary>
	public int MaximalWidth {
		get {
			return _maximalWidth;
		}
		set {
			// Maximal width can not be smaller than minimal width
			if (value < _minimalWidth)
				value = _minimalWidth;
			if (value != _maximalWidth && value >= -1) {
				_maximalWidth = value;
				if (this.Width > _maximalWidth && _maximalWidth > -1)
					this.Width = _maximalWidth;
			}
		}
	}

	/// <summary>
	/// Minimal width of the current Column
	/// </summary>
	public int MinimalWidth {
		get {
			return _minimalWidth;
		}
		set {
			if (value < 0)
				value = 0;
			// Minimal width can not be bigger than maximal width
			if (value > _maximalWidth && _maximalWidth > -1)
				value = _maximalWidth;
			if (value != _minimalWidth) {
				_minimalWidth = value;
				if (this.Width < _minimalWidth && this.Visible)
					this.Width = _minimalWidth;
			}
		}
	}

	/// <summary>
	/// Width of the current Column
	/// </summary>
	public int Width {
		get {
			return _width;
		}
		set {
			if (value < _minimalWidth)
				value = _minimalWidth;
			if (value > _maximalWidth && _maximalWidth > -1)
				value = _maximalWidth;

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
	public int Index {
		get {
			return ((ColumnInfoCollection)Grid.Columns).IndexOf(this);
		}
	}

	/// <summary>
	/// Attached Grid
	/// </summary>
	[Browsable(false)]
	public GridVirtual Grid {
		get {
			return _grid;
		}
	}

	public CellRange Range {
		get {
			if (_grid == null)
				throw new SourceGridException("Invalid Grid object");

			return new CellRange(0, Index, Grid.Rows.Count - 1, Index);
		}
	}

	/// <summary>
	/// A property that the user can use to insert custom informations associated to a specific column
	/// </summary>
	[Browsable(false)]
	public object Tag {
		get {
			return _tag;
		}
		set {
			_tag = value;
		}
	}

	/// <summary>
	/// Flags for autosize and stretch
	/// </summary>
	public AutoSizeMode AutoSizeMode {
		get {
			return _autoSizeMode;
		}
		set {
			_autoSizeMode = value;
		}
	}

	/// <summary>
	/// Gets or sets if the column is visible.
	/// Internally set the width to 0 to hide a column.
	/// </summary>
	public bool Visible {
		get {
			return _visible;
		}
		set {
			if (value != _visible) {
				_visible = value;
				((ColumnInfoCollection)Grid.Columns).OnColumnWidthChanged(new ColumnInfoEventArgs(this));
			}
		}
	}
}
