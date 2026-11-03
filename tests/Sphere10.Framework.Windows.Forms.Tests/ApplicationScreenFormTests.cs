// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows;
using FormsUI = global::Sphere10.Framework.Windows.Forms;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationScreenFormTests {
	private bool _startedFramework;

	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework)
			Sphere10Framework.Instance
				.Build()
				.Start();
	}

	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	[Test]
	public void CompactCaptionKeepsAllWindowActionsAndTaskbarRestoration() {
		using var Host = new HiddenHost();
		var Screen = new MenuScreen();
		Host.ShowScreen(Screen);
		Host.UndockScreen(Screen);
		var Window = (ApplicationScreenForm)Screen.FindForm()!;
		var Caption = Window.Controls.Find("_screenCaption", true).Single();
		var Actions = Caption.Controls.OfType<Button>().ToArray();
		var Style = WinAPI.USER32.GetWindowLong(Window.Handle, -16).ToInt64();
		Assert.That(Window.ShowInTaskbar, Is.True, "Every minimized screen, including a multi-instance screen, must remain reachable");
		Assert.That(Style & 0x000F0000, Is.EqualTo(0x000F0000), "Keep native sizing, system menu, minimize and maximize styles");
		Assert.That(Style & 0x00C00000, Is.Zero, "The custom compact caption replaces the larger native title bar");
		Assert.That(Actions.Select(Action => Action.AccessibleName), Is.EqualTo(new[] { "Re-dock", "Minimize", "Maximize", "Close" }));
		Assert.That(Actions.All(Action => Action.Visible && Action.Text.Length == 0 && Caption.ClientRectangle.Contains(Action.Bounds)), Is.True);
		Assert.That(Actions.All(Action => Action.AccessibilityObject.Role == AccessibleRole.PushButton), Is.True);
		Assert.That(Actions.All(Action => !string.IsNullOrEmpty(Action.AccessibleDescription)), Is.True);
		Assert.That(Window.CaptionBounds, Is.EqualTo(Caption.RectangleToScreen(Caption.ClientRectangle)));
		Assert.That(Caption.Height, Is.LessThanOrEqualTo(Window.LogicalToDeviceUnits(32)));
		Window.Size = Window.MinimumSize;
		Assert.That(Actions.All(Action => Caption.ClientRectangle.Contains(Action.Bounds)), Is.True, "All actions must fit at the minimum window size");
	}

	[Test]
	public void CaptionChildDefersToNativeWindowDraggingAndButtonsRemainClickable() {
		using var Host = new HiddenHost();
		var Screen = new MenuScreen();
		Host.ShowScreen(Screen);
		Host.UndockScreen(Screen);
		var Window = (ApplicationScreenForm)Screen.FindForm()!;
		var Caption = Window.Controls.Find("_screenCaption", true).Single();
		var CaptionPoint = Caption.PointToScreen(new Point(20, Caption.Height / 2));
		Assert.That(HitTest(Caption, CaptionPoint), Is.EqualTo(-1), "The real child HWND must defer to its parent's caption hit test");
		Assert.That(HitTest(Window, CaptionPoint), Is.EqualTo(2), "Native HTCAPTION must own the move loop, double-click and system menu");
		var Minimize = FindAction(Window, "_minimizeButton");
		var ActionPoint = Minimize.PointToScreen(new Point(Minimize.Width / 2, Minimize.Height / 2));
		Assert.That(HitTest(Minimize, ActionPoint), Is.EqualTo(1));
		Assert.That(HitTest(Window, ActionPoint), Is.EqualTo(1), "Caption actions must not start window dragging");
		Assert.That(HitTest(Window, Window.PointToScreen(new Point(Window.ClientSize.Width / 2, Window.ClientSize.Height / 2))), Is.EqualTo(1));
		Assert.That(HitTest(Window, new Point(Window.Left + 1, Window.Top + Window.Height / 2)), Is.EqualTo(10), "The native left resize border must remain available");
	}

	[Test]
	public void CaptionHasNoBlankTopInsetAndRetainsAUsableNativeResizeEdge() {
		using var Host = new HiddenHost();
		var Screen = new MenuScreen();
		Host.ShowScreen(Screen);
		Host.UndockScreen(Screen);
		var Window = (ApplicationScreenForm)Screen.FindForm()!;
		Assert.That(Window.CaptionBounds.Top - Window.Top, Is.EqualTo(Window.LogicalToDeviceUnits(1)), "Only the thin outer border belongs above the caption");
		System.Windows.Forms.Application.DoEvents();
		Assert.That(Window.ClientSize, Is.EqualTo(Window.LogicalToDeviceUnits(new Size(900, 650))), "Initial native layout must preserve the configured client size");
		Window.ClientSize = new Size(640, 380);
		System.Windows.Forms.Application.DoEvents();
		Assert.That(Window.ClientSize, Is.EqualTo(new Size(640, 380)), "Native client-area trimming must also preserve requested client dimensions");
		var TopPoint = new Point(Window.Left + Window.Width / 2, Window.Top + Window.LogicalToDeviceUnits(2));
		Assert.That(HitTest(Window, TopPoint), Is.EqualTo(12), "The resize target extends into the caption instead of reserving a blank row");
		Assert.That(HitTest(Window, new Point(Window.Left + 1, TopPoint.Y)), Is.EqualTo(13));
		Assert.That(HitTest(Window, new Point(Window.Right - 1, TopPoint.Y)), Is.EqualTo(14));
		var Close = FindAction(Window, "_closeButton");
		var AboveClose = new Point(Close.PointToScreen(Point.Empty).X + Close.Width / 2, TopPoint.Y);
		Assert.That(HitTest(Close, AboveClose), Is.EqualTo(-1), "Caption buttons defer to the resize edge along their top few pixels");
		Assert.That(HitTest(Window, AboveClose), Is.EqualTo(12));
	}

	[TestCase(false, false)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	public void MissingScreenChromeDoesNotReserveEmptyRows(bool HasMenu, bool HasToolbar) {
		using var Host = new HiddenHost();
		var Screen = new PartialChromeScreen(HasMenu, HasToolbar);
		Host.ShowScreen(Screen);
		Host.UndockScreen(Screen);
		var Window = (ApplicationScreenForm)Screen.FindForm()!;
		var Caption = Window.Controls.Find("_screenCaption", true).Single();
		var Strips = Window.Controls.OfType<ToolStrip>().Concat(Screen.Controls.OfType<ToolStrip>()).ToArray();
		Assert.That(Strips.Length, Is.EqualTo((HasMenu ? 1 : 0) + (HasToolbar ? 1 : 0)), "Do not create placeholder strips for missing screen commands");
		Assert.That(Strips.All(Strip => Strip.Visible && Strip.Items.Count > 0), Is.True);
		Assert.That(Window.Controls.OfType<TabControl>(), Is.Empty, "A detached single screen has no tab row");
		if (!HasMenu)
			Assert.That(Window.MainMenuStrip, Is.Null);
		var ContentBounds = Window.RectangleToClient(Screen.Content.RectangleToScreen(Screen.Content.ClientRectangle));
		Assert.That(ContentBounds.Top, Is.EqualTo(Caption.Bottom + Strips.Sum(Strip => Strip.Height)), "Content follows only the rows that actually exist");
		Assert.That(ContentBounds.Bottom, Is.EqualTo(Window.ClientRectangle.Bottom));
	}

	[Test]
	public void RecreatingTheWindowHandlePreservesItsLatestResizedBounds() {
		using var Host = new HiddenHost();
		using var Screen = new ApplicationScreen();
		using var Window = new RecreatedScreenForm(Host, Screen) { Opacity = 0 };
		Window.Show();
		Window.Size = new Size(780, 470);
		System.Windows.Forms.Application.DoEvents();
		var ResizedBounds = Window.Bounds;
		var ResizedClientSize = Window.ClientSize;
		Window.RecreateWindowHandle();
		System.Windows.Forms.Application.DoEvents();
		Assert.That(Window.Bounds, Is.EqualTo(ResizedBounds), "A new native handle must retain the most recent resize instead of restoring the initial client size");
		Assert.That(Window.ClientSize, Is.EqualTo(ResizedClientSize));
		Assert.That(Window.CaptionBounds.Top - Window.Top, Is.EqualTo(Window.LogicalToDeviceUnits(1)));
	}

	[Test]
	public void EmptyToolbarCollapsesAndFollowsCommandsAddedOrRemovedWhileDetached() {
		using var Host = new HiddenHost();
		var Screen = new PartialChromeScreen(false, false) { ScreenToolBar = new ToolStrip { Dock = DockStyle.Top } };
		Screen.Controls.Add(Screen.ScreenToolBar);
		Host.ShowScreen(Screen);
		Host.UndockScreen(Screen);
		var Window = (ApplicationScreenForm)Screen.FindForm()!;
		Assert.That(Screen.ScreenToolBar.Visible, Is.False);
		Assert.That(Screen.Content.Top, Is.Zero);
		using var Command = new ToolStripButton("Save");
		Screen.ScreenToolBar.Items.Add(Command);
		Assert.That(Screen.ScreenToolBar.Visible, Is.True);
		Assert.That(Screen.Content.Top, Is.EqualTo(Screen.ScreenToolBar.Bottom));
		Screen.ScreenToolBar.Items.Remove(Command);
		Assert.That(Screen.ScreenToolBar.Visible, Is.False);
		Assert.That(Screen.Content.Top, Is.Zero);
		Assert.That(Window.MainMenuStrip, Is.Null);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void MaximizeAndRestoreUseNativeStateAndTheMonitorWorkingArea(bool DoubleClickCaption) {
		using var Host = new HiddenHost();
		var Screen = new MenuScreen();
		Host.ShowScreen(Screen);
		Host.UndockScreen(Screen);
		var Window = (ApplicationScreenForm)Screen.FindForm()!;
		var OriginalBounds = Window.Bounds;
		var Maximize = FindAction(Window, "_maximizeButton");
		for (var Index = 0; Index < 2; Index++) {
			if (DoubleClickCaption)
				WinAPI.USER32.SendMessage(Window.Handle, 0x00A3, (IntPtr)2, PackPoint(new Point(Window.CaptionBounds.Left + 20, Window.CaptionBounds.Top + 10)));
			else
				Maximize.PerformClick();
			System.Windows.Forms.Application.DoEvents();
			Assert.That(Window.WindowState, Is.EqualTo(Index == 0 ? FormWindowState.Maximized : FormWindowState.Normal));
			Assert.That(Maximize.AccessibleName, Is.EqualTo(Index == 0 ? "Restore" : "Maximize"));
			Assert.That(Maximize.AccessibleDescription, Is.EqualTo(Maximize.AccessibleName));
			Assert.That(Window.Bounds, Is.EqualTo(Index == 0 ? System.Windows.Forms.Screen.FromHandle(Window.Handle).WorkingArea : OriginalBounds));
		}
	}

	[Test]
	public void MinimizeCanBeRestoredThroughTheScreenHost() {
		using var Host = new HiddenHost();
		var Screen = new MenuScreen();
		Host.ShowScreen(Screen);
		Host.UndockScreen(Screen);
		var Window = (ApplicationScreenForm)Screen.FindForm()!;
		var OriginalBounds = Window.Bounds;
		FindAction(Window, "_minimizeButton").PerformClick();
		Assert.That(Window.WindowState, Is.EqualTo(FormWindowState.Minimized));
		Assert.That(Host.ShowScreen(Screen), Is.True);
		System.Windows.Forms.Application.DoEvents();
		Assert.That(Window.WindowState, Is.EqualTo(FormWindowState.Normal));
		Assert.That(Window.Bounds, Is.EqualTo(OriginalBounds));
		Assert.That(Host.IsScreenUndocked(Screen), Is.True);
	}

	[TestCase("_redockButton")]
	[TestCase("_closeButton")]
	public void CaptionCommandsHonorScreenVetoAndThenComplete(string ActionName) {
		using var Host = new HiddenHost();
		var Screen = new MenuScreen();
		Host.ShowScreen(Screen);
		Host.UndockScreen(Screen);
		var Window = (ApplicationScreenForm)Screen.FindForm()!;
		var Action = FindAction(Window, ActionName);
		Screen.CancelHide = true;
		Action.PerformClick();
		Assert.That(Window.IsDisposed, Is.False);
		Assert.That(Host.IsScreenUndocked(Screen), Is.True);
		Screen.CancelHide = false;
		Action.PerformClick();
		Assert.That(Window.IsDisposed, Is.True);
		Assert.That(Screen.IsDisposed, Is.EqualTo(ActionName == "_closeButton"));
		if (ActionName == "_redockButton")
			Assert.That(Host.ActiveScreen, Is.SameAs(Screen));
		else
			Assert.That(Host.Screens, Is.Empty);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ReadingLegacyCommandProvidersWhileDetachedPreservesOriginalNativeViews(bool showInApplicationMenuStrip) {
		using var host = new HiddenHost();
		var screen = new MenuScreen { ShowInApplicationMenuStrip = showInApplicationMenuStrip };
		var originalMenuItems = screen.MenuItems;
		var originalToolBar = screen.ScreenToolBar;
		host.ShowScreen(screen);
		Assert.That(host.UndockScreen(screen), Is.True);
		var window = (ApplicationScreenForm)screen.FindForm()!;
		var menuStrip = window.MainMenuStrip;

		// Cached definitions must remain readable without taking their actual views away from the detached window.
		IApplicationCommandProvider commands = screen;
		var menus = commands.Menus;
		var toolBarItems = commands.ToolBarItems;
		Assert.That(commands.Menus, Is.EqualTo(menus));
		Assert.That(commands.ToolBarItems, Is.EqualTo(toolBarItems));
		Assert.That(window.MainMenuStrip, Is.SameAs(menuStrip));
		Assert.That(menuStrip!.Items.Cast<ToolStripItem>(), Is.EqualTo(originalMenuItems));
		Assert.That(originalMenuItems.All(item => ReferenceEquals(item.Owner, menuStrip)), Is.True);
		Assert.That(menuStrip.Visible, Is.True);
		Assert.That(screen.ScreenToolBar, Is.SameAs(originalToolBar));
		Assert.That(screen.ToolButton.Owner, Is.SameAs(originalToolBar));
		screen.FileCommand.PerformClick();
		screen.ToolButton.PerformClick();
		Assert.That(screen.CommandCount, Is.EqualTo(2));

		Assert.That(host.DockScreen(screen), Is.True);
		Assert.That(window.IsDisposed, Is.True);
		Assert.That(screen.MenuItems, Is.EqualTo(originalMenuItems));
		Assert.That(originalMenuItems.All(item => item.Owner == null && !item.IsDisposed), Is.True);
		Assert.That(screen.ToolButton.Owner, Is.SameAs(originalToolBar));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void RepeatedDetachmentKeepsOriginalToolbarAndTopLevelFileMenu(bool MergeIntoMainMenu) {
		using var Host = new HiddenHost();
		var Screen = new MenuScreen { ShowInApplicationMenuStrip = MergeIntoMainMenu };
		var Toolbar = Screen.ScreenToolBar;
		var Renderer = Toolbar.Renderer;
		var Parent = Toolbar.Parent;
		var Index = Parent!.Controls.GetChildIndex(Toolbar);
		var FileDisposeCount = 0;
		var ActionsDisposeCount = 0;
		var FileCommandDisposeCount = 0;
		Screen.FileMenu.Disposed += (_, _) => FileDisposeCount++;
		Screen.ActionsMenu.Disposed += (_, _) => ActionsDisposeCount++;
		Screen.FileCommand.Disposed += (_, _) => FileCommandDisposeCount++;
		Host.ShowScreen(Screen);
		for (var Iteration = 0; Iteration < 3; Iteration++) {
			Host.UndockScreen(Screen);
			var Window = (ApplicationScreenForm)Screen.FindForm()!;
			Assert.That(Toolbar.FindForm(), Is.SameAs(Window));
			Assert.That(Toolbar.Parent, Is.SameAs(Parent), "A designer toolbar keeps its original screen layout");
			Assert.That(Toolbar.Visible, Is.True);
			Assert.That(Toolbar.Renderer, Is.SameAs(Renderer));
			Assert.That(Toolbar.ImageScalingSize, Is.EqualTo(new Size(24, 24)));
			Assert.That(Toolbar.GripStyle, Is.EqualTo(ToolStripGripStyle.Visible));
			Assert.That(Screen.ToolButton.Owner, Is.SameAs(Toolbar));
			Assert.That(Window.MainMenuStrip!.Items.Cast<ToolStripItem>(), Is.EqualTo(new[] { Screen.FileMenu, Screen.ActionsMenu }));
			Assert.That(Screen.FileCommand.OwnerItem, Is.SameAs(Screen.FileMenu));
			Screen.FileCommand.PerformClick();
			Screen.ToolButton.PerformClick();
			Host.DockScreen(Screen);
			Assert.That(Window.IsDisposed, Is.True);
			Assert.That(Screen.FileMenu.Owner, Is.Null);
			Assert.That(Screen.FileMenu.DropDownItems[0], Is.SameAs(Screen.FileCommand));
			Assert.That(Parent.Controls.GetChildIndex(Toolbar), Is.EqualTo(Index));
			Assert.That(Toolbar.IsDisposed, Is.False);
			Assert.That(new[] { FileDisposeCount, ActionsDisposeCount, FileCommandDisposeCount }, Is.All.EqualTo(0));
		}
		Assert.That(Screen.CommandCount, Is.EqualTo(6));
		Host.CloseScreen(Screen);
		Assert.That(Toolbar.IsDisposed, Is.True);
		Assert.That(new[] { FileDisposeCount, ActionsDisposeCount, FileCommandDisposeCount }, Is.All.EqualTo(1), "Screen-owned menus and children are disposed exactly once");
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ExternalToolbarIsReparentedWholeAndRestoredBeforeWindowDisposal(bool HasOriginalParent) {
		using var Host = new HiddenHost();
		using var Screen = new ApplicationScreen();
		using var OriginalParent = new Panel { Size = new Size(500, 400) };
		Screen.ScreenToolBar = new ToolStrip { Dock = DockStyle.None, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, AutoSize = false, Bounds = new Rectangle(20, 40, 220, 35) };
		Screen.ScreenToolBar.Items.Add(new ToolStripButton("Save"));
		if (HasOriginalParent) {
			OriginalParent.Controls.Add(new Label());
			OriginalParent.Controls.Add(Screen.ScreenToolBar);
			OriginalParent.Controls.Add(new TextBox());
		}
		var Toolbar = Screen.ScreenToolBar;
		var OriginalBounds = Toolbar.Bounds;
		var OriginalIndex = HasOriginalParent ? OriginalParent.Controls.GetChildIndex(Toolbar) : -1;
		using (var Window = new ApplicationScreenForm(Host, Screen) { Opacity = 0 }) {
			Window.Show();
			Assert.That(Toolbar.Parent, Is.SameAs(Window));
			Assert.That(Toolbar.Visible, Is.True);
			Assert.That(Toolbar.Dock, Is.EqualTo(DockStyle.Top));
		}
		Assert.That(Screen.IsDisposed, Is.False);
		Assert.That(Toolbar.IsDisposed, Is.False);
		Assert.That(Toolbar.Parent, Is.SameAs(HasOriginalParent ? OriginalParent : null));
		Assert.That(Toolbar.Dock, Is.EqualTo(DockStyle.None));
		Assert.That(Toolbar.Anchor, Is.EqualTo(AnchorStyles.Bottom | AnchorStyles.Right));
		Assert.That(Toolbar.Bounds, Is.EqualTo(OriginalBounds));
		Assert.That(Toolbar.Visible, Is.True);
		if (HasOriginalParent)
			Assert.That(OriginalParent.Controls.GetChildIndex(Toolbar), Is.EqualTo(OriginalIndex));
	}

	[Test]
	public void ExistingScreenMenuStripRemainsTheNativeKeyboardMenu() {
		using var Host = new HiddenHost();
		using var Screen = new ApplicationScreen();
		var Panel = new Panel { Dock = DockStyle.Fill };
		var Menu = new MenuStrip();
		var File = new ToolStripMenuItem("&File");
		Menu.Items.Add(File);
		Panel.Controls.Add(Menu);
		Screen.Controls.Add(Panel);
		using (var Window = new ApplicationScreenForm(Host, Screen) { Opacity = 0 }) {
			Window.Show();
			Assert.That(Window.MainMenuStrip, Is.SameAs(Menu));
			Assert.That(Menu.Parent, Is.SameAs(Panel));
			Assert.That(File.Owner, Is.SameAs(Menu));
		}
		Assert.That(Menu.IsDisposed, Is.False);
		Assert.That(File.IsDisposed, Is.False);
	}

	[Test]
	public void EmptyEmbeddedMenuCollapsesAndRestoresItsOriginalScreenVisibility() {
		using var Host = new HiddenHost();
		using var Screen = new PartialChromeScreen(false, false);
		var Menu = new MenuStrip { Dock = DockStyle.Top };
		Screen.Controls.Add(Menu);
		Assert.That(Menu.Visible, Is.True);
		using (var Window = new ApplicationScreenForm(Host, Screen) { Opacity = 0 }) {
			Window.Show();
			Assert.That(Window.MainMenuStrip, Is.SameAs(Menu));
			Assert.That(Menu.Visible, Is.False);
			Assert.That(Screen.Content.Top, Is.Zero);
			using var File = new ToolStripMenuItem("File");
			Menu.Items.Add(File);
			Assert.That(Menu.Visible, Is.True);
			Assert.That(Screen.Content.Top, Is.EqualTo(Menu.Bottom));
			Menu.Items.Remove(File);
			Assert.That(Menu.Visible, Is.False);
			Assert.That(Screen.Content.Top, Is.Zero);
		}
		Assert.That(Menu.IsDisposed, Is.False);
		Assert.That(Menu.Parent, Is.SameAs(Screen));
		Assert.That(Menu.Visible, Is.True);
	}

	[Test]
	public void EmbeddedMenuIsFoundWhenThePreviousMainWindowWasNotShown() {
		using var Main = new MainForm { ScreenMode = ScreenMode.MultiView, Opacity = 0 };
		var Screen = new ApplicationScreen();
		var Menu = new MenuStrip();
		Menu.Items.Add(new ToolStripMenuItem("&File"));
		Screen.Controls.Add(Menu);
		Screen.ScreenLoaded += (_, _) => {
			if (Screen.FindForm() is ApplicationScreenForm Detached)
				Detached.Opacity = 0;
		};
		Main.ShowScreen(Screen);
		Assert.That(Menu.Visible, Is.False, "The menu inherits the unshown main window's visibility");
		Main.ScreenHost.UndockScreen(Screen);
		var Window = (ApplicationScreenForm)Screen.FindForm()!;
		Assert.That(Window.MainMenuStrip, Is.SameAs(Menu));
		Assert.That(Menu.Visible, Is.True);
		Assert.That(Main.ScreenHost.DockScreen(Screen), Is.True);
		using var VisibleParent = new Form { Opacity = 0 };
		VisibleParent.Controls.Add(Main.ScreenHost);
		VisibleParent.Show();
		System.Windows.Forms.Application.DoEvents();
		Assert.That(Menu.Visible, Is.True, "Re-docking must retain the menu's own visibility after its original parent becomes visible");
	}

	[Test]
	public void RenamingUpdatesTheAccessibleCaption() {
		using var Host = new HiddenHost();
		var Screen = new MenuScreen();
		Host.ShowScreen(Screen);
		Host.UndockScreen(Screen);
		var Window = (ApplicationScreenForm)Screen.FindForm()!;
		Screen.Title = "Renamed design";
		Assert.That(Window.Text, Is.EqualTo(Screen.Title));
		Assert.That(Window.Controls.Find("_screenCaption", true).Single().AccessibleName, Is.EqualTo(Screen.Title));
	}

	[Test]
	public void DetachedHostKeepsItsOwnActiveContextAndBorrowsTheRootLifetime() {
		using var host = new HiddenHost();
		using var otherHost = new HiddenHost();
		using var firstBlock = new ApplicationBlock { Name = "First" };
		using var secondBlock = new ApplicationBlock { Name = "Second" };
		host.RegisterScreenTypes(firstBlock);
		host.RegisterScreenTypes(secondBlock);
		var detachedScreen = new MenuScreen { Block = firstBlock };
		var mainScreen = new MenuScreen { Block = secondBlock };
		host.ShowScreen(detachedScreen);
		host.ShowScreen(mainScreen);
		Assert.That(host.UndockScreen(detachedScreen), Is.True);

		var window = (ApplicationScreenForm)detachedScreen.FindForm();
		var localHost = window.ScreenHost;
		Assert.That(detachedScreen.Host, Is.SameAs(localHost));
		Assert.That(localHost.ActiveScreen, Is.SameAs(detachedScreen));
		Assert.That(localHost.ActiveBlock, Is.SameAs(firstBlock));
		Assert.That(host.ActiveScreen, Is.SameAs(mainScreen));
		Assert.That(localHost.Blocks, Is.EqualTo(host.Blocks));
		Assert.That(localHost.Screens, Is.EqualTo(host.Screens));
		Assert.That(() => otherHost.ShowScreen(detachedScreen), Throws.ArgumentException, "A local projection does not transfer canonical ownership.");
		Assert.That(host.ShowScreen(detachedScreen), Is.True, "The canonical owner can still focus its detached window.");
		Assert.That(host.ActiveScreen, Is.SameAs(mainScreen));

		localHost.Dispose();
		Assert.That(host.IsDisposed, Is.False);
		Assert.That(host.DockScreen(detachedScreen), Is.True);
		Assert.That(detachedScreen.Host, Is.SameAs(host));
		Assert.That(host.ActiveScreen, Is.SameAs(detachedScreen));
	}

	[Test]
	public async Task DetachedCloseTargetsItsOwnScreenAndHonorsItsGuard() {
		using var host = new HiddenHost();
		var detachedScreen = new MenuScreen();
		var mainScreen = new MenuScreen();
		host.ShowScreen(detachedScreen);
		host.ShowScreen(mainScreen);
		host.UndockScreen(detachedScreen);
		var window = (ApplicationScreenForm)detachedScreen.FindForm();
		var localHost = window.ScreenHost;

		detachedScreen.CancelHide = true;
		Assert.That(await localHost.CanCloseActiveScreenAsync(), Is.False);
		Assert.That(await localHost.CloseActiveScreenAsync(), Is.False);
		Assert.That(window.IsDisposed, Is.False);
		Assert.That(host.ActiveScreen, Is.SameAs(mainScreen));

		detachedScreen.CancelHide = false;
		Assert.That(await localHost.CanCloseActiveScreenAsync(), Is.True);
		Assert.That(await localHost.CloseActiveScreenAsync(), Is.True);
		Assert.That(detachedScreen.IsDisposed, Is.True);
		Assert.That(mainScreen.IsDisposed, Is.False);
		Assert.That(host.ActiveScreen, Is.SameAs(mainScreen));
	}

	[Test]
	public async Task DetachedInteractionServicesHaveLocalIdentityAndForwardApplicationOperations() {
		using var main = new MainForm { ScreenMode = ScreenMode.MultiView, Opacity = 0 };
		var screen = new MenuScreen();
		main.ShowScreen(screen);
		main.ScreenHost.UndockScreen(screen);
		var window = (ApplicationScreenForm)screen.FindForm();
		var services = window.ScreenHost.UserInterfaceServices;

		Assert.That(services.ScreenHost, Is.SameAs(window.ScreenHost));
		Assert.That(services.PrimaryUIController, Is.SameAs(main), "Application UI operations retain their existing root controller.");
		SharedUI.IApplicationScreen commandScreen = null;
		using var command = new WinFormsActionMenuItem("Inspect context", userInterface => commandScreen = userInterface.ScreenHost.ActiveScreen);
		Assert.That(await services.ExecuteMenuItemAsync(command), Is.True);
		Assert.That(commandScreen, Is.SameAs(screen), "Action callbacks receive this window's active-screen host.");
		services.Status = "Detached operation";
		Assert.That(main.Status, Is.EqualTo("Detached operation"));
		Assert.That(main.ScreenHost.DockScreen(screen), Is.True);
		Assert.That(screen.Host, Is.SameAs(main.ScreenHost));
	}

	private static Button FindAction(ApplicationScreenForm Window, string Name) => (Button)Window.Controls.Find(Name, true).Single();

	private static long HitTest(Control Window, Point Position) => WinAPI.USER32.SendMessage(Window.Handle, 0x0084, IntPtr.Zero, PackPoint(Position)).ToInt64();

	private static IntPtr PackPoint(Point Position) => new((Position.Y << 16) | (Position.X & 0xFFFF));

	private sealed class HiddenHost : FormsUI.ApplicationScreenHost {
		public HiddenHost() => ScreenMode = ScreenMode.MultiView;

		protected override ApplicationScreenForm CreateScreenForm(ApplicationScreen Screen)
			=> new(this, Screen) { Opacity = 0, StartPosition = FormStartPosition.Manual, Location = new Point(80, 80) };
	}

	private sealed class RecreatedScreenForm : ApplicationScreenForm {
		public RecreatedScreenForm(FormsUI.IApplicationScreenHost Host, ApplicationScreen Screen)
			: base(Host, Screen) {
		}

		public void RecreateWindowHandle() => RecreateHandle();
	}

	private sealed class PartialChromeScreen : ApplicationScreen {
		public PartialChromeScreen(bool HasMenu, bool HasToolbar) {
			Title = "Screen chrome";
			ActivationMode = ScreenActivationMode.MultiInstance;
			Content = new Panel { Dock = DockStyle.Fill };
			Controls.Add(Content);
			if (HasMenu)
				RegisterMenuItem(new ToolStripMenuItem("&File"));
			if (HasToolbar) {
				ScreenToolBar = new ToolStrip { Dock = DockStyle.Top };
				ScreenToolBar.Items.Add(new ToolStripButton("Save"));
				Controls.Add(ScreenToolBar);
			}
		}

		public Panel Content { get; }
	}

	private sealed class MenuScreen : ApplicationScreen {
		public MenuScreen() {
			Title = "Design document";
			ActivationMode = ScreenActivationMode.MultiInstance;
			ScreenToolBar = new ToolStrip { Dock = DockStyle.Top, Renderer = new ToolStripSystemRenderer(), ImageScalingSize = new Size(24, 24) };
			ToolButton = new ToolStripButton("Save design", null, (_, _) => CommandCount++);
			ScreenToolBar.Items.Add(ToolButton);
			Controls.Add(ScreenToolBar);
			FileCommand = new ToolStripMenuItem("Save", null, (_, _) => CommandCount++);
			FileMenu = new ToolStripMenuItem("&File");
			FileMenu.DropDownItems.Add(FileCommand);
			ActionsMenu = new ToolStripMenuItem("&Actions");
			RegisterMenuItem(FileMenu);
			RegisterMenuItem(ActionsMenu);
		}

		public ToolStripButton ToolButton { get; }
		public ToolStripMenuItem FileMenu { get; }
		public ToolStripMenuItem ActionsMenu { get; }
		public ToolStripMenuItem FileCommand { get; }
		public int CommandCount { get; private set; }
		[DefaultValue(false)]
		public bool CancelHide { get; set; }

		protected override void OnHide(ref bool Cancel) => Cancel = CancelHide;
	}
}
