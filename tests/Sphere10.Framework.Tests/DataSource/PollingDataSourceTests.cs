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
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[NonParallelizable]
public class PollingDataSourceTests {
	private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(40);
	private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

	[Test]
	public async Task ConstructionAndDisabledConfigurationScheduleNothingAndManualRefreshStillRefreshes() {
		using var scheduler = StartedScheduler();
		var calls = 0;
		await using var owner = Polling(_ => { calls++; return Rows(); }, scheduler);
		owner.RefreshInterval = TimeSpan.FromSeconds(4);
		owner.AutoRefresh = true;
		Assert.That(owner.Enabled, Is.False);
		Assert.That(calls, Is.Zero);
		Assert.That(scheduler.GetJobs(), Is.Empty);
		await owner.RefreshAsync();
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(scheduler.GetJobs(), Is.Empty, "A manual refresh does not need an automatic job.");
	}

	[Test]
	public async Task TheFetchRunsOutsideTheCallerThread() {
		var caller = Environment.CurrentManagedThreadId;
		var fetchThread = 0;
		using var entered = new ManualResetEventSlim();
		using var hold = new ManualResetEventSlim();
		await using var owner = Polling(_ => {
			fetchThread = Environment.CurrentManagedThreadId;
			entered.Set();
			Guard.Ensure(hold.Wait(Deadline), "The caller must release the synchronous fetch.");
			return Rows();
		});
		var operation = owner.RefreshAsync();
		Assert.That(entered.Wait(Deadline), Is.True, "Synchronous fetch setup must start while its caller remains occupied.");
		hold.Set();
		await operation.WaitAsync(Deadline);
		Assert.That(fetchThread, Is.Not.EqualTo(caller));
	}

	[Test]
	public async Task ConcurrentManualRequestsJoinOneRefresh() {
		var calls = 0;
		var entered = Signal();
		var finish = Capture();
		using var release = Tools.Scope.ExecuteOnDispose(() => finish.TrySetResult(Array.Empty<Row>()));
		await using var owner = Polling(_ => { Interlocked.Increment(ref calls); entered.TrySetResult(); return finish.Task; });
		var first = owner.RefreshAsync();
		await entered.Task.WaitAsync(Deadline);
		var joined = Enumerable.Range(0, 12).Select(_ => owner.RefreshAsync()).ToArray();
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(joined, Is.All.Matches<Task>(task => !task.IsCompleted));
		finish.SetResult(Array.Empty<Row>());
		await Task.WhenAll(joined.Append(first)).WaitAsync(Deadline);
		Assert.That(calls, Is.EqualTo(1));
	}

