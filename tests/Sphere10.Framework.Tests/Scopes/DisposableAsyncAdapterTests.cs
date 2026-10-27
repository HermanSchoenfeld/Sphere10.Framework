// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class DisposableAsyncAdapterTests {

	[TestCase("disposable")]
	[TestCase("repository")]
	[TestCase("bag")]
	[TestCase("resource")]
	public async Task SyncCleanupReturnsAnIncompleteOperationWhileTheWorkerIsBlocked(string kind) {
		using var entered = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		using var returned = new ManualResetEventSlim();
		var calls = 0;
		var owner = CreateSyncOwner(kind, () => {
			entered.Set();
			if (!release.Wait(TimeSpan.FromSeconds(10)))
				throw new TimeoutException("Cleanup was not released.");
			Interlocked.Increment(ref calls);
		});
		var operation = ValueTask.CompletedTask;
		Exception invocationError = null;
		var caller = new Thread(() => {
			try {
				operation = owner.DisposeAsync();
			} catch (Exception error) {
				invocationError = error;
			} finally {
				returned.Set();
			}
		}) { IsBackground = true };
		caller.Start();
		try {
			Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
			Assert.That(returned.Wait(TimeSpan.FromSeconds(5)), Is.True, "DisposeAsync must return while synchronous cleanup is still blocked.");
			Assert.That(invocationError, Is.Null);
			Assert.That(operation.IsCompleted, Is.False);
			Assert.That(calls, Is.Zero);
		} finally {
			release.Set();
			Assert.That(caller.Join(TimeSpan.FromSeconds(5)), Is.True);
			await operation.AsTask().WaitAsync(TimeSpan.FromSeconds(5));
		}
		Assert.That(calls, Is.EqualTo(1));
	}

	[TestCase("disposable")]
	[TestCase("repository")]
	[TestCase("bag")]
	[TestCase("resource")]
	public async Task SyncCleanupFailuresBelongToTheReturnedOperation(string kind) {
		var owner = CreateSyncOwner(kind, () => throw new IOException("Cleanup failed."));
		var operation = ValueTask.CompletedTask;
		Assert.That(() => { operation = owner.DisposeAsync(); }, Throws.Nothing);
		await Assert.ThatAsync(async () => await operation, Throws.TypeOf<IOException>().With.Message.EqualTo("Cleanup failed."));
	}

	[Test]
	public async Task BagPrefersAsyncDisposalAndAwaitsEachChildInOriginalOrder() {
		var events = new ConcurrentQueue<string>();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var first = new AsyncChild(async () => {
			events.Enqueue("first started");
			entered.SetResult();
			await release.Task;
			events.Enqueue("first completed");
		});
		var second = new SyncOnlyChild(() => events.Enqueue("second"));
		var third = new AsyncChild(() => {
			events.Enqueue("third");
			return ValueTask.CompletedTask;
		});
		var bag = new Disposables(first, second, third);
		var operation = bag.DisposeAsync();
		try {
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(operation.IsCompleted, Is.False);
			Assert.That(events.ToArray(), Is.EqualTo(new[] { "first started" }));
		} finally {
			release.TrySetResult();
			await operation.AsTask().WaitAsync(TimeSpan.FromSeconds(5));
		}
		Assert.That(events.ToArray(), Is.EqualTo(new[] { "first started", "first completed", "second", "third" }));
		Assert.That(first.SyncCalls, Is.Zero);
		Assert.That(first.AsyncCalls, Is.EqualTo(1));
		Assert.That(third.SyncCalls, Is.Zero);
		Assert.That(third.AsyncCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task BagPreservesExceptionPolicyForSyncAndAsyncChildren([Values] bool ignoreExceptions, [Values] bool asyncFailure) {
		var laterCalls = 0;
		IDisposable first = asyncFailure
			? new AsyncChild(() => ValueTask.FromException(new IOException("Child failed.")))
			: new SyncOnlyChild(() => throw new IOException("Child failed."));
		var bag = new Disposables(ignoreExceptions, first, new SyncOnlyChild(() => laterCalls++));
		if (ignoreExceptions)
			await bag.DisposeAsync();
		else
			await Assert.ThatAsync(async () => await bag.DisposeAsync(), Throws.TypeOf<IOException>().With.Message.EqualTo("Child failed."));
		Assert.That(laterCalls, Is.EqualTo(ignoreExceptions ? 1 : 0));
	}

	[Test]
	public void AsyncFirstSynchronousDisposalWaitsForCleanupAndPropagatesFailure([Values] bool repository, [Values] bool fail) {
		using var entered = new ManualResetEventSlim();
		using var returned = new ManualResetEventSlim();
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var completed = false;
		Func<ValueTask> cleanup = async () => {
			entered.Set();
			await release.Task;
			if (fail)
				throw new IOException("Async cleanup failed.");
			completed = true;
		};
		IDisposable owner = repository ? new AsyncRepository(cleanup) : new AsyncOwner(cleanup);
		Exception failure = null;
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
			Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
			Assert.That(returned.Wait(TimeSpan.FromMilliseconds(100)), Is.False, "Synchronous disposal must join the complete asynchronous operation.");
		} finally {
			release.TrySetResult();
			Assert.That(caller.Join(TimeSpan.FromSeconds(5)), Is.True);
		}
		if (fail) {
			Assert.That(failure, Is.TypeOf<IOException>());
			Assert.That(failure.Message, Is.EqualTo("Async cleanup failed."));
		} else {
			Assert.That(failure, Is.Null);
			Assert.That(completed, Is.True);
		}
	}

	[Test]
	public async Task SynchronizedRepositoryAsyncDisposalKeepsTheWorkerLockUntilCompletion([Values] bool fail) {
		using var entered = new ManualResetEventSlim();
		using var returned = new ManualResetEventSlim();
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		SynchronizedRepository<int, int> repository = null;
		var calls = 0;
		var inner = new AsyncRepository(async () => {
			calls++;
			entered.Set();
			await release.Task;
			if (fail)
				throw new IOException("Repository cleanup failed.");
		});
		repository = new SynchronizedRepository<int, int>(inner);
		var operation = ValueTask.CompletedTask;
		Exception invocationError = null;
		var caller = new Thread(() => {
			try {
				operation = repository.DisposeAsync();
			} catch (Exception error) {
				invocationError = error;
			} finally {
				returned.Set();
			}
		}) { IsBackground = true };
		caller.Start();
		try {
			Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
			Assert.That(returned.Wait(TimeSpan.FromSeconds(5)), Is.True, "The caller must not pump or wait for the inner disposal.");
			Assert.That(invocationError, Is.Null);
			Assert.That(operation.IsCompleted, Is.False);
			var acquired = repository.ThreadLock.TryEnterReadLock(0);
			if (acquired)
				repository.ThreadLock.ExitReadLock();
			Assert.That(acquired, Is.False, "The worker's write scope must exclude readers throughout asynchronous cleanup.");
		} finally {
			release.TrySetResult();
			Assert.That(caller.Join(TimeSpan.FromSeconds(5)), Is.True);
			if (fail)
				await Assert.ThatAsync(async () => await operation.AsTask().WaitAsync(TimeSpan.FromSeconds(5)),
					Throws.TypeOf<IOException>().With.Message.EqualTo("Repository cleanup failed."));
			else
				await operation.AsTask().WaitAsync(TimeSpan.FromSeconds(5));
		}
		Assert.That(calls, Is.EqualTo(1));
		var released = repository.ThreadLock.TryEnterReadLock(0);
		if (released)
			repository.ThreadLock.ExitReadLock();
		Assert.That(released, Is.True, "The write scope must be released on success and failure.");
	}

	[Test]
	public async Task NestedSynchronizedRepositoriesPreserveSharedLockDisposal([Values] bool shareInstance, [Values] bool fail) {
		var calls = 0;
		var leaf = new AsyncRepository(() => {
			Interlocked.Increment(ref calls);
			return fail ? ValueTask.FromException(new IOException("Nested cleanup failed.")) : ValueTask.CompletedTask;
		});
		var shared = new SynchronizedObject();
		var innerGate = shareInstance ? shared : new SynchronizedObject { ParentSyncObject = shared };
		var outerGate = shareInstance ? shared : new SynchronizedObject { ParentSyncObject = shared };
		var inner = new SynchronizedRepository<int, int>(leaf, innerGate);
		var outer = new SynchronizedRepository<int, int>(inner, outerGate);

		var operation = outer.DisposeAsync().AsTask();
		if (fail)
			await Assert.ThatAsync(async () => await operation.WaitAsync(TimeSpan.FromSeconds(5)),
				Throws.TypeOf<IOException>().With.Message.EqualTo("Nested cleanup failed."));
		else
			await operation.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.That(calls, Is.EqualTo(1));
		Assert.That(shared.ThreadLock.TryEnterWriteLock(0), Is.True);
		shared.ThreadLock.ExitWriteLock();
	}

	private static IAsyncDisposable CreateSyncOwner(string kind, Action cleanup) => kind switch {
		"disposable" => new ActionDisposable(cleanup),
		"repository" => new SyncRepository(cleanup),
		"bag" => new Disposables(new SyncOnlyChild(cleanup)),
		"resource" => new ResourceOwner(new SyncOnlyChild(cleanup)),
		_ => throw new ArgumentOutOfRangeException(nameof(kind))
	};

	private sealed class SyncOnlyChild(Action cleanup) : IDisposable {
		public void Dispose() => cleanup();
	}

	private sealed class AsyncChild(Func<ValueTask> cleanup) : IDisposable, IAsyncDisposable {
		public int SyncCalls { get; private set; }
		public int AsyncCalls { get; private set; }
		public void Dispose() => SyncCalls++;
		public ValueTask DisposeAsync() {
			AsyncCalls++;
			return cleanup();
		}
	}

	private sealed class ResourceOwner : DisposableResource {
		public ResourceOwner(IDisposable child) => Disposables.Add(child);
	}

	private sealed class AsyncOwner(Func<ValueTask> cleanup) : AsyncDisposable {
		protected override ValueTask FreeManagedResourcesAsync() => cleanup();
	}

	private sealed class SyncRepository(Action cleanup) : SyncRepositoryBase<int, int> {
		protected override void FreeManagedResources() => cleanup();
		public override bool TryGet(int identity, out int entity) => throw new NotSupportedException();
		public override void Create(int entity) => throw new NotSupportedException();
		public override void Update(int entity) => throw new NotSupportedException();
		public override void Delete(int identity) => throw new NotSupportedException();
		public override void Clear() => throw new NotSupportedException();
	}

	private sealed class AsyncRepository(Func<ValueTask> cleanup) : AsyncRepositoryBase<int, int> {
		protected override ValueTask FreeManagedResourcesAsync() => cleanup();
		public override Task<(bool, int)> TryGetAsync(int identity) => throw new NotSupportedException();
		public override Task CreateAsync(int entity) => throw new NotSupportedException();
		public override Task UpdateAsync(int entity) => throw new NotSupportedException();
		public override Task DeleteAsync(int identity) => throw new NotSupportedException();
		public override Task ClearAsync() => throw new NotSupportedException();
	}
}
