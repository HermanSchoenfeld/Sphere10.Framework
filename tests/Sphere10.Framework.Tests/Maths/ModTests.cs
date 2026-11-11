// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ModTests {
	[TestCase(0, 5, 0)]
	[TestCase(7, 5, 2)]
	[TestCase(-1, 5, 4)]
	[TestCase(-5, 5, 0)]
	[TestCase(-6, 5, 4)]
	[TestCase(int.MinValue, 3, 1)]
	public void IntRemaindersStayWithinZeroAndTheModulus(int value, int modulus, int expected) => Assert.That(Tools.Maths.Mod(value, modulus), Is.EqualTo(expected));

	[TestCase(-1L, 21L, 20L)]
	[TestCase(long.MinValue, 7L, 6L)]
	[TestCase(43L, 21L, 1L)]
	public void LongRemaindersStayWithinZeroAndTheModulus(long value, long modulus, long expected) => Assert.That(Tools.Maths.Mod(value, modulus), Is.EqualTo(expected));

	[Test]
	public void NonPositiveModuliAreRejected() {
		Assert.That(() => Tools.Maths.Mod(1, 0), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => Tools.Maths.Mod(1L, -2L), Throws.InstanceOf<ArgumentException>());
	}
}
