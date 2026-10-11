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
public class ApplicationExitTests {

	[Test]
	public async Task ConfirmedExitChecksPermissionsThenFinalizesAndReleasesOwnedResourcesOnce() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var calls = new List<string>();
		var block = new ExitBlock { Name = "Editor", Report = calls.Add };
		var definition = block.DefineScreen(ScreenActivationMode.SingleInstance);
		using var host = new ExitHost(provider, block);
		using var application = new ExitApplication(host) { Report = calls.Add };
		host.UI.Report = calls.Add;
		var screen = await host.OpenScreenAsync(block, definition);
		screen.Report = calls.Add;
		application.ConfigureFinalizers(new[] { new ExitFinalizer(() => calls.Add("finalize")) });
		application.Disposables.Add(() => calls.Add("dispose"));
		application.Initialize();
		IApplication contract = application;

		Assert.That(await contract.ExitAsync(), Is.True);
		Assert.That(await contract.ExitAsync(), Is.True);

		Assert.That(calls, Is.EqualTo(new[] { "confirm", "screen", "block", "exit:False", "finalize", "dispose" }));
		Assert.That(block.GuardApplication, Is.SameAs(application));
		Assert.That(contract.IsExiting, Is.False);
		Assert.That(contract.HasExited, Is.True);
		Assert.That(contract.IsInitialized, Is.False);
		Assert.That(host.Screens.Single(), Is.SameAs(screen), "The application borrows its host rather than closing or disposing screens itself.");
	}

	[Test]
	public async Task DeclinedConfirmationLeavesLifecycleIntactAndCanBeRetried() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new ExitBlock { Name = "Editor" };
		using var host = new ExitHost(provider, block);
		using var application = new ExitApplication(host);
		var finalizations = 0;
		application.ConfigureFinalizers(new[] { new ExitFinalizer(() => finalizations++) });
		application.Initialize();
		host.UI.Answer = false;

		Assert.That(await application.ExitAsync(), Is.False);
		Assert.That(application.IsInitialized, Is.True);
		Assert.That(application.IsExiting, Is.False);
		Assert.That(application.HasExited, Is.False);
		Assert.That(host.UI.ApplicationExiting, Is.False);
		Assert.That(application.PermissionCalls, Is.Zero);
		Assert.That(application.ExitCalls, Is.Zero);
		Assert.That(finalizations, Is.Zero);
		host.UI.Answer = true;

		Assert.That(await application.ExitAsync(), Is.True);
		Assert.That(host.UI.Questions, Is.EqualTo(2));
		Assert.That(finalizations, Is.EqualTo(1));
	}

	[Test]
	public async Task ActiveScreenVetoPreventsBlockChecksAndShutdown() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new ExitBlock { Name = "Editor" };
		var definition = block.DefineScreen();
		using var host = new ExitHost(provider, block);
		using var application = new ExitApplication(host);
		var screen = await host.OpenScreenAsync(block, definition);
		screen.Guard = _ => Task.FromResult(false);

		Assert.That(await application.ExitAsync(), Is.False);

		Assert.That(screen.GuardCalls, Is.EqualTo(1));
		Assert.That(block.GuardCalls, Is.Zero);
		Assert.That(application.ExitCalls, Is.Zero);
		Assert.That(host.ActiveScreen, Is.SameAs(screen));
		Assert.That(application.HasExited, Is.False);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task HiddenAndPermanentRetainedScreensCanVetoExitWithoutAnActiveScreen(bool permanent) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new ExitBlock { Name = "Editor" };
		var mode = permanent ? ScreenActivationMode.PermanentSingleton : ScreenActivationMode.SingleInstance;
		var definition = block.DefineScreen(mode);
		using var host = new ExitHost(provider, block);
		using var application = new ExitApplication(host);
		await host.TrySetScreenModeAsync(ScreenMode.SingleView);
		var screen = await host.OpenScreenAsync(block, definition);
		Assert.That(await host.ClearSelectionAsync(), Is.True);
		Assert.That(host.ActiveScreen, Is.Null);
		screen.GuardCalls = 0;
		screen.Guard = _ => Task.FromResult(false);

		Assert.That(await application.ExitAsync(), Is.False);

		Assert.That(screen.GuardCalls, Is.EqualTo(1));
		Assert.That(host.Screens.Single(), Is.SameAs(screen));
		Assert.That(screen.Host, Is.SameAs(host));
		Assert.That(application.ExitCalls, Is.Zero);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ExitChecksActualScreenOwnerAndDistinctNavigationBlock(bool vetoScreenBlock) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var screenBlock = new ExitBlock { Name = "Screen owner", Allowed = !vetoScreenBlock };
		var navigationBlock = new ExitBlock { Name = "Navigation", Allowed = vetoScreenBlock };
		var definition = screenBlock.DefineScreen();
		using var host = new ExitHost(provider, screenBlock, navigationBlock);
		var screen = await host.OpenScreenAsync(screenBlock, definition);
		using var projection = new NavigationHost(host, navigationBlock);
		using var application = new ExitApplication(projection);

		Assert.That(await application.ExitAsync(), Is.False);

		Assert.That(screen.Block, Is.SameAs(screenBlock));
		Assert.That(screenBlock.GuardCalls, Is.EqualTo(1));
		Assert.That(navigationBlock.GuardCalls, Is.EqualTo(vetoScreenBlock ? 0 : 1));
		Assert.That(screenBlock.GuardApplication, Is.SameAs(application));
		Assert.That(application.ExitCalls, Is.Zero);
	}

	[Test]
	public async Task TheSameScreenAndNavigationBlockIsCheckedOnlyOnce() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new ExitBlock { Name = "Editor" };
		var definition = block.DefineScreen();
		using var host = new ExitHost(provider, block);
		using var application = new ExitApplication(host);
		await host.OpenScreenAsync(block, definition);

		Assert.That(await application.ExitAsync(), Is.True);

		Assert.That(block.GuardCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task ForceBypassesConfirmationScreenAndBlockVetoesButStillFinalizes() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new ExitBlock { Name = "Editor", Allowed = false };
		var definition = block.DefineScreen();
		using var host = new ExitHost(provider, block);
		using var application = new ExitApplication(host);
		var screen = await host.OpenScreenAsync(block, definition);
		screen.Guard = _ => Task.FromResult(false);
		host.UI.Answer = false;
		var finalizations = 0;
		application.ConfigureFinalizers(new[] { new ExitFinalizer(() => finalizations++) });
		application.Initialize();

		Assert.That(await application.ExitAsync(force: true), Is.True);

		Assert.That(host.UI.Questions, Is.Zero);
		Assert.That(application.PermissionCalls, Is.Zero);
		Assert.That(screen.GuardCalls, Is.Zero);
		Assert.That(block.GuardCalls, Is.Zero);
		Assert.That(application.LastForce, Is.True);
		Assert.That(finalizations, Is.EqualTo(1));
	}

	[Test]
	public async Task PendingAndReentrantNormalRequestsShareOneConfirmationAndShutdown() {
		var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var application = new ExitApplication();
		Task<bool> reentrant = null;
		application.Confirmation = _ => {
			reentrant = application.ExitAsync();
			return answer.Task;
		};

		var first = application.ExitAsync();
		var second = application.ExitAsync();
		Assert.That(application.IsExiting, Is.True);
		Assert.That(application.HasExited, Is.False);
		Assert.That(application.ConfirmationCalls, Is.EqualTo(1));
		answer.SetResult(true);

		Assert.That(await first, Is.True);
		Assert.That(await second, Is.True);
		Assert.That(await reentrant, Is.True);
		Assert.That(application.ExitCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task ForceSupersedesAPendingConfirmationWithoutWaitingForItsAnswer() {
		var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var application = new ExitApplication { Confirmation = _ => answer.Task };
		var normal = application.ExitAsync();

		var forced = application.ExitAsync(force: true);

		Assert.That(await forced.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(await normal, Is.True);
		Assert.That(answer.Task.IsCompleted, Is.False);
		Assert.That(application.ConfirmationCalls, Is.EqualTo(1));
		Assert.That(application.PermissionCalls, Is.Zero);
		Assert.That(application.ExitCalls, Is.EqualTo(1));
		Assert.That(application.LastForce, Is.True);
	}

	[Test]
	public async Task ForceSupersedesAPendingPermissionCheck() {
		var permission = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var application = new ExitApplication { Permission = _ => permission.Task };
		var normal = application.ExitAsync();
		Assert.That(application.PermissionCalls, Is.EqualTo(1));

		Assert.That(await application.ExitAsync(force: true).WaitAsync(TimeSpan.FromSeconds(5)), Is.True);

		Assert.That(await normal, Is.True);
		Assert.That(permission.Task.IsCompleted, Is.False);
		Assert.That(application.ExitCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task ForceEscalationRetriesAPlatformCloseThatWasAlreadyAwaitingAVeto() {
		var close = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var application = new ExitApplication { Finish = (force, _) => force ? Task.FromResult(true) : close.Task };
		var normal = application.ExitAsync();
		Assert.That(application.ExitCalls, Is.EqualTo(1));
		Assert.That(application.LastForce, Is.False);

		var forced = application.ExitAsync(force: true);
		close.SetResult(false);

		Assert.That(await normal, Is.True);
		Assert.That(await forced, Is.True);
		Assert.That(application.ExitCalls, Is.EqualTo(2));
		Assert.That(application.LastForce, Is.True);
	}

	[Test]
	public async Task CancellingAJoiningCallerDoesNotCancelTheOriginalExitRequest() {
		var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var application = new ExitApplication { Confirmation = _ => answer.Task };
		using var joiningCancellation = new CancellationTokenSource();
		var owner = application.ExitAsync();
		var joining = application.ExitAsync(cancellationToken: joiningCancellation.Token);

		joiningCancellation.Cancel();

		Assert.That(async () => await joining, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(application.IsExiting, Is.True);
		Assert.That(owner.IsCompleted, Is.False);
		answer.SetResult(true);
		Assert.That(await owner, Is.True);
		Assert.That(application.ExitCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task CancellingTheOriginalCallerCancelsPreparationAndAllowsANewAttempt() {
		var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var application = new ExitApplication { Confirmation = _ => answer.Task };
		using var ownerCancellation = new CancellationTokenSource();
		var owner = application.ExitAsync(cancellationToken: ownerCancellation.Token);
		var joining = application.ExitAsync();

		ownerCancellation.Cancel();

		Assert.That(async () => await owner, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(async () => await joining, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(application.IsExiting, Is.False);
		Assert.That(application.HasExited, Is.False);
		Assert.That(application.ExitCalls, Is.Zero);
		application.Confirmation = _ => Task.FromResult(true);
		Assert.That(await application.ExitAsync(), Is.True);
	}

	[Test]
	public async Task DisposingTheApplicationCancelsPendingExitAndPerformsOwnedCleanup() {
		var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var application = new ExitApplication { Confirmation = _ => answer.Task };
		var disposals = 0;
		application.Disposables.Add(() => disposals++);
		var pending = application.ExitAsync();

		application.Dispose();

		Assert.That(async () => await pending, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(application.ExitCalls, Is.Zero);
		Assert.That(disposals, Is.EqualTo(1));
		Assert.That(application.HasExited, Is.False);
		Assert.That(() => application.ExitAsync(force: true), Throws.InvalidOperationException);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task APermissionCancellationCallbackCannotJoinItsOwnDisposal(bool synchronous) {
		var application = new ExitApplication();
		var permission = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var callbackCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var finalizerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var finalizerGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var registration = default(CancellationTokenRegistration);
		Task disposal = null;
		Exception callbackError = null;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
			finalizerGate.TrySetResult();
			if (callbackCompleted.Task.IsCompleted)
				registration.Dispose();
			if (disposal?.IsCompleted == true)
				application.Dispose();
		});
		application.Permission = token => {
			// Capture the permission operation's execution context before disposal begins.
			registration = token.Register(() => {
				Tools.Exceptions.ExecuteCapturingException(() => {
					if (synchronous)
						application.Dispose();
					else
						application.DisposeAsync().AsTask().GetAwaiter().GetResult();
				}, out callbackError);
				callbackCompleted.TrySetResult();
			});
			return permission.Task;
		};
		application.ConfigureFinalizers(new[] { new AsyncExitFinalizer(async _ => {
			finalizerStarted.TrySetResult();
			await finalizerGate.Task;
		}) });
		var resources = 0;
		application.Disposables.Add(() => resources++);
		await application.InitializeAsync();
		var pendingExit = application.ExitAsync();

		disposal = Task.Run(async () => await application.DisposeAsync());
		await callbackCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await finalizerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var externalDisposal = application.DisposeAsync().AsTask();

		Assert.That(callbackError, Is.TypeOf<InvalidOperationException>().With.Message.Contains("own pending cleanup"));
		Assert.That(disposal.IsCompleted, Is.False);
		Assert.That(externalDisposal.IsCompleted, Is.False, "An independent caller still joins the pending disposal operation.");
		finalizerGate.SetResult();
		await Task.WhenAll(disposal, externalDisposal).WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(async () => await pendingExit.WaitAsync(TimeSpan.FromSeconds(5)), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(resources, Is.EqualTo(1));
		Assert.That(application.Disposables, Is.Empty);
	}

	[Test]
	public async Task IndependentDisposalStillWaitsWhileApplicationOwnershipIsBeingDetached() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var detachGate = new ManualResetEventSlim();
		var detaching = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var block = new DetachBlock { Name = "Workspace" };
		using var host = new ApplicationScreenHost(new[] { block }, provider);
		using var application = new ExitApplication(host);
		using var releaseDetach = Tools.Scope.ExecuteOnDispose(() => {
			block.Detaching = null;
			detachGate.Set();
		});
		block.Detaching = () => {
			detaching.TrySetResult();
			Guard.Ensure(detachGate.Wait(TimeSpan.FromSeconds(5)), "The test did not release ownership detachment.");
		};
		var owner = Task.Run(async () => await application.DisposeAsync());
		await detaching.Task.WaitAsync(TimeSpan.FromSeconds(5));

		var joining = application.DisposeAsync().AsTask();

		Assert.That(owner.IsCompleted, Is.False);
		Assert.That(joining.IsCompleted, Is.False, "The disposed-state marker cannot bypass unfinished ownership cleanup.");
		Assert.That(block.Parent, Is.SameAs(application));
		detachGate.Set();
		await Task.WhenAll(owner, joining).WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(block.Parent, Is.Null);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task OwnershipDetachmentCannotJoinItsOwnPendingDisposal(bool synchronous) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new DetachBlock { Name = "Workspace" };
		using var host = new ApplicationScreenHost(new[] { block }, provider);
		var application = new ExitApplication(host);
		Task disposal = null;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
			block.Detaching = null;
			if (disposal?.IsCompleted == true)
				application.Dispose();
		});
		Exception callbackError = null;
		block.Detaching = () => Tools.Exceptions.ExecuteCapturingException(() => {
			if (synchronous)
				application.Dispose();
			else
				application.DisposeAsync().AsTask().GetAwaiter().GetResult();
		}, out callbackError);

		disposal = Task.Run(async () => await application.DisposeAsync());
		await disposal.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.That(callbackError, Is.TypeOf<InvalidOperationException>().With.Message.Contains("own pending cleanup"));
		Assert.That(block.Parent, Is.Null);
		await application.DisposeAsync();
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task LogicalExitCompletesAfterFailedCleanupEvenWhenItsLoggerFails(bool loggerFails) {
		var failure = new InvalidOperationException("logical shutdown failed");
		var logger = new ExitLogger { ThrowOnError = loggerFails };
		using var application = new ExitApplication { Logger = logger };
		var finalizers = 0;
		var resources = 0;
		application.ConfigureFinalizers(new[] {
			new ExitFinalizer(() => throw failure),
			new ExitFinalizer(() => finalizers++)
		});
		application.Disposables.Add(() => resources++);
		await application.InitializeAsync();

		Assert.That(await application.ExitAsync().WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(await application.ExitAsync(), Is.True);

		Assert.That(application.HasExited, Is.True);
		Assert.That(application.IsExiting, Is.False);
		Assert.That(application.IsInitialized, Is.False);
		Assert.That(application.ExitCalls, Is.EqualTo(1));
		Assert.That(finalizers, Is.EqualTo(1));
		Assert.That(resources, Is.EqualTo(1));
		Assert.That(logger.Messages, Has.Some.Contains(failure.Message));
	}

	[Test]
	public async Task CancellingTheOwnerDuringCommittedLogicalCleanupDoesNotReopenTheExitWorkflow() {
		var finalizerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var finalizerGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		await using var application = new ExitApplication();
		using var releaseFinalizer = Tools.Scope.ExecuteOnDispose(() => finalizerGate.TrySetResult());
		using var cancellation = new CancellationTokenSource();
		var finalizations = 0;
		var resources = 0;
		application.ConfigureFinalizers(new[] { new AsyncExitFinalizer(async token => {
			Assert.That(token.CanBeCanceled, Is.False, "Committed cleanup belongs to the application rather than the requesting caller.");
			finalizerStarted.TrySetResult();
			await finalizerGate.Task;
			finalizations++;
		}) });
		application.Disposables.Add(() => resources++);
		await application.InitializeAsync();
		var owner = application.ExitAsync(cancellationToken: cancellation.Token);
		await finalizerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var joining = application.ExitAsync();

		cancellation.Cancel();

		Assert.That(async () => await owner, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(application.IsExiting, Is.True);
		Assert.That(application.HasExited, Is.False);
		Assert.That(joining.IsCompleted, Is.False);
		finalizerGate.SetResult();
		Assert.That(await joining.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(application.HasExited, Is.True);
		Assert.That(application.IsExiting, Is.False);
		Assert.That(finalizations, Is.EqualTo(1));
		Assert.That(resources, Is.EqualTo(1));
		Assert.That(application.ExitCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task AThrowingCancellationCallbackCannotPreventDisposalFinalizationAndOwnershipCleanup() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new ExitBlock { Name = "Editor" };
		using var host = new ExitHost(provider, block);
		using var application = new ExitApplication(host);
		var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var failure = new InvalidOperationException("cancellation cleanup failed");
		var registration = default(CancellationTokenRegistration);
		using var registrationCleanup = Tools.Scope.ExecuteOnDispose(() => registration.Dispose());
		application.Confirmation = token => {
			registration = token.Register(() => throw failure);
			return answer.Task;
		};
		var finalizations = 0;
		var disposals = 0;
		var notifications = 0;
		application.ConfigureFinalizers(new[] { new ExitFinalizer(() => finalizations++) });
		application.Disposables.Add(() => disposals++);
		application.Changed += () => Interlocked.Increment(ref notifications);
		application.Initialize();
		var pending = application.ExitAsync();
		Assert.That(block.Parent, Is.SameAs(application));

		Tools.Exceptions.ExecuteCapturingException(application.Dispose, out var error);

		Assert.That(error, Is.InstanceOf<AggregateException>());
		Assert.That(((AggregateException)error).Flatten().InnerExceptions, Does.Contain(failure));
		Assert.That(async () => await pending, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(finalizations, Is.EqualTo(1));
		Assert.That(disposals, Is.EqualTo(1));
		Assert.That(application.Disposables, Is.Empty);
		Assert.That(application.IsInitialized, Is.False);
		Assert.That(block.Parent, Is.Null);
		Assert.That(host.Blocks.Single(), Is.SameAs(block), "Failure cleanup still preserves the borrowed host's ownership.");
		var beforeNotification = notifications;
		application.NotifyChange();
		Assert.That(notifications, Is.EqualTo(beforeNotification));
		Assert.That(() => application.ExitAsync(force: true), Throws.InvalidOperationException);
	}

	[Test]
	public async Task ForceStillCompletesWhenAPreparationCancellationCallbackThrows() {
		var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var failure = new InvalidOperationException("permission cancellation failed");
		var registration = default(CancellationTokenRegistration);
		using var registrationCleanup = Tools.Scope.ExecuteOnDispose(() => registration.Dispose());
		var logger = new ExitLogger();
		using var application = new ExitApplication { Logger = logger, Confirmation = token => {
			registration = token.Register(() => throw failure);
			return answer.Task;
		} };
		var normal = application.ExitAsync();

		Assert.That(await application.ExitAsync(force: true).WaitAsync(TimeSpan.FromSeconds(5)), Is.True);

		Assert.That(await normal, Is.True);
		Assert.That(answer.Task.IsCompleted, Is.False);
		Assert.That(application.ExitCalls, Is.EqualTo(1));
		Assert.That(application.LastForce, Is.True);
		Assert.That(application.HasExited, Is.True);
		Assert.That(logger.Messages, Has.Some.Contains(failure.Message));
	}

	[TestCase("confirmation")]
	[TestCase("permission")]
	[TestCase("finish")]
	public async Task FailuresResetPendingStateAndAllowRetry(string stage) {
		var failure = new InvalidOperationException(stage);
		using var application = new ExitApplication();
		switch (stage) {
			case "confirmation": application.Confirmation = _ => Task.FromException<bool>(failure); break;
			case "permission": application.Permission = _ => Task.FromException<bool>(failure); break;
			case "finish": application.Finish = (_, _) => Task.FromException<bool>(failure); break;
		}

		Assert.That(async () => await application.ExitAsync(), Throws.InstanceOf<InvalidOperationException>().With.Message.EqualTo(stage));

		Assert.That(application.IsExiting, Is.False);
		Assert.That(application.HasExited, Is.False);
		application.Confirmation = null;
		application.Permission = null;
		application.Finish = null;
		Assert.That(await application.ExitAsync(), Is.True);
	}

	[Test]
	public async Task APlatformCloseVetoReturnsFalseWithoutMarkingExitComplete() {
		using var application = new ExitApplication { Finish = (_, _) => Task.FromResult(false) };

		Assert.That(await application.ExitAsync(), Is.False);

		Assert.That(application.HasExited, Is.False);
		Assert.That(application.IsExiting, Is.False);
		application.Finish = null;
		Assert.That(await application.ExitAsync(), Is.True);
		Assert.That(application.ExitCalls, Is.EqualTo(2));
	}

	[Test]
	public async Task CancellationAfterPermissionStopsExitBeforePlatformClose() {
		using var cancellation = new CancellationTokenSource();
		using var application = new ExitApplication { Permission = _ => {
			cancellation.Cancel();
			return Task.FromResult(true);
		} };

		Assert.That(async () => await application.ExitAsync(cancellationToken: cancellation.Token), Throws.InstanceOf<OperationCanceledException>());

		Assert.That(application.ExitCalls, Is.Zero);
		Assert.That(application.HasExited, Is.False);
	}

	[Test]
	public async Task ACancelledRequestDoesNotStartConfirmationEvenWhenForced() {
		using var application = new ExitApplication();
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		Assert.That(() => application.ExitAsync(force: true, cancellationToken: cancellation.Token), Throws.InstanceOf<OperationCanceledException>());

		Assert.That(application.ConfirmationCalls, Is.Zero);
		Assert.That(application.ExitCalls, Is.Zero);
		Assert.That(application.IsExiting, Is.False);
		Assert.That(await application.ExitAsync(force: true), Is.True);
	}

	[Test]
	public async Task ApplicationDecoratorForwardsExitAndPendingState() {
		var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var application = new ExitApplication { Confirmation = _ => answer.Task };
		using var decorator = new ExitDecorator(application);
		IApplication contract = decorator;

		var pending = contract.ExitAsync();

		Assert.That(contract.IsExiting, Is.True);
		Assert.That(contract.HasExited, Is.False);
		answer.SetResult(true);
		Assert.That(await pending, Is.True);
		Assert.That(contract.IsExiting, Is.False);
		Assert.That(contract.HasExited, Is.True);
		Assert.That(application.ExitCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task ADeclineNotificationCanRetryWithoutTheOldRequestResettingTheNewExitState() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new ExitHost(provider);
		using var application = new ExitApplication(host);
		host.UI.Answer = false;
		Task<bool> retry = null;
		application.Changed += () => {
			if (!application.IsExiting && !application.HasExited && retry == null) {
				host.UI.Answer = true;
				retry = application.ExitAsync();
			}
		};

		Assert.That(await application.ExitAsync(), Is.False);

		Assert.That(retry, Is.Not.Null);
		Assert.That(await retry, Is.True);
		Assert.That(application.HasExited, Is.True);
		Assert.That(host.UI.ApplicationExiting, Is.True);
		Assert.That(application.ExitCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task AForceRequestDuringDeclineCleanupRunsAfterTheSealedRequestCompletes() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new ExitHost(provider);
		using var application = new ExitApplication(host);
		host.UI.Answer = false;
		Task<bool> forced = null;
		host.UI.ExitStateChanged = exiting => {
			if (!exiting && forced == null)
				forced = application.ExitAsync(force: true);
		};

		Assert.That(await application.ExitAsync(), Is.False);

		Assert.That(forced, Is.Not.Null);
		Assert.That(await forced.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(host.UI.Questions, Is.EqualTo(1));
		Assert.That(application.PermissionCalls, Is.Zero);
		Assert.That(application.ExitCalls, Is.EqualTo(1));
		Assert.That(application.LastForce, Is.True);
		Assert.That(application.HasExited, Is.True);
		Assert.That(host.UI.ApplicationExiting, Is.True);
	}

	[Test]
	public async Task InitializationCannotStartDuringOrAfterExit() {
		var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var application = new ExitApplication { Confirmation = _ => answer.Task };
		var pending = application.ExitAsync();

		Assert.That(application.Initialize, Throws.InvalidOperationException);
		answer.SetResult(true);
		Assert.That(await pending, Is.True);
		Assert.That(application.Initialize, Throws.InvalidOperationException);
	}

	[Test]
	public async Task ChangedNotificationsExposePendingAndCompletedExitState() {
		var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var application = new ExitApplication { Confirmation = _ => answer.Task };
		var states = new List<(bool Exiting, bool Exited)>();
		application.Changed += () => states.Add((application.IsExiting, application.HasExited));
		var pending = application.ExitAsync();

		Assert.That(states, Is.EqualTo(new[] { (true, false) }));
		answer.SetResult(true);
		Assert.That(await pending, Is.True);

		Assert.That(states, Is.EqualTo(new[] { (true, false), (false, true) }));
	}

	private sealed class ExitApplication : AsyncApplicationBase {
		private readonly IApplicationScreenHost _host;

		public ExitApplication(IApplicationScreenHost host = null) {
			_host = host;
			if (host != null)
				ObserveScreenHost();
		}

		public override IApplicationPlugin[] LoadedPlugins => Array.Empty<IApplicationPlugin>();
		public override IApplicationScreenHost ScreenHost => _host;
		public Func<CancellationToken, Task<bool>> Confirmation { get; set; }
		public Func<CancellationToken, Task<bool>> Permission { get; set; }
		public Func<bool, CancellationToken, Task<bool>> Finish { get; set; }
		public Action<string> Report { get; set; }
		public int ConfirmationCalls { get; private set; }
		public int PermissionCalls { get; private set; }
		public int ExitCalls { get; private set; }
		public bool LastForce { get; private set; }

		public void NotifyChange() => OnChanged();

		public void ConfigureFinalizers(IEnumerable<IApplicationFinalizer> tasks) => SetFinalizers(tasks);

		protected override Task<bool> ConfirmExitAsync(CancellationToken cancellationToken) {
			ConfirmationCalls++;
			return Confirmation != null ? Confirmation(cancellationToken) : _host == null ? Task.FromResult(true) : base.ConfirmExitAsync(cancellationToken);
		}

		protected override Task<bool> CanExitAsync(CancellationToken cancellationToken) {
			PermissionCalls++;
			return Permission != null ? Permission(cancellationToken) : base.CanExitAsync(cancellationToken);
		}

		protected override Task<bool> OnExitAsync(bool force, CancellationToken cancellationToken) {
			ExitCalls++;
			LastForce = force;
			Report?.Invoke($"exit:{force}");
			return Finish != null ? Finish(force, cancellationToken) : base.OnExitAsync(force, cancellationToken);
		}
	}

	private sealed class ExitBlock : ApplicationBlockBase {
		public bool Allowed { get; set; } = true;
		public int GuardCalls { get; private set; }
		public IApplication GuardApplication { get; private set; }
		public Action<string> Report { get; set; }

		public ApplicationScreenDefinition DefineScreen(ScreenActivationMode activationMode = ScreenActivationMode.MultiInstance) {
			var definition = new ApplicationScreenDefinition(typeof(ExitScreen), activationMode: activationMode);
			AddScreenDefinition(definition);
			return definition;
		}

		public override Task<bool> CanExitAsync(IApplication application, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			GuardCalls++;
			GuardApplication = application;
			Report?.Invoke("block");
			return Task.FromResult(Allowed);
		}
	}

	private sealed class DetachBlock : ApplicationBlockBase {
		public Action Detaching { get; set; }

		public override IApplication Parent {
			get => base.Parent;
			set {
				if (value == null && base.Parent != null)
					Detaching?.Invoke();
				base.Parent = value;
			}
		}
	}

	private sealed class ExitHost : ApplicationScreenHost<ExitBlock, ExitScreen> {
		public ExitHost(IServiceProvider provider, params ExitBlock[] blocks)
			: base(blocks, provider) {
			UI = new ConfirmationServices(this, provider);
		}

		public ConfirmationServices UI { get; }
		public override IUserInterfaceServices UserInterfaceServices => UI;

		protected override ExitScreen CreateScreen(ExitBlock block, ApplicationScreenDefinition definition) => new();

		protected override void FreeManagedResources() {
			base.FreeManagedResources();
			UI.Dispose();
		}
	}

	private sealed class ExitScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }
		public IApplicationBlock Block { get; set; }
		public Func<CancellationToken, Task<bool>> Guard { get; set; } = _ => Task.FromResult(true);
		public Action<string> Report { get; set; }
		public int GuardCalls { get; set; }

		public Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) {
			GuardCalls++;
			Report?.Invoke("screen");
			return Guard(cancellationToken);
		}
	}

	private sealed class ConfirmationServices(IApplicationScreenHost host, IServiceProvider provider) : ApplicationUserInterfaceServices(host, provider) {
		public bool Answer { get; set; } = true;
		public int Questions { get; private set; }
		public Action<string> Report { get; set; }
		public Action<bool> ExitStateChanged { get; set; }

		public override bool ApplicationExiting {
			get => base.ApplicationExiting;
			set {
				base.ApplicationExiting = value;
				ExitStateChanged?.Invoke(value);
			}
		}

		public override Task<bool> AskYNAsync(string question, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			Questions++;
			Report?.Invoke("confirm");
			return Task.FromResult(Answer);
		}
	}

	private sealed class NavigationHost(IApplicationScreenHost host, IApplicationBlock activeBlock) : ApplicationScreenHostDecorator(host) {
		public override IApplicationBlock ActiveBlock => activeBlock;
	}

	private sealed class ExitDecorator(IApplication application) : ApplicationDecorator(application);

	private sealed class ExitLogger : LoggerBase {
		public ExitLogger() => Options = LogOptions.VerboseProfile;

		public List<string> Messages { get; } = new();
		public bool ThrowOnError { get; set; }

		protected override void Log(LogLevel logLevel, string message) {
			Messages.Add(message);
			Guard.Ensure(!ThrowOnError || logLevel != LogLevel.Error, "The logger rejected an error message.");
		}
	}

	private sealed class AsyncExitFinalizer(Func<CancellationToken, Task> action) : AsyncApplicationFinalizerBase {
		public override Task FinalizeAsync(CancellationToken cancellationToken = default) => action(cancellationToken);
	}

	private sealed class ExitFinalizer(Action action) : SyncApplicationFinalizerBase {
		public override int Priority => 0;
#pragma warning disable CS0465
		public override void Finalize() => action();
#pragma warning restore CS0465
	}
}
