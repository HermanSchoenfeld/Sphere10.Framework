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

/// <summary>Invokes its required finalizer synchronously; asynchronous disposal is unsupported.</summary>
public readonly struct SyncScope : IScope {
	private readonly Action _finalizer;

	public SyncScope(Action finalizer) {
		Guard.ArgumentNotNull(finalizer, nameof(finalizer));
		_finalizer = finalizer;
	}

	public void Dispose() => _finalizer();

	public ValueTask DisposeAsync() => ValueTask.FromException(new NotSupportedException("This scope requires synchronous disposal."));
}

/// <summary>Invokes its required finalizer with its item synchronously; asynchronous disposal is unsupported.</summary>
public readonly struct SyncScope<T> : IScope<T> {
	private readonly Action<T> _finalizer;

	public SyncScope(T item, Action<T> finalizer) {
		Guard.ArgumentNotNull(finalizer, nameof(finalizer));
		Item = item;
		_finalizer = finalizer;
	}

	public T Item { get; }

	public void Dispose() => _finalizer(Item);

	public ValueTask DisposeAsync() => ValueTask.FromException(new NotSupportedException("This scope requires synchronous disposal."));
}
