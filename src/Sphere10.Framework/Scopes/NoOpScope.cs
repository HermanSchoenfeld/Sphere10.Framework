// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;

namespace Sphere10.Framework;

public readonly struct NoOpScope : IScope {
	public void Dispose() {
	}

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>Retains a borrowed item without taking ownership of its finalization.</summary>
public readonly struct NoOpScope<T> : IScope<T> {
	public NoOpScope(T item) {
		Item = item;
	}

	public T Item { get; }

	public void Dispose() {
	}

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

