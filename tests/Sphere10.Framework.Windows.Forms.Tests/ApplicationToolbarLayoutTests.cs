// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationToolbarLayoutTests {
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

	[TestCase(9F)]
	[TestCase(16F)]
	public void VisibilityAndEnabledChangesKeepMainTabsAndActiveContentAtTheSamePosition(float fontSize) {
		using var font = new Font(SystemFonts.MenuFont.FontFamily, fontSize);
		using var form = new ToolbarMainForm { ScreenMode = ScreenMode.MultiView, Size = new Size(1300, 700) };
		using var screen = new ToolbarScreen();
		form.ApplicationToolbar.Font = screen.ToolBar.Font = font;
		form.ShowScreen(screen);
		form.Show();
		System.Windows.Forms.Application.DoEvents();
		var height = form.ApplicationToolbar.Height;
		var tabTop = form.ScreenHost.TabControl.PointToScreen(Point.Empty).Y;
		var contentTop = screen.Content.PointToScreen(Point.Empty).Y;
		for (var change = 0; change < 8; change++) {
			screen.Interval.Visible = screen.Every.Visible = change % 2 == 0;
			screen.RefreshCommand.Visible = !screen.Interval.Visible;
			screen.RefreshCommand.Enabled = change % 3 == 0;
			System.Windows.Forms.Application.DoEvents();
			Assert.That(form.ApplicationToolbar.Height, Is.EqualTo(height));
			Assert.That(form.ScreenHost.TabControl.PointToScreen(Point.Empty).Y, Is.EqualTo(tabTop));
			Assert.That(screen.Content.PointToScreen(Point.Empty).Y, Is.EqualTo(contentTop));
			ClickNative(screen.Command);
			Assert.That(screen.ClickCount, Is.EqualTo(change + 1), "The unchanged toolbar must still dispatch native mouse messages.");
		}
	}

	[TestCase(9F)]
	[TestCase(16F)]
	public void DetachedScreenToolbarKeepsContentPositionWhenCommandsAreHidden(float fontSize) {
		using var font = new Font(SystemFonts.MenuFont.FontFamily, fontSize);
		using var host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var screen = new ToolbarScreen();
		screen.ToolBar.Font = font;
		Assert.That(host.ShowScreen(screen), Is.True);
		Assert.That(host.UndockScreen(screen), Is.True);
		System.Windows.Forms.Application.DoEvents();
		var detached = screen.FindForm();
		Assert.That(detached, Is.TypeOf<WinFormsApplicationScreenForm>());
		var height = screen.ToolBar.Height;
		var contentTop = screen.Content.PointToScreen(Point.Empty).Y;
		for (var change = 0; change < 6; change++) {
			screen.Interval.Visible = screen.Every.Visible = change % 2 == 0;
			screen.RefreshCommand.Visible = !screen.Interval.Visible;
			System.Windows.Forms.Application.DoEvents();
			Assert.That(screen.ToolBar.Height, Is.EqualTo(height));
			Assert.That(screen.Content.PointToScreen(Point.Empty).Y, Is.EqualTo(contentTop));
			ClickNative(screen.Command);
			Assert.That(screen.ClickCount, Is.EqualTo(change + 1));
		}
		Assert.That(host.DockScreen(screen), Is.True);
	}

	[Test]
	public void LargerHostedControlsAndRuntimeFontChangesFitThenReleaseTheirHeightWhenRemoved() {
		using var form = new Form { Size = new Size(900, 500) };
		using var toolbar = new ToolStrip();
		using var height = new ToolStripStableHeightBehavior(toolbar);
		using var content = new Panel { Dock = DockStyle.Fill };
		using var command = new ToolStripButton("Run");
		toolbar.Items.Add(command);
		form.Controls.Add(content);
		form.Controls.Add(toolbar);
		form.Show();
		System.Windows.Forms.Application.DoEvents();
		var ordinaryHeight = toolbar.Height;
		using var text = new TextBox { Multiline = true, Size = new Size(100, 80) };
		using var editor = new ToolStripControlHost(text) { AutoSize = false, Size = text.Size };
		toolbar.Items.Add(editor);
		System.Windows.Forms.Application.DoEvents();
		Assert.That(toolbar.Height, Is.GreaterThan(ordinaryHeight));
		Assert.That(toolbar.DisplayRectangle.Height, Is.GreaterThanOrEqualTo(editor.Height + editor.Margin.Vertical));
		var tallHeight = toolbar.Height;
		editor.Visible = false;
		System.Windows.Forms.Application.DoEvents();
		Assert.That(toolbar.Height, Is.EqualTo(tallHeight));
		editor.Size = new Size(100, 110);
		System.Windows.Forms.Application.DoEvents();
		Assert.That(toolbar.Height, Is.GreaterThan(tallHeight));
		toolbar.Items.Remove(editor);
		System.Windows.Forms.Application.DoEvents();
		Assert.That(toolbar.Height, Is.EqualTo(ordinaryHeight), "A removed control must not impose a permanent high-water mark.");
		using var largeFont = new Font(toolbar.Font.FontFamily, 24);
		var originalFont = toolbar.Font;
		toolbar.Font = largeFont;
		System.Windows.Forms.Application.DoEvents();
		Assert.That(toolbar.Height, Is.GreaterThan(ordinaryHeight));
		Assert.That(toolbar.DisplayRectangle.Height, Is.GreaterThanOrEqualTo(command.GetPreferredSize(Size.Empty).Height + command.Margin.Vertical));
		toolbar.Font = originalFont;
		System.Windows.Forms.Application.DoEvents();
		Assert.That(toolbar.Height, Is.EqualTo(ordinaryHeight));
	}

	private static void ClickNative(ToolStripItem item) {
		var owner = item.Owner;
		var location = new Point(item.Bounds.Left + item.Width / 2, item.Bounds.Top + item.Height / 2);
		var point = (IntPtr)((location.Y << 16) | (location.X & 0xFFFF));
		WinAPI.USER32.SendMessage(new HandleRef(owner, owner.Handle), 0x0200, IntPtr.Zero, point);
		WinAPI.USER32.SendMessage(new HandleRef(owner, owner.Handle), WinAPI.USER32.WM_LBUTTONDOWN, (IntPtr)1, point);
		WinAPI.USER32.SendMessage(new HandleRef(owner, owner.Handle), WinAPI.USER32.WM_LBUTTONUP, IntPtr.Zero, point);
	}

	[Test]
	public void DisposingBehaviorRestoresTheOriginalMinimumAndReleasesTheBorrowedToolbar() {
		using var form = new Form();
		using var toolbar = new ToolStrip();
		var originalMinimum = toolbar.MinimumSize;
		using var command = new ToolStripButton("Run");
		using var tall = new ToolStripButton("Optional") { AutoSize = false, Size = new Size(80, 75) };
		toolbar.Items.AddRange(new ToolStripItem[] { command, tall });
		form.Controls.Add(toolbar);
		var behavior = new ToolStripStableHeightBehavior(toolbar);
		form.Show();
		System.Windows.Forms.Application.DoEvents();
		var stableHeight = toolbar.Height;
		tall.Visible = false;
		System.Windows.Forms.Application.DoEvents();
		Assert.That(toolbar.Height, Is.EqualTo(stableHeight));
		behavior.Dispose();
		System.Windows.Forms.Application.DoEvents();
		Assert.That(toolbar.IsDisposed, Is.False);
		Assert.That(toolbar.MinimumSize, Is.EqualTo(originalMinimum));
		Assert.That(toolbar.Height, Is.LessThan(stableHeight));
		tall.Visible = true;
		System.Windows.Forms.Application.DoEvents();
		Assert.That(toolbar.MinimumSize, Is.EqualTo(originalMinimum), "Disposed subscriptions must no longer change the borrowed toolbar.");
		behavior.Dispose();
	}

	[Test]
	public void ExplicitMinimumSizeChangesRemainOwnedByTheCaller() {
		using var toolbar = new ToolStrip();
		using var behavior = new ToolStripStableHeightBehavior(toolbar);
		using var command = new ToolStripButton("Run");
		toolbar.Items.Add(command);
		var requested = new Size(90, 85);
		toolbar.MinimumSize = requested;
		toolbar.PerformLayout();
		command.Visible = false;
		toolbar.PerformLayout();
		Assert.That(toolbar.MinimumSize, Is.EqualTo(requested));
		behavior.Dispose();
		Assert.That(toolbar.MinimumSize, Is.EqualTo(requested));
	}

	private sealed class ToolbarMainForm : BlockMainForm {
		public ToolStrip ApplicationToolbar => ToolStrip;
	}

	private sealed class ToolbarScreen : WinFormsApplicationScreen {
		public ToolbarScreen() {
			Title = "Toolbar Fixture";
			ToolBar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
			Command = new ToolStripButton("Action", null, (_, _) => ClickCount++);
			Every = new ToolStripLabel("Every");
			Interval = new ToolStripDropDownButton("1 sec") { AutoSize = false, Width = 70 };
			RefreshCommand = new ToolStripButton("Refresh") { Visible = false };
			ToolBar.Items.AddRange(new ToolStripItem[] { Command, Every, Interval, RefreshCommand });
			Content = new Panel { Dock = DockStyle.Fill };
			Controls.Add(Content);
			Controls.Add(ToolBar);
		}

		public ToolStripButton Command { get; }
		public ToolStripLabel Every { get; }
		public ToolStripDropDownButton Interval { get; }
		public ToolStripButton RefreshCommand { get; }
		public Panel Content { get; }
		public int ClickCount { get; private set; }
	}
}
