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
public class PluginBuilderTests {
	[Test]
	public void FluentConfigurationUsesTheExistingBlockAndMenuBuilders() {
		var plugin = new BlazorPluginBuilder()
			.WithName("Workspace")
			.AddBlock(block => block
				.WithId("work")
				.WithName("Work")
				.WithPosition(5)
				.AddScreen<TestScreen>(screen => screen
					.WithTitle("Overview")
				)
				.WithDefaultScreen<TestScreen>()
				.AddMenu(menu => menu
					.WithId("screens")
					.WithText("Screens")
					.AddShowScreenItem<TestScreen>(text: "Overview", id: "overview")
				)
			)
			.Build();
		var block = plugin.Blocks.Single();

		Assert.That(plugin.Name, Is.EqualTo("Workspace"));
		Assert.That(block.Id, Is.EqualTo("work"));
		Assert.That(block.Position, Is.EqualTo(5));
		Assert.That(block.DefaultScreen, Is.EqualTo(typeof(TestScreen)));
		Assert.That(block.Menus.Single().Items.Single().Id, Is.EqualTo("overview"));
		Assert.That(plugin.IoCContainer, Is.Null);
		Assert.That(plugin, Is.InstanceOf<ApplicationPlugin>());
		Assert.That(((IApplicationPlugin)plugin).Blocks.Single(), Is.SameAs(block));
	}

