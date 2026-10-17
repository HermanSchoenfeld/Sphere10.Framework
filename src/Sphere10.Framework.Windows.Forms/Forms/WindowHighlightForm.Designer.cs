// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

partial class WindowHighlightForm {
	protected override void Dispose(bool disposing) {
		if (disposing) {
			Region = null;
			_frameRegion?.Dispose();
			_frameRegion = null;
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		SuspendLayout();
		AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
		BackColor = System.Drawing.SystemColors.Highlight;
		ClientSize = new System.Drawing.Size(320, 200);
		ControlBox = false;
		FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
		MaximizeBox = false;
		MinimizeBox = false;
		Name = "WindowHighlightForm";
		Opacity = 0.99D;
		ShowIcon = false;
		ShowInTaskbar = false;
		StartPosition = System.Windows.Forms.FormStartPosition.Manual;
		TabStop = false;
		ResumeLayout(false);
	}
}
