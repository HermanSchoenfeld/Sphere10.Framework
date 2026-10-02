// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.


using Sphere10.Framework.Application.UI;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationScreenHostTests {
	[TestCase(ScreenMode.SingleView)]
	[TestCase(ScreenMode.MultiView)]
	public async Task SelectingNavigationKeepsTheActiveScreenAndDoesNotRunItsGuardOrLifecycle(ScreenMode mode) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(), provider);
		await host.TrySetScreenModeAsync(mode);
		var session = await host.ActivateScreenAsync("first", "single");
		var guards = 0;
		var component = new ProbeScreen { Guard = _ => { guards++; return Task.FromResult(false); } };
		await host.AttachScreenAsync(session.Id, component);
		var notifications = 0;
		host.Changed += () => notifications++;

		await host.SelectBlockAsync("second");
		await host.SelectBlockAsync("second");
		Assert.That(host.ActiveBlock.Id, Is.EqualTo("second"));
		Assert.That(host.ActiveScreen, Is.SameAs(session));
		Assert.That(host.ActiveScreen.Block.Id, Is.EqualTo("first"));
		Assert.That(session.Screen, Is.SameAs(component));
		Assert.That(host.Screens.Single(), Is.SameAs(session));
		Assert.That(host.OpenScreens.Single(), Is.SameAs(session));
		Assert.That(guards, Is.Zero);
		Assert.That(component.Activations, Is.EqualTo(1));
		Assert.That(component.Deactivations, Is.Zero);
		Assert.That(component.Disposals, Is.Zero);
		Assert.That(notifications, Is.EqualTo(1), "Selecting the same navigation block is idempotent.");

		Assert.That(await host.ShowScreenAsync(session.Id), Is.True);
		Assert.That(host.ActiveBlock.Id, Is.EqualTo("first"), "Selecting an already active tab restores its navigation block.");
		Assert.That(guards, Is.Zero);
		Assert.That(component.Activations, Is.EqualTo(1));
	}

	[Test]
	public async Task NavigationSelectionDoesNotActivateDefaultsAndRejectsInvalidOrCancelledRequests() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(), provider);
		await host.SelectBlockAsync("first");
		Assert.That(host.ActiveBlock.Id, Is.EqualTo("first"));
		Assert.That(host.ActiveScreen, Is.Null);
		Assert.That(host.Screens, Is.Empty);
		Assert.That(async () => await host.SelectBlockAsync("missing"), Throws.ArgumentException);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		Assert.That(async () => await host.SelectBlockAsync("second", cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(host.ActiveBlock.Id, Is.EqualTo("first"));
		Assert.That(await host.ActivateBlockAsync("first"), Is.Not.Null, "Explicit block activation retains default-screen behavior.");
	}

	[Test]
	public async Task ClosingLastScreenKeepsTheBrowsedBlockAndUnregisteringItSelectsAnAvailableBlock() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(), provider);
		var session = await host.ActivateScreenAsync("first", "single");
		await host.SelectBlockAsync("second");
		Assert.That(await host.CloseScreenAsync(session.Id), Is.True);
		Assert.That(host.ActiveScreen, Is.Null);
		Assert.That(host.ActiveBlock.Id, Is.EqualTo("second"));
		Assert.That(await host.UnregisterBlockAsync("second"), Is.True);
		Assert.That(host.ActiveBlock.Id, Is.EqualTo("first"));
		Assert.That(host.Screens, Is.Empty);
	}

	[Test]
	public async Task ReturnedArraysCannotRemoveRegisteredBlocksOrOpenSessions() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(), provider);
		var session = await host.ActivateScreenAsync("first", "single");
		var blocks = host.Blocks;
		var screens = host.OpenScreens;
		blocks[0] = null;
		screens[0] = null;

		Assert.That(host.Blocks, Has.Length.EqualTo(2));
		Assert.That(host.Blocks, Has.None.Null);
		Assert.That(host.OpenScreens.Single(), Is.SameAs(session));
		Assert.That(await host.CloseScreenAsync(session.Id), Is.True);
		Assert.That(host.OpenScreens, Is.Empty);
	}

	[Test]
	public async Task SingleInstanceIsRetainedAcrossBlocksAndMultipleInstancesAreIndependent() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(), provider);
		var single = await host.ActivateScreenAsync("first", "single");
		var component = new ProbeScreen();
		await host.AttachScreenAsync(single.Id, component);
		var firstMultiple = await host.ActivateScreenAsync("first", "multiple");
		var secondMultiple = await host.ActivateScreenAsync("first", "multiple");
		var selected = await host.ActivateScreenAsync("second", "shared");

		Assert.That(selected, Is.SameAs(single));
		Assert.That(selected.Screen, Is.SameAs(component));
		Assert.That(selected.Block.Id, Is.EqualTo("first"));
		Assert.That(firstMultiple.Id, Is.Not.EqualTo(secondMultiple.Id));
		Assert.That(host.OpenScreens, Has.Length.EqualTo(3));
	}

	[Test]
	public async Task HiddenAttachmentDoesNotActivateUntilShown() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(), provider);
		var first = await host.ActivateScreenAsync("first", "single");
		var second = await host.ActivateScreenAsync("first", "multiple");
		var firstComponent = new ProbeScreen();
		var secondComponent = new OtherScreen();
		await host.AttachScreenAsync(first.Id, firstComponent);
		await host.AttachScreenAsync(second.Id, secondComponent);

		Assert.That(firstComponent.Activations, Is.Zero);
		Assert.That(secondComponent.Activations, Is.EqualTo(1));
		await host.ShowScreenAsync(first.Id);
		Assert.That(firstComponent.Activations, Is.EqualTo(1));
		Assert.That(secondComponent.Deactivations, Is.EqualTo(1));
		await host.ShowScreenAsync(first.Id);
		Assert.That(firstComponent.Activations, Is.EqualTo(1));
	}

	[Test]
	public async Task GuardVetoPreservesSelectionAndOpenScreens() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(), provider);
		var session = await host.ActivateScreenAsync("first", "single");
		var component = new ProbeScreen { Guard = _ => Task.FromResult(false) };
		await host.AttachScreenAsync(session.Id, component);
		Assert.That(await host.ActivateScreenAsync("first", "multiple"), Is.Null);
		Assert.That(await host.CloseScreenAsync(session.Id), Is.False);
		Assert.That(host.ActiveScreen, Is.SameAs(session));
		Assert.That(host.OpenScreens, Has.Length.EqualTo(1));
		Assert.That(component.Deactivations, Is.Zero);
	}

	[Test]
	public async Task GuardCancellationLeavesStateUnchangedAndReleasesTransitionGate() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(), provider);
		var session = await host.ActivateScreenAsync("first", "single");
		using var cancellation = new CancellationTokenSource();
		var component = new ProbeScreen { Guard = _ => {
			cancellation.Cancel();
			return Task.FromResult(true);
		} };
		await host.AttachScreenAsync(session.Id, component);
		Assert.That(async () => await host.ActivateScreenAsync("first", "multiple", cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(host.ActiveScreen, Is.SameAs(session));
		Assert.That(host.OpenScreens, Has.Length.EqualTo(1));
		component.Guard = _ => Task.FromResult(true);
		Assert.That(await host.ActivateScreenAsync("first", "multiple"), Is.Not.Null);
	}

	[Test]
	public async Task OverlappingTransitionsWaitForTheCurrentGuard() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(), provider);
		var session = await host.ActivateScreenAsync("first", "single");
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var component = new ProbeScreen { Guard = _ => {
			entered.TrySetResult();
			return release.Task;
		} };
		await host.AttachScreenAsync(session.Id, component);
		var firstTransition = host.ActivateScreenAsync("first", "multiple");
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var secondTransition = host.ActivateScreenAsync("first", "multiple");
		Assert.That(secondTransition.IsCompleted, Is.False);
		Assert.That(host.OpenScreens, Has.Length.EqualTo(1));
		release.SetResult(true);
		var sessions = await Task.WhenAll(firstTransition, secondTransition).WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(sessions.Select(screen => screen.Id).Distinct().Count(), Is.EqualTo(2));
		Assert.That(host.OpenScreens, Has.Length.EqualTo(3));
	}

	[Test]
	public async Task NavigationChecksHiddenScreensAsWellAsActiveScreen() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(), provider);
		var first = await host.ActivateScreenAsync("first", "single");
		var component = new ProbeScreen();
		await host.AttachScreenAsync(first.Id, component);
		await host.ActivateScreenAsync("first", "multiple");
		component.Guard = _ => Task.FromResult(false);
		component.HasUnsavedChanges = true;

		Assert.That(await host.CanNavigateAsync(), Is.False);
		Assert.That(host.HasUnsavedChanges, Is.True);
		Assert.That(host.ActiveScreen, Is.Not.SameAs(first));
	}

	[Test]
	public async Task UnregisterBlockPreflightsEveryScreenBeforeRemovingAnySession() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(), provider);
		var first = await host.ActivateScreenAsync("first", "multiple");
		var second = await host.ActivateScreenAsync("first", "multiple");
		var component = new OtherScreen { Guard = _ => Task.FromResult(false) };
		await host.AttachScreenAsync(second.Id, component);

		Assert.That(await host.UnregisterBlockAsync("first"), Is.False);
		Assert.That(host.OpenScreens.Select(screen => screen.Id), Is.EquivalentTo(new[] { first.Id, second.Id }));
		Assert.That(host.Blocks.Select(block => block.Id), Does.Contain("first"));
		component.Guard = _ => Task.FromResult(true);
		Assert.That(await host.UnregisterBlockAsync("first"), Is.True);
		Assert.That(host.OpenScreens, Is.Empty);
		Assert.That(host.Blocks.Select(block => block.Id), Does.Not.Contain("first"));
		await host.RegisterBlockAsync("first");
		Assert.That(await host.ActivateScreenAsync("first", "multiple"), Is.Not.Null);
	}

	[Test]
	public async Task ClosingActiveScreenSelectsRetainedScreenWithoutDisposingComponents() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(), provider);
		var first = await host.ActivateScreenAsync("first", "single");
		var firstComponent = new ProbeScreen();
		await host.AttachScreenAsync(first.Id, firstComponent);
		var second = await host.ActivateScreenAsync("first", "multiple");
		var secondComponent = new OtherScreen();
		await host.AttachScreenAsync(second.Id, secondComponent);
		Assert.That(await host.CloseScreenAsync(second.Id), Is.True);
		Assert.That(host.ActiveScreen, Is.SameAs(first));
		Assert.That(firstComponent.Activations, Is.EqualTo(2));
		Assert.That(secondComponent.Disposals, Is.Zero, "The renderer owns component disposal.");
		host.Dispose();
		Assert.That(firstComponent.Disposals, Is.Zero);
	}

	[Test]
	public async Task ActionUsesCurrentScopeAndCanNavigateWithoutReenteringTheTransitionGate() {
		var block = new BlazorApplicationBlockBuilder().WithId("actions").WithName("Actions").AddMenu(menu => menu.WithText("Menu")
			.AddScreenItem<ProbeScreen>("screen", "Screen")
			.AddActionItem("run", "Run", async (services, token) => {
				services.GetRequiredService<ActionState>().Calls++;
				await services.GetRequiredService<IBlazorApplicationScreenHost>().ActivateScreenAsync("actions", "screen", token);
			})).Build();
		await using var provider = new ServiceCollection().AddSingleton<IBlazorApplicationBlockCatalog>(new BlazorApplicationBlockCatalog(new[] { block }))
			.AddScoped<IBlazorApplicationScreenHost, BlazorApplicationScreenHost>().AddScoped<ActionState>().BuildServiceProvider();
		await using var firstScope = provider.CreateAsyncScope();
		await using var secondScope = provider.CreateAsyncScope();
		var firstHost = firstScope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var secondHost = secondScope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		Assert.That(await firstHost.ExecuteMenuItemAsync("actions", "run").WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(firstScope.ServiceProvider.GetRequiredService<ActionState>().Calls, Is.EqualTo(1));
		Assert.That(secondScope.ServiceProvider.GetRequiredService<ActionState>().Calls, Is.Zero);
		Assert.That(firstHost.OpenScreens, Has.Length.EqualTo(1));
		Assert.That(secondHost.OpenScreens, Is.Empty);
	}

	[Test]
	public void ActionFailuresAreObservable() {
		var failure = new InvalidOperationException("Action failed");
		var block = new BlazorApplicationBlockBuilder().WithId("actions").WithName("Actions")
			.AddMenu(menu => menu.WithText("Menu").AddActionItem("run", "Run", (_, _) => Task.FromException(failure))).Build();
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new BlazorApplicationBlockCatalog(new[] { block }), provider);
		Assert.That(async () => await host.ExecuteMenuItemAsync("actions", "run"), Throws.InstanceOf<InvalidOperationException>().With.Message.EqualTo(failure.Message));
	}

	[Test]
	public async Task DirtyNotificationReflectsAttachedStateAndDetachClearsIt() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(), provider);
		var session = await host.ActivateScreenAsync("first", "single");
		var component = new ProbeScreen();
		await host.AttachScreenAsync(session.Id, component);
		var notifications = 0;
		host.Changed += () => notifications++;
		component.HasUnsavedChanges = true;
		host.NotifyScreenChanged(session.Id);
		Assert.That(notifications, Is.EqualTo(1));
		Assert.That(host.HasUnsavedChanges, Is.True);
		host.DetachScreen(session.Id, component);
		Assert.That(host.HasUnsavedChanges, Is.False);
		Assert.That(notifications, Is.EqualTo(2));
	}

	[Test]
	public async Task FailedActivationDoesNotLeaveComponentAttached() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(), provider);
		var session = await host.ActivateScreenAsync("first", "single");
		var component = new ProbeScreen { Activation = _ => Task.FromException(new InvalidOperationException("Activation failed")) };
		Assert.That(async () => await host.AttachScreenAsync(session.Id, component), Throws.InstanceOf<InvalidOperationException>());
		Assert.That(session.Screen, Is.Null);
		component.Activation = _ => Task.CompletedTask;
		await host.AttachScreenAsync(session.Id, component);
		Assert.That(session.Screen, Is.SameAs(component));
	}

	[Test]
	public async Task HostDisposalCancelsAnInFlightGuardWithoutCommittingNavigation() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(), provider);
		var session = await host.ActivateScreenAsync("first", "single");
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		await host.AttachScreenAsync(session.Id, new ProbeScreen { Guard = async token => {
			entered.SetResult();
			await Task.Delay(Timeout.Infinite, token);
			return true;
		} });
		var transition = host.ActivateScreenAsync("first", "multiple");
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		host.Dispose();
		Assert.That(async () => await transition, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(host.ActiveScreen, Is.Null);
		Assert.That(host.OpenScreens, Is.Empty);
	}

	[Test]
	public async Task ActionOnlyBlockPreservesRetainedSessionsAndBecomesActive() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var actionBlock = new BlazorApplicationBlockBuilder().WithId("actions").WithName("Actions")
			.AddMenu(menu => menu.WithText("Menu").AddActionItem("run", "Run", () => { })).Build();
		using var host = new BlazorApplicationScreenHost(new BlazorApplicationBlockCatalog(CreateCatalog().Blocks.Append(actionBlock)), provider);
		var session = await host.ActivateScreenAsync("first", "single");
		await host.ActivateBlockAsync("actions");
		Assert.That(host.ActiveBlock.Id, Is.EqualTo("actions"));
		Assert.That(host.ActiveScreen, Is.Null);
		Assert.That(host.OpenScreens.Single(), Is.SameAs(session));
	}

	[Test]
	public async Task SessionParametersCannotBeMutatedByConsumers() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(), provider);
		var session = await host.ActivateScreenAsync("first", "single");
		Assert.That(() => session.Parameters.Add("Message", "changed"), Throws.TypeOf<NotSupportedException>());
	}


	[Test]
	public async Task DisposalDuringAsyncScreenInitializationDoesNotLeaveItAttached() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new BlazorApplicationBlockBuilder().WithId("block").WithName("Block").WithDefaultScreen<InitializingScreen>().Build();
		using var host = new BlazorApplicationScreenHost(new BlazorApplicationBlockCatalog(new[] { block }), provider);
		var session = await host.ActivateBlockAsync("block");
		var screen = new InitializingScreen();
		var initialization = screen.InitializeAsync(host, session);
		await screen.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await screen.DisposeAsync();
		screen.Release.SetResult();
		await initialization.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(session.Screen, Is.Null);
		Assert.That(screen.Disposals, Is.EqualTo(1));
	}

	private static BlazorApplicationBlockCatalog CreateCatalog() => new(new[] {
		new BlazorApplicationBlockBuilder().WithId("first").WithName("First").AddMenu(menu => menu.WithText("Screens")
			.AddScreenItem<ProbeScreen>("single", "Single", ScreenActivationMode.SingleInstance).AddScreenItem<OtherScreen>("multiple", "Multiple", ScreenActivationMode.MultiInstance)).Build(),
		new BlazorApplicationBlockBuilder().WithId("second").WithName("Second").AddMenu(menu => menu.WithText("Screens")
			.AddScreenItem<ProbeScreen>("shared", "Shared", ScreenActivationMode.SingleInstance)).Build()
	});

	public class ProbeScreen : ComponentBase, IBlazorApplicationScreen, IDisposable {
		public bool HasUnsavedChanges { get; set; }

		public Func<CancellationToken, Task<bool>> Guard { get; set; } = _ => Task.FromResult(true);

		public Func<CancellationToken, Task> Activation { get; set; } = _ => Task.CompletedTask;

		public int Activations { get; private set; }

		public int Deactivations { get; private set; }

		public int Disposals { get; private set; }

		public Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) => Guard(cancellationToken);

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

	public class OtherScreen : ProbeScreen {
	}


	public class InitializingScreen : BlazorApplicationScreen {
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public int Disposals { get; private set; }

		public Task InitializeAsync(IBlazorApplicationScreenHost host, BlazorApplicationScreenSession session) {
			ScreenHost = host;
			Session = session;
			return OnInitializedAsync();
		}

		protected override async Task OnActivatedAsync(CancellationToken cancellationToken) {
			Entered.SetResult();
			await Release.Task;
		}

		protected override ValueTask DisposeAsyncCore() {
			Disposals++;
			return ValueTask.CompletedTask;
		}
	}

	public class ActionState {
		public int Calls { get; set; }
	}
}

