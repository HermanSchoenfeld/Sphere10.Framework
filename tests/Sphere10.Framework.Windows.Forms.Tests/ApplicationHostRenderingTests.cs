// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationHostRenderingTests {
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

	[TestCase(false, false, 3)]
	[TestCase(true, false, 3)]
	[TestCase(false, true, 0)]
	[TestCase(true, true, 3)]
	public void UndockingMultilineNativeTabsPreservesEveryHeaderWithoutSelectingAnotherPage(bool scaled, bool mainFrame, int selectedIndex) {
		using var form = mainFrame ? (Form)new NativeTabsMainForm { ScreenMode = ScreenMode.MultiView } : new Form();
		form.StartPosition = FormStartPosition.Manual;
		form.Location = mainFrame ? new Point(80, 80) : new Point(-19000, -19000);
		form.ClientSize = new Size(1900, 850);
		using var ownHost = mainFrame ? null : new OffscreenScreenHost { Dock = DockStyle.Fill, ScreenMode = ScreenMode.MultiView, DetachedLocation = new Point(80, 80), DetachedClientSize = new Size(600, 500) };
		var host = (form as MainForm)?.ScreenHost ?? ownHost;
		using var screen = new WinFormsApplicationScreen { Title = "Native Multiline Tabs", AutoScaleMode = AutoScaleMode.Font, AutoScaleDimensions = new SizeF(7F, 15F) };
		if (mainFrame)
			screen.MinimumSize = new Size(750, 520);
		using var font = new Font(SystemFonts.MessageBoxFont.FontFamily, SystemFonts.MessageBoxFont.SizeInPoints * (scaled ? 1.5f : 1f));
		using var tabs = new NativeHeaderTabControl { Dock = DockStyle.Fill, Multiline = true, Font = font };
		using var image = new Bitmap(16, 16);
		using var images = new ImageList { ImageSize = image.Size };
		images.Images.Add("performance", image);
		images.Images.Add("threads", image);
		tabs.ImageList = images;
		var captions = new[] { "Performance", "GPU", "Image And Command Line", "Threads (195)", "Modules (135)", "Services (8)", "TCP / UDP (88)", "Open File Handles (37)", "Environment", "Security", "Job", "Strings" };
		foreach (var caption in captions)
			tabs.TabPages.Add(new CaptionTrackingTabPage(caption));
		tabs.TabPages[0].ImageKey = "performance";
		tabs.TabPages[3].ImageIndex = 1;
		screen.Controls.Add(tabs);
		screen.Controls.Add(new Label { Dock = DockStyle.Bottom, Text = "Captured Details", Height = 22 });
		screen.ToolBar = new ToolStrip { Dock = DockStyle.Top };
		screen.ToolBar.Items.AddRange(new ToolStripItem[] { new ToolStripButton("Refresh Details"), new ToolStripButton("Copy Executable Path"), new ToolStripButton("Copy Command Line") });
		screen.Controls.Add(screen.ToolBar);
		if (!mainFrame)
			form.Controls.Add(host);
		form.Show();
		host.ShowScreen(screen);
		tabs.SelectedIndex = selectedIndex;
		System.Windows.Forms.Application.DoEvents();
		if (mainFrame)
			Assert.That(screen.ToolBar.Items.Count, Is.Zero, "The real MainForm must lift the screen toolbar commands before detachment.");
		var selectedPage = tabs.SelectedTab;
		var selectedHandle = selectedPage.Handle;
		var tabHandle = tabs.Handle;
		var selectedImageKey = selectedPage.ImageKey;
		var selectedImageIndex = selectedPage.ImageIndex;
		var textAssignments = tabs.TabPages.Cast<CaptionTrackingTabPage>().Sum(page => page.TextAssignments);
		var selectionChanges = 0;
		tabs.SelectedIndexChanged += (_, _) => selectionChanges++;
		tabs.Trace.Clear();
		Assert.That(host.UndockScreen(screen), Is.True);
		System.Windows.Forms.Application.DoEvents();
		var headers = Enumerable.Range(0, tabs.TabCount).Select(tabs.GetTabRect).ToArray();
		using var surface = CaptureNativeClient(tabs, new Size(tabs.ClientSize.Width, headers.Max(header => header.Bottom) + 1));
		var screenshot = Path.Combine(Path.GetTempPath(), $"Sphere10-Undocked-Tabs-{scaled}-{mainFrame}-{Guid.NewGuid():N}.png");
		surface.Save(screenshot);
		TestContext.AddTestAttachment(screenshot, "First detached native header surface before any tab click.");
		var paintedCaptions = headers.Select(header => CountCaptionPixels(surface, header)).ToArray();
		TestContext.Progress.WriteLine($"Multiline native tabs: DPI={tabs.DeviceDpi}; scaled={scaled}; mainFrame={mainFrame}; bounds={tabs.Bounds}; display={tabs.DisplayRectangle}; page={selectedPage.Bounds}; headers={string.Join("; ", headers)}.");
		TestContext.Progress.WriteLine($"Native caption pixel counts before any click: {string.Join(", ", paintedCaptions)}.");
		TestContext.Progress.WriteLine(string.Join(Environment.NewLine, tabs.Trace));
		Assert.Multiple(() => {
			Assert.That(tabs.SelectedTab, Is.SameAs(selectedPage), "Reparenting must preserve the exact selected inner page.");
			Assert.That(tabs.Handle, Is.EqualTo(tabHandle));
			Assert.That(selectedPage.Handle, Is.EqualTo(selectedHandle));
			Assert.That(selectionChanges, Is.Zero, "Native layout completion must not synthesize a page-selection event.");
			Assert.That(tabs.TabPages.Cast<CaptionTrackingTabPage>().Sum(page => page.TextAssignments), Is.EqualTo(textAssignments), "Custom TabPage.Text setters must not be used as a layout callback.");
			Assert.That(selectedPage.ImageKey, Is.EqualTo(selectedImageKey));
			Assert.That(selectedPage.ImageIndex, Is.EqualTo(selectedImageIndex));
			Assert.That(headers.Select(header => header.Top).Distinct().Count(), Is.GreaterThan(1), "The fixture must exercise multiline native headers in the smaller detached window.");
			Assert.That(headers.All(tabs.ClientRectangle.Contains), Is.True, "Every native header must remain inside the TabControl after its first detached layout.");
			Assert.That(headers.All(header => !header.IntersectsWith(selectedPage.Bounds)), Is.True, "The retained page must not cover the native header rows before the user clicks a tab.");
			Assert.That(selectedPage.Bounds, Is.EqualTo(tabs.DisplayRectangle), "The selected page must use the final multiline native display rectangle.");
			Assert.That(paintedCaptions.All(count => count >= 8), Is.True, "Every real native header must contain caption ink after undocking, before any tab click or explicit repaint.");
			Assert.That(screen.ToolBar.Items.Count, Is.EqualTo(3), "Undocking must restore the exact command strip before final native layout.");
			Assert.That(tabs.Top, Is.GreaterThanOrEqualTo(screen.ToolBar.Bottom), "The restored toolbar must not cover the first row of tab headers.");
		});
	}


	[TestCase(false)]
	[TestCase(true)]
	public void UndockingAndRedockingPaintsTheCompleteRetainedGridWithoutResettingItsSelection(bool inactive) {
		// Use only owned offscreen windows and a real SourceGrid beneath native inspection-style tabs.
		using var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-19000, -19000), ClientSize = new Size(900, 650) };
		using var host = new OffscreenScreenHost { Dock = DockStyle.Fill, ScreenMode = ScreenMode.MultiView };
		using var screen = new PaintTrackingScreen();
		using var other = new WinFormsApplicationScreen { Title = "Other Screen" };
		form.Controls.Add(host);
		form.Show();
		host.ShowScreen(other);
		host.ShowScreen(screen);
		System.Windows.Forms.Application.DoEvents();
		screen.Tabs.SelectedIndex = 1;
		screen.Grid.Selection.SelectRow(3, true);
		System.Windows.Forms.Application.DoEvents();
		if (inactive)
			host.ShowScreen(other);
		var selectedCell = screen.Grid[3, 0];
		var selectedPage = screen.Tabs.SelectedTab;
		screen.ObserveNextCompletedPaint();
		Assert.That(host.UndockScreen(screen), Is.True);
		System.Windows.Forms.Application.DoEvents();
		Assert.Multiple(() => {
			Assert.That(host.IsScreenUndocked(screen), Is.True);
			Assert.That(host.ActiveScreen, Is.SameAs(other));
			Assert.That(screen.Grid.PaintCount, Is.EqualTo(1), "Undocking must deliver one completed native grid paint after restoring redraw.");
			Assert.That(screen.Grid.PaintsWhileSuppressed, Is.Zero);
			Assert.That(screen.Grid.LastUpdateRegion, Is.EqualTo(screen.Grid.ClientRectangle), "A partial update region cannot restore the retained grid after moving HWND parents.");
			Assert.That(WinAPI.USER32.IsWindowVisible(screen.Grid.Handle), Is.True);
			Assert.That(screen.Tabs.SelectedTab, Is.SameAs(selectedPage));
			Assert.That(screen.Grid.Selection.IsSelectedRow(3), Is.True);
			Assert.That(screen.Grid[3, 0], Is.SameAs(selectedCell));
			Assert.That(screen.FirstShowCount, Is.EqualTo(1), "A hosting transition must retain the existing screen and first-load state.");
		});

		// Re-docking follows the same final-frame contract and retains the same data and inner tab.
		screen.ObserveNextCompletedPaint();
		Assert.That(host.DockScreen(screen), Is.True);
		screen.Grid.Trace.Add("DockScreen returned");
		System.Windows.Forms.Application.DoEvents();
		TestContext.Progress.WriteLine(string.Join(Environment.NewLine, screen.Grid.Trace));
		Assert.Multiple(() => {
			Assert.That(host.IsScreenUndocked(screen), Is.False);
			Assert.That(host.ActiveScreen, Is.SameAs(screen));
			Assert.That(screen.Grid.PaintCount, Is.GreaterThanOrEqualTo(1), "Windows can queue another native exposure after the synchronous completed frame; raw WM_PAINT count is not a visible-frame count.");
			Assert.That(screen.Grid.PaintsWhileSuppressed, Is.Zero, "Re-docking must finish the transition before exposing a completed grid frame.");
			Assert.That(screen.Grid.LastUpdateRegion, Is.EqualTo(screen.Grid.ClientRectangle));
			Assert.That(screen.Tabs.SelectedTab, Is.SameAs(selectedPage));
			Assert.That(screen.Grid.Selection.IsSelectedRow(3), Is.True);
			Assert.That(screen.Grid[3, 0], Is.SameAs(selectedCell));
			Assert.That(screen.FirstShowCount, Is.EqualTo(1));
		});
	}

	[Test]
	public void NativeTabClickCommitsOneSelectionWithoutReentrantSelectionOrMouseUpRepaint() {
		using var form = new CountingMainForm { ScreenMode = ScreenMode.MultiView, ClientSize = new Size(1000, 650) };
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		form.RegisterBlock(block);
		form.Show();
		var first = (CountingScreen)form.ScreenHost.ActivateScreen(block, typeof(CountingScreen), "First");
		var second = (CountingScreen)form.ScreenHost.ActivateScreen(block, typeof(CountingScreen), "Second");
		form.ScreenHost.ShowScreen(first);
		var tabs = form.ScreenHost.TabControl;
		System.Windows.Forms.Application.DoEvents();
		using var spy = new NativeTabPaintSpy(tabs);
		form.ResetCounts();
		var selections = 0;
		form.ScreenHost.ScreenSelected += _ => selections++;
		var initialShows = second.ShowCount;
		var target = tabs.TabPages.Cast<TabPage>().Single(page => ReferenceEquals(page.Tag, second));
		var bounds = tabs.GetTabRect(tabs.TabPages.IndexOf(target));
		var location = new Point(bounds.Left + 12, bounds.Top + bounds.Height / 2);
		SendTabMessage(tabs, WinAPI.USER32.WM_LBUTTONDOWN, location);
		System.Windows.Forms.Application.DoEvents();
		var paintsAfterDown = spy.PaintCount;
		var drawsAfterDown = spy.DrawCount;
		var invalidationsAfterDown = spy.InvalidationCount;
		SendTabMessage(tabs, WinAPI.USER32.WM_LBUTTONUP, location);
		System.Windows.Forms.Application.DoEvents();
		TestContext.Progress.WriteLine($"Native selection: reentrant set-selection={spy.SelectionsInsideMouseDown}; raw paint messages={spy.PaintMessageCount}; visible paint inside down={spy.PaintsInsideMouseDown}; draws inside down={spy.DrawsInsideMouseDown}; down visible paints/draws={paintsAfterDown}/{drawsAfterDown}; mouse-up visible paints/draws={spy.PaintCount - paintsAfterDown}/{spy.DrawCount - drawsAfterDown}; mouse-up invalidations={spy.InvalidationCount - invalidationsAfterDown}.");
		TestContext.Progress.WriteLine(string.Join(Environment.NewLine, spy.Trace));
		Assert.Multiple(() => {
			Assert.That(tabs.SelectedTab, Is.SameAs(target));
			Assert.That(form.ActiveScreen, Is.SameAs(second));
			Assert.That(selections, Is.EqualTo(1));
			Assert.That(second.ShowCount, Is.EqualTo(initialShows + 1));
			Assert.That(form.CommandUpdateCount, Is.EqualTo(1));
			Assert.That(spy.DrawCount, Is.GreaterThan(0), "The fixture must exercise real owner drawing on the visible tab control.");
			Assert.That(spy.SelectionsInsideMouseDown, Is.Zero, "The host must allow the pending native selection to commit without starting another selection.");
			Assert.That(spy.PaintsInsideMouseDown, Is.Zero, "A selection transition must not force painting while native mouse-down is still selecting the page.");
			Assert.That(spy.DrawsInsideMouseDown, Is.Zero, "No owner-draw header may render an intermediate selection during native mouse-down.");
			Assert.That(spy.PaintCount, Is.EqualTo(1), "The visible native tab control must draw only one final selection frame.");
			Assert.That(spy.InvalidationCount - invalidationsAfterDown, Is.Zero, "An ordinary mouse-up has no drag adornment to repaint.");
			Assert.That(spy.DrawCount - drawsAfterDown, Is.Zero, "The unchanged tab header must not be drawn again on ordinary mouse-up.");
		});
	}

	[Test]
	public void NativeTabClickOnTheSelectedPageDoesNotRepeatItsLifecycleOrRedrawOnMouseUp() {
		using var form = new CountingMainForm { ScreenMode = ScreenMode.MultiView };
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		form.RegisterBlock(block);
		form.Show();
		var screen = (CountingScreen)form.ScreenHost.ActivateScreen(block, typeof(CountingScreen));
		var tabs = form.ScreenHost.TabControl;
		System.Windows.Forms.Application.DoEvents();
		using var spy = new NativeTabPaintSpy(tabs);
		var initialShows = screen.ShowCount;
		var bounds = tabs.GetTabRect(0);
		var location = new Point(bounds.Left + 12, bounds.Top + bounds.Height / 2);
		SendTabMessage(tabs, WinAPI.USER32.WM_LBUTTONDOWN, location);
		System.Windows.Forms.Application.DoEvents();
		var invalidationsAfterDown = spy.InvalidationCount;
		var drawsAfterDown = spy.DrawCount;
		SendTabMessage(tabs, WinAPI.USER32.WM_LBUTTONUP, location);
		System.Windows.Forms.Application.DoEvents();
		TestContext.Progress.WriteLine($"Selected-page click: mouse-up draws={spy.DrawCount - drawsAfterDown}, invalidations={spy.InvalidationCount - invalidationsAfterDown}.");
		Assert.Multiple(() => {
			Assert.That(form.ActiveScreen, Is.SameAs(screen));
			Assert.That(screen.ShowCount, Is.EqualTo(initialShows));
			Assert.That(spy.InvalidationCount - invalidationsAfterDown, Is.Zero);
			Assert.That(spy.DrawCount - drawsAfterDown, Is.Zero);
		});
	}

	[Test]
	public void NativeTabClickRetainsThePreviousSelectionWhenItsScreenVetoesHiding() {
		using var form = new CountingMainForm { ScreenMode = ScreenMode.MultiView };
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		form.RegisterBlock(block);
		form.Show();
		var previous = (GuardedScreen)form.ScreenHost.ActivateScreen(block, typeof(GuardedScreen));
		var target = (CountingScreen)form.ScreenHost.ActivateScreen(block, typeof(CountingScreen));
		form.ScreenHost.ShowScreen(previous);
		previous.CancelHide = true;
		var tabs = form.ScreenHost.TabControl;
		System.Windows.Forms.Application.DoEvents();
		form.ResetCounts();
		var initialShows = target.ShowCount;
		var initialHides = previous.HideCount;
		var bounds = tabs.GetTabRect(1);
		var location = new Point(bounds.Left + 12, bounds.Top + bounds.Height / 2);
		SendTabMessage(tabs, WinAPI.USER32.WM_LBUTTONDOWN, location);
		SendTabMessage(tabs, WinAPI.USER32.WM_LBUTTONUP, location);
		System.Windows.Forms.Application.DoEvents();
		Assert.Multiple(() => {
			Assert.That(form.ActiveScreen, Is.SameAs(previous));
			Assert.That(tabs.SelectedTab.Tag, Is.SameAs(previous));
			Assert.That(previous.HideCount, Is.EqualTo(initialHides + 1));
			Assert.That(target.ShowCount, Is.EqualTo(initialShows));
			Assert.That(form.CommandUpdateCount, Is.Zero);
		});
	}

	[Test]
	public void NativeKeyboardTabSelectionCoordinatesTheHostAndHonorsTheHideGuard() {
		using var form = new CountingMainForm { ScreenMode = ScreenMode.MultiView };
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		form.RegisterBlock(block);
		form.Show();
		var first = (CountingScreen)form.ScreenHost.ActivateScreen(block, typeof(CountingScreen));
		var second = (GuardedScreen)form.ScreenHost.ActivateScreen(block, typeof(GuardedScreen));
		form.ScreenHost.ShowScreen(first);
		var tabs = form.ScreenHost.TabControl;
		tabs.Focus();
		System.Windows.Forms.Application.DoEvents();
		var initialShows = second.ShowCount;
		WinAPI.USER32.SendMessage(new HandleRef(tabs, tabs.Handle), WinAPI.USER32.WM_KEYDOWN, (IntPtr)Keys.Right, (IntPtr)1);
		System.Windows.Forms.Application.DoEvents();
		Assert.That(form.ActiveScreen, Is.SameAs(second));
		Assert.That(tabs.SelectedTab.Tag, Is.SameAs(second));
		Assert.That(second.ShowCount, Is.EqualTo(initialShows + 1));
		second.CancelHide = true;
		var initialHides = second.HideCount;
		WinAPI.USER32.SendMessage(new HandleRef(tabs, tabs.Handle), WinAPI.USER32.WM_KEYDOWN, (IntPtr)Keys.Left, (IntPtr)1);
		System.Windows.Forms.Application.DoEvents();
		Assert.That(form.ActiveScreen, Is.SameAs(second));
		Assert.That(tabs.SelectedTab.Tag, Is.SameAs(second));
		Assert.That(second.HideCount, Is.EqualTo(initialHides + 1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void NativeTabCloseGlyphClosesItsScreenOrHonorsItsHideGuard(bool veto) {
		using var form = new CountingMainForm { ScreenMode = ScreenMode.MultiView };
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		form.RegisterBlock(block);
		form.Show();
		var first = (GuardedScreen)form.ScreenHost.ActivateScreen(block, typeof(GuardedScreen), "First");
		var second = (CountingScreen)form.ScreenHost.ActivateScreen(block, typeof(CountingScreen), "Second");
		form.ScreenHost.ShowScreen(first);
		first.CancelHide = veto;
		var tabs = form.ScreenHost.TabControl;
		System.Windows.Forms.Application.DoEvents();
		var bounds = tabs.GetTabRect(0);
		var location = new Point(bounds.Right - tabs.LogicalToDeviceUnits(14), bounds.Top + bounds.Height / 2);
		SendTabMessage(tabs, WinAPI.USER32.WM_LBUTTONDOWN, location);
		SendTabMessage(tabs, WinAPI.USER32.WM_LBUTTONUP, location);
		System.Windows.Forms.Application.DoEvents();
		Assert.Multiple(() => {
			Assert.That(first.IsDisposed, Is.EqualTo(!veto));
			Assert.That(form.ActiveScreen, Is.SameAs(veto ? first : second));
			Assert.That(tabs.SelectedTab.Tag, Is.SameAs(form.ActiveScreen));
			Assert.That(tabs.TabCount, Is.EqualTo(veto ? 2 : 1));
			Assert.That(tabs.Capture, Is.False);
		});
	}

	private static Bitmap CaptureNativeClient(Control control, Size size) {
		var image = new Bitmap(size.Width, size.Height);
		var captured = false;
		using var rollback = Tools.Scope.ExecuteOnDispose(() => { if (!captured) image.Dispose(); });
		using var graphics = Graphics.FromImage(image);
		var source = WinAPI.USER32.GetDC(control.Handle);
		using var sourceScope = Tools.Scope.ExecuteOnDispose(() => WinAPI.USER32.ReleaseDC(control.Handle, source));
		var target = graphics.GetHdc();
		using var targetScope = Tools.Scope.ExecuteOnDispose(() => graphics.ReleaseHdc(target));
		Assert.That(WinAPI.GDI32.BitBlt(target, 0, 0, size.Width, size.Height, source, 0, 0, WinAPI.GDI32.TernaryRasterOperations.SRCCOPY), Is.True);
		captured = true;
		return image;
	}

	private static int CountCaptionPixels(Bitmap image, Rectangle header) {
		var bounds = Rectangle.Intersect(new Rectangle(Point.Empty, image.Size), Rectangle.Inflate(header, -6, -4));
		var dark = 0;
		var light = 0;
		for (var y = bounds.Top; y < bounds.Bottom; y++)
			for (var x = bounds.Left; x < bounds.Right; x++) {
				var pixel = image.GetPixel(x, y);
				if (pixel.R + pixel.G + pixel.B < 360)
					dark++;
				if (pixel.R + pixel.G + pixel.B > 600)
					light++;
			}
		return light >= 8 ? dark : 0;
	}


	private static void SendTabMessage(TabControl tabs, uint message, Point location) => WinAPI.USER32.SendMessage(new HandleRef(tabs, tabs.Handle), message,
		message == WinAPI.USER32.WM_LBUTTONDOWN ? (IntPtr)1 : IntPtr.Zero, (IntPtr)((location.Y << 16) | (location.X & 0xFFFF)));

	private sealed class NativeTabPaintSpy : NativeWindow, IDisposable {
		private readonly TabControl _tabs;
		private int _mouseDownDepth;
		public int PaintCount { get; private set; }
		public int PaintMessageCount { get; private set; }
		public int DrawCount { get; private set; }
		public int InvalidationCount { get; private set; }
		public int PaintsInsideMouseDown { get; private set; }
		public int DrawsInsideMouseDown { get; private set; }
		public int SelectionsInsideMouseDown { get; private set; }
		public List<string> Trace { get; } = new();

		public NativeTabPaintSpy(TabControl tabs) {
			_tabs = tabs;
			AssignHandle(tabs.Handle);
			tabs.DrawItem += DrawItem;
			tabs.Invalidated += Invalidated;
		}

		public void Dispose() {
			_tabs.DrawItem -= DrawItem;
			_tabs.Invalidated -= Invalidated;
			ReleaseHandle();
		}

		protected override void WndProc(ref Message message) {
			if (_mouseDownDepth > 0 && (message.Msg == 0x000B || message.Msg == 0x0018 || message.Msg == WinAPI.USER32.WM_PAINT)) {
				Trace.Add($"Native input message 0x{message.Msg:X4}, wParam={message.WParam}; managed/native visible={_tabs.Visible}/{WinAPI.USER32.IsWindowVisible(Handle)}; selected={_tabs.SelectedIndex}.");
				if (message.Msg == WinAPI.USER32.WM_PAINT)
					Trace.Add(Environment.StackTrace);
			}
			if (message.Msg == WinAPI.USER32.WM_PAINT) {
				PaintMessageCount++;
				var previousDraws = DrawCount;
				base.WndProc(ref message);
				if (DrawCount > previousDraws) {
					PaintCount++;
					if (_mouseDownDepth > 0)
						PaintsInsideMouseDown++;
				}
				return;
			}
			if (message.Msg == WinAPI.COMCTL32.TCM_SETCURSEL && _mouseDownDepth > 0)
				SelectionsInsideMouseDown++;
			if (message.Msg != WinAPI.USER32.WM_LBUTTONDOWN) {
				base.WndProc(ref message);
				return;
			}
			_mouseDownDepth++;
			{
				using var restoreMouseDispatchDepth = Tools.Scope.ExecuteOnDispose(() => {
					_mouseDownDepth--;
				});
				base.WndProc(ref message);
			}
		}

		private void DrawItem(object sender, DrawItemEventArgs args) {
			DrawCount++;
			if (_mouseDownDepth > 0) {
				DrawsInsideMouseDown++;
				Trace.Add($"Draw tab {args.Index}, selected={_tabs.SelectedIndex}, state={args.State}; native visible={WinAPI.USER32.IsWindowVisible(Handle)}; bounds={args.Bounds}; visible clip={args.Graphics.VisibleClipBounds}.");
			}
		}

		private void Invalidated(object sender, InvalidateEventArgs args) => InvalidationCount++;
	}

	[Test]
	public void ScreenTransitionPublishesOneFinalSelectionAndRebuildsChromeOnce() {
		using var form = new CountingMainForm { ScreenMode = ScreenMode.MultiView };
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		form.RegisterBlock(block);
		form.Show();
		form.ScreenHost.ActivateScreen(block, typeof(CountingScreen));
		System.Windows.Forms.Application.DoEvents();
		form.ResetCounts();
		var selections = 0;
		var changes = 0;
		form.ScreenHost.ScreenSelected += _ => selections++;
		form.ScreenHost.Changed += () => changes++;
		var created = form.ScreenHost.ActivateScreen(block, typeof(CountingScreen));
		Assert.That(form.ActiveScreen, Is.SameAs(created));
		Assert.That(form.ActiveChangingCount, Is.EqualTo(1));
		Assert.That(form.ActiveChangedCount, Is.EqualTo(1));
		Assert.That(form.CommandUpdateCount, Is.EqualTo(1));
		Assert.That(form.DisableRedrawCount, Is.EqualTo(1));
		Assert.That(form.EnableRedrawCount, Is.EqualTo(1));
		Assert.That(changes, Is.EqualTo(1));
		Assert.That(selections, Is.EqualTo(1));
	}

	[Test]
	public void FailedRenderingEntryLeavesTheNativeTransitionReadyForRetry() {
		using var form = new CountingMainForm { ScreenMode = ScreenMode.MultiView };
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		using var failing = new FailingLayoutPanel();
		form.Controls.Add(failing);
		form.RegisterBlock(block);
		form.Show();
		var previous = form.ScreenHost.ActivateScreen(block, typeof(CountingScreen));
		System.Windows.Forms.Application.DoEvents();
		form.ResetCounts();
		var changes = 0;
		var selections = 0;
		form.ScreenHost.Changed += () => changes++;
		form.ScreenHost.ScreenSelected += _ => selections++;
		failing.FailOnNextEnumeration();
		var failure = Assert.Throws<InvalidOperationException>(() => form.ScreenHost.ActivateScreen(block, typeof(CountingScreen)));
		Assert.That(failure, Is.SameAs(failing.Failure));
		Assert.That(form.ActiveScreen, Is.SameAs(previous));
		Assert.That(form.DisableRedrawCount, Is.EqualTo(1));
		Assert.That(form.EnableRedrawCount, Is.EqualTo(1));
		Assert.That(changes, Is.Zero);
		Assert.That(selections, Is.Zero);
		form.ResetCounts();
		var created = form.ScreenHost.ActivateScreen(block, typeof(CountingScreen));
		Assert.That(form.ActiveScreen, Is.SameAs(created));
		Assert.That(form.ActiveChangedCount, Is.EqualTo(1));
		Assert.That(form.CommandUpdateCount, Is.EqualTo(1));
		Assert.That(form.DisableRedrawCount, Is.EqualTo(1));
		Assert.That(form.EnableRedrawCount, Is.EqualTo(1));
		Assert.That(changes, Is.EqualTo(1));
		Assert.That(selections, Is.EqualTo(1));
	}

	[Test]
	public async Task ApplicationCommandsSurviveBlockAndScreenCaptionCollisions() {
		using var form = new CountingMainForm();
		using var block = new WinFormsApplicationBlock { Name = "Workspace", ShowInMenuStrip = true };
		await using var application = new WinFormsApplication(form, new[] { block });
		using var globalMenu = new WinFormsApplicationMenu("&File");
		using var globalHelpMenu = new WinFormsApplicationMenu("&Help");
		var globalRuns = 0;
		var blockRuns = 0;
		var globalHelpRuns = 0;
		var blockHelpRuns = 0;
		var global = new WinFormsActionMenuItem("Save", () => globalRuns++);
		var contextual = new WinFormsActionMenuItem("Save", () => blockRuns++);
		var globalHelp = new WinFormsActionMenuItem("Inspect", () => globalHelpRuns++);
		var contextualHelp = new WinFormsActionMenuItem("Inspect", () => blockHelpRuns++);
		globalMenu.AddItem(global);
		globalHelpMenu.AddItem(globalHelp);
		var blockMenu = new WinFormsApplicationMenu("File") { ShowInMenuStrip = true };
		blockMenu.AddItem(contextual);
		block.AddMenu(blockMenu);
		var blockHelpMenu = new WinFormsApplicationMenu("Help") { ShowInMenuStrip = true };
		blockHelpMenu.AddItem(contextualHelp);
		block.AddMenu(blockHelpMenu);
		application.SetMenus(new[] { globalMenu, globalHelpMenu });
		application.SetToolBarItems(new[] { global });
		await application.InitializeAsync();
		var legacyFile = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text == "File");
		var legacyExit = legacyFile.DropDownItems.Cast<ToolStripItem>().Single(item => item.Text == "E&xit");
		var legacyHelp = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text == "Help");
		var legacyContextHelp = legacyHelp.DropDownItems.Cast<ToolStripItem>().Single(item => item.Text == "Context Help");
		var screen = (CommandScreen)form.ScreenHost.ActivateScreen(block, typeof(CommandScreen));
		var file = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text == "File");
		var help = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text == "Help");
		Assert.That(file, Is.SameAs(legacyFile));
		Assert.That(help, Is.SameAs(legacyHelp));
		Assert.That(form.MenuEntries.Last(), Is.SameAs(legacyHelp));
		Assert.That(file.DropDownItems.OfType<ToolStripMenuItem>().Select(item => item.Text), Is.EqualTo(new[] { "Save", "Save", "E&xit" }));
		file.DropDownItems[0].PerformClick();
		file.DropDownItems[1].PerformClick();
		help.DropDownItems[0].PerformClick();
		help.DropDownItems[1].PerformClick();
		legacyExit.PerformClick();
		legacyContextHelp.PerformClick();
		Assert.That(globalRuns, Is.EqualTo(1));
		Assert.That(blockRuns, Is.Zero, "The screen replaces the block's contextual Save command.");
		Assert.That(screen.ExecuteCount, Is.EqualTo(1));
		Assert.That(globalHelpRuns, Is.EqualTo(1));
		Assert.That(blockHelpRuns, Is.Zero);
		Assert.That(screen.HelpExecuteCount, Is.EqualTo(1));
		Assert.That(form.ExitCount, Is.EqualTo(1));
		Assert.That(form.HelpCount, Is.EqualTo(1));
		Assert.That(form.ToolEntries.Count(item => item.ToolTipText == "Save"), Is.EqualTo(2));
		Assert.That(global.Parent, Is.SameAs(globalMenu));
		Assert.That(screen.Command.Parent, Is.SameAs(screen.Menus[0]), "Menu composition preserves the command's declaring menu.");
		var previousGenerated = file.DropDownItems[1];
		form.ScreenHost.ActivateScreen(block, typeof(CountingScreen));
		file = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text == "File");
		help = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text == "Help");
		file.DropDownItems[0].PerformClick();
		file.DropDownItems[1].PerformClick();
		help.DropDownItems[0].PerformClick();
		help.DropDownItems[1].PerformClick();
		legacyExit.PerformClick();
		legacyContextHelp.PerformClick();
		Assert.That(globalRuns, Is.EqualTo(2));
		Assert.That(blockRuns, Is.EqualTo(1));
		Assert.That(globalHelpRuns, Is.EqualTo(2));
		Assert.That(blockHelpRuns, Is.EqualTo(1));
		Assert.That(form.ExitCount, Is.EqualTo(2));
		Assert.That(form.HelpCount, Is.EqualTo(2));
		Assert.That(legacyExit.IsDisposed, Is.False);
		Assert.That(legacyContextHelp.IsDisposed, Is.False);
		Assert.That(previousGenerated.IsDisposed, Is.True);
		Assert.That(form.MenuEntries.Last(), Is.SameAs(legacyHelp));
	}

	[TestCase("&File")]
	[TestCase("&Actions")]
	public async Task NativeScreenMenusReuseExistingHeadersWithoutTakingCommandOwnership(string caption) {
		using var form = new CountingMainForm();
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		await using var application = new WinFormsApplication(form, new[] { block });
		using var globalMenu = new WinFormsApplicationMenu(caption);
		globalMenu.AddItem(new WinFormsActionMenuItem("Keep", () => { }));
		application.SetMenus(new[] { globalMenu });
		await application.InitializeAsync();
		var file = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text == "File");
		var legacyExit = file.DropDownItems.Cast<ToolStripItem>().Single(item => item.Text == "E&xit");
		var screen = new NativeMenuScreen { ApplicationBlock = block, ApplicationMenuStripText = caption };
		form.ScreenHost.ShowScreen(screen);
		var header = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text.Replace("&", string.Empty) == caption.Replace("&", string.Empty));
		Assert.That(header.DropDownItems.Contains(screen.Command), Is.True);
		Assert.That(screen.Command.IsDisposed, Is.False);
		screen.Command.PerformClick();
		Assert.That(screen.ExecuteCount, Is.EqualTo(1));
		form.ScreenHost.ActivateScreen(block, typeof(CountingScreen));
		header = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text.Replace("&", string.Empty) == caption.Replace("&", string.Empty));
		Assert.That(header.DropDownItems.Contains(screen.Command), Is.False);
		Assert.That(screen.Command.IsDisposed, Is.False);
		Assert.That(legacyExit.IsDisposed, Is.False);
		Assert.That(form.ScreenHost.ShowScreen(screen), Is.True);
		header = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text.Replace("&", string.Empty) == caption.Replace("&", string.Empty));
		Assert.That(header.DropDownItems.Contains(screen.Command), Is.True);
		screen.Command.PerformClick();
		Assert.That(screen.ExecuteCount, Is.EqualTo(2));
	}

	[Test]
	public void SharedModeSetterAndSpecializedContractsUseTheNativeGuardedTransition() {
		using var host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var first = new GuardedScreen();
		var active = new CountingScreen();
		host.ShowScreen(first);
		host.ShowScreen(active);
		first.CancelHide = true;
		IApplicationScreenHost common = host;
		Assert.That(() => common.ScreenMode = ScreenMode.SingleView, Throws.InvalidOperationException);
		Assert.That(common.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
		Assert.That(host.OpenScreens, Is.EqualTo(new[] { first, active }));
		first.CancelHide = false;
		common.ScreenMode = ScreenMode.SingleView;
		Assert.That(host.OpenScreens, Is.EqualTo(new[] { active }));
		Assert.That(first.IsDisposed, Is.True);
		ISingleApplicationScreenHost single = host;
		Assert.That(single.CanCloseActiveScreenAsync().Result, Is.True);
		Assert.That(single.CloseActiveScreenAsync().Result, Is.True);
		Assert.That(common.ActiveScreen, Is.Null);
		Assert.That(active.IsDisposed, Is.True);
		Assert.That(host.Screens, Is.Empty, "Closing the active native screen removes its control and hosting metadata together.");
		common.ScreenMode = ScreenMode.MultiView;
		IMultiApplicationScreenHost multi = host;
		Assert.That(multi.OpenScreens, Is.Empty);
	}

	[Test]
	public void SingleScreenClosePreservesTheNativeControlWhenItsHideGuardCancels() {
		using var host = new WinFormsApplicationScreenHost();
		var screen = new GuardedScreen { CancelHide = true };
		host.ShowScreen(screen);
		ISingleApplicationScreenHost single = host;
		Assert.That(single.CanCloseActiveScreenAsync().Result, Is.False);
		Assert.That(single.CloseActiveScreenAsync().Result, Is.False);
		Assert.That(single.ActiveScreen, Is.SameAs(screen));
		Assert.That(screen.IsDisposed, Is.False);
		Assert.That(host.OpenScreens, Is.EqualTo(new[] { screen }));
		screen.CancelHide = false;
		Assert.That(single.CloseActiveScreenAsync().Result, Is.True);
		Assert.That(screen.IsDisposed, Is.True);
	}

	private sealed class OffscreenScreenHost : WinFormsApplicationScreenHost {
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Point DetachedLocation { get; set; } = new(-18000, -18000);
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Size DetachedClientSize { get; set; } = new(900, 650);

		protected override WinFormsApplicationScreenForm CreateScreenForm(WinFormsApplicationScreen screen)
			=> new(this, screen) { StartPosition = FormStartPosition.Manual, Location = DetachedLocation, ClientSize = DetachedClientSize };
	}

	private sealed class NativeTabsMainForm : MainForm {
		protected override void OnLoad(EventArgs args) { }

		protected override void OnFirstActivated() { }
	}

	private sealed class CaptionTrackingTabPage : TabPage {
		public int TextAssignments;

		public CaptionTrackingTabPage(string caption)
			: base(caption) { }

		public override string Text {
			get => base.Text;
			set { TextAssignments++; base.Text = value; }
		}
	}

	private sealed class NativeHeaderTabControl : TabControl {
		public List<string> Trace { get; } = new();

		protected override void OnResize(EventArgs args) {
			base.OnResize(args);
			Trace.Add($"OnResize: bounds={Bounds}; page={SelectedTab?.Bounds}");
		}

		protected override void OnLayout(LayoutEventArgs args) {
			base.OnLayout(args);
			Trace.Add($"OnLayout: {args.AffectedProperty}; bounds={Bounds}; page={SelectedTab?.Bounds}");
		}

		protected override void WndProc(ref Message message) {
			if (message.Msg is 0x000B or 0x0005 or 0x0047)
				Trace.Add($"Native before 0x{message.Msg:X}({message.WParam}): bounds={Bounds}; page={SelectedTab?.Bounds}");
			base.WndProc(ref message);
			if (message.Msg is 0x000B or 0x0005 or 0x0047)
				Trace.Add($"Native after 0x{message.Msg:X}({message.WParam}): bounds={Bounds}; page={SelectedTab?.Bounds}");
		}
	}

	private sealed class PaintTrackingScreen : WinFormsApplicationScreen {
		private bool _observeNextPaint;

		public PaintTrackingScreen() {
			Title = "Grid Inspection";
			Tabs = new TabControl { Dock = DockStyle.Fill };
			Tabs.TabPages.Add(new TabPage("Image"));
			var modules = new TabPage("Modules");
			Grid = new PaintTrackingGrid { Dock = DockStyle.Fill, SelectionMode = SourceGrid.GridSelectionMode.Row };
			Grid.Redim(26, 2);
			for (var row = 0; row < Grid.RowsCount; row++) {
				Grid[row, 0] = new SourceGrid.Cells.Cell($"Module {row}");
				Grid[row, 1] = new SourceGrid.Cells.Cell($"C:\\Modules\\Module{row}.dll");
				Grid.Rows.SetHeight(row, 24);
			}
			Grid.Columns.SetWidth(0, 240);
			Grid.Columns.SetWidth(1, 500);
			modules.Controls.Add(Grid);
			Tabs.TabPages.Add(modules);
			Controls.Add(Tabs);
		}

		public TabControl Tabs { get; }
		public PaintTrackingGrid Grid { get; }
		public int FirstShowCount { get; private set; }

		public void ObserveNextCompletedPaint() => _observeNextPaint = true;

		protected override void OnShowFirstTime() {
			FirstShowCount++;
			base.OnShowFirstTime();
		}

		protected override void OnShow() {
			base.OnShow();
			if (!_observeNextPaint)
				return;
			_observeNextPaint = false;
			// Native reparenting can queue exposure before scope completion; isolate the required final frame.
			WinAPI.USER32.ValidateRect(new HandleRef(Grid, Grid.Handle), IntPtr.Zero);
			Grid.ResetPaint();
		}
	}

	private sealed class PaintTrackingGrid : SourceGrid.Grid {
		private bool _redrawDisabled;

		public int PaintCount { get; private set; }
		public int PaintsWhileSuppressed { get; private set; }
		public Rectangle LastUpdateRegion { get; private set; }
		public List<string> Trace { get; } = new();

		public void ResetPaint() {
			PaintCount = 0;
			PaintsWhileSuppressed = 0;
			LastUpdateRegion = Rectangle.Empty;
			Trace.Clear();
			Trace.Add($"Observe final frame: redraw disabled={_redrawDisabled}, managed visible={Visible}, native visible={WinAPI.USER32.IsWindowVisible(Handle)}");
		}

		protected override void WndProc(ref Message message) {
			if (message.Msg == 0x000B) {
				_redrawDisabled = message.WParam == IntPtr.Zero;
				Trace.Add($"WM_SETREDRAW: disabled={_redrawDisabled}");
			}
			if (message.Msg == 0x000F && WinAPI.USER32.GetUpdateRect(new HandleRef(this, Handle), out var update, false)) {
				PaintCount++;
				if (_redrawDisabled)
					PaintsWhileSuppressed++;
				LastUpdateRegion = Rectangle.FromLTRB(update.Left, update.Top, update.Right, update.Bottom);
				Trace.Add($"WM_PAINT {PaintCount}: region={LastUpdateRegion}, disabled={_redrawDisabled}");
			}
			base.WndProc(ref message);
		}

		protected override void OnInvalidated(InvalidateEventArgs args) {
			Trace.Add($"Invalidated: {args.InvalidRect}");
			base.OnInvalidated(args);
		}
	}

	private sealed class CountingMainForm : BlockMainForm {
		public int CommandUpdateCount { get; private set; }
		public int ActiveChangingCount { get; private set; }
		public int ActiveChangedCount { get; private set; }
		public int DisableRedrawCount { get; private set; }
		public int EnableRedrawCount { get; private set; }
		public int ExitCount { get; private set; }
		public int HelpCount { get; private set; }
		public ToolStripItem[] MenuEntries => MenuStrip.Items.Cast<ToolStripItem>().ToArray();
		public ToolStripItem[] ToolEntries => ToolStrip.Items.Cast<ToolStripItem>().ToArray();

		public void ResetCounts() => CommandUpdateCount = ActiveChangingCount = ActiveChangedCount = DisableRedrawCount = EnableRedrawCount = 0;

		protected override void OnLoad(EventArgs args) { }

		protected override void OnFirstActivated() { }

		protected override void Exit_Click(object sender, EventArgs args) => ExitCount++;

		protected override void ContextHelp_Click(object sender, EventArgs args) => HelpCount++;

		protected override void UpdateApplicationCommands() {
			CommandUpdateCount++;
			base.UpdateApplicationCommands();
		}

		protected override void OnActiveScreenChanging(WinFormsApplicationScreen screen) {
			ActiveChangingCount++;
			base.OnActiveScreenChanging(screen);
		}

		protected override void OnActiveScreenChanged(WinFormsApplicationScreen screen) {
			ActiveChangedCount++;
			base.OnActiveScreenChanged(screen);
		}

		protected override void WndProc(ref Message message) {
			if (message.Msg == 0x000B) {
				if (message.WParam == IntPtr.Zero)
					DisableRedrawCount++;
				else
					EnableRedrawCount++;
			}
			base.WndProc(ref message);
		}
	}

	public class CountingScreen : WinFormsApplicationScreen {
		public int ShowCount { get; private set; }

		protected override void OnShow() {
			ShowCount++;
			base.OnShow();
		}
	}

	private sealed class FailingLayoutPanel : Panel {
		private bool _failEnumeration;

		public InvalidOperationException Failure { get; } = new("Controlled control failure.");

		public void FailOnNextEnumeration() => _failEnumeration = true;

		protected override ControlCollection CreateControlsInstance() => new FailingControlCollection(this);

		private sealed class FailingControlCollection : ControlCollection {
			private readonly FailingLayoutPanel _owner;

			public FailingControlCollection(FailingLayoutPanel owner)
				: base(owner) => _owner = owner;

			public override IEnumerator GetEnumerator() {
				if (_owner._failEnumeration) {
					_owner._failEnumeration = false;
					throw _owner.Failure;
				}
				return base.GetEnumerator();
			}
		}
	}

	public sealed class NativeMenuScreen : CountingScreen {
		public NativeMenuScreen() {
			ActivationMode = ScreenActivationMode.SingleInstance;
			ApplicationMenuStripText = "&File";
			ShowInApplicationMenuStrip = true;
			Command = new ToolStripMenuItem("Inspect route", null, (_, _) => ExecuteCount++);
			RegisterMenuItem(Command);
		}

		public ToolStripMenuItem Command { get; }
		public int ExecuteCount { get; private set; }
	}

	public sealed class GuardedScreen : CountingScreen {
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool CancelHide { get; set; }
		public int HideCount { get; private set; }

		protected override void OnHide(ref bool cancelHide) {
			HideCount++;
			cancelHide = CancelHide;
		}
	}

	public sealed class CommandScreen : WinFormsApplicationScreen {
		private readonly WinFormsApplicationMenu _menu;
		private readonly WinFormsApplicationMenu _helpMenu;

		public CommandScreen() {
			Command = new WinFormsActionMenuItem("Save", () => ExecuteCount++);
			_menu = new WinFormsApplicationMenu("File");
			_menu.AddItem(Command);
			_helpMenu = new WinFormsApplicationMenu("H&elp");
			_helpMenu.AddItem(new WinFormsActionMenuItem("Inspect", () => HelpExecuteCount++));
		}

		public int ExecuteCount { get; private set; }
		public int HelpExecuteCount { get; private set; }
		public WinFormsActionMenuItem Command { get; }
		public override IWinFormsApplicationMenu[] Menus => new[] { _menu, _helpMenu };
		public override IWinFormsApplicationMenuItem[] ToolBarItems => new[] { Command };

		protected override void Dispose(bool disposing) {
			if (disposing) {
				_menu.Dispose();
				_helpMenu.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
