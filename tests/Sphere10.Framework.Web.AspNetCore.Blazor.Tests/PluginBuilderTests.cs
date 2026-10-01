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
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class PluginBuilderTests {
	[Test]
	public void FluentConfigurationUsesTheExistingBlockAndMenuBuilders() {
		var plugin = new BlazorPluginBuilder().WithName("Workspace")
			.AddBlock(block => block.WithId("work").WithName("Work").WithPosition(5).WithDefaultScreen<TestScreen>("Overview")
				.AddMenu(menu => menu.WithId("screens").WithText("Screens").AddScreenItem<TestScreen>("overview", "Overview")))
			.Build();
		var block = plugin.Blocks.Single();

		Assert.That(plugin.Name, Is.EqualTo("Workspace"));
		Assert.That(block.Id, Is.EqualTo("work"));
		Assert.That(block.Position, Is.EqualTo(5));
		Assert.That(block.DefaultScreen, Is.EqualTo(typeof(TestScreen)));
		Assert.That(block.Menus.Single().Items.Single().Id, Is.EqualTo("overview"));
		Assert.That(plugin.IoCContainer, Is.Null);
	}

	[Test]
	public void RepeatedBuildsHaveIndependentNamesBlocksAndServiceCallbacks() {
		var callbacks = new List<string>();
		var builder = new BlazorPluginBuilder().WithName("First")
			.AddBlock(block => block.WithId("first").WithName("First"))
			.ConfigureServices(_ => callbacks.Add("first"));
		var first = builder.Build();
		var second = builder.WithName("Second").AddBlock(block => block.WithId("second").WithName("Second"))
			.ConfigureServices(_ => callbacks.Add("second")).Build();

		Assert.That(callbacks, Is.Empty, "Building metadata must not execute service registrations.");
		first.Load(new ServiceCollection());
		second.Load(new ServiceCollection());
		Assert.That(callbacks, Is.EqualTo(new[] { "first", "first", "second" }));
		Assert.That(first.Name, Is.EqualTo("First"));
		Assert.That(first.Blocks.Select(block => block.Id), Is.EqualTo(new[] { "first" }));
		Assert.That(second.Blocks.Select(block => block.Id), Is.EqualTo(new[] { "first", "second" }));
	}

	[Test]
	public void PluginSnapshotsInputMetadataAndReturnsDefensiveBlockArrays() {
		var parameters = new Dictionary<string, object> { ["Message"] = "Original" };
		var items = new IBlazorApplicationMenuItem[] {
			new BlazorScreenMenuItem { Id = "screen", Title = "Screen", ScreenType = typeof(TestScreen), Parameters = parameters }
		};
		var blocks = new[] { new BlazorApplicationBlock { Id = "work", Title = "Work", Menus = new[] { new BlazorApplicationMenu { Text = "Menu", Items = items } } } };
		var plugin = new BlazorPluginBuilder().WithName("Workspace").AddBlock(blocks[0]).Build();
		items[0] = null;
		parameters["Message"] = "Changed";
		blocks[0] = null;
		plugin.Blocks[0] = null;

		var screen = (BlazorScreenMenuItem)plugin.Blocks.Single().Menus.Single().Items.Single();
		Assert.That(screen.Parameters["Message"], Is.EqualTo("Original"));
		Assert.That(() => ((IDictionary<string, object>)screen.Parameters)["Message"] = "Changed", Throws.TypeOf<NotSupportedException>());
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase(" ")]
	public void InvalidNamesAreRejectedImmediately(string name) {
		Assert.That(() => new BlazorPluginBuilder().WithName(name), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => new BlazorPlugin(name, Array.Empty<IBlazorApplicationBlock>()), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void MissingNameIsRejectedAtBuild() {
		Assert.That(() => new BlazorPluginBuilder().Build(), Throws.TypeOf<InvalidOperationException>());
	}

	[Test]
	public void NullConfigurationArgumentsAreRejected() {
		var builder = new BlazorPluginBuilder();
		Assert.That(() => builder.AddBlock((IBlazorApplicationBlock)null), Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => builder.AddBlock((Action<BlazorApplicationBlockBuilder>)null), Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => builder.ConfigureServices(null), Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => new ServiceCollection().AddSphere10BlazorPlugin((Action<BlazorPluginBuilder>)null), Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => new ServiceCollection().AddSphere10BlazorPlugin((IBlazorPlugin)null), Throws.TypeOf<ArgumentNullException>());
	}

	[Test]
	public void DuplicateBlockIdsAreRejectedAtBuild() {
		var builder = new BlazorPluginBuilder().WithName("Workspace")
			.AddBlock(block => block.WithId("same").WithName("First"))
			.AddBlock(block => block.WithId("same").WithName("Second"));
		Assert.That(() => builder.Build(), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void ConflictingScreenPoliciesAreRejectedAcrossPluginBlocks() {
		var builder = new BlazorPluginBuilder().WithName("Workspace")
			.AddBlock(block => block.WithId("single").WithName("Single").WithDefaultScreen<TestScreen>())
			.AddBlock(block => block.WithId("multiple").WithName("Multiple")
				.AddMenu(menu => menu.WithText("Screens").AddScreenItem<TestScreen>("screen", "Screen", ScreenActivationMode.MultiInstance)));
		Assert.That(() => builder.Build(), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void RegistrationAllowsServiceOnlyPluginsAndNotifiesAfterConfiguringServices() {
		var services = new ServiceCollection();
		var plugin = new BlazorPluginBuilder().WithName("Services").ConfigureServices(collection => collection.AddScoped<ScopedCounter>()).Build();
		var loaded = 0;
		plugin.Loaded += () => {
			Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(ScopedCounter)), Is.True);
			loaded++;
		};
		var result = services.AddSphere10BlazorPlugin(plugin);
		using var provider = services.BuildServiceProvider();

		Assert.That(result, Is.SameAs(services));
		Assert.That(loaded, Is.EqualTo(1));
		Assert.That(provider.GetServices<IBlazorPlugin>().Single(), Is.SameAs(plugin));
		Assert.That(provider.GetRequiredService<IBlazorApplicationBlockCatalog>().Blocks, Is.Empty);
	}

	[Test]
	public async Task LoadedMenuSubscribersReachTheRegisteredHostActionOnce() {
		var selected = 0;
		var executed = 0;
		var plugin = new BlazorPluginBuilder().WithName("Workspace")
			.AddBlock(block => block.WithId("work").WithName("Work")
				.AddMenu(menu => menu.WithText("Actions").AddActionItem("run", "Run", () => executed++))).Build();
		plugin.Loaded += () => plugin.Blocks.Single().Menus.Single().Items.Single().Select += () => selected++;
		var services = new ServiceCollection().AddSphere10BlazorPlugin(plugin);
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		using var scope = provider.CreateScope();

		await scope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>().ExecuteMenuItemAsync("work", "run");

		Assert.That(executed, Is.EqualTo(1));
		Assert.That(selected, Is.EqualTo(1));
	}

	[Test]
	public void RegistrationRejectsDuplicateBlocksAddedByStartupCallbacks() {
		var services = new ServiceCollection();
		var plugin = new BlazorPluginBuilder().WithName("Workspace")
			.AddBlock(block => block.WithId("work").WithName("Plugin block"))
			.ConfigureServices(collection => collection.AddApplicationBlock(block => block.WithId("work").WithName("Callback block"))).Build();

		Assert.That(() => services.AddSphere10BlazorPlugin(plugin), Throws.InstanceOf<ArgumentException>());
		Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(IBlazorPlugin)), Is.False);
	}

	[Test]
	public async Task CustomPluginCanFinishItsBlockDefinitionsDuringLoad() {
		var plugin = new LateBlockPlugin();
		Assert.That(plugin.Blocks, Is.Empty);
		var services = new ServiceCollection().AddSphere10BlazorPlugin(plugin);
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		using var scope = provider.CreateScope();
		var host = scope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var session = await host.ActivateBlockAsync("late");

		Assert.That(host.Catalog.Blocks.Single().Id, Is.EqualTo("late"));
		Assert.That(session.ScreenType, Is.EqualTo(typeof(TestScreen)));
		Assert.That(provider.GetServices<IBlazorPlugin>().Single(), Is.SameAs(plugin));
	}

	[Test]
	public void RegistrationRejectsDuplicatePluginNamesBeforeCallingServicesAgain() {
		var services = new ServiceCollection();
		var configured = 0;
		services.AddSphere10BlazorPlugin(plugin => plugin.WithName("Workspace"));
		Assert.That(() => services.AddSphere10BlazorPlugin(plugin => plugin.WithName("Workspace")
			.ConfigureServices(_ => configured++)), Throws.InstanceOf<ArgumentException>());
		Assert.That(configured, Is.Zero);
	}

	[Test]
	public void RegistrationValidatesBlockIdsAcrossDirectAndPluginRegistrations() {
		var services = new ServiceCollection();
		services.AddApplicationBlock(block => block.WithId("work").WithName("Direct"));
		Assert.That(() => services.AddSphere10BlazorPlugin(plugin => plugin.WithName("Workspace")
			.AddBlock(block => block.WithId("work").WithName("Plugin block"))), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void RegistrationValidatesPoliciesAcrossPlugins() {
		var services = new ServiceCollection();
		services.AddSphere10BlazorPlugin(plugin => plugin.WithName("First")
			.AddBlock(block => block.WithId("first").WithName("First").WithDefaultScreen<TestScreen>()));
		Assert.That(() => services.AddSphere10BlazorPlugin(plugin => plugin.WithName("Second")
			.AddBlock(block => block.WithId("second").WithName("Second")
				.AddMenu(menu => menu.WithText("Screens").AddScreenItem<TestScreen>("screen", "Screen", ScreenActivationMode.MultiInstance)))),
			Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public async Task PluginBlocksUseTheSharedCatalogAndCircuitScopedServicesAndScreenSessions() {
		var services = new ServiceCollection();
		services.AddSphere10BlazorPlugin(plugin => plugin.WithName("Workspace")
			.ConfigureServices(collection => collection.AddScoped<ScopedCounter>())
			.AddBlock(block => block.WithId("work").WithName("Work").WithDefaultScreen<TestScreen>()
				.AddMenu(menu => menu.WithText("Actions").AddActionItem("increment", "Increment", (provider, token) => {
					token.ThrowIfCancellationRequested();
					provider.GetRequiredService<ScopedCounter>().Count++;
					return Task.CompletedTask;
				}))));
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		using var firstScope = provider.CreateScope();
		using var secondScope = provider.CreateScope();
		var firstHost = firstScope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var secondHost = secondScope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var firstScreen = await firstHost.ActivateBlockAsync("work");
		var secondScreen = await secondHost.ActivateBlockAsync("work");
		await firstHost.ExecuteMenuItemAsync("work", "increment");
		await firstHost.ExecuteMenuItemAsync("work", "increment");
		await secondHost.ExecuteMenuItemAsync("work", "increment");

		Assert.That(firstScope.ServiceProvider.GetRequiredService<ScopedCounter>().Count, Is.EqualTo(2));
		Assert.That(secondScope.ServiceProvider.GetRequiredService<ScopedCounter>().Count, Is.EqualTo(1));
		Assert.That(firstHost, Is.Not.SameAs(secondHost));
		Assert.That(firstHost.Catalog, Is.SameAs(secondHost.Catalog));
		Assert.That(firstScreen.Id, Is.Not.EqualTo(secondScreen.Id));
		Assert.That(provider.GetRequiredService<IApplicationBlockCatalog<IApplicationBlock>>(), Is.SameAs(firstHost.Catalog));
		Assert.That(provider.GetServices<IApplicationBlock>().Single().Id, Is.EqualTo("work"));
		Assert.That(firstScope.ServiceProvider.GetServices<IBlazorPlugin>().Single(), Is.SameAs(secondScope.ServiceProvider.GetServices<IBlazorPlugin>().Single()));
	}

	[Test]
	public void OriginalPluginConstructorInitializersAndLifecycleRemainAvailable() {
		var plugin = new BlazorPlugin("Original", Array.Empty<IBlazorApplicationBlock>()) {
			Name = "Renamed", Blocks = new[] { new BlazorApplicationBlockBuilder().WithName("Work").Build() }
		};
		var loaded = 0;
		var unloaded = 0;
		plugin.Loaded += () => loaded++;
		plugin.Unloaded += () => unloaded++;
		plugin.Load();
		plugin.Unload();

		Assert.That(plugin.Name, Is.EqualTo("Renamed"));
		Assert.That(plugin.Blocks.Single().Name, Is.EqualTo("Work"));
		Assert.That(loaded, Is.EqualTo(1));
		Assert.That(unloaded, Is.EqualTo(1));
	}

	private sealed class LateBlockPlugin : IBlazorPlugin {
		public event EventHandlerEx Loaded;
		public event EventHandlerEx Unloaded;

		public string Name => "Late blocks";

		public IBlazorApplicationBlock[] Blocks { get; private set; } = Array.Empty<IBlazorApplicationBlock>();

		public IServiceProvider IoCContainer => null;

		public void Load(IServiceCollection services) {
			Blocks = new[] { new BlazorApplicationBlockBuilder().WithId("late").WithName("Late block").WithDefaultScreen<TestScreen>().Build() };
			Loaded?.Invoke();
		}

		public void Unload() => Unloaded?.Invoke();
	}

	public class TestScreen : ComponentBase, IBlazorApplicationScreen {
	}

	public class ScopedCounter {
		public int Count { get; set; }
	}
}
