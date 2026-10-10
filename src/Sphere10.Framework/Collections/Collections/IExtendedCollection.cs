// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework; 

public interface IExtendedCollection<T> : ICollection<T>, IReadOnlyExtendedCollection<T>, IWriteOnlyExtendedCollection<T> {
	new long Count { get; }
	new void Add(T item);
	new void Clear();
	new bool Contains(T item);
	new void CopyTo(T[] array, int arrayIndex);
	new bool Remove(T item);

	#region Base Interface Bridges

	int ICollection<T>.Count => checked((int)Count);

	int IReadOnlyCollection<T>.Count => checked((int)Count);

	void ICollection<T>.Add(T item) => Add(item);

	void ICollection<T>.Clear() => Clear();

	bool ICollection<T>.Contains(T item) => Contains(item);

	void ICollection<T>.CopyTo(T[] array, int arrayIndex) => CopyTo(array, arrayIndex);

	bool ICollection<T>.Remove(T item) => Remove(item);

	void IReadOnlyExtendedCollection<T>.CopyTo(T[] array, int arrayIndex) => CopyTo(array, arrayIndex);

	bool IReadOnlyExtendedCollection<T>.Contains(T item) => Contains(item);

	void IWriteOnlyExtendedCollection<T>.Add(T item) => Add(item);

	bool IWriteOnlyExtendedCollection<T>.Remove(T item) => Remove(item);

	void IWriteOnlyExtendedCollection<T>.Clear() => Clear();

	#endregion
}

