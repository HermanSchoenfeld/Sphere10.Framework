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
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationScreenHostTests {
	[Test]
	public async Task AConflictingResolvedRoleIsRejectedBeforeChangingTheHostedSelection() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var owner = new TestApplicationBlock { Name = "Standalone" };
		using var host = new TestHost(provider, owner);
		var normal = new ApplicationScreenDefinition(typeof(ProbeScreen), "Editor", ScreenActivationMode.SingleInstance);
		var empty = new ApplicationScreenDefinition(typeof(ProbeScreen), "Placeholder", ScreenActivationMode.SingleInstance, ScreenKind.Empty);
		owner.AddScreenDefinition(normal);
		var selected = await host.OpenScreenAsync(owner, normal);
		var view = selected;
		owner.AddScreenDefinition(empty);

		Assert.That(async () => await host.OpenScreenAsync(owner, empty), Throws.InstanceOf<ArgumentException>());

		Assert.That(host.ActiveScreen, Is.SameAs(selected));
		Assert.That(host.Screens.Single(), Is.SameAs(selected));
		Assert.That(view.Deactivations, Is.Zero);
	}

	[Test]
	public async Task BaseInterfaceHostsTheActualScreenWithReferenceIdentity() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock { Name = "Standalone" };
		using IApplicationScreenHost host = new TestHost(provider, block);
		var definition = new ApplicationScreenDefinition(typeof(ProbeScreen), "Standalone view", ScreenActivationMode.SingleInstance);
		block.AddScreenDefinition(definition);

		var screen = await host.OpenScreenAsync(block, definition);

		Assert.That(screen, Is.InstanceOf<ProbeScreen>());
		Assert.That(screen.Title, Is.EqualTo("Standalone view"));
		Assert.That(host.GetScreenBlock(screen), Is.SameAs(block));
		Assert.That(host.ActiveScreen, Is.SameAs(screen));
		Assert.That(((IMultiApplicationScreenHost)host).OpenScreens.Single(), Is.SameAs(screen));
		Assert.That(block.Menus, Is.Empty);
	}

	[Test]
	public async Task RemovingAPermanentPolicyOwnerRecreatesTheRemainingAliasBeforeSelectingAnEmptyScreen() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var explicitOwner = CreateBlock("explicit", Item("permanent", typeof(PermanentScreen), ScreenActivationMode.PermanentSingleton));
		var aliasItem = Item("alias", typeof(PermanentScreen), mode: null);
		var aliasOwner = CreateBlock("alias", aliasItem);
		var placeholderOwner = CreateBlock("welcome", Item("empty", typeof(EmptyScreen), ScreenActivationMode.SingleInstance, ScreenKind.Empty));
		placeholderOwner.DefaultScreenDefinition = placeholderOwner.ScreenDefinitions.Single(definition => definition.ScreenType == typeof(EmptyScreen));
		using var host = await CreateHostAsync(provider, explicitOwner, aliasOwner, placeholderOwner);
		await host.InitializeAsync();
		var original = host.ActiveScreen;

		Assert.That(await host.UnregisterBlockAsync(explicitOwner), Is.True);

		Assert.That(host.GetScreenBlock(host.ActiveScreen), Is.SameAs(aliasOwner));
		Assert.That(host.ModeOf(host.ActiveScreen), Is.EqualTo(ScreenActivationMode.PermanentSingleton));
		Assert.That(host.ActiveScreen, Is.Not.SameAs(original));
		Assert.That(host.OpenScreens.Single(), Is.SameAs(host.ActiveScreen));
		Assert.That(host.KindOf(host.ActiveScreen), Is.EqualTo(ScreenKind.Normal));
	}

	[Test]
	public async Task ClearingSelectionHonorsTheLeaveGuardAndRetainsAHiddenSingleton() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = await CreateHostAsync(provider);
		await host.TrySetScreenModeAsync(ScreenMode.SingleView);
		var selected = await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "single"));
		var screen = selected;
		screen.Guard = _ => Task.FromResult(false);

		Assert.That(await host.ClearSelectionAsync(), Is.False);
		Assert.That(host.ActiveScreen, Is.SameAs(selected));
		Assert.That(screen.Deactivations, Is.Zero);
		screen.Guard = _ => Task.FromResult(true);
		Assert.That(await host.ClearSelectionAsync(), Is.True);
		Assert.That(host.ActiveScreen, Is.Null);
		Assert.That(host.Screens.Single(), Is.SameAs(selected));
		Assert.That(host.OpenScreens, Is.Empty);
		Assert.That(screen.Deactivations, Is.EqualTo(1));
	}

	[Test]
	public async Task ScreenSelectionNotificationsDistinguishTabSelectionFromScreenStateChanges() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = await CreateHostAsync(provider);
		var selectedScreens = new List<IApplicationScreen>();
		host.ScreenSelected += screen => selectedScreens.Add(screen);
		var selected = await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "single"));

		host.NotifyScreenChanged(selected);

		Assert.That(selectedScreens, Is.EqualTo(new[] { selected }));
		Assert.That(await host.ShowScreenAsync(selected), Is.True);
		Assert.That(selectedScreens, Is.EqualTo(new[] { selected, selected }));
	}

	[Test]
	public async Task SingletonReuseAcrossBlocksKeepsTheOriginalScreenAndOwningBlock() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = await CreateHostAsync(provider);
		var first = await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "single"));
		first.Title = "Record one";
		await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "multiple"));

		var reused = await host.OpenScreenAsync(Block(host, "second"), Definition(host, "second", "shared"));

		Assert.That(reused, Is.SameAs(first));
		Assert.That(host.GetScreenBlock(reused).Name, Is.EqualTo("first"));
		Assert.That(host.DefinitionOf(reused), Is.SameAs(Definition(host, "first", "single")));
		Assert.That(reused.Title, Is.EqualTo("Record one"));
		Assert.That(reused.Activations, Is.EqualTo(2));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task DefinitionOwnershipUsesTheExactReferenceBeforeInvokingAnyLeaveGuard(bool ownedByAnotherBlock) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var owned = new ApplicationScreenDefinition(typeof(ProbeScreen), "Same", ScreenActivationMode.SingleInstance);
		var alias = new ApplicationScreenDefinition(typeof(ProbeScreen), "Same", ScreenActivationMode.SingleInstance);
		var first = new TestApplicationBlock("First", owned);
		var second = new TestApplicationBlock { Name = "Second" };
		if (ownedByAnotherBlock)
			second.AddScreenDefinition(alias);
		using var host = new TestHost(provider, first, second);
		var selected = await host.OpenScreenAsync(first, owned);
		selected.Guard = _ => Task.FromResult(false);

		Assert.That(async () => await host.OpenScreenAsync(first, alias), Throws.ArgumentException);

		Assert.That(host.ActiveScreen, Is.SameAs(selected));
		Assert.That(host.DefinitionOf(selected), Is.SameAs(owned));
		Assert.That(selected.GuardCalls, Is.Zero);
		Assert.That(selected.Deactivations, Is.Zero);
	}

	[TestCase(ScreenActivationMode.SingleInstance, false)]
	[TestCase(ScreenActivationMode.SingleInstance, true)]
	[TestCase(ScreenActivationMode.PermanentSingleton, false)]
	[TestCase(ScreenActivationMode.PermanentSingleton, true)]
	public async Task AnUndeclaredAliasInheritsTheExplicitPolicyForItsScreenType(ScreenActivationMode declaredMode, bool explicitFirst) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var undeclared = Item("undeclared", typeof(ProbeScreen), mode: null);
		var block = CreateBlock("records", undeclared, Item("declared", typeof(ProbeScreen), declaredMode));
		using var host = await CreateHostAsync(provider, block);

		var first = await host.OpenScreenAsync(Block(host, "records"), Definition(host, "records", explicitFirst ? "declared" : "undeclared"));
		var second = await host.OpenScreenAsync(Block(host, "records"), Definition(host, "records", explicitFirst ? "undeclared" : "declared"));

		Assert.That(host.ModeOf(first), Is.EqualTo(declaredMode), "A screen type has one policy even when an alias omits it.");
		Assert.That(second, Is.SameAs(first));
		Assert.That(host.Screens, Has.Length.EqualTo(1));
	}

	[Test]
	public async Task BatchClosePreflightsEveryGuardBeforeRemovingOrDeactivatingAnything() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = await CreateHostAsync(provider);
		var first = await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "multiple"));
		var firstScreen = first;
		var second = await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "multiple"));
		var secondScreen = second;
		secondScreen.Guard = _ => Task.FromResult(false);
		var before = host.OpenScreens.Select(screen => screen).ToArray();
		var firstDeactivations = firstScreen.Deactivations;

		Assert.That(await host.CloseScreensAsync(new[] { first, second, first }), Is.False);
		Assert.That(host.OpenScreens.Select(screen => screen), Is.EqualTo(before));
		Assert.That(host.ActiveScreen, Is.SameAs(second));
		Assert.That(firstScreen.Deactivations, Is.EqualTo(firstDeactivations));
		Assert.That(secondScreen.Deactivations, Is.Zero);
		secondScreen.Guard = _ => Task.FromResult(true);
		Assert.That(await host.CloseScreensAsync(new[] { first, second, first }), Is.True);
		Assert.That(host.Screens, Is.Empty);
		Assert.That(host.ActiveScreen, Is.Null);
		Assert.That(secondScreen.Deactivations, Is.EqualTo(1));
	}

	[Test]
	public async Task GuardCancellationPreservesSelectionAndReleasesTheTransitionGate() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = await CreateHostAsync(provider);
		var first = await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "single"));
		using var cancellation = new CancellationTokenSource();
		var screen = first;
		screen.Guard = _ => {
			cancellation.Cancel();
			return Task.FromResult(true);
		};

		Assert.That(async () => await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "multiple"), cancellationToken: cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(host.ActiveScreen, Is.SameAs(first));
		Assert.That(host.Screens.Single(), Is.SameAs(first));
		Assert.That(screen.Deactivations, Is.Zero);
		screen.Guard = _ => Task.FromResult(true);
		Assert.That(await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "multiple")), Is.Not.Null);
	}

	[Test]
	public async Task QueuedTransitionsWaitForAnAsynchronousGuard() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = await CreateHostAsync(provider);
		var selected = await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "single"));
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var releaseScope = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult(true));
		selected.Guard = _ => {
			entered.TrySetResult();
			return release.Task;
		};
		var firstTransition = host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "multiple"));
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var secondTransition = host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "multiple"));

		Assert.That(secondTransition.IsCompleted, Is.False);
		Assert.That(host.ActiveScreen, Is.SameAs(selected));
		release.SetResult(true);
		var results = await Task.WhenAll(firstTransition, secondTransition).WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(results.Select(screen => screen).Distinct().Count(), Is.EqualTo(2));
		Assert.That(host.OpenScreens, Has.Length.EqualTo(3));
	}

	[Test]
	public async Task HiddenSingletonsParticipateInUnsavedStateAndNavigationGuards() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = await CreateHostAsync(provider);
		await host.TrySetScreenModeAsync(ScreenMode.SingleView);
		var first = await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "single"));
		var screen = first;
		await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "multiple"));
		screen.HasUnsavedChanges = true;
		screen.Guard = _ => Task.FromResult(false);
		host.NotifyScreenChanged(first);

		Assert.That(host.OpenScreens, Has.Length.EqualTo(1));
		Assert.That(host.Screens, Has.Length.EqualTo(2));
		Assert.That(host.HasUnsavedChanges, Is.True);
		Assert.That(await host.CanNavigateAsync(), Is.False);
		screen.HasUnsavedChanges = false;
		screen.Guard = _ => Task.FromResult(true);
		Assert.That(host.HasUnsavedChanges, Is.False);
		Assert.That(await host.CanNavigateAsync(), Is.True);
	}

	[Test]
	public async Task ClosingTheActiveScreenSelectsItsNeighborWithoutTakingViewOwnership() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = await CreateHostAsync(provider);
		var first = await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "single"));
		var firstScreen = first;
		var second = await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "multiple"));
		var secondScreen = second;

		Assert.That(await host.CloseScreenAsync(second), Is.True);
		Assert.That(host.ActiveScreen, Is.SameAs(first));
		Assert.That(firstScreen.Activations, Is.EqualTo(2));
		Assert.That(secondScreen.Deactivations, Is.EqualTo(1));
		Assert.That(secondScreen.Disposals, Is.Zero);
		host.Dispose();
		Assert.That(firstScreen.Disposals, Is.Zero);
	}

	[Test]
	public async Task ReturnedMembershipArraysCannotReplaceRetainedOrOpenScreens() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = await CreateHostAsync(provider);
		var selected = await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "single"));
		var screens = host.Screens;
		var openScreens = host.OpenScreens;
		screens[0] = null;
		openScreens[0] = null;

		Assert.That(host.Screens.Single(), Is.SameAs(selected));
		Assert.That(host.OpenScreens.Single(), Is.SameAs(selected));
		Assert.That(await host.CloseScreenAsync(selected), Is.True);
		Assert.That(openScreens, Has.Length.EqualTo(1), "Earlier snapshots keep their own membership.");
	}

	[Test]
	public async Task PermanentScreensPreventPlaceholderSelectionAndRejectOrdinaryClosure() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = CreateBlock("startup", Item("welcome", typeof(EmptyScreen), ScreenActivationMode.SingleInstance, ScreenKind.Empty),
			Item("permanent", typeof(PermanentScreen), ScreenActivationMode.PermanentSingleton));
		block.DefaultScreenDefinition = block.ScreenDefinitions.Single(definition => definition.ScreenType == typeof(EmptyScreen));
		using var host = await CreateHostAsync(provider, block);

		await host.InitializeAsync();
		var permanent = host.OpenScreens.Single();
		await host.InitializeAsync();

		Assert.That(host.ModeOf(permanent), Is.EqualTo(ScreenActivationMode.PermanentSingleton));
		Assert.That(host.ActiveScreen, Is.SameAs(permanent));
		Assert.That(host.Screens.Single(), Is.SameAs(permanent));
		Assert.That(await host.OpenScreenAsync(Block(host, "startup"), Definition(host, "startup", "welcome")), Is.Null);
		Assert.That(await host.CanCloseScreenAsync(permanent), Is.False);
		Assert.That(await host.CloseScreenAsync(permanent), Is.False);
		Assert.That(await host.CloseScreensAsync(new[] { permanent }), Is.False);
	}

	[TestCase(ScreenActivationMode.SingleInstance, true)]
	[TestCase(ScreenActivationMode.MultiInstance, false)]
	public async Task EmptyScreenReturnsWhenTheLastNormalScreenCloses(ScreenActivationMode lifetime, bool reuseExpected) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = CreateBlock("workspace", Item("welcome", typeof(EmptyScreen), lifetime, ScreenKind.Empty), Item("editor", typeof(OtherScreen)));
		block.DefaultScreenDefinition = block.ScreenDefinitions.Single(definition => definition.ScreenType == typeof(EmptyScreen));
		using var host = await CreateHostAsync(provider, block);
		await host.InitializeAsync();
		var welcome = host.ActiveScreen;
		Assert.That(host.OpenScreens, Is.Empty);

		var editor = await host.OpenScreenAsync(Block(host, "workspace"), Definition(host, "workspace", "editor"));
		Assert.That(host.OpenScreens.Single(), Is.SameAs(editor));
		Assert.That(await host.CloseScreenAsync(editor), Is.True);
		Assert.That(host.KindOf(host.ActiveScreen), Is.EqualTo(ScreenKind.Empty));
		Assert.That(ReferenceEquals(host.ActiveScreen, welcome), Is.EqualTo(reuseExpected));
		Assert.That(host.OpenScreens, Is.Empty);
	}

	[Test]
	public async Task SwitchingToSingleViewIsAtomicWhenAnInactiveScreenVetoesClosing() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = await CreateHostAsync(provider);
		var inactive = await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "single"));
		var screen = inactive;
		var selected = await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "multiple"));
		screen.Guard = _ => Task.FromResult(false);

		Assert.That(await host.TrySetScreenModeAsync(ScreenMode.SingleView), Is.False);
		Assert.That(host.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
		Assert.That(host.OpenScreens, Has.Length.EqualTo(2));
		Assert.That(host.ActiveScreen, Is.SameAs(selected));
		screen.Guard = _ => Task.FromResult(true);
		Assert.That(await host.TrySetScreenModeAsync(ScreenMode.SingleView), Is.True);
		Assert.That(host.ScreenMode, Is.EqualTo(ScreenMode.SingleView));
		Assert.That(host.OpenScreens.Single(), Is.SameAs(selected));
	}

	[Test]
	public async Task MovingTabsPreservesTheSelectedScreenAndDoesNotRunLifecycleCallbacks() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = await CreateHostAsync(provider);
		var first = await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "multiple"));
		var second = await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "multiple"));
		var screen = second;

		await host.MoveScreenAsync(second, 0);

		Assert.That(host.OpenScreens.Select(screen => screen), Is.EqualTo(new[] { second, first }));
		Assert.That(host.ActiveScreen, Is.SameAs(second));
		Assert.That(screen.Activations, Is.EqualTo(1));
		Assert.That(screen.Deactivations, Is.Zero);
		Assert.That(screen.GuardCalls, Is.Zero);
	}

	[Test]
	public async Task DisposalCancelsPendingGuardsAndLeavesPlatformViewsOwnedByTheirAdapter() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = await CreateHostAsync(provider);
		var selected = await host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "single"));
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var screen = new ProbeScreen { Guard = async token => {
			entered.SetResult();
			await Task.Delay(Timeout.Infinite, token);
			return true;
		} };
		selected.Guard = screen.Guard;
		screen = selected;
		var transition = host.OpenScreenAsync(Block(host, "first"), Definition(host, "first", "multiple"));
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

		host.Dispose();

		Assert.That(async () => await transition, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(host.ActiveScreen, Is.Null);
		Assert.That(host.Screens, Is.Empty);
		Assert.That(screen.Disposals, Is.Zero);
	}

	[Test]
	public async Task BaseInterfaceAndDecoratorDispatchThroughTheOverriddenTypedActivationMethod() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = CreateBlock("records", Item("editor", typeof(OtherScreen)));
		using var typedHost = new TrackingHost(new[] { block }, provider);
		using IApplicationScreenHost host = new TrackingDecorator(typedHost);
		var definition = Tools.UI.GetScreenDefinitions(new[] { block }).Single();
		var notifications = 0;
		EventHandlerEx handler = () => notifications++;
		host.Changed += handler;

		var screen = await host.OpenScreenAsync(block, definition);

		Assert.That(typedHost.ActivationCalls, Is.EqualTo(1));
		Assert.That(((TrackingDecorator)host).ActivationCalls, Is.EqualTo(1));
		Assert.That(host.ActiveScreen, Is.SameAs(typedHost.ActiveScreen));
		Assert.That(host.ActiveScreen, Is.SameAs(screen));
		Assert.That(notifications, Is.GreaterThan(0));
		var beforeRemoval = notifications;
		host.Changed -= handler;
		await host.OpenScreenAsync(block, definition);
		Assert.That(notifications, Is.EqualTo(beforeRemoval), "The decorator removes the subscription from the underlying host.");
	}

	private static Task<TestHost> CreateHostAsync(IServiceProvider provider, params TestApplicationBlock[] blocks) {
		if (blocks.Length == 0)
			blocks = new[] {
				CreateBlock("first", Item("single", typeof(ProbeScreen), ScreenActivationMode.SingleInstance), Item("multiple", typeof(OtherScreen))),
				CreateBlock("second", Item("shared", typeof(ProbeScreen), ScreenActivationMode.SingleInstance))
			};
		return Task.FromResult(new TestHost(provider, blocks));
	}

	private static TestApplicationBlock Block(TestHost host, string name) => host.AvailableBlocks.Single(block => block.Name == name);

	private static ApplicationScreenDefinition Definition(TestHost host, string blockName, string title) =>
		Block(host, blockName).ScreenDefinitions.Single(definition => definition.Title == title);

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

	private sealed class TrackingHost(IEnumerable<TestApplicationBlock> blocks, IServiceProvider provider) : TestHost(provider, blocks.ToArray()) {
		public int ActivationCalls { get; private set; }

		public override Task<ProbeScreen> OpenScreenAsync(TestApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default) {
			ActivationCalls++;
			return base.OpenScreenAsync(block, definition, cancellationToken);
		}
	}

	private sealed class TrackingDecorator(IApplicationScreenHost host) : ApplicationScreenHostDecorator(host) {
		public int ActivationCalls { get; private set; }

		public override Task<IApplicationScreen> OpenScreenAsync(IApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default) {
			ActivationCalls++;
			return base.OpenScreenAsync(block, definition, cancellationToken);
		}
	}

	private class TestHost : ApplicationScreenHost<TestApplicationBlock, ProbeScreen> {
		public TestHost(IServiceProvider provider, params TestApplicationBlock[] blocks) : base(blocks, provider) {
			Definitions = Tools.UI.GetScreenDefinitions(blocks);
		}

		public ApplicationScreenDefinition[] Definitions { get; }

		public ApplicationScreenDefinition DefinitionOf(ProbeScreen screen) => GetScreenDefinition(screen);
		public ScreenActivationMode ModeOf(ProbeScreen screen) => GetScreenActivationMode(screen);
		public ScreenKind KindOf(ProbeScreen screen) => GetScreenKind(screen);

		protected override ProbeScreen CreateScreen(TestApplicationBlock block, ApplicationScreenDefinition definition) {
			var screen = (ProbeScreen)Activator.CreateInstance(definition.ScreenType, nonPublic: true);
			screen.Title = definition.Title;
			return screen;
		}
	}

	private class ProbeScreen : IApplicationScreen, IDisposable {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
		public string Title { get; set; }
		public bool HasUnsavedChanges { get; set; }
		public Func<CancellationToken, Task<bool>> Guard { get; set; } = _ => Task.FromResult(true);
		public Func<CancellationToken, Task> Activation { get; set; } = _ => Task.CompletedTask;
		public int GuardCalls { get; private set; }
		public int Activations { get; private set; }
		public int Deactivations { get; private set; }
		public int Disposals { get; private set; }

		public async Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) {
			GuardCalls++;
			return await Guard(cancellationToken);
		}

		public async Task OnActivatedAsync(CancellationToken cancellationToken = default) {
			Activations++;
			await Activation(cancellationToken);
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
