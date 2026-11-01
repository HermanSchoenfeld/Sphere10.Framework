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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>Opt-in DOM geometry checks against a running local Blazor tester.</summary>
[TestFixture]
[Explicit("Requires BlazorTesterUrl and an installed Chromium or BrowserExecutablePath.")]
[Category("Browser")]
[NonParallelizable]
public class BlazorGridBrowserTests {
	[TestCase(1280, 1000, 1.25f, 1.25)]
	[TestCase(390, 900, 1.5f, 1.0)]
	public async Task AutoPagesFitCompleteRowsAndSilentRefreshKeepsSelectionInteractive(int width, int height, float deviceScale, double zoom) {
		var url = TestContext.Parameters.Get("BlazorTesterUrl", "");
		Assert.That(Uri.TryCreate(url, UriKind.Absolute, out var origin) && origin.IsLoopback && origin.Scheme is "http" or "https", Is.True);
		var executable = TestContext.Parameters.Get("BrowserExecutablePath", "");
		using var playwright = await Playwright.CreateAsync();
		await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
			Headless = true,
			ExecutablePath = string.IsNullOrWhiteSpace(executable) ? null : executable
		});
		await using var context = await browser.NewContextAsync(new BrowserNewContextOptions {
			ViewportSize = new ViewportSize { Width = width, Height = height }, DeviceScaleFactor = deviceScale
		});
		var page = await context.NewPageAsync();
		var errors = new List<string>();
		page.PageError += (_, error) => errors.Add(error);
		await page.GotoAsync(new Uri(origin, "/components/grid").AbsoluteUri);
		await page.EvaluateAsync("zoom => document.documentElement.style.zoom = String(zoom)", zoom);
		var demo = page.Locator(".sphere10-screen:not([hidden]) [data-demo=live-grid]");
		var grid = demo.Locator(":scope > .sphere10-grid");
		var table = grid.Locator(":scope > .sphere10-grid-viewport > table");
		// SSR rows are replaced when the interactive screen starts; wait for the owned browser behavior before sampling DOM identity.
		await page.WaitForFunctionAsync("() => location.pathname === '/application' && !!document.querySelector('.sphere10-screen:not([hidden]) [data-demo=live-grid] > .sphere10-grid > .sphere10-grid-viewport > table')?.style.width");
		await Assertions.Expect(grid).ToHaveAttributeAsync("aria-busy", "false");
		await table.Locator(":scope > tbody > tr").Nth(2).WaitForAsync();
		// Automatic sizing can replace the initially requested rows while its first viewport measurement settles.
		// Observe the retained grid/table/header structure here; the checks below inspect the completed rows themselves.
		var settled = await SampleLayoutAsync(grid, sampleRows: false);
		Assert.That(settled.MaximumGeometryRange, Is.LessThanOrEqualTo(.25));
		Assert.That(await grid.EvaluateAsync<bool>("""
			root => {
				const viewport = root.querySelector('.sphere10-grid-viewport');
				const table = viewport.querySelector('table');
				const scale = viewport.getBoundingClientRect().height / viewport.offsetHeight;
				return table.tBodies[0].rows[table.tBodies[0].rows.length - 1].getBoundingClientRect().bottom
					<= viewport.getBoundingClientRect().top + viewport.clientHeight * scale + 1;
			}
			"""), Is.True, "Automatic paging must fit complete rows including browser zoom, caption/header and the horizontal scrollbar.");
		var first = table.Locator(":scope > tbody > tr").Nth(0);
		var second = table.Locator(":scope > tbody > tr").Nth(1);
		await first.Locator("td").First.ClickAsync();
		await demo.GetByRole(AriaRole.Button, new() { Name = "Publish snapshot", Exact = true }).ClickAsync();
		await second.Locator("td").First.ClickAsync();
		await Assertions.Expect(second).ToHaveAttributeAsync("aria-selected", "true");
		Assert.That(await demo.GetByRole(AriaRole.Button, new() { Name = "Publish snapshot", Exact = true }).IsDisabledAsync(), Is.True,
			"The second row must become selected while the delayed source refresh is still in progress.");
		Assert.That(await grid.Locator(".sphere10-grid-loading").CountAsync(), Is.Zero, "An actual delayed source read must not replace the interactive rows with a loading mask.");
		await Assertions.Expect(demo.Locator("[data-testid=live-grid-status]")).ToContainTextAsync("Published");
		await Assertions.Expect(second).ToHaveAttributeAsync("aria-selected", "true");
		await Assertions.Expect(demo.GetByRole(AriaRole.Button, new() { Name = "Refresh selected row", Exact = true })).ToBeEnabledAsync();
		await grid.GetByRole(AriaRole.Button, new() { Name = "Paging mode", Exact = true }).ClickAsync();
		await grid.GetByRole(AriaRole.Menuitemradio, new() { Name = "Single", Exact = true }).ClickAsync();
		await Assertions.Expect(table.Locator(":scope > tbody > tr")).ToHaveCountAsync(96);
		Assert.That(await grid.GetByRole(AriaRole.Navigation, new() { Name = "Grid pages", Exact = true }).CountAsync(), Is.Zero);
		await grid.GetByRole(AriaRole.Button, new() { Name = "Paging mode", Exact = true }).ClickAsync();
		await grid.GetByRole(AriaRole.Menuitemradio, new() { Name = "Manual", Exact = true }).ClickAsync();
		await Assertions.Expect(grid.GetByRole(AriaRole.Combobox, new() { Name = "Rows per page", Exact = true })).ToHaveValueAsync("12");
		Assert.That(errors, Is.Empty);
	}

	[TestCase(1500, 1000, 1.0f, 1.0)]
	[TestCase(760, 1000, 1.25f, 1.25)]
	public async Task NaturalAndManualWidthsSurvivePagingWhileOtherPreferredColumnsStillStretch(int width, int height, float deviceScale, double zoom) {
		var url = TestContext.Parameters.Get("BlazorTesterUrl", "");
		Assert.That(Uri.TryCreate(url, UriKind.Absolute, out var origin) && origin.IsLoopback && origin.Scheme is "http" or "https", Is.True);
		var executable = TestContext.Parameters.Get("BrowserExecutablePath", "");
		using var playwright = await Playwright.CreateAsync();
		await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
			Headless = true,
			ExecutablePath = string.IsNullOrWhiteSpace(executable) ? null : executable
		});
		await using var context = await browser.NewContextAsync(new BrowserNewContextOptions {
			ViewportSize = new ViewportSize { Width = width, Height = height },
			DeviceScaleFactor = deviceScale
		});
		var page = await context.NewPageAsync();
		var errors = new List<string>();
		page.PageError += (_, error) => errors.Add(error);
		await page.GotoAsync(new Uri(origin, "/components/grid").AbsoluteUri);
		await page.EvaluateAsync("zoom => document.documentElement.style.zoom = String(zoom)", zoom);
		var grid = page.Locator(".sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid");
		var table = grid.Locator(":scope > .sphere10-grid-viewport > table");
		await Assertions.Expect(table.Locator(":scope > tbody > tr")).ToHaveCountAsync(10);
		await page.WaitForFunctionAsync("() => !!document.querySelector('[data-demo=grid] > .sphere10-grid table')?.style.width");
		var initial = await table.Locator(":scope > colgroup > col").EvaluateAllAsync<double[]>("columns => columns.map(column => parseFloat(column.style.width))");
		await grid.GetByRole(AriaRole.Button, new() { Name = "Next page", Exact = true }).ClickAsync();
		await Assertions.Expect(grid.GetByRole(AriaRole.Spinbutton, new() { Name = "Current page", Exact = true })).ToHaveValueAsync("2");
		var paged = await table.Locator(":scope > colgroup > col").EvaluateAllAsync<double[]>("columns => columns.map(column => parseFloat(column.style.width))");
		Assert.That(paged, Is.EqualTo(initial).Within(.25), "Later-page content must not remeasure established column widths.");
		var header = table.Locator(":scope > thead > tr > th").Nth(1);
		var grip = header.Locator(".sphere10-grid-resizer");
		await grip.ScrollIntoViewIfNeededAsync();
		var handle = await grip.BoundingBoxAsync();
		await page.Mouse.MoveAsync(handle.X + handle.Width / 2, handle.Y + handle.Height / 2);
		await page.Mouse.DownAsync();
		await page.Mouse.MoveAsync(handle.X + handle.Width / 2 + 65, handle.Y + handle.Height / 2, new MouseMoveOptions { Steps = 5 });
		await page.Mouse.UpAsync();
		await page.WaitForFunctionAsync("() => Object.keys(localStorage).filter(key => key.startsWith('sphere10.settings:')).some(key => JSON.parse(localStorage.getItem(key)).values?.Grid?.Columns?.some(column => column.Key === 'Name' && column.Width > 0))");
		var manual = await table.Locator("col[data-grid-key=Name]").EvaluateAsync<double>("column => parseFloat(column.style.width)");
		await grid.GetByRole(AriaRole.Button, new() { Name = "Reload", Exact = true }).ClickAsync();
		await Assertions.Expect(grid).ToHaveAttributeAsync("aria-busy", "false");
		Assert.That(await table.Locator("col[data-grid-key=Name]").EvaluateAsync<double>("column => parseFloat(column.style.width)"), Is.EqualTo(manual).Within(.25));
		await page.SetViewportSizeAsync(Math.Max(width + 1600, (int)Math.Ceiling((initial.Sum() + 1000) * zoom)), height);
		await page.WaitForFunctionAsync("() => document.querySelector('[data-demo=grid] > .sphere10-grid table').getBoundingClientRect().width > 0");
		var expanded = await SampleLayoutAsync(grid);
		Assert.That(expanded.MaximumGeometryRange, Is.LessThanOrEqualTo(.25));
		Assert.That(await table.Locator("col[data-grid-key=Name]").EvaluateAsync<double>("column => parseFloat(column.style.width)"), Is.EqualTo(manual).Within(.25), "The explicit width overrides stretching only for the dragged column.");
		var note = await table.Locator("col[data-grid-key=Note]").EvaluateAsync<double>("column => parseFloat(column.style.width)");
		Assert.That(note, Is.GreaterThan(initial[6]), "The other preferred column must continue absorbing viewport growth.");
		await grid.GetByRole(AriaRole.Combobox, new() { Name = "Rows per page", Exact = true }).SelectOptionAsync("5");
		await Assertions.Expect(table.Locator(":scope > tbody > tr")).ToHaveCountAsync(5);
		await page.ReloadAsync();
		await Assertions.Expect(table.Locator(":scope > tbody > tr")).ToHaveCountAsync(5);
		Assert.That(await table.Locator("col[data-grid-key=Name]").EvaluateAsync<double>("column => parseFloat(column.style.width)"), Is.EqualTo(manual).Within(.25), "Only this browser's explicitly keyed grid restores the saved width.");
		Assert.That(errors, Is.Empty);
	}

	// Serialize frame sampling to avoid browser scheduling contention between geometry probes.
	[TestCase(3484, 1440, 1.25f, 1.25)]
	[TestCase(1920, 1080, 1.5f, 1.5)]
	[TestCase(760, 900, 1.25f, 1.25)]
	public async Task RowThreeEditAndReferencePopupSettleAfterFractionalScaling(int width, int height, float deviceScale, double zoom) {
		var url = TestContext.Parameters.Get("BlazorTesterUrl", "");
		Assert.That(Uri.TryCreate(url, UriKind.Absolute, out var origin) && origin.IsLoopback && origin.Scheme is "http" or "https", Is.True,
			"Set BlazorTesterUrl to the running local tester origin in a .runsettings TestRunParameters entry.");
		var executable = TestContext.Parameters.Get("BrowserExecutablePath", "");
		using var playwright = await Playwright.CreateAsync();
		await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
			Headless = true,
			IgnoreDefaultArgs = new[] { "--hide-scrollbars" },
			ExecutablePath = string.IsNullOrWhiteSpace(executable) ? null : executable
		});
		await using var context = await browser.NewContextAsync(new BrowserNewContextOptions {
			ViewportSize = new ViewportSize { Width = width, Height = height },
			DeviceScaleFactor = deviceScale
		});
		var page = await context.NewPageAsync();
		page.SetDefaultTimeout(15000);
		var errors = new List<string>();
		page.PageError += (_, error) => errors.Add(error);
		await page.GotoAsync(new Uri(origin, "/components/grid").AbsoluteUri);
		var table = page.Locator(".sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table");
		var grid = table.Locator("xpath=../..");
		// A width written by the grid module confirms interactive startup beyond the SSR markup.
		await page.WaitForFunctionAsync("() => !!document.querySelector('.sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table')?.style.width");
		await page.WaitForFunctionAsync("""
			() => document.querySelectorAll('.sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table > tbody > tr').length === 10
				&& document.querySelector('.sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table').closest('.sphere10-grid').getAttribute('aria-busy') === 'false'
			""");
		Assert.That(await table.Locator(":scope > tbody > tr").CountAsync(), Is.EqualTo(10));
		await page.WaitForFunctionAsync("() => location.pathname === '/application' && new URLSearchParams(location.search).get('block') === 'components' && new URLSearchParams(location.search).get('screen') === 'gallery'");
		Assert.That(await page.Locator(".sphere10-application").CountAsync(), Is.EqualTo(1), "The grid alias must open inside the single application shell.");
		Assert.That(await table.EvaluateAsync<bool>("element => !!element.closest('.sphere10-screen[role=tabpanel]:not([hidden])')"), Is.True);
		await page.EvaluateAsync("zoom => document.documentElement.style.zoom = String(zoom)", zoom);
		var initial = await SampleLayoutAsync(grid);
		Assert.That(initial.MaximumGeometryRange, Is.LessThanOrEqualTo(0.25), initial.Description("Initial grid"));
		Assert.That(initial.StyleWrites, Is.Zero, initial.Description("Initial grid style writes"));
		Assert.That(initial.ResizeEvents, Is.Zero, initial.Description("Initial grid resize events"));
		if (initial.ConfiguredWidth <= initial.AvailableWidth)
			Assert.That(initial.TableWidth, Is.LessThanOrEqualTo(initial.ViewportWidth + 0.1),
				"Initial grid must fit without an artificial scrollbar when configured columns fit. " + initial.Description("Fractional width allocation"));

		await table.Locator(":scope > tbody > tr").Nth(2).ClickAsync();
		await grid.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
		var editBar = grid.GetByRole(AriaRole.Group, new() { Name = "Edit item", Exact = true });
		await editBar.WaitForAsync();
		Assert.That(await table.Locator(":scope > tbody > tr").Nth(2).GetAttributeAsync("aria-selected"), Is.EqualTo("true"));
		var editing = await SampleLayoutAsync(grid);
		Assert.That(editing.MaximumGeometryRange, Is.LessThanOrEqualTo(0.25), editing.Description("Editing row three"));
		Assert.That(editing.StyleWrites, Is.Zero, editing.Description("Editing row three style writes"));
		Assert.That(editing.ResizeEvents, Is.Zero, editing.Description("Editing row three resize events"));
		if (editing.ConfiguredWidth <= editing.AvailableWidth)
			Assert.That(editing.TableWidth, Is.LessThanOrEqualTo(editing.ViewportWidth + 0.1),
				"Editing grid must fit without an artificial scrollbar when configured columns fit. " + editing.Description("Fractional width allocation"));

		await grid.GetByRole(AriaRole.Button, new() { Name = "Choose related record", Exact = true }).ClickAsync();
		var popup = page.GetByRole(AriaRole.Dialog, new() { Name = "Choose related record", Exact = true });
		await popup.WaitForAsync();
		await popup.Locator("tbody > tr").Nth(4).WaitForAsync();
		Assert.That(await popup.Locator("tbody > tr").CountAsync(), Is.EqualTo(5));
		var picking = await SampleLayoutAsync(grid);
		Assert.That(picking.GridCount, Is.EqualTo(2), "The stability probe must include the nested reference grid.");
		Assert.That(picking.MaximumGeometryRange, Is.LessThanOrEqualTo(0.25), picking.Description("Reference popup"));
		Assert.That(picking.StyleWrites, Is.Zero, picking.Description("Reference popup style writes"));
		Assert.That(picking.ResizeEvents, Is.Zero, picking.Description("Reference popup resize events"));
		var popupBounds = await popup.BoundingBoxAsync();
		Assert.That(popupBounds, Is.Not.Null);
		Assert.That(popupBounds.X, Is.GreaterThanOrEqualTo(0), "The popup must remain inside the zoomed viewport.");
		Assert.That(popupBounds.Y, Is.GreaterThanOrEqualTo(0), "The popup must remain inside the zoomed viewport.");
		Assert.That(popupBounds.X + popupBounds.Width, Is.LessThanOrEqualTo(width + 0.1), "Popup actions must remain reachable horizontally.");
		Assert.That(popupBounds.Y + popupBounds.Height, Is.LessThanOrEqualTo(height + 0.1), "Popup actions must remain reachable vertically.");
		await popup.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
		await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
		await editBar.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
		await editBar.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

		var nameHeader = table.Locator(":scope > thead > tr > th").Nth(1);
		var grip = nameHeader.Locator(".sphere10-grid-resizer");
		await grip.ScrollIntoViewIfNeededAsync();
		var before = await nameHeader.BoundingBoxAsync();
		var handle = await grip.BoundingBoxAsync();
		Assert.That(before, Is.Not.Null);
		Assert.That(handle, Is.Not.Null);
		await page.Mouse.MoveAsync(handle.X + handle.Width / 2, handle.Y + handle.Height / 2);
		await page.Mouse.DownAsync();
		await page.Mouse.MoveAsync(handle.X + handle.Width / 2 + 60, handle.Y + handle.Height / 2, new MouseMoveOptions { Steps = 8 });
		await page.Mouse.UpAsync();
		var resized = await SampleLayoutAsync(grid);
		var after = await nameHeader.BoundingBoxAsync();
		Assert.That(after.Width, Is.GreaterThan(before.Width + 10), "Dragging the resize grip must enlarge the selected column.");
		Assert.That(resized.MaximumGeometryRange, Is.LessThanOrEqualTo(0.25), resized.Description("Manually resized grid"));
		Assert.That(resized.StyleWrites, Is.Zero, resized.Description("Manual resize style writes"));
		Assert.That(resized.ResizeEvents, Is.Zero, resized.Description("Manual resize events"));
		Assert.That(errors, Is.Empty, "Browser script errors must not mask a stopped resize loop.");
	}

	[TestCase(1500, 1000)]
	[TestCase(390, 844)]
	public async Task IconPagerNavigatesAndPageSizeDropdownPreservesEditingGuards(int width, int height) {
		var url = TestContext.Parameters.Get("BlazorTesterUrl", "");
		Assert.That(Uri.TryCreate(url, UriKind.Absolute, out var origin) && origin.IsLoopback && origin.Scheme is "http" or "https", Is.True,
			"Set BlazorTesterUrl to the running local tester origin in a .runsettings TestRunParameters entry.");
		var executable = TestContext.Parameters.Get("BrowserExecutablePath", "");
		using var playwright = await Playwright.CreateAsync();
		await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
			Headless = true,
			ExecutablePath = string.IsNullOrWhiteSpace(executable) ? null : executable
		});
		await using var context = await browser.NewContextAsync(new BrowserNewContextOptions { ViewportSize = new ViewportSize { Width = width, Height = height } });
		var page = await context.NewPageAsync();
		page.SetDefaultTimeout(15000);
		var errors = new List<string>();
		page.PageError += (_, error) => errors.Add(error);
		await page.GotoAsync(new Uri(origin, "/components/grid").AbsoluteUri);
		var grid = page.Locator(".sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid");
		await page.WaitForFunctionAsync("() => !!document.querySelector('.sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table')?.style.width");
		var pager = grid.GetByRole(AriaRole.Navigation, new() { Name = "Grid pages", Exact = true });
		var pageSize = grid.GetByRole(AriaRole.Combobox, new() { Name = "Rows per page", Exact = true });
		var currentPage = pager.GetByRole(AriaRole.Spinbutton, new() { Name = "Current page", Exact = true });
		var first = pager.GetByRole(AriaRole.Button, new() { Name = "First page", Exact = true });
		var previous = pager.GetByRole(AriaRole.Button, new() { Name = "Previous page", Exact = true });
		var next = pager.GetByRole(AriaRole.Button, new() { Name = "Next page", Exact = true });
		var last = pager.GetByRole(AriaRole.Button, new() { Name = "Last page", Exact = true });
		var modeButton = grid.GetByRole(AriaRole.Button, new() { Name = "Paging mode", Exact = true });
		await modeButton.ClickAsync();
		var modes = grid.GetByRole(AriaRole.Menu, new() { Name = "Paging mode", Exact = true });
		Assert.That(await modes.GetByRole(AriaRole.Menuitemradio).CountAsync(), Is.EqualTo(3));
		await Assertions.Expect(modes.GetByRole(AriaRole.Menuitemradio, new() { Name = "Manual", Exact = true })).ToHaveAttributeAsync("aria-checked", "true");
		await modes.GetByRole(AriaRole.Menuitemradio, new() { Name = "Single", Exact = true }).ClickAsync();
		await Assertions.Expect(pager).ToBeHiddenAsync();
		await Assertions.Expect(grid.Locator(":scope > .sphere10-grid-viewport > table > tbody > tr")).ToHaveCountAsync(73);
		await modeButton.ClickAsync();
		await Assertions.Expect(modes.GetByRole(AriaRole.Menuitemradio, new() { Name = "Single", Exact = true })).ToHaveAttributeAsync("aria-checked", "true");
		await modes.GetByRole(AriaRole.Menuitemradio, new() { Name = "Auto", Exact = true }).ClickAsync();
		await Assertions.Expect(pager).ToBeVisibleAsync();
		await modeButton.ClickAsync();
		await Assertions.Expect(modes.GetByRole(AriaRole.Menuitemradio, new() { Name = "Auto", Exact = true })).ToHaveAttributeAsync("aria-checked", "true");
		var modeScreenshot = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"grid-page-mode-menu-{width}.png");
		await modes.ScreenshotAsync(new LocatorScreenshotOptions { Path = modeScreenshot });
		TestContext.AddTestAttachment(modeScreenshot, "Compact paging menu with the selected Auto marker");
		await modes.GetByRole(AriaRole.Menuitemradio, new() { Name = "Manual", Exact = true }).ClickAsync();
		Assert.That(await pager.Locator("button > svg[aria-hidden=true]").CountAsync(), Is.EqualTo(4));
		await Assertions.Expect(first).ToBeDisabledAsync();
		await Assertions.Expect(previous).ToBeDisabledAsync();
		await pageSize.SelectOptionAsync("25");
		await Assertions.Expect(grid.Locator(":scope > .sphere10-grid-viewport > table > tbody > tr")).ToHaveCountAsync(25);
		await last.ClickAsync();
		await Assertions.Expect(currentPage).ToHaveValueAsync("3");
		await Assertions.Expect(next).ToBeDisabledAsync();
		await Assertions.Expect(last).ToBeDisabledAsync();
		await Assertions.Expect(grid.Locator(":scope > .sphere10-grid-viewport > table > tbody > tr")).ToHaveCountAsync(23);
		await previous.ClickAsync();
		await Assertions.Expect(currentPage).ToHaveValueAsync("2");
		await first.ClickAsync();
		await Assertions.Expect(currentPage).ToHaveValueAsync("1");
		await next.ClickAsync();
		await Assertions.Expect(currentPage).ToHaveValueAsync("2");
		await pageSize.SelectOptionAsync("5");
		await Assertions.Expect(currentPage).ToHaveValueAsync("1");
		await Assertions.Expect(grid.Locator(":scope > .sphere10-grid-viewport > table > tbody > tr")).ToHaveCountAsync(5);
		await grid.Locator(":scope > .sphere10-grid-viewport > table > tbody > tr").First.ClickAsync();
		await grid.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
		await Assertions.Expect(pageSize).ToBeDisabledAsync();
		await Assertions.Expect(modeButton).ToBeDisabledAsync();
		await Assertions.Expect(next).ToBeDisabledAsync();
		await grid.GetByRole(AriaRole.Group, new() { Name = "Edit item", Exact = true }).GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
		await Assertions.Expect(pageSize).ToBeEnabledAsync();
		await Assertions.Expect(modeButton).ToBeEnabledAsync();
		await Assertions.Expect(pageSize).ToHaveValueAsync("5");
		await Assertions.Expect(next).ToBeEnabledAsync();
		Assert.That(await pageSize.EvaluateAsync<string>("element => getComputedStyle(element).appearance"), Is.EqualTo("auto"), "The dropdown uses the native browser arrow.");
		Assert.That(await pageSize.EvaluateAsync<string>("element => getComputedStyle(element).backgroundImage"), Is.EqualTo("none"), "Bootstrap must not add a second dropdown arrow.");
		Assert.That(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"), Is.True, "Pager groups must wrap within narrow screens.");
		var screenshot = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"grid-pager-{width}.png");
		await grid.Locator(":scope > .sphere10-grid-footer").ScreenshotAsync(new LocatorScreenshotOptions { Path = screenshot });
		TestContext.AddTestAttachment(screenshot, "Grid icon navigation and page-size dropdown");
		Assert.That(errors, Is.Empty);
	}

	[Test]
	public async Task OwnedWidthsMigrateOnceRoundTripAndOptionsPreserveTheLiveRowDraft() {
		var url = TestContext.Parameters.Get("BlazorTesterUrl", "");
		Assert.That(Uri.TryCreate(url, UriKind.Absolute, out var origin) && origin.IsLoopback, Is.True);
		var executable = TestContext.Parameters.Get("BrowserExecutablePath", "");
		using var playwright = await Playwright.CreateAsync();
		await using var browser = await playwright.Chromium.LaunchAsync(new() {
			Headless = true, ExecutablePath = string.IsNullOrWhiteSpace(executable) ? null : executable
		});
		await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 1000 } });
		await context.AddInitScriptAsync("""
			if (!localStorage.getItem('__grid-preferences-seeded')) {
				localStorage.setItem('__grid-preferences-seeded', 'true');
				localStorage.setItem('sphere10-grid:tester.editable-grid', JSON.stringify({ pageMode:0, pageSize:5, pageNumber:0, columnWidths:{Name:197} }));
			}
			""");
		var page = await context.NewPageAsync();
		page.SetDefaultTimeout(15000);
		var errors = new List<string>();
		page.PageError += (_, error) => errors.Add(error);
		await page.GotoAsync(new Uri(origin, "/components/grid").AbsoluteUri);
		var grid = page.Locator(".sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid");
		var rows = grid.Locator(":scope > .sphere10-grid-viewport > table > tbody > tr");
		var name = grid.Locator("col[data-grid-key=Name]");
		await page.WaitForFunctionAsync("() => parseFloat(document.querySelector('.sphere10-screen:not([hidden]) [data-demo=grid] col[data-grid-key=Name]')?.style.width) === 197");
		await Assertions.Expect(rows).ToHaveCountAsync(5);

		// A completed drag persists through the screen's root; its legacy browser record remains untouched.
		var grip = grid.Locator("thead th").Nth(1).Locator(".sphere10-grid-resizer");
		await grip.ScrollIntoViewIfNeededAsync();
		var handle = await grip.BoundingBoxAsync();
		await page.Mouse.MoveAsync(handle.X + handle.Width / 2, handle.Y + handle.Height / 2);
		await page.Mouse.DownAsync();
		await page.Mouse.MoveAsync(handle.X + handle.Width / 2 + 46, handle.Y + handle.Height / 2, new() { Steps = 5 });
		await page.Mouse.UpAsync();
		await page.WaitForFunctionAsync("() => Object.keys(localStorage).filter(key => key.startsWith('sphere10.settings:')).some(key => JSON.parse(localStorage.getItem(key)).values?.Grid?.Columns?.some(column => column.Key === 'Name' && column.Width > 220))");
		var manual = await name.EvaluateAsync<double>("column => parseFloat(column.style.width)");
		Assert.That(await page.EvaluateAsync<double>("() => JSON.parse(localStorage.getItem('sphere10-grid:tester.editable-grid')).columnWidths.Name"), Is.EqualTo(197));
		await page.ReloadAsync();
		await Assertions.Expect(rows).ToHaveCountAsync(5);
		await page.WaitForFunctionAsync("width => Math.abs(parseFloat(document.querySelector('.sphere10-screen:not([hidden]) [data-demo=grid] col[data-grid-key=Name]')?.style.width) - width) < .25", manual);

		// The grid's own editor uses the same root values as Screen Options, with a detached cancellation boundary.
		var settingsButton = grid.GetByRole(AriaRole.Button, new() { Name = "Settings", Exact = true });
		await settingsButton.ClickAsync();
		var localSettings = grid.GetByRole(AriaRole.Region, new() { Name = "Grid settings", Exact = true });
		var settingsScreenshot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "grid-owned-settings.png");
		await localSettings.ScreenshotAsync(new() { Path = settingsScreenshot });
		TestContext.AddTestAttachment(settingsScreenshot, "The grid editor binds the same values as Screen Options");
		await localSettings.Locator("[data-option-path='PageSize'] input").FillAsync("2");
		await localSettings.Locator("[data-option-path='PageSize'] input").PressAsync("Tab");
		await localSettings.GetByRole(AriaRole.Button, new() { Name = "Cancel settings", Exact = true }).ClickAsync();
		await Assertions.Expect(rows).ToHaveCountAsync(5);
		await settingsButton.ClickAsync();
		await localSettings.Locator("[data-option-path='PageSize'] input").FillAsync("0");
		await localSettings.Locator("[data-option-path='PageSize'] input").PressAsync("Tab");
		await localSettings.GetByRole(AriaRole.Button, new() { Name = "Apply settings", Exact = true }).ClickAsync();
		await Assertions.Expect(localSettings.GetByRole(AriaRole.Alert)).ToContainTextAsync("must be positive");
		await localSettings.Locator("[data-option-path='PageSize'] input").FillAsync("5");
		await localSettings.Locator("[data-option-path='PageSize'] input").PressAsync("Tab");
		await localSettings.Locator("[data-option-path='Columns.1.Width'] input").FillAsync("249");
		await localSettings.Locator("[data-option-path='Columns.1.Width'] input").PressAsync("Tab");
		await localSettings.GetByRole(AriaRole.Button, new() { Name = "Apply settings", Exact = true }).ClickAsync();
		await localSettings.WaitForAsync(new() { State = WaitForSelectorState.Detached });
		await page.WaitForFunctionAsync("() => parseFloat(document.querySelector('.sphere10-screen:not([hidden]) [data-demo=grid] col[data-grid-key=Name]')?.style.width) === 249");
		var sameValues = await OpenGridOptionsAsync(page);
		await Assertions.Expect(sameValues.Locator("[data-option-path='Grid.Columns.1.Width'] input")).ToHaveValueAsync("249");
		await sameValues.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
		await sameValues.WaitForAsync(new() { State = WaitForSelectorState.Detached });

		// Applying page and width preferences never commits or discards an active business-row draft.
		await rows.First.ClickAsync();
		await grid.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
		var draft = grid.GetByLabel("Name", new() { Exact = true });
		await draft.FillAsync("Unsaved grid row");
		await draft.PressAsync("Tab");
		var dialog = await OpenGridOptionsAsync(page);
		await dialog.Locator("[data-option-path='Grid.PageSize'] input").FillAsync("1");
		await dialog.Locator("[data-option-path='Grid.PageSize'] input").PressAsync("Tab");
		await dialog.Locator("[data-option-path='Grid.Columns.1.Width'] input").FillAsync("259");
		await dialog.Locator("[data-option-path='Grid.Columns.1.Width'] input").PressAsync("Tab");
		await dialog.GetByRole(AriaRole.Button, new() { Name = "Apply", Exact = true }).ClickAsync();
		await page.WaitForFunctionAsync("() => parseFloat(document.querySelector('.sphere10-screen:not([hidden]) [data-demo=grid] col[data-grid-key=Name]')?.style.width) === 259");
		Assert.That(await draft.InputValueAsync(), Is.EqualTo("Unsaved grid row"));
		Assert.That(await rows.CountAsync(), Is.EqualTo(5), "The accepted query waits until the active draft ends.");
		await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
		await dialog.WaitForAsync(new() { State = WaitForSelectorState.Detached });
		await grid.GetByRole(AriaRole.Group, new() { Name = "Edit item", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
		await Assertions.Expect(rows).ToHaveCountAsync(1);
		Assert.That(await rows.First.InnerTextAsync(), Does.Not.Contain("Unsaved grid row"));

		// Reset releases manual geometry and retains migration markers, so old values cannot return on reload.
		dialog = await OpenGridOptionsAsync(page);
		await dialog.GetByRole(AriaRole.Button, new() { Name = "Reset Defaults", Exact = true }).ClickAsync();
		await dialog.GetByRole(AriaRole.Button, new() { Name = "Apply", Exact = true }).ClickAsync();
		await Assertions.Expect(rows).ToHaveCountAsync(10);
		await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
		await dialog.WaitForAsync(new() { State = WaitForSelectorState.Detached });
		Assert.That(await page.EvaluateAsync<bool>("() => Object.keys(localStorage).filter(key => key.startsWith('sphere10.settings:')).some(key => { const record=JSON.parse(localStorage.getItem(key)); return record.values?.Grid?.PageSize===10 && record.values.Grid.Columns.every(column=>column.Width===null) && record.sectionVersions?.Grid===1 && record.sectionVersions?.LiveGrid===1; })"), Is.True);
		await page.ReloadAsync();
		await Assertions.Expect(rows).ToHaveCountAsync(10);
		await page.WaitForFunctionAsync("() => !!document.querySelector('.sphere10-screen:not([hidden]) [data-demo=grid] table')?.style.width");
		Assert.That(await name.EvaluateAsync<double>("column => parseFloat(column.style.width)"), Is.Not.EqualTo(197).Within(.25));
		Assert.That(errors, Is.Empty);
	}

	// Opens the actual application Options page while the gallery and its row editor remain mounted.
	private static async Task<ILocator> OpenGridOptionsAsync(IPage page) {
		var commands = page.GetByRole(AriaRole.Navigation, new() { Name = "Application commands", Exact = true });
		await commands.GetByRole(AriaRole.Button, new() { Name = "Options", Exact = true }).ClickAsync();
		await commands.GetByRole(AriaRole.Button, new() { Name = "Screen Options...", Exact = true }).ClickAsync();
		var dialog = page.Locator(".sphere10-options-host");
		await dialog.GetByRole(AriaRole.Navigation, new() { Name = "Screen options pages", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Visual grid", Exact = true }).ClickAsync();
		return dialog;
	}

	private static async Task<LayoutObservation> SampleLayoutAsync(ILocator grid, bool sampleRows = true) {
		var frames = await grid.EvaluateAsync<double[][]>("""
			async (root, sampleRows) => {
				const grids = [root, ...root.querySelectorAll('.sphere10-grid')];
				const targets = grids.flatMap(grid => {
					const viewport = grid.querySelector(':scope > .sphere10-grid-viewport');
					const table = viewport?.querySelector(':scope > table');
					if (!viewport || !table) throw new Error('Expected a rendered grid viewport and table.');
					return [grid, viewport, table, ...(sampleRows ? [table.tBodies[0].rows[2]] : []), ...table.tHead.rows[0].cells].filter(Boolean);
				});
				let styleWrites = 0;
				let resizeEvents = 0;
				const styles = new MutationObserver(records => styleWrites += records.length);
				styles.observe(root, { subtree: true, attributes: true, attributeFilter: ['style'] });
				const sizes = new ResizeObserver(entries => resizeEvents += entries.length);
				for (const target of targets) sizes.observe(target);
				const viewport = targets[1];
				const table = targets[2];
				const configuredWidth = parseFloat(table.style.minWidth) || Array.from(table.querySelector(':scope > colgroup').children)
					.reduce((total, column) => total + Number(column.dataset.gridWidth), 0);
				const frames = [];
				try {
					for (let frame = 0; frame < 150; frame++) {
						await new Promise(requestAnimationFrame);
						const values = [styleWrites, resizeEvents, grids.length, configuredWidth, viewport.clientWidth,
							table.getBoundingClientRect().width, viewport.getBoundingClientRect().width];
						for (const target of targets) {
							if (!target.isConnected) throw new Error('A sampled grid element was removed unexpectedly.');
							const rectangle = target.getBoundingClientRect();
							values.push(rectangle.x, rectangle.y, rectangle.width, rectangle.height,
								target.clientWidth, target.clientHeight, target.scrollWidth, target.scrollHeight);
						}
						frames.push(values);
					}
					return frames;
				} finally {
					styles.disconnect();
					sizes.disconnect();
				}
			}
			""", sampleRows).WaitAsync(TimeSpan.FromSeconds(20));
		var tail = frames.TakeLast(60).ToArray();
		var ranges = Enumerable.Range(3, tail[0].Length - 3).Select(index => tail.Max(frame => frame[index]) - tail.Min(frame => frame[index])).ToArray();
		return new LayoutObservation {
			GridCount = (int)tail[0][2],
			StyleWrites = (int)(tail[^1][0] - tail[0][0]),
			ResizeEvents = (int)(tail[^1][1] - tail[0][1]),
			MaximumGeometryRange = ranges.Max(),
			ConfiguredWidth = tail[^1][3],
			AvailableWidth = tail[^1][4],
			TableWidth = tail[^1][5],
			ViewportWidth = tail[^1][6]
		};
	}

	private sealed class LayoutObservation {
		public int GridCount { get; init; }

		public int StyleWrites { get; init; }

		public int ResizeEvents { get; init; }

		public double MaximumGeometryRange { get; init; }

		public double ConfiguredWidth { get; init; }

		public double AvailableWidth { get; init; }

		public double TableWidth { get; init; }

		public double ViewportWidth { get; init; }

		public string Description(string phase) => $"{phase}: over the final 60 of 150 frames, maximum geometry range was {MaximumGeometryRange:F4}px, " +
			$"with {StyleWrites} style writes and {ResizeEvents} resize notifications across {GridCount} grids; table {TableWidth:F4}px, viewport {ViewportWidth:F4}px.";
	}
}
