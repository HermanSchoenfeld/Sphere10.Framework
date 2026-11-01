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

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>Verifies real interactive Options drafts, persistence and per-browser ownership in the tester's independent registrations.</summary>
[TestFixture]
[Explicit("Run scripts/test-blazor-browser.ps1 -filter FullyQualifiedName~ApplicationSettingsBrowserTests.")]
[Category("Browser")]
[NonParallelizable]
public class ApplicationSettingsBrowserTests {
	[Test]
	public async Task OptionsDraftsValidationApplyResetAndBrowserRestartPreserveRegistrationOwnership() {
		var url = TestContext.Parameters.Get("BlazorTesterUrl", "");
		Assert.That(Uri.TryCreate(url, UriKind.Absolute, out var origin) && origin.IsLoopback, Is.True);
		var executable = TestContext.Parameters.Get("BrowserExecutablePath", "");
		using var playwright = await Playwright.CreateAsync();
		await using var browser = await playwright.Chromium.LaunchAsync(new() {
			Headless = true, ExecutablePath = string.IsNullOrWhiteSpace(executable) ? null : executable
		});
		await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 1000 } });
		var page = await context.NewPageAsync();
		page.SetDefaultTimeout(15000);
		var errors = new List<string>();
		page.PageError += (_, error) => errors.Add(error);
		await page.GotoAsync(new Uri(origin, "/components/grid").AbsoluteUri);
		await page.WaitForFunctionAsync("() => !!document.querySelector('.sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table')?.style.width");
		await page.GetByRole(AriaRole.Navigation, new() { Name = "Application blocks", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Workspace", Exact = true }).ClickAsync();
		var navigation = page.GetByRole(AriaRole.Complementary, new() { Name = "Application navigation", Exact = true });
		await SelectInspectorAsync(page, navigation, "CPU");
		var screen = page.Locator(".sphere10-screen:not([hidden]) [data-demo=screen-registration]");
		await WaitCaptionAsync(page, "CPU summary");
		Assert.That(await page.EvaluateAsync<string>("() => localStorage.getItem('sphere10.settings.browser-profile')"), Is.Not.Null.And.Not.Empty,
			"The live startup path must resolve its browser owner before operational preferences are used.");
		await screen.GetByLabel("Retained draft", new() { Exact = true }).FillAsync("CPU operational draft");
		var instance = await screen.Locator("[data-registration-instance]").InnerTextAsync();
		await SelectInspectorAsync(page, navigation, "GPU");
		await WaitCaptionAsync(page, "GPU summary");
		await screen.GetByLabel("Retained draft", new() { Exact = true }).FillAsync("GPU operational draft");
		await SelectInspectorAsync(page, navigation, "CPU");

		// Nested and collection edits are provisional; cancelling them creates no preference records.
		var dialog = await OpenOptionsAsync(page);
		await CaptureAsync(page, "options-initial.png");
		await SelectOptionsPageAsync(dialog, "CPU");
		Assert.That(await dialog.GetByText("Saved preferences could not be loaded. Defaults are shown.", new() { Exact = true }).CountAsync(), Is.Zero);
		await SetTextAsync(dialog.GetByLabel("Font scale", new() { Exact = true }), "125");
		await dialog.GetByRole(AriaRole.Button, new() { Name = "Add Saved views", Exact = true }).ClickAsync();
		await page.WaitForFunctionAsync("() => document.querySelectorAll('.sphere10-options-host .sphere10-options-collection-item').length === 2");
		await SetTextAsync(dialog.GetByLabel("Name", new() { Exact = true }).Last, "Provisional view");
		await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
		await dialog.WaitForAsync(new() { State = WaitForSelectorState.Detached });
		Assert.That(await page.EvaluateAsync<int>("() => Object.keys(localStorage).filter(key => key.startsWith('sphere10.settings:')).length"), Is.Zero);
		dialog = await OpenOptionsAsync(page);
		await SelectOptionsPageAsync(dialog, "CPU");
		Assert.That(await dialog.GetByLabel("Font scale", new() { Exact = true }).InputValueAsync(), Is.EqualTo("100"));
		Assert.That(await dialog.Locator(".sphere10-options-collection-item").CountAsync(), Is.EqualTo(1));

		// Declared validation and an actual failed browser write both retain the dialog and its retryable draft.
		await SetTextAsync(dialog.GetByLabel("Summary heading", new() { Exact = true }), "");
		await dialog.GetByRole(AriaRole.Button, new() { Name = "Apply", Exact = true }).ClickAsync();
		await dialog.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Summary heading must contain" }).WaitForAsync();
		await SetTextAsync(dialog.GetByLabel("Summary heading", new() { Exact = true }), "CPU applied");
		await SetTextAsync(dialog.GetByLabel("Font scale", new() { Exact = true }), "120");
		await page.EvaluateAsync("""
			() => {
				window.__settingsOriginalSetItem = Storage.prototype.setItem;
				Storage.prototype.setItem = function(key, value) {
					if (String(key).startsWith('sphere10.settings:'))
						throw new DOMException('Isolated test storage rejection', 'QuotaExceededError');
					return window.__settingsOriginalSetItem.call(this, key, value);
				};
			}
			""");
		await dialog.GetByRole(AriaRole.Button, new() { Name = "Apply", Exact = true }).ClickAsync();
		await dialog.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Preferences could not be saved" }).WaitForAsync();
		Assert.That(await dialog.GetByLabel("Summary heading", new() { Exact = true }).InputValueAsync(), Is.EqualTo("CPU applied"));
		Assert.That(await screen.GetByLabel("Retained draft", new() { Exact = true }).InputValueAsync(), Is.EqualTo("CPU operational draft"));
		await page.EvaluateAsync("() => { Storage.prototype.setItem = window.__settingsOriginalSetItem; delete window.__settingsOriginalSetItem; }");
		await dialog.GetByRole(AriaRole.Button, new() { Name = "Apply", Exact = true }).ClickAsync();
		await WaitCaptionAsync(page, "CPU applied");
		Assert.That(await page.EvaluateAsync<bool>("() => Object.keys(localStorage).filter(key => key.startsWith('sphere10.settings:')).some(key => JSON.parse(localStorage.getItem(key)).values?.Caption === 'CPU applied')"), Is.True);
		Assert.That(await screen.Locator("[data-registration-instance]").InnerTextAsync(), Is.EqualTo(instance));
		await CaptureAsync(page, "options-desktop.png");
		await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
		await dialog.WaitForAsync(new() { State = WaitForSelectorState.Detached });

		// Reset uses this registration's defaults and remains provisional until explicitly applied.
		dialog = await OpenOptionsAsync(page);
		await SelectOptionsPageAsync(dialog, "CPU");
		await dialog.GetByRole(AriaRole.Button, new() { Name = "Reset Defaults", Exact = true }).ClickAsync();
		await page.WaitForFunctionAsync("() => document.querySelector('.sphere10-options-host [data-option-path=Caption] input')?.value === 'CPU summary'");
		await WaitCaptionAsync(page, "CPU applied");
		await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
		await dialog.WaitForAsync(new() { State = WaitForSelectorState.Detached });

		// The second registration uses its custom editor and independent persisted values.
		dialog = await OpenOptionsAsync(page);
		await SelectOptionsPageAsync(dialog, "GPU");
		await dialog.Locator("[data-custom-settings-editor]").WaitForAsync();
		Assert.That(await dialog.GetByLabel("Inspector preference preview", new() { Exact = true }).InnerTextAsync(), Is.EqualTo("GPU summary"));
		await SetTextAsync(dialog.GetByLabel("Summary heading", new() { Exact = true }), "GPU applied");
		await dialog.GetByLabel("Detail level", new() { Exact = true }).SelectOptionAsync("Detailed");
		await page.SetViewportSizeAsync(390, 844);
		await CaptureAsync(page, "options-mobile.png");
		var footerBounds = new List<(string Name, float Left, float Top, float Right, float Bottom)>();
		foreach (var name in new[] { "OK", "Cancel", "Apply", "Reset Defaults" }) {
			var bounds = await dialog.GetByRole(AriaRole.Button, new() { Name = name, Exact = true }).BoundingBoxAsync();
			Assert.That(bounds, Is.Not.Null);
			footerBounds.Add((name, bounds.X, bounds.Y, bounds.X + bounds.Width, bounds.Y + bounds.Height));
		}
		var boundsPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, nameof(ApplicationSettingsBrowserTests), "mobile-footer-bounds.json");
		File.WriteAllText(boundsPath, Tools.Json.WriteToString(footerBounds));
		TestContext.AddTestAttachment(boundsPath);
		foreach (var bounds in footerBounds) {
			Assert.That(bounds.Left, Is.GreaterThanOrEqualTo(0), bounds.Name);
			Assert.That(bounds.Right, Is.LessThanOrEqualTo(390), bounds.Name);
			Assert.That(bounds.Bottom, Is.LessThanOrEqualTo(844), bounds.Name);
		}
		await dialog.GetByRole(AriaRole.Button, new() { Name = "OK", Exact = true }).ClickAsync();
		await dialog.WaitForAsync(new() { State = WaitForSelectorState.Detached });
		await page.SetViewportSizeAsync(1440, 1000);
		await SelectInspectorAsync(page, navigation, "GPU");
		await WaitCaptionAsync(page, "GPU applied");
		Assert.That(await screen.GetByLabel("Retained draft", new() { Exact = true }).InputValueAsync(), Is.EqualTo("GPU operational draft"));
		await SelectInspectorAsync(page, navigation, "CPU");
		await WaitCaptionAsync(page, "CPU applied");
		Assert.That(await screen.GetByLabel("Retained draft", new() { Exact = true }).InputValueAsync(), Is.EqualTo("CPU operational draft"));

		// Record the first operational caption after reload, catching restoration that happens after first screen work.
		await context.AddInitScriptAsync("""
			window.__firstInspectorCaption = null;
			new MutationObserver(() => {
				const caption = document.querySelector('.sphere10-screen:not([hidden]) [data-inspector-caption]');
				if (caption && window.__firstInspectorCaption === null)
					window.__firstInspectorCaption = caption.textContent.trim();
			}).observe(document, { childList: true, subtree: true });
			""");
		await page.ReloadAsync();
		await WaitCaptionAsync(page, "CPU applied");
		Assert.That(await page.EvaluateAsync<string>("() => window.__firstInspectorCaption"), Is.EqualTo("CPU applied"));

		// A completed ordinary control action uses the same ordered owner and survives a browser reload without teardown delivery.
		await screen.GetByRole(AriaRole.Button, new() { Name = "Toggle summary", Exact = true }).ClickAsync();
		await page.WaitForFunctionAsync("() => Object.keys(localStorage).filter(key => key.startsWith('sphere10.settings:')).some(key => { const values = JSON.parse(localStorage.getItem(key)).values; return values?.Caption === 'CPU applied' && values.ShowSummary === false; })");
		await page.ReloadAsync();
		await screen.GetByRole(AriaRole.Heading, new() { Name = "CPU inspector", Exact = true }).WaitForAsync();
		Assert.That(await screen.GetByRole(AriaRole.Region, new() { Name = "Inspector summary", Exact = true }).CountAsync(), Is.Zero);
		await using var separate = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 1000 } });
		var freshPage = await separate.NewPageAsync();
		freshPage.SetDefaultTimeout(15000);
		await freshPage.GotoAsync(page.Url);
		await WaitCaptionAsync(freshPage, "CPU summary");
		Assert.That(errors, Is.Empty);
	}

	private static async Task SelectInspectorAsync(IPage page, ILocator navigation, string name) {
		await navigation.GetByRole(AriaRole.Button, new() { Name = name + " inspector", Exact = true }).ClickAsync();
		await page.Locator(".sphere10-screen:not([hidden]) [data-demo=screen-registration]")
			.GetByRole(AriaRole.Heading, new() { Name = name + " inspector", Exact = true }).WaitForAsync();
	}

	private static async Task<ILocator> OpenOptionsAsync(IPage page) {
		var commands = page.GetByRole(AriaRole.Navigation, new() { Name = "Application commands", Exact = true });
		await commands.GetByRole(AriaRole.Button, new() { Name = "Options", Exact = true }).ClickAsync();
		await commands.GetByRole(AriaRole.Button, new() { Name = "Screen Options...", Exact = true }).ClickAsync();
		var dialog = page.Locator(".sphere10-options-host");
		await dialog.GetByRole(AriaRole.Heading, new() { Name = "Screen Options", Exact = true }).WaitForAsync();
		return dialog;
	}

	private static async Task SelectOptionsPageAsync(ILocator dialog, string name) {
		await dialog.GetByRole(AriaRole.Navigation, new() { Name = "Screen options pages", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = name + " inspector", Exact = true }).ClickAsync();
		await dialog.GetByRole(AriaRole.Heading, new() { Name = name + " inspector", Exact = true }).WaitForAsync();
	}

	private static async Task SetTextAsync(ILocator input, string value) {
		await input.FillAsync(value);
		await input.PressAsync("Tab");
	}

	private static async Task WaitCaptionAsync(IPage page, string caption) {
		await page.WaitForFunctionAsync("caption => document.querySelector('.sphere10-screen:not([hidden]) [data-inspector-caption]')?.textContent.trim() === caption", caption);
	}

	private static async Task CaptureAsync(IPage page, string name) {
		var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, nameof(ApplicationSettingsBrowserTests));
		Tools.FileSystem.CreateDirectory(directory);
		var screenshot = Path.Combine(directory, name);
		await page.ScreenshotAsync(new() { Path = screenshot, FullPage = true });
		TestContext.AddTestAttachment(screenshot);
	}
}
