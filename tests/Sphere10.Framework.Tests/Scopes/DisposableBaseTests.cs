// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class DisposableBaseTests {

	[Test]
	public async Task CompletedOwnerFinalizesOnceAcrossBothDisposalDirections([Values] bool asyncFirst, [Values] bool asyncDisposalFirst) {
		var managedCalls = 0;
		var unmanagedCalls = 0;
		var owner = CreateOwner(asyncFirst, () => managedCalls++, () => unmanagedCalls++);

		if (asyncDisposalFirst)
			await owner.DisposeAsync();
		else
			owner.Dispose();
		owner.Dispose();
		await owner.DisposeAsync();
		owner.Dispose();
		await owner.DisposeAsync();

		Assert.That(managedCalls, Is.EqualTo(1));
		Assert.That(unmanagedCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task FailedCleanupCanBeRetried([Values] bool asyncFirst, [Values] bool asyncDisposal) {
		var managedCalls = 0;
		var unmanagedCalls = 0;
		var owner = CreateOwner(asyncFirst, () => {
			if (++managedCalls == 1)
				throw new IOException("Cleanup failed.");
		}, () => unmanagedCalls++);

		if (asyncDisposal)
			await Assert.ThatAsync(async () => await owner.DisposeAsync(), Throws.TypeOf<IOException>().With.Message.EqualTo("Cleanup failed."));
		else
			Assert.That(owner.Dispose, Throws.TypeOf<IOException>().With.Message.EqualTo("Cleanup failed."));
		Assert.That(unmanagedCalls, Is.Zero);

		if (asyncDisposal)
			await owner.DisposeAsync();
		else
			owner.Dispose();
		owner.Dispose();
		await owner.DisposeAsync();

		Assert.That(managedCalls, Is.EqualTo(2));
		Assert.That(unmanagedCalls, Is.EqualTo(1));
	}

	[Test]
	public void SyncFirstSynchronousDisposalRunsOnTheCallingThread() {
		var callerThreadId = Environment.CurrentManagedThreadId;
		var cleanupThreadId = 0;
		var owner = new SyncOwner(() => cleanupThreadId = Environment.CurrentManagedThreadId, static () => { });

		owner.Dispose();

		Assert.That(cleanupThreadId, Is.EqualTo(callerThreadId));
	}

	[Test]
	public async Task AsyncFirstAsynchronousDisposalStartsOnTheCallingThread() {
		var callerThreadId = Environment.CurrentManagedThreadId;
		var cleanupThreadId = 0;
		var owner = new AsyncOwner(() => {
			cleanupThreadId = Environment.CurrentManagedThreadId;
			return ValueTask.CompletedTask;
		}, static () => { });

		await owner.DisposeAsync();

		Assert.That(cleanupThreadId, Is.EqualTo(callerThreadId));
	}

	[Test]
	public void AsyncFirstSynchronousDisposalStartsOutsideTheCallerSynchronizationContext() {
		var context = new RecordingSynchronizationContext();
		SynchronizationContext callbackContext = context;
		var managedCalls = 0;
		Exception failure = null;
		var owner = new AsyncOwner(async () => {
			callbackContext = SynchronizationContext.Current;
			await Task.Yield();
			managedCalls++;
		}, static () => { });
		var caller = new Thread(() => {
			SynchronizationContext.SetSynchronizationContext(context);
			try {
				owner.Dispose();
			} catch (Exception error) {
				failure = error;
			}
		}) { IsBackground = true };

		caller.Start();
		Assert.That(caller.Join(TimeSpan.FromSeconds(10)), Is.True);
		Assert.That(failure, Is.Null);
		Assert.That(callbackContext, Is.Null);
		Assert.That(context.Posts, Is.Zero, "The cleanup factory must start outside the caller's synchronization context.");
		Assert.That(managedCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task AsyncFirstSynchronousDisposalWaitsForAPendingSourceAndConsumesItOnce([Values] bool fail) {
		using var entered = new ManualResetEventSlim();
		using var returned = new ManualResetEventSlim();
		var source = new FinalizationSource();
		var managedCalls = 0;
		var unmanagedCalls = 0;
		Exception failure = null;
		var owner = new AsyncOwner(() => {
			if (++managedCalls != 1)
				return ValueTask.CompletedTask;
			entered.Set();
			return source.Operation;
		}, () => unmanagedCalls++);
		var caller = new Thread(() => {
			try {
				owner.Dispose();
			} catch (Exception error) {
				failure = error;
			} finally {
				returned.Set();
			}
		}) { IsBackground = true };

		caller.Start();
		try {
			Assert.That(entered.Wait(TimeSpan.FromSeconds(10)), Is.True);
			Assert.That(SpinWait.SpinUntil(() => source.ContinuationRegistered || returned.IsSet, TimeSpan.FromSeconds(10)), Is.True);
			Assert.That(source.ContinuationRegistered, Is.True, "The bridge must register an await rather than consume a pending source.");
			Assert.That(returned.IsSet, Is.False);
			Assert.That(unmanagedCalls, Is.Zero);
		} finally {
			source.Complete(fail ? new IOException("Pending cleanup failed.") : null);
			Assert.That(caller.Join(TimeSpan.FromSeconds(10)), Is.True);
		}

		if (fail)
			Assert.That(failure, Is.TypeOf<IOException>().With.Message.EqualTo("Pending cleanup failed."));
		else
			Assert.That(failure, Is.Null);
		Assert.That(source.ResultsConsumed, Is.EqualTo(1));
		Assert.That(unmanagedCalls, Is.EqualTo(fail ? 0 : 1));

		await owner.DisposeAsync();
		owner.Dispose();
		Assert.That(managedCalls, Is.EqualTo(fail ? 2 : 1));
		Assert.That(unmanagedCalls, Is.EqualTo(1));
		Assert.That(source.ResultsConsumed, Is.EqualTo(1));
	}

	[Test]
	public async Task AsyncFirstAsynchronousDisposalWaitsForAPendingSourceAndConsumesItOnce([Values] bool fail) {
		var source = new FinalizationSource();
		var managedCalls = 0;
		var unmanagedCalls = 0;
		var owner = new AsyncOwner(() => ++managedCalls == 1 ? source.Operation : ValueTask.CompletedTask, () => unmanagedCalls++);
		var operation = owner.DisposeAsync();
		try {
			Assert.That(operation.IsCompleted, Is.False);
			Assert.That(source.ContinuationRegistered, Is.True);
			Assert.That(unmanagedCalls, Is.Zero);
		} finally {
			source.Complete(fail ? new IOException("Pending cleanup failed.") : null);
		}

		if (fail)
			await Assert.ThatAsync(async () => await operation, Throws.TypeOf<IOException>().With.Message.EqualTo("Pending cleanup failed."));
		else
			await operation.AsTask().WaitAsync(TimeSpan.FromSeconds(10));
		Assert.That(source.ResultsConsumed, Is.EqualTo(1));
		Assert.That(unmanagedCalls, Is.EqualTo(fail ? 0 : 1));

		owner.Dispose();
		await owner.DisposeAsync();
		Assert.That(managedCalls, Is.EqualTo(fail ? 2 : 1));
		Assert.That(unmanagedCalls, Is.EqualTo(1));
		Assert.That(source.ResultsConsumed, Is.EqualTo(1));
	}

	private static Disposable CreateOwner(bool asyncFirst, Action managedCleanup, Action unmanagedCleanup)
		=> asyncFirst ? new AsyncOwner(() => {
			managedCleanup();
			return ValueTask.CompletedTask;
		}, unmanagedCleanup) : new SyncOwner(managedCleanup, unmanagedCleanup);

	private sealed class SyncOwner(Action managedCleanup, Action unmanagedCleanup) : SyncDisposable {
		protected override void FreeManagedResources() => managedCleanup();

		protected override void FreeUnmanagedResources() => unmanagedCleanup();
	}

	private sealed class AsyncOwner(Func<ValueTask> managedCleanup, Action unmanagedCleanup) : AsyncDisposable {
		protected override ValueTask FreeManagedResourcesAsync() => managedCleanup();

		protected override void FreeUnmanagedResources() => unmanagedCleanup();
	}

	private sealed class RecordingSynchronizationContext : SynchronizationContext {
		private int _posts;

		public int Posts => Volatile.Read(ref _posts);

		public override void Post(SendOrPostCallback callback, object item) {
			Interlocked.Increment(ref _posts);
			ThreadPool.QueueUserWorkItem(static work => work.Callback(work.Item), (Callback: callback, Item: item), false);
		}
	}

	private sealed class FinalizationSource : IValueTaskSource {
		private ManualResetValueTaskSourceCore<bool> _source = new() { RunContinuationsAsynchronously = true };
		private int _continuationRegistered;
		private int _completed;
		private int _resultsConsumed;

		public bool ContinuationRegistered => Volatile.Read(ref _continuationRegistered) != 0;

		public int ResultsConsumed => Volatile.Read(ref _resultsConsumed);

		public ValueTask Operation => new(this, _source.Version);

		public void Complete(Exception error) {
			if (Interlocked.Exchange(ref _completed, 1) != 0)
				return;
			if (error is null)
				_source.SetResult(true);
			else
				_source.SetException(error);
		}

		public ValueTaskSourceStatus GetStatus(short token) => _source.GetStatus(token);

		public void OnCompleted(Action<object> continuation, object item, short token, ValueTaskSourceOnCompletedFlags flags) {
			_source.OnCompleted(continuation, item, token, flags);
			Volatile.Write(ref _continuationRegistered, 1);
		}

		public void GetResult(short token) {
			Interlocked.Increment(ref _resultsConsumed);
			_source.GetResult(token);
		}
	}
}
