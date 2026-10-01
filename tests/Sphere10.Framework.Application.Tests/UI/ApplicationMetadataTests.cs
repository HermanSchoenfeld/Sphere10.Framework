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
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using Block = Sphere10.Framework.Application.UI.ApplicationBlock;
using Menu = Sphere10.Framework.Application.UI.ApplicationMenu<Sphere10.Framework.Application.UI.ApplicationMenuItem>;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationMetadataTests {
	[Test]
	public void BlockStoresDifferentMenuImplementationsThroughTheSharedContract() {
		var block = new Block { Name = "Block" };
		var standardMenu = new Menu { Text = "Standard" };
		var customMenu = new CustomMenu("Custom");
		block.AddMenu(standardMenu);
		block.AddMenu(customMenu);

		Assert.That(block.Menus, Is.EqualTo(new IApplicationMenu[] { standardMenu, customMenu }));
		Assert.That(block.ContainsMenu(customMenu), Is.True);
		block.RemoveMenu(customMenu);
		Assert.That(block.ContainsMenu(customMenu), Is.False);
		Assert.That(block.Menus.Single(), Is.SameAs(standardMenu));
	}

	[Test]
	public void BlockReturnsIndependentMenuArrays() {
		var block = new Block { Name = "Block" };
		IApplicationBlock contract = block;
		var beforeAddition = contract.Menus;
		var menu = new CustomMenu("Menu");
		block.AddMenu(menu);
		var menus = contract.Menus;
		var replacement = new CustomMenu("Replacement");
		menus[0] = replacement;

		Assert.That(beforeAddition, Is.Empty);
		Assert.That(contract.Menus.Single(), Is.SameAs(menu));
		block.RemoveMenu(menu);
		Assert.That(contract.Menus, Is.Empty);
		Assert.That(menus.Single(), Is.SameAs(replacement));
	}

	[Test]
	public void MenuReturnsIndependentItemArraysThroughBothContracts() {
		var menu = new Menu { Text = "Menu" };
		var item = new ApplicationMenuItem { Title = "Original" };
		var beforeAddition = menu.Items;
		menu.AddItem(item);
		var typedItems = menu.Items;
		var sharedItems = ((IApplicationMenu)menu).Items;
		typedItems[0] = new ApplicationMenuItem { Title = "Typed replacement" };
		sharedItems[0] = new ApplicationMenuItem { Title = "Shared replacement" };

		Assert.That(beforeAddition, Is.Empty);
		Assert.That(menu.Items.Single(), Is.SameAs(item));
		menu.RemoveItem(item);
		Assert.That(menu.Items, Is.Empty);
		Assert.That(typedItems.Single().Title, Is.EqualTo("Typed replacement"));
		Assert.That(sharedItems.Single().Title, Is.EqualTo("Shared replacement"));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ReturnedCatalogArraysCannotChangeStoredOrderLookupOrNestedMembership(bool decorated) {
		var first = CreateBlockWithItem(new ApplicationMenuItem { Title = "Stored item" });
		first.Id = "first";
		first.Position = 1;
		var second = new Block { Id = "second", Name = "Second", Position = 2 };
		IApplicationBlockCatalog<Block> catalog = new ApplicationBlockCatalog<Block>(new[] { second, first }, new NeutralSnapshot().Create);
		if (decorated)
			catalog = new CatalogDecorator(catalog);
		var stored = catalog.Get("first");
		var storedMenu = stored.Menus.Single();
		var blocks = catalog.Blocks;
		var menus = blocks[0].Menus;
		var items = menus[0].Items;
		blocks[0] = new Block { Id = "replacement", Name = "Replacement" };
		blocks[1] = null;
		menus[0] = new Menu { Text = "Replacement menu" };
		items[0] = new ApplicationMenuItem { Title = "Replacement item" };

		Assert.That(catalog.Blocks.Select(block => block.Id), Is.EqualTo(new[] { "first", "second" }));
		Assert.That(catalog.Get("first"), Is.SameAs(stored));
		Assert.That(catalog.Get("first").Menus.Single(), Is.SameAs(storedMenu));
		Assert.That(storedMenu.Items.Single().Title, Is.EqualTo("Stored item"));
		Assert.That(() => catalog.Get("replacement"), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void CatalogSnapshotsCollectionsAndMetadataBeforeOrderingAndIndexing() {
		var item = new ApplicationMenuItem { Title = "Original item" };
		var menu = new Menu { Text = "Original menu" };
		menu.AddItem(item);
		var later = new Block { Name = "Later", Position = 20 };
		later.AddMenu(menu);
		var earlier = new Block { Name = "Earlier", Position = 10 };
		var registrations = new List<Block> { later, earlier };
		var snapshot = new NeutralSnapshot();
		var catalog = new ApplicationBlockCatalog<Block>(registrations, snapshot.Create);
		registrations.Clear();
		later.Name = "Renamed";
		later.RemoveMenu(menu);
		menu.Text = "Renamed menu";
		menu.RemoveItem(item);
		item.Title = "Renamed item";

		Assert.That(catalog.Blocks.Select(block => block.Name), Is.EqualTo(new[] { "Earlier", "Later" }));
		var storedMenu = catalog.Get("Later").Menus.Single();
		Assert.That(storedMenu.Text, Is.EqualTo("Original menu"));
		Assert.That(storedMenu.Items.Single().Title, Is.EqualTo("Original item"));
		Assert.That(() => catalog.Get("Renamed"), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase("block")]
	[TestCase("menu")]
	[TestCase("item")]
	public void DuplicateStableIdsAreRejected(string duplicateKind) {
		var firstItem = new ApplicationMenuItem { Id = "item", Title = "First" };
		var firstMenu = new Menu { Id = "menu", Text = "First menu" };
		firstMenu.AddItem(firstItem);
		var block = new Block { Id = "block", Name = "Block" };
		block.AddMenu(firstMenu);
		var secondMenu = new Menu { Id = duplicateKind == "menu" ? "menu" : "second-menu", Text = "Second menu" };
		secondMenu.AddItem(new ApplicationMenuItem { Id = duplicateKind == "item" ? "item" : "second-item", Title = "Second" });
		block.AddMenu(secondMenu);
		var blocks = duplicateKind == "block" ? new[] { block, block } : new[] { block };

		Assert.That(() => new ApplicationBlockCatalog<Block>(blocks, new NeutralSnapshot().Create), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void DefaultScreenIdIsReservedForSyntheticEntries() {
		var menu = new Menu { Text = "Menu" };
		menu.AddItem(new ApplicationMenuItem { Id = NeutralSnapshot.DefaultScreenItemId, Title = "Reserved" });
		var block = new Block { Name = "Block" };
		block.AddMenu(menu);

		Assert.That(() => new NeutralSnapshot().Create(block), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void CatalogRejectsConflictingPoliciesAcrossDifferentBlocks() {
		var first = CreateScreenBlock("First", ScreenActivationMode.SingleInstance);
		var second = CreateScreenBlock("Second", ScreenActivationMode.MultiInstance);

		Assert.That(() => new ApplicationBlockCatalog<Block>(new[] { first, second }, new NeutralSnapshot().Create), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void UnspecifiedActivationPolicyDoesNotConflictWithAnExplicitDeclaration() {
		var first = CreateScreenBlock("First", null);
		var second = CreateScreenBlock("Second", ScreenActivationMode.MultiInstance);
		var catalog = new ApplicationBlockCatalog<Block>(new[] { first, second }, new NeutralSnapshot().Create);

		Assert.That(catalog.Blocks, Has.Length.EqualTo(2));
		Assert.That(((IScreenMenuItem)catalog.Get("First").Menus.Single().Items.Single()).ActivationMode, Is.Null);
	}

	[Test]
	public void DefaultScreenSelectionSkipsActionsAndFindsTheConfiguredScreenType() {
		var firstScreen = new ScreenItem { Title = "First", ScreenType = typeof(PlainScreen) };
		var secondScreen = new ScreenItem { Title = "Second", ScreenType = typeof(OtherScreen) };
		var menu = new Menu { Text = "Menu" };
		menu.AddItem(new ApplicationMenuItem { Title = "Action" });
		menu.AddItem(firstScreen);
		menu.AddItem(secondScreen);
		var block = new Block { Name = "Block" };
		block.AddMenu(menu);
		var snapshot = new NeutralSnapshot();

		Assert.That(snapshot.GetDefaultScreen(block), Is.SameAs(firstScreen));
		block.DefaultScreen = typeof(OtherScreen);
		Assert.That(snapshot.GetDefaultScreen(block).Id, Is.EqualTo(secondScreen.Id));
	}

	[TestCase(true)]
	[TestCase(false)]
	public void SubscriptionCopiesRetainTheirOwnInvocationLists(bool hover) {
		var originalCalls = 0;
		var lateCalls = 0;
		EventHandlerEx original = () => originalCalls++;
		var source = new ApplicationMenuItem { Title = "Item" };
		if (hover)
			source.Hover += original;
		else
			source.Select += original;
		var first = new ApplicationMenuItem();
		first.CopySubscriptionsFrom(source);
		var second = new ApplicationMenuItem();
		second.CopySubscriptionsFrom(first);
		if (hover) {
			source.Hover -= original;
			source.Hover += () => lateCalls++;
			second.NotifyHover();
		} else {
			source.Select -= original;
			source.Select += () => lateCalls++;
			second.NotifySelect();
		}

		Assert.That(originalCalls, Is.EqualTo(1));
		Assert.That(lateCalls, Is.Zero);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void CatalogValidatesScreenTypesWithoutAnExplicitPolicy(bool defaultResolver) {
		var screen = new ScreenItem { Title = "Invalid", ScreenType = typeof(string) };
		var block = CreateBlockWithItem(screen);
		Func<Block, IScreenMenuItem> resolve = defaultResolver ? _ => screen : null;
		if (defaultResolver)
			block.RemoveMenu(block.Menus.Single());

		Assert.That(() => new ApplicationBlockCatalog<Block>(new[] { block }, value => value, resolve), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase(false)]
	[TestCase(true)]
	public void CatalogAppliesPlatformValidationWithoutAnExplicitPolicy(bool defaultResolver) {
		var screen = new ScreenItem { Title = "Screen", ScreenType = typeof(PlainScreen) };
		var block = CreateBlockWithItem(screen);
		Func<Block, IScreenMenuItem> resolve = defaultResolver ? _ => screen : null;
		if (defaultResolver)
			block.RemoveMenu(block.Menus.Single());

		Assert.That(() => new ApplicationBlockCatalog<Block>(new[] { block }, value => value, resolve,
			type => Tools.UI.ValidateScreenType(type, typeof(OtherScreen))), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void CatalogValidatesConfiguredDefaultScreenWithoutAResolver() {
		var block = new Block { Name = "Block", DefaultScreen = typeof(string) };

		Assert.That(() => new ApplicationBlockCatalog<Block>(new[] { block }, value => value), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void DirectDefaultResolutionRejectsMissingMenus() {
		Assert.That(() => new NeutralSnapshot().GetDefaultScreen(new NullMenuBlock()), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void DirectDefaultResolutionValidatesConfiguredScreenType() {
		var block = new Block { Name = "Block", DefaultScreen = typeof(string) };

		Assert.That(() => new NeutralSnapshot().GetDefaultScreen(block), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase(false)]
	[TestCase(true)]
	public void DirectDefaultResolutionRejectsInvalidResolvedScreen(bool invalidPolicy) {
		var screen = new ScreenItem {
			Title = "Invalid",
			ScreenType = invalidPolicy ? typeof(PlainScreen) : typeof(string),
			ActivationMode = invalidPolicy ? (ScreenActivationMode)100 : null
		};
		var block = CreateBlockWithItem(screen);

		Assert.That(() => new NeutralSnapshot().GetDefaultScreen(block), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void DirectDefaultResolutionRejectsNonScreenFactoryResult() {
		var block = new Block { Name = "Block", DefaultScreen = typeof(PlainScreen) };

		Assert.That(() => new NonScreenSnapshot().GetDefaultScreen(block), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void DirectDefaultResolutionAppliesPlatformValidation() {
		var block = CreateBlockWithItem(new ScreenItem { Title = "Screen", ScreenType = typeof(PlainScreen) });

		Assert.That(() => new OtherScreenSnapshot().GetDefaultScreen(block), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void ActionOnlyBlockHasNoDefaultScreen() {
		var block = CreateBlockWithItem(new ApplicationMenuItem { Title = "Action" });

		Assert.That(new NeutralSnapshot().GetDefaultScreen(block), Is.Null);
	}

	[Test]
	public void DefaultResolutionPreservesTheSnapshottedCallbackOnce() {
		var calls = 0;
		EventHandlerEx handler = () => calls++;
		var item = new ScreenItem { Title = "Screen", ScreenType = typeof(PlainScreen) };
		item.Select += handler;
		var snapshot = new NeutralSnapshot();
		var block = snapshot.Create(CreateBlockWithItem(item));
		item.Select -= handler;
		var selected = snapshot.GetDefaultScreen(block);
		selected.NotifySelect();

		Assert.That(selected, Is.SameAs(block.Menus.Single().Items.Single()));
		Assert.That(calls, Is.EqualTo(1));
	}

	private static Block CreateBlockWithItem(ApplicationMenuItem item) {
		var menu = new Menu { Text = "Menu" };
		menu.AddItem(item);
		var block = new Block { Name = "Block" };
		block.AddMenu(menu);
		return block;
	}
	private static Block CreateScreenBlock(string name, ScreenActivationMode? mode) {
		var menu = new Menu { Text = "Menu" };
		menu.AddItem(new ScreenItem { Title = "Screen", ScreenType = typeof(PlainScreen), ActivationMode = mode });
		var block = new Block { Name = name };
		block.AddMenu(menu);
		return block;
	}

	private class ScreenItem : ApplicationMenuItem, IScreenMenuItem {
		public Type ScreenType { get; set; }

		public ScreenActivationMode? ActivationMode { get; set; }
	}

	private sealed class CatalogDecorator : ApplicationBlockCatalogDecorator<Block> {
		public CatalogDecorator(IApplicationBlockCatalog<Block> catalog)
			: base(catalog) {
		}
	}

	private sealed record CustomMenu(string Text) : IApplicationMenu {
		public IApplicationMenuItem[] Items => Array.Empty<IApplicationMenuItem>();
	}
	private class PlainScreen : IApplicationScreen {
	}

	private class OtherScreen : IApplicationScreen {
	}

	private sealed class NullMenuBlock : Block {
		public override IApplicationMenu[] Menus => null;
	}

	private sealed class NonScreenSnapshot : NeutralSnapshot {
		protected override ApplicationMenuItem CreateDefaultScreen(Block block, ApplicationMenuItem matchingScreen) => new() { Title = "Action" };
	}

	private sealed class OtherScreenSnapshot : NeutralSnapshot {
		protected override void ValidateScreenType(Type screenType) => Tools.UI.ValidateScreenType(screenType, typeof(OtherScreen));
	}
	private class NeutralSnapshot : ApplicationBlockSnapshotBase<Block, Menu, ApplicationMenuItem> {
		protected override Block CreateBlockSnapshot(Block block, string id, Menu[] menus) {
			var snapshot = new Block { Id = id, Name = block.Name, Position = block.Position, DefaultScreen = block.DefaultScreen, DefaultScreenTitle = block.DefaultScreenTitle };
			foreach (var menu in menus)
				snapshot.AddMenu(menu);
			return snapshot;
		}

		protected override Menu CreateMenuSnapshot(Menu menu, string id, ApplicationMenuItem[] items) {
			var snapshot = new Menu { Id = id, Text = menu.Text };
			foreach (var item in items)
				snapshot.AddItem(item);
			return snapshot;
		}

		protected override ApplicationMenuItem CreateItemSnapshot(ApplicationMenuItem item, string id) {
			ApplicationMenuItem snapshot = item is ScreenItem screen
				? new ScreenItem { ScreenType = screen.ScreenType, ActivationMode = screen.ActivationMode }
				: new ApplicationMenuItem();
			snapshot.Id = id;
			snapshot.Title = item.Title;
			snapshot.CopySubscriptionsFrom(item);
			return snapshot;
		}

		protected override ApplicationMenuItem CreateDefaultScreen(Block block, ApplicationMenuItem matchingScreen) => matchingScreen
			?? new ScreenItem { Id = DefaultScreenItemId, Title = block.DefaultScreenTitle ?? block.Name, ScreenType = block.DefaultScreen };
	}
}
