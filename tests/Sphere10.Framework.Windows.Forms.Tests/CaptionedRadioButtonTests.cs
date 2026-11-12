// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CaptionedRadioButtonTests {
	[Test]
	public void TextFollowsCaptionSeparatorAndDescription() {
		using var radio = new CaptionedRadioButton { Caption = "Per Display", Description = "unique effect per display" };

		Assert.That(radio.Text, Is.EqualTo("Per Display: unique effect per display"));
		radio.CaptionSeparator = " - ";
		Assert.That(radio.Text, Is.EqualTo("Per Display - unique effect per display"));
		radio.Description = string.Empty;
		Assert.That(radio.Text, Is.EqualTo("Per Display"), "Without a description no separator is shown.");
	}

	[Test]
	public void SiblingsStayMutuallyExclusive() {
		using var panel = new Panel();
		var first = new CaptionedRadioButton { Caption = "All Displays", Checked = true };
		var second = new CaptionedRadioButton { Caption = "Per Display" };
		panel.Controls.AddRange(new Control[] { first, second });

		second.Checked = true;

		Assert.That(first.Checked, Is.False);
		Assert.That(second.Checked, Is.True);
	}

	[Test]
	public void PreferredWidthFitsTheBoldCaptionAndTheRegularDescription() {
		using var radio = new CaptionedRadioButton { Caption = "All Displays", Description = "same effect on all displays (faster)", AutoSize = true };
		using var bold = new Font(radio.Font, FontStyle.Bold);
		var caption = TextRenderer.MeasureText("All Displays", bold, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
		var plain = TextRenderer.MeasureText("All Displays", radio.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

		var preferred = radio.GetPreferredSize(Size.Empty);

		Assert.That(caption.Width, Is.GreaterThan(plain.Width), "The caption is measured bold.");
		Assert.That(preferred.Width, Is.GreaterThan(radio.LogicalToDeviceUnits(13) + caption.Width));
		Assert.That(radio.Size, Is.EqualTo(preferred), "AutoSize follows the text.");
	}

	[Test]
	public void AutoSizedHeightIsAStandardRadioButtonsAndMarginsAreTheDefault() {
		using var radio = new CaptionedRadioButton { Caption = "All Displays", Description = "same effect on all displays (faster)", AutoSize = true };
		using var standard = new RadioButton { Text = "All Displays: same effect on all displays (faster)", AutoSize = true };

		Assert.That(radio.Height, Is.EqualTo(standard.GetPreferredSize(Size.Empty).Height), "Stacked buttons keep the designer's spacing.");
		Assert.That(radio.Margin, Is.EqualTo(standard.Margin));
	}

	[Test]
	public void GlyphIsTheStandardRadioButtonsInEveryState() {
		foreach (var (isChecked, enabled) in new[] { (true, true), (false, true), (true, false), (false, false) }) {
			using var form = new Form { BackColor = Color.White };
			var radio = new CaptionedRadioButton { Caption = "All Displays", Description = "same effect", AutoSize = true, Checked = isChecked, Enabled = enabled };
			var standard = new RadioButton { Text = "All Displays", AutoSize = true, Checked = isChecked, Enabled = enabled, Top = 40 };
			form.Controls.AddRange(new Control[] { radio, standard });
			form.CreateControl();
			var glyphWidth = radio.LogicalToDeviceUnits(13);
			using var captioned = Capture(radio);
			using var plain = Capture(standard);

			var height = Math.Min(captioned.Height, plain.Height);
			var differing = 0;
			for (var x = 0; x < glyphWidth; x++)
				for (var y = 0; y < height; y++)
					if (captioned.GetPixel(x, (captioned.Height - height) / 2 + y) != plain.GetPixel(x, (plain.Height - height) / 2 + y))
						differing++;
			Assert.That(differing, Is.Zero, $"checked {isChecked}, enabled {enabled}");
		}
	}

	[Test]
	public void PaintsTheGlyphAndBothTextRuns() {
		using var radio = new CaptionedRadioButton { Caption = "All Displays", Description = "same effect", AutoSize = true, BackColor = Color.White, ForeColor = Color.Black, Checked = true };
		using var bitmap = new Bitmap(radio.Width, radio.Height, PixelFormat.Format32bppArgb);

		radio.DrawToBitmap(bitmap, new Rectangle(Point.Empty, radio.Size));

		var dark = 0;
		for (var x = 0; x < bitmap.Width; x++)
			for (var y = 0; y < bitmap.Height; y++)
				if (bitmap.GetPixel(x, y).GetBrightness() < 0.4f)
					dark++;
		Assert.That(dark, Is.GreaterThan(50), "The glyph and text are drawn.");
	}

	// Plain RGB keeps GDI fills that an ARGB bitmap would drop.
	private static Bitmap Capture(Control control) {
		var bitmap = new Bitmap(control.Width, control.Height, PixelFormat.Format32bppRgb);
		control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
		return bitmap;
	}
}
