// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>Owns the monitor and role semaphore acquired by a producer-consumer lock.</summary>
public readonly struct ProducerConsumerScope : IScope {
	private readonly object _sharedLock;
	private readonly SemaphoreSlim _semaphore;
	private readonly int _threadId;

	internal ProducerConsumerScope(object sharedLock, SemaphoreSlim semaphore, int threadId) {
		Guard.ArgumentNotNull(sharedLock, nameof(sharedLock));
		Guard.ArgumentNotNull(semaphore, nameof(semaphore));
		_sharedLock = sharedLock;
		_semaphore = semaphore;
		_threadId = threadId;
	}

	public void Dispose() {
		Debug.Assert(Thread.CurrentThread.ManagedThreadId == _threadId, "Different thread terminating access scope, can lead to forever locks");
		Monitor.Pulse(_sharedLock); // This is needed since other call queues may be connected with _lock, waiting for whatever this operation does
		Monitor.Exit(_sharedLock);
		_semaphore.Release();
		//Tools.Debugger.AddMessage($"TID:{_threadId} exited");
	}

	public ValueTask DisposeAsync() => ValueTask.FromException(new NotSupportedException("Thread-affine lock scopes require synchronous disposal."));
}
