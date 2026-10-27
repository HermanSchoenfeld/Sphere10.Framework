// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ThreadAffineScopeTests {

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	public void ActiveNativeScopesRejectAsyncDisposalWithoutReleasingTheirAcquisition(int kind) {
		var critical = new HookedCriticalObject();
		var synchronized = new HookedSynchronizedObject();
		using var threadLock = synchronized.ThreadLock;
		var ownerThread = Environment.CurrentManagedThreadId;
		IScope scope = kind switch {
			0 => critical.EnterAccessScope(),
			1 => synchronized.EnterReadScope(),
			2 => synchronized.EnterWriteScope(),
			_ => throw new ArgumentOutOfRangeException(nameof(kind))
		};
		bool IsHeld() => kind == 0 ? critical.IsLocked : kind == 1 ? threadLock.IsReadLockHeld : threadLock.IsWriteLockHeld;
		try {
			Assert.That(IsHeld(), Is.True);
			AssertAsyncDisposalRejected(scope);
			Assert.That(IsHeld(), Is.True);
			Assert.That(critical.Closed + synchronized.Closed, Is.Zero);
		} finally {
			if (IsHeld())
				scope.Dispose();
		}
		Assert.That(IsHeld(), Is.False);
		Assert.That(critical.Closed + synchronized.Closed, Is.EqualTo(1));
		Assert.That(kind == 0 ? critical.ClosingThread : synchronized.ClosingThread, Is.EqualTo(ownerThread));
		Assert.That(kind == 0 ? critical.ClosedWhileHeld : synchronized.ClosedWhileHeld, Is.False);
	}

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	public void CallbackScopesRejectAsyncDisposalWithoutInvokingTheirFinalizer(int kind) {
		var calls = 0;
		var finalizingThread = 0;
		var ownerThread = Environment.CurrentManagedThreadId;
		Action finalize = () => {
			calls++;
			finalizingThread = Environment.CurrentManagedThreadId;
		};
		IScope scope = kind switch {
			0 => new SyncScope(finalize),
			1 => new ReadScope(finalize),
			2 => new WriteScope(finalize),
			_ => throw new ArgumentOutOfRangeException(nameof(kind))
		};
		AssertAsyncDisposalRejected(scope);
		Assert.That(calls, Is.Zero);
		scope.Dispose();
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(finalizingThread, Is.EqualTo(ownerThread));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void TypedScopesRejectAsyncDisposalWithoutStartingInnerFinalization(bool write) {
		var synchronized = new HookedSynchronizedObject();
		using var threadLock = synchronized.ThreadLock;
		var observation = new InnerFinalization {
			IsLockHeld = () => write ? threadLock.IsWriteLockHeld : threadLock.IsReadLockHeld
		};
		var ownerThread = Environment.CurrentManagedThreadId;
		IScope scope = write
			? new WriteScope<ObservedScope>(new ObservedScope(observation), synchronized.EnterWriteScope())
			: new ReadScope<ObservedScope>(new ObservedScope(observation), synchronized.EnterReadScope());
		try {
			AssertAsyncDisposalRejected(scope);
			Assert.That(observation.SyncCalls, Is.Zero);
			Assert.That(observation.AsyncCalls, Is.Zero);
			Assert.That(observation.IsLockHeld(), Is.True);
			Assert.That(synchronized.Closed, Is.Zero);
		} finally {
			if (observation.IsLockHeld())
				scope.Dispose();
		}
		Assert.That(observation.SyncCalls, Is.EqualTo(1));
		Assert.That(observation.AsyncCalls, Is.Zero);
		Assert.That(observation.FinalizedWhileHeld, Is.True);
		Assert.That(observation.FinalizingThread, Is.EqualTo(ownerThread));
		Assert.That(observation.IsLockHeld(), Is.False);
		Assert.That(synchronized.Closed, Is.EqualTo(1));
	}

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	public void UnconstructedNativeScopesDoNotSilentlyDispose(int kind) {
		IScope scope = kind switch {
			0 => default(AccessScope),
			1 => default(ReadScope),
			2 => default(WriteScope),
			_ => throw new ArgumentOutOfRangeException(nameof(kind))
		};
		if (kind == 0)
			Assert.That(scope.Dispose, Throws.TypeOf<ArgumentNullException>());
		else
			Assert.That(scope.Dispose, Throws.TypeOf<NullReferenceException>());
		AssertAsyncDisposalRejected(scope);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ScopeCapturesItsOwnersLockBeforeTheOpenHookChangesParent(bool write) {
		var firstParent = new SynchronizedObject();
		var secondParent = new SynchronizedObject();
		using var firstLock = firstParent.ThreadLock;
		using var secondLock = secondParent.ThreadLock;
		var synchronized = new HookedSynchronizedObject();
		using var ownLock = synchronized.ThreadLock;
		synchronized.ParentSyncObject = firstParent;
		synchronized.Opening = () => {
			Assert.That(write ? firstLock.IsWriteLockHeld : firstLock.IsReadLockHeld, Is.True);
			synchronized.ParentSyncObject = secondParent;
		};
		IScope scope = write ? synchronized.EnterWriteScope() : synchronized.EnterReadScope();
		try {
			Assert.That(synchronized.ThreadLock, Is.SameAs(secondLock));
			Assert.That(write ? firstLock.IsWriteLockHeld : firstLock.IsReadLockHeld, Is.True);
			Assert.That(secondLock.IsReadLockHeld || secondLock.IsWriteLockHeld, Is.False);
		} finally {
			scope.Dispose();
		}
		Assert.That(firstLock.IsReadLockHeld || firstLock.IsWriteLockHeld, Is.False);
		Assert.That(secondLock.IsReadLockHeld || secondLock.IsWriteLockHeld, Is.False);
		Assert.That(synchronized.Closed, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void FailedOpenHookReleasesCapturedLockAfterChangingParent(bool write) {
		var firstParent = new SynchronizedObject();
		var secondParent = new SynchronizedObject();
		using var firstLock = firstParent.ThreadLock;
		using var secondLock = secondParent.ThreadLock;
		var synchronized = new HookedSynchronizedObject();
		using var ownLock = synchronized.ThreadLock;
		synchronized.ParentSyncObject = firstParent;
		synchronized.Opening = () => {
			Assert.That(write ? firstLock.IsWriteLockHeld : firstLock.IsReadLockHeld, Is.True);
			synchronized.ParentSyncObject = secondParent;
			throw new OpenHookException();
		};
		if (write)
			Assert.That(() => synchronized.EnterWriteScope(), Throws.TypeOf<OpenHookException>());
		else
			Assert.That(() => synchronized.EnterReadScope(), Throws.TypeOf<OpenHookException>());
		Assert.That(synchronized.ThreadLock, Is.SameAs(secondLock));
		Assert.That(firstLock.IsReadLockHeld || firstLock.IsWriteLockHeld, Is.False);
		Assert.That(secondLock.IsReadLockHeld || secondLock.IsWriteLockHeld, Is.False);
		Assert.That(synchronized.Closed, Is.Zero);
	}

	private static void AssertAsyncDisposalRejected(IScope scope) {
		var operation = ValueTask.CompletedTask;
		Assert.That(() => { operation = scope.DisposeAsync(); }, Throws.Nothing);
		Assert.That(operation.IsCompleted, Is.True);
		Assert.That(operation.IsCompletedSuccessfully, Is.False);
		Assert.That(() => operation.GetAwaiter().GetResult(), Throws.TypeOf<NotSupportedException>());
	}

	private sealed class HookedCriticalObject : CriticalObject {
		public int Closed { get; private set; }
		public int ClosingThread { get; private set; }
		public bool ClosedWhileHeld { get; private set; }

		protected override void OnAccessScopeClosed() {
			Closed++;
			ClosingThread = Environment.CurrentManagedThreadId;
			ClosedWhileHeld = IsLocked;
		}
	}

	private sealed class HookedSynchronizedObject : SynchronizedObject {
		public Action Opening { get; set; }
		public int Closed { get; private set; }
		public int ClosingThread { get; private set; }
		public bool ClosedWhileHeld { get; private set; }

		protected override void OnReadScopeOpen() => Opening?.Invoke();
		protected override void OnWriteScopeOpen() => Opening?.Invoke();

		protected override void OnReadScopeClosed() {
			Closed++;
			ClosingThread = Environment.CurrentManagedThreadId;
			ClosedWhileHeld = ThreadLock.IsReadLockHeld;
		}

		protected override void OnWriteScopeClosed() {
			Closed++;
			ClosingThread = Environment.CurrentManagedThreadId;
			ClosedWhileHeld = ThreadLock.IsWriteLockHeld;
		}
	}

	private readonly struct ObservedScope : IScope {
		private readonly InnerFinalization _observation;

		public ObservedScope(InnerFinalization observation) {
			_observation = observation;
		}

		public void Dispose() {
			_observation.SyncCalls++;
			_observation.FinalizedWhileHeld = _observation.IsLockHeld();
			_observation.FinalizingThread = Environment.CurrentManagedThreadId;
		}

		public ValueTask DisposeAsync() {
			_observation.AsyncCalls++;
			return ValueTask.CompletedTask;
		}
	}

	private sealed class InnerFinalization {
		public Func<bool> IsLockHeld { get; set; }
		public int SyncCalls { get; set; }
		public int AsyncCalls { get; set; }
		public bool FinalizedWhileHeld { get; set; }
		public int FinalizingThread { get; set; }
	}

	private sealed class OpenHookException : Exception {
	}
}
