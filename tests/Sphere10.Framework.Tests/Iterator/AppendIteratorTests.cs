// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using NUnit.Framework;
namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class AppendIteratorTests {

	[Test]
	public void TestUnionAntiPattern() {
		var data = new[] { "one" };
		var union = data.Union("one");
		var result = union.ToArray();
		Assert.That(result.Length, Is.EqualTo(1));
		Assert.That(result[0], Is.EqualTo("one"));
	}

	[Test]
	public void TestConcat() {
		var data = new[] { "one" };
		var union = data.Concat("one");
		var result = union.ToArray();
		Assert.That(result.Length, Is.EqualTo(2));
		Assert.That(result[0], Is.EqualTo("one"));
		Assert.That(result[0], Is.EqualTo("one"));
	}


}

