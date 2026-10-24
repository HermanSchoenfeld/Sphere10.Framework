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
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Sphere10.Framework.Utils.Profiler.Tests.ObjectSpaces;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class RecordedObjectSpaceHotPathTests {
	private const string Campaign = "20261022-objectspace-hotpaths";
	private const string PageCampaign = "20261022-objectspace-page-btree";
	private const string DispatchCampaign = "20261022-span-dispatch";
	private static readonly Regex WorkerCommand = new(@"^// Execute: dotnet .*?--benchmarkName (?<name>""(?:\\.|[^""])*"").*?--job (?<job>""(?:\\.|[^""])*"")", RegexOptions.CultureInvariant);
	private static readonly Regex WorkerExit = new(@"^// Benchmark Process (?<pid>\d+) has exited with code (?<code>-?\d+)\.$", RegexOptions.CultureInvariant);
	private static readonly Regex CsvField = new(@"\G(?:^|,)(?:""(?<quoted>(?:[^""]|"""")*)""|(?<plain>[^,""\r\n]*))(?=,|$)", RegexOptions.CultureInvariant);

	public static IEnumerable<TestCaseData> PairedCases() {
		foreach (var storage in new[] { "Memory", "File" }) {
			var backend = storage.ToLowerInvariant();
			yield return Pair(backend + "-warm-clean", "WarmIdentityLookup", "plain", storage);
			yield return Pair(backend + "-warm-clean", "WarmIdentityLookup", "merkle", storage);
			yield return Pair(backend + "-warm-clean", "CleanFlush", "merkle", storage);
			yield return Pair(backend + "-reindex", "ReindexAndPersist", "merkle", storage);
			yield return Pair(backend + "-mixed", "MixedReadWriteAndPersist", "merkle-auto", storage);
			yield return Pair("large-" + backend + "-reindex", "ReindexAndPersist", "merkle", storage);
			yield return Pair("large-" + backend + "-mixed", "MixedReadWriteAndPersist", "merkle-auto", storage);
		}
		yield return Pair("large-file-reindex-confirmation", "ReindexAndPersist", "merkle", "File");
	}

	[TestCaseSource(nameof(PairedCases))]
	public void PairedCapturesRetainMatchingGuaranteesAndAllWorkerSamples(string folder, string method, string profile, string storage)
		=> VerifyPair(Campaign, folder, method, profile, storage, false);

	[TestCase("file-warm-clean", "WarmIdentityLookup", "merkle")]
	[TestCase("file-warm-clean", "CleanFlush", "merkle")]
	[TestCase("file-reindex", "ReindexAndPersist", "merkle")]
	[TestCase("file-mixed", "MixedReadWriteAndPersist", "merkle-auto")]
	[TestCase("confirmation-file-reindex", "ReindexAndPersist", "merkle")]
	[TestCase("confirmation-file-mixed", "MixedReadWriteAndPersist", "merkle-auto")]
	[TestCase("reverse-file-reindex", "ReindexAndPersist", "merkle")]
	public void PairedPageCapturesRetainMatchingGuaranteesAndAllWorkerSamples(string folder, string method, string profile)
		=> VerifyPair(PageCampaign, folder, method, profile, "File", true);

	[TestCase("WarmIdentityLookup")]
	[TestCase("CleanFlush")]
	public void PairedDispatchCapturesRetainMatchingGuaranteesAndAllWorkerSamples(string method)
		=> VerifyPair(DispatchCampaign, "file-warm-clean", method, "merkle", "File", true);

	[TestCase("Memory", BTreeType.BTree)]
	[TestCase("Memory", BTreeType.BTreePlus)]
	[TestCase("File", BTreeType.BTree)]
	[TestCase("File", BTreeType.BTreePlus)]
	public void SelectedImplementationsRetainAllWorkerSamples(string storage, BTreeType implementation) {
		var root = Path.Combine(TestContext.CurrentContext.TestDirectory, "Results", PageCampaign);
		if (!Directory.Exists(root))
			Assert.Ignore("Local implementation captures are unavailable; this fixture never launches benchmarks.");
		var profile = implementation.ToString().ToLowerInvariant();
		var capture = ReadCapture(Path.Combine(root, "implementation-" + storage.ToLowerInvariant()), "implementation", "ReindexAndPersist", profile, storage, false, true);
		WriteCapture(storage + "/" + implementation, capture);
	}

	private static void VerifyPair(string campaign, string folder, string method, string profile, string storage, bool merkleOnly) {
		var root = Path.Combine(TestContext.CurrentContext.TestDirectory, "Results", campaign);
		if (!Directory.Exists(root))
			Assert.Ignore("Local captures are unavailable. Retain completed paired BenchmarkDotNet captures under Results/" + campaign + " as described in Results/README.md; this fixture never launches benchmarks.");

		var before = ReadCapture(Path.Combine(root, "before-" + folder), folder, method, profile, storage, merkleOnly);
		var after = ReadCapture(Path.Combine(root, "after-" + folder), folder, method, profile, storage, merkleOnly);
		Assert.That(JsonNode.DeepEquals(PairedSettings(before.Run), PairedSettings(after.Run)), Is.True,
			"A source optimization comparison must preserve every dataset, profile, storage and integrity setting.");
		Assert.That(JsonNode.DeepEquals(JsonNode.Parse(before.Host.GetRawText()), JsonNode.Parse(after.Host.GetRawText())), Is.True,
			"Paired measurements must retain the same BenchmarkDotNet, runtime, hardware and build configuration.");
		foreach (var name in new[] { "Machine", "OperatingSystem", "Runtime", "Architecture", "LogicalProcessorCount", "FrameworkVersion", "ProfilerVersion" })
			Assert.That(after.Run.GetProperty(name).GetRawText(), Is.EqualTo(before.Run.GetProperty(name).GetRawText()), name);
		Assert.That(after.Arguments, Is.EquivalentTo(before.Arguments));
		Assert.That(after.Run.GetProperty("Settings").GetProperty("RunId").GetString(), Is.Not.EqualTo(before.Run.GetProperty("Settings").GetProperty("RunId").GetString()));
		var beforeStarted = before.Run.GetProperty("StartedAtUtc").GetDateTimeOffset();
		var afterStarted = after.Run.GetProperty("StartedAtUtc").GetDateTimeOffset();
		Assert.That(afterStarted, folder.StartsWith("reverse-", StringComparison.Ordinal) ? Is.LessThan(beforeStarted) : Is.GreaterThan(beforeStarted));
		Assert.That(after.StorageBytes, Is.EqualTo(before.StorageBytes), "Equivalent baseline data must retain the same encoded store size.");

		TestContext.Out.WriteLine($"{folder}: {method}, profile={profile}, storage={storage}.");
		TestContext.Out.WriteLine("Exact paired settings: " + PairedSettings(before.Run).ToJsonString());
		WriteCapture("Before", before);
		WriteCapture("After", after);
		TestContext.Out.WriteLine(FormattableString.Invariant($"Descriptive after/before mean ratio: {after.Mean / before.Mean:F4}; allocation difference: {after.AllocatedBytes - before.AllocatedBytes:N0} B/batch. No speed or timing threshold is asserted."));
	}

	private static TestCaseData Pair(string folder, string method, string profile, string storage)
		=> new TestCaseData(folder, method, profile, storage).SetName($"{nameof(RecordedObjectSpaceHotPathTests)}.{folder}.{method}.{profile}");

	private static Capture ReadCapture(string directory, string folder, string method, string profile, string storage, bool merkleOnly, bool selectedImplementations = false) {
		var runPath = RequiredFile(directory, "workload-run.json");
		var reportPath = RequiredFile(directory, Path.Combine("results", "BenchmarkRun-joined-report-full.bdn.json"));
		var csvPath = RequiredFile(directory, "configuration-ranks.csv");
		var consolePath = RequiredFile(directory, "console.txt");
		using var runDocument = JsonDocument.Parse(File.ReadAllText(runPath));
		using var reportDocument = JsonDocument.Parse(File.ReadAllText(reportPath));
		var run = runDocument.RootElement;
		var report = reportDocument.RootElement;
		var settings = run.GetProperty("Settings");
		var large = folder.StartsWith("large-", StringComparison.Ordinal);
		var warm = folder.EndsWith("-warm-clean", StringComparison.Ordinal);
		var records = large ? 1024 : 256;
		var operations = large ? 800 : 200;
		Assert.That(settings.GetProperty("RecordCount").GetInt32(), Is.EqualTo(records));
		Assert.That(settings.GetProperty("OperationCount").GetInt32(), Is.EqualTo(operations));
		Assert.That(settings.GetProperty("PayloadBytes").GetInt32(), Is.EqualTo(128));
		Assert.That(settings.GetProperty("ShuffledInsertion").GetBoolean(), Is.True);
		Assert.That(settings.GetProperty("LookupPattern").GetString(), Is.EqualTo("Shuffled"));
		Assert.That(settings.GetProperty("HotSetPercent").GetInt32(), Is.EqualTo(10));
		Assert.That(settings.GetProperty("PayloadPattern").GetString(), Is.EqualTo("Fixed"));
		Assert.That(settings.GetProperty("CategoryCount").GetInt32(), Is.EqualTo(records));
		Assert.That(settings.GetProperty("HotCategoryPercent").GetInt32(), Is.Zero);
		Assert.That(settings.GetProperty("WorkloadMix").GetString(), Is.EqualTo("Balanced"));
		Assert.That(settings.GetProperty("MixedReadPercentage").GetInt32(), Is.EqualTo(50));
		Assert.That(settings.GetProperty("Trace").GetBoolean(), Is.False, "Diagnostic traces must not be used as the comparison timings.");
		Assert.That(Guid.TryParseExact(settings.GetProperty("RunId").GetString(), "N", out _), Is.True);

		var profiles = settings.GetProperty("Profiles").EnumerateArray().ToArray();
		var expectedProfiles = selectedImplementations ? new[] { "btree", "btreeplus" } : warm && !merkleOnly ? new[] { "plain", "merkle" } : new[] { profile };
		Assert.That(profiles.Select(item => item.GetProperty("Name").GetString()), Is.EquivalentTo(expectedProfiles));
		foreach (var item in profiles)
			ValidateProfile(item, storage, selectedImplementations);
		var selected = profiles.Single(item => item.GetProperty("Name").GetString() == profile);
		var comparisonGroup = selected.GetProperty("ComparisonGroup").GetString();
		var arguments = run.GetProperty("BenchmarkArguments").EnumerateArray().Select(item => item.GetString()).ToArray();
		ValidateArguments(arguments, warm);
		var filters = ArgumentsAfter(arguments, "--filter");
		Assert.That(filters, Is.EquivalentTo(warm ? new[] { "*WarmIdentityLookup*", "*CleanFlush*" } : new[] { "*" + method + "*" }));

		var host = report.GetProperty("HostEnvironmentInfo");
		Assert.That(host.GetProperty("HasAttachedDebugger").GetBoolean(), Is.False);
		Assert.That(host.GetProperty("Configuration").GetString(), Is.EqualTo("RELEASE"));
		Assert.That(host.GetProperty("Architecture").GetString(), Is.EqualTo(run.GetProperty("Architecture").GetString()));
		Assert.That(host.GetProperty("RuntimeVersion").GetString(), Does.StartWith(run.GetProperty("Runtime").GetString()));
		Assert.That(host.GetProperty("LogicalCoreCount").GetInt32(), Is.EqualTo(run.GetProperty("LogicalProcessorCount").GetInt32()));
		var benchmarks = report.GetProperty("Benchmarks").EnumerateArray().ToArray();
		var expectedNames = selectedImplementations ? expectedProfiles.Select(name => FullName(method, name)).ToArray() : warm
			? (merkleOnly
				? new[] { FullName("WarmIdentityLookup", "merkle"), FullName("CleanFlush", "merkle") }
				: new[] { FullName("WarmIdentityLookup", "plain"), FullName("WarmIdentityLookup", "merkle"), FullName("CleanFlush", "merkle") })
			: new[] { FullName(method, profile) };
		Assert.That(benchmarks.Select(item => item.GetProperty("FullName").GetString()), Is.EquivalentTo(expectedNames),
			"Declared profiles and abbreviated display names are not evidence that their exact benchmark ran.");
		ValidateConsole(File.ReadAllText(consolePath), expectedNames, warm);
		var benchmark = benchmarks.Single(item => item.GetProperty("FullName").GetString() == FullName(method, profile));
		Assert.That(benchmark.GetProperty("Method").GetString(), Is.EqualTo(method));
		Assert.That(benchmark.GetProperty("Parameters").GetString(), Is.EqualTo("Profile=" + profile));
		var samples = ValidateMeasurements(benchmark, warm);
		var statistics = benchmark.GetProperty("Statistics");
		var original = statistics.GetProperty("OriginalValues").EnumerateArray().Select(item => item.GetDouble()).ToArray();
		Assert.That(statistics.GetProperty("N").GetInt32(), Is.EqualTo(20));
		Assert.That(original, Has.Length.EqualTo(20));
		Assert.That(original.OrderBy(value => value).ToArray(), Is.EqualTo(samples.Select(item => item.Normalized).OrderBy(value => value).ToArray()).Within(0.0001));
		var mean = statistics.GetProperty("Mean").GetDouble();
		Assert.That(mean, Is.EqualTo(original.Average()).Within(Math.Max(0.0001, mean * 1E-12)));
		var allocated = benchmark.GetProperty("Memory").GetProperty("BytesAllocatedPerOperation").GetDouble();
		Assert.That(double.IsFinite(allocated) && allocated >= 0, Is.True);
		var metrics = benchmark.GetProperty("Metrics").EnumerateArray()
			.Where(item => item.GetProperty("Descriptor").GetProperty("Id").GetString() == "Allocated Memory").ToArray();
		Assert.That(metrics, Has.Length.EqualTo(1));
		Assert.That(metrics[0].GetProperty("Value").GetDouble(), Is.EqualTo(allocated));
		var storageBytes = ValidateRankings(csvPath, method, profile, storage, comparisonGroup, mean, allocated, operations, expectedNames.Length);
		return new Capture(run.Clone(), host.Clone(), arguments, samples, mean, statistics.GetProperty("ConfidenceInterval").GetProperty("Margin").GetDouble(), allocated, storageBytes, operations);
	}

	private static void ValidateProfile(JsonElement profile, string storage, bool selectedImplementations) {
		var name = profile.GetProperty("Name").GetString();
		Assert.That(name, selectedImplementations ? Is.AnyOf("btree", "btreeplus") : Is.AnyOf("plain", "merkle", "merkle-auto"));
		var configuration = profile.GetProperty("Configuration");
		if (selectedImplementations)
			Assert.That(configuration.GetProperty("IndexBTreeImplementation").GetString(), Is.EqualTo(name == "btree" ? "BTree" : "BTreePlus"));
		Assert.That(configuration.GetProperty("SchemaProfile").GetString(), Is.EqualTo("PayloadOnly"));
		Assert.That(configuration.GetProperty("Storage").GetString(), Is.EqualTo(storage));
		Assert.That(configuration.GetProperty("IndexProfile").GetString(), Is.EqualTo("Full"));
		Assert.That(configuration.GetProperty("IndexBTreeOrder").GetInt32(), Is.EqualTo(64));
		Assert.That(configuration.GetProperty("ClusterSize").GetInt32(), Is.EqualTo(4096));
		Assert.That(configuration.GetProperty("AverageRecordSizeBytes").GetInt32(), Is.EqualTo(256));
		Assert.That(configuration.GetProperty("PageSize").GetInt32(), Is.EqualTo(262144));
		Assert.That(configuration.GetProperty("MaxMemory").GetInt64(), Is.EqualTo(16777216));
		Assert.That(configuration.GetProperty("Policy").GetString(), Is.EqualTo("Default"));
		Assert.That(configuration.GetProperty("Endianness").GetString(), Is.EqualTo("LittleEndian"));
		Assert.That(configuration.GetProperty("HashFunction").GetString(), Is.EqualTo("SHA2_256"));
		Assert.That(configuration.GetProperty("Merkleized").GetBoolean(), Is.EqualTo(name != "plain"));
		Assert.That(configuration.GetProperty("AutoSave").GetBoolean(), Is.EqualTo(name == "merkle-auto"));
		Assert.That(profile.GetProperty("ComparisonGroup").GetString(), Is.EqualTo(storage + "/PayloadOnly/Full/checks-True/" + (name == "plain" ? "no-merkle" : "merkle-SHA2_256")));
	}

	private static void ValidateArguments(string[] arguments, bool warm) {
		foreach (var (option, expected) in new[] { ("--launchCount", "2"), ("--iterationCount", "10"), ("--warmupCount", "5"), ("--outliers", "DontRemove") })
			Assert.That(ArgumentsAfter(arguments, option), Is.EqualTo(new[] { expected }), option);
		Assert.That(arguments, Does.Contain("--noForcedGCs"));
		Assert.That(arguments, Does.Contain("--keepFiles"));
		Assert.That(ArgumentsAfter(arguments, "--strategy"), Is.EqualTo(new[] { warm ? "Throughput" : "Monitoring" }));
		if (warm)
			Assert.That(ArgumentsAfter(arguments, "--iterationTime"), Is.EqualTo(new[] { "250" }));
		else {
			Assert.That(ArgumentsAfter(arguments, "--invocationCount"), Is.EqualTo(new[] { "1" }));
			Assert.That(ArgumentsAfter(arguments, "--unrollFactor"), Is.EqualTo(new[] { "1" }));
		}
	}

	private static Sample[] ValidateMeasurements(JsonElement benchmark, bool warm) {
		var measurements = benchmark.GetProperty("Measurements").EnumerateArray().ToArray();
		Assert.That(measurements.Any(item => IsMeasurement(item, "Overhead", "Actual")), Is.False,
			"These retained jobs disable overhead subtraction; actual/result batch normalization must reconcile directly.");
		var actual = measurements.Where(item => IsMeasurement(item, "Workload", "Actual")).ToArray();
		var results = measurements.Where(item => IsMeasurement(item, "Workload", "Result")).ToArray();
		var warming = measurements.Where(item => IsMeasurement(item, "Workload", "Warmup")).ToArray();
		Assert.That(actual, Has.Length.EqualTo(20));
		Assert.That(results, Has.Length.EqualTo(20), "DontRemove retains every measured batch, including reported outliers.");
		Assert.That(warming, Has.Length.EqualTo(10));
		foreach (var stage in new[] { (Items: actual, Count: 10), (Items: results, Count: 10), (Items: warming, Count: 5) }) {
			Assert.That(stage.Items.Select(item => item.GetProperty("LaunchIndex").GetInt32()).Distinct().OrderBy(value => value), Is.EqualTo(new[] { 1, 2 }));
			foreach (var launch in new[] { 1, 2 }) {
				var rows = stage.Items.Where(item => item.GetProperty("LaunchIndex").GetInt32() == launch).ToArray();
				Assert.That(rows, Has.Length.EqualTo(stage.Count));
				Assert.That(rows.Select(item => item.GetProperty("IterationIndex").GetInt32()).OrderBy(value => value), Is.EqualTo(Enumerable.Range(1, stage.Count)));
				foreach (var row in rows) {
					var sample = ReadSample(row);
					Assert.That(sample.Operations, warm ? Is.GreaterThan(0L) : Is.EqualTo(1L));
				}
			}
		}
		var samples = actual.Select(ReadSample).OrderBy(item => item.Launch).ThenBy(item => item.Iteration).ToArray();
		var retained = results.Select(ReadSample).OrderBy(item => item.Launch).ThenBy(item => item.Iteration).ToArray();
		for (var index = 0; index < samples.Length; index++) {
			Assert.That((retained[index].Launch, retained[index].Iteration, retained[index].Operations), Is.EqualTo((samples[index].Launch, samples[index].Iteration, samples[index].Operations)));
			Assert.That(retained[index].Normalized, Is.EqualTo(samples[index].Normalized).Within(0.0001));
		}
		return samples;
	}

	private static Sample ReadSample(JsonElement row) {
		var operations = row.GetProperty("Operations").GetInt64();
		var nanoseconds = row.GetProperty("Nanoseconds").GetDouble();
		Assert.That(operations, Is.GreaterThan(0));
		Assert.That(double.IsFinite(nanoseconds) && nanoseconds > 0, Is.True);
		return new Sample(row.GetProperty("LaunchIndex").GetInt32(), row.GetProperty("IterationIndex").GetInt32(), operations, nanoseconds, nanoseconds / operations);
	}

	private static void ValidateConsole(string text, string[] expectedNames, bool warm) {
		Assert.That(Regex.IsMatch(text, @"(?im)^.*(?:Unhandled exception|// ERROR:|Failed to build|There are not any results runs|No benchmarks were selected).*$"), Is.False,
			"Retain successful complete logs; failed workers or oracle cleanup must not disappear from the evidence.");
		var completed = new List<string>();
		string pending = null;
		foreach (var line in text.Split('\n').Select(line => line.TrimEnd('\r'))) {
			if (line.StartsWith("// Execute: dotnet ", StringComparison.Ordinal)) {
				Assert.That(pending, Is.Null, "The previous worker has no retained exit status.");
				var match = WorkerCommand.Match(line);
				Assert.That(match.Success, Is.True, "Cannot parse the original measured worker command.");
				pending = JsonSerializer.Deserialize<string>(match.Groups["name"].Value);
				Assert.That(pending, Is.AnyOf(expectedNames));
				var job = JsonSerializer.Deserialize<string>(match.Groups["job"].Value);
				foreach (var control in new[] { "OutlierMode=DontRemove", "Force=False", "IterationCount=10", "LaunchCount=2", "WarmupCount=5", "RunStrategy=" + (warm ? "Throughput" : "Monitoring") })
					Assert.That(job, Does.Contain(control), "Original worker command differs from workload-run.json.");
			} else if (line.StartsWith("// Benchmark Process ", StringComparison.Ordinal) && line.Contains(" has exited with code ", StringComparison.Ordinal)) {
				var match = WorkerExit.Match(line);
				Assert.That(match.Success, Is.True);
				Assert.That(pending, Is.Not.Null, "An exit must belong to an exact preceding worker benchmark.");
				Assert.That(int.Parse(match.Groups["pid"].Value, CultureInfo.InvariantCulture), Is.GreaterThan(0));
				Assert.That(int.Parse(match.Groups["code"].Value, CultureInfo.InvariantCulture), Is.Zero, "Worker/oracle failure invalidates the capture.");
				completed.Add(pending);
				pending = null;
			}
		}
		Assert.That(pending, Is.Null, "The final worker did not retain an exit status.");
		Assert.That(completed, Has.Count.EqualTo(expectedNames.Length * 2));
		foreach (var name in expectedNames)
			Assert.That(completed.Count(item => item == name), Is.EqualTo(2), "Two independent successful workers are required for " + name);
		var summaries = Regex.Matches(text, @"(?m)^Global total time:.*executed benchmarks: (\d+)");
		Assert.That(summaries, Has.Count.EqualTo(1));
		Assert.That(int.Parse(summaries[0].Groups[1].Value, CultureInfo.InvariantCulture), Is.EqualTo(expectedNames.Length));
	}

	private static long ValidateRankings(string path, string method, string profile, string storage, string comparisonGroup, double mean, double allocated, int operations, int expectedRows) {
		var rows = File.ReadLines(path).Where(line => !string.IsNullOrWhiteSpace(line)).Select(ParseCsvLine).ToArray();
		var expectedHeader = "Workload,Job,Storage,ComparisonGroup,Profile,Rank,MeanNanoseconds,StandardErrorNanoseconds,StandardDeviationNanoseconds,Samples,ItemsPerInvocation,ItemsPerSecond,BatchAllocatedBytes,StorageBytes".Split(',');
		Assert.That(rows[0], Is.EqualTo(expectedHeader));
		Assert.That(rows, Has.Length.EqualTo(expectedRows + 1));
		Assert.That(rows.Skip(1).All(row => row.Length == expectedHeader.Length), Is.True);
		var selected = rows.Skip(1).Where(row => row[0] == method && row[4] == profile).ToArray();
		Assert.That(selected, Has.Length.EqualTo(1));
		var item = selected[0];
		Assert.That(item[2], Is.EqualTo(storage));
		Assert.That(item[3], Is.EqualTo(comparisonGroup));
		Assert.That(int.Parse(item[9], CultureInfo.InvariantCulture), Is.EqualTo(20));
		Assert.That(int.Parse(item[10], CultureInfo.InvariantCulture), Is.EqualTo(operations));
		Assert.That(double.Parse(item[6], CultureInfo.InvariantCulture), Is.EqualTo(mean).Within(Math.Max(0.0001, mean * 1E-12)));
		Assert.That(double.Parse(item[12], CultureInfo.InvariantCulture), Is.EqualTo(allocated));
		Assert.That(double.Parse(item[11], CultureInfo.InvariantCulture), Is.EqualTo(operations * 1E9 / mean).Within(Math.Max(0.0001, operations * 1E-3 / mean)));
		var bytes = long.Parse(item[13], CultureInfo.InvariantCulture);
		Assert.That(bytes, Is.GreaterThan(0));
		return bytes;
	}

	private static string[] ParseCsvLine(string line) {
		var fields = CsvField.Matches(line).ToArray();
		if (fields.Sum(field => field.Length) != line.Length)
			throw new FormatException("Incomplete or malformed retained CSV row.");
		return fields.Select(field => field.Groups["quoted"].Success ? field.Groups["quoted"].Value.Replace("\"\"", "\"") : field.Groups["plain"].Value).ToArray();
	}

	private static string[] ArgumentsAfter(string[] arguments, string option) {
		Assert.That(arguments.Count(item => item == option), Is.EqualTo(1), "Missing or duplicate argument " + option);
		return arguments.Skip(Array.IndexOf(arguments, option) + 1).TakeWhile(item => !item.StartsWith("--", StringComparison.Ordinal)).ToArray();
	}

	private static bool IsMeasurement(JsonElement item, string mode, string stage)
		=> item.GetProperty("IterationMode").GetString() == mode && item.GetProperty("IterationStage").GetString() == stage;

	private static string FullName(string method, string profile) {
		var type = method == "CleanFlush" ? "ObjectSpaceMerkleBenchmarks" : method == "WarmIdentityLookup" ? "ObjectSpaceWarmReadBenchmarks" : "ObjectSpaceMutationBenchmarks";
		return "Sphere10.Framework.Utils.Profiler.ObjectSpaces." + type + "." + method + "(Profile: " + JsonSerializer.Serialize(profile) + ")";
	}

	private static JsonObject PairedSettings(JsonElement run) {
		var settings = JsonNode.Parse(run.GetProperty("Settings").GetRawText()).AsObject();
		foreach (var name in new[] { "RunId", "OutputDirectory", "DataDirectory" })
			settings.Remove(name);
		return settings;
	}

	private static string RequiredFile(string directory, string name) {
		var path = Path.Combine(directory, name);
		Assert.That(File.Exists(path), Is.True, "A present campaign must retain every complete before/after capture: " + path);
		TestContext.AddTestAttachment(path, name);
		return path;
	}

	private static void WriteCapture(string stage, Capture capture) {
		TestContext.Out.WriteLine(FormattableString.Invariant($"{stage}: mean {capture.Mean / 1E6:F4} ms/batch; reported interval half-width {capture.IntervalMargin / 1E6:F4} ms; {capture.AllocatedBytes:N0} allocated B/batch; {capture.StorageBytes:N0} baseline storage bytes."));
		TestContext.Out.WriteLine("2 workers x 10 actual batches, 5 warmups each; all outliers retained; no forced GC.");
		foreach (var sample in capture.Samples)
			TestContext.Out.WriteLine(FormattableString.Invariant($"{stage} L{sample.Launch}/I{sample.Iteration}: {sample.Nanoseconds:R} ns / {sample.Operations} invocations = {sample.Normalized:R} ns/batch; {sample.Normalized / capture.ItemsPerBatch:R} ns/item."));
	}

	private sealed record Sample(int Launch, int Iteration, long Operations, double Nanoseconds, double Normalized);
	private sealed record Capture(JsonElement Run, JsonElement Host, string[] Arguments, Sample[] Samples, double Mean, double IntervalMargin, double AllocatedBytes, long StorageBytes, int ItemsPerBatch);
}
