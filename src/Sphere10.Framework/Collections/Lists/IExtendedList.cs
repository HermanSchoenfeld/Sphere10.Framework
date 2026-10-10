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

public interface IExtendedList<T> : IExtendedCollection<T>, IReadOnlyExtendedList<T>, IWriteOnlyExtendedList<T>, IList<T> {
	new void Insert(long index, T item);

	new void RemoveAt(long index);

	#region Base Interface Bridges

	// Below interface methods resolve ambiguities between IReadOnlyExtendedList<T> and IList<T>
	new T this[int index] {
		get => this[(long)index];
		set => this[(long)index] = value;
	}

	new T this[long index] {
		get => Read(index);
		set => Update(index, value);
	}

	new int IndexOf(T item) => checked((int)IndexOfL(item));

	T IList<T>.this[int index] {
		get => this[index];
		set => this[index] = value;
	}

	T IReadOnlyList<T>.this[int index] => this[index];

	T IReadOnlyExtendedList<T>.this[long index] => this[index];

	T IWriteOnlyExtendedList<T>.this[int index] {
		set => this[index] = value;
	}

	T IWriteOnlyExtendedList<T>.this[long index] {
		set => this[index] = value;
	}

	int IList<T>.IndexOf(T item) => IndexOf(item);

	void IList<T>.Insert(int index, T item) => Insert((long)index, item);

	void IList<T>.RemoveAt(int index) => RemoveAt((long)index);

	void IWriteOnlyExtendedList<T>.Insert(long index, T item) => Insert(index, item);

	void IWriteOnlyExtendedList<T>.RemoveAt(long index) => RemoveAt(index);

	#endregion
}


public static class IExtendedListExtensions {

	public static void RemoveRange<T>(this IExtendedList<T> list, Range range) {
		var (offset, length) = range.GetOffsetAndLength(((IList<T>)list).Count);
		list.RemoveRange(offset, length);
	}

	/// <summary>
	/// Performs a binary search on the specified collection.
	/// </summary>
	/// <typeparam name="TItem">The type of the item.</typeparam>
	/// <param name="list">The list to be searched.</param>
	/// <param name="value">The value to search for.</param>
	/// <param name="comparer">The comparer that is used to compare the value with the list items.</param>
	/// <returns></returns>
	public static long BinarySearch<TItem>(this IExtendedList<TItem> list, TItem value, IComparer<TItem> comparer = null) {
		Guard.ArgumentNotNull(list, nameof(list));
		comparer ??= Comparer<TItem>.Default;
		return Tools.Collection.BinarySearch(list, value, 0, list.Count - 1, comparer.Compare);
	}
}

