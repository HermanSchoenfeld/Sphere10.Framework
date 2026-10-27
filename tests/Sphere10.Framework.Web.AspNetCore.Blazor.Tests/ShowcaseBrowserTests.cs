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
using NUnit.Framework.Interfaces;
using Sphere10.Framework.Web.AspNetCore.Blazor.Theming;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>Exercises composed application examples through the real host, native commands, themes and wizard transitions.</summary>
[TestFixture]
[Explicit("Requires BlazorTesterUrl and an installed Chromium or BrowserExecutablePath.")]
[Category("Browser")]
[NonParallelizable]
public class ShowcaseBrowserTests {
	private readonly List<string> _errors = new();
	private IPlaywright _playwright;
	private IBrowser _browser;
	private IPage _page;
	private Uri _origin;

	private ILocator ActiveScreen => _page.Locator(".sphere10-screen:not([hidden])");

	private ILocator ApplicationToolbar => _page.GetByRole(AriaRole.Toolbar, new() { Name = "Application toolbar", Exact = true });

	private ILocator ApplicationCommands => _page.GetByRole(AriaRole.Navigation, new() { Name = "Application commands", Exact = true });

	[SetUp]
	public async Task SetUp() {
		var url = TestContext.Parameters.Get("BlazorTesterUrl", "");
		Assert.That(Uri.TryCreate(url, UriKind.Absolute, out _origin) && _origin.IsLoopback && _origin.Scheme is "http" or "https", Is.True,
			"Set BlazorTesterUrl to the running local tester through TestRunParameters.");
		var executable = TestContext.Parameters.Get("BrowserExecutablePath", "");
		_errors.Clear();
		_playwright = await Playwright.CreateAsync();
		_browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
			Headless = true,
			IgnoreDefaultArgs = new[] { "--hide-scrollbars" },
			ExecutablePath = string.IsNullOrWhiteSpace(executable) ? null : executable
		});
		_page = await _browser.NewPageAsync(new BrowserNewPageOptions { ViewportSize = new ViewportSize { Width = 1500, Height = 1000 } });
		_page.SetDefaultTimeout(15000);
		_page.PageError += (_, error) => _errors.Add(error);
		_page.Console += (_, message) => {
			if (message.Type == "error")
				_errors.Add(message.Text);
		};
		await _page.GotoAsync(new Uri(_origin, "/components/grid").AbsoluteUri);
		await _page.WaitForFunctionAsync("""
			() => {
				const grid = document.querySelector('.sphere10-screen:not([hidden]) [data-demo=grid] > .sphere10-grid > .sphere10-grid-viewport > table');
				return grid && !!grid.style.width && grid.closest('.sphere10-grid').getAttribute('aria-busy') === 'false';
			}
			""");
	}

	[TearDown]
	public async Task TearDown() {
		using var runtimeCleanup = Tools.Scope.ExecuteOnDispose(() => _playwright?.Dispose());
		await using var browserCleanup = Tools.Scope.ExecuteOnDisposeAsync<IBrowser>(static browser => browser?.DisposeAsync() ?? ValueTask.CompletedTask, _browser);
		if (_page != null && !_page.IsClosed && TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
			await AttachScreenshotAsync("failure");
		Assert.That(_errors, Is.Empty, "Showcase interactions must preserve the browser circuit without console or script errors.");
	}

	[TestCase(1500, 1000)]
	[TestCase(390, 844)]
	public async Task NativeCommandsFollowContextSupportKeyboardSubmenusAndLeaveWithTheirScreen(int width, int height) {
		await _page.SetViewportSizeAsync(width, height);
		await OpenPatternScreenAsync("Menus & commands", "[data-showcase=commands]");
		var screen = ActiveScreen.Locator("[data-showcase=commands]");
		var toolbar = ApplicationToolbar;
		var commands = ApplicationCommands;
		var preview = toolbar.GetByRole(AriaRole.Button, new() { Name = "Preview document", Exact = true });
		var export = toolbar.GetByRole(AriaRole.Button, new() { Name = "Export", Exact = true });
		Assert.That(await preview.IsDisabledAsync(), Is.True);
		Assert.That(await export.IsDisabledAsync(), Is.True);
		await screen.GetByLabel("Sample document", new() { Exact = true }).SelectOptionAsync("0");
		await screen.GetByTestId("command-selected-document").Filter(new() { HasText = "Field notes" }).WaitForAsync();
		Assert.That(await preview.IsEnabledAsync(), Is.True);
		Assert.That(await export.IsEnabledAsync(), Is.True);

		// The native toolbar group supports a two-level keyboard path and focus restoration.
		await export.ClickAsync();
		var exportMenu = toolbar.GetByRole(AriaRole.Group, new() { Name = "Export", Exact = true });
		var documents = exportMenu.GetByRole(AriaRole.Button, new() { Name = "Documents", Exact = true });
		await documents.FocusAsync();
		await documents.PressAsync("ArrowRight");
		var markdown = exportMenu.GetByRole(AriaRole.Button, new() { Name = "Markdown", Exact = true });
		await markdown.WaitForAsync();
		await WaitForFocusAsync(markdown);
		await markdown.PressAsync("ArrowLeft");
		await markdown.WaitForAsync(new() { State = WaitForSelectorState.Detached });
		await WaitForFocusAsync(documents);
		await documents.PressAsync("ArrowRight");
		await WaitForFocusAsync(markdown);
		await markdown.PressAsync("Enter");
		await screen.GetByTestId("command-output-label").Filter(new() { HasText = "Field notes · Markdown" }).WaitForAsync();
		Assert.That(await screen.GetByTestId("command-output").InnerTextAsync(), Does.Contain("# Field notes").And.Contain("Audience: Operations team").And.Contain("Status: Draft"));
		Assert.That(await export.GetAttributeAsync("aria-expanded"), Is.EqualTo("false"));

		// File merges the same preview and export definitions into the application menu.
		await commands.GetByRole(AriaRole.Button, new() { Name = "File", Exact = true }).ClickAsync();
		var file = commands.GetByRole(AriaRole.Group, new() { Name = "File", Exact = true });
		await file.GetByRole(AriaRole.Button, new() { Name = "Export", Exact = true }).ClickAsync();
		await file.GetByRole(AriaRole.Button, new() { Name = "Structured data", Exact = true }).ClickAsync();
		await file.GetByRole(AriaRole.Button, new() { Name = "CSV", Exact = true }).ClickAsync();
		await screen.GetByTestId("command-output-label").Filter(new() { HasText = "Field notes · CSV" }).WaitForAsync();
		Assert.That(await screen.GetByTestId("command-output").InnerTextAsync(), Is.EqualTo("Document,Audience,Sections,Status\nField notes,Operations team,4,Draft"));
		await preview.ClickAsync();
		var dialog = _page.Locator("#modern-modal");
		await dialog.GetByRole(AriaRole.Heading, new() { Name = "Document preview", Exact = true }).WaitForAsync();
		Assert.That(await dialog.InnerTextAsync(), Does.Contain("Field notes contains 4 sections").And.Contain("Review status: draft"));
		await CloseDialogAsync();

		await screen.GetByRole(AriaRole.Checkbox, new() { Name = "Allow review actions", Exact = true }).UncheckAsync();
		await commands.GetByRole(AriaRole.Button, new() { Name = "Tools", Exact = true }).ClickAsync();
		var tools = commands.GetByRole(AriaRole.Group, new() { Name = "Tools", Exact = true });
		Assert.That(await tools.GetByRole(AriaRole.Button, new() { Name = "Mark reviewed", Exact = true }).IsDisabledAsync(), Is.True);
		Assert.That(await tools.GetByRole(AriaRole.Button, new() { Name = "Inspect command context", Exact = true }).CountAsync(), Is.Zero);
		await commands.GetByRole(AriaRole.Button, new() { Name = "Tools", Exact = true }).PressAsync("Escape");
		await screen.GetByRole(AriaRole.Checkbox, new() { Name = "Allow review actions", Exact = true }).CheckAsync();
		await screen.GetByRole(AriaRole.Checkbox, new() { Name = "Show advanced command", Exact = true }).CheckAsync();
		await commands.GetByRole(AriaRole.Button, new() { Name = "Tools", Exact = true }).ClickAsync();
		await tools.GetByRole(AriaRole.Button, new() { Name = "Inspect command context", Exact = true }).WaitForAsync();
		await tools.GetByRole(AriaRole.Button, new() { Name = "Mark reviewed", Exact = true }).ClickAsync();
		await screen.GetByTestId("command-review-state").Filter(new() { HasText = "Reviewed" }).WaitForAsync();
		await commands.GetByRole(AriaRole.Button, new() { Name = "Tools", Exact = true }).ClickAsync();
		Assert.That(await tools.GetByRole(AriaRole.Button, new() { Name = "Mark reviewed", Exact = true }).IsDisabledAsync(), Is.True);
		Assert.That(await tools.GetByRole(AriaRole.Button, new() { Name = "Clear review marks", Exact = true }).IsEnabledAsync(), Is.True);
		await tools.GetByRole(AriaRole.Button, new() { Name = "Inspect command context", Exact = true }).ClickAsync();
		await dialog.GetByRole(AriaRole.Heading, new() { Name = "Command context", Exact = true }).WaitForAsync();
		Assert.That(await dialog.InnerTextAsync(), Does.Contain("Selection: Field notes").And.Contain("Reviewed documents: 1"));
		await CloseDialogAsync();
		await screen.GetByRole(AriaRole.Checkbox, new() { Name = "Show advanced command", Exact = true }).UncheckAsync();
		await commands.GetByRole(AriaRole.Button, new() { Name = "Tools", Exact = true }).ClickAsync();
		Assert.That(await tools.GetByRole(AriaRole.Button, new() { Name = "Inspect command context", Exact = true }).CountAsync(), Is.Zero);
		await commands.GetByRole(AriaRole.Button, new() { Name = "Tools", Exact = true }).PressAsync("Escape");
		Assert.That(await screen.GetByTestId("command-activity").Locator("li").CountAsync(), Is.EqualTo(6));
		await VerifyContainmentAsync();
		await AttachScreenshotAsync($"commands-{width}");

		var panelId = await screen.EvaluateAsync<string>("element => element.closest('[role=tabpanel]').id");
		await OpenPatternScreenAsync("Theme studio", "[data-demo=theme-studio]");
		Assert.That(await toolbar.GetByRole(AriaRole.Button, new() { Name = "Preview document", Exact = true }).CountAsync(), Is.Zero);
		Assert.That(await toolbar.GetByRole(AriaRole.Button, new() { Name = "Export", Exact = true }).CountAsync(), Is.Zero);
		Assert.That(await commands.GetByRole(AriaRole.Button, new() { Name = "Tools", Exact = true }).CountAsync(), Is.Zero, "Tools is contributed only while its owning commands screen is active.");
		await _page.GetByRole(AriaRole.Tab, new() { Name = "Menus & commands", Exact = true }).ClickAsync();
		await screen.WaitForAsync();
		Assert.That(await screen.EvaluateAsync<string>("element => element.closest('[role=tabpanel]').id"), Is.EqualTo(panelId));
		Assert.That(await screen.GetByLabel("Sample document", new() { Exact = true }).InputValueAsync(), Is.EqualTo("0"));
		Assert.That(await screen.GetByTestId("command-review-state").InnerTextAsync(), Is.EqualTo("Reviewed"));
	}

	[TestCase("dark", 1500, 1000)]
	[TestCase("dark", 390, 844)]
	[TestCase("landkit", 1500, 1000)]
	[TestCase("landkit", 390, 844)]
	public async Task ThemeStudioPreservesDraftAndKeyboardChartSelectionAcrossPresets(string themeId, int width, int height) {
		await _page.SetViewportSizeAsync(width, height);
		await OpenPatternScreenAsync("Theme studio", "[data-demo=theme-studio]");
		var studio = ActiveScreen.Locator("[data-demo=theme-studio]");
		var panelId = await studio.EvaluateAsync<string>("element => element.closest('[role=tabpanel]').id");
		await studio.GetByLabel("Release title", new() { Exact = true }).FillAsync("Accessible autumn release");
		await studio.GetByLabel("Team note", new() { Exact = true }).FillAsync("Review the keyboard interactions before sharing.");
		await studio.GetByRole(AriaRole.Checkbox, new() { Name = "Send review reminders", Exact = true }).UncheckAsync();
		await studio.GetByRole(AriaRole.Combobox, new() { Name = "Preview period", Exact = true }).SelectOptionAsync("previous");
		var week = studio.GetByRole(AriaRole.Button, new() { Name = "Week 2, 16 completed items", Exact = true });
		await week.FocusAsync();
		await week.PressAsync("Enter");
		await studio.GetByTestId("studio-week").Filter(new() { HasText = "Week 2 · 16 completed items · Last month" }).WaitForAsync();
		var definition = ThemeCatalog.All.Single(theme => theme.Id == themeId);
		await studio.GetByRole(AriaRole.Button, new() { Name = $"Use {definition.Name}", Exact = true }).ClickAsync();
		await WaitForThemeAsync(themeId);
		Assert.That(await studio.GetByLabel("Release title", new() { Exact = true }).InputValueAsync(), Is.EqualTo("Accessible autumn release"));
		Assert.That(await studio.GetByLabel("Team note", new() { Exact = true }).InputValueAsync(), Is.EqualTo("Review the keyboard interactions before sharing."));
		Assert.That(await studio.GetByRole(AriaRole.Checkbox, new() { Name = "Send review reminders", Exact = true }).IsCheckedAsync(), Is.False);
		Assert.That(await week.GetAttributeAsync("aria-pressed"), Is.EqualTo("true"));
		Assert.That(await studio.EvaluateAsync<string>("element => element.closest('[role=tabpanel]').id"), Is.EqualTo(panelId));
		Assert.That(await studio.GetByTestId("studio-active-theme").InnerTextAsync(), Is.EqualTo(definition.Name));
		await VerifyContainmentAsync();
		await AttachScreenshotAsync($"theme-studio-{themeId}-{width}");
		await studio.GetByRole(AriaRole.Button, new() { Name = "Preview release", Exact = true }).ClickAsync();
		var dialog = _page.Locator("#modern-modal");
		await dialog.GetByRole(AriaRole.Heading, new() { Name = "Release preview", Exact = true }).WaitForAsync();
		Assert.That(await dialog.InnerTextAsync(), Does.Contain("Accessible autumn release").And.Contain("Week 2: 16 completed items").And.Contain("paused"));
		var bounds = await dialog.Locator(".modal-content").BoundingBoxAsync();
		Assert.That(bounds.X, Is.GreaterThanOrEqualTo(-1));
		Assert.That(bounds.X + bounds.Width, Is.LessThanOrEqualTo(width + 1));
		await AttachScreenshotAsync($"theme-studio-preview-{themeId}-{width}");
		await CloseDialogAsync();
		await ApplicationToolbar.GetByRole(AriaRole.Combobox, new() { Name = "Theme", Exact = true }).SelectOptionAsync("classic-blue");
		await WaitForThemeAsync("classic-blue");
		Assert.That(await studio.GetByTestId("studio-title-preview").InnerTextAsync(), Is.EqualTo("Accessible autumn release"));
		Assert.That(await studio.GetByTestId("studio-week").InnerTextAsync(), Is.EqualTo("Week 2 · 16 completed items · Last month"));
	}

	[TestCase(1500, 1000)]
	[TestCase(390, 844)]
	public async Task CollectionCardsAndTableShareFilteringAndKeyedEditorsPreserveSaveDiscardGuards(int width, int height) {
		await _page.SetViewportSizeAsync(width, height);
		await OpenPatternScreenAsync("Cards & collections", "[data-showcase=cards]");
		var gallery = ActiveScreen.Locator("[data-showcase=cards]");
		await gallery.GetByRole(AriaRole.Button, new() { Name = "Select Coastal notes", Exact = true }).ClickAsync();
		await gallery.GetByRole(AriaRole.Button, new() { Name = "Select Coastal notes", Exact = true }).And(_page.Locator("[aria-pressed=true]")).WaitForAsync();
		Assert.That(await gallery.GetByRole(AriaRole.Button, new() { Name = "Select Coastal notes", Exact = true }).GetAttributeAsync("aria-pressed"), Is.EqualTo("true"));
		await VerifyContainmentAsync();
		await AttachScreenshotAsync($"collection-cards-{width}");
		await gallery.GetByRole(AriaRole.Button, new() { Name = "Open Coastal notes", Exact = true }).ClickAsync();
		var detail = ActiveScreen.Locator("[data-showcase=collection-detail][data-collection-id=coastal-notes]");
		await detail.WaitForAsync();
		var panelId = await detail.EvaluateAsync<string>("element => element.closest('[role=tabpanel]').id");
		Assert.That(await detail.GetByRole(AriaRole.Button, new() { Name = "Save collection", Exact = true }).IsDisabledAsync(), Is.True);
		await detail.GetByLabel("Collection name", new() { Exact = true }).FillAsync("Tidal study");
		await detail.GetByLabel("Owner", new() { Exact = true }).FillAsync("Pat Chen");
		await detail.GetByText("Unsaved collection changes", new() { Exact = true }).WaitForAsync();
		Assert.That(await detail.GetByRole(AriaRole.Button, new() { Name = "Back to collections", Exact = true }).IsDisabledAsync(), Is.True);
		await _page.GetByRole(AriaRole.Button, new() { Name = "Close Tidal study", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Tab, new() { Name = "Creative collections", Exact = true }).ClickAsync();

		// Saving provides a circuit round trip after both rejected leave operations.
		await ApplicationToolbar.GetByRole(AriaRole.Button, new() { Name = "Save collection", Exact = true }).ClickAsync();
		await detail.GetByText("Collection saved. The gallery and table now show these values.", new() { Exact = true }).WaitForAsync();
		Assert.That(await detail.EvaluateAsync<string>("element => element.closest('[role=tabpanel]').id"), Is.EqualTo(panelId));
		Assert.That(await _page.GetByRole(AriaRole.Tab, new() { Name = "Tidal study", Exact = true }).GetAttributeAsync("aria-selected"), Is.EqualTo("true"));
		await detail.GetByRole(AriaRole.Button, new() { Name = "Back to collections", Exact = true }).ClickAsync();
		await gallery.WaitForAsync();
		await gallery.GetByLabel("Search collections", new() { Exact = true }).FillAsync("Tidal study");
		await gallery.GetByRole(AriaRole.Button, new() { Name = "Search", Exact = true }).ClickAsync();
		await gallery.Locator("article[data-collection-id=coastal-notes]").GetByRole(AriaRole.Heading, new() { Name = "Tidal study", Exact = true }).WaitForAsync();
		Assert.That(await gallery.Locator("article[data-collection-id]").CountAsync(), Is.EqualTo(1));
		Assert.That(await gallery.Locator("article[data-collection-id=coastal-notes]").InnerTextAsync(), Does.Contain("Pat Chen"));
		await gallery.GetByRole(AriaRole.Group, new() { Name = "Collection presentation", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Table", Exact = true }).ClickAsync();
		var table = gallery.Locator("#collection-table");
		await table.GetByRole(AriaRole.Cell, new() { Name = "Tidal study", Exact = true }).WaitForAsync();
		Assert.That(await table.GetByRole(AriaRole.Cell, new() { Name = "Pat Chen", Exact = true }).CountAsync(), Is.EqualTo(1));
		Assert.That(await gallery.GetByLabel("Search collections", new() { Exact = true }).InputValueAsync(), Is.EqualTo("Tidal study"));
		await VerifyContainmentAsync();
		await AttachScreenshotAsync($"collection-table-{width}");
		await gallery.GetByRole(AriaRole.Group, new() { Name = "Collection presentation", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Cards", Exact = true }).ClickAsync();
		await gallery.GetByRole(AriaRole.Button, new() { Name = "Open Tidal study", Exact = true }).ClickAsync();
		await detail.WaitForAsync();
		Assert.That(await detail.EvaluateAsync<string>("element => element.closest('[role=tabpanel]').id"), Is.EqualTo(panelId), "Reopening one ID must reuse its retained editor tab.");
		Assert.That(await _page.GetByRole(AriaRole.Tab, new() { Name = "Tidal study", Exact = true }).CountAsync(), Is.EqualTo(1));
		await detail.GetByLabel("Owner", new() { Exact = true }).FillAsync("Discard this owner");
		await ApplicationCommands.GetByRole(AriaRole.Button, new() { Name = "File", Exact = true }).ClickAsync();
		await ApplicationCommands.GetByRole(AriaRole.Group, new() { Name = "File", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Discard changes", Exact = true }).ClickAsync();
		await detail.GetByText("Changes discarded. The last saved values have been restored.", new() { Exact = true }).WaitForAsync();
		Assert.That(await detail.GetByLabel("Owner", new() { Exact = true }).InputValueAsync(), Is.EqualTo("Pat Chen"));
		Assert.That(await ApplicationToolbar.GetByRole(AriaRole.Button, new() { Name = "Save collection", Exact = true }).IsDisabledAsync(), Is.True);
		await VerifyContainmentAsync();
		await AttachScreenshotAsync($"collection-editor-{width}");
		await detail.GetByRole(AriaRole.Button, new() { Name = "Back to collections", Exact = true }).ClickAsync();
		await gallery.GetByRole(AriaRole.Button, new() { Name = "Clear", Exact = true }).ClickAsync();
		await gallery.GetByRole(AriaRole.Button, new() { Name = "Open Field guide", Exact = true }).ClickAsync();
		var otherDetail = ActiveScreen.Locator("[data-showcase=collection-detail][data-collection-id=field-guide]");
		await otherDetail.WaitForAsync();
		Assert.That(await otherDetail.EvaluateAsync<string>("element => element.closest('[role=tabpanel]').id"), Is.Not.EqualTo(panelId));
		Assert.That(await _page.GetByRole(AriaRole.Tab, new() { Name = "Tidal study", Exact = true }).CountAsync(), Is.EqualTo(1));
	}

	[TestCase(false, 1500, 1000)]
	[TestCase(false, 390, 844)]
	[TestCase(true, 1500, 1000)]
	[TestCase(true, 390, 844)]
	public async Task GuidedWizardValidatesReturnsFromDetourChangesBranchAndCommitsOneSnapshot(bool modal, int width, int height) {
		await _page.SetViewportSizeAsync(width, height);
		await OpenPatternScreenAsync("Guided workflows", "[data-demo=guided-wizards]");
		var example = ActiveScreen.Locator("[data-demo=guided-wizards]");
		var launcher = example.GetByRole(AriaRole.Button, new() { Name = modal ? "Open dialog wizard" : "Start embedded wizard", Exact = true });
		await launcher.ClickAsync();
		var host = modal ? _page.Locator("#modern-modal") : example.Locator("section[aria-label='Embedded workspace wizard']");
		await host.GetByRole(AriaRole.Heading, new() { Name = "Name your workspace", Exact = true }).WaitForAsync();
		await host.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
		await host.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Enter a workspace name before continuing." }).WaitForAsync();
		await host.GetByLabel("Workspace name", new() { Exact = true }).FillAsync("Browser workspace");
		await host.GetByRole(AriaRole.Radio, new() { Name = "Team", Exact = true }).CheckAsync();
		await host.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
		await host.GetByRole(AriaRole.Heading, new() { Name = "Make it yours", Exact = true }).WaitForAsync();
		await host.GetByRole(AriaRole.Button, new() { Name = "Choose a preset", Exact = true }).ClickAsync();
		await host.GetByRole(AriaRole.Heading, new() { Name = "Choose a starting preset", Exact = true }).WaitForAsync();
		await host.GetByRole(AriaRole.Button, new() { Name = "Apply preset", Exact = true }).ClickAsync();
		await host.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Choose a preset, or use Back to keep your settings." }).WaitForAsync();
		await host.GetByRole(AriaRole.Group, new() { Name = "Workspace presets", Exact = true })
			.GetByRole(AriaRole.Button).Filter(new() { HasText = "Collaborative" }).ClickAsync();
		await host.GetByRole(AriaRole.Button, new() { Name = "Apply preset", Exact = true }).ClickAsync();
		await host.GetByRole(AriaRole.Heading, new() { Name = "Make it yours", Exact = true }).WaitForAsync();
		Assert.That(await host.GetByLabel("Workspace capacity", new() { Exact = true }).InputValueAsync(), Is.EqualTo("12"));
		Assert.That(await host.GetByLabel("Workspace access", new() { Exact = true }).InputValueAsync(), Is.EqualTo("Anyone in the organization"));
		Assert.That(await example.GetByTestId("workspace-result").InnerTextAsync(), Does.Contain("Successful completions: 0"), "Returning from a Finish-position detour must not complete the parent wizard.");
		await host.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
		await host.GetByRole(AriaRole.Heading, new() { Name = "A team needs reviewers", Exact = true }).WaitForAsync();
		await host.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
		await host.GetByRole(AriaRole.Alert).WaitForAsync();
		await host.GetByLabel("Reviewers", new() { Exact = true }).FillAsync("Alex Morgan, Sam Rivera");
		await host.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
		await host.GetByRole(AriaRole.Heading, new() { Name = "Review your workspace", Exact = true }).WaitForAsync();
		Assert.That(await host.Locator(".workspace-review").InnerTextAsync(), Does.Contain("Alex Morgan, Sam Rivera"));
		await host.GetByRole(AriaRole.Button, new() { Name = "Back", Exact = true }).ClickAsync();
		await host.GetByRole(AriaRole.Heading, new() { Name = "A team needs reviewers", Exact = true }).WaitForAsync();
		Assert.That(await host.GetByLabel("Reviewers", new() { Exact = true }).InputValueAsync(), Is.EqualTo("Alex Morgan, Sam Rivera"));
		await host.GetByRole(AriaRole.Button, new() { Name = "Back", Exact = true }).ClickAsync();
		await host.GetByRole(AriaRole.Heading, new() { Name = "Make it yours", Exact = true }).WaitForAsync();
		await host.GetByRole(AriaRole.Button, new() { Name = "Back", Exact = true }).ClickAsync();
		await host.GetByRole(AriaRole.Heading, new() { Name = "Name your workspace", Exact = true }).WaitForAsync();
		Assert.That(await host.GetByLabel("Workspace name", new() { Exact = true }).InputValueAsync(), Is.EqualTo("Browser workspace"));
		Assert.That(await host.GetByRole(AriaRole.Radio, new() { Name = "Team", Exact = true }).IsCheckedAsync(), Is.True);
		await host.GetByRole(AriaRole.Radio, new() { Name = "Solo", Exact = true }).CheckAsync();
		await host.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
		await host.GetByRole(AriaRole.Heading, new() { Name = "Make it yours", Exact = true }).WaitForAsync();
		await host.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
		await host.GetByRole(AriaRole.Heading, new() { Name = "Review your workspace", Exact = true }).WaitForAsync();
		Assert.That(await host.Locator(".workspace-review dt").AllTextContentsAsync(), Does.Not.Contain("Reviewers"), "Changing to Solo must remove the obsolete Team branch.");
		await host.GetByRole(AriaRole.Button, new() { Name = "Create workspace", Exact = true }).ClickAsync();
		await host.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Confirm that you have reviewed the settings" }).WaitForAsync();
		await host.GetByRole(AriaRole.Checkbox, new() { Name = "I have reviewed these settings", Exact = true }).CheckAsync();
		await VerifyContainmentAsync();
		await AttachScreenshotAsync($"wizard-{(modal ? "modal" : "embedded")}-{width}-review");
		await host.GetByRole(AriaRole.Button, new() { Name = "Create workspace", Exact = true }).ClickAsync();
		await host.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
		await example.GetByTestId("workspace-result").Filter(new() { HasText = "Workspace 'Browser workspace' completed." }).WaitForAsync();
		Assert.That(await example.GetByTestId("workspace-result").InnerTextAsync(), Does.Contain("Successful completions: 1"));
		var snapshot = example.GetByTestId("workspace-snapshot");
		Assert.That(await snapshot.InnerTextAsync(), Does.Contain("Browser workspace").And.Contain("Solo").And.Contain("12 people").And.Contain("Collaborative"));
		Assert.That(await snapshot.Locator("dt").AllTextContentsAsync(), Does.Not.Contain("Reviewers"));
		await launcher.ClickAsync();
		await host.GetByRole(AriaRole.Heading, new() { Name = "Name your workspace", Exact = true }).WaitForAsync();
		Assert.That(await host.GetByLabel("Workspace name", new() { Exact = true }).InputValueAsync(), Is.Empty, "A fresh run must use its own draft.");
		await host.GetByLabel("Workspace name", new() { Exact = true }).FillAsync("Cancel this draft");
		await host.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
		await host.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
		await example.GetByTestId("workspace-result").Filter(new() { HasText = "Setup cancelled. Latest completed workspace kept." }).WaitForAsync();
		Assert.That(await example.GetByTestId("workspace-result").InnerTextAsync(), Does.Contain("Successful completions: 1"));
		Assert.That(await snapshot.InnerTextAsync(), Does.Contain("Browser workspace").And.Not.Contain("Cancel this draft"));
		Assert.That(await _page.Locator(".modal-backdrop").CountAsync(), Is.Zero);
		await VerifyContainmentAsync();
		await AttachScreenshotAsync($"wizard-{(modal ? "modal" : "embedded")}-{width}-completed");
	}

	[Test]
	public async Task BlockIconsCanBrowseMenusOrActivateDefaultScreensWithoutLosingRetainedState() {
		var blocks = _page.GetByRole(AriaRole.Navigation, new() { Name = "Application blocks", Exact = true });
		await blocks.GetByRole(AriaRole.Button, new() { Name = "Workspace", Exact = true }).ClickAsync();
		await _page.GetByRole(AriaRole.Complementary, new() { Name = "Application navigation", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Overview", Exact = true }).ClickAsync();
		await ActiveScreen.GetByRole(AriaRole.Heading, new() { Name = "ApplicationBlock workspace", Exact = true }).WaitForAsync();
		var activation = ActiveScreen.GetByRole(AriaRole.Checkbox, new() { Name = "Activate the block's default screen", Exact = true });
		Assert.That(await activation.IsCheckedAsync(), Is.False);
		await ActiveScreen.GetByRole(AriaRole.Button, new() { Name = "Increment", Exact = true }).ClickAsync();
		await ActiveScreen.GetByText("Counter: 1", new() { Exact = true }).WaitForAsync();
		var panelId = await ActiveScreen.GetAttributeAsync("id");
		await blocks.GetByRole(AriaRole.Button, new() { Name = "Application patterns", Exact = true }).ClickAsync();
		await blocks.GetByRole(AriaRole.Button, new() { Name = "Application patterns", Exact = true }).And(_page.Locator("[aria-current=page]")).WaitForAsync();
		Assert.That(await ActiveScreen.GetAttributeAsync("id"), Is.EqualTo(panelId), "Browsing a block changes its navigation while retaining the active screen.");
		await activation.CheckAsync();
		await ActiveScreen.GetByText("Selecting a block opens its default screen.", new() { Exact = true }).WaitForAsync();
		await blocks.GetByRole(AriaRole.Button, new() { Name = "Workspace", Exact = true }).ClickAsync();
		await ActiveScreen.GetByText("Counter: 1", new() { Exact = true }).WaitForAsync();
		await blocks.GetByRole(AriaRole.Button, new() { Name = "Application patterns", Exact = true }).ClickAsync();
		await ActiveScreen.Locator("[data-showcase=cards]").WaitForAsync();
		Assert.That(await _page.GetByRole(AriaRole.Tab, new() { Name = "Creative collections", Exact = true }).GetAttributeAsync("aria-selected"), Is.EqualTo("true"));
		await blocks.GetByRole(AriaRole.Button, new() { Name = "Workspace", Exact = true }).ClickAsync();
		await ActiveScreen.GetByText("Counter: 1", new() { Exact = true }).WaitForAsync();
		Assert.That(await ActiveScreen.GetAttributeAsync("id"), Is.EqualTo(panelId));
		Assert.That(await activation.IsCheckedAsync(), Is.True);
		await activation.UncheckAsync();
		await AttachScreenshotAsync("block-activation");
	}

	private async Task OpenPatternScreenAsync(string title, string marker) {
		var sidebar = _page.GetByRole(AriaRole.Complementary, new() { Name = "Application navigation", Exact = true });
		if (!await sidebar.IsVisibleAsync())
			await _page.GetByRole(AriaRole.Button, new() { Name = "Menu", Exact = true }).ClickAsync();
		var block = _page.GetByRole(AriaRole.Navigation, new() { Name = "Application blocks", Exact = true })
			.GetByRole(AriaRole.Button, new() { Name = "Application patterns", Exact = true });
		if (await block.GetAttributeAsync("aria-current") != "page")
			await block.ClickAsync();
		if (!await sidebar.IsVisibleAsync())
			await _page.GetByRole(AriaRole.Button, new() { Name = "Menu", Exact = true }).ClickAsync();
		await sidebar.GetByRole(AriaRole.Button, new() { Name = title, Exact = true }).ClickAsync();
		await ActiveScreen.Locator(marker).WaitForAsync();
		var menuToggle = _page.GetByRole(AriaRole.Button, new() { Name = "Menu", Exact = true });
		if (await menuToggle.IsVisibleAsync() && await menuToggle.GetAttributeAsync("aria-expanded") == "true")
			await menuToggle.ClickAsync();
	}

	private async Task CloseDialogAsync() {
		var dialog = _page.Locator("#modern-modal");
		await dialog.Locator(".modal-footer").GetByRole(AriaRole.Button, new() { Name = "Close", Exact = true }).ClickAsync();
		await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
	}

	private async Task WaitForFocusAsync(ILocator element) {
		await element.EvaluateAsync("element => element.setAttribute('data-await-focus', 'true')");
		await _page.WaitForFunctionAsync("() => document.activeElement?.getAttribute('data-await-focus') === 'true'");
		Assert.That(await element.EvaluateAsync<bool>("element => element === document.activeElement"), Is.True);
		await element.EvaluateAsync("element => element.removeAttribute('data-await-focus')");
	}

	private Task WaitForThemeAsync(string theme) => _page.WaitForFunctionAsync("""
		theme => {
			const provider = document.querySelector('[data-sphere10-theme]');
			const link = document.querySelector('link[data-sphere10-theme-stylesheet]');
			if (provider?.getAttribute('data-sphere10-theme') !== theme || link?.getAttribute('data-sphere10-theme-stylesheet') !== theme || !link.sheet || link.sheet.href !== link.href)
				return false;
			return Array.from(link.sheet.cssRules).some(rule => rule.type === CSSRule.STYLE_RULE && rule.selectorText.includes('[data-sphere10-theme=') && provider.matches(rule.selectorText));
		}
		""", theme);

	private async Task VerifyContainmentAsync() {
		var sizes = await _page.EvaluateAsync<double[]>("""
			() => {
				const body = document.querySelector('.sphere10-screen-body');
				return [document.documentElement.scrollWidth, document.documentElement.clientWidth, body.scrollWidth, body.clientWidth];
			}
			""");
		Assert.That(sizes[0], Is.LessThanOrEqualTo(sizes[1] + 1), "The showcase shell must stay inside the viewport.");
		Assert.That(sizes[2], Is.LessThanOrEqualTo(sizes[3] + 1), "Showcase content must fit its pane without horizontal page scrolling.");
	}

	private async Task AttachScreenshotAsync(string phase) {
		var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ShowcaseBrowserTests");
		Tools.FileSystem.CreateDirectory(directory);
		var path = Path.Combine(directory, $"{TestContext.CurrentContext.Test.ID}-{phase}.png");
		await _page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
		TestContext.AddTestAttachment(path, phase);
	}
}
