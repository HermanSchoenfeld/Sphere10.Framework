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
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationExitFinalizationTests {
	[Test]
	public async Task SyncExitCannotReportCompletionWhenItsContextCannotJoinPendingFinalization() {
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var application = new SyncApplication(() => {
			started.TrySetResult();
			release.Task.GetAwaiter().GetResult();
		});
		using var releaseWork = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
		var resource = new Resource();
		application.Disposables.Add(resource);
		application.Initialize();
		var finalization = application.FinalizeAsync();
		await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var attempt = Task.Run(() => {
			var previous = SynchronizationContext.Current;
			using var context = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
			SynchronizationContext.SetSynchronizationContext(new RejectingSynchronizationContext());
			Tools.Exceptions.ExecuteCapturingException(() => application.Exit(force: true), out var error);
			return error;
		});

		Assert.That(await attempt.WaitAsync(TimeSpan.FromSeconds(5)), Is.TypeOf<InvalidOperationException>());
		Assert.That(finalization.IsCompleted, Is.False);
		Assert.That(application.HasExited, Is.False);
		Assert.That(application.IsExiting, Is.False);
		Assert.That(resource.Disposals, Is.Zero);
		release.SetResult();
		await finalization.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(application.Exit(force: true), Is.True);
		Assert.That(application.HasExited, Is.True);
		Assert.That(resource.Disposals, Is.EqualTo(1));
	}

	[Test]
	public async Task AsyncApplicationsSyncExitCannotBlockTheContextOfPendingFinalization() {
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		SynchronizationContext finalizerContext = null;
		await using var application = new AsyncApplication(async () => {
			finalizerContext = SynchronizationContext.Current;
			started.TrySetResult();
			await release.Task;
		});
		using var releaseWork = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
		var resource = new Resource();
		application.Disposables.Add(resource);
		await application.InitializeAsync();
		var callerContext = new RecordingSynchronizationContext();
		var previous = SynchronizationContext.Current;
		Task finalization;
		using (Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous))) {
			SynchronizationContext.SetSynchronizationContext(callerContext);
			finalization = application.FinalizeAsync();
		}
		await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var attempt = Task.Run(() => {
			var workerContext = SynchronizationContext.Current;
			using var context = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(workerContext));
			SynchronizationContext.SetSynchronizationContext(callerContext);
			Tools.Exceptions.ExecuteCapturingException(() => application.Exit(force: true), out var error);
			return error;
		});

		Assert.That(await attempt.WaitAsync(TimeSpan.FromSeconds(5)), Is.TypeOf<InvalidOperationException>());
		Assert.That(finalizerContext, Is.SameAs(callerContext));
		Assert.That(finalization.IsCompleted, Is.False);
		Assert.That(application.HasExited, Is.False);
		Assert.That(application.IsExiting, Is.False);
		Assert.That(resource.Disposals, Is.Zero);
		release.SetResult();
		await finalization.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(callerContext.Posts, Is.GreaterThan(0), "The awaited finalizer must resume through its captured caller context.");
		Assert.That(await application.ExitAsync(force: true).WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(application.HasExited, Is.True);
		Assert.That(resource.Disposals, Is.EqualTo(1));
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public async Task AFinalizerCannotStartItsOwnExitBeforeCleanupCompletes(bool asynchronousApplication, bool asynchronousExit) {
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		ApplicationBase application = null;
		Exception exitError = null;
		Action syncFinalizer = () => {
			try {
				var exit = asynchronousExit ? application.ExitAsync(force: true) : Task.Run(() => application.Exit(force: true));
				exit.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
			} catch (Exception error) {
				exitError = error;
			}
			started.TrySetResult();
			release.Task.GetAwaiter().GetResult();
		};
		Func<Task> asyncFinalizer = async () => {
			try {
				var exit = asynchronousExit ? application.ExitAsync(force: true) : Task.Run(() => application.Exit(force: true));
				await exit.WaitAsync(TimeSpan.FromSeconds(5));
			} catch (Exception error) {
				exitError = error;
			}
			started.TrySetResult();
			await release.Task;
		};
		application = asynchronousApplication ? new AsyncApplication(asyncFinalizer) : new SyncApplication(syncFinalizer);
		await using var ownedApplication = application;
		using var releaseWork = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
		var resource = new Resource();
		application.Disposables.Add(resource);
		await application.InitializeAsync();
		var finalization = application.FinalizeAsync();
		await started.Task.WaitAsync(TimeSpan.FromSeconds(10));

		Assert.That(exitError, Is.TypeOf<InvalidOperationException>());
		Assert.That(exitError.Message, Does.Contain("cannot start its own exit"));
		Assert.That(finalization.IsCompleted, Is.False);
		Assert.That(application.IsExiting, Is.False);
		Assert.That(application.HasExited, Is.False);
		Assert.That(resource.Disposals, Is.Zero);
		release.SetResult();
		await finalization.WaitAsync(TimeSpan.FromSeconds(5));
		var retry = asynchronousExit ? application.ExitAsync(force: true) : Task.Run(() => application.Exit(force: true));
		Assert.That(await retry.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(application.HasExited, Is.True);
		Assert.That(resource.Disposals, Is.EqualTo(1));
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public async Task FailureBeforeBaseFinalizationRemainsRetryable(bool asynchronousApplication, bool asynchronousExit) {
		var failure = new InvalidOperationException("finalization rejected before cleanup");
		Exception currentFailure = failure;
		var finalizers = 0;
		await using ApplicationBase application = asynchronousApplication
			? new AsyncApplication(() => {
				finalizers++;
				return Task.CompletedTask;
			}, () => currentFailure)
			: new SyncApplication(() => finalizers++, () => currentFailure);
		using var allowCleanup = Tools.Scope.ExecuteOnDispose(() => currentFailure = null);
		var resource = new Resource();
		application.Disposables.Add(resource);
		await application.InitializeAsync();
		Exception exitError = null;
		try {
			var exit = asynchronousExit ? application.ExitAsync(force: true) : Task.Run(() => application.Exit(force: true));
			await exit.WaitAsync(TimeSpan.FromSeconds(5));
		} catch (Exception error) {
			exitError = error;
		}

		Assert.That(exitError, Is.SameAs(failure));
		Assert.That(application.IsInitialized, Is.True);
		Assert.That(application.IsExiting, Is.False);
		Assert.That(application.HasExited, Is.False);
		Assert.That(finalizers, Is.Zero);
		Assert.That(resource.Disposals, Is.Zero);
		currentFailure = null;
		var retry = asynchronousExit ? application.ExitAsync(force: true) : Task.Run(() => application.Exit(force: true));
		Assert.That(await retry.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(application.HasExited, Is.True);
		Assert.That(finalizers, Is.EqualTo(1));
		Assert.That(resource.Disposals, Is.EqualTo(1));
	}

	private sealed class SyncApplication : SyncApplicationBase {
		private readonly Func<Exception> _finalizationFailure;

		public SyncApplication(Action finalizer, Func<Exception> finalizationFailure = null) {
			_finalizationFailure = finalizationFailure;
			SetFinalizers(new[] { new SyncFinalizer(finalizer) });
		}

		public override IApplicationPlugin[] LoadedPlugins => Array.Empty<IApplicationPlugin>();
		public override IApplicationScreenHost ScreenHost => null;

#pragma warning disable CS0465 // Implements the synchronous application lifecycle contract.
		public override void Finalize() {
			var error = _finalizationFailure?.Invoke();
			if (error != null)
				throw error;
			base.Finalize();
		}
#pragma warning restore CS0465
	}

	private sealed class AsyncApplication : AsyncApplicationBase {
		private readonly Func<Exception> _finalizationFailure;

		public AsyncApplication(Func<Task> finalizer, Func<Exception> finalizationFailure = null) {
			_finalizationFailure = finalizationFailure;
			SetFinalizers(new[] { new AsyncFinalizer(finalizer) });
		}

		public override IApplicationPlugin[] LoadedPlugins => Array.Empty<IApplicationPlugin>();
		public override IApplicationScreenHost ScreenHost => null;

		public override Task FinalizeAsync(CancellationToken cancellationToken = default) {
			var error = _finalizationFailure?.Invoke();
			return error == null ? base.FinalizeAsync(cancellationToken) : Task.FromException(error);
		}
	}

	private sealed class SyncFinalizer(Action action) : SyncApplicationFinalizerBase {
#pragma warning disable CS0465 // Implements the synchronous task contract.
		public override void Finalize() => action();
#pragma warning restore CS0465
	}

	private sealed class AsyncFinalizer(Func<Task> action) : AsyncApplicationFinalizerBase {
		public override Task FinalizeAsync(CancellationToken cancellationToken = default) => action();
	}

	private sealed class Resource : IDisposable {
		public int Disposals { get; private set; }

		public void Dispose() => Disposals++;
	}

	private sealed class RecordingSynchronizationContext : SynchronizationContext {
		private int _posts;

		public int Posts => Volatile.Read(ref _posts);

		public override void Post(SendOrPostCallback callback, object state) {
			Interlocked.Increment(ref _posts);
			base.Post(callback, state);
		}
	}

	private sealed class RejectingSynchronizationContext : SynchronizationContext {
		public override void Post(SendOrPostCallback callback, object state)
			=> throw new InvalidOperationException("Pending cleanup cannot post to a blocked caller context.");
	}
}
