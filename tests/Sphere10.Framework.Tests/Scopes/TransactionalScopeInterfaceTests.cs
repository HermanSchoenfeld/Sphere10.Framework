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

namespace Sphere10.Framework.UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class TransactionalScopeInterfaceTests {
	[Test]
	public void NonGenericDefaultNarrowsContextRoot() {
		using var root = new DirectTransactionalScope();
		using var nested = new DirectTransactionalScope { RootScope = root.RootScope };

		Assert.That(((ITransactionalScope)root).RootScope, Is.SameAs(root.RootScope));
		Assert.That(((ITransactionalScope)nested).RootScope, Is.SameAs(root.RootScope));
		Assert.That(((IContextScope)nested).RootScope, Is.SameAs(root.RootScope));
	}

	[Test]
	public void NonGenericDefaultAndDecoratorPreserveNullForNonTransactionalRoot() {
		var contextID = Guid.NewGuid().ToString();
		using var root = new ActionContextScope(() => { }, ContextScopePolicy.MustBeRoot, contextID);
		var rootState = ((IContextScope)root).RootScope;
		using var inner = new DirectTransactionalScope { RootScope = rootState };
		var decorator = new TransactionalScopeDecorator<DirectTransactionalScope>(inner);

		Assert.That(((IContextScope)inner).RootScope, Is.SameAs(rootState));
		Assert.That(((ITransactionalScope)inner).RootScope, Is.Null);
		Assert.That(decorator.RootScope, Is.Null);
		Assert.That(((ITransactionalScope)decorator).RootScope, Is.Null);
	}

	[Test]
	public void NestedActionScopesShareRootAndCommitOwningTransaction() {
		var contextID = Guid.NewGuid().ToString();
		var transaction = new object();
		var commits = 0;
		var closes = 0;
		using (var root = new ActionTransactionalScope<object>(ContextScopePolicy.MustBeRoot, contextID, () => transaction, _ => commits++, _ => { }, _ => closes++)) {
			var rootState = ((IContextScope)root).RootScope;
			root.BeginTransaction();
			using (var nested = new ActionTransactionalScope<object>(ContextScopePolicy.MustBeNested, contextID, () => new object(), _ => { }, _ => { }, _ => { })) {
				var typed = (ITransactionalScope<TransactionalScopeBase<object>>)nested;
				var untyped = (ITransactionalScope)nested;
				var decorator = new TransactionalScopeDecorator<ActionTransactionalScope<object>>(nested);

				Assert.That(nested.RootScope, Is.EqualTo(root));
				Assert.That(typed.RootScope, Is.SameAs(rootState));
				Assert.That(untyped.RootScope, Is.SameAs(rootState));
				Assert.That(((IContextScope)nested).RootScope, Is.SameAs(rootState));
				Assert.That(((ITransactionalScope)decorator).RootScope, Is.SameAs(rootState));
				Assert.That(nested.Transaction, Is.SameAs(transaction));
				nested.BeginTransaction();
				nested.Commit();
				Assert.That(commits, Is.Zero);
			}
			root.Commit();
			Assert.That(root.Transaction, Is.Null);
		}

		Assert.That(commits, Is.EqualTo(1));
		Assert.That(closes, Is.EqualTo(1));
		Assert.That(CallContext.LogicalGetData(contextID), Is.Null);
	}

