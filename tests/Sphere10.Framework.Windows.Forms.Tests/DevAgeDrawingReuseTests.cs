// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Checks shared drawing calculations through the imported and existing native consumers.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class DevAgeDrawingReuseTests {

	[TestCase(-1f, 0, 0, 0)]
	[TestCase(-0.5f, 10, 50, 100)]
	[TestCase(0f, 20, 100, 200)]
	[TestCase(0.5f, 137, 177, 227)]
	[TestCase(1f, 255, 255, 255)]
	public void ImportedShadingPreservesAlphaAndItsEstablishedInterpolation(float light, int red, int green, int blue) {
		var source = Color.FromArgb(73, 20, 100, 200);
		var expected = Color.FromArgb(73, red, green, blue);

		Assert.That(Tools.Drawing.CalculateLightDarkColor(source, light), Is.EqualTo(expected));
		Assert.That(source.GetShade(light), Is.EqualTo(expected));
	}

	[TestCase(-1.001f)]
	[TestCase(1.001f)]
	public void ImportedShadingKeepsItsOwnPublicArgumentName(float light) {
		Assert.That(() => Tools.Drawing.CalculateLightDarkColor(Color.Red, light),
			Throws.TypeOf<ArgumentException>().With.Property(nameof(ArgumentException.ParamName)).EqualTo("light"));
	}

	[Test]
	public void UnchangedShadingRetainsNamedAndEmptyColorIdentity() {
		Assert.That(Tools.Drawing.CalculateLightDarkColor(Color.Red, 0), Is.EqualTo(Color.Red));
		Assert.That(Tools.Drawing.CalculateLightDarkColor(Color.Red, 0).IsNamedColor, Is.True);
		Assert.That(Tools.Drawing.CalculateLightDarkColor(Color.Empty, 0), Is.EqualTo(Color.Empty));
	}

	[TestCase(ContentAlignment.TopLeft, TextImageRelation.Overlay, 4, 4)]
	[TestCase(ContentAlignment.TopCenter, TextImageRelation.Overlay, 36, 4)]
	[TestCase(ContentAlignment.TopRight, TextImageRelation.Overlay, 68, 4)]
	[TestCase(ContentAlignment.MiddleLeft, TextImageRelation.Overlay, 4, 17)]
	[TestCase(ContentAlignment.MiddleCenter, TextImageRelation.Overlay, 36, 17)]
	[TestCase(ContentAlignment.MiddleRight, TextImageRelation.Overlay, 68, 17)]
	[TestCase(ContentAlignment.BottomLeft, TextImageRelation.Overlay, 4, 30)]
	[TestCase(ContentAlignment.BottomCenter, TextImageRelation.Overlay, 36, 30)]
	[TestCase(ContentAlignment.BottomRight, TextImageRelation.Overlay, 68, 30)]
	[TestCase(ContentAlignment.TopLeft, TextImageRelation.ImageAboveText, 4, 4)]
	[TestCase(ContentAlignment.TopCenter, TextImageRelation.ImageAboveText, 36, 4)]
	[TestCase(ContentAlignment.TopRight, TextImageRelation.ImageAboveText, 68, 4)]
	[TestCase(ContentAlignment.MiddleLeft, TextImageRelation.ImageAboveText, 4, 12)]
	[TestCase(ContentAlignment.MiddleCenter, TextImageRelation.ImageAboveText, 36, 12)]
	[TestCase(ContentAlignment.MiddleRight, TextImageRelation.ImageAboveText, 68, 12)]
	[TestCase(ContentAlignment.BottomLeft, TextImageRelation.ImageAboveText, 4, 30)]
	[TestCase(ContentAlignment.BottomCenter, TextImageRelation.ImageAboveText, 36, 30)]
	[TestCase(ContentAlignment.BottomRight, TextImageRelation.ImageAboveText, 68, 30)]
	[TestCase(ContentAlignment.TopLeft, TextImageRelation.ImageBeforeText, 4, 4)]
	[TestCase(ContentAlignment.TopCenter, TextImageRelation.ImageBeforeText, 25, 4)]
	[TestCase(ContentAlignment.TopRight, TextImageRelation.ImageBeforeText, 68, 4)]
	[TestCase(ContentAlignment.MiddleLeft, TextImageRelation.ImageBeforeText, 4, 17)]
	[TestCase(ContentAlignment.MiddleCenter, TextImageRelation.ImageBeforeText, 25, 17)]
	[TestCase(ContentAlignment.MiddleRight, TextImageRelation.ImageBeforeText, 68, 17)]
	[TestCase(ContentAlignment.BottomLeft, TextImageRelation.ImageBeforeText, 4, 30)]
	[TestCase(ContentAlignment.BottomCenter, TextImageRelation.ImageBeforeText, 25, 30)]
	[TestCase(ContentAlignment.BottomRight, TextImageRelation.ImageBeforeText, 68, 30)]
	public void SplitButtonPaintingRetainsImagePlacementAcrossSharedAlignmentClassifiers(ContentAlignment alignment,
		TextImageRelation relation, int x, int y) {
		var color = Color.FromArgb(3, 211, 97);
		using var image = CreateImage(10, 6, color);
		using var button = new SplitButton {
			AutoSize = false,
			Size = new Size(100, 40),
			ShowSplit = true,
			Text = string.Empty,
			Image = image,
			ImageAlign = alignment,
			TextAlign = alignment,
			TextImageRelation = relation
		};
		using var rendered = new Bitmap(button.Width, button.Height);

		button.DrawToBitmap(rendered, button.ClientRectangle);

		Assert.That(FindColorBounds(rendered, color), Is.EqualTo(new Rectangle(x, y, image.Width, image.Height)));
	}

	[TestCase(ContentAlignment.TopLeft, 0, 0)]
	[TestCase(ContentAlignment.TopCenter, 45, 0)]
	[TestCase(ContentAlignment.TopRight, 90, 0)]
	[TestCase(ContentAlignment.MiddleLeft, 0, 37)]
	[TestCase(ContentAlignment.MiddleCenter, 45, 37)]
	[TestCase(ContentAlignment.MiddleRight, 90, 37)]
	[TestCase(ContentAlignment.BottomLeft, 0, 74)]
	[TestCase(ContentAlignment.BottomCenter, 45, 74)]
	[TestCase(ContentAlignment.BottomRight, 90, 74)]
	public void TaskPanePaintingRetainsWatermarkPlacement(ContentAlignment alignment, int x, int y) {
		var color = Color.FromArgb(3, 211, 97);
		using var image = CreateImage(10, 6, color);
		using var pane = new TaskPane { Size = new Size(100, 80) };
		pane.CustomSettings.Watermark = image;
		pane.CustomSettings.WatermarkAlignment = alignment;
		using var rendered = new Bitmap(pane.Width, pane.Height);

		pane.DrawToBitmap(rendered, pane.ClientRectangle);

		Assert.That(FindColorBounds(rendered, color), Is.EqualTo(new Rectangle(x, y, image.Width, image.Height)));
	}

	[TestCase(ContentAlignment.TopLeft, StringAlignment.Near)]
	[TestCase(ContentAlignment.TopCenter, StringAlignment.Center)]
	[TestCase(ContentAlignment.TopRight, StringAlignment.Far)]
	[TestCase(ContentAlignment.MiddleLeft, StringAlignment.Near)]
	[TestCase(ContentAlignment.MiddleCenter, StringAlignment.Center)]
	[TestCase(ContentAlignment.MiddleRight, StringAlignment.Far)]
	[TestCase(ContentAlignment.BottomLeft, StringAlignment.Near)]
	[TestCase(ContentAlignment.BottomCenter, StringAlignment.Center)]
	[TestCase(ContentAlignment.BottomRight, StringAlignment.Far)]
	public void TaskItemChangesOnlyItsHorizontalStringFormatAlignment(ContentAlignment alignment, StringAlignment expected) {
		using var item = new TaskItemProbe();
		item.TextAlign = alignment == ContentAlignment.TopLeft ? ContentAlignment.BottomRight : ContentAlignment.TopLeft;
		item.SetLineAlignment(StringAlignment.Far);

		item.TextAlign = alignment;

		Assert.That(item.ReadHorizontalAlignment(), Is.EqualTo(expected));
		Assert.That(item.ReadLineAlignment(), Is.EqualTo(StringAlignment.Far), "The existing task-item text flow owns the vertical axis.");
	}

	// The unique solid image makes the rendered content location independent of text metrics and native themes.
	private static Bitmap CreateImage(int width, int height, Color color) {
		var image = new Bitmap(width, height);
		using var graphics = Graphics.FromImage(image);
		graphics.Clear(color);
		return image;
	}

	// Read the actual rendered content rather than calling the controls' private layout calculations.
	private static Rectangle FindColorBounds(Bitmap image, Color color) {
		var left = image.Width;
		var top = image.Height;
		var right = -1;
		var bottom = -1;
		for (var y = 0; y < image.Height; y++) {
			for (var x = 0; x < image.Width; x++) {
				if (image.GetPixel(x, y).ToArgb() != color.ToArgb())
					continue;
				left = Math.Min(left, x);
				top = Math.Min(top, y);
				right = Math.Max(right, x);
				bottom = Math.Max(bottom, y);
			}
		}
		return right < 0 ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
	}

	private sealed class TaskItemProbe : TaskItem {
		// Configure an existing vertical layout decision before the public text-alignment update.
		public void SetLineAlignment(StringAlignment alignment) => StringFormat.LineAlignment = alignment;

		// Observe the native string format consumed by the task item's paint path.
		public StringAlignment ReadHorizontalAlignment() => StringFormat.Alignment;

		// Verify horizontal classification does not replace the independent vertical setting.
		public StringAlignment ReadLineAlignment() => StringFormat.LineAlignment;
	}
}
