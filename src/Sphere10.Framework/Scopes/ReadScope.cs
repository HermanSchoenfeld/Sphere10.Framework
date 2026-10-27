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

namespace Sphere10.Framework;

/// <summary>Owns one synchronous read lock acquisition or cleanup callback. Keep the value on its acquiring thread and do not copy its ownership.</summary>
public readonly struct ReadScope : IScope {
	private readonly ReaderWriterLockSlim _threadLock;
	private readonly object _ownerOrCallback;

	internal ReadScope(SynchronizedObjectBase owner) {
		Guard.ArgumentNotNull(owner, nameof(owner));
		_threadLock = owner.ThreadLock;
		_ownerOrCallback = owner;
		_threadLock.EnterReadLock();
		try {
			owner.NotifyReadScopeOpened();
		} catch {
			_threadLock.ExitReadLock();
			throw;
		}
	}

	public ReadScope(Action onDispose) {
		Guard.ArgumentNotNull(onDispose, nameof(onDispose));
		_threadLock = null;
		_ownerOrCallback = onDispose;
	}

	public void Dispose() {
		if (_threadLock is null) {
			((Action)_ownerOrCallback)();
		} else {
			_threadLock.ExitReadLock();
			((SynchronizedObjectBase)_ownerOrCallback).NotifyReadScopeClosed();
		}
	}

	public ValueTask DisposeAsync() => ValueTask.FromException(new NotSupportedException("Thread-affine lock scopes require synchronous disposal."));
}

/// <summary>Owns a caller-selected cleanup scope followed by one read-lock release.</summary>
public struct ReadScope<TScope> : IScope
	where TScope : struct, IScope {
	private TScope _innerScope;
	private ReadScope _lockScope;

	public ReadScope(TScope innerScope, ReadScope lockScope) {
		_innerScope = innerScope;
		_lockScope = lockScope;
	}

	public void Dispose() {
		_innerScope.Dispose();
		_lockScope.Dispose();
	}

	public ValueTask DisposeAsync() => ValueTask.FromException(new NotSupportedException("Thread-affine lock scopes require synchronous disposal."));
}
