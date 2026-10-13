// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using Sphere10.Framework.Utils.Profiler.Caches;

namespace Sphere10.Framework.Utils.Profiler.Tests.Caches;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CacheProfilerTests {
	public static IEnumerable<TestCaseData> WorkloadCases() {
		foreach (var sizes in Tools.Enums.GetValues<CacheSizePattern>())
			foreach (var profile in CacheProfile.CreateSuite("reaping", 64, sizes))
				foreach (var workload in new[] { "TypedHotKeys", "ObjectHotKeys", "ObjectPreboxedHotKeys", "SequentialPressure", "HotSetPressure" })
					yield return new TestCaseData(profile, workload).SetName($"CacheWorkload_{profile.Name}_{workload}");
	}

	[TestCaseSource(nameof(WorkloadCases))]
	public void WorkloadMatchesIndependentPolicyAndValueOracle(CacheProfile profile, string workload) {
		var settings = Settings(profile);
		using var fixture = new CacheBenchmarkFixture(settings, profile);
		fixture.PrepareIteration();
		var checksum = Execute(fixture, workload);
		var metrics = fixture.Verify();
		Assert.That(metrics.Verified, Is.True);
		Assert.That(metrics.ResultChecksum, Is.EqualTo(checksum));
		Assert.That(metrics.BaselineSize, Is.EqualTo(profile.Capacity));
		Assert.That(metrics.FinalSize, Is.InRange(0, profile.Capacity));
		Assert.That(metrics.AccessCount, Is.EqualTo(settings.OperationCount));
		if (workload.EndsWith("HotKeys", StringComparison.Ordinal)) {
			Assert.That(metrics.FetchCount, Is.Zero);
			Assert.That(metrics.VictimCount, Is.Zero);
			Assert.That(metrics.FinalSize, Is.EqualTo(profile.Capacity));
		} else {
			Assert.That(metrics.FetchCount, Is.Positive);
			Assert.That(metrics.ReapCount, Is.Positive);
			Assert.That(metrics.VictimCount, Is.GreaterThanOrEqualTo(metrics.ReapCount));
		}
		if (workload == "SequentialPressure")
			Assert.That(metrics.FetchCount, Is.EqualTo(settings.OperationCount));
	}

	[TestCase(CacheReapPolicy.LongestIdle)]
	[TestCase(CacheReapPolicy.Oldest)]
	public void QuarterBatchAmortizesSequentialReapsWhileRetainingRefillCosts(CacheReapPolicy policy) {
		var minimal = new CacheProfile { Name = "minimal", ReapPolicy = policy, Capacity = 64, ReapLevel = 1.0 / 64 };
		var quarter = new CacheProfile { Name = "quarter", ReapPolicy = policy, Capacity = 64, ReapLevel = 0.25 };
		using var minimalFixture = new CacheBenchmarkFixture(Settings(minimal), minimal);
		using var quarterFixture = new CacheBenchmarkFixture(Settings(quarter), quarter);
		minimalFixture.PrepareIteration();
		quarterFixture.PrepareIteration();
		Assert.That(minimalFixture.SequentialPressure(), Is.EqualTo(quarterFixture.SequentialPressure()));
		var singleMetrics = minimalFixture.Verify();
		var batchMetrics = quarterFixture.Verify();
		Assert.That(batchMetrics.FetchCount, Is.EqualTo(singleMetrics.FetchCount));
		Assert.That(batchMetrics.ReapCount, Is.LessThan(singleMetrics.ReapCount));
		Assert.That(batchMetrics.VictimCount, Is.EqualTo(singleMetrics.VictimCount));
	}

	[TestCase(CacheSizePattern.Unit)]
	[TestCase(CacheSizePattern.Weighted)]
	public void PreparingAgainRestoresSameBaselineAndDeterministicCounters(CacheSizePattern pattern) {
		var profile = new CacheProfile { Name = "reset", Capacity = 64, SizePattern = pattern };
		using var fixture = new CacheBenchmarkFixture(Settings(profile), profile);
		fixture.PrepareIteration();
		fixture.HotSetPressure();
		var first = fixture.Verify();
		fixture.PrepareIteration();
		fixture.HotSetPressure();
		var second = fixture.Verify();
		Assert.That(JsonSerializer.Serialize(second, CacheProfilerSettings.JsonOptions), Is.EqualTo(JsonSerializer.Serialize(first, CacheProfilerSettings.JsonOptions)));
	}

