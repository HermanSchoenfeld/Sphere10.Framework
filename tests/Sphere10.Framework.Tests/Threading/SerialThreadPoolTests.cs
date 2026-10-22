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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SerialThreadPoolTests {
	[Test]
	public void ProducersDuringAnActiveCallbackScheduleOnlyOneDrainerAndPreserveOrder() {
		var scheduled = new ConcurrentQueue<Action>();
		var queue = new SerialThreadPool(new QueueAdapter<Action>(), null, scheduled.Enqueue);
		using var entered = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		var executed = new List<int>();
		queue.QueueUserWorkItem(() => {
			entered.Set();
			release.Wait(TimeSpan.FromSeconds(5));
			executed.Add(1);
		});
		Assert.That(scheduled.TryDequeue(out var worker), Is.True);
		var running = Task.Run(worker);
		try {
			Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
			queue.QueueUserWorkItem(() => executed.Add(2));
			queue.QueueUserWorkItem(() => executed.Add(3));
			Assert.That(scheduled, Is.Empty, "A running callback already owns the next input batch; no blocked duplicate worker should be scheduled.");
			release.Set();
			Assert.That(running.Wait(TimeSpan.FromSeconds(5)), Is.True);
			Assert.That(executed, Is.EqualTo(new[] { 1, 2, 3 }));
		} finally {
			release.Set();
			running.Wait(TimeSpan.FromSeconds(5));
		}
	}

	[Test]
	public void EnqueueAfterTheIdleTransitionSchedulesANewWorker() {
		var scheduled = new ConcurrentQueue<Action>();
		var queue = new SerialThreadPool(new QueueAdapter<Action>(), null, scheduled.Enqueue);
		var executions = 0;
		for (var batch = 0; batch < 3; batch++) {
			queue.QueueUserWorkItem(() => executions++);
			Assert.That(scheduled.TryDequeue(out var worker), Is.True, "Work queued after the previous drainer exits must receive a new scheduling credit.");
			worker();
			Assert.That(scheduled, Is.Empty);
			Assert.That(executions, Is.EqualTo(batch + 1));
		}
	}

	[Test]
	public void CallbackFailureIsReportedAndLaterCallbacksStillRun() {
		var expected = new InvalidOperationException("Test callback failure.");
		Exception reported = null;
		using var completed = new ManualResetEventSlim();
		var queue = new SerialThreadPool(error => reported = error);
		queue.QueueUserWorkItem(() => throw expected);
		queue.QueueUserWorkItem(completed.Set);
		Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(reported, Is.SameAs(expected));
	}

	[Test]
	public void ErrorHandlerFailureReschedulesRemainingCallbacksBeforeNewWork() {
		var scheduled = new ConcurrentQueue<Action>();
		var expected = new InvalidOperationException("Test error-handler failure.");
		var executed = new List<int>();
		var queue = new SerialThreadPool(new QueueAdapter<Action>(), _ => throw expected, scheduled.Enqueue);
		queue.QueueUserWorkItem(() => throw new Exception("Test callback failure."));
		queue.QueueUserWorkItem(() => executed.Add(1));
		Assert.That(scheduled.TryDequeue(out var failedWorker), Is.True);
		Assert.That(failedWorker, Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo(expected.Message));
		queue.QueueUserWorkItem(() => executed.Add(2));
		Assert.That(scheduled.Count, Is.EqualTo(1));
		Assert.That(scheduled.TryDequeue(out var resumedWorker), Is.True);
		resumedWorker();
		Assert.That(executed, Is.EqualTo(new[] { 1, 2 }));
		Assert.That(scheduled, Is.Empty);
	}

	[Test]
	public void SchedulingFailureDoesNotRetainTheRunningFlagOrLoseQueuedWork() {
		var scheduled = new ConcurrentQueue<Action>();
		var executed = new List<int>();
		var shouldFail = true;
		var queue = new SerialThreadPool(new QueueAdapter<Action>(), null, worker => {
			if (shouldFail)
				throw new InvalidOperationException("Test scheduler failure.");
			scheduled.Enqueue(worker);
		});
		Assert.That(() => queue.QueueUserWorkItem(() => executed.Add(1)), Throws.TypeOf<InvalidOperationException>());
		shouldFail = false;
		queue.QueueUserWorkItem(() => executed.Add(2));
		Assert.That(scheduled.TryDequeue(out var worker), Is.True);
		worker();
		Assert.That(executed, Is.EqualTo(new[] { 1, 2 }));
		Assert.That(scheduled, Is.Empty);
	}
}
