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

namespace Sphere10.Framework.UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class TypedSynchronizedObjectAdapterTests {
	[Test]
	public void DirectImplementerDisposesItsInnerScopeBeforeReleasingTheLock() {
		var synchronized = new DirectSynchronizedObject();
		using var threadLock = synchronized.ThreadLock;
		ISynchronizedObject<ActionScope, ActionScope> contract = synchronized;
		var read = contract.EnterReadScope();
		Assert.That(threadLock.IsReadLockHeld, Is.True);
		read.Dispose();
		Assert.That(synchronized.ReadClosed, Is.EqualTo(1));
		Assert.That(threadLock.IsReadLockHeld, Is.False);
		var write = contract.EnterWriteScope();
		Assert.That(threadLock.IsWriteLockHeld, Is.True);
		write.Dispose();
		Assert.That(synchronized.WriteClosed, Is.EqualTo(1));
		Assert.That(threadLock.IsWriteLockHeld, Is.False);
	}

	[Test]
	public void TypedParentPreservesAssignmentAndCapturedLockIdentity() {
		var synchronized = new SynchronizedObject<NoOpScope, NoOpScope>();
		var parent = new SynchronizedObject<NoOpScope, NoOpScope>();
		using var childLock = synchronized.ThreadLock;
		using var parentLock = parent.ThreadLock;
		synchronized.ParentSyncObject = parent;
		Assert.That(synchronized.ThreadLock, Is.SameAs(parentLock));
		var scope = synchronized.EnterReadScope();
		synchronized.ParentSyncObject = null;
		scope.Dispose();
		Assert.That(parentLock.IsReadLockHeld, Is.False);
		Assert.That(synchronized.ThreadLock, Is.SameAs(childLock));
	}

	[Test]
	public void TypedAndOrdinaryContractsRemainDistinct() {
		Assert.That(typeof(ISynchronizedObject).IsAssignableFrom(typeof(ISynchronizedObject<NoOpScope, NoOpScope>)), Is.False);
		Assert.That(typeof(ISynchronizedObject).IsAssignableFrom(typeof(SynchronizedObject<NoOpScope, NoOpScope>)), Is.False);
	}

	[Test]
	public void ConcreteSynchronizedObjectRetainsReadAndWriteLockLifetime() {
		var synchronized = new SynchronizedObject<NoOpScope, NoOpScope>();
		using var threadLock = synchronized.ThreadLock;
		ISynchronizedObject<NoOpScope, NoOpScope> contract = synchronized;
		using (var readScope = contract.EnterReadScope()) {
			Assert.That(readScope, Is.TypeOf<ReadScope<NoOpScope>>());
			Assert.That(threadLock.IsReadLockHeld, Is.True);
		}
		Assert.That(threadLock.IsReadLockHeld, Is.False);
		using (var writeScope = contract.EnterWriteScope()) {
			Assert.That(writeScope, Is.TypeOf<WriteScope<NoOpScope>>());
			Assert.That(threadLock.IsWriteLockHeld, Is.True);
		}
		Assert.That(threadLock.IsWriteLockHeld, Is.False);
	}

	[Test]
	public void FailedInnerConstructionReleasesTheAcquiredLock() {
		var synchronized = new SynchronizedObject<ThrowingScope, ThrowingScope>();
		using var threadLock = synchronized.ThreadLock;
		Assert.That(() => synchronized.EnterReadScope(), Throws.Exception);
		Assert.That(threadLock.IsReadLockHeld, Is.False);
		Assert.That(() => synchronized.EnterWriteScope(), Throws.Exception);
		Assert.That(threadLock.IsWriteLockHeld, Is.False);
	}

	[Test]
	public void FailedInnerCleanupRetainsLockForRetry() {
		var synchronized = new SynchronizedObject();
		using var threadLock = synchronized.ThreadLock;
		var calls = 0;
		var scope = new ReadScope<ActionScope>(Tools.Scope.ExecuteOnDispose(() => {
			if (++calls == 1)
				throw new InvalidOperationException();
		}), synchronized.EnterReadScope());
		Assert.That(() => scope.Dispose(), Throws.TypeOf<InvalidOperationException>());
		Assert.That(threadLock.IsReadLockHeld, Is.True);
		scope.Dispose();
		Assert.That(calls, Is.EqualTo(2));
		Assert.That(threadLock.IsReadLockHeld, Is.False);
	}

	private sealed class DirectSynchronizedObject : ISynchronizedObject<ActionScope, ActionScope> {
		private readonly ReaderWriterLockSlim _threadLock = new();

		public ISynchronizedObject<ActionScope, ActionScope> ParentSyncObject { get; set; }
		public ReaderWriterLockSlim ThreadLock => ParentSyncObject?.ThreadLock ?? _threadLock;
		public int ReadClosed { get; private set; }
		public int WriteClosed { get; private set; }

		public ReadScope<ActionScope> EnterReadScope() {
			var threadLock = ThreadLock;
			threadLock.EnterReadLock();
			return new ReadScope<ActionScope>(Tools.Scope.ExecuteOnDispose(() => {
				Assert.That(threadLock.IsReadLockHeld, Is.True);
				ReadClosed++;
			}), new ReadScope(threadLock.ExitReadLock));
		}

		public WriteScope<ActionScope> EnterWriteScope() {
			var threadLock = ThreadLock;
			threadLock.EnterWriteLock();
			return new WriteScope<ActionScope>(Tools.Scope.ExecuteOnDispose(() => {
				Assert.That(threadLock.IsWriteLockHeld, Is.True);
				WriteClosed++;
			}), new WriteScope(threadLock.ExitWriteLock));
		}
	}

	private struct ThrowingScope : IScope {
		public ThrowingScope() => throw new InvalidOperationException("ActionScope construction failed.");
		public void Dispose() { }
		public ValueTask DisposeAsync() => default;
	}
}
