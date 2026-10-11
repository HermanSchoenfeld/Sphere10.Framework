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
public class ApplicationPluginTests {
	[Test]
	public void LoadRegistersServicesBeforeHooksAndEventsAndUnloadPreservesOrdering() {
		var calls = new List<string>();
		var marker = new Marker();
		var plugin = new RecordingPlugin(calls, services => {
			services.AddSingleton(marker);
			calls.Add("configure");
		});
		plugin.Loaded += () => calls.Add("loaded event");
		plugin.Unloaded += () => calls.Add("unloaded event");
		var services = new ServiceCollection();
		plugin.Load(services);
		plugin.Unload();
		Assert.That(calls, Is.EqualTo(new[] { "configure", "loaded hook", "loaded event", "unloaded hook", "unloaded event" }));
		Assert.That(services.Single().ImplementationInstance, Is.SameAs(marker));
		calls.Clear();
		plugin.Load();
		Assert.That(calls, Is.EqualTo(new[] { "loaded hook", "loaded event" }), "Notification-only loading does not register services again.");
	}

	[Test]
	public void FailedRegistrationDoesNotAnnounceLoadedAndNullServicesAreRejected() {
		var calls = new List<string>();
		var plugin = new RecordingPlugin(calls, _ => throw new InvalidOperationException("configuration failed"));
		plugin.Loaded += () => calls.Add("loaded event");
		Assert.That(() => plugin.Load(null), Throws.ArgumentNullException);
		Assert.That(() => plugin.Load(new ServiceCollection()), Throws.InvalidOperationException.With.Message.EqualTo("configuration failed"));
		Assert.That(calls, Is.Empty);
	}

	[Test]
	public void MembershipArraysAreOwnedWhileDefinitionIdentityAndInitCompatibilityArePreserved() {
		var block = new TestApplicationBlock { Name = "Work" };
		var input = new IApplicationBlock[] { block };
		var plugin = new ApplicationPlugin("Original", Array.Empty<IApplicationBlock>()) { Name = "Configured", Blocks = input };
		input[0] = null;
		plugin.Blocks[0] = null;
		Assert.That(plugin.Name, Is.EqualTo("Configured"));
		Assert.That(plugin.Blocks.Single(), Is.SameAs(block));
		block.Name = "Updated definition";
		Assert.That(plugin.Blocks.Single().Name, Is.EqualTo("Updated definition"));
	}

	[Test]
	public void TypedAdapterInitializerSharesStorageWithTheCommonContract() {
		var first = new SpecialBlock { Name = "First" };
		var second = new SpecialBlock { Name = "Second" };
		var plugin = new TypedPlugin(new[] { first }) { Blocks = new[] { second } };
		IApplicationPlugin shared = plugin;
		Assert.That(shared.Blocks.Single(), Is.SameAs(second));
		Assert.That(plugin.Blocks.Single(), Is.SameAs(second));
		shared.Blocks[0] = null;
		plugin.Blocks[0] = null;
		Assert.That(shared.Blocks.Single(), Is.SameAs(second));
	}

	[Test]
	public void BuilderCopiesAccumulatedMembershipAndServiceConfigurationForEachBuild() {
		var calls = new List<string>();
		var firstBlock = new TestApplicationBlock { Name = "First" };
		var secondBlock = new TestApplicationBlock { Name = "Second" };
		var builder = new ApplicationPluginBuilder()
			.WithName("First plugin")
			.AddBlock(firstBlock)
			.ConfigureServices(_ => calls.Add("first"));
		var first = builder.Build();
		var second = builder
			.WithName("Second plugin")
			.AddBlock(secondBlock)
			.ConfigureServices(_ => calls.Add("second"))
			.Build();
		Assert.That(calls, Is.Empty, "Building definitions does not perform startup work.");
		first.Load(new ServiceCollection());
		second.Load(new ServiceCollection());
		Assert.That(calls, Is.EqualTo(new[] { "first", "first", "second" }));
		Assert.That(first.Name, Is.EqualTo("First plugin"));
		Assert.That(first.Blocks, Is.EqualTo(new[] { firstBlock }));
		Assert.That(second.Blocks, Is.EqualTo(new[] { firstBlock, secondBlock }));
		Assert.That(() => new ApplicationPluginBuilder().Build(), Throws.InvalidOperationException);
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase(" ")]
	public void PluginNamesMustBePresent(string name) {
		Assert.That(() => new ApplicationPlugin(name, Array.Empty<IApplicationBlock>()), Throws.ArgumentException);
		Assert.That(() => new ApplicationPluginBuilder().WithName(name), Throws.ArgumentException);
	}

