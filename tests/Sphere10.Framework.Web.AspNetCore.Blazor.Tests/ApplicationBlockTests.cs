// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.


using Sphere10.Framework.Application.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationBlockTests {
	[Test]
	public void MenuCopiesInputAndReturnedItemArrays() {
		var item = new BlazorActionMenuItem { Id = "run", Title = "Run", Action = () => { } };
		var items = new IBlazorApplicationMenuItem[] { item };
		var menu = new BlazorApplicationMenu { Text = "Actions", Items = items };
		items[0] = null;
		var exposed = menu.Items;
		exposed[0] = null;

		Assert.That(menu.Items, Has.Length.EqualTo(1));
		Assert.That(menu.Items[0], Is.SameAs(item));
	}

	[Test]
	public void CatalogCopiesMutableRegistrationCollectionsAndParameters() {
		var parameters = new Dictionary<string, object> { ["Message"] = "Original" };
		var items = new IBlazorApplicationMenuItem[] {
			new BlazorScreenMenuItem { Id = "screen", Title = "Screen", ScreenType = typeof(TestScreen), Parameters = parameters }
		};
		var menus = new IBlazorApplicationMenu[] { new BlazorApplicationMenu { Id = "menu", Text = "Menu", Items = items } };
		var blocks = new List<IBlazorApplicationBlock> { new BlazorApplicationBlock { Id = "block", Title = "Block", Menus = menus } };
		var catalog = new BlazorApplicationBlockCatalog(blocks);
		parameters["Message"] = "Changed";
		items[0] = null;
		menus[0] = null;
		blocks.Clear();
		catalog.Blocks[0] = null;
		catalog.Get("block").Menus[0].Items[0] = null;

		var block = catalog.Get("block");
		var screen = (BlazorScreenMenuItem)block.Menus.Single().Items.Single();
		Assert.That(catalog.Blocks, Has.Length.EqualTo(1));
		Assert.That(screen.Parameters["Message"], Is.EqualTo("Original"));
		Assert.That(() => ((IDictionary<string, object>)screen.Parameters).Add("New", "value"), Throws.TypeOf<NotSupportedException>());
	}

	[Test]
	public void BlockReturnsIndependentTypedMenuArraysWithoutChangingMetadata() {
		var firstMenu = new BlazorApplicationMenu { Id = "first", Text = "First" };
		var secondMenu = new BlazorApplicationMenu { Id = "second", Text = "Second" };
		var sourceMenus = new IBlazorApplicationMenu[] { firstMenu, secondMenu };
		var block = new BlazorApplicationBlock { Title = "Block", Menus = sourceMenus };
		var menus = block.Menus;
		IApplicationBlock sharedBlock = block;
		sourceMenus[0] = null;

		Assert.That(block.Menus, Is.Not.SameAs(menus));
		Assert.That(menus, Has.Length.EqualTo(2));
		Assert.That(menus[0], Is.SameAs(firstMenu));
		Assert.That(menus[1], Is.SameAs(secondMenu));
		Assert.That(sharedBlock.Menus, Is.EqualTo(new[] { firstMenu, secondMenu }));
		menus[0] = null;
		sharedBlock.Menus[1] = null;
		Assert.That(block.Menus, Is.EqualTo(new[] { firstMenu, secondMenu }));
	}

	[Test]
	public void BuilderBuildsIndependentSnapshots() {
		var builder = new BlazorApplicationBlockBuilder().WithId("first").WithName("First").AddMenu(menu => menu.WithText("Screens")
			.AddScreenItem<TestScreen>("first-screen", "First screen"));
		var first = builder.Build();
		var second = builder.WithId("second").WithName("Second").AddMenu(menu => menu.WithText("Actions")
			.AddActionItem("refresh", "Refresh", () => { })).Build();

		Assert.That(first.Id, Is.EqualTo("first"));
		Assert.That(first.Title, Is.EqualTo("First"));
		Assert.That(first.Name, Is.EqualTo(first.Title));
		Assert.That(first.Menus.Length, Is.EqualTo(1));
		Assert.That(second.Menus.Length, Is.EqualTo(2));
	}

	[TestCase("block")]
	[TestCase("menu")]
	[TestCase("item")]
	public void DuplicateStableIdsAreRejected(string duplicateKind) {
		var first = new BlazorApplicationBlockBuilder().WithId("block").WithName("Block")
			.AddMenu(menu => menu.WithId("menu").WithText("First").AddScreenItem<TestScreen>("item", "First"));
		Action build = duplicateKind switch {
			"block" => () => new BlazorApplicationBlockCatalog(new[] { first.Build(), first.Build() }),
			"menu" => () => first.AddMenu(menu => menu.WithId("menu").WithText("Second").AddScreenItem<TestScreen>("other-item", "Second")).Build(),
			_ => () => first.AddMenu(menu => menu.WithId("other-menu").WithText("Second").AddScreenItem<TestScreen>("item", "Second")).Build()
		};
		Assert.That(build, Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void ReservedDefaultIdIsRejected() {
		Assert.That(() => new BlazorApplicationBlockBuilder().WithName("Block")
			.AddMenu(menu => menu.WithText("Menu").AddScreenItem<TestScreen>("__default", "Screen")).Build(), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void EmptyMenuIdIsRejected() {
		var menu = new BlazorApplicationMenu { Id = "", Text = "Menu" };
		Assert.That(() => new BlazorApplicationBlockCatalog(new[] { new BlazorApplicationBlock { Id = "block", Title = "Block", Menus = new[] { menu } } }),
			Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void CatalogOrdersBlocksByPosition() {
		var second = new BlazorApplicationBlockBuilder().WithId("second").WithName("Second").WithPosition(20).Build();
		var first = new BlazorApplicationBlockBuilder().WithId("first").WithName("First").WithPosition(10).Build();
		Assert.That(new BlazorApplicationBlockCatalog(new[] { second, first }).Blocks.Select(block => block.Id), Is.EqualTo(new[] { "first", "second" }));
	}

	[Test]
	public void ConflictingActivationPoliciesAcrossBlocksAreRejected() {
		var single = new BlazorApplicationBlockBuilder().WithId("single").WithName("Single").WithDefaultScreen<TestScreen>().Build();
		var multiple = new BlazorApplicationBlockBuilder().WithId("multiple").WithName("Multiple")
			.AddMenu(menu => menu.WithText("Menu").AddScreenItem<TestScreen>("screen", "Screen", ScreenActivationMode.MultiInstance)).Build();
		Assert.That(() => new BlazorApplicationBlockCatalog(new[] { single, multiple }), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase(typeof(ComponentBase))]
	[TestCase(typeof(string))]
	[TestCase(typeof(AbstractScreen))]
	[TestCase(typeof(GenericScreen<>))]
	public void InvalidScreenTypesAreRejected(Type screenType) {
		Assert.That(() => new BlazorApplicationBlockBuilder().WithName("Block").WithDefaultScreen(screenType), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public async Task DefaultScreenTitleAndStableIdAreRetained() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new BlazorApplicationBlockBuilder().WithId("block").WithName("Block").WithDefaultScreen<TestScreen>("Dashboard")
			.AddMenu(menu => menu.WithText("Menu").AddScreenItem<TestScreen>("screen", "Open dashboard")).Build();
		using var host = new BlazorApplicationScreenHost(new BlazorApplicationBlockCatalog(new[] { block }), provider);
		var session = await host.ActivateBlockAsync("block");

		Assert.That(session.Title, Is.EqualTo("Dashboard"));
		Assert.That(session.MenuItem.Id, Is.EqualTo("screen"));
		Assert.That(await host.ActivateScreenAsync("block", session.MenuItem.Id), Is.SameAs(session));
	}

	[Test]
	public async Task DefaultOnlyScreenRoundTripsThroughItsSyntheticId() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new BlazorApplicationBlockBuilder().WithId("block").WithName("Block").WithDefaultScreen<TestScreen>("Dashboard").Build();
		var catalog = new BlazorApplicationBlockCatalog(new[] { block });
		using var firstHost = new BlazorApplicationScreenHost(catalog, provider);
		using var secondHost = new BlazorApplicationScreenHost(catalog, provider);
		var session = await firstHost.ActivateBlockAsync("block");
		var reopened = await secondHost.ActivateScreenAsync("block", session.MenuItem.Id);

		Assert.That(reopened.ScreenType, Is.EqualTo(typeof(TestScreen)));
		Assert.That(reopened.Title, Is.EqualTo("Dashboard"));
		Assert.That(reopened.Id, Is.Not.EqualTo(session.Id));
	}

	public class TestScreen : ComponentBase, IBlazorApplicationScreen {
	}

	public abstract class AbstractScreen : ComponentBase, IBlazorApplicationScreen {
	}

	public class GenericScreen<T> : ComponentBase, IBlazorApplicationScreen {
	}
}

