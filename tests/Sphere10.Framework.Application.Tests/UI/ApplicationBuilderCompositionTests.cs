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
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

/// <summary>Exercises shared application builder composition tests.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationBuilderCompositionTests {
	private static readonly AsyncLocal<ChildBuilderProbe> _currentProbe = new();

	/// <summary>Verifies add retains existing definition without creating or building achild.</summary>
	[TestCase("item")]
	[TestCase("menu")]
	[TestCase("block")]
	[TestCase("plugin")]
	public void AddRetainsExistingDefinitionWithoutCreatingOrBuildingAChild(string definitionKind) {
		using var scenario = CreateScenario(definitionKind);

		scenario.Add(scenario.Existing);
		var definitions = scenario.Build();

		Assert.That(definitions, Has.Length.EqualTo(1));
		Assert.That(definitions[0], Is.SameAs(scenario.Existing));
		Assert.That(scenario.Probe.Constructions, Is.Zero);
		Assert.That(scenario.Probe.Builds, Is.Zero);
	}

	/// <summary>Verifies configure builds one child immediately and preserves declaration order.</summary>
	[Test]
	public void ConfigureBuildsOneChildImmediatelyAndPreservesDeclarationOrder([Values("item", "menu", "block", "plugin")] string definitionKind, [Values] bool configuredFirst) {
		using var scenario = CreateScenario(definitionKind);
		var callbackCalls = 0;

		if (!configuredFirst)
			scenario.Add(scenario.Existing);
		scenario.Configure(() => callbackCalls++);
		if (configuredFirst)
			scenario.Add(scenario.Existing);

		Assert.That(callbackCalls, Is.EqualTo(1));
		Assert.That(scenario.Probe.Constructions, Is.EqualTo(1));
		Assert.That(scenario.Probe.Builds, Is.EqualTo(1));
		var definitions = scenario.Build();
		Assert.That(definitions, Has.Length.EqualTo(2));
		Assert.That(definitions[0], Is.SameAs(configuredFirst ? scenario.Configured : scenario.Existing));
		Assert.That(definitions[1], Is.SameAs(configuredFirst ? scenario.Existing : scenario.Configured));
		Assert.That(scenario.Probe.Constructions, Is.EqualTo(1));
		Assert.That(scenario.Probe.Builds, Is.EqualTo(1));
	}

	/// <summary>Verifies configure uses anew child builder for each definition.</summary>
	[TestCase("item")]
	[TestCase("menu")]
	[TestCase("block")]
	[TestCase("plugin")]
	public void ConfigureUsesANewChildBuilderForEachDefinition(string definitionKind) {
		using var scenario = CreateScenario(definitionKind);
		var callbackCalls = 0;

		scenario.Configure(() => callbackCalls++);
		scenario.Configure(() => callbackCalls++);

		Assert.That(callbackCalls, Is.EqualTo(2));
		Assert.That(scenario.Probe.Constructions, Is.EqualTo(2));
		Assert.That(scenario.Probe.Builds, Is.EqualTo(2));
	}

	/// <summary>Verifies add rejects null definitions without creating achild.</summary>
	[TestCase("item")]
	[TestCase("menu")]
	[TestCase("block")]
	[TestCase("plugin")]
	public void AddRejectsNullDefinitionsWithoutCreatingAChild(string definitionKind) {
		using var scenario = CreateScenario(definitionKind);

		Assert.That(() => scenario.Add(null), Throws.ArgumentNullException);
		Assert.That(scenario.Build(), Is.Empty);
		Assert.That(scenario.Probe.Constructions, Is.Zero);
		Assert.That(scenario.Probe.Builds, Is.Zero);
	}

	/// <summary>Verifies configure rejects null callbacks before creating achild.</summary>
	[TestCase("item")]
	[TestCase("menu")]
	[TestCase("block")]
	[TestCase("plugin")]
	public void ConfigureRejectsNullCallbacksBeforeCreatingAChild(string definitionKind) {
		using var scenario = CreateScenario(definitionKind);

		Assert.That(() => scenario.Configure(null), Throws.ArgumentNullException);
		Assert.That(scenario.Build(), Is.Empty);
		Assert.That(scenario.Probe.Constructions, Is.Zero);
		Assert.That(scenario.Probe.Builds, Is.Zero);
	}

	/// <summary>Verifies failed configuration does not build or append the child.</summary>
	[TestCase("item")]
	[TestCase("menu")]
	[TestCase("block")]
	[TestCase("plugin")]
	public void FailedConfigurationDoesNotBuildOrAppendTheChild(string definitionKind) {
		using var scenario = CreateScenario(definitionKind);
		scenario.Add(scenario.Existing);

		Assert.That(() => scenario.Configure(() => throw new ApplicationException("Configuration failed")), Throws.TypeOf<ApplicationException>());
		Assert.That(scenario.Build(), Is.EqualTo(new[] { scenario.Existing }));
		Assert.That(scenario.Probe.Constructions, Is.EqualTo(1));
		Assert.That(scenario.Probe.Builds, Is.Zero);
	}

	/// <summary>Verifies failed child build does not append adefinition.</summary>
	[TestCase("item")]
	[TestCase("menu")]
	[TestCase("block")]
	[TestCase("plugin")]
	public void FailedChildBuildDoesNotAppendADefinition(string definitionKind) {
		using var scenario = CreateScenario(definitionKind);
		scenario.Add(scenario.Existing);
		scenario.Probe.FailBuild = true;

		Assert.That(() => scenario.Configure(() => { }), Throws.InvalidOperationException);
		Assert.That(scenario.Build(), Is.EqualTo(new[] { scenario.Existing }));
		Assert.That(scenario.Probe.Constructions, Is.EqualTo(1));
		Assert.That(scenario.Probe.Builds, Is.EqualTo(1));
	}

	/// <summary>Verifies terminal application rejects configuration before creating achild.</summary>
	[Test]
	public void TerminalApplicationRejectsConfigurationBeforeCreatingAChild() {
		using var scenario = CreateScenario("plugin");
		scenario.Build();
		var callbackInvoked = false;

		Assert.That(() => scenario.Configure(() => callbackInvoked = true), Throws.InvalidOperationException);
		Assert.That(callbackInvoked, Is.False);
		Assert.That(scenario.Probe.Constructions, Is.Zero);
		Assert.That(scenario.Probe.Builds, Is.Zero);
	}

	/// <summary>Composes one builder hierarchy level and the callbacks exercised by the fixture.</summary>
	private static CompositionScenario CreateScenario(string definitionKind) {
		var probe = new ChildBuilderProbe();
		_currentProbe.Value = probe;
		switch (definitionKind) {
			case "item": {
				var builder = new MenuBuilder().WithText("Menu");
				var configured = new ItemDefinition("Configured");
				return new CompositionScenario(
					probe, new ItemDefinition("Existing"), configured,
					item => builder.AddItem((ItemDefinition)item),
					callback => builder.AddItem(callback == null ? null : child => {
						child.WithProduct(configured);
						callback();
					}),
					() => builder.Build().Items.Cast<object>().ToArray()
				);
			}
			case "menu": {
				var builder = new BlockBuilder().WithName("Block");
				var configured = new MenuDefinition("Configured", Array.Empty<ItemDefinition>());
				return new CompositionScenario(
					probe, new MenuDefinition("Existing", Array.Empty<ItemDefinition>()), configured,
					menu => builder.AddMenu((MenuDefinition)menu),
					callback => builder.AddMenu(callback == null ? null : child => {
						child.WithProduct(configured);
						callback();
					}),
					() => builder.Build().Menus.Cast<object>().ToArray()
				);
			}
			case "block": {
				var builder = new PluginBuilder().WithName("Plugin");
				var configured = new BlockDefinition("Configured", Array.Empty<MenuDefinition>());
				return new CompositionScenario(
					probe, new BlockDefinition("Existing", Array.Empty<MenuDefinition>()), configured,
					block => builder.AddBlock((BlockDefinition)block),
					callback => builder.AddBlock(callback == null ? null : child => {
						child.WithProduct(configured);
						callback();
					}),
					() => builder.Build().Blocks.Cast<object>().ToArray()
				);
			}
			default: {
				var builder = new RegistrationBuilder();
				var configured = new ApplicationPlugin("Configured", Array.Empty<IApplicationBlock>());
				return new CompositionScenario(
					probe, new ApplicationPlugin("Existing", Array.Empty<IApplicationBlock>()), configured,
					plugin => builder.AddPlugin((ApplicationPlugin)plugin),
					callback => builder.AddPlugin(callback == null ? null : child => {
						child.WithProduct(configured);
						callback();
					}),
					() => builder.Build().Cast<object>().ToArray()
				);
			}
		}
	}

	#region Inner Types

	/// <summary>Supplies composition scenario for the fixture's existing contract checks.</summary>
	private sealed record CompositionScenario(
		ChildBuilderProbe Probe, object Existing, object Configured, Action<object> Add, Action<Action> Configure, Func<object[]> Build
	) : IDisposable {
		/// <inheritdoc />
		public void Dispose() => _currentProbe.Value = null;
	}

	/// <summary>Supplies child builder probe for the fixture's existing contract checks.</summary>
	private sealed class ChildBuilderProbe {
		/// <inheritdoc />
		public int Constructions { get; set; }

		/// <inheritdoc />
		public int Builds { get; set; }

		/// <inheritdoc />
		public bool FailBuild { get; set; }
	}

	/// <summary>Supplies child builder for the fixture's existing contract checks.</summary>
	private sealed class ChildBuilder<TProduct> : IApplicationUIBuilder<TProduct> {
		private readonly ChildBuilderProbe _probe;
		private TProduct _product;

		/// <summary>Initializes the existing host or test adapter without changing its registration ownership.</summary>
		public ChildBuilder() {
			_probe = _currentProbe.Value;
			_probe.Constructions++;
		}

		/// <summary>Sets the product returned by the fixture's child builder.</summary>
		public ChildBuilder<TProduct> WithProduct(TProduct product) {
			_product = product;
			return this;
		}

		/// <summary>Builds the configured test product without running deferred actions.</summary>
		public TProduct Build() {
			_probe.Builds++;
			Guard.Ensure(!_probe.FailBuild, "Child build failed.");
			return _product;
		}
	}

	/// <summary>Supplies item definition for the fixture's existing contract checks.</summary>
	private sealed record ItemDefinition(string Title) : IApplicationMenuItem {
		/// <inheritdoc />
		public IApplicationMenu Parent { get; set; }
	}

	/// <summary>Supplies menu definition for the fixture's existing contract checks.</summary>
	private sealed record MenuDefinition(string Text, ItemDefinition[] ItemDefinitions) : IApplicationMenu {
		/// <inheritdoc />
		public IApplicationBlock Parent { get; set; }

		/// <inheritdoc />
		public IApplicationMenuItem[] Items => ItemDefinitions;
	}

	/// <summary>Supplies block definition for the fixture's existing contract checks.</summary>
	private sealed record BlockDefinition(string Name, MenuDefinition[] MenuDefinitions) : IApplicationBlock, IApplicationCommandProvider {
		/// <inheritdoc />
		public SettingsObject BlockSettings { get; set; }

		/// <inheritdoc />
		public IApplication Parent { get; set; }

		/// <inheritdoc />
		public IApplicationPlugin Source { get; set; }

		/// <inheritdoc />
		public int Position => 0;

		/// <inheritdoc />
		public ApplicationScreenDefinition[] ScreenDefinitions => Array.Empty<ApplicationScreenDefinition>();

		/// <inheritdoc />
		public ApplicationScreenDefinition DefaultScreenDefinition => null;

		/// <inheritdoc />
		public IApplicationMenu[] Menus => MenuDefinitions;
	}

	/// <summary>Supplies menu builder for the fixture's existing contract checks.</summary>
	private sealed class MenuBuilder : ApplicationMenuBuilderBase<MenuBuilder, ItemDefinition, ChildBuilder<ItemDefinition>, MenuDefinition> {
		/// <inheritdoc />
		protected override MenuDefinition CreateMenu(IReadOnlyList<ItemDefinition> items) => new(Text, items.ToArray());
	}

	/// <summary>Supplies block builder for the fixture's existing contract checks.</summary>
	private sealed class BlockBuilder : ApplicationBlockBuilderBase<BlockBuilder, MenuDefinition, ChildBuilder<MenuDefinition>, BlockDefinition> {
		/// <inheritdoc />
		protected override BlockDefinition CreateBlock(IReadOnlyList<MenuDefinition> menus) => new(Name, menus.ToArray());
	}

	/// <summary>Supplies plugin builder for the fixture's existing contract checks.</summary>
	private sealed class PluginBuilder : ApplicationPluginBuilderBase<PluginBuilder, BlockDefinition, ChildBuilder<BlockDefinition>, ApplicationPlugin> {
		/// <inheritdoc />
		protected override ApplicationPlugin CreatePlugin(BlockDefinition[] blocks, Action<IServiceCollection> configureServices) => new(Name, blocks, configureServices);
	}

	/// <summary>Supplies registration builder for the fixture's existing contract checks.</summary>
	private sealed class RegistrationBuilder
		: ApplicationBuilderBase<RegistrationBuilder, IApplication, ApplicationPlugin, ChildBuilder<ApplicationPlugin>, ApplicationPlugin[]> {
		/// <summary>Initializes the existing host or test adapter without changing its registration ownership.</summary>
		public RegistrationBuilder()
			: base("Application") {
		}

		/// <inheritdoc />
		protected override ApplicationPlugin[] CreateApplication(ApplicationPlugin[] plugins, Action<IApplication> configureApplication) => plugins;
	}

	#endregion
}
