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
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

partial class MultiSelectionButton {
	private System.Windows.Forms.Button _mainButton;
	private Sphere10.Framework.Windows.Forms.DropDownButton _arrowButton;
	private System.ComponentModel.Container _components = null;

	private void InitializeComponent() {
		var resources = new System.Resources.ResourceManager(typeof(MultiSelectionButton));
		this._mainButton = new System.Windows.Forms.Button();
		this._arrowButton = new Sphere10.Framework.Windows.Forms.DropDownButton();
		this.SuspendLayout();
		//
		// _mainButton
		//
		this._mainButton.Dock = System.Windows.Forms.DockStyle.Fill;
		this._mainButton.Location = new System.Drawing.Point(0, 0);
		this._mainButton.Name = "btMain";
		this._mainButton.Size = new System.Drawing.Size(61, 23);
		this._mainButton.TabIndex = 0;
		this._mainButton.Text = "button1";
		this._mainButton.Click += new System.EventHandler(this.MainClick);
		//
		// _arrowButton
		//
		this._arrowButton.Dock = System.Windows.Forms.DockStyle.Right;
		this._arrowButton.Location = new System.Drawing.Point(61, 0);
		this._arrowButton.Name = "btArrow";
		this._arrowButton.Size = new System.Drawing.Size(14, 23);
		this._arrowButton.TabIndex = 1;
		this._arrowButton.Click += new System.EventHandler(this.ArrowClick);
		//
		// MultiSelectionButton
		//
		this.Controls.Add(this._mainButton);
		this.Controls.Add(this._arrowButton);
		this.Name = "ButtonMultiSelection";
		this.Size = new System.Drawing.Size(75, 23);
		this.ResumeLayout(false);
	}
}
