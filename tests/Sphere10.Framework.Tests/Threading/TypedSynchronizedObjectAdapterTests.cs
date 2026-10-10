// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using NUnit.Framework;

namespace Sphere10.Framework.UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class TypedSynchronizedObjectAdapterTests {
	[Test]
	public void DirectImplementerUsesTypedScopeDefaults() {
		var synchronized = new DirectSynchronizedObject();
		using var threadLock = synchronized.ThreadLock;
		var untyped = (ISynchronizedObject)synchronized;

		using (var readScope = untyped.EnterReadScope())
			Assert.That(readScope, Is.SameAs(synchronized.ReadScope));
		using (var writeScope = untyped.EnterWriteScope())
			Assert.That(writeScope, Is.SameAs(synchronized.WriteScope));

		Assert.That(synchronized.ReadScope.IsDisposed, Is.True);
		Assert.That(synchronized.WriteScope.IsDisposed, Is.True);
	}

	[Test]
	public void ParentDefaultPreservesAssignmentAndNull([Values(false, true)] bool useBaseClass) {
		var synchronized = CreateSynchronizedObject(useBaseClass);
		var parent = CreateSynchronizedObject(useBaseClass);
		using var childLock = synchronized.ThreadLock;
		using var parentLock = parent.ThreadLock;
		var untyped = (ISynchronizedObject)synchronized;

		untyped.ParentSyncObject = parent;

		Assert.That(synchronized.ParentSyncObject, Is.SameAs(parent));
		Assert.That(untyped.ParentSyncObject, Is.SameAs(parent));
		Assert.That(untyped.ThreadLock, Is.SameAs(parentLock));
		untyped.ParentSyncObject = null;
		Assert.That(synchronized.ParentSyncObject, Is.Null);
		Assert.That(untyped.ThreadLock, Is.SameAs(childLock));
	}

	[Test]
	public void ParentDefaultRejectsIncompatibleScopeTypes([Values(false, true)] bool useBaseClass) {
		var synchronized = CreateSynchronizedObject(useBaseClass);
		using var threadLock = synchronized.ThreadLock;
		var incompatible = new SynchronizedObject();
		using var incompatibleLock = incompatible.ThreadLock;
		var untyped = (ISynchronizedObject)synchronized;

		Assert.That(() => untyped.ParentSyncObject = incompatible, Throws.TypeOf<InvalidCastException>());
		Assert.That(synchronized.ParentSyncObject, Is.Null);
	}

	[Test]
	public void ConcreteSynchronizedObjectRetainsReadAndWriteLockLifetime() {
		var synchronized = new SynchronizedObject<Scope, Scope>();
		using var threadLock = synchronized.ThreadLock;
		var untyped = (ISynchronizedObject)synchronized;

		using (var readScope = untyped.EnterReadScope()) {
			Assert.That(readScope, Is.TypeOf<Scope>());
			Assert.That(threadLock.IsReadLockHeld, Is.True);
		}
		Assert.That(threadLock.IsReadLockHeld, Is.False);
		using (var writeScope = untyped.EnterWriteScope()) {
			Assert.That(writeScope, Is.TypeOf<Scope>());
			Assert.That(threadLock.IsWriteLockHeld, Is.True);
		}
		Assert.That(threadLock.IsWriteLockHeld, Is.False);
	}

	[Test]
	public void ClassAdaptersRetainLockAndParentDispatchWhenTypedInterfaceIsReimplemented() {
		var synchronized = new RemappedSynchronizedObject();
		var parent = new SynchronizedObject<Scope, Scope>();
		using var threadLock = synchronized.ThreadLock;
		using var parentLock = parent.ThreadLock;
		var typed = (ISynchronizedObject<Scope, Scope>)synchronized;
		var untyped = (ISynchronizedObject)synchronized;

		typed.ParentSyncObject = parent;
		Assert.That(synchronized.ParentSyncObject, Is.Null);
		Assert.That(untyped.ParentSyncObject, Is.Null);
		untyped.ParentSyncObject = parent;
		Assert.That(synchronized.ParentSyncObject, Is.SameAs(parent));
		untyped.ParentSyncObject = null;
		Assert.That(synchronized.ParentSyncObject, Is.Null);
		Assert.That(typed.ParentSyncObject, Is.SameAs(parent));

		using (typed.EnterReadScope())
			Assert.That(threadLock.IsReadLockHeld, Is.False);
		using (untyped.EnterReadScope())
			Assert.That(threadLock.IsReadLockHeld, Is.True);
		Assert.That(threadLock.IsReadLockHeld, Is.False);
		using (typed.EnterWriteScope())
			Assert.That(threadLock.IsWriteLockHeld, Is.False);
		using (untyped.EnterWriteScope())
			Assert.That(threadLock.IsWriteLockHeld, Is.True);
		Assert.That(threadLock.IsWriteLockHeld, Is.False);
	}

	private static ISynchronizedObject<Scope, Scope> CreateSynchronizedObject(bool useBaseClass)
		=> useBaseClass ? new SynchronizedObject<Scope, Scope>() : new DirectScopeSynchronizedObject();

	private sealed class RemappedSynchronizedObject : SynchronizedObject<Scope, Scope>, ISynchronizedObject<Scope, Scope> {
		ISynchronizedObject<Scope, Scope> ISynchronizedObject<Scope, Scope>.ParentSyncObject { get; set; }

		Scope ISynchronizedObject<Scope, Scope>.EnterReadScope() => new();

		Scope ISynchronizedObject<Scope, Scope>.EnterWriteScope() => new();
	}

	private sealed class DirectScopeSynchronizedObject : ISynchronizedObject<Scope, Scope> {
		private readonly ReaderWriterLockSlim _threadLock = new();

		public ISynchronizedObject<Scope, Scope> ParentSyncObject { get; set; }

		public ReaderWriterLockSlim ThreadLock => ParentSyncObject?.ThreadLock ?? _threadLock;

		public Scope EnterReadScope() => new();

		public Scope EnterWriteScope() => new();
	}

	private sealed class DirectSynchronizedObject : ISynchronizedObject<TestReadScope, TestWriteScope> {
		public ISynchronizedObject<TestReadScope, TestWriteScope> ParentSyncObject { get; set; }

		public ReaderWriterLockSlim ThreadLock { get; } = new();

		public TestReadScope ReadScope { get; } = new();

		public TestWriteScope WriteScope { get; } = new();

		public TestReadScope EnterReadScope() => ReadScope;

		public TestWriteScope EnterWriteScope() => WriteScope;
	}

	private sealed class TestReadScope : IDisposable {
		public bool IsDisposed { get; private set; }

		public void Dispose() => IsDisposed = true;
	}

	private sealed class TestWriteScope : IDisposable {
		public bool IsDisposed { get; private set; }

		public void Dispose() => IsDisposed = true;
	}
}
