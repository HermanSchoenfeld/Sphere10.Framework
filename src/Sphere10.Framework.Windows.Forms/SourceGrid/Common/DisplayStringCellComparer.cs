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
using System.Runtime.Serialization;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// A comparer for the Cell class. (Not for CellVirtual). Using the DisplayString of the cell.
/// </summary>
public class DisplayStringCellComparer : IComparer {
	public virtual System.Int32 Compare(System.Object x, System.Object y) {
		//Cell object
		if (x == null && y == null)
			return 0;
		if (x == null)
			return -1;
		if (y == null)
			return 1;

		if (x is IComparable)
			return ((IComparable)x).CompareTo(y);
		if (y is IComparable)
			return (-1 * ((IComparable)y).CompareTo(x));

		//Cell.Value object
		var vx = ((Cells.ICell)x).DisplayText;
		var vy = ((Cells.ICell)y).DisplayText;
		if (vx == null && vy == null)
			return 0;
		if (vx == null)
			return -1;
		if (vy == null)
			return 1;

		return vx.CompareTo(vy);
	}
}
