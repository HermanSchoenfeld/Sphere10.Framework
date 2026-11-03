// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A search editor whose inline action clears the current filter.</summary>
[ToolboxBitmap(typeof(SearchTextBox), "SmallSearchIcon")]
public partial class SearchTextBox : TextBoxEx {
	private const int LogicalIconWidth = 20;

	/// <summary>Creates a search editor with an inline clear action when text is present.</summary>
	public SearchTextBox() {
		InitializeComponent();
		FitPicture();
	}

	/// <summary>The inline image surface, owned by this control; its resource images remain borrowed.</summary>
	public PictureBox PictureBox => _pictureBox;

	/// <summary>Updates the action before notifying consumers of the current search text.</summary>
	protected override void OnTextChanged(EventArgs e) {
		if (_pictureBox != null) {
			_pictureBox.Image = TextLength == 0 ? Resources.SmallSearchIcon : Resources.Cross;
			_pictureBox.Cursor = TextLength == 0 ? Cursors.Default : Cursors.Hand;
			_pictureBox.AccessibleName = TextLength == 0 ? "Search" : "Clear search";
		}
		base.OnTextChanged(e);
	}

	/// <summary>Restores the native text margin whenever Windows recreates the editor.</summary>
	protected override void OnHandleCreated(EventArgs e) {
		base.OnHandleCreated(e);
		FitPicture();
	}

	/// <summary>Keeps the action centered when the editor or its font changes size.</summary>
	protected override void OnResize(EventArgs e) {
		base.OnResize(e);
		FitPicture();
	}

	/// <summary>Scales the action's reserved width with its owning window's DPI.</summary>
	protected override void OnDpiChangedAfterParent(EventArgs e) {
		base.OnDpiChangedAfterParent(e);
		FitPicture();
	}

	// Clear uses the ordinary TextChanged event so every consumer retains its existing search workflow.
	private void PictureBox_Click(object sender, EventArgs e) {
		Clear();
		Focus();
	}

	// Parent font/DPI scaling can resize the child after the editor's own resize callback.
	private void PictureBox_SizeChanged(object sender, EventArgs e) {
		FitPicture();
	}

	// Center within the native client area and reserve the same space for the editor's text.
	private void FitPicture() {
		if (_pictureBox == null)
			return;

		var width = Math.Min(ClientSize.Width, LogicalToDeviceUnits(LogicalIconWidth));
		_pictureBox.Bounds = new Rectangle(ClientSize.Width - width, 0, width, ClientSize.Height);
		if (IsHandleCreated)
			WinAPI.USER32.SendMessage(Handle, WinAPI.WindowMessageFlags.EM_SETMARGINS, (IntPtr)2, (IntPtr)(width << 16));
	}
}

