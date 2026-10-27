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

/// <summary>Awaits its required finalizer; synchronous disposal is unsupported.</summary>
public readonly struct AsyncScope : IScope {
	private readonly Func<ValueTask> _finalizer;

	public AsyncScope(Func<ValueTask> finalizer) {
		Guard.ArgumentNotNull(finalizer, nameof(finalizer));
		_finalizer = finalizer;
	}

	public void Dispose() => throw new NotSupportedException("This scope requires asynchronous disposal.");

	public async ValueTask DisposeAsync() {
		await _finalizer();
	}
}

/// <summary>Awaits its required finalizer with its item; synchronous disposal is unsupported.</summary>
public readonly struct AsyncScope<T> : IScope<T> {
	private readonly Func<T, ValueTask> _finalizer;

	public AsyncScope(T item, Func<T, ValueTask> finalizer) {
		Guard.ArgumentNotNull(finalizer, nameof(finalizer));
		Item = item;
		_finalizer = finalizer;
	}

	public T Item { get; }

	public void Dispose() => throw new NotSupportedException("This scope requires asynchronous disposal.");

	public async ValueTask DisposeAsync() {
		await _finalizer(Item);
	}
}
