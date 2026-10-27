// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;

namespace Sphere10.Framework;

public sealed class FastLock {
	private readonly ReaderWriterLockSlim _lock;

	public FastLock() {
		_lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
	}

	/// <summary>Acquires a recursive lock whose scope must be disposed synchronously on this thread.</summary>
	public SyncScope<ReaderWriterLockSlim> EnterLockScope() {
		_lock.EnterWriteLock();
		return new SyncScope<ReaderWriterLockSlim>(_lock, static threadLock => threadLock.ExitWriteLock());
	}

}

