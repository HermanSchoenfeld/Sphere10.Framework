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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.BlazorTester;
using Sphere10.Framework.Utils.BlazorTester.Layouts;
using Sphere10.Framework.Utils.BlazorTester.Loader.Pages;
using Sphere10.Framework.Utils.BlazorTester.Modern.UI.Controls;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.Pages;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Tables;
using Sphere10.Framework.Web.AspNetCore.Blazor.Services;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid;

using ModernWizard = Sphere10.Framework.Web.AspNetCore.Blazor.Wizard;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class GalleryRenderingTests {
	[TestCase("/", "workspace", "overview", "ApplicationBlock workspace")]
	[TestCase("/application", "workspace", "overview", "ApplicationBlock workspace")]
	[TestCase("/components/grid", "components", "gallery", "Editable grid")]
	[TestCase("/modern", "components", "gallery", "Editable grid")]
	[TestCase("/components/tables", "components", "tables", "Virtual paged table")]
	[TestCase("/components/dialogs", "components", "dialogs", "Awaited dialogs")]
	[TestCase("/components/wizards", "components", "wizards", "Branching wallet wizard")]
	[TestCase("/widget-gallery", "legacy", "gallery", "Legacy plugin gallery")]
	[TestCase("/widget-gallery/modals", "legacy", "dialogs", "Info Modal")]
	[TestCase("/widget-gallery/tables", "legacy", "tables", "Virtual paged table")]
	[TestCase("/widget-gallery/wizards", "legacy", "wizards", "New Widget")]
	[TestCase("/servers", "legacy", "servers", "Endpoint sample")]
	[TestCase("/legacy/dashboard", "legacy", "dashboard", "Legacy dashboard")]
	public async Task EveryDemoAliasRendersItsRegisteredScreenInOneApplicationShell(string route, string blockId, string screenId, string expectedText) {
		var services = new ServiceCollection().AddLogging();
		Program.ConfigureServices(services);
		services.AddSingleton<NavigationManager>(new TestNavigationManager("http://localhost/", "http://localhost" + route));
		services.AddSingleton<IJSRuntime, TestJsRuntime>();
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		var html = await renderer.Dispatcher.InvokeAsync(async () => {
			var parameters = ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(RouteView.RouteData)] = new RouteData(typeof(Sphere10.Framework.Utils.BlazorTester.Application.Workspace), new Dictionary<string, object>()),
				[nameof(RouteView.DefaultLayout)] = typeof(DemoLayout)
			});
			var rendered = await renderer.RenderComponentAsync<RouteView>(parameters);
			return rendered.ToHtmlString();
		});
		Assert.That(host.ActiveBlock.Id, Is.EqualTo(blockId));
		Assert.That(host.ActiveScreen.MenuItem.Id, Is.EqualTo(screenId));
		Assert.That(host.ActiveScreen.Screen, Is.Not.Null, "Alias content must be an attached application screen.");
		Assert.That(html, Does.Contain(expectedText));
		Assert.That(Regex.Matches(html, "class=\"sphere10-application(?: |\")").Count, Is.EqualTo(1), "Every demo has one application shell.");
		Assert.That(Regex.Matches(html, "aria-label=\"Application navigation\"").Count, Is.EqualTo(1));
		Assert.That(Regex.Matches(html, "aria-label=\"Application blocks\"").Count, Is.EqualTo(1));
		Assert.That(Regex.Matches(html, "aria-label=\"Application commands\"").Count, Is.EqualTo(1));
		Assert.That(Regex.Matches(html, "aria-label=\"Application toolbar\"").Count, Is.EqualTo(1));
		Assert.That(Regex.Matches(html, "id=\"legacy-modal\"").Count, Is.EqualTo(1));
		Assert.That(Regex.Matches(html, "id=\"modern-modal\"").Count, Is.EqualTo(1));
		Assert.That(Regex.Matches(html, "<label[^>]*class=\"sphere10-theme-selector(?: |\")").Count, Is.EqualTo(1));
		Assert.That(html, Does.Contain("data-sphere10-theme=\"classic-blue\"").And.Contain("aria-label=\"Endpoint\""));
		Assert.That(html, Does.Contain("sphere10-tool-content").And.Contain("aria-label=\"Demo user\""));
		Assert.That(html, Does.Not.Contain("class=\"demo-sidebar\"").And.Not.Contain("aria-label=\"Demo navigation\""));
		Assert.That(html, Does.Not.Contain("modern/css/light.css").And.Not.Contain("modern/css/dark.css"));
		Assert.That(html, Does.Not.Contain("// Copyright"), "Source notices must remain Razor comments.");
	}

	[Test]
	public void DemoAliasesBelongToOneRoutableWorkspaceAndResolveRegisteredScreens() {
		var routes = typeof(Program).Assembly.GetTypes()
			.SelectMany(type => type.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>().Select(route => (route.Template, Type: type)))
			.ToLookup(route => route.Template, route => route.Type);
		var services = new ServiceCollection().AddLogging();
		Program.ConfigureServices(services);
		using var provider = services.BuildServiceProvider();
		var catalog = provider.GetRequiredService<IBlazorApplicationBlockCatalog>();
		Assert.That(DemoNavigation.Aliases.Select(alias => alias.Route), Is.Unique);
		foreach (var alias in DemoNavigation.Aliases) {
			Assert.That(routes[alias.Route].ToArray(), Is.EqualTo(new[] { typeof(Sphere10.Framework.Utils.BlazorTester.Application.Workspace) }), alias.Route);
			var screen = catalog.Get(alias.BlockId).Menus.SelectMany(menu => menu.Items).OfType<BlazorScreenMenuItem>().Single(item => item.Id == alias.ScreenId);
			Assert.That(screen.Icon, Does.StartWith("fas fa-"), alias.Route);
		}
		Assert.That(routes["/modern"].Single(), Is.EqualTo(routes["/components/grid"].Single()));
	}

	[Test]
	public async Task NotFoundUsesProvidersWithoutCreatingAnotherApplicationShell() {
		var services = new ServiceCollection().AddLogging();
		Program.ConfigureServices(services);
		services.AddSingleton<NavigationManager>(new TestNavigationManager("http://localhost/", "http://localhost/not-found"));
		services.AddSingleton<IJSRuntime, TestJsRuntime>();
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var html = await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<RouteView>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(RouteView.RouteData)] = new RouteData(typeof(NotFound), new Dictionary<string, object>()),
				[nameof(RouteView.DefaultLayout)] = typeof(DemoLayout)
			}));
			return rendered.ToHtmlString();
		});
		Assert.That(html, Does.Contain("Page Not Found"));
		Assert.That(html, Does.Not.Contain("class=\"sphere10-application\""));
	}

	[Test]
	public void WorkspacePreservesStableScreenIdsAndSuppliesIconsForEveryMenuItem() {
		var services = new ServiceCollection().AddLogging();
		Program.ConfigureServices(services);
		using var provider = services.BuildServiceProvider();
		var catalog = provider.GetRequiredService<IBlazorApplicationBlockCatalog>();
		var plugins = provider.GetServices<IBlazorPlugin>().ToArray();
		Assert.That(plugins.SelectMany(plugin => plugin.Blocks).Select(block => block.Id), Is.EquivalentTo(catalog.Blocks.Select(block => block.Id)),
			"The registered demo plugins must supply the workspace blocks through the existing catalog.");
		var workspace = catalog.Get("workspace").Menus.SelectMany(menu => menu.Items).ToDictionary(item => item.Id);
		Assert.That(workspace.Keys, Is.EquivalentTo(new[] { "overview", "editor", "scratchpad", "action" }));
		Assert.That(workspace["overview"].Icon, Is.EqualTo("fas fa-home"));
		Assert.That(workspace["editor"].Icon, Is.EqualTo("fas fa-edit"));
		Assert.That(workspace["scratchpad"].Icon, Is.EqualTo("fas fa-sticky-note"));
		Assert.That(workspace["action"].Icon, Is.EqualTo("fas fa-play"));
		Assert.That(((BlazorScreenMenuItem)workspace["editor"]).ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(((BlazorScreenMenuItem)workspace["scratchpad"]).ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		var components = catalog.Get("components").Menus.SelectMany(menu => menu.Items).ToDictionary(item => item.Id);
		Assert.That(components.Keys, Is.EquivalentTo(new[] { "gallery", "tables", "dialogs", "wizards" }));
		Assert.That(components["gallery"], Is.TypeOf<BlazorScreenMenuItem>());
		Assert.That(((BlazorScreenMenuItem)components["gallery"]).ScreenType, Is.EqualTo(typeof(Sphere10.Framework.Utils.BlazorTester.Application.ComponentScreen)));
		var legacy = catalog.Get("legacy").Menus.SelectMany(menu => menu.Items).ToDictionary(item => item.Id);
		Assert.That(legacy.Keys, Is.EquivalentTo(new[] { "gallery", "dialogs", "tables", "wizards", "servers", "dashboard" }));
		Assert.That(legacy.Values, Is.All.InstanceOf<BlazorScreenMenuItem>());
		Assert.That(catalog.Blocks.SelectMany(block => block.Menus).SelectMany(menu => menu.Items).Select(item => item.Icon), Is.All.Not.Null.And.Not.Empty);
	}

	[TestCase(typeof(Sphere10.Framework.Utils.BlazorTester.Demos.GridDemo))]
	[TestCase(typeof(Sphere10.Framework.Utils.BlazorTester.Demos.TablesDemo))]
	[TestCase(typeof(Sphere10.Framework.Utils.BlazorTester.Demos.DialogsDemo))]
	[TestCase(typeof(Sphere10.Framework.Utils.BlazorTester.Demos.WizardsDemo))]
	public void ReusableDemosDoNotOwnRoutesOrLayouts(Type demoType) {
		Assert.That(demoType.GetCustomAttributes(typeof(RouteAttribute), true), Is.Empty);
		Assert.That(demoType.GetCustomAttributes(typeof(LayoutAttribute), true), Is.Empty);
	}
	[Test]
	public void FrameworkRegistrationsResolveGenericComponentsAndIsolateModalSessions() {
		var services = new ServiceCollection().AddSphere10Blazor();
		using var provider = services.BuildServiceProvider();
		using var first = provider.CreateScope();
		using var second = provider.CreateScope();
		Assert.That(first.ServiceProvider.GetRequiredService<PagedTableViewModel<int>>(), Is.Not.Null);
		Assert.That(first.ServiceProvider.GetRequiredService<RapidTableViewModel<int>>(), Is.Not.Null);
		Assert.That(first.ServiceProvider.GetRequiredService<VirtualPagedTableViewModel<int>>(), Is.Not.Null);
		Assert.That(first.ServiceProvider.GetRequiredService<IBlazorWizardBuilder<object>>(), Is.Not.Null);
		Assert.That(first.ServiceProvider.GetRequiredService<ModernWizard.IBlazorWizardBuilder<object>>(), Is.Not.Null);
		Assert.That(first.ServiceProvider.GetRequiredService<IModalService>(), Is.Not.SameAs(second.ServiceProvider.GetRequiredService<IModalService>()));
		Assert.That(first.ServiceProvider.GetRequiredService<Modal.ModalService>(), Is.Not.SameAs(second.ServiceProvider.GetRequiredService<Modal.ModalService>()));
		Assert.That(first.ServiceProvider.GetRequiredService<Modal.ViewService>(), Is.Not.SameAs(second.ServiceProvider.GetRequiredService<Modal.ViewService>()));
	}

	[Test]
	public async Task ModernGridPagesAndRefreshesLocalData() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IJSRuntime, TestJsRuntime>();
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var dataSource = new TestClassDataSource();
		BlazorGrid<TestClass> grid = null;
		await renderer.Dispatcher.InvokeAsync(async () => {
			var parameters = ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(GridHost.DataSource)] = dataSource,
				[nameof(GridHost.Capture)] = (Action<BlazorGrid<TestClass>>)(instance => grid = instance)
			});
			var rendered = await renderer.RenderComponentAsync<GridHost>(parameters);
			Assert.That(grid.TotalDataCount, Is.EqualTo(73));
			Assert.That(grid.TotalPages, Is.EqualTo(8));
			await grid.SetPageAsync(7);
			Assert.That(grid.CurrentPage, Is.EqualTo(7));
			Assert.That(rendered.ToHtmlString(), Does.Contain("73"));
			var item = dataSource.New();
			item.Name = "Created by regression test";
			dataSource.Create(item);
			await grid.RefreshAsync();
			Assert.That(grid.TotalDataCount, Is.EqualTo(74));
			Assert.That(rendered.ToHtmlString(), Does.Contain(item.Name));
			dataSource.Delete(item);
			await grid.RefreshAsync();
			Assert.That(grid.TotalDataCount, Is.EqualTo(73));
		});
	}

	[Test]
	public async Task WorkspaceActionUpdatesTheRetainedOverview() {
		var services = new ServiceCollection();
		services.AddLogging();
		Program.ConfigureServices(services);
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			await host.ActivateBlockAsync("workspace");
			var rendered = await renderer.RenderComponentAsync<UI.Application.ApplicationScreenHostView>();
			await host.ExecuteMenuItemAsync("workspace", "action");
			Assert.That(rendered.ToHtmlString(), Does.Contain("Scoped async action executions: 1"));
		});
	}
	public class GridHost : ComponentBase {
		[Parameter] public TestClassDataSource DataSource { get; set; }

		[Parameter] public Action<BlazorGrid<TestClass>> Capture { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<BlazorGrid<TestClass>>(0);
			builder.AddAttribute(1, nameof(BlazorGrid<TestClass>.DataSource), DataSource);
			builder.AddAttribute(2, nameof(BlazorGrid<TestClass>.PageSize), 10);
			builder.AddComponentReferenceCapture(3, instance => Capture((BlazorGrid<TestClass>)instance));
			builder.CloseComponent();
		}
	}

	public class TestJsRuntime : IJSRuntime {
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) => ValueTask.FromResult(default(TValue));

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) => ValueTask.FromResult(default(TValue));
	}
}
