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

partial class ColorPicker {
	private System.Windows.Forms.Button _browseButton;
	private System.Windows.Forms.Panel _colorPanel;
	private System.Windows.Forms.Label _colorLabel;
	private System.ComponentModel.Container _components = null;
	private System.Windows.Forms.ToolTip _actionToolTip;

	private void InitializeComponent() {
		_components = new System.ComponentModel.Container();
		_actionToolTip = new System.Windows.Forms.ToolTip(_components);
		this._colorLabel = new System.Windows.Forms.Label();
		this._colorPanel = new System.Windows.Forms.Panel();
		this._browseButton = new System.Windows.Forms.Button();
		this.SuspendLayout();
		//
		// _colorLabel
		//
		this._colorLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
		this._colorLabel.BackColor = System.Drawing.SystemColors.Window;
		this._colorLabel.Location = new System.Drawing.Point(28, 2);
		this._colorLabel.Name = "labelColor";
		this._colorLabel.Size = new System.Drawing.Size(80, 16);
		this._colorLabel.TabIndex = 2;
		this._colorLabel.Text = "Black";
		this._colorLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		//
		// _colorPanel
		//
		this._colorPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Left)));
		this._colorPanel.BackColor = System.Drawing.Color.Black;
		this._colorPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this._colorPanel.Location = new System.Drawing.Point(2, 2);
		this._colorPanel.Name = "panelColor";
		this._colorPanel.Size = new System.Drawing.Size(20, 16);
		this._colorPanel.TabIndex = 1;
		this._colorPanel.BackColorChanged += new System.EventHandler(this.PanelColorBackColorChanged);
		//
		// _browseButton
		//
		this._browseButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Right)));
		this._browseButton.BackColor = System.Drawing.SystemColors.Control;
		this._browseButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
		this._browseButton.Location = new System.Drawing.Point(110, 2);
		this._browseButton.Name = "btBrowse";
		_actionToolTip.SetToolTip(_browseButton, "Choose a color.");
		this._browseButton.Size = new System.Drawing.Size(24, 16);
		this._browseButton.TabIndex = 0;
		this._browseButton.Text = "...";
		this._browseButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
		this._browseButton.Click += new System.EventHandler(this.BrowseClick);
		//
		// ColorPicker
		//
		this.Controls.Add(this._colorPanel);
		this.Controls.Add(this._colorLabel);
		this.Controls.Add(this._browseButton);
		this.Name = "ColorPicker";
		this.Size = new System.Drawing.Size(136, 20);
		this.ResumeLayout(false);
	}
}
