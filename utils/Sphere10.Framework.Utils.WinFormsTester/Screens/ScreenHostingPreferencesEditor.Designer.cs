// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester.Screens;

partial class ScreenHostingPreferencesEditor {
	private Label _preview;

	// Keep the custom preview visible and editable in the native Designer.
	private void InitializeComponent() {
		_preview = new Label();
		SuspendLayout();

		_preview.Name = "PreferencePreview";
		_preview.Dock = DockStyle.Bottom;
		_preview.AutoSize = true;
		_preview.MaximumSize = new Size(0, 0);
		_preview.Padding = new Padding(8);
		_preview.ForeColor = Color.RoyalBlue;
		_preview.Text = "GPU preferences preview";
		Controls.Add(_preview);
		_preview.BringToFront();
		Name = "ScreenHostingPreferencesEditor";
		Size = new Size(640, 480);

		ResumeLayout(false);
		PerformLayout();
	}
}