	[Test]
	public void MembershipValidationRejectsMissingAndDuplicateBlocks() {
		var block = new TestApplicationBlock { Name = "First" };
		Assert.That(() => new ApplicationPlugin("Work", null), Throws.ArgumentNullException);
		Assert.That(() => new ApplicationPlugin("Work", new IApplicationBlock[] { null }), Throws.ArgumentNullException);
		Assert.That(() => new ApplicationPlugin("Work", new[] { new TestApplicationBlock() }), Throws.ArgumentException);
		Assert.That(() => new ApplicationPlugin("Work", new[] { block, block }), Throws.ArgumentException);
	}

	[Test]
	public void PluginValidationRejectsDuplicateNamesAndBlockOwnershipAcrossPlugins() {
		var firstBlock = new TestApplicationBlock { Name = "First" };
		var first = new ApplicationPlugin("First", new[] { firstBlock });
		var sameName = new ApplicationPlugin("First", Array.Empty<IApplicationBlock>());
		var sameBlock = new ApplicationPlugin("Second", new[] { firstBlock });
		Assert.That(() => Tools.UI.ValidatePlugins(new[] { first, sameName }), Throws.ArgumentException);
		Assert.That(() => Tools.UI.ValidatePlugins(new[] { first, sameBlock }), Throws.ArgumentException);
		Assert.That(() => Tools.UI.ValidatePlugins(new IApplicationPlugin[] { null }), Throws.ArgumentNullException);
		Assert.That(() => Tools.UI.ValidatePlugins(new[] { new UnvalidatedPlugin { Name = " " } }), Throws.ArgumentException);
		Assert.That(() => Tools.UI.ValidatePlugins(new[] { new UnvalidatedPlugin { Name = "Invalid", Blocks = null } }), Throws.ArgumentNullException);
	}

	[Test]
	public void EmptyApplicationsAndServiceOnlyPluginsAreSupportedAndValidationCopiesMembership() {
		Assert.That(Tools.UI.ValidatePlugins(Array.Empty<IApplicationPlugin>()), Is.Empty);
		var plugin = new ApplicationPluginBuilder()
			.WithName("Services")
			.ConfigureServices(services => services.AddSingleton<Marker>())
			.Build();
		var input = new IApplicationPlugin[] { plugin };
		var validated = Tools.UI.ValidatePlugins(input);
		input[0] = null;
		Assert.That(validated.Single(), Is.SameAs(plugin));
		Assert.That(plugin.Blocks, Is.Empty);
	}

	[Test]
	public void DecoratorForwardsIdentityRegistrationAndBothEventSubscriptions() {
		var plugin = new ApplicationPlugin("Work", new[] { new TestApplicationBlock { Name = "Work" } });
		var decorator = new PluginDecorator(plugin);
		var loaded = 0;
		var unloaded = 0;
		EventHandlerEx onLoaded = () => loaded++;
		EventHandlerEx onUnloaded = () => unloaded++;
		decorator.Loaded += onLoaded;
		decorator.Unloaded += onUnloaded;
		decorator.Load(new ServiceCollection());
		decorator.Unload();
		Assert.That(decorator.Name, Is.EqualTo(plugin.Name));
		Assert.That(decorator.Blocks.Single(), Is.SameAs(plugin.Blocks.Single()));
		Assert.That(loaded, Is.EqualTo(1));
		Assert.That(unloaded, Is.EqualTo(1));
		decorator.Loaded -= onLoaded;
		decorator.Unloaded -= onUnloaded;
		decorator.Load(new ServiceCollection());
		decorator.Unload();
		Assert.That(loaded, Is.EqualTo(1));
		Assert.That(unloaded, Is.EqualTo(1));
	}

