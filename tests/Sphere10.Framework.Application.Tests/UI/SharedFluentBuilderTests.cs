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

namespace Sphere10.Framework.Application.Tests;

/// <summary>Exercises shared fluent builder configuration, registration and callback contracts.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SharedFluentBuilderTests {
	/// <summary>Verifies a block retains the actual supplied settings object.</summary>
	[Test]
	public void BlockBuilderRetainsSuppliedSettingsInstance() {
		var settings = new Preferences();
		var builder = new BlockBuilder()
			.WithName("Inspect")
			.UseSettings(settings);
		settings.Name = "caller changed";
		var block = builder.Build();

		Assert.That(((Preferences)block.BlockSettings).Name, Is.EqualTo("caller changed"));
		Assert.That(block.BlockSettings, Is.SameAs(settings));
		Assert.That(block.ScreenDefinitions, Is.Empty, "Block preferences do not require a screen or menu registration.");
	}

	/// <summary>Verifies optional definition references keep callback order and return the parent builder.</summary>
	[Test]
	public void OptionalDefinitionReferencesKeepCallbackOrderAndReturnTheParentBuilder() {
		var configured = 0;
		var executed = 0;
		var menuBuilder = new MenuBuilder()
			.WithText("Actions");
		Assert.That(menuBuilder
			.AddItem(item => {
				configured++;
				item
					.WithText("Run")
					.WithAction(() => executed++);
			}, out var itemDefinition), Is.SameAs(menuBuilder));
		var blockBuilder = new BlockBuilder()
			.WithName("Work");
		Assert.That(blockBuilder
			.AddMenu(menuBuilder.Build(), out var menuDefinition), Is.SameAs(blockBuilder));
		var pluginBuilder = new PluginBuilder()
			.WithName("Operations");
		Assert.That(pluginBuilder
			.AddBlock(blockBuilder.Build(), out var blockDefinition), Is.SameAs(pluginBuilder));
		var plugin = pluginBuilder.Build();

		Assert.That(configured, Is.EqualTo(1));
		Assert.That(executed, Is.Zero, "Configuration and graph construction must not run action callbacks.");
		Assert.That(itemDefinition.Title, Is.EqualTo("Run"));
		Assert.That(menuDefinition.Text, Is.EqualTo("Actions"));
		Assert.That(plugin.Blocks.Single(), Is.SameAs(blockDefinition));
		((ActionItem)((IApplicationCommandProvider)blockDefinition).Menus.Single().Items.Single()).Action();
		Assert.That(executed, Is.EqualTo(1));
	}

	/// <summary>Verifies plugin composes typed child builders and ordered service callbacks.</summary>
	[Test]
	public void PluginComposesTypedChildBuildersAndOrderedServiceCallbacks() {
		var calls = new List<string>();
		var plugin = new PluginBuilder()
			.WithName("Operations")
			.AddServices(_ => calls.Add("first"))
			.AddBlock(block => block
				.WithName("Inventory")
				.WithPosition(3)
				.AddScreen(new ApplicationScreenDefinition(typeof(Screen), "Stock", ScreenActivationMode.SingleInstance))
				.WithDefaultScreen<Screen>()
				.AddMenu(menu => menu
					.WithText("Items")
					.AddItem(item => item
						.WithText("Adjust stock")
						.WithAction(() => calls.Add("action"))
					)
				)
			)
			.AddServices(_ => calls.Add("second"))
			.Build();

		plugin.Load(new ServiceCollection());
		var block = plugin.Blocks[0];
		var menu = ((IApplicationCommandProvider)block).Menus[0];
		((ActionItem)menu.Items[0]).Action();
		Assert.That(calls, Is.EqualTo(new[] { "first", "second", "action" }));
		Assert.That(block.Name, Is.EqualTo("Inventory"));
		Assert.That(block.Position, Is.EqualTo(3));
		Assert.That(block.DefaultScreenDefinition.ScreenType, Is.EqualTo(typeof(Screen)));
		Assert.That(block.DefaultScreenDefinition.Title, Is.EqualTo("Stock"));
		Assert.That(block.DefaultScreenDefinition.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(menu.Text, Is.EqualTo("Items"));
		Assert.That(menu.Items[0].Title, Is.EqualTo("Adjust stock"));
	}

	/// <summary>Verifies failed child configuration does not append partial definitions.</summary>
	[Test]
	public void FailedChildConfigurationDoesNotAppendPartialDefinitions() {
		var builder = new PluginBuilder()
			.WithName("Operations")
			.AddBlock(block => block
				.WithName("First")
			);
		Assert.That(() => builder
			.AddBlock(block => block
				.WithName("Invalid")
				.AddMenu(menu => menu
					.WithText("")
				)
			), Throws.InvalidOperationException);
		Assert.That(builder.Build().Blocks.Length, Is.EqualTo(1));
		Assert.That(() => builder
			.AddBlock((Action<BlockBuilder>)null), Throws.ArgumentNullException);
	}

	/// <summary>Verifies fluent screen policy rejects invalid combinations before attaching block.</summary>
	[Test]
	public void FluentScreenPolicyRejectsInvalidCombinationsBeforeAttachingBlock() {
		var builder = new PluginBuilder().WithName("Operations");
		Assert.That(() => builder
			.AddBlock(block => block
				.WithName("Invalid")
				.AddScreen(new ApplicationScreenDefinition(typeof(Screen), activationMode: ScreenActivationMode.PermanentSingleton, screenKind: ScreenKind.Empty))
			), Throws.ArgumentException);
		Assert.That(builder.Build().Blocks, Is.Empty);
	}

	/// <summary>Verifies type based default reuses the matching menus canonical definition.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public void TypeBasedDefaultReusesTheMatchingMenusCanonicalDefinition(bool genericDefault) {
		var definition = new ApplicationScreenDefinition(typeof(Screen), "Empty workspace", ScreenActivationMode.SingleInstance, ScreenKind.Empty);
		var menu = new MenuBuilder()
			.WithText("Views")
			.AddItem(new ScreenItem { Title = "Navigation caption", ScreenDefinition = definition })
			.Build();
		var builder = new BlockBuilder()
			.WithName("Workspace")
			.AddScreen(definition)
			.AddMenu(menu);
		if (genericDefault)
			builder.WithDefaultScreen<Screen>();
		else
			builder.WithDefaultScreen(typeof(Screen));
		var block = builder.Build();

		Assert.That(block.DefaultScreenDefinition, Is.SameAs(definition));
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(block.DefaultScreenDefinition));
		Assert.That(((IScreenMenuItem)block.Menus.Single().Items.Single()).ScreenDefinition, Is.SameAs(block.DefaultScreenDefinition));
		Assert.That(block.DefaultScreenDefinition.Title, Is.EqualTo("Empty workspace"));
		Assert.That(block.DefaultScreenDefinition.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(block.DefaultScreenDefinition.ScreenKind, Is.EqualTo(ScreenKind.Empty));
	}

	/// <summary>Verifies type based default reuses an explicitly registered definition without a menu.</summary>
	[Test]
	public void TypeBasedDefaultReusesAnExplicitlyRegisteredDefinitionWithoutAMenu() {
		var definition = new ApplicationScreenDefinition(typeof(Screen), "Standalone workspace", ScreenActivationMode.SingleInstance, ScreenKind.Empty);
		var builder = new BlockBuilder()
			.WithName("Workspace")
			.AddScreen(definition)
			.WithDefaultScreen<Screen>();
		var block = builder.Build();

		Assert.That(block.Menus, Is.Empty);
		Assert.That(block.DefaultScreenDefinition, Is.SameAs(definition));
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(block.DefaultScreenDefinition));
		Assert.That(block.DefaultScreenDefinition.Title, Is.EqualTo("Standalone workspace"));
		Assert.That(block.DefaultScreenDefinition.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(block.DefaultScreenDefinition.ScreenKind, Is.EqualTo(ScreenKind.Empty));
	}

	/// <summary>Verifies default reference preserves registered metadata and independent command caption.</summary>
	[Test]
	public void DefaultReferencePreservesRegisteredMetadataAndIndependentCommandCaption() {
		var definition = new ApplicationScreenDefinition(typeof(Screen), "Pinned title", ScreenActivationMode.SingleInstance, ScreenKind.Empty);
		var menu = new MenuBuilder()
			.WithText("Views")
			.AddItem(new ScreenItem { Title = "Navigation caption", ScreenDefinition = definition })
			.Build();
		var builder = new BlockBuilder()
			.WithName("Workspace")
			.AddScreen(definition)
			.WithDefaultScreen<Screen>()
			.AddMenu(menu);
		var block = builder.Build();

		Assert.That(block.DefaultScreenDefinition, Is.SameAs(definition));
		Assert.That(block.DefaultScreenDefinition.Title, Is.EqualTo("Pinned title"));
		Assert.That(block.DefaultScreenDefinition.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(block.DefaultScreenDefinition.ScreenKind, Is.EqualTo(ScreenKind.Empty));
		Assert.That(block.ScreenDefinitions, Has.Length.EqualTo(1));
		Assert.That(block.Menus.Single().Items.Single().Title, Is.EqualTo("Navigation caption"));
	}

	/// <summary>Verifies explicit default definition remains untouched by matching menu metadata.</summary>
	[Test]
	public void ExplicitDefaultDefinitionRemainsUntouchedByMatchingMenuMetadata() {
		var selected = new ApplicationScreenDefinition(typeof(Screen), "Canonical default");
		var matching = new ApplicationScreenDefinition(typeof(Screen), "Menu screen", ScreenActivationMode.SingleInstance);
		var menu = new MenuBuilder()
			.WithText("Views")
			.AddItem(new ScreenItem { Title = "Navigation caption", ScreenDefinition = matching })
			.Build();
		var builder = new BlockBuilder()
			.WithName("Workspace")
			.AddScreen(selected)
			.AddScreen(matching)
			.WithDefaultScreen(selected)
			.AddMenu(menu);
		var block = builder.Build();

		Assert.That(block.DefaultScreenDefinition, Is.SameAs(selected));
		Assert.That(block.DefaultScreenDefinition.Title, Is.EqualTo("Canonical default"));
		Assert.That(block.DefaultScreenDefinition.ActivationMode, Is.Null);
		Assert.That(block.ScreenDefinitions.Select(definition => definition.Title), Is.EquivalentTo(new[] { selected.Title, matching.Title }));
		Assert.That(block.ScreenDefinitions.Any(definition => ReferenceEquals(definition, block.DefaultScreenDefinition)), Is.True);
	}

	/// <summary>Verifies action items reject screen references.</summary>
	[TestCase(true)]
	[TestCase(false)]
	public void ActionItemsRejectScreenReferences(bool definitionReference) {
		var builder = new ItemBuilder()
			.WithText("Action")
			.WithAction(() => { });
		if (definitionReference)
			Assert.That(() => builder.WithScreen(new ApplicationScreenDefinition(typeof(Screen))), Throws.InvalidOperationException);
		else
			Assert.That(() => builder.WithScreen<Screen>(), Throws.InvalidOperationException);
	}

	/// <summary>Verifies interface returning wizard fluent methods share lifecycle and reset state.</summary>
	[Test]
	public async Task InterfaceReturningWizardFluentMethodsShareLifecycleAndResetState() {
		ITestWizardBuilder builder = new WizardBuilder();
		Assert.That(() => builder.WithModel(new object()), Throws.InvalidOperationException);
		Assert.That(() => builder.NewWizard("Setup").Build(), Throws.InvalidOperationException);
		var first = builder
			.WithModel(new object())
			.WithCancellation(false)
			.AddStep("First")
			.OnFinished(_ => Task.FromResult(new Result<bool>(true)))
			.Build();
		var second = builder
			.NewWizard("Another setup")
			.WithModel(new object())
			.AddStep("Other")
			.OnFinished(_ => Task.FromResult(new Result<bool>(true)))
			.Build();

		Assert.That(first.IsCancellable, Is.False);
		Assert.That(second.IsCancellable, Is.True);
		Assert.That(first.Steps, Is.EqualTo(new[] { "First" }));
		Assert.That(second.Steps, Is.EqualTo(new[] { "Other" }));
		Assert.That((await second.FinishAsync()).Value, Is.True);
	}

	#region Inner Types

	// A supplied settings instance exercises the builder's ownership contract.
	private sealed class Preferences : SettingsObject {
		/// <summary>Gets or sets a caller-controlled value.</summary>
		public string Name { get; set; } = "default";
	}

	// Creates ordinary plugin products through the shared fluent builder contract.
	private sealed class PluginBuilder : ApplicationPluginBuilderBase<PluginBuilder, IApplicationBlock, BlockBuilder, ApplicationPlugin> {
		/// <inheritdoc />
		protected override ApplicationPlugin CreatePlugin(IApplicationBlock[] blocks, Action<IServiceCollection> configureServices) {
			return new(Name, blocks, configureServices);
		}
	}

	// This shared fixture supplies ordinary products; platform graph-copy behavior is covered by the Blazor host tests.
	private sealed class BlockBuilder : ApplicationBlockBuilderBase<BlockBuilder, IApplicationMenu, MenuBuilder, TestApplicationBlock> {
		/// <inheritdoc />
		protected override TestApplicationBlock CreateBlock(IReadOnlyList<IApplicationMenu> menus) {
			var block = new TestApplicationBlock { Name = Name, Position = Position };
			foreach (var menu in menus)
				block.AddMenu(menu);
			return block;
		}
	}

	// Creates ordinary menu products for shared builder behavior checks.
	private sealed class MenuBuilder : ApplicationMenuBuilderBase<MenuBuilder, IApplicationMenuItem, ItemBuilder, TestApplicationMenu<IApplicationMenuItem>> {
		/// <inheritdoc />
		protected override TestApplicationMenu<IApplicationMenuItem> CreateMenu(IReadOnlyList<IApplicationMenuItem> items) {
			var menu = new TestApplicationMenu<IApplicationMenuItem> { Text = Text };
			foreach (var item in items)
				menu.AddItem(item);
			return menu;
		}
	}

	// Creates action commands for shared fluent callback checks.
	private sealed class ItemBuilder : ApplicationMenuItemBuilderBase<ItemBuilder>, IApplicationUIBuilder<ActionItem> {
		/// <summary>Creates the configured test command.</summary>
		public ActionItem Build() {
			ValidateItem();
			return new ActionItem { Title = Text, Action = Action };
		}
	}

	// Stores the deferred callback configured by a test item builder.
	private sealed class ActionItem : TestApplicationMenuItem {
		/// <summary>Gets the deferred action supplied by the builder.</summary>
		public Action Action { get; init; }
	}

	// Tracks a screen definition and reverse activator membership for shared builder checks.
	private sealed class ScreenItem : TestApplicationMenuItem, IScreenMenuItem {
		/// <summary>Gets or sets the test command's exact registration reference.</summary>
		public ApplicationScreenRegistration Registration {
			get => field ?? ScreenDefinition?.Registration;
			set => field = value;
		}

		/// <summary>Gets or sets the test command's canonical definition and reverse membership.</summary>
		public ApplicationScreenDefinition ScreenDefinition {
			get {
				return field;
			}
			set {
				if (Parent != null)
					field?.RemoveActivator(this);
				field = value;
				if (Parent != null)
					field?.AddActivator(this);
			}
		}

		/// <summary>Binds the command to the selected test registration.</summary>
		public void ResolveScreenDefinition(ApplicationScreenDefinition definition) {
			ScreenDefinition = definition;
			if (string.IsNullOrWhiteSpace(Title))
				Title = definition.Title;
		}
	}

	// Supplies a valid platform-neutral screen type.
	private sealed class Screen : IApplicationScreen {
		/// <inheritdoc />
		public IApplicationScreenHost Host { get; set; }

		/// <inheritdoc />
		public IApplicationBlock Block { get; set; }
	}

	// Supplies an alternate platform-neutral screen type.
	private sealed class OtherScreen : IApplicationScreen {
		/// <inheritdoc />
		public IApplicationScreenHost Host { get; set; }

		/// <inheritdoc />
		public IApplicationBlock Block { get; set; }
	}

	// Exercises fluent methods exposed through an interface-returning builder contract.
	private interface ITestWizardBuilder : IApplicationUIBuilder<Wizard<object, string>> {
		/// <summary>Starts a new wizard configuration.</summary>
		ITestWizardBuilder NewWizard(string title);

		/// <summary>Sets the model shared by the wizard steps.</summary>
		ITestWizardBuilder WithModel(object model);

		/// <summary>Sets whether the configured wizard permits cancellation.</summary>
		ITestWizardBuilder WithCancellation(bool isCancellable);

		/// <summary>Adds one step to the configured wizard.</summary>
		ITestWizardBuilder AddStep(string step);

		/// <summary>Sets the deferred completion callback.</summary>
		ITestWizardBuilder OnFinished(Func<object, Task<Result<bool>>> onFinished);
	}

	// Creates a wizard using the shared builder lifecycle and callback state.
	private sealed class WizardBuilder : WizardBuilderBase<ITestWizardBuilder, object, string, Wizard<object, string>>, ITestWizardBuilder {
		/// <inheritdoc />
		public WizardBuilder()
			: base(requiresStart: true, requiresModel: true, requiresFinish: true) {
		}

		/// <inheritdoc />
		protected override Wizard<object, string> CreateWizard(string[] steps) {
			return new(Title, Model, steps, FinishCallback, CancelCallback) { IsCancellable = IsCancellable };
		}
	}

	#endregion
}
