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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class AsyncTransactionalScopeBridgeTests {

	[Test]
	public void PublicSynchronousOperationsAvoidTheCallerContextAndPreserveTransactionFlow([Values] bool rollback) {
		using var returned = new ManualResetEventSlim();
		var context = new QueuedSynchronizationContext();
		var contextID = Guid.NewGuid().ToString();
		var previous = new object();
		var calls = new ConcurrentQueue<(CallbackKind Kind, object Ambient, SynchronizationContext Context)>();
		var notifications = new ConcurrentQueue<string>();
		ObservedAsyncTransactionalScope scope = null;
		Exception failure = null;
		var restored = false;
		var caller = new Thread(() => {
			SynchronizationContext.SetSynchronizationContext(context);
			CallContext.LogicalSetData(contextID, previous);
			try {
				scope = new ObservedAsyncTransactionalScope(contextID, async kind => {
					calls.Enqueue((kind, CallContext.LogicalGetData(contextID), SynchronizationContext.Current));
					await Task.Yield();
					notifications.Enqueue(kind.ToString());
				});
				scope.Committing += () => notifications.Enqueue("CommittingEvent");
				scope.Committed += () => notifications.Enqueue("CommittedEvent");
				scope.RollingBack += () => notifications.Enqueue("RollingBackEvent");
				scope.RolledBack += () => notifications.Enqueue("RolledBackEvent");
				scope.BeginTransaction();
				if (rollback)
					scope.Rollback();
				else
					scope.Commit();
				scope.Dispose();
				restored = ReferenceEquals(CallContext.LogicalGetData(contextID), previous);
			} catch (Exception error) {
				failure = error;
			} finally {
				CallContext.LogicalSetData(contextID, null);
				returned.Set();
			}
		}) { IsBackground = true };

		caller.Start();
		var completedWithoutPumping = returned.Wait(TimeSpan.FromSeconds(5));
		if (!completedWithoutPumping) {
			// Rescue a regressed adapter's captured continuations so a failing test does not leave its caller blocked.
			var stopwatch = Stopwatch.StartNew();
			while (!returned.IsSet && stopwatch.Elapsed < TimeSpan.FromSeconds(10))
				if (!context.PumpOnce())
					returned.Wait(TimeSpan.FromMilliseconds(10));
		}

		Assert.That(caller.Join(TimeSpan.FromSeconds(10)), Is.True);
		Assert.That(completedWithoutPumping, Is.True, "Synchronous adapters must not depend on pumping the blocked caller's context.");
		Assert.That(failure, Is.Null);
		Assert.That(context.Posts, Is.Zero);
		Assert.That(restored, Is.True);
		Assert.That(scope.Transaction, Is.Null);
		var observedCalls = calls.ToArray();
		Assert.That(observedCalls.Select(call => call.Kind), Is.EqualTo(rollback
			? new[] { CallbackKind.Begin, CallbackKind.RollingBack, CallbackKind.Rollback, CallbackKind.RolledBack, CallbackKind.Close, CallbackKind.TransactionEnd, CallbackKind.ContextEnd }
			: new[] { CallbackKind.Begin, CallbackKind.Committing, CallbackKind.Commit, CallbackKind.Committed, CallbackKind.Close, CallbackKind.TransactionEnd, CallbackKind.ContextEnd }));
		foreach (var call in observedCalls) {
			Assert.That(call.Context, Is.Null, $"{call.Kind} captured the caller's synchronization context.");
			Assert.That(call.Ambient, Is.SameAs(call.Kind is CallbackKind.TransactionEnd or CallbackKind.ContextEnd ? previous : scope));
		}
		Assert.That(notifications.ToArray(), Is.EqualTo(rollback
			? new[] { "Begin", "RollingBack", "RollingBackEvent", "Rollback", "RolledBack", "RolledBackEvent", "Close", "TransactionEnd", "ContextEnd" }
			: new[] { "Begin", "Committing", "CommittingEvent", "Commit", "Committed", "CommittedEvent", "Close", "TransactionEnd", "ContextEnd" }));
	}

	[Test]
	public async Task SynchronousAdaptersJoinPendingHooksAndPropagateTheirFailures([Values] CallbackKind callback, [Values] bool fail) {
		using var returned = new ManualResetEventSlim();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var contextID = Guid.NewGuid().ToString();
		var calls = 0;
		var completed = false;
		Exception failure = null;
		var caller = new Thread(() => {
			try {
				var scope = new ObservedAsyncTransactionalScope(contextID, async kind => {
					if (kind != callback)
						return;
					calls++;
					entered.SetResult();
					await release.Task;
					if (fail)
						throw new IOException("Transaction hook failed.");
					completed = true;
				});
				scope.Invoke(callback);
			} catch (Exception error) {
				failure = error;
			} finally {
				CallContext.LogicalSetData(contextID, null);
				returned.Set();
			}
		}) { IsBackground = true };

		caller.Start();
		try {
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.That(returned.Wait(TimeSpan.FromMilliseconds(100)), Is.False, "The synchronous caller must remain blocked until the pending hook finishes.");
			Assert.That(completed, Is.False);
		} finally {
			release.TrySetResult();
			Assert.That(caller.Join(TimeSpan.FromSeconds(10)), Is.True);
		}

		Assert.That(calls, Is.EqualTo(1));
		Assert.That(completed, Is.EqualTo(!fail));
		if (fail)
			Assert.That(failure, Is.TypeOf<IOException>().With.Message.EqualTo("Transaction hook failed."));
		else
			Assert.That(failure, Is.Null);
	}

	public enum CallbackKind {
		Begin,
		Close,
		Commit,
		Rollback,
		ContextEnd,
		TransactionEnd,
		Committing,
		Committed,
		RollingBack,
		RolledBack
	}

	private sealed class ObservedAsyncTransactionalScope(string contextID, Func<CallbackKind, Task> observe)
		: AsyncTransactionalScope<object>(ContextScopePolicy.MustBeRoot, contextID) {

		public void Invoke(CallbackKind callback) {
			switch (callback) {
				case CallbackKind.Begin: BeginTransactionInternal(); break;
				case CallbackKind.Close: CloseTransactionInternal(new object()); break;
				case CallbackKind.Commit: CommitInternal(new object()); break;
				case CallbackKind.Rollback: RollbackInternal(new object()); break;
				case CallbackKind.ContextEnd: OnContextEnd(); break;
				case CallbackKind.TransactionEnd: OnTransactionalScopeEnd(new List<Exception>()); break;
				case CallbackKind.Committing: OnCommitting(); break;
				case CallbackKind.Committed: OnCommitted(); break;
				case CallbackKind.RollingBack: OnRollingBack(); break;
				case CallbackKind.RolledBack: OnRolledBack(); break;
				default: throw new ArgumentOutOfRangeException(nameof(callback));
			}
		}

		protected override async Task<object> BeginTransactionInternalAsync() {
			await observe(CallbackKind.Begin);
			return new object();
		}

		protected override Task CloseTransactionInternalAsync(object transaction) => observe(CallbackKind.Close);

		protected override Task CommitInternalAsync(object transaction) => observe(CallbackKind.Commit);

		protected override Task RollbackInternalAsync(object transaction) => observe(CallbackKind.Rollback);

		protected override ValueTask OnContextEndAsync() => new(observe(CallbackKind.ContextEnd));

		protected override Task OnTransactionalScopeEndAsync(List<Exception> errors) => observe(CallbackKind.TransactionEnd);

		protected override Task OnCommittingAsync() => observe(CallbackKind.Committing);

		protected override Task OnCommittedAsync() => observe(CallbackKind.Committed);

		protected override Task OnRollingBackAsync() => observe(CallbackKind.RollingBack);

		protected override Task OnRolledBackAsync() => observe(CallbackKind.RolledBack);
	}

	private sealed class QueuedSynchronizationContext : SynchronizationContext {
		private readonly ConcurrentQueue<(SendOrPostCallback Callback, object Item)> _callbacks = new();
		private int _posts;

		public int Posts => Volatile.Read(ref _posts);

		public override void Post(SendOrPostCallback callback, object item) {
			Interlocked.Increment(ref _posts);
			_callbacks.Enqueue((callback, item));
		}

		public bool PumpOnce() {
			if (!_callbacks.TryDequeue(out var work))
				return false;
			var previousContext = Current;
			try {
				SetSynchronizationContext(this);
				work.Callback(work.Item);
			} finally {
				SetSynchronizationContext(previousContext);
			}
			return true;
		}
	}
}
