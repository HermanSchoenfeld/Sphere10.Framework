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
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests.UI;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationLoadedPluginLifecycleTests {
	[TestCase(false)]
	[TestCase(true)]
	public async Task LoadedPluginsUseIndependentCachedTasksFromEachApplicationsProvider(bool useAsync) {
		var firstPlugin = CreatePlugin("First");
		var secondPlugin = CreatePlugin("Second");
		var services = CreateServices(firstPlugin, secondPlugin);
		firstPlugin.Load(services);
		secondPlugin.Load(services);
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var firstScope = provider.CreateAsyncScope();
		await using var secondScope = provider.CreateAsyncScope();
		var first = firstScope.ServiceProvider.GetRequiredService<ProbeApplication>();
		var second = secondScope.ServiceProvider.GetRequiredService<ProbeApplication>();
		var firstState = firstScope.ServiceProvider.GetRequiredService<LifecycleState>();
		var secondState = secondScope.ServiceProvider.GetRequiredService<LifecycleState>();
		Assert.That(firstState.Initializers, Is.Empty);
		Assert.That(firstState.Finalizers, Is.Empty);
		Assert.That(secondState.Initializers, Is.Empty);

		if (useAsync) {
			await first.InitializeAsync();
			await first.InitializeAsync();
			await second.InitializeAsync();
			await first.FinalizeAsync();
			await first.FinalizeAsync();
			await second.FinalizeAsync();
		} else {
			first.Initialize();
			first.Initialize();
			second.Initialize();
			first.Finalize();
			first.Finalize();
			second.Finalize();
		}

		var expected = new[] { "initialize First", "initialize Second", "finalize First", "finalize Second" };
		Assert.That(firstState.Calls, Is.EqualTo(expected));
		Assert.That(secondState.Calls, Is.EqualTo(expected));
		Assert.That(firstState.Initializers, Has.Count.EqualTo(2));
		Assert.That(firstState.Finalizers, Has.Count.EqualTo(2));
		Assert.That(firstState.Initializers[0], Is.Not.SameAs(firstState.Initializers[1]));
		Assert.That(firstState.Finalizers[0], Is.Not.SameAs(firstState.Finalizers[1]));
		Assert.That(secondState.Initializers[0], Is.Not.SameAs(firstState.Initializers[0]));
		Assert.That(secondState.Finalizers[0], Is.Not.SameAs(firstState.Finalizers[0]));
		Assert.That(firstState.Initializers.All(task => ReferenceEquals(task.Application, first)), Is.True);
		Assert.That(secondState.Initializers.All(task => ReferenceEquals(task.Application, second)), Is.True);
		Assert.That(firstState.Finalizers.All(task => ReferenceEquals(task.Application, first)), Is.True);
		Assert.That(secondState.Finalizers.All(task => ReferenceEquals(task.Application, second)), Is.True);
		Assert.That(first.StartupTasks, Is.EqualTo(firstState.Initializers));
		Assert.That(first.ShutdownTasks, Is.EqualTo(firstState.Finalizers));
		first.StartupTasks[0] = null;
		first.ShutdownTasks[0] = null;
		Assert.That(first.StartupTasks[0], Is.SameAs(firstState.Initializers[0]));
		Assert.That(first.ShutdownTasks[0], Is.SameAs(firstState.Finalizers[0]));
	}

	[Test]
	public void InventoriesIncludeOnlyLoadedPluginsAndKeepGlobalPriorityAndFinalizerOrdering() {
		var first = CreatePlugin("First", 30);
		var second = CreatePlugin("Second", 0);
		var unloaded = CreatePlugin("Unloaded", -10);
		var services = CreateServices(first, second);
		first.Load(services);
		second.Load(services);
		unloaded.Load(services);
		services.AddKeyedTransient<IApplicationInitializer>(typeof(IApplication), (provider, _) =>
			new ProbeInitializer(provider.GetRequiredService<IApplication>(), provider.GetRequiredService<LifecycleState>(), "Host", 10));
		services.AddKeyedTransient<IApplicationFinalizer>(typeof(IApplication), (provider, _) =>
			new ProbeFinalizer(provider.GetRequiredService<IApplication>(), provider.GetRequiredService<LifecycleState>(), "Host"));
		services.AddTransient<IApplicationInitializer>(provider =>
			new ProbeInitializer(provider.GetRequiredService<IApplication>(), provider.GetRequiredService<LifecycleState>(), "Process", -20));
		services.AddTransient<IApplicationFinalizer>(provider =>
			new ProbeFinalizer(provider.GetRequiredService<IApplication>(), provider.GetRequiredService<LifecycleState>(), "Process"));
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		using var scope = provider.CreateScope();
		var application = scope.ServiceProvider.GetRequiredService<ProbeApplication>();
		var state = scope.ServiceProvider.GetRequiredService<LifecycleState>();
		application.Initialize();
		application.Finalize();
		Assert.That(state.Calls, Is.EqualTo(new[] {
			"initialize Second", "initialize Host", "initialize First",
			"finalize Host", "finalize First", "finalize Second"
		}));
		Assert.That(state.Initializers.Select(task => task.Name), Is.EqualTo(new[] { "Host", "First", "Second" }));
		Assert.That(state.Finalizers.Select(task => task.Name), Is.EqualTo(new[] { "Host", "First", "Second" }));
		Assert.That(application.LoadedPlugins.All(plugin => plugin.Blocks.Length == 0), Is.True, "Service-only plugins contribute lifecycle tasks too.");
	}

	private static ApplicationPlugin CreatePlugin(string name, int priority = 0) => new ApplicationPluginBuilder()
		.WithName(name)
		.RegisterInitializer(provider => new ProbeInitializer(provider.GetRequiredService<IApplication>(), provider.GetRequiredService<LifecycleState>(), name, priority))
		.RegisterFinalizer(provider => new ProbeFinalizer(provider.GetRequiredService<IApplication>(), provider.GetRequiredService<LifecycleState>(), name))
		.Build();

	private static ServiceCollection CreateServices(params IApplicationPlugin[] plugins) {
		var services = new ServiceCollection();
		services.AddScoped<LifecycleState>();
		services.AddScoped<ProbeApplication>(provider => new ProbeApplication(provider, plugins));
		services.AddScoped<IApplication>(provider => provider.GetRequiredService<ProbeApplication>());
		services.AddSingleton<ILogger>(new NoOpLogger());
		return services;
	}

	private sealed class ProbeApplication : SyncApplicationBase {
		private readonly IApplicationPlugin[] _plugins;

		public ProbeApplication(IServiceProvider services, IApplicationPlugin[] plugins)
			: base(services) {
			_plugins = Tools.UI.ValidatePlugins(plugins);
		}

		public override IApplicationPlugin[] LoadedPlugins => Tools.Array.Clone(_plugins);
		public override IApplicationScreenHost ScreenHost => null;
		public IApplicationInitializer[] StartupTasks => Initializers;
		public IApplicationFinalizer[] ShutdownTasks => Finalizers;
	}

	private sealed class LifecycleState {
		public List<string> Calls { get; } = new();
		public List<ProbeInitializer> Initializers { get; } = new();
		public List<ProbeFinalizer> Finalizers { get; } = new();
	}

	private sealed class ProbeInitializer : SyncApplicationInitializerBase {
		private readonly LifecycleState _state;
		private readonly int _priority;

		public ProbeInitializer(IApplication application, LifecycleState state, string name, int priority) {
			Application = application;
			Name = name;
			_state = state;
			_priority = priority;
			state.Initializers.Add(this);
		}

		public IApplication Application { get; }
		public string Name { get; }
		public override int Priority => _priority;
		public override void Initialize() => _state.Calls.Add("initialize " + Name);
	}

	private sealed class ProbeFinalizer : SyncApplicationFinalizerBase {
		private readonly LifecycleState _state;

		public ProbeFinalizer(IApplication application, LifecycleState state, string name) {
			Application = application;
			Name = name;
			_state = state;
			state.Finalizers.Add(this);
		}

		public IApplication Application { get; }
		public string Name { get; }
#pragma warning disable CS0465 // Implements the existing lifecycle task contract, not a CLR finalizer.
		public override void Finalize() => _state.Calls.Add("finalize " + Name);
#pragma warning restore CS0465
	}
}