	[Test]
	public void TypedAndObjectPathsUseIdenticalHotKeysWithoutFetches() {
		var profile = CacheProfile.CreateSuite("access", 64, CacheSizePattern.Unit)[0];
		using var fixture = new CacheBenchmarkFixture(Settings(profile), profile);
		fixture.PrepareIteration();
		var typed = fixture.TypedHotKeys();
		Assert.That(fixture.Verify().FetchCount, Is.Zero);
		fixture.PrepareIteration();
		Assert.That(fixture.ObjectHotKeys(), Is.EqualTo(typed));
		Assert.That(fixture.Verify().FetchCount, Is.Zero);
		fixture.PrepareIteration();
		Assert.That(fixture.ObjectPreboxedHotKeys(), Is.EqualTo(typed));
		Assert.That(fixture.Verify().FetchCount, Is.Zero);
	}

	[TestCase(0)]
	[TestCase(128)]
	public void DeclaredFetchWorkIsVerifiedWithActualMisses(int fetchWork) {
		var profile = new CacheProfile { Name = "fetch", Capacity = 64 };
		var settings = Settings(profile);
		settings.FetchWork = fetchWork;
		using var fixture = new CacheBenchmarkFixture(settings, profile);
		fixture.PrepareIteration();
		fixture.SequentialPressure();
		var metrics = fixture.Verify();
		Assert.That(metrics.FetchCount, Is.EqualTo(settings.OperationCount));
		Assert.That(metrics.FetchWorkChecksum, Is.Not.Zero);
	}

	[TestCase(CacheReapPolicy.LongestIdle, 0)]
	[TestCase(CacheReapPolicy.LongestIdle, 100)]
	[TestCase(CacheReapPolicy.Oldest, 0)]
	[TestCase(CacheReapPolicy.Oldest, 100)]
	public void HotAccessControlChangesActualMisses(CacheReapPolicy policy, int hotAccessPercent) {
		var profile = new CacheProfile { Name = "distribution", Capacity = 64, ReapPolicy = policy };
		var settings = Settings(profile);
		settings.HotAccessPercent = hotAccessPercent;
		using var fixture = new CacheBenchmarkFixture(settings, profile);
		fixture.PrepareIteration();
		fixture.HotSetPressure();
		Assert.That(fixture.Verify().FetchCount, Is.EqualTo(hotAccessPercent == 100 ? 0 : settings.OperationCount));
	}

