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
public class StringTests {


	[Test]
	public void TrimStart_1() {
		Assert.That("alpha".TrimStart("alpha"), Is.EqualTo(""));
	}

	[Test]
	public void TrimStart_2() {
		Assert.That("alpha1".TrimStart("alpha"), Is.EqualTo("1"));
	}

	[Test]
	public void TrimStart_3() {
		Assert.That("1alpha2".TrimStart("alpha"), Is.EqualTo("1alpha2"));
	}


	[Test]
	public void TrimStart_4() {
		Assert.That("aLphA".TrimStart("alpha", false), Is.EqualTo(""));
	}

	[Test]
	public void TrimStart_5() {
		Assert.That("AlpHa1".TrimStart("alpha", false), Is.EqualTo("1"));
	}

	[Test]
	public void TrimStart_6() {
		Assert.That("1aLPha2".TrimStart("alpha", false), Is.EqualTo("1aLPha2"));
	}


	[Test]
	public void TrimEnd_1() {
		Assert.That("alpha".TrimEnd("alpha"), Is.EqualTo(""));
	}

	[Test]
	public void TrimEnd_2() {
		Assert.That("1alpha".TrimEnd("alpha"), Is.EqualTo("1"));
	}

	[Test]
	public void TrimEnd_3() {
		Assert.That("1aLpha2".TrimEnd("alpha"), Is.EqualTo("1aLpha2"));
	}

	[Test]
	public void TrimEnd_4() {
		Assert.That("alpHa".TrimEnd("alpha", false), Is.EqualTo(""));
	}

	[Test]
	public void TrimEnd_5() {
		Assert.That("1AlphA".TrimEnd("alpha", false), Is.EqualTo("1"));
	}

	[Test]
	public void TrimEnd_6() {
		Assert.That("1alpHa2".TrimEnd("alpha", false), Is.EqualTo("1alpHa2"));
	}
}

