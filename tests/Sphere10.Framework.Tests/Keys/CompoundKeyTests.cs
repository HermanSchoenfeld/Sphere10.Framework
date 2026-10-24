// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CompoundKeyTests {

	[Test]
	public void FactoriesInferComponentTypesAndPreserveValues() {
		var two = CompoundKey.From("account", 7);
		var three = CompoundKey.From("account", 7, 11L);
		var four = CompoundKey.From("account", 7, 11L, true);
		Assert.That(two.Key1, Is.EqualTo("account"));
		Assert.That(two.Key2, Is.EqualTo(7));
		Assert.That(three.Key1, Is.EqualTo("account"));
		Assert.That(three.Key2, Is.EqualTo(7));
		Assert.That(three.Key3, Is.EqualTo(11L));
		Assert.That(four.Key1, Is.EqualTo("account"));
		Assert.That(four.Key2, Is.EqualTo(7));
		Assert.That(four.Key3, Is.EqualTo(11L));
		Assert.That(four.Key4, Is.True);
		Assert.That(two.ToString(), Is.EqualTo("(account, 7)"));
		Assert.That(three.ToString(), Is.EqualTo("(account, 7, 11)"));
		Assert.That(four.ToString(), Is.EqualTo("(account, 7, 11, True)"));
	}

	[TestCase(2, 0)]
	[TestCase(2, 1)]
	[TestCase(3, 0)]
	[TestCase(3, 1)]
	[TestCase(3, 2)]
	[TestCase(4, 0)]
	[TestCase(4, 1)]
	[TestCase(4, 2)]
	[TestCase(4, 3)]
	public void EqualityAndDictionaryLookupIncludeEveryComponent(int arity, int changedComponent) {
		var components = new[] { 1, 2, 3, 4 };
		var original = CreateKey(arity, components);
		var equal = CreateKey(arity, components);
		components[changedComponent]++;
		var changed = CreateKey(arity, components);
		var dictionary = new Dictionary<object, string> { [original] = "original" };
		Assert.That(original.Equals(equal), Is.True);
		Assert.That(original.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
		Assert.That(original.Equals(changed), Is.False);
		Assert.That(original.Equals(null), Is.False);
		Assert.That(original.Equals("unrelated"), Is.False);
		Assert.That(dictionary[equal], Is.EqualTo("original"));
		Assert.That(dictionary.ContainsKey(changed), Is.False);
	}

	[Test]
	public void TypedEqualityAndOperatorsAgreeForEveryArity() {
		var two = CompoundKey.From(1, 2);
		var three = CompoundKey.From(1, 2, 3);
		var four = CompoundKey.From(1, 2, 3, 4);
		Assert.That(two.Equals(new CompoundKey<int, int>(1, 2)), Is.True);
		Assert.That(two == CompoundKey.From(1, 2), Is.True);
		Assert.That(two != CompoundKey.From(1, 3), Is.True);
		Assert.That(three.Equals(new CompoundKey<int, int, int>(1, 2, 3)), Is.True);
		Assert.That(three == CompoundKey.From(1, 2, 3), Is.True);
		Assert.That(three != CompoundKey.From(1, 2, 4), Is.True);
		Assert.That(four.Equals(new CompoundKey<int, int, int, int>(1, 2, 3, 4)), Is.True);
		Assert.That(four == CompoundKey.From(1, 2, 3, 4), Is.True);
		Assert.That(four != CompoundKey.From(1, 2, 3, 5), Is.True);
		Assert.That(((object)two).Equals(three), Is.False);
		Assert.That(((object)three).Equals(four), Is.False);
	}

	[Test]
	public void DefaultKeysAndNullComponentsUseDefaultComponentEquality() {
		Assert.That(default(CompoundKey<string, int>), Is.EqualTo(CompoundKey.From((string)null, 0)));
		Assert.That(default(CompoundKey<string, int, string>), Is.EqualTo(CompoundKey.From((string)null, 0, (string)null)));
		Assert.That(default(CompoundKey<string, int, string, int>), Is.EqualTo(CompoundKey.From((string)null, 0, (string)null, 0)));
		var component = new[] { 1, 2 };
		Assert.That(CompoundKey.From(component, 1, 2), Is.EqualTo(CompoundKey.From(component, 1, 2)));
		Assert.That(CompoundKey.From(component, 1, 2).Equals(CompoundKey.From(new[] { 1, 2 }, 1, 2)), Is.False);
		Assert.That(CompoundKey.From(1, 2, 3, component).Equals(CompoundKey.From(1, 2, 3, new[] { 1, 2 })), Is.False);
	}

	[TestCase(2, 0)]
	[TestCase(2, 1)]
	[TestCase(3, 0)]
	[TestCase(3, 1)]
	[TestCase(3, 2)]
	[TestCase(4, 0)]
	[TestCase(4, 1)]
	[TestCase(4, 2)]
	[TestCase(4, 3)]
	public void ComparersUseTheFirstUnequalComponent(int arity, int firstUnequalComponent) {
		var left = new[] { 1, 1, 1, 1 };
		var right = new[] { 1, 1, 1, 1 };
		right[firstUnequalComponent] = 2;
		for (var index = firstUnequalComponent + 1; index < arity; index++)
			left[index] = 9;
		Assert.That(CompareKeys(arity, left, right), Is.LessThan(0));
		Assert.That(CompareKeys(arity, right, left), Is.GreaterThan(0));
		Assert.That(CompareKeys(arity, left, left), Is.Zero);
	}

	[Test]
	public void CustomComparersDefineComponentOrderingIncludingNulls() {
		var descending = Comparer<int>.Create((left, right) => right.CompareTo(left));
		var three = new CompoundKeyComparer<string, int, string>(StringComparer.OrdinalIgnoreCase, descending, StringComparer.Ordinal);
		var four = new CompoundKeyComparer<string, int, string, int>(StringComparer.OrdinalIgnoreCase, descending, StringComparer.Ordinal, descending);
		Assert.That(three.Compare(CompoundKey.From("A", 9, "z"), CompoundKey.From("a", 2, "a")), Is.LessThan(0));
		Assert.That(three.Compare(CompoundKey.From("a", 2, (string)null), CompoundKey.From("A", 2, "a")), Is.LessThan(0));
		Assert.That(four.Compare(CompoundKey.From("A", 2, "a", 9), CompoundKey.From("a", 2, "a", 2)), Is.LessThan(0));
		Assert.That(four.Compare(CompoundKey.From((string)null, 2, "a", 9), CompoundKey.From("a", 2, "a", 2)), Is.LessThan(0));
	}

	[TestCase(2, 0)]
	[TestCase(2, 1)]
	[TestCase(3, 0)]
	[TestCase(3, 1)]
	[TestCase(3, 2)]
	[TestCase(4, 0)]
	[TestCase(4, 1)]
	[TestCase(4, 2)]
	[TestCase(4, 3)]
	public void ComparersRejectMissingComponentComparer(int arity, int component) {
		var comparers = new[] { Comparer<int>.Default, Comparer<int>.Default, Comparer<int>.Default, Comparer<int>.Default };
		comparers[component] = null;
		Assert.That(() => CreateComparer(arity, comparers), Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo($"key{component + 1}Comparer"));
	}

	private static object CreateKey(int arity, int[] components) => arity switch {
		2 => CompoundKey.From(components[0], components[1]),
		3 => CompoundKey.From(components[0], components[1], components[2]),
		_ => CompoundKey.From(components[0], components[1], components[2], components[3])
	};

	private static int CompareKeys(int arity, int[] left, int[] right) => arity switch {
		2 => new CompoundKeyComparer<int, int>(Comparer<int>.Default, Comparer<int>.Default).Compare(
			CompoundKey.From(left[0], left[1]), CompoundKey.From(right[0], right[1])),
		3 => new CompoundKeyComparer<int, int, int>(Comparer<int>.Default, Comparer<int>.Default, Comparer<int>.Default).Compare(
			CompoundKey.From(left[0], left[1], left[2]), CompoundKey.From(right[0], right[1], right[2])),
		_ => new CompoundKeyComparer<int, int, int, int>(Comparer<int>.Default, Comparer<int>.Default, Comparer<int>.Default, Comparer<int>.Default).Compare(
			CompoundKey.From(left[0], left[1], left[2], left[3]), CompoundKey.From(right[0], right[1], right[2], right[3]))
	};

	private static object CreateComparer(int arity, IComparer<int>[] comparers) => arity switch {
		2 => new CompoundKeyComparer<int, int>(comparers[0], comparers[1]),
		3 => new CompoundKeyComparer<int, int, int>(comparers[0], comparers[1], comparers[2]),
		_ => new CompoundKeyComparer<int, int, int, int>(comparers[0], comparers[1], comparers[2], comparers[3])
	};
}
