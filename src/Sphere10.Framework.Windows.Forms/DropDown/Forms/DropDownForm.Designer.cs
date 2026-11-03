// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

partial class DropDownForm {
	private System.Windows.Forms.Panel _containerPanel;
	private System.ComponentModel.Container _components = null;

	private void InitializeComponent() {
		this._containerPanel = new System.Windows.Forms.Panel();
		this.SuspendLayout();
		//
		// _containerPanel
		//
		this._containerPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this._containerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this._containerPanel.Location = new System.Drawing.Point(0, 0);
		this._containerPanel.Name = "panelContainer";
		this._containerPanel.Size = new System.Drawing.Size(84, 48);
		this._containerPanel.TabIndex = 0;
		//
		// ctlDropDownCustom
		//
		this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
		this.ClientSize = new System.Drawing.Size(84, 48);
		this.ControlBox = false;
		this.Controls.Add(this._containerPanel);
		this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
		this.MaximizeBox = false;
		this.MinimizeBox = false;
		this.Name = "ctlDropDownCustom";
		this.ShowInTaskbar = false;
		this.Text = "ctlDropDownCustom";
		this.Visible = false;
		this.StartPosition = FormStartPosition.Manual;
		this.Deactivate += new System.EventHandler(this.DropDownDeactivate);
		this.Layout += new LayoutEventHandler(DropDownLayout);
		this.ResumeLayout(false);
	}
}
