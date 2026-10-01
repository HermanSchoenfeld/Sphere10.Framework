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
		var sidebar = _page.GetByRole(AriaRole.Complementary, new() { Name = "Demo navigation", Exact = true });
		var dock = _page.Locator(".demo-block-dock");
		Assert.That(await theme.InputValueAsync(), Is.EqualTo("classic-blue"));
		Assert.That(await theme.Locator("option:checked").InnerTextAsync(), Is.EqualTo("Classic blue"));
		Assert.That(await endpoint.IsVisibleAsync(), Is.True);
		Assert.That(await sidebar.EvaluateAsync<string>("element => getComputedStyle(element).backgroundImage"), Does.Contain("linear-gradient"));
		Assert.That(await _page.Locator(".demo-header").EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor"), Is.EqualTo("rgb(255, 255, 255)"));
		Assert.That(await dock.GetByRole(AriaRole.Link, new() { Name = "Workspace", Exact = true }).Locator("img").GetAttributeAsync("src"),
			Is.EqualTo("img/heading-solid.svg"));
		Assert.That(await dock.GetByRole(AriaRole.Link, new() { Name = "Component gallery", Exact = true }).Locator("img").GetAttributeAsync("src"),
			Is.EqualTo("img/boxes-solid.svg"));
		var endpointBounds = await endpoint.BoundingBoxAsync();
		var dockBounds = await dock.BoundingBoxAsync();
		Assert.That(endpointBounds, Is.Not.Null);
		Assert.That(dockBounds, Is.Not.Null);
		Assert.That(endpointBounds.X, Is.LessThan(width / 2), "Endpoint selection belongs in the top-left shell region.");
		if (width > 850) {
			Assert.That(endpointBounds.Y, Is.LessThan(180));
			Assert.That(dockBounds.Y, Is.GreaterThan(height / 2), "Block icons remain docked beneath sidebar navigation.");
			Assert.That(dockBounds.Y + dockBounds.Height, Is.LessThanOrEqualTo(height + 1));
		} else {
			var menu = _page.GetByRole(AriaRole.Button, new() { Name = "Menu", Exact = true });
			Assert.That(await _page.Locator("#demo-navigation").IsVisibleAsync(), Is.False);
			await menu.ClickAsync();
			await _page.Locator("#demo-navigation").WaitForAsync();
			Assert.That((await menu.GetAttributeAsync("aria-expanded")).ToLowerInvariant(), Is.EqualTo("true"));
			await AttachScreenshotAsync("mobile-navigation");
			await menu.ClickAsync();
			await _page.Locator("#demo-navigation").WaitForAsync(new() { State = WaitForSelectorState.Hidden });
		}
		var documentSize = await _page.EvaluateAsync<double[]>("() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
		Assert.That(documentSize[0], Is.LessThanOrEqualTo(documentSize[1] + 1), "The shell must contain the grid's intentional horizontal scrolling.");
		await AttachScreenshotAsync("classic-blue");

		var grid = _page.Locator("#TestGrid").Locator("xpath=../..");
		await _page.Locator("#TestGrid > tbody > tr").First.ClickAsync();
		await grid.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
		var name = grid.GetByRole(AriaRole.Textbox, new() { Name = "Name", Exact = true });
		await name.FillAsync("Draft retained across themes");
		await name.PressAsync("Tab");
		await theme.SelectOptionAsync("blue");
		await WaitForThemeAsync("blue");
		Assert.That(await theme.Locator("option:checked").InnerTextAsync(), Is.EqualTo("Blue"));
		Assert.That(await _page.Locator(".demo-header").EvaluateAsync<string>("element => getComputedStyle(element).backgroundImage"), Does.Contain("linear-gradient"));
		Assert.That(await _page.Locator(".sphere10-theme").EvaluateAsync<string>("element => getComputedStyle(element).fontFamily"), Does.Contain("system-ui"));
		Assert.That(await name.InputValueAsync(), Is.EqualTo("Draft retained across themes"));
		await AttachScreenshotAsync("blue-draft");
		await theme.SelectOptionAsync("classic-blue");
		await WaitForThemeAsync("classic-blue");
		Assert.That(await _page.Locator(".demo-header").EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor"), Is.EqualTo("rgb(255, 255, 255)"));
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
		Assert.That(new Uri(_page.Url).AbsolutePath, Is.EqualTo("/components/dialogs"));
		Assert.That(await theme.InputValueAsync(), Is.EqualTo("light"));
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
		var addedRow = _page.Locator(".demo-main tbody > tr").Filter(new() { HasText = added });
		await addedRow.GetByText("Selected", new() { Exact = true }).WaitForAsync();
		Assert.That(await endpoint.InputValueAsync(), Is.EqualTo(added));

		var originalRow = _page.Locator(".demo-main tbody > tr").Filter(new() { HasText = original });
		await originalRow.GetByRole(AriaRole.Button, new() { Name = "Select", Exact = true }).ClickAsync();
		await _page.WaitForFunctionAsync("value => document.querySelector('select[aria-label=Endpoint]').value === value", original);
		await originalRow.GetByText("Selected", new() { Exact = true }).WaitForAsync();
		await _page.GetByRole(AriaRole.Link, new() { Name = "CRUD grid", Exact = true }).ClickAsync();
		await WaitForInteractiveGridAsync();
		Assert.That(await endpoint.InputValueAsync(), Is.EqualTo(original));
		Assert.That(await endpoint.Locator("option").AllTextContentsAsync(), Does.Contain(added));
		await endpoint.SelectOptionAsync(added);
		await _page.GetByRole(AriaRole.Link, new() { Name = "Manage endpoints", Exact = true }).ClickAsync();
		await addedRow.GetByText("Selected", new() { Exact = true }).WaitForAsync();
		await AttachScreenshotAsync("endpoint-synchronization");
	}

	[Test]
	public async Task BlockDockRespectsUnsavedGuardAndBrowserHistoryRetainsScreenState() {
		await OpenInteractiveGridAsync();
		var dock = _page.Locator(".demo-block-dock");
		var components = dock.GetByRole(AriaRole.Link, new() { Name = "Component gallery", Exact = true });
		var workspace = dock.GetByRole(AriaRole.Link, new() { Name = "Workspace", Exact = true });
		await components.ClickAsync();
		await WaitForBlockAsync("components");
		await WaitForInteractiveGridAsync();
		Assert.That(new Uri(_page.Url).AbsolutePath, Is.EqualTo("/application"));
		await components.And(_page.Locator("[aria-current=page]")).WaitForAsync();
		Assert.That(await components.GetAttributeAsync("aria-current"), Is.EqualTo("page"));
		await workspace.ClickAsync();
		await WaitForBlockAsync("workspace");
		await _page.GetByRole(AriaRole.Heading, new() { Name = "ApplicationBlock workspace", Exact = true }).WaitForAsync();
		await _page.GetByRole(AriaRole.Button, new() { Name = "Increment", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Button, new() { Name = "Increment", Exact = true }).ClickAsync();
		await _page.GetByText("Counter: 2", new() { Exact = true }).WaitForAsync();
		await _page.GetByRole(AriaRole.Navigation, new() { Name = "Workspace menus", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Guarded editor", Exact = true }).ClickAsync();
		var note = _page.GetByRole(AriaRole.Textbox, new() { Name = "Session note", Exact = true });
		await note.FillAsync("Saved draft survives history");
		await _page.GetByText("Unsaved changes — navigation is blocked.", new() { Exact = true }).WaitForAsync();
		await components.ClickAsync();

		// Saving is a circuit round trip after the blocked dock request; it can complete only in the retained editor.
		await _page.GetByRole(AriaRole.Button, new() { Name = "Save in session", Exact = true }).ClickAsync();
		await _page.GetByText("No unsaved changes.", new() { Exact = true }).WaitForAsync();
		await WaitForBlockAsync("workspace");
		Assert.That(await note.InputValueAsync(), Is.EqualTo("Saved draft survives history"));
		await workspace.And(_page.Locator("[aria-current=page]")).WaitForAsync();
		Assert.That(await workspace.GetAttributeAsync("aria-current"), Is.EqualTo("page"));
		await components.ClickAsync();
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
	}

	private Task WaitForInteractiveGridAsync() => _page.WaitForFunctionAsync("""
		() => {
			const grid = document.querySelector('#TestGrid');
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