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
public class ApplicationScreenPoliciesBrowserTests {
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
			var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"screen-policy-{TestContext.CurrentContext.Test.ID}.png");
			await _page.ScreenshotAsync(new() { Path = path, FullPage = true });
			TestContext.AddTestAttachment(path);
		}
		Assert.That(_errors, Is.Empty);
	}

	[Test]
	public async Task EmptyScreenIsTablessAndObeysConfiguredLifetimeAcrossRealScreenNavigation() {
		if (TestContext.Parameters.Get("PermanentProfile", "false") == "true")
			Assert.Ignore("The permanent profile intentionally never has an empty workspace.");
		await CloseAllAsync();
		await _page.GetByRole(AriaRole.Heading, new() { Name = "No screens are open", Exact = true }).WaitForAsync();
		Assert.That(await _page.GetByRole(AriaRole.Tab).CountAsync(), Is.Zero);
		var original = await _page.Locator("[data-empty-instance]").InnerTextAsync();
		var retained = await _page.GetByText("Lifetime: SingleInstance", new() { Exact = true }).CountAsync() > 0;
		await _page.GetByRole(AriaRole.Textbox, new() { Name = "Empty workspace note", Exact = true }).FillAsync("Retained empty draft");
		await _page.GetByRole(AriaRole.Navigation, new() { Name = "Application blocks", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Workspace", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Navigation, new() { Name = "Workspace menus", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Overview", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Tab, new() { Name = "Overview", Exact = true }).WaitForAsync();
		Assert.That(await _page.GetByRole(AriaRole.Textbox, new() { Name = "Empty workspace note", Exact = true }).IsVisibleAsync(), Is.False);
		await CloseAllAsync();
		var note = _page.GetByRole(AriaRole.Textbox, new() { Name = "Empty workspace note", Exact = true });
		await note.WaitForAsync();
		Assert.That(await note.InputValueAsync(), Is.EqualTo(retained ? "Retained empty draft" : ""));
		Assert.That(await _page.Locator("[data-empty-instance]").InnerTextAsync() == original, Is.EqualTo(retained));
		Assert.That(await _page.GetByRole(AriaRole.Tab).CountAsync(), Is.Zero);
		await AttachScreenshotAsync("empty-screen");
	}

	[Test]
	public async Task PermanentScreenHasNoCloseAffordanceAndSurvivesCloseAllAndLayoutChanges() {
		if (TestContext.Parameters.Get("PermanentProfile", "false") != "true")
			Assert.Ignore("Run this case against the Permanent singleton launch profile with PermanentProfile=true.");
		var permanent = _page.GetByRole(AriaRole.Tab, new() { Name = "Permanent screen", Exact = true });
		await permanent.WaitForAsync();
		Assert.That(await _page.GetByRole(AriaRole.Button, new() { Name = "Close Permanent screen", Exact = true }).CountAsync(), Is.Zero);
		await permanent.ClickAsync();
		var note = _page.GetByRole(AriaRole.Textbox, new() { Name = "Permanent screen note", Exact = true });
		await note.FillAsync("Permanent draft");
		var instance = await _page.Locator("[data-permanent-instance]").InnerTextAsync();
		await _page.GetByRole(AriaRole.Tab, new() { Name = "Overview", Exact = true }).ClickAsync();
		await CloseAllAsync();
		await note.WaitForAsync();
		Assert.That(await note.InputValueAsync(), Is.EqualTo("Permanent draft"));
		Assert.That(await _page.GetByRole(AriaRole.Tab).CountAsync(), Is.EqualTo(1));
		await SelectViewAsync("Single screen");
		await _page.GetByRole(AriaRole.Tab).WaitForAsync(new() { State = WaitForSelectorState.Detached });
		Assert.That(await _page.Locator("[data-permanent-instance]").InnerTextAsync(), Is.EqualTo(instance));
		await SelectViewAsync("Tabbed screens");
		await permanent.WaitForAsync();
		Assert.That(await note.InputValueAsync(), Is.EqualTo("Permanent draft"));
		Assert.That(await _page.GetByRole(AriaRole.Heading, new() { Name = "No screens are open", Exact = true }).CountAsync(), Is.Zero);
		await AttachScreenshotAsync("permanent-screen");
	}

	private async Task CloseAllAsync() {
		var commands = _page.GetByRole(AriaRole.Navigation, new() { Name = "Application commands", Exact = true });
		await commands.GetByRole(AriaRole.Button, new() { Name = "File", Exact = true }).ClickAsync();
		await commands.GetByRole(AriaRole.Group, new() { Name = "File", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Close all screens", Exact = true }).ClickAsync();
	}

	private async Task SelectViewAsync(string command) {
		var commands = _page.GetByRole(AriaRole.Navigation, new() { Name = "Application commands", Exact = true });
		await commands.GetByRole(AriaRole.Button, new() { Name = "View", Exact = true }).ClickAsync();
		await commands.GetByRole(AriaRole.Group, new() { Name = "View", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = command, Exact = true }).ClickAsync();
	}

	private async Task AttachScreenshotAsync(string name) {
		var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{name}-{TestContext.CurrentContext.Test.ID}.png");
		await _page.ScreenshotAsync(new() { Path = path, FullPage = true });
		TestContext.AddTestAttachment(path);
	}
}
