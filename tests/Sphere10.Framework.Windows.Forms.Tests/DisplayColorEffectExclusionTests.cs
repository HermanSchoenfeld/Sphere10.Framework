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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[Explicit("Creates native colour-effect windows. Stop every other magnification application first.")]
[Category("DesktopEffects")]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class DisplayColorEffectExclusionTests {
	private SynchronizationContext _previousContext;
	private WindowsFormsSynchronizationContext _context;

	[SetUp]
	public void SetUp() {
		_previousContext = SynchronizationContext.Current;
		_context = new WindowsFormsSynchronizationContext();
		SynchronizationContext.SetSynchronizationContext(_context);
	}

	[TearDown]
	public void TearDown() => SynchronizationContext.SetSynchronizationContext(_previousContext);

	[Test]
	public void WindowExclusionsFollowOcclusionMovementVisibilityAndDisplayDpi() {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		foreach (var screen in Screen.AllScreens) {
			var bounds = new Rectangle(screen.Bounds.Left + 150, screen.Bounds.Top + 150, 360, 240);
			using var backdrop = CreateForm(bounds, Color.White);
			using var selected = CreateForm(new Rectangle(bounds.X + 40, bounds.Y + 30, 200, 150), Color.Red);
			using var foreground = CreateForm(new Rectangle(bounds.X + 150, bounds.Y + 80, 160, 120), Color.Lime);
			backdrop.Show();
			selected.Show();
			foreground.Show();
			var target = ReadWindow(selected);
			using var renderer = new DisplayColorEffectRenderer(_context);
			Exception failure = null;
			renderer.Failed += error => failure = error;
			renderer.SetExclusions(new[] { target }, Array.Empty<string>());
			renderer.Render(new[] { new DisplayColorEffect(screen.DeviceName, bounds, ColorMatrices.CreateInversion()) }, false);
			PumpUntil(() => renderer.IsRenderingPerDisplay);
			Assert.That(renderer.IsRenderingPerDisplay, Is.True, "A visible window exception requires the capture path even for one shared matrix.");
			AssertPixels("selected and occluder", (new Point(bounds.X + 70, bounds.Y + 50), Color.Red), (new Point(bounds.X + 180, bounds.Y + 110), Color.Magenta));
			var host = ReadField<Dictionary<string, MagnifierWindow>>(renderer, "_windows")[screen.DeviceName].HostHandle;
			PlaceBelow(selected, host);
			AssertPixels("selected above occluder", (new Point(bounds.X + 180, bounds.Y + 110), Color.Red));
			PlaceBelow(foreground, host);
			AssertPixels("occluder restored", (new Point(bounds.X + 180, bounds.Y + 110), Color.Magenta));
			selected.Bounds = new Rectangle(bounds.X + 20, bounds.Y + 70, 200, 140);
			AssertPixels("moved", (new Point(bounds.X + 50, bounds.Y + 100), Color.Red), (new Point(bounds.X + 70, bounds.Y + 50), Color.Black));
			selected.Width = 80;
			AssertPixels("resized", (new Point(bounds.X + 50, bounds.Y + 100), Color.Red), (new Point(bounds.X + 130, bounds.Y + 100), Color.Black));
			selected.Width = 200;
			selected.WindowState = FormWindowState.Minimized;
			AssertPixels("minimized", (new Point(bounds.X + 50, bounds.Y + 100), Color.Black));
			selected.WindowState = FormWindowState.Normal;
			PlaceBelow(selected, foreground.Handle);
			AssertPixels("restored", (new Point(bounds.X + 50, bounds.Y + 100), Color.Red));
			foreground.Hide();
			AssertPixels("uncovered", (new Point(bounds.X + 180, bounds.Y + 110), Color.Red));
			selected.Hide();
			AssertPixels("hidden", (new Point(bounds.X + 50, bounds.Y + 100), Color.Black));
			selected.Show();
			AssertPixels("shown", (new Point(bounds.X + 50, bounds.Y + 100), Color.Red));
			selected.Close();
			AssertPixels("closed", (new Point(bounds.X + 50, bounds.Y + 100), Color.Black));
			renderer.Clear(false);
			Assert.That(renderer.CaptureWindowCount, Is.Zero);
			Assert.That(failure, Is.Null);
		}
	}

	[Test]
	public void ApplicationRulesResolveVerifiedPathsAndRemovingRulesRestoresTheCompositor() {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var bounds = new Rectangle(Screen.PrimaryScreen.Bounds.X + 150, Screen.PrimaryScreen.Bounds.Y + 150, 300, 200);
		using var backdrop = CreateForm(bounds, Color.Red);
		using var child = new Button { Bounds = new Rectangle(10, 10, 50, 30) };
		backdrop.Controls.Add(child);
		backdrop.Show();
		Assert.That(WindowsWindow.TryGetInfo(child.Handle, out var childInfo), Is.True);
		var root = Tools.Windows.Windowing.GetTopLevelTargetAsync(childInfo).GetAwaiter().GetResult();
		Assert.That(root.Window.Handle, Is.EqualTo(backdrop.Handle));
		var path = Tools.Windows.Windowing.GetExecutablePathAsync(root.Window).GetAwaiter().GetResult();
		Assert.That(path, Is.EqualTo(Environment.ProcessPath).IgnoreCase);
		var applications = new[] { path };
		using var renderer = new DisplayColorEffectRenderer(_context);
		renderer.SetExclusions(Array.Empty<WindowsWindowInfo>(), applications);
		applications[0] = "C:\\a-different-app.exe";
		var display = new DisplayColorEffect("test", bounds, ColorMatrices.CreateInversion());
		renderer.Render(new[] { display }, false);
		AssertPixels("app rule", (new Point(bounds.X + 150, bounds.Y + 100), Color.Red));
		renderer.SetExclusions(Array.Empty<WindowsWindowInfo>(), Array.Empty<string>());
		renderer.Render(new[] { display }, false);
		Assert.That(renderer.IsRenderingPerDisplay, Is.False);
		Assert.That(renderer.CaptureWindowCount, Is.Zero);
		AssertPixels("compositor restored", (new Point(bounds.X + 150, bounds.Y + 100), Color.Cyan));
	}

	[Test]
	public void ApplicationRulesUseCaptureOnlyWhileOneOfTheirWindowsIsOnScreen() {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var bounds = new Rectangle(Screen.PrimaryScreen.Bounds.X + 150, Screen.PrimaryScreen.Bounds.Y + 150, 300, 200);
		using var window = CreateForm(bounds, Color.Red);
		using var renderer = new DisplayColorEffectRenderer(_context);
		var paths = new List<bool>();
		renderer.RenderingChanged += () => paths.Add(renderer.IsRenderingPerDisplay);
		renderer.SetExclusions(Array.Empty<WindowsWindowInfo>(), new[] { Environment.ProcessPath });
		var display = new DisplayColorEffect("test", bounds, ColorMatrices.CreateInversion());
		renderer.Render(new[] { display }, false);

		// No window of the application is on the screen, so the shared matrix stays with the compositor.
		Pump(TimeSpan.FromMilliseconds(600));
		Assert.That(renderer.IsRenderingPerDisplay, Is.False, "An application rule without a visible window must not cost every display a frame of lag.");
		Assert.That(renderer.CaptureWindowCount, Is.Zero);

		// One of its windows appears: capture takes over and leaves it unchanged.
		window.Show();
		PumpUntil(() => renderer.IsRenderingPerDisplay);
		AssertPixels("application window shown", (new Point(bounds.X + 150, bounds.Y + 100), Color.Red));

		// It goes again: capture is held for a moment, then the compositor takes the displays back without another request.
		window.Hide();
		Pump(TimeSpan.FromMilliseconds(500));
		Assert.That(renderer.IsRenderingPerDisplay, Is.True, "Capture is held while geometry may still come back.");
		var released = Stopwatch.StartNew();
		while (renderer.IsRenderingPerDisplay && released.Elapsed < TimeSpan.FromSeconds(6))
			Pump(TimeSpan.FromMilliseconds(20));
		Assert.That(renderer.IsRenderingPerDisplay, Is.False);
		Assert.That(renderer.CaptureWindowCount, Is.Zero);
		Assert.That(paths, Is.EqualTo(new[] { true, false }));

		// And it comes back when the window does.
		window.Show();
		PumpUntil(() => renderer.IsRenderingPerDisplay);
		AssertPixels("application window shown again", (new Point(bounds.X + 150, bounds.Y + 100), Color.Red));
		renderer.Clear(false);
		Assert.That(renderer.CaptureWindowCount, Is.Zero);
	}

	[Test]
	public void StoreAppRulesMatchTheAppInsideItsFrame() {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var frame = OpenStoreApp("calculator:", out var openedHere);
		try {
			var frameHost = Tools.Windows.Windowing.GetExecutablePathAsync(frame).GetAwaiter().GetResult();
			var application = Tools.Windows.Windowing.GetApplicationExecutablePathAsync(frame).GetAwaiter().GetResult();
			Assert.That(Path.GetFileName(frameHost), Is.EqualTo("ApplicationFrameHost.exe").IgnoreCase, "Every Store app's top-level window belongs to the frame host.");
			Assert.That(application, Is.Not.EqualTo(frameHost).IgnoreCase, "A Store app is identified by its own executable, not by the frame host all Store apps share.");
			var displays = Screen.AllScreens.Select(screen => new DisplayColorEffect(screen.DeviceName, Tools.Windows.Display.GetBounds(screen.DeviceName), ColorMatrices.CreateInversion())).ToArray();

			// A rule for the app, and one for the frame host as older settings may hold, each find the app's visible frame,
			// which is what brings capture in.
			foreach (var rule in new[] { application, frameHost }) {
				using var renderer = new DisplayColorEffectRenderer(_context);
				renderer.SetExclusions(Array.Empty<WindowsWindowInfo>(), new[] { rule });
				renderer.Render(displays, false);
				PumpUntil(() => renderer.IsRenderingPerDisplay);
				renderer.Clear(false);
			}

			// A rule for another application leaves the displays with the compositor.
			using var unrelated = new DisplayColorEffectRenderer(_context);
			unrelated.SetExclusions(Array.Empty<WindowsWindowInfo>(), new[] { "C:\\another-app.exe" });
			unrelated.Render(displays, false);
			Pump(TimeSpan.FromMilliseconds(600));
			Assert.That(unrelated.IsRenderingPerDisplay, Is.False);
			unrelated.Clear(false);
		} finally {
			if (openedHere)
				Tools.Windows.Windowing.PostAsync(frame, 0x0010, UIntPtr.Zero, IntPtr.Zero).GetAwaiter().GetResult();
		}
	}

	[Test]
	public void ReusedProcessIdentityDoesNotLeaveAHole() {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var bounds = new Rectangle(Screen.PrimaryScreen.Bounds.X + 150, Screen.PrimaryScreen.Bounds.Y + 150, 200, 150);
		using var backdrop = CreateForm(bounds, Color.Red);
		backdrop.Show();
		var stale = ReadWindow(backdrop) with { ProcessStartTimeUtc = DateTime.UnixEpoch };
		using var renderer = new DisplayColorEffectRenderer(_context);
		renderer.SetExclusions(new[] { stale }, Array.Empty<string>());
		renderer.Render(new[] { new DisplayColorEffect("test", bounds, ColorMatrices.CreateInversion()) }, false);
		AssertPixels("stale identity", (new Point(bounds.X + 100, bounds.Y + 75), Color.Cyan));
		PumpUntil(() => ReadField<object>(renderer, "_exclusionTracker") == null);
		Assert.That(renderer.IsRenderingPerDisplay, Is.False, "A confirmed process identity mismatch must retire the ineffective rule.");
		Assert.That(ReadField<HashSet<WindowsWindowInfo>>(renderer, "_retiredWindows"), Does.Contain(stale));
	}

	[Test]
	public void WindowClosedBeforeSubscriptionIsRetiredOnItsFirstVerification() {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var bounds = new Rectangle(Screen.PrimaryScreen.Bounds.X + 150, Screen.PrimaryScreen.Bounds.Y + 150, 240, 180);
		using var backdrop = CreateForm(bounds, Color.White);
		using var selected = CreateForm(bounds, Color.Red);
		backdrop.Show();
		selected.Show();
		var rules = new[] { ReadWindow(selected) };
		selected.Close();
		Pump(TimeSpan.FromMilliseconds(100));
		using var renderer = new DisplayColorEffectRenderer(_context);
		var displays = new[] { new DisplayColorEffect("test", bounds, ColorMatrices.CreateInversion()) };
		Exception failure = null;
		renderer.Failed += error => failure = error;
		renderer.SetExclusions(rules, Array.Empty<string>());
		renderer.Render(displays, false);
		PumpUntil(() => ReadField<object>(renderer, "_exclusionTracker") == null);
		Assert.That(renderer.IsRenderingPerDisplay, Is.False, "An HWND lost between picking and subscription cannot force permanent capture.");
		Assert.That(ReadField<HashSet<WindowsWindowInfo>>(renderer, "_retiredWindows"), Does.Contain(rules[0]));
		AssertPixels("closed before subscription", (new Point(bounds.X + 100, bounds.Y + 75), Color.Black));
		renderer.SetExclusions(rules, Array.Empty<string>());
		renderer.Render(displays, false);
		Assert.That(renderer.IsRenderingPerDisplay, Is.False);
		Assert.That(ReadField<object>(renderer, "_exclusionTracker"), Is.Null);
		Assert.That(failure, Is.Null);
		Assert.That(ReadField<MagnificationSession>(renderer, "_session").GetFullscreenColorEffect().ApproximatelyEquals(displays[0].Matrix), Is.True);
		AssertPixels("expired rule reapplied", (new Point(bounds.X + 100, bounds.Y + 75), Color.Black));
	}

	[Test]
	public void PausedRulesRetireClosedWindowsWithoutStartingGeometryReads() {
		using var selected = CreateForm(new Rectangle(150, 150, 100, 100), Color.Red);
		selected.Show();
		var rules = new[] { ReadWindow(selected) };
		using var renderer = new DisplayColorEffectRenderer(_context);
		renderer.SetExclusions(rules, Array.Empty<string>());
		var tracker = ReadField<object>(renderer, "_exclusionTracker");
		var idle = ReadField<object>(tracker, "_completion");
		Assert.That(ReadField<WindowsWindowEventHook>(tracker, "_destroyHook").IsInstalled, Is.True);
		Assert.That(ReadField<WindowsWindowEventHook>(tracker, "_systemHook").IsInstalled, Is.False);
		Assert.That(ReadField<WindowsWindowEventHook>(tracker, "_objectHook").IsInstalled, Is.False);
		selected.Left += 10;
		Pump(TimeSpan.FromMilliseconds(100));
		Assert.That(ReadField<object>(tracker, "_completion"), Is.SameAs(idle), "Inactive window movement must not start geometry or app queries.");
		selected.Close();
		Pump(TimeSpan.FromMilliseconds(100));
		Assert.That(ReadField<HashSet<WindowsWindowInfo>>(renderer, "_retiredWindows"), Does.Contain(rules[0]));
		renderer.SetExclusions(rules, Array.Empty<string>());
		Assert.That(ReadField<object>(renderer, "_exclusionTracker"), Is.Null, "Expired unchanged rules retain their identities without retaining subscriptions.");
		renderer.SetExclusions(rules, new[] { "C:\\another-app.exe" });
		Assert.That(ReadField<WindowsWindowInfo[]>(ReadField<object>(renderer, "_exclusionTracker"), "_windows"), Is.Empty,
			"An unrelated app-rule change cannot revive a closed window.");
		renderer.SetExclusions(Array.Empty<WindowsWindowInfo>(), Array.Empty<string>());
		Assert.That(ReadField<object>(renderer, "_exclusionTracker"), Is.Null, "Removing all rules releases every subscription.");
	}

	[Test]
	public void ClosingTheLastExcludedWindowRestoresSharedRenderingButKeepsDifferentDisplaysCaptured() {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var bounds = new Rectangle(Screen.PrimaryScreen.Bounds.X + 160, Screen.PrimaryScreen.Bounds.Y + 160, 300, 200);
		using var backdrop = CreateForm(bounds, Color.White);
		using var selected = CreateForm(new Rectangle(bounds.X + 30, bounds.Y + 30, 140, 100), Color.Red);
		backdrop.Show();
		selected.Show();
		var rules = new[] { ReadWindow(selected) };
		using var renderer = new DisplayColorEffectRenderer(_context);
		var shared = new[] { new DisplayColorEffect("first", bounds, ColorMatrices.CreateInversion()) };
		renderer.SetExclusions(rules, Array.Empty<string>());
		renderer.Render(shared, false);
		var tracker = ReadField<object>(renderer, "_exclusionTracker");
		AssertPixels("live exclusion", (new Point(bounds.X + 70, bounds.Y + 70), Color.Red));
		selected.Close();
		PumpUntil(() => !renderer.IsRenderingPerDisplay);
		Assert.That(renderer.IsRenderingPerDisplay, Is.False,
			"Closing the last effective exception must restore the cheaper shared compositor without another render request.");
		Assert.That(renderer.CaptureWindowCount, Is.Zero);
		Assert.That(ReadField<object>(renderer, "_exclusionTracker"), Is.Null);
		Assert.That(ReadField<WindowsWindowEventHook>(tracker, "_destroyHook").IsInstalled, Is.False);
		Assert.That(ReadField<WindowsWindowEventHook>(tracker, "_systemHook").IsInstalled, Is.False);
		Assert.That(ReadField<WindowsWindowEventHook>(tracker, "_objectHook").IsInstalled, Is.False);
		AssertPixels("closed exclusion", (new Point(bounds.X + 70, bounds.Y + 70), Color.Black));
		renderer.SetExclusions(rules, Array.Empty<string>());
		renderer.Render(shared, false);
		Assert.That(renderer.IsRenderingPerDisplay, Is.False, "Reapplying configured but expired rules cannot revive capture or the retired HWND.");
		Assert.That(ReadField<object>(renderer, "_exclusionTracker"), Is.Null);

		// Different display matrices still require capture independently of the expired window rule.
		var different = new[] { shared[0], new DisplayColorEffect("second", new Rectangle(bounds.Right + 20, bounds.Y, 100, 100), ColorMatrices.Identity) };
		renderer.Render(different, false);
		Assert.That(renderer.IsRenderingPerDisplay, Is.True);
		Assert.That(renderer.CaptureWindowCount, Is.EqualTo(1));
		Assert.That(ReadField<object>(renderer, "_exclusionTracker"), Is.Null);
		using var another = CreateForm(new Rectangle(bounds.X + 30, bounds.Y + 30, 140, 100), Color.Red);
		another.Show();
		renderer.SetExclusions(new[] { ReadWindow(another) }, Array.Empty<string>());
		renderer.Render(different, false);
		another.Close();
		PumpUntil(() => ReadField<object>(renderer, "_exclusionTracker") == null);
		Assert.That(ReadField<object>(renderer, "_exclusionTracker"), Is.Null);
		Assert.That(renderer.IsRenderingPerDisplay, Is.True, "Closing a window exception cannot discard different display matrices.");
		Assert.That(renderer.CaptureWindowCount, Is.EqualTo(1));
	}

	[Test]
	public void OneWindowKeepsItsExceptionWhileCrossingDisplays() {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var displays = Screen.AllScreens.Select(screen => new DisplayColorEffect(screen.DeviceName,
			new Rectangle(screen.Bounds.X + 160, screen.Bounds.Y + 160, 340, 220), ColorMatrices.CreateInversion())).ToArray();
		using var backdrops = new Disposables(displays.Select(display => {
			var form = CreateForm(display.Bounds, Color.White);
			form.Show();
			return form;
		}).ToArray());
		using var selected = CreateForm(new Rectangle(displays[0].Bounds.X + 30, displays[0].Bounds.Y + 30, 140, 100), Color.Red);
		selected.Show();
		using var renderer = new DisplayColorEffectRenderer(_context);
		renderer.SetExclusions(new[] { ReadWindow(selected) }, Array.Empty<string>());
		renderer.Render(displays, false);
		Point? previous = null;
		foreach (var display in displays) {
			selected.Bounds = new Rectangle(display.Bounds.X + 30, display.Bounds.Y + 30, 140, 100);
			var sample = new Point(display.Bounds.X + 70, display.Bounds.Y + 70);
			var expected = new List<(Point, Color)> { (sample, Color.Red) };
			if (previous.HasValue)
				expected.Add((previous.Value, Color.Black));
			AssertPixels("crossed to " + display.DeviceName, expected.ToArray());
			previous = sample;
		}
	}

	// A visible Store app frame that holds its app's window: one this opens through the protocol, or one that was already open.
	private static WindowsWindowInfo OpenStoreApp(string protocol, out bool openedHere) {
		var before = StoreAppFrames().Select(frame => frame.Handle).ToHashSet();
		openedHere = false;
		try {
			Process.Start(new ProcessStartInfo(protocol) { UseShellExecute = true })?.Dispose();
		} catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException) {
			Assert.Ignore($"No application opens {protocol} here.");
		}
		var waited = Stopwatch.StartNew();
		while (waited.Elapsed < TimeSpan.FromSeconds(15)) {
			Pump(TimeSpan.FromMilliseconds(250));
			var frames = StoreAppFrames();
			var opened = frames.FirstOrDefault(frame => !before.Contains(frame.Handle));
			if (opened != null) {
				openedHere = true;
				return opened;
			}
			if (waited.Elapsed > TimeSpan.FromSeconds(5) && frames.Length > 0)
				return frames[0];
		}
		Assert.Ignore($"No Store app frame appeared for {protocol}.");
		return null;
	}

	private static WindowsWindowInfo[] StoreAppFrames()
		=> Tools.Windows.Windowing.EnumerateAsync(includeProcessDetails: true, includeTitles: false, visibleOnly: true).GetAwaiter().GetResult()
			.Where(window => window.ClassName == "ApplicationFrameWindow")
			.Where(window => !string.Equals(Path.GetFileName(TryRead(() => Tools.Windows.Windowing.GetApplicationExecutablePathAsync(window).GetAwaiter().GetResult())), "ApplicationFrameHost.exe", StringComparison.OrdinalIgnoreCase))
			.ToArray();

	private static string TryRead(Func<string> read) {
		try {
			return read();
		} catch (Exception) {
			return "ApplicationFrameHost.exe";
		}
	}

	private static WindowsWindowInfo ReadWindow(Form form) {
		Assert.That(WindowsWindow.TryGetInfo(form.Handle, out var target), Is.True);
		return target;
	}

	private static Form CreateForm(Rectangle bounds, Color color) => new Form {
		FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual, ShowInTaskbar = false, TopMost = true, Bounds = bounds, BackColor = color
	};

	private static void AssertPixels(string stage, params (Point Point, Color Expected)[] samples) {
		var timeout = Stopwatch.StartNew();
		Color[] actual = Array.Empty<Color>();
		do {
			System.Windows.Forms.Application.DoEvents();
			Thread.Sleep(15);
			actual = samples.Select(sample => ReadPixel(sample.Point)).ToArray();
			if (actual.Select((color, index) => Matches(color, samples[index].Expected)).All(match => match))
				return;
		} while (timeout.Elapsed < TimeSpan.FromSeconds(3));
		Assert.That(false, Is.True, stage + ": expected " + string.Join(", ", samples.Select(sample => sample.Expected)) + "; actual " + string.Join(", ", actual));
	}

	private static Color ReadPixel(Point point) {
		using var bitmap = new Bitmap(1, 1);
		using var graphics = Graphics.FromImage(bitmap);
		graphics.CopyFromScreen(point, Point.Empty, bitmap.Size);
		return bitmap.GetPixel(0, 0);
	}

	private static bool Matches(Color actual, Color expected)
		=> Math.Abs(actual.R - expected.R) < 24 && Math.Abs(actual.G - expected.G) < 24 && Math.Abs(actual.B - expected.B) < 24;

	private static T ReadField<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);

	private static void Pump(TimeSpan duration) {
		var clock = Stopwatch.StartNew();
		while (clock.Elapsed < duration) {
			System.Windows.Forms.Application.DoEvents();
			Thread.Sleep(5);
		}
	}

	private static void PumpUntil(Func<bool> condition) {
		var clock = Stopwatch.StartNew();
		while (!condition() && clock.Elapsed < TimeSpan.FromSeconds(3))
			Pump(TimeSpan.FromMilliseconds(15));
		Assert.That(condition(), Is.True, "The queued desktop change did not complete.");
	}

	private static void PlaceBelow(Form window, IntPtr above) {
		Assert.That(WinAPI.USER32.SetWindowPos(window.Handle, above, 0, 0, 0, 0,
			WinAPI.USER32.SetWindowPosFlags.SWP_NOMOVE | WinAPI.USER32.SetWindowPosFlags.SWP_NOSIZE | WinAPI.USER32.SetWindowPosFlags.SWP_NOACTIVATE), Is.True);
	}
}
