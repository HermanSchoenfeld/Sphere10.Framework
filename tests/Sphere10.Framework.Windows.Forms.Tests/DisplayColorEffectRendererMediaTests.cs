// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[Explicit("Changes desktop colours and samples an owned fixture. Stop other magnification applications first.")]
[Category("DesktopEffects")]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class DisplayColorEffectRendererMediaTests {
	[Test]
	public void MediaPixelsSurviveRefreshesWithoutRecreatingCaptureWindows() {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var previousContext = SynchronizationContext.Current;
		// WinForms shares the marshaling control for this reusable NUnit UI thread; the thread owns its lifetime.
		var context = new WindowsFormsSynchronizationContext();
		using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(context);
		var screen = Tools.Windows.Display.GetBounds(Screen.PrimaryScreen.DeviceName);
		using var owner = new Form {
			Text = "Renderer media pixel fixture", FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false,
			StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(screen.Left + 100, screen.Top + 100, 360, 220),
			BackColor = Color.White
		};
		var mediaColor = Color.FromArgb(60, 130, 220);
		using var media = new Panel { Bounds = new Rectangle(20, 20, 180, 100), BackColor = mediaColor };
		owner.Controls.Add(media);
		owner.Show();
		owner.Update();
		Assert.That(WindowsWindow.TryGetInfo(owner.Handle, out var window), Is.True);
		var mediaBounds = media.RectangleToScreen(media.ClientRectangle);
		var mediaPoint = new Point(mediaBounds.Left + mediaBounds.Width / 2, mediaBounds.Top + mediaBounds.Height / 2);
		var textPoint = owner.PointToScreen(new Point(250, 150));
		using var renderer = new DisplayColorEffectRenderer(context) { WindowTitle = "Media integration capture" };
		var source = new FixtureRegionSource(context, window, mediaBounds);
		var paths = new List<bool>();
		Exception failure = null;
		renderer.RenderingChanged += () => paths.Add(renderer.IsRenderingPerDisplay);
		renderer.Failed += error => failure = error;
		renderer.SetRegionSource(source);
		var displays = Screen.AllScreens.Select(display => new DisplayColorEffect(display.DeviceName,
			Tools.Windows.Display.GetBounds(display.DeviceName), ColorMatrices.CreateInversion())).ToArray();
		renderer.Render(displays, false);
		WaitUntil(() => renderer.IsRenderingPerDisplay && Near(ReadPixel(mediaPoint), mediaColor) && Near(ReadPixel(textPoint), Color.Black),
			"A real source, occlusion mask and renderer must preserve media pixels while inverting the surrounding surface.");
		var hosts = displays.Select(display => WinAPI.USER32.FindWindowExW(IntPtr.Zero, IntPtr.Zero, "Static", renderer.WindowTitle + " " + display.DeviceName)).ToArray();
		Assert.That(hosts, Has.All.Not.EqualTo(IntPtr.Zero));
		for (var iteration = 0; iteration < 4; iteration++) {
			source.InvalidateLayout();
			Pump(TimeSpan.FromMilliseconds(250));
			WaitUntil(() => Near(ReadPixel(mediaPoint), mediaColor), "Refreshed geometry must restore the media mask.");
			Assert.That(Near(ReadPixel(textPoint), Color.Black), Is.True, "Content outside media stays transformed during refresh.");
			Assert.That(hosts.All(WinAPI.USER32.IsWindow), Is.True, "Temporary geometry invalidation must not destroy the display captures.");
		}
		Assert.That(paths, Is.EqualTo(new[] { true }), "Geometry updates must not alternate the global compositor and display captures.");
		Assert.That(failure, Is.Null);
		renderer.SetRegionSource(null);
		Assert.That(renderer.IsRenderingPerDisplay, Is.False);
		Assert.That(hosts.All(handle => !WinAPI.USER32.IsWindow(handle)), Is.True);
	}

	private static Color ReadPixel(Point point) {
		using var pixel = new Bitmap(1, 1);
		using var graphics = Graphics.FromImage(pixel);
		graphics.CopyFromScreen(point, Point.Empty, pixel.Size);
		return pixel.GetPixel(0, 0);
	}

	private static bool Near(Color actual, Color expected)
		=> Math.Abs(actual.R - expected.R) < 12 && Math.Abs(actual.G - expected.G) < 12 && Math.Abs(actual.B - expected.B) < 12;

	private static void WaitUntil(Func<bool> condition, string message) {
		var elapsed = Stopwatch.StartNew();
		while (!condition() && elapsed.Elapsed < TimeSpan.FromSeconds(5))
			Pump(TimeSpan.FromMilliseconds(20));
		Assert.That(condition(), Is.True, message);
	}

	private static void Pump(TimeSpan duration) {
		var elapsed = Stopwatch.StartNew();
		while (elapsed.Elapsed < duration) {
			System.Windows.Forms.Application.DoEvents();
			Thread.Sleep(1);
		}
	}

	#region Inner Types
	// The production source keeps its scheduler, expiry, native hooks and dispatch. Only provider discovery is controlled.
	private sealed class FixtureRegionSource(SynchronizationContext dispatcher, WindowsWindowInfo window, Rectangle bounds)
		: WindowsMediaRegionSource(dispatcher) {
		public void InvalidateLayout() => Invalidate(window.Handle);

		protected override Task<WindowsWindowInfo[]> ReadWindowsAsync(CancellationToken cancellationToken) => Task.FromResult(new[] { window });

		protected override Task<WindowsUiAutomationMediaCapture> DiscoverAsync(WindowsWindowInfo expected, CancellationToken cancellationToken)
			=> CaptureAsync(expected, 0, true, cancellationToken);

		protected override Task<WindowsUiAutomationMediaCapture> CaptureAsync(WindowsWindowInfo expected, int workerIndex, bool rediscover, CancellationToken cancellationToken)
			=> Task.FromResult(new WindowsUiAutomationMediaCapture {
				Window = expected, IsAvailable = true, CapturedAtUtc = DateTime.UtcNow,
				Regions = new[] { new WindowsUiAutomationMediaRegion(WindowsUiAutomationMediaKind.Image, "owned-fixture", bounds) }
			});
	}
	#endregion
}
