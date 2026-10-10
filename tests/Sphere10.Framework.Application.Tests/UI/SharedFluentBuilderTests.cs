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

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SharedFluentBuilderTests {
	[Test]
	public void PluginComposesTypedChildBuildersAndOrderedServiceCallbacks() {
		var calls = new List<string>();
		var plugin = new PluginBuilder()
			.WithName("Operations")
			.ConfigureServices(_ => calls.Add("first"))
			.ConfigureBlock(block => block
				.WithName("Inventory")
				.WithPosition(3)
				.WithDefaultScreen<Screen>("Stock", ScreenActivationMode.SingleInstance)
				.ConfigureMenu(menu => menu
					.WithText("Items")
					.ConfigureItem(item => item
						.WithText("Adjust stock")
						.WithAction(() => calls.Add("action"))
					)
				)
			)
			.ConfigureServices(_ => calls.Add("second"))
			.Build();

		plugin.Load(new ServiceCollection());
		var block = plugin.Blocks[0];
		var menu = block.Menus[0];
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

	[Test]
	public void FailedChildConfigurationDoesNotAppendPartialDefinitions() {
		var builder = new PluginBuilder()
			.WithName("Operations")
			.ConfigureBlock(block => block
				.WithName("First")
			);
		Assert.That(() => builder.ConfigureBlock(block => block.WithName("Invalid").ConfigureMenu(menu => menu.WithText(""))), Throws.InvalidOperationException);
		Assert.That(builder.Build().Blocks.Length, Is.EqualTo(1));
		Assert.That(() => builder.ConfigureBlock((Action<BlockBuilder>)null), Throws.ArgumentNullException);
	}

	[Test]
	public void FluentScreenPolicyRejectsInvalidCombinationsBeforeAttachingBlock() {
		var builder = new PluginBuilder().WithName("Operations");
		Assert.That(() => builder.ConfigureBlock(block => block
			.WithName("Invalid")
			.WithDefaultScreen<Screen>(activationMode: ScreenActivationMode.PermanentSingleton, screenKind: ScreenKind.Empty)), Throws.ArgumentException);
		Assert.That(builder.Build().Blocks, Is.Empty);
	}

	[Test]
	public void RepeatedMenuAndBlockBuildsProduceIndependentOwningGraphs() {
		var menuBuilder = new MenuBuilder().WithText("Views")
			.AddItem(new ScreenItem { Title = "Open", ScreenDefinition = new(typeof(Screen)) });
		var firstMenu = menuBuilder.Build();
		var secondMenu = menuBuilder.Build();
		Assert.That(firstMenu.Items.Single().Parent, Is.SameAs(firstMenu));
		Assert.That(secondMenu.Items.Single().Parent, Is.SameAs(secondMenu));
		Assert.That(secondMenu.Items.Single(), Is.Not.SameAs(firstMenu.Items.Single()));
		var builder = new BlockBuilder().WithName("Work").AddMenu(firstMenu).WithDefaultScreen<Screen>();
		var first = builder.Build();
		var second = builder.Build();
		Assert.That(first.Menus.Single().Parent, Is.SameAs(first));
		Assert.That(second.Menus.Single().Parent, Is.SameAs(second));
		Assert.That(first.DefaultScreenDefinition, Is.SameAs(((IScreenMenuItem)first.Menus.Single().Items.Single()).ScreenDefinition));
		Assert.That(second.DefaultScreenDefinition, Is.SameAs(((IScreenMenuItem)second.Menus.Single().Items.Single()).ScreenDefinition));
		Assert.That(second.DefaultScreenDefinition, Is.Not.SameAs(first.DefaultScreenDefinition));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void TypeBasedDefaultReusesTheMatchingMenusCanonicalDefinition(bool genericDefault) {
		var definition = new ApplicationScreenDefinition(typeof(Screen), "Empty workspace", ScreenActivationMode.SingleInstance, ScreenKind.Empty);
		var menu = new MenuBuilder().WithText("Views")
			.AddItem(new ScreenItem { Title = "Navigation caption", ScreenDefinition = definition }).Build();
		var builder = new BlockBuilder().WithName("Workspace").AddMenu(menu);
		if (genericDefault)
			builder.WithDefaultScreen<Screen>();
		else
			builder.WithDefaultScreen(typeof(Screen));
		var block = builder.Build();

		Assert.That(block.DefaultScreenDefinition, Is.Not.SameAs(definition));
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(block.DefaultScreenDefinition));
		Assert.That(((IScreenMenuItem)block.Menus.Single().Items.Single()).ScreenDefinition, Is.SameAs(block.DefaultScreenDefinition));
		Assert.That(block.DefaultScreenDefinition.Title, Is.EqualTo("Empty workspace"));
		Assert.That(block.DefaultScreenDefinition.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(block.DefaultScreenDefinition.ScreenKind, Is.EqualTo(ScreenKind.Empty));
	}

	[Test]
	public void TypeBasedDefaultReusesAnExplicitlyRegisteredDefinitionWithoutAMenu() {
		var definition = new ApplicationScreenDefinition(typeof(Screen), "Standalone workspace", ScreenActivationMode.SingleInstance, ScreenKind.Empty);
		var builder = new BlockBuilder().WithName("Workspace")
			.AddScreenDefinition(definition).WithDefaultScreen<Screen>();
		var block = builder.Build();

		Assert.That(block.Menus, Is.Empty);
		Assert.That(block.DefaultScreenDefinition, Is.Not.SameAs(definition));
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(block.DefaultScreenDefinition));
		Assert.That(block.DefaultScreenDefinition.Title, Is.EqualTo("Standalone workspace"));
		Assert.That(block.DefaultScreenDefinition.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(block.DefaultScreenDefinition.ScreenKind, Is.EqualTo(ScreenKind.Empty));
		Assert.That(builder.Build().DefaultScreenDefinition, Is.Not.SameAs(block.DefaultScreenDefinition));
	}

	[Test]
	public void ExplicitDefaultTitleOverridesOnlyTheInferredCaption() {
		var definition = new ApplicationScreenDefinition(typeof(Screen), "Menu screen", ScreenActivationMode.SingleInstance, ScreenKind.Empty);
		var menu = new MenuBuilder().WithText("Views")
			.AddItem(new ScreenItem { Title = "Navigation caption", ScreenDefinition = definition }).Build();
		var builder = new BlockBuilder().WithName("Workspace")
			.WithDefaultScreen<Screen>("Pinned title").AddMenu(menu);
		var block = builder.Build();

		Assert.That(block.DefaultScreenDefinition, Is.Not.SameAs(definition));
		Assert.That(block.DefaultScreenDefinition.Title, Is.EqualTo("Pinned title"));
		Assert.That(block.DefaultScreenDefinition.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(block.DefaultScreenDefinition.ScreenKind, Is.EqualTo(ScreenKind.Empty));
		Assert.That(block.ScreenDefinitions, Has.Length.EqualTo(2));
		Assert.That(builder.Build().DefaultScreenDefinition, Is.Not.SameAs(block.DefaultScreenDefinition));
	}

	[Test]
	public void ExplicitDefaultDefinitionRemainsUntouchedByMatchingMenuMetadata() {
		var selected = new ApplicationScreenDefinition(typeof(Screen), "Canonical default");
		var matching = new ApplicationScreenDefinition(typeof(Screen), "Menu screen", ScreenActivationMode.SingleInstance);
		var menu = new MenuBuilder().WithText("Views")
			.AddItem(new ScreenItem { Title = "Navigation caption", ScreenDefinition = matching }).Build();
		var builder = new BlockBuilder().WithName("Workspace").WithDefaultScreen(selected).AddMenu(menu);
		var block = builder.Build();

		Assert.That(block.DefaultScreenDefinition, Is.Not.SameAs(selected));
		Assert.That(block.DefaultScreenDefinition.Title, Is.EqualTo("Canonical default"));
		Assert.That(block.DefaultScreenDefinition.ActivationMode, Is.Null);
		Assert.That(block.ScreenDefinitions.Select(definition => definition.Title), Is.EquivalentTo(new[] { selected.Title, matching.Title }));
		Assert.That(block.ScreenDefinitions.Any(definition => ReferenceEquals(definition, block.DefaultScreenDefinition)), Is.True);
		Assert.That(builder.Build().DefaultScreenDefinition, Is.Not.SameAs(block.DefaultScreenDefinition));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void RepeatedBuildsReevaluateInferredMetadataWithoutOverwritingTheDefaultTemplate(bool explicitTitle) {
		var original = new ApplicationScreenDefinition(typeof(Screen), "Original", ScreenActivationMode.SingleInstance);
		var item = new ScreenItem { Title = "Navigation caption", ScreenDefinition = original };
		var menu = new MenuBuilder().WithText("Views").AddItem(item).Build();
		var builder = new BlockBuilder().WithName("Workspace")
			.WithDefaultScreen<Screen>(explicitTitle ? "Pinned title" : null).AddMenu(menu);
		var first = builder.Build();
		var updated = new ApplicationScreenDefinition(typeof(Screen), "Updated", ScreenActivationMode.MultiInstance, ScreenKind.Empty);
		((ScreenItem)menu.Items.Single()).ScreenDefinition = updated;
		var second = builder.Build();
		var third = builder.Build();

		Assert.That(first.DefaultScreenDefinition.Title, Is.EqualTo(explicitTitle ? "Pinned title" : "Original"));
		Assert.That(first.DefaultScreenDefinition.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(first.DefaultScreenDefinition.ScreenKind, Is.EqualTo(ScreenKind.Normal));
		Assert.That(second.DefaultScreenDefinition.Title, Is.EqualTo(explicitTitle ? "Pinned title" : "Updated"));
		Assert.That(second.DefaultScreenDefinition.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		Assert.That(second.DefaultScreenDefinition.ScreenKind, Is.EqualTo(ScreenKind.Empty));
		Assert.That(second.ScreenDefinitions.Any(definition => definition.Title == updated.Title), Is.True);
		Assert.That(third.DefaultScreenDefinition, Is.Not.SameAs(second.DefaultScreenDefinition));
		Assert.That(third.DefaultScreenDefinition.Title, Is.EqualTo(second.DefaultScreenDefinition.Title));
		if (!explicitTitle)
			Assert.That(second.DefaultScreenDefinition, Is.SameAs(((IScreenMenuItem)second.Menus.Single().Items.Single()).ScreenDefinition));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void RemovingTheMatchingMenuScreenRestoresTheOriginalDefaultTemplate(bool explicitTitle) {
		var matching = new ApplicationScreenDefinition(typeof(Screen), "Inferred title", ScreenActivationMode.SingleInstance, ScreenKind.Empty);
		var item = new ScreenItem { Title = "Navigation caption", ScreenDefinition = matching };
		var menu = new MenuBuilder().WithText("Views").AddItem(item).Build();
		var builder = new BlockBuilder().WithName("Workspace")
			.WithDefaultScreen<Screen>(explicitTitle ? "Pinned title" : null).AddMenu(menu);
		var first = builder.Build();
		((ScreenItem)menu.Items.Single()).ScreenDefinition = new(typeof(OtherScreen), "Other screen", ScreenActivationMode.MultiInstance);
		var second = builder.Build();

		Assert.That(first.DefaultScreenDefinition.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(first.DefaultScreenDefinition.ScreenKind, Is.EqualTo(ScreenKind.Empty));
		Assert.That(second.DefaultScreenDefinition.ScreenType, Is.EqualTo(typeof(Screen)));
		Assert.That(second.DefaultScreenDefinition.Title, Is.EqualTo(explicitTitle ? "Pinned title" : nameof(Screen)));
		Assert.That(second.DefaultScreenDefinition.ActivationMode, Is.Null);
		Assert.That(second.DefaultScreenDefinition.ScreenKind, Is.EqualTo(ScreenKind.Normal));
		Assert.That(second.ScreenDefinitions.Any(definition => definition.ScreenType == matching.ScreenType && definition.Title == matching.Title
			&& definition.ActivationMode == matching.ActivationMode && definition.ScreenKind == matching.ScreenKind), Is.False);
		Assert.That(builder.Build().DefaultScreenDefinition, Is.Not.SameAs(second.DefaultScreenDefinition));
	}

	[TestCase(true)]
	[TestCase(false)]
	public void ActionItemsRejectScreenOnlyConfiguration(bool activationMode) {
		var builder = new ItemBuilder()
			.WithText("Action")
			.WithAction(() => { });
		if (activationMode)
			builder.AsSingleInstance();
		else
			builder.WithScreenTitle("Screen caption");
		Assert.That(() => builder.Build(), Throws.InvalidOperationException);
	}

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

	private sealed class PluginBuilder : ApplicationPluginBuilderBase<PluginBuilder, IApplicationBlock, BlockBuilder, ApplicationPlugin> {
		protected override ApplicationPlugin CreatePlugin(IApplicationBlock[] blocks, Action<IServiceCollection> configureServices) => new(Name, blocks, configureServices);
	}

	private sealed class BlockBuilder : ApplicationBlockBuilderBase<BlockBuilder, IApplicationMenu, MenuBuilder, TestApplicationBlock> {
		protected override TestApplicationBlock CreateBlock(IReadOnlyList<IApplicationMenu> menus) {
			var template = new BlockTemplate(menus.ToArray()) { Name = Name, Position = Position };
			foreach (var definition in ScreenDefinitions)
				template.AddScreenDefinition(definition);
			template.DefaultScreenDefinition = DefaultScreenDefinition;
			return new DefinitionSnapshot().Create(template);
		}

		protected override void RegisterScreenDefinitions(TestApplicationBlock block) {
		}
	}

	private sealed class MenuBuilder : ApplicationMenuBuilderBase<MenuBuilder, IApplicationMenuItem, ItemBuilder, TestApplicationMenu<IApplicationMenuItem>> {
		protected override TestApplicationMenu<IApplicationMenuItem> CreateMenu(IReadOnlyList<IApplicationMenuItem> items) {
			var menu = new MenuTemplate { Text = Text };
			foreach (var item in items)
				menu.AddItem(item);
			return (TestApplicationMenu<IApplicationMenuItem>)new DefinitionSnapshot().CreateMenu(menu);
		}
	}

	private sealed class BlockTemplate(IApplicationMenu[] menus) : TestApplicationBlock {
		public override IApplicationMenu[] Menus => Tools.Array.Clone(menus);
	}

	private sealed class MenuTemplate : TestApplicationMenu<IApplicationMenuItem> {
		protected override bool OwnsItems => false;
	}

	private sealed class DefinitionSnapshot : ApplicationBlockSnapshotBase<TestApplicationBlock, IApplicationMenu, IApplicationMenuItem> {
		protected override TestApplicationBlock CreateBlockSnapshot(TestApplicationBlock block, IApplicationMenu[] menus,
			Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition> definitions) {
			var snapshot = new TestApplicationBlock { Name = block.Name, Position = block.Position };
			foreach (var definition in block.ScreenDefinitions)
				snapshot.AddScreenDefinition(SnapshotDefinition(definition, definitions));
			snapshot.DefaultScreenDefinition = SnapshotDefinition(block.DefaultScreenDefinition, definitions);
			foreach (var menu in menus)
				snapshot.AddMenu(menu);
			return snapshot;
		}

		protected override IApplicationMenu CreateMenuSnapshot(IApplicationMenu menu, IApplicationMenuItem[] items) {
			var snapshot = new TestApplicationMenu<IApplicationMenuItem> { Text = menu.Text };
			foreach (var item in items)
				snapshot.AddItem(item);
			return snapshot;
		}

		protected override IApplicationMenuItem CreateItemSnapshot(IApplicationMenuItem item, Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition> definitions) {
			var snapshot = item switch {
				IScreenMenuItem screen => (TestApplicationMenuItem)new ScreenItem { ScreenDefinition = SnapshotDefinition(screen.ScreenDefinition, definitions) },
				ActionItem action => new ActionItem { Action = action.Action },
				_ => new TestApplicationMenuItem()
			};
			snapshot.Title = item.Title;
			if (item is ApplicationMenuItemBase subscriptions)
				snapshot.CopySubscriptionsFrom(subscriptions);
			return snapshot;
		}
	}

	private sealed class ItemBuilder : ApplicationMenuItemBuilderBase<ItemBuilder>, IApplicationUIBuilder<ActionItem> {
		public ActionItem Build() {
			ValidateItem();
			return new ActionItem { Title = Text, Action = Action };
		}
	}

	private sealed class ActionItem : TestApplicationMenuItem {
		public Action Action { get; init; }
	}

	private sealed class ScreenItem : TestApplicationMenuItem, IScreenMenuItem {
		private ApplicationScreenDefinition _definition;

		public ApplicationScreenDefinition ScreenDefinition {
			get => _definition;
			set {
				if (Parent != null)
					_definition?.RemoveActivator(this);
				_definition = value;
				if (Parent != null)
					_definition?.AddActivator(this);
			}
		}
	}

	private sealed class Screen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
	}

	private sealed class OtherScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
	}

	private interface ITestWizardBuilder : IApplicationUIBuilder<Wizard<object, string>> {
		ITestWizardBuilder NewWizard(string title);
		ITestWizardBuilder WithModel(object model);
		ITestWizardBuilder WithCancellation(bool isCancellable);
		ITestWizardBuilder AddStep(string step);
		ITestWizardBuilder OnFinished(Func<object, Task<Result<bool>>> onFinished);
	}

	private sealed class WizardBuilder : WizardBuilderBase<ITestWizardBuilder, object, string, Wizard<object, string>>, ITestWizardBuilder {
		public WizardBuilder()
			: base(requiresStart: true, requiresModel: true, requiresFinish: true) {
		}

		protected override Wizard<object, string> CreateWizard(string[] steps) =>
			new(Title, Model, steps, FinishCallback, CancelCallback) { IsCancellable = IsCancellable };
	}
}
