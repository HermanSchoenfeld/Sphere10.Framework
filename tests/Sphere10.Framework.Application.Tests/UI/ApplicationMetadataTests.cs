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
using Block = Sphere10.Framework.Application.Tests.TestApplicationBlock;
using Menu = Sphere10.Framework.Application.Tests.TestApplicationMenu<Sphere10.Framework.Application.Tests.TestApplicationMenuItem>;

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
		var item = new TestApplicationMenuItem { Title = "Original" };
		var beforeAddition = menu.Items;
		menu.AddItem(item);
		var typedItems = menu.Items;
		var sharedItems = ((IApplicationMenu)menu).Items;
		typedItems[0] = new TestApplicationMenuItem { Title = "Typed replacement" };
		sharedItems[0] = new TestApplicationMenuItem { Title = "Shared replacement" };
		Assert.That(beforeAddition, Is.Empty);
		Assert.That(menu.Items.Single(), Is.SameAs(item));
		menu.RemoveItem(item);
		Assert.That(menu.Items, Is.Empty);
		Assert.That(typedItems.Single().Title, Is.EqualTo("Typed replacement"));
		Assert.That(sharedItems.Single().Title, Is.EqualTo("Shared replacement"));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ReturnedHostArraysCannotChangeStoredOrderOrNestedMembership(bool decorated) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var first = CreateBlockWithItem(new TestApplicationMenuItem { Title = "Stored item" });
		first.Name = "First";
		first.Position = 1;
		var second = new Block { Name = "Second", Position = 2 };
		using var owner = new ApplicationScreenHost<Block, IApplicationScreen>(new[] { second, first }.Select(new NeutralSnapshot().Create).ToArray(), provider);
		IApplicationScreenHost host = decorated ? new HostDecorator(owner) : owner;
		var stored = host.AvailableBlocks[0];
		await host.SelectBlockAsync(stored);
		var storedMenu = stored.Menus.Single();
		var blocks = host.Blocks;
		var menus = blocks[0].Menus;
		var items = menus[0].Items;
		blocks[0] = new Block { Name = "Replacement" };
		blocks[1] = null;
		menus[0] = new Menu { Text = "Replacement menu" };
		items[0] = new TestApplicationMenuItem { Title = "Replacement item" };
		Assert.That(host.Blocks.Select(block => block.Name), Is.EqualTo(new[] { "First", "Second" }));
		await host.SelectBlockAsync(stored);
		Assert.That(host.ActiveBlock, Is.SameAs(stored));
		Assert.That(host.ActiveBlock.Menus.Single(), Is.SameAs(storedMenu));
		Assert.That(storedMenu.Items.Single().Title, Is.EqualTo("Stored item"));
		Assert.That(async () => await host.SelectBlockAsync(new Block { Name = "First" }), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public async Task HostRetainsRegistrationSnapshotsBeforeOrdering() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var item = new TestApplicationMenuItem { Title = "Original item" };
		var menu = new Menu { Text = "Original menu" };
		menu.AddItem(item);
		var later = new Block { Name = "Later", Position = 20 };
		later.AddMenu(menu);
		var earlier = new Block { Name = "Earlier", Position = 10 };
		var registrations = new List<Block> { later, earlier };
		using var host = new ApplicationScreenHost<Block, IApplicationScreen>(registrations.Select(new NeutralSnapshot().Create).ToArray(), provider);
		registrations.Clear();
		later.Name = "Renamed";
		later.RemoveMenu(menu);
		menu.Text = "Renamed menu";
		menu.RemoveItem(item);
		item.Title = "Renamed item";
		Assert.That(host.Blocks.Select(block => block.Name), Is.EqualTo(new[] { "Earlier", "Later" }));
		await host.SelectBlockAsync(host.AvailableBlocks[1]);
		Assert.That(host.ActiveBlock.Menus.Single().Text, Is.EqualTo("Original menu"));
		Assert.That(host.ActiveBlock.Menus.Single().Items.Single().Title, Is.EqualTo("Original item"));
		Assert.That(async () => await host.SelectBlockAsync(later), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase("menu")]
	[TestCase("item")]
	public void EqualDisplayLabelsRetainDistinctMenuAndItemReferences(string duplicateKind) {
		var firstItem = new TestApplicationMenuItem { Title = "Shared title" };
		var firstMenu = new Menu { Text = "Shared menu" };
		firstMenu.AddItem(firstItem);
		var secondMenu = new Menu { Text = duplicateKind == "menu" ? "Shared menu" : "Other menu" };
		secondMenu.AddItem(new TestApplicationMenuItem { Title = duplicateKind == "item" ? "Shared title" : "Other item" });
		var block = new Block { Name = "Block" };
		block.AddMenu(firstMenu);
		block.AddMenu(secondMenu);
		var snapshot = new NeutralSnapshot().Create(block);
		Assert.That(snapshot.Menus, Has.Length.EqualTo(2));
		Assert.That(snapshot.Menus[0], Is.Not.SameAs(snapshot.Menus[1]));
		Assert.That(snapshot.Menus[0].Items[0], Is.Not.SameAs(snapshot.Menus[1].Items[0]));
	}

	[Test]
	public async Task HostDistinguishesDifferentBlocksByReferenceWhenTheirNamesMatch() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var first = new Block { Name = "Same" };
		var second = new Block { Name = "Same" };
		using var host = new ApplicationScreenHost(new[] { first, second }, provider);
		await host.SelectBlockAsync(first);
		Assert.That(host.ActiveBlock, Is.SameAs(first));
		await host.SelectBlockAsync(second);
		Assert.That(host.ActiveBlock, Is.SameAs(second));
		Assert.That(host.Blocks, Has.Length.EqualTo(2));
		Assert.That(await host.UnregisterBlockAsync(first), Is.True);
		Assert.That(host.Blocks.Single(), Is.SameAs(second));
	}

	[Test]
	public void ScreenOwnershipSurvivesRemovingItsNavigationMenu() {
		var definition = new ApplicationScreenDefinition(typeof(PlainScreen), "Screen");
		var block = CreateBlockWithItem(new ScreenItem { Title = "Screen", ScreenDefinition = definition });
		block.RemoveMenu(block.Menus.Single());
		Assert.That(block.Menus, Is.Empty);
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(definition));
	}

	[Test]
	public void DefinitionArraysAreDefensiveAndRegisteringTheSameReferenceIsIdempotent() {
		var first = new ApplicationScreenDefinition(typeof(PlainScreen), "Same");
		var alias = new ApplicationScreenDefinition(typeof(PlainScreen), "Same");
		var block = new Block { Name = "Block" };
		block.AddScreenDefinition(first);
		block.AddScreenDefinition(first);
		block.AddScreenDefinition(alias);
		var snapshot = block.ScreenDefinitions;
		snapshot[0] = null;
		Assert.That(block.ScreenDefinitions, Has.Length.EqualTo(2));
		Assert.That(block.ScreenDefinitions[0], Is.SameAs(first));
		Assert.That(block.ScreenDefinitions[1], Is.SameAs(alias));
		Assert.That(block.DefaultScreenDefinition, Is.Null);
		block.DefaultScreenDefinition = alias;
		Assert.That(block.ScreenDefinitions, Has.Length.EqualTo(2));
		Assert.That(block.DefaultScreenDefinition, Is.SameAs(alias));
	}

	[Test]
	public void HostRejectsConflictingPoliciesAcrossDifferentBlocks() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var first = CreateScreenBlock("First", ScreenActivationMode.SingleInstance);
		var second = CreateScreenBlock("Second", ScreenActivationMode.MultiInstance);
		Assert.That(() => new ApplicationScreenHost<Block, IApplicationScreen>(new[] { first, second }, provider), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void UnspecifiedActivationPolicyDoesNotConflictWithAnExplicitDeclaration() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var first = CreateScreenBlock("First", null);
		var second = CreateScreenBlock("Second", ScreenActivationMode.MultiInstance);
		using var host = new ApplicationScreenHost<Block, IApplicationScreen>(new[] { first, second }, provider);
		Assert.That(host.Blocks, Has.Length.EqualTo(2));
		Assert.That(first.ScreenDefinitions.Single().ActivationMode, Is.Null);
	}

	[Test]
	public void DefaultSelectionUsesTheExactRegisteredDefinitionInsteadOfItsTypeOrCaption() {
		var first = new ApplicationScreenDefinition(typeof(PlainScreen), "Same", ScreenActivationMode.SingleInstance);
		var second = new ApplicationScreenDefinition(typeof(PlainScreen), "Same", ScreenActivationMode.SingleInstance);
		var firstItem = new ScreenItem { Title = "First", ScreenDefinition = first };
		var secondItem = new ScreenItem { Title = "Second", ScreenDefinition = second };
		var block = CreateBlockWithItem(firstItem);
		var menu = new Menu { Text = "Other" };
		menu.AddItem(secondItem);
		block.AddMenu(menu);
		block.DefaultScreenDefinition = second;
		Assert.That(Tools.UI.GetDefaultScreen(new[] { block }), Is.SameAs(second));
	}

	[TestCase(true)]
	[TestCase(false)]
	public void SubscriptionCopiesRetainTheirOwnInvocationLists(bool hover) {
		var originalCalls = 0;
		var lateCalls = 0;
		EventHandlerEx original = () => originalCalls++;
		var source = new TestApplicationMenuItem { Title = "Item" };
		if (hover)
			source.Hover += original;
		else
			source.Select += original;
		var first = new TestApplicationMenuItem();
		first.CopySubscriptionsFrom(source);
		var second = new TestApplicationMenuItem();
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
	public void HostRejectsMalformedDefinitionOwnershipBeforeInitialization(bool malformedDefault) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var definition = new ApplicationScreenDefinition(typeof(PlainScreen));
		var block = new MalformedBlock(malformedDefault ? Array.Empty<ApplicationScreenDefinition>() : new ApplicationScreenDefinition[] { null },
			malformedDefault ? definition : null);
		Assert.That(() => new ApplicationScreenHost<Block, IApplicationScreen>(new[] { block }, provider), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase(false)]
	[TestCase(true)]
	public void TypedHostValidatesOwnedScreensWhetherOrNotTheyAreDefaults(bool defaultScreen) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var definition = new ApplicationScreenDefinition(typeof(PlainScreen));
		var block = new Block { Name = "Block" };
		block.AddScreenDefinition(definition);
		if (defaultScreen)
			block.DefaultScreenDefinition = definition;
		Assert.That(() => new ApplicationScreenHost<Block, OtherScreen>(new[] { block }, provider), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void InvalidScreenTypeIsRejectedWhenTheDefinitionIsConstructed() =>
		Assert.That(() => new ApplicationScreenDefinition(typeof(string)), Throws.InstanceOf<ArgumentException>());

	[Test]
	public void DirectDefaultResolutionRejectsMissingMenus() =>
		Assert.That(() => new NeutralSnapshot().Create(new NullMenuBlock()), Throws.InstanceOf<ArgumentException>());

	[Test]
	public void DirectDefaultResolutionRejectsAnUnownedDefault() =>
		Assert.That(() => new NeutralSnapshot().Create(new MalformedBlock(Array.Empty<ApplicationScreenDefinition>(), new(typeof(PlainScreen)))), Throws.InstanceOf<ArgumentException>());

	[TestCase(false)]
	[TestCase(true)]
	public void DirectDefaultResolutionRejectsMalformedOwnedDefinitions(bool nullArray) =>
		Assert.That(() => new NeutralSnapshot().Create(new MalformedBlock(nullArray ? null : new ApplicationScreenDefinition[] { null }, null)), Throws.InstanceOf<ArgumentException>());

	[Test]
	public void SnapshotRetainsAMenuIndependentDefaultDefinitionWithoutCreatingACommand() {
		var definition = new ApplicationScreenDefinition(typeof(PlainScreen), "Standalone");
		var block = new Block("Block", definition);
		var snapshot = new NeutralSnapshot().Create(block);
		Assert.That(snapshot.DefaultScreenDefinition, Is.Not.SameAs(definition));
		Assert.That(snapshot.DefaultScreenDefinition, Is.SameAs(snapshot.ScreenDefinitions.Single()));
		Assert.That(snapshot.DefaultScreenDefinition.ScreenType, Is.EqualTo(definition.ScreenType));
		Assert.That(snapshot.Menus, Is.Empty);
	}

	[Test]
	public void SnapshotCannotIntroduceAnotherOwnedScreenDefinition() {
		var definition = new ApplicationScreenDefinition(typeof(PlainScreen), "Original");
		var additional = new ApplicationScreenDefinition(typeof(PlainScreen), "Unrequested alias");
		var block = new Block("Block", definition);
		Assert.That(() => new AdditionalDefinitionSnapshot(additional).Create(block), Throws.ArgumentException);
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(definition));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SnapshotCannotDropOrRetargetTheSelectedDefault(bool dropDefault) {
		var original = new ApplicationScreenDefinition(typeof(PlainScreen), "Original");
		var alternative = new ApplicationScreenDefinition(typeof(PlainScreen), "Alternative");
		var block = new Block("Block", original);
		block.AddScreenDefinition(alternative);
		Assert.That(() => new DefaultDefinitionSnapshot(dropDefault ? null : alternative).Create(block), Throws.ArgumentException);
		Assert.That(block.DefaultScreenDefinition, Is.SameAs(original));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SnapshotItemCannotLoseOrReplaceItsCanonicalScreenDefinition(bool loseScreenBinding) {
		var definition = new ApplicationScreenDefinition(typeof(PlainScreen), "Screen");
		var item = new ScreenItem { Title = "Screen", ScreenDefinition = definition };
		Assert.That(() => new AlteredScreenItemSnapshot(loseScreenBinding).CreateItem(item), Throws.ArgumentException);
		Assert.That(item.ScreenDefinition, Is.SameAs(definition));
	}

	[Test]
	public void HostRejectsNullToolbarCommandsBeforeInitialization() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		Assert.That(() => new ApplicationScreenHost(new[] { new NullToolbarItemBlock() }, provider), Throws.ArgumentException);
	}

	[TestCase("default")]
	[TestCase("menu")]
	[TestCase("toolbar")]
	public void DefinitionRemovalRequiresItsNavigationReferencesToBeReleased(string referenceKind) {
		var definition = new ApplicationScreenDefinition(typeof(PlainScreen), "Screen");
		var item = new ScreenItem { Title = "Screen", ScreenDefinition = definition };
		var menu = new Menu { Text = "Views" };
		menu.AddItem(item);
		Block block = referenceKind == "toolbar" ? new ToolbarBlock(new[] { item }) : new Block();
		block.Name = "Block";
		block.AddScreenDefinition(definition);
		if (referenceKind == "default")
			block.DefaultScreenDefinition = definition;
		else if (referenceKind == "menu")
			block.AddMenu(menu);

		Assert.That(() => block.RemoveScreenDefinition(definition), Throws.ArgumentException);
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(definition));
		if (referenceKind == "default")
			block.DefaultScreenDefinition = null;
		else if (referenceKind == "menu")
			block.RemoveMenu(menu);
		else
			((ToolbarBlock)block).Commands = Array.Empty<IApplicationMenuItem>();
		block.RemoveScreenDefinition(definition);
		Assert.That(block.ScreenDefinitions, Is.Empty);
	}

	[Test]
	public void DefinitionRemovalUsesReferenceIdentityAndIgnoresAbsentEqualValues() {
		var first = new EqualScreenDefinition();
		var second = new EqualScreenDefinition();
		var block = new Block { Name = "Block" };
		block.AddScreenDefinition(first);
		block.AddScreenDefinition(second);
		Assert.That(block.ScreenDefinitions, Has.Length.EqualTo(2));
		block.RemoveScreenDefinition(second);
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(first));
		block.RemoveScreenDefinition(new EqualScreenDefinition());
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(first));
	}

	[Test]
	public void DefinitionRemovalRejectsNullWithoutChangingMembership() {
		var definition = new ApplicationScreenDefinition(typeof(PlainScreen));
		var block = new Block { Name = "Block" };
		block.AddScreenDefinition(definition);
		Assert.That(() => block.RemoveScreenDefinition(null), Throws.ArgumentNullException);
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(definition));
	}

	[Test]
	public void DirectDefaultResolutionAppliesPlatformValidation() {
		var block = CreateBlockWithItem(new ScreenItem { Title = "Screen", ScreenDefinition = new(typeof(PlainScreen)) });
		Assert.That(() => new OtherScreenSnapshot().Create(block), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void ActionOnlyBlockHasNoDefaultScreen() {
		var block = CreateBlockWithItem(new TestApplicationMenuItem { Title = "Action" });
		Assert.That(Tools.UI.GetDefaultScreen(new[] { new NeutralSnapshot().Create(block) }), Is.Null);
	}

	[Test]
	public void SnapshotClonesDefinitionGraphsAndPreservesCanonicalReferencesAndCopiedCallbacks() {
		var calls = 0;
		EventHandlerEx handler = () => calls++;
		var definition = new ApplicationScreenDefinition(typeof(PlainScreen), "Screen");
		var item = new ScreenItem { Title = "Screen", ScreenDefinition = definition };
		item.Select += handler;
		var snapshot = new NeutralSnapshot();
		var source = CreateBlockWithItem(item);
		source.DefaultScreenDefinition = definition;
		var plugin = new ApplicationPlugin("Startup", new[] { source });
		_ = new ApplicationPluginRegistration<ApplicationPlugin>(plugin, plugin.Blocks);
		source.Source = plugin;
		var block = snapshot.Create(source);
		item.Select -= handler;
		var selected = (IScreenMenuItem)block.Menus.Single().Items.Single();
		((TestApplicationMenuItem)selected).NotifySelect();
		Assert.That(selected, Is.SameAs(block.Menus.Single().Items.Single()));
		Assert.That(selected.ScreenDefinition, Is.Not.SameAs(definition));
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(selected.ScreenDefinition));
		Assert.That(block.DefaultScreenDefinition, Is.SameAs(selected.ScreenDefinition));
		Assert.That(selected.Parent, Is.SameAs(block.Menus.Single()));
		Assert.That(selected.Parent.Parent, Is.SameAs(block));
		Assert.That(selected.ScreenDefinition.Activators.Single(), Is.SameAs(selected));
		Assert.That(block.Source, Is.SameAs(plugin));
		Assert.That(block.Parent, Is.Null, "A snapshot keeps startup provenance without copying a running application parent.");
		Assert.That(calls, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SnapshotGraphsKeepIndependentParentsAndCanonicalReverseMembership(bool copySnapshot) {
		var definition = new ApplicationScreenDefinition(typeof(PlainScreen), "Shared target");
		var firstItem = new ScreenItem { Title = "First", ScreenDefinition = definition };
		var secondItem = new ScreenItem { Title = "Second", ScreenDefinition = definition };
		var source = CreateBlockWithItem(firstItem);
		var secondMenu = new Menu { Text = "Second" };
		secondMenu.AddItem(secondItem);
		source.AddMenu(secondMenu);
		source.DefaultScreenDefinition = definition;
		var snapshots = new NeutralSnapshot();
		var first = snapshots.Create(source);
		var second = snapshots.Create(copySnapshot ? first : source);
		var firstCommands = first.Menus.SelectMany(menu => menu.Items).Cast<IScreenMenuItem>().ToArray();
		var secondCommands = second.Menus.SelectMany(menu => menu.Items).Cast<IScreenMenuItem>().ToArray();
		Assert.That(first.DefaultScreenDefinition, Is.Not.SameAs(definition));
		Assert.That(second.DefaultScreenDefinition, Is.Not.SameAs(definition).And.Not.SameAs(first.DefaultScreenDefinition));
		Assert.That(firstCommands.All(item => ReferenceEquals(item.ScreenDefinition, first.DefaultScreenDefinition)), Is.True);
		Assert.That(secondCommands.All(item => ReferenceEquals(item.ScreenDefinition, second.DefaultScreenDefinition)), Is.True);
		Assert.That(firstCommands.All(item => ReferenceEquals(item.Parent.Parent, first)), Is.True);
		Assert.That(secondCommands.All(item => ReferenceEquals(item.Parent.Parent, second)), Is.True);
		Assert.That(definition.Activators, Is.EqualTo(new IApplicationMenuItem[] { firstItem, secondItem }));
		Assert.That(first.DefaultScreenDefinition.Activators, Is.EqualTo(firstCommands));
		Assert.That(second.DefaultScreenDefinition.Activators, Is.EqualTo(secondCommands));
		((Menu)first.Menus[0]).RemoveItem((TestApplicationMenuItem)firstCommands[0]);
		Assert.That(first.DefaultScreenDefinition.Activators.Single(), Is.SameAs(firstCommands[1]));
		Assert.That(firstCommands[0].Parent, Is.Null);
		Assert.That(second.DefaultScreenDefinition.Activators, Has.Length.EqualTo(2));
		Assert.That(definition.Activators, Has.Length.EqualTo(2));
	}

	private static Block CreateBlockWithItem(TestApplicationMenuItem item) {
		var menu = new Menu { Text = "Menu" };
		menu.AddItem(item);
		var block = new Block { Name = "Block" };
		block.AddMenu(menu);
		return block;
	}

	private static Block CreateScreenBlock(string name, ScreenActivationMode? mode) {
		var block = CreateBlockWithItem(new ScreenItem { Title = "Screen", ScreenDefinition = new(typeof(PlainScreen), "Screen", mode) });
		block.Name = name;
		return block;
	}

	private sealed class ScreenItem : TestApplicationMenuItem, IScreenMenuItem {
		public ApplicationScreenDefinition ScreenDefinition { get; set; }
	}

	private sealed class HostDecorator(IApplicationScreenHost screenHost) : ApplicationScreenHostDecorator(screenHost) {
	}

	private sealed record CustomMenu(string Text) : IApplicationMenu {
		public IApplicationBlock Parent { get; set; }

		public IApplicationMenuItem[] Items => Array.Empty<IApplicationMenuItem>();
	}

	private class PlainScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
	}

	private sealed class OtherScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
	}

	private sealed class NullMenuBlock : Block {
		public override string Name => "Invalid";
		public override IApplicationMenu[] Menus => null;
	}

	private sealed class MalformedBlock(ApplicationScreenDefinition[] definitions, ApplicationScreenDefinition defaultScreen) : Block {
		public override string Name => "Invalid";
		public override ApplicationScreenDefinition[] ScreenDefinitions => definitions;
		public override ApplicationScreenDefinition DefaultScreenDefinition { get => defaultScreen; set { } }
	}

	private sealed class NullToolbarItemBlock : Block, IApplicationCommandProvider {
		public override string Name => "Invalid toolbar";
		IApplicationMenuItem[] IApplicationCommandProvider.ToolBarItems => new IApplicationMenuItem[] { null };
	}

	private sealed class ToolbarBlock(IApplicationMenuItem[] commands) : Block, IApplicationCommandProvider {
		public IApplicationMenuItem[] Commands { get; set; } = commands;
		IApplicationMenuItem[] IApplicationCommandProvider.ToolBarItems => Commands;
	}

	private sealed class EqualScreenDefinition() : ApplicationScreenDefinition(typeof(PlainScreen), "Equal") {
		public override bool Equals(object obj) => obj is EqualScreenDefinition;
		public override int GetHashCode() => 0;
	}

	private sealed class AdditionalDefinitionSnapshot(ApplicationScreenDefinition additional) : NeutralSnapshot {
		protected override Block CreateBlockSnapshot(Block block, Menu[] menus, Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition> definitions) {
			var result = base.CreateBlockSnapshot(block, menus, definitions);
			result.AddScreenDefinition(additional);
			return result;
		}
	}

	private sealed class DefaultDefinitionSnapshot(ApplicationScreenDefinition replacement) : NeutralSnapshot {
		protected override Block CreateBlockSnapshot(Block block, Menu[] menus, Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition> definitions) {
			var result = base.CreateBlockSnapshot(block, menus, definitions);
			result.DefaultScreenDefinition = replacement;
			return result;
		}
	}

	private sealed class AlteredScreenItemSnapshot(bool loseScreenBinding) : NeutralSnapshot {
		protected override TestApplicationMenuItem CreateItemSnapshot(TestApplicationMenuItem item, Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition> definitions) => loseScreenBinding
			? new TestApplicationMenuItem { Title = item.Title }
			: new ScreenItem { Title = item.Title, ScreenDefinition = new(((IScreenMenuItem)item).ScreenDefinition.ScreenType, item.Title) };
	}

	private sealed class OtherScreenSnapshot : NeutralSnapshot {
		protected override void ValidateScreenType(Type screenType) => Tools.UI.ValidateScreenType(screenType, typeof(OtherScreen));
	}

	private class NeutralSnapshot : ApplicationBlockSnapshotBase<Block, Menu, TestApplicationMenuItem> {
		protected override Block CreateBlockSnapshot(Block block, Menu[] menus, Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition> definitions) {
			var snapshot = new Block { Name = block.Name, Position = block.Position };
			foreach (var definition in block.ScreenDefinitions)
				snapshot.AddScreenDefinition(SnapshotDefinition(definition, definitions));
			snapshot.DefaultScreenDefinition = block.DefaultScreenDefinition == null ? null : SnapshotDefinition(block.DefaultScreenDefinition, definitions);
			foreach (var menu in menus)
				snapshot.AddMenu(menu);
			return snapshot;
		}

		protected override Menu CreateMenuSnapshot(Menu menu, TestApplicationMenuItem[] items) {
			var snapshot = new Menu { Text = menu.Text };
			foreach (var item in items)
				snapshot.AddItem(item);
			return snapshot;
		}

		protected override TestApplicationMenuItem CreateItemSnapshot(TestApplicationMenuItem item, Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition> definitions) {
			TestApplicationMenuItem snapshot = item is ScreenItem screen ? new ScreenItem { ScreenDefinition = SnapshotDefinition(screen.ScreenDefinition, definitions) } : new TestApplicationMenuItem();
			snapshot.Title = item.Title;
			snapshot.CopySubscriptionsFrom(item);
			return snapshot;
		}
	}
}
