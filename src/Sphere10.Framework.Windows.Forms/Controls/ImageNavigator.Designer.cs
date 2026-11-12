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

partial class ImageNavigator {
	private System.Windows.Forms.Label _statusLabel;
	private System.Windows.Forms.PictureBox _pictureBox;
	private System.Windows.Forms.Button _nextButton;
	private System.Windows.Forms.Button _previousButton;
	private System.ComponentModel.Container _components = null;
	private System.Windows.Forms.ToolTip _actionToolTip;

	private void InitializeComponent() {
		_components ??= new System.ComponentModel.Container();
		_actionToolTip = new System.Windows.Forms.ToolTip(_components);
		var resources = new System.Resources.ResourceManager(typeof(ImageNavigator));
		this._pictureBox = new System.Windows.Forms.PictureBox();
		this._nextButton = new System.Windows.Forms.Button();
		this._previousButton = new System.Windows.Forms.Button();
		this._statusLabel = new System.Windows.Forms.Label();
		this.SuspendLayout();
		//
		// _pictureBox
		//
		this._pictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
		this._pictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this._pictureBox.Location = new System.Drawing.Point(0, 0);
		this._pictureBox.Name = "pictureBox";
		this._pictureBox.Size = new System.Drawing.Size(344, 224);
		this._pictureBox.TabIndex = 0;
		this._pictureBox.TabStop = false;
		//
		// _nextButton
		//
		this._nextButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
		this._nextButton.DialogResult = System.Windows.Forms.DialogResult.None;
		this._nextButton.Image = ((System.Drawing.Image)(resources.GetObject("btNext.Image")));
		this._nextButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this._nextButton.Location = new System.Drawing.Point(176, 228);
		this._nextButton.Name = "btNext";
		_actionToolTip.SetToolTip(_nextButton, "Show the next image.");
		this._nextButton.Size = new System.Drawing.Size(24, 24);
		this._nextButton.TabIndex = 1;
		this._nextButton.TabStop = false;
		this._nextButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this._nextButton.Click += new System.EventHandler(this.NextClick);
		//
		// _previousButton
		//
		this._previousButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
		this._previousButton.DialogResult = System.Windows.Forms.DialogResult.None;
		this._previousButton.Image = ((System.Drawing.Image)(resources.GetObject("btPrevious.Image")));
		this._previousButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this._previousButton.Location = new System.Drawing.Point(144, 228);
		this._previousButton.Name = "btPrevious";
		_actionToolTip.SetToolTip(_previousButton, "Show the previous image.");
		this._previousButton.Size = new System.Drawing.Size(24, 24);
		this._previousButton.TabIndex = 2;
		this._previousButton.TabStop = false;
		this._previousButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this._previousButton.Click += new System.EventHandler(this.PreviousClick);
		//
		// _statusLabel
		//
		this._statusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
		this._statusLabel.Location = new System.Drawing.Point(4, 228);
		this._statusLabel.Name = "lblStatus";
		this._statusLabel.Size = new System.Drawing.Size(132, 23);
		this._statusLabel.TabIndex = 3;
		this._statusLabel.Text = "status";
		this._statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		//
		// ImageNavigator
		//
		this.Controls.Add(this._previousButton);
		this.Controls.Add(this._nextButton);
		this.Controls.Add(this._statusLabel);
		this.Controls.Add(this._pictureBox);
		this.Name = "ImageNavigator";
		this.Size = new System.Drawing.Size(344, 256);
		this.ResumeLayout(false);
	}
}