	[Test]
	public void RepeatedBuildsHaveIndependentNamesBlocksAndServiceCallbacks() {
		var callbacks = new List<string>();
		var builder = new BlazorPluginBuilder()
			.WithName("First")
			.AddBlock(block => block
				.WithId("first")
				.WithName("First")
			)
			.AddServices(_ => callbacks.Add("first"));
		var first = builder.Build();
		var second = builder
			.WithName("Second")
			.AddBlock(block => block
				.WithId("second")
				.WithName("Second")
			)
			.AddServices(_ => callbacks.Add("second"))
			.Build();

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
		var definition = new BlazorApplicationScreenDefinition(typeof(TestScreen), "Screen", parameters: parameters);
		var items = new IBlazorApplicationMenuItem[] {
			new BlazorScreenMenuItem { Id = "screen", Title = "Screen", ScreenDefinition = definition }
		};
		var blocks = new[] { new BlazorApplicationBlock { Id = "work", Title = "Work", ScreenDefinitions = new[] { definition }, Menus = new[] { new BlazorApplicationMenu { Text = "Menu", Items = items } } } };
		var plugin = new BlazorPluginBuilder()
			.WithName("Workspace")
			.AddBlock(blocks[0])
			.Build();
		items[0] = null;
		parameters["Message"] = "Changed";
		blocks[0] = null;
		plugin.Blocks[0] = null;
		((IApplicationPlugin)plugin).Blocks[0] = null;

		var screen = (BlazorScreenMenuItem)plugin.Blocks.Single().Menus.Single().Items.Single();
		var screenParameters = ((BlazorApplicationScreenDefinition)screen.ScreenDefinition).Parameters;
		Assert.That(screenParameters["Message"], Is.EqualTo("Original"));
		Assert.That(() => ((IDictionary<string, object>)screenParameters)["Message"] = "Changed", Throws.TypeOf<NotSupportedException>());
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase(" ")]
	public void InvalidNamesAreRejectedImmediately(string name) {
		Assert.That(() => new BlazorPluginBuilder()
			.WithName(name), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => new BlazorPlugin(name, Array.Empty<IBlazorApplicationBlock>()), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void MissingNameIsRejectedAtBuild() {
		Assert.That(() => new BlazorPluginBuilder()
			.Build(), Throws.TypeOf<InvalidOperationException>());
	}

	[Test]
	public void NullConfigurationArgumentsAreRejected() {
		var builder = new BlazorPluginBuilder();
		Assert.That(() => builder.AddBlock((IBlazorApplicationBlock)null), Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => builder.AddBlock((Action<BlazorApplicationBlockBuilder>)null), Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => builder.AddServices(null), Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => new ServiceCollection()
			.AddSphere10BlazorPlugin((Action<BlazorPluginBuilder>)null), Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => new ServiceCollection()
			.AddSphere10BlazorPlugin((IBlazorPlugin)null), Throws.TypeOf<ArgumentNullException>());
	}

	[Test]
	public void DuplicateBlockIdsAreRejectedAtBuild() {
		var builder = new BlazorPluginBuilder()
			.WithName("Workspace")
			.AddBlock(block => block
				.WithId("same")
				.WithName("First")
			)
			.AddBlock(block => block
				.WithId("same")
				.WithName("Second")
			);
		Assert.That(() => builder.Build(), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void DifferentScreenPoliciesRemainIndependentAcrossPluginBlocks() {
		var builder = new BlazorPluginBuilder()
			.WithName("Workspace")
			.AddBlock(block => block
				.WithId("single")
				.WithName("Single")
				.AddScreen<TestScreen>(screen => screen
					.WithActivationMode(ScreenActivationMode.SingleInstance)
				)
				.WithDefaultScreen<TestScreen>()
			)
			.AddBlock(block => block
				.WithId("multiple")
				.WithName("Multiple")
				.AddScreen<TestScreen>(screen => screen
					.WithTitle("Screen")
					.WithActivationMode(ScreenActivationMode.MultiInstance)
				)
				.AddMenu(menu => menu
					.WithText("Screens")
					.AddShowScreenItem<TestScreen>(text: "Screen", id: "screen")
				)
			);
		var plugin = builder.Build();
		Assert.That(plugin.Blocks.SelectMany(block => block.ScreenDefinitions).Select(definition => definition.ActivationMode),
			Is.EqualTo(new ScreenActivationMode?[] { ScreenActivationMode.SingleInstance, ScreenActivationMode.MultiInstance }));
	}

	[Test]
	public async Task RegistrationAllowsServiceOnlyPluginsAndNotifiesAfterConfiguringServices() {
		var services = new ServiceCollection();
		var plugin = new BlazorPluginBuilder()
			.WithName("Services")
			.AddServices(collection => collection.AddScoped<ScopedCounter>())
			.Build();
		var loaded = 0;
		plugin.Loaded += () => {
			Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(ScopedCounter)), Is.True);
			loaded++;
		};
		var result = services
			.AddSphere10BlazorPlugin(plugin);
		await using var provider = services.BuildServiceProvider();

		Assert.That(result, Is.SameAs(services));
		Assert.That(loaded, Is.EqualTo(1));
		Assert.That(provider.GetServices<IBlazorPlugin>().Single(), Is.SameAs(plugin));
		Assert.That(provider.GetServices<IApplicationPlugin>().Single(), Is.SameAs(plugin));
		Assert.That(provider.GetRequiredService<IBlazorApplicationScreenHost>().AvailableBlocks, Is.Empty);
		Assert.That(provider.GetRequiredService<IBlazorApplication>().LoadedPlugins.Single(), Is.SameAs(plugin));
		Assert.That(provider.GetRequiredService<IBlazorApplication>().Blocks, Is.Empty);
		Assert.That(provider.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>().Single().Blocks, Is.Empty);
	}

	[Test]
	public async Task LoadedMenuSubscribersReachTheRegisteredHostActionOnce() {
		var selected = 0;
		var executed = 0;
		var plugin = new BlazorPluginBuilder()
			.WithName("Workspace")
			.AddBlock(block => block
				.WithId("work")
				.WithName("Work")
				.AddMenu(menu => menu
					.WithText("Actions")
					.AddActionItem("run", "Run", () => executed++)
				)
			)
			.Build();
		plugin.Loaded += () => plugin.Blocks.Single().Menus.Single().Items.Single().Select += () => selected++;
		var services = new ServiceCollection()
			.AddSphere10BlazorPlugin(plugin);
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		using var scope = provider.CreateScope();
		var userInterface = scope.ServiceProvider.GetRequiredService<IBlazorUserInterfaceServices>();
		var command = ((IBlazorApplicationScreenHost)userInterface.ScreenHost).Blocks.Single().Menus.Single().Items.Single();

		await userInterface.ExecuteMenuItemAsync(command);

		Assert.That(executed, Is.EqualTo(1));
		Assert.That(selected, Is.EqualTo(1));
	}

	[Test]
	public async Task RegistrationRejectsDuplicateBlocksAddedByStartupCallbacks() {
		var services = new ServiceCollection();
		var plugin = new BlazorPluginBuilder()
			.WithName("Workspace")
			.AddBlock(block => block
				.WithId("work")
				.WithName("Plugin block")
			)
			.AddServices(collection => collection
				.AddApplicationBlock(block => block
					.WithId("work")
					.WithName("Callback block")
				)
			)
			.Build();
		var loaded = 0;
		plugin.Loaded += () => loaded++;

		Assert.That(() => services
			.AddSphere10BlazorPlugin(plugin), Throws.InstanceOf<ArgumentException>());
		Assert.That(loaded, Is.EqualTo(1), "The Loaded callback precedes final validation; only the receipt publishes a successful registration.");
		Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(IBlazorPlugin)), Is.False);
		Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(IApplicationPlugin)), Is.False);
		Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(ApplicationPluginRegistration<IBlazorPlugin>)), Is.False);
		await using var provider = services.BuildServiceProvider();
		Assert.That(provider.GetRequiredService<IBlazorApplication>().LoadedPlugins, Is.Empty);
		Assert.That(provider.GetRequiredService<IBlazorApplication>().Blocks.Single().Name, Is.EqualTo("Callback block"),
			"Existing callback registrations remain usable without inventing a loaded plugin owner.");
	}

	[Test]
	public async Task CustomPluginCanFinishItsBlockDefinitionsDuringLoad() {
		var plugin = new LateBlockPlugin();
		Assert.That(plugin.Blocks, Is.Empty);
		var services = new ServiceCollection()
			.AddSphere10BlazorPlugin(plugin);
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var scope = provider.CreateAsyncScope();
		var host = scope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var session = await host.ActivateBlockAsync("late");

		Assert.That(host.AvailableBlocks.Single().Id, Is.EqualTo("late"));
		Assert.That(session.ScreenType, Is.EqualTo(typeof(TestScreen)));
		Assert.That(provider.GetServices<IBlazorPlugin>().Single(), Is.SameAs(plugin));
		Assert.That(provider.GetServices<IApplicationPlugin>().Single(), Is.SameAs(plugin));
		Assert.That(scope.ServiceProvider.GetRequiredService<IBlazorApplication>().LoadedPlugins.Single(), Is.SameAs(plugin));
		Assert.That(((IBlazorApplicationBlock)provider.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>().Single().Blocks.Single()).Id, Is.EqualTo("late"));
	}

	[Test]
	public void RegistrationRejectsDuplicatePluginNamesBeforeCallingServicesAgain() {
		var services = new ServiceCollection();
		var configured = 0;
		services
			.AddSphere10BlazorPlugin(plugin => plugin
				.WithName("Workspace")
				.AddInitializer<IncrementUsageByOneInitializer>()
			);
		Assert.That(() => services
			.AddSphere10BlazorPlugin(plugin => plugin
				.WithName("Workspace")
				.AddInitializer<IncrementUsageByOneInitializer>()
				.AddServices(_ => configured++)
			), Throws.InstanceOf<ArgumentException>());
		Assert.That(configured, Is.Zero);
		Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(IApplicationInitializer) && descriptor.IsKeyedService && Equals(descriptor.ServiceKey, "Workspace")), Is.EqualTo(1));
	}

	[Test]
	public void RegistrationValidatesBlockIdsAcrossDirectAndPluginRegistrations() {
		var services = new ServiceCollection();
		services
			.AddApplicationBlock(block => block
				.WithId("work")
				.WithName("Direct")
			);
		Assert.That(() => services
			.AddSphere10BlazorPlugin(plugin => plugin
				.WithName("Workspace")
				.AddBlock(block => block
					.WithId("work")
					.WithName("Plugin block")
				)
			), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void RegistrationPreservesIndependentPoliciesAcrossPlugins() {
		var services = new ServiceCollection();
		services
			.AddSphere10BlazorPlugin(plugin => plugin
				.WithName("First")
				.AddBlock(block => block
					.WithId("first")
					.WithName("First")
					.AddScreen<TestScreen>(screen => screen
						.WithActivationMode(ScreenActivationMode.SingleInstance)
					)
					.WithDefaultScreen<TestScreen>()
				)
			);
		services
			.AddSphere10BlazorPlugin(plugin => plugin
				.WithName("Second")
				.AddBlock(block => block
					.WithId("second")
					.WithName("Second")
					.AddScreen<TestScreen>(screen => screen
						.WithTitle("Screen")
						.WithActivationMode(ScreenActivationMode.MultiInstance)
					)
					.AddMenu(menu => menu
						.WithText("Screens")
						.AddShowScreenItem<TestScreen>(text: "Screen", id: "screen")
					)
				)
			);
		using var provider = services.BuildServiceProvider();
		var definitions = provider.GetRequiredService<IBlazorApplicationScreenHost>().Blocks.SelectMany(block => block.ScreenDefinitions).ToArray();
		Assert.That(definitions.Select(definition => definition.ActivationMode),
			Is.EqualTo(new ScreenActivationMode?[] { ScreenActivationMode.SingleInstance, ScreenActivationMode.MultiInstance }));
	}

	[Test]
	public async Task PluginBlocksUseUiServicesAndCircuitScopedScreenSessions() {
		var services = new ServiceCollection();
		services
			.AddSphere10BlazorPlugin(plugin => plugin
				.WithName("Workspace")
				.AddServices(collection => collection.AddScoped<ScopedCounter>())
				.AddBlock(block => block
					.WithId("work")
					.WithName("Work")
					.AddScreen<TestScreen>(screen => screen
						.WithTitle(nameof(TestScreen))
					)
					.WithDefaultScreen<TestScreen>()
					.AddMenu(menu => menu
						.WithText("Actions")
						.AddActionItem("increment", "Increment", (provider, token) => {
							token.ThrowIfCancellationRequested();
							provider.GetRequiredService<ScopedCounter>().Count++;
							return Task.CompletedTask;
						})
					)
				)
			);
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		using var firstScope = provider.CreateScope();
		using var secondScope = provider.CreateScope();
		var firstHost = firstScope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var firstHostUserInterface = firstScope.ServiceProvider.GetRequiredService<IBlazorUserInterfaceServices>();
		var secondHost = secondScope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var secondHostUserInterface = secondScope.ServiceProvider.GetRequiredService<IBlazorUserInterfaceServices>();
		var firstScreen = await firstHost.ActivateBlockAsync("work");
		var secondScreen = await secondHost.ActivateBlockAsync("work");
		var firstCommand = firstHost.Blocks.Single().Menus.Single().Items.Single();
		var secondCommand = secondHost.Blocks.Single().Menus.Single().Items.Single();
		await firstHostUserInterface.ExecuteMenuItemAsync(firstCommand);
		await firstHostUserInterface.ExecuteMenuItemAsync(firstCommand);
		await secondHostUserInterface.ExecuteMenuItemAsync(secondCommand);

		Assert.That(firstScope.ServiceProvider.GetRequiredService<ScopedCounter>().Count, Is.EqualTo(2));
		Assert.That(secondScope.ServiceProvider.GetRequiredService<ScopedCounter>().Count, Is.EqualTo(1));
		Assert.That(firstHost, Is.Not.SameAs(secondHost));
		Assert.That(firstHost.AvailableBlocks.Select(block => block.Id), Is.EqualTo(secondHost.AvailableBlocks.Select(block => block.Id)));
		Assert.That(firstScreen.Id, Is.Not.EqualTo(secondScreen.Id));
		Assert.That(firstScope.ServiceProvider.GetRequiredService<IUserInterfaceServices>(), Is.SameAs(firstHostUserInterface));
		Assert.That(provider.GetServices<IApplicationBlock>().Single().Name, Is.EqualTo("Work"));
		Assert.That(firstScope.ServiceProvider.GetServices<IBlazorPlugin>().Single(), Is.SameAs(secondScope.ServiceProvider.GetServices<IBlazorPlugin>().Single()));
	}

	[Test]
	public void OriginalPluginConstructorInitializersAndLifecycleRemainAvailable() {
		var plugin = new BlazorPlugin("Original", Array.Empty<IBlazorApplicationBlock>()) {
			Name = "Renamed", Blocks = new[] {
				new BlazorApplicationBlockBuilder()
					.WithName("Work")
					.Build()
			}
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

	[Test]
	public void ExplicitPluginMembersDispatchThroughTheGenericSharedContract() {
		IBlazorPlugin platform = new ExplicitPlugin();
		IApplicationPlugin shared = platform;
		var loaded = 0;
		var unloaded = 0;
		shared.Loaded += () => loaded++;
		shared.Unloaded += () => unloaded++;
		var services = new ServiceCollection();
		shared.Load(services);
		shared.Unload();
		Assert.That(shared.Name, Is.EqualTo(platform.Name));
		Assert.That(shared.Blocks.Single(), Is.SameAs(platform.Blocks.Single()));
		Assert.That(services.Single().ServiceType, Is.EqualTo(typeof(ScopedCounter)));
		Assert.That(loaded, Is.EqualTo(1));
		Assert.That(unloaded, Is.EqualTo(1));
	}

	private sealed class ExplicitPlugin : IBlazorPlugin {
		private event EventHandlerEx _loaded;
		private event EventHandlerEx _unloaded;
		private readonly IBlazorApplicationBlock _block = new BlazorApplicationBlockBuilder()
			.WithName("Explicit")
			.Build();

		public SettingsObject PluginSettings { get; set; }

		event EventHandlerEx IApplicationPlugin.Loaded {
			add => _loaded += value;
			remove => _loaded -= value;
		}

		event EventHandlerEx IApplicationPlugin.Unloaded {
			add => _unloaded += value;
			remove => _unloaded -= value;
		}

		string IApplicationPlugin.Name => "Explicit";

		IBlazorApplicationBlock[] IApplicationPlugin<IBlazorApplicationBlock>.Blocks => new[] { _block };

		IServiceProvider IBlazorPlugin.IoCContainer => null;

		void IApplicationPlugin.Load(IServiceCollection services) {
			services.AddScoped<ScopedCounter>();
			_loaded?.Invoke();
		}

		void IApplicationPlugin.Unload() => _unloaded?.Invoke();
	}

	private sealed class LateBlockPlugin : IBlazorPlugin {
		public event EventHandlerEx Loaded;
		public event EventHandlerEx Unloaded;

		public SettingsObject PluginSettings { get; set; }

		public string Name => "Late blocks";

		public IBlazorApplicationBlock[] Blocks { get; private set; } = Array.Empty<IBlazorApplicationBlock>();

		public IServiceProvider IoCContainer => null;

		public void Load(IServiceCollection services) {
			Blocks = new[] {
				new BlazorApplicationBlockBuilder()
					.WithId("late")
					.WithName("Late block")
					.AddScreen<TestScreen>(screen => screen
						.WithTitle(nameof(TestScreen))
					)
					.WithDefaultScreen<TestScreen>()
					.Build()
			};
			Loaded?.Invoke();
		}

		public void Unload() => Unloaded?.Invoke();
	}

	public class TestScreen : ComponentBase, IBlazorApplicationScreen {
		public IBlazorApplicationScreenHost Host { get; set; }

		public IBlazorApplicationBlock Block { get; set; }

	}

	public class ScopedCounter {
		public int Count { get; set; }
	}
}
