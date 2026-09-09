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
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationScreenDockingTests {
	private bool _startedFramework;

	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework)
			Sphere10Framework.Instance.Build().Start();
	}

	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GripDropAcceptsOnlyTheTabHeaderBand(bool HasDockedScreen) {
		using var Host = new ApplicationScreenHost { Size = new Size(800, 400), ScreenMode = ScreenMode.MultiView };
		if (HasDockedScreen)
			Host.ShowScreen(new DockingScreen());
		var Detached = new DockingScreen();
		Host.ShowScreen(Detached);
		Host.UndockScreen(Detached);
		_ = Host.Handle;
		var Target = Host.DockTargetBounds;
		Assert.That(Target.IsEmpty, Is.False, "Even an empty workspace needs a header docking target");
		Assert.That(Host.UpdateDockPreview(Detached, Center(Target)), Is.True);
		var ContentLocation = Host.PointToScreen(new Point(Host.Width / 2, Host.Height / 2));
		Assert.That(Host.UpdateDockPreview(Detached, ContentLocation), Is.False, "The content panel must not act as a docking target");
		Assert.That(Host.TabControl.DockPreviewVisible, Is.False);
		Assert.That(Host.CompleteScreenDock(Detached, ContentLocation), Is.False);
		Assert.That(Host.IsScreenUndocked(Detached), Is.True);
		Assert.That(Host.UpdateDockPreview(Detached, new Point(Target.Left - 1, Target.Top)), Is.False);
		Assert.That(Host.UpdateDockPreview(Detached, new Point(Target.Right, Target.Top)), Is.False);
		Assert.That(Host.UpdateDockPreview(Detached, new Point(Target.Left, Target.Top - 1)), Is.False);
		Assert.That(Host.UpdateDockPreview(Detached, new Point(Target.Left, Target.Bottom)), Is.False);
	}

	[TestCase(-1)]
	[TestCase(1)]
	public void CursorOverHeaderCannotDockAWindowWhoseCaptionIsFarAway(int Direction) {
		using var Host = new ApplicationScreenHost { Size = new Size(800, 400), ScreenMode = ScreenMode.MultiView };
		var Detached = new DockingScreen();
		Host.ShowScreen(Detached);
		Host.UndockScreen(Detached);
		_ = Host.Handle;
		var Pointer = Center(Host.DockTargetBounds);
		var Window = (ApplicationScreenForm)Detached.FindForm()!;
		var Caption = new Rectangle(Pointer.X - Window.Width / 2, Pointer.Y - Window.CaptionBounds.Height / 2, Window.Width, Window.CaptionBounds.Height);
		Assert.That(Host.UpdateDockPreview(Detached, Pointer, Caption), Is.True);
		Caption.Offset(0, Direction * Host.Height);
		Assert.That(Host.UpdateDockPreview(Detached, Pointer, Caption), Is.False);
		Assert.That(Host.TabControl.DockPreviewVisible, Is.False);
		Assert.That(Host.CompleteScreenDock(Detached, Pointer, Caption), Is.False);
		Assert.That(Host.IsScreenUndocked(Detached), Is.True);
	}

	[Test]
	public void CaptionProximityUsesWindowGeometryRatherThanThePointerHeight() {
		using var Host = new ApplicationScreenHost { Size = new Size(800, 400), ScreenMode = ScreenMode.MultiView };
		var Detached = new DockingScreen();
		Host.ShowScreen(Detached);
		Host.UndockScreen(Detached);
		_ = Host.Handle;
		var Target = Host.DockTargetBounds;
		var CenterLocation = Center(Target);
		var Window = (ApplicationScreenForm)Detached.FindForm()!;
		var Caption = new Rectangle(CenterLocation.X - Window.Width / 2, CenterLocation.Y - Window.CaptionBounds.Height / 2, Window.Width, Window.CaptionBounds.Height);
		var Pointer = new Point(CenterLocation.X, Target.Bottom + Target.Height);
		Assert.That(Host.UpdateDockPreview(Detached, Pointer), Is.False);
		Assert.That(Host.UpdateDockPreview(Detached, Pointer, Caption), Is.True);
		Assert.That(Host.CompleteScreenDock(Detached, Pointer, Caption), Is.True);
		Assert.That(Host.ActiveScreen, Is.SameAs(Detached));
		Assert.That(Window.IsDisposed, Is.True);
		Assert.That(Host.TabControl.DockPreviewVisible, Is.False);
	}

	[Test]
	public void SingleViewAndForeignScreensHaveNoDockTarget() {
		using var Host = new ApplicationScreenHost { Size = new Size(800, 400) };
		using var ForeignScreen = new DockingScreen();
		Assert.That(Host.DockTargetBounds, Is.EqualTo(Rectangle.Empty));
		Host.ScreenMode = ScreenMode.MultiView;
		_ = Host.Handle;
		Assert.That(Host.UpdateDockPreview(ForeignScreen, Center(Host.DockTargetBounds)), Is.False);
		Assert.That(Host.TabControl.DockPreviewVisible, Is.False);
	}

	[Test]
	public void DockTargetTracksTheActualHeaderHeight() {
		using var LargerFont = new Font(SystemFonts.DefaultFont.FontFamily, SystemFonts.DefaultFont.Size * 2);
		using var Host = new ApplicationScreenHost { Size = new Size(800, 600), ScreenMode = ScreenMode.MultiView };
		Host.ShowScreen(new DockingScreen());
		_ = Host.Handle;
		_ = Host.TabControl.Handle;
		var OriginalTarget = Host.DockTargetBounds;
		Host.TabControl.Font = LargerFont;
		var LargerTarget = Host.DockTargetBounds;
		Assert.That(LargerTarget.Height, Is.GreaterThan(OriginalTarget.Height));
		Assert.That(LargerTarget.Contains(Host.PointToScreen(new Point(Host.Width / 2, Host.Height / 2))), Is.False);
	}

	[TestCase(1.0)]
	[TestCase(1.5)]
	[TestCase(2.0)]
	public void DetachedWindowUsesMonitorDpiAndPreservesExistingScreenScale(double ScreenScale) {
		using var Host = new ApplicationScreenHost();
		using var Screen = new DockingScreen { Title = "DPI startup" };
		var ExistingButton = new Button {
			Size = new Size((int)(120 * ScreenScale), (int)(40 * ScreenScale)),
			Location = new Point(20, 30),
			Padding = new Padding((int)(8 * ScreenScale))
		};
		Screen.Controls.Add(ExistingButton);
		var OriginalButtonBounds = ExistingButton.Bounds;
		var OriginalButtonPadding = ExistingButton.Padding;
		using var Window = new ApplicationScreenForm(Host, Screen);
		var Dpi = Window.DeviceDpi;
		Assert.That(Window.AutoScaleDimensions, Is.EqualTo(new SizeF(Dpi, Dpi)));
		Assert.That(Window.ClientSize, Is.EqualTo(Window.LogicalToDeviceUnits(new Size(900, 650))),
			"Default detached-window dimensions are logical units and must scale on initial creation");
		Assert.That(ExistingButton.Bounds, Is.EqualTo(OriginalButtonBounds), "Reparenting an existing screen must not apply the window's design scaling again");
		Assert.That(ExistingButton.Padding, Is.EqualTo(OriginalButtonPadding));
	}

	private static Point Center(Rectangle Bounds) => new(Bounds.Left + Bounds.Width / 2, Bounds.Top + Bounds.Height / 2);

	private class DockingScreen : ApplicationScreen {
		public DockingScreen() => ActivationMode = ScreenActivationMode.MultiInstance;
	}
}
