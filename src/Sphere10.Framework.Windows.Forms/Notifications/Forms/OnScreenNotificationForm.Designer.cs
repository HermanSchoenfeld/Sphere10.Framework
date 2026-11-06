// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

partial class OnScreenNotificationForm {
	private IContainer components;
	private Label _messageLabel;
	private Timer _dismissTimer;

	protected override void Dispose(bool disposing) {
		if (disposing)
			components?.Dispose();
		base.Dispose(disposing);
	}

	#region Windows Form Designer generated code

	private void InitializeComponent() {
		components = new Container();
		_messageLabel = new Label();
		_dismissTimer = new Timer(components);
		SuspendLayout();
		//
		// _messageLabel
		//
		_messageLabel.AutoSize = true;
		_messageLabel.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point);
		_messageLabel.ForeColor = Color.White;
		_messageLabel.Location = new Point(0, 0);
		_messageLabel.Margin = new Padding(0);
		_messageLabel.MinimumSize = new Size(300, 68);
		_messageLabel.Name = "_messageLabel";
		_messageLabel.Padding = new Padding(28, 12, 28, 12);
		_messageLabel.Size = new Size(300, 68);
		_messageLabel.TabIndex = 0;
		_messageLabel.Text = "STATUS";
		_messageLabel.TextAlign = ContentAlignment.MiddleCenter;
		//
		// _dismissTimer
		//
		_dismissTimer.Interval = 1500;
		_dismissTimer.Tick += _dismissTimer_Tick;
		//
		// OnScreenNotificationForm
		//
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		AutoSize = true;
		AutoSizeMode = AutoSizeMode.GrowAndShrink;
		BackColor = Color.FromArgb(32, 35, 42);
		ClientSize = new Size(300, 68);
		ControlBox = false;
		Controls.Add(_messageLabel);
		FormBorderStyle = FormBorderStyle.None;
		MaximizeBox = false;
		MinimizeBox = false;
		Name = "OnScreenNotificationForm";
		Opacity = 0.82D;
		ShowIcon = false;
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.Manual;
		Text = "Status";
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion
}
