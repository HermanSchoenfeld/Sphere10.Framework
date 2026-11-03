// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

public class RangePaintEventArgs : EventArgs {
	private GridVirtual _grid;
	private Sphere10.Framework.Drawing.GraphicsCache _graphicsCache;
	private CellRange _drawingRange;

	public RangePaintEventArgs(GridVirtual grid, Sphere10.Framework.Drawing.GraphicsCache graphicsCache, CellRange drawingRange) {
		_grid = grid;
		_graphicsCache = graphicsCache;
		_drawingRange = drawingRange;
	}

	public GridVirtual Grid {
		get {
			return _grid;
		}
	}

	public Sphere10.Framework.Drawing.GraphicsCache GraphicsCache {
		get {
			return _graphicsCache;
		}
		set {
			_graphicsCache = value;
		}
	}

	public CellRange DrawingRange {
		get {
			return _drawingRange;
		}
		set {
			_drawingRange = value;
		}
	}
}
