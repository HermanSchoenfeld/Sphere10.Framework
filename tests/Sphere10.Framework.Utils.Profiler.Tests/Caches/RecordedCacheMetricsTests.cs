// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Sphere10.Framework.Utils.Profiler.Caches;

namespace Sphere10.Framework.Utils.Profiler.Tests.Caches;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class RecordedCacheMetricsTests {
	public static IEnumerable<TestCaseData> RetainedCachePhases() {
		var root = Path.Combine(TestContext.CurrentContext.TestDirectory, "Results");
		var found = false;
		var paths = Directory.Exists(root) ? Directory.GetFiles(root, "run.json", SearchOption.AllDirectories) : Array.Empty<string>();
		foreach (var path in paths.OrderBy(path => path, StringComparer.Ordinal)) {
			using var run = JsonDocument.Parse(File.ReadAllText(path));
			if (!run.RootElement.TryGetProperty("Settings", out var settings) || !settings.TryGetProperty("HotAccessPercent", out _) || !settings.TryGetProperty("FetchWork", out _))
				continue;
			var relative = Path.GetRelativePath(root, Path.GetDirectoryName(path));
			found = true;
			yield return new TestCaseData(relative).SetName("RecordedCacheMetricsTests." + relative.Replace(Path.DirectorySeparatorChar, '.'));
		}
		if (!found)
			yield return new TestCaseData("missing-cache-evidence")
				.SetName("RecordedCacheMetricsTests.NoLocalCacheEvidence")
				.Ignore("Cache benchmark captures are local ignored output. Run the cache profiler and retain its worker evidence before inspecting these results.");
	}

