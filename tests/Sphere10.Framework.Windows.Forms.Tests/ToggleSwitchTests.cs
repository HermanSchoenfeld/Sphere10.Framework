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

/// <summary>Exercises the switch's check box behaviour, metrics and painting without showing a window.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ToggleSwitchTests {
	// BM_CLICK reaches ButtonBase's click path, which the mouse and Space also use, without moving the real pointer.
	private const uint ButtonClickMessage = 0x00F5;

	[Test]
	public void ClickWithAutoCheckTogglesAndRaisesCheckedChangedOnce() {
		using var toggle = new ToggleSwitch();
		var changes = 0;
		toggle.CheckedChanged += (_, _) => changes++;
		Click(toggle);
		Assert.That(toggle.Checked, Is.True);
		Assert.That(changes, Is.EqualTo(1));
		Click(toggle);
		Assert.That(toggle.Checked, Is.False);
		Assert.That(changes, Is.EqualTo(2));
	}

	[Test]
	public void ClickWithoutAutoCheckRaisesClickAndKeepsTheState() {
		using var toggle = new ToggleSwitch { AutoCheck = false };
		var clicks = 0;
		var changes = 0;
		toggle.Click += (_, _) => clicks++;
		toggle.CheckedChanged += (_, _) => changes++;
		Click(toggle);
		Assert.That(clicks, Is.EqualTo(1));
		Assert.That(toggle.Checked, Is.False);
		Assert.That(changes, Is.Zero);
	}

	[Test]
	public void PreferredSizeIsTheDefaultTrackAndWidensForTrackTextsAndCaption() {
		using var toggle = new DpiToggleSwitch();
		toggle.SimulateDpi(96);
		Assert.That(toggle.GetPreferredSize(Size.Empty), Is.EqualTo(new Size(44, 22)));

		// The track widens to the longer of its texts and keeps its height; a caption adds its width beside the track.
		toggle.OnText = "AUTOMATIC";
		var withOnText = toggle.GetPreferredSize(Size.Empty);
		Assert.That(withOnText.Width, Is.GreaterThan(44));
		Assert.That(withOnText.Height, Is.EqualTo(22));
		toggle.OffText = "MANUAL OVERRIDE";
		var withLongerOffText = toggle.GetPreferredSize(Size.Empty);
		Assert.That(withLongerOffText.Width, Is.GreaterThan(withOnText.Width));
		toggle.Text = "Lumina";
		Assert.That(toggle.GetPreferredSize(Size.Empty).Width, Is.GreaterThan(withLongerOffText.Width));
	}

	[Test]
	public void PreferredSizeScalesWithTheDpi() {
		using var toggle = new DpiToggleSwitch();
		toggle.SimulateDpi(96);
		toggle.OnText = "ON";
		toggle.OffText = "OFF";
		var withTextsAt96 = toggle.GetPreferredSize(Size.Empty);
		toggle.SimulateDpi(192);
		Assert.That(toggle.GetPreferredSize(Size.Empty).Width, Is.GreaterThan(withTextsAt96.Width));
		toggle.OnText = toggle.OffText = string.Empty;
		Assert.That(toggle.GetPreferredSize(Size.Empty), Is.EqualTo(new Size(88, 44)));
	}

	[Test]
	public void AutoSizeAppliesThePreferredSizeAndFollowsTheTrackTexts() {
		using var parent = new Panel { Size = new Size(400, 100) };
		var toggle = new ToggleSwitch { AutoSize = true, Text = "Enabled" };
		parent.Controls.Add(toggle);
		Assert.That(toggle.Size, Is.EqualTo(toggle.GetPreferredSize(Size.Empty)));
		var width = toggle.Width;
		toggle.OnText = "AUTOMATIC";
		Assert.That(toggle.Size, Is.EqualTo(toggle.GetPreferredSize(Size.Empty)));
		Assert.That(toggle.Width, Is.GreaterThan(width));
	}

	[TestCase(true)]
	[TestCase(false)]
	public void TrackShowsTheStateColorAwayFromTheThumb(bool isChecked) {
		using var toggle = new ToggleSwitch { OnBackColor = Color.FromArgb(0, 120, 215), OffBackColor = Color.FromArgb(200, 40, 40), BackColor = Color.White, Checked = isChecked };
		toggle.Size = toggle.GetPreferredSize(Size.Empty);
		using var bitmap = Render(toggle);

		// The thumb rests at the end of its state, so a point near the opposite end shows only the track.
		var sample = new Point(isChecked ? toggle.Width / 5 : toggle.Width * 4 / 5, toggle.Height / 2);
		var expected = isChecked ? toggle.OnBackColor : toggle.OffBackColor;
		Assert.That(bitmap.GetPixel(sample.X, sample.Y).ToArgb(), Is.EqualTo(expected.ToArgb()));
	}

	[Test]
	public void ExplicitTallerSizeGrowsTheTrackToFillTheHeightAndKeepsItsProportions() {
		using var toggle = new ToggleSwitch { OnBackColor = Color.FromArgb(0, 120, 215), OffBackColor = Color.FromArgb(200, 40, 40), BackColor = Color.White };
		var preferred = toggle.GetPreferredSize(Size.Empty);
		toggle.Size = new Size(preferred.Width * 4, preferred.Height * 2);
		using var bitmap = Render(toggle);

		// The track fills the doubled height, so it doubles its width: near its top edge, away from the thumb, the off color shows where
		// a preferred-size track centred in the control would leave the background.
		var grownTrack = new Rectangle(0, 0, preferred.Width * 2, preferred.Height * 2);
		Assert.That(bitmap.GetPixel(grownTrack.Width * 3 / 4, grownTrack.Height / 8).ToArgb(), Is.EqualTo(toggle.OffBackColor.ToArgb()));

		// Beyond the grown track the background remains.
		Assert.That(bitmap.GetPixel(grownTrack.Right + (toggle.Width - grownTrack.Right) / 2, toggle.Height / 2).ToArgb(), Is.EqualTo(Color.White.ToArgb()));

		// An auto-sized switch keeps its preferred track, centred in the height it is given.
		toggle.AutoSize = true;
		using var autoSized = Render(toggle);
		Assert.That(autoSized.GetPixel(grownTrack.Width * 3 / 4, grownTrack.Height / 8).ToArgb(), Is.EqualTo(Color.White.ToArgb()));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void TransparentSwitchShowsItsParentAtTheCorners(bool gradientParent) {
		var parentColor = Color.FromArgb(30, 160, 60);
		using var parent = gradientParent ? new GradientPanel(parentColor, parentColor, 0) : new Panel { BackColor = parentColor };
		parent.Size = new Size(200, 80);
		var toggle = new ToggleSwitch { BackColor = Color.Transparent, Location = new Point(12, 10) };
		toggle.Size = toggle.GetPreferredSize(Size.Empty);
		parent.Controls.Add(toggle);

		// The parent paints the transparent background through its native client area, as it does on screen.
		parent.CreateControl();
		using var bitmap = Render(toggle);
		foreach (var corner in new[] { new Point(0, 0), new Point(toggle.Width - 1, 0), new Point(0, toggle.Height - 1), new Point(toggle.Width - 1, toggle.Height - 1) })
			Assert.That(bitmap.GetPixel(corner.X, corner.Y).ToArgb(), Is.EqualTo(parentColor.ToArgb()), $"Corner {corner}");
	}

	[Test]
	public void AppearanceAndFlatStyleStayAtTheirDefaults() {
		using var toggle = new ToggleSwitch { Appearance = Appearance.Button, FlatStyle = FlatStyle.System };
		Assert.That(toggle.Appearance, Is.EqualTo(Appearance.Normal));
		Assert.That(toggle.FlatStyle, Is.EqualTo(FlatStyle.Standard));
	}

	[Test]
	public void StateObservationUsesTheCheckBoxProvider() {
		Assert.That(ControlStateEventProviderManager.Instance.TryGetControlStateProvider(typeof(ToggleSwitch), out var provider), Is.True);
		Assert.That(provider, Is.InstanceOf<CommonControlStateEventProvider>());
	}

	private static void Click(ToggleSwitch toggle) => WinAPI.USER32.SendMessage(toggle.Handle, ButtonClickMessage, IntPtr.Zero, IntPtr.Zero);

	// An opaque surface keeps GDI fills, such as a parent's plain back color, which leave no alpha on an ARGB bitmap.
	private static Bitmap Render(Control control) {
		var bitmap = new Bitmap(control.Width, control.Height, PixelFormat.Format32bppRgb);
		control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
		return bitmap;
	}

	#region Inner Types

	// Drives the DPI rescaling a monitor change performs, without moving a window between displays.
	private sealed class DpiToggleSwitch : ToggleSwitch {
		private int _dpi;

		public DpiToggleSwitch() => _dpi = DeviceDpi;

		public void SimulateDpi(int dpi) {
			RescaleConstantsForDpi(_dpi, dpi);
			_dpi = dpi;
		}
	}

	#endregion
}
