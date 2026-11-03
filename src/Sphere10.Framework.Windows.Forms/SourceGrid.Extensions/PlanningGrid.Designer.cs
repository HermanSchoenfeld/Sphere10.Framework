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
using System.ComponentModel;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Controllers;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Planning;

partial class PlanningGrid {
	private Grid _grid;

	/// <summary>
	/// Required designer variable.
	/// </summary>
	private System.ComponentModel.Container _components = null;

	// Component Designer generated code
	/// <summary>
	/// Required method for Designer support - do not modify
	/// the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent() {
		_grid = new Grid();
		this.SuspendLayout();
		//
		// grid
		//
		_grid.AutoStretchColumnsToFitWidth = false;
		_grid.AutoStretchRowsToFitHeight = false;
		_grid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		_grid.Dock = System.Windows.Forms.DockStyle.Fill;
		_grid.Location = new System.Drawing.Point(0, 0);
		_grid.Name = "grid";
		_grid.Size = new System.Drawing.Size(360, 340);
		_grid.SpecialKeys = GridSpecialKeys.Default;
		_grid.TabIndex = 0;
		//
		// PlanningGrid
		//
		this.Controls.Add(_grid);
		this.Name = "PlanningGrid";
		this.Size = new System.Drawing.Size(360, 340);

		_grid.Selection.FocusStyle = FocusStyle.None;
		this.ResumeLayout(false);
	}
}
