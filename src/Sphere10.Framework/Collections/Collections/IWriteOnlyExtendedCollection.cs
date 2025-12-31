// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework;

public interface IWriteOnlyExtendedCollection<in T> {
	void Add(T item);

	void AddRange(IEnumerable<T> items);

	bool Remove(T item);

	IEnumerable<bool> RemoveRange(IEnumerable<T> items);

	void Clear();
}


public static class IWriteOnlyExtendedCollectionExtensions {

	public static void AddRange<T>(this IWriteOnlyExtendedCollection<T> collection, params T[] items) {
		collection.AddRange((IEnumerable<T>)items);
	}
}

