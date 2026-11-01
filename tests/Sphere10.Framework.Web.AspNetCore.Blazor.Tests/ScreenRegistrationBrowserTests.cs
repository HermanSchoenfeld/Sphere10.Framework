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

/// <summary>Exercises exact registrations, retained same-type screens and menu-free bookmarks in the real shell.</summary>
[TestFixture]
[Explicit("Run scripts/test-blazor-browser.ps1 -filter FullyQualifiedName~ScreenRegistrationBrowserTests.")]
[Category("Browser")]
[NonParallelizable]
public class ScreenRegistrationBrowserTests {
	[Test]
	public async Task SameTypeRegistrationsKeepIndependentDraftsAndMenuFreeBookmarksSurviveReload() {
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

		// Enter through the real circuit and its registered block and screen commands.
		await page.GotoAsync(new Uri(origin, "/components/grid").AbsoluteUri);
		await page.WaitForFunctionAsync("() => !!document.querySelector('.sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table')?.style.width");
		await page.GetByRole(AriaRole.Navigation, new() { Name = "Application blocks", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Workspace", Exact = true }).ClickAsync();
		var navigation = page.GetByRole(AriaRole.Complementary, new() { Name = "Application navigation", Exact = true });
		await navigation.GetByRole(AriaRole.Button, new() { Name = "CPU inspector", Exact = true }).ClickAsync();
		var screen = page.Locator(".sphere10-screen:not([hidden]) [data-demo=screen-registration]");
		await screen.GetByRole(AriaRole.Heading, new() { Name = "CPU inspector", Exact = true }).WaitForAsync();
		var cpuInstance = await screen.Locator("[data-registration-instance]").InnerTextAsync();
		await screen.GetByLabel("Retained draft", new() { Exact = true }).FillAsync("CPU draft");
		await navigation.GetByRole(AriaRole.Button, new() { Name = "GPU inspector", Exact = true }).ClickAsync();
		await screen.GetByRole(AriaRole.Heading, new() { Name = "GPU inspector", Exact = true }).WaitForAsync();
		Assert.That(await screen.GetByLabel("Retained draft", new() { Exact = true }).InputValueAsync(), Is.Empty);
		Assert.That(await screen.Locator("[data-registration-instance]").InnerTextAsync(), Is.Not.EqualTo(cpuInstance));
		await screen.GetByLabel("Retained draft", new() { Exact = true }).FillAsync("GPU draft");
		await navigation.GetByRole(AriaRole.Button, new() { Name = "CPU inspector", Exact = true }).ClickAsync();
		await screen.GetByRole(AriaRole.Heading, new() { Name = "CPU inspector", Exact = true }).WaitForAsync();
		Assert.That(await screen.GetByLabel("Retained draft", new() { Exact = true }).InputValueAsync(), Is.EqualTo("CPU draft"));
		Assert.That(await screen.Locator("[data-registration-instance]").InnerTextAsync(), Is.EqualTo(cpuInstance));

		// The independent registration is navigable without a menu entry or a generated command object.
		await screen.GetByRole(AriaRole.Button, new() { Name = "Open menu-free details", Exact = true }).ClickAsync();
		await page.Locator(".sphere10-screen:not([hidden]) [data-demo=menu-free-registration]").WaitForAsync();
		await page.WaitForFunctionAsync("() => new URLSearchParams(location.search).get('screen')?.startsWith('__screen:')");
		await page.ReloadAsync();
		await page.Locator(".sphere10-screen:not([hidden]) [data-demo=menu-free-registration]").WaitForAsync();
		Assert.That(await navigation.GetByRole(AriaRole.Button, new() { Name = "Menu-free details", Exact = true }).CountAsync(), Is.Zero);
		Assert.That(errors, Is.Empty);

		var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, nameof(ScreenRegistrationBrowserTests));
		Tools.FileSystem.CreateDirectory(directory);
		var screenshot = Path.Combine(directory, "menu-free-registration.png");
		await page.ScreenshotAsync(new() { Path = screenshot, FullPage = true });
		TestContext.AddTestAttachment(screenshot);
	}
}
