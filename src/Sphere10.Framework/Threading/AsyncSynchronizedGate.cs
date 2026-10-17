// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>FIFO exclusive admission for asynchronous work. Disposal releases admission; internal state uses short synchronized write scopes.</summary>
/// <remarks>Unlike a read/write scope this admission scope is not thread-affine, so it can span await. Admission is non-reentrant.</remarks>
public sealed class AsyncSynchronizedGate : SynchronizedObject {
	private readonly Queue<Waiter> _waiters = new();
	private bool _occupied;

	public async Task<IDisposable> EnterScopeAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		var waiter = new Waiter(cancellationToken);
		using var registration = cancellationToken.Register(() => Cancel(waiter));
		using (EnterWriteScope()) {
			if (!waiter.Cancelled) {
				if (_occupied)
					_waiters.Enqueue(waiter);
				else {
					_occupied = true;
					Grant(waiter);
				}
			}
		}
		return await waiter.Completion.Task.ConfigureAwait(false);
	}

	private void Cancel(Waiter waiter) {
		using var scope = EnterWriteScope();
		if (waiter.Granted)
			return;
		waiter.Cancelled = true;
		waiter.Completion.TrySetCanceled(waiter.CancellationToken);
	}

	private void Grant(Waiter waiter) {
		waiter.Granted = true;
		waiter.Completion.SetResult(new ActionScope(Release));
	}

	private void Release() {
		using var scope = EnterWriteScope();
		while (_waiters.Count > 0) {
			var next = _waiters.Dequeue();
			if (next.Cancelled)
				continue;
			Grant(next);
			return;
		}
		_occupied = false;
	}

	private sealed class Waiter {
		public Waiter(CancellationToken cancellationToken) {
			CancellationToken = cancellationToken;
		}

		public CancellationToken CancellationToken { get; }
		public TaskCompletionSource<IDisposable> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public bool Cancelled { get; set; }
		public bool Granted { get; set; }
	}
}
