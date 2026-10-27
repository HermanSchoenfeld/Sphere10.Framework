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
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ScopeToolTests {

	[Test]
	public void SynchronousCallbackFactoryPreservesCapturedLocalBinding() {
		var original = new DisposableItem();
		var replacement = new DisposableItem();
		var current = original;
		using (ActionScope scope = Tools.Scope.ExecuteOnDispose(() => current.Dispose())) {
			current = replacement;
			Assert.That(original.Calls, Is.Zero);
			Assert.That(replacement.Calls, Is.Zero);
		}
		Assert.That(original.Calls, Is.Zero);
		Assert.That(replacement.Calls, Is.EqualTo(1));
	}

	[Test]
	public void SynchronousCallbackFactoryRetainsTheOriginalItemAndDefersCleanup() {
		var item = new DisposableItem();
		using (ActionScope<DisposableItem> scope = Tools.Scope.ExecuteOnDispose(static value => value.Dispose(), item)) {
			Assert.That(scope.Item, Is.SameAs(item));
			Assert.That(item.Calls, Is.Zero);
		}
		Assert.That(item.Calls, Is.EqualTo(1));
	}

	[Test]
	public void CallbackFactoriesRejectMissingFinalizers([Values] bool generic, [Values] bool asynchronous) {
		Assert.That(() => {
			if (asynchronous) {
				if (generic)
					_ = Tools.Scope.ExecuteOnDisposeAsync<DisposableItem>(null, new DisposableItem());
				else
					_ = Tools.Scope.ExecuteOnDisposeAsync((Func<ValueTask>)null);
			} else {
				if (generic)
					_ = Tools.Scope.ExecuteOnDispose<DisposableItem>(null, new DisposableItem());
				else
					_ = Tools.Scope.ExecuteOnDispose((Action)null);
			}
		}, Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("finalizer"));
	}

	[Test]
	public async Task CallbackFactoriesAllowNullItemsWhenTheCallbackAcceptsThem([Values] bool asynchronous) {
		var calls = 0;
		if (asynchronous) {
			TaskScope<string> scope = Tools.Scope.ExecuteOnDisposeAsync<string>(value => {
				Assert.That(value, Is.Null);
				calls++;
				return ValueTask.CompletedTask;
			}, null);
			Assert.That(scope.Item, Is.Null);
			Assert.That(calls, Is.Zero);
			await scope.DisposeAsync();
		} else {
			ActionScope<string> scope = Tools.Scope.ExecuteOnDispose<string>(value => {
				Assert.That(value, Is.Null);
				calls++;
			}, null);
			Assert.That(scope.Item, Is.Null);
			Assert.That(calls, Is.Zero);
			scope.Dispose();
		}
		Assert.That(calls, Is.EqualTo(1));
	}

	[Test]
	public void SynchronousCallbackFactoriesPropagateTheOriginalFailure([Values] bool generic) {
		var failure = new InvalidOperationException("Callback failure");
		if (generic) {
			ActionScope<InvalidOperationException> scope = Tools.Scope.ExecuteOnDispose(static error => throw error, failure);
			Assert.That(() => scope.Dispose(), Throws.Exception.SameAs(failure));
		} else {
			ActionScope scope = Tools.Scope.ExecuteOnDispose(() => throw failure);
			Assert.That(() => scope.Dispose(), Throws.Exception.SameAs(failure));
		}
	}

	[Test]
	public async Task AsyncCallbackFactoriesAwaitCompletionAndPropagateFailure([Values] bool generic, [Values] bool fail) {
		var item = new DisposableItem();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var failure = new IOException("Async callback failure");
		async ValueTask Complete(DisposableItem value) {
			entered.SetResult();
			await completion.Task;
			value.Dispose();
			if (fail)
				throw failure;
		}
		ValueTask operation;
		if (generic) {
			TaskScope<DisposableItem> scope = Tools.Scope.ExecuteOnDisposeAsync(Complete, item);
			Assert.That(scope.Item, Is.SameAs(item));
			Assert.That(entered.Task.IsCompleted, Is.False);
			operation = scope.DisposeAsync();
		} else {
			TaskScope scope = Tools.Scope.ExecuteOnDisposeAsync(() => Complete(item));
			Assert.That(entered.Task.IsCompleted, Is.False);
			operation = scope.DisposeAsync();
		}
		try {
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(operation.IsCompleted, Is.False);
			Assert.That(item.Calls, Is.Zero);
		} finally {
			completion.TrySetResult();
		}
		if (fail)
			await Assert.ThatAsync(async () => await operation, Throws.Exception.SameAs(failure));
		else
			await operation;
		Assert.That(item.Calls, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ReadWriteFactoriesRetainTheOwnersAcquisition(bool write) {
		ISynchronizedObject owner = new SynchronizedObject();
		using var threadLock = owner.ThreadLock;
		if (write) {
			using WriteScope scope = Tools.Scope.EnterWriteScope(owner);
			Assert.That(threadLock.IsWriteLockHeld, Is.True);
		} else {
			using ReadScope scope = Tools.Scope.EnterReadScope(owner);
			Assert.That(threadLock.IsReadLockHeld, Is.True);
		}
		Assert.That(threadLock.IsReadLockHeld || threadLock.IsWriteLockHeld, Is.False);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void TypedFactoriesPreserveConcreteInnerScopeTypes(bool write) {
		ISynchronizedObject<NoOpScope, NoOpScope> owner = new SynchronizedObject<NoOpScope, NoOpScope>();
		using var threadLock = owner.ThreadLock;
		if (write) {
			using WriteScope<NoOpScope> scope = Tools.Scope.EnterWriteScope(owner);
			Assert.That(threadLock.IsWriteLockHeld, Is.True);
		} else {
			using ReadScope<NoOpScope> scope = Tools.Scope.EnterReadScope(owner);
			Assert.That(threadLock.IsReadLockHeld, Is.True);
		}
		Assert.That(threadLock.IsReadLockHeld || threadLock.IsWriteLockHeld, Is.False);
	}

	[Test]
	public void AccessFactoryUsesTheProvidersScope() {
		var provider = new CustomCriticalObject();
		ICriticalObject owner = provider;
		using (AccessScope scope = Tools.Scope.EnterAccessScope(owner)) {
			Assert.That(provider.Entered, Is.EqualTo(1));
			Assert.That(provider.Closed, Is.Zero);
			Assert.That(provider.IsLocked, Is.True);
		}
		Assert.That(provider.Closed, Is.EqualTo(1));
		Assert.That(provider.IsLocked, Is.False);
	}

	[Test]
	public void PositionFactoryRestoresPositionWithoutOwningTheStream() {
		using var stream = new MemoryStream(new byte[16]);
		stream.Position = 7;
		using (RestorePositionScope scope = Tools.Scope.EnterRestorePositionScope(stream)) {
			Assert.That(stream.Position, Is.EqualTo(7));
			stream.ReadByte();
		}
		Assert.That(stream.Position, Is.EqualTo(7));
		Assert.That(stream.CanRead, Is.True);
	}

	[TestCase(4, SeekOrigin.Begin, 4)]
	[TestCase(2, SeekOrigin.Current, 5)]
	[TestCase(-2, SeekOrigin.End, 14)]
	public void SeekingPositionFactoryUsesTheRequestedOriginAndRestoresTheOriginalPosition(long offset, SeekOrigin origin, long expectedPosition) {
		using var stream = new MemoryStream(new byte[16]);
		stream.Position = 3;
		using (RestorePositionScope scope = Tools.Scope.EnterRestorePositionSeek(stream, offset, origin))
			Assert.That(stream.Position, Is.EqualTo(expectedPosition));
		Assert.That(stream.Position, Is.EqualTo(3));
		Assert.That(stream.CanRead, Is.True);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void PageFactoriesKeepTheOwnersPagePinnedUntilDisposal(bool byIndex) {
		using var list = new MemoryPagedList<int>(16, 16, sizeof(int));
		list.AddRange(new[] { 1, 2, 3, 4, 5, 6, 7, 8 });
		IPagedList<int> owner = list;
		PageScope<int> scope = byIndex ? Tools.Scope.EnterOpenPageScope(owner, 0L) : Tools.Scope.EnterOpenPageScope(owner, owner.Pages[0]);
		try {
			Assert.That(owner.Pages[0].State, Is.EqualTo(PageState.Loaded));
			Assert.That(() => owner.EnterOpenPageScope(owner.Pages[1]), Throws.TypeOf<InvalidOperationException>());
		} finally {
			scope.Dispose();
		}
		using (Tools.Scope.EnterOpenPageScope(owner, 1L))
			Assert.That(owner.Pages[0].State, Is.EqualTo(PageState.Unloaded));
	}

	[Test]
	public void MonitorLockFactoryReleasesTheAcquiredMonitor() {
		var syncRoot = new object();
		using (MonitorScope scope = Tools.Scope.EnterLockScope(syncRoot))
			Assert.That(Monitor.IsEntered(syncRoot), Is.True);
		Assert.That(Monitor.IsEntered(syncRoot), Is.False);
	}

	[Test]
	public void FastLockFactoryReturnsItsSynchronousOnlyScope() {
		var gate = new FastLock();
		var scope = Tools.Scope.EnterLockScope(gate);
		Assert.That(scope.Item.IsWriteLockHeld, Is.True);
		scope.Dispose();
		Assert.That(scope.Item.IsWriteLockHeld, Is.False);
	}

	[Test]
	public void NonReentrantLockFactoryReturnsItsSynchronousOnlyScope() {
		var gate = new NonReentrantLock();
		using (SyncScope<NonReentrantLock> scope = Tools.Scope.EnterLockScope(gate))
			Assert.That(gate.IsLocked, Is.True);
		Assert.That(gate.IsLocked, Is.False);
	}

	[Test]
	public void WaitFactoriesRejectMissingSemaphore([Values] bool throughFactory) {
		SemaphoreSlim semaphore = null;
		Assert.That(() => {
			_ = throughFactory ? Tools.Scope.EnterWaitScope(semaphore) : semaphore.EnterWaitScope();
		}, Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("semaphore"));
	}

	[Test]
	public void WaitFactoryReturnsTheAcquiredSemaphorePermit() {
		using var semaphore = new SemaphoreSlim(1, 1);
		using (ActionScope<SemaphoreSlim> scope = Tools.Scope.EnterWaitScope(semaphore))
			Assert.That(semaphore.CurrentCount, Is.Zero);
		Assert.That(semaphore.CurrentCount, Is.EqualTo(1));
	}

	[Test]
	public void HasherFactoryReturnsAUsableBorrowedHasherForTheRequestedAlgorithm() {
		using Hashers.HasherScope scope = Tools.Scope.EnterBorrowHasherScope(CHF.SHA2_256, out var hasher);
		hasher.Transform("abc"u8);
		Assert.That(hasher.GetResult(), Is.EqualTo(Hashers.Hash(CHF.SHA2_256, "abc"u8)));
	}

	[Test]
	public async Task SynchronousOnlyFactoryRetainsTheItemWithoutRunningCleanupOnAsyncDisposal() {
		var item = new DisposableItem();
		var scope = Tools.Scope.EnterSyncScope(static value => value.Dispose(), item);
		try {
			Assert.That(scope.Item, Is.SameAs(item));
			await Assert.ThatAsync(async () => await scope.DisposeAsync(), Throws.TypeOf<NotSupportedException>());
			Assert.That(item.Calls, Is.Zero);
		} finally {
			scope.Dispose();
		}
		Assert.That(item.Calls, Is.EqualTo(1));
	}

	[Test]
	public async Task AsyncOnlyFactoryAwaitsCleanupAndRejectsSynchronousDisposal() {
		var item = new DisposableItem();
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var scope = Tools.Scope.EnterAsyncScope(async value => {
			await completion.Task;
			value.Dispose();
		}, item);
		Assert.That(scope.Item, Is.SameAs(item));
		Assert.That(() => scope.Dispose(), Throws.TypeOf<NotSupportedException>());
		var operation = scope.DisposeAsync();
		Assert.That(operation.IsCompleted, Is.False);
		Assert.That(item.Calls, Is.Zero);
		completion.SetResult();
		await operation;
		Assert.That(item.Calls, Is.EqualTo(1));
	}

	[Test]
	public async Task NoOpAndValueFactoriesDoNotOwnTheSuppliedItem() {
		var item = new DisposableItem();
		NoOpScope noOp = Tools.Scope.EnterNoOpScope();
		NoOpScope<DisposableItem> value = Tools.Scope.EnterValueScope(item);
		noOp.Dispose();
		await noOp.DisposeAsync();
		value.Dispose();
		await value.DisposeAsync();
		Assert.That(value.Item, Is.SameAs(item));
		Assert.That(item.Calls, Is.Zero);
	}

	private sealed class CustomCriticalObject : CriticalObject {
		public int Entered { get; private set; }
		public int Closed { get; private set; }

		protected override void OnAccessScopeOpen() => Entered++;

		protected override void OnAccessScopeClosed() => Closed++;
	}

	private sealed class DisposableItem : IDisposable {
		public int Calls { get; private set; }
		public void Dispose() => Calls++;
	}
}
