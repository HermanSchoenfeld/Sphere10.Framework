// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Buffers;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>Owns an array rental and returns it to its originating pool.</summary>
/// <remarks>Keep one concrete owner and dispose it once. The rented array may be larger than Length;
/// Span exposes only the requested logical range. Pool misses and asynchronous scheduling can allocate.</remarks>
public readonly struct BorrowArrayScope<T> : IScope<T[]> {

	private readonly ArrayPool<T> _pool;
	private readonly bool _clearArray;

	public BorrowArrayScope(int minimumLength, bool clearArray = false)
		: this(ArrayPool<T>.Shared, minimumLength, clearArray) {
	}

	public BorrowArrayScope(ArrayPool<T> pool, int minimumLength, bool clearArray = false) {
		Guard.ArgumentNotNull(pool, nameof(pool));
		Guard.ArgumentGTE(minimumLength, 0, nameof(minimumLength));
		_pool = pool;
		_clearArray = clearArray;
		Length = minimumLength;
		Item = pool.Rent(minimumLength);
	}

	public T[] Item { get; }

	public int Length { get; }

	public Span<T> Span => Item.AsSpan(0, Length);

	public void Dispose() => _pool.Return(Item, _clearArray);

	public ValueTask DisposeAsync() => ReturnAsync(_pool, Item, _clearArray);

	private static ValueTask ReturnAsync(ArrayPool<T> pool, T[] item, bool clearArray)
		=> new(Task.Run(() => pool.Return(item, clearArray)));
}
