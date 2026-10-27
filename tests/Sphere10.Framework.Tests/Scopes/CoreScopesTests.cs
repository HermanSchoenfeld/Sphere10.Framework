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
public class CoreScopesTests {

	[Test]
	public async Task NoOpScopesHaveNoFinalizer() {
		var noOp = default(NoOpScope);
		noOp.Dispose();
		await noOp.DisposeAsync();
		var itemScope = default(NoOpScope<int>);
		itemScope.Dispose();
		await itemScope.DisposeAsync();
		Assert.That(itemScope.Item, Is.Zero);
	}

	[Test]
	public async Task ItemScopeDoesNotDisposeItsBorrowedItem() {
		var item = new DisposableItem();
		var scope = new NoOpScope<DisposableItem>(item);
		scope.Dispose();
		await scope.DisposeAsync();
		Assert.That(scope.Item, Is.SameAs(item));
		Assert.That(item.Calls, Is.Zero);
	}

	[Test]
	public void SynchronousDisposalRunsTheFinalizerOnTheCallingThread() {
		var calls = 0;
		var disposingThread = 0;
		var ownerThread = Environment.CurrentManagedThreadId;
		var scope = Tools.Scope.ExecuteOnDispose(() => {
			calls++;
			disposingThread = Environment.CurrentManagedThreadId;
		});
		scope.Dispose();
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(disposingThread, Is.EqualTo(ownerThread));
	}

	[Test]
	public async Task AsyncFinalizerRunsForEveryExplicitDisposal() {
		var calls = 0;
		var scope = Tools.Scope.ExecuteOnDisposeAsync(() => {
			calls++;
			return ValueTask.CompletedTask;
		});
		await scope.DisposeAsync();
		await scope.DisposeAsync();
		scope.Dispose();
		Assert.That(calls, Is.EqualTo(3));
	}

	[Test]
	public async Task GenericAsyncFinalizerRetainsItsPrefetchedItem() {
		var original = 7;
		var observed = 0;
		var scope = Tools.Scope.ExecuteOnDisposeAsync<int>(item => {
			observed += item;
			return ValueTask.CompletedTask;
		}, original);
		original = 9;
		Assert.That(scope.Item, Is.EqualTo(7));
		await scope.DisposeAsync();
		scope.Dispose();
		Assert.That(observed, Is.EqualTo(14));
		Assert.That(original, Is.EqualTo(9));
	}

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	[TestCase(3)]
	[TestCase(4)]
	[TestCase(5)]
	[TestCase(6)]
	[TestCase(7)]
	public void RequiredCallbacksAreValidatedAtConstruction(int kind) {
		Assert.That(() => {
			object scope = kind switch {
				0 => Tools.Scope.ExecuteOnDispose((Action)null),
				1 => Tools.Scope.ExecuteOnDisposeAsync((Func<ValueTask>)null),
				2 => Tools.Scope.ExecuteOnDispose<int>((Action<int>)null, 7),
				3 => Tools.Scope.ExecuteOnDisposeAsync<int>((Func<int, ValueTask>)null, 7),
				4 => new SyncScope(null),
				5 => new SyncScope<int>(7, null),
				6 => new AsyncScope(null),
				7 => new AsyncScope<int>(7, null),
				_ => throw new ArgumentOutOfRangeException(nameof(kind))
			};
			_ = scope;
		}, Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("finalizer"));
	}


	[Test]
	public void NullableCallbackDataIsPassedToTheFinalizer() {
		string observed = "unchanged";
		var scope = Tools.Scope.ExecuteOnDispose<string>(item => { observed = item; }, null);
		scope.Dispose();
		Assert.That(observed, Is.Null);
	}

