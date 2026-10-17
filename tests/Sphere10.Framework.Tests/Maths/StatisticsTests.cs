// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class StatisticsTests {
	[TestCase(65536)]
	[TestCase(100000)]
	public void LongRunningStatisticsCannotOverflowPopulationVarianceDenominator(int sampleCount) {
		var statistics = new Statistics(0);
		for (var index = 0; index < sampleCount; index++)
			statistics.AddDatum(index % 2 == 0 ? 20 : 40);
		Assert.That(statistics.PopulationVariance, Is.EqualTo(100).Within(0.00001));
		Assert.That(statistics.PopulationStandardDeviation, Is.EqualTo(10).Within(0.00001));
	}
}
