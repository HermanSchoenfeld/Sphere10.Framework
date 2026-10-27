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

/// <summary>Owns one critical-object access acquisition.</summary>
/// <remarks>Keep one concrete owner, normally a using local. Dispose that owner once.
/// Monitor acquisitions must be released on the acquiring thread.</remarks>
public readonly struct AccessScope : IScope {
	private readonly object _lock;
	private readonly CriticalObject _owner;

	internal AccessScope(object acquiredLock, CriticalObject owner) {
		Guard.ArgumentNotNull(acquiredLock, nameof(acquiredLock));
		Guard.ArgumentNotNull(owner, nameof(owner));
		_lock = acquiredLock;
		_owner = owner;
	}

	public void Dispose() {
		Monitor.Exit(_lock);
		_owner.NotifyAccessScopeClosed();
	}

	public ValueTask DisposeAsync() => ValueTask.FromException(new NotSupportedException("Thread-affine lock scopes require synchronous disposal."));
}
