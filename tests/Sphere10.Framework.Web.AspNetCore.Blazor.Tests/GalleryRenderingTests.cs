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
	[TestCase(typeof(Sphere10.Framework.Utils.BlazorTester.Pages.OverviewPage), "/", "Sphere10 Blazor demos")]
	[TestCase(typeof(Servers), "/servers", "Endpoint sample")]
	[TestCase(typeof(Sphere10.Framework.Utils.BlazorTester.WidgetGallery.WidgetGallery), "/widget-gallery", "Legacy plugin gallery")]
	[TestCase(typeof(Modals), "/widget-gallery/modals", "Info Modal")]
	[TestCase(typeof(Tables), "/widget-gallery/tables", "Virtual paged table")]
	[TestCase(typeof(Wizards), "/widget-gallery/wizards", "New Widget")]
	[TestCase(typeof(Sphere10.Framework.Utils.BlazorTester.Modern.UI.Index), "/modern", "Editable grid")]
	[TestCase(typeof(Sphere10.Framework.Utils.BlazorTester.Modern.UI.Index), "/components/grid", "Editable grid")]
	[TestCase(typeof(Sphere10.Framework.Utils.BlazorTester.Pages.TablesPage), "/components/tables", "Virtual paged table")]
	[TestCase(typeof(Sphere10.Framework.Utils.BlazorTester.Pages.DialogsPage), "/components/dialogs", "Awaited dialogs")]
	[TestCase(typeof(Sphere10.Framework.Utils.BlazorTester.Pages.WizardsPage), "/components/wizards", "Branching wallet wizard")]
	[TestCase(typeof(Sphere10.Framework.Utils.BlazorTester.Application.Workspace), "/application", "Application workspace")]
	[TestCase(typeof(Home), "/legacy/dashboard", "Legacy dashboard")]
	[TestCase(typeof(NotFound), "/not-found", "Page Not Found")]
	public async Task GalleryRouteRendersWithItsLayout(Type componentType, string route, string expectedText) {
		var services = new ServiceCollection();
		services.AddLogging();
		Program.ConfigureServices(services);
		services.AddSingleton<NavigationManager>(new TestNavigationManager("http://localhost/", "http://localhost" + route));
		services.AddSingleton<IJSRuntime, TestJsRuntime>();
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var html = await renderer.Dispatcher.InvokeAsync(async () => {
			var parameters = ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(RouteView.RouteData)] = new RouteData(componentType, new Dictionary<string, object>()),
				[nameof(RouteView.DefaultLayout)] = typeof(DemoLayout)
			});
			var rendered = await renderer.RenderComponentAsync<RouteView>(parameters);
			return rendered.ToHtmlString();
		});
		Assert.That(html, Does.Contain(expectedText));
		Assert.That(Regex.Matches(html, "data-testid=\"demo-shell\"").Count, Is.EqualTo(1), "Every route has one common demo shell.");
		Assert.That(Regex.Matches(html, "id=\"legacy-modal\"").Count, Is.EqualTo(1), "The shared shell owns the original modal host.");
		Assert.That(Regex.Matches(html, "id=\"modern-modal\"").Count, Is.EqualTo(1), "The shared shell owns the modern modal host.");
		Assert.That(Regex.Matches(html, "<label[^>]*class=\"sphere10-theme-selector(?: |\")").Count, Is.EqualTo(1), "Only the common header owns the theme selector.");
		Assert.That(html, Does.Contain("data-sphere10-theme=\"classic-blue\""));
		Assert.That(html, Does.Contain("aria-label=\"Demo navigation\""));
		Assert.That(html, Does.Contain("aria-label=\"Endpoint\""));
		Assert.That(html, Does.Contain("Classic blue"));
		Assert.That(html, Does.Contain("Application blocks"));
		Assert.That(html, Does.Not.Contain("modern/css/light.css"));
		Assert.That(html, Does.Not.Contain("modern/css/dark.css"));
		Assert.That(html, Does.Not.Contain("// Copyright"), "Source notices must remain Razor comments.");
	}

	[Test]
	public void DemoNavigationMapsEveryLinkToOneActiveRouteAndMeaningfulIcon() {
		var routes = typeof(Program).Assembly.GetTypes()
			.SelectMany(type => type.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>().Select(route => (route.Template, Type: type)))
			.ToLookup(route => route.Template, route => route.Type);
		var links = DemoNavigation.Groups.SelectMany(group => group.Links).ToArray();
		Assert.That(DemoNavigation.Groups.Select(group => group.Title), Is.EqualTo(new[] { "Overview", "Components", "Workspace", "Legacy examples" }));
		Assert.That(links.Select(link => link.Href), Is.Unique);
		foreach (var link in links) {
			Assert.That(routes[link.Href].Count(), Is.EqualTo(1), link.Href);
			Assert.That(link.Icon, Does.StartWith("fas fa-"), link.Title);
		}
		Assert.That(routes["/modern"].Single(), Is.EqualTo(routes["/components/grid"].Single()), "The original URL remains a grid alias.");
		Assert.That(links.Single(link => link.Href == "/components/grid").Icon, Is.EqualTo("fas fa-table"));
		Assert.That(links.Single(link => link.Href == "/components/tables").Icon, Is.EqualTo("fas fa-list-alt"));
		Assert.That(links.Single(link => link.Href == "/components/dialogs").Icon, Is.EqualTo("fas fa-comment-alt"));
		Assert.That(links.Single(link => link.Href == "/components/wizards").Icon, Is.EqualTo("fas fa-magic"));
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
		var components = catalog.Get("components").Menus.SelectMany(menu => menu.Items).ToDictionary(item => item.Id);
		Assert.That(components.Keys, Is.EquivalentTo(new[] { "gallery", "tables", "dialogs", "wizards" }));
		Assert.That(components["gallery"], Is.TypeOf<BlazorScreenMenuItem>());
		Assert.That(((BlazorScreenMenuItem)components["gallery"]).ScreenType, Is.EqualTo(typeof(Sphere10.Framework.Utils.BlazorTester.Application.ComponentScreen)));
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
