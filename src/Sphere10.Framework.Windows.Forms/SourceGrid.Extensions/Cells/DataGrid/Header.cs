// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.DataGrid;

/// <summary>
/// A cell used for the top/left cell when using DataGridRowHeader.
/// </summary>
public class Header : Cells.Virtual.Header {
	public Header() {
		Model.AddModel(new Cells.Models.NullValueModel());
	}
}
