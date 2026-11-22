// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ThrottleTests {
	private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

	[Test]
	public async Task ExecuteAsyncCoalescesPendingRequestsWhileCallbackRuns() {
		var throttle = new Throttle(TimeSpan.Zero);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = new List<int>();
		var first = throttle.ExecuteAsync(() => {
			calls.Add(1);
			return release.Task;
		});
		var second = throttle.ExecuteAsync(() => {
			calls.Add(2);
			return Task.CompletedTask;
		});
		var latest = throttle.ExecuteAsync(() => {
			calls.Add(3);
			return Task.CompletedTask;
		});

		Assert.That(calls, Is.EqualTo(new[] { 1 }));
		release.SetResult();
		await Task.WhenAll(first, second, latest).WaitAsync(Deadline);
		Assert.That(calls, Is.EqualTo(new[] { 1, 3 }));
	}

	[Test]
	public async Task ExecuteAsyncReportsFailureAfterRunningPendingWork() {
		var throttle = new Throttle(TimeSpan.Zero);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var pendingRan = false;
		var execution = throttle.ExecuteAsync(() => release.Task);
		var pending = throttle.ExecuteAsync(() => {
			pendingRan = true;
			return Task.CompletedTask;
		});

		release.SetException(new InvalidOperationException("Callback failed"));
		await Assert.ThatAsync(async () => await Task.WhenAll(execution, pending).WaitAsync(Deadline),
			Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Callback failed"));
		Assert.That(pendingRan, Is.True);
	}

	[TestCase(0)]
	[TestCase(100)]
	public async Task ExecuteAsyncCompletesAfterCallerContextStopsDispatching(int intervalMilliseconds) {
		var throttle = new Throttle(TimeSpan.FromMilliseconds(intervalMilliseconds));
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var context = new RetiredSynchronizationContext();
		var previous = SynchronizationContext.Current;
		Task execution;
		using (Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous))) {
			SynchronizationContext.SetSynchronizationContext(context);
			throttle.RegisterEvent();
			execution = throttle.ExecuteAsync(() => release.Task);
		}

		release.SetResult();
		await execution.WaitAsync(Deadline);
		Assert.That(context.Posts, Is.Zero, "Throttle bookkeeping must not depend on the callback owner's dispatcher remaining alive.");
	}

	[Test]
	public async Task StampedeDetectionCompletesAfterCallerContextStopsDispatching() {
		var throttle = new Throttle(TimeSpan.FromMilliseconds(100));
		var context = new RetiredSynchronizationContext();
		var previous = SynchronizationContext.Current;
		Task<bool> execution;
		using (Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous))) {
			SynchronizationContext.SetSynchronizationContext(context);
			execution = throttle.IsCallerFirstInStampede();
		}

		Assert.That(await execution.WaitAsync(Deadline), Is.True);
		Assert.That(context.Posts, Is.Zero, "Finishing the quiet-period check must not require a caller dispatcher.");
	}

	[Test]
	public void ExistingConstructorsRetainPeriodicMode() {
		Assert.That(new Throttle(TimeSpan.FromSeconds(5)).Mode, Is.EqualTo(ThrottleMode.NotifyEveryInterval));
		Assert.That(new Throttle(2).Mode, Is.EqualTo(ThrottleMode.NotifyEveryInterval));
		Assert.That(new Throttle(TimeSpan.FromMilliseconds(-1)).Mode, Is.EqualTo(ThrottleMode.NotifyEveryInterval));
	}

	[Test]
	public async Task PeriodicModeKeepsDeliveringDuringContinuousInputAndFinishesWithLatestRequest() {
		var throttle = new Throttle(TimeSpan.FromMilliseconds(80), ThrottleMode.NotifyEveryInterval);
		var watch = System.Diagnostics.Stopwatch.StartNew();
		var starts = new System.Collections.Concurrent.ConcurrentQueue<long>();
		var latest = 0;
		var received = 0;
		Task drain = Task.CompletedTask;
		while (watch.Elapsed < TimeSpan.FromMilliseconds(500)) {
			var value = ++latest;
			drain = throttle.ExecuteAsync(() => {
				starts.Enqueue(watch.ElapsedMilliseconds);
				Volatile.Write(ref received, value);
				return Task.CompletedTask;
			});
			await Task.Delay(10);
		}
		Assert.That(starts.Count, Is.GreaterThanOrEqualTo(3), "A continuing burst must not postpone periodic delivery until it goes quiet.");
		await drain.WaitAsync(Deadline);
		Assert.That(Volatile.Read(ref received), Is.EqualTo(latest));
		var delivered = starts.ToArray();
		for (var index = 1; index < delivered.Length; index++)
			Assert.That(delivered[index] - delivered[index - 1], Is.GreaterThanOrEqualTo(60));
	}

	[Test]
	public async Task BurstFinishedDeliversOnlyLatestAfterQuietPeriod() {
		var throttle = new Throttle(TimeSpan.FromMilliseconds(100), ThrottleMode.NotifyOnEventBurstFinished);
		var calls = new System.Collections.Concurrent.ConcurrentQueue<int>();
		Task drain = Task.CompletedTask;
		for (var index = 1; index <= 6; index++) {
			var value = index;
			drain = throttle.ExecuteAsync(() => { calls.Enqueue(value); return Task.CompletedTask; });
			await Task.Delay(20);
		}
		Assert.That(calls, Is.Empty, "No callback is admitted while requests keep resetting the quiet interval.");
		await drain.WaitAsync(Deadline);
		Assert.That(calls, Is.EqualTo(new[] { 6 }));
	}

	[Test]
	public async Task BurstStartAndFinishedDoesNotDuplicateAnIsolatedInput() {
		var throttle = new Throttle(TimeSpan.FromMilliseconds(100), ThrottleMode.NotifyOnEventBurstStartAndFinished);
		var calls = 0;
		await throttle.ExecuteAsync(() => { Interlocked.Increment(ref calls); return Task.CompletedTask; });
		Assert.That(calls, Is.EqualTo(1), "The first input delivers immediately.");
		await Task.Delay(150);
		Assert.That(calls, Is.EqualTo(1), "A single input must not generate a second trailing delivery.");
		await throttle.ExecuteAsync(() => { Interlocked.Increment(ref calls); return Task.CompletedTask; });
		Assert.That(calls, Is.EqualTo(2), "An independent later burst starts immediately.");
	}

	[Test]
	public async Task BurstStartAndFinishedDeliversLeadingAndLatestAdditionalInput() {
		var throttle = new Throttle(TimeSpan.FromMilliseconds(150), ThrottleMode.NotifyOnEventBurstStartAndFinished);
		var calls = new System.Collections.Concurrent.ConcurrentQueue<int>();
		await throttle.ExecuteAsync(() => { calls.Enqueue(1); return Task.CompletedTask; });
		var second = throttle.ExecuteAsync(() => { calls.Enqueue(2); return Task.CompletedTask; });
		var last = throttle.ExecuteAsync(() => { calls.Enqueue(3); return Task.CompletedTask; });
		Assert.That(calls, Is.EqualTo(new[] { 1 }));
		await Task.WhenAll(second, last).WaitAsync(Deadline);
		Assert.That(calls, Is.EqualTo(new[] { 1, 3 }));
	}

	[Test]
	public async Task NoneRunsEveryPendingCallbackSequentiallyWithoutIntervalDelay() {
		var throttle = new Throttle(TimeSpan.FromHours(1), ThrottleMode.None);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = new List<int>();
		var first = throttle.ExecuteAsync(() => { calls.Add(1); return release.Task; });
		var second = throttle.ExecuteAsync(() => { calls.Add(2); return Task.CompletedTask; });
		var third = throttle.ExecuteAsync(() => { calls.Add(3); return Task.CompletedTask; });
		Assert.That(calls, Is.EqualTo(new[] { 1 }));
		Assert.That(second, Is.SameAs(first));
		Assert.That(third, Is.SameAs(first));
		release.SetResult();
		await Task.WhenAll(first, second, third).WaitAsync(Deadline);
		Assert.That(calls, Is.EqualTo(new[] { 1, 2, 3 }));
	}

	[TestCase(ThrottleMode.None)]
	[TestCase(ThrottleMode.NotifyOnEventBurstFinished)]
	[TestCase(ThrottleMode.NotifyOnEventBurstStartAndFinished)]
	public async Task NewModesDrainFailuresAndDoNotDependOnCallerDispatcher(ThrottleMode mode) {
		var throttle = new Throttle(TimeSpan.FromMilliseconds(30), mode);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var context = new RetiredSynchronizationContext();
		var previous = SynchronizationContext.Current;
		Task first;
		using (Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous))) {
			SynchronizationContext.SetSynchronizationContext(context);
			first = throttle.ExecuteAsync(() => { entered.TrySetResult(); return release.Task; });
		}
		await entered.Task.WaitAsync(Deadline);
		var pendingRan = false;
		var pending = throttle.ExecuteAsync(() => { pendingRan = true; return Task.CompletedTask; });
		release.SetException(new InvalidOperationException("Mode callback failed"));
		await Assert.ThatAsync(async () => await Task.WhenAll(first, pending).WaitAsync(Deadline),
			Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Mode callback failed"));
		Assert.That(pendingRan, Is.True);
		Assert.That(context.Posts, Is.Zero);
	}

	#region Inner Types
	private sealed class RetiredSynchronizationContext : SynchronizationContext {
		private int _posts;

		public int Posts => Volatile.Read(ref _posts);

		public override void Post(SendOrPostCallback callback, object state) => Interlocked.Increment(ref _posts);

		public override SynchronizationContext CreateCopy() => this;
	}
	#endregion
}
