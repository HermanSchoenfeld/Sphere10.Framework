// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// Range custom
/// </summary>
public class RangeLoader : IRangeLoader {
	private CellRange _range;

	/// <summary>
	/// Constructor
	/// </summary>
	public RangeLoader(CellRange range) {
		_range = range;
	}

	/// <summary>
	/// Gets or sets the Range loaded in the class.
	/// </summary>
	public CellRange Range {
		get => _range;
		set => _range = value;
	}

	/// <summary>
	/// Returns the Range struct from the specific instance
	/// </summary>
	/// <param name="grid"></param>
	/// <returns></returns>
	public virtual CellRange GetRange(GridVirtual grid) {
		return _range;
	}
}
