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

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationCommandMergeTests {
	[Test]
	public void NarrowerCommandsOverrideInPlaceAndHelpRemainsLastWithoutMutatingSources() {
		var original = new TestApplicationMenuItem { Title = "Save" };
		var screenSave = new TestApplicationMenuItem { Title = "Save" };
		var blockAction = new TestApplicationMenuItem { Title = "Block action" };
		var extra = new TestApplicationMenuItem { Title = "Screen action" };
		var app = new[] { Menu("work", original), Menu("help", new TestApplicationMenuItem { Title = "Help" }) };
		var block = new[] { Menu("work", blockAction) };
		var screen = new[] { Menu("work", screenSave, extra), Menu("screen", extra) };

		var merged = Tools.UI.MergeMenus<TestApplicationMenu<TestApplicationMenuItem>, TestApplicationMenuItem>(
			(menu, items) => Menu(menu.Text, items), app, block, screen);
		Assert.That(merged.Select(menu => menu.Text), Is.EqualTo(new[] { "work", "screen", "help" }));
		Assert.That(merged[0].Items, Is.EqualTo(new[] { screenSave, blockAction, extra }));
		Assert.That(app[0].Items.Single(), Is.SameAs(original));
		merged[0].RemoveItem(screenSave);
		Assert.That(screen[0].Items, Has.Length.EqualTo(2));
		Assert.That(Tools.UI.MergeMenus<TestApplicationMenu<TestApplicationMenuItem>, TestApplicationMenuItem>(
			(menu, items) => Menu(menu.Text, items), app, block)[0].Items, Is.EqualTo(new[] { original, blockAction }));
	}

	[Test]
	public void SeparatorsAreNormalizedAndOverridingDoesNotDuplicateCommands() {
		var first = new TestApplicationMenuItem { Title = "First" };
		var replacement = new TestApplicationMenuItem { Title = "First" };
		var last = new TestApplicationMenuItem { Title = "Last" };
		var separator = new Separator();
		var merged = Tools.UI.MergeMenuItems<IApplicationMenuItem>(
			new IApplicationMenuItem[] { separator, first, separator, separator },
			new IApplicationMenuItem[] { replacement, separator, last, separator });
		Assert.That(merged, Is.EqualTo(new IApplicationMenuItem[] { replacement, separator, last }));
		merged[0] = last;
		Assert.That(first.Title, Is.EqualTo("First"));
		Assert.That(Tools.UI.MergeMenuItems<IApplicationMenuItem>(new IApplicationMenuItem[] { separator }), Is.Empty);
	}

	[Test]
	public void PresentationCompositionBorrowsCommandsWithoutChangingTheirSourceParents() {
		var block = new TestApplicationBlock { Name = "Work" };
		var source = new TestApplicationMenu<TestApplicationMenuItem> { Text = "File" };
		var item = new TestApplicationMenuItem { Title = "Save" };
		source.AddItem(item);
		block.AddMenu(source);
		var composed = Tools.UI.MergeMenus<TestApplicationMenu<TestApplicationMenuItem>, TestApplicationMenuItem>(
			(menu, items) => Menu(menu.Text, items), new[] { source }).Single();
		Assert.That(composed.Items.Single(), Is.SameAs(item));
		Assert.That(item.Parent, Is.SameAs(source));
		Assert.That(item.Parent.Parent, Is.SameAs(block));
		composed.RemoveItem(item);
		Assert.That(item.Parent, Is.SameAs(source));
		Assert.That(source.Items.Single(), Is.SameAs(item));
	}

	[Test]
	public void InvalidCommandLayersFailBeforeRendering() {
		Assert.That(() => Tools.UI.MergeMenuItems<IApplicationMenuItem>((IApplicationMenuItem[][])null), Throws.ArgumentNullException);
		Assert.That(() => Tools.UI.MergeMenuItems(new IApplicationMenuItem[] { null }), Throws.ArgumentNullException);
		Assert.That(() => Tools.UI.MergeMenuItems(new[] { new TestApplicationMenuItem { Title = "" } }), Throws.ArgumentException);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ApplicationCommandsKeepTheirCallbacksWhileScreenCommandsReplaceMatchingBlockCommands(bool toolbar) {
		var calls = new List<string>();
		var applicationSave = new KeyedAction { Key = "save", Title = "Application save", Action = () => calls.Add("application") };
		var blockSave = new KeyedAction { Key = "save", Title = "Block save", Action = () => calls.Add("block") };
		var screenSave = new KeyedAction { Key = "save", Title = "Screen save", Action = () => calls.Add("screen") };
		var application = Provider(applicationSave);
		var block = Provider(blockSave);
		var screen = Provider(screenSave);
		var layers = new[] {
			new ApplicationCommandLayer(ApplicationCommandLevel.Screen, screen),
			new ApplicationCommandLayer(ApplicationCommandLevel.Application, application),
			new ApplicationCommandLayer(ApplicationCommandLevel.Block, block)
		};
		var composed = toolbar
			? Tools.UI.MergeApplicationToolBarItems<KeyedAction>(item => item.Key, layers)
			: Tools.UI.MergeApplicationMenus<TestApplicationMenu<KeyedAction>, KeyedAction>(
				(menu, items) => ActionMenu(menu.Text, items), menu => menu.Text, item => item.Key, layers).Single().Items;

		Assert.That(composed, Is.EqualTo(new[] { applicationSave, screenSave }));
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new ApplicationScreenHost(services);
		using var interactions = new ApplicationUserInterfaceServices(host, services);
		foreach (var item in composed)
			Assert.That(await interactions.ExecuteMenuItemAsync(item), Is.True);
		Assert.That(calls, Is.EqualTo(new[] { "application", "screen" }));
		Assert.That(host.ActiveBlock, Is.Null, "Global actions do not require a selected block.");
		Assert.That(application.ToolBarItems.Single(), Is.SameAs(applicationSave));
		Assert.That(block.Menus.Single().Items.Single(), Is.SameAs(blockSave));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AReusedApplicationCommandOverridesBlockContextWithoutDuplicatingTheGlobalCommand(bool toolbar) {
		var calls = new List<string>();
		var global = new KeyedAction { Key = "save", Title = "Shared save", Action = () => calls.Add("shared") };
		var contextual = new KeyedAction { Key = "save", Title = "Block save", Action = () => calls.Add("block") };
		var layers = new[] {
			new ApplicationCommandLayer(ApplicationCommandLevel.Application, Provider(global)),
			new ApplicationCommandLayer(ApplicationCommandLevel.Block, Provider(contextual)),
			new ApplicationCommandLayer(ApplicationCommandLevel.Screen, Provider(global))
		};
		var composed = toolbar
			? Tools.UI.MergeApplicationToolBarItems<KeyedAction>(item => item.Key, layers)
			: Tools.UI.MergeApplicationMenus<TestApplicationMenu<KeyedAction>, KeyedAction>(
				(menu, items) => ActionMenu(menu.Text, items), menu => menu.Text, item => item.Key, layers).Single().Items;
		Assert.That(composed.Single(), Is.SameAs(global));
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new ApplicationScreenHost(provider);
		Assert.That(await host.UserInterfaceServices.ExecuteMenuItemAsync(composed.Single()), Is.True);
		Assert.That(calls, Is.EqualTo(new[] { "shared" }));
		Assert.That(global.Key, Is.EqualTo("save"));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void CaptionBasedCompositionProtectsApplicationCommandsFromContextualNameCollisions(bool toolbar) {
		var global = new TestApplicationMenuItem { Title = "Save" };
		var contextual = new TestApplicationMenuItem { Title = "Save" };
		var application = new Commands { Menus = new[] { Menu("File", global) }, ToolBarItems = new[] { global } };
		var screen = new Commands { Menus = new[] { Menu("File", contextual) }, ToolBarItems = new[] { contextual } };
		var layers = new[] {
			new ApplicationCommandLayer(ApplicationCommandLevel.Application, application),
			new ApplicationCommandLayer(ApplicationCommandLevel.Screen, screen)
		};
		var composed = toolbar
			? Tools.UI.MergeApplicationToolBarItems<TestApplicationMenuItem>(layers)
			: Tools.UI.MergeApplicationMenus<TestApplicationMenu<TestApplicationMenuItem>, TestApplicationMenuItem>(
				(menu, items) => Menu(menu.Text, items), layers).Single().Items;
		Assert.That(composed, Is.EqualTo(new[] { global, contextual }));
	}

	[Test]
	public async Task ContextualLayersFollowTheHostedScreensOwnerAndFallBackToTheBrowsedBlock() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var owner = new TestApplicationBlock("Owner", new(typeof(CommandScreen)));
		var browsed = new TestApplicationBlock { Name = "Browsed" };
		using var host = new ApplicationScreenHost(new[] { owner, browsed }, provider);
		using var application = new CommandApplication(host);
		await host.SelectBlockAsync(browsed);
		var beforeActivation = Tools.UI.GetCommandLayers(application);
		Assert.That(beforeActivation.Select(layer => layer.Level), Is.EqualTo(new[] { ApplicationCommandLevel.Application, ApplicationCommandLevel.Block }));
		Assert.That(beforeActivation[1].Provider, Is.SameAs(browsed));
		var screen = await host.ActivateBlockAsync(owner);
		await host.SelectBlockAsync(browsed);
		var active = Tools.UI.GetCommandLayers(application);
		Assert.That(active.Select(layer => layer.Level), Is.EqualTo(new[] { ApplicationCommandLevel.Application, ApplicationCommandLevel.Block, ApplicationCommandLevel.Screen }));
		Assert.That(active[0].Provider, Is.SameAs(application));
		Assert.That(active[1].Provider, Is.SameAs(owner));
		Assert.That(active[2].Provider, Is.SameAs(screen));
		Assert.That(await host.ClearSelectionAsync(), Is.True);
		var cleared = Tools.UI.GetCommandLayers(application);
		Assert.That(cleared, Has.Length.EqualTo(2));
		Assert.That(cleared[1].Provider, Is.SameAs(browsed));
	}

	[Test]
	public void AnApplicationWithoutAHostStillContributesItsGlobalCommands() {
		using var application = new CommandApplication(null);
		var save = new TestApplicationMenuItem { Title = "Save" };
		application.SetMenus(new[] { Menu("File", save) });
		var layers = Tools.UI.GetCommandLayers(application);
		Assert.That(layers.Single().Level, Is.EqualTo(ApplicationCommandLevel.Application));
		Assert.That(layers.Single().Provider, Is.SameAs(application));
		Assert.That(Tools.UI.MergeApplicationMenus<TestApplicationMenu<TestApplicationMenuItem>, TestApplicationMenuItem>(
			(menu, items) => Menu(menu.Text, items), layers).Single().Items.Single(), Is.SameAs(save));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ScopedConvenienceApisRejectEmptyCommandCaptions(bool toolbar) {
		var empty = new TestApplicationMenuItem { Title = "" };
		var layer = new ApplicationCommandLayer(ApplicationCommandLevel.Application,
			new Commands { Menus = new[] { Menu("File", empty) }, ToolBarItems = new[] { empty } });
		if (toolbar)
			Assert.That(() => Tools.UI.MergeApplicationToolBarItems<TestApplicationMenuItem>(layer), Throws.ArgumentException);
		else
			Assert.That(() => Tools.UI.MergeApplicationMenus<TestApplicationMenu<TestApplicationMenuItem>, TestApplicationMenuItem>(
				(menu, items) => Menu(menu.Text, items), layer), Throws.ArgumentException);
	}

	private static TestApplicationMenu<TestApplicationMenuItem> Menu(string text, params TestApplicationMenuItem[] items) {
		var menu = new PresentationMenu<TestApplicationMenuItem> { Text = text };
		foreach (var item in items)
			menu.AddItem(item);
		return menu;
	}

	private static TestApplicationMenu<KeyedAction> ActionMenu(string text, params KeyedAction[] items) {
		var menu = new PresentationMenu<KeyedAction> { Text = text };
		foreach (var item in items)
			menu.AddItem(item);
		return menu;
	}

	private static Commands Provider(KeyedAction item) => new() { Menus = new[] { ActionMenu("File", item) }, ToolBarItems = new[] { item } };

	private sealed class Commands : IApplicationCommandProvider {
		public IApplicationMenu[] Menus { get; init; } = Array.Empty<IApplicationMenu>();
		public IApplicationMenuItem[] ToolBarItems { get; init; } = Array.Empty<IApplicationMenuItem>();
	}

	private sealed class PresentationMenu<TItem> : TestApplicationMenu<TItem>
		where TItem : class, IApplicationMenuItem {
		protected override bool OwnsItems => false;
	}

	private sealed class KeyedAction : ApplicationAction, IApplicationMenuItem {
		public IApplicationMenu Parent { get; set; }

		public string Key { get; init; }
		public string Title { get; init; }
	}

	private sealed class CommandApplication : SyncApplicationBase {
		public CommandApplication(IApplicationScreenHost host) {
			ScreenHost = host;
			if (host != null)
				ObserveScreenHost();
		}

		public override IApplicationPlugin[] LoadedPlugins => Array.Empty<IApplicationPlugin>();
		public override IApplicationScreenHost ScreenHost { get; }
	}

	private sealed class CommandScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
	}

	private sealed class Separator : IApplicationMenuSeparator {
		public IApplicationMenu Parent { get; set; }

		public string Title => "Separator";
	}
}
