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

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class WindowsWindowApplicationTests {
	[Test]
	public void AnOrdinaryWindowShowsTheApplicationOfItsOwnProcess() {
		using var window = new Form {
			Text = "Application path fixture", FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual, ShowInTaskbar = false,
			Bounds = new Rectangle(Screen.PrimaryScreen.WorkingArea.Left + 60, Screen.PrimaryScreen.WorkingArea.Top + 60, 120, 80)
		};
		window.Show();
		Assert.That(WindowsWindow.TryGetInfo(window.Handle, out var info), Is.True);

		var application = Tools.Windows.Windowing.GetApplicationExecutablePathAsync(info).GetAwaiter().GetResult();

		Assert.That(application, Is.EqualTo(Environment.ProcessPath).IgnoreCase);
		Assert.That(application, Is.EqualTo(Tools.Windows.Windowing.GetExecutablePathAsync(info).GetAwaiter().GetResult()), "Only a Store app's frame shows another process's application.");
	}

	[Test]
	public void AWindowThatHasClosedHasNoApplication() {
		var window = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(-2000, -2000, 50, 50) };
		window.Show();
		Assert.That(WindowsWindow.TryGetInfo(window.Handle, out var info), Is.True);
		window.Dispose();

		Assert.That(() => Tools.Windows.Windowing.GetApplicationExecutablePathAsync(info).GetAwaiter().GetResult(), Throws.Exception, "A closed window's identity is not read again.");
	}
}
