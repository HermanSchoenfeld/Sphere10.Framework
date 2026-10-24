// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>Creates compound keys with inferred component types.</summary>
public static class CompoundKey {

	public static CompoundKey<TKey1, TKey2> From<TKey1, TKey2>(TKey1 key1, TKey2 key2) => new(key1, key2);

	public static CompoundKey<TKey1, TKey2, TKey3> From<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3) => new(key1, key2, key3);

	public static CompoundKey<TKey1, TKey2, TKey3, TKey4> From<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4) => new(key1, key2, key3, key4);
}

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

	public override bool Equals(object obj) => obj is CompoundKey<TKey1, TKey2> other && Equals(other);

	public override int GetHashCode() => HashCode.Combine(Key1, Key2);

	public override string ToString() => $"({Key1}, {Key2})";

	public static bool operator ==(CompoundKey<TKey1, TKey2> left, CompoundKey<TKey1, TKey2> right) => left.Equals(right);

	public static bool operator !=(CompoundKey<TKey1, TKey2> left, CompoundKey<TKey1, TKey2> right) => !left.Equals(right);
}

/// <summary>A lightweight compound key composed of three ordered components.</summary>
public readonly struct CompoundKey<TKey1, TKey2, TKey3> : IEquatable<CompoundKey<TKey1, TKey2, TKey3>> {

	public CompoundKey(TKey1 key1, TKey2 key2, TKey3 key3) {
		Key1 = key1;
		Key2 = key2;
		Key3 = key3;
	}

	public TKey1 Key1 { get; }

	public TKey2 Key2 { get; }

	public TKey3 Key3 { get; }

	public bool Equals(CompoundKey<TKey1, TKey2, TKey3> other) =>
		EqualityComparer<TKey1>.Default.Equals(Key1, other.Key1) &&
		EqualityComparer<TKey2>.Default.Equals(Key2, other.Key2) &&
		EqualityComparer<TKey3>.Default.Equals(Key3, other.Key3);

	public override bool Equals(object obj) => obj is CompoundKey<TKey1, TKey2, TKey3> other && Equals(other);

	public override int GetHashCode() => HashCode.Combine(Key1, Key2, Key3);

	public override string ToString() => $"({Key1}, {Key2}, {Key3})";

	public static bool operator ==(CompoundKey<TKey1, TKey2, TKey3> left, CompoundKey<TKey1, TKey2, TKey3> right) => left.Equals(right);

	public static bool operator !=(CompoundKey<TKey1, TKey2, TKey3> left, CompoundKey<TKey1, TKey2, TKey3> right) => !left.Equals(right);
}

/// <summary>A lightweight compound key composed of four ordered components.</summary>
public readonly struct CompoundKey<TKey1, TKey2, TKey3, TKey4> : IEquatable<CompoundKey<TKey1, TKey2, TKey3, TKey4>> {

	public CompoundKey(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4) {
		Key1 = key1;
		Key2 = key2;
		Key3 = key3;
		Key4 = key4;
	}

	public TKey1 Key1 { get; }

	public TKey2 Key2 { get; }

	public TKey3 Key3 { get; }

	public TKey4 Key4 { get; }

	public bool Equals(CompoundKey<TKey1, TKey2, TKey3, TKey4> other) =>
		EqualityComparer<TKey1>.Default.Equals(Key1, other.Key1) &&
		EqualityComparer<TKey2>.Default.Equals(Key2, other.Key2) &&
		EqualityComparer<TKey3>.Default.Equals(Key3, other.Key3) &&
		EqualityComparer<TKey4>.Default.Equals(Key4, other.Key4);

	public override bool Equals(object obj) => obj is CompoundKey<TKey1, TKey2, TKey3, TKey4> other && Equals(other);

	public override int GetHashCode() => HashCode.Combine(Key1, Key2, Key3, Key4);

	public override string ToString() => $"({Key1}, {Key2}, {Key3}, {Key4})";

	public static bool operator ==(CompoundKey<TKey1, TKey2, TKey3, TKey4> left, CompoundKey<TKey1, TKey2, TKey3, TKey4> right) => left.Equals(right);

	public static bool operator !=(CompoundKey<TKey1, TKey2, TKey3, TKey4> left, CompoundKey<TKey1, TKey2, TKey3, TKey4> right) => !left.Equals(right);
}