	[Test]
	public void IncompatibleConcreteRootStillThrowsDuringNestedConstruction() {
		var contextID = Guid.NewGuid().ToString();
		using var incompatible = new ActionTransactionalScope<string>(ContextScopePolicy.MustBeRoot, contextID, () => "transaction", _ => { }, _ => { }, _ => { });

		Assert.That(
			() => new ActionTransactionalScope<object>(ContextScopePolicy.MustBeNested, contextID, () => new object(), _ => { }, _ => { }, _ => { }),
			Throws.TypeOf<InvalidCastException>()
		);
		Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(((IContextScope)incompatible).RootScope));
	}

	[Test]
	public void NestedParticipantOwnershipAndSiblingRollbackVoteRemainShared() {
		var contextID = Guid.NewGuid().ToString();
		var transaction = new object();
		var commits = 0;
		var rollbacks = 0;
		var closes = 0;
		using (var root = new ActionTransactionalScope<object>(ContextScopePolicy.MustBeRoot, contextID, () => new object(), _ => { }, _ => { }, _ => { })) {
			using (var owner = new ActionTransactionalScope<object>(ContextScopePolicy.MustBeNested, contextID, () => transaction, _ => commits++, _ => rollbacks++, _ => closes++)) {
				owner.BeginTransaction();
				using (var sibling = new ActionTransactionalScope<object>(ContextScopePolicy.MustBeNested, contextID, () => new object(), _ => { }, _ => { }, _ => { })) {
					sibling.BeginTransaction();
					Assert.That(sibling.Transaction, Is.SameAs(transaction));
					sibling.Rollback();
				}
				owner.Commit();
				Assert.That(root.Transaction, Is.Null);
			}
		}
		Assert.That(commits, Is.Zero);
		Assert.That(rollbacks, Is.EqualTo(1));
		Assert.That(closes, Is.EqualTo(1));
		Assert.That(CallContext.LogicalGetData(contextID), Is.Null);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ActionAndTaskScopesCanShareTheSameTransactionRoot(bool taskRoot) {
		var contextID = Guid.NewGuid().ToString();
		var transaction = new object();
		var commits = 0;
		var closes = 0;
		if (taskRoot) {
			using var root = new TaskTransactionalScope<object>(ContextScopePolicy.MustBeRoot, contextID, () => Task.FromResult(transaction), _ => { commits++; return Task.CompletedTask; }, _ => Task.CompletedTask, _ => { closes++; return Task.CompletedTask; });
			root.BeginTransaction();
			using (var nested = new ActionTransactionalScope<object>(ContextScopePolicy.MustBeNested, contextID, () => new object(), _ => { }, _ => { }, _ => { })) {
				Assert.That(((IContextScope)nested).RootScope, Is.SameAs(((IContextScope)root).RootScope));
				Assert.That(nested.Transaction, Is.SameAs(transaction));
				nested.BeginTransaction();
				nested.Commit();
			}
			root.Commit();
		} else {
			using var root = new ActionTransactionalScope<object>(ContextScopePolicy.MustBeRoot, contextID, () => transaction, _ => commits++, _ => { }, _ => closes++);
			root.BeginTransaction();
			using (var nested = new TaskTransactionalScope<object>(ContextScopePolicy.MustBeNested, contextID, () => Task.FromResult(new object()), _ => Task.CompletedTask, _ => Task.CompletedTask, _ => Task.CompletedTask)) {
				Assert.That(((IContextScope)nested).RootScope, Is.SameAs(((IContextScope)root).RootScope));
				Assert.That(nested.Transaction, Is.SameAs(transaction));
				nested.BeginTransaction();
				nested.Commit();
			}
			root.Commit();
		}
		Assert.That(commits, Is.EqualTo(1));
		Assert.That(closes, Is.EqualTo(1));
		Assert.That(CallContext.LogicalGetData(contextID), Is.Null);
	}

	[Test]
	public void TransactionalDecoratorPreservesItsProvidersTransactionalRoot() {
		using var root = new DirectTransactionalScope();
		using var remappedRoot = new DirectTransactionalScope();
		var provider = new RemappedTransactionalScope(root, remappedRoot.RootScope);
		var transactionalDecorator = new TransactionalScopeDecorator<RemappedTransactionalScope>(provider);

		Assert.That(transactionalDecorator.RootScope, Is.SameAs(root.RootScope));
		Assert.That(((ITransactionalScope)transactionalDecorator).RootScope, Is.SameAs(root.RootScope));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AsyncSurfaceOfActionTransactionPreservesOperationAndEventOrder(bool rollback) {
		var calls = new List<string>();
		void Record(string name) {
			calls.Add(name);
		}
		var contextID = Guid.NewGuid().ToString();
		var scope = new ActionTransactionalScope<object>(ContextScopePolicy.MustBeRoot, contextID,
			() => { Record("begin"); return new object(); }, _ => Record("commit"), _ => Record("rollback"), _ => Record("close"));
		scope.Committing += () => Record("committing");
		scope.Committed += () => Record("committed");
		scope.RollingBack += () => Record("rolling-back");
		scope.RolledBack += () => Record("rolled-back");
		await scope.BeginTransactionAsync();
		if (rollback)
			await scope.RollbackAsync();
		else
			await scope.CommitAsync();
		await scope.DisposeAsync();
		Assert.That(calls, Is.EqualTo(rollback
			? new[] { "begin", "rolling-back", "rollback", "rolled-back", "close" }
			: new[] { "begin", "committing", "commit", "committed", "close" }));
		Assert.That(CallContext.LogicalGetData(contextID), Is.Null);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SynchronousTaskTransactionAdapterPropagatesBackendFailure(bool rollback) {
		using var scope = new TaskTransactionalScope<object>(ContextScopePolicy.MustBeRoot, Guid.NewGuid().ToString(),
			() => Task.FromResult(new object()), _ => Task.FromException(new TransactionTestException()),
			_ => Task.FromException(new TransactionTestException()), _ => Task.CompletedTask);
		scope.BeginTransaction();
		if (rollback)
			Assert.That(scope.Rollback, Throws.TypeOf<TransactionTestException>());
		else
			Assert.That(scope.Commit, Throws.TypeOf<TransactionTestException>());
	}

	[Test]
	public async Task TaskTransactionDisposalAwaitsItsCloseAndRestoresCallerContext() {
		var contextID = Guid.NewGuid().ToString();
		var closes = 0;
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var scope = new TaskTransactionalScope<object>(ContextScopePolicy.MustBeRoot, contextID,
			() => Task.FromResult(new object()), _ => Task.CompletedTask, _ => Task.CompletedTask,
			_ => { closes++; return release.Task; });
		await scope.BeginTransactionAsync();
		var completion = scope.DisposeAsync();
		Assert.That(closes, Is.EqualTo(1));
		Assert.That(completion.IsCompleted, Is.False);
		Assert.That(CallContext.LogicalGetData(contextID), Is.Null);
		release.SetResult();
		await completion.AsTask().WaitAsync(TimeSpan.FromSeconds(10));
		Assert.That(scope.Transaction, Is.Null);
	}

	[TestCase("begin", false)]
	[TestCase("begin", true)]
	[TestCase("close", false)]
	[TestCase("close", true)]
	[TestCase("commit", false)]
	[TestCase("commit", true)]
	[TestCase("rollback", false)]
	[TestCase("rollback", true)]
	[TestCase("context-end", false)]
	[TestCase("context-end", true)]
	[TestCase("transaction-end", false)]
	[TestCase("transaction-end", true)]
	[TestCase("committing", false)]
	[TestCase("committing", true)]
	[TestCase("committed", false)]
	[TestCase("committed", true)]
	[TestCase("rolling-back", false)]
	[TestCase("rolling-back", true)]
	[TestCase("rolled-back", false)]
	[TestCase("rolled-back", true)]
	[NonParallelizable]
	public async Task SynchronousTransactionAsyncBridgesReturnBeforeCallbacksComplete(string callback, bool fail) {
		using var release = new ManualResetEventSlim();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var scope = new ObservedSyncTransactionalScope(name => {
			if (name != callback)
				return;
			entered.TrySetResult();
			if (!release.Wait(TimeSpan.FromSeconds(10)))
				throw new TimeoutException("The asynchronous callback was not released.");
			if (fail)
				throw new TransactionTestException();
		});
		var initiating = Task.Factory.StartNew(() => scope.InvokeAsync(callback), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		try {
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
			var completion = await initiating.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.That(completion.IsCompleted, Is.False);
			release.Set();
			if (fail)
				await Assert.ThatAsync(async () => await completion, Throws.TypeOf<TransactionTestException>());
			else
				await completion.WaitAsync(TimeSpan.FromSeconds(10));
		} finally {
			release.Set();
			try {
				await initiating.Unwrap().WaitAsync(TimeSpan.FromSeconds(10));
			} catch (TransactionTestException) {
			}
			CallContext.LogicalSetData(scope.ContextID, null);
		}
	}

	private sealed class ObservedSyncTransactionalScope : SyncTransactionalScope<object> {
		private readonly Action<string> _observe;

		public ObservedSyncTransactionalScope(Action<string> observe) : base(ContextScopePolicy.MustBeRoot, Guid.NewGuid().ToString()) => _observe = observe;

		public Task InvokeAsync(string callback) => callback switch {
			"begin" => BeginTransactionInternalAsync(),
			"close" => CloseTransactionInternalAsync(new object()),
			"commit" => CommitInternalAsync(new object()),
			"rollback" => RollbackInternalAsync(new object()),
			"context-end" => OnContextEndAsync().AsTask(),
			"transaction-end" => OnTransactionalScopeEndAsync(new List<Exception>()),
			"committing" => OnCommittingAsync(),
			"committed" => OnCommittedAsync(),
			"rolling-back" => OnRollingBackAsync(),
			"rolled-back" => OnRolledBackAsync(),
			_ => throw new ArgumentOutOfRangeException(nameof(callback))
		};

		protected override object BeginTransactionInternal() { _observe("begin"); return new object(); }
		protected override void CloseTransactionInternal(object transaction) => _observe("close");
		protected override void CommitInternal(object transaction) => _observe("commit");
		protected override void RollbackInternal(object transaction) => _observe("rollback");
		protected override void OnContextEnd() => _observe("context-end");
		protected override void OnTransactionalScopeEnd(List<Exception> errors) => _observe("transaction-end");
		protected override void OnCommitting() => _observe("committing");
		protected override void OnCommitted() => _observe("committed");
		protected override void OnRollingBack() => _observe("rolling-back");
		protected override void OnRolledBack() => _observe("rolled-back");
	}

	private sealed class TransactionTestException : Exception {
	}

	private readonly struct RemappedTransactionalScope : ITransactionalScope {
		private readonly DirectTransactionalScope _inner;
		private readonly IContextScope _contextRoot;

		public event EventHandlerEx Committing { add { } remove { } }
		public event EventHandlerEx Committed { add { } remove { } }
		public event EventHandlerEx RollingBack { add { } remove { } }
		public event EventHandlerEx RolledBack { add { } remove { } }

		public RemappedTransactionalScope(DirectTransactionalScope inner, IContextScope contextRoot) {
			_inner = inner;
			_contextRoot = contextRoot;
		}

		public string ContextID => _inner.ContextID;
		public ContextScopePolicy Policy => _inner.Policy;
		public IContextScope RootScope => _contextRoot;
		ITransactionalScope ITransactionalScope.RootScope => _inner.RootScope as ITransactionalScope;
		public void BeginTransaction() => _inner.BeginTransaction();
		public Task BeginTransactionAsync() => _inner.BeginTransactionAsync();
		public void Commit() => _inner.Commit();
		public Task CommitAsync() => _inner.CommitAsync();
		public void Rollback() => _inner.Rollback();
		public Task RollbackAsync() => _inner.RollbackAsync();
		public void Dispose() => _inner.Dispose();
		public ValueTask DisposeAsync() => _inner.DisposeAsync();
	}

	private sealed class DirectTransactionalScope : ITransactionalScope {
		public event EventHandlerEx Committing { add { } remove { } }
		public event EventHandlerEx Committed { add { } remove { } }
		public event EventHandlerEx RollingBack { add { } remove { } }
		public event EventHandlerEx RolledBack { add { } remove { } }

		public DirectTransactionalScope() => RootScope = this;

		public string ContextID => "direct";

		public ContextScopePolicy Policy => ContextScopePolicy.None;

		public IContextScope RootScope { get; set; }

		public void BeginTransaction() { }

		public Task BeginTransactionAsync() => Task.CompletedTask;

		public void Commit() { }

		public Task CommitAsync() => Task.CompletedTask;

		public void Rollback() { }

		public Task RollbackAsync() => Task.CompletedTask;
		public void Dispose() { }
		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
}
