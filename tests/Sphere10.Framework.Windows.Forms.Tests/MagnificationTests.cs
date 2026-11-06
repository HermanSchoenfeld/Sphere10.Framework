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
using Sphere10.Framework.Drawing;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[Explicit("Creates native magnifier windows and changes the desktop colour effect. Stop other magnification applications first.")]
[Category("DesktopEffects")]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class MagnificationTests {
	[Test]
	public void FullscreenColorEffectRoundTripsAndResetsOnDisposal() {
		var negative = ColorMatrices.FromArray(new[,] {
			{ -1f, 0f, 0f, 0f, 0f }, { 0f, -1f, 0f, 0f, 0f }, { 0f, 0f, -1f, 0f, 0f }, { 0f, 0f, 0f, 1f, 0f }, { 1f, 1f, 1f, 0f, 1f }
		});
		using (var session = new MagnificationSession()) {
			session.SetFullscreenColorEffect(negative);
			Assert.That(session.GetFullscreenColorEffect().ApproximatelyEquals(negative), Is.True);
			session.ResetFullscreenColorEffect();
			Assert.That(session.GetFullscreenColorEffect().IsIdentity(), Is.True);
		}
	}

	[Test]
	public void MagnifierWindowCreatesRefreshesAndReleasesItsNativeHandles() {
		using var session = new MagnificationSession();
		var screen = Screen.PrimaryScreen.Bounds;
		var region = new Rectangle(screen.Left + 40, screen.Top + 40, 240, 160);
		using var backdrop = new Form { FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, BackColor = Color.White, Bounds = region };
		backdrop.Show();
		var magnifier = new MagnifierWindow(session, region, "Magnifier test");
		try {
			magnifier.ColorEffect = ColorMatrices.CreateContrast(0.5f);
			magnifier.Show();
			Assert.That(magnifier.IsShown, Is.True);
			Assert.That(WinAPI.USER32.IsWindow(magnifier.HostHandle), Is.True);
			Assert.That(WinAPI.USER32.IsWindow(magnifier.MagnifierHandle), Is.True);
			Assert.That(WinAPI.MAGNIFICATION.MagGetColorEffect(magnifier.MagnifierHandle, out var effect), Is.True);
			Assert.That(new System.Drawing.Imaging.ColorMatrix(effect.ToMatrix().ToJaggedArrayForTest()).ApproximatelyEquals(ColorMatrices.CreateContrast(0.5f)), Is.True);
			magnifier.SourceBounds = new Rectangle(region.Left + 10, region.Top + 10, 100, 80);
			magnifier.Refresh();
			Assert.That(WinAPI.MAGNIFICATION.MagGetWindowSource(magnifier.MagnifierHandle, out var source), Is.True);
			Assert.That(Rectangle.FromLTRB(source.Left, source.Top, source.Right, source.Bottom), Is.EqualTo(magnifier.RenderedBounds));
			var host = magnifier.HostHandle;
			magnifier.Hide();
			Assert.That(magnifier.IsShown, Is.False);
			Assert.That(WinAPI.USER32.IsWindow(host), Is.False);
		} finally {
			magnifier.Dispose();
		}
	}

	[Test]
	public void PassiveOverlayAboveTheHostRendersOnceWhenExcluded() {
		// A click-through sprite above the magnifier shows live; capturing it as well paints a stale copy where it was.
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		using var session = new MagnificationSession();
		var screen = Screen.PrimaryScreen.Bounds;
		var region = new Rectangle(screen.Left + 40, screen.Top + 40, 240, 160);
		var home = new Rectangle(region.Left + 60, region.Top + 40, 40, 40);
		var away = new Rectangle(region.Right + 40, region.Top + 40, 40, 40);
		var sample = new Point(home.Left + home.Width / 2, home.Top + home.Height / 2);
		using var backdrop = new Form { FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, BackColor = Color.White, Bounds = region };
		using var sprite = new SpriteForm { BackColor = Color.Red, Bounds = home };
		backdrop.Show();
		var negative = ColorMatrices.FromArray(new[,] {
			{ -1f, 0f, 0f, 0f, 0f }, { 0f, -1f, 0f, 0f, 0f }, { 0f, 0f, -1f, 0f, 0f }, { 0f, 0f, 0f, 1f, 0f }, { 1f, 1f, 1f, 0f, 1f }
		});
		var magnifier = new MagnifierWindow(session, region, "Overlay test") { ColorEffect = negative };
		try {
			magnifier.Show();
			sprite.Show();
			WinAPI.USER32.SetWindowPos(sprite.Handle, WinAPI.USER32.HWND_TOPMOST, home.X, home.Y, home.Width, home.Height, WinAPI.USER32.SetWindowPosFlags.SWP_NOACTIVATE);
			Settle();
			magnifier.Refresh();
			Settle();
			Assert.That(Classify(SampleScreen(sample)), Is.EqualTo("red"), "The live sprite shows above the host.");
			sprite.Bounds = away;
			Settle();
			Assert.That(Classify(SampleScreen(sample)), Is.EqualTo("cyan"), "Without exclusion the capture keeps a stale inverted copy of the sprite.");

			var overlays = WindowsWindowLayering.GetPassiveOverlaysAbove(new[] { magnifier.HostHandle });
			Assert.That(overlays, Does.Contain(sprite.Handle));
			magnifier.ExcludedWindows = overlays;
			sprite.Bounds = home;
			Settle();
			magnifier.Refresh();
			Settle();
			Assert.That(Classify(SampleScreen(sample)), Is.EqualTo("red"), "The excluded sprite still shows live above the host.");
			sprite.Bounds = away;
			Settle();
			Assert.That(Classify(SampleScreen(sample)), Is.EqualTo("black"), "With the sprite excluded only the inverted backdrop remains where it was.");
		} finally {
			magnifier.Dispose();
		}
	}

	private static void Settle() {
		var clock = System.Diagnostics.Stopwatch.StartNew();
		while (clock.ElapsedMilliseconds < 200)
			System.Windows.Forms.Application.DoEvents();
	}

	private static Color SampleScreen(Point point) {
		using var bitmap = new Bitmap(1, 1);
		using var graphics = Graphics.FromImage(bitmap);
		graphics.CopyFromScreen(point.X, point.Y, 0, 0, bitmap.Size);
		return bitmap.GetPixel(0, 0);
	}

	private static string Classify(Color color) {
		if (color.R > 180 && color.G < 80 && color.B < 80)
			return "red";
		if (color.R < 80 && color.G > 180 && color.B > 180)
			return "cyan";
		if (color.R < 80 && color.G < 80 && color.B < 80)
			return "black";
		return "other(" + color.R + "," + color.G + "," + color.B + ")";
	}

	private sealed class SpriteForm : Form {
		public SpriteForm() {
			FormBorderStyle = FormBorderStyle.None;
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.Manual;
		}

		protected override bool ShowWithoutActivation => true;

		protected override CreateParams CreateParams {
			get {
				var parameters = base.CreateParams;
				parameters.ExStyle |= WinAPI.USER32.WS_EX_TOPMOST | WinAPI.USER32.WS_EX_TRANSPARENT | WinAPI.USER32.WS_EX_TOOLWINDOW | WinAPI.USER32.WS_EX_LAYERED | WinAPI.USER32.WS_EX_NOACTIVATE;
				return parameters;
			}
		}

		protected override void OnHandleCreated(EventArgs e) {
			base.OnHandleCreated(e);
			// A layered window only paints once its alpha is declared; the sprite is fully opaque.
			WinAPI.USER32.SetLayeredWindowAttributes(Handle, 0, 255, WinAPI.USER32.LWA_ALPHA);
		}
	}
}

internal static class MagnificationTestExtensions {
	public static float[][] ToJaggedArrayForTest(this float[,] values) {
		var rows = new float[5][];
		for (var row = 0; row < 5; row++) {
			rows[row] = new float[5];
			for (var column = 0; column < 5; column++)
				rows[row][column] = values[row, column];
		}
		return rows;
	}
}
