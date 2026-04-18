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
public class PackedComparerTests {

	[Test]
	public void TestPack() {
		IComparer<string> stringComparer = StringComparer.InvariantCultureIgnoreCase;
		Assert.That(()=> stringComparer.AsPacked(), Is.Not.Null);
	}

	[Test]
	public void TestUnpack() {
		IComparer<string> stringComparer = StringComparer.InvariantCultureIgnoreCase;
		var packedComparer = PackedComparer.Pack(stringComparer);

		var unpacked = packedComparer.Unpack<string>();

		Assert.That(unpacked, Is.SameAs(stringComparer));
	}

	[Test]
	public void TestUnpack_WrongTypeThrows() {
		IComparer<string> stringComparer = StringComparer.InvariantCultureIgnoreCase;
		var packedComparer = PackedComparer.Pack(stringComparer);
		Assert.That(() => packedComparer.Unpack<int>(), Throws.InvalidOperationException);
	}

	[Test]
	public void TestCompare_Consistency() {
		IComparer<string> stringComparer = StringComparer.InvariantCultureIgnoreCase;
		var packedComparer = stringComparer.AsPacked();
		Assert.That(packedComparer.Compare(null, null), Is.EqualTo(stringComparer.Compare(null, null)));
		Assert.That(packedComparer.Compare(null, "B"), Is.EqualTo(stringComparer.Compare(null, "B")));
		Assert.That(packedComparer.Compare("A", null), Is.EqualTo(stringComparer.Compare("A", null)));
		Assert.That(packedComparer.Compare("a", "b"), Is.EqualTo(stringComparer.Compare("a", "b")));
		Assert.That(packedComparer.Compare("a", "a"), Is.EqualTo(stringComparer.Compare("a", "a")));
		Assert.That(packedComparer.Compare("a", "A"), Is.EqualTo(stringComparer.Compare("a", "A")));
	}

	[Test]
	public void TestCompareWrongTypeThrowsCastException_1() {
		IComparer<string> stringComparer = StringComparer.InvariantCultureIgnoreCase;
		var packedComparer = stringComparer.AsPacked();
		Assert.That(() => packedComparer.Compare(1, 2), Throws.InstanceOf<InvalidCastException>());
	}

	[Test]
	public void TestCompareWrongTypeThrowsCastException_2() {
		IComparer<string> stringComparer = StringComparer.InvariantCultureIgnoreCase;
		var packedComparer = stringComparer.AsPacked();
		Assert.That(() => packedComparer.Compare("a", 2), Throws.InstanceOf<InvalidCastException>());
	}

}

