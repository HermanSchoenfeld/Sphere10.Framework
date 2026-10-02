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
		var first = CreateBlock("first", new ScreenItem { ScreenType = typeof(FirstScreen), IsDefault = true });
		first.Position = 100;
		var second = CreateBlock("second", new ScreenItem { ScreenType = typeof(SecondScreen), IsDefault = true });
		second.Position = -100;
		var third = CreateBlock("third", new ScreenItem { ScreenType = typeof(ThirdScreen), IsDefault = true });
		var plugins = new[] { new ApplicationPlugin("First plugin", new[] { first, second }), new ApplicationPlugin("Second plugin", new[] { third }) };
		var ordered = Tools.UI.OrderApplicationBlocks(plugins, new[] { third, second, first });
		var selected = Tools.UI.GetDefaultScreen(Tools.UI.GetScreenDefinitions(ordered));

		Assert.That(ordered, Is.EqualTo(new[] { first, second, third }));
		Assert.That(selected.Block, Is.SameAs(first));
		Assert.That(selected.ScreenType, Is.EqualTo(typeof(FirstScreen)));
	}

	[Test]
	public void PluginOrderingUsesLiveSnapshotsSkipsRemovedBlocksAndAppendsStandaloneBlocks() {
		var declared = new ApplicationBlock { Name = "declared" };
		var removed = new ApplicationBlock { Name = "removed" };
		var live = new ApplicationBlock { Name = "live", Id = declared.Id };
		var standalone = new ApplicationBlock { Name = "standalone" };
		var plugin = new ApplicationPlugin("Plugin", new[] { removed, declared });

		var ordered = Tools.UI.OrderApplicationBlocks(new[] { plugin }, new[] { standalone, live });

		Assert.That(ordered, Is.EqualTo(new[] { live, standalone }));
		ordered[0] = removed;
		Assert.That(Tools.UI.OrderApplicationBlocks(new[] { plugin }, new[] { standalone, live })[0], Is.SameAs(live));
	}

	[Test]
	public void MultipleDefaultMenuItemsUseDeclarationOrder() {
		var block = CreateBlock("Block", new ScreenItem { ScreenType = typeof(FirstScreen), IsDefault = true },
			new ScreenItem { ScreenType = typeof(SecondScreen), IsDefault = true });

		Assert.That(Tools.UI.GetDefaultScreen(Tools.UI.GetScreenDefinitions(new[] { block })).ScreenType, Is.EqualTo(typeof(FirstScreen)));
	}

	[Test]
	public void UnmarkedScreensDoNotBecomeExplicitDefaults() {
		var block = CreateBlock("Block", new ScreenItem { ScreenType = typeof(FirstScreen) });

		Assert.That(Tools.UI.GetDefaultScreen(Tools.UI.GetScreenDefinitions(new[] { block })), Is.Null);
	}

	[Test]
	public void BlockDefaultResolvesMenuIdentityAndLifetimeWithoutDuplicatingTheScreen() {
		var item = new ScreenItem { Id = "home", Title = "Menu title", ScreenType = typeof(FirstScreen), ActivationMode = ScreenActivationMode.PermanentSingleton };
		var block = CreateBlock("Block", item);
		block.DefaultScreen = typeof(FirstScreen);
		block.DefaultScreenTitle = "Home";

		var definition = Tools.UI.GetScreenDefinitions(new[] { block }).Single();

		Assert.That(definition.Block, Is.SameAs(block));
		Assert.That(definition.MenuItemId, Is.EqualTo("home"));
		Assert.That(definition.Title, Is.EqualTo("Home"));
		Assert.That(definition.ActivationMode, Is.EqualTo(ScreenActivationMode.PermanentSingleton));
		Assert.That(definition.IsDefault, Is.True);
	}

	[TestCase(ScreenActivationMode.SingleInstance)]
	[TestCase(ScreenActivationMode.MultiInstance)]
	public void StandaloneEmptyDefaultRetainsItsConfiguredLifetime(ScreenActivationMode mode) {
		var block = new ApplicationBlock {
			Name = "Empty", DefaultScreen = typeof(FirstScreen), DefaultScreenTitle = "Welcome",
			DefaultScreenActivationMode = mode, DefaultScreenKind = ScreenKind.Empty
		};

		var definition = Tools.UI.GetScreenDefinitions(new[] { block }).Single();

		Assert.That(definition.MenuItemId, Is.EqualTo(ApplicationScreenDefinition.DefaultMenuItemId));
		Assert.That(definition.ActivationMode, Is.EqualTo(mode));
		Assert.That(definition.ScreenKind, Is.EqualTo(ScreenKind.Empty));
		Assert.That(Tools.UI.GetDefaultScreen(new[] { definition }), Is.SameAs(definition));
	}

	[Test]
	public void EmptyDefaultCannotReplaceAnAutomaticallyOpenedPermanentScreen() {
		var block = CreateBlock("Block",
			new ScreenItem { ScreenType = typeof(FirstScreen), ScreenKind = ScreenKind.Empty, IsDefault = true },
			new ScreenItem { ScreenType = typeof(SecondScreen), ActivationMode = ScreenActivationMode.PermanentSingleton },
			new ScreenItem { ScreenType = typeof(ThirdScreen), IsDefault = true });
		var definitions = Tools.UI.GetScreenDefinitions(new[] { block });

		Assert.That(Tools.UI.GetDefaultScreen(definitions).ScreenType, Is.EqualTo(typeof(ThirdScreen)));
		Assert.That(Tools.UI.GetEmptyScreen(definitions).ScreenType, Is.EqualTo(typeof(FirstScreen)));
	}

	[Test]
	public void EmptySelectionPrefersFirstMarkedPlaceholderThenFallsBackToFirstPlaceholder() {
		var first = new ScreenItem { ScreenType = typeof(FirstScreen), ScreenKind = ScreenKind.Empty };
		var second = new ScreenItem { ScreenType = typeof(SecondScreen), ScreenKind = ScreenKind.Empty, IsDefault = true };
		var block = CreateBlock("Block", first, second);

		Assert.That(Tools.UI.GetEmptyScreen(Tools.UI.GetScreenDefinitions(new[] { block })).ScreenType, Is.EqualTo(typeof(SecondScreen)));
		second.IsDefault = false;
		Assert.That(Tools.UI.GetEmptyScreen(Tools.UI.GetScreenDefinitions(new[] { block })).ScreenType, Is.EqualTo(typeof(FirstScreen)));
	}

	[Test]
	public void ScreenKindsCannotConflictForTheSameComponentType() {
		var block = CreateBlock("Block", new ScreenItem { ScreenType = typeof(FirstScreen) },
			new ScreenItem { ScreenType = typeof(FirstScreen), ScreenKind = ScreenKind.Empty });

		Assert.That(() => Tools.UI.GetScreenDefinitions(new[] { block }), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void DefaultPolicyCannotConflictWithItsMenuPolicy() {
		var block = CreateBlock("Block", new ScreenItem { ScreenType = typeof(FirstScreen), ActivationMode = ScreenActivationMode.MultiInstance });
		block.DefaultScreen = typeof(FirstScreen);
		block.DefaultScreenActivationMode = ScreenActivationMode.PermanentSingleton;

		Assert.That(() => Tools.UI.GetScreenDefinitions(new[] { block }), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase(ScreenActivationMode.SingleInstance, true)]
	[TestCase(ScreenActivationMode.MultiInstance, false)]
	[TestCase(ScreenActivationMode.PermanentSingleton, true)]
	public void LifetimeClassificationIncludesPermanentSingleton(ScreenActivationMode mode, bool expected) {
		var registry = new ScreenActivationPolicyRegistry();
		registry.RegisterInstance(typeof(FirstScreen), mode);

		Assert.That(Tools.UI.IsSingleton(mode), Is.EqualTo(expected));
		Assert.That(registry.TryGetPolicy(typeof(FirstScreen), out var registered), Is.True);
		Assert.That(registered, Is.EqualTo(mode));
	}

	[TestCase(ScreenActivationMode.PermanentSingleton, ScreenKind.Empty)]
	[TestCase((ScreenActivationMode)999, ScreenKind.Normal)]
	[TestCase(ScreenActivationMode.SingleInstance, (ScreenKind)999)]
	public void InvalidScreenCombinationsFailBeforeHostRegistration(ScreenActivationMode mode, ScreenKind kind) {
		var block = new ApplicationBlock { Name = "Block", DefaultScreen = typeof(FirstScreen), DefaultScreenActivationMode = mode, DefaultScreenKind = kind };

		Assert.That(() => Tools.UI.GetScreenDefinitions(new[] { block }), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SelectionRejectsNullDefinitions(bool empty) {
		var definitions = new ApplicationScreenDefinition[] { null };

		Assert.That(() => empty ? Tools.UI.GetEmptyScreen(definitions) : Tools.UI.GetDefaultScreen(definitions), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase(true)]
	[TestCase(false)]
	public void ResolutionRejectsMalformedMenuMembership(bool nullMenu) {
		var block = new MalformedBlock(nullMenu ? new IApplicationMenu[] { null } : new IApplicationMenu[] { new NullItemsMenu() });

		Assert.That(() => Tools.UI.GetScreenDefinitions(new[] { block }), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void SharedBuilderPreservesDefaultLifetimeAndKind() {
		var block = new BlockBuilder().Default(typeof(FirstScreen), ScreenActivationMode.MultiInstance, ScreenKind.Empty).Build();

		Assert.That(block.DefaultScreen, Is.EqualTo(typeof(FirstScreen)));
		Assert.That(block.DefaultScreenActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		Assert.That(block.DefaultScreenKind, Is.EqualTo(ScreenKind.Empty));
	}

	private static ApplicationBlock CreateBlock(string name, params ScreenItem[] items) {
		var menu = new ApplicationMenu<ScreenItem> { Text = "Screens" };
		foreach (var item in items) {
			if (string.IsNullOrEmpty(item.Title))
				item.Title = item.ScreenType.Name;
			menu.AddItem(item);
		}
		var block = new ApplicationBlock { Name = name };
		block.AddMenu(menu);
		return block;
	}

	private sealed class ScreenItem : ApplicationMenuItem, IScreenMenuItem {
		public Type ScreenType { get; set; }

		public ScreenActivationMode? ActivationMode { get; set; }

		public ScreenKind ScreenKind { get; set; }

		public bool IsDefault { get; set; }
	}

	private sealed class BlockBuilder : ApplicationBlockBuilderBase<IApplicationMenu, ApplicationBlock> {
		public BlockBuilder Default(Type screen, ScreenActivationMode mode, ScreenKind kind) {
			SetName("Block");
			SetDefaultScreen(screen, "Title", mode, kind);
			return this;
		}

		protected override ApplicationBlock CreateBlock(IReadOnlyList<IApplicationMenu> menus) => new() {
			Name = Name, DefaultScreen = DefaultScreen, DefaultScreenTitle = DefaultScreenTitle,
			DefaultScreenActivationMode = DefaultScreenActivationMode, DefaultScreenKind = DefaultScreenKind
		};
	}

	private sealed class MalformedBlock(IApplicationMenu[] menus) : ApplicationBlock {
		public override IApplicationMenu[] Menus => menus;
	}

	private sealed class NullItemsMenu : IApplicationMenu {
		public string Text => "Malformed";

		public IApplicationMenuItem[] Items => null;
	}

	private class FirstScreen : IApplicationScreen {
	}

	private class SecondScreen : IApplicationScreen {
	}

	private class ThirdScreen : IApplicationScreen {
	}
}
