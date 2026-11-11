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

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class DataSourceLifetimeTests {
	private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

	[Test]
	public async Task ClosingRegistrationDisablesImmediatelyAndGlobalCompletionDrainsItsPendingStop() {
		var entered = Signal();
		var finish = Signal();
		using var release = Tools.Scope.ExecuteOnDispose(() => finish.TrySetResult());
		var source = new OwnedSource(() => { entered.SetResult(); return finish.Task; }) { Enabled = true };
		await using var lifetime = new DataSourceLifetime();
		var registration = lifetime.Register(source);
		Assert.That(source.StopCalls, Is.Zero);
		registration.Dispose();
		Assert.That(source.Enabled, Is.False);
		await entered.Task.WaitAsync(Deadline);
		var completion = lifetime.CompleteAsync();
		Assert.That(completion, Is.SameAs(lifetime.CompleteAsync()));
		Assert.That(completion.IsCompleted, Is.False);
		registration.Dispose();
		finish.SetResult();
		await completion.WaitAsync(Deadline);
		Assert.That(source.StopCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task CompleteAndConcurrentRegistrationReleaseCannotLoseAnAcceptedStop() {
		var finish = Signal();
		using var release = Tools.Scope.ExecuteOnDispose(() => finish.TrySetResult());
		await using var lifetime = new DataSourceLifetime();
		var first = new OwnedSource(() => finish.Task) { Enabled = true };
		var second = new OwnedSource(() => finish.Task) { Enabled = true };
		var registration = lifetime.Register(first);
		lifetime.Register(second);
		var closing = Task.Run(registration.Dispose);
		var completion = lifetime.CompleteAsync();
		await closing.WaitAsync(Deadline);
		Assert.That(completion.IsCompleted, Is.False);
		Assert.That(first.Enabled, Is.False);
		Assert.That(second.Enabled, Is.False);
		Assert.That(() => lifetime.Register(new OwnedSource(() => Task.CompletedTask)), Throws.TypeOf<InvalidOperationException>());
		finish.SetResult();
		await completion.WaitAsync(Deadline);
		Assert.That(first.StopCalls, Is.EqualTo(1));
		Assert.That(second.StopCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task SynchronousStopPrefixCannotBlockTheClosingThread() {
		using var entered = new ManualResetEventSlim();
		using var finish = new ManualResetEventSlim();
		using var release = Tools.Scope.ExecuteOnDispose(finish.Set);
		var caller = Environment.CurrentManagedThreadId;
		var worker = 0;
		var source = new OwnedSource(() => {
			worker = Environment.CurrentManagedThreadId;
			entered.Set();
			Guard.Ensure(finish.Wait(Deadline), "The closing caller must release the worker-side stop prefix.");
			return Task.CompletedTask;
		});
		await using var lifetime = new DataSourceLifetime();
		var registration = lifetime.Register(source);
		registration.Dispose();
		Assert.That(entered.Wait(Deadline), Is.True);
		Assert.That(worker, Is.Not.EqualTo(caller));
		var completion = lifetime.CompleteAsync();
		Assert.That(completion.IsCompleted, Is.False);
		finish.Set();
		await completion.WaitAsync(Deadline);
	}

	[Test]
	public async Task OneFailedStopDoesNotSkipOtherSourcesAndItsFailureReachesGlobalFinalization() {
		var failure = new InvalidOperationException("Source cleanup failed");
		var first = new OwnedSource(() => Task.FromException(failure));
		var second = new OwnedSource(() => Task.CompletedTask);
		var lifetime = new DataSourceLifetime();
		lifetime.Register(first);
		lifetime.Register(second);
		var completion = lifetime.CompleteAsync();
		await Assert.ThatAsync(() => completion, Throws.Exception.SameAs(failure));
		Assert.That(first.StopCalls, Is.EqualTo(1));
		Assert.That(second.StopCalls, Is.EqualTo(1));
		lifetime.Dispose();
	}

	[Test]
	public async Task OwnerCancellationDrainsARealRefreshWithoutReportingExpectedShutdownAsFailure() {
		var entered = Signal();
		await using var source = new PollingDataSource<int>(new ActionRefreshableDataSource<int>(() => new ActionAsyncListDataSource<int>(async token => {
			entered.SetResult();
			await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
			return Array.Empty<int>();
		})));
		await using var lifetime = new DataSourceLifetime();
		lifetime.Register(source);
		var pending = source.RefreshAsync();
		await entered.Task.WaitAsync(Deadline);
		await lifetime.CompleteAsync().WaitAsync(Deadline);
		await Assert.ThatAsync(() => pending, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(source.Enabled, Is.False);
	}

	[Test]
	public async Task OwnedCaptureCancellationWaitsForActualShutdownAndFactorySetupRunsOnWorker() {
		using var caller = new CancellationTokenSource();
		var entered = Signal();
		var canceled = Signal();
		var finish = new TaskCompletionSource<int[]>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var release = Tools.Scope.ExecuteOnDispose(() => finish.TrySetResult(Array.Empty<int>()));
		using var factoryEntered = new ManualResetEventSlim();
		using var releaseFactory = new ManualResetEventSlim();
		using var factoryCleanup = Tools.Scope.ExecuteOnDispose(releaseFactory.Set);
		var callerThread = Environment.CurrentManagedThreadId;
		var factoryThread = 0;
		ActionRefreshableDataSource<int> owned = null;
		var capture = Tools.DataSources.CaptureAsync<int>(() => {
			factoryThread = Environment.CurrentManagedThreadId;
			factoryEntered.Set();
			Guard.Ensure(releaseFactory.Wait(Deadline), "The factory must be dispatched before the caller yields.");
			owned = new ActionRefreshableDataSource<int>(() => new ActionAsyncListDataSource<int>(async token => {
				token.Register(() => canceled.TrySetResult());
				entered.SetResult();
				return await finish.Task;
			}));
			return owned;
		}, caller.Token);
		Assert.That(factoryEntered.Wait(Deadline), Is.True);
		Assert.That(factoryThread, Is.Not.EqualTo(callerThread));
		releaseFactory.Set();
		await entered.Task.WaitAsync(Deadline);
		caller.Cancel();
		await canceled.Task.WaitAsync(Deadline);
		Assert.That(capture.IsCompleted, Is.False, "Cancellation must not abandon a source whose callback still owns resources.");
		finish.SetResult(new[] { 42 });
		await Assert.ThatAsync(() => capture, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(owned.RequiresLoad, Is.True, "Late canceled capture must not publish.");
		Assert.That(() => owned.RefreshAsync(), Throws.TypeOf<ObjectDisposedException>());
	}

	private static TaskCompletionSource Signal() => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

	#region Inner Types
	private sealed class OwnedSource : IRefreshingDataSource {
		public bool AutoRefresh { get; set; } = true;
		private readonly Func<Task> _stop;
		private int _stopCalls;

		public OwnedSource(Func<Task> stop) => _stop = stop;

		public int StopCalls => Volatile.Read(ref _stopCalls);
		public bool Enabled { get; set; }
		public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromSeconds(1);
		public event EventHandlerEx<Exception> RefreshFailed { add { } remove { } }

		public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

		public Task StopAsync() {
			Interlocked.Increment(ref _stopCalls);
			return _stop();
		}
	}
	#endregion
}
