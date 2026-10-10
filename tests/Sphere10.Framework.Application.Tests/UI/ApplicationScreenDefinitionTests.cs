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

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationScreenDefinitionTests {
	[Test]
	public void DefaultFollowsPluginThenBlockOrderInsteadOfGlobalDisplayPosition() {
		var first = CreateBlock("First", Definition(typeof(FirstScreen)));
		first.Position = 100;
		first.DefaultScreenDefinition = first.ScreenDefinitions.Single();
		var second = CreateBlock("Second", Definition(typeof(SecondScreen)));
		second.Position = -100;
		second.DefaultScreenDefinition = second.ScreenDefinitions.Single();
		var third = CreateBlock("Third", Definition(typeof(ThirdScreen)));
		third.DefaultScreenDefinition = third.ScreenDefinitions.Single();
		var plugins = new[] { new ApplicationPlugin("First plugin", new[] { first, second }), new ApplicationPlugin("Second plugin", new[] { third }) };
		var ordered = Tools.UI.OrderApplicationBlocks(plugins, new[] { third, second, first });
		Assert.That(ordered, Is.EqualTo(new[] { first, second, third }));
		Assert.That(Tools.UI.GetDefaultScreen(ordered), Is.SameAs(first.DefaultScreenDefinition));
	}

	[Test]
	public void PluginOrderingUsesReferenceMembershipSkipsRemovedBlocksAndAppendsStandaloneBlocks() {
		var declared = new TestApplicationBlock { Name = "Declared" };
		var removed = new TestApplicationBlock { Name = "Removed" };
		var standalone = new TestApplicationBlock { Name = "Declared" };
		var plugin = new ApplicationPlugin("Plugin", new[] { removed, declared });
		var ordered = Tools.UI.OrderApplicationBlocks(new[] { plugin }, new[] { standalone, declared });
		Assert.That(ordered, Is.EqualTo(new[] { declared, standalone }));
		ordered[0] = removed;
		Assert.That(Tools.UI.OrderApplicationBlocks(new[] { plugin }, new[] { standalone, declared })[0], Is.SameAs(declared));
	}

	[Test]
	public void BlockDefaultCanSelectALaterDeclaredDefinition() {
		var first = Definition(typeof(FirstScreen));
		var second = Definition(typeof(SecondScreen));
		var block = CreateBlock("Block", first, second);
		block.DefaultScreenDefinition = second;
		Assert.That(Tools.UI.GetDefaultScreen(new[] { block }), Is.SameAs(second));
		Assert.That(block.ScreenDefinitions, Is.EqualTo(new[] { first, second }));
	}

	[Test]
	public void RegisteredScreensDoNotBecomeExplicitDefaults() {
		var block = CreateBlock("Block", Definition(typeof(FirstScreen)));
		Assert.That(Tools.UI.GetDefaultScreen(new[] { block }), Is.Null);
	}

	[Test]
	public void MenuCommandReferencesTheOwnedDefaultWithoutDuplicatingItsDefinition() {
		var definition = new ApplicationScreenDefinition(typeof(FirstScreen), "Home", ScreenActivationMode.PermanentSingleton);
		var block = CreateBlock("Block", definition);
		block.DefaultScreenDefinition = definition;
		var menu = new TestApplicationMenu<ScreenItem> { Text = "Screens" };
		var item = new ScreenItem { Title = "Open home", ScreenDefinition = definition };
		menu.AddItem(item);
		block.AddMenu(menu);
		Assert.That(Tools.UI.GetScreenDefinitions(new[] { block }).Single(), Is.SameAs(definition));
		Assert.That(item.ScreenDefinition, Is.SameAs(block.DefaultScreenDefinition));
		Assert.That(Tools.UI.GetDefaultScreen(new[] { block }), Is.SameAs(definition));
	}

	[TestCase(ScreenActivationMode.SingleInstance)]
	[TestCase(ScreenActivationMode.MultiInstance)]
	public void StandaloneEmptyDefaultRetainsItsConfiguredLifetime(ScreenActivationMode mode) {
		var definition = new ApplicationScreenDefinition(typeof(FirstScreen), "Welcome", mode, ScreenKind.Empty);
		var block = new TestApplicationBlock("Empty", definition);
		Assert.That(Tools.UI.GetScreenDefinitions(new[] { block }).Single(), Is.SameAs(definition));
		Assert.That(definition.ActivationMode, Is.EqualTo(mode));
		Assert.That(definition.ScreenKind, Is.EqualTo(ScreenKind.Empty));
		Assert.That(Tools.UI.GetEmptyScreen(new[] { block }), Is.SameAs(definition));
	}

	[Test]
	public void EmptyDefaultCannotReplaceAnAutomaticallyOpenedPermanentScreen() {
		var empty = Definition(typeof(FirstScreen), ScreenActivationMode.SingleInstance, ScreenKind.Empty);
		var permanent = Definition(typeof(SecondScreen), ScreenActivationMode.PermanentSingleton);
		var block = CreateBlock("Block", empty, permanent);
		block.DefaultScreenDefinition = empty;
		Assert.That(Tools.UI.GetDefaultScreen(new[] { block }), Is.Null);
		Assert.That(Tools.UI.GetEmptyScreen(new[] { block }), Is.SameAs(empty));
	}

	[Test]
	public void EmptySelectionPrefersTheDefaultReferenceThenFallsBackToDeclarationOrder() {
		var first = Definition(typeof(FirstScreen), ScreenActivationMode.SingleInstance, ScreenKind.Empty);
		var second = Definition(typeof(SecondScreen), ScreenActivationMode.SingleInstance, ScreenKind.Empty);
		var block = CreateBlock("Block", first, second);
		block.DefaultScreenDefinition = second;
		Assert.That(Tools.UI.GetEmptyScreen(new[] { block }), Is.SameAs(second));
		block.DefaultScreenDefinition = null;
		Assert.That(Tools.UI.GetEmptyScreen(new[] { block }), Is.SameAs(first));
	}

	[Test]
	public void ScreenKindsCannotConflictForTheSameType() {
		var block = CreateBlock("Block", Definition(typeof(FirstScreen)), Definition(typeof(FirstScreen), ScreenActivationMode.SingleInstance, ScreenKind.Empty));
		Assert.That(() => Tools.UI.GetScreenDefinitions(new[] { block }), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void DistinctDefaultAndMenuAliasesCannotDeclareConflictingTypePolicies() {
		var block = CreateBlock("Block", Definition(typeof(FirstScreen), ScreenActivationMode.MultiInstance));
		block.DefaultScreenDefinition = Definition(typeof(FirstScreen), ScreenActivationMode.PermanentSingleton);
		Assert.That(() => Tools.UI.GetScreenDefinitions(new[] { block }), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase(ScreenActivationMode.SingleInstance, true)]
	[TestCase(ScreenActivationMode.MultiInstance, false)]
	[TestCase(ScreenActivationMode.PermanentSingleton, true)]
	public void LifetimeClassificationIncludesPermanentSingleton(ScreenActivationMode mode, bool expected) {
		var definition = new ApplicationScreenDefinition(typeof(FirstScreen), activationMode: mode);
		Assert.That(() => ApplicationScreenDefinition.ValidateDefinitions(new[] { definition }), Throws.Nothing);
		Assert.That(Tools.UI.IsSingleton(mode), Is.EqualTo(expected));
		Assert.That(definition.ActivationMode, Is.EqualTo(mode));
	}

	[TestCase(ScreenActivationMode.PermanentSingleton, ScreenKind.Empty)]
	[TestCase((ScreenActivationMode)999, ScreenKind.Normal)]
	[TestCase(ScreenActivationMode.SingleInstance, (ScreenKind)999)]
	public void InvalidScreenCombinationsFailWhenConstructingTheDefinition(ScreenActivationMode mode, ScreenKind kind) =>
		Assert.That(() => Definition(typeof(FirstScreen), mode, kind), Throws.InstanceOf<ArgumentException>());

	[TestCase(false)]
	[TestCase(true)]
	public void SelectionRejectsMalformedDefinitionMembership(bool empty) {
		var block = new MalformedBlock(Array.Empty<IApplicationMenu>(), new ApplicationScreenDefinition[] { null });
		Assert.That(() => empty ? Tools.UI.GetEmptyScreen(new[] { block }) : Tools.UI.GetDefaultScreen(new[] { block }), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase(true)]
	[TestCase(false)]
	public void ResolutionRejectsMalformedMenuMembership(bool nullMenu) {
		var block = new MalformedBlock(nullMenu ? new IApplicationMenu[] { null } : new IApplicationMenu[] { new NullItemsMenu() }, Array.Empty<ApplicationScreenDefinition>());
		Assert.That(() => Tools.UI.GetScreenDefinitions(new[] { block }), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void SharedBuilderPreservesDefaultDefinitionIdentityAndPolicy() {
		var definition = new ApplicationScreenDefinition(typeof(FirstScreen), "Title", ScreenActivationMode.MultiInstance, ScreenKind.Empty);
		var block = new BlockBuilder()
			.Default(definition)
			.Build();
		Assert.That(block.DefaultScreenDefinition, Is.SameAs(definition));
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(definition));
	}

	private static ApplicationScreenDefinition Definition(Type type, ScreenActivationMode? mode = null, ScreenKind kind = ScreenKind.Normal) => new(type, type.Name, mode, kind);

	private static TestApplicationBlock CreateBlock(string name, params ApplicationScreenDefinition[] definitions) {
		var block = new TestApplicationBlock { Name = name };
		foreach (var definition in definitions)
			block.AddScreenDefinition(definition);
		return block;
	}

	private sealed class ScreenItem : TestApplicationMenuItem, IScreenMenuItem {
		public ApplicationScreenDefinition ScreenDefinition { get; set; }
	}

	private sealed class BlockBuilder : ApplicationBlockBuilderBase<IApplicationMenu, TestApplicationBlock> {
		public BlockBuilder Default(ApplicationScreenDefinition definition) {
			SetName("Block");
			SetDefaultScreen(definition);
			return this;
		}

		protected override TestApplicationBlock CreateBlock(IReadOnlyList<IApplicationMenu> menus) => new() { Name = Name };
	}

	private sealed class MalformedBlock(IApplicationMenu[] menus, ApplicationScreenDefinition[] definitions) : TestApplicationBlock {
		public override string Name => "Invalid";
		public override IApplicationMenu[] Menus => menus;
		public override ApplicationScreenDefinition[] ScreenDefinitions => definitions;
	}

	private sealed class NullItemsMenu : IApplicationMenu {
		public IApplicationBlock Parent { get; set; }

		public string Text => "Malformed";
		public IApplicationMenuItem[] Items => null;
	}

	private class FirstScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
	}

	private class SecondScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
	}

	private class ThirdScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
	}
}
