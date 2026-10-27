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

/// <summary>Owns one monitor acquisition. Dispose once on the acquiring thread.</summary>
public readonly struct MonitorScope : IScope {
	private readonly object _lock;

	public MonitorScope(object @lock) {
		Guard.ArgumentNotNull(@lock, nameof(@lock));
		_lock = @lock;
		Monitor.Enter(_lock);
	}

	public void Dispose() => Monitor.Exit(_lock);

	public ValueTask DisposeAsync() => ValueTask.FromException(new NotSupportedException("Thread-affine lock scopes require synchronous disposal."));
}
