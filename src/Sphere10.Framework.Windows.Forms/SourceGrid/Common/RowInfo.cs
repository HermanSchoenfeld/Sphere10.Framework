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
/// Row Information
/// </summary>
public class RowInfo {
	private int _height;
	private GridVirtual _grid;
	private object _tag;
	private AutoSizeMode _autoSizeMode = AutoSizeMode.Default;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="grid"></param>
	public RowInfo(GridVirtual grid) {
		_grid = grid;
		_height = Grid.DefaultHeight;
	}

	/// <summary>
	/// Height of the current row
	/// </summary>
	public int Height {
		get => _height;
		set {
			if (value < 0)
				value = 0;

			if (_height != value) {
				_height = value;
				((RowInfoCollection)_grid.Rows).OnRowHeightChanged(new RowInfoEventArgs(this));
			}
		}
	}

	//private int m_Index;
	/// <summary>
	/// Index of the current row
	/// </summary>
	public int Index => ((RowInfoCollection)Grid.Rows).IndexOf(this);

	/// <summary>
	/// Attached Grid
	/// </summary>
	[Browsable(false)]
	public GridVirtual Grid => _grid;

	public CellRange Range {
		get {
			if (_grid == null)
				throw new SourceGridException("Invalid Grid object");

			return new CellRange(Index, 0, Index, Grid.Columns.Count - 1);
		}
	}

	/// <summary>
	/// A property that the user can use to insert custom informations associated to a specific row
	/// </summary>
	[Browsable(false)]
	public object Tag {
		get => _tag;
		set => _tag = value;
	}

	/// <summary>
	/// Flags for autosize and stretch
	/// </summary>
	public AutoSizeMode AutoSizeMode {
		get => _autoSizeMode;
		set => _autoSizeMode = value;
	}

	/// <summary>
	/// Gets or sets if the row is visible.
	/// Internally set the height to 0 to hide a row.
	/// </summary>
	public bool Visible {
		get => Grid.Rows.IsRowVisible(this.Index);
		set => Grid.Rows.ShowRow(this.Index, value);
	}
}
