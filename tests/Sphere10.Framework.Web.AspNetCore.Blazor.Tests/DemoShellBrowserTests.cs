// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>Exercises the running tester's shared shell through its actual browser controls and scoped services.</summary>
[TestFixture]
[Explicit("Requires BlazorTesterUrl and an installed Chromium or BrowserExecutablePath.")]
[Category("Browser")]
[NonParallelizable]
public class DemoShellBrowserTests {
	private readonly List<string> _errors = new();
	private IPlaywright _playwright;
	private IBrowser _browser;
	private IPage _page;
	private Uri _origin;

	[SetUp]
	public async Task SetUp() {
		var url = TestContext.Parameters.Get("BlazorTesterUrl", "");
		Assert.That(Uri.TryCreate(url, UriKind.Absolute, out _origin) && _origin.IsLoopback && _origin.Scheme is "http" or "https", Is.True,
			"Set BlazorTesterUrl to a running local tester in the runsettings TestRunParameters.");
		var executable = TestContext.Parameters.Get("BrowserExecutablePath", "");
		_errors.Clear();
		_playwright = await Playwright.CreateAsync();
		_browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
			Headless = true,
			IgnoreDefaultArgs = new[] { "--hide-scrollbars" },
			ExecutablePath = string.IsNullOrWhiteSpace(executable) ? null : executable
		});
		var context = await _browser.NewContextAsync(new BrowserNewContextOptions {
			ViewportSize = new ViewportSize { Width = 1500, Height = 1000 },
			DeviceScaleFactor = 1.25f
		});
		_page = await context.NewPageAsync();
		_page.SetDefaultTimeout(15000);
		_page.PageError += (_, error) => _errors.Add(error);
		_page.Console += (_, message) => {
			if (message.Type == "error")
				_errors.Add(message.Text);
		};
	}

	[TearDown]
	public async Task TearDown() {
		using var runtimeCleanup = Tools.Scope.ExecuteOnDispose(() => _playwright?.Dispose());
		await using var browserCleanup = new TaskScope(async () => {
			if (_browser != null)
				await _browser.DisposeAsync();
		});
		if (_page != null && !_page.IsClosed && TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
			await AttachScreenshotAsync("failure");
		Assert.That(_errors, Is.Empty, "The shared shell must not produce browser console or script errors.");
	}

	[TestCase(1500, 1000)]
	[TestCase(390, 844)]
	public async Task ClassicShellAndThemeSwitchPreserveGridDraftWithoutPageOverflow(int width, int height) {
		await _page.SetViewportSizeAsync(width, height);
		await OpenInteractiveGridAsync();
		var theme = _page.GetByRole(AriaRole.Combobox, new() { Name = "Theme", Exact = true });
		var endpoint = _page.GetByRole(AriaRole.Combobox, new() { Name = "Endpoint", Exact = true });
		var sidebar = _page.GetByRole(AriaRole.Complementary, new() { Name = "Application navigation", Exact = true });
		var dock = _page.GetByRole(AriaRole.Navigation, new() { Name = "Application blocks", Exact = true });
		if (width <= 760)
			await _page.GetByRole(AriaRole.Button, new() { Name = "Menu", Exact = true }).ClickAsync();
		Assert.That(await theme.InputValueAsync(), Is.EqualTo("classic-blue"));
		Assert.That(await theme.Locator("option:checked").InnerTextAsync(), Is.EqualTo("Classic blue"));
		await endpoint.WaitForAsync();
		Assert.That(await endpoint.IsVisibleAsync(), Is.True);
		Assert.That(await sidebar.EvaluateAsync<string>("element => getComputedStyle(element).backgroundImage"), Does.Contain("linear-gradient"));
		Assert.That(await _page.Locator(".sphere10-header").EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor"), Is.EqualTo("rgb(255, 255, 255)"));
		Assert.That(await dock.GetByRole(AriaRole.Button, new() { Name = "Workspace", Exact = true }).Locator("img").GetAttributeAsync("src"),
			Is.EqualTo("img/heading-solid.svg"));
		Assert.That(await dock.GetByRole(AriaRole.Button, new() { Name = "Component gallery", Exact = true }).Locator("img").GetAttributeAsync("src"),
			Is.EqualTo("img/boxes-solid.svg"));
		var endpointBounds = await endpoint.BoundingBoxAsync();
		var dockBounds = await dock.BoundingBoxAsync();
		Assert.That(endpointBounds, Is.Not.Null);
		Assert.That(dockBounds, Is.Not.Null);
		Assert.That(endpointBounds.X, Is.LessThan(width / 2), "Endpoint selection belongs in the top-left shell region.");
		if (width > 760) {
			Assert.That(endpointBounds.Y, Is.GreaterThanOrEqualTo((await _page.Locator(".sphere10-chrome").BoundingBoxAsync()).Height), "The endpoint belongs below the full-width application commands.");
			Assert.That(dockBounds.Y, Is.GreaterThan(height / 2), "Block icons remain docked beneath sidebar navigation.");
			Assert.That(dockBounds.Y + dockBounds.Height, Is.LessThanOrEqualTo(height + 1));
		} else {
			var menu = _page.GetByRole(AriaRole.Button, new() { Name = "Menu", Exact = true });
			Assert.That(await sidebar.IsVisibleAsync(), Is.True);
			Assert.That(await menu.GetAttributeAsync("aria-expanded"), Is.EqualTo("true"));
			await AttachScreenshotAsync("mobile-navigation");
			await menu.ClickAsync();
			await sidebar.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
		}
		var documentSize = await _page.EvaluateAsync<double[]>("() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
		Assert.That(documentSize[0], Is.LessThanOrEqualTo(documentSize[1] + 1), "The shell must contain the grid's intentional horizontal scrolling.");
		await AttachScreenshotAsync("classic-blue");

		var grid = _page.Locator(".sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table").Locator("xpath=../..");
		await _page.Locator(".sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table > tbody > tr").First.ClickAsync();
		await grid.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
		var name = grid.GetByRole(AriaRole.Textbox, new() { Name = "Name", Exact = true });
		await name.FillAsync("Draft retained across themes");
		await name.PressAsync("Tab");
		await theme.SelectOptionAsync("blue");
		await WaitForThemeAsync("blue");
		Assert.That(await theme.Locator("option:checked").InnerTextAsync(), Is.EqualTo("Blue"));
		Assert.That(await _page.Locator(".sphere10-header").EvaluateAsync<string>("element => getComputedStyle(element).backgroundImage"), Does.Contain("linear-gradient"));
		Assert.That(await _page.Locator(".sphere10-theme").EvaluateAsync<string>("element => getComputedStyle(element).fontFamily"), Does.Contain("system-ui"));
		Assert.That(await name.InputValueAsync(), Is.EqualTo("Draft retained across themes"));
		await AttachScreenshotAsync("blue-draft");
		await theme.SelectOptionAsync("classic-blue");
		await WaitForThemeAsync("classic-blue");
		Assert.That(await _page.Locator(".sphere10-header").EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor"), Is.EqualTo("rgb(255, 255, 255)"));
		Assert.That(await _page.Locator(".sphere10-theme").EvaluateAsync<string>("element => getComputedStyle(element).fontFamily"), Does.StartWith("Roboto"));
		Assert.That(await name.InputValueAsync(), Is.EqualTo("Draft retained across themes"));
		await theme.SelectOptionAsync("dark");
		await WaitForThemeAsync("dark");
		Assert.That(await name.InputValueAsync(), Is.EqualTo("Draft retained across themes"));
		await AttachScreenshotAsync("dark-draft");
		await theme.SelectOptionAsync("light");
		await WaitForThemeAsync("light");
		Assert.That(await name.InputValueAsync(), Is.EqualTo("Draft retained across themes"));
		documentSize = await _page.EvaluateAsync<double[]>("() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
		Assert.That(documentSize[0], Is.LessThanOrEqualTo(documentSize[1] + 1));
		await grid.GetByRole(AriaRole.Group, new() { Name = "Edit item", Exact = true }).GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();

		var search = _page.Locator(".demo-search");
		await search.GetByRole(AriaRole.Searchbox, new() { Name = "Search", Exact = true }).FillAsync("Dialogs");
		await search.GetByRole(AriaRole.Searchbox, new() { Name = "Search", Exact = true }).PressAsync("End");
		await search.GetByRole(AriaRole.Link, new() { Name = "Dialogs", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Heading, new() { Name = "Awaited dialogs", Exact = true }).WaitForAsync();
		await _page.WaitForFunctionAsync("() => location.pathname === '/application' && new URLSearchParams(location.search).get('block') === 'components' && new URLSearchParams(location.search).get('screen') === 'dialogs'");
		Assert.That(await theme.InputValueAsync(), Is.EqualTo("light"));
	}

	[TestCase(1500, 1000)]
	[TestCase(390, 844)]
	public async Task ToolbarIdentityActionsAndDismissalPreserveTheActiveScreenAndDraft(int width, int height) {
		await _page.SetViewportSizeAsync(width, height);
		await OpenInteractiveGridAsync();
		var toolbar = _page.GetByRole(AriaRole.Toolbar, new() { Name = "Application toolbar", Exact = true });
		var theme = toolbar.GetByRole(AriaRole.Combobox, new() { Name = "Theme", Exact = true });
		var search = toolbar.GetByRole(AriaRole.Searchbox, new() { Name = "Search", Exact = true });
		var identity = toolbar.GetByRole(AriaRole.Button, new() { Name = "Demo user", Exact = true });
		Assert.That(await search.IsVisibleAsync(), Is.True, "Search belongs in the compact toolbar alongside theme and identity controls.");
		Assert.That(await theme.IsVisibleAsync(), Is.True);
		await identity.WaitForAsync();
		var toolbarBounds = await toolbar.BoundingBoxAsync();
		var identityBounds = await identity.BoundingBoxAsync();
		Assert.That(identityBounds.X, Is.GreaterThanOrEqualTo(0));
		Assert.That(identityBounds.X + identityBounds.Width, Is.LessThanOrEqualTo(width + 1));
		Assert.That(identityBounds.Y, Is.GreaterThanOrEqualTo(toolbarBounds.Y - 1));
		Assert.That(identityBounds.Y + identityBounds.Height, Is.LessThanOrEqualTo(toolbarBounds.Y + toolbarBounds.Height + 1));
		Assert.That(identityBounds.Width, Is.EqualTo(32).Within(.5));
		Assert.That(identityBounds.Height, Is.EqualTo(identityBounds.Width).Within(.5), "The identity trigger must remain a fixed circular avatar.");
		Assert.That(await identity.GetAttributeAsync("title"), Is.EqualTo("Demo user"));
		Assert.That((await identity.InnerTextAsync()).Trim(), Is.Empty, "The account name belongs in the dropdown and accessible name, not beside the avatar.");
		Assert.That(await identity.Locator(".sphere10-identity-silhouette").CountAsync(), Is.EqualTo(1));

		var table = _page.Locator(".sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table");
		var grid = table.Locator("xpath=../..");
		var originalPanel = await table.EvaluateAsync<string>("element => element.closest('[role=tabpanel]').id");
		await table.Locator(":scope > tbody > tr").First.ClickAsync();
		await grid.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
		var name = grid.GetByRole(AriaRole.Textbox, new() { Name = "Name", Exact = true });
		await name.FillAsync("Draft retained across demo identity changes");
		await name.PressAsync("Tab");
		var originalUrl = _page.Url;

		await identity.ClickAsync();
		var profile = _page.GetByRole(AriaRole.Menuitem, new() { Name = "Profile", Exact = true });
		await profile.WaitForAsync();
		Assert.That(await identity.GetAttributeAsync("aria-expanded"), Is.EqualTo("true"));
		await _page.GetByText("Local demo account", new() { Exact = true }).Last.WaitForAsync();
		var signOut = _page.GetByRole(AriaRole.Menuitem, new() { Name = "Sign out of demo", Exact = true });
		await profile.PressAsync("End");
		await _page.WaitForFunctionAsync("() => document.activeElement?.getAttribute('role') === 'menuitem' && document.activeElement.textContent.trim() === 'Sign out of demo'");
		Assert.That(await signOut.EvaluateAsync<bool>("element => element === document.activeElement"), Is.True);
		await signOut.PressAsync("Home");
		await _page.WaitForFunctionAsync("() => document.activeElement?.getAttribute('role') === 'menuitem' && document.activeElement.textContent.trim() === 'Profile'");
		Assert.That(await profile.EvaluateAsync<bool>("element => element === document.activeElement"), Is.True);
		Assert.That(await identity.GetAttributeAsync("aria-expanded"), Is.EqualTo("true"), "Keyboard navigation must keep the menu open without executing a command.");
		Assert.That(await _page.Locator("#modern-modal").IsVisibleAsync(), Is.False);
		Assert.That(await toolbar.GetByRole(AriaRole.Button, new() { Name = "Guest", Exact = true }).CountAsync(), Is.Zero);
		await AttachScreenshotAsync("identity-dropdown");
		await identity.PressAsync("Escape");
		await profile.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
		await _page.WaitForFunctionAsync("() => document.activeElement?.getAttribute('aria-label') === 'Demo user'");
		Assert.That(await identity.EvaluateAsync<bool>("element => element === document.activeElement"), Is.True);
		await identity.ClickAsync();
		await profile.WaitForAsync();
		var body = await _page.Locator(".sphere10-screen-body").BoundingBoxAsync();
		await _page.Mouse.ClickAsync(body.X + 8, body.Y + Math.Min(body.Height - 8, 120));
		await profile.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
		Assert.That(await identity.GetAttributeAsync("aria-expanded"), Is.EqualTo("false"));

		await identity.ClickAsync();
		await profile.ClickAsync();
		var dialog = _page.Locator("#modern-modal");
		await dialog.GetByRole(AriaRole.Heading, new() { Name = "Demo profile", Exact = true }).WaitForAsync();
		Assert.That(await dialog.InnerTextAsync(), Does.Contain("Demo user").And.Contain("demo@sphere10.local").And.Contain("Tester").And.Contain("does not authenticate with a server"));
		await AttachScreenshotAsync("identity-profile");
		await dialog.Locator(".modal-footer").GetByRole(AriaRole.Button, new() { Name = "Close", Exact = true }).ClickAsync();
		await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
		await identity.ClickAsync();
		await _page.GetByRole(AriaRole.Menuitem, new() { Name = "Sign out of demo", Exact = true }).ClickAsync();
		var guest = toolbar.GetByRole(AriaRole.Button, new() { Name = "Guest", Exact = true });
		await guest.WaitForAsync();
		var guestBounds = await guest.BoundingBoxAsync();
		Assert.That(guestBounds.Width, Is.EqualTo(identityBounds.Width).Within(.5), "Signing out must not resize the avatar.");
		Assert.That(guestBounds.Height, Is.EqualTo(identityBounds.Height).Within(.5));
		Assert.That(guestBounds.X, Is.EqualTo(identityBounds.X).Within(.5), "Changing the identity must not shift the toolbar controls.");
		Assert.That(guestBounds.Y, Is.EqualTo(identityBounds.Y).Within(.5));
		Assert.That(await guest.GetAttributeAsync("title"), Is.EqualTo("Guest"));
		await AttachScreenshotAsync("toolbar-identity-guest");
		Assert.That(_page.Url, Is.EqualTo(originalUrl));
		Assert.That(await name.InputValueAsync(), Is.EqualTo("Draft retained across demo identity changes"));
		Assert.That(await table.EvaluateAsync<string>("element => element.closest('[role=tabpanel]').id"), Is.EqualTo(originalPanel));
		await theme.SelectOptionAsync("dark");
		await WaitForThemeAsync("dark");
		await guest.ClickAsync();
		await _page.GetByRole(AriaRole.Menuitem, new() { Name = "Use demo account", Exact = true }).ClickAsync();
		await identity.WaitForAsync();
		var restoredBounds = await identity.BoundingBoxAsync();
		Assert.That(restoredBounds.Width, Is.EqualTo(identityBounds.Width).Within(.5));
		Assert.That(restoredBounds.Height, Is.EqualTo(identityBounds.Height).Within(.5), "Signing in again under another theme must retain the fixed avatar size.");
		Assert.That(await name.InputValueAsync(), Is.EqualTo("Draft retained across demo identity changes"));
		Assert.That(await theme.InputValueAsync(), Is.EqualTo("dark"));
		var documentSize = await _page.EvaluateAsync<double[]>("() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
		Assert.That(documentSize[0], Is.LessThanOrEqualTo(documentSize[1] + 1));
		await AttachScreenshotAsync("toolbar-identity-dark");
		await grid.GetByRole(AriaRole.Group, new() { Name = "Edit item", Exact = true }).GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
	}

	[Test]
	public async Task NativeStyleShellPlacesSharedCommandsAboveSidebarTabsAndScreenBody() {
		await OpenInteractiveGridAsync();
		await _page.WaitForFunctionAsync("() => document.title === 'Sphere10 Blazor demos'");
		Assert.That(await _page.TitleAsync(), Is.EqualTo("Sphere10 Blazor demos"));
		var favicon = _page.Locator("head link[rel=icon]");
		Assert.That(await favicon.CountAsync(), Is.EqualTo(1), "The configured application icon must replace the template favicon.");
		Assert.That(await favicon.GetAttributeAsync("type"), Is.EqualTo("image/svg+xml"));
		var faviconUrl = new Uri(new Uri(_page.Url), await favicon.GetAttributeAsync("href"));
		Assert.That(faviconUrl.AbsolutePath, Does.Match(@"^/img/logo(?:\.[A-Za-z0-9_-]+)?\.svg$"));
		Assert.That(await _page.EvaluateAsync<int>("async url => (await fetch(url)).status", faviconUrl.AbsoluteUri), Is.EqualTo(200));
		var shell = _page.Locator(".sphere10-application");
		var chrome = _page.Locator(".sphere10-chrome");
		var sidebar = _page.GetByRole(AriaRole.Complementary, new() { Name = "Application navigation", Exact = true });
		var commands = _page.GetByRole(AriaRole.Navigation, new() { Name = "Application commands", Exact = true });
		var toolbar = _page.GetByRole(AriaRole.Toolbar, new() { Name = "Application toolbar", Exact = true });
		var tabs = _page.GetByRole(AriaRole.Tablist, new() { Name = "Application screens", Exact = true });
		var body = _page.Locator(".sphere10-screen-body");
		var shellBounds = await shell.BoundingBoxAsync();
		var chromeBounds = await chrome.BoundingBoxAsync();
		var sidebarBounds = await sidebar.BoundingBoxAsync();
		var commandBounds = await commands.BoundingBoxAsync();
		var toolbarBounds = await toolbar.BoundingBoxAsync();
		var tabsBounds = await tabs.BoundingBoxAsync();
		var bodyBounds = await body.BoundingBoxAsync();
		Assert.That(shellBounds.X, Is.EqualTo(0).Within(1));
		Assert.That(shellBounds.Y, Is.EqualTo(0).Within(1));
		Assert.That(shellBounds.Width, Is.EqualTo(1500).Within(1));
		Assert.That(shellBounds.Height, Is.EqualTo(1000).Within(1));
		Assert.That(chromeBounds.X, Is.EqualTo(shellBounds.X).Within(1));
		Assert.That(chromeBounds.Width, Is.EqualTo(shellBounds.Width).Within(1), "Commands belong above both navigation and content.");
		Assert.That(commandBounds.X, Is.LessThan(sidebarBounds.Width));
		Assert.That(toolbarBounds.X, Is.LessThan(sidebarBounds.Width));
		Assert.That(toolbarBounds.Y, Is.GreaterThanOrEqualTo(commandBounds.Y + commandBounds.Height - 1));
		Assert.That(sidebarBounds.Y, Is.GreaterThanOrEqualTo(chromeBounds.Y + chromeBounds.Height - 1));
		Assert.That(tabsBounds.Y, Is.GreaterThanOrEqualTo(sidebarBounds.Y - 1));
		Assert.That(tabsBounds.X, Is.GreaterThanOrEqualTo(sidebarBounds.X + sidebarBounds.Width - 1));
		Assert.That(bodyBounds.Y, Is.GreaterThanOrEqualTo(tabsBounds.Y + tabsBounds.Height - 1));
		Assert.That(bodyBounds.X, Is.GreaterThanOrEqualTo(sidebarBounds.X + sidebarBounds.Width - 1));
		Assert.That(await commands.Locator(".sphere10-menu-button").AllTextContentsAsync(), Does.Contain("File").And.Contain("View").And.Contain("Help"));
		Assert.That(await commands.Locator(".sphere10-menu-button").Last.InnerTextAsync(), Is.EqualTo("Help"));
		Assert.That(await sidebar.GetByRole(AriaRole.Navigation, new() { Name = "Application blocks", Exact = true }).GetByRole(AriaRole.Button).CountAsync(), Is.EqualTo(4));
		Assert.That(await _page.Locator(".demo-sidebar, .demo-header, .demo-main").CountAsync(), Is.Zero, "Provider layout must not create a second navigation shell.");
		Assert.That(await _page.Locator(".sphere10-screen[role=tabpanel]:not([hidden])").CountAsync(), Is.EqualTo(1));
		await AttachScreenshotAsync("full-page-application-shell");
	}

	[Test]
	public async Task BrowsingBlocksKeepsTheActiveGridUntilAScreenIsSelected() {
		await OpenInteractiveGridAsync();
		var dock = _page.GetByRole(AriaRole.Navigation, new() { Name = "Application blocks", Exact = true });
		var table = _page.Locator(".sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table");
		var grid = table.Locator("xpath=../..");
		var originalPanel = await table.EvaluateAsync<string>("element => element.closest('[role=tabpanel]').id");
		await table.Locator(":scope > tbody > tr").Nth(2).ClickAsync();
		await grid.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
		var name = grid.GetByRole(AriaRole.Textbox, new() { Name = "Name", Exact = true });
		await name.FillAsync("Retained across application blocks");
		await name.PressAsync("Tab");
		var gridUrl = _page.Url;
		await dock.GetByRole(AriaRole.Button, new() { Name = "Workspace", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Navigation, new() { Name = "Workspace menus", Exact = true }).WaitForAsync();
		Assert.That(_page.Url, Is.EqualTo(gridUrl), "Browsing block menus must not add a screen history entry.");
		Assert.That(await _page.Locator("#" + originalPanel).GetAttributeAsync("hidden"), Is.Null);
		Assert.That(await name.InputValueAsync(), Is.EqualTo("Retained across application blocks"));
		await _page.GetByRole(AriaRole.Navigation, new() { Name = "Workspace menus", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Overview", Exact = true }).ClickAsync();
		await WaitForBlockAsync("workspace");
		Assert.That(await _page.Locator("#" + originalPanel).GetAttributeAsync("hidden"), Is.Not.Null);
		await dock.GetByRole(AriaRole.Button, new() { Name = "Component gallery", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Tab, new() { Name = "CRUD grid", Exact = true }).ClickAsync();
		await WaitForInteractiveGridAsync();
		Assert.That(await table.EvaluateAsync<string>("element => element.closest('[role=tabpanel]').id"), Is.EqualTo(originalPanel));
		Assert.That(await name.InputValueAsync(), Is.EqualTo("Retained across application blocks"));
		Assert.That(await table.Locator(":scope > tbody > tr").Nth(2).GetAttributeAsync("aria-selected"), Is.EqualTo("true"));
		await grid.GetByRole(AriaRole.Group, new() { Name = "Edit item", Exact = true }).GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
	}

	[Test]
	public async Task ReopeningTheGridCreatesIndependentDraftsAndUniqueElementIds() {
		await OpenInteractiveGridAsync();
		var activePanel = _page.Locator(".sphere10-screen[role=tabpanel]:not([hidden])");
		var firstPanelId = await activePanel.GetAttributeAsync("id");
		var firstPanel = _page.Locator("#" + firstPanelId);
		var firstTable = firstPanel.Locator("[data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table");
		var firstGrid = firstTable.Locator("xpath=../..");
		var firstTableId = await firstTable.GetAttributeAsync("id");
		await firstTable.Locator(":scope > tbody > tr").First.ClickAsync();
		await firstGrid.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
		var firstName = firstGrid.GetByRole(AriaRole.Textbox, new() { Name = "Name", Exact = true });
		await firstName.FillAsync("Draft in the first grid instance");
		await firstName.PressAsync("Tab");

		await _page.GetByRole(AriaRole.Navigation, new() { Name = "Component gallery menus", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "CRUD grid", Exact = true }).ClickAsync();
		await _page.WaitForFunctionAsync("id => document.querySelector('.sphere10-screen[role=tabpanel]:not([hidden])')?.id !== id", firstPanelId);
		await WaitForInteractiveGridAsync();
		var secondPanelId = await activePanel.GetAttributeAsync("id");
		Assert.That(secondPanelId, Is.Not.EqualTo(firstPanelId));
		Assert.That(await _page.GetByRole(AriaRole.Tab, new() { Name = "CRUD grid", Exact = true }).CountAsync(), Is.EqualTo(2));
		var secondTable = activePanel.Locator("[data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table");
		var secondGrid = secondTable.Locator("xpath=../..");
		Assert.That(await secondTable.GetAttributeAsync("id"), Is.Not.EqualTo(firstTableId));
		await secondTable.Locator(":scope > tbody > tr").First.ClickAsync();
		await secondGrid.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
		var secondName = secondGrid.GetByRole(AriaRole.Textbox, new() { Name = "Name", Exact = true });
		Assert.That(await secondName.InputValueAsync(), Is.Not.EqualTo("Draft in the first grid instance"));
		await secondName.FillAsync("Draft in the second grid instance");
		await secondName.PressAsync("Tab");
		Assert.That(await _page.EvaluateAsync<bool>("() => { const ids = Array.from(document.querySelectorAll('[data-demo=grid] [id]'), element => element.id); return new Set(ids).size === ids.length; }"),
			Is.True, "Tables and their associated search and paging inputs must have unique IDs across retained screen instances.");

		await _page.GetByRole(AriaRole.Tab, new() { Name = "CRUD grid", Exact = true }).And(_page.Locator($"[aria-controls='{firstPanelId}']")).ClickAsync();
		await firstPanel.WaitForAsync();
		Assert.That(await firstName.InputValueAsync(), Is.EqualTo("Draft in the first grid instance"));
		Assert.That(await _page.Locator("#" + secondPanelId).GetAttributeAsync("hidden"), Is.Not.Null);
		await AttachScreenshotAsync("independent-grid-instances");
	}

	[TestCase("Tables", "tables", "Virtual paged table")]
	[TestCase("Dialogs", "dialogs", "Awaited dialogs")]
	public async Task InContentCompatibilityLinksActivateHostedScreensAndPreserveTheGrid(string label, string screenId, string heading) {
		await OpenInteractiveGridAsync();
		var table = _page.Locator(".sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table");
		var grid = table.Locator("xpath=../..");
		var originalPanel = await table.EvaluateAsync<string>("element => element.closest('[role=tabpanel]').id");
		await table.Locator(":scope > tbody > tr").First.ClickAsync();
		await grid.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
		var name = grid.GetByRole(AriaRole.Textbox, new() { Name = "Name", Exact = true });
		await name.FillAsync("Retained through alias navigation");
		await name.PressAsync("Tab");
		await _page.GetByRole(AriaRole.Navigation, new() { Name = "Related component demos", Exact = true })
			.GetByRole(AriaRole.Link, new() { Name = label, Exact = true }).ClickAsync();
		await _page.WaitForFunctionAsync("screen => location.pathname === '/application' && new URLSearchParams(location.search).get('block') === 'components' && new URLSearchParams(location.search).get('screen') === screen", screenId);
		await _page.GetByRole(AriaRole.Heading, new() { Name = heading, Exact = true }).WaitForAsync();
		Assert.That(await _page.GetByRole(AriaRole.Tab, new() { Name = label, Exact = true }).GetAttributeAsync("aria-selected"), Is.EqualTo("true"));
		await _page.GetByRole(AriaRole.Tab, new() { Name = "CRUD grid", Exact = true }).ClickAsync();
		await WaitForInteractiveGridAsync();
		Assert.That(await table.EvaluateAsync<string>("element => element.closest('[role=tabpanel]').id"), Is.EqualTo(originalPanel));
		Assert.That(await name.InputValueAsync(), Is.EqualTo("Retained through alias navigation"));
		Assert.That(await _page.Locator(".sphere10-application").CountAsync(), Is.EqualTo(1));
	}

	[Test]
	public async Task EndpointPageAndSidebarShareSelectionAcrossNavigation() {
		await OpenInteractiveGridAsync();
		var endpoint = _page.GetByRole(AriaRole.Combobox, new() { Name = "Endpoint", Exact = true });
		var original = await endpoint.InputValueAsync();
		var added = $"https://browser-{Guid.NewGuid():N}.example.test/";
		await _page.GetByRole(AriaRole.Link, new() { Name = "Manage endpoints", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Heading, new() { Name = "Endpoint sample", Exact = true }).WaitForAsync();
		await _page.GetByLabel("New Server", new() { Exact = true }).FillAsync(added);
		await _page.GetByLabel("New Server", new() { Exact = true }).PressAsync("Tab");
		await _page.GetByRole(AriaRole.Button, new() { Name = "Add", Exact = true }).ClickAsync();
		await _page.WaitForFunctionAsync("value => Array.from(document.querySelector('select[aria-label=Endpoint]').options).some(option => option.value === value)", added);
		await endpoint.SelectOptionAsync(added);
		var addedRow = _page.Locator(".sphere10-screen:not([hidden]) tbody > tr").Filter(new() { HasText = added });
		await addedRow.GetByText("Selected", new() { Exact = true }).WaitForAsync();
		Assert.That(await endpoint.InputValueAsync(), Is.EqualTo(added));

		var originalRow = _page.Locator(".sphere10-screen:not([hidden]) tbody > tr").Filter(new() { HasText = original });
		await originalRow.GetByRole(AriaRole.Button, new() { Name = "Select", Exact = true }).ClickAsync();
		await _page.WaitForFunctionAsync("value => document.querySelector('select[aria-label=Endpoint]').value === value", original);
		await originalRow.GetByText("Selected", new() { Exact = true }).WaitForAsync();
		await _page.GetByRole(AriaRole.Navigation, new() { Name = "Application blocks", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Component gallery", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Navigation, new() { Name = "Component gallery menus", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "CRUD grid", Exact = true }).ClickAsync();
		await WaitForInteractiveGridAsync();
		Assert.That(await endpoint.InputValueAsync(), Is.EqualTo(original));
		Assert.That(await endpoint.Locator("option").AllTextContentsAsync(), Does.Contain(added));
		await endpoint.SelectOptionAsync(added);
		await _page.GetByRole(AriaRole.Link, new() { Name = "Manage endpoints", Exact = true }).ClickAsync();
		await addedRow.GetByText("Selected", new() { Exact = true }).WaitForAsync();
		await AttachScreenshotAsync("endpoint-synchronization");
	}

	[Test]
	public async Task BlockBrowsingIsAllowedWhileScreenSwitchingHonorsUnsavedGuardAndHistory() {
		await OpenInteractiveGridAsync();
		var dock = _page.GetByRole(AriaRole.Navigation, new() { Name = "Application blocks", Exact = true });
		var components = dock.GetByRole(AriaRole.Button, new() { Name = "Component gallery", Exact = true });
		var workspace = dock.GetByRole(AriaRole.Button, new() { Name = "Workspace", Exact = true });
		await components.ClickAsync();
		await WaitForBlockAsync("components");
		await WaitForInteractiveGridAsync();
		Assert.That(new Uri(_page.Url).AbsolutePath, Is.EqualTo("/application"));
		await components.And(_page.Locator("[aria-current=page]")).WaitForAsync();
		Assert.That(await components.GetAttributeAsync("aria-current"), Is.EqualTo("page"));
		await workspace.ClickAsync();
		await _page.GetByRole(AriaRole.Navigation, new() { Name = "Workspace menus", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Overview", Exact = true }).ClickAsync();
		await WaitForBlockAsync("workspace");
		await _page.GetByRole(AriaRole.Heading, new() { Name = "ApplicationBlock workspace", Exact = true }).WaitForAsync();
		await _page.GetByRole(AriaRole.Button, new() { Name = "Increment", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Button, new() { Name = "Increment", Exact = true }).ClickAsync();
		await _page.GetByText("Counter: 2", new() { Exact = true }).WaitForAsync();
		await _page.GetByRole(AriaRole.Navigation, new() { Name = "Workspace menus", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Single-instance screen", Exact = true }).ClickAsync();
		var note = _page.GetByRole(AriaRole.Textbox, new() { Name = "Session note", Exact = true });
		await note.FillAsync("Saved draft survives history");
		await _page.GetByText("Unsaved changes — navigation is blocked.", new() { Exact = true }).WaitForAsync();
		var editorUrl = _page.Url;
		await components.ClickAsync();
		await components.And(_page.Locator("[aria-current=page]")).WaitForAsync();
		var componentMenus = _page.GetByRole(AriaRole.Navigation, new() { Name = "Component gallery menus", Exact = true });
		await componentMenus.GetByRole(AriaRole.Button, new() { Name = "CRUD grid", Exact = true }).ClickAsync();

		// Save provides a circuit round trip after the vetoed screen request.
		await _page.GetByRole(AriaRole.Tabpanel, new() { Name = "Single-instance screen", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Save in session", Exact = true }).ClickAsync();
		await _page.GetByText("No unsaved changes.", new() { Exact = true }).WaitForAsync();
		Assert.That(_page.Url, Is.EqualTo(editorUrl));
		Assert.That(await note.InputValueAsync(), Is.EqualTo("Saved draft survives history"));
		Assert.That(await components.GetAttributeAsync("aria-current"), Is.EqualTo("page"), "A blocked tab switch must not prevent browsing another block's menus.");
		await componentMenus.GetByRole(AriaRole.Button, new() { Name = "CRUD grid", Exact = true }).ClickAsync();
		await WaitForBlockAsync("components");
		await WaitForInteractiveGridAsync();
		await _page.GoBackAsync();
		await note.WaitForAsync();
		Assert.That(await note.InputValueAsync(), Is.EqualTo("Saved draft survives history"));
		await _page.GetByText("No unsaved changes.", new() { Exact = true }).WaitForAsync();
		await _page.GoForwardAsync();
		await WaitForBlockAsync("components");
		await WaitForInteractiveGridAsync();
		await _page.GoBackAsync();
		await note.WaitForAsync();
		Assert.That(await note.InputValueAsync(), Is.EqualTo("Saved draft survives history"));
		await _page.GetByRole(AriaRole.Navigation, new() { Name = "Workspace menus", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Overview", Exact = true }).ClickAsync();
		await _page.GetByText("Counter: 2", new() { Exact = true }).WaitForAsync();
		await AttachScreenshotAsync("retained-workspace");
	}

	private async Task OpenInteractiveGridAsync() {
		await _page.GotoAsync(new Uri(_origin, "/components/grid").AbsoluteUri);
		await WaitForInteractiveGridAsync();
		await WaitForBlockAsync("components");
		Assert.That(await _page.Locator(".sphere10-application").CountAsync(), Is.EqualTo(1));
		Assert.That(await _page.GetByRole(AriaRole.Complementary, new() { Name = "Application navigation", Exact = true, IncludeHidden = true }).CountAsync(), Is.EqualTo(1));
	}

	private Task WaitForInteractiveGridAsync() => _page.WaitForFunctionAsync("""
		() => {
			const grid = document.querySelector('.sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table');
			return grid && grid.offsetParent !== null && !!grid.style.width
				&& grid.querySelectorAll(':scope > tbody > tr').length === 10
				&& grid.closest('.sphere10-grid').getAttribute('aria-busy') === 'false';
		}
		""");

	private Task WaitForThemeAsync(string theme) => _page.WaitForFunctionAsync(
		"theme => document.querySelector('[data-sphere10-theme]')?.getAttribute('data-sphere10-theme') === theme", theme);

	private Task WaitForBlockAsync(string block) => _page.WaitForFunctionAsync(
		"block => location.pathname === '/application' && new URLSearchParams(location.search).get('block') === block", block);

	private async Task AttachScreenshotAsync(string phase) {
		var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "DemoShellBrowserTests");
		Tools.FileSystem.CreateDirectory(directory);
		var path = Path.Combine(directory, $"{TestContext.CurrentContext.Test.ID}-{phase}.png");
		await _page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
		TestContext.AddTestAttachment(path, phase);
	}
}