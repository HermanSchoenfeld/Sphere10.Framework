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
public class DirectionalCallbackScopeTests {

	[TestCase(false)]
	[TestCase(true)]
	public async Task SynchronousScopesRejectAsyncDisposalBeforeInvokingTheirFinalizer(bool withItem) {
		var calls = 0;
		var observed = 0;
		var finalizingThread = 0;
		void Finalize() {
			calls++;
			finalizingThread = Environment.CurrentManagedThreadId;
		}
		var simple = new SyncScope(Finalize);
		var generic = new SyncScope<int>(7, item => {
			observed = item;
			Finalize();
		});
		var operation = ValueTask.CompletedTask;
		Assert.That(() => {
			operation = withItem ? generic.DisposeAsync() : simple.DisposeAsync();
		}, Throws.Nothing);
		await Assert.ThatAsync(async () => await operation, Throws.TypeOf<NotSupportedException>());
		Assert.That(calls, Is.Zero);
		var ownerThread = Environment.CurrentManagedThreadId;
		if (withItem) {
			generic.Dispose();
			generic.Dispose();
			Assert.That(generic.Item, Is.EqualTo(7));
			Assert.That(observed, Is.EqualTo(7));
		} else {
			simple.Dispose();
			simple.Dispose();
		}
		Assert.That(calls, Is.EqualTo(2));
		Assert.That(finalizingThread, Is.EqualTo(ownerThread));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AsynchronousScopesRejectSyncDisposalAndAwaitActualFinalization(bool withItem) {
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var started = false;
		var calls = 0;
		var observed = 0;
		async ValueTask Finalize() {
			started = true;
			await release.Task;
			calls++;
		}
		var simple = new AsyncScope(Finalize);
		var generic = new AsyncScope<int>(7, item => {
			observed = item;
			return Finalize();
		});
		Assert.That(() => {
			if (withItem)
				generic.Dispose();
			else
				simple.Dispose();
		}, Throws.TypeOf<NotSupportedException>());
		Assert.That(started, Is.False);
		var operation = withItem ? generic.DisposeAsync() : simple.DisposeAsync();
		try {
			Assert.That(started, Is.True);
			Assert.That(operation.IsCompleted, Is.False);
			Assert.That(calls, Is.Zero);
			if (withItem) {
				Assert.That(generic.Item, Is.EqualTo(7));
				Assert.That(observed, Is.EqualTo(7));
			}
		} finally {
			release.TrySetResult();
			await operation.AsTask().WaitAsync(TimeSpan.FromSeconds(10));
		}
		Assert.That(calls, Is.EqualTo(1));
		await (withItem ? generic.DisposeAsync() : simple.DisposeAsync());
		Assert.That(calls, Is.EqualTo(2));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AsynchronousScopesReturnSynchronousCallbackFailuresThroughTheirOperation(bool withItem) {
		var calls = 0;
		var observed = 0;
		ValueTask Finalize() {
			calls++;
			throw new FinalizationException();
		}
		var simple = new AsyncScope(Finalize);
		var generic = new AsyncScope<int>(7, item => {
			observed = item;
			return Finalize();
		});
		var operation = ValueTask.CompletedTask;
		Assert.That(() => {
			operation = withItem ? generic.DisposeAsync() : simple.DisposeAsync();
		}, Throws.Nothing);
		await Assert.ThatAsync(async () => await operation, Throws.TypeOf<FinalizationException>());
		Assert.That(calls, Is.EqualTo(1));
		if (withItem)
			Assert.That(observed, Is.EqualTo(7));
	}

	[Test]
	public async Task AsynchronousScopePropagatesCancellation() {
		var scope = new AsyncScope(() => ValueTask.FromCanceled(new CancellationToken(true)));
		await Assert.ThatAsync(async () => await scope.DisposeAsync(), Throws.InstanceOf<OperationCanceledException>());
	}

	private sealed class FinalizationException : Exception {
	}
}
