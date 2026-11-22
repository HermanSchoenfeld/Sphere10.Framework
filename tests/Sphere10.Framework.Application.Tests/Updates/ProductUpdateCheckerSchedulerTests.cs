// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests.Updates;

/// <summary>
/// The update checker on the Framework's real scheduler and timer: the scheduler accepts the checker's job, runs it on a worker and
/// queues the next run one interval after the check ends. The case waits on the check itself, with a deadline, rather than on time.
/// </summary>
[TestFixture]
[NonParallelizable]
public class ProductUpdateCheckerSchedulerTests {
	private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

	[Test]
	public async Task TheSchedulerRunsTheFirstCheckOnAWorkerAndQueuesTheNextOneIntervalAfterItEnds() {
		var callerThread = Environment.CurrentManagedThreadId;
		var entered = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var announced = new TaskCompletionSource<ProductUpdate>(TaskCreationOptions.RunContinuationsAsynchronously);
		var service = new GatedService(entered, release);
		var options = new ProductUpdateCheckOptions { InitialDelay = TimeSpan.FromMilliseconds(50), Interval = TimeSpan.FromHours(1) };
		await using var checker = new ObservedChecker(service, options);
		checker.UpdateAvailable += update => announced.TrySetResult(update);
		checker.Start();
		var scheduler = checker.JobScheduler;
		var job = scheduler.GetJobs().Single();
		Assert.That(scheduler.Policy.HasFlag(SchedulerPolicy.ForceSyncronous), Is.False);
		Assert.That(job.Policy.HasFlag(JobPolicy.Asyncronous), Is.True, "The job's policy matches its scheduler, which would otherwise refuse it.");
		Assert.That(job.Status, Is.EqualTo(JobStatus.Scheduled));

		// The scheduler's timer fires the job; the check then runs on a worker while this thread is free.
		var checkThread = await entered.Task.WaitAsync(Deadline);
		Assert.That(checkThread, Is.Not.EqualTo(callerThread));
		Assert.That(scheduler.GetJobs(), Is.Empty, "A running check is not queued again until it ends.");
		var released = DateTime.UtcNow;
		release.SetResult();
		Assert.That((await announced.Task.WaitAsync(Deadline)).Version, Is.EqualTo(ApplicationVersion.Parse("2.0.0.0")));

		// Once the check has ended the same job is back on the timeline, one interval after that end.
		await UntilAsync(() => scheduler.GetJobs().Any());
		var schedule = scheduler.GetJobs().Single().Schedules.Single();
		Assert.That(scheduler.GetJobs().Single(), Is.SameAs(job));
		Assert.That(schedule.ReschedulePolicy, Is.EqualTo(ReschedulePolicy.OnFinish));
		Assert.That(schedule.IterationsExecuted, Is.EqualTo(1u));
		Assert.That(schedule.LastEndTime, Is.GreaterThanOrEqualTo(released));
		Assert.That(schedule.NextStartTime, Is.EqualTo(schedule.LastEndTime.Value + options.Interval));
		Assert.That(service.Calls, Is.EqualTo(1));
		// The job waits paused until the scheduler is started, then runs once.
		Assert.That(checker.Statuses, Is.EqualTo(new[] { JobStatus.Initialized, JobStatus.Paused, JobStatus.Scheduled, JobStatus.Started, JobStatus.Completed }));

		await checker.StopAsync().WaitAsync(Deadline);
		Assert.That(scheduler.GetJobs(), Is.Empty);
	}

	private static async Task UntilAsync(Func<bool> condition) {
		var watch = Stopwatch.StartNew();
		while (!condition() && watch.Elapsed < Deadline)
			await Task.Delay(10);
		Assert.That(condition(), Is.True, "The scheduler must queue the job again after the check has ended.");
	}

	#region Inner Types

	/// <summary>Signals the thread a check runs on and answers when the case releases it.</summary>
	private sealed class GatedService : IProductUpdateService {
		private readonly TaskCompletionSource<int> _entered;
		private readonly TaskCompletionSource _release;
		private int _calls;

		public GatedService(TaskCompletionSource<int> entered, TaskCompletionSource release) {
			_entered = entered;
			_release = release;
		}

		public int Calls => Volatile.Read(ref _calls);

		public async Task<ProductUpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default) {
			Interlocked.Increment(ref _calls);
			_entered.TrySetResult(Environment.CurrentManagedThreadId);
			await _release.Task.WaitAsync(cancellationToken);
			return ProductUpdateCheckResult.Available(new ReleaseServerProductUpdate { Version = ApplicationVersion.Parse("2.0.0.0"), DownloadUrl = "https://updates.example.com/setup.exe" });
		}
	}

	/// <summary>A checker on a real scheduler that records its job's status changes.</summary>
	private sealed class ObservedChecker : ProductUpdateChecker {
		private readonly ConcurrentQueue<JobStatus> _statuses = new();

		public ObservedChecker(IProductUpdateService service, ProductUpdateCheckOptions options)
			: base(new ReleaseServerProductUpdater(service), options) {
		}

		public Scheduler<BaseJob> JobScheduler { get; private set; }

		public JobStatus[] Statuses => _statuses.ToArray();

		protected override Scheduler<BaseJob> CreateScheduler() {
			JobScheduler = base.CreateScheduler();
			JobScheduler.JobStatusChanged += (_, _, status) => _statuses.Enqueue(status);
			return JobScheduler;
		}
	}

	#endregion
}
