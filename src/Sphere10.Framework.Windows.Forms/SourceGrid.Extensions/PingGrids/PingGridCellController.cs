// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Controllers;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Extensions.PingGrids;

// Controller
/// <summary>
/// Notify PingGrid of value editing
/// </summary>
public class PingGridCellController : ControllerBase {
	public override void OnValueChanging(CellContext sender, ValueChangeEventArgs args) {
		base.OnValueChanging(sender, args);
	}

	public override void OnEditStarting(CellContext sender, CancelEventArgs args) {
		base.OnEditStarting(sender, args);
	}
}
