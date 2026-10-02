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

[TestFixture]
[Explicit("Requires BlazorTesterUrl and an installed Chromium or BrowserExecutablePath.")]
[Category("Browser")]
[NonParallelizable]
public class WizardBrowserTests {
	[TestCase(false)]
	[TestCase(true)]
	public async Task SharedWorkflowValidatesCompletesAndHonorsModalCancellation(bool legacy) {
		var url = TestContext.Parameters.Get("BlazorTesterUrl", "");
		Assert.That(Uri.TryCreate(url, UriKind.Absolute, out var origin) && origin.IsLoopback && origin.Scheme is "http" or "https", Is.True);
		var executable = TestContext.Parameters.Get("BrowserExecutablePath", "");
		using var playwright = await Playwright.CreateAsync();
		await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
			Headless = true,
			ExecutablePath = string.IsNullOrWhiteSpace(executable) ? null : executable
		});
		await using var context = await browser.NewContextAsync(new BrowserNewContextOptions {
			ViewportSize = new ViewportSize { Width = 1440, Height = 1000 }
		});
		var page = await context.NewPageAsync();
		page.SetDefaultTimeout(15000);
		var errors = new List<string>();
		page.PageError += (_, error) => errors.Add(error);
		page.Console += (_, message) => {
			if (message.Type == "error")
				errors.Add(message.Text);
		};

		// The module-written grid width proves the circuit is interactive before following a normal in-app link.
		await page.GotoAsync(new Uri(origin, "/components/grid").AbsoluteUri);
		await page.WaitForFunctionAsync("() => !!document.querySelector('.sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table')?.style.width");
		await page.GetByRole(AriaRole.Navigation, new() { Name = "Application blocks", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = legacy ? "Legacy examples" : "Component gallery", Exact = true }).ClickAsync();
		await page.GetByRole(AriaRole.Complementary, new() { Name = "Application navigation", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = legacy ? "Legacy wizard" : "Wizards", Exact = true }).ClickAsync();
		await page.WaitForFunctionAsync("block => location.pathname === '/application' && new URLSearchParams(location.search).get('block') === block && new URLSearchParams(location.search).get('screen') === 'wizards'", legacy ? "legacy" : "components");
		Assert.That(await page.Locator(".sphere10-application").CountAsync(), Is.EqualTo(1));
		Assert.That(await page.Locator(".sphere10-screen[role=tabpanel]:not([hidden])").CountAsync(), Is.EqualTo(1));
		var allowCancellation = page.GetByRole(AriaRole.Checkbox, new() { Name = "Allow cancellation", Exact = true });
		await allowCancellation.UncheckAsync();
		var launcher = page.GetByRole(AriaRole.Button, new() { Name = legacy ? "New Widget" : "New Wallet", Exact = true });
		await launcher.ClickAsync();
		var dialog = page.Locator(legacy ? "#legacy-modal" : "#modern-modal");
		await dialog.WaitForAsync();
		Assert.That(await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).CountAsync(), Is.Zero);
		Assert.That(await dialog.Locator("button.close").CountAsync(), Is.Zero);
		await page.Keyboard.PressAsync("Escape");
		await dialog.GetByLabel(legacy ? "Name" : "Wallet Name", new() { Exact = true }).FillAsync("");
		await dialog.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
		await dialog.GetByRole(AriaRole.Alert).WaitForAsync();
		Assert.That(await dialog.IsVisibleAsync(), Is.True, "Escape must preserve a non-cancellable wizard and its validation state.");

		var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "WizardBrowserTests");
		Tools.FileSystem.CreateDirectory(directory);
		var screenshot = Path.Combine(directory, legacy ? "widget-validation.png" : "wallet-validation.png");
		await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshot, FullPage = true });
		TestContext.AddTestAttachment(screenshot, "Validated wizard before completion");

		if (legacy) {
			await dialog.GetByLabel("Name", new() { Exact = true }).FillAsync("Shared widget workflow");
			await dialog.GetByLabel("Description", new() { Exact = true }).FillAsync("Browser validation");
			await dialog.GetByLabel("Price", new() { Exact = true }).FillAsync("12");
			await dialog.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
			await dialog.GetByRole(AriaRole.Heading, new() { Name = "Summary", Exact = true }).WaitForAsync();
			await dialog.GetByRole(AriaRole.Button, new() { Name = "Finish", Exact = true }).ClickAsync();
			await page.GetByRole(AriaRole.Cell, new() { Name = "Shared widget workflow", Exact = true }).WaitForAsync();
		} else {
			await dialog.GetByLabel("Wallet Name", new() { Exact = true }).FillAsync("Shared wallet workflow");
			await dialog.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
			await dialog.GetByRole(AriaRole.Radio, new() { Name = "Standard", Exact = true }).CheckAsync();
			await dialog.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
			await dialog.GetByLabel("Password", new() { Exact = true }).FillAsync("Demo-password");
			await dialog.GetByLabel("Confirm password", new() { Exact = true }).FillAsync("Demo-password");
			await dialog.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
			await dialog.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();
			await page.GetByText("Demo wallet 'Shared wallet workflow' completed (Standard). No wallet is stored.", new() { Exact = true }).WaitForAsync();
		}
		await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

		// A fresh cancellable workflow uses the same host and returns focus through the PascalCase modal module.
		await allowCancellation.CheckAsync();
		await launcher.ClickAsync();
		await dialog.WaitForAsync();
		await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).WaitForAsync();
		await page.Keyboard.PressAsync("Escape");
		await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
		Assert.That(await launcher.EvaluateAsync<bool>("element => element === document.activeElement"), Is.True);
		Assert.That(await page.Locator(".modal-backdrop").CountAsync(), Is.Zero, "Closing must remove the backdrop and release the page.");
		Assert.That(errors, Is.Empty, "Wizard transitions and modal teardown must not fail the browser circuit.");
	}
}
