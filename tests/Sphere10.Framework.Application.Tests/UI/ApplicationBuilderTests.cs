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
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationBuilderTests {
	[Test]
	public void BlockBuildsRetainIndependentMenuArrays() {
		var firstMenu = new MenuBuilder()
			.WithText("First menu")
			.Build();
		var secondMenu = new MenuBuilder()
			.WithText("Second menu")
			.Build();
		var builder = new BlockBuilder()
			.WithName("First")
			.AddMenu(firstMenu);
		var first = builder.Build();
		var second = builder
			.WithName("Second")
			.AddMenu(secondMenu)
			.Build();

		Assert.That(first.Name, Is.EqualTo("First"));
		Assert.That(first.MenuDefinitions, Is.EqualTo(new[] { firstMenu }));
		Assert.That(second.MenuDefinitions, Is.EqualTo(new[] { firstMenu, secondMenu }));
		first.MenuDefinitions[0] = secondMenu;
		Assert.That(second.MenuDefinitions, Is.EqualTo(new[] { firstMenu, secondMenu }));
		Assert.That(builder.Build().MenuDefinitions, Is.EqualTo(new[] { firstMenu, secondMenu }));
	}

	[Test]
	public void MenuBuildsRetainIndependentItemArrays() {
		var firstItem = new ItemBuilder()
			.WithText("First")
			.WithAction(() => { })
			.Build();
		var secondItem = new ItemBuilder()
			.WithText("Second")
			.WithAction(() => { })
			.Build();
		var builder = new MenuBuilder()
			.WithText("First menu")
			.AddItem(firstItem);
		var first = builder.Build();
		var second = builder
			.WithText("Second menu")
			.AddItem(secondItem)
			.Build();

		Assert.That(first.Text, Is.EqualTo("First menu"));
		Assert.That(first.ItemDefinitions, Is.EqualTo(new[] { firstItem }));
		Assert.That(second.ItemDefinitions, Is.EqualTo(new[] { firstItem, secondItem }));
		first.ItemDefinitions[0] = secondItem;
		Assert.That(second.ItemDefinitions, Is.EqualTo(new[] { firstItem, secondItem }));
		Assert.That(builder.Build().ItemDefinitions, Is.EqualTo(new[] { firstItem, secondItem }));
	}

	[Test]
	public void ItemBuildsRetainTheirImmutableScreenDefinitionReferences() {
		var firstDefinition = new ApplicationScreenDefinition(typeof(PlainScreen), "Original", ScreenActivationMode.SingleInstance);
		var secondDefinition = new ApplicationScreenDefinition(typeof(PlainScreen), "Second", ScreenActivationMode.SingleInstance);
		var builder = new ItemBuilder()
			.WithText("Screen")
			.WithScreen(firstDefinition);
		var first = (ScreenItemDefinition)builder.Build();
		var second = (ScreenItemDefinition)builder.WithScreen(secondDefinition).Build();

		Assert.That(first.ScreenDefinition, Is.SameAs(firstDefinition));
		Assert.That(second.ScreenDefinition, Is.SameAs(secondDefinition));
		Assert.That(first.ScreenDefinition.Title, Is.EqualTo("Original"));
		Assert.That(second.ScreenDefinition.Title, Is.EqualTo("Second"));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ExplicitDefinitionIdentityDoesNotDependOnCaptionConfigurationOrder(bool captionFirst) {
		var definition = new ApplicationScreenDefinition(typeof(PlainScreen), "Screen title", ScreenActivationMode.SingleInstance);
		var builder = new ItemBuilder();
		if (captionFirst)
			builder.WithText("Menu caption").WithScreen(definition);
		else
			builder.WithScreen(definition).WithText("Menu caption");
		var item = (ScreenItemDefinition)builder.Build();

		Assert.That(item.Title, Is.EqualTo("Menu caption"));
		Assert.That(item.ScreenDefinition, Is.SameAs(definition));
		Assert.That(item.ScreenDefinition.Title, Is.EqualTo("Screen title"));
	}

	[Test]
	public void ActionConfigurationReplacesTheOtherActionKind() {
		var syncCalls = 0;
		var builder = new ItemBuilder()
			.WithText("Action")
			.WithAction(() => syncCalls++);
		var first = builder.Build();
		var second = builder.WithAction((_, _) => Task.CompletedTask).Build();
		var third = builder.WithAction(() => syncCalls += 10).Build();
		first.Action();
		third.Action();

		Assert.That(syncCalls, Is.EqualTo(11));
		Assert.That(first.AsyncAction, Is.Null);
		Assert.That(second.Action, Is.Null);
		Assert.That(second.AsyncAction, Is.Not.Null);
		Assert.That(third.AsyncAction, Is.Null);
	}

	[Test]
	public void ScreenAndActionAreMutuallyExclusive([Values] bool screenFirst, [Values] bool asynchronousAction) {
		var builder = new ItemBuilder().WithText("Item");
		Action addAction = asynchronousAction
			? () => builder.WithAction((_, _) => Task.CompletedTask)
			: () => builder.WithAction(() => { });
		if (screenFirst) {
			builder.WithScreen(typeof(PlainScreen));
			Assert.That(() => addAction(), Throws.TypeOf<InvalidOperationException>());
		} else {
			addAction();
			Assert.That(() => builder.WithScreen(typeof(PlainScreen)), Throws.TypeOf<InvalidOperationException>());
		}
	}

	[TestCase("block")]
	[TestCase("menu")]
	[TestCase("item")]
	public void BuildRejectsIncompleteDefinitions(string definitionKind) {
		Action build = definitionKind switch {
			"block" => () => new BlockBuilder().Build(),
			"menu" => () => new MenuBuilder().Build(),
			_ => () => new ItemBuilder().WithText("Empty").Build()
		};

		Assert.That(() => build(), Throws.TypeOf<InvalidOperationException>());
	}

	private sealed record BlockDefinition(string Name, MenuDefinition[] MenuDefinitions) : IApplicationBlock {
		public IApplication Parent { get; set; }

		public IApplicationPlugin Source { get; set; }

		public int Position => 0;

		public ApplicationScreenDefinition[] ScreenDefinitions => Array.Empty<ApplicationScreenDefinition>();

		public ApplicationScreenDefinition DefaultScreenDefinition => null;

		public IApplicationMenu[] Menus => MenuDefinitions;
	}

	private sealed record MenuDefinition(string Text, ItemDefinition[] ItemDefinitions) : IApplicationMenu {
		public IApplicationBlock Parent { get; set; }

		public IApplicationMenuItem[] Items => ItemDefinitions;
	}

	private record ItemDefinition(
		string Title, Action Action, Func<IServiceProvider, CancellationToken, Task> AsyncAction
	) : IApplicationMenuItem {
		public IApplicationMenu Parent { get; set; }
	}

	private sealed record ScreenItemDefinition(string ScreenTitle, ApplicationScreenDefinition ScreenDefinition)
		: ItemDefinition(ScreenTitle, null, null), IScreenMenuItem;

	private sealed class BlockBuilder : ApplicationBlockBuilderBase<MenuDefinition, BlockDefinition> {
		public BlockBuilder WithName(string name) {
			SetName(name);
			return this;
		}

		public BlockBuilder AddMenu(MenuDefinition menu) {
			AddMenuDefinition(menu);
			return this;
		}

		protected override BlockDefinition CreateBlock(IReadOnlyList<MenuDefinition> menus) => new(Name, menus.ToArray());
	}

	private sealed class MenuBuilder : ApplicationMenuBuilderBase<ItemDefinition, MenuDefinition> {
		public MenuBuilder WithText(string text) {
			SetText(text);
			return this;
		}

		public MenuBuilder AddItem(ItemDefinition item) {
			AddItemInternal(item);
			return this;
		}

		protected override MenuDefinition CreateMenu(IReadOnlyList<ItemDefinition> items) => new(Text, items.ToArray());
	}

	private sealed class ItemBuilder : ApplicationMenuItemBuilderBase {
		public ItemBuilder WithText(string text) {
			SetText(text);
			return this;
		}

		public ItemBuilder WithScreen(Type screenType) {
			SetScreenType(screenType);
			return this;
		}

		public ItemBuilder WithScreen(ApplicationScreenDefinition definition) {
			SetScreenDefinition(definition);
			return this;
		}

		public ItemBuilder WithAction(Action action) {
			SetAction(action);
			return this;
		}

		public ItemBuilder WithAction(Func<IServiceProvider, CancellationToken, Task> action) {
			SetAction(action);
			return this;
		}

		public ItemDefinition Build() {
			ValidateItem();
			return ScreenDefinition == null ? new ItemDefinition(Text, Action, AsyncAction) : new ScreenItemDefinition(Text, ScreenDefinition);
		}
	}

	private class PlainScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
	}
}
