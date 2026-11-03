// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework;

public class BinaryHeap<T> : ICollection<T>, IHeap<T> where T : IComparable<T> {
	// Constants
	private const int DEFAULT_SIZE = 4;

	// Fields
	private T[] _data = new T[DEFAULT_SIZE];
	private bool _sorted;

	// Properties
	/// <summary>
	/// Gets the number of values in the heap. 
	/// </summary>
	public int Count { get; private set; } = 0;

	/// <summary>
	/// Gets or sets the capacity of the heap.
	/// </summary>
	public int Capacity {
		get {
			return field;
		}
		set {
			var previousCapacity = field;
			field = Math.Max(value, Count);
			if (field != previousCapacity) {
				T[] temp = new T[field];
				Array.Copy(_data, temp, Count);
				_data = temp;
			}
		}
	} = DEFAULT_SIZE;

	// Methods
	/// <summary>
	/// Creates a new binary heap.
	/// </summary>
	public BinaryHeap() {
	}

	private BinaryHeap(T[] data, int count) {
		Capacity = count;
		Count = count;
		Array.Copy(data, _data, count);
	}
	/// <summary>
	/// Gets the first value in the heap without removing it.
	/// </summary>
	/// <returns>The lowest value of type TValue.</returns>
	public T Peek() {
		return _data[0];
	}

	/// <summary>
	/// Removes all items from the heap.
	/// </summary>
	public void Clear() {
		Count = 0;
		_data = new T[Capacity];
	}
	/// <summary>
	/// Adds a key and value to the heap.
	/// </summary>
	/// <param name="item">The item to add to the heap.</param>
	public void Add(T item) {
		if (Count == Capacity) {
			Capacity *= 2;
		}
		_data[Count] = item;
		UpHeap();
		Count++;
	}

	/// <summary>
	/// Removes and returns the first item in the heap.
	/// </summary>
	/// <returns>The next value in the heap.</returns>
	public T Pop() {
		if (Count == 0) {
			throw new InvalidOperationException("Cannot remove item, heap is empty.");
		}
		T v = _data[0];
		Count--;
		_data[0] = _data[Count];
		_data[Count] = default(T); //Clears the Last Node
		DownHeap();
		return v;
	}

	//helper function that performs up-heap bubbling
	private void UpHeap() {
		_sorted = false;
		int p = Count;
		T item = _data[p];
		int par = Parent(p);
		while (par > -1 && item.CompareTo(_data[par]) < 0) {
			_data[p] = _data[par]; //Swap nodes
			p = par;
			par = Parent(p);
		}
		_data[p] = item;
	}

	//helper function that performs down-heap bubbling
	private void DownHeap() {
		_sorted = false;
		int n;
		int p = 0;
		T item = _data[p];
		while (true) {
			int ch1 = Child1(p);
			if (ch1 >= Count) break;
			int ch2 = Child2(p);
			if (ch2 >= Count) {
				n = ch1;
			} else {
				n = _data[ch1].CompareTo(_data[ch2]) < 0 ? ch1 : ch2;
			}
			if (item.CompareTo(_data[n]) > 0) {
				_data[p] = _data[n]; //Swap nodes
				p = n;
			} else {
				break;
			}
		}
		_data[p] = item;
	}

	private void EnsureSort() {
		if (_sorted) return;
		Array.Sort(_data, 0, Count);
		_sorted = true;
	}

	//helper function that calculates the parent of a node
	private static int Parent(int index) {
		return (index - 1) >> 1;
	}

	//helper function that calculates the first child of a node
	private static int Child1(int index) {
		return (index << 1) + 1;
	}

	//helper function that calculates the second child of a node
	private static int Child2(int index) {
		return (index << 1) + 2;
	}

	/// <summary>
	/// Creates a new instance of an identical binary heap.
	/// </summary>
	/// <returns>A BinaryHeap.</returns>
	public BinaryHeap<T> Copy() {
		return new BinaryHeap<T>(_data, Count);
	}

	/// <summary>
	/// Gets an enumerator for the binary heap.
	/// </summary>
	/// <returns>An IEnumerator of type T.</returns>
	public IEnumerator<T> GetEnumerator() {
		EnsureSort();
		for (int i = 0; i < Count; i++) {
			yield return _data[i];
		}
	}

	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}

	/// <summary>
	/// Checks to see if the binary heap contains the specified item.
	/// </summary>
	/// <param name="item">The item to search the binary heap for.</param>
	/// <returns>A boolean, true if binary heap contains item.</returns>
	public bool Contains(T item) {
		EnsureSort();
		return Array.BinarySearch<T>(_data, 0, Count, item) >= 0;
	}

	/// <summary>
	/// Copies the binary heap to an array at the specified index.
	/// </summary>
	/// <param name="array">One dimensional array that is the destination of the copied elements.</param>
	/// <param name="arrayIndex">The zero-based index at which copying begins.</param>
	public void CopyTo(T[] array, int arrayIndex) {
		EnsureSort();
		Array.Copy(_data, array, Count);
	}

	/// <summary>
	/// Gets whether or not the binary heap is readonly.
	/// </summary>
	public bool IsReadOnly => false;

	/// <summary>
	/// Removes an item from the binary heap. This utilizes the type T's Comparer and will not remove duplicates.
	/// </summary>
	/// <param name="item">The item to be removed.</param>
	/// <returns>Boolean true if the item was removed.</returns>
	public bool Remove(T item) {
		EnsureSort();
		int i = Array.BinarySearch<T>(_data, 0, Count, item);
		if (i < 0) return false;
		Array.Copy(_data, i + 1, _data, i, Count - i);
		_data[Count] = default(T);
		Count--;
		return true;
	}

}

