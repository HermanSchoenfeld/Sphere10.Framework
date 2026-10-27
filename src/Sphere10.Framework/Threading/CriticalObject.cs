// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;

namespace Sphere10.Framework;

public class CriticalObject : ICriticalObject {
	
	private readonly object _lock;

	public CriticalObject() : this(new object()) {
	}

	public CriticalObject(object @lock) {
		Guard.ArgumentNotNull(@lock, nameof(@lock));
		_lock = @lock; 
	}

	public ICriticalObject ParentCriticalObject { get; set; }

	public object Lock => ParentCriticalObject?.Lock ?? _lock;

	public bool IsLocked => Monitor.IsEntered(Lock);

	public AccessScope EnterAccessScope() {
		var lockObj = Lock;
		Monitor.Enter(lockObj);
		try {
			OnAccessScopeOpen();
			return new AccessScope(lockObj, this);
		} catch {
			Monitor.Exit(lockObj);
			throw;
		}
	}

	internal void NotifyAccessScopeClosed() => OnAccessScopeClosed();

	protected virtual void OnAccessScopeOpen() {
	}

	protected virtual void OnAccessScopeClosed() {
	}

}

