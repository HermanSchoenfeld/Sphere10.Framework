// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Sphere10.Framework;

/// <summary>
/// Resizable list backed by a contiguous array with long indexing support.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
/// <remarks>
/// Used widely in tests for capacity growth and range operations.
/// See tests/Sphere10.Framework.Tests/Collections/Lists/ExtendedListTests.cs.
/// </remarks>
public class ExtendedList<T> : RangedListBase<T> {
	private const long DefaultCapacityGrowthSize = 4096;
	private T[] _internalArray;
	private long _length;
	private readonly IEqualityComparer<T> _comparer;


	/// <summary>
	/// Initializes an empty list with default growth settings.
	/// </summary>
	/// <param name="comparer">The comparer to use for item equality.</param>
	public ExtendedList(IEqualityComparer<T> comparer = null)
		: this(DefaultCapacityGrowthSize, comparer) {
	}

	/// <summary>
	/// Initializes an empty list with an initial capacity.
	/// </summary>
	/// <param name="capacity">The initial capacity.</param>
	/// <param name="comparer">The comparer to use for item equality.</param>
	public ExtendedList(long capacity, IEqualityComparer<T> comparer = null)
		: this(capacity, long.MaxValue, comparer) {
	}

	/// <summary>
	/// Initializes an empty list with a capacity and a maximum capacity.
	/// </summary>
	/// <param name="capacity">The initial capacity.</param>
	/// <param name="maxCapacity">The maximum capacity.</param>
	/// <param name="comparer">The comparer to use for item equality.</param>
	public ExtendedList(long capacity, long maxCapacity, IEqualityComparer<T> comparer = null)
		: this(capacity, DefaultCapacityGrowthSize, maxCapacity,	comparer) {
	}

	/// <summary>
	/// Initializes an empty list with capacity, growth size, and maximum capacity.
	/// </summary>
	/// <param name="capacity">The initial capacity.</param>
	/// <param name="capacityGrowthSize">The growth size for capacity increments.</param>
	/// <param name="maxCapacity">The maximum capacity.</param>
	/// <param name="comparer">The comparer to use for item equality.</param>
	public ExtendedList(long capacity, long capacityGrowthSize, long maxCapacity, IEqualityComparer<T> comparer = null)
		: this(new T[capacity], 0, capacityGrowthSize, maxCapacity, comparer) {
	}

	/// <summary>
	/// Initializes the list from an existing array.
	/// </summary>
	/// <param name="sourceArray">The source array.</param>
	/// <param name="comparer">The comparer to use for item equality.</param>
	public ExtendedList(T[] sourceArray, IEqualityComparer<T> comparer = null)
		: this(sourceArray, sourceArray.LongLength, 0L, sourceArray.LongLength, comparer) {
	}

	private ExtendedList(T[] internalArray, long currentLogicalSize, long capacityGrowthSize, long maxCapacity, IEqualityComparer<T> comparer = null) {
		Guard.ArgumentNotNull(internalArray, nameof(internalArray));
		Guard.ArgumentInRange(capacityGrowthSize, 0, long.MaxValue, nameof(capacityGrowthSize));
		Guard.ArgumentInRange(maxCapacity, currentLogicalSize, long.MaxValue, nameof(maxCapacity));
		Guard.ArgumentInRange(currentLogicalSize, 0, internalArray.LongLength, nameof(currentLogicalSize));

		_internalArray = internalArray;
		_length = currentLogicalSize;
		CapacityGrowthSize = capacityGrowthSize;
		MaxCapacity = maxCapacity;
		_comparer = comparer ?? EqualityComparer<T>.Default;
	}

	/// <summary>
	/// Gets the growth increment used when resizing.
	/// </summary>
	public long CapacityGrowthSize { get; }

	/// <summary>
	/// Gets the maximum capacity allowed for this list.
	/// </summary>
	public long MaxCapacity { get; }

	/// <summary>
	/// Gets the number of elements in the list.
	/// </summary>
	public override long Count => _length;

	/// <summary>
	/// Always false for this mutable list implementation.
	/// </summary>
	public override bool IsReadOnly => false;

	/// <summary>
	/// Returns the index of each item, or -1 if not found.
	/// </summary>
	/// <param name="items">The items to locate.</param>
	public override long[] IndexOfRange(IEnumerable<T> items) {
		Guard.ArgumentNotNull(items, nameof(items));
		var itemsArr = items as T[] ?? items.ToArray();
		var results = new long[itemsArr.Length];
		for (var i = 0; i < itemsArr.Length; i++) {
			var itemIndex = -1L;
			for (var j = 0; j < _length; j++) {
				if (_comparer.Equals(itemsArr[i], _internalArray[j])) {
					itemIndex = j;
					break;
				}
			}
			results[i] = itemIndex;
		}
		return results;
	}

	/// <summary>
	/// Reads a contiguous range of items from the list.
	/// </summary>
	/// <param name="index">The starting index.</param>
	/// <param name="count">The number of items to read.</param>
	public override IEnumerable<T> ReadRange(long index, long count) {
		CheckRange(index, count);

		// optimize for single read
		if (count == 1)
			return new[] { _internalArray[index] };

		var endIndex = Math.Min(index + count - 1, _length - 1);
		var readCount = endIndex - index + 1;
		if (readCount <= 0)
			return Array.Empty<T>();
		var readResult = new T[readCount];
		Array.Copy(_internalArray, index, readResult, 0, readCount);
		return readResult;
	}

