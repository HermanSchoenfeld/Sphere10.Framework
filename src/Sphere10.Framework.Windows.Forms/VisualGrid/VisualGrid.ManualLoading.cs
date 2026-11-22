// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

// A loading message the owner shows and hides around its own work, apart from the automatic indicator of a data source read.
public partial class VisualGrid {
	private const int LogicalLoadingGlyphSize = 48;
	private const int LogicalLoadingCardWidth = 420;
	private const int LogicalLoadingSpacing = 10;
	private Panel _manualLoading;
	private LoadingCircle _manualLoadingCircle;
	private PictureBox _manualLoadingImage;
	private Label _manualLoadingTitle;
	private Label _manualLoadingSubtitle;
	private bool _manualLoadingDisablesInput;

	/// <summary>Whether a message shown by <see cref="ShowLoading"/> is on display, independently of any data source read.</summary>
	[Browsable(false)]
	public bool LoadingVisible => _manualLoading is { Visible: true };

	/// <summary>Shows a loading message over the grid until <see cref="HideLoading"/>. It presents only: no data is read, the grid's
	/// Enabled state is unchanged and a repeated call replaces the message.</summary>
	/// <param name="subtitle">Optional detail under the title.</param>
	/// <param name="image">Optional picture shown in place of the animated circle. It is copied, so the caller keeps its own.</param>
	/// <param name="disableInput">Covers the whole grid so its rows cannot be used; false shows a card over the rows and leaves them usable.</param>
	public void ShowLoading(string title, string subtitle = null, Image image = null, bool disableInput = true) {
		Guard.Ensure(!InvokeRequired, "Show a loading message on the grid's UI thread.");
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		EnsureManualLoading();
		_manualLoadingDisablesInput = disableInput;
		_manualLoadingTitle.Text = title ?? string.Empty;
		_manualLoadingSubtitle.Text = subtitle ?? string.Empty;
		_manualLoadingSubtitle.Visible = !string.IsNullOrWhiteSpace(subtitle);
		ReleaseManualLoadingImage();
		if (image != null)
			_manualLoadingImage.Image = new Bitmap(image);
		_manualLoadingImage.Visible = image != null;
		_manualLoadingCircle.Visible = image == null;
		_manualLoadingCircle.Active = image == null;
		ArrangeManualLoading();
		_manualLoading.Visible = true;
		_manualLoading.BringToFront();
	}

	/// <summary>Removes the message shown by <see cref="ShowLoading"/>. An automatic loading indicator of a read in progress stays.</summary>
	public void HideLoading() {
		Guard.Ensure(!InvokeRequired, "Hide a loading message on the grid's UI thread.");
		if (_manualLoading == null || IsDisposed)
			return;
		_manualLoading.Visible = false;
		_manualLoadingCircle.Active = false;
		ReleaseManualLoadingImage();
	}

	private void EnsureManualLoading() {
		if (_manualLoading != null)
			return;
		_manualLoadingCircle = new LoadingCircle { Name = "LoadingCircle", StylePreset = LoadingCircle.StylePresets.MacOSX, BackColor = SystemColors.Window, TabStop = false };
		_manualLoadingImage = new PictureBox { Name = "LoadingImage", SizeMode = PictureBoxSizeMode.Zoom, BackColor = SystemColors.Window, Visible = false };
		_manualLoadingTitle = new Label { Name = "LoadingTitle", AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(Font, FontStyle.Bold), UseMnemonic = false };
		_manualLoadingSubtitle = new Label { Name = "LoadingSubtitle", AutoSize = false, TextAlign = ContentAlignment.TopCenter, UseMnemonic = false, Visible = false };
		_manualLoading = new Panel { Name = "LoadingOverlay", BackColor = SystemColors.Window, Visible = false, AccessibleName = "Loading" };
		_manualLoading.Controls.AddRange(new Control[] { _manualLoadingCircle, _manualLoadingImage, _manualLoadingTitle, _manualLoadingSubtitle });
		_manualLoading.Disposed += (_, _) => {
			ReleaseManualLoadingImage();
			_manualLoadingTitle.Font.Dispose();
		};
		Controls.Add(_manualLoading);
		Resize += (_, _) => ArrangeManualLoading();
	}

	// The picture is this grid's own copy.
	private void ReleaseManualLoadingImage() {
		if (_manualLoadingImage?.Image is not { } image)
			return;
		_manualLoadingImage.Image = null;
		image.Dispose();
	}

	// Laid out in code from logical units, so the card is correct on every display it is shown on.
	private void ArrangeManualLoading() {
		if (_manualLoading == null || IsDisposed)
			return;
		var spacing = LogicalToDeviceUnits(LogicalLoadingSpacing);
		var glyph = LogicalToDeviceUnits(LogicalLoadingGlyphSize);
		var width = Math.Max(glyph, Math.Min(LogicalToDeviceUnits(LogicalLoadingCardWidth), ClientSize.Width - 2 * spacing));
		var titleHeight = _manualLoadingTitle.GetPreferredSize(new Size(width, 0)).Height;
		var subtitleHeight = _manualLoadingSubtitle.Visible ? _manualLoadingSubtitle.GetPreferredSize(new Size(width, 0)).Height : 0;
		var height = glyph + spacing + titleHeight + (subtitleHeight > 0 ? spacing / 2 + subtitleHeight : 0);
		_manualLoading.Bounds = _manualLoadingDisablesInput ? ClientRectangle
			: new Rectangle((ClientSize.Width - width) / 2 - spacing, Math.Max(0, (ClientSize.Height - height) / 2 - spacing), width + 2 * spacing, height + 2 * spacing);
		var left = (_manualLoading.ClientSize.Width - width) / 2;
		var top = Math.Max(spacing, (_manualLoading.ClientSize.Height - height) / 2);
		var glyphBounds = new Rectangle(left + (width - glyph) / 2, top, glyph, glyph);
		_manualLoadingCircle.Bounds = glyphBounds;
		_manualLoadingImage.Bounds = glyphBounds;
		_manualLoadingCircle.InnerCircleRadius = Math.Max(1, glyph / 5);
		_manualLoadingCircle.OuterCircleRadius = Math.Max(1, glyph / 3);
		_manualLoadingCircle.SpokeThickness = Math.Max(1, glyph / 15);
		_manualLoadingTitle.Bounds = new Rectangle(left, glyphBounds.Bottom + spacing, width, titleHeight);
		_manualLoadingSubtitle.Bounds = new Rectangle(left, _manualLoadingTitle.Bottom + spacing / 2, width, subtitleHeight);
	}
}
