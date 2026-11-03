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
using System.Collections.Generic;
using System.ComponentModel;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

partial class ListEditor {
	private Grid _grid;

	private System.Windows.Forms.Button _moveDownButton;

	private System.Windows.Forms.Button _moveUpButton;

	private System.Windows.Forms.Button _removeButton;

	private System.Windows.Forms.Button _addButton;

	private System.Windows.Forms.Button _refreshButton;

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
		var resources = new System.Resources.ResourceManager(typeof(ListEditor));
		_grid = new Grid();
		_moveDownButton = new System.Windows.Forms.Button();
		_moveUpButton = new System.Windows.Forms.Button();
		_removeButton = new System.Windows.Forms.Button();
		_addButton = new System.Windows.Forms.Button();
		_refreshButton = new System.Windows.Forms.Button();
		this.SuspendLayout();
		//
		// grid
		//
		_grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
		_grid.AutoStretchColumnsToFitWidth = false;
		_grid.AutoStretchRowsToFitHeight = false;
		_grid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		_grid.CustomSort = false;
		_grid.Location = new System.Drawing.Point(4, 32);
		_grid.Name = "grid";
		_grid.Size = new System.Drawing.Size(332, 216);
		_grid.SpecialKeys = GridSpecialKeys.Default;
		_grid.TabIndex = 0;
		//
		// btDown
		//
		_moveDownButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
		_moveDownButton.Enabled = false;
		_moveDownButton.Image = ((System.Drawing.Image)(resources.GetObject("btDown.Image")));
		_moveDownButton.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
		_moveDownButton.Location = new System.Drawing.Point(312, 4);
		_moveDownButton.Name = "btDown";
		_moveDownButton.Size = new System.Drawing.Size(24, 23);
		_moveDownButton.TabIndex = 1;
		_moveDownButton.Click += new System.EventHandler(this.MoveDownClicked);
		//
		// btUp
		//
		_moveUpButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
		_moveUpButton.Enabled = false;
		_moveUpButton.Image = ((System.Drawing.Image)(resources.GetObject("btUp.Image")));
		_moveUpButton.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
		_moveUpButton.Location = new System.Drawing.Point(284, 4);
		_moveUpButton.Name = "btUp";
		_moveUpButton.Size = new System.Drawing.Size(24, 23);
		_moveUpButton.TabIndex = 2;
		_moveUpButton.Click += new System.EventHandler(this.MoveUpClicked);
		//
		// btRemove
		//
		_removeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
		_removeButton.Enabled = false;
		_removeButton.Image = ((System.Drawing.Image)(resources.GetObject("btRemove.Image")));
		_removeButton.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
		_removeButton.Location = new System.Drawing.Point(220, 4);
		_removeButton.Name = "btRemove";
		_removeButton.Size = new System.Drawing.Size(24, 23);
		_removeButton.TabIndex = 3;
		_removeButton.Click += new System.EventHandler(this.RemoveClicked);
		//
		// btAdd
		//
		_addButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
		_addButton.Image = ((System.Drawing.Image)(resources.GetObject("btAdd.Image")));
		_addButton.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
		_addButton.Location = new System.Drawing.Point(192, 4);
		_addButton.Name = "btAdd";
		_addButton.Size = new System.Drawing.Size(24, 23);
		_addButton.TabIndex = 4;
		_addButton.Click += new System.EventHandler(this.AddClicked);
		//
		// btrRefreshList
		//
		_refreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
		_refreshButton.Image = ((System.Drawing.Image)(resources.GetObject("btrRefreshList.Image")));
		_refreshButton.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
		_refreshButton.Location = new System.Drawing.Point(252, 4);
		_refreshButton.Name = "btrRefreshList";
		_refreshButton.Size = new System.Drawing.Size(24, 23);
		_refreshButton.TabIndex = 5;
		_refreshButton.Click += new System.EventHandler(this.RefreshListClicked);
		//
		// ListEditor
		//
		this.Controls.Add(_refreshButton);
		this.Controls.Add(_addButton);
		this.Controls.Add(_removeButton);
		this.Controls.Add(_moveUpButton);
		this.Controls.Add(_moveDownButton);
		this.Controls.Add(_grid);
		this.Name = "ListEditor";
		this.Size = new System.Drawing.Size(340, 252);

		_grid.Selection.FocusRowLeaving += new RowCancelEventHandler(SelectionFocusRowLeaving);
		_grid.Selection.FocusRowEntered += new RowEventHandler(SelectionFocusRowEntered);
		_grid.Selection.FocusStyle = FocusStyle.None;
		this.ResumeLayout(false);
	}
}
