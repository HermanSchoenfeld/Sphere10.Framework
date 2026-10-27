// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ValueSynchronizationScopeTests {
	[Test]
	public void AcquiredScopesReleaseTheirLocks() {
		var synchronized = new HookedSynchronizedObject();
		var read = synchronized.EnterReadScope();
		read.Dispose();
		var write = synchronized.EnterWriteScope();
		write.Dispose();
		Assert.That(synchronized.ReadClosed, Is.EqualTo(1));
		Assert.That(synchronized.WriteClosed, Is.EqualTo(1));
	}

	[Test]
	public void NestedScopesPreserveRecursionAndHookOrdering([Values] bool write) {
		var synchronized = new HookedSynchronizedObject();
		IDisposable outer = write ? synchronized.EnterWriteScope() : synchronized.EnterReadScope();
		IDisposable inner = write ? synchronized.EnterWriteScope() : synchronized.EnterReadScope();
		Assert.That(write ? synchronized.ThreadLock.RecursiveWriteCount : synchronized.ThreadLock.RecursiveReadCount, Is.EqualTo(2));
		inner.Dispose();
		Assert.That(write ? synchronized.ThreadLock.IsWriteLockHeld : synchronized.ThreadLock.IsReadLockHeld, Is.True);
		outer.Dispose();
		Assert.That(synchronized.Events, Is.EqualTo(write
			? new[] { "write-open:True", "write-open:True", "write-close:True", "write-close:False" }
			: new[] { "read-open:True", "read-open:True", "read-close:True", "read-close:False" }));
	}

	[Test]
	public void NoRecursionPolicyStillRejectsNestedAcquisition([Values] bool write) {
		var synchronized = new SynchronizedObject(LockRecursionPolicy.NoRecursion);
		if (write) {
			using var outer = synchronized.EnterWriteScope();
			Assert.That(() => synchronized.EnterWriteScope(), Throws.TypeOf<LockRecursionException>());
			Assert.That(synchronized.ThreadLock.IsWriteLockHeld, Is.True);
		} else {
			using var outer = synchronized.EnterReadScope();
			Assert.That(() => synchronized.EnterReadScope(), Throws.TypeOf<LockRecursionException>());
			Assert.That(synchronized.ThreadLock.IsReadLockHeld, Is.True);
		}
	}

	[Test]
	public void OpenHookFailureRollsBackOnlyItsAcquisition([Values] bool write) {
		var synchronized = new HookedSynchronizedObject();
		IDisposable outer = write ? synchronized.EnterWriteScope() : synchronized.EnterReadScope();
		synchronized.ThrowOnOpen = true;
		if (write)
			Assert.That(() => synchronized.EnterWriteScope(), Throws.TypeOf<TestScopeException>());
		else
			Assert.That(() => synchronized.EnterReadScope(), Throws.TypeOf<TestScopeException>());
		Assert.That(write ? synchronized.ThreadLock.RecursiveWriteCount : synchronized.ThreadLock.RecursiveReadCount, Is.EqualTo(1));
		Assert.That(synchronized.ReadClosed + synchronized.WriteClosed, Is.Zero);
		outer.Dispose();
		Assert.That(synchronized.ThreadLock.IsReadLockHeld || synchronized.ThreadLock.IsWriteLockHeld, Is.False);
	}

	[Test]
	public void ClosedHookRunsAfterRelease([Values] bool write) {
		var synchronized = new HookedSynchronizedObject { ThrowOnClose = true };
		IDisposable scope = write ? synchronized.EnterWriteScope() : synchronized.EnterReadScope();
		Assert.That(() => scope.Dispose(), Throws.TypeOf<TestScopeException>());
		Assert.That(synchronized.ThreadLock.IsReadLockHeld || synchronized.ThreadLock.IsWriteLockHeld, Is.False);
		Assert.That(synchronized.ReadClosed + synchronized.WriteClosed, Is.EqualTo(1));
	}

	[Test]
	public void ClosedHookRunsOnTheAcquiringThread([Values] bool write) {
		var synchronized = new HookedSynchronizedObject();
		IDisposable scope = write ? synchronized.EnterWriteScope() : synchronized.EnterReadScope();
		var acquiringThread = Environment.CurrentManagedThreadId;
		synchronized.ClosedCallback = () => Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(acquiringThread));
		scope.Dispose();
		Assert.That(synchronized.ReadClosed + synchronized.WriteClosed, Is.EqualTo(1));
		Assert.That(synchronized.ThreadLock.IsReadLockHeld || synchronized.ThreadLock.IsWriteLockHeld, Is.False);
	}

	[Test]
	public void WrongThreadDisposalCanBeRetriedOnTheOwnerThread([Values] bool write) {
		var synchronized = new SynchronizedObject();
		if (write)
			VerifyWrongThreadRetry(synchronized.EnterWriteScope(), synchronized, true);
		else
			VerifyWrongThreadRetry(synchronized.EnterReadScope(), synchronized, false);
	}

	[Test]
	public void ScopeReleasesTheLockItAcquiredAfterParentChanges([Values] bool write) {
		var synchronized = new SynchronizedObject();
		var originalLock = synchronized.ThreadLock;
		var firstParent = new SynchronizedObject();
		var secondParent = new SynchronizedObject();
		synchronized.ParentSyncObject = firstParent;
		IDisposable scope = write ? synchronized.EnterWriteScope() : synchronized.EnterReadScope();
		synchronized.ParentSyncObject = secondParent;
		scope.Dispose();
		Assert.That(firstParent.ThreadLock.IsReadLockHeld || firstParent.ThreadLock.IsWriteLockHeld, Is.False);
		Assert.That(secondParent.ThreadLock.IsReadLockHeld || secondParent.ThreadLock.IsWriteLockHeld, Is.False);
		Assert.That(originalLock.IsReadLockHeld || originalLock.IsWriteLockHeld, Is.False);
	}


	[Test]
	public void InterfaceMethodsOwnTheirAcquisition([Values] bool write) {
		ISynchronizedObject synchronized = new HookedSynchronizedObject();
		IDisposable scope = write ? synchronized.EnterWriteScope() : synchronized.EnterReadScope();
		Assert.That(write ? synchronized.ThreadLock.IsWriteLockHeld : synchronized.ThreadLock.IsReadLockHeld, Is.True);
		scope.Dispose();
		Assert.That(synchronized.ThreadLock.IsReadLockHeld || synchronized.ThreadLock.IsWriteLockHeld, Is.False);
	}

	[Test]
	public void CustomCallbackPreservesCleanupAndRetry([Values] bool write) {
		var provider = new CustomSynchronizedObject { FailFirstDispose = true };
		ISynchronizedObject contract = provider;
		IDisposable scope = write ? contract.EnterWriteScope() : contract.EnterReadScope();
		Assert.That(write ? provider.ThreadLock.IsWriteLockHeld : provider.ThreadLock.IsReadLockHeld, Is.True);
		Assert.That(() => scope.Dispose(), Throws.TypeOf<TestScopeException>());
		Assert.That(write ? provider.ThreadLock.IsWriteLockHeld : provider.ThreadLock.IsReadLockHeld, Is.True);
		scope.Dispose();
		Assert.That(provider.Completed, Is.EqualTo(1));
		Assert.That(provider.Attempts, Is.EqualTo(2));
		Assert.That(provider.ThreadLock.IsReadLockHeld || provider.ThreadLock.IsWriteLockHeld, Is.False);
	}

	[Test]
	public void TypedScopeFamilyReleasesLocksThroughExplicitOwnership([Values] bool write) {
		var synchronized = new HookedTypedSynchronizedObject();
		ISynchronizedObject<NoOpScope, NoOpScope> contract = synchronized;
		if (write) {
			using var scope = contract.EnterWriteScope();
			Assert.That(synchronized.ThreadLock.IsWriteLockHeld, Is.True);
		} else {
			using var scope = contract.EnterReadScope();
			Assert.That(synchronized.ThreadLock.IsReadLockHeld, Is.True);
		}
		Assert.That(synchronized.Closed, Is.EqualTo(1));
		Assert.That(synchronized.ThreadLock.IsReadLockHeld || synchronized.ThreadLock.IsWriteLockHeld, Is.False);
	}

	[Test]
	public void CallbackScopesRunTheirFinalizerDirectly([Values] bool write) {
		var calls = 0;
		if (write) {
			var scope = new WriteScope(() => calls++);
			scope.Dispose();
			scope.Dispose();
		} else {
			var scope = new ReadScope(() => calls++);
			scope.Dispose();
			scope.Dispose();
		}
		Assert.That(calls, Is.EqualTo(2));
	}

	[Test]
	public void DictionaryForwarderPreservesCustomProviderScopes() {
		var provider = new CustomSynchronizedObject();
		var dictionary = new SynchronizedDictionary<int, int>(new Dictionary<int, int>(), provider);
		dictionary.Add(7, 9);
		Assert.That(provider.Writes, Is.EqualTo(1));
		Assert.That(dictionary.ContainsKey(7), Is.True);
		Assert.That(provider.Reads, Is.EqualTo(1));
		var enumerator = dictionary.GetEnumerator();
		Assert.That(provider.ThreadLock.IsReadLockHeld, Is.True);
		Assert.That(enumerator.MoveNext(), Is.True);
		enumerator.Dispose();
		enumerator.Dispose();
		Assert.That(provider.Reads, Is.EqualTo(2));
		Assert.That(provider.Completed, Is.EqualTo(3));
		Assert.That(provider.ThreadLock.IsReadLockHeld, Is.False);
	}




	[Test]
	public void SynchronizedEnumeratorsHoldOneScopeUntilDisposed([Values(0, 1, 2, 3)] int family, [Values] bool beforeFirstMove) {
		IEnumerable<int> collection = family switch {
			0 => new SynchronizedCollection<int>(new List<int> { 1, 2 }),
			1 => new SynchronizedList<int>(new List<int> { 1, 2 }),
			2 => new SynchronizedExtendedList<int>(new ExtendedList<int>(new[] { 1, 2 })),
			3 => new SynchronizedSet<int>(new HashSet<int> { 1, 2 }),
			_ => throw new ArgumentOutOfRangeException(nameof(family))
		};
		var synchronized = (ISynchronizedObject)collection;
		var enumerator = collection.GetEnumerator();
		Assert.That(synchronized.ThreadLock.IsReadLockHeld, Is.True);
		if (!beforeFirstMove)
			Assert.That(enumerator.MoveNext(), Is.True);
		enumerator.Dispose();
		enumerator.Dispose();
		Assert.That(synchronized.ThreadLock.IsReadLockHeld, Is.False);
	}

	[Test]
	public void EnumeratorConstructionFailureReleasesItsReadScope() {
		var source = new CallbackCollection { FailEnumeration = true };
		var collection = new SynchronizedCollection<int>(source);
		Assert.That(() => collection.GetEnumerator(), Throws.TypeOf<TestScopeException>());
		Assert.That(collection.ThreadLock.IsReadLockHeld, Is.False);
	}

	[Test]
	public void EnumeratorReleasesScopeBeforeInnerDisposeIncludingFailures([Values] bool failDispose) {
		var source = new CallbackCollection();
		var collection = new SynchronizedCollection<int>(source);
		var callbacks = 0;
		var expected = new TestScopeException();
		source.OnDispose = () => {
			Assert.That(collection.ThreadLock.IsReadLockHeld, Is.False);
			callbacks++;
			if (failDispose)
				throw expected;
		};
		var enumerator = collection.GetEnumerator();
		if (failDispose)
			Assert.That(enumerator.Dispose, Throws.Exception.SameAs(expected));
		else
			enumerator.Dispose();
		Assert.That(callbacks, Is.EqualTo(1));
		Assert.That(collection.ThreadLock.IsReadLockHeld, Is.False);
		source.OnDispose = () => {
			Assert.That(collection.ThreadLock.IsReadLockHeld, Is.False);
			callbacks++;
		};
		Assert.That(() => enumerator.Dispose(), Throws.Nothing);
		Assert.That(callbacks, Is.EqualTo(2), "Repeated disposal must still reach the inner enumerator without reacquiring or releasing the read scope again.");
	}


	private static void VerifyWrongThreadRetry<TScope>(TScope scope, SynchronizedObject synchronized, bool write)
		where TScope : struct, IDisposable {
		Exception failure = null;
		var thread = new Thread(() => {
			try {
				scope.Dispose();
			} catch (Exception error) {
				failure = error;
			}
		});
		thread.Start();
		Assert.That(thread.Join(TimeSpan.FromSeconds(10)), Is.True);
		Assert.That(failure, Is.TypeOf<SynchronizationLockException>());
		Assert.That(write ? synchronized.ThreadLock.IsWriteLockHeld : synchronized.ThreadLock.IsReadLockHeld, Is.True);
		scope.Dispose();
		Assert.That(synchronized.ThreadLock.IsReadLockHeld || synchronized.ThreadLock.IsWriteLockHeld, Is.False);
	}

	private sealed class TestScopeException : Exception {
	}

	private sealed class HookedSynchronizedObject : SynchronizedObject {
		public List<string> Events { get; } = new();
		public bool ThrowOnOpen { get; set; }
		public bool ThrowOnClose { get; set; }
		public Action ClosedCallback { get; set; }
		public int ReadClosed { get; private set; }
		public int WriteClosed { get; private set; }

		protected override void OnReadScopeOpen() {
			Events.Add($"read-open:{ThreadLock.IsReadLockHeld}");
			if (ThrowOnOpen)
				throw new TestScopeException();
		}

		protected override void OnWriteScopeOpen() {
			Events.Add($"write-open:{ThreadLock.IsWriteLockHeld}");
			if (ThrowOnOpen)
				throw new TestScopeException();
		}

		protected override void OnReadScopeClosed() {
			ReadClosed++;
			Events.Add($"read-close:{ThreadLock.IsReadLockHeld}");
			ClosedCallback?.Invoke();
			if (ThrowOnClose)
				throw new TestScopeException();
		}

		protected override void OnWriteScopeClosed() {
			WriteClosed++;
			Events.Add($"write-close:{ThreadLock.IsWriteLockHeld}");
			ClosedCallback?.Invoke();
			if (ThrowOnClose)
				throw new TestScopeException();
		}
	}

	private sealed class HookedTypedSynchronizedObject : SynchronizedObject<NoOpScope, NoOpScope> {
		public int Closed { get; private set; }
		protected override void OnReadScopeClosed() => Closed++;
		protected override void OnWriteScopeClosed() => Closed++;
	}

	private sealed class CustomSynchronizedObject : ISynchronizedObject {
		public ISynchronizedObject ParentSyncObject { get; set; }
		public ReaderWriterLockSlim ThreadLock { get; } = new(LockRecursionPolicy.SupportsRecursion);
		public int Reads { get; private set; }
		public int Writes { get; private set; }
		public int Attempts { get; private set; }
		public int Completed { get; private set; }
		public bool FailFirstDispose { get; set; }

		public ReadScope EnterReadScope() {
			Reads++;
			ThreadLock.EnterReadLock();
			return new ReadScope(() => Complete(false));
		}

		public WriteScope EnterWriteScope() {
			Writes++;
			ThreadLock.EnterWriteLock();
			return new WriteScope(() => Complete(true));
		}

		private void Complete(bool write) {
			Attempts++;
			if (FailFirstDispose && Attempts == 1)
				throw new TestScopeException();
			if (write)
				ThreadLock.ExitWriteLock();
			else
				ThreadLock.ExitReadLock();
			Completed++;
		}
	}

	private sealed class CallbackCollection : ICollection<int> {
		public bool FailEnumeration { get; set; }
		public Action OnDispose { get; set; }
		public int Count => 1;
		public bool IsReadOnly => true;
		public void Add(int item) => throw new NotSupportedException();
		public void Clear() => throw new NotSupportedException();
		public bool Contains(int item) => item == 1;
		public void CopyTo(int[] array, int arrayIndex) => array[arrayIndex] = 1;
		public bool Remove(int item) => throw new NotSupportedException();

		public IEnumerator<int> GetEnumerator() {
			if (FailEnumeration)
				throw new TestScopeException();
			return new CallbackEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		private sealed class CallbackEnumerator(CallbackCollection owner) : IEnumerator<int> {
			private bool _moved;
			public int Current => 1;
			object IEnumerator.Current => Current;
			public bool MoveNext() => !_moved && (_moved = true);
			public void Reset() => _moved = false;
			public void Dispose() => owner.OnDispose?.Invoke();
		}
	}
}
