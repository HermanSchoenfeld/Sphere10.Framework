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
using System.Runtime.CompilerServices;
using System.Threading;

namespace Sphere10.Framework;

public abstract class ExtendedCollectionBase<T> : IExtendedCollection<T> {

	protected volatile int Version;
	
	protected ExtendedCollectionBase() {
		Version = 0;
	}
	
	public abstract long Count { get; }


	public abstract bool IsReadOnly { get; }

	public abstract void Add(T item);

	public abstract void AddRange(IEnumerable<T> items);

	public abstract bool[] RemoveRange(IEnumerable<T> items);

	public abstract void Clear();

	public abstract bool Remove(T item);

	public abstract bool Contains(T item);

	public abstract IEnumerable<bool> ContainsRange(IEnumerable<T> items);

	public abstract void CopyTo(T[] array, int arrayIndex);

	public abstract IEnumerator<T> GetEnumerator();

	int ICollection<T>.Count {
		get {
			checked {
				return (int)Count;
			}
		}
	}

	int IReadOnlyCollection<T>.Count {
		get {
			checked {
				return (int)Count;
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return ((IEnumerable<T>)this).GetEnumerator();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected void CheckVersion(int enumeratedVersion) {
		if (Version != enumeratedVersion) 
			throw new InvalidOperationException("Collection was changed during enumeration");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected void UpdateVersion() => Interlocked.Increment(ref Version);

}