	[Test]
	public void RetainedMetricsContainVerifiedConfigurationAndBatchCounters() {
		var directory = Path.Combine(Path.GetTempPath(), $"Sphere10CacheProfilerTests-{Guid.NewGuid():N}");
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => { if (Directory.Exists(directory)) Directory.Delete(directory, true); });
		var profile = new CacheProfile { Name = "metrics", Capacity = 64, SizePattern = CacheSizePattern.Weighted };
		var settings = Settings(profile);
		settings.OutputDirectory = directory;
		using var fixture = new CacheBenchmarkFixture(settings, profile);
		fixture.PrepareIteration();
		fixture.HotSetPressure();
		var metrics = fixture.Verify();
		fixture.WriteMetrics(metrics);
		var path = Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories).Single();
		using var retained = JsonDocument.Parse(File.ReadAllText(path));
		Assert.That(retained.RootElement.GetProperty("Profile").GetProperty("ReapLevel").GetDouble(), Is.EqualTo(profile.ReapLevel));
		Assert.That(retained.RootElement.GetProperty("Metrics").GetProperty("Verified").GetBoolean(), Is.True);
		Assert.That(retained.RootElement.GetProperty("Metrics").GetProperty("FetchCount").GetInt32(), Is.EqualTo(metrics.FetchCount));
		Assert.That(retained.RootElement.GetProperty("Metrics").GetProperty("VictimSize").GetInt64(), Is.EqualTo(metrics.VictimSize));
	}

	[TestCase(0)]
	[TestCase(-0.25)]
	[TestCase(1.0)]
	[TestCase(1.01)]
	[TestCase(double.NaN)]
	[TestCase(double.PositiveInfinity)]
	public void InvalidReapLevelsAreRejectedBeforeWorkloadCreation(double reapLevel) {
		var profile = new CacheProfile { Name = "invalid", ReapLevel = reapLevel };
		Assert.That(() => profile.Validate(), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase(CacheReapPolicy.None)]
	[TestCase(CacheReapPolicy.Smallest)]
	public void UnsupportedPolicyIsRejectedForThisComparison(CacheReapPolicy policy) {
		Assert.That(() => new CacheProfile { Name = "invalid", ReapPolicy = policy }.Validate(), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase("--hot-set", "0")]
	[TestCase("--hot-access", "101")]
	[TestCase("--capacity", "0")]
	[TestCase("--operations", "0")]
	[TestCase("--fetch-work", "-1")]
	public void InvalidWorkloadControlsAreRejected(string option, string value) {
		Assert.That(() => CacheProfilerSettings.Parse(new[] { option, value }, out _, out _), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void ConfigurationInspectionPreservesSuiteSelectionAndForwardedBenchmarkControls() {
		var settings = CacheProfilerSettings.Parse(new[] { "--suite", "reaping", "--capacity", "64", "--sizes", "Weighted", "--operations", "400",
			"--hot-set", "20", "--hot-access", "70", "--fetch-work", "128", "--profiles", "fifo-minimal-weighted,lru-quarter-weighted", "--show-config", "--iterationCount", "10" },
			out var forwarded, out var show);
		Assert.That(show, Is.True);
		Assert.That(settings.Profiles.Select(profile => profile.Name), Is.EquivalentTo(new[] { "fifo-minimal-weighted", "lru-quarter-weighted" }));
		Assert.That(settings.Profiles.All(profile => profile.Capacity == 64 && profile.SizePattern == CacheSizePattern.Weighted), Is.True);
		Assert.That(settings.HotSetPercent, Is.EqualTo(20));
		Assert.That(settings.HotAccessPercent, Is.EqualTo(70));
		Assert.That(settings.FetchWork, Is.EqualTo(128));
		Assert.That(forwarded, Is.EqualTo(new[] { "--iterationCount", "10" }));
	}

	[Test]
	public void DuplicateProfileNamesAndUnknownSelectionsAreRejected() {
		var profile = new CacheProfile { Name = "duplicate" };
		var settings = Settings(profile);
		settings.Profiles = new[] { profile, profile };
		Assert.That(() => settings.Validate(), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => CacheProfilerSettings.Parse(new[] { "--profiles", "missing" }, out _, out _), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase(17)]
	[TestCase(41)]
	[TestCase(49)]
	[TestCase(63)]
	[TestCase(127)]
	[TestCase(1000)]
	[TestCase(1024)]
	public void MinimalReapingSuiteAlwaysTargetsOneEstimatorUnit(int capacity) {
		var profiles = CacheProfile.CreateSuite("reaping", capacity, CacheSizePattern.Weighted);
		Assert.That(profiles.Where(profile => profile.Name.Contains("minimal", StringComparison.Ordinal)).Select(profile => profile.MinimumBatchRelease), Is.All.EqualTo(1));
	}

	private static CacheProfilerSettings Settings(CacheProfile profile) => new() {
		OperationCount = 400, HotSetPercent = 10, HotAccessPercent = 80,
		OutputDirectory = Path.Combine(Path.GetTempPath(), $"Sphere10CacheProfilerTests-{Guid.NewGuid():N}"), Profiles = new[] { profile }
	};

	private static long Execute(CacheBenchmarkFixture fixture, string workload) => workload switch {
		"TypedHotKeys" => fixture.TypedHotKeys(),
		"ObjectHotKeys" => fixture.ObjectHotKeys(),
		"ObjectPreboxedHotKeys" => fixture.ObjectPreboxedHotKeys(),
		"SequentialPressure" => fixture.SequentialPressure(),
		"HotSetPressure" => fixture.HotSetPressure(),
		_ => throw new ArgumentException("Unknown cache workload.", nameof(workload))
	};
}
