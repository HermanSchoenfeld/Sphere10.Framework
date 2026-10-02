// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.BlazorTester;
using Sphere10.Framework.Utils.BlazorTester.Application;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.Models;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.Services;
using Sphere10.Framework.Web.AspNetCore.Blazor.Services;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;
using Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Category("Integration")]
[Parallelizable(ParallelScope.Children)]
public class DemoPluginRegistrationTests {
	[Test]
	public void StartupValidatesAndRegistersModernPluginsWithoutLegacyNavigationServices() {
		using var provider = CreateProvider();
		var plugins = provider.GetServices<IBlazorPlugin>().ToArray();
		var catalog = provider.GetRequiredService<IBlazorApplicationBlockCatalog>();
		var registeredBlocks = provider.GetServices<IBlazorApplicationBlock>().ToArray();

		Assert.That(plugins.Select(plugin => plugin.Name), Is.EquivalentTo(new[] { "Sphere10.Framework", "Widget Gallery", "Screen policies" }));
		Assert.That(plugins.Single(plugin => plugin.Name == "Sphere10.Framework").Blocks.Select(block => block.Id), Is.EqualTo(new[] { "workspace" }));
		Assert.That(plugins.Single(plugin => plugin.Name == "Widget Gallery").Blocks.Select(block => block.Id), Is.EqualTo(new[] { "components", "legacy" }));
		Assert.That(registeredBlocks.Select(block => block.Id), Is.EquivalentTo(new[] { "workspace", "components", "legacy", "empty-workspace" }));
		Assert.That(catalog.Blocks.Select(block => block.Id), Is.EqualTo(new[] { "workspace", "components", "legacy", "empty-workspace" }));
		Assert.That(provider.GetService<IBlazorRoutedPluginLocator>(), Is.Null);
		Assert.That(provider.GetService<IBlazorRoutedPluginManager>(), Is.Null);
		Assert.That(provider.GetService<IBlazorRoutedApplicationManager>(), Is.Null);
	}

	[TestCase("components", "gallery")]
	[TestCase("legacy", "gallery")]
	public async Task GalleryMenusCreateNewInstancesWhileSingletonDemonstrationsRemainExplicit(string blockId, string screenId) {
		using var provider = CreateProvider();
		using var scope = provider.CreateScope();
		var host = scope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var first = await host.ActivateScreenAsync(blockId, screenId);
		var second = await host.ActivateScreenAsync(blockId, screenId);
		Assert.That(second.Id, Is.Not.EqualTo(first.Id));
		Assert.That(first.MenuItem.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));

		var editor = await host.ActivateScreenAsync("workspace", "editor");
		Assert.That(await host.ActivateScreenAsync("workspace", "editor"), Is.SameAs(editor));
		Assert.That(editor.MenuItem.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
	}

	[Test]
	public void WidgetPluginRegistersWorkingTransientRandomAndValidationServices() {
		using var provider = CreateProvider();
		using var scope = provider.CreateScope();
		var random = scope.ServiceProvider.GetRequiredService<IRandomNumberService>();
		var validator = scope.ServiceProvider.GetRequiredService<IValidator<NewWidgetModel>>();

		Assert.That(random.GetRandomNumber(), Is.GreaterThanOrEqualTo(0));
		Assert.That(random, Is.Not.SameAs(scope.ServiceProvider.GetRequiredService<IRandomNumberService>()));
		Assert.That(validator, Is.Not.SameAs(scope.ServiceProvider.GetRequiredService<IValidator<NewWidgetModel>>()));
		Assert.That(validator.Validate(new NewWidgetModel()).IsValid, Is.False);
		Assert.That(validator.Validate(new NewWidgetModel { Name = "Widget", Description = "Integration sample", Price = 12.50m }).IsValid, Is.True);
	}

	[Test]
	public async Task WorkspaceActionsResolveStateFromTheExecutingScope() {
		using var provider = CreateProvider();
		using var first = provider.CreateScope();
		using var second = provider.CreateScope();
		var firstHost = first.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var secondHost = second.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var firstState = first.ServiceProvider.GetRequiredService<WorkspaceStatus>();
		var secondState = second.ServiceProvider.GetRequiredService<WorkspaceStatus>();

		await firstHost.ExecuteMenuItemAsync("workspace", "action");
		Assert.That(firstState.ActionCount, Is.EqualTo(1));
		Assert.That(secondState.ActionCount, Is.Zero);
		await secondHost.ExecuteMenuItemAsync("workspace", "action");
		await firstHost.ExecuteMenuItemAsync("workspace", "action");
		Assert.That(firstState.ActionCount, Is.EqualTo(2));
		Assert.That(secondState.ActionCount, Is.EqualTo(1));
		Assert.That(firstState, Is.Not.SameAs(secondState));
	}

	[Test]
	public async Task ScreenHostsShareDefinitionsAndKeepSelectionsAndSessionsSeparate() {
		using var provider = CreateProvider();
		using var first = provider.CreateScope();
		using var second = provider.CreateScope();
		var firstHost = first.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var secondHost = second.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var firstSession = await firstHost.ActivateBlockAsync("workspace");

		Assert.That(secondHost.ActiveScreen, Is.Null);
		Assert.That(secondHost.OpenScreens, Is.Empty);
		var secondSession = await secondHost.ActivateBlockAsync("workspace");
		await firstHost.ActivateBlockAsync("components");
		Assert.That(firstHost, Is.Not.SameAs(secondHost));
		Assert.That(firstHost.Catalog, Is.SameAs(secondHost.Catalog));
		Assert.That(firstSession.Id, Is.Not.EqualTo(secondSession.Id));
		Assert.That(firstHost.ActiveBlock.Id, Is.EqualTo("components"));
		Assert.That(secondHost.ActiveBlock.Id, Is.EqualTo("workspace"));
		Assert.That(secondHost.ActiveScreen, Is.SameAs(secondSession));
		Assert.That(firstHost.OpenScreens, Has.Length.EqualTo(2));
		Assert.That(secondHost.OpenScreens, Has.Length.EqualTo(1));
	}

	[Test]
	public async Task EndpointSelectionAndAddedEndpointsStayWithinTheirScope() {
		using var provider = CreateProvider();
		using var first = provider.CreateScope();
		using var second = provider.CreateScope();
		var firstEndpoints = first.ServiceProvider.GetRequiredService<IEndpointManager>();
		var secondEndpoints = second.ServiceProvider.GetRequiredService<IEndpointManager>();
		var originalEndpoint = secondEndpoints.Endpoint;
		var addedEndpoint = new Uri("https://scope-one.example.test/");

		await firstEndpoints.AddEndpointAsync(addedEndpoint);
		await firstEndpoints.SetCurrentEndpointAsync(addedEndpoint);
		Assert.That(firstEndpoints, Is.SameAs(first.ServiceProvider.GetRequiredService<IEndpointManager>()));
		Assert.That(firstEndpoints, Is.Not.SameAs(secondEndpoints));
		Assert.That(firstEndpoints.Endpoint, Is.EqualTo(addedEndpoint));
		Assert.That(firstEndpoints.Endpoints, Does.Contain(addedEndpoint));
		Assert.That(secondEndpoints.Endpoint, Is.EqualTo(originalEndpoint));
		Assert.That(secondEndpoints.Endpoints, Does.Not.Contain(addedEndpoint));
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
