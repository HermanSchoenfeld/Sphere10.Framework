// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Drawing;
using Sphere10.Framework.Windows.Forms.Drawing;
using Sphere10.Framework.Windows.Forms.Drawing.VisualElements;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Checks drawing geometry and native ownership after extracting general SourceGrid helpers.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class DrawingExtractionTests {
	[TestCase(KnownColor.Black, KnownColor.White)]
	[TestCase(KnownColor.White, KnownColor.Black)]
	[TestCase(KnownColor.MidnightBlue, KnownColor.White)]
	[TestCase(KnownColor.LightSkyBlue, KnownColor.Black)]
	public void ContrastingTextColorSupportsDarkAndLightBackgrounds(KnownColor background, KnownColor expected) {
		Assert.That(Tools.Drawing.GetContrastingTextColor(Color.FromKnownColor(background)), Is.EqualTo(Color.FromKnownColor(expected)));
	}

	[TestCase(ContentAlignment.TopLeft)]
	[TestCase(ContentAlignment.TopCenter)]
	[TestCase(ContentAlignment.TopRight)]
	[TestCase(ContentAlignment.MiddleLeft)]
	[TestCase(ContentAlignment.MiddleCenter)]
	[TestCase(ContentAlignment.MiddleRight)]
	[TestCase(ContentAlignment.BottomLeft)]
	[TestCase(ContentAlignment.BottomCenter)]
	[TestCase(ContentAlignment.BottomRight)]
	public void NativeAlignmentRoundTripsWithoutASecondAlignmentEnum(ContentAlignment alignment) {
		using var format = new StringFormat();
		Tools.Drawing.ApplyContentAlignmentToStringFormat(alignment, format);

		Assert.That(Tools.Drawing.StringFormatToContentAlignment(format), Is.EqualTo(alignment));
	}

	[Test]
	public void FractionalPaddingAndAsymmetricBordersRetainContentAndHitTestingGeometry() {
		var area = new RectangleF(0, 0, 100, 50);
		var padding = new Sphere10.Framework.Drawing.Padding(1.5f, 2.5f, 3.5f, 4.5f);
		var border = new RectangleBorder(
			new BorderLine(Color.Black, 2, DashStyle.Solid, 1),
			new BorderLine(Color.Black, 3, DashStyle.Solid, 2),
			new BorderLine(Color.Black, 4, DashStyle.Solid, 0.5f),
			new BorderLine(Color.Black, 1, DashStyle.Solid, 1.5f));

		Assert.That(padding.GetContentRectangle(area), Is.EqualTo(new RectangleF(1.5f, 3.5f, 96, 42)));
		Assert.That(padding.GetExtent(new SizeF(96, 42)), Is.EqualTo(area.Size));
		Assert.That(border.GetContentRectangle(area), Is.EqualTo(new RectangleF(4.5f, 3, 93, 42)));
		Assert.That(border.GetExtent(new SizeF(93, 42)), Is.EqualTo(area.Size));
		Assert.That(border.GetPointPartType(area, new PointF(2, 10), out var distance), Is.EqualTo(RectanglePartType.LeftBorder));
		Assert.That(distance, Is.EqualTo(2));
		Assert.That(border.GetPointPartType(area, new PointF(20, 1), out distance), Is.EqualTo(RectanglePartType.TopBorder));
		Assert.That(distance, Is.EqualTo(1));
		Assert.That(border.GetPointPartType(area, new PointF(20, 20), out distance), Is.EqualTo(RectanglePartType.ContentArea));
		Assert.That(distance, Is.EqualTo(-1));
		Assert.That(AnchorArea.CalculateArea(area, new SizeF(20, 10), new AnchorArea(ContentAlignment.BottomRight, false)),
			Is.EqualTo(new RectangleF(80, 40, 20, 10)));
	}

	[Test]
	public void FractionalRoundedRectangleHelpersKeepTheirPixelBoundary() {
		using var bitmap = new Bitmap(40, 30);
		using var graphics = Graphics.FromImage(bitmap);
		graphics.Clear(Color.White);
		using var brush = new SolidBrush(Color.Red);
		graphics.FillRoundedRectangle(new RoundedRectangle(new Rectangle(5, 5, 20, 10), 0), brush);

		Assert.That(bitmap.GetPixel(5, 5).ToArgb(), Is.EqualTo(Color.Red.ToArgb()));
		Assert.That(bitmap.GetPixel(23, 13).ToArgb(), Is.EqualTo(Color.Red.ToArgb()));
		Assert.That(bitmap.GetPixel(25, 15).ToArgb(), Is.EqualTo(Color.White.ToArgb()));
		using var path = new RoundedRectangle(new Rectangle(5, 5, 20, 10), 0.25).ToGraphicsPath();
		// Native arc bounds include subpixel floating-point rounding.
		var bounds = path.GetBounds();
		Assert.That(bounds.X, Is.EqualTo(5).Within(0.0001));
		Assert.That(bounds.Y, Is.EqualTo(5).Within(0.0001));
		Assert.That(bounds.Width, Is.EqualTo(20).Within(0.0001));
		Assert.That(bounds.Height, Is.EqualTo(10).Within(0.0001));
	}

	[TestCase(0d)]
	[TestCase(1d)]
	public void RoundedRectangleAcceptsTheInclusiveRoundingBoundaries(double value) {
		var rectangle = new RoundedRectangle(new Rectangle(0, 0, 20, 10), 0.5);

		rectangle.RoundValue = value;

		Assert.That(rectangle.RoundValue, Is.EqualTo(value));
	}

	[TestCase(-0.001d)]
	[TestCase(1.001d)]
	[TestCase(double.NaN)]
	[TestCase(double.NegativeInfinity)]
	[TestCase(double.PositiveInfinity)]
	public void RoundedRectangleRejectsAnInvalidIncomingValueWithoutChangingItsExistingRounding(double value) {
		var rectangle = new RoundedRectangle(new Rectangle(0, 0, 20, 10), 0.5);

		Assert.That(() => rectangle.RoundValue = value, Throws.TypeOf<ApplicationException>());
		Assert.That(rectangle.RoundValue, Is.EqualTo(0.5));
		rectangle.RoundValue = 0.25;
		Assert.That(rectangle.RoundValue, Is.EqualTo(0.25));
	}

	[TestCase(FontStyle.Bold)]
	[TestCase(FontStyle.Italic | FontStyle.Underline)]
	public void RichTextAppendRetainsNativeFormattingAfterItsTemporaryFontIsDisposed(FontStyle style) {
		using var font = new Font(Control.DefaultFont.FontFamily, 12);
		using var editor = new RichTextBoxEx { Width = 240, Font = font, Text = "Before " };
		editor.AppendText("Styled", style);
		editor.Select("Before ".Length, "Styled".Length);
		using var selectedFont = editor.SelectionFont;

		Assert.That(editor.Text, Is.EqualTo("Before Styled"));
		Assert.That(selectedFont.Style, Is.EqualTo(style));
		var measured = editor.MeasureTextBoxContent(font);
		Assert.That(measured.Width, Is.GreaterThan(0));
		Assert.That(measured.Height, Is.GreaterThan(0));
		Assert.That(font.GetHeight(), Is.GreaterThan(0), "Measurement borrows the caller's font.");
		Assert.That(editor.MeasureTextBoxContent(font), Is.EqualTo(measured));
	}

	[Test]
	public void DrawingCachesAndMeasurementLeaveBorrowedGraphicsUsable() {
		using var bitmap = new Bitmap(12, 12);
		using var graphics = Graphics.FromImage(bitmap);
		var cache = new GraphicsCache(graphics, Rectangle.Empty, 1, 1);
		var firstBrush = cache.BrushCache.GetBrush(Color.Red);
		Assert.That(cache.BrushCache.GetBrush(Color.Red), Is.SameAs(firstBrush));
		Assert.That(cache.PenCache.GetPen(Color.Black, 1, DashStyle.Solid),
			Is.SameAs(cache.PenCache.GetPen(Color.Black, 1, DashStyle.Solid)));
		Assert.That(cache.PenCache.GetPen(Color.Black, 2, DashStyle.Solid).Width, Is.EqualTo(2));
		graphics.FillRectangle(cache.BrushCache.GetBrush(Color.Blue), 0, 0, 12, 12);
		cache.Dispose();
		cache.Dispose();

		var measurement = new GraphicsMeasurement(graphics);
		measurement.Dispose();
		graphics.FillRectangle(Brushes.Green, 0, 0, 1, 1);
		Assert.That(bitmap.GetPixel(0, 0).ToArgb(), Is.EqualTo(Color.Green.ToArgb()));
		Assert.That(bitmap.GetPixel(5, 5).ToArgb(), Is.EqualTo(Color.Blue.ToArgb()));
	}

	[Test]
	public void DifferentRichTextViewsRenderIndependentlyAndDisposeTheirNativeSurfaces() {
		using var font = new Font(Control.DefaultFont.FontFamily, 12, FontStyle.Bold);
		var first = new RecordingRichTextVisual {
			Value = RichTextConversion.StringToRichText("First rendered value"),
			ForeColor = Color.Red,
			TextAlignment = ContentAlignment.TopRight,
			Font = font
		};
		var second = new RecordingRichTextVisual {
			Value = RichTextConversion.StringToRichText("Independent second value"),
			TextAlignment = ContentAlignment.MiddleLeft
		};
		using var firstImage = Render(first);
		using var secondImage = Render(second);
		using var firstAgain = Render(first);

		Assert.That(Pixels(firstImage), Is.EqualTo(Pixels(firstAgain)), "A different visual must not alter the next render's formatting.");
		Assert.That(Pixels(firstImage).Any(pixel => Color.FromArgb(pixel).R > Color.FromArgb(pixel).G + 30), Is.True);
		// ClearType can give black glyph edges colored subpixels; inspect the actual native format.
		Assert.That(second.RenderedColors.Single().ToArgb(), Is.EqualTo(Control.DefaultForeColor.ToArgb()));
		Assert.That(Pixels(secondImage).Any(pixel => pixel != Color.White.ToArgb()), Is.True);
		Assert.That(first.RenderedText, Is.All.EqualTo("First rendered value"));
		Assert.That(second.RenderedText.Single(), Is.EqualTo("Independent second value"));
		Assert.That(first.Editors.Concat(second.Editors).All(editor => editor.IsDisposed), Is.True);
		Assert.That(first.Editors[0], Is.Not.SameAs(first.Editors[1]));
	}

	[Test]
	public void RichTextFormattingReachesTheNativeRenderingSurface() {
		var visual = new RecordingRichTextVisual {
			Value = new Sphere10.Framework.Windows.Forms.RichText(@"{\rtf1\ansi\b Styled}")
		};
		using var image = Render(visual);

		Assert.That(visual.RenderedText.Single(), Is.EqualTo("Styled"));
		Assert.That(visual.RenderedBold.Single(), Is.True);
		Assert.That(visual.Editors.Single().IsDisposed, Is.True);
	}

	// Render the real visual into a fixed surface with explicit caller ownership.
	private static Bitmap Render(RichTextGDI visual) {
		var bitmap = new Bitmap(260, 70);
		using var graphics = Graphics.FromImage(bitmap);
		using var cache = new GraphicsCache(graphics);
		graphics.Clear(Color.White);
		visual.Draw(cache, new RectangleF(0, 0, bitmap.Width, bitmap.Height));
		return bitmap;
	}

	// Capture pixel values without depending on encoder or image-file metadata.
	private static int[] Pixels(Bitmap bitmap) {
		var result = new int[bitmap.Width * bitmap.Height];
		for (var y = 0; y < bitmap.Height; y++)
			for (var x = 0; x < bitmap.Width; x++)
				result[y * bitmap.Width + x] = bitmap.GetPixel(x, y).ToArgb();
		return result;
	}

	private sealed class RecordingRichTextVisual : RichTextGDI {
		private readonly List<RichTextBoxEx> _editors = new();
		private readonly List<string> _renderedText = new();
		private readonly List<Color> _renderedColors = new();
		private readonly List<bool> _renderedBold = new();

		public RichTextBoxEx[] Editors => _editors.ToArray();

		public string[] RenderedText => _renderedText.ToArray();

		public Color[] RenderedColors => _renderedColors.ToArray();

		public bool[] RenderedBold => _renderedBold.ToArray();

		protected override RichTextBoxEx CreateRenderControl() {
			var editor = base.CreateRenderControl();
			_editors.Add(editor);
			_renderedText.Add(editor.Text);
			editor.SelectAll();
			_renderedColors.Add(editor.SelectionColor);
			using var selectedFont = editor.SelectionFont;
			_renderedBold.Add(selectedFont?.Bold == true);
			return editor;
		}
	}
}
