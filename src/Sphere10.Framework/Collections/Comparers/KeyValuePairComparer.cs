// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>
/// Compares <see cref="KeyValuePair{TKey, TValue}"/> instances by comparing the key first
/// and, when keys are equal, comparing the value as a tiebreaker.
/// </summary>
public class KeyValuePairComparer<TKey, TValue> : IComparer<KeyValuePair<TKey, TValue>> {
	private readonly IComparer<TKey> _keyComparer;
	private readonly IComparer<TValue> _valueComparer;

	public KeyValuePairComparer(IComparer<TKey> keyComparer, IComparer<TValue> valueComparer) {
		Guard.ArgumentNotNull(keyComparer, nameof(keyComparer));
		Guard.ArgumentNotNull(valueComparer, nameof(valueComparer));
		_keyComparer = keyComparer;
		_valueComparer = valueComparer;
	}

	public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) {
		var KeyCmp = _keyComparer.Compare(x.Key, y.Key);
		if (KeyCmp != 0)
			return KeyCmp;
		return _valueComparer.Compare(x.Value, y.Value);
	}
}
