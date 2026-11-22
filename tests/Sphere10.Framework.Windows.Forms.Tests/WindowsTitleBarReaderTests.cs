// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class WindowsTitleBarReaderTests {
	[Test]
	public void ControlsTheApplicationReportsAreReadFromItsAccessibilityTree() {
		using var window = CreateWindow(out var info);
		// Near the left: WinForms scales a control added to a window of a larger display, which must not push it past the window's edge.
		using var button = new Button { Text = "Own", Bounds = new Rectangle(40, 8, 60, 28) };
		window.Controls.Add(button);
		window.Update();
		var strip = Strip(window, button);

		var content = Read(info, strip);

		Assert.That(content.Source, Is.EqualTo(WindowsTitleBarContentSource.Accessibility), content.Availability);
		var physical = Physical(button);
		Assert.That(content.Occupied.Any(area => area.IntersectsWith(physical)), Is.True, string.Join(" ", content.Occupied) + " must include " + physical);
		Assert.That(content.Occupied.All(area => strip.Contains(area)), Is.True, "Content is clipped to the strip that was read.");
	}

	[Test]
	public void DrawnContentIsReadFromTheWindowsOwnPixelsWhenNothingIsReported() {
		using var window = CreateWindow(out var info);
		var icon = new Rectangle(400, 10, 24, 24);
		window.Paint += (_, e) => {
			e.Graphics.Clear(Color.FromArgb(32, 48, 64));
			e.Graphics.FillRectangle(Brushes.Orange, icon);
		};
		window.Invalidate();
		window.Update();
		var origin = Physical(window);
		var strip = new Rectangle(origin.Left, origin.Top + 8, origin.Width, 28);

		var content = Read(info, strip);

		Assert.That(content.Source, Is.EqualTo(WindowsTitleBarContentSource.Pixels), content.Availability);
		var drawn = new Rectangle(origin.Left + icon.Left, strip.Top, icon.Width, strip.Height);
		Assert.That(content.Occupied.Any(area => area.IntersectsWith(drawn)), Is.True, string.Join(" ", content.Occupied) + " must include " + drawn);
		Assert.That(content.Occupied.Sum(area => area.Width), Is.LessThan(origin.Width / 4), "The plain background is free.");
	}

	// A per-monitor aware window, so its coordinates are physical pixels, shown without taking the focus. Its client area stands in for a title bar.
	private static FixtureWindow CreateWindow(out WindowsWindowInfo info) {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var screen = Tools.Windows.Display.GetBounds(Screen.PrimaryScreen.DeviceName);
		var window = new FixtureWindow { FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual, ShowInTaskbar = false, TopMost = true,
			Bounds = new Rectangle(screen.Left + 120, screen.Top + 160, 480, 60), BackColor = Color.FromArgb(32, 48, 64) };
		window.Show();
		window.Update();
		Assert.That(WindowsWindow.TryGetInfo(window.Handle, out info), Is.True);
		return window;
	}

	private static Rectangle Physical(Control control) {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		Assert.That(WinAPI.USER32.GetWindowRect(control.Handle, out var rect), Is.True);
		return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
	}

	private static Rectangle Strip(Form window, Control inside) {
		var frame = Physical(window);
		var control = Physical(inside);
		return new Rectangle(frame.Left, control.Top, frame.Width, control.Height);
	}

	// The reader blocks on the window's thread, which is this one, so it runs on a worker while this thread keeps pumping.
	private static WindowsTitleBarContent Read(WindowsWindowInfo window, Rectangle strip) {
		var read = Task.Run(() => WindowsTitleBarReader.Read(window, strip, TimeSpan.FromSeconds(2)));
		var watch = Stopwatch.StartNew();
		while (!read.IsCompleted && watch.Elapsed < TimeSpan.FromSeconds(10)) {
			System.Windows.Forms.Application.DoEvents();
			Thread.Sleep(5);
		}
		Assert.That(read.IsCompleted, Is.True, "The read must finish.");
		return read.GetAwaiter().GetResult();
	}

	#region Inner Types
	private sealed class FixtureWindow : Form {
		protected override bool ShowWithoutActivation => true;
	}
	#endregion
}
