// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;

namespace Sphere10.Framework.Consensus.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class TargetAlgorithmTests {
	private const uint PreviousCompactTarget = 0x20123456;

	[Test]
	[TestCase(134217728u)]
	[TestCase(0x20123456u)]
	[TestCase(0x40ABCDEFu)]
	[TestCase(3892314111u)]
	public void Molina_CompactTargetRoundTrips(uint compactTarget) {
		var algorithm = new MolinaTargetAlgorithm();
		Assert.That(algorithm.FromTarget(algorithm.ToTarget(compactTarget)), Is.EqualTo(compactTarget));
		Assert.That(algorithm.FromDigest(algorithm.ToDigest(compactTarget)), Is.EqualTo(compactTarget));
	}

	[Test]
	public void Molina_HigherCompactTargetIsHarder() {
		var algorithm = new MolinaTargetAlgorithm();
		Assert.That(algorithm.ToTarget(0x20123456), Is.GreaterThan(algorithm.ToTarget(0x20123457)));
		Assert.That(algorithm.ToTarget(0x20FFFFFF), Is.GreaterThan(algorithm.ToTarget(0x21000000)));
	}

	[Test]
	public void ASERT_RealTime() {
		var algorithm = new MolinaTargetAlgorithm();
		var configuration = CreateConfiguration();
		Assert.That(new ASERT_RTT(algorithm, configuration).RealTime, Is.True);
		Assert.That(new ASERT2(algorithm, configuration).RealTime, Is.False);
	}

	[Test]
	public void ASERT_WithoutEnoughTimestampsStartsAtMinimum() {
		var algorithm = new MolinaTargetAlgorithm();
		var configuration = CreateConfiguration();
		Assert.That(new ASERT_RTT(algorithm, configuration).CalculateNextBlockTarget(Array.Empty<DateTime>(), PreviousCompactTarget, 1), Is.EqualTo(algorithm.MinCompactTarget));
		Assert.That(new ASERT2(algorithm, configuration).CalculateNextBlockTarget(new[] { DateTime.UtcNow }, PreviousCompactTarget, 1), Is.EqualTo(algorithm.MinCompactTarget));
	}

	[Test]
	public void ASERT2_OnScheduleBlockKeepsTarget() {
		var algorithm = new MolinaTargetAlgorithm();
		var configuration = CreateConfiguration();
		var head = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var timestamps = new[] { head, head - configuration.BlockTime };
		// The adjustment factor is applied with six decimal digits, so an on-schedule block moves the target by at most a few mantissa units
		Assert.That(new ASERT2(algorithm, configuration).CalculateNextBlockTarget(timestamps, PreviousCompactTarget, 2), Is.InRange(PreviousCompactTarget - 64, PreviousCompactTarget + 64));
	}

	[Test]
	public void ASERT2_SlowBlockEasesAndFastBlockHardens() {
		var algorithm = new MolinaTargetAlgorithm();
		var configuration = CreateConfiguration();
		var asert = new ASERT2(algorithm, configuration);
		var head = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var slow = asert.CalculateNextBlockTarget(new[] { head, head - configuration.BlockTime * 2 }, PreviousCompactTarget, 2);
		var fast = asert.CalculateNextBlockTarget(new[] { head, head - configuration.BlockTime / 2 }, PreviousCompactTarget, 2);
		Assert.That(slow, Is.LessThan(PreviousCompactTarget));
		Assert.That(fast, Is.GreaterThan(PreviousCompactTarget));
	}

	[Test]
	public void ASERT_RTT_StaleHeadEases() {
		var algorithm = new MolinaTargetAlgorithm();
		var configuration = CreateConfiguration();
		var staleHead = DateTime.UtcNow - configuration.BlockTime * 3;
		Assert.That(new ASERT_RTT(algorithm, configuration).CalculateNextBlockTarget(new[] { staleHead }, PreviousCompactTarget, 2), Is.LessThan(PreviousCompactTarget));
	}

	private static ASERTConfiguration CreateConfiguration()
		=> new() { BlockTime = TimeSpan.FromSeconds(60), RelaxationTime = TimeSpan.FromSeconds(600) };
}
