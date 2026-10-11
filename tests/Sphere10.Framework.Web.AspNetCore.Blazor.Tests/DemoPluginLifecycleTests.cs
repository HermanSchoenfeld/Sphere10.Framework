// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.BlazorTester;
using Sphere10.Framework.Utils.BlazorTester.Application;
using Sphere10.Framework.Utils.BlazorTester.Lifecycle;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Category("Integration")]
[Parallelizable(ParallelScope.Children)]
public class DemoPluginLifecycleTests {
	[Test]
	public async Task StartupRegistersIndependentNamedTasksForEveryLoadedPluginWithoutExecutingThem() {
		var services = CreateServices();
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
		await using var firstScope = provider.CreateAsyncScope();
		await using var secondScope = provider.CreateAsyncScope();
		var first = firstScope.ServiceProvider.GetRequiredService<IApplication>();
		var second = secondScope.ServiceProvider.GetRequiredService<IApplication>();
		var pluginNames = first.LoadedPlugins.Select(plugin => plugin.Name).ToArray();
		var firstStatus = firstScope.ServiceProvider.GetRequiredService<WorkspaceStatus>();
		var secondStatus = secondScope.ServiceProvider.GetRequiredService<WorkspaceStatus>();
		var initializerKeys = services.Where(descriptor => descriptor.ServiceType == typeof(IApplicationInitializer) && descriptor.IsKeyedService)
			.Select(descriptor => descriptor.ServiceKey).ToArray();
		var finalizerKeys = services.Where(descriptor => descriptor.ServiceType == typeof(IApplicationFinalizer) && descriptor.IsKeyedService)
			.Select(descriptor => descriptor.ServiceKey).ToArray();

		Assert.That(pluginNames, Is.EqualTo(new[] { "Sphere10.Framework", "Widget Gallery", "Application patterns", "Screen policies" }));
		Assert.That(initializerKeys, Is.EqualTo(pluginNames));
		Assert.That(finalizerKeys, Is.EqualTo(pluginNames));
		Assert.That(second.LoadedPlugins, Is.EqualTo(first.LoadedPlugins));
		foreach (var pluginName in pluginNames) {
			var firstInitializer = firstScope.ServiceProvider.GetKeyedServices<IApplicationInitializer>(pluginName).Single();
			var secondInitializer = secondScope.ServiceProvider.GetKeyedServices<IApplicationInitializer>(pluginName).Single();
			var firstFinalizer = firstScope.ServiceProvider.GetKeyedServices<IApplicationFinalizer>(pluginName).Single();
			var secondFinalizer = secondScope.ServiceProvider.GetKeyedServices<IApplicationFinalizer>(pluginName).Single();
			Assert.That(firstInitializer, Is.TypeOf<DemoPluginInitializer>());
			Assert.That(secondInitializer, Is.TypeOf<DemoPluginInitializer>().And.Not.SameAs(firstInitializer));
			Assert.That(firstFinalizer, Is.TypeOf<DemoPluginFinalizer>());
			Assert.That(secondFinalizer, Is.TypeOf<DemoPluginFinalizer>().And.Not.SameAs(firstFinalizer));
		}
		Assert.That(firstScope.ServiceProvider.GetKeyedServices<IApplicationInitializer>(typeof(IApplication)), Is.Empty);
		Assert.That(firstScope.ServiceProvider.GetKeyedServices<IApplicationFinalizer>(typeof(IApplication)), Is.Empty);
		Assert.That(firstStatus.InitializedPlugins, Is.Empty);
		Assert.That(firstStatus.FinalizedPlugins, Is.Empty);
		Assert.That(secondStatus.InitializedPlugins, Is.Empty);
		Assert.That(secondStatus.FinalizedPlugins, Is.Empty);
		Assert.That(first.IsInitialized, Is.False);
		Assert.That(second.IsInitialized, Is.False);
		await firstScope.DisposeAsync();
		await secondScope.DisposeAsync();
		Assert.That(firstStatus.FinalizedPlugins, Is.Empty, "Disposing an unopened circuit must not run demo shutdown tasks.");
		Assert.That(secondStatus.FinalizedPlugins, Is.Empty);
	}

