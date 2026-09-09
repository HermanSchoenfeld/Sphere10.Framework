// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class NavigationPaneTests {
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
	public void NavigationWidthTracksUserDividerAndExplicitWidth() {
		using var Form = new BlockMainForm();
		var SplitContainer = Form.Controls.OfType<SplitContainer>().Single();
		SplitContainer.SplitterDistance = 347;
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(347), "Persisted width must follow the user's divider position");
		Form.NavigationPaneWidth = 420;
		Assert.That(SplitContainer.SplitterDistance, Is.EqualTo(420));
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(420));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void NavigationWidthCanBeRestoredWhileHidden(bool FilledScreen) {
		using var Form = new BlockMainForm { ScreenMode = ScreenMode.MultiView, NavigationPaneWidth = 355 };
		var SplitContainer = Form.Controls.OfType<SplitContainer>().Single();
		if (FilledScreen)
			Form.ShowScreen(new NavigationScreen { DisplayMode = ScreenDisplayMode.Filled });
		else
			Form.NavigationPaneCollapsed = true;
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(355));
		Form.NavigationPaneWidth = 410;
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(410), "Hidden sidebars retain the restored width until shown");
		Assert.That(SplitContainer.Panel1Collapsed, Is.True, "Restoring width must not reveal navigation on a filled screen");
		if (FilledScreen)
			Form.ShowScreen(new NavigationScreen());
		else
			Form.NavigationPaneCollapsed = false;
		Assert.That(SplitContainer.SplitterDistance, Is.EqualTo(410));
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(410));
	}

	[TestCase(1)]
	[TestCase(int.MaxValue)]
	public void NavigationWidthClampsToCurrentWindowLimits(int RequestedWidth) {
		using var Form = new BlockMainForm { ClientSize = new Size(400, 450) };
		var SplitContainer = Form.Controls.OfType<SplitContainer>().Single();
		Form.NavigationPaneWidth = RequestedWidth;
		Assert.That(Form.NavigationPaneWidth,
			Is.InRange(SplitContainer.Panel1MinSize, SplitContainer.Width - SplitContainer.SplitterWidth - SplitContainer.Panel2MinSize));
		var ClampedWidth = Form.NavigationPaneWidth;
		Form.NavigationPaneCollapsed = true;
		Form.NavigationPaneCollapsed = false;
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(ClampedWidth));
	}

	[TestCase(144)]
	[TestCase(192)]
	public void RestoredHiddenNavigationWidthFollowsDpiChanges(int NewDpi) {
		using var Form = new ProbeMainForm { ClientSize = new Size(1800, 900), NavigationPaneCollapsed = true, NavigationPaneWidth = 347 };
		var ExpectedWidth = (int)Math.Round(347 * (double)NewDpi / Form.DeviceDpi);
		Form.RescaleDpi(Form.DeviceDpi, NewDpi);
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(ExpectedWidth));
		Form.NavigationPaneCollapsed = false;
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(ExpectedWidth));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void NavigationWidthCannotExceedConfiguredMaximum(bool Collapsed) {
		using var Form = new BlockMainForm { ClientSize = new Size(1280, 900), NavigationPaneCollapsed = Collapsed };
		Assert.That(Form.MaximumNavigationPaneWidth, Is.EqualTo(480));
		Form.NavigationPaneWidth = int.MaxValue;
		var ExpectedWidth = (int)Math.Round(480 * Form.DeviceDpi / 96.0);
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(ExpectedWidth));
		Form.NavigationPaneCollapsed = false;
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(ExpectedWidth), "Restoring an oversized saved preference must use the same limit as resizing the menu");
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ReducingMaximumNavigationWidthConstrainsExistingPreference(bool Collapsed) {
		using var Form = new BlockMainForm { ClientSize = new Size(1280, 900), NavigationPaneWidth = 460, NavigationPaneCollapsed = Collapsed };
		Form.MaximumNavigationPaneWidth = 340;
		var ExpectedWidth = (int)Math.Round(340 * Form.DeviceDpi / 96.0);
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(ExpectedWidth));
		Form.NavigationPaneCollapsed = false;
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(ExpectedWidth));
	}

	[Test]
	public void DirectDividerChangesCannotBypassMaximumWidth() {
		using var Form = new BlockMainForm { ClientSize = new Size(1280, 900) };
		var SplitContainer = Form.Controls.OfType<SplitContainer>().Single();
		SplitContainer.SplitterDistance = 900;
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo((int)Math.Round(480 * Form.DeviceDpi / 96.0)));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void DividerDragStopsAtMaximumAndCanContinueBackwards(bool RightToLeft) {
		using var Form = new NavigationLayoutProbeMainForm {
			ClientSize = new Size(1280, 900), ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000)
		};
		Form.Show();
		var SplitContainer = Form.Controls.OfType<SplitContainer>().Single();
		SplitContainer.RightToLeft = RightToLeft ? System.Windows.Forms.RightToLeft.Yes : System.Windows.Forms.RightToLeft.No;
		var MaximumWidth = (int)Math.Round(480 * Form.DeviceDpi / 96.0);
		Assert.That(DragNavigationDivider(SplitContainer, MaximumWidth, MaximumWidth + 120), Is.True, "Reaching the boundary must keep the native drag gesture active");
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(MaximumWidth), "Releasing beyond the boundary must retain the maximum width");
		var SmallerWidth = MaximumWidth - 100;
		Assert.That(DragNavigationDivider(SplitContainer, MaximumWidth + 120, SmallerWidth), Is.True);
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(SmallerWidth), "The same gesture must allow moving back inside the range");
	}

	[TestCase(false)]
	[TestCase(true)]
	public void NarrowWindowKeepsContentRoomWhenSidebarIsVisible(bool CollapsedDuringResize) {
		using var Form = new BlockMainForm { ClientSize = new Size(1280, 900), NavigationPaneWidth = 480, NavigationPaneCollapsed = CollapsedDuringResize };
		var SplitContainer = Form.Controls.OfType<SplitContainer>().Single();
		Form.ClientSize = new Size(600, 500);
		Form.NavigationPaneCollapsed = false;
		var ContentWidth = (int)Math.Round(320 * Form.DeviceDpi / 96.0);
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(SplitContainer.Width - SplitContainer.SplitterWidth - ContentWidth));
		Assert.That(SplitContainer.Panel2.Width, Is.GreaterThanOrEqualTo(ContentWidth));
		Assert.That(SplitContainer.Width, Is.EqualTo(Form.ClientSize.Width), "The previous drag range must not force the container wider than the shrunken window");
		Form.ClientSize = new Size(1280, 900);
		Form.NavigationPaneWidth = 480;
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(480), "Growing the window must release the smaller window's drag range");
	}

	[TestCase(96)]
	[TestCase(144)]
	[TestCase(192)]
	public void MaximumNavigationWidthFollowsMonitorDpi(int NewDpi) {
		using var Form = new ProbeMainForm { ClientSize = new Size(2400, 1200), NavigationPaneCollapsed = true };
		Form.RescaleDpi(Form.DeviceDpi, NewDpi);
		Form.NavigationPaneWidth = int.MaxValue;
		Assert.That(Form.MaximumNavigationPaneWidth, Is.EqualTo(480), "The configured maximum stays in logical pixels");
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo((int)Math.Round(480 * NewDpi / 96.0)));
		Form.NavigationPaneCollapsed = false;
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo((int)Math.Round(480 * NewDpi / 96.0)));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void MinimizingAndRestoringKeepsNavigationWidthInsideNativeSplitterLimits(bool Collapsed) {
		using var Form = new NavigationResizeProbeMainForm {
			ClientSize = new Size(1280, 900), NavigationPaneWidth = 410, NavigationPaneCollapsed = Collapsed,
			ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea.Location, Opacity = 0
		};
		Form.Show();
		var LayoutErrors = Form.LayoutErrors;
		Assert.That(LayoutErrors, Is.Empty, "Showing the native probe must complete before minimize/restore is exercised.");
		var SplitContainer = Form.Controls.OfType<SplitContainer>().Single();
		var OriginalWidth = Form.NavigationPaneWidth;
		for (var Attempt = 0; Attempt < 3; Attempt++) {
			Assert.That(() => Form.WindowState = FormWindowState.Minimized, Throws.Nothing, "A minimized native window may temporarily give the docked splitter no usable width.");
			Assert.That(LayoutErrors, Is.Empty, "Native minimize layout must not report a thread exception.");
			Assert.That(Form.WindowState, Is.EqualTo(FormWindowState.Minimized));
			Assert.That(Form.NavigationPaneWidth, Is.EqualTo(OriginalWidth), "Settings captured while minimized must retain the usable navigation width.");
			Assert.That(() => Form.WindowState = FormWindowState.Normal, Throws.Nothing);
			Assert.That(LayoutErrors, Is.Empty, "Native restore layout must not report a thread exception.");
			Assert.That(Form.NavigationPaneWidth, Is.EqualTo(OriginalWidth), "Minimizing must not replace the user's remembered navigation width.");
			Assert.That(Form.NavigationPaneCollapsed, Is.EqualTo(Collapsed));
			if (!Collapsed)
				Assert.That(SplitContainer.SplitterDistance, Is.InRange(SplitContainer.Panel1MinSize, SplitContainer.Width - SplitContainer.SplitterWidth - SplitContainer.Panel2MinSize));
		}
	}

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(40)]
	public void TransientUnusableClientWidthDoesNotAssignAnInvalidSplitterDistance(int ClientWidth) {
		using var Form = new NavigationResizeProbeMainForm { MinimumSize = Size.Empty, ClientSize = new Size(1280, 900), NavigationPaneWidth = 410 };
		var SplitContainer = Form.Controls.OfType<SplitContainer>().Single();
		var OriginalWidth = Form.NavigationPaneWidth;
		Assert.That(() => Form.ClientSize = new Size(ClientWidth, 500), Throws.Nothing);
		Assert.That(Form.ClientSize.Width, Is.EqualTo(ClientWidth));
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(OriginalWidth), "An unusable transient client width must not replace the stored navigation preference.");
		Assert.That(() => {
			Form.NavigationPaneCollapsed = true;
			Form.NavigationPaneCollapsed = false;
			Form.ClientSize = new Size(1280, 900);
		}, Throws.Nothing);
		Assert.That(Form.LayoutErrors, Is.Empty);
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(OriginalWidth), "Restoring useful client bounds after collapse/reveal must restore the original navigation preference.");
		Assert.That(SplitContainer.SplitterDistance, Is.InRange(SplitContainer.Panel1MinSize, SplitContainer.Width - SplitContainer.SplitterWidth - SplitContainer.Panel2MinSize));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void HiddenNavigationCanBeRevealedAndHiddenWhileMinimizedWithoutLosingItsWidth(bool FilledScreen) {
		using var Form = new NavigationResizeProbeMainForm {
			ClientSize = new Size(1280, 900), NavigationPaneWidth = 410, ScreenMode = ScreenMode.MultiView, NavigationPaneCollapsed = !FilledScreen,
			ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea.Location, Opacity = 0
		};
		if (FilledScreen)
			Form.ShowScreen(new NavigationScreen { DisplayMode = ScreenDisplayMode.Filled });
		Form.Show();
		Assert.That(Form.LayoutErrors, Is.Empty, "Showing the native probe must complete before changing hidden navigation.");
		var SplitContainer = Form.Controls.OfType<SplitContainer>().Single();
		var OriginalWidth = Form.NavigationPaneWidth;
		Assert.That(SplitContainer.Panel1Collapsed, Is.True);
		Form.WindowState = FormWindowState.Minimized;
		Assert.That(Form.LayoutErrors, Is.Empty);
		Assert.That(() => {
			if (FilledScreen) {
				Form.ShowScreen(new NavigationScreen());
				Form.ShowScreen(new NavigationScreen { DisplayMode = ScreenDisplayMode.Filled });
			} else {
				Form.NavigationPaneCollapsed = false;
				Form.NavigationPaneCollapsed = true;
			}
		}, Throws.Nothing);
		Assert.That(Form.LayoutErrors, Is.Empty);
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(OriginalWidth));
		Assert.That(SplitContainer.Panel1Collapsed, Is.True);
		Form.WindowState = FormWindowState.Normal;
		if (FilledScreen)
			Form.ShowScreen(new NavigationScreen());
		else
			Form.NavigationPaneCollapsed = false;
		Assert.That(Form.LayoutErrors, Is.Empty);
		Assert.That(Form.NavigationPaneWidth, Is.EqualTo(OriginalWidth));
		Assert.That(SplitContainer.Panel1Collapsed, Is.False);
		Assert.That(SplitContainer.SplitterDistance, Is.InRange(SplitContainer.Panel1MinSize, SplitContainer.Width - SplitContainer.SplitterWidth - SplitContainer.Panel2MinSize));
	}

	[Test]
	public void DefaultSidebarFitsLongScreenNames() {
		using var Form = new NavigationLayoutProbeMainForm { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var Icon = new Bitmap(16, 16);
		using var Block = new ApplicationBlockBuilder()
			.WithName("Navigation width")
			.AddMenu(Menu => Menu.WithText("Screens")
				.AddScreenItem<NavigationScreen>("ApplicationServicesTester", Icon)
				.AddScreenItem<NavigationScreen>("FlagsCheckedListBox", Icon))
			.Build();
		Form.RegisterBlock(Block);
		Form.Show();
		var Pane = Form.PluginBindings[Block];
		var Menu = Pane.Expandos.Cast<Expando>().Single();
		Pane.DoLayout(true);
		Menu.DoLayout(true);
		foreach (var Item in Menu.Items.Cast<TaskItem>())
			Assert.That(Item.Width, Is.GreaterThanOrEqualTo(Item.PreferredWidth), $"'{Item.Text}' should fit beside its icon without wrapping at the default navigation font");
	}

	[TestCase(ScreenMode.SingleView)]
	[TestCase(ScreenMode.MultiView)]
	public void SidebarButtonPreservesWidthAndActiveScreen(ScreenMode Mode) {
		using var Form = new BlockMainForm { ScreenMode = Mode };
		var Screen = new NavigationScreen();
		Form.ShowScreen(Screen);
		var SplitContainer = Form.Controls.OfType<SplitContainer>().Single();
		SplitContainer.SplitterDistance = 280;
		var OriginalWidth = SplitContainer.SplitterDistance;
		var Toolbar = Form.Controls.OfType<ToolStrip>().Single(Strip => Strip is not MenuStrip && Strip is not StatusStrip);
		var Toggle = Toolbar.Items.OfType<SidebarToggleButton>().Single();
		Assert.That(Toggle.Checked, Is.True);
		Assert.That(Toggle.AccessibleName, Is.EqualTo("Hide sidebar"));
		Toggle.PerformClick();
		Assert.That(Form.NavigationPaneCollapsed, Is.True);
		Assert.That(SplitContainer.Panel1Collapsed, Is.True);
		Assert.That(Toggle.Checked, Is.False);
		Assert.That(Toggle.AccessibleName, Is.EqualTo("Show sidebar"));
		Assert.That(Toggle.ToolTipText, Does.Contain("Ctrl+Alt+M"));
		Assert.That(Form.ActiveScreen, Is.SameAs(Screen));
		Assert.That(Screen.ActionButton.Owner, Is.SameAs(Toolbar));
		Form.ClientSize = new Size(Form.ClientSize.Width + 120, Form.ClientSize.Height);
		Toggle.PerformClick();
		Assert.That(Form.NavigationPaneCollapsed, Is.False);
		Assert.That(SplitContainer.Panel1Collapsed, Is.False);
		Assert.That(Toggle.Checked, Is.True);
		Assert.That(SplitContainer.SplitterDistance, Is.EqualTo(OriginalWidth));
		Assert.That(Form.ActiveScreen, Is.SameAs(Screen));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SidebarButtonUsesExistingToolbarWithoutNavigationPanelsOrGutters(bool Collapsed) {
		using var Form = new BlockMainForm { NavigationPaneCollapsed = Collapsed };
		var SplitContainer = Form.Controls.OfType<SplitContainer>().Single();
		var Navigation = SplitContainer.Panel1.Controls.OfType<ApplicationBar>().Single();
		Assert.That(SplitContainer.Panel1.Controls.Cast<Control>(), Is.EqualTo(new Control[] { Navigation }));
		Assert.That(SplitContainer.Panel2.Controls.Cast<Control>(), Is.EqualTo(new Control[] { Form.ScreenHost }));
		Assert.That(Form.ScreenHost.Bounds, Is.EqualTo(SplitContainer.Panel2.ClientRectangle), "The screen host fills the entire content panel without a restore-button gutter");
		if (!Collapsed)
			Assert.That(Navigation.Bounds, Is.EqualTo(SplitContainer.Panel1.ClientRectangle), "The sidebar starts at the top without a separate button header");
		var Toolbar = Form.Controls.OfType<ToolStrip>().Single(Strip => Strip is not MenuStrip && Strip is not StatusStrip);
		var Toggle = Toolbar.Items.OfType<SidebarToggleButton>().Single();
		Assert.That(Toolbar.Items.IndexOf(Toggle), Is.Zero);
		Assert.That(Toggle.Overflow, Is.EqualTo(ToolStripItemOverflow.Never));
		Assert.That(Toggle.DisplayStyle, Is.EqualTo(ToolStripItemDisplayStyle.None));
		Assert.That(Toggle.AccessibilityObject.Role, Is.EqualTo(AccessibleRole.CheckButton));
		Assert.That(Toggle.AccessibilityObject.State.HasFlag(AccessibleStates.Checked), Is.EqualTo(!Collapsed));
	}

	[Test]
	public void SameSidebarButtonSurvivesScreenToolbarRebuildsAndFilledScreens() {
		using var Form = new BlockMainForm { ScreenMode = ScreenMode.MultiView };
		var Toolbar = Form.Controls.OfType<ToolStrip>().Single(Strip => Strip is not MenuStrip && Strip is not StatusStrip);
		var Toggle = Toolbar.Items.OfType<SidebarToggleButton>().Single();
		var NormalScreen = new NavigationScreen();
		var FilledScreen = new NavigationScreen { DisplayMode = ScreenDisplayMode.Filled };
		Form.ShowScreen(NormalScreen);
		for (var Index = 0; Index < 3; Index++) {
			Form.ShowScreen(FilledScreen);
			Assert.That(Toggle.Enabled, Is.False);
			Assert.That(Toggle.Checked, Is.False);
			Assert.That(Form.NavigationPaneCollapsed, Is.False, "A filled screen does not change the saved sidebar preference");
			Form.ShowScreen(NormalScreen);
			Assert.That(Toolbar.Items.OfType<SidebarToggleButton>().Single(), Is.SameAs(Toggle));
			Assert.That(Toggle.IsDisposed, Is.False);
			Assert.That(Toggle.Enabled, Is.True);
			Assert.That(Toggle.Checked, Is.True);
		}
		Toggle.PerformClick();
		Assert.That(Form.NavigationPaneCollapsed, Is.True);
	}

	[Test]
	public void SidebarKeyboardShortcutUpdatesButtonState() {
		using var Form = new ProbeMainForm();
		var Toolbar = Form.Controls.OfType<ToolStrip>().Single(Strip => Strip is not MenuStrip && Strip is not StatusStrip);
		var Toggle = Toolbar.Items.OfType<SidebarToggleButton>().Single();
		Assert.That(Form.ToggleSidebarWithKeyboard(), Is.True);
		Assert.That(Form.NavigationPaneCollapsed, Is.True);
		Assert.That(Toggle.Checked, Is.False);
		Assert.That(Form.ToggleSidebarWithKeyboard(), Is.True);
		Assert.That(Form.NavigationPaneCollapsed, Is.False);
		Assert.That(Toggle.Checked, Is.True);
	}

	[TestCase(ScreenMode.SingleView, false)]
	[TestCase(ScreenMode.SingleView, true)]
	[TestCase(ScreenMode.MultiView, false)]
	[TestCase(ScreenMode.MultiView, true)]
	public void SwitchingAndClosingScreensRetainsNavigationPreference(ScreenMode Mode, bool Collapsed) {
		using var Form = new BlockMainForm { ScreenMode = Mode, NavigationPaneCollapsed = Collapsed };
		var First = new NavigationScreen();
		var Second = new NavigationScreen();
		Form.ShowScreen(First);
		Form.ShowScreen(Second);
		var SplitContainer = Form.Controls.OfType<SplitContainer>().Single();
		Assert.That(SplitContainer.Panel1Collapsed, Is.EqualTo(Collapsed));
		Assert.That(Form.NavigationPaneCollapsed, Is.EqualTo(Collapsed));
		Assert.That(Form.ScreenHost.CloseScreens(Form.ScreenHost.Screens), Is.True);
		Assert.That(SplitContainer.Panel1Collapsed, Is.EqualTo(Collapsed));
		Assert.That(Form.NavigationPaneCollapsed, Is.EqualTo(Collapsed));
	}

	[TestCase(ScreenDisplayMode.Filled, false)]
	[TestCase(ScreenDisplayMode.Filled, true)]
	[TestCase(ScreenDisplayMode.FilledAndMaximized, false)]
	[TestCase(ScreenDisplayMode.FilledAndMaximized, true)]
	public void FilledScreensTemporarilyHideNavigationWithoutChangingPreference(ScreenDisplayMode DisplayMode, bool Collapsed) {
		using var Form = new BlockMainForm { ScreenMode = ScreenMode.MultiView, NavigationPaneCollapsed = Collapsed };
		var NormalScreen = new NavigationScreen();
		var FilledScreen = new NavigationScreen { DisplayMode = DisplayMode };
		Form.ShowScreen(NormalScreen);
		var SplitContainer = Form.Controls.OfType<SplitContainer>().Single();
		var OriginalWidth = SplitContainer.SplitterDistance;
		Form.ShowScreen(FilledScreen);
		Assert.That(SplitContainer.Panel1Collapsed, Is.True);
		Assert.That(Form.NavigationPaneCollapsed, Is.EqualTo(Collapsed));
		Form.ShowScreen(NormalScreen);
		Assert.That(SplitContainer.Panel1Collapsed, Is.EqualTo(Collapsed));
		Assert.That(Form.NavigationPaneCollapsed, Is.EqualTo(Collapsed));
		Form.NavigationPaneCollapsed = false;
		Assert.That(SplitContainer.SplitterDistance, Is.EqualTo(OriginalWidth));
	}

	[TestCase(320, false)]
	[TestCase(320, true)]
	[TestCase(160, false)]
	[TestCase(160, true)]
	public void ExpandingAfterWindowShrinksKeepsSplitterWithinClientArea(int WindowWidth, bool FilledScreen) {
		using var Form = new BlockMainForm { ScreenMode = ScreenMode.MultiView };
		var SplitContainer = Form.Controls.OfType<SplitContainer>().Single();
		SplitContainer.SplitterDistance = 500;
		if (FilledScreen)
			Form.ShowScreen(new NavigationScreen { DisplayMode = ScreenDisplayMode.Filled });
		else
			Form.NavigationPaneCollapsed = true;
		Form.ClientSize = new Size(WindowWidth, Form.ClientSize.Height);
		Assert.That(() => {
			if (FilledScreen)
				Form.ShowScreen(new NavigationScreen());
			else
				Form.NavigationPaneCollapsed = false;
		}, Throws.Nothing);
		Assert.That(SplitContainer.Panel1Collapsed, Is.False);
		Assert.That(SplitContainer.SplitterDistance,
			Is.InRange(SplitContainer.Panel1MinSize, SplitContainer.Width - SplitContainer.SplitterWidth - SplitContainer.Panel2MinSize));
		Assert.That(SplitContainer.Panel2.Width, Is.GreaterThanOrEqualTo(SplitContainer.Panel2MinSize));
		Assert.That(SplitContainer.Width, Is.EqualTo(Form.ClientSize.Width));
	}

	[TestCase(120, false)]
	[TestCase(144, false)]
	[TestCase(192, false)]
	[TestCase(120, true)]
	[TestCase(144, true)]
	[TestCase(192, true)]
	public void HiddenNavigationWidthFollowsDpiChanges(int NewDpi, bool FilledScreen) {
		using var Form = new ProbeMainForm { ClientSize = new Size(1800, 900), ScreenMode = ScreenMode.MultiView };
		var SplitContainer = Form.Controls.OfType<SplitContainer>().Single();
		SplitContainer.SplitterDistance = 281;
		var OriginalWidth = SplitContainer.SplitterDistance;
		var OriginalDpi = Form.DeviceDpi;
		if (FilledScreen)
			Form.ShowScreen(new NavigationScreen { DisplayMode = ScreenDisplayMode.Filled });
		else
			Form.NavigationPaneCollapsed = true;
		Form.RescaleDpi(OriginalDpi, NewDpi);
		if (FilledScreen)
			Form.ShowScreen(new NavigationScreen());
		else
			Form.NavigationPaneCollapsed = false;
		Assert.That(SplitContainer.Panel1Collapsed, Is.False);
		Assert.That(SplitContainer.SplitterDistance, Is.EqualTo((int)Math.Round(OriginalWidth * (double)NewDpi / OriginalDpi)),
			"Restoring a hidden navigation pane must use the current monitor's DPI");
	}

	[Test]
	public void HiddenNavigationWidthDoesNotDriftAfterRepeatedDpiChanges() {
		using var Form = new ProbeMainForm();
		var SplitContainer = Form.Controls.OfType<SplitContainer>().Single();
		SplitContainer.SplitterDistance = 281;
		Form.NavigationPaneCollapsed = true;
		var PreviousDpi = Form.DeviceDpi;
		foreach (var NewDpi in new[] { 120, 144, 192, 120, Form.DeviceDpi }) {
			Form.RescaleDpi(PreviousDpi, NewDpi);
			PreviousDpi = NewDpi;
		}
		Form.NavigationPaneCollapsed = false;
		Assert.That(SplitContainer.SplitterDistance, Is.EqualTo(281));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void DockPreviewFollowsSelectedNavigationPaneWithoutSwitchingScreens(bool Collapsed) {
		using var Form = new ProbeMainForm { ScreenMode = ScreenMode.MultiView, NavigationPaneCollapsed = Collapsed };
		using var FirstBlock = new ApplicationBlock { Name = "First" };
		using var SecondBlock = new ApplicationBlock { Name = "Second" };
		Form.RegisterBlock(FirstBlock);
		Form.RegisterBlock(SecondBlock);
		var FirstPane = Form.PluginBindings[FirstBlock];
		var SecondPane = Form.PluginBindings[SecondBlock];
		FirstPane.CustomSettings.GradientStartColor = Color.LightSkyBlue;
		SecondPane.CustomSettings.GradientStartColor = Color.MidnightBlue;
		var Screen = new NavigationScreen { ApplicationBlock = FirstBlock };
		Form.ShowScreen(Screen);
		var Toolbar = Screen.ActionButton.Owner;
		var LifecycleChanges = 0;
		Screen.ScreenDisplayed += (_, _) => LifecycleChanges++;
		Screen.ScreenHidden += (_, _) => LifecycleChanges++;
		var Navigation = Form.Controls.OfType<SplitContainer>().Single().Panel1.Controls.OfType<ApplicationBar>().Single();
		Assert.That(Form.ScreenHost.TabControl.DockPreviewBackColor, Is.EqualTo(FirstPane.GradientStartColor));
		Form.ClickNavigationButton(Navigation.Items.Single(Item => ReferenceEquals(Item.MenuControl, SecondPane)).Button);
		Assert.That(Navigation.ApplicationBarControl, Is.SameAs(SecondPane));
		Assert.That(Form.ScreenHost.TabControl.DockPreviewBackColor, Is.EqualTo(SecondPane.GradientStartColor));
		Assert.That(Form.ScreenHost.TabControl.DockPreviewForeColor, Is.EqualTo(Color.White));
		FirstPane.CustomSettings.GradientStartColor = Color.LightBlue;
		Assert.That(Form.ScreenHost.TabControl.DockPreviewBackColor, Is.EqualTo(SecondPane.GradientStartColor), "An inactive pane must not recolor the docking preview");
		Form.ClickNavigationButton(Navigation.Items.Single(Item => ReferenceEquals(Item.MenuControl, FirstPane)).Button);
		Assert.That(Navigation.ApplicationBarControl, Is.SameAs(FirstPane));
		Assert.That(Form.ScreenHost.TabControl.DockPreviewBackColor, Is.EqualTo(FirstPane.GradientStartColor));
		Assert.That(Form.ScreenHost.TabControl.DockPreviewForeColor, Is.EqualTo(Color.Black));
		Assert.That(Form.ActiveScreen, Is.SameAs(Screen));
		Assert.That(Screen.ActionButton.Owner, Is.SameAs(Toolbar));
		Assert.That(LifecycleChanges, Is.Zero);
		Assert.That(Form.NavigationPaneCollapsed, Is.EqualTo(Collapsed));
	}

	[Test]
	public void DockPreviewTracksLivePaneSettingsAndSystemThemeChanges() {
		using var Form = new BlockMainForm();
		using var Block = new ApplicationBlock { Name = "Themed" };
		Form.RegisterBlock(Block);
		var Pane = Form.PluginBindings[Block];
		Pane.CustomSettings.GradientStartColor = Color.MidnightBlue;
		Assert.That(Form.ScreenHost.TabControl.DockPreviewBackColor, Is.EqualTo(Color.MidnightBlue));
		Pane.ResetCustomSettings();
		Assert.That(Form.ScreenHost.TabControl.DockPreviewBackColor, Is.EqualTo(Pane.GradientStartColor));
		var Theme = ExplorerBarInfo.Default;
		Theme.TaskPane.GradientStartColor = Color.CornflowerBlue;
		Pane.UseCustomTheme(Theme);
		Assert.That(Form.ScreenHost.TabControl.DockPreviewBackColor, Is.EqualTo(Color.CornflowerBlue), "System theme changes arrive through the pane's BackColorChanged event");
	}

	[Test]
	public void RemovingLastNavigationPaneRestoresDefaultDockPreviewColors() {
		using var Form = new BlockMainForm();
		var DefaultBackColor = Form.ScreenHost.TabControl.DockPreviewBackColor;
		var DefaultForeColor = Form.ScreenHost.TabControl.DockPreviewForeColor;
		var Block = new ApplicationBlock { Name = "Themed" };
		Form.RegisterBlock(Block);
		Form.PluginBindings[Block].CustomSettings.GradientStartColor = Color.MidnightBlue;
		Form.UnregisterBlock(Block);
		Assert.That(Form.ScreenHost.TabControl.DockPreviewBackColor, Is.EqualTo(DefaultBackColor));
		Assert.That(Form.ScreenHost.TabControl.DockPreviewForeColor, Is.EqualTo(DefaultForeColor));
	}

	private static bool DragNavigationDivider(SplitContainer SplitContainer, params int[] Widths) {
		var InitialWidth = SplitContainer.SplitterDistance;
		var Start = new Point(SplitContainer.SplitterRectangle.Left + SplitContainer.SplitterWidth / 2, 100);
		var Last = Start;
		var Direction = SplitContainer.RightToLeft == RightToLeft.Yes ? -1 : 1;
		void RaiseMouse(string Method, Point Location) => typeof(SplitContainer).GetMethod(Method, BindingFlags.Instance | BindingFlags.NonPublic)!
			.Invoke(SplitContainer, new object[] { new MouseEventArgs(MouseButtons.Left, 1, Location.X, Location.Y, 0) });
		RaiseMouse("OnMouseDown", Start);
		using var ReleaseMouse = Tools.Scope.ExecuteOnDispose(() => RaiseMouse("OnMouseUp", Last));
		foreach (var Width in Widths) {
			Last = new Point(Start.X + Direction * (Width - InitialWidth), Start.Y);
			RaiseMouse("OnMouseMove", Last);
		}
		return SplitContainer.Capture;
	}

	private class ProbeMainForm : BlockMainForm {
		public void RescaleDpi(int OldDpi, int NewDpi) => RescaleConstantsForDpi(OldDpi, NewDpi);

		public void ClickNavigationButton(SquareButton Button) => InvokeOnClick(Button, EventArgs.Empty);

		public bool ToggleSidebarWithKeyboard() {
			var Message = System.Windows.Forms.Message.Create(Handle, 0x0100, (IntPtr)Keys.M, IntPtr.Zero);
			return ProcessCmdKey(ref Message, Keys.Control | Keys.Alt | Keys.M);
		}
	}

	private class NavigationLayoutProbeMainForm : ProbeMainForm {
		protected override void OnLoad(EventArgs Args) {
		}

		protected override void OnFirstActivated() {
		}
	}

	private class NavigationResizeProbeMainForm : NavigationLayoutProbeMainForm {
		public List<Exception> LayoutErrors { get; } = new();

		protected override bool ShowWithoutActivation => true;

		public override void ReportError(Exception Error) => LayoutErrors.Add(Error);
	}

	private class NavigationScreen : ApplicationScreen {
		public NavigationScreen() {
			ActivationMode = ScreenActivationMode.MultiInstance;
			ToolBar = new ToolStrip();
			ActionButton = new ToolStripButton("Screen action");
			ToolBar.Items.Add(ActionButton);
			Controls.Add(ToolBar);
		}

		public ToolStripButton ActionButton { get; }
	}
}