	[Test]
	public async Task NullableAsyncCallbackDataIsPassedToTheFinalizer() {
		string observed = "unchanged";
		var scope = Tools.Scope.ExecuteOnDisposeAsync<string>(item => {
			observed = item;
			return ValueTask.CompletedTask;
		}, null);
		await scope.DisposeAsync();
		Assert.That(observed, Is.Null);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task TaskScopeReturnsFactoryFailuresAsFaultedOperations(bool withItem) {
		var calls = 0;
		var observed = 0;
		ValueTask Finalize() {
			calls++;
			throw new FinalizationException();
		}
		var simple = Tools.Scope.ExecuteOnDisposeAsync(Finalize);
		var generic = Tools.Scope.ExecuteOnDisposeAsync<int>(item => {
			observed = item;
			return Finalize();
		}, 7);
		var operation = ValueTask.CompletedTask;
		Assert.That(() => {
			operation = withItem ? generic.DisposeAsync() : simple.DisposeAsync();
		}, Throws.Nothing);
		await Assert.ThatAsync(async () => await operation, Throws.TypeOf<FinalizationException>());
		Assert.That(() => {
			if (withItem)
				generic.Dispose();
			else
				simple.Dispose();
		}, Throws.TypeOf<FinalizationException>());
		Assert.That(calls, Is.EqualTo(2));
		if (withItem)
			Assert.That(observed, Is.EqualTo(7));
	}

	[Test]
	public async Task FaultedOperationsPropagateThroughBothDisposalForms() {
		var calls = 0;
		var scope = Tools.Scope.ExecuteOnDisposeAsync(() => {
			calls++;
			return ValueTask.FromException(new FinalizationException());
		});
		var operation = ValueTask.CompletedTask;
		Assert.That(() => { operation = scope.DisposeAsync(); }, Throws.Nothing);
		await Assert.ThatAsync(async () => await operation, Throws.TypeOf<FinalizationException>());
		Assert.That(() => scope.Dispose(), Throws.TypeOf<FinalizationException>());
		Assert.That(calls, Is.EqualTo(2));
	}

	[Test]
	public async Task CancellationPropagatesThroughBothDisposalForms() {
		var scope = Tools.Scope.ExecuteOnDisposeAsync(() => ValueTask.FromCanceled(new CancellationToken(true)));
		await Assert.ThatAsync(async () => await scope.DisposeAsync(), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(() => scope.Dispose(), Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	public async Task AwaitUsingWaitsForTheFinalizerAfterTheBody() {
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var bodyRan = false;
		var finalized = false;
		var calls = 0;
		async Task Operation() {
			await using var scope = Tools.Scope.ExecuteOnDisposeAsync(async () => {
				calls++;
				await release.Task;
				finalized = true;
			});
			bodyRan = true;
		}
		var operation = Operation();
		try {
			Assert.That(bodyRan, Is.True);
			Assert.That(calls, Is.EqualTo(1));
			Assert.That(finalized, Is.False);
			Assert.That(operation.IsCompleted, Is.False);
		} finally {
			release.TrySetResult();
		}
		await operation.WaitAsync(TimeSpan.FromSeconds(10));
		Assert.That(finalized, Is.True);
	}

	[Test]
	public async Task AwaitUsingFinalizesWhenTheBodyThrows() {
		var calls = 0;
		async Task Operation() {
			await using var scope = Tools.Scope.ExecuteOnDisposeAsync(() => {
				calls++;
				return ValueTask.CompletedTask;
			});
			await Task.Yield();
			throw new FinalizationException();
		}
		await Assert.ThatAsync(Operation, Throws.TypeOf<FinalizationException>());
		Assert.That(calls, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task CompletedValueTaskSourcesAreConsumedOncePerExplicitCall(bool synchronous) {
		var first = new FinalizationSource();
		var second = new FinalizationSource();
		first.Complete();
		second.Complete();
		var calls = 0;
		var scope = Tools.Scope.ExecuteOnDisposeAsync(() => ++calls == 1 ? first.Operation : second.Operation);
		if (synchronous) {
			scope.Dispose();
			scope.Dispose();
		} else {
			await scope.DisposeAsync();
			await scope.DisposeAsync();
		}
		Assert.That(calls, Is.EqualTo(2));
		Assert.That(first.ResultsConsumed, Is.EqualTo(1));
		Assert.That(second.ResultsConsumed, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AwaitedPendingValueTaskSourceIsConsumedOnce(bool asyncOnly) {
		var source = new FinalizationSource();
		IScope<FinalizationSource> scope = asyncOnly
			? new AsyncScope<FinalizationSource>(source, static item => item.Operation)
			: Tools.Scope.ExecuteOnDisposeAsync<FinalizationSource>(static item => item.Operation, source);
		var operation = scope.DisposeAsync();
		Assert.That(operation.IsCompleted, Is.False);
		Assert.That(scope.Item, Is.SameAs(source));
		source.Complete();
		await operation.AsTask().WaitAsync(TimeSpan.FromSeconds(10));
		Assert.That(source.ResultsConsumed, Is.EqualTo(1));
	}

	[Test]
	public void SynchronousDisposalWaitsForTaskAndValueTaskSourceCompletion([Values] bool sourceBacked, [Values] bool withItem) {
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var source = new FinalizationSource();
		using var started = new ManualResetEventSlim();
		using var finished = new ManualResetEventSlim();
		Exception failure = null;
		var scope = Tools.Scope.ExecuteOnDisposeAsync(() => {
			started.Set();
			return sourceBacked ? source.Operation : new ValueTask(release.Task);
		});
		var genericScope = Tools.Scope.ExecuteOnDisposeAsync<FinalizationSource>(item => {
			started.Set();
			return sourceBacked ? item.Operation : new ValueTask(release.Task);
		}, source);
		var thread = new Thread(() => {
			try {
				if (withItem)
					genericScope.Dispose();
				else
					scope.Dispose();
			} catch (Exception error) {
				failure = error;
			} finally {
				finished.Set();
			}
		});
		thread.Start();
		try {
			Assert.That(started.Wait(TimeSpan.FromSeconds(10)), Is.True);
			if (sourceBacked)
				Assert.That(SpinWait.SpinUntil(() => source.ContinuationRegistered || finished.IsSet, TimeSpan.FromSeconds(10)) && source.ContinuationRegistered, Is.True);
			Assert.That(finished.IsSet, Is.False, "Dispose must wait for finalization to complete.");
		} finally {
			if (sourceBacked)
				source.Complete();
			else
				release.TrySetResult();
			Assert.That(thread.Join(TimeSpan.FromSeconds(10)), Is.True);
		}
		Assert.That(failure, Is.Null);
		Assert.That(finished.IsSet, Is.True);
		Assert.That(source.ResultsConsumed, Is.EqualTo(sourceBacked ? 1 : 0));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void TaskScopeSynchronousBridgeStartsOutsideTheCallerContext(bool withItem) {
		var previousContext = SynchronizationContext.Current;
		var context = new RecordingSynchronizationContext();
		SynchronizationContext callbackContext = context;
		var calls = 0;
		var observed = 0;
		async ValueTask Finalize() {
			callbackContext = SynchronizationContext.Current;
			await Task.Yield();
			calls++;
		}
		try {
			SynchronizationContext.SetSynchronizationContext(context);
			if (withItem) {
				var scope = Tools.Scope.ExecuteOnDisposeAsync<int>(async item => {
					observed = item;
					await Finalize();
				}, 7);
				scope.Dispose();
			} else {
				var scope = Tools.Scope.ExecuteOnDisposeAsync(Finalize);
				scope.Dispose();
			}
			Assert.That(callbackContext, Is.Null);
			Assert.That(SynchronizationContext.Current, Is.SameAs(context));
			Assert.That(context.Posts, Is.Zero);
			Assert.That(calls, Is.EqualTo(1));
			if (withItem)
				Assert.That(observed, Is.EqualTo(7));
		} finally {
			SynchronizationContext.SetSynchronizationContext(previousContext);
		}
	}

	[Test]
	public async Task TaskScopeUsesTheOriginalItemAndExplicitTaskAdapter() {
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var scope = Tools.Scope.ExecuteOnDisposeAsync<TaskCompletionSource>(static item => new ValueTask(item.Task), release);
		Assert.That(scope.Item, Is.SameAs(release));
		var operation = scope.DisposeAsync();
		Assert.That(operation.IsCompleted, Is.False);
		release.SetResult();
		await operation.AsTask().WaitAsync(TimeSpan.FromSeconds(10));
	}

	private sealed class DisposableItem : IDisposable {
		public int Calls { get; private set; }
		public void Dispose() => Calls++;
	}

	private sealed class FinalizationSource : IValueTaskSource {
		private ManualResetValueTaskSourceCore<bool> _source = new() { RunContinuationsAsynchronously = true };
		private int _continuationRegistered;

		public int ResultsConsumed { get; private set; }
		public bool ContinuationRegistered => Volatile.Read(ref _continuationRegistered) != 0;
		public ValueTask Operation => new(this, _source.Version);
		public void Complete() => _source.SetResult(true);
		public ValueTaskSourceStatus GetStatus(short token) => _source.GetStatus(token);

		public void OnCompleted(Action<object> continuation, object item, short token, ValueTaskSourceOnCompletedFlags flags) {
			_source.OnCompleted(continuation, item, token, flags);
			Volatile.Write(ref _continuationRegistered, 1);
		}

		public void GetResult(short token) {
			ResultsConsumed++;
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
