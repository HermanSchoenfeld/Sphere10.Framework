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
using FormsUI = global::Sphere10.Framework.Windows.Forms;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises screen command projections against the actual native views, callbacks and hosting owners.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class NativeScreenCommandProviderTests {
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
	public void NativeStripsProjectLiveCommandsWithoutExtraDefinitionsOrTagChanges() {
		RunWithMessageLoop(async owner => {
			using var image = new Bitmap(2, 2);
			using var screen = new NativeCommandScreen("Screen tools", image) { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			FormsUI.IApplicationScreen native = screen;
			SharedUI.IApplicationScreen portable = screen;
			var menu = native.Menus.Single();
			var menuAction = menu.Items.Single(item => item.Title == "Screen action");
			var toolAction = native.ToolBarItems.Single(item => item.Title == "Screen refresh");

			Assert.That(portable.Menus.Single(), Is.SameAs(menu));
			Assert.That(portable.ToolBarItems.Single(item => item.Title == "Screen refresh"), Is.SameAs(toolAction));
			Assert.That(screen.MenuItems, Is.EqualTo(new[] { screen.Header }));
			Assert.That(screen.ScreenMenuStrip.Items.Cast<ToolStripItem>(), Is.EqualTo(new[] { screen.Header }));
			Assert.That(screen.ScreenToolBar.Items.Cast<ToolStripItem>(), Is.EqualTo(new[] { screen.RefreshButton, screen.CopyButton }));
			Assert.That(screen.ScreenMenuStrip.Visible && screen.ScreenToolBar.Visible, Is.True);

			// Cached command projections read the current native state and execute the original Click handlers.
			screen.Header.Text = "Changed tools";
			screen.Action.Text = "Changed action";
			screen.RefreshButton.Text = "Changed refresh";
			Assert.That(menu.Text, Is.EqualTo("Changed tools"));
			Assert.That(menuAction.Title, Is.EqualTo("Changed action"));
			Assert.That(toolAction.Title, Is.EqualTo("Changed refresh"));
			((IWinFormsLinkMenuItem)menuAction).OnSelect();
			((IWinFormsLinkMenuItem)toolAction).OnSelect();
			Assert.That(screen.ActionClicks, Is.EqualTo(1));
			Assert.That(screen.RefreshClicks, Is.EqualTo(1));
			Assert.That(screen.Header.Tag, Is.SameAs(screen.HeaderTag));
			Assert.That(screen.Action.Tag, Is.SameAs(screen.ActionTag));
			Assert.That(screen.RefreshButton.Tag, Is.SameAs(screen.RefreshTag));
			Assert.That(image.Width, Is.EqualTo(2));
		});
	}

	[Test]
	public void NavigationAndDockingBorrowTheSameViewsWhileProviderCommandsRemainAvailable() {
		RunWithMessageLoop(async owner => {
			using var form = new CommandMainForm { ScreenMode = ScreenMode.MultiView };
			using var screen = new NativeCommandScreen("Screen tools") { Title = "Native commands" };
			using var other = new ApplicationScreen { Title = "Other screen" };
			await using var application = new FormsUI.Application(form, Array.Empty<FormsUI.IApplicationBlock>());
			await application.InitializeAsync();
			form.Show(owner);
			form.ShowScreen(screen);
			await Task.Yield();
			FormsUI.IApplicationScreen native = screen;
			var menu = native.Menus.Single();
			var action = menu.Items.Single(item => item.Title == "Screen action");
			var refresh = native.ToolBarItems.Single(item => item.Title == "Screen refresh");

			Assert.That(form.MenuEntries, Does.Contain(screen.Header));
			Assert.That(screen.ScreenMenuStrip.Items, Is.Empty, "The main frame borrows the original native header.");
			Assert.That(screen.ScreenToolBar.Items, Is.Empty);
			Assert.That(screen.Visible, Is.True);
			Assert.That(screen.ScreenMenuStrip.Visible || screen.ScreenToolBar.Visible, Is.False,
				"Borrowed views must not leave empty local menu or toolbar rows in the active screen.");
			Assert.That(screen.MenuItems, Is.EqualTo(new[] { screen.Header }));
			Assert.That(menu.Items.Select(item => item.Title), Is.EqualTo(new[] { "Screen action", "Screen details" }));
			Assert.That(native.ToolBarItems.Select(item => item.Title), Is.EqualTo(new[] { "Screen refresh", "Screen copy" }));
			Assert.That(form.ToolEntries.Where(item => ReferenceEquals(item, screen.RefreshButton) || ReferenceEquals(item, screen.CopyButton)),
				Is.EqualTo(new[] { screen.RefreshButton, screen.CopyButton }));

			form.ShowScreen(other);
			await Task.Yield();
			Assert.That(form.MenuEntries, Does.Not.Contain(screen.Header));
			Assert.That(form.ToolEntries, Does.Not.Contain(screen.RefreshButton).And.Not.Contain(screen.CopyButton));
			Assert.That(screen.ScreenMenuStrip.Items.Cast<ToolStripItem>(), Is.EqualTo(new[] { screen.Header }));
			Assert.That(screen.ScreenToolBar.Items.Cast<ToolStripItem>(), Is.EqualTo(new[] { screen.RefreshButton, screen.CopyButton }));
			form.ShowScreen(screen);
			Assert.That(form.MenuEntries, Does.Contain(screen.Header));

			// The detached screen restores its own strips, then lends those same objects again on re-dock.
			Assert.That(form.ScreenHost.UndockScreen(screen), Is.True);
			screen.FindForm().Location = owner.Location;
			await Task.Yield();
			Assert.That(screen.FindForm(), Is.Not.SameAs(form));
			Assert.That(screen.ScreenMenuStrip.Items.Cast<ToolStripItem>(), Does.Contain(screen.Header));
			Assert.That(screen.ScreenToolBar.Items.Cast<ToolStripItem>().Where(item => ReferenceEquals(item, screen.RefreshButton) || ReferenceEquals(item, screen.CopyButton)),
				Is.EqualTo(new[] { screen.RefreshButton, screen.CopyButton }));
			Assert.That(screen.Visible, Is.True);
			Assert.That(screen.ScreenMenuStrip.Visible && screen.ScreenToolBar.Visible, Is.True,
				"The detached visible screen must recover its requested native strip visibility.");
			Assert.That(native.Menus.Single(), Is.SameAs(menu));
			Assert.That(form.ScreenHost.DockScreen(screen), Is.True);
			await Task.Yield();
			Assert.That(form.MenuEntries, Does.Contain(screen.Header));
			Assert.That(form.ToolEntries, Does.Contain(screen.RefreshButton).And.Contain(screen.CopyButton));
			Assert.That(screen.ScreenMenuStrip.Visible || screen.ScreenToolBar.Visible, Is.False);
			((IWinFormsLinkMenuItem)action).OnSelect();
			((IWinFormsLinkMenuItem)refresh).OnSelect();
			Assert.That(screen.ActionClicks, Is.EqualTo(1));
			Assert.That(screen.RefreshClicks, Is.EqualTo(1));
			Assert.That(screen.Action.Tag, Is.SameAs(screen.ActionTag));
			Assert.That(screen.RefreshButton.Tag, Is.SameAs(screen.RefreshTag));
			Assert.That(screen.OwnedDisposals, Is.Zero);
		});
	}

	/// <summary>A detached window shows its screen's own strips and its plugin's and block's commands, never the application's.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public void DetachedWindowsShowOnlyTheirScreenPluginAndBlockCommands(bool hideLocalStrips) {
		RunWithMessageLoop(async owner => {
			using var image = new Bitmap(2, 2);
			using var form = new DesignerCommandMainForm { ScreenMode = ScreenMode.MultiView };
			form.DesignerAction.Image = image;
			form.DesignerRefresh.Image = image;
			using var detachedBlock = new CommandBlock { Name = "Detached owner", ShowInMenuStrip = true, ShowInToolStrip = true };
			using var rootBlock = new CommandBlock { Name = "Root owner" };
			var blockMenu = new ApplicationMenu("Block tools") { ShowInMenuStrip = true };
			blockMenu.AddItem(new WinFormsActionMenuItem("Block action", () => {
			}));
			detachedBlock.AddMenu(blockMenu);
			using var pluginMenu = new ApplicationMenu("Plugin tools") { ShowInMenuStrip = true };
			pluginMenu.AddItem(new WinFormsActionMenuItem("Plugin action", () => {
			}));
			using var pluginTool = new WinFormsActionMenuItem("Plugin refresh", () => {
			});
			var plugin = new CommandPlugin(detachedBlock, pluginMenu, pluginTool);
			using var detached = new NativeCommandScreen("Detached tools") { Block = detachedBlock };
			using var docked = new NativeCommandScreen("Docked tools") { Block = rootBlock };
			detached.ScreenMenuStrip.Visible = !hideLocalStrips;
			detached.ScreenToolBar.Visible = !hideLocalStrips;
			// The main form's own entries before any screen merges into it are the application's commands.
			var applicationNames = form.MenuEntries.Concat(form.ToolEntries).Select(item => item.Name).Where(name => !string.IsNullOrEmpty(name))
				.Append(form.DesignerHeader.Name).Append(form.DesignerRefresh.Name).Append(form.DesignerSplit.Name).Append("_contextHelpToolStripButton").ToHashSet();
			await using var application = new FormsUI.Application(form, new[] { rootBlock }, new[] { plugin });
			await application.InitializeAsync();
			form.Show(owner);
			form.ShowScreen(detached);
			Assert.That(form.ScreenHost.UndockScreen(detached), Is.True);
			var window = (ApplicationScreenForm)detached.FindForm();
			window.Location = owner.Location;
			form.ShowScreen(docked);
			await Task.Yield();

			var items = WindowItems(window).ToArray();
			Assert.That(items.Select(item => item.Name).Where(applicationNames.Contains), Is.Empty, "Application menus and toolbar items stay with the main window.");
			var captions = items.OfType<ToolStripMenuItem>().Select(item => Caption(item.Text)).ToArray();
			Assert.That(captions, Does.Contain("Plugin tools").And.Contain("Block tools"));
			Assert.That(Descendants(items).Select(item => item.Text), Does.Contain("Plugin action").And.Contain("Block action"));
			Assert.That(items.Select(item => item.ToolTipText ?? item.Text), Does.Contain("Plugin refresh"));
			if (!hideLocalStrips) {
				Assert.That(captions, Does.Contain("Detached tools"));
				Assert.That(items, Does.Contain(detached.RefreshButton));
			}

			// The application's own commands remain on the main window, unchanged by the detached presentation.
			Assert.That(form.MenuEntries, Does.Contain(form.DesignerHeader));
			Assert.That(form.ToolEntries, Does.Contain(form.DesignerRefresh));
			Assert.That(form.ScreenHost.DockScreen(detached), Is.True);
			Assert.That(form.DesignerHeader.IsDisposed || form.DesignerAction.IsDisposed || form.DesignerRefresh.IsDisposed, Is.False);
			Assert.That(image.Width, Is.EqualTo(2));
		});
	}

	// Sidebar selection never contributes commands without the visible screen that owns them.
	[Test]
	public void OnlyTheActiveScreensPluginAndBlockContributeContextCommands() {
		RunWithMessageLoop(async owner => {
			using var form = new CommandMainForm { ScreenMode = ScreenMode.MultiView };
			using var activeBlock = new CommandBlock { Name = "Active", ShowInMenuStrip = true, ShowInToolStrip = true };
			using var browsedBlock = new CommandBlock { Name = "Browsed", ShowInMenuStrip = true, ShowInToolStrip = true };
			using var pluginMenu = new ApplicationMenu("File") { ShowInMenuStrip = true };
			using var pluginTool = new WinFormsActionMenuItem("Plugin refresh", () => {
			});
			var blockMenu = new ApplicationMenu("File") { ShowInMenuStrip = true };
			blockMenu.AddItem(new WinFormsActionMenuItem("Active block action", () => {
			}));
			activeBlock.AddMenu(blockMenu);
			var browsedMenu = new ApplicationMenu("Browsed commands") { ShowInMenuStrip = true };
			browsedMenu.AddItem(new WinFormsActionMenuItem("Browsed action", () => {
			}));
			browsedBlock.AddMenu(browsedMenu);
			pluginMenu.AddItem(new WinFormsActionMenuItem("Plugin action", () => {
			}));
			var plugin = new CommandPlugin(activeBlock, pluginMenu, pluginTool);
			using var screen = new NativeCommandScreen("File") { Block = activeBlock };
			var applicationMenus = form.MenuEntries;
			var file = applicationMenus.OfType<ToolStripMenuItem>().Single(item => Caption(item.Text) == "File");
			var applicationFileItems = file.DropDownItems.Cast<ToolStripItem>().ToArray();
			var applicationTools = form.ToolEntries;
			await using var application = new FormsUI.Application(form, new[] { browsedBlock }, new[] { plugin });
			await application.InitializeAsync();
			form.Show(owner);
			form.SelectedBlock = browsedBlock;

			Assert.That(form.ActiveScreen, Is.Null);
			Assert.That(form.MenuEntries, Is.EqualTo(applicationMenus));
			Assert.That(form.ToolEntries, Is.EqualTo(applicationTools));

			form.ShowScreen(screen);
			form.SelectedBlock = browsedBlock;
			await Task.Yield();
			Assert.That(file.DropDownItems.Cast<ToolStripItem>().Select(item => item.Text),
				Does.Contain("Plugin action").And.Contain("Active block action").And.Contain("Screen action"));
			Assert.That(form.MenuEntries.Select(item => item.Text), Does.Not.Contain("Browsed commands"));
			Assert.That(form.ToolEntries.Select(item => item.ToolTipText), Does.Contain("Plugin refresh").And.Not.Contain("Browsed action"));

			Assert.That(await form.ScreenHost.CloseScreenAsync(screen), Is.True);
			await Task.Yield();
			Assert.That(form.ActiveScreen, Is.Null);
			Assert.That(form.SelectedBlock, Is.SameAs(browsedBlock));
			Assert.That(form.MenuEntries, Is.EqualTo(applicationMenus));
			Assert.That(file.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(applicationFileItems));
			Assert.That(form.ToolEntries, Is.EqualTo(applicationTools));
		});
	}

	[Test]
	public void ActiveOwnersComposeTheirFrozenCommandsThroughTheOriginalDefinitionsAndNativeViews() {
		RunWithMessageLoop(async owner => {
			using var form = new CommandMainForm { ScreenMode = ScreenMode.MultiView };
			using var block = new CommandBlock { Name = "Command block", ShowInMenuStrip = true, ShowInToolStrip = true };
			using var blockMenu = new ApplicationMenu("Shared tools") { ShowInMenuStrip = true };
			using var pluginMenu = new ApplicationMenu("Shared tools") { ShowInMenuStrip = true };
			var blockClicks = 0;
			var pluginClicks = 0;
			var pluginToolClicks = 0;
			var blockAction = new WinFormsActionMenuItem("Block action", () => blockClicks++);
			var pluginAction = new WinFormsActionMenuItem("Plugin action", () => pluginClicks++);
			using var pluginTool = new WinFormsActionMenuItem("Plugin refresh", () => pluginToolClicks++);
			blockMenu.AddItem(blockAction);
			pluginMenu.AddItem(pluginAction);
			block.AddMenu(blockMenu);
			var plugin = new CommandPlugin(block, pluginMenu, pluginTool);
			using var screen = new NativeCommandScreen("Shared tools") { Block = block };
			await using var application = new FormsUI.Application(form, Array.Empty<FormsUI.IApplicationBlock>(), new[] { plugin });
			await application.InitializeAsync();
			form.Show(owner);
			form.ShowScreen(screen);
			await Task.Yield();

			var header = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text == "Shared tools");
			Assert.That(header.DropDownItems.Cast<ToolStripItem>().Select(item => item.Text),
				Is.EqualTo(new[] { "Plugin action", "Block action", "Screen action", "Screen details" }));
			Assert.That(header.DropDownItems.Cast<ToolStripItem>(), Does.Contain(screen.Action).And.Contain(screen.Details));
			Assert.That(form.ToolEntries, Does.Contain(screen.RefreshButton).And.Contain(screen.CopyButton));
			header.DropDownItems.OfType<ToolStripMenuItem>().Single(item => item.Text == "Plugin action").PerformClick();
			header.DropDownItems.OfType<ToolStripMenuItem>().Single(item => item.Text == "Block action").PerformClick();
			form.ToolEntries.Single(item => item.ToolTipText == "Plugin refresh").PerformClick();
			form.ToolEntries.Single(item => item.ToolTipText == "Block action").PerformClick();
			screen.Action.PerformClick();
			screen.RefreshButton.PerformClick();
			Assert.That(pluginClicks, Is.EqualTo(1));
			Assert.That(blockClicks, Is.EqualTo(2));
			Assert.That(pluginToolClicks, Is.EqualTo(1));
			Assert.That(screen.ActionClicks, Is.EqualTo(1));
			Assert.That(screen.RefreshClicks, Is.EqualTo(1));

			// Rebuilding only creates presentation; every owner retains its once-activated command identities.
			var pluginMenus = plugin.Menus;
			pluginMenus[0] = null;
			var blockMenus = block.Menus;
			blockMenus[0] = null;
			var screenAction = screen.Menus.Single().Items.Single(item => item.Title == "Screen action");
			form.RebuildCommands();
			form.RebuildCommands();
			Assert.That(plugin.Menus.Single(), Is.SameAs(pluginMenu));
			Assert.That(plugin.ToolBarItems.Single(), Is.SameAs(pluginTool));
			Assert.That(block.Menus.Single(), Is.SameAs(blockMenu));
			Assert.That(screen.Menus.Single().Items.Single(item => item.Title == "Screen action"), Is.SameAs(screenAction));
			Assert.That(plugin.MenuActivations, Is.EqualTo(1));
			Assert.That(plugin.ToolBarActivations, Is.EqualTo(1));
			Assert.That(block.MenuActivations, Is.EqualTo(1));
			Assert.That(block.ToolBarActivations, Is.EqualTo(1));
			Assert.That(screen.MenuActivations, Is.EqualTo(1));
			Assert.That(screen.ToolBarActivations, Is.EqualTo(1));
			using var extraMenu = new ApplicationMenu("Too late");
			Assert.That(() => block.AddMenu(extraMenu), Throws.InvalidOperationException);
		});
	}

	[Test]
	public void MatchingFileHeadersMergeActualChildrenAndRestoreTheirOriginalSourceOnLeave() {
		RunWithMessageLoop(async owner => {
			using var form = new CommandMainForm { ScreenMode = ScreenMode.MultiView };
			using var screen = new NativeCommandScreen("File");
			using var other = new ApplicationScreen { Title = "Other screen" };
			await using var application = new FormsUI.Application(form, Array.Empty<FormsUI.IApplicationBlock>());
			await application.InitializeAsync();
			form.Show(owner);
			await Task.Yield();
			var file = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => Caption(item.Text) == "File");
			var applicationItems = file.DropDownItems.Cast<ToolStripItem>().ToArray();

			form.ShowScreen(screen);
			await Task.Yield();
			Assert.That(form.MenuEntries.OfType<ToolStripMenuItem>().Count(item => Caption(item.Text) == "File"), Is.EqualTo(1));
			Assert.That(file.DropDownItems.Cast<ToolStripItem>(), Does.Contain(screen.Action).And.Contain(screen.Details));
			Assert.That(file.DropDownItems.Cast<ToolStripItem>().Where(applicationItems.Contains), Is.EqualTo(applicationItems));
			Assert.That(screen.Header.DropDownItems, Is.Empty);
			Assert.That(screen.Visible, Is.True);
			Assert.That(screen.ScreenMenuStrip.Visible, Is.False,
				"Moving the File children must also collapse the now-empty local menu row.");
			Assert.That(((SharedUI.IApplicationScreen)screen).Menus.Single().Items.Select(item => item.Title),
				Is.EqualTo(new[] { "Screen action", "Screen details" }));

			form.ShowScreen(other);
			Assert.That(screen.Header.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new[] { screen.Action, screen.Details }));
			Assert.That(screen.Action.Owner, Is.SameAs(screen.Header.DropDown));
			Assert.That(screen.Details.Owner, Is.SameAs(screen.Header.DropDown));
			Assert.That(file.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(applicationItems));
			Assert.That(screen.Header.Tag, Is.SameAs(screen.HeaderTag));
			Assert.That(screen.Action.Tag, Is.SameAs(screen.ActionTag));
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ActivatedDetachedStripsRejectReplacementAndRestoreTheirExternalOwners(bool toolbarFirst) {
		RunWithMessageLoop(async owner => {
			using var source = new Panel { Bounds = new Rectangle(10, 10, 600, 180) };
			owner.Controls.Add(source);
			using var menu = new MenuStrip { Name = "Original menu", AutoSize = false, Dock = DockStyle.None, Bounds = new Rectangle(10, 10, 250, 25) };
			using var tools = new ToolStrip { Name = "Original tools", AutoSize = false, Dock = DockStyle.None, Bounds = new Rectangle(10, 40, 250, 25), Visible = false };
			using var replacementMenu = new MenuStrip { Name = "Unused menu" };
			using var replacementTools = new ToolStrip { Name = "Unused tools" };
			var tag = new object();
			var header = new ToolStripMenuItem("Original tools");
			var action = new ToolStripMenuItem("Original action") { Tag = tag };
			var button = new ToolStripButton("Original refresh") { Tag = tag };
			header.DropDownItems.Add(action);
			menu.Items.Add(header);
			tools.Items.Add(button);
			source.Controls.AddRange(toolbarFirst
				? new Control[] { tools, menu, replacementTools, replacementMenu }
				: new Control[] { menu, tools, replacementMenu, replacementTools });
			var menuBounds = menu.Bounds;
			var toolsBounds = tools.Bounds;
			var menuIndex = source.Controls.GetChildIndex(menu);
			var toolsIndex = source.Controls.GetChildIndex(tools);
			var originalStrips = source.Controls.Cast<Control>().ToArray();
			var clicks = 0;
			var menuDisposals = 0;
			var toolDisposals = 0;
			action.Click += (_, _) => clicks++;
			button.Click += (_, _) => clicks++;
			menu.Disposed += (_, _) => menuDisposals++;
			tools.Disposed += (_, _) => toolDisposals++;

			using var form = new CommandMainForm { ScreenMode = ScreenMode.MultiView };
			using var screen = new ApplicationScreen { Title = "External commands", ScreenMenuStrip = menu, ScreenToolBar = tools };
			await using var application = new FormsUI.Application(form, Array.Empty<FormsUI.IApplicationBlock>());
			await application.InitializeAsync();
			form.Show(owner);
			form.ShowScreen(screen);
			Assert.That(form.ScreenHost.UndockScreen(screen), Is.True);
			var window = (ApplicationScreenForm)screen.FindForm();
			window.Location = owner.Location;
			await Task.Yield();
			Assert.That(window.MainMenuStrip, Is.SameAs(menu));
			Assert.That(menu.Parent, Is.SameAs(window));
			Assert.That(tools.Parent, Is.SameAs(window));

			// Activated membership is fixed; rejected replacements cannot steal or dispose either owner's controls.
			Assert.That(() => screen.ScreenMenuStrip = replacementMenu, Throws.InvalidOperationException);
			Assert.That(() => screen.ScreenToolBar = replacementTools, Throws.InvalidOperationException);
			Assert.That(screen.ScreenMenuStrip, Is.SameAs(menu));
			Assert.That(screen.ScreenToolBar, Is.SameAs(tools));
			Assert.That(replacementMenu.Parent, Is.SameAs(source));
			Assert.That(replacementTools.Parent, Is.SameAs(source));
			SharedUI.IApplicationScreen commands = screen;
			((IWinFormsLinkMenuItem)commands.Menus.Single().Items.Single()).OnSelect();
			((IWinFormsLinkMenuItem)commands.ToolBarItems.Single()).OnSelect();
			Assert.That(clicks, Is.EqualTo(2));

			Assert.That(form.ScreenHost.DockScreen(screen), Is.True);
			Assert.That(source.Controls.Cast<Control>(), Is.EqualTo(originalStrips));
			AssertReturnedStrip(menu, source, menuIndex, menuBounds);
			AssertReturnedStrip(tools, source, toolsIndex, toolsBounds);
			Assert.That(form.MenuEntries, Does.Contain(header));
			Assert.That(form.ToolEntries, Does.Contain(button));
			Assert.That(menuDisposals + toolDisposals, Is.Zero);

			// Window disposal returns the borrowed source layout before the screen's one native disposal owner runs.
			Assert.That(form.ScreenHost.UndockScreen(screen), Is.True);
			window = (ApplicationScreenForm)screen.FindForm();
			var restoredBeforeScreenDisposal = false;
			screen.ScreenDestroyed += (_, _) => {
				restoredBeforeScreenDisposal = ReferenceEquals(menu.Parent, source) && ReferenceEquals(tools.Parent, source)
					&& menu.Bounds == menuBounds && tools.Bounds == toolsBounds && !menu.IsDisposed && !tools.IsDisposed
					&& menu.Visible && !tools.Visible && source.Controls.Cast<Control>().SequenceEqual(originalStrips);
			};
			window.Dispose();
			Assert.That(restoredBeforeScreenDisposal, Is.True);
			Assert.That(screen.IsDisposed, Is.True);
			Assert.That(menuDisposals, Is.EqualTo(1));
			Assert.That(toolDisposals, Is.EqualTo(1));
			Assert.That(replacementMenu.IsDisposed || replacementTools.IsDisposed, Is.False);
			Assert.That(action.Tag, Is.SameAs(tag));
			Assert.That(button.Tag, Is.SameAs(tag));
		});
	}

	[Test]
	public void ExplicitlyReparentedAndRemovedBorrowedCommandsAreNotReclaimedOnNavigation() {
		RunWithMessageLoop(async owner => {
			using var targetMenu = new MenuStrip();
			using var targetTools = new ToolStrip();
			var targetHeader = new ToolStripMenuItem("Other owner");
			targetMenu.Items.Add(targetHeader);
			owner.Controls.Add(targetMenu);
			owner.Controls.Add(targetTools);
			using var form = new CommandMainForm { ScreenMode = ScreenMode.MultiView };
			using var screen = new NativeCommandScreen("File");
			using var other = new ApplicationScreen { Title = "Other screen" };
			using var removedMenuItem = screen.Details;
			using var removedToolButton = screen.CopyButton;
			await using var application = new FormsUI.Application(form, Array.Empty<FormsUI.IApplicationBlock>());
			await application.InitializeAsync();
			form.Show(owner);
			form.ShowScreen(screen);
			await Task.Yield();
			SharedUI.IApplicationScreen commands = screen;
			Assert.That(commands.Menus.Single().Items.Count(), Is.EqualTo(2));
			Assert.That(commands.ToolBarItems.Count(), Is.EqualTo(2));
			var file = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => Caption(item.Text) == "File");
			Assert.That(file.DropDownItems, Does.Contain(screen.Action).And.Contain(screen.Details));
			Assert.That(form.ToolEntries, Does.Contain(screen.RefreshButton).And.Contain(screen.CopyButton));

			// Native ownership edits do not mutate activated definitions; the generic merger still preserves the client destination.
			targetHeader.DropDownItems.Add(screen.Action);
			file.DropDownItems.Remove(screen.Details);
			targetTools.Items.Add(screen.RefreshButton);
			screen.CopyButton.Owner.Items.Remove(screen.CopyButton);
			Assert.That(commands.Menus.Single().Items.Select(item => item.Title), Is.EqualTo(new[] { "Screen action", "Screen details" }));
			Assert.That(commands.ToolBarItems.Select(item => item.Title), Is.EqualTo(new[] { "Screen refresh", "Screen copy" }));

			form.ShowScreen(other);
			Assert.That(screen.Action.Owner, Is.SameAs(targetHeader.DropDown));
			Assert.That(screen.RefreshButton.Owner, Is.SameAs(targetTools));
			Assert.That(screen.Details.Owner, Is.Null);
			Assert.That(screen.CopyButton.Owner, Is.Null);
			Assert.That(screen.Header.DropDownItems, Is.Empty);
			Assert.That(screen.ScreenToolBar.Items, Is.Empty);
			Assert.That(commands.Menus.Single().Items.Select(item => item.Title), Is.EqualTo(new[] { "Screen action", "Screen details" }));
			Assert.That(commands.ToolBarItems.Select(item => item.Title), Is.EqualTo(new[] { "Screen refresh", "Screen copy" }));
			Assert.That(screen.Action.Tag, Is.SameAs(screen.ActionTag));
			Assert.That(screen.RefreshButton.Tag, Is.SameAs(screen.RefreshTag));
		});
	}

	[Test]
	public void LegacyRegisteredNativeItemsStillProjectAndExecuteWithoutAnAssignedMenuStrip() {
		RunWithMessageLoop(async owner => {
			using var screen = new LegacyCommandScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			FormsUI.IApplicationScreen native = screen;
			SharedUI.IApplicationScreen portable = screen;

			Assert.That(screen.ScreenMenuStrip, Is.Null);
			Assert.That(screen.MenuItems, Is.EqualTo(new[] { screen.Action }));
			var action = native.Menus.Single().Items.Single();
			Assert.That(portable.Menus.Single().Items.Single(), Is.SameAs(action));
			Assert.That(action.Title, Is.EqualTo("Legacy action"));
			((IWinFormsLinkMenuItem)action).OnSelect();
			Assert.That(screen.Clicks, Is.EqualTo(1));
			Assert.That(screen.Action.Tag, Is.SameAs(screen.ActionTag));
		});
	}

	[Test]
	public void ProjectionDoesNotOwnNativeViewsAndScreenDisposalReleasesThemOnce() {
		RunWithMessageLoop(async owner => {
			using var image = new Bitmap(2, 2);
			using var screen = new NativeCommandScreen("Screen tools", image) { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			var menuStrip = screen.ScreenMenuStrip;
			var toolStrip = screen.ScreenToolBar;
			var header = screen.Header;
			var action = screen.Action;
			var refresh = screen.RefreshButton;
			FormsUI.IApplicationScreen commands = screen;

			// Provider definitions borrow the native views and images; they are not a second disposal owner.
			commands.Menus.Single().Dispose();
			commands.ToolBarItems.Single(item => item.Title == "Screen refresh").Dispose();
			Assert.That(menuStrip.IsDisposed || toolStrip.IsDisposed || action.IsDisposed || refresh.IsDisposed, Is.False);
			Assert.That(image.Width, Is.EqualTo(2));
			Assert.That(screen.OwnedDisposals, Is.Zero);

			screen.Dispose();
			Assert.That(menuStrip.IsDisposed && toolStrip.IsDisposed && header.IsDisposed && action.IsDisposed && refresh.IsDisposed, Is.True);
			Assert.That(screen.OwnedDisposals, Is.EqualTo(7), "Two strips, one header, two menu actions and two toolbar buttons have one native owner.");
			screen.Dispose();
			Assert.That(screen.OwnedDisposals, Is.EqualTo(7));
			Assert.That(image.Width, Is.EqualTo(2));
		});
	}

	// Restored external controls retain their native owner, order and undocked geometry.
	private static void AssertReturnedStrip(Control strip, Control parent, int index, Rectangle bounds) {
		Assert.That(strip.Parent, Is.SameAs(parent));
		Assert.That(parent.Controls.GetChildIndex(strip), Is.EqualTo(index));
		Assert.That(strip.Dock, Is.EqualTo(DockStyle.None));
		Assert.That(strip.Anchor, Is.EqualTo(AnchorStyles.Top | AnchorStyles.Left));
		Assert.That(strip.Bounds, Is.EqualTo(bounds));
	}

	// Native mnemonic markers do not change the semantic caption used when merging top-level menus.
	private static string Caption(string text) => text.Replace("&", string.Empty);

	// Each case owns a real dispatcher; all native reads and retained-view transitions stay on its creating thread.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form {
			ShowInTaskbar = false,
			StartPosition = FormStartPosition.Manual,
			Location = new Point(-20000, -20000),
			ClientSize = new Size(1000, 700)
		};
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The native screen-command test timed out.");
			System.Windows.Forms.Application.ExitThread();
		};
		owner.Shown += async (_, _) => {
			try {
				await test(owner);
				completed = true;
			} catch (Exception error) {
				failure = error;
			} finally {
				watchdog.Stop();
				foreach (var window in System.Windows.Forms.Application.OpenForms.Cast<Form>().Where(window => !ReferenceEquals(window, owner)).Reverse().ToArray())
					window.Dispose();
				owner.Close();
			}
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True, "The case must complete before its dispatcher is released.");
	}

	// Walks the generated drop-down items of displayed strips without opening their popups.
	private static System.Collections.Generic.IEnumerable<ToolStripItem> Descendants(System.Collections.Generic.IEnumerable<ToolStripItem> items) {
		foreach (var item in items) {
			yield return item;
			if (item is ToolStripDropDownItem dropDown)
				foreach (var child in Descendants(dropDown.DropDownItems.Cast<ToolStripItem>()))
					yield return child;
		}
	}

	// Enumerate the displayed strips without traversing their separate popup windows or retaining view bindings.
	private static System.Collections.Generic.IEnumerable<ToolStripItem> WindowItems(Control parent) {
		foreach (Control control in parent.Controls) {
			if (control is ToolStrip strip)
				foreach (ToolStripItem item in strip.Items)
					yield return item;
			foreach (var item in WindowItems(control))
				yield return item;
		}
	}

	#region Inner Types

	// The probe only exposes the existing protected frame surfaces; it does not replace composition behavior.
	private class CommandMainForm : BlockMainForm {
		public CommandMainForm() {
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.Manual;
			Location = new Point(-20000, -20000);
			ClientSize = new Size(1000, 700);
		}

		public ToolStripItem[] MenuEntries => MenuStrip.Items.Cast<ToolStripItem>().ToArray();

		public ToolStripItem[] ToolEntries => ToolStrip.Items.Cast<ToolStripItem>().ToArray();

		public void RebuildCommands() => RebuildApplicationCommands();
	}

	// Designer-native commands stay on the main form while detached windows invoke those same callbacks.
	private sealed class DesignerCommandMainForm : CommandMainForm {
		public DesignerCommandMainForm() {
			DesignerHeader = new ToolStripMenuItem("Application designer") { Name = "designerApplicationMenu" };
			var nested = new ToolStripMenuItem("Nested actions");
			DesignerAction = new ToolStripMenuItem("Designer action") { Name = "designerApplicationAction", Checked = true, CheckOnClick = true };
			DisabledAction = new ToolStripMenuItem("Disabled action") { Name = "disabledDesignerAction", Enabled = false };
			nested.DropDownItems.AddRange(new ToolStripItem[] { DesignerAction, new ToolStripSeparator(), DisabledAction });
			DesignerHeader.DropDownItems.Add(nested);
			MenuStrip.Items.Add(DesignerHeader);
			DesignerRefresh = new ToolStripButton("Designer refresh") { Name = "designerApplicationRefresh", Checked = true, CheckOnClick = true };
			ToolStrip.Items.Add(DesignerRefresh);
			DesignerSplit = new ToolStripSplitButton("Designer split") { Name = "designerApplicationSplit" };
			DesignerSplit.DropDownItems.Add(new ToolStripMenuItem("Split details"));
			ToolStrip.Items.Add(DesignerSplit);
			DesignerAction.Click += (_, _) => DesignerClicks++;
			DesignerRefresh.Click += (_, _) => {
				ToolbarClicks++;
				LastToolbarHost = InvocationHost;
			};
			DesignerSplit.ButtonClick += (_, _) => {
				SplitClicks++;
				LastSplitHost = InvocationHost;
			};
			DesignerSplit.Click += (_, _) => SplitItemClicks++;
		}

		public ToolStripMenuItem DesignerHeader { get; }

		public ToolStripMenuItem DesignerAction { get; }

		public ToolStripMenuItem DisabledAction { get; }

		public ToolStripButton DesignerRefresh { get; }

		public ToolStripSplitButton DesignerSplit { get; }

		public ToolStripMenuItem OptionsCommand
			=> OptionsToolStripMenuItem.DropDownItems.OfType<ToolStripMenuItem>().Single(item => item.Name == "_screenOptionsMenuItem");

		public int DesignerClicks { get; private set; }

		public int ToolbarClicks { get; private set; }

		public int SplitClicks { get; private set; }

		public int SplitItemClicks { get; private set; }

		public SharedUI.IApplicationScreenHost LastToolbarHost { get; private set; }

		public SharedUI.IApplicationScreenHost LastSplitHost { get; private set; }

		public SharedUI.IApplicationScreen HelpScreen { get; private set; }

		public SharedUI.IApplicationScreenHost InvocationHost => CommandUserInterfaceServices.ScreenHost;

		// Observe the actual native event's explicit host without starting a browser or help process.
		protected override void ContextHelp_Click(object sender, EventArgs args) => HelpScreen = CommandUserInterfaceServices.ScreenHost?.ActiveScreen;
	}

	// Both owners expose their definitions once, while each displaying window owns its generated views.
	private sealed class CommandPlugin : FormsUI.ApplicationPlugin {
		private readonly FormsUI.IApplicationMenu _menu;
		private readonly FormsUI.IApplicationMenuItem _tool;

		public CommandPlugin(FormsUI.IApplicationBlock block, FormsUI.IApplicationMenu menu, FormsUI.IApplicationMenuItem tool)
			: base("Command plugin", new[] { block }) {
			_menu = menu;
			_tool = tool;
		}

		public int MenuActivations { get; private set; }

		public int ToolBarActivations { get; private set; }

		protected override FormsUI.IApplicationMenu[] GetMenus() {
			MenuActivations++;
			return new[] { _menu };
		}

		protected override FormsUI.IApplicationMenuItem[] GetToolBarItems() {
			ToolBarActivations++;
			return new[] { _tool };
		}
	}

	// The ordinary block implementation still supplies and owns its configured menu definitions.
	private sealed class CommandBlock : ApplicationBlock {
		public int MenuActivations { get; private set; }

		public int ToolBarActivations { get; private set; }

		protected override FormsUI.IApplicationMenu[] GetMenus() {
			MenuActivations++;
			return base.GetMenus();
		}

		protected override FormsUI.IApplicationMenuItem[] GetToolBarItems() {
			ToolBarActivations++;
			return base.GetToolBarItems();
		}
	}

	// A designer-like screen assigns real native strips and Click handlers, with no parallel command definitions.
	private sealed class NativeCommandScreen : ApplicationScreen {
		public NativeCommandScreen(string caption, Image image = null) {
			Title = "Native commands";
			ScreenMenuStrip = new MenuStrip();
			ScreenToolBar = new ToolStrip();
			Header = new ToolStripMenuItem(caption) { Tag = HeaderTag };
			Action = new ToolStripMenuItem("Screen action") { Tag = ActionTag, Image = image };
			Details = new ToolStripMenuItem("Screen details");
			RefreshButton = new ToolStripButton("Screen refresh") { Tag = RefreshTag, Image = image };
			CopyButton = new ToolStripButton("Screen copy");
			Header.DropDownItems.AddRange(new ToolStripItem[] { Action, Details });
			ScreenMenuStrip.Items.Add(Header);
			ScreenToolBar.Items.AddRange(new ToolStripItem[] { RefreshButton, CopyButton });
			Controls.Add(ScreenMenuStrip);
			Controls.Add(ScreenToolBar);
			Action.Click += (_, _) => ActionClicks++;
			RefreshButton.Click += (_, _) => RefreshClicks++;
			foreach (var control in new Control[] { ScreenMenuStrip, ScreenToolBar })
				control.Disposed += (_, _) => OwnedDisposals++;
			foreach (var item in new ToolStripItem[] { Header, Action, Details, RefreshButton, CopyButton })
				item.Disposed += (_, _) => OwnedDisposals++;
		}

		public object HeaderTag { get; } = new();

		public object ActionTag { get; } = new();

		public object RefreshTag { get; } = new();

		public ToolStripMenuItem Header { get; }

		public ToolStripMenuItem Action { get; }

		public ToolStripMenuItem Details { get; }

		public ToolStripButton RefreshButton { get; }

		public ToolStripButton CopyButton { get; }

		public int ActionClicks { get; private set; }

		public int RefreshClicks { get; private set; }

		public int OwnedDisposals { get; private set; }

		public int MenuActivations { get; private set; }

		public int ToolBarActivations { get; private set; }

		protected override FormsUI.IApplicationMenu[] GetMenus() {
			MenuActivations++;
			return base.GetMenus();
		}

		protected override FormsUI.IApplicationMenuItem[] GetToolBarItems() {
			ToolBarActivations++;
			return base.GetToolBarItems();
		}
	}

	// Existing screens can keep RegisterMenuItem until their designer owns a dedicated MenuStrip.
	private sealed class LegacyCommandScreen : ApplicationScreen {
		public LegacyCommandScreen() {
			ApplicationMenuStripText = "Legacy commands";
			ShowInApplicationMenuStrip = true;
			Action = new ToolStripMenuItem("Legacy action") { Tag = ActionTag };
			Action.Click += (_, _) => Clicks++;
			RegisterMenuItem(Action);
		}

		public object ActionTag { get; } = new();

		public ToolStripMenuItem Action { get; }

		public int Clicks { get; private set; }
	}

	#endregion
}
