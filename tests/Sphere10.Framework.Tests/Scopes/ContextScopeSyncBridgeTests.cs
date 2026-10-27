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
using System.Threading.Tasks.Sources;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ContextScopeSyncBridgeTests {
	[Test]
	public async Task SynchronousDisposalWaitsForPendingSourceAndConsumesItOnce([Values] CleanupKind kind, [Values] bool fail) {
		var source = new FinalizationSource();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var contextID = Guid.NewGuid().ToString();
		var previous = new object();
		var calls = 0;
		var disposing = Task.Factory.StartNew(() => {
			CallContext.LogicalSetData(contextID, previous);
			try {
				var scope = CreateScope(kind, () => {
					Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
					Interlocked.Increment(ref calls);
					entered.SetResult();
					return source.Operation;
				}, contextID);
				scope.Dispose();
				return ReferenceEquals(CallContext.LogicalGetData(contextID), previous);
			} finally {
				CallContext.LogicalSetData(contextID, null);
			}
		}, CancellationToken.None, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		try {
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.That(SpinWait.SpinUntil(() => source.ContinuationRegistered || disposing.IsCompleted, TimeSpan.FromSeconds(10)), Is.True);
			Assert.That(source.ContinuationRegistered, Is.True, "Synchronous cleanup must await a pending source rather than call GetResult before completion.");
			Assert.That(disposing.IsCompleted, Is.False);
			source.Complete(fail ? new FinalizationException() : null);
			if (fail)
				await Assert.ThatAsync(async () => await disposing.WaitAsync(TimeSpan.FromSeconds(10)), Throws.TypeOf<FinalizationException>());
			else
				Assert.That(await disposing.WaitAsync(TimeSpan.FromSeconds(10)), Is.True, "The caller's ambient context must be restored before cleanup starts.");
			Assert.That(calls, Is.EqualTo(1));
			Assert.That(source.ResultsConsumed, Is.EqualTo(1));
		} finally {
			source.Complete();
			try {
				await disposing.WaitAsync(TimeSpan.FromSeconds(10));
			} catch (FinalizationException) when (fail) {
			}
		}
	}

	[Test]
	public void SynchronousDisposalStartsAsyncCleanupOutsideTheCallerContext([Values] CleanupKind kind) {
		var previousContext = SynchronizationContext.Current;
		var context = new RecordingSynchronizationContext();
		var contextID = Guid.NewGuid().ToString();
		var previous = new object();
		SynchronizationContext callbackContext = context;
		var calls = 0;
		async ValueTask Finalize() {
			callbackContext = SynchronizationContext.Current;
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
			await Task.Yield();
			calls++;
		}
		CallContext.LogicalSetData(contextID, previous);
		try {
			SynchronizationContext.SetSynchronizationContext(context);
			var scope = CreateScope(kind, Finalize, contextID);
			scope.Dispose();
			Assert.That(callbackContext, Is.Null);
			Assert.That(SynchronizationContext.Current, Is.SameAs(context));
			Assert.That(context.Posts, Is.Zero);
			Assert.That(calls, Is.EqualTo(1));
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
		} finally {
			SynchronizationContext.SetSynchronizationContext(previousContext);
			CallContext.LogicalSetData(contextID, null);
		}
	}

	[Test]
	public void SynchronousDisposalPropagatesFactoryFailureAfterRestoringAmbientState([Values] CleanupKind kind) {
		var contextID = Guid.NewGuid().ToString();
		var previous = new object();
		CallContext.LogicalSetData(contextID, previous);
		try {
			var scope = CreateScope(kind, () => throw new FinalizationException(), contextID);
			Assert.That(scope.Dispose, Throws.TypeOf<FinalizationException>());
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
		} finally {
			CallContext.LogicalSetData(contextID, null);
		}
	}

	private static IContextScope CreateScope(CleanupKind kind, Func<ValueTask> finalizer, string contextID) => kind switch {
		CleanupKind.Scope => new SourceContextScope(finalizer, static () => ValueTask.CompletedTask, contextID),
		CleanupKind.Context => new SourceContextScope(static () => ValueTask.CompletedTask, finalizer, contextID),
		CleanupKind.TransactionalContext => new SourceTransactionalScope(finalizer, contextID),
		_ => throw new ArgumentOutOfRangeException(nameof(kind))
	};

	public enum CleanupKind {
		Scope,
		Context,
		TransactionalContext
	}

	private sealed class SourceContextScope : AsyncContextScope {
		private readonly Func<ValueTask> _scopeFinalizer;
		private readonly Func<ValueTask> _contextFinalizer;

		public SourceContextScope(Func<ValueTask> scopeFinalizer, Func<ValueTask> contextFinalizer, string contextID)
			: base(ContextScopePolicy.MustBeRoot, contextID) {
			_scopeFinalizer = scopeFinalizer;
			_contextFinalizer = contextFinalizer;
		}

		protected override ValueTask OnScopeEndInternalAsync() => _scopeFinalizer();

		protected override ValueTask OnContextEndAsync() => _contextFinalizer();
	}

	private sealed class SourceTransactionalScope : AsyncTransactionalScope<object> {
		private readonly Func<ValueTask> _contextFinalizer;

		public SourceTransactionalScope(Func<ValueTask> contextFinalizer, string contextID)
			: base(ContextScopePolicy.MustBeRoot, contextID) {
			_contextFinalizer = contextFinalizer;
		}

		protected override Task<object> BeginTransactionInternalAsync() => Task.FromResult(new object());

		protected override Task CloseTransactionInternalAsync(object transaction) => Task.CompletedTask;

		protected override Task CommitInternalAsync(object transaction) => Task.CompletedTask;

		protected override Task RollbackInternalAsync(object transaction) => Task.CompletedTask;

		protected override ValueTask OnContextEndAsync() => _contextFinalizer();
	}

	private sealed class FinalizationSource : IValueTaskSource {
		private ManualResetValueTaskSourceCore<bool> _source = new() { RunContinuationsAsynchronously = true };
		private int _continuationRegistered;
		private int _completed;
		private int _resultsConsumed;

		public bool ContinuationRegistered => Volatile.Read(ref _continuationRegistered) != 0;

		public int ResultsConsumed => Volatile.Read(ref _resultsConsumed);

		public ValueTask Operation => new(this, _source.Version);

		public void Complete(Exception error = null) {
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

	private sealed class RecordingSynchronizationContext : SynchronizationContext {
		private int _posts;

		public int Posts => Volatile.Read(ref _posts);

		public override void Post(SendOrPostCallback callback, object item) {
			Interlocked.Increment(ref _posts);
			ThreadPool.QueueUserWorkItem(static work => work.Callback(work.Item), (Callback: callback, Item: item), false);
		}
	}

	private sealed class FinalizationException : Exception {
	}
}
