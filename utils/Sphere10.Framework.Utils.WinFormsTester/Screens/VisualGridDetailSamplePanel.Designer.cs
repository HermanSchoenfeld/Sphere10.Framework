// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

partial class VisualGridDetailSamplePanel {
	private PictureBox _picture;
	private Label _heading;
	private Label _sample;
	private TextBox _notes;

	protected override void Dispose(bool disposing) {
		if (disposing)
			_picture.Image?.Dispose();
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		_picture = new PictureBox();
		_heading = new Label();
		_sample = new Label();
		_notes = new TextBox();
		SuspendLayout();
		Padding = new Padding(12);
		_picture.Dock = DockStyle.Left;
		_picture.Width = 100;
		_picture.SizeMode = PictureBoxSizeMode.Zoom;
		_heading.Dock = DockStyle.Top;
		_heading.Height = 28;
		_sample.Dock = DockStyle.Top;
		_sample.Height = 36;
		_notes.Dock = DockStyle.Fill;
		_notes.Multiline = true;
		_notes.ScrollBars = ScrollBars.Vertical;
		Controls.Add(_notes);
		Controls.Add(_sample);
		Controls.Add(_heading);
		Controls.Add(_picture);
		Size = new System.Drawing.Size(600, 220);
		ResumeLayout(false);
		PerformLayout();
	}
}
