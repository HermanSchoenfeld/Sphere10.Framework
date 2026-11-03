// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Sphere10.Framework.ComponentModel;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows.Forms.SourceGrid.Selection;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

// Controller
public class DataGridCellController : Cells.Controllers.ControllerBase {
	public override void OnValueChanging(CellContext sender, ValueChangeEventArgs args) {
		base.OnValueChanging(sender, args);

		//BeginEdit on the row, set the Cancel = true if failed to start editing.
		var success = ((DataGrid)sender.Grid).BeginEditRow(sender.Position.Row);
		if (success == false)
			throw new SourceGridException("Failed to editing row " + sender.Position.Row.ToString());
	}

	public override void OnEditStarting(CellContext sender, CancelEventArgs args) {
		base.OnEditStarting(sender, args);

		//BeginEdit on the row, set the Cancel = true if failed to start editing.
		var success = ((DataGrid)sender.Grid).BeginEditRow(sender.Position.Row);
		args.Cancel = !success;
	}
}
