// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Models;

public struct SortStatus {
	public Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle Style;
	public System.Collections.IComparer Comparer;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="style">Status of current sort.</param>
	public SortStatus(Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle style) {
		Style = style;
		Comparer = null;
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="style">Status of current sort.</param>
	/// <param name="comparer">Comparer used to sort the column. The comparer will take 2 Cell. If null the default ValueCellComparer is used.</param>
	public SortStatus(Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle style, System.Collections.IComparer comparer)
		: this(style) {
		Comparer = comparer;
	}
}
