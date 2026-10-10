// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class WinFormsApplicationPluginTests {
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
		var plugin = new WinFormsApplicationPluginBuilder()
			.WithName("Features")
			.AddBlock(block)
			.ConfigureServices(_ => configured++)
			.Build();
		IApplicationPlugin shared = plugin;
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
	public void RegistrationLoadsOnceAndInitializesTheExistingBlockPathOnce() {
		var startupActions = 0;
		var loaded = 0;
		var unloaded = 0;
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		using var block = new WinFormsApplicationBlockBuilder()
			.WithName("Workspace")
			.ConfigureMenu(menu => menu
				.WithText("Commands")
				.AddActionItem("Startup", () => startupActions++, executeOnLoad: true)
			)
			.Build();
		var plugin = new WinFormsApplicationPluginBuilder()
			.WithName("Workspace plugin")
			.AddBlock(block)
			.ConfigureServices(registry => registry.AddSingleton<StartupMarker>())
			.Build();
		plugin.Loaded += () => loaded++;
		plugin.Unloaded += () => unloaded++;
		services.AddWinFormsApplicationPlugin(plugin);
		Assert.That(loaded, Is.EqualTo(1));
		using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IWinFormsApplication>();
		var shared = provider.GetRequiredService<IApplication>();
		Assert.That(shared, Is.SameAs(application));
		Assert.That(provider.GetRequiredService<StartupMarker>(), Is.Not.Null);
		Assert.That(provider.GetRequiredService<IWinFormsApplicationPlugin>(), Is.SameAs(plugin));
		Assert.That(provider.GetRequiredService<IApplicationPlugin>(), Is.SameAs(plugin));
		Assert.That(shared.LoadedPlugins.Single(), Is.SameAs(application.LoadedPlugins.Single()));
		application.Initialize();
		application.Initialize();
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
		application.Dispose();
		Assert.That(unloaded, Is.Zero, "Disposing the application adapter must not unload shared startup definitions.");
	}

	[Test]
	public void CallbackRegistrationAndDirectBlocksShareTheLiveApplicationMembership() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		using var explicitBlock = new WinFormsApplicationBlock { Name = "Plugin block", Position = 2 };
		using var legacyBlock = new WinFormsApplicationBlock { Name = "Legacy", Position = 1 };
		services
			.AddWinFormsApplicationPlugin(plugin => plugin
				.WithName("Application")
				.AddBlock(explicitBlock)
			);
		services.AddApplicationBlock(legacyBlock);
		services.AddApplicationBlock(explicitBlock);
		using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IWinFormsApplication>();
		application.Initialize();
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
	public void DirectBlockConstructorPreservesStartupAndOwnershipWithoutCreatingAPlugin() {
		using var form = new TrackingMainForm();
		using var block = new TrackingBlock { Name = "Existing block" };
		using var application = new WinFormsApplication(form, new[] { block });
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(application.Blocks, Is.Empty);
		application.Initialize();
		Assert.That(form.RegisterCount, Is.EqualTo(1));
		Assert.That(application.Blocks.Single(), Is.SameAs(block));
		Assert.That(application.ScreenHost.ActiveBlock, Is.SameAs(block));
		application.Dispose();
		Assert.That(block.DisposeCount, Is.Zero);
		Assert.That(form.IsDisposed, Is.False);
	}

	[Test]
	public void DirectTransientBlockIsMaterializedOnceForRuntimeMembership() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		services.AddApplicationBlock<LegacyTransientBlock>();
		using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IWinFormsApplication>();
		application.Initialize();
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(application.Blocks.Single(), Is.SameAs(application.ScreenHost.Blocks.Single()));
		Assert.That(provider.GetRequiredService<IApplication>().Blocks.Single(), Is.SameAs(application.Blocks.Single()));
		Assert.That(((TrackingMainForm)application.BlockManager).RegisterCount, Is.EqualTo(1));
	}

	[Test]
	public void AlreadyLoadedPluginConstructorBindsCapturedProvenanceWithoutLoadingAgain() {
		using var form = new TrackingMainForm();
		using var block = new WinFormsApplicationBlock { Name = "Loaded contribution" };
		var loads = 0;
		var plugin = new WinFormsApplicationPlugin("Loaded", new[] { block }, _ => loads++);
		plugin.Load(new ServiceCollection());
		Assert.That(block.Source, Is.Null);
		using var application = new WinFormsApplication(form, Array.Empty<IWinFormsApplicationBlock>(), new[] { plugin });
		Assert.That(block.Source, Is.SameAs(plugin));
		Assert.That(block.Parent, Is.Null);
		Assert.That(loads, Is.EqualTo(1));
		application.Initialize();
		Assert.That(block.Parent, Is.SameAs(application));
		Assert.That(application.LoadedPlugins.Single(), Is.SameAs(plugin));
	}

	[Test]
	public void LiveMembershipTracksAlreadyRegisteredAndLaterBlocksWithoutCreatingAPlugin() {
		using var form = new TrackingMainForm();
		var initial = new WinFormsApplicationBlock { Name = "Before adapter" };
		var later = new TrackingBlock { Name = "After adapter" };
		form.RegisterBlock(initial);
		using var application = new WinFormsApplication(form, Array.Empty<IWinFormsApplicationBlock>());
		IApplication shared = application;
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(application.Blocks.Single(), Is.SameAs(initial));
		application.Initialize();
		form.RegisterBlock(later);
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(shared.Blocks, Is.EqualTo(new[] { initial, later }));
		Assert.That(form.Blocks, Is.EqualTo(shared.Blocks));
		shared.Blocks[0] = null;
		application.Blocks[0] = null;
		form.Blocks[0] = null;
		Assert.That(application.Blocks[0], Is.SameAs(initial));
		form.ActiveBlock = later;
		Assert.That(application.ScreenHost.ActiveBlock, Is.SameAs(later));
		form.UnregisterBlock(later);
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(application.Blocks, Is.EqualTo(new[] { initial }));
		Assert.That(later.DisposeCount, Is.EqualTo(1), "The existing block manager remains the only owner during removal.");
		using var last = new WinFormsApplicationBlock { Name = "Last block" };
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
			);
		var loads = 0;
		var duplicate = new WinFormsApplicationPluginBuilder()
			.WithName("Duplicate")
			.ConfigureServices(_ => loads++)
			.Build();
		Assert.That(() => services.AddWinFormsApplicationPlugin(duplicate), Throws.ArgumentException);
		Assert.That(loads, Is.Zero);
		Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(IWinFormsApplicationPlugin)), Is.EqualTo(1));
	}

	[Test]
	public void BlocksCannotBeOwnedByTwoPlugins() {
		using var block = new WinFormsApplicationBlock { Name = "Block" };
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

	[Test]
	public void PluginsContributeIndividualNavigationBlocksInPositionOrder() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		using var first = new WinFormsApplicationBlockBuilder()
			.WithName("First")
			.WithPosition(30)
			.WithDefaultScreen<ProbeScreen>()
			.Build();
		using var second = new WinFormsApplicationBlockBuilder()
			.WithName("Second")
			.WithPosition(20)
			.WithDefaultScreen<OtherScreen>()
			.Build();
		using var third = new WinFormsApplicationBlock { Name = "Third", Position = 10 };
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
		using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IWinFormsApplication>();
		application.Initialize();
		var form = (TrackingMainForm)application.BlockManager;
		var navigation = form.Controls.OfType<SplitContainer>().Single().Panel1.Controls.OfType<ApplicationBar>().Single();
		Assert.That(application.LoadedPlugins.Select(plugin => plugin.Name), Is.EqualTo(new[] { "First contribution", "Second contribution" }));
		Assert.That(application.Blocks, Is.EqualTo(new[] { third, second, first }));
		Assert.That(navigation.Items.Select(item => item.Text), Is.EqualTo(new[] { "Third", "Second", "First" }));
		Assert.That(form.RegisterCount, Is.EqualTo(3));
		Assert.That(application.ScreenHost.ActiveScreen, Is.TypeOf<ProbeScreen>(), "Startup follows contribution order independently of icon Position.");
		form.ClickNavigationButton(navigation.Items.Single(item => ReferenceEquals(item.MenuControl, form.BlockBindings[second])).Button);
		Assert.That(application.ScreenHost.ActiveBlock, Is.SameAs(second));
		form.ClickNavigationButton(navigation.Items.Single(item => ReferenceEquals(item.MenuControl, form.BlockBindings[third])).Button);
		Assert.That(application.ScreenHost.ActiveBlock, Is.SameAs(third));
		Assert.That(navigation.ApplicationBarControl, Is.SameAs(form.BlockBindings[third]));
	}

	[Test]
	public void ServiceOnlyPluginIsLoadedWithoutCreatingANavigationBlock() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		var loads = 0;
		services.AddWinFormsApplicationPlugin(plugin => plugin
			.WithName("Services")
			.ConfigureServices(registry => {
				loads++;
				registry.AddSingleton<StartupMarker>();
			})
		);
		using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IWinFormsApplication>();
		application.Initialize();
		Assert.That(loads, Is.EqualTo(1));
		Assert.That(provider.GetRequiredService<StartupMarker>(), Is.Not.Null);
		Assert.That(application.LoadedPlugins.Single().Name, Is.EqualTo("Services"));
		Assert.That(application.Blocks, Is.Empty);
		Assert.That(((TrackingMainForm)application.BlockManager).BlockBindings, Is.Empty);
	}

	[Test]
	public void ManuallyRegisteredUnloadedPluginIsExcludedFromTheApplicationInventory() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		using var block = new WinFormsApplicationBlock { Name = "Unloaded block" };
		var loads = 0;
		var plugin = new WinFormsApplicationPlugin("Unloaded", new[] { block }, _ => loads++);
		services.AddSingleton<IWinFormsApplicationPlugin>(plugin);
		services.AddSingleton<IApplicationPlugin>(plugin);
		using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IWinFormsApplication>();
		application.Initialize();
		Assert.That(provider.GetRequiredService<IWinFormsApplicationPlugin>(), Is.SameAs(plugin));
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(application.Blocks, Is.Empty);
		Assert.That(loads, Is.Zero);
		Assert.That(block.Source, Is.Null);
		Assert.That(block.Parent, Is.Null);
	}

	[Test]
	public void FailedLoadDoesNotPublishARegistrationReceipt() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		using var block = new WinFormsApplicationBlock { Name = "Failed contribution" };
		var plugin = new WinFormsApplicationPlugin("Fails", new[] { block }, _ => Guard.Ensure(false, "Load failed."));
		Assert.That(() => services.AddWinFormsApplicationPlugin(plugin), Throws.InvalidOperationException);
		Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(ApplicationPluginRegistration<IWinFormsApplicationPlugin>)), Is.False);
		Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(IWinFormsApplicationPlugin)), Is.False);
		using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IWinFormsApplication>();
		application.Initialize();
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(application.Blocks, Is.Empty);
		Assert.That(block.Source, Is.Null);
		Assert.That(block.Parent, Is.Null);
	}

	[Test]
	public void PostLoadValidationFailureDoesNotPublishTheFailedPluginOrItsBlocks() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		using var owned = new WinFormsApplicationBlock { Name = "Owned" };
		using var initial = new WinFormsApplicationBlock { Name = "Initial" };
		var first = new WinFormsApplicationPlugin("First", new[] { owned });
		services.AddWinFormsApplicationPlugin(first);
		var failed = new MutablePlugin("Failed", new[] { initial });
		failed.Loaded += () => failed.CurrentBlocks = new[] { owned };
		Assert.That(() => services.AddWinFormsApplicationPlugin(failed), Throws.ArgumentException);
		using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IWinFormsApplication>();
		application.Initialize();
		Assert.That(application.LoadedPlugins, Is.EqualTo(new[] { first }));
		Assert.That(application.Blocks, Is.EqualTo(new[] { owned }));
		Assert.That(owned.Source, Is.SameAs(first));
		Assert.That(initial.Source, Is.Null);
		Assert.That(provider.GetServices<ApplicationPluginRegistration<IWinFormsApplicationPlugin>>().Count(), Is.EqualTo(1));
	}

	[Test]
	public void RegistrationCapturesPostLoadMembershipAndPreservesItsArrayOwnership() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		using var initial = new WinFormsApplicationBlock { Name = "Initial" };
		using var contributed = new WinFormsApplicationBlockBuilder()
			.WithName("Contributed")
			.WithDefaultScreen<ProbeScreen>()
			.Build();
		using var later = new WinFormsApplicationBlock { Name = "Later" };
		var plugin = new MutablePlugin("Mutable", new[] { initial });
		plugin.Loaded += () => plugin.CurrentBlocks = new[] { contributed };
		services.AddWinFormsApplicationPlugin(plugin);
		using var provider = services.BuildServiceProvider();
		var registration = provider.GetRequiredService<ApplicationPluginRegistration<IWinFormsApplicationPlugin>>();
		Assert.That(registration.Plugin, Is.SameAs(plugin));
		Assert.That(registration.Blocks, Is.EqualTo(new[] { contributed }));
		Assert.That(contributed.Source, Is.SameAs(plugin));
		Assert.That(contributed.Parent, Is.Null);
		Assert.That(initial.Source, Is.Null);
		registration.Blocks[0] = later;
		plugin.CurrentBlocks[0] = later;
		var application = provider.GetRequiredService<IWinFormsApplication>();
		application.Initialize();
		Assert.That(application.LoadedPlugins.Single(), Is.SameAs(plugin));
		Assert.That(application.Blocks, Is.EqualTo(new[] { contributed }));
		Assert.That(contributed.Parent, Is.SameAs(application));
		Assert.That(((IApplicationBlock)contributed).Parent, Is.SameAs(application));
		Assert.That(((IApplicationBlock)contributed).Source, Is.SameAs(plugin));
		Assert.That(later.Source, Is.Null);
		Assert.That(application.ScreenHost.ActiveScreen.ApplicationBlock, Is.SameAs(contributed));
		Assert.That(registration.Blocks, Is.EqualTo(new[] { contributed }));
	}

	[Test]
	public void DuplicateValidationUsesCapturedContributionsAfterAPluginGetterChanges() {
		using var contributed = new WinFormsApplicationBlock { Name = "Contributed" };
		using var later = new WinFormsApplicationBlock { Name = "Later" };
		var first = new MutablePlugin("First", new[] { contributed });
		var services = new ServiceCollection();
		services.AddWinFormsApplicationPlugin(first);
		first.CurrentBlocks = new[] { later };
		var loads = 0;
		var duplicate = new WinFormsApplicationPlugin("Second", new[] { contributed }, _ => loads++);
		Assert.That(() => services.AddWinFormsApplicationPlugin(duplicate), Throws.ArgumentException);
		Assert.That(loads, Is.Zero);
		Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(ApplicationPluginRegistration<IWinFormsApplicationPlugin>)), Is.EqualTo(1));
	}

	[Test]
	public void RemovingAContributedBlockUpdatesLiveMembershipWithoutUnloadingItsPlugin() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		var block = new TrackingBlock { Name = "Contributed" };
		var plugin = new WinFormsApplicationPlugin("Loaded", new[] { block });
		var unloads = 0;
		plugin.Unloaded += () => unloads++;
		services.AddWinFormsApplicationPlugin(plugin);
		using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IWinFormsApplication>();
		application.Initialize();
		Assert.That(block.Parent, Is.SameAs(application));
		Assert.That(block.Source, Is.SameAs(plugin));
		application.BlockManager.UnregisterBlock(block);
		Assert.That(application.Blocks, Is.Empty);
		Assert.That(((IApplication)application).Blocks, Is.Empty);
		Assert.That(application.LoadedPlugins.Single(), Is.SameAs(plugin));
		Assert.That(unloads, Is.Zero);
		Assert.That(block.DisposeCount, Is.EqualTo(1));
		Assert.That(block.Parent, Is.Null);
		Assert.That(block.Source, Is.SameAs(plugin));
		application.BlockManager.RegisterBlock(block);
		Assert.That(block.Parent, Is.SameAs(application));
		Assert.That(block.Source, Is.SameAs(plugin));
		application.Dispose();
		Assert.That(block.Parent, Is.Null);
		Assert.That(block.Source, Is.SameAs(plugin));
		Assert.That(unloads, Is.Zero);
	}

	public sealed class TrackingMainForm : BlockMainForm {
		private int _registerCount;

		public int RegisterCount => _registerCount;

		public void ClickNavigationButton(SquareButton button) => InvokeOnClick(button, EventArgs.Empty);

		public override void RegisterBlock(IWinFormsApplicationBlock block) {
			_registerCount++;
			base.RegisterBlock(block);
		}
	}

	public sealed class LegacyTransientBlock : WinFormsApplicationBlock {
		public LegacyTransientBlock() => Name = "Legacy transient";
	}

	public sealed class ProbeScreen : WinFormsApplicationScreen {
	}

	public sealed class OtherScreen : WinFormsApplicationScreen {
	}

	public sealed class StartupMarker {
	}

	private sealed class TrackingBlock : WinFormsApplicationBlock {
		public int DisposeCount { get; private set; }

		public override void Dispose() {
			DisposeCount++;
			base.Dispose();
		}
	}

	private sealed class MutablePlugin : WinFormsApplicationPlugin {
		public MutablePlugin(string name, IWinFormsApplicationBlock[] blocks)
			: base(name, blocks) {
			CurrentBlocks = blocks;
		}

		public IWinFormsApplicationBlock[] CurrentBlocks { get; set; }

		public override IWinFormsApplicationBlock[] Blocks {
			get => CurrentBlocks;
			init => base.Blocks = value;
		}
	}
}
