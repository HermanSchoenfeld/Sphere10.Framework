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
public class ContextScopeLifecycleTests {
	[Test]
	public async Task CallContextStoresReferencesAcrossAwaitAndSupportsReplacementAndClear() {
		var contextID = Guid.NewGuid().ToString();
		var first = new object();
		var second = new object();
		Assert.That(CallContext.LogicalGetData(contextID), Is.Null);
		try {
			CallContext.LogicalSetData(contextID, first);
			await Task.Yield();
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(first));
			CallContext.LogicalSetData(contextID, second);
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(second));
			CallContext.LogicalSetData(contextID, null);
			Assert.That(CallContext.LogicalGetData(contextID), Is.Null);
		} finally {
			CallContext.LogicalSetData(contextID, null);
		}
	}

	[Test]
	public async Task AsyncChildMethodCannotReplaceItsCallersAmbientData() {
		var contextID = Guid.NewGuid().ToString();
		var previous = new object();
		var child = new object();
		CallContext.LogicalSetData(contextID, previous);
		try {
			async Task SetChildData() {
				Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
				CallContext.LogicalSetData(contextID, child);
				await Task.Yield();
				Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(child));
			}
			await SetChildData();
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
		} finally {
			CallContext.LogicalSetData(contextID, null);
		}
	}

	[Test]
	public void ContextNamesHaveIndependentAmbientRoots() {
		var firstID = Guid.NewGuid().ToString();
		var secondID = Guid.NewGuid().ToString();
		using (IContextScope first = new ActionContextScope(() => { }, ContextScopePolicy.MustBeRoot, firstID)) {
			using (IContextScope second = new ActionContextScope(() => { }, ContextScopePolicy.MustBeRoot, secondID)) {
				Assert.That(first.RootScope, Is.Not.SameAs(second.RootScope));
				Assert.That(CallContext.LogicalGetData(firstID), Is.SameAs(first.RootScope));
				Assert.That(CallContext.LogicalGetData(secondID), Is.SameAs(second.RootScope));
			}
			Assert.That(CallContext.LogicalGetData(secondID), Is.Null);
			Assert.That(CallContext.LogicalGetData(firstID), Is.SameAs(first.RootScope));
		}
		Assert.That(CallContext.LogicalGetData(firstID), Is.Null);
	}

	[TestCase(ContextScopePolicy.None)]
	[TestCase(ContextScopePolicy.MustBeRoot)]
	public void RootRestoresPreviousAmbientValue(ContextScopePolicy policy) {
		var contextID = Guid.NewGuid().ToString();
		var previous = new object();
		var finalized = 0;
		CallContext.LogicalSetData(contextID, previous);
		try {
			using (IContextScope root = new ActionContextScope(() => finalized++, policy, contextID)) {
				Assert.That(root.ContextID, Is.EqualTo(contextID));
				Assert.That(root.Policy, Is.EqualTo(policy));
				Assert.That(root.RootScope, Is.SameAs(CallContext.LogicalGetData(contextID)));
				Assert.That(root.RootScope.RootScope, Is.SameAs(root.RootScope));
			}
			Assert.That(finalized, Is.EqualTo(1));
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
		} finally {
			CallContext.LogicalSetData(contextID, null);
		}
	}

	[Test]
	public async Task MixedDisposalFinalizesClassOwnedContextsOnceAndPreservesANewerRoot([Values] bool taskBacked, [Values] bool asyncFirst) {
		var contextID = Guid.NewGuid().ToString();
		var previous = new object();
		var scopeEnds = 0;
		var contextEnds = 0;
		CallContext.LogicalSetData(contextID, previous);
		try {
			IContextScope root = CreateContextScope(taskBacked, contextID, () => {
				Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
				contextEnds++;
			}, () => {
				Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
				scopeEnds++;
			});
			var owner = (Disposable)root;
			if (asyncFirst)
				await owner.DisposeAsync();
			else
				owner.Dispose();
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));

			using (IContextScope replacement = new ActionContextScope(() => { }, ContextScopePolicy.MustBeRoot, contextID)) {
				root.Dispose();
				await root.DisposeAsync();
				owner.Dispose();
				await owner.DisposeAsync();
				Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(replacement));
				Assert.That(scopeEnds, Is.EqualTo(1));
				Assert.That(contextEnds, Is.EqualTo(1));
			}
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
		} finally {
			CallContext.LogicalSetData(contextID, null);
		}
	}

	[Test]
	public async Task RepeatedNestedDisposalFinalizesOnlyItsOwnScopeOnce([Values] bool taskBacked) {
		var contextID = Guid.NewGuid().ToString();
		var nestedScopeEnds = 0;
		var nestedContextEnds = 0;
		using (IContextScope root = new ActionContextScope(() => { }, ContextScopePolicy.MustBeRoot, contextID)) {
			IContextScope nested = taskBacked
				? new TaskContextScope(() => { nestedContextEnds++; return Task.CompletedTask; }, () => { nestedScopeEnds++; return Task.CompletedTask; }, ContextScopePolicy.MustBeNested, contextID)
				: new ActionContextScope(() => nestedContextEnds++, () => nestedScopeEnds++, ContextScopePolicy.MustBeNested, contextID);
			await nested.DisposeAsync();
			nested.Dispose();
			await nested.DisposeAsync();
			Assert.That(nestedScopeEnds, Is.EqualTo(1));
			Assert.That(nestedContextEnds, Is.Zero);
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(root));
		}
		Assert.That(CallContext.LogicalGetData(contextID), Is.Null);
	}

	[Test]
	public async Task FailedContextCleanupCanBeRetriedWithoutReplacingANewerRoot([Values] bool taskBacked, [Values] bool asyncFirst) {
		var contextID = Guid.NewGuid().ToString();
		var previous = new object();
		var scopeEnds = 0;
		var contextEnds = 0;
		CallContext.LogicalSetData(contextID, previous);
		try {
			var root = CreateContextScope(taskBacked, contextID, () => contextEnds++, () => {
				if (++scopeEnds == 1)
					throw new InvalidOperationException("context cleanup failed");
			});
			if (asyncFirst) {
				// Restore this caller's ambient value before the async assertion enters its child execution context.
				var operation = root.DisposeAsync();
				await Assert.ThatAsync(async () => await operation, Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("context cleanup failed"));
			} else
				Assert.That(root.Dispose, Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("context cleanup failed"));
			Assert.That(contextEnds, Is.Zero);
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));

			using (IContextScope replacement = new ActionContextScope(() => { }, ContextScopePolicy.MustBeRoot, contextID)) {
				if (asyncFirst)
					root.Dispose();
				else
					await root.DisposeAsync();
				root.Dispose();
				await root.DisposeAsync();
				Assert.That(scopeEnds, Is.EqualTo(2));
				Assert.That(contextEnds, Is.EqualTo(1));
				Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(replacement));
			}
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
		} finally {
			CallContext.LogicalSetData(contextID, null);
		}
	}

	[TestCase(ContextScopePolicy.None)]
	[TestCase(ContextScopePolicy.MustBeNested)]
	public void NestedScopesRetainRootAndFinalizeOnlyTheirOwnScope(ContextScopePolicy nestedPolicy) {
		var contextID = Guid.NewGuid().ToString();
		var ended = new List<string>();
		using (IContextScope root = new ActionContextScope(() => ended.Add("root context"), () => ended.Add("root scope"), ContextScopePolicy.MustBeRoot, contextID)) {
			using (IContextScope nested = new ActionContextScope(() => ended.Add("nested context"), () => ended.Add("nested scope"), nestedPolicy, contextID)) {
				Assert.That(nested.Policy, Is.EqualTo(nestedPolicy));
				Assert.That(nested.RootScope, Is.SameAs(root.RootScope));
				Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(root.RootScope));
			}
			Assert.That(ended, Is.EqualTo(new[] { "nested scope" }));
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(root.RootScope));
		}
		Assert.That(ended, Is.EqualTo(new[] { "nested scope", "root scope", "root context" }));
		Assert.That(CallContext.LogicalGetData(contextID), Is.Null);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void NestedScopesPreserveTheStoredCustomContextIdentity(bool taskBacked) {
		var contextID = Guid.NewGuid().ToString();
		IContextScope custom = new CustomContextScope(contextID);
		var scopeEnds = 0;
		var contextEnds = 0;
		CallContext.LogicalSetData(contextID, custom);
		try {
			// The stored provider itself is the root, even when its own RootScope projection is null.
			Assert.That(custom.RootScope, Is.Null);
			IContextScope nested;
			if (taskBacked) {
				var handle = new TaskContextScope(() => { contextEnds++; return Task.CompletedTask; }, () => { scopeEnds++; return Task.CompletedTask; }, ContextScopePolicy.MustBeNested, contextID);
				Assert.That(handle.IsRootScope, Is.False);
				Assert.That(handle.RootScope, Is.SameAs(custom));
				nested = handle;
			} else {
				var handle = new ActionContextScope(() => contextEnds++, () => scopeEnds++, ContextScopePolicy.MustBeNested, contextID);
				Assert.That(handle.IsRootScope, Is.False);
				Assert.That(handle.RootScope, Is.SameAs(custom));
				nested = handle;
			}
			using (nested) {
				Assert.That(nested.RootScope, Is.SameAs(custom));
				Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(custom));
			}
			Assert.That(scopeEnds, Is.EqualTo(1));
			Assert.That(contextEnds, Is.Zero);
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(custom));
		} finally {
			CallContext.LogicalSetData(contextID, null);
		}
	}

	[Test]
	public void RootAndNestedPolicyFailuresPreserveAmbientState() {
		var contextID = Guid.NewGuid().ToString();
		var previous = new object();
		CallContext.LogicalSetData(contextID, previous);
		try {
			Assert.That(() => new ActionContextScope(() => { }, ContextScopePolicy.MustBeNested, contextID), Throws.TypeOf<SoftwareException>());
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
			using (IContextScope root = new ActionContextScope(() => { }, ContextScopePolicy.MustBeRoot, contextID)) {
				Assert.That(() => new TaskContextScope(() => Task.CompletedTask, ContextScopePolicy.MustBeRoot, contextID), Throws.TypeOf<SoftwareException>());
				Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(root.RootScope));
			}
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
		} finally {
			CallContext.LogicalSetData(contextID, null);
		}
	}

	[Test]
	public void FailedContextStartRestoresPreviousAmbientValue() {
		var contextID = Guid.NewGuid().ToString();
		var previous = new object();
		CallContext.LogicalSetData(contextID, previous);
		try {
			Assert.That(() => new FailingStartScope(contextID), Throws.TypeOf<InvalidOperationException>());
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
		} finally {
			CallContext.LogicalSetData(contextID, null);
		}
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AwaitUsingRestoresCallerAndPermitsNextRoot(bool taskBacked) {
		var contextID = Guid.NewGuid().ToString();
		var previous = new object();
		var scopeEnds = 0;
		var contextEnds = 0;
		CallContext.LogicalSetData(contextID, previous);
		try {
			IContextScope root = taskBacked
				? new TaskContextScope(async () => { await Task.Yield(); contextEnds++; }, async () => { await Task.Yield(); scopeEnds++; }, ContextScopePolicy.MustBeRoot, contextID)
				: new ActionContextScope(() => contextEnds++, () => scopeEnds++, ContextScopePolicy.MustBeRoot, contextID);
			await using (root) {
				await Task.Yield();
				Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(root.RootScope));
				await using (IContextScope nested = new ActionContextScope(() => contextEnds++, () => scopeEnds++, ContextScopePolicy.MustBeNested, contextID)) {
					Assert.That(nested.RootScope, Is.SameAs(root.RootScope));
					await Task.Yield();
				}
				Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(root.RootScope));
			}
			Assert.That(scopeEnds, Is.EqualTo(2));
			Assert.That(contextEnds, Is.EqualTo(1));
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
			using (var next = new ActionContextScope(() => { }, ContextScopePolicy.MustBeRoot, contextID))
				Assert.That(next.IsRootScope, Is.True);
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
		} finally {
			CallContext.LogicalSetData(contextID, null);
		}
	}

	[Test]
	public async Task AsyncDisposalRestoresCallerBeforeReturningAndAwaitsFinalization() {
		var contextID = Guid.NewGuid().ToString();
		var previous = new object();
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var completed = false;
		CallContext.LogicalSetData(contextID, previous);
		var scope = new TaskContextScope(async () => { await release.Task; completed = true; }, ContextScopePolicy.MustBeRoot, contextID);
		var pending = scope.DisposeAsync().AsTask();
		try {
			Assert.That(pending.IsCompleted, Is.False);
			Assert.That(completed, Is.False);
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
			release.SetResult();
			await pending.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.That(completed, Is.True);
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
		} finally {
			release.TrySetResult();
			await pending;
			CallContext.LogicalSetData(contextID, null);
		}
	}

	[Test]
	public async Task SynchronousTaskContextDisposalWaitsForPendingCleanup() {
		var contextID = Guid.NewGuid().ToString();
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var completed = false;
		var disposing = Task.Run(() => {
			var scope = new TaskContextScope(async () => { started.SetResult(); await release.Task; completed = true; }, ContextScopePolicy.MustBeRoot, contextID);
			scope.Dispose();
			Assert.That(completed, Is.True);
			Assert.That(CallContext.LogicalGetData(contextID), Is.Null);
		});
		try {
			await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.That(disposing.IsCompleted, Is.False);
			release.SetResult();
			await disposing.WaitAsync(TimeSpan.FromSeconds(10));
		} finally {
			release.TrySetResult();
		}
		Assert.That(CallContext.LogicalGetData(contextID), Is.Null);
	}

	[Test]
	public async Task AsyncCleanupFailurePreservesTheRestoredAmbientValueAndANewerRoot() {
		var contextID = Guid.NewGuid().ToString();
		var previous = new object();
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		CallContext.LogicalSetData(contextID, previous);
		var scope = new TaskContextScope(() => release.Task, ContextScopePolicy.MustBeRoot, contextID);
		try {
			var pending = scope.DisposeAsync().AsTask();
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
			using (IContextScope next = new ActionContextScope(() => { }, ContextScopePolicy.MustBeRoot, contextID)) {
				release.SetException(new InvalidOperationException("cleanup failed"));
				await Assert.ThatAsync(async () => await pending, Throws.TypeOf<InvalidOperationException>());
				Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(next.RootScope));
			}
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
		} finally {
			release.TrySetResult();
			CallContext.LogicalSetData(contextID, null);
		}
	}

	[Test]
	public void SynchronousScopeCleanupFailurePropagatesBeforeContextFinalization() {
		var contextID = Guid.NewGuid().ToString();
		var previous = new object();
		var contextEnds = 0;
		CallContext.LogicalSetData(contextID, previous);
		var scope = new ActionContextScope(() => contextEnds++, () => throw new InvalidOperationException("cleanup failed"), ContextScopePolicy.MustBeRoot, contextID);
		try {
			Assert.That(scope.Dispose, Throws.TypeOf<InvalidOperationException>());
			Assert.That(contextEnds, Is.Zero);
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
		} finally {
			CallContext.LogicalSetData(contextID, null);
		}
	}

	[Test]
	public async Task ChildTasksInheritRootWhileAmbientWritesRemainIsolated() {
		var contextID = Guid.NewGuid().ToString();
		var dataID = Guid.NewGuid().ToString();
		var previous = new object();
		var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var arrivals = 0;
		var nestedEnds = 0;
		var rootEnds = 0;
		CallContext.LogicalSetData(dataID, previous);
		try {
			using (IContextScope root = new ActionContextScope(() => rootEnds++, ContextScopePolicy.MustBeRoot, contextID)) {
				async Task Child(object value) {
					Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(root.RootScope));
					await using (IContextScope nested = new ActionContextScope(() => { }, () => Interlocked.Increment(ref nestedEnds), ContextScopePolicy.MustBeNested, contextID)) {
						CallContext.LogicalSetData(dataID, value);
						if (Interlocked.Increment(ref arrivals) == 2) ready.SetResult();
						await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
						Assert.That(nested.RootScope, Is.SameAs(root.RootScope));
						Assert.That(CallContext.LogicalGetData(dataID), Is.SameAs(value));
					}
					Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(root.RootScope));
				}
				await Task.WhenAll(Task.Run(() => Child(new object())), Task.Run(() => Child(new object())));
				Assert.That(nestedEnds, Is.EqualTo(2));
				Assert.That(rootEnds, Is.Zero);
				Assert.That(CallContext.LogicalGetData(dataID), Is.SameAs(previous));
				Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(root.RootScope));
			}
			Assert.That(rootEnds, Is.EqualTo(1));
			Assert.That(CallContext.LogicalGetData(contextID), Is.Null);
		} finally {
			CallContext.LogicalSetData(dataID, null);
		}
	}

	[Test]
	public async Task ParallelChildRootsWithSameNameDoNotReplaceEachOtherOrParentData() {
		var contextID = Guid.NewGuid().ToString();
		var previous = new object();
		var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var arrivals = 0;
		CallContext.LogicalSetData(contextID, previous);
		try {
			async Task<IContextScope> Child() {
				IContextScope state;
				await using (IContextScope root = new TaskContextScope(async () => await Task.Yield(), ContextScopePolicy.MustBeRoot, contextID)) {
					state = root.RootScope;
					if (Interlocked.Increment(ref arrivals) == 2) ready.SetResult();
					await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
					Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(state));
				}
				Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
				return state;
			}
			var states = await Task.WhenAll(Task.Run(Child), Task.Run(Child));
			Assert.That(states[0], Is.Not.SameAs(states[1]));
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
		} finally {
			CallContext.LogicalSetData(contextID, null);
		}
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ContextInterfacePreservesContextIdentityAndCleanup(bool taskBacked) {
		var contextID = Guid.NewGuid().ToString();
		var ended = 0;
		IContextScope scope = taskBacked
			? new TaskContextScope(async () => { await Task.Yield(); ended++; }, ContextScopePolicy.MustBeRoot, contextID)
			: new ActionContextScope(() => ended++, ContextScopePolicy.MustBeRoot, contextID);
		Assert.That(scope.ContextID, Is.EqualTo(contextID));
		Assert.That(scope.Policy, Is.EqualTo(ContextScopePolicy.MustBeRoot));
		Assert.That(scope.RootScope, Is.SameAs(CallContext.LogicalGetData(contextID)));
		await scope.DisposeAsync();
		Assert.That(ended, Is.EqualTo(1));
		Assert.That(CallContext.LogicalGetData(contextID), Is.Null);
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	[NonParallelizable]
	public async Task ActionContextAsyncDisposalReturnsBeforeItsSynchronousCallbackCompletes(bool scopeFinalizer, bool fail) {
		using var release = new ManualResetEventSlim();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var contextID = Guid.NewGuid().ToString();
		var previous = new object();
		void Finalize() {
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
			entered.SetResult();
			if (!release.Wait(TimeSpan.FromSeconds(10)))
				throw new TimeoutException("The asynchronous callback was not released.");
			if (fail)
				throw new InvalidOperationException("context finalization failed");
		}
		var initiating = Task.Factory.StartNew(() => {
			CallContext.LogicalSetData(contextID, previous);
			var scope = new ActionContextScope(scopeFinalizer ? null : Finalize, scopeFinalizer ? Finalize : null, ContextScopePolicy.MustBeRoot, contextID);
			var completion = scope.DisposeAsync().AsTask();
			Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(previous));
			return completion;
		}, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		try {
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
			var completion = await initiating.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.That(completion.IsCompleted, Is.False);
			release.Set();
			if (fail)
				await Assert.ThatAsync(async () => await completion, Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("context finalization failed"));
			else
				await completion.WaitAsync(TimeSpan.FromSeconds(10));
		} finally {
			release.Set();
			try {
				await initiating.Unwrap().WaitAsync(TimeSpan.FromSeconds(10));
			} catch (InvalidOperationException) when (fail) {
			}
		}
	}

	private static IContextScope CreateContextScope(bool taskBacked, string contextID, Action contextFinalizer, Action scopeFinalizer)
		=> taskBacked
			? new TaskContextScope(async () => { contextFinalizer(); await Task.Yield(); }, async () => { scopeFinalizer(); await Task.Yield(); }, ContextScopePolicy.MustBeRoot, contextID)
			: new ActionContextScope(contextFinalizer, scopeFinalizer, ContextScopePolicy.MustBeRoot, contextID);

	private sealed class CustomContextScope : IContextScope {
		public CustomContextScope(string contextID) => ContextID = contextID;
		public string ContextID { get; }
		public ContextScopePolicy Policy => ContextScopePolicy.None;
		public IContextScope RootScope => null;
		public void Dispose() { }
		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	private sealed class FailingStartScope : ContextScope {
		public FailingStartScope(string contextID) : base(ContextScopePolicy.MustBeRoot, contextID) { }
		protected override void OnContextStart() => throw new InvalidOperationException("context start failed");
		protected override ValueTask OnContextEndAsync() => ValueTask.CompletedTask;
		protected override void OnContextEnd() { }
	}
}
