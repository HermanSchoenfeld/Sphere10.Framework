// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework;

/// <summary>A synchronized oldest-first observation history bounded independently by item count and caller-defined retained cost.</summary>
/// <remarks>Oversized observations are rejected; accepted observations evict oldest items until both bounds fit. Snapshots borrow item references.</remarks>
public sealed class BoundedHistory<T> : SynchronizedObject, IReadOnlyCollection<T> {
	private readonly QueueAdapter<(T Item, long Cost)> _items = new();
	private readonly Func<T, long> _cost;
	private long _retainedCost;
	private long _received;
	private long _discarded;

	public BoundedHistory(int maximumItems, long maximumCost = long.MaxValue, Func<T, long> cost = null) {
		Guard.ArgumentGT(maximumItems, 0, nameof(maximumItems));
		Guard.ArgumentGT(maximumCost, 0L, nameof(maximumCost));
		MaximumItems = maximumItems;
		MaximumCost = maximumCost;
		_cost = cost ?? (_ => 1);
	}

	public int MaximumItems { get; }
	public long MaximumCost { get; }
	public int Count { get { using var scope = EnterReadScope(); return _items.Count; } }
	public long RetainedCost { get { using var scope = EnterReadScope(); return _retainedCost; } }
	public long Received { get { using var scope = EnterReadScope(); return _received; } }
	public long Discarded { get { using var scope = EnterReadScope(); return _discarded; } }

	public bool TryAdd(T item) {
		var cost = _cost(item);
		Guard.Argument(cost >= 0, nameof(item), "History item cost cannot be negative.");
		using var scope = EnterWriteScope();
		_received++;
		if (cost > MaximumCost) { _discarded++; return false; }
		while (_items.Count > 0 && (_items.Count >= MaximumItems || cost > MaximumCost - _retainedCost)) {
			_retainedCost -= _items.Dequeue().Cost;
			_discarded++;
		}
		_items.Enqueue((item, cost));
		_retainedCost += cost;
		return true;
	}

	public T[] Snapshot() {
		using var scope = EnterReadScope();
		return _items.Select(item => item.Item).ToArray();
	}

	public void Clear(bool resetCounters = true) {
		using var scope = EnterWriteScope();
		_items.Clear();
		_retainedCost = 0;
		if (resetCounters)
			_received = _discarded = 0;
	}

	public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Snapshot()).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
