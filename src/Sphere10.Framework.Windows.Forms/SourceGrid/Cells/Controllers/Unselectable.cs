// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Controllers;

/// <summary>
/// Implements a behavior that cannot receive the focus. This behavior can be shared between multiple cells.
/// </summary>
public class Unselectable : ControllerBase {
	public readonly static Unselectable Default = new Unselectable();

	public override void OnFocusEntering(CellContext sender, System.ComponentModel.CancelEventArgs e) {
		base.OnFocusEntering(sender, e);

		e.Cancel = !CanReceiveFocus(sender, e);
	}

	public override bool CanReceiveFocus(CellContext sender, EventArgs e) {
		return false;
	}
}
