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

namespace Sphere10.Framework.Windows.Forms;

partial class ValidatedTextBoxButton {
	private System.Windows.Forms.Button _browseButton;
	private System.Windows.Forms.TextBox _textBox;
	private System.ComponentModel.Container _components = null;
	private System.Windows.Forms.ToolTip _actionToolTip;

	private void InitializeComponent() {
		_components = new System.ComponentModel.Container();
		_actionToolTip = new System.Windows.Forms.ToolTip(_components);
		this._browseButton = new System.Windows.Forms.Button();
		this._textBox = new Sphere10.Framework.Windows.Forms.TextBoxEx();
		this.SuspendLayout();
		//
		// _browseButton
		//
		this._browseButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Right)));
		this._browseButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
		this._browseButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
		this._browseButton.Location = new System.Drawing.Point(134, 2);
		this._browseButton.Name = "btDown";
		_actionToolTip.SetToolTip(_browseButton, "Open the value editor.");
		this._browseButton.Size = new System.Drawing.Size(24, 16);
		this._browseButton.TabIndex = 1;
		this._browseButton.Text = "...";
		this._browseButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
		this._browseButton.Click += new System.EventHandler(this.DownClick);
		//
		// _textBox
		//
		this._textBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
		this._textBox.AutoSize = false;
		this._textBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
		this._textBox.HideSelection = false;
		this._textBox.Location = new System.Drawing.Point(2, 2);
		this._textBox.Name = "txtBox";
		this._textBox.Size = new System.Drawing.Size(132, 16);
		this._textBox.TabIndex = 0;
		this._textBox.WordWrap = false;
		//
		// TextBoxTypedButton
		//
		this.BackColor = System.Drawing.Color.Transparent;
		this.Controls.Add(this._textBox);
		this.Controls.Add(this._browseButton);
		this.Name = "TextBoxTypedButton";
		this.Size = new System.Drawing.Size(160, 20);
		this.ResumeLayout(false);
	}
}
