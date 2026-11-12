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
public class TaskExtensionsTests {
	private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

	// WaitSafe

	[Test]
	public void WaitSafeReturnsOnlyAfterTheTaskCompletes() {
		var completed = false;
		var task = Task.Run(async () => {
			await Task.Delay(100);
			completed = true;
		});

		task.WaitSafe();

		Assert.That(completed, Is.True);
	}

	[Test]
	public void WaitSafeRethrowsTheTasksOwnException() {
		var task = Task.Run(async () => {
			await Task.Delay(50);
			throw new InvalidOperationException("failed");
		});

		Assert.That(() => task.WaitSafe(), Throws.InvalidOperationException.With.Message.EqualTo("failed"));
	}

	[Test]
	public void WaitSafeCancellationEndsOnlyTheWait() {
		var release = new TaskCompletionSource();
		var task = Task.Run(() => release.Task);
		using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

		Assert.That(() => task.WaitSafe(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(task.IsCompleted, Is.False);

		release.SetResult();
		task.WaitSafe();
		Assert.That(task.IsCompletedSuccessfully, Is.True);
	}

	// ResultSafe

	[Test]
	public void ResultSafeReturnsTheTasksResult() {
		var task = Task.Run(async () => {
			await Task.Delay(50);
			return 42;
		});

		Assert.That(task.ResultSafe(), Is.EqualTo(42));
	}

	[Test]
	public void ResultSafeRethrowsTheTasksOwnException() {
		var task = Task.Run<int>(async () => {
			await Task.Delay(50);
			throw new InvalidOperationException("failed");
		});

		Assert.That(() => task.ResultSafe(), Throws.InvalidOperationException.With.Message.EqualTo("failed"));
	}

	[Test]
	public void ResultSafeCancellationEndsOnlyTheWait() {
		var release = new TaskCompletionSource<int>();
		var task = Task.Run(() => release.Task);
		using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

		Assert.That(() => task.ResultSafe(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());

		release.SetResult(7);
		Assert.That(task.ResultSafe(), Is.EqualTo(7));
	}

	// Async-first adapters

	[Test]
	public void AsyncLoadableLoadWaitsForTheAsynchronousLoad() {
		var loadable = new DelayedLoadable();

		loadable.Load();

		Assert.That(loadable.IsLoaded, Is.True);
	}

	[Test]
	public void AsyncLoadableLoadRethrowsTheAsynchronousFailure() {
		var loadable = new DelayedLoadable { Fail = true };

		Assert.That(() => loadable.Load(), Throws.InvalidOperationException);
	}

	[Test]
	public void AsyncLoadableLoadCompletesOnACallerContextThatNeverRunsContinuations() {
		var loadable = new DelayedLoadable();
		Exception error = null;
		var thread = new Thread(() => {
			SynchronizationContext.SetSynchronizationContext(new NonPumpingSynchronizationContext());
			try {
				loadable.Load();
			} catch (Exception exception) {
				error = exception;
			}
		}) { IsBackground = true };

		thread.Start();

		Assert.That(thread.Join(Deadline), Is.True);
		Assert.That(error, Is.Null);
		Assert.That(loadable.IsLoaded, Is.True);
	}

	private sealed class DelayedLoadable : AsyncLoadableBase {

		public bool Fail { get; init; }

		public bool IsLoaded { get; private set; }

		protected override async Task LoadInternalAsync(CancellationToken cancellationToken) {
			await Task.Delay(100, cancellationToken);
			if (Fail)
				throw new InvalidOperationException("load failed");
			IsLoaded = true;
		}
	}

	private sealed class NonPumpingSynchronizationContext : SynchronizationContext {

		public override void Post(SendOrPostCallback callback, object state) {
			// Continuations captured on this context never run, as on a blocked UI thread
		}
	}
}
