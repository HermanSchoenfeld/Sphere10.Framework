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

/// <summary>Exercises both wizard generations in an isolated browser against the local tester.</summary>
[TestFixture]
[Explicit("Run scripts/test-blazor-browser.ps1 to supply the local tester and browser.")]
[Category("Browser")]
[NonParallelizable]
public class WizardBrowserTests {
	/// <summary>Verifies validation, completion, responsive modal layout, cancellation and focus restoration.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task SharedWorkflowValidatesCompletesAndHonorsModalCancellation(bool legacy) {
		var url = TestContext.Parameters.Get("BlazorTesterUrl", "");
		Assert.That(Uri.TryCreate(url, UriKind.Absolute, out var origin) && origin.IsLoopback && origin.Scheme is "http" or "https", Is.True,
			"Run scripts/test-blazor-browser.ps1 or supply a loopback BlazorTesterUrl through runsettings.");
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

		await OpenWizardsScreenAsync(page, origin, legacy);
		var allowCancellation = page.GetByRole(AriaRole.Checkbox, new() { Name = "Allow cancellation", Exact = true });
		await allowCancellation.UncheckAsync();
		var launcher = page.GetByRole(AriaRole.Button, new() { Name = legacy ? "New Widget" : "New Wallet", Exact = true });
		await launcher.ClickAsync();
		var dialog = page.Locator(legacy ? "#legacy-modal" : "#modern-modal");
		await dialog.WaitForAsync();
		Assert.That(await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).CountAsync(), Is.Zero);
		Assert.That(await dialog.Locator("button.close").CountAsync(), Is.Zero);
		await page.Keyboard.PressAsync("Escape");
		await page.Mouse.ClickAsync(8, 8);
		await dialog.GetByLabel(legacy ? "Name" : "Wallet Name", new() { Exact = true }).FillAsync("");
		await dialog.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
		await dialog.GetByRole(AriaRole.Alert).WaitForAsync();
		Assert.That(await dialog.IsVisibleAsync(), Is.True, "Escape and a click outside must preserve a non-cancellable wizard and its validation state.");
		Assert.That(await page.Locator(".modal-backdrop").CountAsync(), Is.EqualTo(1), "A wizard that cannot be cancelled must not be asked about.");

		// Measure the visible content after the transition; the centering wrapper spans the viewport.
		await dialog.EvaluateAsync("async element => await Promise.all(element.getAnimations({ subtree: true }).map(animation => animation.finished.catch(() => {})))");
		var bounds = await dialog.Locator(".modal-content").BoundingBoxAsync();
		Assert.That(bounds.X + bounds.Width / 2, Is.EqualTo(720).Within(1), "Wizard content must be horizontally centered.");
		Assert.That(bounds.Y + bounds.Height / 2, Is.EqualTo(500).Within(1), "Wizard content must be vertically centered.");

		// A short viewport must keep the top reachable and allow scrolling to the final action.
		await page.SetViewportSizeAsync(390, 320);
		bounds = await dialog.Locator(".modal-content").BoundingBoxAsync();
		Assert.That(bounds.Y, Is.GreaterThanOrEqualTo(0), "Oversized dialogs must not place their header above the viewport.");
		var next = dialog.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true });
		await next.ScrollIntoViewIfNeededAsync();
		var nextBounds = await next.BoundingBoxAsync();
		Assert.That(nextBounds.Y, Is.GreaterThanOrEqualTo(0));
		Assert.That(nextBounds.Y + nextBounds.Height, Is.LessThanOrEqualTo(320));
		await page.SetViewportSizeAsync(1440, 1000);
		await dialog.EvaluateAsync("element => element.scrollTop = 0");

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

		// The current generation asks before Escape cancels a wizard; the original generation closes directly.
		if (!legacy)
			await page.Locator("#modern-modal-confirmation").GetByRole(AriaRole.Button, new() { Name = "Yes", Exact = true }).ClickAsync();
		await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
		Assert.That(await launcher.EvaluateAsync<bool>("element => element === document.activeElement"), Is.True);
		Assert.That(await page.Locator(".modal-backdrop").CountAsync(), Is.Zero, "Closing must remove the backdrop and release the page.");
		Assert.That(errors, Is.Empty, "Wizard transitions and modal teardown must not fail the browser circuit.");
	}

	/// <summary>Verifies that a click outside a wizard and Escape ask before cancelling it, that No keeps its entries and focus, and that its close button cancels directly.</summary>
	[Test]
	public async Task ClickOutsideAndEscapeAskBeforeCancellingAWizardAndItsCloseButtonCancelsDirectly() {
		var url = TestContext.Parameters.Get("BlazorTesterUrl", "");
		Assert.That(Uri.TryCreate(url, UriKind.Absolute, out var origin) && origin.IsLoopback && origin.Scheme is "http" or "https", Is.True,
			"Run scripts/test-blazor-browser.ps1 or supply a loopback BlazorTesterUrl through runsettings.");
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
		await OpenWizardsScreenAsync(page, origin, false);
		var launcher = page.GetByRole(AriaRole.Button, new() { Name = "New Wallet", Exact = true });
		await launcher.ClickAsync();
		var dialog = page.Locator("#modern-modal");
		var question = page.Locator("#modern-modal-confirmation");
		await dialog.WaitForAsync();
		var name = dialog.GetByLabel("Wallet Name", new() { Exact = true });
		await name.FillAsync("Kept wallet");

		// A click outside asks above the wizard, which stays in place beneath the question and takes no input.
		await page.Mouse.ClickAsync(8, 8);
		await question.GetByText("Are you sure you want to close?", new() { Exact = true }).WaitForAsync();
		Assert.That(await dialog.IsVisibleAsync(), Is.True);
		Assert.That(await page.Locator(".modal-backdrop").CountAsync(), Is.EqualTo(2));
		Assert.That(await name.EvaluateAsync<bool>("element => { const box = element.getBoundingClientRect(); return document.getElementById('modern-modal-confirmation').contains(document.elementFromPoint(box.x + box.width / 2, box.y + box.height / 2)); }"), Is.True,
			"The question must cover the wizard.");
		var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "WizardBrowserTests");
		Tools.FileSystem.CreateDirectory(directory);
		var screenshot = Path.Combine(directory, "wallet-close-question.png");
		await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshot, FullPage = true });
		TestContext.AddTestAttachment(screenshot, "Close question above the wizard");
		await question.GetByRole(AriaRole.Button, new() { Name = "No", Exact = true }).ClickAsync();
		await question.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
		Assert.That(await dialog.IsVisibleAsync(), Is.True);
		Assert.That(await name.InputValueAsync(), Is.EqualTo("Kept wallet"));
		Assert.That(await dialog.EvaluateAsync<bool>("element => element.contains(document.activeElement)"), Is.True, "No must return focus to the wizard.");
		Assert.That(await page.Locator(".modal-backdrop").CountAsync(), Is.EqualTo(1));

		// Escape asks as well. Escape on the question answers No and returns to the field being edited.
		await name.FocusAsync();
		await page.Keyboard.PressAsync("Escape");
		await question.GetByRole(AriaRole.Button, new() { Name = "Yes", Exact = true }).WaitForAsync();
		await page.Keyboard.PressAsync("Escape");
		await question.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
		Assert.That(await name.EvaluateAsync<bool>("element => element === document.activeElement"), Is.True);
		Assert.That(await name.InputValueAsync(), Is.EqualTo("Kept wallet"));

		// Yes cancels the wizard and returns focus to the page.
		await page.Keyboard.PressAsync("Escape");
		await question.GetByRole(AriaRole.Button, new() { Name = "Yes", Exact = true }).ClickAsync();
		await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
		Assert.That(await question.IsHiddenAsync(), Is.True);
		Assert.That(await launcher.EvaluateAsync<bool>("element => element === document.activeElement"), Is.True);
		Assert.That(await page.Locator(".modal-backdrop").CountAsync(), Is.Zero);

		// The close button cancels a new wizard without asking.
		await launcher.ClickAsync();
		await dialog.WaitForAsync();
		await dialog.GetByLabel("Wallet Name", new() { Exact = true }).FillAsync("Closed wallet");
		await dialog.Locator("button.close").ClickAsync();
		await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
		Assert.That(await question.IsHiddenAsync(), Is.True, "The close button must not ask.");
		Assert.That(await page.Locator(".modal-backdrop").CountAsync(), Is.Zero);
		Assert.That(errors, Is.Empty, "Asking and answering must not fail the browser circuit.");
	}

	// The module-written grid width proves the circuit is interactive before following a normal in-app link.
	private static async Task OpenWizardsScreenAsync(IPage page, Uri origin, bool legacy) {
		await page.GotoAsync(new Uri(origin, "/components/grid").AbsoluteUri);
		await page.WaitForFunctionAsync("() => !!document.querySelector('.sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table')?.style.width");
		await page.GetByRole(AriaRole.Navigation, new() { Name = "Application blocks", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = legacy ? "Legacy examples" : "Component gallery", Exact = true }).ClickAsync();
		await page.GetByRole(AriaRole.Complementary, new() { Name = "Application navigation", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = legacy ? "Legacy wizard" : "Wizards", Exact = true }).ClickAsync();
		await page.WaitForFunctionAsync("block => location.pathname === '/application' && new URLSearchParams(location.search).get('block') === block && new URLSearchParams(location.search).get('screen') === 'wizards'", legacy ? "legacy" : "components");
		Assert.That(await page.Locator(".sphere10-application").CountAsync(), Is.EqualTo(1));
		Assert.That(await page.Locator(".sphere10-screen[role=tabpanel]:not([hidden])").CountAsync(), Is.EqualTo(1));
	}
}
