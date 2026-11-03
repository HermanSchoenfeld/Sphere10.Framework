// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells;

/// <summary>
/// A cell that rappresent a button
/// </summary>
public class Button : Cell {
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="value"></param>
	public Button(object value)
		: base(value) {
		View = Views.Button.Default;
		AddController(Controllers.MouseInvalidate.Default);
		//NOTE: I don't add the Unselectable controller because the Execute event of the Button it is not fired when the cell is not selected
	}
}
