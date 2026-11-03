// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

partial class ControlCatalogHostedScreen {
	private Label _className;
	private TextBox _draft;

	private void InitializeComponent() {
		_className = new Label();
		_draft = new TextBox();
		SuspendLayout();
		_className.Name = "hostedClassName";
		_className.Dock = DockStyle.Top;
		_className.Height = 58;
		_className.Padding = new Padding(12);
		_className.Text = "Sphere10.Framework.Windows.Forms.WinFormsApplicationScreen";
		_draft.Name = "hostedDraft";
		_draft.Dock = DockStyle.Fill;
		_draft.Multiline = true;
		_draft.Text = "Edit this draft, switch native tabs, or detach and dock this screen. The native host retains the same instance.";
		Controls.Add(_draft);
		Controls.Add(_className);
		Name = "ControlCatalogHostedScreen";
		Size = new Size(600, 400);
		AutoScaleMode = AutoScaleMode.Font;
		ResumeLayout(false);
		PerformLayout();
	}
}

