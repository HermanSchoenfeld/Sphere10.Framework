// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
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

	private static async Task<LayoutObservation> SampleLayoutAsync(ILocator grid) {
		var frames = await grid.EvaluateAsync<double[][]>("""
			async root => {
				const grids = [root, ...root.querySelectorAll('.sphere10-grid')];
				const targets = grids.flatMap(grid => {
					const viewport = grid.querySelector(':scope > .sphere10-grid-viewport');
					const table = viewport?.querySelector(':scope > table');
					if (!viewport || !table) throw new Error('Expected a rendered grid viewport and table.');
					return [grid, viewport, table, table.tBodies[0].rows[2], ...table.tHead.rows[0].cells].filter(Boolean);
				});
				let styleWrites = 0;
				let resizeEvents = 0;
				const styles = new MutationObserver(records => styleWrites += records.length);
				styles.observe(root, { subtree: true, attributes: true, attributeFilter: ['style'] });
				const sizes = new ResizeObserver(entries => resizeEvents += entries.length);
				for (const target of targets) sizes.observe(target);
				const viewport = targets[1];
				const table = targets[2];
				const configuredWidth = Array.from(table.querySelector(':scope > colgroup').children)
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
			""").WaitAsync(TimeSpan.FromSeconds(20));
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
