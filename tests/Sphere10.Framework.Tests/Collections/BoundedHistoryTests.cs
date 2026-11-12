// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
public sealed class BoundedHistoryTests {
	[Test]
	public void CountCostAndOversizedAdmissionHaveIndependentOldestFirstBounds() {
		var history = new BoundedHistory<string>(3, 6, value => value.Length);
		Assert.That(history.TryAdd("aa"), Is.True);
		history.TryAdd("bb"); history.TryAdd("cc"); history.TryAdd("ddd");
		Assert.That(history.Snapshot(), Is.EqualTo(new[] { "cc", "ddd" }));
		Assert.That(history.RetainedCost, Is.EqualTo(5));
		Assert.That(history.TryAdd("1234567"), Is.False);
		Assert.That(history.Snapshot(), Is.EqualTo(new[] { "cc", "ddd" }));
		Assert.That(history.Discarded, Is.EqualTo(3));
		Assert.That(history.Received, Is.EqualTo(5));
		var detached = history.Snapshot(); detached[0] = "changed";
		Assert.That(history.First(), Is.EqualTo("cc"));
		history.Clear(resetCounters: false);
		Assert.That(history, Is.Empty);
		Assert.That(history.RetainedCost, Is.Zero);
		Assert.That(history.Received, Is.EqualTo(5));
		history.Clear(); Assert.That(history.Received, Is.Zero);
	}

	[Test]
	public void ConcurrentPublicationPreservesBoundsAndEveryDiscardedObservationIsCounted() {
		var history = new BoundedHistory<int>(31, 17, _ => 1);
		Parallel.For(0, 10000, index => history.TryAdd(index));
		Assert.That(history.Count, Is.EqualTo(17));
		Assert.That(history.Snapshot().Distinct().Count(), Is.EqualTo(17));
		Assert.That(history.Received, Is.EqualTo(10000));
		Assert.That(history.Discarded + history.Count, Is.EqualTo(history.Received));
		Assert.That(history.RetainedCost, Is.EqualTo(17));
	}
}
