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

/// <summary>
/// A synchronization mechanism similar to the <see cref="Monitor"/> but with "condition-based locking". This can be used by a producer/consumer queue to synchronize
/// access to resource. Readers "block until" the collection has items and writers "block until" the collection has free space.
///
/// The constructs are provided by <see cref="BlockUntil"/> and <see cref="DelayUntil"/> which return a scope that functions conceptually similar to a C# "lock" scope except
/// with a condition.
///
/// 
/// </summary>
/// <remarks>
/// lock(obj AND CONDITION) { 
///     ... code has exclusive lock on obj and condition is true
/// }
/// The delay helpers acquire the thread-affine monitor inside Task.Run; they do not marshal disposal back to that worker.
/// Returned scopes require synchronous disposal on the acquiring thread.
/// </remarks>
public class ProducerConsumerLock : IDisposable {
	private volatile bool _disposed;

	private readonly SemaphoreSlim _producerSemaphore;
	private readonly SemaphoreSlim _consumerSemaphore;

	public ProducerConsumerLock()
		: this(new object()) {
	}

	public ProducerConsumerLock(object sharedLock) {
		SharedLock = sharedLock;
		_disposed = false;
		_producerSemaphore = new(1);
		_consumerSemaphore = new(1);
	}

	public object SharedLock { get; }

	public ProducerConsumerScope ProducerBlockUntil(Func<bool> condition) => BlockUntil(_producerSemaphore, condition);

	public ProducerConsumerScope ConsumerBlockUntil(Func<bool> condition) => BlockUntil(_consumerSemaphore, condition);

	public ProducerConsumerScope BlockUntil(SemaphoreSlim semaphore, Func<bool> condition) {
		Guard.ArgumentNotNull(condition, nameof(condition));
		var threadID = Thread.CurrentThread.ManagedThreadId;
		var role = semaphore == _producerSemaphore ? ProducerConsumerType.Producer : ProducerConsumerType.Consumer;
		//Tools.Debugger.AddMessage($"{role} TID:{threadID} waiting");
		semaphore.Wait(); // note: if MANY more threads than cores start piling up here, this can become very slow!
		//Tools.Debugger.AddMessage($"{role} TID:{threadID} entered");
		try {
			Monitor.Enter(SharedLock);
			while (!condition() || _disposed) {
				ObjectDisposedException.ThrowIf(_disposed, this);
				Monitor.Pulse(SharedLock);
				Monitor.Wait(SharedLock);
				Debug.Assert(Thread.CurrentThread.ManagedThreadId == threadID, "Different thread in lock loop, can lead to forever locks");
			}
		} catch {
			if (Monitor.IsEntered(SharedLock)) {
				Monitor.Pulse(SharedLock);
				Monitor.Exit(SharedLock);
			}
			semaphore.Release();
			throw;
		}


		return new ProducerConsumerScope(SharedLock, semaphore, threadID);
	}
	public async Task<ProducerConsumerScope> ProducerDelayUntil(Func<bool> condition) {
		return await Task.Run(() => ProducerBlockUntil(condition));
	}

	public async Task<ProducerConsumerScope> ConsumerDelayUntil(Func<bool> condition) {
		return await Task.Run(() => ConsumerBlockUntil(condition));
	}

	public void Pulse() {
		lock (SharedLock)
			Monitor.Pulse(SharedLock);
	}

	public void Dispose() {
		if (_disposed)
			return;
		_disposed = true;
		lock (SharedLock)
			Monitor.PulseAll(SharedLock);
		Tools.Exceptions.ExecuteIgnoringException(_producerSemaphore.Dispose);
		Tools.Exceptions.ExecuteIgnoringException(_consumerSemaphore.Dispose);
	}

}

