// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Compares resizing and alignment while owning each generated image explicitly.</summary>
public partial class ImageResizeScreen : ApplicationScreen {
	private readonly PictureBoxEx[,] _boxes;
	private Image _original;

	/// <summary>Creates all matrix cells without reading a file or allocating sample images.</summary>
	public ImageResizeScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		_boxes = new[,] {
			{ _pictureBoxEx11, _pictureBoxEx12, _pictureBoxEx13, _pictureBoxEx14, _pictureBoxEx15, _pictureBoxEx16, _pictureBoxEx17, _pictureBoxEx18, _pictureBoxEx19 },
			{ _pictureBoxEx21, _pictureBoxEx22, _pictureBoxEx23, _pictureBoxEx24, _pictureBoxEx25, _pictureBoxEx26, _pictureBoxEx27, _pictureBoxEx28, _pictureBoxEx29 },
			{ _pictureBoxEx31, _pictureBoxEx32, _pictureBoxEx33, _pictureBoxEx34, _pictureBoxEx35, _pictureBoxEx36, _pictureBoxEx37, _pictureBoxEx38, _pictureBoxEx39 },
			{ _pictureBoxEx41, _pictureBoxEx42, _pictureBoxEx43, _pictureBoxEx44, _pictureBoxEx45, _pictureBoxEx46, _pictureBoxEx47, _pictureBoxEx48, _pictureBoxEx49 }
		};
	}

	/// <summary>The preferences declared by this screen type and restored for its exact registration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override ImageResizeScreenSettings ScreenSettings => (ImageResizeScreenSettings)base.ScreenSettings;

	protected override void InitializeUIPrimingData() {
		base.InitializeUIPrimingData();
		if (Tools.Runtime.IsDesignMode || _original != null)
			return;
		Refresh();
		CreateSample();
	}

	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		if (!HasSettings)
			return;
		var values = ScreenSettings;
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
			_widthNumeric.Value = Math.Clamp(values.Width, 24, 256);
			_heightNumeric.Value = Math.Clamp(values.Height, 24, 256);
		}
		RedrawBoxes();
	}

	// Native controls borrow displayed images; this screen owns and disposes all created bitmaps.
	protected override void OnDestroyScreen() {
		ClearImages();
		_original?.Dispose();
		_original = null;
		base.OnDestroyScreen();
	}

	private void LoadClicked(object sender, EventArgs e) {
		try {
			using var source = Image.FromFile(_pathSelectorControl.Path);
			ReplaceSource(new Bitmap(source));
		} catch (Exception error) {
			_status.Text = "Image could not be loaded: " + error.Message;
		}
	}

	private void SampleClicked(object sender, EventArgs e) => CreateSample();

	// A labelled asymmetric sample makes alignment differences visible without requiring a file.
	private void CreateSample() {
		var sample = new Bitmap(240, 140);
		using (var graphics = Graphics.FromImage(sample)) {
			graphics.Clear(Color.AliceBlue);
			using var blue = new SolidBrush(Color.SteelBlue);
			using var orange = new SolidBrush(Color.DarkOrange);
			graphics.FillRectangle(blue, 0, 0, 150, 140);
			graphics.FillEllipse(orange, 155, 20, 70, 70);
			graphics.DrawString("TOP LEFT", SystemFonts.MessageBoxFont, Brushes.White, 8, 8);
			graphics.DrawString("Original 240 × 140", SystemFonts.MessageBoxFont, Brushes.White, 8, 110);
		}
		ReplaceSource(sample);
	}

	private void ReplaceSource(Image source) {
		var previous = _original;
		_original = source;
		previous?.Dispose();
		RedrawBoxes();
	}

	private void DestinationSizeChanged(object sender, EventArgs e) {
		if (_boxes == null)
			return;
		if (!Updating && HasSettings) {
			var values = ScreenSettings;
			values.Width = decimal.ToInt32(_widthNumeric.Value);
			values.Height = decimal.ToInt32(_heightNumeric.Value);
			MarkSettingsChanged();
		}
		RedrawBoxes();
	}

	private void RedrawBoxes() {
		//StretchToFit,
		//CropAndZoomToFit,
		//PreserveAspectRatioWithPadding,
		//PreserveAspectRatioWithoutPadding,
		if (_original == null || _boxes == null)
			return;
		var size = new Size(decimal.ToInt32(_widthNumeric.Value), decimal.ToInt32(_heightNumeric.Value));
		for (var column = 1; column < 10; column++)
			_matrix.ColumnStyles[column].Width = size.Width + 10;
		for (var row = 1; row < 5; row++)
			_matrix.RowStyles[row].Height = size.Height + 10;
		for (var row = 0; row < 4; row++) {
			for (var column = 0; column < 9; column++) {
				var image = _original.Resize(size, (ResizeMethod)(row + 1), (ResizeAlignment)(column + 1), paddingColor: Color.Transparent);
				var box = _boxes[row, column];
				var previous = box.Image;
				box.Image = image;
				box.Size = size;
				previous?.Dispose();
			}
		}
		_status.Text = $"Source {_original.Width} × {_original.Height}; destination {size.Width} × {size.Height}; all 36 combinations shown.";
	}

	private void ClearImages() {
		if (_boxes == null)
			return;
		foreach (var box in _boxes) {
			var image = box.Image;
			box.Image = null;
			image?.Dispose();
		}
	}

	#region Inner Types

	/// <summary>Destination geometry for the image resizing matrix.</summary>
	[UserEditable]
	public class ImageResizeScreenSettings : SettingsObject {
		/// <summary>The output width, from 24 to 256 logical pixels.</summary>
		[DefaultValue(80)]
		[Range(24, 256)]
		public int Width { get; set; } = 80;

		/// <summary>The output height, from 24 to 256 logical pixels.</summary>
		[DefaultValue(80)]
		[Range(24, 256)]
		public int Height { get; set; } = 80;
	}
	#endregion
}
