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
public class ByteArrayComparerTests {

	[Test]
	public void TestNull() {
		Assert.That(ByteArrayComparer.Instance.Compare(null, null), Is.EqualTo(0));
	}

	[Test]
	public void TestEmpty() {
		Assert.That(ByteArrayComparer.Instance.Compare(new byte[0], new byte[0]), Is.EqualTo(0));
	}

	[Test]
	public void TestSame() {
		Assert.That(ByteArrayComparer.Instance.Compare(new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 3 }), Is.EqualTo(0));
	}

	[Test]
	public void TestSmaller() {
		Assert.That(ByteArrayComparer.Instance.Compare(new byte[] { 1, 2, 3 }, new byte[] { 3, 2, 1 }), Is.EqualTo(-1));
	}

	[Test]
	public void TestGreater() {
		Assert.That(ByteArrayComparer.Instance.Compare(new byte[] { 3, 2, 1 }, new byte[] { 1, 2, 3 }), Is.EqualTo(1));
	}
}

