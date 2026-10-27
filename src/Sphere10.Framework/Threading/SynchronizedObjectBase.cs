// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;

namespace Sphere10.Framework;

/// <summary>Owns a reader/writer lock and the resource hooks used by its concrete scopes.</summary>
public abstract class SynchronizedObjectBase : IReaderWriterLock {
	internal readonly ReaderWriterLockSlim _threadLock;

	protected SynchronizedObjectBase(LockRecursionPolicy policy) {
		_threadLock = new ReaderWriterLockSlim(policy);
	}

	public ReaderWriterLockSlim ThreadLock => ParentSyncObjectCore?.ThreadLock ?? _threadLock;

	protected IReaderWriterLock ParentSyncObjectCore { get; set; }

	internal void NotifyReadScopeOpened() => OnReadScopeOpen();

	internal void NotifyWriteScopeOpened() => OnWriteScopeOpen();

	internal void NotifyReadScopeClosed() => OnReadScopeClosed();

	internal void NotifyWriteScopeClosed() => OnWriteScopeClosed();

	protected virtual void OnReadScopeOpen() {
	}

	protected virtual void OnReadScopeClosed() {
	}

	protected virtual void OnWriteScopeOpen() {
	}

	protected virtual void OnWriteScopeClosed() {
	}

	protected virtual void EnsureReadable() {
		if (!(ThreadLock.IsReadLockHeld || ThreadLock.IsUpgradeableReadLockHeld))
			throw new SoftwareException("Resource has not entered a read scope");
	}

	protected virtual void EnsureWritable() {
		if (!ThreadLock.IsWriteLockHeld)
			throw new SoftwareException("Resource has not entered a write scope");
	}
}
