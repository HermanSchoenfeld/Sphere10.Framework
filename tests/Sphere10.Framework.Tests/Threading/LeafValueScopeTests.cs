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
public class LeafValueScopeTests {

	[Test]
	public void SemaphoreScopeReleasesItsAcquiredPermit() {
		using var semaphore = new SemaphoreSlim(1, 1);
		var scope = semaphore.EnterWaitScope();
		Assert.That(semaphore.CurrentCount, Is.Zero);
		scope.Dispose();
		Assert.That(semaphore.CurrentCount, Is.EqualTo(1));
	}

	[Test]
	public void FastLockScopesRetainNestedAcquisitions() {
		var gate = new FastLock();
		using (gate.EnterLockScope()) {
			var inner = gate.EnterLockScope();
			inner.Dispose();
		}
		using var next = gate.EnterLockScope();
	}

	[Test]
	public void NonReentrantScopeRejectsReentryAndReleasesItsAcquisition() {
		var gate = new NonReentrantLock();
		var scope = gate.EnterLockScope();
		Assert.That(gate.IsLocked, Is.True);
		Assert.That(() => gate.EnterLockScope(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Reentry is not allowed."));
		scope.Dispose();
		Assert.That(gate.IsLocked, Is.False);
		using var next = gate.EnterLockScope();
		Assert.That(gate.IsLocked, Is.True);
	}

	[Test]
	public void NonReentrantScopeCanRetryOnTheOwnerThreadAfterWrongThreadDisposal() {
		var gate = new NonReentrantLock();
		var scope = gate.EnterLockScope();
		Exception error = null;
		var otherThread = new Thread(() => Tools.Exceptions.ExecuteCapturingException(() => scope.Dispose(), out error));
		otherThread.Start();
		Assert.That(otherThread.Join(TimeSpan.FromSeconds(3)), Is.True);
		Assert.That(error, Is.TypeOf<InvalidOperationException>());
		Assert.That(gate.IsLocked, Is.True);
		scope.Dispose();
		Assert.That(gate.IsLocked, Is.False);
	}

	[Test]
	public void FastLockScopeRejectsAsyncDisposalWithoutReleasingItsAcquisition() {
		var gate = new FastLock();
		var scope = gate.EnterLockScope();
		try {
			AssertAsyncDisposalRejected(scope);
			Assert.That(scope.Item.IsWriteLockHeld, Is.True);
		} finally {
			scope.Dispose();
		}
		Assert.That(scope.Item.IsWriteLockHeld, Is.False);
		using var next = gate.EnterLockScope();
		Assert.That(next.Item.IsWriteLockHeld, Is.True);
	}

	[Test]
	public void NonReentrantScopeRejectsAsyncDisposalWithoutReleasingItsAcquisition() {
		var gate = new NonReentrantLock();
		var scope = gate.EnterLockScope();
		try {
			AssertAsyncDisposalRejected(scope);
			Assert.That(gate.IsLocked, Is.True);
		} finally {
			scope.Dispose();
		}
		Assert.That(gate.IsLocked, Is.False);
		using var next = gate.EnterLockScope();
		Assert.That(gate.IsLocked, Is.True);
	}

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	public void ProducerConsumerScopesRejectAsyncDisposalWithoutReleasingTheirMonitor(int kind) {
		using var gate = new ProducerConsumerLock();
		using var semaphore = new SemaphoreSlim(1, 1);
		ProducerConsumerScope Acquire() => kind switch {
			0 => gate.ProducerBlockUntil(static () => true),
			1 => gate.ConsumerBlockUntil(static () => true),
			2 => gate.BlockUntil(semaphore, static () => true),
			_ => throw new ArgumentOutOfRangeException(nameof(kind))
		};
		var scope = Acquire();
		try {
			AssertAsyncDisposalRejected(scope);
			Assert.That(Monitor.IsEntered(gate.SharedLock), Is.True);
		} finally {
			scope.Dispose();
		}
		Assert.That(Monitor.IsEntered(gate.SharedLock), Is.False);
		using var next = Acquire();
		Assert.That(Monitor.IsEntered(gate.SharedLock), Is.True);
	}

	[Test]
	public void DisposedProducerConsumerLockUnwindsItsPendingAcquisition() {
		using var gate = new ProducerConsumerLock();
		using var semaphore = new SemaphoreSlim(1, 1);
		Assert.That(() => gate.BlockUntil(semaphore, () => {
			gate.Dispose();
			return false;
		}), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(Monitor.IsEntered(gate.SharedLock), Is.False);
		Assert.That(semaphore.CurrentCount, Is.EqualTo(1));
	}

	[Test]
	public void MonitorHelperScopesRetainRecursionAndRelease() {
		var sharedLock = new object();
		using (Tools.Threads.EnterLockScope(sharedLock)) {
			Assert.That(Monitor.IsEntered(sharedLock), Is.True);
			using (Tools.Threads.EnterLockScope(sharedLock))
				Assert.That(Monitor.IsEntered(sharedLock), Is.True);
			Assert.That(Monitor.IsEntered(sharedLock), Is.True);
		}
		Assert.That(Monitor.IsEntered(sharedLock), Is.False);
	}

	[Test]
	public void MonitorScopeRejectsAsyncDisposalWithoutReleasingItsAcquisition() {
		var sharedLock = new object();
		var scope = Tools.Threads.EnterLockScope(sharedLock);
		try {
			AssertAsyncDisposalRejected(scope);
			Assert.That(Monitor.IsEntered(sharedLock), Is.True);
		} finally {
			scope.Dispose();
		}
		Assert.That(Monitor.IsEntered(sharedLock), Is.False);
	}

	[Test]
	public void MonitorScopeCanRetryOnTheOwnerThreadAfterWrongThreadDisposal() {
		var sharedLock = new object();
		var scope = Tools.Threads.EnterLockScope(sharedLock);
		Exception error = null;
		var otherThread = new Thread(() => Tools.Exceptions.ExecuteCapturingException(scope.Dispose, out error));
		otherThread.Start();
		Assert.That(otherThread.Join(TimeSpan.FromSeconds(3)), Is.True);
		Assert.That(error, Is.TypeOf<SynchronizationLockException>());
		Assert.That(Monitor.IsEntered(sharedLock), Is.True);
		scope.Dispose();
		Assert.That(Monitor.IsEntered(sharedLock), Is.False);
	}

	[Test]
	public void ProducerConsumerConditionFailureReleasesOnlyItsOwnAcquisition() {
		using var gate = new ProducerConsumerLock();
		using var semaphore = new SemaphoreSlim(1, 1);
		using (Tools.Threads.EnterLockScope(gate.SharedLock)) {
			Assert.That(() => gate.BlockUntil(semaphore, () => throw new InvalidOperationException("condition failed")),
				Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("condition failed"));
			Assert.That(Monitor.IsEntered(gate.SharedLock), Is.True);
			Assert.That(semaphore.CurrentCount, Is.EqualTo(1));
		}
		Assert.That(Monitor.IsEntered(gate.SharedLock), Is.False);
		using var next = gate.BlockUntil(semaphore, static () => true);
		Assert.That(semaphore.CurrentCount, Is.Zero);
	}


	private static void AssertAsyncDisposalRejected<TScope>(TScope scope)
		where TScope : struct, IScope {
		var operation = ValueTask.CompletedTask;
		Assert.That(() => { operation = scope.DisposeAsync(); }, Throws.Nothing);
		Assert.That(operation.IsCompleted, Is.True);
		Assert.That(operation.IsCompletedSuccessfully, Is.False);
		Assert.That(() => operation.GetAwaiter().GetResult(), Throws.TypeOf<NotSupportedException>());
	}
}
