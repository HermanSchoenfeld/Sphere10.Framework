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

partial class LogScreen {
	private IContainer components;
	private TextBox _outputTextBox;

	protected override void Dispose(bool disposing) {
		if (disposing) {
			EndOwnedWork();
			components?.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		components = new Container();
		_outputTextBox = new TextBox();
		SuspendLayout();
		_outputTextBox.BackColor = SystemColors.Window;
		_outputTextBox.Dock = DockStyle.Fill;
		_outputTextBox.Font = new Font("Consolas", 9F);
		_outputTextBox.Location = new Point(0, 0);
		_outputTextBox.Multiline = true;
		_outputTextBox.Name = "_outputTextBox";
		_outputTextBox.ReadOnly = true;
		_outputTextBox.ScrollBars = ScrollBars.Both;
		_outputTextBox.Size = new Size(800, 500);
		_outputTextBox.TabIndex = 0;
		_outputTextBox.WordWrap = false;
		AutoScaleMode = AutoScaleMode.Dpi;
		Controls.Add(_outputTextBox);
		Name = "LogScreen";
		Size = new Size(800, 500);
		Title = "Log Output - Capture";
		ResumeLayout(false);
		PerformLayout();
	}
}
