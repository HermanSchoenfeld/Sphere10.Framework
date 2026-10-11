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
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class LoadedPluginRegistrationTests {
	[TestCase(false)]
	[TestCase(true)]
	public void RegisteredButUnloadedPluginsAreExcludedFromBothApplicationFactories(bool configureApplication) {
		var plugin = new BlazorPlugin("Unloaded", Array.Empty<IBlazorApplicationBlock>());
		var loaded = 0;
		plugin.Loaded += () => loaded++;
		var services = new ServiceCollection().AddSphere10Blazor();
		services.AddSingleton<IBlazorPlugin>(plugin);
		if (configureApplication)
			services.AddSphere10BlazorApplication(_ => { });
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		using var scope = provider.CreateScope();
		var application = scope.ServiceProvider.GetRequiredService<IBlazorApplication>();

		Assert.That(provider.GetServices<IBlazorPlugin>().Single(), Is.SameAs(plugin));
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(((IApplication)application).LoadedPlugins, Is.Empty);
		Assert.That(application.Blocks, Is.Empty);
		Assert.That(provider.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>(), Is.Empty);
		Assert.That(loaded, Is.Zero, "Circuit resolution must not turn ordinary DI metadata into a startup load.");
	}

	[Test]
	public void FailedLoadDoesNotPublishAReceiptOrDisplaceSuccessfulPlugins() {
		var successful = new BlazorPlugin("Successful", Array.Empty<IBlazorApplicationBlock>());
		var failing = new BlazorPluginBuilder()
			.WithName("Failing")
			.ConfigureServices(services => {
				services.AddSingleton<StartupMarker>();
				Guard.Ensure(false, "Startup failed.");
			})
			.Build();
		var services = new ServiceCollection().AddSphere10BlazorPlugin(successful);
		Assert.That(() => services.AddSphere10BlazorPlugin(failing), Throws.InvalidOperationException);
		using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IBlazorApplication>();

		Assert.That(application.LoadedPlugins, Is.EqualTo(new[] { successful }));
		Assert.That(provider.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>().Single().Plugin, Is.SameAs(successful));
		Assert.That(provider.GetServices<IApplicationPlugin>(), Is.EqualTo(new[] { successful }));
		Assert.That(provider.GetRequiredService<StartupMarker>(), Is.Not.Null, "The receipt does not imply rollback of service callback side effects.");
	}

	[TestCase("Configure")]
	[TestCase("Loaded")]
	[TestCase("Validation")]
	public void FailedPluginTasksDoNotContaminateASameNameReplacement(string failureStage) {
		var calls = new List<string>();
		var services = new ServiceCollection();
		services.AddKeyedSingleton<IApplicationInitializer>("Reused", new CallbackInitializer(() => calls.Add("Existing initialized")));
		services.AddKeyedSingleton<IApplicationFinalizer>("Reused", new CallbackFinalizer(() => calls.Add("Existing finalized")));
		var owned = new BlazorApplicationBlockBuilder()
			.WithId("owned")
			.WithName("Owned")
			.Build();
		services.AddSphere10BlazorPlugin(new BlazorPlugin("Owner", new[] { owned }));
		var failed = (MutablePlugin)new MutablePluginBuilder()
			.WithName("Reused")
			.RegisterInitializer<CallbackInitializer>(_ => new CallbackInitializer(() => calls.Add("Abandoned initialized")))
			.RegisterFinalizer<CallbackFinalizer>(_ => new CallbackFinalizer(() => calls.Add("Abandoned finalized")))
			.ConfigureServices(components => {
				components.AddSingleton<StartupMarker>();
				components.AddKeyedSingleton<StartupMarker>("Reused", new StartupMarker());
				Guard.Ensure(failureStage != "Configure", "Configuration failed.");
			})
			.Build();
		failed.Loaded += () => {
			Guard.Ensure(failureStage != "Loaded", "Loaded callback failed.");
			if (failureStage == "Validation")
				failed.CurrentBlocks = new[] { owned };
		};
		if (failureStage == "Validation")
			Assert.That(() => services.AddSphere10BlazorPlugin(failed), Throws.ArgumentException);
		else
			Assert.That(() => services.AddSphere10BlazorPlugin(failed), Throws.InvalidOperationException);
		Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(IApplicationInitializer) && descriptor.IsKeyedService && Equals(descriptor.ServiceKey, "Reused")), Is.EqualTo(1));
		Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(IApplicationFinalizer) && descriptor.IsKeyedService && Equals(descriptor.ServiceKey, "Reused")), Is.EqualTo(1));
		services.AddSphere10BlazorPlugin(plugin => plugin
			.WithName("Reused")
			.RegisterInitializer<CallbackInitializer>(_ => new CallbackInitializer(() => calls.Add("Replacement initialized")))
			.RegisterFinalizer<CallbackFinalizer>(_ => new CallbackFinalizer(() => calls.Add("Replacement finalized")))
		);
		using var provider = services.BuildServiceProvider();
		Assert.That(provider.GetRequiredService<StartupMarker>(), Is.Not.Null, "Ordinary service side effects retain the existing registration policy.");
		Assert.That(provider.GetRequiredKeyedService<StartupMarker>("Reused"), Is.Not.Null, "Cleanup only removes named lifecycle descriptors.");
		Assert.That(provider.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>().Select(registration => registration.Plugin.Name), Is.EqualTo(new[] { "Owner", "Reused" }));
		foreach (var initializer in provider.GetKeyedServices<IApplicationInitializer>("Reused"))
			initializer.Initialize();
		foreach (var finalizer in provider.GetKeyedServices<IApplicationFinalizer>("Reused"))
			finalizer.Finalize();
		Assert.That(calls, Is.EqualTo(new[] { "Existing initialized", "Replacement initialized", "Existing finalized", "Replacement finalized" }));
	}

	[Test]
	public void NestedSameNameRegistrationIsRejectedBeforeLoadingAndReleasesTheNameOnFailure() {
		var services = new ServiceCollection();
		var nestedLoads = 0;
		var calls = new List<string>();
		var outer = new BlazorPluginBuilder()
			.WithName("Reused")
			.RegisterInitializer<CallbackInitializer>(_ => new CallbackInitializer(() => calls.Add("Abandoned initialized")))
			.RegisterFinalizer<CallbackFinalizer>(_ => new CallbackFinalizer(() => calls.Add("Abandoned finalized")))
			.ConfigureServices(components => components.AddSphere10BlazorPlugin(plugin => plugin
				.WithName("Reused")
				.ConfigureServices(_ => nestedLoads++)
			))
			.Build();
		Assert.That(() => services.AddSphere10BlazorPlugin(outer), Throws.ArgumentException);
		Assert.That(nestedLoads, Is.Zero);
		Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(ApplicationPluginRegistration<IBlazorPlugin>)), Is.False);
		Assert.That(services.Any(descriptor => descriptor.IsKeyedService && Equals(descriptor.ServiceKey, "Reused")), Is.False);
		services.AddSphere10BlazorPlugin(plugin => plugin
			.WithName("Reused")
			.RegisterInitializer<CallbackInitializer>(_ => new CallbackInitializer(() => calls.Add("Replacement initialized")))
			.RegisterFinalizer<CallbackFinalizer>(_ => new CallbackFinalizer(() => calls.Add("Replacement finalized")))
		);
		using var provider = services.BuildServiceProvider();
		provider.GetKeyedServices<IApplicationInitializer>("Reused").Single().Initialize();
		provider.GetKeyedServices<IApplicationFinalizer>("Reused").Single().Finalize();
		Assert.That(calls, Is.EqualTo(new[] { "Replacement initialized", "Replacement finalized" }));
	}

	[Test]
	public void SuccessfulNestedDifferentNameRegistrationSurvivesItsOuterPluginFailure() {
		var services = new ServiceCollection();
		var calls = new List<string>();
		var outer = new BlazorPluginBuilder()
			.WithName("Outer")
			.RegisterInitializer<CallbackInitializer>(_ => new CallbackInitializer(() => calls.Add("Abandoned initialized")))
			.RegisterFinalizer<CallbackFinalizer>(_ => new CallbackFinalizer(() => calls.Add("Abandoned finalized")))
			.ConfigureServices(components => {
				components.AddSphere10BlazorPlugin(plugin => plugin
					.WithName("Nested")
					.RegisterInitializer<CallbackInitializer>(_ => new CallbackInitializer(() => calls.Add("Nested initialized")))
					.RegisterFinalizer<CallbackFinalizer>(_ => new CallbackFinalizer(() => calls.Add("Nested finalized")))
				);
				Guard.Ensure(false, "Outer loading failed.");
			})
			.Build();
		Assert.That(() => services.AddSphere10BlazorPlugin(outer), Throws.InvalidOperationException);
		Assert.That(services.Any(descriptor => descriptor.IsKeyedService && Equals(descriptor.ServiceKey, "Outer")), Is.False);
		services.AddSphere10BlazorPlugin(plugin => plugin
			.WithName("Outer")
			.RegisterInitializer<CallbackInitializer>(_ => new CallbackInitializer(() => calls.Add("Replacement initialized")))
			.RegisterFinalizer<CallbackFinalizer>(_ => new CallbackFinalizer(() => calls.Add("Replacement finalized")))
		);
		using var provider = services.BuildServiceProvider();
		var registrations = provider.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>().ToArray();
		Assert.That(registrations.Select(registration => registration.Plugin.Name), Is.EqualTo(new[] { "Nested", "Outer" }));
		foreach (var registration in registrations)
			provider.GetKeyedServices<IApplicationInitializer>(registration.Plugin.Name).Single().Initialize();
		foreach (var registration in registrations)
			provider.GetKeyedServices<IApplicationFinalizer>(registration.Plugin.Name).Single().Finalize();
		Assert.That(calls, Is.EqualTo(new[] { "Nested initialized", "Replacement initialized", "Nested finalized", "Replacement finalized" }));
	}

	[Test]
	public void DuplicateRegistrationDoesNotLoadAgainOrPublishAnotherReceipt() {
		var plugin = new BlazorPlugin("Once", Array.Empty<IBlazorApplicationBlock>());
		var loaded = 0;
		plugin.Loaded += () => loaded++;
		var services = new ServiceCollection().AddSphere10BlazorPlugin(plugin);

		Assert.That(() => services.AddSphere10BlazorPlugin(plugin), Throws.ArgumentException);
		Assert.That(loaded, Is.EqualTo(1));
		Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(ApplicationPluginRegistration<IBlazorPlugin>)), Is.EqualTo(1));
	}

	[Test]
	public async Task CapturedContributionsKeepStartupOrderWhenSourceMembershipChanges() {
		var first = new BlazorApplicationBlockBuilder()
			.WithId("first")
			.WithName("First")
			.WithPosition(50)
			.WithDefaultScreen<FirstScreen>()
			.Build();
		var second = new BlazorApplicationBlockBuilder()
			.WithId("second")
			.WithName("Second")
			.WithPosition(-10)
			.WithDefaultScreen<SecondScreen>()
			.Build();
		var plugin = new MutablePlugin(new[] { first, second });
		var services = new ServiceCollection().AddSphere10BlazorPlugin(plugin);
		plugin.CurrentBlocks = Array.Empty<IBlazorApplicationBlock>();
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		using var scope = provider.CreateScope();
		var receipt = provider.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>().Single();
		receipt.Blocks[0] = null;
		var application = scope.ServiceProvider.GetRequiredService<IBlazorApplication>();
		await application.ScreenHost.InitializeAsync();

		Assert.That(receipt.Plugin, Is.SameAs(plugin));
		Assert.That(receipt.Blocks.Cast<IBlazorApplicationBlock>().Select(block => block.Id), Is.EqualTo(new[] { "first", "second" }));
		Assert.That(receipt.Blocks.First(), Is.Not.SameAs(first), "The receipt captures the validated post-load contribution snapshots.");
		Assert.That(application.LoadedPlugins.Single(), Is.SameAs(plugin));
		Assert.That(application.Blocks.Select(block => block.Id), Is.EqualTo(new[] { "second", "first" }));
		Assert.That(application.ScreenHost.ActiveScreen.Block.Id, Is.EqualTo("first"));
		Assert.That(application.ScreenHost.ActiveScreen.ScreenType, Is.EqualTo(typeof(FirstScreen)));
	}

	public sealed class StartupMarker {
	}

	private sealed class MutablePlugin : BlazorPlugin {
		public MutablePlugin(IBlazorApplicationBlock[] blocks)
			: this("Mutable", blocks) {
		}

		public MutablePlugin(string name, IBlazorApplicationBlock[] blocks, Action<IServiceCollection> configureServices = null)
			: base(name, blocks, configureServices) {
			CurrentBlocks = blocks;
		}

		public IBlazorApplicationBlock[] CurrentBlocks { get; set; }

		public override IBlazorApplicationBlock[] Blocks {
			get => Tools.Array.Clone(CurrentBlocks);
			init => base.Blocks = value;
		}
	}

	private sealed class MutablePluginBuilder : BlazorPluginBuilder {
		protected override BlazorPlugin CreatePlugin(IBlazorApplicationBlock[] blocks, Action<IServiceCollection> configureServices)
			=> new MutablePlugin(Name, blocks, configureServices);
	}

	private sealed class CallbackInitializer(Action callback) : SyncApplicationInitializerBase {
		public override void Initialize() => callback();
	}

	private sealed class CallbackFinalizer(Action callback) : SyncApplicationFinalizerBase {
#pragma warning disable CS0465 // Implement the existing lifecycle task contract.
		public override void Finalize() => callback();
#pragma warning restore CS0465
	}

	public sealed class FirstScreen : ComponentBase, IBlazorApplicationScreen {
		public IBlazorApplicationScreenHost Host { get; set; }

		public IBlazorApplicationBlock Block { get; set; }

	}

	public sealed class SecondScreen : ComponentBase, IBlazorApplicationScreen {
		public IBlazorApplicationScreenHost Host { get; set; }

		public IBlazorApplicationBlock Block { get; set; }

	}
}
