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
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using FormsUI = global::Sphere10.Framework.Windows.Forms;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationPluginTests {
	private bool _startedFramework;

	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework)
			Sphere10Framework.Instance
				.Build()
				.Start();
	}

	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	[Test]
	public void SharedStorageCopiesMembershipWithoutCopyingOrDisposingNativeDefinitions() {
		using var block = new TrackingBlock { Name = "Workspace" };
		var configured = 0;
		var loaded = 0;
		var unloaded = 0;
		var plugin = new FormsUI.ApplicationPluginBuilder()
			.WithName("Features")
			.AddBlock(block)
			.AddServices(_ => configured++)
			.Build();
		SharedUI.IApplicationPlugin shared = plugin;
		shared.Loaded += () => loaded++;
		shared.Unloaded += () => unloaded++;
		plugin.Blocks[0] = null;
		shared.Blocks[0] = null;
		Assert.That(plugin.Blocks.Single(), Is.SameAs(block));
		Assert.That(shared.Blocks.Single(), Is.SameAs(block));
		shared.Load(new ServiceCollection());
		shared.Unload();
		Assert.That(configured, Is.EqualTo(1));
		Assert.That(loaded, Is.EqualTo(1));
		Assert.That(unloaded, Is.EqualTo(1));
		Assert.That(block.DisposeCount, Is.Zero, "Plugins group definitions; the existing native owner still controls disposal.");
	}

	[Test]
	public async Task RegistrationLoadsOnceAndInitializesTheExistingBlockPathOnce() {
		var startupActions = 0;
		var loaded = 0;
		var unloaded = 0;
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		using var block = new ApplicationBlockBuilder()
			.WithName("Workspace")
			.AddScreen<ProbeScreen>(screen => screen.WithTitle("Workspace"))
			.AddMenu(menu => menu
				.WithText("Commands")
				.AddActionItem("Startup", () => startupActions++, executeOnLoad: true)
			)
			.Build();
		var plugin = new FormsUI.ApplicationPluginBuilder()
			.WithName("Workspace plugin")
			.AddBlock(block)
			.AddServices(registry => registry.AddSingleton<StartupMarker>())
			.Build();
		plugin.Loaded += () => loaded++;
		plugin.Unloaded += () => unloaded++;
		services.AddWinFormsApplicationPlugin(plugin);
		Assert.That(loaded, Is.EqualTo(1));
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<FormsUI.IApplication>();
		var shared = provider.GetRequiredService<SharedUI.IApplication>();
		Assert.That(shared, Is.SameAs(application));
		Assert.That(provider.GetRequiredService<StartupMarker>(), Is.Not.Null);
		Assert.That(provider.GetRequiredService<FormsUI.IApplicationPlugin>(), Is.SameAs(plugin));
		Assert.That(provider.GetRequiredService<SharedUI.IApplicationPlugin>(), Is.SameAs(plugin));
		Assert.That(shared.LoadedPlugins.Single(), Is.SameAs(application.LoadedPlugins.Single()));
		application.Initialize();
		await application.InitializeAsync();
		Assert.That(application.ScreenHost.Blocks.Single(), Is.SameAs(block));
		Assert.That(((TrackingMainForm)provider.GetRequiredService<IMainForm>()).RegisterCount, Is.EqualTo(1));
		Assert.That(startupActions, Is.EqualTo(1));
		Assert.That(loaded, Is.EqualTo(1));
		application.ScreenHost.ActivateScreen(block, typeof(ProbeScreen));
		Assert.That(application.ScreenHost.ActiveBlock, Is.SameAs(block));
		Assert.That(application.Blocks.Single(), Is.SameAs(block));
		Assert.That(shared.Blocks.Single(), Is.SameAs(block));
		application.LoadedPlugins[0] = null;
		shared.LoadedPlugins[0] = null;
		Assert.That(application.LoadedPlugins.Single(), Is.SameAs(plugin));
		await application.DisposeAsync();
		Assert.That(unloaded, Is.Zero, "Disposing the application adapter must not unload shared startup definitions.");
	}

	[Test]
	public async Task CallbackRegistrationAndDirectBlocksShareTheLiveApplicationMembership() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		using var explicitBlock = new ApplicationBlock { Name = "Plugin block", Position = 2 };
		explicitBlock.AddScreenDefinition(new SharedUI.ApplicationScreenDefinition(typeof(OtherScreen)));
		using var legacyBlock = new ApplicationBlock { Name = "Legacy", Position = 1 };
		legacyBlock.AddScreenDefinition(new SharedUI.ApplicationScreenDefinition(typeof(ProbeScreen)));
		services
			.AddWinFormsApplicationPlugin(plugin => plugin
				.WithName("Application")
				.AddBlock(explicitBlock)
			);
		services.AddApplicationBlock(legacyBlock);
		services.AddApplicationBlock(explicitBlock);
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<FormsUI.IApplication>();
		await application.InitializeAsync();
		Assert.That(application.LoadedPlugins.Select(plugin => plugin.Name), Is.EqualTo(new[] { "Application" }));
		Assert.That(application.LoadedPlugins[0].Blocks, Is.EqualTo(new[] { explicitBlock }));
		Assert.That(application.Blocks, Is.EqualTo(new[] { legacyBlock, explicitBlock }));
		Assert.That(((TrackingMainForm)application.BlockManager).RegisterCount, Is.EqualTo(2));
		application.ScreenHost.ActivateScreen(legacyBlock, typeof(ProbeScreen));
		Assert.That(application.ScreenHost.ActiveBlock, Is.SameAs(legacyBlock));
		application.ScreenHost.ActivateScreen(explicitBlock, typeof(OtherScreen));
		Assert.That(application.ScreenHost.ActiveBlock, Is.SameAs(explicitBlock));
	}

	[Test]
	public async Task DirectBlockConstructorPreservesStartupAndOwnershipWithoutCreatingAPlugin() {
		using var form = new TrackingMainForm();
		using var block = new TrackingBlock { Name = "Existing block" };
		await using var application = new FormsUI.Application(form, new[] { block });
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(application.Blocks, Is.Empty);
		await application.InitializeAsync();
		Assert.That(form.RegisterCount, Is.EqualTo(1));
		Assert.That(application.Blocks.Single(), Is.SameAs(block));
		Assert.That(application.ScreenHost.SelectedBlock, Is.SameAs(block));
		Assert.That(application.ScreenHost.ActiveBlock, Is.Null);
		await application.DisposeAsync();
		Assert.That(block.DisposeCount, Is.Zero);
		Assert.That(form.IsDisposed, Is.False);
	}

	[Test]
	public async Task DirectTransientBlockIsMaterializedOnceForRuntimeMembership() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		services.AddApplicationBlock<LegacyTransientBlock>();
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<FormsUI.IApplication>();
		await application.InitializeAsync();
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(application.Blocks.Single(), Is.SameAs(application.ScreenHost.Blocks.Single()));
		Assert.That(provider.GetRequiredService<SharedUI.IApplication>().Blocks.Single(), Is.SameAs(application.Blocks.Single()));
		Assert.That(((TrackingMainForm)application.BlockManager).RegisterCount, Is.EqualTo(1));
	}

	[Test]
	public async Task AlreadyLoadedPluginConstructorBindsCapturedProvenanceWithoutLoadingAgain() {
		using var form = new TrackingMainForm();
		using var block = new ApplicationBlock { Name = "Loaded contribution" };
		var loads = 0;
		var plugin = new FormsUI.ApplicationPlugin("Loaded", new[] { block }, _ => loads++);
		plugin.Load(new ServiceCollection());
		Assert.That(block.Source, Is.Null);
		await using var application = new FormsUI.Application(form, Array.Empty<FormsUI.IApplicationBlock>(), new[] { plugin });
		Assert.That(block.Source, Is.SameAs(plugin));
		Assert.That(block.Parent, Is.Null);
		Assert.That(loads, Is.EqualTo(1));
		await application.InitializeAsync();
		Assert.That(block.Parent, Is.SameAs(application));
		Assert.That(application.LoadedPlugins.Single(), Is.SameAs(plugin));
	}

	[Test]
	public async Task LiveMembershipTracksAlreadyRegisteredAndLaterBlocksWithoutCreatingAPlugin() {
		using var form = new TrackingMainForm();
		var initial = new ApplicationBlock { Name = "Before adapter" };
		var later = new TrackingBlock { Name = "After adapter" };
		form.RegisterBlock(initial);
		await using var application = new FormsUI.Application(form, Array.Empty<FormsUI.IApplicationBlock>());
		SharedUI.IApplication shared = application;
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(application.Blocks.Single(), Is.SameAs(initial));
		await application.InitializeAsync();
		form.RegisterBlock(later);
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(shared.Blocks, Is.EqualTo(new[] { initial, later }));
		Assert.That(form.Blocks, Is.EqualTo(shared.Blocks));
		shared.Blocks[0] = null;
		application.Blocks[0] = null;
		form.Blocks[0] = null;
		Assert.That(application.Blocks[0], Is.SameAs(initial));
		form.SelectedBlock = later;
		Assert.That(application.ScreenHost.SelectedBlock, Is.SameAs(later));
		form.UnregisterBlock(later);
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(application.Blocks, Is.EqualTo(new[] { initial }));
		Assert.That(later.DisposeCount, Is.EqualTo(1), "The existing block manager remains the only owner during removal.");
		using var last = new ApplicationBlock { Name = "Last block" };
		form.UnregisterBlock(initial);
		Assert.That(application.Blocks, Is.Empty);
		form.RegisterBlock(last);
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(shared.Blocks.Single(), Is.SameAs(last));
	}

	[Test]
	public void DuplicatePluginNamesAreRejectedBeforeLoadingServices() {
		var services = new ServiceCollection();
		services
			.AddWinFormsApplicationPlugin(plugin => plugin
				.WithName("Duplicate")
				.AddInitializer<IncrementUsageByOneInitializer>()
			);
		var loads = 0;
		var duplicate = new FormsUI.ApplicationPluginBuilder()
			.WithName("Duplicate")
			.AddInitializer<IncrementUsageByOneInitializer>()
			.AddServices(_ => loads++)
			.Build();
		Assert.That(() => services.AddWinFormsApplicationPlugin(duplicate), Throws.ArgumentException);
		Assert.That(loads, Is.Zero);
		Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(FormsUI.IApplicationPlugin)), Is.EqualTo(1));
		Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(IApplicationInitializer) && descriptor.IsKeyedService && Equals(descriptor.ServiceKey, "Duplicate")), Is.EqualTo(1));
	}

	[Test]
	public void BlocksCannotBeOwnedByTwoPlugins() {
		using var block = new ApplicationBlock { Name = "Block" };
		var services = new ServiceCollection();
		services
			.AddWinFormsApplicationPlugin(plugin => plugin
				.WithName("First")
				.AddBlock(block)
			);
		Assert.That(() => services
			.AddWinFormsApplicationPlugin(plugin => plugin
				.WithName("Second")
				.AddBlock(block)
			), Throws.ArgumentException);
	}

	[TestCase("Configure")]
	[TestCase("Loaded")]
	[TestCase("Validation")]
	public void FailedPluginTasksDoNotContaminateASameNameReplacement(string failureStage) {
		var calls = new List<string>();
		var services = new ServiceCollection();
		services.AddKeyedSingleton<IApplicationInitializer>("Reused", new CallbackInitializer(() => calls.Add("Existing initialized")));
		services.AddKeyedSingleton<IApplicationFinalizer>("Reused", new CallbackFinalizer(() => calls.Add("Existing finalized")));
		using var owned = new ApplicationBlock { Name = "Owned" };
		services.AddWinFormsApplicationPlugin(new FormsUI.ApplicationPlugin("Owner", new[] { owned }));
		var failed = (MutablePlugin)new MutablePluginBuilder()
			.WithName("Reused")
			.AddInitializer<CallbackInitializer>(_ => new CallbackInitializer(() => calls.Add("Abandoned initialized")))
			.AddFinalizer<CallbackFinalizer>(_ => new CallbackFinalizer(() => calls.Add("Abandoned finalized")))
			.AddServices(components => {
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
			Assert.That(() => services.AddWinFormsApplicationPlugin(failed), Throws.ArgumentException);
		else
			Assert.That(() => services.AddWinFormsApplicationPlugin(failed), Throws.InvalidOperationException);
		Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(IApplicationInitializer) && descriptor.IsKeyedService && Equals(descriptor.ServiceKey, "Reused")), Is.EqualTo(1));
		Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(IApplicationFinalizer) && descriptor.IsKeyedService && Equals(descriptor.ServiceKey, "Reused")), Is.EqualTo(1));
		services.AddWinFormsApplicationPlugin(plugin => plugin
			.WithName("Reused")
			.AddInitializer<CallbackInitializer>(_ => new CallbackInitializer(() => calls.Add("Replacement initialized")))
			.AddFinalizer<CallbackFinalizer>(_ => new CallbackFinalizer(() => calls.Add("Replacement finalized")))
		);
		using var provider = services.BuildServiceProvider();
		Assert.That(provider.GetRequiredService<StartupMarker>(), Is.Not.Null, "Ordinary service side effects retain the existing registration policy.");
		Assert.That(provider.GetRequiredKeyedService<StartupMarker>("Reused"), Is.Not.Null, "Cleanup only removes named lifecycle descriptors.");
		Assert.That(provider.GetServices<ApplicationPluginRegistration<FormsUI.IApplicationPlugin>>().Select(registration => registration.Plugin.Name), Is.EqualTo(new[] { "Owner", "Reused" }));
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
		var outer = new FormsUI.ApplicationPluginBuilder()
			.WithName("Reused")
			.AddInitializer<CallbackInitializer>(_ => new CallbackInitializer(() => calls.Add("Abandoned initialized")))
			.AddFinalizer<CallbackFinalizer>(_ => new CallbackFinalizer(() => calls.Add("Abandoned finalized")))
			.AddServices(components => components.AddWinFormsApplicationPlugin(plugin => plugin
				.WithName("Reused")
				.AddServices(_ => nestedLoads++)
			))
			.Build();
		Assert.That(() => services.AddWinFormsApplicationPlugin(outer), Throws.ArgumentException);
		Assert.That(nestedLoads, Is.Zero);
		Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(ApplicationPluginRegistration<FormsUI.IApplicationPlugin>)), Is.False);
		Assert.That(services.Any(descriptor => descriptor.IsKeyedService && Equals(descriptor.ServiceKey, "Reused")), Is.False);
		services.AddWinFormsApplicationPlugin(plugin => plugin
			.WithName("Reused")
			.AddInitializer<CallbackInitializer>(_ => new CallbackInitializer(() => calls.Add("Replacement initialized")))
			.AddFinalizer<CallbackFinalizer>(_ => new CallbackFinalizer(() => calls.Add("Replacement finalized")))
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
		var outer = new FormsUI.ApplicationPluginBuilder()
			.WithName("Outer")
			.AddInitializer<CallbackInitializer>(_ => new CallbackInitializer(() => calls.Add("Abandoned initialized")))
			.AddFinalizer<CallbackFinalizer>(_ => new CallbackFinalizer(() => calls.Add("Abandoned finalized")))
			.AddServices(components => {
				components.AddWinFormsApplicationPlugin(plugin => plugin
					.WithName("Nested")
					.AddInitializer<CallbackInitializer>(_ => new CallbackInitializer(() => calls.Add("Nested initialized")))
					.AddFinalizer<CallbackFinalizer>(_ => new CallbackFinalizer(() => calls.Add("Nested finalized")))
				);
				Guard.Ensure(false, "Outer loading failed.");
			})
			.Build();
		Assert.That(() => services.AddWinFormsApplicationPlugin(outer), Throws.InvalidOperationException);
		Assert.That(services.Any(descriptor => descriptor.IsKeyedService && Equals(descriptor.ServiceKey, "Outer")), Is.False);
		services.AddWinFormsApplicationPlugin(plugin => plugin
			.WithName("Outer")
			.AddInitializer<CallbackInitializer>(_ => new CallbackInitializer(() => calls.Add("Replacement initialized")))
			.AddFinalizer<CallbackFinalizer>(_ => new CallbackFinalizer(() => calls.Add("Replacement finalized")))
		);
		using var provider = services.BuildServiceProvider();
		var registrations = provider.GetServices<ApplicationPluginRegistration<FormsUI.IApplicationPlugin>>().ToArray();
		Assert.That(registrations.Select(registration => registration.Plugin.Name), Is.EqualTo(new[] { "Nested", "Outer" }));
		foreach (var registration in registrations)
			provider.GetKeyedServices<IApplicationInitializer>(registration.Plugin.Name).Single().Initialize();
		foreach (var registration in registrations)
			provider.GetKeyedServices<IApplicationFinalizer>(registration.Plugin.Name).Single().Finalize();
		Assert.That(calls, Is.EqualTo(new[] { "Nested initialized", "Replacement initialized", "Nested finalized", "Replacement finalized" }));
	}

	[Test]
	public async Task PluginsContributeIndividualNavigationBlocksInPositionOrder() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		using var first = new ApplicationBlockBuilder()
			.WithName("First")
			.WithPosition(30)
			.AddScreen<ProbeScreen>(screen => screen.WithTitle(nameof(ProbeScreen)))
			.WithDefaultScreen<ProbeScreen>()
			.Build();
		using var second = new ApplicationBlockBuilder()
			.WithName("Second")
			.WithPosition(20)
			.AddScreen<OtherScreen>(screen => screen.WithTitle(nameof(OtherScreen)))
			.WithDefaultScreen<OtherScreen>()
			.Build();
		using var third = new ApplicationBlock { Name = "Third", Position = 10 };
		services
			.AddWinFormsApplicationPlugin(plugin => plugin
				.WithName("First contribution")
				.AddBlock(first)
				.AddBlock(second)
			)
			.AddWinFormsApplicationPlugin(plugin => plugin
				.WithName("Second contribution")
				.AddBlock(third)
			);
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<FormsUI.IApplication>();
		await application.InitializeAsync();
		var form = (TrackingMainForm)application.BlockManager;
		var navigation = form.Controls.OfType<SplitContainer>().Single().Panel1.Controls.OfType<ApplicationBar>().Single();
		Assert.That(application.LoadedPlugins.Select(plugin => plugin.Name), Is.EqualTo(new[] { "First contribution", "Second contribution" }));
		Assert.That(application.Blocks, Is.EqualTo(new[] { third, second, first }));
		Assert.That(navigation.Items.Select(item => item.Text), Is.EqualTo(new[] { "Third", "Second", "First" }));
		Assert.That(form.RegisterCount, Is.EqualTo(3));
		Assert.That(application.ScreenHost.ActiveScreen, Is.TypeOf<ProbeScreen>(), "Startup follows contribution order independently of icon Position.");
		form.ClickNavigationButton(navigation.Items.Single(item => ReferenceEquals(item.Button.Tag, second)).Button);
		Assert.That(application.ScreenHost.SelectedBlock, Is.SameAs(second));
		form.ClickNavigationButton(navigation.Items.Single(item => ReferenceEquals(item.Button.Tag, third)).Button);
		Assert.That(application.ScreenHost.SelectedBlock, Is.SameAs(third));
		Assert.That(navigation.SelectedItem.Button.Tag, Is.SameAs(third));
		Assert.That(navigation.ApplicationBarControl, Is.SameAs(navigation.SelectedItem.MenuControl));
	}

	[Test]
	public async Task ServiceOnlyPluginIsLoadedWithoutCreatingANavigationBlock() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		var loads = 0;
		services.AddWinFormsApplicationPlugin(plugin => plugin
			.WithName("Services")
			.AddServices(registry => {
				loads++;
				registry.AddSingleton<StartupMarker>();
			})
		);
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<FormsUI.IApplication>();
		await application.InitializeAsync();
		Assert.That(loads, Is.EqualTo(1));
		Assert.That(provider.GetRequiredService<StartupMarker>(), Is.Not.Null);
		Assert.That(application.LoadedPlugins.Single().Name, Is.EqualTo("Services"));
		Assert.That(application.Blocks, Is.Empty);
		Assert.That(((TrackingMainForm)application.BlockManager).RegisteredBlocks, Is.Empty);
	}

	[Test]
	public async Task ManuallyRegisteredUnloadedPluginIsExcludedFromTheApplicationInventory() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		using var block = new ApplicationBlock { Name = "Unloaded block" };
		var loads = 0;
		var plugin = new FormsUI.ApplicationPlugin("Unloaded", new[] { block }, _ => loads++);
		services.AddSingleton<FormsUI.IApplicationPlugin>(plugin);
		services.AddSingleton<SharedUI.IApplicationPlugin>(plugin);
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<FormsUI.IApplication>();
		await application.InitializeAsync();
		Assert.That(provider.GetRequiredService<FormsUI.IApplicationPlugin>(), Is.SameAs(plugin));
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(application.Blocks, Is.Empty);
		Assert.That(loads, Is.Zero);
		Assert.That(block.Source, Is.Null);
		Assert.That(block.Parent, Is.Null);
	}

	[Test]
	public async Task FailedLoadDoesNotPublishARegistrationReceipt() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		using var block = new ApplicationBlock { Name = "Failed contribution" };
		var plugin = new FormsUI.ApplicationPlugin("Fails", new[] { block }, _ => Guard.Ensure(false, "Load failed."));
		Assert.That(() => services.AddWinFormsApplicationPlugin(plugin), Throws.InvalidOperationException);
		Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(ApplicationPluginRegistration<FormsUI.IApplicationPlugin>)), Is.False);
		Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(FormsUI.IApplicationPlugin)), Is.False);
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<FormsUI.IApplication>();
		await application.InitializeAsync();
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(application.Blocks, Is.Empty);
		Assert.That(block.Source, Is.Null);
		Assert.That(block.Parent, Is.Null);
	}

	[Test]
	public async Task PostLoadValidationFailureDoesNotPublishTheFailedPluginOrItsBlocks() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		using var owned = new ApplicationBlock { Name = "Owned" };
		using var initial = new ApplicationBlock { Name = "Initial" };
		var first = new FormsUI.ApplicationPlugin("First", new[] { owned });
		services.AddWinFormsApplicationPlugin(first);
		var failed = new MutablePlugin("Failed", new[] { initial });
		failed.Loaded += () => failed.CurrentBlocks = new[] { owned };
		Assert.That(() => services.AddWinFormsApplicationPlugin(failed), Throws.ArgumentException);
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<FormsUI.IApplication>();
		await application.InitializeAsync();
		Assert.That(application.LoadedPlugins, Is.EqualTo(new[] { first }));
		Assert.That(application.Blocks, Is.EqualTo(new[] { owned }));
		Assert.That(owned.Source, Is.SameAs(first));
		Assert.That(initial.Source, Is.Null);
		Assert.That(provider.GetServices<ApplicationPluginRegistration<FormsUI.IApplicationPlugin>>().Count(), Is.EqualTo(1));
	}

	[Test]
	public async Task RegistrationCapturesPostLoadMembershipAndPreservesItsArrayOwnership() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		using var initial = new ApplicationBlock { Name = "Initial" };
		using var contributed = new ApplicationBlockBuilder()
			.WithName("Contributed")
			.AddScreen<ProbeScreen>(screen => screen.WithTitle(nameof(ProbeScreen)))
			.WithDefaultScreen<ProbeScreen>()
			.Build();
		using var later = new ApplicationBlock { Name = "Later" };
		var plugin = new MutablePlugin("Mutable", new[] { initial });
		plugin.Loaded += () => plugin.CurrentBlocks = new[] { contributed };
		services.AddWinFormsApplicationPlugin(plugin);
		await using var provider = services.BuildServiceProvider();
		var registration = provider.GetRequiredService<ApplicationPluginRegistration<FormsUI.IApplicationPlugin>>();
		Assert.That(registration.Plugin, Is.SameAs(plugin));
		Assert.That(registration.Blocks, Is.EqualTo(new[] { contributed }));
		Assert.That(contributed.Source, Is.SameAs(plugin));
		Assert.That(contributed.Parent, Is.Null);
		Assert.That(initial.Source, Is.Null);
		registration.Blocks[0] = later;
		plugin.CurrentBlocks[0] = later;
		var application = provider.GetRequiredService<FormsUI.IApplication>();
		await application.InitializeAsync();
		Assert.That(application.LoadedPlugins.Single(), Is.SameAs(plugin));
		Assert.That(application.Blocks, Is.EqualTo(new[] { contributed }));
		Assert.That(contributed.Parent, Is.SameAs(application));
		Assert.That(((SharedUI.IApplicationBlock)contributed).Parent, Is.SameAs(application));
		Assert.That(((SharedUI.IApplicationBlock)contributed).Source, Is.SameAs(plugin));
		Assert.That(later.Source, Is.Null);
		Assert.That(application.ScreenHost.ActiveScreen.ApplicationBlock, Is.SameAs(contributed));
		Assert.That(registration.Blocks, Is.EqualTo(new[] { contributed }));
	}

	[Test]
	public void DuplicateValidationUsesCapturedContributionsAfterAPluginGetterChanges() {
		using var contributed = new ApplicationBlock { Name = "Contributed" };
		using var later = new ApplicationBlock { Name = "Later" };
		var first = new MutablePlugin("First", new[] { contributed });
		var services = new ServiceCollection();
		services.AddWinFormsApplicationPlugin(first);
		first.CurrentBlocks = new[] { later };
		var loads = 0;
		var duplicate = new FormsUI.ApplicationPlugin("Second", new[] { contributed }, _ => loads++);
		Assert.That(() => services.AddWinFormsApplicationPlugin(duplicate), Throws.ArgumentException);
		Assert.That(loads, Is.Zero);
		Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(ApplicationPluginRegistration<FormsUI.IApplicationPlugin>)), Is.EqualTo(1));
	}

	[Test]
	public async Task RemovingAContributedBlockUpdatesLiveMembershipWithoutUnloadingItsPlugin() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		var block = new TrackingBlock { Name = "Contributed" };
		var plugin = new FormsUI.ApplicationPlugin("Loaded", new[] { block });
		var unloads = 0;
		plugin.Unloaded += () => unloads++;
		services.AddWinFormsApplicationPlugin(plugin);
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<FormsUI.IApplication>();
		await application.InitializeAsync();
		Assert.That(block.Parent, Is.SameAs(application));
		Assert.That(block.Source, Is.SameAs(plugin));
		application.BlockManager.UnregisterBlock(block);
		Assert.That(application.Blocks, Is.Empty);
		Assert.That(((SharedUI.IApplication)application).Blocks, Is.Empty);
		Assert.That(application.LoadedPlugins.Single(), Is.SameAs(plugin));
		Assert.That(unloads, Is.Zero);
		Assert.That(block.DisposeCount, Is.EqualTo(1));
		Assert.That(block.Parent, Is.Null);
		Assert.That(block.Source, Is.SameAs(plugin));
		application.BlockManager.RegisterBlock(block);
		Assert.That(block.Parent, Is.SameAs(application));
		Assert.That(block.Source, Is.SameAs(plugin));
		await application.DisposeAsync();
		Assert.That(block.Parent, Is.Null);
		Assert.That(block.Source, Is.SameAs(plugin));
		Assert.That(unloads, Is.Zero);
	}

	public sealed class TrackingMainForm : BlockMainForm {
		public int RegisterCount { get; private set; }

		public void ClickNavigationButton(SquareButton button) => InvokeOnClick(button, EventArgs.Empty);

		public override void RegisterBlock(FormsUI.IApplicationBlock block) {
			RegisterCount++;
			base.RegisterBlock(block);
		}
	}

	public sealed class LegacyTransientBlock : ApplicationBlock {
		public LegacyTransientBlock() => Name = "Legacy transient";
	}

	public sealed class ProbeScreen : ApplicationScreen {
	}

	public sealed class OtherScreen : ApplicationScreen {
	}

	public sealed class StartupMarker {
	}

	private sealed class TrackingBlock : ApplicationBlock {
		public int DisposeCount { get; private set; }

		public override void Dispose() {
			DisposeCount++;
			base.Dispose();
		}
	}

	private sealed class MutablePlugin : FormsUI.ApplicationPlugin {
		public MutablePlugin(string name, FormsUI.IApplicationBlock[] blocks, Action<IServiceCollection> configureServices = null)
			: base(name, blocks, configureServices) {
			CurrentBlocks = blocks;
		}

		public FormsUI.IApplicationBlock[] CurrentBlocks { get; set; }

		public override FormsUI.IApplicationBlock[] Blocks {
			get => CurrentBlocks;
			init => base.Blocks = value;
		}
	}

	private sealed class MutablePluginBuilder : FormsUI.ApplicationPluginBuilder {
		protected override FormsUI.ApplicationPlugin CreatePlugin(FormsUI.IApplicationBlock[] blocks, Action<IServiceCollection> configureServices)
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
}
