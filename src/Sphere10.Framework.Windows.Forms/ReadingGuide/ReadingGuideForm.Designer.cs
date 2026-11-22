// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

// Native presentation and owned-clock disposal for the passive reading guide.
partial class ReadingGuideForm {
	protected override void Dispose(bool disposing) {
		if (disposing && _pointerClock != null) {
			_pointerClock.Frame -= PointerClock_Frame;
			_pointerClock.Dispose();
			_pointerClock = null;
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		SuspendLayout();
		AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
		BackColor = System.Drawing.Color.Gold;
		ClientSize = new System.Drawing.Size(320, 4);
		ControlBox = false;
		Enabled = false;
		FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
		MaximizeBox = false;
		MinimizeBox = false;
		Name = "ReadingGuideForm";
		ShowIcon = false;
		ShowInTaskbar = false;
		StartPosition = System.Windows.Forms.FormStartPosition.Manual;
		TabStop = false;
		ResumeLayout(false);
	}
}
