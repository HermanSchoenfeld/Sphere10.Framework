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

[TestFixture]
[Explicit("Requires BlazorTesterUrl and installed Chromium or BrowserExecutablePath.")]
[Category("Browser")]
[NonParallelizable]
public class ApplicationTabsBrowserTests {
	private readonly List<string> _errors = new();
	private IPlaywright _playwright;
	private IBrowser _browser;
	private IPage _page;

	[SetUp]
	public async Task SetUp() {
		var url = TestContext.Parameters.Get("BlazorTesterUrl", "");
		Assert.That(Uri.TryCreate(url, UriKind.Absolute, out var origin) && origin.IsLoopback, Is.True);
		var executable = TestContext.Parameters.Get("BrowserExecutablePath", "");
		_errors.Clear();
		_playwright = await Playwright.CreateAsync();
		_browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = string.IsNullOrWhiteSpace(executable) ? null : executable });
		_page = await _browser.NewPageAsync(new() { ViewportSize = new() { Width = 1500, Height = 1000 } });
		_page.SetDefaultTimeout(15000);
		_page.PageError += (_, message) => _errors.Add(message);
		_page.Console += (_, message) => {
			if (message.Type == "error")
				_errors.Add(message.Text);
		};
		await _page.GotoAsync(new Uri(origin, "/components/grid").AbsoluteUri);
		await _page.WaitForFunctionAsync("() => !!document.querySelector('.sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table')?.style.width && document.querySelector('.sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table')?.closest('.sphere10-grid').getAttribute('aria-busy') === 'false'");
		await _page.GetByRole(AriaRole.Navigation, new() { Name = "Application blocks", Exact = true }).GetByRole(AriaRole.Button, new() { Name = "Workspace", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Navigation, new() { Name = "Workspace menus", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Overview", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Tab, new() { Name = "Overview", Exact = true }).WaitForAsync();
		await _page.GetByRole(AriaRole.Button, new() { Name = "Close CRUD grid", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Tab, new() { Name = "CRUD grid", Exact = true }).WaitForAsync(new() { State = WaitForSelectorState.Detached });
	}

	[TearDown]
	public async Task TearDown() {
		using var runtime = Tools.Scope.ExecuteOnDispose(() => _playwright?.Dispose());
		await using var browser = new TaskScope(async () => {
			if (_browser != null)
				await _browser.DisposeAsync();
		});
		if (_page != null && TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed) {
			var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"tabs-{TestContext.CurrentContext.Test.ID}.png");
			await _page.ScreenshotAsync(new() { Path = path, FullPage = true });
			TestContext.AddTestAttachment(path);
		}
		Assert.That(_errors, Is.Empty);
	}

	[Test]
	public async Task CommandsMergeTabsRetainStateAndBatchCloseHonorsGuard() {
		var toolbar = _page.GetByRole(AriaRole.Toolbar, new() { Name = "Application toolbar" });
		var workspaceMenu = _page.GetByRole(AriaRole.Navigation, new() { Name = "Workspace menus", Exact = true });
		await _page.GetByRole(AriaRole.Button, new() { Name = "Increment", Exact = true }).ClickAsync();
		await _page.GetByText("Counter: 1", new() { Exact = true }).WaitForAsync();
		await workspaceMenu.GetByRole(AriaRole.Button, new() { Name = "Single-instance screen", Exact = true }).ClickAsync();
		await toolbar.GetByRole(AriaRole.Button, new() { Name = "Save in session", Exact = true }).WaitForAsync();
		Assert.That(await toolbar.GetByRole(AriaRole.Button, new() { Name = "Application command", Exact = true }).CountAsync(), Is.Zero);
		Assert.That(await toolbar.GetByRole(AriaRole.Button, new() { Name = "Workspace action", Exact = true }).CountAsync(), Is.EqualTo(1));
		await _page.GetByRole(AriaRole.Textbox, new() { Name = "Session note" }).FillAsync("Draft retained by tab guard");
		await _page.GetByRole(AriaRole.Tab, new() { Name = "Overview", Exact = true }).ClickAsync();
		await _page.GetByText("Unsaved changes — navigation is blocked.", new() { Exact = true }).WaitForAsync();
		Assert.That(await _page.GetByRole(AriaRole.Tab, new() { Name = "Single-instance screen" }).GetAttributeAsync("aria-selected"), Is.EqualTo("true"));
		await _page.GetByRole(AriaRole.Tab, new() { Name = "Single-instance screen" }).ClickAsync(new() { Button = Microsoft.Playwright.MouseButton.Right });
		await _page.GetByRole(AriaRole.Group, new() { Name = "Tab commands" }).GetByRole(AriaRole.Button, new() { Name = "Close all screens", Exact = true }).ClickAsync();
		Assert.That(await _page.GetByRole(AriaRole.Tab).CountAsync(), Is.GreaterThanOrEqualTo(2));
		var commands = _page.GetByRole(AriaRole.Navigation, new() { Name = "Application commands", Exact = true });
		Assert.That(await commands.Locator(".sphere10-menu-button").Last.InnerTextAsync(), Is.EqualTo("Help"));
		await commands.GetByRole(AriaRole.Button, new() { Name = "File", Exact = true }).ClickAsync();
		var file = commands.GetByRole(AriaRole.Group, new() { Name = "File", Exact = true });
		Assert.That(await file.GetByRole(AriaRole.Button, new() { Name = "Application command", Exact = true }).CountAsync(), Is.Zero);
		await file.GetByRole(AriaRole.Button, new() { Name = "Save in session", Exact = true }).ClickAsync();
		await file.WaitForAsync(new() { State = WaitForSelectorState.Detached });
		await _page.GetByRole(AriaRole.Tab, new() { Name = "Overview", Exact = true }).ClickAsync();
		await _page.GetByText("Counter: 1", new() { Exact = true }).WaitForAsync();
		await toolbar.GetByRole(AriaRole.Button, new() { Name = "Application command", Exact = true }).WaitForAsync();
		Assert.That(await toolbar.GetByRole(AriaRole.Button, new() { Name = "Save in session", Exact = true }).CountAsync(), Is.Zero);
		await commands.GetByRole(AriaRole.Button, new() { Name = "File", Exact = true }).ClickAsync();
		await file.GetByRole(AriaRole.Button, new() { Name = "Application command", Exact = true }).ClickAsync();
		await file.WaitForAsync(new() { State = WaitForSelectorState.Detached });
		await _page.GetByText("Scoped async action executions: 1", new() { Exact = true }).WaitForAsync();
		await commands.GetByRole(AriaRole.Button, new() { Name = "Help", Exact = true }).ClickAsync();
		await commands.GetByRole(AriaRole.Button, new() { Name = "Help", Exact = true }).PressAsync("Escape");
		await commands.GetByRole(AriaRole.Group, new() { Name = "Help", Exact = true }).WaitForAsync(new() { State = WaitForSelectorState.Detached });
		await commands.GetByRole(AriaRole.Button, new() { Name = "Help", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Button, new() { Name = "Dismiss application menu", Exact = true }).ClickAsync(new() { Position = new() { X = 5, Y = 5 } });
		await commands.GetByRole(AriaRole.Group, new() { Name = "Help", Exact = true }).WaitForAsync(new() { State = WaitForSelectorState.Detached });
		await _page.GoBackAsync();
		await _page.GetByRole(AriaRole.Textbox, new() { Name = "Session note" }).WaitForAsync();
		Assert.That(await _page.GetByRole(AriaRole.Textbox, new() { Name = "Session note" }).InputValueAsync(), Is.EqualTo("Draft retained by tab guard"));
	}

	[Test]
	public async Task ReorderRenameAndCloseOperateOnTheSameScreenInstance() {
		var toolbar = _page.GetByRole(AriaRole.Toolbar, new() { Name = "Application toolbar" });
		await toolbar.GetByRole(AriaRole.Button, new() { Name = "New scratchpad", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Textbox, new() { Name = "Tab title", Exact = true }).FillAsync("Retained document with a long readable title");
		await _page.GetByRole(AriaRole.Textbox, new() { Name = "Notes", Exact = true }).FillAsync("Independent content");
		var tab = _page.GetByRole(AriaRole.Tab, new() { Name = "Retained document with a long readable title", Exact = true });
		var panel = await tab.GetAttributeAsync("aria-controls");
		await tab.FocusAsync();
		await tab.PressAsync("Control+Shift+Home");
		await _page.WaitForFunctionAsync("panel => document.querySelector('[role=tab]')?.getAttribute('aria-controls') === panel", panel);
		Assert.That(await _page.GetByRole(AriaRole.Tab).First.GetAttributeAsync("aria-controls"), Is.EqualTo(panel));
		var overview = _page.GetByRole(AriaRole.Tab, new() { Name = "Overview", Exact = true });
		await tab.DragToAsync(overview);
		await _page.WaitForFunctionAsync("panel => [...document.querySelectorAll('[role=tab]')].at(-1)?.getAttribute('aria-controls') === panel", panel);
		Assert.That(await _page.GetByRole(AriaRole.Tab).Last.GetAttributeAsync("aria-controls"), Is.EqualTo(panel));
		await overview.ClickAsync();
		await tab.ClickAsync();
		Assert.That(await _page.GetByRole(AriaRole.Textbox, new() { Name = "Notes", Exact = true }).InputValueAsync(), Is.EqualTo("Independent content"));
		await tab.ClickAsync(new() { Button = Microsoft.Playwright.MouseButton.Middle });
		await tab.WaitForAsync(new() { State = WaitForSelectorState.Detached });
		await _page.GetByRole(AriaRole.Heading, new() { Name = "ApplicationBlock workspace", Exact = true }).WaitForAsync();
	}

	[Test]
	public async Task KeyboardSelectionFocusFollowsArrowAndBoundaryKeysAndRespectsGuard() {
		var menu = _page.GetByRole(AriaRole.Navigation, new() { Name = "Workspace menus", Exact = true });
		await menu.GetByRole(AriaRole.Button, new() { Name = "Single-instance screen", Exact = true }).ClickAsync();
		var editor = _page.GetByRole(AriaRole.Tab, new() { Name = "Single-instance screen", Exact = true });
		var overview = _page.GetByRole(AriaRole.Tab, new() { Name = "Overview", Exact = true });
		await editor.FocusAsync();
		await editor.PressAsync("ArrowLeft");
		await AssertSelectedAndFocusedAsync(overview);
		await overview.PressAsync("ArrowRight");
		await AssertSelectedAndFocusedAsync(editor);
		await editor.PressAsync("Home");
		await AssertSelectedAndFocusedAsync(overview);
		await overview.PressAsync("End");
		await AssertSelectedAndFocusedAsync(editor);
		await _page.GetByRole(AriaRole.Textbox, new() { Name = "Session note" }).FillAsync("Guarded keyboard draft");
		await editor.FocusAsync();
		await editor.PressAsync("ArrowLeft");
		await AssertSelectedAndFocusedAsync(editor);
		await editor.PressAsync("Home");
		await AssertSelectedAndFocusedAsync(editor);
	}

	[Test]
	public async Task SingleInstanceMenuReusesTheSameSessionAndRetainsItsLocalState() {
		var menu = _page.GetByRole(AriaRole.Navigation, new() { Name = "Workspace menus", Exact = true });
		await menu.GetByRole(AriaRole.Button, new() { Name = "Single-instance screen", Exact = true }).ClickAsync();
		var panel = _page.GetByRole(AriaRole.Tabpanel, new() { Name = "Single-instance screen", Exact = true });
		await panel.WaitForAsync();
		var instance = await panel.Locator("[data-screen-instance]").InnerTextAsync();
		var panelId = await panel.GetAttributeAsync("id");
		Assert.That(await panel.Locator("[data-screen-activation-mode]").InnerTextAsync(), Does.Contain("SingleInstance"));
		await panel.GetByRole(AriaRole.Textbox, new() { Name = "Tab title", Exact = true }).FillAsync("Retained singleton");
		panel = _page.Locator("#" + panelId);
		await panel.GetByRole(AriaRole.Button, new() { Name = "Increment instance counter", Exact = true }).ClickAsync();
		await panel.GetByText("Instance counter: 1", new() { Exact = true }).WaitForAsync();
		await menu.GetByRole(AriaRole.Button, new() { Name = "Overview", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Heading, new() { Name = "ApplicationBlock workspace", Exact = true }).WaitForAsync();
		await menu.GetByRole(AriaRole.Button, new() { Name = "Single-instance screen", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Tab, new() { Name = "Retained singleton", Exact = true }).And(_page.Locator("[aria-selected=true]")).WaitForAsync();
		Assert.That(await panel.Locator("[data-screen-instance]").InnerTextAsync(), Is.EqualTo(instance));
		Assert.That(await panel.Locator("[data-screen-counter]").InnerTextAsync(), Is.EqualTo("Instance counter: 1"));
		Assert.That(await _page.GetByRole(AriaRole.Tab).CountAsync(), Is.EqualTo(2), "Reopening the singleton must reuse its existing tab beside Overview.");
	}

	[Test]
	public async Task MultiInstanceScreensHaveIndependentStateAndSwitchLocksWhileBlockBrowsingStaysAvailable() {
		var menu = _page.GetByRole(AriaRole.Navigation, new() { Name = "Workspace menus", Exact = true });
		await menu.GetByRole(AriaRole.Button, new() { Name = "New screen instance", Exact = true }).ClickAsync();
		var first = _page.GetByRole(AriaRole.Tabpanel, new() { Name = "New scratchpad", Exact = true });
		await first.WaitForAsync();
		var firstId = await first.GetAttributeAsync("id");
		var firstInstance = await first.Locator("[data-screen-instance]").InnerTextAsync();
		await first.GetByRole(AriaRole.Textbox, new() { Name = "Tab title", Exact = true }).FillAsync("Instance one");
		first = _page.Locator("#" + firstId);
		await first.GetByRole(AriaRole.Textbox, new() { Name = "Notes", Exact = true }).FillAsync("First instance notes");
		await first.GetByRole(AriaRole.Button, new() { Name = "Increment instance counter", Exact = true }).ClickAsync();
		await first.GetByText("Instance counter: 1", new() { Exact = true }).WaitForAsync();
		await menu.GetByRole(AriaRole.Button, new() { Name = "New screen instance", Exact = true }).ClickAsync();
		var second = _page.GetByRole(AriaRole.Tabpanel, new() { Name = "New scratchpad", Exact = true });
		await second.WaitForAsync();
		var secondId = await second.GetAttributeAsync("id");
		Assert.That(secondId, Is.Not.EqualTo(firstId));
		Assert.That(await second.Locator("[data-screen-instance]").InnerTextAsync(), Is.Not.EqualTo(firstInstance));
		Assert.That(await second.Locator("[data-screen-activation-mode]").InnerTextAsync(), Does.Contain("MultiInstance"));
		Assert.That(await second.GetByRole(AriaRole.Textbox, new() { Name = "Notes", Exact = true }).InputValueAsync(), Is.Empty);
		Assert.That(await second.Locator("[data-screen-counter]").InnerTextAsync(), Is.EqualTo("Instance counter: 0"));
		await second.GetByRole(AriaRole.Textbox, new() { Name = "Tab title", Exact = true }).FillAsync("Instance two");
		second = _page.Locator("#" + secondId);
		await second.GetByRole(AriaRole.Checkbox, new() { Name = "Prevent tab switching", Exact = true }).CheckAsync();
		await second.GetByText("Tab switching is prevented. Clear the checkbox to switch or close this screen.", new() { Exact = true }).WaitForAsync();
		var lockedUrl = _page.Url;
		var dock = _page.GetByRole(AriaRole.Navigation, new() { Name = "Application blocks", Exact = true });
		await dock.GetByRole(AriaRole.Button, new() { Name = "Component gallery", Exact = true }).ClickAsync();
		var components = _page.GetByRole(AriaRole.Navigation, new() { Name = "Component gallery menus", Exact = true });
		await components.WaitForAsync();
		Assert.That(_page.Url, Is.EqualTo(lockedUrl));
		Assert.That(await second.IsVisibleAsync(), Is.True);
		await components.GetByRole(AriaRole.Button, new() { Name = "CRUD grid", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Tab, new() { Name = "Instance one", Exact = true }).ClickAsync();
		await second.GetByRole(AriaRole.Button, new() { Name = "Increment instance counter", Exact = true }).ClickAsync();
		await second.GetByText("Instance counter: 1", new() { Exact = true }).WaitForAsync();
		Assert.That(_page.Url, Is.EqualTo(lockedUrl), "Both the menu request and tab request must be vetoed by the active instance.");
		Assert.That(await _page.GetByRole(AriaRole.Tab, new() { Name = "Instance two", Exact = true }).GetAttributeAsync("aria-selected"), Is.EqualTo("true"));
		Assert.That(await _page.GetByRole(AriaRole.Tab, new() { Name = "CRUD grid", Exact = true }).CountAsync(), Is.Zero);
		await second.GetByRole(AriaRole.Checkbox, new() { Name = "Prevent tab switching", Exact = true }).UncheckAsync();
		await second.GetByText("Tab switching is allowed.", new() { Exact = true }).WaitForAsync();
		await _page.GetByRole(AriaRole.Tab, new() { Name = "Instance one", Exact = true }).ClickAsync();
		await first.WaitForAsync();
		Assert.That(await first.GetByRole(AriaRole.Checkbox, new() { Name = "Prevent tab switching", Exact = true }).IsCheckedAsync(), Is.False);
		Assert.That(await first.GetByRole(AriaRole.Textbox, new() { Name = "Notes", Exact = true }).InputValueAsync(), Is.EqualTo("First instance notes"));
		Assert.That(await first.Locator("[data-screen-counter]").InnerTextAsync(), Is.EqualTo("Instance counter: 1"));
		await _page.GetByRole(AriaRole.Tab, new() { Name = "Instance two", Exact = true }).ClickAsync();
		await second.WaitForAsync();
		Assert.That(await second.GetByRole(AriaRole.Checkbox, new() { Name = "Prevent tab switching", Exact = true }).IsCheckedAsync(), Is.False);
		Assert.That(await second.GetByRole(AriaRole.Textbox, new() { Name = "Notes", Exact = true }).InputValueAsync(), Is.Empty);
		var screenshot = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"tabs-{TestContext.CurrentContext.Test.ID}-multi-instance.png");
		await _page.ScreenshotAsync(new() { Path = screenshot, FullPage = true });
		TestContext.AddTestAttachment(screenshot, "Independent screen instances with Instance two active");
		await dock.GetByRole(AriaRole.Button, new() { Name = "Component gallery", Exact = true }).ClickAsync();
		await components.GetByRole(AriaRole.Button, new() { Name = "CRUD grid", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Tab, new() { Name = "CRUD grid", Exact = true }).And(_page.Locator("[aria-selected=true]")).WaitForAsync();
		Assert.That(_page.Url, Is.Not.EqualTo(lockedUrl), "Clearing the lock permits menu navigation as well as tab selection.");
	}

	[Test]
	public async Task SingleViewRetainsSingletonAndReleasesMultiInstance() {
		var commands = _page.GetByRole(AriaRole.Navigation, new() { Name = "Application commands", Exact = true });
		var menu = _page.GetByRole(AriaRole.Navigation, new() { Name = "Workspace menus", Exact = true });
		await _page.GetByRole(AriaRole.Button, new() { Name = "Increment", Exact = true }).ClickAsync();
		await _page.GetByText("Counter: 1", new() { Exact = true }).WaitForAsync();
		await commands.GetByRole(AriaRole.Button, new() { Name = "View", Exact = true }).ClickAsync();
		await commands.GetByRole(AriaRole.Button, new() { Name = "Single screen", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Tablist).WaitForAsync(new() { State = WaitForSelectorState.Detached });
		await menu.GetByRole(AriaRole.Button, new() { Name = "New screen instance", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Textbox, new() { Name = "Notes", Exact = true }).FillAsync("Released multi-instance draft");
		await menu.GetByRole(AriaRole.Button, new() { Name = "Overview", Exact = true }).ClickAsync();
		await _page.GetByText("Counter: 1", new() { Exact = true }).WaitForAsync();
		await menu.GetByRole(AriaRole.Button, new() { Name = "New screen instance", Exact = true }).ClickAsync();
		Assert.That(await _page.GetByRole(AriaRole.Textbox, new() { Name = "Notes", Exact = true }).InputValueAsync(), Is.Empty);
		await commands.GetByRole(AriaRole.Button, new() { Name = "View", Exact = true }).ClickAsync();
		await commands.GetByRole(AriaRole.Button, new() { Name = "Tabbed screens", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Tab, new() { Name = "New scratchpad", Exact = true }).WaitForAsync();
		Assert.That(await _page.GetByRole(AriaRole.Tab).CountAsync(), Is.EqualTo(1));
	}

	private async Task AssertSelectedAndFocusedAsync(ILocator tab) {
		var id = await tab.GetAttributeAsync("id");
		await _page.WaitForFunctionAsync("id => document.activeElement?.id === id && document.getElementById(id)?.getAttribute('aria-selected') === 'true'", id);
	}
}
