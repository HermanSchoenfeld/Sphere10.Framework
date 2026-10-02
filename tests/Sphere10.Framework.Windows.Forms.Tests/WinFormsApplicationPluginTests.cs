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
			Sphere10Framework.Instance.Build().Start();
	}

	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	[Test]
	public void SharedStorageCopiesMembershipWithoutCopyingOrDisposingNativeDefinitions() {
		using var block = new TrackingBlock { Id = "work", Name = "Workspace" };
		var configured = 0;
		var loaded = 0;
		var unloaded = 0;
		var plugin = new WinFormsApplicationPluginBuilder().WithName("Features").AddBlock(block)
			.ConfigureServices(_ => configured++).Build();
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
		using var block = new WinFormsApplicationBlockBuilder().WithId("workspace").WithName("Workspace")
			.AddMenu(menu => menu.WithText("Commands").AddActionItem("Startup", () => startupActions++, executeOnLoad: true)).Build();
		var plugin = new WinFormsApplicationPluginBuilder().WithName("Workspace plugin").AddBlock(block)
			.ConfigureServices(registry => registry.AddSingleton<StartupMarker>()).Build();
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
		Assert.That(shared.Plugins.Single(), Is.SameAs(application.Plugins.Single()));
		application.Initialize();
		application.Initialize();
		Assert.That(application.Blocks.Single(), Is.SameAs(block));
		Assert.That(((TrackingMainForm)provider.GetRequiredService<IMainForm>()).RegisterCount, Is.EqualTo(1));
		Assert.That(startupActions, Is.EqualTo(1));
		Assert.That(loaded, Is.EqualTo(1));
		application.ScreenHost.ActivateScreen(block, typeof(ProbeScreen));
		Assert.That(application.ActivePlugin, Is.SameAs(plugin));
		Assert.That(shared.ActivePlugin, Is.SameAs(plugin));
		application.Plugins[0] = null;
		shared.Plugins[0] = null;
		Assert.That(application.Plugins.Single(), Is.SameAs(plugin));
		application.Dispose();
		Assert.That(unloaded, Is.Zero, "Disposing the application adapter must not unload shared startup definitions.");
	}

	[Test]
	public void CallbackRegistrationAndLegacyBlocksShareOneCollisionSafeHierarchy() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		using var explicitBlock = new WinFormsApplicationBlock { Id = "plugin-block", Name = "Plugin block", Position = 2 };
		using var legacyBlock = new WinFormsApplicationBlock { Id = "legacy", Name = "Legacy", Position = 1 };
		services.AddWinFormsApplicationPlugin(plugin => plugin.WithName("Application").AddBlock(explicitBlock));
		services.AddApplicationBlock(legacyBlock);
		services.AddApplicationBlock(explicitBlock);
		using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IWinFormsApplication>();
		application.Initialize();
		Assert.That(application.Plugins.Select(plugin => plugin.Name), Is.EqualTo(new[] { "Application", "Application 2" }));
		Assert.That(application.Plugins[0].Blocks, Is.EqualTo(new[] { explicitBlock }));
		Assert.That(application.Plugins[1].Blocks, Is.EqualTo(new[] { legacyBlock }));
		Assert.That(application.Blocks, Is.EqualTo(new[] { legacyBlock, explicitBlock }));
		Assert.That(((TrackingMainForm)application.BlockManager).RegisterCount, Is.EqualTo(2));
		application.ScreenHost.ActivateScreen(legacyBlock, typeof(ProbeScreen));
		Assert.That(application.ActivePlugin, Is.SameAs(application.Plugins[1]));
		application.ScreenHost.ActivateScreen(explicitBlock, typeof(OtherScreen));
		Assert.That(application.ActivePlugin, Is.SameAs(application.Plugins[0]));
	}

	[Test]
	public void LegacyConstructorGroupsBlocksWithoutChangingStartupOrOwnership() {
		using var form = new TrackingMainForm();
		using var block = new TrackingBlock { Name = "Existing block" };
		using var application = new WinFormsApplication(form, new[] { block });
		Assert.That(application.Plugins.Single().Name, Is.EqualTo("Application"));
		Assert.That(application.Plugins.Single().Blocks.Single(), Is.SameAs(block));
		application.Initialize();
		Assert.That(form.RegisterCount, Is.EqualTo(1));
		Assert.That(application.ActivePlugin, Is.SameAs(application.Plugins.Single()));
		application.Dispose();
		Assert.That(block.DisposeCount, Is.Zero);
		Assert.That(form.IsDisposed, Is.False);
	}

	[Test]
	public void LegacyTransientBlockIsMaterializedOnceForTheRuntimeGrouping() {
		var services = new ServiceCollection();
		services.AddMainForm<TrackingMainForm>();
		services.AddApplicationBlock<LegacyTransientBlock>();
		using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IWinFormsApplication>();
		application.Initialize();
		Assert.That(application.Plugins.Single().Blocks.Single(), Is.SameAs(application.Blocks.Single()));
		Assert.That(provider.GetRequiredService<IApplication>().Plugins.Single(), Is.SameAs(application.Plugins.Single()));
		Assert.That(((TrackingMainForm)application.BlockManager).RegisterCount, Is.EqualTo(1));
	}

	[Test]
	public void ImplicitPluginTracksAlreadyRegisteredAndLaterBlocksWithStableIdentity() {
		using var form = new TrackingMainForm();
		var initial = new WinFormsApplicationBlock { Name = "Before adapter" };
		var later = new TrackingBlock { Name = "After adapter" };
		form.RegisterBlock(initial);
		using var application = new WinFormsApplication(form, Array.Empty<IWinFormsApplicationBlock>());
		IApplication shared = application;
		var implicitPlugin = application.Plugins.Single();
		Assert.That(implicitPlugin.Blocks.Single(), Is.SameAs(initial));
		application.Initialize();
		form.RegisterBlock(later);
		Assert.That(application.Plugins.Single(), Is.SameAs(implicitPlugin));
		Assert.That(shared.Plugins.Single().Blocks, Is.EqualTo(new[] { initial, later }));
		form.ActiveBlock = later;
		Assert.That(application.ActivePlugin, Is.SameAs(implicitPlugin));
		form.UnregisterBlock(later);
		Assert.That(application.Plugins.Single(), Is.SameAs(implicitPlugin));
		Assert.That(implicitPlugin.Blocks, Is.EqualTo(new[] { initial }));
		Assert.That(later.DisposeCount, Is.EqualTo(1), "The existing block manager remains the only owner during removal.");
		using var last = new WinFormsApplicationBlock { Name = "Last block" };
		form.UnregisterBlock(initial);
		Assert.That(application.Plugins, Is.Empty);
		form.RegisterBlock(last);
		Assert.That(application.Plugins.Single(), Is.SameAs(implicitPlugin), "Temporary emptiness must not replace the implicit grouping.");
		Assert.That(shared.ActivePlugin, Is.SameAs(implicitPlugin));
	}

	[Test]
	public void DuplicatePluginNamesAreRejectedBeforeLoadingServices() {
		var services = new ServiceCollection();
		services.AddWinFormsApplicationPlugin(plugin => plugin.WithName("Duplicate"));
		var loads = 0;
		var duplicate = new WinFormsApplicationPluginBuilder().WithName("Duplicate").ConfigureServices(_ => loads++).Build();
		Assert.That(() => services.AddWinFormsApplicationPlugin(duplicate), Throws.ArgumentException);
		Assert.That(loads, Is.Zero);
		Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(IWinFormsApplicationPlugin)), Is.EqualTo(1));
	}

	[Test]
	public void BlocksCannotBeOwnedByTwoPlugins() {
		using var block = new WinFormsApplicationBlock { Id = "same", Name = "Block" };
		var services = new ServiceCollection();
		services.AddWinFormsApplicationPlugin(plugin => plugin.WithName("First").AddBlock(block));
		Assert.That(() => services.AddWinFormsApplicationPlugin(plugin => plugin.WithName("Second").AddBlock(block)), Throws.ArgumentException);
	}

	public sealed class TrackingMainForm : BlockMainForm {
		private int _registerCount;

		public int RegisterCount => _registerCount;

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
}
