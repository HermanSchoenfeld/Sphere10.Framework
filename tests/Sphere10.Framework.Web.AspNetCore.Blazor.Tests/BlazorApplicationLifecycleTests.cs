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
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlazorApplicationLifecycleTests {
	[TestCase(false)]
	[TestCase(true)]
	public async Task CircuitLifecycleUsesFreshKeyedTasksWithoutRunningProcessTasks(bool configureApplication) {
		var plugin = CreatePlugin();
		var loaded = 0;
		var unloaded = 0;
		plugin.Loaded += () => loaded++;
		plugin.Unloaded += () => unloaded++;
		var services = new ServiceCollection().AddSphere10BlazorPlugin(plugin);
		var logger = new NoOpLogger();
		services.AddSingleton<Sphere10.Framework.ILogger>(logger);
		if (configureApplication)
			services.AddSphere10BlazorApplication(application => application.SetToolBarItems(Array.Empty<IApplicationMenuItem>()));
		services.AddSphere10Blazor();
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var firstScope = provider.CreateAsyncScope();
		await using var secondScope = provider.CreateAsyncScope();
		var first = firstScope.ServiceProvider.GetRequiredService<IApplication>();
		var second = secondScope.ServiceProvider.GetRequiredService<IApplication>();
		var firstState = firstScope.ServiceProvider.GetRequiredService<LifecycleState>();
		var secondState = secondScope.ServiceProvider.GetRequiredService<LifecycleState>();
		var firstHandler = firstScope.ServiceProvider.GetServices<CircuitHandler>().Single();
		var secondHandler = secondScope.ServiceProvider.GetServices<CircuitHandler>().Single();
		Assert.That(first.Logger, Is.SameAs(logger));
		Assert.That(first.IsInitialized, Is.False);
		Assert.That(second.IsInitialized, Is.False);
		Assert.That(firstState.Initializations, Is.Zero);
		Assert.That(firstScope.ServiceProvider.GetKeyedServices<IApplicationInitializer>(typeof(IApplication)), Is.Empty);
		Assert.That(firstScope.ServiceProvider.GetKeyedServices<IApplicationFinalizer>(typeof(IApplication)), Is.Empty);
		var resourceDisposals = 0;
		first.Disposables.Add(Tools.Scope.ExecuteOnDispose(() => resourceDisposals++));

		await firstHandler.OnCircuitOpenedAsync(null, CancellationToken.None);
		await firstHandler.OnCircuitOpenedAsync(null, CancellationToken.None);
		Assert.That(first.IsInitialized, Is.True);
		Assert.That(firstState.Initializations, Is.EqualTo(1));
		Assert.That(secondState.Initializations, Is.Zero);
		Assert.That(first.ScreenHost.ActiveScreen, Is.Null, "Application tasks must not change the shell's screen startup policy.");
		await firstHandler.OnConnectionDownAsync(null, CancellationToken.None);
		await firstHandler.OnConnectionUpAsync(null, CancellationToken.None);
		Assert.That(firstState.Initializations, Is.EqualTo(1));
		Assert.That(firstState.Finalizations, Is.Zero, "A temporary disconnect retains the application.");

		await secondHandler.OnCircuitOpenedAsync(null, CancellationToken.None);
		Assert.That(firstState.Initializer, Is.Not.SameAs(secondState.Initializer));
		Assert.That(firstState.Finalizer, Is.Not.SameAs(secondState.Finalizer));
		await firstHandler.OnCircuitClosedAsync(null, new CancellationToken(true));
		await firstHandler.OnCircuitClosedAsync(null, CancellationToken.None);
		Assert.That(firstState.Finalizations, Is.EqualTo(1));
		Assert.That(resourceDisposals, Is.EqualTo(1));
		Assert.That(secondState.Initializations, Is.EqualTo(1));
		Assert.That(secondState.Finalizations, Is.Zero);
		await first.DisposeAsync();
		Assert.That(firstState.Finalizations, Is.EqualTo(1), "Container disposal must not repeat circuit finalization.");
		Assert.That(resourceDisposals, Is.EqualTo(1));
		await secondHandler.OnCircuitClosedAsync(null, CancellationToken.None);
		Assert.That(secondState.Finalizations, Is.EqualTo(1));
		Assert.That(firstState.ProcessInitializations + secondState.ProcessInitializations, Is.Zero);
		Assert.That(firstState.ProcessFinalizations + secondState.ProcessFinalizations, Is.Zero);
		Assert.That(loaded, Is.EqualTo(1));
		Assert.That(unloaded, Is.Zero, "Circuit tasks do not own shared plugin lifetime.");
	}

	[Test]
	public async Task SameLifecycleTaskTypesHaveSeparatePluginInstancesInEveryCircuit() {
		var services = new ServiceCollection();
		services.AddScoped<LifecycleState>();
		services
			.AddSphere10BlazorPlugin(plugin => plugin
				.WithName("First")
				.RegisterInitializer<CircuitInitializer>()
				.RegisterFinalizer<CircuitFinalizer>()
			)
			.AddSphere10BlazorPlugin(plugin => plugin
				.WithName("Second")
				.RegisterInitializer<CircuitInitializer>()
				.RegisterFinalizer<CircuitFinalizer>()
			);
		Assert.That(services.Where(descriptor => descriptor.ServiceType == typeof(IApplicationInitializer) && descriptor.IsKeyedService)
			.Select(descriptor => descriptor.ServiceKey), Is.EqualTo(new[] { "First", "Second" }));
		Assert.That(services.Where(descriptor => descriptor.ServiceType == typeof(IApplicationFinalizer) && descriptor.IsKeyedService)
			.Select(descriptor => descriptor.ServiceKey), Is.EqualTo(new[] { "First", "Second" }));
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var firstScope = provider.CreateAsyncScope();
		await using var secondScope = provider.CreateAsyncScope();
		var first = firstScope.ServiceProvider.GetRequiredService<IApplication>();
		var second = secondScope.ServiceProvider.GetRequiredService<IApplication>();
		Assert.That(first.LoadedPlugins.Select(plugin => plugin.Name), Is.EqualTo(new[] { "First", "Second" }));
		Assert.That(second.LoadedPlugins, Is.EqualTo(first.LoadedPlugins));
		Assert.That(firstScope.ServiceProvider.GetKeyedServices<IApplicationInitializer>(typeof(IApplication)), Is.Empty);
		Assert.That(firstScope.ServiceProvider.GetKeyedServices<IApplicationFinalizer>(typeof(IApplication)), Is.Empty);
		Assert.That(firstScope.ServiceProvider.GetServices<IApplicationInitializer>(), Is.Empty);
		Assert.That(firstScope.ServiceProvider.GetServices<IApplicationFinalizer>(), Is.Empty);
		var firstState = firstScope.ServiceProvider.GetRequiredService<LifecycleState>();
		var secondState = secondScope.ServiceProvider.GetRequiredService<LifecycleState>();
		var firstHandler = firstScope.ServiceProvider.GetServices<CircuitHandler>().Single();
		var secondHandler = secondScope.ServiceProvider.GetServices<CircuitHandler>().Single();
		await firstHandler.OnCircuitOpenedAsync(null, CancellationToken.None);
		await firstHandler.OnCircuitOpenedAsync(null, CancellationToken.None);
		Assert.That(firstState.Initializations, Is.EqualTo(2));
		Assert.That(firstState.InitializerInstances, Has.Length.EqualTo(2));
		Assert.That(firstState.InitializerInstances[0], Is.Not.SameAs(firstState.InitializerInstances[1]));
		Assert.That(secondState.Initializations, Is.Zero);
		await secondHandler.OnCircuitOpenedAsync(null, CancellationToken.None);
		Assert.That(secondState.InitializerInstances, Has.Length.EqualTo(2));
		Assert.That(secondState.InitializerInstances[0], Is.Not.SameAs(secondState.InitializerInstances[1]));
		Assert.That(firstState.InitializerInstances.Intersect(secondState.InitializerInstances), Is.Empty);
		await firstHandler.OnCircuitClosedAsync(null, CancellationToken.None);
		Assert.That(firstState.Finalizations, Is.EqualTo(2));
		Assert.That(firstState.FinalizerInstances, Has.Length.EqualTo(2));
		Assert.That(firstState.FinalizerInstances[0], Is.Not.SameAs(firstState.FinalizerInstances[1]));
		Assert.That(secondState.Initializations, Is.EqualTo(2));
		Assert.That(secondState.Finalizations, Is.Zero);
		await firstScope.DisposeAsync();
		Assert.That(firstState.Finalizations, Is.EqualTo(2), "Disposing a circuit must not repeat either plugin's finalizer.");
		await secondHandler.OnCircuitClosedAsync(null, CancellationToken.None);
		Assert.That(secondState.Finalizations, Is.EqualTo(2));
		Assert.That(secondState.FinalizerInstances, Has.Length.EqualTo(2));
		Assert.That(secondState.FinalizerInstances[0], Is.Not.SameAs(secondState.FinalizerInstances[1]));
		Assert.That(firstState.FinalizerInstances.Intersect(secondState.FinalizerInstances), Is.Empty);
	}

	[Test]
	public async Task ServerRenderingDoesNotRunApplicationOrProcessLifecycleTasks() {
		var services = new ServiceCollection().AddLogging().AddSphere10BlazorPlugin(CreatePlugin());
		services.AddSingleton<NavigationManager>(new TestNavigationManager("http://localhost/", "http://localhost/application"));
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		LifecycleState state;
		await using (var scope = provider.CreateAsyncScope()) {
			state = scope.ServiceProvider.GetRequiredService<LifecycleState>();
			var application = scope.ServiceProvider.GetRequiredService<IApplication>();
			await using var renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
			await renderer.Dispatcher.InvokeAsync(async () => {
				var rendered = await renderer.RenderComponentAsync<ApplicationShell>();
				Assert.That(rendered.ToHtmlString(), Does.Contain("sphere10-application"));
			});
			Assert.That(application.IsInitialized, Is.False);
			Assert.That(state.Initializations, Is.Zero);
		}
		Assert.That(state.Finalizations, Is.Zero, "Disposing a prerender scope must not run unstarted circuit tasks.");
		Assert.That(state.ProcessInitializations, Is.Zero);
		Assert.That(state.ProcessFinalizations, Is.Zero);
	}

	[Test]
	public async Task CircuitFinalizationLeavesTheBorrowedHostAndSharedPluginAvailable() {
		var block = new BlazorApplicationBlockBuilder()
			.WithId("workspace")
			.WithName("Workspace")
			.WithDefaultScreen<ProbeScreen>()
			.Build();
		var plugin = CreatePlugin(block);
		var unloaded = 0;
		plugin.Unloaded += () => unloaded++;
		await using var provider = new ServiceCollection().AddSphere10BlazorPlugin(plugin).BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var scope = provider.CreateAsyncScope();
		var application = scope.ServiceProvider.GetRequiredService<IApplication>();
		var handler = scope.ServiceProvider.GetServices<CircuitHandler>().Single();
		var host = scope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		await handler.OnCircuitOpenedAsync(null, CancellationToken.None);
		var screen = await host.ActivateBlockAsync("workspace");
		await handler.OnCircuitClosedAsync(null, CancellationToken.None);
		Assert.That(await host.CloseScreenAsync(screen.Id), Is.True);
		Assert.That(application.LoadedPlugins.Single(), Is.SameAs(plugin));
		Assert.That(scope.ServiceProvider.GetRequiredService<LifecycleState>().Finalizations, Is.EqualTo(1));
		Assert.That(unloaded, Is.Zero);
	}

	[Test]
	public async Task ClosingAnUnopenedCircuitDoesNotConstructAnApplication() {
		var configured = 0;
		var services = new ServiceCollection().AddSphere10BlazorApplication(_ => configured++);
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var scope = provider.CreateAsyncScope();
		var handler = scope.ServiceProvider.GetServices<CircuitHandler>().Single();
		await handler.OnCircuitClosedAsync(null, CancellationToken.None);
		Assert.That(configured, Is.Zero);
	}

	[Test]
	public async Task CircuitCallbacksAwaitApplicationInitializationAndFinalization() {
		await using var provider = CreateAsyncLifecycleServices().BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var scope = provider.CreateAsyncScope();
		var application = scope.ServiceProvider.GetRequiredService<IApplication>();
		var state = scope.ServiceProvider.GetRequiredService<AsyncLifecycleState>();
		using var completeLifecycle = Tools.Scope.ExecuteOnDispose(() => {
			state.CompleteInitialization.TrySetResult();
			state.CompleteFinalization.TrySetResult();
		});
		var handler = scope.ServiceProvider.GetServices<CircuitHandler>().Single();
		var opened = handler.OnCircuitOpenedAsync(null, CancellationToken.None);
		await state.InitializationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(opened.IsCompleted, Is.False);
		Assert.That(application.IsInitialized, Is.False);
		state.CompleteInitialization.SetResult();
		await opened.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(application.IsInitialized, Is.True);
		var closed = handler.OnCircuitClosedAsync(null, new CancellationToken(true));
		await state.FinalizationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(closed.IsCompleted, Is.False, "Circuit closure must await pending finalization despite circuit cancellation.");
		state.CompleteFinalization.SetResult();
		await closed.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(application.IsInitialized, Is.False);
	}

	[Test]
	public async Task AsyncScopeDisposalAwaitsApplicationFinalizationBeforeReleasingOwnedResources() {
		await using var provider = CreateAsyncLifecycleServices().BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var scope = provider.CreateAsyncScope();
		var application = scope.ServiceProvider.GetRequiredService<IApplication>();
		var state = scope.ServiceProvider.GetRequiredService<AsyncLifecycleState>();
		using var completeLifecycle = Tools.Scope.ExecuteOnDispose(() => {
			state.CompleteInitialization.TrySetResult();
			state.CompleteFinalization.TrySetResult();
		});
		state.CompleteInitialization.SetResult();
		await application.InitializeAsync();
		var resourceDisposals = 0;
		application.Disposables.Add(Tools.Scope.ExecuteOnDispose(() => resourceDisposals++));
		var disposed = scope.DisposeAsync().AsTask();
		await state.FinalizationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(disposed.IsCompleted, Is.False);
		Assert.That(resourceDisposals, Is.Zero);
		state.CompleteFinalization.SetResult();
		await disposed.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(resourceDisposals, Is.EqualTo(1));
		Assert.That(application.IsInitialized, Is.False);
	}

	private static ServiceCollection CreateAsyncLifecycleServices() {
		var services = new ServiceCollection();
		services.AddScoped<AsyncLifecycleState>();
		services.AddScoped<IBlazorApplication>(provider => new AsyncLifecycleApplication(provider.GetRequiredService<IBlazorApplicationScreenHost>(),
			provider.GetRequiredService<AsyncLifecycleState>()));
		services.AddSphere10Blazor();
		return services;
	}

	private static BlazorPlugin CreatePlugin(params IBlazorApplicationBlock[] blocks) {
		var builder = new BlazorPluginBuilder()
			.WithName("Lifecycle")
			.RegisterInitializer<CircuitInitializer>()
			.RegisterFinalizer<CircuitFinalizer>()
			.ConfigureServices(services => {
				services.AddScoped<LifecycleState>();
				services.AddInitializer<ProcessInitializer>();
				services.AddFinalizer<ProcessFinalizer>();
			});
		foreach (var block in blocks)
			builder.AddBlock(block);
		return builder.Build();
	}

	public sealed class LifecycleState {
		private readonly List<CircuitInitializer> _initializerInstances = new();
		private readonly List<CircuitFinalizer> _finalizerInstances = new();

		public int Initializations { get; set; }
		public int Finalizations { get; set; }
		public int ProcessInitializations { get; set; }
		public int ProcessFinalizations { get; set; }
		public CircuitInitializer Initializer { get; set; }
		public CircuitFinalizer Finalizer { get; set; }
		public CircuitInitializer[] InitializerInstances => _initializerInstances.ToArray();
		public CircuitFinalizer[] FinalizerInstances => _finalizerInstances.ToArray();

		internal void RecordInitialization(CircuitInitializer initializer) {
			_initializerInstances.Add(initializer);
			Initializations++;
		}

		internal void RecordFinalization(CircuitFinalizer finalizer) {
			_finalizerInstances.Add(finalizer);
			Finalizations++;
		}
	}

	public sealed class CircuitInitializer : SyncApplicationInitializerBase {
		private readonly LifecycleState _state;

		public CircuitInitializer(LifecycleState state) {
			_state = state;
			_state.Initializer = this;
		}

		public override void Initialize() => _state.RecordInitialization(this);
	}

	public sealed class CircuitFinalizer : SyncApplicationFinalizerBase {
		private readonly LifecycleState _state;

		public CircuitFinalizer(LifecycleState state) {
			_state = state;
			_state.Finalizer = this;
		}

#pragma warning disable CS0465 // Reuse the existing application finalizer method name.
		public override void Finalize() => _state.RecordFinalization(this);
#pragma warning restore CS0465
	}

	public sealed class ProcessInitializer : SyncApplicationInitializerBase {
		private readonly LifecycleState _state;

		public ProcessInitializer(LifecycleState state) {
			_state = state;
		}

		public override void Initialize() => _state.ProcessInitializations++;
	}

	public sealed class ProcessFinalizer : SyncApplicationFinalizerBase {
		private readonly LifecycleState _state;

		public ProcessFinalizer(LifecycleState state) {
			_state = state;
		}

#pragma warning disable CS0465 // Reuse the existing application finalizer method name.
		public override void Finalize() => _state.ProcessFinalizations++;
#pragma warning restore CS0465
	}

	public sealed class ProbeScreen : BlazorApplicationScreen {
	}

	public sealed class AsyncLifecycleState {
		public TaskCompletionSource InitializationStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource CompleteInitialization { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource FinalizationStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource CompleteFinalization { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	}

	private sealed class AsyncLifecycleApplication : BlazorApplication {
		private readonly AsyncLifecycleState _state;

		public AsyncLifecycleApplication(IBlazorApplicationScreenHost screenHost, AsyncLifecycleState state)
			: base(screenHost, Array.Empty<IBlazorPlugin>()) {
			_state = state;
		}

		protected override async Task<bool> OnInitializeAsync(CancellationToken cancellationToken) {
			_state.InitializationStarted.TrySetResult();
			await _state.CompleteInitialization.Task.WaitAsync(cancellationToken);
			return await base.OnInitializeAsync(cancellationToken);
		}

		protected override async Task OnFinalizeAsync(CancellationToken cancellationToken) {
			_state.FinalizationStarted.TrySetResult();
			await _state.CompleteFinalization.Task.WaitAsync(cancellationToken);
			await base.OnFinalizeAsync(cancellationToken);
		}
	}
}
