// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
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
				.WithId("inventory")
				.WithName("Inventory")
				.WithPosition(3)
				.WithDefaultScreen<Screen>("Stock", ScreenActivationMode.SingleInstance)
				.ConfigureMenu(menu => menu
					.WithId("items")
					.WithText("Items")
					.ConfigureItem(item => item
						.WithId("adjust")
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
		Assert.That(block.Id, Is.EqualTo("inventory"));
		Assert.That(block.Position, Is.EqualTo(3));
		Assert.That(block.DefaultScreen, Is.EqualTo(typeof(Screen)));
		Assert.That(block.DefaultScreenTitle, Is.EqualTo("Stock"));
		Assert.That(block.DefaultScreenActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(menu.Id, Is.EqualTo("items"));
		Assert.That(menu.Items[0].Id, Is.EqualTo("adjust"));
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

	private sealed class BlockBuilder : ApplicationBlockBuilderBase<BlockBuilder, IApplicationMenu, MenuBuilder, ApplicationBlock> {
		protected override ApplicationBlock CreateBlock(IReadOnlyList<IApplicationMenu> menus) {
			var block = new ApplicationBlock {
				Id = Id, Name = Name, Position = Position, DefaultScreen = DefaultScreen, DefaultScreenTitle = DefaultScreenTitle,
				DefaultScreenActivationMode = DefaultScreenActivationMode, DefaultScreenKind = DefaultScreenKind
			};
			foreach (var menu in menus)
				block.AddMenu(menu);
			return block;
		}
	}

	private sealed class MenuBuilder : ApplicationMenuBuilderBase<MenuBuilder, IApplicationMenuItem, ItemBuilder, ApplicationMenu<IApplicationMenuItem>> {
		protected override ApplicationMenu<IApplicationMenuItem> CreateMenu(IReadOnlyList<IApplicationMenuItem> items) {
			var menu = new ApplicationMenu<IApplicationMenuItem> { Id = Id, Text = Text };
			foreach (var item in items)
				menu.AddItem(item);
			return menu;
		}
	}

	private sealed class ItemBuilder : ApplicationMenuItemBuilderBase<ItemBuilder>, IApplicationUIBuilder<ActionItem> {
		public ActionItem Build() {
			ValidateItem();
			return new ActionItem { Id = Id, Title = Text, Action = Action };
		}
	}

	private sealed class ActionItem : ApplicationMenuItem {
		public Action Action { get; init; }
	}

	private sealed class Screen : IApplicationScreen {
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
