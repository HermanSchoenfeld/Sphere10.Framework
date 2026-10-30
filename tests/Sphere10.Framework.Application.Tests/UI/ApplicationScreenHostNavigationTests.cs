// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationScreenHostNavigationTests {
	[Test]
	public async Task BatchCloseSelectsTheFirstSurvivingSuccessorInTheOriginalTabOrder() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var screens = new ProbeScreen[5];
		for (var index = 0; index < screens.Length; index++)
			screens[index] = await host.ActivateScreenAsync(Block(host, "first"), ScreenItemFor(host, "first", "multiple"));
		await host.ShowScreenAsync(screens[2]);

		Assert.That(await host.CloseScreensAsync(new[] { screens[0], screens[2] }), Is.True);

		Assert.That(host.ActiveScreen, Is.SameAs(screens[3]));
		Assert.That(host.OpenScreens, Is.EqualTo(new[] { screens[1], screens[3], screens[4] }));
	}

	[Test]
	public async Task ClosingAnInactiveTabPreservesTheCurrentSelectionAndActivationCount() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var first = await host.ActivateScreenAsync(Block(host, "first"), ScreenItemFor(host, "first", "single"));
		var active = await host.ActivateScreenAsync(Block(host, "first"), ScreenItemFor(host, "first", "multiple"));
		var activations = active.Activations;

		Assert.That(await host.CloseScreenAsync(first), Is.True);

		Assert.That(host.ActiveScreen, Is.SameAs(active));
		Assert.That(active.Activations, Is.EqualTo(activations));
	}

	[Test]
	public async Task ClosingTheLastTabPublishesANullSelection() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var screen = await host.ActivateScreenAsync(Block(host, "first"), ScreenItemFor(host, "first", "single"));
		IApplicationScreen selected = screen;
		host.ScreenSelected += next => selected = next;

		Assert.That(await host.CloseScreenAsync(screen), Is.True);

		Assert.That(host.ActiveScreen, Is.Null);
		Assert.That(selected, Is.Null);
		Assert.That(host.OpenScreens, Is.Empty);
	}

	[Test]
	public async Task AnUnregisteredBlockCannotHostAScreenOrChangeApplicationNavigationState() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		await host.SelectBlockAsync(Block(host, "second"));
		var standalone = new TestApplicationBlock { Name = "Standalone" };
		var definition = new ApplicationScreenDefinition(typeof(ProbeScreen), "Viewer", ScreenActivationMode.SingleInstance);
		standalone.AddScreenDefinition(definition);

		Assert.That(async () => await host.OpenScreenAsync(standalone, definition), Throws.ArgumentException);

		Assert.That(host.ActiveScreen, Is.Null);
		Assert.That(host.Screens, Is.Empty);
		Assert.That(host.ActiveBlock.Name, Is.EqualTo("second"));
		Assert.That(host.Blocks.Select(block => block.Name), Is.EqualTo(new[] { "first", "second" }));
		Assert.That(host.AvailableBlocks, Has.Length.EqualTo(2));
	}

	[TestCase(ScreenMode.SingleView)]
	[TestCase(ScreenMode.MultiView)]
	public async Task BrowsingAnotherBlockPreservesScreenOwnershipAndSkipsItsLeaveGuard(ScreenMode mode) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		await host.TrySetScreenModeAsync(mode);
		var session = await host.ActivateScreenAsync(Block(host, "first"), ScreenItemFor(host, "first", "single"));
		var screen = (ProbeScreen)session;
		screen.Guard = _ => Task.FromResult(false);
		var notifications = 0;
		host.Changed += () => notifications++;

		await host.SelectBlockAsync(Block(host, "second"));
		await host.SelectBlockAsync(Block(host, "second"));

		Assert.That(host.ActiveBlock.Name, Is.EqualTo("second"));
		Assert.That(host.ActiveScreen, Is.SameAs(session));
		Assert.That(host.GetScreenBlock(session).Name, Is.EqualTo("first"));
		Assert.That(screen.GuardCalls, Is.Zero);
		Assert.That(screen.Activations, Is.EqualTo(1));
		Assert.That(screen.Deactivations, Is.Zero);
		Assert.That(notifications, Is.EqualTo(1));
		host.NotifyScreenChanged(session);
		Assert.That(host.ActiveBlock.Name, Is.EqualTo("second"), "Reporting dirty state must not change the browsed block.");
		Assert.That(await host.ShowScreenAsync(session), Is.True);
		Assert.That(host.ActiveBlock.Name, Is.EqualTo("first"), "Selecting the same tab restores navigation to its owner.");
		Assert.That(screen.GuardCalls, Is.Zero);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ExplicitBlockActivationUsesItsDefaultWhetherOrNotItHasAMatchingMenuEntry(bool registeredInMenu) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var definition = new ApplicationScreenDefinition(typeof(ProbeScreen), "Home dashboard", ScreenActivationMode.SingleInstance);
		var block = registeredInMenu ? CreateBlock("home", new ScreenItem { Title = "Home", ScreenDefinition = definition }) : CreateBlock("home");
		if (!registeredInMenu)
			block.AddScreenDefinition(definition);
		block.DefaultScreenDefinition = definition;
		using var host = CreateHost(provider, block);

		var selected = await host.ActivateBlockAsync(Block(host, "home"));

		Assert.That(selected, Is.Not.Null);
		Assert.That(selected.GetType(), Is.EqualTo(typeof(ProbeScreen)));
		Assert.That(selected.Title, Is.EqualTo("Home dashboard"));
		Assert.That(host.DefinitionOf(selected), Is.SameAs(definition));
	}

	[Test]
	public async Task BlockDefaultTakesPrecedenceOverAnEarlierDeclaredMenuScreen() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var earlier = Item("earlier", typeof(OtherScreen));
		var block = CreateBlock("home", earlier, Item("home-view", typeof(ProbeScreen), ScreenActivationMode.SingleInstance));
		block.DefaultScreenDefinition = block.ScreenDefinitions.Single(definition => definition.ScreenType == typeof(ProbeScreen));
		using var host = CreateHost(provider, block);

		var selected = await host.ActivateBlockAsync(Block(host, "home"));

		Assert.That(host.DefinitionOf(selected).Title, Is.EqualTo("home-view"));
		Assert.That(selected.GetType(), Is.EqualTo(typeof(ProbeScreen)));
	}

	[Test]
	public async Task BlockRemovalPreflightsPermanentScreensAndRestorationOpensThemAgain() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = CreateBlock("administration", Item("permanent", typeof(PermanentScreen), ScreenActivationMode.PermanentSingleton));
		using var host = CreateHost(provider, block);
		await host.InitializeAsync();
		var initial = host.ActiveScreen;
		var screen = initial;
		screen.Guard = _ => Task.FromResult(false);

		Assert.That(await host.UnregisterBlockAsync(Block(host, "administration")), Is.False);
		Assert.That(host.Blocks.Single(), Is.SameAs(block));
		Assert.That(host.ActiveScreen, Is.SameAs(initial));
		screen.Guard = _ => Task.FromResult(true);
		Assert.That(await host.UnregisterBlockAsync(Block(host, "administration")), Is.True);
		Assert.That(host.Screens, Is.Empty);
		Assert.That(host.Blocks, Is.Empty);
		Assert.That(host.AvailableBlocks.Single(), Is.SameAs(block), "Removal keeps startup definitions available for restoration.");
		await host.RegisterBlockAsync(Block(host, "administration"));
		Assert.That(host.Blocks.Single(), Is.SameAs(block));
		Assert.That(host.ModeOf(host.OpenScreens.Single()) == ScreenActivationMode.PermanentSingleton, Is.True);
		Assert.That(host.ActiveScreen, Is.Not.SameAs(initial));
	}

	[Test]
	public async Task MembershipNotificationsObserveCommittedBlockAndPermanentScreenRegistration() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = CreateBlock("administration", Item("permanent", typeof(PermanentScreen), ScreenActivationMode.PermanentSingleton));
		using var host = CreateHost(provider, block);
		await host.InitializeAsync();
		var notifications = 0;
		host.Changed += () => {
			notifications++;
			Assert.That(host.Blocks.Any(candidate => ReferenceEquals(candidate, block)),
				Is.EqualTo(host.Screens.Any(session => ReferenceEquals(host.GetScreenBlock(session), block))),
				"Navigation membership and hosted permanent screens must change atomically for UI observers.");
		};

		Assert.That(await host.UnregisterBlockAsync(block), Is.True);
		Assert.That(notifications, Is.EqualTo(1));
		await host.RegisterBlockAsync(block);
		Assert.That(notifications, Is.EqualTo(2));
	}

	[Test]
	public async Task RegisteringPermanentScreensHonorsTheCurrentPlaceholderGuardBeforeAddingABlock() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var welcome = CreateBlock("welcome", Item("empty", typeof(EmptyScreen), ScreenActivationMode.SingleInstance, ScreenKind.Empty));
		welcome.DefaultScreenDefinition = welcome.ScreenDefinitions.Single(definition => definition.ScreenType == typeof(EmptyScreen));
		var administration = CreateBlock("administration", Item("permanent", typeof(PermanentScreen), ScreenActivationMode.PermanentSingleton));
		using var host = CreateHost(provider, welcome, administration);
		Assert.That(await host.UnregisterBlockAsync(Block(host, "administration")), Is.True);
		await host.InitializeAsync();
		var selected = host.ActiveScreen;
		var screen = selected;
		screen.Guard = _ => Task.FromResult(false);

		await host.RegisterBlockAsync(Block(host, "administration"));

		Assert.That(host.Blocks.Select(block => block.Name), Is.EqualTo(new[] { "welcome" }));
		Assert.That(host.ActiveScreen, Is.SameAs(selected));
		Assert.That(host.OpenScreens, Is.Empty);
		Assert.That(screen.Deactivations, Is.Zero);
		screen.Guard = _ => Task.FromResult(true);
		await host.RegisterBlockAsync(Block(host, "administration"));
		Assert.That(host.Blocks, Has.Length.EqualTo(2));
		Assert.That(host.ModeOf(host.ActiveScreen) == ScreenActivationMode.PermanentSingleton, Is.True);
	}

	[Test]
	public async Task ReturnedBlockMembershipCannotAlterRegistrationOrAvailableDefinitions() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var registered = host.Blocks;
		var available = host.AvailableBlocks;
		registered[0] = null;
		available[0] = null;

		Assert.That(host.Blocks, Has.Length.EqualTo(2));
		Assert.That(host.Blocks, Has.None.Null);
		Assert.That(host.AvailableBlocks, Has.None.Null);
		Assert.That(await host.ActivateBlockAsync(Block(host, "first")), Is.Not.Null);
		Assert.That(await host.UnregisterBlockAsync(Block(host, "first")), Is.True);
		Assert.That(host.AvailableBlocks, Has.Length.EqualTo(2));
		Assert.That(registered, Has.Length.EqualTo(2));
	}

	private static TestHost CreateHost(IServiceProvider provider, params TestApplicationBlock[] blocks) {
		if (blocks.Length == 0)
			blocks = new[] {
				CreateBlock("first", Item("single", typeof(ProbeScreen), ScreenActivationMode.SingleInstance), Item("multiple", typeof(OtherScreen))),
				CreateBlock("second", Item("shared", typeof(ProbeScreen), ScreenActivationMode.SingleInstance))
			};
		return new TestHost(blocks, provider);
	}

	private static TestApplicationBlock Block(TestHost host, string id) => host.AvailableBlocks.Single(block => block.Name == id);
	private static IScreenMenuItem ScreenItemFor(TestHost host, string blockId, string itemId) =>
		(IScreenMenuItem)Block(host, blockId).Menus.SelectMany(menu => menu.Items).Single(item => item.Title == itemId);

	private sealed class TestHost(TestApplicationBlock[] blocks, IServiceProvider provider) : ApplicationScreenHost<TestApplicationBlock, ProbeScreen>(blocks, provider) {
		public ApplicationScreenDefinition DefinitionOf(ProbeScreen screen) => GetScreenDefinition(screen);
		public ScreenActivationMode ModeOf(ProbeScreen screen) => GetScreenActivationMode(screen);

		protected override ProbeScreen CreateScreen(TestApplicationBlock block, ApplicationScreenDefinition definition) {
			var screen = (ProbeScreen)Activator.CreateInstance(definition.ScreenType, nonPublic: true);
			screen.Title = definition.Title;
			return screen;
		}
	}

	private static TestApplicationBlock CreateBlock(string id, params ScreenItem[] items) {
		var block = new TestApplicationBlock { Name = id };
		var menu = new TestApplicationMenu<ScreenItem> { Text = "Screens" };
		foreach (var item in items) {
			menu.AddItem(item);
			block.AddScreenDefinition(item.ScreenDefinition);
		}
		block.AddMenu(menu);
		return block;
	}

	private static ScreenItem Item(string id, Type screenType, ScreenActivationMode? mode = ScreenActivationMode.MultiInstance, ScreenKind kind = ScreenKind.Normal) =>
		new() { Title = id, ScreenDefinition = new(screenType, id, mode, kind) };

	private sealed class ScreenItem : TestApplicationMenuItem, IScreenMenuItem {
		public ApplicationScreenDefinition ScreenDefinition { get; set; }
	}

	private class ProbeScreen : IApplicationScreen, IDisposable {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
		public string Title { get; set; }
		public bool HasUnsavedChanges { get; set; }
		public Func<CancellationToken, Task<bool>> Guard { get; set; } = _ => Task.FromResult(true);
		public int GuardCalls { get; private set; }
		public int Activations { get; private set; }
		public int Deactivations { get; private set; }
		public int Disposals { get; private set; }

		public async Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) {
			GuardCalls++;
			return await Guard(cancellationToken);
		}

		public Task OnActivatedAsync(CancellationToken cancellationToken = default) {
			Activations++;
			return Task.CompletedTask;
		}

		public Task OnDeactivatedAsync(CancellationToken cancellationToken = default) {
			Deactivations++;
			return Task.CompletedTask;
		}

		public void Dispose() => Disposals++;
	}

	private sealed class OtherScreen : ProbeScreen {
	}

	private sealed class EmptyScreen : ProbeScreen {
	}

	private sealed class PermanentScreen : ProbeScreen {
	}
}
