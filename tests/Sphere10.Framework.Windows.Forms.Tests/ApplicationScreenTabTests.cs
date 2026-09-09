// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationScreenTabTests {
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

	[Test]
	public void TabsMoveDuringDragWithoutWaitingForMouseUp() {
		using var Tabs = new TestTabs { Size = new Size(800, 400) };
		Tabs.TabPages.AddRange(new[] { new TabPage("First"), new TabPage("Second"), new TabPage("Third") });
		_ = Tabs.Handle;
		var First = Tabs.TabPages[0];
		Tabs.BeginDrag(First);
		Tabs.DragTo(Center(Tabs.GetTabRect(1)));
		Assert.That(Tabs.TabPages.IndexOf(First), Is.EqualTo(1), "Order must change while the mouse button remains pressed");
		Assert.That(Tabs.SelectedTab, Is.SameAs(First));
		Tabs.DragTo(Center(Tabs.GetTabRect(2)));
		Assert.That(Tabs.TabPages.IndexOf(First), Is.EqualTo(2));
		Tabs.EndDrag(Center(Tabs.GetTabRect(2)));
		Assert.That(Tabs.SelectedTab, Is.SameAs(First));
	}

	[Test]
	public void EscapeRestoresOriginalTabOrderDuringDrag() {
		using var Tabs = new TestTabs { Size = new Size(800, 400) };
		Tabs.TabPages.AddRange(new[] { new TabPage("First"), new TabPage("Second"), new TabPage("Third") });
		_ = Tabs.Handle;
		var First = Tabs.TabPages[0];
		Tabs.BeginDrag(First);
		Tabs.DragTo(Center(Tabs.GetTabRect(2)));
		Assert.That(Tabs.TabPages.IndexOf(First), Is.EqualTo(2));
		Assert.That(Tabs.PreprocessEscape(), Is.True);
		Assert.That(Tabs.TabPages.IndexOf(First), Is.Zero);
		Assert.That(Tabs.SelectedTab, Is.SameAs(First));
	}

	[Test]
	public void EscapeCancelsDragBeforeTheParentFormCancelButton() {
		using var Owner = new Form();
		using var Tabs = new TestTabs { Dock = DockStyle.Fill };
		var CancelButton = new ProbeCancelButton();
		Owner.CancelButton = CancelButton;
		Owner.Controls.Add(Tabs);
		Owner.ClientSize = new Size(800, 400);
		Tabs.TabPages.AddRange(new[] { new TabPage("First"), new TabPage("Second"), new TabPage("Third") });
		_ = Owner.Handle;
		_ = Tabs.Handle;
		var First = Tabs.TabPages[0];
		Tabs.BeginDrag(First);
		Tabs.DragTo(Center(Tabs.GetTabRect(2)));
		Assert.That(Tabs.TabPages.IndexOf(First), Is.EqualTo(2));
		Assert.That(Tabs.PreprocessEscape(), Is.True);
		Assert.That(Tabs.TabPages.IndexOf(First), Is.Zero);
		Assert.That(CancelButton.ClickCount, Is.Zero, "Cancelling a tab drag must not also cancel its containing form");
		Assert.That(Tabs.Capture, Is.False);
		Assert.That(Tabs.PreprocessEscape(), Is.True);
		Assert.That(CancelButton.ClickCount, Is.EqualTo(1), "Escape must retain normal form behavior after the drag ends");
	}

	[Test]
	public void LosingCaptureRestoresOrderAndPreventsALateUndock() {
		using var Tabs = new TestTabs { Size = new Size(800, 400) };
		using var OtherControl = new Control();
		using var Screen = new DragScreen();
		Tabs.TabPages.AddRange(new[] { new TabPage("First") { Tag = Screen }, new TabPage("Second"), new TabPage("Third") });
		_ = Tabs.Handle;
		var UndockRequests = 0;
		Tabs.ScreenUndockRequested += _ => UndockRequests++;
		var First = Tabs.TabPages[0];
		Tabs.BeginDrag(First);
		Tabs.DragTo(Center(Tabs.GetTabRect(2)));
		Assert.That(Tabs.TabPages.IndexOf(First), Is.EqualTo(2));
		OtherControl.Capture = true;
		Assert.That(Tabs.Capture, Is.False);
		Assert.That(Tabs.TabPages.IndexOf(First), Is.Zero);
		Assert.That(Tabs.SelectedTab, Is.SameAs(First));
		Tabs.EndDrag(new Point(20, 150));
		Assert.That(UndockRequests, Is.Zero);
	}

	[Test]
	public void NativeMouseUpCommitsOrderAndReleasesCapture() {
		using var Tabs = new TestTabs { Size = new Size(800, 400) };
		Tabs.TabPages.AddRange(new[] { new TabPage("First"), new TabPage("Second"), new TabPage("Third") });
		_ = Tabs.Handle;
		var First = Tabs.TabPages[0];
		Tabs.BeginDrag(First);
		Tabs.DragTo(Center(Tabs.GetTabRect(2)));
		Tabs.EndNativeDrag(Center(Tabs.GetTabRect(2)));
		Assert.That(Tabs.TabPages.IndexOf(First), Is.EqualTo(2), "Normal capture release on mouse-up must not roll back the drag");
		Assert.That(Tabs.SelectedTab, Is.SameAs(First));
		Assert.That(Tabs.Capture, Is.False);
	}

	[Test]
	public void NativeMouseUpOutsideTheTabBarStillRequestsUndocking() {
		using var Tabs = new TestTabs { Size = new Size(800, 400) };
		using var Screen = new DragScreen();
		Tabs.TabPages.AddRange(new[] { new TabPage("First") { Tag = Screen }, new TabPage("Second") });
		_ = Tabs.Handle;
		ApplicationScreen? RequestedScreen = null;
		Tabs.ScreenUndockRequested += Requested => RequestedScreen = Requested;
		Tabs.BeginDrag(Tabs.TabPages[0]);
		var DropLocation = new Point(20, 150);
		Tabs.DragTo(DropLocation);
		Tabs.EndNativeDrag(DropLocation);
		Assert.That(RequestedScreen, Is.SameAs(Screen));
		Assert.That(Tabs.Capture, Is.False);
	}

	[TestCase(1)]
	[TestCase(3)]
	public void StaleDrawDuringDragOutDoesNotInterruptTabRemoval(int PageCount) {
		using var Tabs = new TestTabs { Size = new Size(800, 400) };
		using var Screen = new DragScreen();
		for (var Index = 0; Index < PageCount; Index++)
			Tabs.TabPages.Add(new TabPage($"Screen {Index}"));
		_ = Tabs.Handle;
		var RemovedIndex = PageCount - 1;
		using var RemovedPage = Tabs.TabPages[RemovedIndex];
		RemovedPage.Tag = Screen;
		var DrawBounds = Tabs.GetTabRect(RemovedIndex);
		using var Canvas = new Bitmap(Tabs.Width, Tabs.Height);
		using var SurfaceGraphics = Graphics.FromImage(Canvas);
		var StaleDraws = 0;
		Tabs.ScreenUndockRequested += _ => {
			Tabs.TabPages.Remove(RemovedPage);
			StaleDraws++;
			Tabs.DrawTab(SurfaceGraphics, RemovedIndex, DrawBounds);
		};
		Tabs.BeginDrag(RemovedPage);
		var DropLocation = new Point(20, 150);
		Tabs.DragTo(DropLocation);
		Assert.That(() => Tabs.EndNativeDrag(DropLocation), Throws.Nothing);
		Assert.That(StaleDraws, Is.EqualTo(1), "Exercise a stale draw callback while handling the native mouse-up that removes the tab");
		Assert.That(Tabs.TabCount, Is.EqualTo(PageCount - 1));
		Assert.That(Tabs.Capture, Is.False);
		if (Tabs.TabCount > 0) {
			var ValidDraws = 0;
			Tabs.DrawItem += (_, _) => ValidDraws++;
			Tabs.DrawTab(SurfaceGraphics, 0, Tabs.GetTabRect(0));
			Assert.That(ValidDraws, Is.EqualTo(1), "Remaining tabs must continue to paint after undocking");
		}
	}

	[Test]
	public void StaleDrawAfterLastTabRemovalWithDockPreviewIsIgnored() {
		using var Tabs = new TestTabs { Size = new Size(800, 400) };
		Tabs.TabPages.Add(new TabPage("Screen"));
		_ = Tabs.Handle;
		var DrawBounds = Tabs.GetTabRect(0);
		Tabs.ShowDockPreview("Design", new Point(799, 5));
		using var Canvas = new Bitmap(Tabs.Width, Tabs.Height);
		using var SurfaceGraphics = Graphics.FromImage(Canvas);
		Tabs.TabPages.Clear();
		Assert.That(() => Tabs.DrawTab(SurfaceGraphics, 0, DrawBounds), Throws.Nothing);
		Assert.That(Tabs.DockPreviewVisible, Is.False);
		Assert.That(Tabs.SelectedTab, Is.Null);
	}

	[TestCase(-1)]
	[TestCase(0)]
	public void DrawWithoutATabIsIgnored(int Index) {
		using var Tabs = new TestTabs();
		using var Canvas = new Bitmap(100, 30);
		using var SurfaceGraphics = Graphics.FromImage(Canvas);
		Assert.That(() => Tabs.DrawTab(SurfaceGraphics, Index, new Rectangle(0, 0, 100, 30)), Throws.Nothing);
	}

	[Test]
	public void DockPreviewNeverChangesTabsWidthsSelectionOrPageLayout() {
		using var Tabs = new ApplicationScreenTabControl { Size = new Size(800, 400) };
		Tabs.TabPages.AddRange(new[] { new TabPage("First"), new TabPage("Second") });
		_ = Tabs.Handle;
		var OriginalPages = Tabs.TabPages.Cast<TabPage>().ToArray();
		var OriginalBounds = Enumerable.Range(0, Tabs.TabCount).Select(Tabs.GetTabRect).ToArray();
		var Selected = Tabs.SelectedTab;
		var PageBounds = Selected!.Bounds;
		var Layouts = 0;
		var Selections = 0;
		Tabs.Layout += (_, _) => Layouts++;
		Tabs.SelectedIndexChanged += (_, _) => Selections++;
		Tabs.ShowDockPreview("Design", new Point(2, 5));
		Assert.That(Tabs.DockPreviewVisible, Is.True);
		Assert.That(Tabs.DockPreviewIndex, Is.Zero);
		Tabs.ShowDockPreview("Design", new Point(799, 5));
		Assert.That(Tabs.DockPreviewIndex, Is.EqualTo(2));
		Tabs.HideDockPreview();
		Assert.That(Tabs.DockPreviewVisible, Is.False);
		Assert.That(Tabs.DockPreviewIndex, Is.EqualTo(-1));
		Assert.That(Tabs.TabPages.Cast<TabPage>(), Is.EqualTo(OriginalPages));
		Assert.That(Enumerable.Range(0, Tabs.TabCount).Select(Tabs.GetTabRect), Is.EqualTo(OriginalBounds));
		Assert.That(Tabs.SelectedTab, Is.SameAs(Selected));
		Assert.That(Selected.Bounds, Is.EqualTo(PageBounds));
		Assert.That(Layouts, Is.Zero);
		Assert.That(Selections, Is.Zero);
	}

	[Test]
	public void RepeatedPreviewOverTheSameInsertionPointDoesNotRepaint() {
		using var Tabs = new TestTabs { Size = new Size(800, 400) };
		Tabs.TabPages.AddRange(new[] { new TabPage("First"), new TabPage("Second") });
		_ = Tabs.Handle;
		Tabs.ShowDockPreview("Design", new Point(3, 5));
		Tabs.Update();
		var Paints = Tabs.NativePaintCount;
		var Invalidations = 0;
		Tabs.Invalidated += (_, _) => Invalidations++;
		for (var Index = 0; Index < 100; Index++) {
			Tabs.ShowDockPreview("Design", new Point(3 + Index % 3, 5));
			Tabs.Update();
		}
		Assert.That(Invalidations, Is.Zero, "Moving within the same insertion position must not repaint the tab bar");
		Assert.That(Tabs.NativePaintCount, Is.EqualTo(Paints));
		Tabs.HideDockPreview();
		Invalidations = 0;
		Tabs.HideDockPreview();
		Assert.That(Invalidations, Is.Zero);
	}

	[TestCase(0)]
	[TestCase(2)]
	public void DockPreviewPaintsALargeHighlightedTabAndRestoresTheHeader(int PageCount) {
		using var Tabs = new ApplicationScreenTabControl { Size = new Size(800, 400) };
		for (var Index = 0; Index < PageCount; Index++)
			Tabs.TabPages.Add(new TabPage($"Screen {Index}"));
		_ = Tabs.Handle;
		using var Original = new Bitmap(Tabs.Width, Tabs.Height);
		using var Preview = new Bitmap(Tabs.Width, Tabs.Height);
		using var Restored = new Bitmap(Tabs.Width, Tabs.Height);
		Tabs.DrawToBitmap(Original, Tabs.ClientRectangle);
		Tabs.ShowDockPreview("Design", new Point(3, 5));
		Tabs.DrawToBitmap(Preview, Tabs.ClientRectangle);
		var HighlightPixels = 0;
		var CaptionPixels = 0;
		for (var Y = 0; Y < Tabs.TabStripBounds.Bottom; Y++) {
			for (var X = 0; X < Tabs.Width; X++) {
				var Pixel = Preview.GetPixel(X, Y).ToArgb();
				if (Pixel == Tabs.DockPreviewBackColor.ToArgb())
					HighlightPixels++;
				if (Pixel == Tabs.DockPreviewForeColor.ToArgb() && Pixel != Original.GetPixel(X, Y).ToArgb())
					CaptionPixels++;
			}
		}
		Assert.That(HighlightPixels, Is.GreaterThan(Tabs.LogicalToDeviceUnits(120) * Tabs.LogicalToDeviceUnits(10)), "The docking cue must fill a tab-sized area, not just a thin line");
		Assert.That(CaptionPixels, Is.GreaterThan(30), "The highlighted docking tab must paint its contrasting Release to dock caption");
		Tabs.HideDockPreview();
		Tabs.DrawToBitmap(Restored, Tabs.ClientRectangle);
		for (var Y = 0; Y < Tabs.TabStripBounds.Bottom; Y++)
			for (var X = 0; X < Tabs.Width; X++)
				Assert.That(Restored.GetPixel(X, Y), Is.EqualTo(Original.GetPixel(X, Y)), "Hiding the preview must restore the underlying header");
	}

	[TestCase(96)]
	[TestCase(144)]
	[TestCase(192)]
	public void NativePaintPreservesCaptionInAnOffsetDockingBuffer(int Dpi) {
		using var Tabs = new TestTabs { Size = new Size(2400, 400) };
		Tabs.TabPages.AddRange(new[] {
			new TabPage("Application settings"), new TabPage("Design document one"), new TabPage("Design document two"), new TabPage("Design document three")
		});
		_ = Tabs.Handle;
		Tabs.ApplyDpiChange(Tabs.DeviceDpi, Dpi);
		Tabs.RuntimeCaptureSize = new Size((int)Math.Round(Tabs.MaximumTabWidth * Dpi / 96.0), Tabs.TabStripBounds.Height);
		var PaintCount = Tabs.NativePaintCount;
		Tabs.ShowDockPreview("Design and planning worksheet with a complete descriptive title", new Point(2300, 5));
		// Parked test controls do not receive automatic paint messages; dispatch WM_PAINT to the same native handle after invalidation.
		Tabs.PaintNativePreview();
		Assert.That(Tabs.NativePaintCount, Is.GreaterThan(PaintCount), "Exercise the native WM_PAINT handler, not the unbuffered WM_PRINT bitmap path");
		Assert.That(Tabs.RuntimeBufferOffsetX, Is.LessThan(-Tabs.RuntimeCaptureSize.Width), "The native buffer origin must be far enough right to expose text drawn in the wrong coordinate system");
		Assert.That(Tabs.RuntimeCaptionPixels, Is.GreaterThan(30), "The actual WM_PAINT buffer must contain the Release to dock caption before it is copied onto the header");
		PaintCount = Tabs.NativePaintCount;
		for (var Index = 0; Index < 10; Index++) {
			Tabs.ShowDockPreview("Design and planning worksheet with a complete descriptive title", new Point(2300 + Index, 5));
			Tabs.Update();
		}
		Assert.That(Tabs.NativePaintCount, Is.EqualTo(PaintCount), "Keeping the tooltip and preview on the same insertion target must not refresh the header");
	}

	[Test]
	public void DockPreviewUsesExplorerBarColorsAndSupportsThemeChangesWithoutRelayout() {
		using var Tabs = new ApplicationScreenTabControl { Size = new Size(800, 400) };
		using var Pane = new TaskPane();
		Assert.That(Tabs.DockPreviewBackColor, Is.EqualTo(Pane.GradientStartColor), "Docking must use the same blue as the navigation pane");
		Assert.That(Tabs.DockPreviewForeColor.GetBrightness(), Is.LessThan(Tabs.DockPreviewBackColor.GetBrightness()));
		Tabs.TabPages.AddRange(new[] { new TabPage("First"), new TabPage("Second") });
		_ = Tabs.Handle;
		var Selected = Tabs.SelectedTab;
		var OriginalBounds = Enumerable.Range(0, Tabs.TabCount).Select(Tabs.GetTabRect).ToArray();
		Tabs.ShowDockPreview("Design", new Point(3, 5));
		var Layouts = 0;
		var Invalidations = 0;
		Tabs.Layout += (_, _) => Layouts++;
		Tabs.Invalidated += (_, _) => Invalidations++;
		Tabs.DockPreviewBackColor = Color.DarkSlateBlue;
		Tabs.DockPreviewForeColor = Color.White;
		Assert.That(Invalidations, Is.EqualTo(2));
		Invalidations = 0;
		Tabs.DockPreviewBackColor = Color.DarkSlateBlue;
		Tabs.DockPreviewForeColor = Color.White;
		Assert.That(Invalidations, Is.Zero, "Reapplying the same navigation colors must not refresh the preview");
		Assert.That(Layouts, Is.Zero);
		Assert.That(Tabs.SelectedTab, Is.SameAs(Selected));
		Assert.That(Enumerable.Range(0, Tabs.TabCount).Select(Tabs.GetTabRect), Is.EqualTo(OriginalBounds));
		using var Preview = new Bitmap(Tabs.Width, Tabs.Height);
		Tabs.DrawToBitmap(Preview, Tabs.ClientRectangle);
		Assert.That(Preview.GetPixel(20, 3), Is.EqualTo(Color.FromArgb(Tabs.DockPreviewBackColor.ToArgb())), "The painted cue must use the updated navigation theme");
	}

	[Test]
	public void PreviewInvalidatesOnlyTheOldAndNewHeaderAreas() {
		using var Tabs = new ApplicationScreenTabControl { Size = new Size(800, 400) };
		Tabs.TabPages.AddRange(new[] { new TabPage("First"), new TabPage("Second") });
		_ = Tabs.Handle;
		var Invalidations = 0;
		Tabs.Invalidated += (_, Args) => {
			Invalidations++;
			Assert.That(Args.InvalidRect.Height, Is.LessThanOrEqualTo(Tabs.TabStripBounds.Height));
			Assert.That(Args.InvalidRect.Width, Is.InRange(Tabs.LogicalToDeviceUnits(180), Tabs.LogicalToDeviceUnits(Tabs.MaximumTabWidth)));
		};
		Tabs.ShowDockPreview("Design", new Point(3, 5));
		Tabs.ShowDockPreview("Design", new Point(799, 5));
		Tabs.HideDockPreview();
		Assert.That(Invalidations, Is.EqualTo(4), "Showing, moving, and hiding a docking preview must only invalidate the affected rectangles");
	}

	[Test]
	public void EmptyPreviewDoesNotCreateATabOrSelectAPage() {
		using var Tabs = new ApplicationScreenTabControl { Size = new Size(800, 400) };
		_ = Tabs.Handle;
		var Header = Tabs.TabStripBounds;
		Assert.That(Header.Width, Is.EqualTo(Tabs.ClientSize.Width));
		Assert.That(Header.Height, Is.GreaterThan(Tabs.Font.Height));
		Assert.That(Header.Bottom, Is.LessThan(Tabs.ClientSize.Height));
		Tabs.ShowDockPreview("Design", Center(Header));
		Assert.That(Tabs.DockPreviewIndex, Is.Zero);
		Assert.That(Tabs.TabCount, Is.Zero);
		Assert.That(Tabs.SelectedIndex, Is.EqualTo(-1));
		Assert.That(Tabs.TabStripBounds, Is.EqualTo(Header));
		Tabs.HideDockPreview();
		Assert.That(Tabs.TabCount, Is.Zero);
	}

	[Test]
	public void TabWidthsFitTheirTitlesUntilTheMaximumAndKeepFullToolTips() {
		using var Tabs = new ApplicationScreenTabControl { Size = new Size(1100, 400), MaximumTabWidth = 260 };
		var LongTitle = new string('W', 150);
		Tabs.TabPages.AddRange(new[] { new TabPage("Edit"), new TabPage("Application settings"), new TabPage(LongTitle) });
		_ = Tabs.Handle;
		var MaximumWidth = (int)Math.Round(Tabs.MaximumTabWidth * Tabs.DeviceDpi / 96.0);
		Assert.That(Tabs.GetTabRect(0).Width, Is.LessThan(Tabs.GetTabRect(1).Width));
		Assert.That(Tabs.GetTabRect(1).Width, Is.LessThan(Tabs.GetTabRect(2).Width));
		Assert.That(Tabs.GetTabRect(2).Width, Is.LessThanOrEqualTo(MaximumWidth));
		Assert.That(Tabs.GetTabRect(2).Width, Is.GreaterThan(MaximumWidth - 30));
		Assert.That(Tabs.TabPages[2].Text, Is.EqualTo(LongTitle));
		Assert.That(Tabs.TabPages[2].ToolTipText, Is.EqualTo(LongTitle));
		Assert.That(Tabs.ShowToolTips, Is.True);
		Tabs.TabPages[0].Text = LongTitle;
		Assert.That(Tabs.GetTabRect(0).Width, Is.LessThanOrEqualTo(MaximumWidth));
		Assert.That(Tabs.TabPages[0].ToolTipText, Is.EqualTo(LongTitle));
		Tabs.TabPages[0].Text = "A";
		Assert.That(Tabs.GetTabRect(0).Width, Is.LessThan(Tabs.GetTabRect(1).Width));
	}

	[Test]
	public void TitlesBelowTheMaximumHaveRoomForEveryLiteralCharacter() {
		using var Tabs = new ApplicationScreenTabControl { Size = new Size(1800, 400), MaximumTabWidth = 260 };
		var Titles = new[] { "Application settings", "Finance & Administration", "A && B && C", "&&&&&&&&&&&&&&&&", "A very wide application settings" };
		Tabs.TabPages.AddRange(Titles.Select(Title => new TabPage(Title)).ToArray());
		_ = Tabs.Handle;
		var ReservedWidth = Tabs.LogicalToDeviceUnits(16) + 3 * Tabs.LogicalToDeviceUnits(6);
		for (var Index = 0; Index < Titles.Length; Index++) {
			var TextWidth = TextRenderer.MeasureText(Titles[Index], Tabs.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
			Assert.That(Tabs.GetTabRect(Index).Width, Is.LessThan(Tabs.LogicalToDeviceUnits(Tabs.MaximumTabWidth)));
			Assert.That(Tabs.GetTabRect(Index).Width - ReservedWidth, Is.GreaterThanOrEqualTo(TextWidth), $"The complete title '{Titles[Index]}' must fit before the close button");
			Assert.That(Tabs.TabPages[Index].Text, Is.EqualTo(Titles[Index]));
			Assert.That(Tabs.TabPages[Index].ToolTipText, Is.EqualTo(Titles[Index]));
		}
	}

	[Test]
	public void WidthCapSurvivesFontChangesAndHandleRecreation() {
		using var Tabs = new TestTabs { Size = new Size(1100, 400), MaximumTabWidth = 180 };
		Tabs.TabPages.Add(new TabPage(new string('W', 100)));
		_ = Tabs.Handle;
		using var LargerFont = new Font(Tabs.Font.FontFamily, Tabs.Font.Size * 1.5f);
		Tabs.Font = LargerFont;
		var MaximumWidth = (int)Math.Round(Tabs.MaximumTabWidth * Tabs.DeviceDpi / 96.0);
		Assert.That(Tabs.GetTabRect(0).Width, Is.LessThanOrEqualTo(MaximumWidth));
		Tabs.RecreateNativeHandle();
		Assert.That(Tabs.GetTabRect(0).Width, Is.LessThanOrEqualTo(MaximumWidth));
		Tabs.MaximumTabWidth = 120;
		Assert.That(Tabs.GetTabRect(0).Width, Is.LessThanOrEqualTo((int)Math.Round(120 * Tabs.DeviceDpi / 96.0)));
	}

	[TestCase(144)]
	[TestCase(192)]
	public void DpiChangesScaleTabPaddingHeaderAndWidthCap(int Dpi) {
		using var Tabs = new TestTabs { Size = new Size(1100, 400), MaximumTabWidth = 180 };
		Tabs.TabPages.Add(new TabPage(new string('W', 100)));
		_ = Tabs.Handle;
		var OriginalWidth = Tabs.GetTabRect(0).Width;
		var OriginalHeaderHeight = Tabs.TabStripBounds.Height;
		var OriginalPadding = Tabs.Padding;
		var OriginalDpi = Tabs.DeviceDpi;
		Tabs.ApplyDpiChange(OriginalDpi, Dpi);
		Assert.That(Tabs.Padding.X, Is.EqualTo((int)Math.Round(OriginalPadding.X * Dpi / (double)OriginalDpi)));
		Assert.That(Tabs.Padding.Y, Is.EqualTo((int)Math.Round(OriginalPadding.Y * Dpi / (double)OriginalDpi)));
		Assert.That(Tabs.TabStripBounds.Height, Is.GreaterThan(OriginalHeaderHeight));
		Assert.That(Tabs.GetTabRect(0).Width, Is.GreaterThan(OriginalWidth));
		Assert.That(Tabs.GetTabRect(0).Width, Is.LessThanOrEqualTo((int)Math.Round(180 * Dpi / 96.0)));
		var PreviewBounds = Rectangle.Empty;
		Tabs.Invalidated += (_, Args) => PreviewBounds = Args.InvalidRect;
		Tabs.ShowDockPreview(new string('W', 100), new Point(3, 5));
		Assert.That(PreviewBounds.Width, Is.EqualTo((int)Math.Round(180 * Dpi / 96.0)), "The large docking preview must scale with the header");
		Assert.That(PreviewBounds.Height, Is.EqualTo(Tabs.TabStripBounds.Height));
	}

	[Test]
	public void LargeFontCannotRaiseTheNativeMinimumAboveTheConfiguredMaximum() {
		using var Tabs = new TestTabs { Size = new Size(1100, 400), MaximumTabWidth = 64 };
		var Title = new string('W', 100);
		Tabs.TabPages.Add(new TabPage(Title));
		_ = Tabs.Handle;
		using var LargeFont = new Font(Tabs.Font.FontFamily, 72);
		Tabs.Font = LargeFont;
		var MaximumWidth = (int)Math.Round(64 * Tabs.DeviceDpi / 96.0);
		Assert.That(Tabs.GetTabRect(0).Width, Is.LessThanOrEqualTo(MaximumWidth));
		Tabs.RecreateNativeHandle();
		Assert.That(Tabs.GetTabRect(0).Width, Is.LessThanOrEqualTo(MaximumWidth));
		Assert.That(Tabs.TabPages[0].Text, Is.EqualTo(Title));
		Assert.That(Tabs.TabPages[0].ToolTipText, Is.EqualTo(Title));
	}

	[Test]
	public void EmptyHostAlsoShowsDockPreviewAndRemovesItWhenLeaving() {
		using var Host = new ApplicationScreenHost { Size = new Size(800, 400), ScreenMode = ScreenMode.MultiView };
		var Screen = new DragScreen();
		Host.ShowScreen(Screen);
		Host.UndockScreen(Screen);
		_ = Host.Handle;
		Assert.That(Host.UpdateDockPreview(Screen, Host.PointToScreen(new Point(10, 10))), Is.True);
		Assert.That(Host.TabControl.DockPreviewVisible, Is.True);
		Assert.That(Host.OpenScreens.Count, Is.EqualTo(1), "A preview is not an open screen");
		Assert.That(Host.ActiveScreen, Is.Null);
		Assert.That(Host.UpdateDockPreview(Screen, Host.PointToScreen(new Point(-30, -30))), Is.False);
		Assert.That(Host.TabControl.TabCount, Is.Zero);
	}

	[Test]
	public void DropUsesPreviewInsertionPositionAndCancelledDockClearsHint() {
		using var Host = new ApplicationScreenHost { Size = new Size(800, 400), ScreenMode = ScreenMode.MultiView };
		var First = new DragScreen { Title = "First" };
		var Second = new DragScreen { Title = "Second" };
		var Detached = new DragScreen { Title = "Detached" };
		Host.ShowScreen(First);
		Host.ShowScreen(Second);
		Host.ShowScreen(Detached);
		Host.UndockScreen(Detached);
		_ = Host.Handle;
		var DropLocation = Host.TabControl.PointToScreen(new Point(1, 5));
		Host.UpdateDockPreview(Detached, DropLocation);
		Assert.That(Host.CompleteScreenDock(Detached, DropLocation), Is.True);
		Assert.That(Host.TabControl.TabPages[0].Tag, Is.SameAs(Detached));
		Assert.That(Host.TabControl.DockPreviewVisible, Is.False);
		Assert.That(Host.ActiveScreen, Is.SameAs(Detached));
		Host.UndockScreen(Detached);
		Detached.ScreenHidden += (_, Args) => Args.Cancel = true;
		Host.UpdateDockPreview(Detached, DropLocation);
		Assert.That(Host.CompleteScreenDock(Detached, DropLocation), Is.False);
		Assert.That(Host.TabControl.DockPreviewVisible, Is.False);
		Assert.That(Host.IsScreenUndocked(Detached), Is.True);
	}

	[Test]
	public void ReorderingKeepsActiveScreenAndDoesNotRedisplayIt() {
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var First = new DragScreen();
		var Second = new DragScreen();
		Host.ShowScreen(First);
		Host.ShowScreen(Second);
		var Shows = 0;
		Second.ScreenDisplayed += (_, _) => Shows++;
		Host.TabControl.MoveTab(Host.TabControl.SelectedTab!, 0);
		Assert.That(Host.ActiveScreen, Is.SameAs(Second));
		Assert.That(Host.TabControl.SelectedTab!.Tag, Is.SameAs(Second));
		Assert.That(Shows, Is.Zero);
	}

	private static Point Center(Rectangle Bounds) => new(Bounds.Left + Bounds.Width / 2, Bounds.Top + Bounds.Height / 2);

	private class TestTabs : ApplicationScreenTabControl {
		private bool _processingNativePaint;
		[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
		public Size RuntimeCaptureSize { get; set; }
		public int RuntimeCaptionPixels;
		public float RuntimeBufferOffsetX;

		public int NativePaintCount { get; private set; }

		public void ApplyDpiChange(int PreviousDpi, int NewDpi) => RescaleConstantsForDpi(PreviousDpi, NewDpi);

		public void RecreateNativeHandle() => RecreateHandle();

		public void PaintNativePreview() => WinAPI.USER32.SendMessage(Handle, 0x000F, IntPtr.Zero, IntPtr.Zero);

		public void DrawTab(Graphics Graphics, int Index, Rectangle Bounds)
			=> OnDrawItem(new DrawItemEventArgs(Graphics, Font, Bounds, Index, DrawItemState.Default));

		public void BeginDrag(TabPage Page) {
			SelectedTab = Page;
			var Location = Center(GetTabRect(TabPages.IndexOf(Page)));
			OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, Location.X, Location.Y, 0));
		}

		public void DragTo(Point Location) => OnMouseMove(new MouseEventArgs(MouseButtons.Left, 0, Location.X, Location.Y, 0));

		public void EndDrag(Point Location) => OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, Location.X, Location.Y, 0));

		public void EndNativeDrag(Point Location) {
			var MouseMessage = Message.Create(Handle, 0x0202, IntPtr.Zero, (IntPtr)(Location.X | Location.Y << 16));
			WndProc(ref MouseMessage);
		}

		public bool PreprocessEscape() {
			var KeyMessage = Message.Create(Handle, 0x0100, (IntPtr)Keys.Escape, IntPtr.Zero);
			return PreProcessMessage(ref KeyMessage);
		}

		protected override void WndProc(ref Message Message) {
			if (Message.Msg == 0x000F)
				NativePaintCount++;
			var WasProcessingNativePaint = _processingNativePaint;
			_processingNativePaint = Message.Msg == 0x000F;
			using var PaintScope = Tools.Scope.ExecuteOnDispose(() => _processingNativePaint = WasProcessingNativePaint);
			base.WndProc(ref Message);
		}

		protected override void DrawDockPreview(Graphics Surface) {
			base.DrawDockPreview(Surface);
			if (!_processingNativePaint || RuntimeCaptureSize.IsEmpty)
				return;
			using var Transform = Surface.Transform;
			RuntimeBufferOffsetX = Transform.OffsetX;
			var DeviceContext = Surface.GetHdc();
			using var ContextScope = Tools.Scope.ExecuteOnDispose(() => Surface.ReleaseHdc(DeviceContext));
			RuntimeCaptionPixels = 0;
			var CaptionColor = (uint)ColorTranslator.ToWin32(DockPreviewForeColor);
			for (var Y = 0; Y < RuntimeCaptureSize.Height; Y++)
				for (var X = 0; X < RuntimeCaptureSize.Width; X++)
					if (WinAPI.GDI32.GetPixel(DeviceContext, X, Y) == CaptionColor)
						RuntimeCaptionPixels++;
		}
	}

	private class ProbeCancelButton : IButtonControl {
		public DialogResult DialogResult { get; set; }
		public int ClickCount { get; private set; }
		public void NotifyDefault(bool Value) { }
		public void PerformClick() => ClickCount++;
	}

	private class DragScreen : ApplicationScreen {
		public DragScreen() => ActivationMode = ScreenActivationMode.MultiInstance;
	}
}
