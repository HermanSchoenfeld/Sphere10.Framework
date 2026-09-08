// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Sphere10.Framework.Consensus.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class PeriodicStatisticsTests {
	[TestCase(0)]
	[TestCase(-1)]
	public void ConstructorRejectsNonPositivePeriods(int milliseconds) {
		Assert.That(() => new PeriodicStatistics(TimeSpan.FromMilliseconds(milliseconds), 10), Throws.ArgumentException);
	}

	[Test]
	public void ConstructorRejectsMissingStore() {
		Assert.That(() => new PeriodicStatistics(TimeSpan.FromSeconds(1), 10, null), Throws.ArgumentNullException);
	}

	[Test]
	public void StartInitializesPeriodAndAllowsEventRegistration() {
		var statistics = new PeriodicStatistics(TimeSpan.FromHours(1), 10, new List<Statistics>());
		Assert.That(() => statistics.RegisterEvent(1), Throws.InvalidOperationException);
		var beforeStart = DateTime.UtcNow;
		statistics.Start();

		Assert.That(statistics.StartedOn, Is.InRange(beforeStart, DateTime.UtcNow));
		Assert.That(statistics.PeriodsAvailable, Is.InRange(0, 1));
		Assert.That(() => statistics.RegisterEvent(5, 2), Throws.Nothing);
		Assert.That(() => statistics.Start(), Throws.InvalidOperationException);
	}
}