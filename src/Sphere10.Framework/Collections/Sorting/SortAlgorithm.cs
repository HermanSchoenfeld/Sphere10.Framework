// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework;

public abstract class SortAlgorithm<T> {
	protected IComparer<T> Comparer;

	protected SortAlgorithm(IComparer<T> comparer = null) {
		Comparer = comparer ?? Comparer<T>.Default;
	}

	public abstract void Sort(IExtendedList<T> list);

	protected virtual void Swap(IExtendedList<T> list, long leftIdx, long rightIdx) {
		(list[leftIdx], list[rightIdx]) = (list[rightIdx], list[leftIdx]);
	}
}

