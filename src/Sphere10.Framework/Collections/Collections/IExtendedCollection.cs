// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
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
}