	[Test]
	public async Task PrerenderingTheActualWorkspaceDoesNotExecutePluginLifecycleTasks() {
		await using var provider = CreateProvider();
		await using var scope = provider.CreateAsyncScope();
		var application = scope.ServiceProvider.GetRequiredService<IApplication>();
		var status = scope.ServiceProvider.GetRequiredService<WorkspaceStatus>();
		await using (var renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>())) {
			var html = await renderer.Dispatcher.InvokeAsync(async () => {
				var rendered = await renderer.RenderComponentAsync<Workspace>();
				return rendered.ToHtmlString();
			});
			Assert.That(html, Does.Contain("ApplicationBlock workspace"));
			Assert.That(html, Does.Match("data-plugin-initializer-count[^>]*>Plugins initialized: 0</span>"));
			Assert.That(application.IsInitialized, Is.False);
			Assert.That(status.InitializedPlugins, Is.Empty);
			Assert.That(status.FinalizedPlugins, Is.Empty);
		}
		await scope.DisposeAsync();
		Assert.That(status.InitializedPlugins, Is.Empty);
		Assert.That(status.FinalizedPlugins, Is.Empty, "A prerender scope never started the interactive application lifecycle.");
	}

	[Test]
	public async Task InitializedWorkspaceDisplaysTheCompletedPluginTaskCount() {
		await using var provider = CreateProvider();
		await using var scope = provider.CreateAsyncScope();
		var application = scope.ServiceProvider.GetRequiredService<IApplication>();
		var status = scope.ServiceProvider.GetRequiredService<WorkspaceStatus>();
		var handler = scope.ServiceProvider.GetServices<CircuitHandler>().Single();
		await handler.OnCircuitOpenedAsync(null, CancellationToken.None);
		await using var renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		var html = await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<Workspace>();
			return rendered.ToHtmlString();
		});

		Assert.That(status.InitializedPlugins, Is.EqualTo(application.LoadedPlugins.Select(plugin => plugin.Name).ToArray()));
		Assert.That(html, Does.Match("data-plugin-initializer-count[^>]*>Plugins initialized: 4</span>"));
	}

	[Test]
	public async Task CircuitCallbacksRunEachPluginsTasksOnceAndKeepTheirStateWithinTheCircuit() {
		await using var provider = CreateProvider();
		await using var firstScope = provider.CreateAsyncScope();
		await using var secondScope = provider.CreateAsyncScope();
		var first = firstScope.ServiceProvider.GetRequiredService<IApplication>();
		var second = secondScope.ServiceProvider.GetRequiredService<IApplication>();
		var firstStatus = firstScope.ServiceProvider.GetRequiredService<WorkspaceStatus>();
		var secondStatus = secondScope.ServiceProvider.GetRequiredService<WorkspaceStatus>();
		var firstHandler = firstScope.ServiceProvider.GetServices<CircuitHandler>().Single();
		var secondHandler = secondScope.ServiceProvider.GetServices<CircuitHandler>().Single();
		var pluginNames = first.LoadedPlugins.Select(plugin => plugin.Name).ToArray();

		await firstHandler.OnCircuitOpenedAsync(null, CancellationToken.None);
		await firstHandler.OnCircuitOpenedAsync(null, CancellationToken.None);
		Assert.That(first.IsInitialized, Is.True);
		Assert.That(firstStatus.InitializedPlugins, Is.EqualTo(pluginNames));
		Assert.That(firstStatus.FinalizedPlugins, Is.Empty);
		Assert.That(second.IsInitialized, Is.False);
		Assert.That(secondStatus.InitializedPlugins, Is.Empty);
		await firstHandler.OnConnectionDownAsync(null, CancellationToken.None);
		await firstHandler.OnConnectionUpAsync(null, CancellationToken.None);
		Assert.That(firstStatus.InitializedPlugins, Is.EqualTo(pluginNames));
		Assert.That(firstStatus.FinalizedPlugins, Is.Empty, "A temporary disconnect must retain every plugin's circuit state.");

		await secondHandler.OnCircuitOpenedAsync(null, CancellationToken.None);
		Assert.That(second.IsInitialized, Is.True);
		Assert.That(secondStatus, Is.Not.SameAs(firstStatus));
		Assert.That(secondStatus.InitializedPlugins, Is.EqualTo(pluginNames));
		await firstHandler.OnCircuitClosedAsync(null, new CancellationToken(true));
		await firstHandler.OnCircuitClosedAsync(null, CancellationToken.None);
		Assert.That(first.IsInitialized, Is.False);
		Assert.That(firstStatus.FinalizedPlugins, Is.EqualTo(pluginNames));
		Assert.That(secondStatus.FinalizedPlugins, Is.Empty);
		Assert.That(second.IsInitialized, Is.True);
		await firstScope.DisposeAsync();
		Assert.That(firstStatus.FinalizedPlugins, Is.EqualTo(pluginNames), "Scope disposal must not repeat tasks already completed by circuit closure.");

		await secondHandler.OnCircuitClosedAsync(null, CancellationToken.None);
		Assert.That(secondStatus.FinalizedPlugins, Is.EqualTo(pluginNames));
		Assert.That(second.IsInitialized, Is.False);
		var initializedSnapshot = secondStatus.InitializedPlugins;
		var finalizedSnapshot = secondStatus.FinalizedPlugins;
		initializedSnapshot[0] = "Changed by caller";
		finalizedSnapshot[0] = "Changed by caller";
		Assert.That(secondStatus.InitializedPlugins, Is.EqualTo(pluginNames));
		Assert.That(secondStatus.FinalizedPlugins, Is.EqualTo(pluginNames));
	}

	[Test]
	public async Task ApplicationExitAwaitsEveryPluginsFinalizerBeforeCircuitTeardownAndDoesNotRepeatThem() {
		await using var provider = CreateProvider();
		await using var scope = provider.CreateAsyncScope();
		var application = scope.ServiceProvider.GetRequiredService<IApplication>();
		var status = scope.ServiceProvider.GetRequiredService<WorkspaceStatus>();
		var handler = scope.ServiceProvider.GetServices<CircuitHandler>().Single();
		var pluginNames = application.LoadedPlugins.Select(plugin => plugin.Name).ToArray();
		await handler.OnCircuitOpenedAsync(null, CancellationToken.None);
		Assert.That(status.FinalizedPlugins, Is.Empty);

		Assert.That(await application.ExitAsync(force: true), Is.True);
		Assert.That(application.HasExited, Is.True);
		Assert.That(application.IsInitialized, Is.False);
		Assert.That(status.InitializedPlugins, Is.EqualTo(pluginNames));
		Assert.That(status.FinalizedPlugins, Is.EqualTo(pluginNames), "Exit must return only after every demo finalizer has finished.");
		Assert.That(await application.ExitAsync(force: true), Is.True);
		await handler.OnCircuitClosedAsync(null, CancellationToken.None);
		await scope.DisposeAsync();
		Assert.That(status.FinalizedPlugins, Is.EqualTo(pluginNames), "Repeated exit, circuit closure and container disposal must preserve once-only shutdown.");
	}

	private static ServiceCollection CreateServices() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<NavigationManager>(new TestNavigationManager("http://localhost/", "http://localhost/application"));
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		Program.ConfigureServices(services);
		return services;
	}

	private static ServiceProvider CreateProvider()
		=> CreateServices().BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
}
