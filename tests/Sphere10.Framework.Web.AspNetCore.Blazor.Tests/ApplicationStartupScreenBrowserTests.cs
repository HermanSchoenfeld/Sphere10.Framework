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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Explicit("Requires BlazorTesterUrl and an installed Chromium or BrowserExecutablePath.")]
[Category("Browser")]
[NonParallelizable]
public class ApplicationStartupScreenBrowserTests {
	[Test]
	public async Task SavedChoiceReopensFromRootWhileExplicitBookmarksAndCorruptPreferencesKeepTheirOwnFallbacks() {
		var url = TestContext.Parameters.Get("BlazorTesterUrl", "");
		Assert.That(Uri.TryCreate(url, UriKind.Absolute, out var origin) && origin.IsLoopback, Is.True);
		var executable = TestContext.Parameters.Get("BrowserExecutablePath", "");
		using var playwright = await Playwright.CreateAsync();
		await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = string.IsNullOrWhiteSpace(executable) ? null : executable });
		await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1400, Height = 950 } });
		await context.AddInitScriptAsync("""
			window.startupOverviewWasRendered = false;
			new MutationObserver(() => {
				if (Array.from(document.querySelectorAll('.sphere10-screen h1')).some(node => node.textContent === 'ApplicationBlock workspace'))
					window.startupOverviewWasRendered = true;
			}).observe(document, { childList: true, subtree: true });
			""");
		var page = await context.NewPageAsync();
		var errors = new List<string>();
		page.PageError += (_, message) => errors.Add(message);
		page.Console += (_, message) => {
			if (message.Type == "error")
				errors.Add(message.Text);
		};
		page.SetDefaultTimeout(15000);
		await page.GotoAsync(new Uri(origin, "/components/grid").AbsoluteUri);
		await page.WaitForFunctionAsync("""
			() => document.querySelector('[data-plugin-initializer-count]')?.textContent.includes('Plugins initialized: 4')
				&& !!document.querySelector('.sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table')?.style.width
			""");
		var gridTab = page.GetByRole(AriaRole.Tab, new() { Name = "CRUD grid", Exact = true });
		await Assertions.Expect(gridTab).ToHaveAttributeAsync("aria-selected", "true");
		await gridTab.ClickAsync(new() { Button = Microsoft.Playwright.MouseButton.Right });
		var startup = page.GetByRole(AriaRole.Menuitemcheckbox, new() { NameRegex = new Regex("Make Startup Screen$") });
		await Assertions.Expect(startup).ToBeEnabledAsync();
		await startup.ClickAsync();
		await page.WaitForFunctionAsync("() => !!localStorage.getItem('sphere10.startup-screen.Sphere10.BlazorTester.Workspace')");
		await gridTab.ClickAsync(new() { Button = Microsoft.Playwright.MouseButton.Right });
		await Assertions.Expect(startup).ToHaveAttributeAsync("aria-checked", "true");
		await page.GetByRole(AriaRole.Button, new() { Name = "Dismiss tab commands", Exact = true }).ClickAsync();

		await page.GotoAsync(new Uri(origin, "/").AbsoluteUri);
		await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "CRUD grid", Exact = true })).ToHaveAttributeAsync("aria-selected", "true");
		await page.Locator(".sphere10-screen:not([hidden]) [data-demo=grid]").WaitForAsync();
		Assert.That(await page.EvaluateAsync<bool>("() => window.startupOverviewWasRendered"), Is.False,
			"Saved startup is resolved before opening any unrelated default screen.");

		await page.GotoAsync(new Uri(origin, "/components/tables").AbsoluteUri);
		await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Tables", Exact = true })).ToHaveAttributeAsync("aria-selected", "true");
		Assert.That(await page.GetByRole(AriaRole.Tab, new() { Name = "CRUD grid", Exact = true }).CountAsync(), Is.Zero);

		await page.EvaluateAsync("() => localStorage.setItem('sphere10.startup-screen.Sphere10.BlazorTester.Workspace', '{invalid JSON')");
		await page.GotoAsync(new Uri(origin, "/").AbsoluteUri);
		await page.GetByRole(AriaRole.Heading, new() { Name = "ApplicationBlock workspace", Exact = true }).WaitForAsync();
		await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Overview", Exact = true })).ToHaveAttributeAsync("aria-selected", "true");
		var screenshot = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"startup-screen-{TestContext.CurrentContext.Test.ID}.png");
		await page.ScreenshotAsync(new() { Path = screenshot, FullPage = true });
		TestContext.AddTestAttachment(screenshot);
		Assert.That(errors, Is.Empty);
	}
}
