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

namespace Sphere10.Framework.Application.Tests.Lifecycle;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationAsyncTaskRunnerTests {
	[Test]
	public async Task SequentialPriorityTasksFinishBeforeBothParallelTasksStartAndTheRunnerAwaitsEveryTask() {
		var calls = new ConcurrentQueue<string>();
		var sequentialGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var parallelGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var bothParallelStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var parallelStarted = 0;
		var tasks = new IApplicationInitializer[] {
			new AsyncInitializer(async _ => { calls.Enqueue("later"); await Task.Yield(); }, 20),
			new AsyncInitializer(async _ => { calls.Enqueue("earlier"); await sequentialGate.Task; }, 10),
			new AsyncInitializer(Parallel, parallelizable: true),
			new AsyncInitializer(Parallel, parallelizable: true)
		};
		var runner = Tools.Application.ExecuteInitializersAsync(tasks);
		Assert.That(calls, Is.EqualTo(new[] { "earlier" }));
		Assert.That(runner.IsCompleted, Is.False);
		sequentialGate.SetResult();
		await bothParallelStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(calls.ToArray()[1], Is.EqualTo("later"));
		Assert.That(runner.IsCompleted, Is.False);
		parallelGate.SetResult();
		await runner.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(parallelStarted, Is.EqualTo(2));

		async Task Parallel(CancellationToken cancellationToken) {
			calls.Enqueue("parallel");
			if (Interlocked.Increment(ref parallelStarted) == 2)
				bothParallelStarted.TrySetResult();
			await parallelGate.Task.WaitAsync(cancellationToken);
		}
	}

	[Test]
	public async Task ParallelFailuresAreReportedTogetherAfterBothTasksComplete() {
		var first = new InvalidOperationException("first");
		var second = new InvalidOperationException("second");
		var completed = 0;
		Exception error = null;
		try {
			await Tools.Application.ExecuteInitializersAsync(new IApplicationInitializer[] {
				new AsyncInitializer(async _ => { await Task.Yield(); Interlocked.Increment(ref completed); throw first; }, parallelizable: true),
				new AsyncInitializer(async _ => { await Task.Yield(); Interlocked.Increment(ref completed); throw second; }, parallelizable: true)
			});
		} catch (Exception failure) {
			error = failure;
		}
		Assert.That(error, Is.TypeOf<AggregateException>());
		Assert.That(((AggregateException)error).Flatten().InnerExceptions, Is.EquivalentTo(new[] { first, second }));
		Assert.That(completed, Is.EqualTo(2));
	}

	[Test]
	public async Task SyncTaskBasesRunSynchronousImplementationsAndRespectCancellation() {
		var calls = new List<string>();
		IApplicationInitializer initializer = new SyncInitializer(() => calls.Add("initialize"));
		IApplicationFinalizer finalizer = new SyncFinalizer(() => calls.Add("finalize"));
		await initializer.InitializeAsync();
		await finalizer.FinalizeAsync();
		Assert.That(calls, Is.EqualTo(new[] { "initialize", "finalize" }));
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		Assert.That(async () => await initializer.InitializeAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(async () => await finalizer.FinalizeAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(calls.Count, Is.EqualTo(2));
	}

	[Test]
	public async Task FinalizersKeepRegistrationOrderAndProgressAdvancesOnlyAfterEachAwaitedTaskFinishes() {
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = new List<string>();
		var progress = new List<(int? Completed, decimal Percent)>();
		var runner = Tools.Application.ExecuteFinalizersAsync(new IApplicationFinalizer[] {
			new AsyncFinalizer(async _ => { calls.Add("first"); await gate.Task; }),
			new AsyncFinalizer(async _ => { calls.Add("second"); await Task.Yield(); })
		}, progress: (_, _, completed, _, percent) => progress.Add((completed, percent)));
		Assert.That(calls, Is.EqualTo(new[] { "first" }));
		Assert.That(progress, Is.EqualTo(new[] { ((int?)0, 0M) }));
		gate.SetResult();
		await runner.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(calls, Is.EqualTo(new[] { "first", "second" }));
		Assert.That(progress, Is.EqualTo(new[] { ((int?)0, 0M), ((int?)1, 50M), ((int?)1, 50M), ((int?)2, 100M) }));
	}

	[Test]
	public async Task FinalizersContinueAfterAsyncFailuresAndProgressObserverFailures() {
		var first = new InvalidOperationException("task");
		var observer = new InvalidOperationException("progress");
		var calls = new List<string>();
		Exception error = null;
		try {
			await Tools.Application.ExecuteFinalizersAsync(new IApplicationFinalizer[] {
				new AsyncFinalizer(async _ => { await Task.Yield(); calls.Add("failed"); throw first; }),
				new AsyncFinalizer(async _ => { await Task.Yield(); calls.Add("next"); })
			}, progress: (_, _, _, _, _) => throw observer);
		} catch (Exception failure) {
			error = failure;
		}
		Assert.That(error, Is.TypeOf<AggregateException>());
		Assert.That(((AggregateException)error).Flatten().InnerExceptions, Does.Contain(first).And.Contain(observer));
		Assert.That(calls, Is.EqualTo(new[] { "failed", "next" }));
	}

	private sealed class AsyncInitializer(Func<CancellationToken, Task> action, int priority = 100, bool parallelizable = false) : AsyncApplicationInitializerBase {
		public override int Priority => priority;
		public override bool Parallelizable => parallelizable;
		public override Task InitializeAsync(CancellationToken cancellationToken = default) => action(cancellationToken);
	}

	private sealed class SyncInitializer(Action action) : SyncApplicationInitializerBase {
		public override int Priority => 0;
		public override void Initialize() => action();
	}

#pragma warning disable CS0465 // Implements the existing synchronous task contract.
	private sealed class AsyncFinalizer(Func<CancellationToken, Task> action) : AsyncApplicationFinalizerBase {
		public override int Priority => 0;
		public override Task FinalizeAsync(CancellationToken cancellationToken = default) => action(cancellationToken);
	}

	private sealed class SyncFinalizer(Action action) : SyncApplicationFinalizerBase {
		public override int Priority => 0;
		public override void Finalize() => action();
	}
#pragma warning restore CS0465
}
