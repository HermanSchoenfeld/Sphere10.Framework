// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using NUnit.Framework;
namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class PackedEqualityComparerTests {

	[Test]
	public void TestPack() {
		IEqualityComparer<string> stringComparer = StringComparer.InvariantCultureIgnoreCase;
		Assert.That(()=> stringComparer.AsPacked(), Is.Not.Null);
	}

	[Test]
	public void TestUnpack() {
		IEqualityComparer<string> stringComparer = StringComparer.InvariantCultureIgnoreCase;
		var packedComparer = PackedEqualityComparer.Pack(stringComparer);

		var unpacked = packedComparer.Unpack<string>();

		Assert.That(unpacked, Is.SameAs(stringComparer));
	}

	[Test]
	public void TestUnpack_WrongTypeThrows() {
		IEqualityComparer<string> stringComparer = StringComparer.InvariantCultureIgnoreCase;
		var packedComparer = PackedEqualityComparer.Pack(stringComparer);
		Assert.That(() => packedComparer.Unpack<int>(), Throws.InvalidOperationException);
	}

	[Test]
	public void TestEquals_Consistency() {
		IEqualityComparer<string> stringComparer = StringComparer.InvariantCultureIgnoreCase;
		var packedComparer = stringComparer.AsPacked();
		Assert.That(packedComparer.Equals(null, null), Is.EqualTo(stringComparer.Equals(null, null)));
		Assert.That(packedComparer.Equals(null, "B"), Is.EqualTo(stringComparer.Equals(null, "B")));
		Assert.That(packedComparer.Equals("A", null), Is.EqualTo(stringComparer.Equals("A", null)));
		Assert.That(packedComparer.Equals("a", "b"), Is.EqualTo(stringComparer.Equals("a", "b")));
		Assert.That(packedComparer.Equals("a", "a"), Is.EqualTo(stringComparer.Equals("a", "a")));
		Assert.That(packedComparer.Equals("a", "A"), Is.EqualTo(stringComparer.Equals("a", "A")));
	}

	[Test]
	public void TestEquals_WrongTypeThrowsCastException_1() {
		IEqualityComparer<string> stringComparer = StringComparer.InvariantCultureIgnoreCase;
		var packedComparer = stringComparer.AsPacked();
		Assert.That(() => packedComparer.Equals(1, 2), Throws.InstanceOf<InvalidCastException>());
	}

	[Test]
	public void TestEquals_WrongTypeThrowsCastException_2() {
		IEqualityComparer<string> stringComparer = StringComparer.InvariantCultureIgnoreCase;
		var packedComparer = stringComparer.AsPacked();
		Assert.That(() => packedComparer.Equals("a", 2), Throws.InstanceOf<InvalidCastException>());
	}

	[Test]
	public void TestGetHashCode_Consistency() {
		IEqualityComparer<string> stringComparer = StringComparer.InvariantCultureIgnoreCase;
		var packedComparer = stringComparer.AsPacked();
		Assert.That(packedComparer.GetHashCode("a"), Is.EqualTo(stringComparer.GetHashCode("a")));
		Assert.That(packedComparer.GetHashCode("B"), Is.EqualTo(stringComparer.GetHashCode("B")));
		Assert.That(packedComparer.GetHashCode("c"), Is.EqualTo(stringComparer.GetHashCode("c")));
	}

	[Test]
	public void TestGetHashCode_WrongTypeThrowsCastException() {
		IEqualityComparer<string> stringComparer = StringComparer.InvariantCultureIgnoreCase;
		var packedComparer = stringComparer.AsPacked();
		Assert.That(() => packedComparer.GetHashCode(1), Throws.InstanceOf<InvalidCastException>());
	}

}