	[TestCaseSource(nameof(RetainedCachePhases))]
	public void RetainedBatchesReconcileConfigurationAccountingAndDeterministicCounters(string relativeDirectory) {
		var directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "Results", relativeDirectory);
		Assert.That(Directory.Exists(directory), Is.True, "Retain the completed cache measurement phase under Results and rebuild the tests.");
		using var run = ReadAttachment(Path.Combine(directory, "run.json"));
		var settings = JsonSerializer.Deserialize<CacheProfilerSettings>(run.RootElement.GetProperty("Settings"), CacheProfilerSettings.JsonOptions);
		settings.Validate();
		var arguments = run.RootElement.GetProperty("BenchmarkArguments").EnumerateArray().Select(item => item.GetString()).ToArray();
		Assert.That(int.TryParse(GetArgument(arguments, "--launchCount"), out var launchCount), Is.True);
		Assert.That(int.TryParse(GetArgument(arguments, "--warmupCount"), out var warmups), Is.True);
		Assert.That(int.TryParse(GetArgument(arguments, "--iterationCount"), out var iterations), Is.True);
		Assert.That(launchCount, Is.GreaterThanOrEqualTo(2));
		Assert.That(warmups, Is.GreaterThanOrEqualTo(5));
		Assert.That(iterations, Is.GreaterThanOrEqualTo(10));
		using var status = ReadAttachment(Path.Combine(directory, "execution-result.json"));
		Assert.That(status.RootElement.GetProperty("DomainExitCode").GetInt32(), Is.Zero);
		Assert.That(status.RootElement.GetProperty("Origin").GetString(), Is.Not.Empty);
		var consolePath = Path.Combine(directory, "console.log");
		Assert.That(File.Exists(consolePath), Is.True);
		TestContext.AddTestAttachment(consolePath, "Original cache worker process exits");
		var measuredCases = ReadMeasuredCases(directory, settings);
		var cases = measuredCases.Values.ToArray();
		var processes = ReadWorkerCaseBindings(File.ReadAllLines(consolePath), measuredCases);
		Assert.That(processes.Count, Is.EqualTo(cases.Length * launchCount), "Missing or extra original cache worker launch.");
		foreach (var measuredCase in cases)
			Assert.That(processes.Values.Count(item => item == measuredCase), Is.EqualTo(launchCount), "Each measured cache case must have the declared original worker launches.");
		Assert.That(cases.Select(item => item.Profile).Distinct(StringComparer.Ordinal), Is.EquivalentTo(settings.Profiles.Select(profile => profile.Name)), "Every declared cache profile must have an actual measured case.");
		var metricsRoot = Path.Combine(directory, "cache-metrics", settings.RunId);
		Assert.That(Directory.Exists(metricsRoot), Is.True);
		var paths = Directory.GetFiles(metricsRoot, "*.json").OrderBy(path => path, StringComparer.Ordinal).ToArray();
		Assert.That(paths, Is.Not.Empty);
		var retained = new List<RecordedBatch>();
		foreach (var path in paths) {
			using var batch = ReadAttachment(path);
			var root = batch.RootElement;
			var profile = JsonSerializer.Deserialize<CacheProfile>(root.GetProperty("Profile"), CacheProfilerSettings.JsonOptions);
			var expectedProfile = settings.GetProfile(profile.Name);
			Assert.That(JsonSerializer.Serialize(profile, CacheProfilerSettings.JsonOptions), Is.EqualTo(JsonSerializer.Serialize(expectedProfile, CacheProfilerSettings.JsonOptions)), path);
			Assert.That(root.GetProperty("Profile").GetProperty("MinimumBatchRelease").GetInt64(), Is.EqualTo(expectedProfile.MinimumBatchRelease), path);
			var controls = root.GetProperty("Settings");
			Assert.That(controls.GetProperty("OperationCount").GetInt32(), Is.EqualTo(settings.OperationCount), path);
			Assert.That(controls.GetProperty("HotSetPercent").GetInt32(), Is.EqualTo(settings.HotSetPercent), path);
			Assert.That(controls.GetProperty("HotAccessPercent").GetInt32(), Is.EqualTo(settings.HotAccessPercent), path);
			Assert.That(controls.GetProperty("FetchWork").GetInt32(), Is.EqualTo(settings.FetchWork), path);
			var metrics = JsonSerializer.Deserialize<CacheWorkloadMetrics>(root.GetProperty("Metrics"), CacheProfilerSettings.JsonOptions);
			AssertAccounting(settings, expectedProfile, metrics);
			Assert.That(cases, Does.Contain((metrics.Profile, metrics.Workload)), "A metrics batch must correspond to an actual measured profile/method.");
			var processId = root.GetProperty("ProcessId").GetInt32();
			AssertWorkerCaseBinding(processes, processId, metrics);
			var fixtureId = root.GetProperty("FixtureId").GetString();
			Assert.That(Guid.TryParseExact(fixtureId, "N", out var fixture) && fixture != Guid.Empty, Is.True);
			var iteration = root.GetProperty("Iteration").GetInt32();
			Assert.That(iteration, Is.Positive);
			Assert.That(DateTimeOffset.TryParse(root.GetProperty("RecordedAtUtc").GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _), Is.True);
			retained.Add(new RecordedBatch(processId, fixtureId, iteration, metrics));
		}
		Assert.That(retained.Select(item => (item.Metrics.Profile, item.Metrics.Workload)).Distinct(), Is.EquivalentTo(cases));
		foreach (var measured in cases) {
			var matching = retained.Where(item => item.Metrics.Profile == measured.Profile && item.Metrics.Workload == measured.Workload).ToArray();
			Assert.That(matching.Select(item => item.ProcessId).Distinct().Count(), Is.EqualTo(launchCount), "Missing or extra retained cache worker launch.");
			var canonical = JsonSerializer.Serialize(matching[0].Metrics, CacheProfilerSettings.JsonOptions);
			Assert.That(matching.All(item => JsonSerializer.Serialize(item.Metrics, CacheProfilerSettings.JsonOptions) == canonical), Is.True, "Deterministic cache counters/checksums changed across reset batches or independent launches.");
			foreach (var worker in matching.GroupBy(item => (item.ProcessId, item.FixtureId))) {
				Assert.That(worker.Count(), Is.GreaterThanOrEqualTo(warmups + iterations), "Retain all declared warmup and actual cache batches; diagnostic batches may add more.");
				Assert.That(worker.Select(item => item.Iteration).OrderBy(index => index), Is.EqualTo(Enumerable.Range(1, worker.Count())), "Cache iteration identities must be distinct and contiguous.");
			}
			var sample = matching[0].Metrics;
			TestContext.Out.WriteLine($"{measured.Profile}/{measured.Workload}: {matching.Length} verified retained batches across {launchCount} workers; capacity {sample.Capacity} estimator units, "
				+ $"{sample.BaselineItemCount} preseed items, {sample.OperationCount} accesses, {sample.FetchCount} fetches/{sample.FetchedSize} units, "
				+ $"{sample.ReapCount} reaps/{sample.VictimCount} victims/{sample.VictimSize} units; final {sample.FinalItemCount} items/{sample.FinalSize} units.");
		}
		TestContext.Out.WriteLine("Per-batch metrics include warmup, actual and possible diagnostic iterations. They do not identify BDN sample stages; the separate retained measurement fixture reconciles raw timing stages.");
	}

	[TestCase("unverified")]
	[TestCase("wrong-capacity")]
	[TestCase("wrong-operations")]
	[TestCase("incorrect-accounting")]
	[TestCase("hot-refetch")]
	[TestCase("wrong-batch-release")]
	public void MetricsAccountingRejectsInconsistentRecordedBatches(string defect) {
		var settings = SyntheticSettings();
		var profile = settings.Profiles[0];
		var metrics = SyntheticMetrics();
		switch (defect) {
			case "unverified": metrics.Verified = false; break;
			case "wrong-capacity": metrics.Capacity++; break;
			case "wrong-operations": metrics.OperationCount++; break;
			case "incorrect-accounting": metrics.FinalSize--; break;
			case "hot-refetch": metrics.FetchCount++; break;
			case "wrong-batch-release": metrics.MinimumBatchRelease++; break;
		}
		Assert.That(() => AssertAccounting(settings, profile, metrics), Throws.InstanceOf<AssertionException>());
	}

	[Test]
	public void MetricsAccountingAcceptsVerifiedEstimatorBudgetWithoutCallingItHeapBytes() {
		var settings = SyntheticSettings();
		Assert.That(() => AssertAccounting(settings, settings.Profiles[0], SyntheticMetrics()), Throws.Nothing);
	}

	[Test]
	public void RecordedMetricsRejectAnotherSuccessfulWorkersCaseBinding() {
		var typed = "Sphere10.Framework.Utils.Profiler.Caches.CacheAccessBenchmarks.TypedHotKeys(Profile: \"synthetic\")";
		var boxed = "Sphere10.Framework.Utils.Profiler.Caches.CacheAccessBenchmarks.ObjectHotKeys(Profile: \"synthetic\")";
		var cases = new Dictionary<string, (string Profile, string Workload)> {
			[typed] = ("synthetic", "TypedHotKeys"), [boxed] = ("synthetic", "ObjectHotKeys")
		};
		var lines = new[] {
			"// Execute: dotnet generated.dll --benchmarkName " + JsonSerializer.Serialize(typed) + " --benchmarkId 0 in D:/synthetic/worker",
			"// Benchmark Process Environment Information:",
			"// Benchmark Process 101 has exited with code 0.",
			"// Execute: dotnet generated.dll --benchmarkName " + JsonSerializer.Serialize(boxed) + " --benchmarkId 1 in D:/synthetic/worker",
			"// Benchmark Process 202 has exited with code 0."
		};
		var bindings = ReadWorkerCaseBindings(lines, cases);
		var metrics = SyntheticMetrics();
		Assert.That(() => AssertWorkerCaseBinding(bindings, 101, metrics), Throws.Nothing);
		Assert.That(() => AssertWorkerCaseBinding(bindings, 202, metrics), Throws.InstanceOf<AssertionException>());
	}

	[TestCase("missing-exit")]
	[TestCase("exit-without-command")]
	[TestCase("overlapping-command")]
	[TestCase("failed-worker")]
	public void WorkerCaseBindingRejectsIncompleteOrAmbiguousOriginalConsole(string defect) {
		var name = "Sphere10.Framework.Utils.Profiler.Caches.CacheAccessBenchmarks.TypedHotKeys(Profile: \"synthetic\")";
		var command = "// Execute: dotnet generated.dll --benchmarkName " + JsonSerializer.Serialize(name) + " --benchmarkId 0 in D:/synthetic/worker";
		var exit = "// Benchmark Process 101 has exited with code 0.";
		var lines = defect switch {
			"missing-exit" => new[] { command },
			"exit-without-command" => new[] { exit },
			"overlapping-command" => new[] { command, command, exit },
			_ => new[] { command, "// Benchmark Process 101 has exited with code 1." }
		};
		var cases = new Dictionary<string, (string Profile, string Workload)> { [name] = ("synthetic", "TypedHotKeys") };
		Assert.That(() => ReadWorkerCaseBindings(lines, cases), Throws.InstanceOf<AssertionException>());
	}

	private static void AssertAccounting(CacheProfilerSettings settings, CacheProfile profile, CacheWorkloadMetrics metrics) {
		Assert.That(metrics.Verified, Is.True);
		Assert.That(metrics.Profile, Is.EqualTo(profile.Name));
		Assert.That(metrics.Capacity, Is.EqualTo(profile.Capacity));
		Assert.That(metrics.ReapLevel, Is.EqualTo(profile.ReapLevel));
		Assert.That(metrics.MinimumBatchRelease, Is.EqualTo(profile.MinimumBatchRelease));
		Assert.That(metrics.OperationCount, Is.EqualTo(settings.OperationCount));
		Assert.That(metrics.AccessCount, Is.EqualTo(settings.OperationCount));
		var baselineCount = BaselineCount(profile);
		Assert.That(metrics.BaselineItemCount, Is.EqualTo(baselineCount));
		Assert.That(metrics.BaselineSize, Is.EqualTo(profile.Capacity));
		Assert.That(metrics.HotKeyCount, Is.EqualTo(Math.Max(1, baselineCount * settings.HotSetPercent / 100)));
		Assert.That(metrics.FetchCount, Is.InRange(0, settings.OperationCount));
		Assert.That(metrics.VictimCount, Is.InRange(0, baselineCount + metrics.FetchCount));
		Assert.That(metrics.ReapCount, Is.InRange(0, metrics.FetchCount));
		Assert.That(metrics.VictimCount, Is.GreaterThanOrEqualTo(metrics.ReapCount));
		var maximumSize = profile.SizePattern == CacheSizePattern.Unit ? 1L : 4L;
		Assert.That(metrics.FetchedSize, Is.InRange((long)metrics.FetchCount, metrics.FetchCount * maximumSize));
		Assert.That(metrics.VictimSize, Is.InRange((long)metrics.VictimCount, metrics.VictimCount * maximumSize));
		Assert.That(metrics.FinalItemCount, Is.EqualTo(baselineCount + metrics.FetchCount - metrics.VictimCount));
		Assert.That(metrics.FinalSize, Is.EqualTo(profile.Capacity + metrics.FetchedSize - metrics.VictimSize));
		Assert.That(metrics.FinalSize, Is.InRange(0L, (long)profile.Capacity));
		Assert.That(metrics.ResultChecksum, Is.Positive);
		Assert.That(metrics.MembershipChecksum, Is.GreaterThanOrEqualTo(0));
		if (metrics.Workload is "TypedHotKeys" or "ObjectHotKeys" or "ObjectPreboxedHotKeys") {
			Assert.That(metrics.FetchCount, Is.Zero);
			Assert.That(metrics.VictimCount, Is.Zero);
			Assert.That(metrics.ReapCount, Is.Zero);
			Assert.That(metrics.FetchWorkChecksum, Is.Zero);
		} else {
			Assert.That(metrics.Workload, Is.AnyOf("SequentialPressure", "HotSetPressure"));
			if (metrics.Workload == "SequentialPressure" || settings.HotAccessPercent == 0)
				Assert.That(metrics.FetchCount, Is.EqualTo(settings.OperationCount));
			if (metrics.Workload == "HotSetPressure" && settings.HotAccessPercent == 100)
				Assert.That(metrics.FetchCount, Is.Zero);
		}
	}

	private static Dictionary<string, (string Profile, string Workload)> ReadMeasuredCases(string directory, CacheProfilerSettings settings) {
		var cases = new Dictionary<string, (string Profile, string Workload)>(StringComparer.Ordinal);
		var paths = Directory.GetFiles(Path.Combine(directory, "results"), "*-report-full.json");
		Assert.That(paths, Is.Not.Empty);
		foreach (var path in paths) {
			using var report = ReadAttachment(path);
			foreach (var benchmark in report.RootElement.GetProperty("Benchmarks").EnumerateArray()) {
				Assert.That(benchmark.GetProperty("Namespace").GetString(), Is.EqualTo("Sphere10.Framework.Utils.Profiler.Caches"));
				var fullName = benchmark.GetProperty("FullName").GetString();
				var profiles = settings.Profiles.Where(profile => fullName.EndsWith("(Profile: " + JsonSerializer.Serialize(profile.Name) + ")", StringComparison.Ordinal)).ToArray();
				Assert.That(profiles, Has.Length.EqualTo(1));
				Assert.That(benchmark.GetProperty("Statistics").ValueKind, Is.EqualTo(JsonValueKind.Object));
				Assert.That(cases.TryAdd(fullName, (profiles[0].Name, benchmark.GetProperty("Method").GetString())), Is.True, "Duplicate measured cache FullName.");
			}
		}
		Assert.That(cases.Values.Distinct().Count(), Is.EqualTo(cases.Count));
		return cases;
	}

	private static Dictionary<int, (string Profile, string Workload)> ReadWorkerCaseBindings(string[] lines, Dictionary<string, (string Profile, string Workload)> cases) {
		var command = new Regex(@"^// Execute: dotnet \S+\.dll .*?--benchmarkName (?<name>""(?:\\.|[^""])*"").*?--benchmarkId \d+ in .+$", RegexOptions.CultureInvariant);
		var exited = new Regex(@"^// Benchmark Process (?<process>\d+) has exited with code (?<code>-?\d+)\.$", RegexOptions.CultureInvariant);
		var bindings = new Dictionary<int, (string Profile, string Workload)>();
		var pending = (string)null;
		foreach (var line in lines) {
			if (line.StartsWith("// Execute: dotnet ", StringComparison.Ordinal)) {
				Assert.That(pending, Is.Null, "A new sequential worker command appeared before the previous worker exited.");
				var match = command.Match(line);
				Assert.That(match.Success, Is.True, "Unparsed original cache worker command: " + line);
				pending = JsonSerializer.Deserialize<string>(match.Groups["name"].Value);
				Assert.That(cases.ContainsKey(pending), Is.True, "The executed worker must identify an exact retained measured cache FullName.");
				continue;
			}
			var exit = exited.Match(line);
			if (!exit.Success)
				continue;
			Assert.That(pending, Is.Not.Null, "Worker exit has no preceding original Execute command.");
			Assert.That(exit.Groups["code"].Value, Is.EqualTo("0"), "The original cache worker failed.");
			var processId = int.Parse(exit.Groups["process"].Value, CultureInfo.InvariantCulture);
			Assert.That(bindings.TryAdd(processId, cases[pending]), Is.True, "A reused worker PID would make the retained batch case attribution ambiguous.");
			pending = null;
		}
		Assert.That(pending, Is.Null, "The final executed worker did not retain its process exit.");
		Assert.That(bindings, Is.Not.Empty);
		return bindings;
	}

	private static void AssertWorkerCaseBinding(Dictionary<int, (string Profile, string Workload)> bindings, int processId, CacheWorkloadMetrics metrics) {
		Assert.That(bindings.TryGetValue(processId, out var measured), Is.True, "The metrics must come from a successful original worker process.");
		Assert.That((metrics.Profile, metrics.Workload), Is.EqualTo(measured), "The batch's Profile/Workload must match the exact FullName executed by its original PID.");
	}

	private static int BaselineCount(CacheProfile profile) {
		var size = 0;
		var count = 0;
		while (size < profile.Capacity) {
			size += Math.Min(profile.Capacity - size, profile.SizePattern == CacheSizePattern.Unit ? 1 : count % 4 + 1);
			count++;
		}
		return count;
	}

	private static string GetArgument(string[] arguments, string option) {
		var index = Array.IndexOf(arguments, option);
		return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
	}

	private static JsonDocument ReadAttachment(string path) {
		Assert.That(File.Exists(path), Is.True, "Missing retained cache evidence attachment: " + path);
		TestContext.AddTestAttachment(path, Path.GetFileName(path));
		return JsonDocument.Parse(File.ReadAllText(path));
	}

	private static CacheProfilerSettings SyntheticSettings() => new() {
		OperationCount = 100, HotSetPercent = 10, Profiles = new[] { new CacheProfile { Name = "synthetic", Capacity = 64 } }
	};

	private static CacheWorkloadMetrics SyntheticMetrics() => new() {
		Profile = "synthetic", Workload = "TypedHotKeys", Verified = true,
		OperationCount = 100, AccessCount = 100, Capacity = 64, ReapLevel = 0.25, MinimumBatchRelease = 16,
		BaselineItemCount = 64, BaselineSize = 64, HotKeyCount = 6,
		FinalItemCount = 64, FinalSize = 64, ResultChecksum = 100, MembershipChecksum = 64
	};

	private sealed record RecordedBatch(int ProcessId, string FixtureId, int Iteration, CacheWorkloadMetrics Metrics);
}
