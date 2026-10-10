// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework;

public interface IWriteOnlyExtendedList<in T> : IWriteOnlyExtendedCollection<T> {
	void Update(long index, T item);

	void UpdateRange(long index, IEnumerable<T> items);

	void Insert(long index, T item);

	void InsertRange(long index, IEnumerable<T> items);

	void RemoveAt(long index);

	void RemoveRange(long index, long count);

	#region Base Interface Bridges

	T this[int index] {
		set => this[(long)index] = value;
	}

	T this[long index] {
		set => Update(index, value);
	}

	#endregion
}

