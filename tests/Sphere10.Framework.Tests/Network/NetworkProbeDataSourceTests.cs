// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public sealed class NetworkProbeDataSourceTests {
	private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
	private static readonly DateTime SampledAt = new(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc);

	[Test]
	public async Task PassiveReadsAndEnabledWithoutAnExplicitCommandDoNotCreateAProbeOrScheduleOne() {
		using var scheduler = new ManualScheduler();
		await using var source = new NetworkProbeDataSource(scheduler);
		Assert.That(source.Enabled, Is.False);
		source.Enabled = true;
		source.RefreshInterval = TimeSpan.FromSeconds(5);
		await source.RefreshAsync();
		var page = await source.ReadRangeAsync(string.Empty, 5, 0, null, SortDirection.None);
		Assert.That(page.TotalCount, Is.Zero);
		Assert.That(source.IsRunning, Is.False);
		Assert.That(scheduler.Jobs, Is.Empty, "Only an explicit command supplies probe work to a dormant source.");
	}

	[Test]
	public async Task OneShotPublishesOnAWorkerAndFilteringPagingAndExportDoNotRepeatIt() {
		var callerThread = Environment.CurrentManagedThreadId;
		var captureThread = 0;
		var calls = 0;
		var logged = new List<NetworkProbeObservation>();
		using var entered = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		using var scheduler = new ManualScheduler();
		await using var source = new NetworkProbeDataSource(scheduler);
		source.ObservationAvailable += logged.Add;
		var capture = source.StartAsync("Owned Probe", " localhost ", TimeSpan.FromSeconds(2), TimeSpan.Zero, (target, timeout, publish, token) => {
			captureThread = Environment.CurrentManagedThreadId;
			entered.Set();
			Guard.Ensure(release.Wait(Deadline), "The caller releases the synchronous probe setup.");
			Interlocked.Increment(ref calls);
			Assert.That(target, Is.EqualTo("localhost"));
			Assert.That(timeout, Is.EqualTo(TimeSpan.FromSeconds(2)));
			publish(Row("first"));
			publish(Row("second"));
			return Task.CompletedTask;
		});
		Assert.That(entered.Wait(Deadline), Is.True, "Synchronous setup must begin while its calling thread remains occupied.");
		release.Set();
		await capture.WaitAsync(Deadline);
		var page = await source.ReadRangeAsync("second", 1, 0, nameof(NetworkProbeObservation.Detail), SortDirection.Ascending);
		var export = source.Data;
		await source.RefreshAsync();
		Assert.That(captureThread, Is.Not.EqualTo(callerThread), "Even synchronously completed probe APIs must run outside the caller.");
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(scheduler.Jobs, Is.Empty, "A one-shot command schedules no repetition.");
		Assert.That(source.IsRunning, Is.False);
		Assert.That(source.Enabled, Is.False);
		Assert.That(page.TotalCount, Is.EqualTo(1));
		Assert.That(page.Items.Single().Detail, Is.EqualTo("second"));
		Assert.That(export.Select(row => row.Detail), Is.EqualTo(new[] { "second", "first" }));
		Assert.That(logged.Select(row => row.Detail), Is.EqualTo(new[] { "first", "second" }), "Each accepted observation is logged once, independently of table presentation.");
	}

	[Test]
	public async Task RepetitionInstallsOneRepeatingJobAndPauseRejectsAStaleRun() {
		using var scheduler = new ManualScheduler();
		var calls = 0;
		await using var source = new NetworkProbeDataSource(scheduler);
		var completion = source.StartAsync("Repeat", "localhost", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), (_, _, publish, _) => {
			Interlocked.Increment(ref calls);
			publish(Row("sample"));
			return Task.CompletedTask;
		});
		await UntilAsync(() => scheduler.Jobs.Length == 1);
		Assert.That(completion.IsCompleted, Is.False);
		Assert.That(calls, Is.EqualTo(1), "The first command runs immediately, then the source schedules the repetition.");
		var job = scheduler.TakeNext();
		await scheduler.ExecuteAsync(job).WaitAsync(Deadline);
		// A tick fires the round and returns; the round itself completes through the source's own completion.
		await source.Completion.WaitAsync(Deadline);
		Assert.That(calls, Is.EqualTo(2));
		source.Enabled = false;
		await scheduler.ExecuteAsync(job).WaitAsync(Deadline);
		await source.Completion.WaitAsync(Deadline);
		Assert.That(calls, Is.EqualTo(2), "A run taken from the timeline before Pause must not probe for a superseded policy.");
		Assert.That(source.IsRunning, Is.True, "Pausing repetition retains the explicit session until Cancel.");
		await source.CancelAsync().WaitAsync(Deadline);
		await completion.WaitAsync(Deadline);
		Assert.That(scheduler.Jobs, Is.Empty);
		Assert.That(source.IsRunning, Is.False);
	}

	[Test]
	public async Task CancelDrainsTheAcceptedProbeAndTheSourceCanRunANewCommandAfterwards() {
		var entered = Signal();
		var canceled = Signal();
		var release = Signal();
		using var ensureRelease = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
		await using var source = new NetworkProbeDataSource();
		var first = source.StartAsync("Blocking", "localhost", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1), async (_, _, _, token) => {
			using var registration = token.Register(() => canceled.TrySetResult());
			entered.TrySetResult();
			await release.Task;
			token.ThrowIfCancellationRequested();
		});
		await entered.Task.WaitAsync(Deadline);
		var cancel = source.CancelAsync();
		await canceled.Task.WaitAsync(Deadline);
		Assert.That(cancel.IsCompleted, Is.False, "Cancel must not claim drain while a native/protocol callback is still returning.");
		Assert.That(source.IsRunning, Is.True);
		Assert.That(() => source.StartAsync("Replacement", "localhost", TimeSpan.FromSeconds(2), TimeSpan.Zero, (_, _, _, _) => Task.CompletedTask), Throws.InstanceOf<InvalidOperationException>());
		release.SetResult();
		await Task.WhenAll(first, cancel).WaitAsync(Deadline);
		Assert.That(source.Data.Single().Status, Is.EqualTo("Cancelled"));
		await source.StartAsync("Next", "localhost", TimeSpan.FromSeconds(2), TimeSpan.Zero, (_, _, publish, _) => { publish(Row("next")); return Task.CompletedTask; }).WaitAsync(Deadline);
		Assert.That(source.Data.First().Detail, Is.EqualTo("next"));
		Assert.That(source.IsRunning, Is.False);
	}

	[Test]
	public async Task SourceStopDrainsAndDiscardsLateObservationsAndPermanentlyRejectsCommands() {
		var entered = Signal();
		var canceled = Signal();
		var release = Signal();
		using var ensureRelease = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
		await using var source = new NetworkProbeDataSource();
		var logs = 0;
		source.ObservationAvailable += _ => Interlocked.Increment(ref logs);
		var run = source.StartAsync("Late", "localhost", TimeSpan.FromSeconds(2), TimeSpan.Zero, async (_, _, publish, token) => {
			using var registration = token.Register(() => canceled.TrySetResult());
			publish(Row("before"));
			entered.TrySetResult();
			await release.Task;
			publish(Row("late"));
		});
		await entered.Task.WaitAsync(Deadline);
		var stop = source.StopAsync();
		await canceled.Task.WaitAsync(Deadline);
		Assert.That(stop.IsCompleted, Is.False);
		release.SetResult();
		await Task.WhenAll(run, stop).WaitAsync(Deadline);
		Assert.That(source.Data, Is.Empty);
		Assert.That(source.RetainedCount, Is.Zero);
		Assert.That(logs, Is.EqualTo(1));
		source.Enabled = false;
		Assert.That(() => source.Enabled = true, Throws.InstanceOf<ObjectDisposedException>());
		Assert.That(() => source.RefreshAsync(), Throws.InstanceOf<ObjectDisposedException>());
		Assert.That(() => source.StartAsync("No", "localhost", TimeSpan.FromSeconds(2), TimeSpan.Zero, (_, _, _, _) => Task.CompletedTask), Throws.InstanceOf<ObjectDisposedException>());
	}

	[Test]
	public async Task StopReportsCancellationCallbackFailuresAfterDrainingAndReleasingHistory() {
		using var source = new NetworkProbeDataSource();
		var entered = Signal();
		var exited = Signal();
		var run = source.StartAsync("Callback Failure", "localhost", TimeSpan.FromSeconds(2), TimeSpan.Zero, async (_, _, publish, token) => {
			using var completed = Tools.Scope.ExecuteOnDispose(() => exited.TrySetResult());
			using var registration = token.Register(() => throw new InvalidOperationException("owned cancellation callback failed"));
			publish(Row("before"));
			entered.TrySetResult();
			await Task.Delay(Timeout.Infinite, token);
		});
		await entered.Task.WaitAsync(Deadline);
		await Assert.ThatAsync(async () => await source.StopAsync().WaitAsync(Deadline), Throws.InstanceOf<AggregateException>());
		await Assert.ThatAsync(async () => await run.WaitAsync(Deadline), Throws.InstanceOf<AggregateException>());
		Assert.That(exited.Task.IsCompleted, Is.True);
		Assert.That(source.Data, Is.Empty);
		Assert.That(source.RetainedCount, Is.Zero);
	}

	[Test]
	public async Task ObservationCountAndRetainedCostAreBoundedAndClearResetsTheRetainedDataset() {
		await using var source = new NetworkProbeDataSource();
		var logs = 0;
		source.ObservationAvailable += _ => logs++;
		await source.StartAsync("Bounds", "localhost", TimeSpan.FromSeconds(2), TimeSpan.Zero, (_, _, publish, _) => {
			for (var i = 0; i < 1002; i++)
				publish(Row(i.ToString()));
			publish(Row(new string('X', 2 * 1024 * 1024)));
			return Task.CompletedTask;
		}).WaitAsync(Deadline);
		Assert.That(source.Count, Is.EqualTo(1000));
		Assert.That(source.Discarded, Is.EqualTo(3));
		Assert.That(logs, Is.EqualTo(1002), "Repainting or trimming old rows must not repeat log output.");
		Assert.That(source.Data.Last().Detail, Is.EqualTo("2"));
		await source.ClearAsync();
		Assert.That(source.Data, Is.Empty);
		Assert.That(source.Discarded, Is.Zero);
		await source.StartAsync("Cost", "localhost", TimeSpan.FromSeconds(2), TimeSpan.Zero, (_, _, publish, _) => {
			publish(Row(new string('Y', 1024 * 1024)));
			publish(Row(new string('Z', 1024 * 1024)));
			return Task.CompletedTask;
		}).WaitAsync(Deadline);
		Assert.That(source.Count, Is.EqualTo(1), "The retained-cost bound also applies below the observation-count limit.");
		Assert.That(source.Discarded, Is.EqualTo(1));
	}

	[Test]
	public async Task CancelingOneWaitDoesNotCancelTheExplicitProbeOrItsOtherWaiter() {
		using var caller = new CancellationTokenSource();
		var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = Signal();
		using var ensureRelease = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
		await using var source = new NetworkProbeDataSource();
		var first = source.StartAsync("Owned", "localhost", TimeSpan.FromSeconds(2), TimeSpan.Zero, async (_, _, publish, token) => {
			entered.TrySetResult(token);
			await release.Task.WaitAsync(token);
			publish(Row("accepted"));
		}, caller.Token);
		var captured = await entered.Task.WaitAsync(Deadline);
		var second = source.RefreshAsync();
		caller.Cancel();
		await Assert.ThatAsync(async () => await first, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(captured.IsCancellationRequested, Is.False);
		Assert.That(second.IsCompleted, Is.False);
		release.SetResult();
		await Task.WhenAll(second, source.RunCompletion).WaitAsync(Deadline);
		Assert.That(source.Data.Single().Detail, Is.EqualTo("accepted"));
	}

	private static NetworkProbeObservation Row(string detail) => new(SampledAt, "Owned", "localhost", "127.0.0.1", null, "Accepted", null, null, detail);
	private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

	private static async Task UntilAsync(Func<bool> condition) {
		var watch = Stopwatch.StartNew();
		while (!condition() && watch.Elapsed < Deadline)
			await Task.Delay(10);
		Assert.That(condition(), Is.True, "The owned source must reach its expected scheduling boundary.");
	}

	#region Inner Types
	/// <summary>Records the repeating job a source installs and runs it only when the test says so.</summary>
	private sealed class ManualScheduler : Scheduler {
		private readonly SynchronizedObject _state = new();
		private readonly List<IJob> _jobs = new();
		public IJob[] Jobs { get { using var scope = _state.EnterReadScope(); return _jobs.ToArray(); } }
		public override void AddJob(IJob job) { using var scope = _state.EnterWriteScope(); _jobs.Add(job); }
		public override void RemoveJob(IJob job) { using var scope = _state.EnterWriteScope(); _jobs.Remove(job); }
		public override void Start() { }
		public override void Stop() { }
		public IJob TakeNext() { using var scope = _state.EnterWriteScope(); var job = _jobs.Single(); _jobs.Remove(job); return job; }
		public Task ExecuteAsync(IJob job) => Task.Run(job.Execute);
	}
	#endregion
}
