// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;
namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ByteArrayEqualityComparerTests {

	[Test]
	public void TestNull() {
		Assert.That(ByteArrayEqualityComparer.Instance.Equals(null, null), Is.EqualTo(true));
	}

	[Test]
	public void TestEmpty() {
		Assert.That(ByteArrayEqualityComparer.Instance.Equals(new byte[0], new byte[0]), Is.EqualTo(true));
	}

	[Test]
	public void TestSame() {
		Assert.That(ByteArrayEqualityComparer.Instance.Equals(new byte[] { 1, 2 }, new byte[] { 1, 2 }), Is.EqualTo(true));
	}

	[Test]
	public void TestDiff() {
		Assert.That(ByteArrayEqualityComparer.Instance.Equals(new byte[] { 1, 2 }, new byte[] { 2, 1 }), Is.EqualTo(false));
	}

	[Test]
	public void TestDiffLonger_1() {
		Assert.That(ByteArrayEqualityComparer.Instance.Equals(new byte[] { 1, 2, 3 }, new byte[] { 2, 1 }), Is.EqualTo(false));
	}

	[Test]
	public void TestDiffLonger_2() {
		Assert.That(ByteArrayEqualityComparer.Instance.Equals(new byte[] { 1, 2 }, new byte[] { 2, 1, 3 }), Is.EqualTo(false));
	}

}

