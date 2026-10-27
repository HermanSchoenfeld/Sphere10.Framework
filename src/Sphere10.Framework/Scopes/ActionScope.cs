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

/// <summary>Runs a synchronous finalizer directly or schedules it for asynchronous disposal.</summary>
public readonly struct ActionScope : IScope {
	private readonly Action _finalizer;

	public ActionScope(Action finalizer) {
		Guard.ArgumentNotNull(finalizer, nameof(finalizer));
		_finalizer = finalizer;
	}

	public void Dispose() => _finalizer();

	public ValueTask DisposeAsync() => new(Task.Run(_finalizer));
}

/// <summary>Runs a synchronous finalizer with its original item.</summary>
public readonly struct ActionScope<T> : IScope<T> {
	private readonly Action<T> _finalizer;

	public ActionScope(T item, Action<T> finalizer) {
		Guard.ArgumentNotNull(finalizer, nameof(finalizer));
		Item = item;
		_finalizer = finalizer;
	}

	public T Item { get; }

	public void Dispose() => _finalizer(Item);

	public ValueTask DisposeAsync() {
		var finalizer = _finalizer;
		var item = Item;
		return new ValueTask(Task.Run(() => finalizer(item)));
	}
}

