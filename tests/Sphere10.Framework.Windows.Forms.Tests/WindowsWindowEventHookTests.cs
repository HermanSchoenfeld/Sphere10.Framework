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
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class WindowsWindowEventHookTests {
	[Test]
	public void RaisesAWindowsShowAndHideOnTheInstallingThreadUntilItIsUninstalled() {
		using var hook = Tools.Windows.Hooks.CreateWindowEventHook(WinAPI.USER32.EVENT_OBJECT_SHOW, WinAPI.USER32.EVENT_OBJECT_HIDE);
		Assert.That(hook.FirstEvent, Is.EqualTo(WinAPI.USER32.EVENT_OBJECT_SHOW));
		Assert.That(hook.LastEvent, Is.EqualTo(WinAPI.USER32.EVENT_OBJECT_HIDE));
		Assert.That(hook.IsInstalled, Is.False, "A created hook is not installed.");
		var installer = Environment.CurrentManagedThreadId;
		var events = new List<WindowsWindowEvent>();
		var threads = new HashSet<int>();
		hook.EventRaised += change => {
			events.Add(change);
			threads.Add(Environment.CurrentManagedThreadId);
		};

		hook.InstallHook();
		Assert.That(hook.IsInstalled, Is.True);
		Assert.That(hook.InstallHook, Throws.InvalidOperationException);

		// The window stays off screen and never takes the focus; its show and hide are still accessible events.
		using var window = new QuietWindow { Text = "Window event fixture", StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(-30000, -30000, 300, 200) };
		window.Show();
		PumpUntil(() => events.Any(change => change.Window == window.Handle && change.EventType == WinAPI.USER32.EVENT_OBJECT_SHOW && change.IsWindowObject), "Showing a window raises its show event.");
		window.Hide();
		PumpUntil(() => events.Any(change => change.Window == window.Handle && change.EventType == WinAPI.USER32.EVENT_OBJECT_HIDE && change.IsWindowObject), "Hiding a window raises its hide event.");
		Assert.That(threads, Is.EquivalentTo(new[] { installer }), "Events are raised on the thread that installed the hook.");
		Assert.That(events.Select(change => change.EventType), Has.All.InRange(hook.FirstEvent, hook.LastEvent));

		hook.UninstallHook();
		Assert.That(hook.IsInstalled, Is.False);
		Assert.That(hook.UninstallHook, Throws.Nothing);
		// Events queued before the hook was removed may still arrive; none follows a change made afterwards.
		Pump(200);
		var seen = events.Count;
		window.Show();
		Pump(300);
		Assert.That(events, Has.Count.EqualTo(seen));
	}

	[Test]
	public void ReinstallsAfterUninstallAndRejectsAnInvertedRange() {
		using var hook = new WindowsWindowEventHook(WinAPI.USER32.EVENT_OBJECT_CREATE, WinAPI.USER32.EVENT_OBJECT_LOCATIONCHANGE);
		hook.InstallHook();
		hook.UninstallHook();
		hook.InstallHook();
		Assert.That(hook.IsInstalled, Is.True);
		hook.Dispose();
		Assert.That(hook.IsInstalled, Is.False, "Disposing removes the hook.");
		Assert.That(() => new WindowsWindowEventHook(WinAPI.USER32.EVENT_OBJECT_HIDE, WinAPI.USER32.EVENT_OBJECT_SHOW), Throws.ArgumentException);
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
	private sealed class QuietWindow : Form {
		protected override bool ShowWithoutActivation => true;
	}
	#endregion
}
