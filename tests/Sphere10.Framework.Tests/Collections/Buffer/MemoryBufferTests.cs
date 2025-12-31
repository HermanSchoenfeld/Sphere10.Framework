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
public class MemoryBufferTests {

	[Test]
	public void IntegrationTests(
		[Values(0, 3, 111)] int startCapacity,
		[Values(1, 391)] int growCapacity,
		[Values(71, 2177)] int maxCapacity) {
		var list = new MemoryBuffer(startCapacity, growCapacity, maxCapacity);
		AssertEx.ListIntegrationTest<byte>(list, maxCapacity, (rng, i) => rng.NextBytes(i), mutateFromEndOnly: true);
	}
}