	/// <summary>
	/// Copies a range directly into caller-owned storage without allocating a range snapshot.
	/// </summary>
	public void ReadRange(long index, Span<T> destination) {
		CheckRange(index, destination.Length);
		_internalArray.AsSpan(checked((int)index), destination.Length).CopyTo(destination);
	}

	/// <summary>
	/// Appends a sequence of items to the list.
	/// </summary>
	/// <param name="items">The items to add.</param>
	public override void AddRange(IEnumerable<T> items) {
		// TODO: needs updating to support multiple _internalArray's and 2^64-1 addressable items
		Guard.ArgumentNotNull(items, nameof(items));
		AddRange(GetContiguousItems(items));
	}

	// Keep array calls on the virtual enumerable contract for existing subclasses.
	public void AddRange(T[] items) => AddRange((IEnumerable<T>)items);

	/// <summary>
	/// Appends caller-owned contiguous items without materializing an enumerable.
	/// </summary>
	public void AddRange(ReadOnlySpan<T> items) {
		if (items.IsEmpty)
			return;
		GrowSpaceIfRequired(items.Length);
		UpdateVersion();
		items.CopyTo(_internalArray.AsSpan(checked((int)_length), items.Length));
		_length += items.Length;
	}

	/// <summary>
	/// Replaces a range of items starting at the specified index.
	/// </summary>
	/// <param name="index">The starting index.</param>
	/// <param name="items">The items to write.</param>
	public override void UpdateRange(long index, IEnumerable<T> items) {
		Guard.ArgumentNotNull(items, nameof(items));
		UpdateRange(index, GetContiguousItems(items));
	}

	public void UpdateRange(long index, T[] items) => UpdateRange(index, (IEnumerable<T>)items);

	/// <summary>
	/// Replaces a range directly from caller-owned contiguous storage.
	/// </summary>
	public void UpdateRange(long index, ReadOnlySpan<T> items) {
		CheckRange(index, items.Length);
		if (items.IsEmpty)
			return;
		UpdateVersion();
		items.CopyTo(_internalArray.AsSpan(checked((int)index), items.Length));
	}

	/// <summary>
	/// Inserts a range of items at the specified index.
	/// </summary>
	/// <param name="index">The insert index.</param>
	/// <param name="items">The items to insert.</param>
	public override void InsertRange(long index, IEnumerable<T> items) {
		Guard.ArgumentNotNull(items, nameof(items));
		Guard.ArgumentInRange(index, 0, Math.Max(0, _length), nameof(index));
		InsertRange(index, GetContiguousItems(items));
	}

	public void InsertRange(long index, T[] items) => InsertRange(index, (IEnumerable<T>)items);

	/// <summary>
	/// Inserts caller-owned contiguous items, preserving overlapping source contents.
	/// </summary>
	public void InsertRange(long index, ReadOnlySpan<T> items) {
		Guard.ArgumentInRange(index, 0, Math.Max(0, _length), nameof(index));
		GrowSpaceIfRequired(items.Length);
		if (items.Overlaps(_internalArray.AsSpan()))
			items = items.ToArray();
		UpdateVersion();

		// Use Array.Copy to move original items, since handles overlap scenarios
		Array.Copy(_internalArray, index, _internalArray, index + items.Length, _length - index);
		items.CopyTo(_internalArray.AsSpan(checked((int)index), items.Length));
		_length += items.Length;
	}

	/// <summary>
	/// Removes a range of items starting at the specified index.
	/// </summary>
	/// <param name="index">The starting index.</param>
	/// <param name="count">The number of items to remove.</param>
	public override void RemoveRange(long index, long count) {
		CheckRange(index, count);

		if (count == 0)
			return;

		// Use Array.Copy to move original items, since handles overlap scenarios
		Array.Copy(_internalArray, index + count, _internalArray, index, (_length - (index + count)));
		_length -= count;
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
			Array.Clear(_internalArray, checked((int)_length), checked((int)count));
		UpdateVersion();
	}

	private static ReadOnlySpan<T> GetContiguousItems(IEnumerable<T> items) {
		// Borrow known contiguous sources only for the duration of this synchronous operation.
		if (items is T[] array)
			return array;
		if (items is List<T> list)
			return CollectionsMarshal.AsSpan(list);
		return items.ToArray();
	}

	private void GrowSpaceIfRequired(long newItems) {
		var newCapacity = _length + newItems;

		// check if don't need to grow capacity
		if (newCapacity <= _internalArray.LongLength)
			return;

		// check if growth exceeds max
		if (newCapacity > MaxCapacity || CapacityGrowthSize == 0)
			throw new ArgumentException("Insufficient capacity");

		// determine how many new bytes are actually needed
		var actualNewBytes = newItems - (_internalArray.LongLength - _length);
		Debug.Assert(actualNewBytes > 0);

		// calc the new capacity (grows in blocks)
		var remainingGrowthCapacity = MaxCapacity - _internalArray.LongLength;
		var growAmount = (int)Math.Min(Math.Ceiling(actualNewBytes / (double)CapacityGrowthSize) * CapacityGrowthSize, remainingGrowthCapacity);
		Debug.Assert(_internalArray.LongLength + growAmount <= MaxCapacity);

		// Resize store
		Tools.Collection.ResizeArray(ref _internalArray, _internalArray.LongLength + growAmount);
	}

}

