// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests.Lifecycle;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationTaskBaseTests {
	[TestCase(typeof(IApplicationInitializer), "Initialize", "InitializeAsync")]
	[TestCase(typeof(IApplicationFinalizer), "Finalize", "FinalizeAsync")]
	[TestCase(typeof(ApplicationInitializerBase), "Initialize", "InitializeAsync")]
	[TestCase(typeof(ApplicationFinalizerBase), "Finalize", "FinalizeAsync")]
	public void NeutralContractsRequireBothExecutionDirections(Type type, string synchronous, string asynchronous) {
		Assert.That(type.GetMethod(synchronous).IsAbstract, Is.True);
		Assert.That(type.GetMethod(asynchronous).IsAbstract, Is.True);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SyncPrimitiveRunsOnTheCallingThread(bool finalizer) {
		var callerThread = Environment.CurrentManagedThreadId;
		var executingThread = 0;
		ExecuteSync(finalizer, () => executingThread = Environment.CurrentManagedThreadId);
		Assert.That(executingThread, Is.EqualTo(callerThread));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task SyncTaskAsyncAdapterReturnsBeforeWorkerCompletes(bool finalizer) {
		using var release = new ManualResetEventSlim();
		using var cleanup = Tools.Scope.ExecuteOnDispose(release.Set);
		var started = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		var published = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
		var callerThread = 0;
		var caller = new Thread(() => {
			callerThread = Environment.CurrentManagedThreadId;
			SynchronizationContext.SetSynchronizationContext(new RejectingSynchronizationContext());
			published.SetResult(ExecuteSyncAsync(finalizer, () => {
				started.SetResult(Environment.CurrentManagedThreadId);
				release.Wait();
			}));
		}) { IsBackground = true };
		caller.Start();
		var operation = await published.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var workerThread = await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(workerThread, Is.Not.EqualTo(callerThread));
		Assert.That(operation.IsCompleted, Is.False);
		release.Set();
		await operation.WaitAsync(TimeSpan.FromSeconds(5));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AsyncTaskSyncAdapterWaitsForCompletion(bool finalizer) {
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
		var completed = false;
		var invocation = Task.Run(() => ExecuteAsyncSync(finalizer, async _ => {
			started.SetResult();
			await release.Task;
			completed = true;
		}));
		await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(invocation.IsCompleted, Is.False);
		Assert.That(completed, Is.False);
		release.SetResult();
		await invocation.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(completed, Is.True);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void AsyncTaskSyncAdapterStartsTheOperationOutsideTheCallingContext(bool finalizer) {
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(new RejectingSynchronizationContext());
		ExecuteAsyncSync(finalizer, async _ => {
			Assert.That(SynchronizationContext.Current, Is.Null);
			await Task.Yield();
			completed = true;
		});
		Assert.That(completed, Is.True);
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public async Task DirectionAdaptersPropagateTheOriginalFailure(bool finalizer, bool asynchronous) {
		var failure = new InvalidOperationException("task failure");
		Exception actual;
		if (asynchronous)
			Tools.Exceptions.ExecuteCapturingException(() => ExecuteAsyncSync(finalizer, async _ => { await Task.Yield(); throw failure; }), out actual);
		else {
			actual = null;
			try {
				await ExecuteSyncAsync(finalizer, () => throw failure);
			} catch (Exception error) {
				actual = error;
			}
		}
		Assert.That(actual, Is.SameAs(failure));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task LicenseInitializerWaitsForTheActualVerifier(bool synchronous) {
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
		using var cancellation = new CancellationTokenSource();
		var verifier = new LicenseVerifier(token => {
			Assert.That(token, Is.EqualTo(synchronous ? CancellationToken.None : cancellation.Token));
			started.SetResult();
			return release.Task;
		});
		var initializer = new VerifyLicenseInitializer(verifier, null);
		var operation = synchronous ? Task.Run(initializer.Initialize) : initializer.InitializeAsync(cancellation.Token);
		await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(operation.IsCompleted, Is.False);
		Assert.That(initializer.Priority, Is.EqualTo(int.MaxValue));
		Assert.That(initializer.Parallelizable, Is.True);
		release.SetResult();
		await operation.WaitAsync(TimeSpan.FromSeconds(5));
	}

	private static void ExecuteSync(bool finalizer, Action action) {
		if (finalizer)
			new SyncFinalizer(action).Finalize();
		else
			new SyncInitializer(action).Initialize();
	}

	private static Task ExecuteSyncAsync(bool finalizer, Action action)
		=> finalizer ? new SyncFinalizer(action).FinalizeAsync() : new SyncInitializer(action).InitializeAsync();

	private static void ExecuteAsyncSync(bool finalizer, Func<CancellationToken, Task> action) {
		if (finalizer)
			new AsyncFinalizer(action).Finalize();
		else
			new AsyncInitializer(action).Initialize();
	}

	private sealed class SyncInitializer(Action action) : SyncApplicationInitializerBase {
		public override void Initialize() => action();
	}

	private sealed class AsyncInitializer(Func<CancellationToken, Task> action) : AsyncApplicationInitializerBase {
		public override Task InitializeAsync(CancellationToken cancellationToken = default) => action(cancellationToken);
	}

	private sealed class SyncFinalizer(Action action) : SyncApplicationFinalizerBase {
#pragma warning disable CS0465 // Implements the existing synchronous task contract.
		public override void Finalize() => action();
#pragma warning restore CS0465
	}

	private sealed class AsyncFinalizer(Func<CancellationToken, Task> action) : AsyncApplicationFinalizerBase {
		public override Task FinalizeAsync(CancellationToken cancellationToken = default) => action(cancellationToken);
	}

	private sealed class LicenseVerifier(Func<CancellationToken, Task> action) : IBackgroundLicenseVerifier {
		public Task VerifyLicense(CancellationToken cancellationToken) => action(cancellationToken);
	}

	private sealed class RejectingSynchronizationContext : SynchronizationContext {
		public override void Post(SendOrPostCallback callback, object state)
			=> throw new InvalidOperationException("The synchronous adapter must not post to its blocked calling context.");
	}
}
