// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
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
using Sphere10.Framework.Utils.BlazorTester.Loader.Layouts;
using Sphere10.Framework.Utils.BlazorTester.Loader.Pages;
using Sphere10.Framework.Utils.BlazorTester.Modern.UI.Controls;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.Pages;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Tables;
using Sphere10.Framework.Web.AspNetCore.Blazor.Services;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class GalleryRenderingTests {
	[TestCase(typeof(Home), "/", "Home")]
	[TestCase(typeof(Servers), "/servers", "Servers")]
	[TestCase(typeof(Sphere10.Framework.Utils.BlazorTester.WidgetGallery.WidgetGallery), "/widget-gallery", "Widget")]
	[TestCase(typeof(Modals), "/widget-gallery/modals", "Info Modal")]
	[TestCase(typeof(Tables), "/widget-gallery/tables", "Virtual paged table")]
	[TestCase(typeof(Wizards), "/widget-gallery/wizards", "New Widget")]
	[TestCase(typeof(Sphere10.Framework.Utils.BlazorTester.Modern.UI.Index), "/modern", "Editable grid")]
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
				[nameof(RouteView.DefaultLayout)] = typeof(MainLayout)
			});
			var rendered = await renderer.RenderComponentAsync<RouteView>(parameters);
			return rendered.ToHtmlString();
		});
		Assert.That(html, Does.Contain(expectedText));
		Assert.That(html, Does.Contain("id=\"modal\""), "Every gallery layout must provide a modal host.");
		Assert.That(html, Does.Not.Contain("// Copyright"), "Source notices must remain Razor comments.");
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
		Assert.That(first.ServiceProvider.GetRequiredService<IWizardBuilder<object>>(), Is.Not.Null);
		Assert.That(first.ServiceProvider.GetRequiredService<Logic.Wizard.IWizardBuilder<object>>(), Is.Not.Null);
		Assert.That(first.ServiceProvider.GetRequiredService<IModalService>(), Is.Not.SameAs(second.ServiceProvider.GetRequiredService<IModalService>()));
		Assert.That(first.ServiceProvider.GetRequiredService<Logic.Modal.ModalService>(), Is.Not.SameAs(second.ServiceProvider.GetRequiredService<Logic.Modal.ModalService>()));
		Assert.That(first.ServiceProvider.GetRequiredService<Logic.Modal.ViewService>(), Is.Not.SameAs(second.ServiceProvider.GetRequiredService<Logic.Modal.ViewService>()));
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
		var host = provider.GetRequiredService<Logic.IApplicationScreenHost>();
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
