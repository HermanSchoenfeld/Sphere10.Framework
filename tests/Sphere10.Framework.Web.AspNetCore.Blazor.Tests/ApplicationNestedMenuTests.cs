// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.MainFrame;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationNestedMenuTests {
	[Test]
	public async Task NestedGroupExpandsOnClickAndSelectingAChildClosesTheWholeDropdown() {
		var site = Command("site", "Ridge North");
		var active = Group("active", "Active Sites", site);
		var selections = new List<string>();
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationCommandBar bar = null;
			var rendered = await renderer.RenderComponentAsync<ApplicationCommandStateTests.CommandBarHarness>(Parameters(
				new[] { active }, component => bar = component,
				EventCallback.Factory.Create<IBlazorApplicationMenuItem>(new object(), item => selections.Add(item.Id))));
			Invoke(bar, "ToggleMenu", "menu:views");
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Contain("Active Sites").And.Not.Contain("Ridge North"));
			Invoke(bar, "ToggleSubmenu", "menu:views/active");
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Contain("role=\"group\" aria-label=\"Active Sites\"").And.Contain("Ridge North").And.Contain("aria-controls="));
			Assert.That(selections, Is.Empty);
			await (Task)Invoke(bar, "ExecuteAsync", site);
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(selections, Is.EqualTo(new[] { "site" }));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("Ridge North").And.Not.Contain("Dismiss application menu"));
			Assert.That(Field<string>(bar, "_openSubmenu"), Is.Null);
		});
	}

	[TestCase("Escape")]
	[TestCase("ArrowLeft")]
	public async Task KeyboardClosesTheInnermostGroupAndRestoresItsOpenerFirst(string key) {
		var site = Command("site", "Ridge North");
		var region = Group("region", "Northern region", site);
		var active = Group("active", "Active Sites", region);
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationCommandBar bar = null;
			var rendered = await renderer.RenderComponentAsync<ApplicationCommandStateTests.CommandBarHarness>(Parameters(new[] { active }, component => bar = component));
			Invoke(bar, "ToggleMenu", "menu:views");
			Invoke(bar, "ToggleSubmenu", "menu:views/active");
			Invoke(bar, "ToggleSubmenu", "menu:views/active/region");
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Contain("Ridge North"));
			Invoke(bar, "KeyDown", new KeyboardEventArgs { Key = key });
			Assert.That(Field<string>(bar, "_focusMenu"), Is.EqualTo("menu:views/active/region"));
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Contain("Northern region").And.Not.Contain("Ridge North"));
			Assert.That(Field<string>(bar, "_openMenu"), Is.EqualTo("menu:views"));
			Invoke(bar, "KeyDown", new KeyboardEventArgs { Key = "Escape" });
			Assert.That(Field<string>(bar, "_focusMenu"), Is.EqualTo("menu:views/active"));
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Contain("Active Sites").And.Not.Contain("Northern region"));
			Invoke(bar, "KeyDown", new KeyboardEventArgs { Key = "Escape" });
			Assert.That(Field<string>(bar, "_focusMenu"), Is.EqualTo("menu:views"));
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("Dismiss application menu"));
		});
	}

	[Test]
	public async Task ArrowRightEntersFirstEnabledChildAndEmptyPlaceholderRemainsDisabled() {
		var unavailable = Command("unavailable", "Not available", false);
		var site = Command("site", "Ridge North");
		var active = Group("active", "Active Sites", unavailable, site);
		var empty = Group("inactive", "Inactive Sites", Command("empty", "No inactive sites", false));
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationCommandBar bar = null;
			var rendered = await renderer.RenderComponentAsync<ApplicationCommandStateTests.CommandBarHarness>(Parameters(new[] { active, empty }, component => bar = component));
			Invoke(bar, "ToggleMenu", "menu:views");
			Invoke(bar, "SubmenuKeyDown", new KeyboardEventArgs { Key = "ArrowRight" }, "menu:views/active");
			Assert.That(Field<string>(bar, "_focusMenu"), Is.EqualTo("menu:views/active/site"));
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Contain("Ridge North"));
			Invoke(bar, "SubmenuKeyDown", new KeyboardEventArgs { Key = "ArrowRight" }, "menu:views/inactive");
			Assert.That(Field<string>(bar, "_focusMenu"), Is.EqualTo("menu:views/inactive"));
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(Regex.IsMatch(rendered.ToHtmlString(), "<button\\b[^>]*\\bdisabled[^>]*>\\s*No inactive sites\\s*</button>"), Is.True);
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("Ridge North"));
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task RefreshingMenuDataDropsRemovedOrDisabledExpandedGroups(bool disableGroup) {
		var active = Group("active", "Active Sites", Command("site", "Ridge North"));
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationCommandBar bar = null;
			var rendered = await renderer.RenderComponentAsync<ApplicationCommandStateTests.CommandBarHarness>(Parameters(new[] { active }, component => bar = component));
			Invoke(bar, "ToggleMenu", "menu:views");
			Invoke(bar, "ToggleSubmenu", "menu:views/active");
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Contain("Ridge North"));
			var replacement = disableGroup
				? new BlazorApplicationMenuItemBuilder()
					.WithId("active")
					.WithText("Active Sites")
					.EnabledWhen(() => false)
					.WithChildren(Command("site", "Ridge North"))
					.Build()
				: Command("all", "All Projects");
			await bar.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationCommandBar.Menus)] = new[] { Menu(replacement) }
			}));
			Assert.That(Field<string>(bar, "_openSubmenu"), Is.Null);
			Assert.That(Field<string>(bar, "_focusMenu"), Is.EqualTo("menu:views"));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("Ridge North"));
			Invoke(bar, "ToggleSubmenu", "menu:views/active");
			Assert.That(Field<string>(bar, "_openSubmenu"), Is.Null);
		});
	}

	[Test]
	public async Task PathSeparatorsInIdsDoNotCollideWithNestedMenuIdentity() {
		var first = Group("active", "Active Sites", Command("site", "Ridge North"));
		var second = Group("active/north", "Regional Sites", Command("site", "Valley West"));
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationCommandBar bar = null;
			var rendered = await renderer.RenderComponentAsync<ApplicationCommandStateTests.CommandBarHarness>(Parameters(new[] { first, second }, component => bar = component));
			await bar.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationCommandBar.Menus)] = new[] {
					Menu(first, second),
					new BlazorApplicationMenu { Id = "views/active", Text = "Other Views", Items = new[] { Command("site", "Other project") } }
				}
			}));
			Invoke(bar, "ToggleMenu", "menu:views");
			Invoke(bar, "ToggleSubmenu", "menu:views/active%2Fnorth");
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Contain("Valley West").And.Not.Contain("Ridge North").And.Not.Contain("Other project"));
			Invoke(bar, "ToggleMenu", "menu:views%2Factive");
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Contain("Other project").And.Not.Contain("Valley West"));
			Assert.That(Field<string>(bar, "_openSubmenu"), Is.Null);
		});
	}

	[Test]
	public async Task OutsideDismissalAlsoClearsTheExpandedBranch() {
		var active = Group("active", "Active Sites", Command("site", "Ridge North"));
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationCommandBar bar = null;
			var rendered = await renderer.RenderComponentAsync<ApplicationCommandStateTests.CommandBarHarness>(Parameters(new[] { active }, component => bar = component));
			Invoke(bar, "ToggleMenu", "menu:views");
			Invoke(bar, "ToggleSubmenu", "menu:views/active");
			await bar.SetParametersAsync(ParameterView.Empty);
			Invoke(bar, "CloseMenu");
			Invoke(bar, "ToggleMenu", "menu:views");
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Contain("Active Sites").And.Not.Contain("Ridge North"));
			Assert.That(Field<string>(bar, "_openSubmenu"), Is.Null);
		});
	}

	private static IBlazorApplicationMenuItem Command(string id, string title, bool enabled = true) =>
		new BlazorApplicationMenuItemBuilder()
			.WithId(id)
			.WithText(title)
			.EnabledWhen(() => enabled)
			.WithAction((_, _) => Task.CompletedTask)
			.Build();

	private static IBlazorApplicationMenuItem Group(string id, string title, params IBlazorApplicationMenuItem[] children) =>
		new BlazorApplicationMenuItemBuilder()
			.WithId(id)
			.WithText(title)
			.WithChildren(children)
			.Build();

	private static IBlazorApplicationMenu Menu(params IBlazorApplicationMenuItem[] items) =>
		new BlazorApplicationMenu(ownsItems: false) { Id = "views", Text = "Views", Items = items };

	private static ParameterView Parameters(IBlazorApplicationMenuItem[] items, Action<ApplicationCommandBar> capture, EventCallback<IBlazorApplicationMenuItem> select = default) =>
		ParameterView.FromDictionary(new Dictionary<string, object> {
			[nameof(ApplicationCommandStateTests.CommandBarHarness.Menus)] = new[] { Menu(items) },
			[nameof(ApplicationCommandStateTests.CommandBarHarness.Items)] = Array.Empty<IBlazorApplicationMenuItem>(),
			[nameof(ApplicationCommandStateTests.CommandBarHarness.Capture)] = capture,
			[nameof(ApplicationCommandStateTests.CommandBarHarness.Select)] = select
		});

	private static object Invoke(object component, string method, params object[] arguments) =>
		component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, arguments);

	private static T Field<T>(object component, string name) =>
		(T)component.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(component);
}
