// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Application;

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
	public void HostCopiesMutableRegistrationCollectionsAndParameters() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(provider);
		var parameters = new Dictionary<string, object> { ["Message"] = "Original" };
		var items = new IBlazorApplicationMenuItem[] {
			new BlazorScreenMenuItem { Id = "screen", Title = "Screen", ScreenType = typeof(TestScreen), Parameters = parameters }
		};
		var menus = new IBlazorApplicationMenu[] { new BlazorApplicationMenu { Id = "menu", Text = "Menu", Items = items } };
		var blocks = new List<IBlazorApplicationBlock> { new BlazorApplicationBlock { Id = "block", Title = "Block", Menus = menus } };
		using var metadataHost = new BlazorApplicationScreenHost(blocks, provider);
		parameters["Message"] = "Changed";
		items[0] = null;
		menus[0] = null;
		blocks.Clear();
		metadataHost.AvailableBlocks[0] = null;
		metadataHost.AvailableBlocks.Single().Menus[0].Items[0] = null;

		var block = metadataHost.AvailableBlocks.Single();
		var screen = (BlazorScreenMenuItem)block.Menus.Single().Items.Single();
		Assert.That(metadataHost.AvailableBlocks, Has.Length.EqualTo(1));
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
		var builder = new BlazorApplicationBlockBuilder()
			.WithId("first")
			.WithName("First")
			.ConfigureMenu(menu => menu
				.WithText("Screens")
				.AddScreenItem<TestScreen>("first-screen", "First screen")
			);
		var first = builder.Build();
		var second = builder
			.WithId("second")
			.WithName("Second")
			.ConfigureMenu(menu => menu
				.WithText("Actions")
				.AddActionItem("refresh", "Refresh", () => { })
			)
			.Build();

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
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(provider);
		var first = new BlazorApplicationBlockBuilder()
			.WithId("block")
			.WithName("Block")
			.ConfigureMenu(menu => menu
				.WithId("menu")
				.WithText("First")
				.AddScreenItem<TestScreen>("item", "First")
			);
		Action build = duplicateKind switch {
			"block" => () => new BlazorApplicationScreenHost(new[] { first.Build(), first.Build() }, provider),
			"menu" => () => first
				.ConfigureMenu(menu => menu
					.WithId("menu")
					.WithText("Second")
					.AddScreenItem<TestScreen>("other-item", "Second")
				)
				.Build(),
			_ => () => first
				.ConfigureMenu(menu => menu
					.WithId("other-menu")
					.WithText("Second")
					.AddScreenItem<TestScreen>("item", "Second")
				)
				.Build()
		};
		Assert.That(build, Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void ReservedDefaultIdIsRejected() {
		Assert.That(() => new BlazorApplicationBlockBuilder()
			.WithName("Block")
			.ConfigureMenu(menu => menu
				.WithText("Menu")
				.AddScreenItem<TestScreen>("__default", "Screen")
			)
			.Build(), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void EmptyMenuIdIsRejected() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(provider);
		var menu = new BlazorApplicationMenu { Id = "", Text = "Menu" };
		Assert.That(() => new BlazorApplicationScreenHost(new[] { new BlazorApplicationBlock { Id = "block", Title = "Block", Menus = new[] { menu } } }, provider),
			Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void HostOrdersAvailableBlocksByPosition() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(provider);
		var second = new BlazorApplicationBlockBuilder()
			.WithId("second")
			.WithName("Second")
			.WithPosition(20)
			.Build();
		var first = new BlazorApplicationBlockBuilder()
			.WithId("first")
			.WithName("First")
			.WithPosition(10)
			.Build();
		using var metadataHost = new BlazorApplicationScreenHost(new[] { second, first }, provider);
		Assert.That(metadataHost.AvailableBlocks.Select(block => block.Id), Is.EqualTo(new[] { "first", "second" }));
	}

	[Test]
	public void ConflictingActivationPoliciesAcrossBlocksAreRejected() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(provider);
		var single = new BlazorApplicationBlockBuilder()
			.WithId("single")
			.WithName("Single")
			.WithDefaultScreen<TestScreen>(activationMode: ScreenActivationMode.SingleInstance)
			.Build();
		var multiple = new BlazorApplicationBlockBuilder()
			.WithId("multiple")
			.WithName("Multiple")
			.ConfigureMenu(menu => menu
				.WithText("Menu")
				.AddScreenItem<TestScreen>("screen", "Screen", ScreenActivationMode.MultiInstance)
			)
			.Build();
		Assert.That(() => new BlazorApplicationScreenHost(new[] { single, multiple }, provider), Throws.InstanceOf<ArgumentException>());
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
		var block = new BlazorApplicationBlockBuilder()
			.WithId("block")
			.WithName("Block")
			.WithDefaultScreen<TestScreen>("Dashboard")
			.ConfigureMenu(menu => menu
				.WithText("Menu")
				.AddScreenItem<TestScreen>("screen", "Open dashboard", ScreenActivationMode.SingleInstance)
			)
			.Build();
		using var host = new BlazorApplicationScreenHost(new[] { block }, provider);
		var session = await host.ActivateBlockAsync("block");

		Assert.That(session.Title, Is.EqualTo("Dashboard"));
		Assert.That(session.MenuItemId, Is.EqualTo("screen"));
		Assert.That(await host.ActivateScreenAsync("block", session.MenuItemId), Is.SameAs(session));
	}

	[TestCase("model")]
	[TestCase("factory")]
	[TestCase("generic-builder")]
	[TestCase("type-builder")]
	[TestCase("item-builder")]
	[TestCase("default-only")]
	public async Task OmittedLifetimeCreatesIndependentSessionsThroughEveryRegistrationPath(string registration) {
		var builder = new BlazorApplicationBlockBuilder()
			.WithId("block")
			.WithName("Block");
		if (registration == "default-only")
			builder.WithDefaultScreen<TestScreen>();
		else
			builder
				.ConfigureMenu(menu => {
					menu.WithText("Screens");
					switch (registration) {
						case "model":
							menu.AddItem(new BlazorScreenMenuItem { Id = "screen", Title = "Screen", ScreenType = typeof(TestScreen) });
							break;
						case "factory":
							menu.AddItem(BlazorScreenMenuItem.For<TestScreen>("screen", "Screen"));
							break;
						case "generic-builder":
							menu.AddScreenItem<TestScreen>("screen", "Screen");
							break;
						case "type-builder":
							menu.AddScreenItem("screen", "Screen", typeof(TestScreen));
							break;
						case "item-builder":
							menu
								.ConfigureItem(item => item
									.WithId("screen")
									.WithText("Screen")
									.WithScreen<TestScreen>()
								);
							break;
					}
				});
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new[] { builder.Build() }, provider);
		var first = await host.ActivateBlockAsync("block");
		var second = await host.ActivateBlockAsync("block");

		Assert.That(first.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		Assert.That(second.Id, Is.Not.EqualTo(first.Id));
		Assert.That(host.OpenScreens, Has.Length.EqualTo(2));
	}

	[Test]
	public async Task DefaultOnlyScreenRoundTripsThroughItsSyntheticId() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new BlazorApplicationBlockBuilder()
			.WithId("block")
			.WithName("Block")
			.WithDefaultScreen<TestScreen>("Dashboard")
			.Build();
		var blocks = new[] { block };
		using var firstHost = new BlazorApplicationScreenHost(blocks, provider);
		using var secondHost = new BlazorApplicationScreenHost(blocks, provider);
		var session = await firstHost.ActivateBlockAsync("block");
		var reopened = await secondHost.ActivateScreenAsync("block", session.MenuItemId);

		Assert.That(reopened.ScreenType, Is.EqualTo(typeof(TestScreen)));
		Assert.That(reopened.Title, Is.EqualTo("Dashboard"));
		Assert.That(reopened.Id, Is.Not.EqualTo(session.Id));
	}

	public class TestScreen : ComponentBase, IBlazorApplicationScreen {
		public IBlazorApplicationScreenHost Host { get; set; }

		public IBlazorApplicationBlock Block { get; set; }

	}

	public abstract class AbstractScreen : ComponentBase, IBlazorApplicationScreen {
		public IBlazorApplicationScreenHost Host { get; set; }

		public IBlazorApplicationBlock Block { get; set; }

	}

	public class GenericScreen<T> : ComponentBase, IBlazorApplicationScreen {
		public IBlazorApplicationScreenHost Host { get; set; }

		public IBlazorApplicationBlock Block { get; set; }

	}
}

