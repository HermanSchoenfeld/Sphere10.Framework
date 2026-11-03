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

/// <summary>Exercises shared definition, command and host ownership contracts.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationMetadataTests {
	/// <summary>Verifies block stores different menu implementations through the shared contract.</summary>
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

	/// <summary>Verifies block returns independent menu arrays.</summary>
	[Test]
	public void BlockReturnsIndependentMenuArrays() {
		var block = new Block { Name = "Block" };
		IApplicationCommandProvider contract = block;
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

	/// <summary>Verifies menu returns independent item arrays through both contracts.</summary>
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

	/// <summary>Verifies returned host arrays cannot change stored order or nested membership.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task ReturnedHostArraysCannotChangeStoredOrderOrNestedMembership(bool decorated) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var first = CreateBlockWithItem(new TestApplicationMenuItem { Title = "Stored item" });
		first.Name = "First";
		first.Position = 1;
		var second = new Block { Name = "Second", Position = 2 };
		using var owner = new ApplicationScreenHost<Block, IApplicationScreen>(new[] { second, first }, provider);
		IApplicationScreenHost host = decorated ? new HostDecorator(owner) : owner;
		var stored = host.AvailableBlocks[0];
		await host.SelectBlockAsync(stored);
		var storedMenu = ((IApplicationCommandProvider)stored).Menus.Single();
		var blocks = host.Blocks;
		var menus = ((IApplicationCommandProvider)blocks[0]).Menus;
		var items = menus[0].Items;
		blocks[0] = new Block { Name = "Replacement" };
		blocks[1] = null;
		menus[0] = new Menu { Text = "Replacement menu" };
		items[0] = new TestApplicationMenuItem { Title = "Replacement item" };
		Assert.That(host.Blocks.Select(block => block.Name), Is.EqualTo(new[] { "First", "Second" }));
		await host.SelectBlockAsync(stored);
		Assert.That(host.SelectedBlock, Is.SameAs(stored));
		Assert.That(((IApplicationCommandProvider)host.SelectedBlock).Menus.Single(), Is.SameAs(storedMenu));
		Assert.That(storedMenu.Items.Single().Title, Is.EqualTo("Stored item"));
		Assert.That(async () => await host.SelectBlockAsync(new Block { Name = "First" }), Throws.InstanceOf<ArgumentException>());
	}

	/// <summary>Verifies equal display labels retain distinct menu and item references.</summary>
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
		Assert.That(block.Menus, Has.Length.EqualTo(2));
		Assert.That(block.Menus[0], Is.Not.SameAs(block.Menus[1]));
		Assert.That(block.Menus[0].Items[0], Is.Not.SameAs(block.Menus[1].Items[0]));
	}

	/// <summary>Verifies host distinguishes different blocks by reference when their names match.</summary>
	[Test]
	public async Task HostDistinguishesDifferentBlocksByReferenceWhenTheirNamesMatch() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var first = new Block { Name = "Same" };
		var second = new Block { Name = "Same" };
		using var host = new ApplicationScreenHost(new[] { first, second }, provider);
		await host.SelectBlockAsync(first);
		Assert.That(host.SelectedBlock, Is.SameAs(first));
		await host.SelectBlockAsync(second);
		Assert.That(host.SelectedBlock, Is.SameAs(second));
		Assert.That(host.Blocks, Has.Length.EqualTo(2));
		Assert.That(await host.UnregisterBlockAsync(first), Is.True);
		Assert.That(host.Blocks.Single(), Is.SameAs(second));
	}

	/// <summary>Verifies screen ownership survives removing its navigation menu.</summary>
	[Test]
	public void ScreenOwnershipSurvivesRemovingItsNavigationMenu() {
		var definition = new ApplicationScreenDefinition(typeof(PlainScreen), "Screen");
		var block = CreateBlockWithItem(new ScreenItem { Title = "Screen", ScreenDefinition = definition });
		block.RemoveMenu(block.Menus.Single());
		Assert.That(block.Menus, Is.Empty);
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(definition));
	}

	/// <summary>Verifies definition arrays are defensive and registering the same reference is idempotent.</summary>
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

	/// <summary>Verifies host allows independent policies across different blocks.</summary>
	[Test]
	public void HostAllowsIndependentPoliciesAcrossDifferentBlocks() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var first = CreateScreenBlock("First", ScreenActivationMode.SingleInstance);
		var second = CreateScreenBlock("Second", ScreenActivationMode.MultiInstance);
		using var host = new ApplicationScreenHost<Block, IApplicationScreen>(new[] { first, second }, provider);
		Assert.That(host.Blocks, Has.Length.EqualTo(2));
	}

	/// <summary>Verifies unspecified activation policy does not conflict with an explicit declaration.</summary>
	[Test]
	public void UnspecifiedActivationPolicyDoesNotConflictWithAnExplicitDeclaration() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var first = CreateScreenBlock("First", null);
		var second = CreateScreenBlock("Second", ScreenActivationMode.MultiInstance);
		using var host = new ApplicationScreenHost<Block, IApplicationScreen>(new[] { first, second }, provider);
		Assert.That(host.Blocks, Has.Length.EqualTo(2));
		Assert.That(first.ScreenDefinitions.Single().ActivationMode, Is.Null);
	}

	/// <summary>Verifies default selection uses the exact registered definition instead of its type or caption.</summary>
	[Test]
	public void DefaultSelectionUsesTheExactRegisteredDefinitionInsteadOfItsTypeOrCaption() {
		var first = new ApplicationScreenDefinition(typeof(PlainScreen), "Same", ScreenActivationMode.SingleInstance);
		var second = new ApplicationScreenDefinition(typeof(PlainScreen), "Same", ScreenActivationMode.SingleInstance);
		var firstItem = new ScreenItem { Title = "First", ScreenDefinition = first };
		var secondItem = new ScreenItem { Title = "Second", ScreenDefinition = second };
		var block = CreateBlockWithItem(firstItem);
		var menu = new Menu { Text = "Other" };
		menu.AddItem(secondItem);
		block.AddScreenDefinition(second);
		block.AddMenu(menu);
		block.DefaultScreenDefinition = second;
		Assert.That(Tools.UI.GetDefaultScreen(new[] { block }), Is.SameAs(second));
	}

	/// <summary>Verifies subscription copies retain their own invocation lists.</summary>
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

	/// <summary>Verifies host rejects malformed definition ownership before initialization.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public void HostRejectsMalformedDefinitionOwnershipBeforeInitialization(bool malformedDefault) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var definition = new ApplicationScreenDefinition(typeof(PlainScreen));
		var block = new MalformedBlock(malformedDefault ? Array.Empty<ApplicationScreenDefinition>() : new ApplicationScreenDefinition[] { null },
			malformedDefault ? definition : null);
		Assert.That(() => new ApplicationScreenHost<Block, IApplicationScreen>(new[] { block }, provider), Throws.InstanceOf<ArgumentException>());
	}

	/// <summary>Verifies typed host validates owned screens whether or not they are defaults.</summary>
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

	/// <summary>Verifies invalid screen type is rejected when the definition is constructed.</summary>
	[Test]
	public void InvalidScreenTypeIsRejectedWhenTheDefinitionIsConstructed() {
		Assert.That(() => new ApplicationScreenDefinition(typeof(string)), Throws.InstanceOf<ArgumentException>());
	}

	/// <summary>Verifies direct default resolution rejects missing menus.</summary>
	[Test]
	public void DirectDefaultResolutionRejectsMissingMenus() {
		var block = new NullMenuBlock();
		Assert.That(() => Tools.UI.ResolveScreenReferences(block, block), Throws.InstanceOf<ArgumentException>());
	}

	/// <summary>Verifies direct default resolution rejects an unowned default.</summary>
	[Test]
	public void DirectDefaultResolutionRejectsAnUnownedDefault() {
		var block = new MalformedBlock(Array.Empty<ApplicationScreenDefinition>(), new(typeof(PlainScreen)));
		Assert.That(() => Tools.UI.GetScreenDefinitions(new[] { block }), Throws.InstanceOf<ArgumentException>());
	}

	/// <summary>Verifies direct default resolution rejects malformed owned definitions.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public void DirectDefaultResolutionRejectsMalformedOwnedDefinitions(bool nullArray) {
		var block = new MalformedBlock(nullArray ? null : new ApplicationScreenDefinition[] { null }, null);
		Assert.That(() => Tools.UI.GetScreenDefinitions(new[] { block }), Throws.InstanceOf<ArgumentException>());
	}

	/// <summary>Verifies presentation resolution rejects null toolbar commands without making them a host requirement.</summary>
	[Test]
	public void PresentationResolutionRejectsNullToolbarCommandsWithoutMakingThemAHostRequirement() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new NullToolbarItemBlock();
		using var host = new ApplicationScreenHost(new[] { block }, provider);
		Assert.That(() => Tools.UI.ResolveScreenReferences(block, block), Throws.ArgumentException);
		Assert.That(host.Blocks.Single(), Is.SameAs(block));
	}

	/// <summary>Verifies definition removal requires its navigation references to be released.</summary>
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

	/// <summary>Verifies definition removal uses reference identity and ignores absent equal values.</summary>
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

	/// <summary>Verifies definition removal rejects null without changing membership.</summary>
	[Test]
	public void DefinitionRemovalRejectsNullWithoutChangingMembership() {
		var definition = new ApplicationScreenDefinition(typeof(PlainScreen));
		var block = new Block { Name = "Block" };
		block.AddScreenDefinition(definition);
		Assert.That(() => block.RemoveScreenDefinition(null), Throws.ArgumentNullException);
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(definition));
	}

	/// <summary>Verifies action only block has no default screen.</summary>
	[Test]
	public void ActionOnlyBlockHasNoDefaultScreen() {
		var block = CreateBlockWithItem(new TestApplicationMenuItem { Title = "Action" });
		Assert.That(Tools.UI.GetDefaultScreen(new[] { block }), Is.Null);
	}

	// Creates a block owning one command and any referenced screen definition.
	private static Block CreateBlockWithItem(TestApplicationMenuItem item) {
		var menu = new Menu { Text = "Menu" };
		menu.AddItem(item);
		var block = new Block { Name = "Block" };
		if (item is IScreenMenuItem screen)
			block.AddScreenDefinition(screen.ScreenDefinition);
		block.AddMenu(menu);
		return block;
	}

	// Creates a named block with one configured activation policy.
	private static Block CreateScreenBlock(string name, ScreenActivationMode? mode) {
		var block = CreateBlockWithItem(new ScreenItem { Title = "Screen", ScreenDefinition = new(typeof(PlainScreen), "Screen", mode) });
		block.Name = name;
		return block;
	}

	// Supplies a screen-reference command for shared ownership checks.
	private sealed class ScreenItem : TestApplicationMenuItem, IScreenMenuItem {
		/// <inheritdoc />
		public ApplicationScreenDefinition ScreenDefinition { get; set; }
	}

	// Exercises the same host contract through its existing decorator.
	private sealed class HostDecorator(IApplicationScreenHost screenHost) : ApplicationScreenHostDecorator(screenHost) {
	}

	// Supplies an alternate menu implementation without command items.
	private sealed record CustomMenu(string Text) : IApplicationMenu {
		/// <inheritdoc />
		public IApplicationBlock Parent { get; set; }

		public IApplicationMenuItem[] Items {
			get {
				return Array.Empty<IApplicationMenuItem>();
			}
		}
	}

	// Supplies a platform-neutral screen for metadata validation.
	private class PlainScreen : IApplicationScreen {
		/// <inheritdoc />
		public IApplicationScreenHost Host { get; set; }

		/// <inheritdoc />
		public IApplicationBlock Block { get; set; }
	}

	// Supplies an incompatible typed-host screen for validation checks.
	private sealed class OtherScreen : IApplicationScreen {
		/// <inheritdoc />
		public IApplicationScreenHost Host { get; set; }

		/// <inheritdoc />
		public IApplicationBlock Block { get; set; }
	}

	// Supplies malformed menu data at the presentation boundary.
	private sealed class NullMenuBlock : Block {
		/// <inheritdoc />
		public override string Name {
			get {
				return "Invalid";
			}
		}

		/// <inheritdoc />
		public override IApplicationMenu[] Menus {
			get {
				return null;
			}
		}
	}

	// Supplies invalid owned/default definitions at the validation boundary.
	private sealed class MalformedBlock(ApplicationScreenDefinition[] definitions, ApplicationScreenDefinition defaultScreen) : Block {
		/// <inheritdoc />
		public override string Name {
			get {
				return "Invalid";
			}
		}

		/// <inheritdoc />
		public override ApplicationScreenDefinition[] ScreenDefinitions {
			get {
				return definitions;
			}
		}

		/// <inheritdoc />
		public override ApplicationScreenDefinition DefaultScreenDefinition {
			get {
				return defaultScreen;
			}
			set {
			}
		}
	}

	// Supplies a null toolbar command without changing host ownership requirements.
	private sealed class NullToolbarItemBlock : Block, IApplicationCommandProvider {
		/// <inheritdoc />
		public override string Name {
			get {
				return "Invalid toolbar";
			}
		}

		/// <inheritdoc />
		IApplicationMenuItem[] IApplicationCommandProvider.ToolBarItems {
			get {
				return new IApplicationMenuItem[] { null };
			}
		}
	}

	// Exposes mutable toolbar membership for definition-removal checks.
	private sealed class ToolbarBlock(IApplicationMenuItem[] commands) : Block, IApplicationCommandProvider {
		/// <summary>Gets or sets the toolbar membership used by the removal check.</summary>
		public IApplicationMenuItem[] Commands { get; set; } = commands;

		/// <inheritdoc />
		IApplicationMenuItem[] IApplicationCommandProvider.ToolBarItems {
			get {
				return Commands;
			}
		}
	}

	// Uses value equality to ensure ownership operations use reference identity.
	private sealed class EqualScreenDefinition() : ApplicationScreenDefinition(typeof(PlainScreen), "Equal") {
		/// <inheritdoc />
		public override bool Equals(object obj) {
			return obj is EqualScreenDefinition;
		}

		/// <inheritdoc />
		public override int GetHashCode() {
			return 0;
		}
	}
}
