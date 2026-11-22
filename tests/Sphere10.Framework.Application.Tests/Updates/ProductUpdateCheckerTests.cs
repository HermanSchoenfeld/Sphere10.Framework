// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests.Updates;

/// <summary>
/// The background update checker's schedule, notifications, failure handling and shutdown, checking through the release server's
/// updater. A manually driven scheduler and controlled service completions stand in for elapsed time, so no case waits for a timer.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ProductUpdateCheckerTests {
	private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

	[Test]
	public void DefaultOptionsCheckThirtySecondsAfterStartAndThenHourly() {
		var options = new ProductUpdateCheckOptions();
		Assert.That(options.InitialDelay, Is.EqualTo(TimeSpan.FromSeconds(30)));
		Assert.That(options.Interval, Is.EqualTo(TimeSpan.FromHours(1)));
	}

	[Test]
	public async Task StartSchedulesTheFirstCheckAfterTheInitialDelayAndEachLaterCheckOneIntervalAfterThePreviousEnds() {
		var service = new ScriptedService(_ => Task.FromResult(ProductUpdateCheckResult.NoUpdate));
		var options = new ProductUpdateCheckOptions { InitialDelay = TimeSpan.FromSeconds(45), Interval = TimeSpan.FromMinutes(20) };
		await using var checker = new TestChecker(service, options);
		var before = DateTime.UtcNow;
		checker.Start();
		var after = DateTime.UtcNow;
		var job = checker.JobScheduler.Jobs.Single();
		var schedule = job.Schedules.Single();
		Assert.That(checker.IsRunning, Is.True);
		Assert.That(checker.JobScheduler.IsStarted, Is.True);
		Assert.That(service.Calls, Is.Zero, "Starting only schedules; no check runs during start-up.");
		Assert.That(schedule.NextStartTime, Is.InRange(before + options.InitialDelay, after + options.InitialDelay));
		Assert.That(schedule.ReschedulePolicy, Is.EqualTo(ReschedulePolicy.OnFinish));
		// Run the first iteration as the scheduler does.
		var started = DateTime.UtcNow;
		schedule.NotifyStart(started);
		await Task.Run(job.Execute).WaitAsync(Deadline);
		var ended = DateTime.UtcNow;
		schedule.NotifyExecution(started, ended);
		Assert.That(service.Calls, Is.EqualTo(1));
		Assert.That(schedule.NextStartTime, Is.EqualTo(ended + options.Interval));
		checker.Start();
		Assert.That(checker.JobScheduler.Jobs, Has.Length.EqualTo(1), "A second start schedules nothing more.");
	}

	[Test]
	public async Task AnUpdateIsAnnouncedOnceForEachVersion() {
		var answers = new Queue<ProductUpdateCheckResult>(new[] { Update("2.0.0.0"), Update("2.0.0.0"), Update("3.0.0.0"), Update("3.0.0.0") });
		var service = new ScriptedService(_ => Task.FromResult(answers.Dequeue()));
		await using var checker = new TestChecker(service);
		var announced = new List<string>();
		checker.UpdateAvailable += update => announced.Add(update.Version.ToString());
		for (var i = 0; i < 4; i++) {
			var result = await checker.CheckAsync().WaitAsync(Deadline);
			Assert.That(result.IsUpdateAvailable, Is.True);
		}
		Assert.That(announced, Is.EqualTo(new[] { "2.0.0.0", "3.0.0.0" }));
		Assert.That(checker.HookCalls, Is.EqualTo(2), "The template hook runs with each announcement.");
		Assert.That(checker.AvailableUpdate.Version, Is.EqualTo(ApplicationVersion.Parse("3.0.0.0")));
	}

	[Test]
	public async Task NoUpdateAndMissingAnswersAnnounceNothing() {
		var answers = new Queue<ProductUpdateCheckResult>(new[] { Update("2.0.0.0"), ProductUpdateCheckResult.NoUpdate, null });
		var service = new ScriptedService(_ => Task.FromResult(answers.Dequeue()));
		await using var checker = new TestChecker(service);
		var announcements = 0;
		checker.UpdateAvailable += _ => announcements++;
		await checker.CheckAsync().WaitAsync(Deadline);
		Assert.That(checker.AvailableUpdate, Is.Not.Null);
		var none = await checker.CheckAsync().WaitAsync(Deadline);
		Assert.That(none.IsUpdateAvailable, Is.False);
		Assert.That(checker.AvailableUpdate, Is.Null, "The latest answer replaces an earlier update.");
		var missing = await checker.CheckAsync().WaitAsync(Deadline);
		Assert.That(missing, Is.SameAs(ProductUpdateCheckResult.NoUpdate));
		Assert.That(announcements, Is.EqualTo(1));
	}

	[Test]
	public async Task AFailedScheduledCheckIsLoggedAndTheNextCheckStillRuns() {
		var failure = new InvalidOperationException("The release server is unreachable.");
		var calls = 0;
		var service = new ScriptedService(_ => Interlocked.Increment(ref calls) == 1 ? Task.FromException<ProductUpdateCheckResult>(failure) : Task.FromResult(Update("2.0.0.0")));
		var errors = new ConcurrentQueue<string>();
		await using var checker = new TestChecker(service, logger: new ActionLogger(_ => { }, _ => { }, _ => { }, errors.Enqueue));
		var announcements = 0;
		checker.UpdateAvailable += _ => Interlocked.Increment(ref announcements);
		checker.Start();
		var job = checker.JobScheduler.Jobs.Single();
		await Task.Run(job.Execute).WaitAsync(Deadline);
		Assert.That(errors, Has.Count.EqualTo(1), "The failure is logged, not thrown into the scheduler.");
		Assert.That(errors.Single(), Does.Contain(failure.Message));
		Assert.That(announcements, Is.Zero);
		await Task.Run(job.Execute).WaitAsync(Deadline);
		Assert.That(errors, Has.Count.EqualTo(1));
		Assert.That(announcements, Is.EqualTo(1));
	}

	[Test]
	public async Task ARequestMadeWhileACheckIsRunningJoinsIt() {
		var entered = Signal();
		var release = Signal();
		var service = new ScriptedService(async _ => {
			entered.TrySetResult();
			await release.Task;
			return Update("2.0.0.0");
		});
		await using var checker = new TestChecker(service);
		var announcements = 0;
		checker.UpdateAvailable += _ => Interlocked.Increment(ref announcements);
		var first = checker.CheckAsync();
		await entered.Task.WaitAsync(Deadline);
		var second = checker.CheckAsync();
		using var abandoned = new CancellationTokenSource();
		var third = checker.CheckAsync(abandoned.Token);
		abandoned.Cancel();
		await Assert.ThatAsync(async () => await third, Throws.InstanceOf<OperationCanceledException>(), "A caller's token ends that caller's wait.");
		Assert.That(first.IsCompleted, Is.False, "A caller that stops waiting does not cancel the check.");
		release.SetResult();
		var results = await Task.WhenAll(first, second).WaitAsync(Deadline);
		Assert.That(results[0], Is.SameAs(results[1]));
		Assert.That(service.Calls, Is.EqualTo(1));
		Assert.That(announcements, Is.EqualTo(1));
	}

	[Test]
	public async Task StoppingCancelsTheCheckInFlightAndCompletesOnlyWhenItHasEnded() {
		var entered = Signal();
		var cancelled = Signal();
		var release = Signal();
		// The service observes the cancellation but still answers, so the checker itself must discard the late answer.
		var service = new ScriptedService(async token => {
			using var registration = token.Register(() => cancelled.TrySetResult());
			entered.TrySetResult();
			await release.Task;
			return Update("2.0.0.0");
		});
		var checker = new TestChecker(service);
		var announcements = 0;
		checker.UpdateAvailable += _ => Interlocked.Increment(ref announcements);
		checker.Start();
		var check = checker.CheckAsync();
		await entered.Task.WaitAsync(Deadline);
		var stop = checker.StopAsync();
		await cancelled.Task.WaitAsync(Deadline);
		Assert.That(stop.IsCompleted, Is.False, "Stopping waits for the check in flight.");
		Assert.That(checker.IsRunning, Is.False);
		Assert.That(checker.JobScheduler.Jobs, Is.Empty);
		release.SetResult();
		await stop.WaitAsync(Deadline);
		await Assert.ThatAsync(async () => await check, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(announcements, Is.Zero, "An answer that arrives during shutdown is not announced.");
		Assert.That(checker.JobScheduler.IsDisposed, Is.True);
		Assert.That(checker.StopAsync(), Is.SameAs(stop));
	}

	[Test]
	public async Task AStoppedCheckerRefusesChecksAndIgnoresAScheduledRunTakenBeforeItStopped() {
		var service = new ScriptedService(_ => Task.FromResult(Update("2.0.0.0")));
		var errors = new ConcurrentQueue<string>();
		var checker = new TestChecker(service, logger: new ActionLogger(_ => { }, _ => { }, _ => { }, errors.Enqueue));
		checker.Start();
		var job = checker.JobScheduler.Jobs.Single();
		await checker.StopAsync().WaitAsync(Deadline);
		Assert.That(() => { _ = checker.CheckAsync(); }, Throws.InstanceOf<ObjectDisposedException>());
		Assert.That(() => checker.Start(), Throws.InstanceOf<ObjectDisposedException>());
		// A run the scheduler took from its timeline before the stop still executes once; it must neither check nor report a failure.
		await Task.Run(job.Execute).WaitAsync(Deadline);
		Assert.That(service.Calls, Is.Zero);
		Assert.That(errors, Is.Empty);
	}

	[Test]
	public async Task AnAnnouncementThatFailsIsRepeatedByTheNextCheck() {
		var service = new ScriptedService(_ => Task.FromResult(Update("2.0.0.0")));
		await using var checker = new TestChecker(service);
		var failure = new InvalidOperationException("The notice could not be shown.");
		var announcements = 0;
		checker.UpdateAvailable += _ => {
			if (Interlocked.Increment(ref announcements) == 1)
				throw failure;
		};
		await Assert.ThatAsync(async () => await checker.CheckAsync(), Throws.Exception.SameAs(failure));
		await checker.CheckAsync().WaitAsync(Deadline);
		await checker.CheckAsync().WaitAsync(Deadline);
		Assert.That(announcements, Is.EqualTo(2), "The version counts as announced once a handler has accepted it.");
	}

	[Test]
	public async Task AnUpdateOfferedAgainIsAnnouncedByTheNextCheckThatFindsIt() {
		var service = new ScriptedService(_ => Task.FromResult(Update("2.0.0.0")));
		await using var checker = new TestChecker(service);
		var announced = new List<ProductUpdate>();
		checker.UpdateAvailable += announced.Add;
		await checker.CheckAsync().WaitAsync(Deadline);
		await checker.CheckAsync().WaitAsync(Deadline);
		Assert.That(announced, Has.Count.EqualTo(1));
		checker.OfferAgain(Update("9.0.0.0").Update);
		await checker.CheckAsync().WaitAsync(Deadline);
		Assert.That(announced, Has.Count.EqualTo(1), "Handing back another version withdraws nothing.");
		checker.OfferAgain(announced[0]);
		await checker.CheckAsync().WaitAsync(Deadline);
		await checker.CheckAsync().WaitAsync(Deadline);
		Assert.That(announced, Has.Count.EqualTo(2), "The version is announced once more and then counts as announced again.");
		Assert.That(() => checker.OfferAgain(null), Throws.ArgumentNullException);
	}

	[Test]
	public async Task AnObserverThatRefusesDuringTheNotificationIsOfferedTheUpdateAgain() {
		var service = new ScriptedService(_ => Task.FromResult(Update("2.0.0.0")));
		await using var checker = new TestChecker(service);
		var announcements = 0;
		checker.UpdateAvailable += update => {
			if (++announcements <= 2)
				checker.OfferAgain(update);
		};
		for (var i = 0; i < 5; i++)
			await checker.CheckAsync().WaitAsync(Deadline);
		Assert.That(announcements, Is.EqualTo(3), "The refusal made inside the notification is kept, and the accepted announcement is not repeated.");
	}

	[Test]
	public async Task ANewerVersionRefusedWhileAnOlderOneIsPresentedIsOfferedUntilItIsAccepted() {
		var answers = new Queue<ProductUpdateCheckResult>(new[] { Update("2.0.0.0"), Update("3.0.0.0"), Update("3.0.0.0"), Update("3.0.0.0"), Update("3.0.0.0") });
		var service = new ScriptedService(_ => Task.FromResult(answers.Dequeue()));
		await using var checker = new TestChecker(service);
		var announced = new List<string>();
		var presenting = false;
		// The observer stands for a presenter that shows one notice at a time and hands back what it cannot show.
		checker.UpdateAvailable += update => {
			announced.Add(update.Version.ToString());
			if (presenting)
				checker.OfferAgain(update);
			else
				presenting = true;
		};
		await checker.CheckAsync().WaitAsync(Deadline);
		await checker.CheckAsync().WaitAsync(Deadline);
		await checker.CheckAsync().WaitAsync(Deadline);
		presenting = false;
		await checker.CheckAsync().WaitAsync(Deadline);
		await checker.CheckAsync().WaitAsync(Deadline);
		Assert.That(announced, Is.EqualTo(new[] { "2.0.0.0", "3.0.0.0", "3.0.0.0", "3.0.0.0" }));
	}

	[Test]
	public async Task DisposingTheCheckerStopsItsSchedule() {
		var checker = new TestChecker(new ScriptedService(_ => Task.FromResult(ProductUpdateCheckResult.NoUpdate)));
		checker.Start();
		await checker.DisposeAsync();
		Assert.That(checker.IsRunning, Is.False);
		Assert.That(checker.JobScheduler.IsDisposed, Is.True);
		Assert.That(checker.JobScheduler.Jobs, Is.Empty);
	}

	[Test]
	public void InvalidArgumentsAreRejected() {
		var service = new ScriptedService(_ => Task.FromResult(ProductUpdateCheckResult.NoUpdate));
		var updater = new ReleaseServerProductUpdater(service);
		Assert.That(() => new ProductUpdateChecker(null), Throws.ArgumentNullException);
		Assert.That(() => new ProductUpdateChecker(updater, new ProductUpdateCheckOptions { InitialDelay = TimeSpan.FromSeconds(-1) }), Throws.ArgumentException);
		Assert.That(() => new ProductUpdateChecker(updater, new ProductUpdateCheckOptions { Interval = TimeSpan.Zero }), Throws.ArgumentException);
	}

	private static ProductUpdateCheckResult Update(string version)
		=> ProductUpdateCheckResult.Available(new ReleaseServerProductUpdate { Version = ApplicationVersion.Parse(version), DownloadUrl = "https://updates.example.com/setup.exe" });

	private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

	#region Inner Types

	/// <summary>Answers each check from a supplied function and counts the calls.</summary>
	private sealed class ScriptedService : IProductUpdateService {
		private readonly Func<CancellationToken, Task<ProductUpdateCheckResult>> _answer;
		private int _calls;

		public ScriptedService(Func<CancellationToken, Task<ProductUpdateCheckResult>> answer) => _answer = answer;

		public int Calls => Volatile.Read(ref _calls);

		public Task<ProductUpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default) {
			Interlocked.Increment(ref _calls);
			return _answer(cancellationToken);
		}
	}

	/// <summary>
	/// A checker over the release server's updater for the scripted service, whose repeating job is recorded by a
	/// <see cref="ManualScheduler"/> and run only when a case says so.
	/// </summary>
	private sealed class TestChecker : ProductUpdateChecker {
		private int _hookCalls;

		public TestChecker(IProductUpdateService service, ProductUpdateCheckOptions options = null, ILogger logger = null)
			: base(new ReleaseServerProductUpdater(service), options, logger) {
		}

		public ManualScheduler JobScheduler { get; } = new();

		public int HookCalls => Volatile.Read(ref _hookCalls);

		protected override Scheduler<BaseJob> CreateScheduler() => JobScheduler;

		protected override void OnUpdateAvailable(ProductUpdate update) => Interlocked.Increment(ref _hookCalls);
	}

	/// <summary>Records the jobs its owner installs instead of running them on a timer.</summary>
	private sealed class ManualScheduler : Scheduler<BaseJob> {
		private readonly SynchronizedObject _state = new();
		private readonly List<BaseJob> _jobs = new();

		public BaseJob[] Jobs {
			get {
				using (_state.EnterReadScope())
					return _jobs.ToArray();
			}
		}

		public bool IsStarted { get; private set; }

		public bool IsDisposed { get; private set; }

		public override void AddJob(BaseJob job) {
			using (_state.EnterWriteScope())
				_jobs.Add(job);
		}

		public override void RemoveJob(BaseJob job) {
			using (_state.EnterWriteScope())
				_jobs.Remove(job);
		}

		public override void Start() => IsStarted = true;

		public override void Stop() => IsStarted = false;

		protected override void DisposeInternal() {
			IsDisposed = true;
			base.DisposeInternal();
		}
	}

	#endregion
}
