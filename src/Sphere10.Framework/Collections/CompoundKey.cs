// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>
/// A lightweight generic compound key composed of two ordered components.
/// Useful for building composite keys in B-tree structures.
/// </summary>
public readonly struct CompoundKey<TKey1, TKey2> : IEquatable<CompoundKey<TKey1, TKey2>> {

	public CompoundKey(TKey1 key1, TKey2 key2) {
		Key1 = key1;
		Key2 = key2;
	}

	public TKey1 Key1 { get; }

	public TKey2 Key2 { get; }

	public bool Equals(CompoundKey<TKey1, TKey2> other) =>
		EqualityComparer<TKey1>.Default.Equals(Key1, other.Key1) &&
		EqualityComparer<TKey2>.Default.Equals(Key2, other.Key2);

	public override bool Equals(object obj) => obj is CompoundKey<TKey1, TKey2> Other && Equals(Other);

	public override int GetHashCode() => HashCode.Combine(Key1, Key2);

	public override string ToString() => $"({Key1}, {Key2})";

	public static bool operator ==(CompoundKey<TKey1, TKey2> left, CompoundKey<TKey1, TKey2> right) => left.Equals(right);

	public static bool operator !=(CompoundKey<TKey1, TKey2> left, CompoundKey<TKey1, TKey2> right) => !left.Equals(right);
}
