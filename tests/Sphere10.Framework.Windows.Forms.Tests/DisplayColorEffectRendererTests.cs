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
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[Explicit("Creates native magnifier windows and changes the desktop colour effect. Stop other magnification applications first.")]
[Category("DesktopEffects")]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class DisplayColorEffectRendererTests {
	private SynchronizationContext _previousContext;

	[SetUp]
	public void SetUp() {
		_previousContext = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
	}

	[TearDown]
	public void TearDown() => SynchronizationContext.SetSynchronizationContext(_previousContext);

	[Test]
	public void InvalidRequestsLeaveTheAppliedEffectIntact() {
		using var renderer = new DisplayColorEffectRenderer(SynchronizationContext.Current);
		var display = CreateDisplay("first", ColorMatrices.CreateBrightness(0.1f));
		renderer.Render(new[] { display }, false);
		Assert.That(() => renderer.Render(new[] { display, display with { DeviceName = "FIRST" } }, false), Throws.ArgumentException);
		Assert.That(ReadFullscreen().ApproximatelyEquals(display.Matrix), Is.True);
		Assert.That(() => renderer.Render(new[] { display with { Matrix = null } }, false), Throws.ArgumentException);
		Assert.That(ReadFullscreen().ApproximatelyEquals(display.Matrix), Is.True);
	}

	[Test]
	public void RetargetStartsFromTheAppliedMatrixAfterTheCallerMutatesItsInput() {
		using var renderer = new DisplayColorEffectRenderer(SynchronizationContext.Current) { FadeDuration = TimeSpan.FromSeconds(2) };
		var matrix = ColorMatrices.CreateBrightness(0.1f);
		renderer.Render(new[] { CreateDisplay("first", matrix) }, false);
		matrix.Matrix40 = 0.8f;
		renderer.Render(new[] { CreateDisplay("first", ColorMatrices.CreateBrightness(0.2f)) }, true);
		Pump(TimeSpan.FromMilliseconds(80));
		Assert.That(ReadFullscreen().Matrix40, Is.InRange(0.1f, 0.2f), "The fade starts from the applied value, not the caller's mutated matrix.");
	}

	[Test]
	public void StalledTransitionFinishesOnTheNextAvailableFrame() {
		using var renderer = new DisplayColorEffectRenderer(SynchronizationContext.Current) { FadeDuration = TimeSpan.FromMilliseconds(150) };
		var target = ColorMatrices.CreateBrightness(0.2f);
		renderer.Render(new[] { CreateDisplay("first", ColorMatrices.CreateBrightness(0.1f)) }, false);
		renderer.Render(new[] { CreateDisplay("first", target) }, true);

		// Deliberately prevent UI dispatch until the transition's duration has passed.
		Thread.Sleep(250);
		System.Windows.Forms.Application.DoEvents();
		Assert.That(ReadFullscreen().ApproximatelyEquals(target), Is.True, "One dispatched frame samples elapsed time rather than draining obsolete samples.");
	}

	[TestCase(0)]
	[TestCase(-100)]
	public void NonpositiveDurationAppliesTheTargetImmediately(int milliseconds) {
		using var renderer = new DisplayColorEffectRenderer(SynchronizationContext.Current) { FadeDuration = TimeSpan.FromMilliseconds(milliseconds) };
		var target = ColorMatrices.CreateBrightness(0.2f);
		renderer.Render(new[] { CreateDisplay("first", ColorMatrices.CreateBrightness(0.1f)) }, false);
		renderer.Render(new[] { CreateDisplay("first", target) }, true);
		Assert.That(ReadFullscreen().ApproximatelyEquals(target), Is.True);
	}

	[Test]
	public void CaptureMembershipReusesDeviceIdentityAndReleasesRemovedWindows() {
		using var renderer = new DisplayColorEffectRenderer(SynchronizationContext.Current) { WindowTitle = "Renderer membership test" };
		var first = CreateDisplay("first", ColorMatrices.CreateBrightness(0.1f));
		var second = CreateDisplay("second", ColorMatrices.Identity);
		renderer.Render(new[] { first, second }, false);
		var host = WinAPI.USER32.FindWindowExW(IntPtr.Zero, IntPtr.Zero, "Static", "Renderer membership test first");
		Assert.That(host, Is.Not.EqualTo(IntPtr.Zero));
		Assert.That(renderer.CaptureWindowCount, Is.EqualTo(1));

		// Device names are case insensitive; an explicit request still updates the existing window's effect.
		var replacement = first with { DeviceName = "FIRST", Matrix = ColorMatrices.CreateBrightness(0.2f) };
		renderer.Render(new[] { replacement, second }, false);
		Assert.That(WinAPI.USER32.IsWindow(host), Is.True);
		Assert.That(renderer.CaptureWindowCount, Is.EqualTo(1));
		var magnifier = WinAPI.USER32.FindWindowExW(host, IntPtr.Zero, WinAPI.MAGNIFICATION.WC_MAGNIFIER, null);
		Assert.That(WinAPI.MAGNIFICATION.MagGetColorEffect(magnifier, out var effect), Is.True);
		Assert.That(ColorMatrices.FromArray(effect.ToMatrix()).ApproximatelyEquals(replacement.Matrix), Is.True);

		// Returning to one shared effect destroys the capture host and leaves no stale membership.
		renderer.Render(new[] { second }, false);
		Assert.That(renderer.IsRenderingPerDisplay, Is.False);
		Assert.That(renderer.CaptureWindowCount, Is.Zero);
		Assert.That(WinAPI.USER32.IsWindow(host), Is.False);
	}

	[Test]
	public void PathNotificationsKeepTheirExistingLifecycleBoundary() {
		using var renderer = new DisplayColorEffectRenderer(SynchronizationContext.Current);
		var paths = new List<bool>();
		var captureCounts = new List<int>();
		renderer.RenderingChanged += () => {
			paths.Add(renderer.IsRenderingPerDisplay);
			captureCounts.Add(renderer.CaptureWindowCount);
		};
		renderer.Render(new[] { CreateDisplay("first", ColorMatrices.CreateBrightness(0.1f)), CreateDisplay("second", ColorMatrices.Identity) }, false);
		renderer.Clear(false);
		Assert.That(paths, Is.EqualTo(new[] { true, false }));
		Assert.That(captureCounts, Is.EqualTo(new[] { 0, 0 }), "Path switching is announced before new captures are created and after old captures are disposed.");
	}

	[Test]
	public void EmptyRegionSourceKeepsCompositorAndSurvivesClear() {
		using var renderer = new DisplayColorEffectRenderer(SynchronizationContext.Current);
		var source = new ManualRegionSource();
		renderer.SetRegionSource(source);
		Assert.That(source.Starts, Is.Zero, "Configuration alone does not begin discovery.");
		var display = CreateDisplay("first", ColorMatrices.CreateBrightness(0.1f));
		renderer.Render(new[] { display }, false);
		source.Publish(Array.Empty<WindowsWindowRegion>());
		Assert.That(renderer.IsRenderingPerDisplay, Is.False);
		Assert.That(source.Starts, Is.EqualTo(1));
		renderer.Clear(false);
		Assert.That(source.Stops, Is.EqualTo(1));
		Assert.That(source.Disposals, Is.Zero);
		Assert.That(renderer.RegionSource, Is.SameAs(source));
		renderer.Render(new[] { display }, false);
		Assert.That(source.Starts, Is.EqualTo(2));
		renderer.SetRegionSource(null);
		Assert.That(source.Stops, Is.EqualTo(2));
		Assert.That(source.Disposals, Is.EqualTo(1));
		Assert.That(ReadFullscreen().ApproximatelyEquals(display.Matrix), Is.True);
	}

	[Test]
	public void PerDisplayRenderingUsesCaptureWindowsEvenForOneSharedMatrix() {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		using var renderer = new DisplayColorEffectRenderer(SynchronizationContext.Current);
		Assert.That(renderer.Rendering, Is.EqualTo(DisplayColorEffectRendering.Automatic));
		var displays = new[] { CreateDisplay("first", ColorMatrices.CreateInversion()), CreateDisplay("second", ColorMatrices.CreateInversion()) };

		// The same matrix everywhere takes the full-screen effect, unless per-display rendering was chosen.
		renderer.Render(displays, false);
		Assert.That(renderer.IsRenderingPerDisplay, Is.False);
		renderer.Rendering = DisplayColorEffectRendering.PerDisplay;
		Assert.That(renderer.IsRenderingPerDisplay, Is.False, "The mode applies from the next render request.");
		renderer.Render(displays, false);
		Assert.That(renderer.IsRenderingPerDisplay, Is.True);
		Assert.That(renderer.CaptureWindowCount, Is.EqualTo(2));
		Pump(TimeSpan.FromMilliseconds(200));
		Assert.That(renderer.IsRenderingPerDisplay, Is.True, "Per-display rendering never returns to the full-screen effect by itself.");

		// A display without an effect needs no capture window; automatic rendering takes the full-screen effect back.
		renderer.Render(new[] { displays[0], CreateDisplay("second", ColorMatrices.Identity) }, false);
		Assert.That(renderer.CaptureWindowCount, Is.EqualTo(1));
		renderer.Rendering = DisplayColorEffectRendering.Automatic;
		renderer.Render(displays, false);
		Assert.That(renderer.IsRenderingPerDisplay, Is.False);
		Assert.That(renderer.CaptureWindowCount, Is.Zero);
		Assert.That(() => renderer.Rendering = (DisplayColorEffectRendering)9, Throws.ArgumentException);
	}

	[Test]
	public void EmptyRegionUpdatesKeepCaptureUntilTheSourceIsRemoved() {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		using var owner = new Form { Bounds = CreateDisplay("first", ColorMatrices.Identity).Bounds, ShowInTaskbar = false };
		owner.Show();
		Assert.That(WindowsWindow.TryGetInfo(owner.Handle, out var info), Is.True);
		using var renderer = new DisplayColorEffectRenderer(SynchronizationContext.Current);
		var source = new ManualRegionSource();
		renderer.SetRegionSource(source);
		var display = new DisplayColorEffect("first", owner.Bounds, ColorMatrices.CreateBrightness(0.1f));
		renderer.Render(new[] { display }, false);
		var area = new Rectangle(owner.Left + 20, owner.Top + 30, 40, 30);
		var areas = new[] { area };
		source.Publish(new[] { new WindowsWindowRegion(info, areas) });
		areas[0] = Rectangle.Empty;
		var elapsed = Stopwatch.StartNew();
		while (!renderer.IsRenderingPerDisplay && elapsed.Elapsed < TimeSpan.FromSeconds(3))
			Pump(TimeSpan.FromMilliseconds(10));
		Assert.That(renderer.IsRenderingPerDisplay, Is.True, "Published areas were copied before the producer reused its array.");
		Assert.That(renderer.CaptureWindowCount, Is.EqualTo(1));
		var host = WinAPI.USER32.FindWindowExW(IntPtr.Zero, IntPtr.Zero, "Static", renderer.WindowTitle + " first");
		Assert.That(host, Is.Not.EqualTo(IntPtr.Zero));
		source.Publish(Array.Empty<WindowsWindowRegion>());
		Assert.That(renderer.IsRenderingPerDisplay, Is.True, "An invalidation must not switch the entire desktop's rendering path.");
		Assert.That(WinAPI.USER32.IsWindow(host), Is.True);
		// Reconfigured discovery arrives as a new source, which inherits capture instead of passing through the compositor.
		var replacement = new ManualRegionSource();
		renderer.SetRegionSource(replacement);
		Assert.That(source.Disposals, Is.EqualTo(1));
		Assert.That(replacement.Starts, Is.EqualTo(1));
		Assert.That(renderer.IsRenderingPerDisplay, Is.True, "Replacing the source must not switch the entire desktop's rendering path.");
		Assert.That(WinAPI.USER32.IsWindow(host), Is.True);
		renderer.SetRegionSource(null);
		Assert.That(replacement.Disposals, Is.EqualTo(1));
		Assert.That(renderer.IsRenderingPerDisplay, Is.False);
		Assert.That(renderer.CaptureWindowCount, Is.Zero);
		Assert.That(WinAPI.USER32.IsWindow(host), Is.False);
		Assert.That(ReadFullscreen().ApproximatelyEquals(display.Matrix), Is.True);
	}

	private static DisplayColorEffect CreateDisplay(string deviceName, ColorMatrix matrix) {
		var screen = Screen.PrimaryScreen.Bounds;
		return new DisplayColorEffect(deviceName, new Rectangle(screen.Left + 40, screen.Top + 40, 120, 80), matrix);
	}

	private static ColorMatrix ReadFullscreen() {
		Assert.That(WinAPI.MAGNIFICATION.MagGetFullscreenColorEffect(out var effect), Is.True);
		return ColorMatrices.FromArray(effect.ToMatrix());
	}

	private static void Pump(TimeSpan duration) {
		var elapsed = Stopwatch.StartNew();
		while (elapsed.Elapsed < duration) {
			System.Windows.Forms.Application.DoEvents();
			Thread.Sleep(1);
		}
	}

	#region Inner Types
	private sealed class ManualRegionSource : IDesktopRegionSource {
		public event EventHandlerEx<WindowsWindowRegion[]> Changed;
		public event EventHandlerEx<Exception> Failed { add { } remove { } }

		public int Starts { get; private set; }
		public int Stops { get; private set; }
		public int Disposals { get; private set; }
		public int Synchronizations { get; private set; }

		public void Start() => Starts++;

		public void Stop() => Stops++;

		public void Synchronize() => Synchronizations++;

		public void Dispose() => Disposals++;

		public void Publish(WindowsWindowRegion[] regions) => Changed?.Invoke(regions);
	}
	#endregion
}
