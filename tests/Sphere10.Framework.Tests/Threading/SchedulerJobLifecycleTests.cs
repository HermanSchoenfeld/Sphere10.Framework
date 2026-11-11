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

/// <summary>Removing or pausing a repeating job while its schedule is queued or running must take effect; the scheduler used to re-add the schedule afterwards.</summary>
[TestFixture]
[NonParallelizable]
public class SchedulerJobLifecycleTests {
	private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(50);
	private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);

	[SetUp]
	public void Setup() => Assume.That(!Tools.NUnit.IsGitHubAction, "Test fixture is disabled on GitHub Actions");

	[Test]
	public async Task RemovingARepeatingJobWhileItRunsDoesNotRescheduleIt() {
		using var scheduler = new Scheduler(SchedulerPolicy.DontThrow);
		using var started = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		var runs = 0;
		var job = JobBuilder.For(() => {
			Interlocked.Increment(ref runs);
			started.Set();
			release.Wait(Deadline);
		}).Repeat.OnInterval(Interval).Build();
		scheduler.AddJob(job);
		scheduler.Start();
		Assert.That(started.Wait(Deadline), Is.True);
		scheduler.RemoveJob(job);
		release.Set();
		await Task.Delay(Interval * 6);
		Assert.That(runs, Is.EqualTo(1), "A job removed during its run must not run again.");
		Assert.That(scheduler.GetJobs(), Is.Empty);
	}

	[Test]
	public async Task RemovingAPausedJobDiscardsItsPausedSchedules() {
		using var scheduler = new Scheduler(SchedulerPolicy.DontThrow);
		var runs = 0;
		var job = JobBuilder.For(() => Interlocked.Increment(ref runs)).Repeat.OnInterval(Interval).Build();
		scheduler.AddJob(job);
		scheduler.Start();
		scheduler.PauseJob(job);
		scheduler.RemoveJob(job);
		Assert.That(scheduler.GetJobs(), Is.Empty, "Removing a paused job must discard its paused schedules.");
		scheduler.ResumeJob(job);
		await Task.Delay(Interval * 4);
		Assert.That(runs, Is.Zero, "Resuming a removed job must restore nothing.");
		Assert.That(scheduler.GetJobs(), Is.Empty);
	}

	[Test]
	public async Task PausingARepeatingJobWhileItRunsKeepsItPausedUntilResumed() {
		using var scheduler = new Scheduler(SchedulerPolicy.DontThrow);
		using var started = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		var runs = 0;
		var job = JobBuilder.For(() => {
			Interlocked.Increment(ref runs);
			started.Set();
			release.Wait(Deadline);
		}).Repeat.OnInterval(Interval).Build();
		scheduler.AddJob(job);
		scheduler.Start();
		Assert.That(started.Wait(Deadline), Is.True);
		scheduler.PauseJob(job);
		release.Set();
		await Task.Delay(Interval * 6);
		Assert.That(runs, Is.EqualTo(1), "A job paused during its run must wait for Resume.");
		Assert.That(scheduler.GetJobs(), Is.EqualTo(new[] { job }), "A paused job remains registered.");
		scheduler.ResumeJob(job);
		var deadline = DateTime.UtcNow + Deadline;
		while (Volatile.Read(ref runs) < 2 && DateTime.UtcNow < deadline)
			await Task.Delay(Interval);
		Assert.That(runs, Is.GreaterThanOrEqualTo(2), "A resumed job runs again.");
	}
}
