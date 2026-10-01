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
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationBlockTests {
	[Test]
	public void CatalogCopiesMutableRegistrationCollectionsAndParameters() {
		var parameters = new Dictionary<string, object> { ["Message"] = "Original" };
		var items = new List<IApplicationMenuItem> {
			new ShowScreenMenuItem { Id = "screen", Title = "Screen", ScreenType = typeof(TestScreen), Parameters = parameters }
		};
		var menus = new List<IApplicationMenu> { new ApplicationMenu { Id = "menu", Text = "Menu", Items = items } };
		var blocks = new List<IApplicationBlock> { new ApplicationBlock { Id = "block", Title = "Block", Menus = menus } };
		var catalog = new ApplicationBlockCatalog(blocks);
		parameters["Message"] = "Changed";
		items.Clear();
		menus.Clear();
		blocks.Clear();

		var block = catalog.Get("block");
		var screen = (ShowScreenMenuItem)block.Menus.Single().Items.Single();
		Assert.That(catalog.Blocks, Has.Count.EqualTo(1));
		Assert.That(screen.Parameters["Message"], Is.EqualTo("Original"));
		Assert.That(() => ((IDictionary<string, object>)screen.Parameters).Add("New", "value"), Throws.TypeOf<NotSupportedException>());
	}

	[Test]
	public void BuilderBuildsIndependentSnapshots() {
		var builder = new ApplicationBlockBuilder().WithId("first").WithName("First").AddMenu(menu => menu.WithText("Screens")
			.AddScreenItem<TestScreen>("first-screen", "First screen"));
		var first = builder.Build();
		var second = builder.WithId("second").WithName("Second").AddMenu(menu => menu.WithText("Actions")
			.AddActionItem("refresh", "Refresh", () => { })).Build();

		Assert.That(first.Id, Is.EqualTo("first"));
		Assert.That(first.Title, Is.EqualTo("First"));
		Assert.That(first.Name, Is.EqualTo(first.Title));
		Assert.That(first.Menus.Count(), Is.EqualTo(1));
		Assert.That(second.Menus.Count(), Is.EqualTo(2));
	}

	[TestCase("block")]
	[TestCase("menu")]
	[TestCase("item")]
	public void DuplicateStableIdsAreRejected(string duplicateKind) {
		var first = new ApplicationBlockBuilder().WithId("block").WithName("Block")
			.AddMenu(menu => menu.WithId("menu").WithText("First").AddScreenItem<TestScreen>("item", "First"));
		Action build = duplicateKind switch {
			"block" => () => new ApplicationBlockCatalog(new[] { first.Build(), first.Build() }),
			"menu" => () => first.AddMenu(menu => menu.WithId("menu").WithText("Second").AddScreenItem<TestScreen>("other-item", "Second")).Build(),
			_ => () => first.AddMenu(menu => menu.WithId("other-menu").WithText("Second").AddScreenItem<TestScreen>("item", "Second")).Build()
		};
		Assert.That(build, Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void ReservedDefaultIdIsRejected() {
		Assert.That(() => new ApplicationBlockBuilder().WithName("Block")
			.AddMenu(menu => menu.WithText("Menu").AddScreenItem<TestScreen>("__default", "Screen")).Build(), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void EmptyMenuIdIsRejected() {
		var menu = new ApplicationMenu { Id = "", Text = "Menu" };
		Assert.That(() => new ApplicationBlockCatalog(new[] { new ApplicationBlock { Id = "block", Title = "Block", Menus = new[] { menu } } }),
			Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void CatalogOrdersBlocksByPosition() {
		var second = new ApplicationBlockBuilder().WithId("second").WithName("Second").WithPosition(20).Build();
		var first = new ApplicationBlockBuilder().WithId("first").WithName("First").WithPosition(10).Build();
		Assert.That(new ApplicationBlockCatalog(new[] { second, first }).Blocks.Select(block => block.Id), Is.EqualTo(new[] { "first", "second" }));
	}

	[Test]
	public void ConflictingActivationPoliciesAcrossBlocksAreRejected() {
		var single = new ApplicationBlockBuilder().WithId("single").WithName("Single").WithDefaultScreen<TestScreen>().Build();
		var multiple = new ApplicationBlockBuilder().WithId("multiple").WithName("Multiple")
			.AddMenu(menu => menu.WithText("Menu").AddScreenItem<TestScreen>("screen", "Screen", ScreenActivationMode.MultiInstance)).Build();
		Assert.That(() => new ApplicationBlockCatalog(new[] { single, multiple }), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase(typeof(ComponentBase))]
	[TestCase(typeof(string))]
	[TestCase(typeof(AbstractScreen))]
	[TestCase(typeof(GenericScreen<>))]
	public void InvalidScreenTypesAreRejected(Type screenType) {
		Assert.That(() => new ApplicationBlockBuilder().WithName("Block").WithDefaultScreen(screenType), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public async Task DefaultScreenTitleAndStableIdAreRetained() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new ApplicationBlockBuilder().WithId("block").WithName("Block").WithDefaultScreen<TestScreen>("Dashboard")
			.AddMenu(menu => menu.WithText("Menu").AddScreenItem<TestScreen>("screen", "Open dashboard")).Build();
		using var host = new ApplicationScreenHost(new ApplicationBlockCatalog(new[] { block }), provider);
		var session = await host.ActivateBlockAsync("block");

		Assert.That(session.Title, Is.EqualTo("Dashboard"));
		Assert.That(session.MenuItem.Id, Is.EqualTo("screen"));
		Assert.That(await host.ActivateScreenAsync("block", session.MenuItem.Id), Is.SameAs(session));
	}

	[Test]
	public async Task DefaultOnlyScreenRoundTripsThroughItsSyntheticId() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new ApplicationBlockBuilder().WithId("block").WithName("Block").WithDefaultScreen<TestScreen>("Dashboard").Build();
		var catalog = new ApplicationBlockCatalog(new[] { block });
		using var firstHost = new ApplicationScreenHost(catalog, provider);
		using var secondHost = new ApplicationScreenHost(catalog, provider);
		var session = await firstHost.ActivateBlockAsync("block");
		var reopened = await secondHost.ActivateScreenAsync("block", session.MenuItem.Id);

		Assert.That(reopened.ScreenType, Is.EqualTo(typeof(TestScreen)));
		Assert.That(reopened.Title, Is.EqualTo("Dashboard"));
		Assert.That(reopened.Id, Is.Not.EqualTo(session.Id));
	}

	public class TestScreen : ComponentBase, IApplicationScreen {
	}

	public abstract class AbstractScreen : ComponentBase, IApplicationScreen {
	}

	public class GenericScreen<T> : ComponentBase, IApplicationScreen {
	}
}

