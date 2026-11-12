// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class OnScreenNotificationFormTests {
	[Test]
	public void ShowsPassivelyNearTheBottomOfTheScreenAndDismissesItself() {
		using var form = new OnScreenNotificationForm { DisplayDurationMilliseconds = 300 };
		var screen = Screen.PrimaryScreen;
		var foreground = WinAPI.USER32.GetForegroundWindow();

		form.ShowMessage("LUMINA MODE: SMART INVERSION", screen);

		Assert.That(form.Visible, Is.True);
		Assert.That(WinAPI.USER32.IsWindowVisible(form.Handle), Is.True);
		Assert.That(WinAPI.USER32.GetForegroundWindow(), Is.EqualTo(foreground), "The popup must not take focus.");
		Assert.That(WindowsWindowLayering.IsPassiveOverlay(form.Handle), Is.True);
		Assert.That(screen.WorkingArea.Contains(form.Bounds), Is.True);
		Assert.That(form.Bottom, Is.LessThan(screen.WorkingArea.Bottom));
		Assert.That(form.Width, Is.GreaterThan(300), "Long messages widen the popup.");
		var clock = Stopwatch.StartNew();
		while (form.Visible && clock.ElapsedMilliseconds < 3000)
			System.Windows.Forms.Application.DoEvents();
		Assert.That(form.Visible, Is.False, "The popup hides after its display duration.");

		// A second message replaces the text and restarts the timer.
		form.ShowMessage("LUMINA OFF", screen);
		Assert.That(form.Visible, Is.True);
		form.Hide();
		Assert.That(form.Visible, Is.False);
	}

	[Test]
	public void TransientMessagesShowOnTheirScreenAndDisposeThemselvesOnceDismissed() {
		var foreground = WinAPI.USER32.GetForegroundWindow();
		var notifications = Screen.AllScreens.Select((screen, index) => (Screen: screen, Form: OnScreenNotificationForm.ShowTransient((index + 1).ToString(), screen, 200))).ToArray();

		foreach (var (screen, form) in notifications) {
			Assert.That(form.Visible, Is.True);
			Assert.That(screen.WorkingArea.Contains(form.Bounds), Is.True);
			Assert.That(form.DisplayDurationMilliseconds, Is.EqualTo(200));
		}
		Assert.That(WinAPI.USER32.GetForegroundWindow(), Is.EqualTo(foreground), "Transient popups must not take focus.");
		var clock = Stopwatch.StartNew();
		while (notifications.Any(notification => !notification.Form.IsDisposed) && clock.ElapsedMilliseconds < 3000)
			System.Windows.Forms.Application.DoEvents();
		Assert.That(notifications.Select(notification => notification.Form.IsDisposed), Has.All.True);
	}
}
