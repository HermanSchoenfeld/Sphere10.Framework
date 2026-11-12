// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>An ordinary designer-editable control, with no dependency on child tables.</summary>
public partial class VisualGridDetailSamplePanel : UserControl {
	public VisualGridDetailSamplePanel() : this(true) {
	}

	public VisualGridDetailSamplePanel(bool showImage) {
		InitializeComponent();
		_picture.Visible = showImage;
		_picture.Image = showImage ? System.Drawing.SystemIcons.Application.ToBitmap() : null;
		_notes.Text = "This ordinary TextBox remains editable while a new row-detail model is prepared. The row-detail owner owns the panel and its image.";
	}

	public void SetContent(string title, int publication) {
		_heading.Text = title;
		_sample.Text = $"Detached model publication {publication}. The same native panel is reused across refreshes.";
	}
}
