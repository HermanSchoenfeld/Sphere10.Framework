// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>Awaits an asynchronous finalizer or joins its complete operation for synchronous disposal.</summary>
public readonly struct TaskScope : IScope {
	private readonly Func<ValueTask> _finalizer;

	public TaskScope(Func<ValueTask> finalizer) {
		Guard.ArgumentNotNull(finalizer, nameof(finalizer));
		_finalizer = finalizer;
	}

	public void Dispose() {
		var finalizer = _finalizer;
		// Sync-over-async pattern
		Task.Run(() => finalizer().AsTask()).GetAwaiter().GetResult();
	}

	public async ValueTask DisposeAsync() => await _finalizer();
}

/// <summary>Awaits an asynchronous finalizer with its original item.</summary>
public readonly struct TaskScope<T> : IScope<T> {
	private readonly Func<T, ValueTask> _finalizer;

	public TaskScope(T item, Func<T, ValueTask> finalizer) {
		Guard.ArgumentNotNull(finalizer, nameof(finalizer));
		Item = item;
		_finalizer = finalizer;
	}

	public T Item { get; }

	public void Dispose() {
		var finalizer = _finalizer;
		var item = Item;
		// Sync-over-async pattern
		Task.Run(() => finalizer(item).AsTask()).GetAwaiter().GetResult();
	}

	public async ValueTask DisposeAsync() => await _finalizer(Item);
}