	[Test]
	public async Task CallerCancellationOnlyCancelsItsWaitAndNotTheSharedFetch() {
		using var caller = new CancellationTokenSource();
		var entered = Signal();
		var finish = Capture();
		using var release = Tools.Scope.ExecuteOnDispose(() => finish.TrySetResult(Array.Empty<Row>()));
		CancellationToken captured = default;
		await using var owner = Polling(token => { captured = token; entered.SetResult(); return finish.Task; });
		var first = owner.RefreshAsync(caller.Token);
		await entered.Task.WaitAsync(Deadline);
		var second = owner.RefreshAsync();
		caller.Cancel();
		await Assert.ThatAsync(() => first, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(captured.IsCancellationRequested, Is.False);
		Assert.That(second.IsCompleted, Is.False);
		finish.SetResult(Array.Empty<Row>());
		await second.WaitAsync(Deadline);
	}

	[Test]
	public async Task EnablingSchedulesOneRepeatingJobWhichRefreshesEveryIntervalWithoutImmediateCollection() {
		using var scheduler = StartedScheduler();
		var calls = 0;
		await using var owner = Polling(_ => { Interlocked.Increment(ref calls); return Rows(); }, scheduler);
		owner.RefreshInterval = Interval;
		owner.Enabled = true;
		Assert.That(calls, Is.Zero, "Enabling schedules the first refresh one interval later.");
		Assert.That(scheduler.GetJobs().Count(), Is.EqualTo(1));
		await WaitUntilAsync(() => Volatile.Read(ref calls) >= 3);
		Assert.That(scheduler.GetJobs().Count(), Is.EqualTo(1), "The repeating job stays registered between refreshes.");
	}

	[Test]
	public async Task TheJobFiresTheRefreshWithoutBlockingAndOverlappingTicksJoinIt() {
		using var scheduler = StartedScheduler();
		var calls = 0;
		var entered = Signal();
		var finish = Capture();
		using var release = Tools.Scope.ExecuteOnDispose(() => finish.TrySetResult(Array.Empty<Row>()));
		await using var owner = Polling(_ => {
			Interlocked.Increment(ref calls);
			entered.TrySetResult();
			return finish.Task;
		}, scheduler);
		owner.RefreshInterval = Interval;
		owner.Enabled = true;
		await entered.Task.WaitAsync(Deadline);
		await Task.Delay(Interval * 5);
		Assert.That(Volatile.Read(ref calls), Is.EqualTo(1), "Ticks during a slow fetch join it instead of fetching again.");
		finish.SetResult(Array.Empty<Row>());
		await WaitUntilAsync(() => Volatile.Read(ref calls) >= 2);
	}

	[Test]
	public async Task PolicyAndIntervalChangesReplaceTheJobAndDisablingEitherGateRemovesIt() {
		using var scheduler = StartedScheduler();
		await using var owner = Polling(_ => Rows(), scheduler);
		owner.RefreshInterval = TimeSpan.FromHours(1);
		owner.Enabled = true;
		var job = scheduler.GetJobs().Single();
		owner.Enabled = true;
		Assert.That(scheduler.GetJobs().Single(), Is.SameAs(job), "An unchanged policy keeps the current job and its due time.");
		owner.RefreshInterval = TimeSpan.FromHours(2);
		Assert.That(scheduler.GetJobs().Single(), Is.Not.SameAs(job), "A new interval installs a new job.");
		owner.AutoRefresh = false;
		Assert.That(scheduler.GetJobs(), Is.Empty);
		owner.AutoRefresh = true;
		Assert.That(scheduler.GetJobs().Count(), Is.EqualTo(1));
		owner.Enabled = false;
		Assert.That(scheduler.GetJobs(), Is.Empty);
		Assert.That(owner.AutoRefresh, Is.True, "Lifecycle admission must not overwrite the user's refresh choice.");
	}

	[Test]
	public async Task AnInfiniteIntervalLeavesAutomaticRefreshToTheOwnerClock() {
		using var scheduler = StartedScheduler();
		var calls = 0;
		await using var owner = Polling(_ => { calls++; return Rows(); }, scheduler);
		owner.RefreshInterval = Timeout.InfiniteTimeSpan;
		owner.Enabled = true;
		Assert.That(owner.AutoRefresh, Is.True);
		Assert.That(scheduler.GetJobs(), Is.Empty);
		await owner.RefreshAsync();
		Assert.That(calls, Is.EqualTo(1));
	}

	[Test]
	public async Task FailureIsReportedOnceAndAutomaticRefreshContinues() {
		using var scheduler = StartedScheduler();
		var calls = 0;
		var failures = new List<Exception>();
		var error = new InvalidOperationException("Collection failed");
		await using var owner = Polling(_ => Interlocked.Increment(ref calls) == 1 ? Task.FromException<IEnumerable<Row>>(error) : Rows(), scheduler);
		owner.RefreshFailed += failure => { lock (failures) failures.Add(failure); };
		owner.RefreshInterval = Interval;
		owner.Enabled = true;
		await WaitUntilAsync(() => Volatile.Read(ref calls) >= 3);
		lock (failures)
			Assert.That(failures, Is.EqualTo(new[] { error }));
		Assert.That(scheduler.GetJobs().Count(), Is.EqualTo(1), "A failed refresh does not remove the automatic job.");
	}

	[Test]
	public async Task ExplicitRefreshFailuresAreReportedOnceAndRethrownAndSubscriberFailureIsIsolated() {
		var notified = 0;
		await using var owner = Polling(_ => Task.FromException<IEnumerable<Row>>(new InvalidOperationException("Expected refresh failure")));
		owner.RefreshFailed += _ => throw new InvalidOperationException("Broken consumer");
		owner.RefreshFailed += _ => { owner.Enabled = false; owner.RefreshInterval = TimeSpan.FromSeconds(2); notified++; };
		await Assert.ThatAsync(() => owner.RefreshAsync(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Expected refresh failure"));
		await WaitUntilAsync(() => notified == 1);
		Assert.That(() => owner.Refresh(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Expected refresh failure"));
		Assert.That(notified, Is.EqualTo(2), "The synchronous direction reports its failure too.");
	}

	[Test]
	public async Task StopCancelsAndDrainsAcceptedWorkAndPermanentlyRejectsNewRefreshes() {
		using var scheduler = StartedScheduler();
		var entered = Signal();
		var canceled = Signal();
		var finish = Capture();
		using var release = Tools.Scope.ExecuteOnDispose(() => finish.TrySetResult(Array.Empty<Row>()));
		var failures = 0;
		await using var owner = Polling(token => {
			token.Register(() => canceled.TrySetResult());
			entered.SetResult();
			return finish.Task;
		}, scheduler);
		owner.RefreshFailed += _ => failures++;
		owner.RefreshInterval = TimeSpan.FromHours(1);
		owner.Enabled = true;
		var pending = owner.RefreshAsync();
		await entered.Task.WaitAsync(Deadline);
		var stopped = owner.StopAsync();
		await canceled.Task.WaitAsync(Deadline);
		Assert.That(stopped, Is.SameAs(owner.StopAsync()));
		Assert.That(stopped.IsCompleted, Is.False);
		Assert.That(owner.Enabled, Is.False);
		Assert.That(owner.IsStopped, Is.True);
		Assert.That(scheduler.GetJobs(), Is.Empty, "Stopping removes the automatic job at once.");
		owner.Enabled = false;
		owner.AutoRefresh = false;
		Assert.That(() => owner.Enabled = true, Throws.TypeOf<ObjectDisposedException>());
		Assert.That(() => owner.RefreshInterval = TimeSpan.FromSeconds(2), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(() => owner.RefreshAsync(), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(() => owner.Load(), Throws.TypeOf<ObjectDisposedException>());
		finish.SetResult(Array.Empty<Row>());
		await Assert.ThatAsync(() => pending, Throws.InstanceOf<OperationCanceledException>());
		await stopped.WaitAsync(Deadline);
		Assert.That(failures, Is.Zero, "Expected lifetime cancellation is not a refresh failure.");
	}

	[Test]
	public async Task StopReleasesOwnedDependenciesAfterTheDecoratedSourceIsDisposed() {
		var disposedBeforeRelease = false;
		var inner = new ActionRefreshableDataSource<Row>(() => new ListDataSource<Row>(Array.Empty<Row>()));
		await using var owner = new ReleasingSource(inner) { Release = () => disposedBeforeRelease = inner.IsDisposed };
		await owner.StopAsync().WaitAsync(Deadline);
		Assert.That(disposedBeforeRelease, Is.True);
	}

	[Test]
	public async Task SynchronousDisposalDoesNotWaitForAnUncooperativeRefreshButAsyncDisposalDrainsIt() {
		var entered = Signal();
		var finish = Capture();
		using var release = Tools.Scope.ExecuteOnDispose(() => finish.TrySetResult(Array.Empty<Row>()));
		var owner = Polling(_ => { entered.SetResult(); return finish.Task; });
		var pending = owner.RefreshAsync();
		await entered.Task.WaitAsync(Deadline);
		owner.Dispose();
		var disposal = owner.DisposeAsync().AsTask();
		Assert.That(disposal.IsCompleted, Is.False);
		finish.SetResult(Array.Empty<Row>());
		await Assert.ThatAsync(() => pending, Throws.InstanceOf<OperationCanceledException>());
		await disposal.WaitAsync(Deadline);
	}

	[TestCase(0)]
	[TestCase(-2)]
	[TestCase(2147483648d)]
	public async Task RejectsIntervalsOutsideTheSchedulerTimerRange(double milliseconds) {
		await using var owner = Polling(_ => Rows());
		Assert.That(() => owner.RefreshInterval = TimeSpan.FromMilliseconds(milliseconds), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public async Task PreCanceledCallerDoesNotAdmitARefresh() {
		var calls = 0;
		await using var owner = Polling(_ => { calls++; return Rows(); });
		using var canceled = new CancellationTokenSource();
		canceled.Cancel();
		Assert.That(() => owner.RefreshAsync(canceled.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(calls, Is.Zero);
	}

	[Test]
	public async Task DisablingFromARefreshNotificationRemovesTheFutureChain() {
		using var scheduler = StartedScheduler();
		var calls = 0;
		var finished = Signal();
		await using var owner = Polling(_ => {
			if (Interlocked.Increment(ref calls) == 2)
				finished.TrySetResult();
			return Rows();
		}, scheduler);
		owner.RefreshInterval = Interval;
		owner.Enabled = true;
		await finished.Task.WaitAsync(Deadline);
		owner.Enabled = false;
		await owner.StopAsync().WaitAsync(Deadline);
		var observed = Volatile.Read(ref calls);
		await Task.Delay(Interval * 4);
		Assert.That(Volatile.Read(ref calls), Is.EqualTo(observed), "Disabling must remove the future refresh chain.");
		Assert.That(scheduler.GetJobs(), Is.Empty);
	}

	[Test]
	public async Task ReadsThroughThePollingDecoratorLoadTheDecoratedSourceOnce() {
		var calls = 0;
		await using var owner = Polling(_ => { Interlocked.Increment(ref calls); return Task.FromResult<IEnumerable<Row>>(new[] { new Row(1, "Ready") }); });
		Assert.That(owner.RequiresLoad, Is.True);
		Assert.That((await owner.ReadRangeAsync(null, 10, 0, null, SortDirection.None)).Items.Single().Name, Is.EqualTo("Ready"));
		Assert.That(await owner.CountAsync, Is.EqualTo(1));
		Assert.That(owner.RequiresLoad, Is.False);
		Assert.That(calls, Is.EqualTo(1));
	}

	private static PollingDataSource<Row> Polling(Func<CancellationToken, Task<IEnumerable<Row>>> fetch, Scheduler scheduler = null)
		=> new PollingDataSource<Row>(new ActionRefreshableDataSource<Row>(() => new ActionAsyncListDataSource<Row>(fetch)), scheduler);

	private static Task<IEnumerable<Row>> Rows() => Task.FromResult<IEnumerable<Row>>(Array.Empty<Row>());

	private static Scheduler StartedScheduler() {
		var scheduler = new Scheduler(SchedulerPolicy.DontThrow);
		scheduler.Start();
		return scheduler;
	}

	private static async Task WaitUntilAsync(Func<bool> condition) {
		var deadline = DateTime.UtcNow + Deadline;
		while (!condition()) {
			Assert.That(DateTime.UtcNow, Is.LessThan(deadline), "The expected condition did not occur in time.");
			await Task.Delay(10);
		}
	}

	private static TaskCompletionSource Signal() => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

	private static TaskCompletionSource<IEnumerable<Row>> Capture() => new TaskCompletionSource<IEnumerable<Row>>(TaskCreationOptions.RunContinuationsAsynchronously);

	#region Inner Types

	public sealed record Row(int Id, string Name);

	private sealed class ReleasingSource : PollingDataSource<Row> {
		public ReleasingSource(IRefreshableDataSource<Row> source)
			: base(source) {
		}

		public Action Release { get; init; } = () => { };

		protected override Task ReleaseAsync() {
			Release();
			return Task.CompletedTask;
		}
	}

	#endregion
}
