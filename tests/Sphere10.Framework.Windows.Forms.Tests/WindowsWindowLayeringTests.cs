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
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class WindowsWindowLayeringTests {
	[Test]
	public void PassiveOverlaysAreReportedOnlyWhileAboveTheReferenceWindows() {
		// Off-screen placement keeps these style probes away from the desktop.
		using var host = new OverlayProbeForm(clickThrough: true);
		using var sprite = new OverlayProbeForm(clickThrough: true);
		using var fadingPopup = new OverlayProbeForm(clickThrough: false);
		host.Show();
		sprite.Show();
		fadingPopup.Show();
		var references = new[] { host.Handle };

		Assert.That(WindowsWindowLayering.IsPassiveOverlay(sprite.Handle), Is.True);
		Assert.That(WindowsWindowLayering.IsPassiveOverlay(fadingPopup.Handle), Is.False, "A layered window that still takes input is not a passive overlay.");
		Assert.That(WindowsWindowLayering.GetPassiveOverlaysAbove(references), Is.EqualTo(new[] { sprite.Handle }));

		// Promoting the reference above the sprite must stop reporting it; excluding it would hide it beneath the reference.
		Assert.That(WinAPI.USER32.SetWindowPos(host.Handle, WinAPI.USER32.HWND_TOPMOST, 0, 0, 0, 0, WinAPI.USER32.SetWindowPosFlags.SWP_NOMOVE | WinAPI.USER32.SetWindowPosFlags.SWP_NOSIZE | WinAPI.USER32.SetWindowPosFlags.SWP_NOACTIVATE), Is.True);
		Assert.That(WindowsWindowLayering.GetPassiveOverlaysAbove(references), Is.Empty);

		Assert.That(WinAPI.USER32.SetWindowPos(sprite.Handle, WinAPI.USER32.HWND_TOPMOST, 0, 0, 0, 0, WinAPI.USER32.SetWindowPosFlags.SWP_NOMOVE | WinAPI.USER32.SetWindowPosFlags.SWP_NOSIZE | WinAPI.USER32.SetWindowPosFlags.SWP_NOACTIVATE), Is.True);
		Assert.That(WindowsWindowLayering.GetPassiveOverlaysAbove(references), Is.EqualTo(new[] { sprite.Handle }));
		sprite.Hide();
		Assert.That(WindowsWindowLayering.GetPassiveOverlaysAbove(references), Is.Empty, "Hidden overlays are not captured and need no exclusion.");
	}

	[Test]
	public void WithoutReferenceWindowsNothingIsAbove() {
		Assert.That(WindowsWindowLayering.GetPassiveOverlaysAbove(null), Is.Empty);
		Assert.That(WindowsWindowLayering.GetPassiveOverlaysAbove(Array.Empty<IntPtr>()), Is.Empty);
		Assert.That(WindowsWindowLayering.GetPassiveOverlaysAbove(new[] { IntPtr.Zero }), Is.Empty);
	}

	private sealed class OverlayProbeForm : Form {
		private readonly bool _clickThrough;

		public OverlayProbeForm(bool clickThrough) {
			_clickThrough = clickThrough;
			FormBorderStyle = FormBorderStyle.None;
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.Manual;
			Bounds = new Rectangle(-30000, -30000, 16, 16);
		}

		protected override bool ShowWithoutActivation => true;

		protected override CreateParams CreateParams {
			get {
				var parameters = base.CreateParams;
				parameters.ExStyle |= WinAPI.USER32.WS_EX_TOPMOST | WinAPI.USER32.WS_EX_TOOLWINDOW | WinAPI.USER32.WS_EX_LAYERED | WinAPI.USER32.WS_EX_NOACTIVATE;
				if (_clickThrough)
					parameters.ExStyle |= WinAPI.USER32.WS_EX_TRANSPARENT;
				return parameters;
			}
		}
	}
}
