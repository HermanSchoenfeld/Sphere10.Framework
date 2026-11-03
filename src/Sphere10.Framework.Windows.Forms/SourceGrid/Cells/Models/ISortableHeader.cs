// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Models;

/// <summary>
/// Summary description for ICellSortableHeader.
/// </summary>
public interface ISortableHeader : IModel {
	/// <summary>
	/// Returns the current sort status
	/// </summary>
	/// <param name="cellContext"></param>
	/// <returns></returns>
	SortStatus GetSortStatus(CellContext cellContext);

	/// <summary>
	/// Set the current sort mode
	/// </summary>
	/// <param name="cellContext"></param>
	/// <param name="style"></param>
	void SetSortMode(CellContext cellContext, Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle style);
}
