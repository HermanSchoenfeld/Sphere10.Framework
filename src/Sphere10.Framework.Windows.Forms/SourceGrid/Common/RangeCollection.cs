// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// A collection of elements of type Range
/// </summary>
[Serializable]
public class RangeCollection : List<CellRange> {
	/// <summary>
	/// Returns true if the specified cell position is present in any range in the current collection.
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public bool ContainsCell(Position position) {
		foreach (var range in this) {
			if (range.Contains(position))
				return true;
		}
		return false;
	}
}
