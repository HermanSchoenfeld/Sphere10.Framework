// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Decorators;

public class DecoratorHighlight : DecoratorBase {
	private CellRange _range = CellRange.Empty;

	/// <summary>
	/// Gets or sets the range to draw
	/// </summary>
	public CellRange Range {
		get {
			return _range;
		}
		set {
			_range = value;
		}
	}

	public override bool IntersectWith(CellRange range) {
		return Range.IntersectsWith(range);
	}

	public override void Draw(RangePaintEventArgs e) {
	}
}
