// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Utils.BlazorTester;
using Sphere10.Framework.Utils.BlazorTester.Loader.Components;
using Sphere10.Framework.Utils.BlazorTester.Loader.ViewModels;
using Sphere10.Framework.Web.AspNetCore.Blazor.Services;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Category("Integration")]
[Parallelizable(ParallelScope.Children)]
public class EndpointSelectorTests {
	[Test]
	public void ReturnedEndpointArraysCannotAlterSharedSelectionOrAvailableEndpoints() {
		using var provider = CreateProvider();
		using var scope = provider.CreateScope();
		var manager = scope.ServiceProvider.GetRequiredService<IEndpointManager>();
		var selector = scope.ServiceProvider.GetRequiredService<SidebarBrandViewModel>();
		var servers = scope.ServiceProvider.GetRequiredService<ServersViewModel>();
		var original = manager.Endpoint;
		manager.Endpoints[0] = new Uri("https://not-registered.example.test/");
		selector.Endpoints[0] = null;
		servers.Servers[0] = null;

		Assert.That(manager.Endpoint, Is.EqualTo(original));
		Assert.That(manager.Endpoints, Is.EqualTo(new[] { original }));
		Assert.That(selector.Endpoints, Is.EqualTo(new[] { original }));
		Assert.That(servers.Servers, Is.EqualTo(new[] { original }));
	}

	[Test]
	public void ModernPluginRegistersSelectorAndOriginalBlockIcons() {
		using var provider = CreateProvider();
		using var scope = provider.CreateScope();
		var selector = scope.ServiceProvider.GetRequiredService<SidebarBrandViewModel>();
		var manager = scope.ServiceProvider.GetRequiredService<IEndpointManager>();
		var catalog = scope.ServiceProvider.GetRequiredService<IBlazorApplicationBlockCatalog>();

		Assert.That(selector.Endpoint, Is.EqualTo(manager.Endpoint));
		Assert.That(catalog.Get("workspace").IconUrl, Is.EqualTo("img/heading-solid.svg"));
		Assert.That(catalog.Get("workspace").Tooltip, Is.EqualTo("Workspace"));
		Assert.That(catalog.Get("components").IconUrl, Is.EqualTo("img/boxes-solid.svg"));
		Assert.That(catalog.Get("components").Tooltip, Is.EqualTo("Component gallery"));
	}

	[Test]
	public async Task SelectorAndServersPageShareChangesInBothDirections() {
		using var provider = CreateProvider();
		using var scope = provider.CreateScope();
		using var otherScope = provider.CreateScope();
		var selector = scope.ServiceProvider.GetRequiredService<SidebarBrandViewModel>();
		var servers = scope.ServiceProvider.GetRequiredService<ServersViewModel>();
		var otherSelector = otherScope.ServiceProvider.GetRequiredService<SidebarBrandViewModel>();
		var original = selector.Endpoint;
		var added = new Uri("https://added-from-servers.example.test/");
		var selectorChanges = 0;
		var pageChanges = 0;
		selector.StateHasChangedDelegate = () => selectorChanges++;
		servers.StateHasChangedDelegate = () => pageChanges++;

		servers.NewServer.Uri = added.AbsoluteUri;
		await servers.OnAddNewServerAsync();
		Assert.That(selector.Endpoints, Does.Contain(added));
		Assert.That(selectorChanges, Is.GreaterThan(0), "An endpoint added on /servers must refresh the existing selector.");
		await selector.OnSelectEndpointAsync(added);
		Assert.That(servers.ActiveServer, Is.EqualTo(added));
		Assert.That(pageChanges, Is.GreaterThan(0));
		await servers.OnSelectActiveServer(original);
		Assert.That(selector.Endpoint, Is.EqualTo(original));
		Assert.That(otherSelector.Endpoints, Does.Not.Contain(added), "The selector must not share endpoint changes with another circuit.");
		Assert.That(otherSelector.Endpoint, Is.EqualTo(original));
	}

	[Test]
	public async Task RenderedSelectorUpdatesFromBackgroundEndpointNotifications() {
		await using var provider = CreateProvider();
		await using var scope = provider.CreateAsyncScope();
		await using var renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		var manager = scope.ServiceProvider.GetRequiredService<IEndpointManager>();
		var rendered = await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<SidebarBrand>());
		var initialHtml = await renderer.Dispatcher.InvokeAsync(rendered.ToHtmlString);

		Assert.That(initialHtml, Does.Contain("aria-label=\"Endpoint\""));
		Assert.That(initialHtml, Does.Contain("href=\"application?block=legacy&amp;screen=servers\""));
		Assert.That(initialHtml, Does.Contain("Manage endpoints"));
		Assert.That(initialHtml, Does.Contain("src=\"img/brand-icon.svg\""));
		Assert.That(initialHtml, Does.Not.Contain("data-toggle"));
		var added = new Uri("https://background.example.test/a-long-endpoint-name/");
		await Task.Run(async () => {
			await manager.AddEndpointAsync(added);
			await manager.SetCurrentEndpointAsync(added);
		});
		var updatedHtml = await renderer.Dispatcher.InvokeAsync(rendered.ToHtmlString);

		Assert.That(updatedHtml, Does.Contain(added.AbsoluteUri));
		Assert.That(Regex.IsMatch(updatedHtml, "<option(?=[^>]*value=\"" + Regex.Escape(added.AbsoluteUri) + "\")(?=[^>]*selected)[^>]*>"), Is.True,
			"The current endpoint must be selected after a notification from outside the renderer.");
	}

	[Test]
	public async Task DisposedSelectorStopsReceivingEndpointNotifications() {
		using var provider = CreateProvider();
		using var scope = provider.CreateScope();
		var manager = scope.ServiceProvider.GetRequiredService<IEndpointManager>();
		var selector = scope.ServiceProvider.GetRequiredService<SidebarBrandViewModel>();
		var changes = 0;
		selector.StateHasChangedDelegate = () => changes++;
		var first = new Uri("https://before-disposal.example.test/");
		await manager.AddEndpointAsync(first);
		await manager.SetCurrentEndpointAsync(first);
		Assert.That(changes, Is.EqualTo(2));

		selector.Dispose();
		selector.Dispose();
		var second = new Uri("https://after-disposal.example.test/");
		await manager.AddEndpointAsync(second);
		await manager.SetCurrentEndpointAsync(second);
		Assert.That(changes, Is.EqualTo(2));
	}

	private static ServiceProvider CreateProvider() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<NavigationManager>(new TestNavigationManager());
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		Program.ConfigureServices(services);
		return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
	}
}