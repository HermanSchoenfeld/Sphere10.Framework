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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class WindowTitleBarButtonsTests {
	[Test]
	public void ButtonSitsBesideTheCaptionButtonsFollowsItsWindowAndReportsItsClick() {
		var screen = Screen.PrimaryScreen.WorkingArea;
		using var image = new Bitmap(16, 16);
		using var other = new Bitmap(16, 16);
		using var window = new FixtureWindow { Text = "Title-bar button fixture", StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(screen.Left + 80, screen.Top + 80, 520, 240) };
		window.Show();
		window.Update();
		using var buttons = new FixtureButtons(window.Handle) { IncludeOwnWindows = true, Image = image, ToolTip = "Fixture" };
		var attached = new List<WindowsWindowInfo>();
		var detached = new List<WindowsWindowInfo>();
		var clicks = new List<WindowTitleBarButtonClick>();
		var failures = new List<Exception>();
		buttons.Attached += attached.Add;
		buttons.Detached += detached.Add;
		buttons.Clicked += clicks.Add;
		buttons.Failed += failures.Add;

		// Only the window the subclass accepts gets a button, although every application window on the desktop is eligible.
		buttons.Start();
		Assert.That(buttons.IsStarted, Is.True);
		PumpUntil(() => attached.Count == 1, "The fixture window must get a button.");
		Assert.That(attached[0].Handle, Is.EqualTo(window.Handle));
		Assert.That(buttons.Windows, Is.EqualTo(attached));

		// The button is a square inside the title bar, in the right-hand half, left of the caption buttons.
		Assert.That(buttons.TryGetBounds(attached[0], out var bounds), Is.True);
		var frame = Frame(window);
		Assert.That(bounds.Width, Is.EqualTo(bounds.Height));
		Assert.That(bounds.Width, Is.GreaterThanOrEqualTo(16));
		Assert.That(frame.Contains(bounds), Is.True, frame + " must contain " + bounds);
		Assert.That(bounds.Left, Is.GreaterThan(frame.Left + frame.Width / 2), frame + " holds " + bounds);
		Assert.That(bounds.Bottom, Is.LessThanOrEqualTo(ClientTop(window)), "The button stays out of the window's content.");

		// A window that moves takes its button with it, at the same place in its title bar.
		var offset = new Size(frame.Right - bounds.Left, bounds.Top - frame.Top);
		window.Location = new Point(window.Left + 60, window.Top + 40);
		PumpUntil(() => Frame(window) is var moved && moved != frame && buttons.TryGetBounds(attached[0], out var follower)
			&& new Size(moved.Right - follower.Left, follower.Top - moved.Top) == offset, "The button must follow its window.");

		// Its own image and tool tip replace the shared ones until they are cleared; a window without a button is ignored.
		buttons.SetImage(attached[0], other, "Fixture window");
		buttons.SetImage(attached[0], null);
		Assert.That(() => buttons.SetImage(attached[0] with { Handle = new IntPtr(1) }, other), Throws.Nothing);

		// The button is a window above its own in z-order, and it never takes the keyboard from it.
		Assert.That(buttons.TryGetBounds(attached[0], out bounds), Is.True);
		var button = FindButtonAbove(window);
		Assert.That(button, Is.Not.Null, "The button sits above its window.");
		Assert.That(button.CanSelect, Is.False);

		// A click reports the window and where the button is. The click is dispatched to the button's handlers: what else
		// lies over the developer's desktop decides where real input would land.
		typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(button, new object[] { EventArgs.Empty });
		Assert.That(clicks, Has.Count.EqualTo(1));
		Assert.That(clicks[0].Window, Is.EqualTo(attached[0]));
		Assert.That(clicks[0].Bounds, Is.EqualTo(bounds));

		// A minimized window has no title bar to hold a button; restoring it brings the button back.
		window.WindowState = FormWindowState.Minimized;
		PumpUntil(() => !buttons.TryGetBounds(attached[0], out _), "A minimized window shows no button.");
		Pump(700);
		Assert.That(detached, Is.Empty, "A minimized window keeps its hidden button through later inventories.");
		Assert.That(buttons.Windows, Is.EqualTo(attached));
		window.WindowState = FormWindowState.Normal;
		PumpUntil(() => buttons.TryGetBounds(attached[0], out _), "A restored window shows its button again.");

		// Closing the window removes its button.
		window.Close();
		PumpUntil(() => detached.Count == 1, "A closed window must lose its button.");
		Assert.That(detached[0], Is.EqualTo(attached[0]));
		Assert.That(buttons.Windows, Is.Empty);
		Assert.That(failures, Is.Empty);
		Assert.That(buttons.Hooks, Is.EqualTo(new[] { "OnAttached", "OnClicked", "OnDetached" }), "Each notification runs its template hook once.");

		buttons.Stop();
		Assert.That(buttons.IsStarted, Is.False);
		buttons.Dispose();
		Assert.That(buttons.Start, Throws.TypeOf<ObjectDisposedException>());
	}

	[Test]
	public void TheButtonMovesLeftOfWhatTheTitleBarShowsAndStaysPutWithoutRoom() {
		var screen = Screen.PrimaryScreen.WorkingArea;
		using var window = new FixtureWindow { Text = "Title-bar content fixture", StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(screen.Left + 80, screen.Top + 80, 620, 240) };
		window.Show();
		window.Update();
		using var buttons = new FixtureButtons(window.Handle) { IncludeOwnWindows = true };
		var reads = new List<Rectangle>();
		// The application shows a button where the lightbulb would go, and another just left of it.
		buttons.ReadTitleBar = strip => {
			reads.Add(strip);
			var right = strip.Right - 4;
			return new WindowsTitleBarContent {
				Source = WindowsTitleBarContentSource.Accessibility,
				Occupied = new[] { Rectangle.FromLTRB(right - 60, strip.Top, right, strip.Bottom), Rectangle.FromLTRB(right - 100, strip.Top, right - 64, strip.Bottom) }
			};
		};
		var attached = new List<WindowsWindowInfo>();
		buttons.Attached += attached.Add;
		buttons.Start();
		PumpUntil(() => attached.Count == 1 && reads.Count == 1, "The title bar must be read once the window keeps its size.");
		var strip = reads[0];
		PumpUntil(() => buttons.TryGetBounds(attached[0], out var moved) && moved.Right <= strip.Right - 100, "The button must move left of the application's own buttons.");
		Assert.That(buttons.TryGetBounds(attached[0], out var bounds), Is.True);
		Assert.That(bounds.Right, Is.LessThan(strip.Right - 100));
		Assert.That(bounds.Top, Is.EqualTo(strip.Top), "It keeps its height in the title bar.");

		// Moving the window keeps the spot; resizing it reads the title bar again, and with no room the button returns beside the caption buttons.
		buttons.ReadTitleBar = strip => {
			reads.Add(strip);
			return new WindowsTitleBarContent { Source = WindowsTitleBarContentSource.Accessibility, Occupied = new[] { strip } };
		};
		window.Location = new Point(window.Left + 30, window.Top + 20);
		Pump(800);
		Assert.That(reads, Has.Count.EqualTo(1), "A move without a resize reads nothing.");
		window.Width += 40;
		PumpUntil(() => reads.Count == 2, "A resized title bar is read again.");
		PumpUntil(() => buttons.TryGetBounds(attached[0], out var restored) && restored.Right > reads[1].Right - 30, "Without room the button stays beside the caption buttons.");
		buttons.Stop();
	}

	[Test]
	public void TheMenuOpensBeneathTheButtonOnEveryDisplay() {
		var screen = Screen.PrimaryScreen.WorkingArea;
		using var window = new FixtureWindow { Text = "Title-bar menu fixture", StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(screen.Left + 80, screen.Top + 80, 320, 160) };
		window.Show();
		Assert.That(WindowsWindow.TryGetInfo(window.Handle, out var info), Is.True);
		using var menu = new ContextMenuStrip();
		menu.Items.Add("Off");
		menu.Items.Add("Light entire window");
		foreach (var display in Screen.AllScreens) {
			// A button near the right-hand edge of each display, in physical pixels as the component reports it. On a display whose
			// scale differs from the process's, WinForms alone would put the menu on another display.
			var physical = Tools.Windows.Display.GetBounds(display.DeviceName);
			var button = new Rectangle(physical.Right - 160, physical.Top + 120, 24, 24);
			WindowTitleBarButtons.ShowMenu(new WindowTitleBarButtonClick(info, button), menu);
			PumpUntil(() => menu.Visible, "The menu must open.");
			using (new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2)) {
				Assert.That(WinAPI.USER32.GetWindowRect(menu.Handle, out var rect), Is.True);
				var opened = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
				Assert.That(physical.Contains(opened), Is.True, display.DeviceName + ": " + opened + " must lie on " + physical);
				// A menu that is not per-monitor aware moves in steps of its display's scale, so it lands within a pixel.
				Assert.That(opened.Top, Is.EqualTo(button.Bottom).Within(1), display.DeviceName);
				Assert.That(WinAPI.USER32.GetAncestor(WinAPI.USER32.WindowFromPoint(new WinAPI.POINT { x = button.Left + 10, y = button.Bottom + 20 }), WinAPI.USER32.GA_ROOT),
					Is.EqualTo(menu.Handle), display.DeviceName + ": the menu is what lies beneath the button.");
				Assert.That(opened.Right, Is.LessThanOrEqualTo(physical.Right));
				Assert.That(opened.Left, Is.LessThanOrEqualTo(button.Left), display.DeviceName + ": the menu stays under the button or, at the edge, as near as fits.");
			}
			menu.Close();
			Pump(100);
		}
	}

	[Test]
	public void StoppingRemovesEveryButtonAndTheButtonsCanStartAgain() {
		var screen = Screen.PrimaryScreen.WorkingArea;
		using var window = new FixtureWindow { Text = "Title-bar restart fixture", StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(screen.Left + 120, screen.Top + 120, 420, 200) };
		window.Show();
		window.Update();
		using var buttons = new FixtureButtons(window.Handle) { IncludeOwnWindows = true };
		var attached = 0;
		var detached = 0;
		buttons.Attached += _ => attached++;
		buttons.Detached += _ => detached++;

		buttons.Start();
		buttons.Start();
		PumpUntil(() => attached == 1, "The fixture window must get a button.");
		buttons.Stop();
		Assert.That(detached, Is.EqualTo(1), "Stopping removes the buttons at once.");
		Assert.That(buttons.Windows, Is.Empty);

		buttons.Start();
		PumpUntil(() => attached == 2, "Starting again finds the window again.");
	}

	[Test]
	public void OwnWindowsGetNoButtonUnlessTheyAreIncluded() {
		var screen = Screen.PrimaryScreen.WorkingArea;
		using var window = new FixtureWindow { Text = "Title-bar exclusion fixture", StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(screen.Left + 160, screen.Top + 160, 420, 200) };
		window.Show();
		window.Update();
		using var buttons = new FixtureButtons(window.Handle);
		var attached = 0;
		buttons.Attached += _ => attached++;

		buttons.Start();
		Pump(700);
		Assert.That(attached, Is.Zero, "By default a process puts no buttons on its own windows.");
		Assert.That(buttons.Offered, Does.Not.Contain(window.Handle), "The subclass is asked only about windows that qualify.");
	}

	// The frame as the window manager draws it, in the physical pixels the buttons report.
	private static Rectangle Frame(Form window) {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		Assert.That(WinAPI.USER32.GetWindowRect(window.Handle, out var frame), Is.True);
		return Rectangle.FromLTRB(frame.Left, frame.Top, frame.Right, frame.Bottom);
	}

	// Walks up the z-order from the window: a hidden helper window, such as a tool tip's, may lie between it and its button.
	private static Control FindButtonAbove(Form window) {
		for (var handle = WinAPI.USER32.GetWindow(window.Handle, WinAPI.USER32.GetWindow_Cmd.GW_HWNDPREV); handle != IntPtr.Zero;
			handle = WinAPI.USER32.GetWindow(handle, WinAPI.USER32.GetWindow_Cmd.GW_HWNDPREV))
			if (Control.FromHandle(handle) is Form form && form != window && form.GetType().Name == "WindowTitleBarButtonForm")
				return form;
		return null;
	}

	private static int ClientTop(Form window) {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		return window.PointToScreen(Point.Empty).Y;
	}

	private static void Pump(int milliseconds) {
		var watch = Stopwatch.StartNew();
		while (watch.ElapsedMilliseconds < milliseconds) {
			System.Windows.Forms.Application.DoEvents();
			Thread.Sleep(5);
		}
	}

	private static void PumpUntil(Func<bool> condition, string message) {
		var watch = Stopwatch.StartNew();
		while (!condition()) {
			Assert.That(watch.ElapsedMilliseconds, Is.LessThanOrEqualTo(5000), message);
			System.Windows.Forms.Application.DoEvents();
			Thread.Sleep(5);
		}
	}

	#region Inner Types
	// An ordinary application window that appears without taking the focus from whoever is using the desktop.
	private sealed class FixtureWindow : Form {
		protected override bool ShowWithoutActivation => true;
	}

	// Keeps the fixture's buttons off every other application's windows on the developer's desktop.
	private sealed class FixtureButtons(IntPtr window) : WindowTitleBarButtons {
		public HashSet<IntPtr> Offered { get; } = new();

		/// <summary>What the fixture's title bar holds; by default nothing could be read, which keeps the button where it is.</summary>
		public Func<Rectangle, WindowsTitleBarContent> ReadTitleBar { get; set; } = _ => new WindowsTitleBarContent();

		/// <summary>The template hooks that ran, in order.</summary>
		public List<string> Hooks { get; } = new();

		protected override bool ShouldAttach(WindowsWindowInfo candidate) {
			Offered.Add(candidate.Handle);
			return candidate.Handle == window;
		}

		protected override void OnAttached(WindowsWindowInfo attached) => Hooks.Add(nameof(OnAttached));

		protected override void OnDetached(WindowsWindowInfo detached) => Hooks.Add(nameof(OnDetached));

		protected override void OnClicked(WindowTitleBarButtonClick click) => Hooks.Add(nameof(OnClicked));

		protected override void OnFailed(Exception error) => Hooks.Add(nameof(OnFailed));

		// No helper process is started from the test host.
		protected override Task<WindowsTitleBarContent> ReadTitleBarAsync(WindowsWindowInfo target, Rectangle strip, CancellationToken cancellation)
			=> Task.FromResult(ReadTitleBar(strip));
	}
	#endregion
}
