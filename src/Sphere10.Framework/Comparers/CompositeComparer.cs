// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework;

/// <summary>Applies ordered comparisons until a key distinguishes the compared values.</summary>
public class CompositeComparer<T> : IComparer<T> {
	private readonly IComparer<T> _primary;
	private readonly IComparer<T> _secondary;

	/// <summary>Creates a comparer with one effective ordering key.</summary>
	public CompositeComparer(IComparer<T> single)
		: this(single, IdempotentComparer<T>.Instance) {
	}

	/// <summary>Creates a comparer from an ordered sequence of comparison keys.</summary>
	public CompositeComparer(IEnumerable<IComparer<T>> comparers)
		: this(From(comparers)) {
	}

	/// <summary>Creates a comparer that uses the secondary key when the primary key is equal.</summary>
	public CompositeComparer(IComparer<T> primary, IComparer<T> secondary) {
		Guard.ArgumentNotNull(primary, nameof(primary));
		Guard.ArgumentNotNull(secondary, nameof(secondary));
		_primary = primary;
		_secondary = secondary;
	}

	/// <summary>Compares the primary key before considering the secondary key.</summary>
	public int Compare(T x, T y) {
		var result = _primary.Compare(x, y);
		return result == 0 ? _secondary.Compare(x, y) : result;
	}

	/// <summary>Composes keys in order, applying each subsequent comparison only when the preceding keys are equal.</summary>
	public static CompositeComparer<T> From(IEnumerable<IComparer<T>> comparers) {
		Guard.ArgumentNotNull(comparers, nameof(comparers));
		var comparerArray = comparers as IComparer<T>[] ?? comparers.ToArray();
		switch (comparerArray.Length) {
			case 0:
				throw new ArgumentException("Must have at least one comparer", nameof(comparers));
			case 1:
				return new CompositeComparer<T>(comparerArray[0]);
			case 2:
				return new CompositeComparer<T>(comparerArray[0], comparerArray[1]);
			default:
				return new CompositeComparer<T>(comparerArray.First(), From(comparerArray.Skip(1)));
		}
	}
}