	[Test]
	public async Task ApplicationProjectsLiveHostBlocksAndOriginalLoadedPluginsWithoutSyntheticGroups() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var first = new TestApplicationBlock { Name = "First" };
		var second = new TestApplicationBlock { Name = "Second" };
		var plugin = new ApplicationPlugin("Work", new[] { first, second });
		var services = new ApplicationPlugin("Services", Array.Empty<IApplicationBlock>());
		var standalone = new TestApplicationBlock { Name = "Definition" };
		using var host = new ApplicationScreenHost(new[] { first, standalone, second }, provider);
		using var application = new PluginApplication(new[] { plugin, services }, host);
		Assert.That(application.LoadedPlugins, Is.EqualTo(new[] { plugin, services }));
		Assert.That(application.LoadedPlugins[0], Is.SameAs(plugin));
		Assert.That(application.Blocks, Is.EqualTo(new[] { first, standalone, second }));
		application.LoadedPlugins[0] = null;
		application.Blocks[0] = null;
		Assert.That(application.LoadedPlugins[0], Is.SameAs(plugin));
		Assert.That(application.Blocks[0], Is.SameAs(first));
		await host.SelectBlockAsync(standalone);
		Assert.That(application.ScreenHost.ActiveBlock, Is.SameAs(standalone));
		Assert.That(application.LoadedPlugins, Is.EqualTo(new[] { plugin, services }));
		await host.UnregisterBlockAsync(second);
		Assert.That(application.Blocks, Is.EqualTo(new[] { first, standalone }));
		await host.RegisterBlockAsync(second);
		Assert.That(application.Blocks, Is.EqualTo(new[] { first, standalone, second }));
	}

	[Test]
	public void ApplicationDisposalDoesNotManageStartupPluginLifetimeAndEmptyMembershipIsSupported() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var plugin = new ApplicationPlugin("Services", Array.Empty<IApplicationBlock>());
		var loaded = 0;
		var unloaded = 0;
		plugin.Loaded += () => loaded++;
		plugin.Unloaded += () => unloaded++;
		plugin.Load(new ServiceCollection());
		using var host = new ApplicationScreenHost(provider);
		var application = new PluginApplication(new[] { plugin }, host);
		application.Dispose();
		Assert.That(loaded, Is.EqualTo(1));
		Assert.That(unloaded, Is.Zero);
		Assert.That(application.LoadedPlugins.Single(), Is.SameAs(plugin));
		Assert.That(application.Blocks, Is.Empty);
		using var emptyHost = new ApplicationScreenHost(provider);
		using var empty = new PluginApplication(Array.Empty<IApplicationPlugin>(), emptyHost);
		Assert.That(empty.Blocks, Is.Empty);
		Assert.That(empty.LoadedPlugins, Is.Empty);
	}

	private sealed class Marker {
	}

	private sealed class RecordingPlugin : ApplicationPlugin {
		private readonly List<string> _calls;

		public RecordingPlugin(List<string> calls, Action<IServiceCollection> configure)
			: base("Recording", Array.Empty<IApplicationBlock>(), configure) {
			_calls = calls;
		}

		protected override void OnLoaded() => _calls.Add("loaded hook");

		protected override void OnUnloaded() => _calls.Add("unloaded hook");
	}

	private sealed class SpecialBlock : TestApplicationBlock {
	}

	private sealed class TypedPlugin : ApplicationPlugin {
		public TypedPlugin(IEnumerable<SpecialBlock> blocks)
			: base("Typed", blocks) {
		}

		public new SpecialBlock[] Blocks {
			get => base.Blocks.Cast<SpecialBlock>().ToArray();
			init => base.Blocks = value;
		}
	}

	private sealed class UnvalidatedPlugin : ApplicationPluginBase {
		public override string Name { get; init; }

		public override IApplicationBlock[] Blocks { get; init; } = Array.Empty<IApplicationBlock>();
	}

	private sealed class PluginDecorator : ApplicationPluginDecorator<ApplicationPlugin> {
		public PluginDecorator(ApplicationPlugin plugin)
			: base(plugin) {
		}
	}

	private sealed class PluginApplication : SyncApplicationBase {
		private readonly IApplicationPlugin[] _loadedPlugins;

		public PluginApplication(IEnumerable<IApplicationPlugin> loadedPlugins, IApplicationScreenHost host) {
			_loadedPlugins = Tools.UI.ValidatePlugins(loadedPlugins);
			ScreenHost = host;
			ObserveScreenHost();
		}

		public override IApplicationPlugin[] LoadedPlugins => Tools.Array.Clone(_loadedPlugins);

		public override IApplicationScreenHost ScreenHost { get; }
	}
}
