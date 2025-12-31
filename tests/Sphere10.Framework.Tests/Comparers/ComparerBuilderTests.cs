// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using Sphere10.Framework.NUnit;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ComparerBuilderTests {

	[Test]
	public void TestComparerBuilder() {
		var comparer = 
			ComparerBuilder
				.For<TestClass>()
				.StartWith(x => x.Alpha)
				.ThenByDescending(x => x.Beta);

		var items = new TestClass[] {
			new() { Alpha = "B", Beta = 1 },
			new() { Alpha = "A", Beta = 1 },
			new() { Alpha = "B", Beta = 2 },
			new() { Alpha = "A", Beta = 2 },
		};

		var expected = new[] {
			items[3],
			items[1],
			items[2],
			items[0]
		};
		var actual = items.OrderBy(x => x, comparer).ToArray();
		ClassicAssert.AreEqual(expected, actual);
	}
	
	[Test]
	public void TestEqualityComparerBuilder() {
		var comparer = 
			EqualityComparerBuilder
				.For<TestClass>()
				.By(x => x.Alpha)
				.ThenBy(x => x.Beta);

		Assert.That(comparer.Equals(new() { Alpha = "A", Beta = 1 }, new() { Alpha = "A", Beta = 1 }));
		Assert.That(!comparer.Equals(new() { Alpha = "A", Beta = 1 }, new() { Alpha = "A", Beta = 2 }));
	}

	private class TestClass {
		public string Alpha { get; set; }

		public int Beta;
	}

}

