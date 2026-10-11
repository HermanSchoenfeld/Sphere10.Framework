// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationAsyncLifecycleTests {
	[Test]
	public async Task AsyncLifecycleAwaitsTaskOverridesAndOwnedAsyncResourcesBeforeCompletionEvents() {
		var calls = new List<string>();
		var startupGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var shutdownGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var resourceGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		await using var application = new TestApplication();
		var initializer = new AsyncInitializer(async token => {
			calls.Add("initializing task");
			await startupGate.Task.WaitAsync(token);
			calls.Add("initialized task");
		});
		var finalizer = new AsyncFinalizer(async _ => {
			calls.Add("finalizing task");
			await shutdownGate.Task;
			calls.Add("finalized task");
		});
		application.Configure(new[] { initializer }, new[] { finalizer });
		application.Disposables.Add(new AsyncResource(async () => {
			calls.Add("disposing resource");
			await resourceGate.Task;
			calls.Add("disposed resource");
		}));
		application.Initialized += () => calls.Add("initialized event");
		application.Finalized += () => calls.Add("finalized event");
		var startup = application.InitializeAsync();
		Assert.That(startup.IsCompleted, Is.False);
		Assert.That(application.IsInitialized, Is.False);
		startupGate.SetResult();
		await startup.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(calls, Is.EqualTo(new[] { "initializing task", "initialized task", "initialized event" }));
		var shutdown = application.FinalizeAsync();
		Assert.That(shutdown.IsCompleted, Is.False);
		shutdownGate.SetResult();
		await finalizer.Completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(shutdown.IsCompleted, Is.False);
		resourceGate.SetResult();
		await shutdown.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(calls, Is.EqualTo(new[] { "initializing task", "initialized task", "initialized event", "finalizing task", "finalized task",
			"disposing resource", "disposed resource", "finalized event" }));
		Assert.That(initializer.SynchronousCalls, Is.Zero);
		Assert.That(finalizer.SynchronousCalls, Is.Zero);
	}

	[Test]
	public async Task PendingStartupCoalescesAndAJoiningCancellationOnlyCancelsItsWait() {
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		await using var application = new TestApplication();
		var initializer = new AsyncInitializer(token => gate.Task.WaitAsync(token));
		application.Configure(new[] { initializer });
		using var cancellation = new CancellationTokenSource();
		var owner = application.InitializeAsync();
		var joining = application.InitializeAsync(cancellation.Token);
		cancellation.Cancel();
		Assert.That(async () => await joining, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(owner.IsCompleted, Is.False);
		Assert.That(initializer.AsynchronousCalls, Is.EqualTo(1));
		gate.SetResult();
		await owner.WaitAsync(TimeSpan.FromSeconds(5));
		application.Initialize();
		await application.InitializeAsync();
		Assert.That(initializer.AsynchronousCalls, Is.EqualTo(1));
		Assert.That(initializer.SynchronousCalls, Is.Zero);
	}

	[Test]
	public async Task StartupOwnerCancellationAllowsFinalizationWithoutRetryingStartup() {
		await using var application = new TestApplication();
		var initializer = new AsyncInitializer(token => Task.Delay(Timeout.Infinite, token));
		var finalizer = new AsyncFinalizer(_ => Task.CompletedTask);
		application.Configure(new[] { initializer }, new[] { finalizer });
		using var cancellation = new CancellationTokenSource();
		var startup = application.InitializeAsync(cancellation.Token);
		cancellation.Cancel();
		Assert.That(async () => await startup, Throws.InstanceOf<OperationCanceledException>());
		await initializer.Completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await application.FinalizeAsync();
		Assert.That(application.IsInitialized, Is.False);
		Assert.That(finalizer.AsynchronousCalls, Is.EqualTo(1));
		Assert.That(() => application.Initialize(), Throws.InvalidOperationException);
	}

	[Test]
	public async Task FinalizationWaitsForPendingStartupAndSharesItsShutdownWithOtherCallers() {
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		await using var application = new TestApplication();
		var initializer = new AsyncInitializer(token => gate.Task.WaitAsync(token));
		var finalizer = new AsyncFinalizer(_ => Task.CompletedTask);
		application.Configure(new[] { initializer }, new[] { finalizer });
		var startup = application.InitializeAsync();
		var first = application.FinalizeAsync();
		var second = application.FinalizeAsync();
		Assert.That(first.IsCompleted, Is.False);
		Assert.That(second.IsCompleted, Is.False);
		Assert.That(finalizer.AsynchronousCalls, Is.Zero);
		gate.SetResult();
		await Task.WhenAll(startup, first, second).WaitAsync(TimeSpan.FromSeconds(5));
		application.Finalize();
		Assert.That(finalizer.AsynchronousCalls, Is.EqualTo(1));
		Assert.That(finalizer.SynchronousCalls, Is.Zero);
		Assert.That(application.IsInitialized, Is.False);
	}

	[Test]
	public async Task CancellingAFinalizationWaitDoesNotInterruptCommittedShutdownOrAsyncDisposal() {
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var disposals = 0;
		var finalizer = new AsyncFinalizer(_ => gate.Task);
		var application = new TestApplication();
		application.Configure(finalizers: new[] { finalizer });
		application.Disposables.Add(new AsyncResource(() => {
			disposals++;
			return Task.CompletedTask;
		}));
		await application.InitializeAsync();
		using var cancellation = new CancellationTokenSource();
		var shutdown = application.FinalizeAsync(cancellation.Token);
		cancellation.Cancel();
		Assert.That(async () => await shutdown, Throws.InstanceOf<OperationCanceledException>());
		var disposal = application.DisposeAsync().AsTask();
		Assert.That(disposal.IsCompleted, Is.False);
		Assert.That(disposals, Is.Zero);
		gate.SetResult();
		await disposal.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(disposals, Is.EqualTo(1));
		Assert.That(finalizer.AsynchronousCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task DisposalCancelsPendingStartupAndAwaitsFinalizationAndOwnedCleanup() {
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var application = new TestApplication();
		var initializer = new AsyncInitializer(token => Task.Delay(Timeout.Infinite, token));
		var finalizer = new AsyncFinalizer(_ => gate.Task);
		application.Configure(new[] { initializer }, new[] { finalizer });
		var disposals = 0;
		application.Disposables.Add(() => disposals++);
		var startup = application.InitializeAsync();
		var disposal = application.DisposeAsync().AsTask();
		Assert.That(async () => await startup, Throws.InstanceOf<OperationCanceledException>());
		await finalizer.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(disposal.IsCompleted, Is.False);
		Assert.That(disposals, Is.Zero);
		gate.SetResult();
		await disposal.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(disposals, Is.EqualTo(1));
		Assert.That(finalizer.AsynchronousCalls, Is.EqualTo(1));
		Assert.That(() => application.Initialize(), Throws.InvalidOperationException);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ReadinessVetoDoesNotRunTasksOrFreezeConfigurationAndCanBeRetried(bool asynchronous) {
		await using var application = new TestApplication { Ready = false };
		var calls = 0;
		application.Configure(new[] { new SyncInitializer(() => calls++) });
		if (asynchronous)
			await application.InitializeAsync();
		else
			application.Initialize();
		Assert.That(application.IsInitialized, Is.False);
		Assert.That(calls, Is.Zero);
		application.Configure(new[] { new SyncInitializer(() => calls += 2) });
		application.Ready = true;
		await application.InitializeAsync();
		Assert.That(calls, Is.EqualTo(2));
		Assert.That(application.IsInitialized, Is.True);
	}

	[Test]
	public async Task AsyncFinalizationAttemptsEveryTaskAndResourceAndReportsFailuresAfterCleanup() {
		await using var application = new TestApplication();
		var calls = new List<string>();
		var taskError = new InvalidOperationException("task");
		var resourceError = new InvalidOperationException("resource");
		application.Configure(finalizers: new[] {
			new AsyncFinalizer(async _ => { await Task.Yield(); calls.Add("failed task"); throw taskError; }),
			new AsyncFinalizer(async _ => { await Task.Yield(); calls.Add("next task"); })
		});
		application.Disposables.Add(new AsyncResource(async () => { await Task.Yield(); calls.Add("failed resource"); throw resourceError; }));
		application.Disposables.Add(new AsyncResource(async () => { await Task.Yield(); calls.Add("next resource"); }));
		application.Finalized += () => calls.Add("finalized");
		await application.InitializeAsync();
		Exception error = null;
		try {
			await application.FinalizeAsync();
		} catch (Exception failure) {
			error = failure;
		}
		Assert.That(error, Is.TypeOf<AggregateException>());
		Assert.That(((AggregateException)error).Flatten().InnerExceptions, Is.EquivalentTo(new[] { taskError, resourceError }));
		Assert.That(calls, Is.EqualTo(new[] { "failed task", "next task", "failed resource", "next resource", "finalized" }));
		application.Finalize();
		await application.FinalizeAsync();
		Assert.That(calls.Count, Is.EqualTo(5));
	}

	[Test]
	public async Task DecoratorAwaitsLifecycleAndBothExitFormsForwardWithoutExposingTaskCollections() {
		await using var application = new TestApplication();
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var initializer = new AsyncInitializer(_ => gate.Task);
		application.Configure(new[] { initializer });
		await using var decorator = new TestDecorator(application);
		IApplication contract = decorator;
		var startup = contract.InitializeAsync();
		Assert.That(startup.IsCompleted, Is.False);
		gate.SetResult();
		await startup.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(contract.IsInitialized, Is.True);
		await contract.FinalizeAsync();
		Assert.That(await contract.ExitAsync(force: true), Is.True);
		Assert.That(contract.Exit(force: true), Is.True);
		Assert.That(typeof(IApplication).GetProperty("Initializers"), Is.Null);
		Assert.That(typeof(IApplication).GetProperty("Finalizers"), Is.Null);
		Assert.That(typeof(ApplicationBase).GetProperty("Initializers", BindingFlags.Public | BindingFlags.Instance), Is.Null);
		Assert.That(typeof(ApplicationBase).GetMethod("SetInitializers", BindingFlags.Public | BindingFlags.Instance), Is.Null);
	}

	[Test]
	public async Task SyncLifecycleAndDisposalRunSynchronouslyOnAContextWithoutStartingAsyncOverrides() {
		var calls = new List<string>();
		var application = new SyncTestApplication();
		application.Configure(new[] { new SyncInitializer(() => calls.Add("initialize")) }, new[] { new SyncFinalizer(() => calls.Add("finalize")) });
		application.Disposables.Add(() => calls.Add("dispose"));
		var previous = SynchronizationContext.Current;
		using (Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous))) {
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
			application.Initialize();
			application.Dispose();
			Assert.That(calls, Is.EqualTo(new[] { "initialize", "finalize", "dispose" }));
		}
		await application.DisposeAsync();
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ReentrantFinalizationDoesNotWaitOnItsOwnPendingShutdown(bool asynchronous) {
		await using var application = new TestApplication();
		var calls = 0;
		application.Configure(finalizers: new[] { new SyncFinalizer(() => calls++) });
		application.Finalizing += application.Finalize;
		application.Initialize();
		if (asynchronous)
			await application.FinalizeAsync().WaitAsync(TimeSpan.FromSeconds(5));
		else
			application.Finalize();
		Assert.That(calls, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ReentrantStartupIsRejectedInsteadOfWaitingOnItsOwnPendingInitialization(bool asynchronous) {
		await using var application = new TestApplication();
		application.Configure(new[] { new SyncInitializer(application.Initialize) });
		if (asynchronous)
			Assert.That(async () => await application.InitializeAsync().WaitAsync(TimeSpan.FromSeconds(5)), Throws.InvalidOperationException);
		else
			Assert.That(application.Initialize, Throws.InvalidOperationException);
	}

	[Test]
	public async Task SyncCallsOnAContextRejectPendingAsyncStartupWithoutStartingShutdownOrDisposal() {
		await using var application = new TestApplication();
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var finalizations = 0;
		application.Configure(new[] { new AsyncInitializer(_ => gate.Task) }, new[] { new SyncFinalizer(() => finalizations++) });
		var startup = application.InitializeAsync();
		var previous = SynchronizationContext.Current;
		using (Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous))) {
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
			Assert.That(application.Initialize, Throws.InvalidOperationException);
			Assert.That(application.Finalize, Throws.InvalidOperationException);
			Assert.That(application.Dispose, Throws.InvalidOperationException);
			Assert.That(finalizations, Is.Zero);
		}
		gate.SetResult();
		await startup.WaitAsync(TimeSpan.FromSeconds(5));
		application.Finalize();
		Assert.That(finalizations, Is.EqualTo(1));
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public async Task StartupCannotSynchronouslyFinalizeOrDisposeItsOwnPendingOperation(bool asynchronous, bool dispose) {
		await using var application = new TestApplication();
		var finalizations = 0;
		application.Configure(new[] { new SyncInitializer(() => {
			if (dispose)
				application.Dispose();
			else
				application.Finalize();
		}) }, new[] { new SyncFinalizer(() => finalizations++) });
		if (asynchronous)
			Assert.That(async () => await application.InitializeAsync().WaitAsync(TimeSpan.FromSeconds(5)), Throws.InvalidOperationException);
		else
			Assert.That(application.Initialize, Throws.InvalidOperationException);
		Assert.That(finalizations, Is.Zero);
		await application.FinalizeAsync();
		Assert.That(finalizations, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task StartupCannotAwaitItsOwnFinalizationOrDisposal(bool dispose) {
		await using var application = new TestApplication();
		var finalizations = 0;
		application.Configure(new[] { new AsyncInitializer(async _ => {
			await Task.Yield();
			if (dispose)
				await application.DisposeAsync();
			else
				await application.FinalizeAsync();
		}) }, new[] { new SyncFinalizer(() => finalizations++) });
		Assert.That(async () => await application.InitializeAsync().WaitAsync(TimeSpan.FromSeconds(5)), Throws.InvalidOperationException);
		Assert.That(finalizations, Is.Zero);
		await application.FinalizeAsync();
		Assert.That(finalizations, Is.EqualTo(1));
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public async Task FinalizerCannotDisposeItsOwnPendingShutdown(bool synchronous, bool disposing) {
		await using var application = new TestApplication();
		var finalizations = 0;
		var resources = 0;
		application.Configure(finalizers: new[] {
			new AsyncFinalizer(async _ => {
				await Task.Yield();
				if (synchronous)
					application.Dispose();
				else
					await application.DisposeAsync();
			}),
			new AsyncFinalizer(_ => { finalizations++; return Task.CompletedTask; })
		});
		application.Disposables.Add(() => resources++);
		await application.InitializeAsync();
		var shutdown = disposing ? application.DisposeAsync().AsTask() : application.FinalizeAsync();
		Assert.That(async () => await shutdown.WaitAsync(TimeSpan.FromSeconds(5)), Throws.TypeOf<AggregateException>().With.InnerException.TypeOf<InvalidOperationException>());
		Assert.That(finalizations, Is.EqualTo(1));
		Assert.That(resources, Is.EqualTo(1));
		Assert.That(application.Disposables, Is.Empty);
	}

	private sealed class TestApplication : AsyncApplicationBase {
		public bool Ready { get; set; } = true;
		public override IApplicationPlugin[] LoadedPlugins => Array.Empty<IApplicationPlugin>();
		public override IApplicationScreenHost ScreenHost => null;

		public void Configure(IEnumerable<IApplicationInitializer> initializers = null, IEnumerable<IApplicationFinalizer> finalizers = null) {
			SetInitializers(initializers ?? Array.Empty<IApplicationInitializer>());
			SetFinalizers(finalizers ?? Array.Empty<IApplicationFinalizer>());
		}

		protected override Task<bool> OnInitializeAsync(CancellationToken cancellationToken) => Task.FromResult(Ready);
	}

	private sealed class TestDecorator(IApplication application) : ApplicationDecorator(application);

	private sealed class SyncTestApplication : SyncApplicationBase {
		public override IApplicationPlugin[] LoadedPlugins => Array.Empty<IApplicationPlugin>();
		public override IApplicationScreenHost ScreenHost => null;

		public void Configure(IEnumerable<IApplicationInitializer> initializers, IEnumerable<IApplicationFinalizer> finalizers) {
			SetInitializers(initializers);
			SetFinalizers(finalizers);
		}
	}

	private sealed class AsyncInitializer(Func<CancellationToken, Task> action) : AsyncApplicationInitializerBase {
		public int SynchronousCalls { get; private set; }
		public int AsynchronousCalls { get; private set; }
		public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public override void Initialize() {
			SynchronousCalls++;
			base.Initialize();
		}

		public override async Task InitializeAsync(CancellationToken cancellationToken = default) {
			AsynchronousCalls++;
			using var completion = Tools.Scope.ExecuteOnDispose(() => Completed.TrySetResult());
			await action(cancellationToken);
		}
	}

#pragma warning disable CS0465 // Implements the existing synchronous task contract.
	private sealed class AsyncFinalizer(Func<CancellationToken, Task> action) : AsyncApplicationFinalizerBase {
		public int SynchronousCalls { get; private set; }
		public int AsynchronousCalls { get; private set; }
		public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public override void Finalize() {
			SynchronousCalls++;
			base.Finalize();
		}

		public override async Task FinalizeAsync(CancellationToken cancellationToken = default) {
			AsynchronousCalls++;
			Started.TrySetResult();
			using var completion = Tools.Scope.ExecuteOnDispose(() => Completed.TrySetResult());
			await action(cancellationToken);
		}
	}

	private sealed class SyncFinalizer(Action action) : SyncApplicationFinalizerBase {
		public override int Priority => 0;
		public override void Finalize() => action();
	}
#pragma warning restore CS0465

	private sealed class SyncInitializer(Action action) : SyncApplicationInitializerBase {
		public override int Priority => 0;
		public override void Initialize() => action();
	}

	private sealed class AsyncResource(Func<Task> dispose) : IDisposable, IAsyncDisposable {
		public void Dispose() => throw new InvalidOperationException("The resource requires asynchronous disposal.");
		public async ValueTask DisposeAsync() => await dispose();
	}
}
