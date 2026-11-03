// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

partial class SearchTextBox {
	private System.Windows.Forms.PictureBox _pictureBox;

	// Own the static image surface and its native click wiring on the control's design surface.
	private void InitializeComponent() {
		_pictureBox = new System.Windows.Forms.PictureBox();
		((System.ComponentModel.ISupportInitialize)_pictureBox).BeginInit();
		SuspendLayout();
		//
		// _pictureBox
		//
		_pictureBox.AccessibleName = "Search";
		_pictureBox.BackColor = System.Drawing.Color.Transparent;
		_pictureBox.Cursor = System.Windows.Forms.Cursors.Default;
		_pictureBox.Image = Resources.SmallSearchIcon;
		_pictureBox.Name = "_pictureBox";
		_pictureBox.Size = new System.Drawing.Size(20, 16);
		_pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
		_pictureBox.TabStop = false;
		_pictureBox.Click += PictureBox_Click;
		_pictureBox.SizeChanged += PictureBox_SizeChanged;
		//
		// SearchTextBox
		//
		Controls.Add(_pictureBox);
		PlaceHolderText = "Search";
		((System.ComponentModel.ISupportInitialize)_pictureBox).EndInit();
		ResumeLayout(false);
	}
}
