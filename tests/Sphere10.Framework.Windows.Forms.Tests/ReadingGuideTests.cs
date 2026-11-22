// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
[Explicit("Shows a passive guide on each desktop display; run without another native desktop fixture.")]
public class ReadingGuideTests {
	[Test]
	public void PhysicalPositioningIsPassiveAndTrackingRunsOnlyWhileRequested() {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		using var guide = new ReadingGuideForm { FollowPointer = false, Thickness = 7, Band = true, BandOpacity = 0.35 };
		var foreground = WinAPI.USER32.GetForegroundWindow();
		foreach (var screen in Screen.AllScreens) {
			var bounds = Tools.Windows.Display.GetBounds(screen.DeviceName);
			var centre = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
			guide.MoveTo(centre);
			guide.ShowGuide();
			System.Windows.Forms.Application.DoEvents();
			Assert.That(guide.Bounds, Is.EqualTo(new Rectangle(bounds.Left, centre.Y - 3, bounds.Width, 7)));
			Assert.That(WinAPI.USER32.GetForegroundWindow(), Is.EqualTo(foreground));
			guide.MoveVertical(20);
			Assert.That(guide.Top, Is.EqualTo(centre.Y + 17));
			Assert.That(Clock(guide), Is.Null, "A fixed guide creates no tracking clock.");
		}
		guide.FollowPointer = true;
		Assert.That(Clock(guide).IsRunning, Is.True);
		guide.Hide();
		Assert.That(Clock(guide).IsRunning, Is.False);
		guide.Dispose();
		Assert.That(Clock(guide), Is.Null);
	}

	private static DisplayFrameClock Clock(ReadingGuideForm guide)
		=> (DisplayFrameClock)typeof(ReadingGuideForm).GetField("_pointerClock", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(guide);
}
