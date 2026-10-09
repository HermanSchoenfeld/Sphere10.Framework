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
using Sphere10.Framework.Web.AspNetCore.Blazor.Theming;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>Checks Bootstrap integration through the registered, interactive theme component gallery.</summary>
[TestFixture]
[Explicit("Requires BlazorTesterUrl and an installed Chromium or BrowserExecutablePath.")]
[Category("Browser")]
[NonParallelizable]
public class ThemeComponentsBrowserTests {
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
		var context = await _browser.NewContextAsync(new BrowserNewContextOptions { ViewportSize = new ViewportSize { Width = 1500, Height = 1000 } });
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
		Assert.That(_errors, Is.Empty, "The theme gallery must not produce browser or script errors.");
	}

	[Test]
	public async Task BootstrapComponentsUseEachPresetAndRetainInteractiveState([Values] ThemeMode mode, [Values(1500, 390)] int width) {
		await _page.SetViewportSizeAsync(width, width == 390 ? 844 : 1000);
		await _page.GotoAsync(new Uri(_origin, "/components/themes").AbsoluteUri);
		var gallery = _page.Locator(".sphere10-screen:not([hidden]) [data-demo=themes]");
		await gallery.GetByRole(AriaRole.Heading, new() { Name = "Theme component gallery", Exact = true }).WaitForAsync();
		await _page.WaitForFunctionAsync("() => location.pathname === '/application' && new URLSearchParams(location.search).get('screen') === 'themes'");
		await gallery.GetByTestId("theme-counter").ClickAsync();
		await gallery.GetByTestId("theme-state").Filter(new() { HasText = "Clicks: 1" }).WaitForAsync();
		var panel = await gallery.EvaluateAsync<string>("element => element.closest('[role=tabpanel]').id");
		await gallery.GetByRole(AriaRole.Textbox, new() { Name = "Theme draft", Exact = true }).FillAsync("Retained component gallery draft");
		await gallery.GetByTestId("draft-preview").Filter(new() { HasText = "Retained component gallery draft" }).WaitForAsync();
		await gallery.GetByRole(AriaRole.Textbox, new() { Name = "Nested draft", Exact = true }).FillAsync("Retained nested provider draft");
		await gallery.GetByRole(AriaRole.Combobox, new() { Name = "Sample selection", Exact = true }).SelectOptionAsync("Complete");
		await gallery.GetByRole(AriaRole.Tablist, new() { Name = "Sample tabs", Exact = true }).GetByRole(AriaRole.Tab, new() { Name = "Details", Exact = true }).ClickAsync();
		await gallery.GetByRole(AriaRole.Navigation, new() { Name = "Sample pagination", Exact = true }).GetByRole(AriaRole.Button, new() { Name = "2", Exact = true }).ClickAsync();
		await gallery.GetByRole(AriaRole.Button, new() { Name = "Sample menu", Exact = true }).ClickAsync();
		await gallery.GetByRole(AriaRole.Menuitem, new() { Name = "Profile", Exact = true }).ClickAsync();
		await gallery.GetByTestId("theme-state").Filter(new() { HasText = "Choice: Profile · Page: 2" }).WaitForAsync();

		var definition = ThemeCatalog.Get(mode);
		await _page.GetByRole(AriaRole.Combobox, new() { Name = "Theme", Exact = true }).SelectOptionAsync(definition.Id);
		await WaitForThemeAsync(definition.Id);
		await WaitForTransitionsAsync();
		Assert.That(await gallery.GetByRole(AriaRole.Textbox, new() { Name = "Theme draft", Exact = true }).InputValueAsync(), Is.EqualTo("Retained component gallery draft"));
		Assert.That(await gallery.GetByRole(AriaRole.Textbox, new() { Name = "Nested draft", Exact = true }).InputValueAsync(), Is.EqualTo("Retained nested provider draft"));
		Assert.That(await gallery.GetByRole(AriaRole.Combobox, new() { Name = "Sample selection", Exact = true }).InputValueAsync(), Is.EqualTo("Complete"));
		Assert.That(await gallery.GetByRole(AriaRole.Tabpanel, new() { Name = "Sample tab content", Exact = true }).InnerTextAsync(), Does.Contain("Details"));
		Assert.That(await gallery.GetByTestId("theme-state").InnerTextAsync(), Does.Contain("Clicks: 1").And.Contain("Choice: Profile · Page: 2"));
		Assert.That(await gallery.EvaluateAsync<string>("element => element.closest('[role=tabpanel]').id"), Is.EqualTo(panel));

		var roleColors = await ReadRoleColorsAsync(gallery);
		foreach (var colors in roleColors) {
			Assert.That(colors.SolidBackground, Is.EqualTo(colors.ExpectedBackground).Within(1), $"{definition.Name}: {colors.Role} button background");
			Assert.That(colors.SolidForeground, Is.EqualTo(colors.ExpectedForeground).Within(1), $"{definition.Name}: {colors.Role} button text");
			Assert.That(colors.OutlineForeground, Is.EqualTo(colors.ExpectedText).Within(1), $"{definition.Name}: {colors.Role} outline button");
			Assert.That(colors.BadgeBackground, Is.EqualTo(colors.ExpectedBackground).Within(1), $"{definition.Name}: {colors.Role} badge background");
			Assert.That(colors.BadgeForeground, Is.EqualTo(colors.ExpectedForeground).Within(1), $"{definition.Name}: {colors.Role} badge text");
			Assert.That(colors.DisabledBackground, Is.EqualTo(colors.ExpectedBackground).Within(1), $"{definition.Name}: {colors.Role} disabled button");
			Assert.That(colors.TextForeground, Is.EqualTo(colors.ExpectedText).Within(1), $"{definition.Name}: {colors.Role} semantic text utility");
			Assert.That(colors.OutlineContrast, Is.GreaterThanOrEqualTo(4.45), $"{definition.Name}: {colors.Role} outline text must remain readable.");
			Assert.That(colors.ProgressBackground, Is.EqualTo(colors.ExpectedBackground).Within(1), $"{definition.Name}: {colors.Role} progress background");
			Assert.That(colors.ProgressForeground, Is.EqualTo(colors.ExpectedForeground).Within(1), $"{definition.Name}: {colors.Role} progress text");
			Assert.That(colors.AlertContrast, Is.GreaterThanOrEqualTo(4.45), $"{definition.Name}: {colors.Role} alert must remain readable.");
			Assert.That(colors.TableContrast, Is.GreaterThanOrEqualTo(4.45), $"{definition.Name}: {colors.Role} table row must remain readable.");
		}

		var utilities = await ReadUtilityColorsAsync(gallery);
		Assert.That(utilities.Background, Is.EqualTo(utilities.ExpectedBackground).Within(1), "Text and border utilities must not change a primary background.");
		Assert.That(utilities.Foreground, Is.EqualTo(utilities.ExpectedForeground).Within(1), "A background utility must not change the requested semantic text color.");
		Assert.That(utilities.Border, Is.EqualTo(utilities.ExpectedBorder).Within(1), "Border color must remain independent of background and text roles.");
		Assert.That(utilities.ButtonBackground, Is.EqualTo(utilities.ExpectedBackground).Within(1), "A warning text utility must not turn a primary button into a warning button.");
		Assert.That(utilities.ButtonForeground, Is.EqualTo(utilities.ExpectedButtonForeground).Within(1));
		Assert.That(utilities.BodyBackground, Is.EqualTo(utilities.ExpectedBody).Within(1), "Body utilities must follow the theme background.");
		Assert.That(utilities.BodyBackgroundHalf, Is.EqualTo(utilities.ExpectedBodyHalf).Within(1), "Background opacity must compose with the body palette.");
		Assert.That(utilities.LinkForeground, Is.EqualTo(utilities.ExpectedLink).Within(1));
		Assert.That(utilities.LinkForegroundHalf, Is.EqualTo(utilities.ExpectedLinkHalf).Within(1), "Link opacity must retain the semantic color with half opacity.");
		Assert.That(utilities.LinkUnderlineHalf, Is.EqualTo(utilities.ExpectedLinkHalf).Within(1), "Underline opacity must be independent of the link foreground opacity.");

		var primary = gallery.GetByTestId("solid-primary");
		var normalBackground = await primary.EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor");
		await primary.HoverAsync();
		await _page.WaitForFunctionAsync("background => getComputedStyle(document.querySelector('[data-testid=solid-primary]')).backgroundColor !== background", normalBackground);
		Assert.That(await primary.EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor"), Is.Not.EqualTo(normalBackground), "Hover must have a visible theme-aware state.");
		var outline = gallery.GetByTestId("outline-primary");
		await primary.FocusAsync();
		await primary.PressAsync("Tab");
		Assert.That(await outline.EvaluateAsync<bool>("element => element === document.activeElement"), Is.True);
		Assert.That(await outline.EvaluateAsync<string>("element => getComputedStyle(element).boxShadow"), Is.Not.EqualTo("none"), "Keyboard focus must remain visible.");
		Assert.That(await gallery.GetByTestId("disabled-primary").IsEnabledAsync(), Is.False);
		Assert.That(await gallery.GetByTestId("disabled-primary").EvaluateAsync<double>("element => Number(getComputedStyle(element).opacity)"), Is.LessThan(1));
		Assert.That(await gallery.GetByRole(AriaRole.Textbox, new() { Name = "Disabled field", Exact = true }).IsEnabledAsync(), Is.False);

		var surfaces = await ReadSurfacesAsync(gallery);
		Assert.That(surfaces.Card, Is.EqualTo(surfaces.ExpectedSurface).Within(1), "Cards must use the active theme's surface.");
		Assert.That(surfaces.Input, Is.EqualTo(surfaces.ExpectedSurface).Within(1), "Form controls must use the active theme's surface.");
		Assert.That(surfaces.NestedCard, Is.EqualTo(surfaces.Card).Within(1), "Nested providers must preserve scoped palette ownership.");
		Assert.That(surfaces.SemanticCardBackground, Is.EqualTo(roleColors[0].ExpectedBackground).Within(1));
		Assert.That(surfaces.SemanticCardText, Is.EqualTo(roleColors[0].ExpectedForeground).Within(1), "A contextual card must retain its contrast foreground in the body.");
		Assert.That(surfaces.SemanticCardHeading, Is.EqualTo(roleColors[0].ExpectedForeground).Within(1), "Global heading rules must not override a contextual card's foreground.");
		Assert.That(surfaces.SemanticCardHeaderBackground, Is.EqualTo(roleColors[0].ExpectedBackground).Within(1), "Contextual card caps must retain the requested role surface.");
		Assert.That(surfaces.SemanticCardHeaderForeground, Is.EqualTo(roleColors[0].ExpectedForeground).Within(1));
		Assert.That(surfaces.ToastHeaderImage, Is.EqualTo(surfaces.CardHeaderImage), "Toast headers must support the same gradients as card headers.");
		Assert.That(surfaces.PopoverHeaderImage, Is.EqualTo(surfaces.CardHeaderImage), "Popover headers must support the same gradients as card headers.");
		if (mode == ThemeMode.MacAdmin)
			Assert.That(surfaces.CardHeaderImage, Does.Contain("linear-gradient"), "MacAdmin's silver gradient treatment must remain visible.");
		if (definition.IsDark) {
			Assert.That(surfaces.Card, Is.Not.EqualTo(new[] { 255, 255, 255, 255 }));
			Assert.That(surfaces.DisabledInput, Is.Not.EqualTo(new[] { 233, 236, 239, 255 }), "Bootstrap's fixed light disabled background must not leak into dark themes.");
			Assert.That(surfaces.Table, Is.Not.EqualTo(new[] { 255, 255, 255, 255 }));
		}
		await gallery.GetByRole(AriaRole.Button, new() { Name = "Project information", Exact = true }).ClickAsync();
		await gallery.Locator(".accordion-body").WaitForAsync(new() { State = WaitForSelectorState.Hidden });
		Assert.That(await gallery.GetByRole(AriaRole.Button, new() { Name = "Project information", Exact = true }).GetAttributeAsync("aria-expanded"), Is.EqualTo("false"));
		await gallery.GetByRole(AriaRole.Button, new() { Name = "Project information", Exact = true }).ClickAsync();
		await gallery.Locator(".accordion-body").WaitForAsync();
		await gallery.GetByRole(AriaRole.Button, new() { Name = "Sample menu", Exact = true }).ClickAsync();
		await gallery.GetByRole(AriaRole.Menuitem, new() { Name = "Preferences", Exact = true }).ClickAsync();
		await gallery.GetByTestId("theme-state").Filter(new() { HasText = "Choice: Preferences" }).WaitForAsync();
		await gallery.GetByRole(AriaRole.Button, new() { Name = "Open themed dialog", Exact = true }).ClickAsync();
		var dialog = _page.Locator("#modern-modal");
		await dialog.GetByRole(AriaRole.Heading, new() { Name = "Theme preview", Exact = true }).WaitForAsync();
		Assert.That(await dialog.Locator(".modal-content").EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor"),
			Is.EqualTo(await gallery.GetByTestId("sample-card").EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor")), "Framework dialogs must use the same themed surface.");
		Assert.That(await dialog.Locator(".modal-footer").EvaluateAsync<string>("element => getComputedStyle(element).backgroundImage"),
			Is.EqualTo(surfaces.CardHeaderImage), "Modal footers must support gradient cap backgrounds.");
		await dialog.EvaluateAsync("async element => await Promise.all(element.getAnimations({ subtree: true }).map(animation => animation.finished.catch(() => {})))");
		var modalBounds = await dialog.Locator(".modal-content").BoundingBoxAsync();
		Assert.That(modalBounds.X + modalBounds.Width / 2, Is.EqualTo(_page.ViewportSize.Width / 2.0).Within(1), "Dialog content must be horizontally centered.");
		Assert.That(modalBounds.Y + modalBounds.Height / 2, Is.EqualTo(_page.ViewportSize.Height / 2.0).Within(1), "Dialog content must be vertically centered.");
		await dialog.Locator(".modal-footer").GetByRole(AriaRole.Button, new() { Name = "Close", Exact = true }).ClickAsync();
		await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
		var documentSize = await _page.EvaluateAsync<double[]>("() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
		Assert.That(documentSize[0], Is.LessThanOrEqualTo(documentSize[1] + 1), "The full gallery must remain contained on narrow screens.");
		await gallery.GetByRole(AriaRole.Heading, new() { Name = "Theme component gallery", Exact = true }).ScrollIntoViewIfNeededAsync();
		await AttachScreenshotAsync($"{definition.Id}-{width}");
	}

	private Task WaitForTransitionsAsync() => _page.WaitForFunctionAsync("""
		() => {
			const gallery = document.querySelector('.sphere10-screen:not([hidden]) [data-demo=themes]');
			if (!gallery) return false;
			// Flush style/layout before checking Bootstrap's finite color transitions; infinite spinners must not block the check.
			gallery.getBoundingClientRect();
			return !gallery.getAnimations({ subtree: true }).some(animation =>
				(animation.pending || animation.playState === 'running') && Number.isFinite(animation.effect?.getTiming().iterations));
		}
		""");

	private Task WaitForThemeAsync(string theme) => _page.WaitForFunctionAsync("""
		theme => {
			const providers = Array.from(document.querySelectorAll('[data-sphere10-theme]'));
			const links = Array.from(document.querySelectorAll('link[data-sphere10-theme-stylesheet]'));
			return providers.length === 2 && providers.every(provider => provider.getAttribute('data-sphere10-theme') === theme)
				&& links.length === 2 && links.every(link => {
					if (link.getAttribute('data-sphere10-theme-stylesheet') !== theme || !link.sheet || link.sheet.href !== link.href) return false;
					const rules = Array.from(link.sheet.cssRules);
					const isLoaded = sheet => !!sheet && sheet.cssRules.length > 0
						&& Array.from(sheet.cssRules).every(rule => rule.type !== CSSRule.IMPORT_RULE || isLoaded(rule.styleSheet));
					if (!isLoaded(link.sheet)) return false;
					const matching = rules.filter(rule => rule.type === CSSRule.STYLE_RULE && rule.selectorText.includes('[data-sphere10-theme=') && providers[0].matches(rule.selectorText));
					const expected = matching.map(rule => rule.style.getPropertyValue('--sphere10-accent').trim()).filter(Boolean).pop();
					return expected && providers.every(provider => getComputedStyle(provider).getPropertyValue('--sphere10-accent').trim() === expected);
				});
		}
		""", theme);

	private static Task<RoleColors[]> ReadRoleColorsAsync(ILocator gallery) => gallery.EvaluateAsync<RoleColors[]>("""
		gallery => {
			const context = document.createElement('canvas').getContext('2d', { willReadFrequently: true });
			const pixels = color => { if (!CSS.supports('color', color)) throw new Error(`Invalid semantic color: ${color}`); context.clearRect(0, 0, 1, 1); context.fillStyle = color; context.fillRect(0, 0, 1, 1); return Array.from(context.getImageData(0, 0, 1, 1).data); };
			const normalize = color => pixels(color);
			const luminance = color => color.slice(0, 3).map(value => value / 255).map(value => value <= .04045 ? value / 12.92 : ((value + .055) / 1.055) ** 2.4).reduce((sum, value, index) => sum + value * [.2126, .7152, .0722][index], 0);
			const composite = (foreground, background) => {
				const foregroundAlpha = foreground[3] / 255;
				const backgroundAlpha = background[3] / 255;
				const alpha = foregroundAlpha + backgroundAlpha * (1 - foregroundAlpha);
				return alpha ? foreground.slice(0, 3).map((value, index) => (value * foregroundAlpha + background[index] * backgroundAlpha * (1 - foregroundAlpha)) / alpha).concat(alpha * 255) : [0, 0, 0, 0];
			};
			const contrast = element => {
				const style = getComputedStyle(element);
				let background = pixels(style.backgroundColor);
				let ancestor = element.parentElement;
				while (background[3] < 255 && ancestor) {
					background = composite(background, pixels(getComputedStyle(ancestor).backgroundColor));
					ancestor = ancestor.parentElement;
				}
				const foregroundLuminance = luminance(composite(pixels(style.color), background));
				const backgroundLuminance = luminance(background);
				return (Math.max(foregroundLuminance, backgroundLuminance) + .05) / (Math.min(foregroundLuminance, backgroundLuminance) + .05);
			};
			const tokens = getComputedStyle(gallery.closest('.sphere10-theme'));
			return Array.from(gallery.querySelectorAll('[data-role-row]')).map(row => {
				const role = row.getAttribute('data-role-row');
				const token = role === 'primary' ? 'accent' : role;
				const style = name => getComputedStyle(gallery.querySelector(`[data-testid="${name}-${role}"]`));
				return {
					Role: role,
					ExpectedBackground: normalize(tokens.getPropertyValue(`--sphere10-${token}`).trim()),
					ExpectedForeground: normalize(tokens.getPropertyValue(`--sphere10-${token}-contrast`).trim()),
					ExpectedText: normalize(tokens.getPropertyValue(`--sphere10-${token}-text`).trim()),
					TextForeground: normalize(style('text').color),
					SolidBackground: normalize(style('solid').backgroundColor), SolidForeground: normalize(style('solid').color),
					OutlineForeground: normalize(style('outline').color), BadgeBackground: normalize(style('badge').backgroundColor),
					BadgeForeground: normalize(style('badge').color), DisabledBackground: normalize(style('disabled').backgroundColor),
					OutlineContrast: contrast(gallery.querySelector(`[data-testid="outline-${role}"]`)),
					ProgressBackground: normalize(style('progress').backgroundColor), ProgressForeground: normalize(style('progress').color),
					AlertContrast: contrast(gallery.querySelector(`[data-testid="alert-${role}"]`)),
					TableContrast: contrast(gallery.querySelector(`[data-table-role="${role}"] td`))
				};
			});
		}
		""");

	private static Task<SurfaceColors> ReadSurfacesAsync(ILocator gallery) => gallery.EvaluateAsync<SurfaceColors>("""
		gallery => {
			const context = document.createElement('canvas').getContext('2d', { willReadFrequently: true });
			const normalize = color => { if (!CSS.supports('color', color)) throw new Error(`Invalid semantic color: ${color}`); context.clearRect(0, 0, 1, 1); context.fillStyle = color; context.fillRect(0, 0, 1, 1); return Array.from(context.getImageData(0, 0, 1, 1).data); };
			const background = selector => normalize(getComputedStyle(gallery.querySelector(selector)).backgroundColor);
			return {
				ExpectedSurface: normalize(getComputedStyle(gallery.closest('.sphere10-theme')).getPropertyValue('--sphere10-surface').trim()),
				Card: background('[data-testid=sample-card]'), NestedCard: background('[data-testid=nested-card]'),
				Input: background('[aria-label="Theme draft"]'), DisabledInput: background('[aria-label="Disabled field"]'),
				Table: background('[data-testid=sample-table] tbody td'),
				SemanticCardBackground: background('[data-testid=semantic-card]'),
				SemanticCardText: normalize(getComputedStyle(gallery.querySelector('[data-testid=semantic-card] .card-text')).color),
				SemanticCardHeading: normalize(getComputedStyle(gallery.querySelector('[data-testid=semantic-card] .card-title')).color),
				SemanticCardHeaderBackground: background('[data-testid=semantic-card] .card-header'),
				SemanticCardHeaderForeground: normalize(getComputedStyle(gallery.querySelector('[data-testid=semantic-card] .card-header')).color),
				CardHeaderImage: getComputedStyle(gallery.querySelector('[data-testid=sample-card] .card-header')).backgroundImage,
				ToastHeaderImage: getComputedStyle(gallery.querySelector('[data-testid=sample-toast] .toast-header')).backgroundImage,
				PopoverHeaderImage: getComputedStyle(gallery.querySelector('[data-testid=sample-popover] .popover-header')).backgroundImage
			};
		}
		""");

	private static Task<UtilityColors> ReadUtilityColorsAsync(ILocator gallery) => gallery.EvaluateAsync<UtilityColors>("""
		gallery => {
			const context = document.createElement('canvas').getContext('2d', { willReadFrequently: true });
			const normalize = color => { if (!CSS.supports('color', color)) throw new Error(`Invalid semantic color: ${color}`); context.clearRect(0, 0, 1, 1); context.fillStyle = color; context.fillRect(0, 0, 1, 1); return Array.from(context.getImageData(0, 0, 1, 1).data); };
			const tokens = getComputedStyle(gallery.closest('.sphere10-theme'));
			const expected = token => normalize(tokens.getPropertyValue(token).trim());
			const utility = getComputedStyle(gallery.querySelector('[data-testid=combined-utilities]'));
			const button = getComputedStyle(gallery.querySelector('[data-testid=combined-button]'));
			return {
				ExpectedBackground: expected('--sphere10-accent'), ExpectedForeground: expected('--sphere10-danger-text'),
				ExpectedBorder: expected('--sphere10-warning'), ExpectedButtonForeground: expected('--sphere10-warning-text'),
				Background: normalize(utility.backgroundColor), Foreground: normalize(utility.color), Border: normalize(utility.borderTopColor),
				ButtonBackground: normalize(button.backgroundColor), ButtonForeground: normalize(button.color),
				ExpectedBody: expected('--sphere10-background'), ExpectedBodyHalf: expected('--sphere10-background').map((value, index) => index === 3 ? Math.round(value * .5) : value),
				BodyBackground: normalize(getComputedStyle(gallery.querySelector('[data-testid=body-background]')).backgroundColor),
				BodyBackgroundHalf: normalize(getComputedStyle(gallery.querySelector('[data-testid=body-background-half]')).backgroundColor),
				ExpectedLink: expected('--sphere10-accent-text'), ExpectedLinkHalf: expected('--sphere10-accent-text').map((value, index) => index === 3 ? Math.round(value * .5) : value),
				LinkForeground: normalize(getComputedStyle(gallery.querySelector('[data-testid=semantic-link]')).color),
				LinkForegroundHalf: normalize(getComputedStyle(gallery.querySelector('[data-testid=semantic-link-half]')).color),
				LinkUnderlineHalf: normalize(getComputedStyle(gallery.querySelector('[data-testid=semantic-link-half]')).textDecorationColor)
			};
		}
		""");

	private async Task AttachScreenshotAsync(string phase) {
		var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ThemeComponentsBrowserTests");
		Tools.FileSystem.CreateDirectory(directory);
		var path = Path.Combine(directory, $"{TestContext.CurrentContext.Test.ID}-{phase}.png");
		await _page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
		TestContext.AddTestAttachment(path, phase);
	}

	public sealed class RoleColors {
		public string Role { get; set; }

		public int[] ExpectedBackground { get; set; }

		public int[] ExpectedForeground { get; set; }

		public int[] ExpectedText { get; set; }

		public int[] TextForeground { get; set; }

		public int[] SolidBackground { get; set; }

		public int[] SolidForeground { get; set; }

		public int[] OutlineForeground { get; set; }

		public int[] BadgeBackground { get; set; }

		public int[] BadgeForeground { get; set; }

		public int[] DisabledBackground { get; set; }

		public int[] ProgressBackground { get; set; }

		public int[] ProgressForeground { get; set; }

		public double OutlineContrast { get; set; }

		public double AlertContrast { get; set; }

		public double TableContrast { get; set; }
	}

	public sealed class UtilityColors {
		public int[] ExpectedBackground { get; set; }

		public int[] ExpectedForeground { get; set; }

		public int[] ExpectedBorder { get; set; }

		public int[] ExpectedButtonForeground { get; set; }

		public int[] Background { get; set; }

		public int[] Foreground { get; set; }

		public int[] Border { get; set; }

		public int[] ButtonBackground { get; set; }

		public int[] ButtonForeground { get; set; }

		public int[] ExpectedBody { get; set; }

		public int[] ExpectedBodyHalf { get; set; }

		public int[] BodyBackground { get; set; }

		public int[] BodyBackgroundHalf { get; set; }

		public int[] ExpectedLink { get; set; }

		public int[] ExpectedLinkHalf { get; set; }

		public int[] LinkForeground { get; set; }

		public int[] LinkForegroundHalf { get; set; }

		public int[] LinkUnderlineHalf { get; set; }
	}

	public sealed class SurfaceColors {
		public int[] ExpectedSurface { get; set; }

		public int[] Card { get; set; }

		public int[] NestedCard { get; set; }

		public int[] Input { get; set; }

		public int[] DisabledInput { get; set; }

		public int[] Table { get; set; }

		public int[] SemanticCardBackground { get; set; }

		public int[] SemanticCardText { get; set; }

		public int[] SemanticCardHeading { get; set; }

		public int[] SemanticCardHeaderBackground { get; set; }

		public int[] SemanticCardHeaderForeground { get; set; }

		public string CardHeaderImage { get; set; }

		public string ToastHeaderImage { get; set; }

		public string PopoverHeaderImage { get; set; }
	}
}
