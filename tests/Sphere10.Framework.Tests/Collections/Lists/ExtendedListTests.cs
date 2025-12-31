// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;
using Sphere10.Framework.NUnit;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ExtendedListTests {

	[Test]
	[Pairwise]
	public void IntegrationTests(
		[Values(0, 3, 111)] int startCapacity,
		[Values(19, 7, 13)] int growCapacity,
		[Values(10, 793, 2000)] int maxCapacity) {
		var list = new ExtendedList<int>(startCapacity, growCapacity, maxCapacity);
		AssertEx.ListIntegrationTest(list, (int)list.MaxCapacity, (rng, i) => rng.NextInts(i));
	}
}

