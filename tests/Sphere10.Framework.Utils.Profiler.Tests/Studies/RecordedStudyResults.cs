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
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Sphere10.Framework.Utils.Profiler.BTrees;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;
using Sphere10.Framework.Utils.Profiler.Studies;

namespace Sphere10.Framework.Utils.Profiler.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class RecordedStudyResults {
	public static IEnumerable<TestCaseData> RetainedStudies() {
		var root = Path.Combine(TestContext.CurrentContext.TestDirectory, "Results");
		var plans = Directory.Exists(root) ? Directory.GetFiles(root, "run-plan.json", SearchOption.AllDirectories) : Array.Empty<string>();
		if (plans.Length == 0) {
			yield return new TestCaseData("missing-evidence").SetName("RecordedStudyResults.RetainedEvidenceIsRequired");
			yield break;
		}
		foreach (var path in plans.OrderBy(path => path, StringComparer.Ordinal)) {
			var relative = Path.GetRelativePath(root, Path.GetDirectoryName(path));
			yield return new TestCaseData(relative).SetName($"RecordedStudyResults.{relative.Replace(Path.DirectorySeparatorChar, '.')}");
		}
	}

	[TestCaseSource(nameof(RetainedStudies))]
	public void MeasurementsReproduceTheDisplayedCandidates(string relativeDirectory) {
		// These are retained measurements, not benchmark execution during a unit-test run.
		var directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "Results", relativeDirectory);
		Assert.That(Directory.Exists(directory), Is.True, "Retain measured study evidence under the profiler test project's Results folder and rebuild the tests.");
		foreach (var name in new[] { "run-plan.json", "host.json", "samples.jsonl", "samples.csv", "results.json", "recommendations.json", "report.md" }) {
			var path = Path.Combine(directory, name);
			Assert.That(File.Exists(path), Is.True, $"Missing retained evidence: {path}");
			TestContext.AddTestAttachment(path, name);
		}
		foreach (var name in new[] { "storage-host.json", "binary-fingerprints.json", "binaryfingerprints.json" }) {
			var path = Path.Combine(directory, name);
			if (File.Exists(path))
				TestContext.AddTestAttachment(path, name);
		}
		var plan = JsonSerializer.Deserialize<StudyPlan>(File.ReadAllText(Path.Combine(directory, "run-plan.json")), ProfilerSettings.JsonOptions);
		var results = StudyEvidence.Read(Path.Combine(directory, "results.json"));
		using var host = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "host.json")));
		var frequency = host.RootElement.GetProperty("Frequency").GetInt64();
		Assert.That(plan, Is.Not.Null);
		Assert.That(results, Is.Not.Null.And.Not.Empty);
		Assert.That(plan.Stage, Is.EqualTo("Screening"));
		Assert.That(plan.SampleCount, Is.GreaterThanOrEqualTo(3));
		Assert.That(plan.WarmupCount, Is.GreaterThanOrEqualTo(1));
		Assert.That(frequency, Is.GreaterThan(0));
		Assert.That(host.RootElement.GetProperty("Stage").GetString(), Is.EqualTo(plan.Stage));
		Assert.That(host.RootElement.GetProperty("CommandArguments").GetArrayLength(), Is.GreaterThan(0));
		if (host.RootElement.TryGetProperty("HasAttachedDebugger", out var debugger))
			Assert.That(debugger.GetBoolean(), Is.False);
		if (host.RootElement.TryGetProperty("Configuration", out var configuration))
			Assert.That(configuration.GetString(), Is.EqualTo("Release").IgnoreCase);
		if (host.RootElement.TryGetProperty("Assemblies", out var assemblies))
			foreach (var assembly in assemblies.EnumerateArray()) {
				Assert.That(assembly.GetProperty("Name").GetString(), Is.Not.Empty);
				Assert.That(assembly.GetProperty("Version").GetString(), Is.Not.Empty);
				Assert.That(Guid.TryParse(assembly.GetProperty("ModuleVersionId").GetString(), out _), Is.True);
				var digest = assembly.GetProperty("Sha256").GetString();
				Assert.That(digest, Has.Length.EqualTo(64));
				Assert.That(digest.All(Uri.IsHexDigit), Is.True);
			}
		Assert.That(results.Select(result => result.Case.Id), Is.EquivalentTo(plan.Cases.Select(item => item.Id)));
		Assert.That(results.Select(result => result.Case.Id).Distinct().Count(), Is.EqualTo(results.Length));

		// Reconcile every duration with its retained clock ticks, and every case with its exact input settings.
		foreach (var result in results) {
			var planned = plan.Cases.Single(item => item.Id == result.Case.Id);
			Assert.That(JsonSerializer.Serialize(result.Case, ProfilerSettings.JsonOptions), Is.EqualTo(JsonSerializer.Serialize(planned, ProfilerSettings.JsonOptions)), result.Case.Id);
			if (result.IsEligible(plan.SampleCount)) {
				Assert.That(result.Samples.Select(sample => sample.Round), Is.EquivalentTo(Enumerable.Range(0, plan.SampleCount)));
				Assert.That(result.Samples.Select(sample => sample.Checksum).Distinct().Count(), Is.EqualTo(1), $"Reset batches returned different checksums: {result.Case.Id}");
			} else {
				Assert.That(string.IsNullOrWhiteSpace(result.Error), Is.False, $"A failed or incomplete case must retain its cause: {result.Case.Id}");
				Assert.That(result.Verified, Is.False, $"An errored case cannot be reported as verified: {result.Case.Id}");
				TestContext.Out.WriteLine($"EXCLUDED failed measurement {result.Case.Id}: {result.Error}");
			}
			foreach (var sample in result.Samples) {
				Assert.That(sample.Round, Is.InRange(0, plan.SampleCount - 1));
				Assert.That(sample.ElapsedTicks, Is.GreaterThan(0));
				Assert.That(sample.AllocatedBytes, Is.GreaterThanOrEqualTo(0));
				Assert.That(sample.Checksum, Is.Not.Null);
				Assert.That(sample.Collections, Has.Length.EqualTo(3));
				Assert.That(sample.Collections.All(count => count >= 0), Is.True);
				var nanoseconds = sample.ElapsedTicks * (1000000000D / frequency);
				Assert.That(sample.Nanoseconds, Is.EqualTo(nanoseconds).Within(Math.Max(0.0001, nanoseconds * 1E-12)), $"{result.Case.Id}, round {sample.Round}");
			}
		}
		VerifyJournal(directory, results);
		VerifyCsv(directory, results);

		// Recompute decisions from the raw samples; no winning order or timing is hard-coded in a test.
		var recommendations = StudyEvidence.Recommend(results, plan.SampleCount);
		var recorded = JsonSerializer.Deserialize<StudyRecommendation[]>(File.ReadAllText(Path.Combine(directory, "recommendations.json")), ProfilerSettings.JsonOptions);
		Assert.That(JsonSerializer.Serialize(recorded, ProfilerSettings.JsonOptions), Is.EqualTo(JsonSerializer.Serialize(recommendations, ProfilerSettings.JsonOptions)));
		Assert.That(NormalizeLineEndings(File.ReadAllText(Path.Combine(directory, "report.md"))),
			Is.EqualTo(NormalizeLineEndings(StudyEvidence.Format(results, plan.SampleCount))));
		foreach (var recommendation in recommendations) {
			var matching = results.Where(result => result.Case.ComparisonGroup == recommendation.ComparisonGroup && result.IsEligible(plan.SampleCount)).ToArray();
			var winner = matching.Single(result => result.Case.Id == recommendation.WinnerId);
			var lowestMean = matching.Min(result => result.Samples.Sum(sample => sample.Nanoseconds) / plan.SampleCount);
			Assert.That(winner.MeanNanoseconds, Is.EqualTo(lowestMean).Within(Math.Max(0.0001, lowestMean * 1E-12)));
			Assert.That(recommendation.IndistinguishableIds, Does.Contain(winner.Case.Id));
			TestContext.Out.WriteLine($"{recommendation.ComparisonGroup}: candidate {winner.Case.Profile}; mean {winner.MeanNanoseconds / 1000000:F4} ms, "
				+ $"approximate 95% half-width {winner.ConfidenceHalfWidth / 1000000:F4} ms, {plan.SampleCount} reset batches; overlapping candidates: "
				+ string.Join(", ", recommendation.IndistinguishableIds.Select(id => matching.Single(result => result.Case.Id == id).Case.Profile)));
			foreach (var result in matching.Where(result => result.HasTemporalDrift))
				TestContext.Out.WriteLine($"WARNING: {result.Case.Profile} has potential temporal drift; chronological half medians differ by {result.TemporalDriftRatio:F2} times. "
					+ "The mean is provisional and every measured sample remains retained.");
		}
		TestContext.Out.WriteLine("Screening uses one process and does not establish a universal optimum. Read the attached exact settings, host, journal and report; independent BenchmarkDotNet confirmations are separate evidence.");
		TestContext.Out.WriteLine(File.ReadAllText(Path.Combine(directory, "report.md")));
	}

	[Test]
	public void CsvExportReconcilesQuotedIdentifiersAndChecksumsWithoutAddingDataDependencies() {
		var directory = Path.Combine(Path.GetTempPath(), "Sphere10EvidenceCsvTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var item = new StudyCase { Id = "case,\"quoted\"\r\nnext", Domain = "object-spaces", ObjectSpaces = new ProfilerSettings {
			Profiles = ObjectSpaceProfile.CreateSuite("baseline", ObjectSpaceLabStorage.Memory)
		} };
		var result = new StudyResult { Case = item, Verified = true, Samples = Enumerable.Range(0, 3).Select(round => new StudySample {
			Round = round, ElapsedTicks = round + 1, Nanoseconds = (round + 1) * 100D, AllocatedBytes = round * 64,
			Collections = new[] { 0, 0, 0 }, Checksum = "123,\"value\"\nnext"
		}).ToArray() };
		StudyEvidence.Write(directory, new StudyPlan { SampleCount = 3, Cases = new[] { item } }, new[] { result });
		VerifyCsv(directory, new[] { result });
	}

	private static void VerifyJournal(string directory, StudyResult[] results) {
		var observed = new HashSet<(string CaseId, int Round)>();
		foreach (var line in File.ReadLines(Path.Combine(directory, "samples.jsonl"))) {
			using var document = JsonDocument.Parse(line);
			var caseId = document.RootElement.GetProperty("CaseId").GetString();
			var sample = JsonSerializer.Deserialize<StudySample>(document.RootElement.GetProperty("Sample"), ProfilerSettings.JsonOptions);
			var retained = results.Single(result => result.Case.Id == caseId).Samples.Single(item => item.Round == sample.Round);
			Assert.That(observed.Add((caseId, sample.Round)), Is.True, $"Duplicate journal entry: {caseId}, round {sample.Round}");
			Assert.That(document.RootElement.GetProperty("Verified").GetBoolean(), Is.True);
			Assert.That(JsonSerializer.Serialize(sample, ProfilerSettings.JsonOptions), Is.EqualTo(JsonSerializer.Serialize(retained, ProfilerSettings.JsonOptions)));
		}
		Assert.That(observed.Count, Is.EqualTo(results.Sum(result => result.Samples.Length)));
	}

	private static string NormalizeLineEndings(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');

	private static void VerifyCsv(string directory, StudyResult[] results) {
		var rows = ParseCsv(File.ReadAllText(Path.Combine(directory, "samples.csv"))).ToArray();
		var columns = new[] { "Case", "Domain", "Profile", "Workload", "Mix", "Group", "Round", "ElapsedTicks", "Nanoseconds", "AllocatedBytes", "Checksum" };
		Assert.That(rows, Is.Not.Empty);
		Assert.That(rows[0], Is.EqualTo(columns));
		var observed = new HashSet<(string CaseId, int Round)>();
		foreach (var row in rows.Skip(1)) {
			Assert.That(row, Has.Length.EqualTo(columns.Length));
			var result = results.Single(item => item.Case.Id == row[0]);
			var round = int.Parse(row[6], CultureInfo.InvariantCulture);
			var sample = result.Samples.Single(item => item.Round == round);
			Assert.That(observed.Add((result.Case.Id, round)), Is.True, $"Duplicate CSV entry: {result.Case.Id}, round {round}");
			Assert.Multiple(() => {
				Assert.That(row[1], Is.EqualTo(result.Case.Domain));
				Assert.That(row[2], Is.EqualTo(result.Case.Profile));
				Assert.That(row[3], Is.EqualTo(result.Case.Workload));
				Assert.That(row[4], Is.EqualTo(result.Case.Mix));
				Assert.That(row[5], Is.EqualTo(result.Case.ComparisonGroup));
				Assert.That(long.Parse(row[7], CultureInfo.InvariantCulture), Is.EqualTo(sample.ElapsedTicks));
				Assert.That(double.Parse(row[8], CultureInfo.InvariantCulture), Is.EqualTo(sample.Nanoseconds));
				Assert.That(long.Parse(row[9], CultureInfo.InvariantCulture), Is.EqualTo(sample.AllocatedBytes));
				Assert.That(row[10], Is.EqualTo(sample.Checksum));
			});
		}
		Assert.That(observed.Count, Is.EqualTo(results.Sum(result => result.Samples.Length)));
	}

	// The Core-only test project has no dependency on the CSV reader in Sphere10.Framework.Data.
	// Parse generated CSV independently, including quoted delimiters, escaped quotes and either newline convention.
	private static IEnumerable<string[]> ParseCsv(string text) {
		var fields = new List<string>();
		var field = new StringBuilder();
		var quoted = false;
		var closedQuote = false;
		for (var index = 0; index < text.Length; index++) {
			var character = text[index];
			if (quoted) {
				if (character != '"')
					field.Append(character);
				else if (index + 1 < text.Length && text[index + 1] == '"') {
					field.Append('"');
					index++;
				} else {
					quoted = false;
					closedQuote = true;
				}
				continue;
			}
			if (character == ',') {
				fields.Add(field.ToString());
				field.Clear();
				closedQuote = false;
			} else if (character is '\r' or '\n') {
				if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
					index++;
				fields.Add(field.ToString());
				yield return fields.ToArray();
				fields.Clear();
				field.Clear();
				closedQuote = false;
			} else if (character == '"' && field.Length == 0 && !closedQuote)
				quoted = true;
			else {
				if (character == '"' || closedQuote)
					throw new FormatException("Unexpected character outside a quoted CSV field.");
				field.Append(character);
			}
		}
		if (quoted)
			throw new FormatException("Unterminated quoted CSV field.");
		if (field.Length > 0 || fields.Count > 0 || closedQuote) {
			fields.Add(field.ToString());
			yield return fields.ToArray();
		}
	}
}

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class RecordedBenchmarkDotNetConfirmations {
	public static IEnumerable<TestCaseData> RetainedConfirmations() {
		var root = Path.Combine(TestContext.CurrentContext.TestDirectory, "Results");
		var reports = Directory.Exists(root) ? Directory.GetFiles(root, "*-report-full.json", SearchOption.AllDirectories) : Array.Empty<string>();
		if (reports.Length == 0) {
			yield return new TestCaseData("missing-confirmations").SetName("RecordedBenchmarkDotNetConfirmations.RetainedConfirmationIsRequired");
			yield break;
		}
		foreach (var path in reports.OrderBy(path => path, StringComparer.Ordinal)) {
			var relative = Path.GetRelativePath(root, path);
			yield return new TestCaseData(relative).SetName($"RecordedBenchmarkDotNetConfirmations.{relative.Replace(Path.DirectorySeparatorChar, '.')}");
		}
	}

	[TestCaseSource(nameof(RetainedConfirmations))]
	public void IndependentProcessMeasurementsExposeTheirSamplesAndUncertainty(string relativePath) {
		var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Results", relativePath);
		Assert.That(File.Exists(path), Is.True, "Retain the finalist BenchmarkDotNet full JSON reports under Results and rebuild the tests.");
		TestContext.AddTestAttachment(path, "BenchmarkDotNet finalist confirmation");
		var directory = Path.GetDirectoryName(Path.GetDirectoryName(path));
		var arguments = ReadRunArguments(directory);
		using var report = JsonDocument.Parse(File.ReadAllText(path));
		var host = report.RootElement.GetProperty("HostEnvironmentInfo");
		Assert.That(host.GetProperty("HasAttachedDebugger").GetBoolean(), Is.False);
		Assert.That(host.GetProperty("Configuration").GetString(), Is.EqualTo("RELEASE").IgnoreCase);
		var benchmarks = report.RootElement.GetProperty("Benchmarks").EnumerateArray().ToArray();
		Assert.That(benchmarks, Is.Not.Empty);
		foreach (var benchmark in benchmarks) {
			var name = benchmark.GetProperty("FullName").GetString();
			var display = benchmark.GetProperty("DisplayInfo").GetString();
			Assert.That(display, Does.Not.Contain(": Dry("), "Dry validates execution; it cannot confirm a preferred configuration.");
			var statistics = benchmark.GetProperty("Statistics");
			Assert.That(statistics.ValueKind, Is.EqualTo(JsonValueKind.Object), name);
			var samples = statistics.GetProperty("OriginalValues").EnumerateArray().Select(item => item.GetDouble()).ToArray();
			Assert.That(samples.Length, Is.GreaterThanOrEqualTo(3), name);
			Assert.That(statistics.GetProperty("N").GetInt32(), Is.EqualTo(samples.Length), name);
			Assert.That(samples.All(value => value > 0 && double.IsFinite(value)), Is.True, name);
			var mean = samples.Average();
			Assert.That(statistics.GetProperty("Mean").GetDouble(), Is.EqualTo(mean).Within(Math.Max(0.0001, mean * 1E-12)), name);
			var interval = statistics.GetProperty("ConfidenceInterval");
			Assert.That(interval.GetProperty("Lower").GetDouble(), Is.LessThanOrEqualTo(mean), name);
			Assert.That(interval.GetProperty("Upper").GetDouble(), Is.GreaterThanOrEqualTo(mean), name);
			var actual = benchmark.GetProperty("Measurements").EnumerateArray().Where(measurement => measurement.GetProperty("IterationMode").GetString() == "Workload"
				&& measurement.GetProperty("IterationStage").GetString() == "Actual").ToArray();
			Assert.That(actual.Length, Is.GreaterThanOrEqualTo(samples.Length), name);
			Assert.That(actual.All(measurement => measurement.GetProperty("Operations").GetInt64() > 0), Is.True, name);
			if (int.TryParse(GetArgument(arguments, "--launchCount"), out var launchCount) && launchCount >= 2)
				VerifyControlledMeasurements(benchmark, arguments);
			TestContext.Out.WriteLine($"{name}: {samples.Length} measured samples; mean {mean / 1000000:F4} ms, "
				+ $"reported interval [{interval.GetProperty("Lower").GetDouble() / 1000000:F4}, {interval.GetProperty("Upper").GetDouble() / 1000000:F4}] ms.");
			TestContext.Out.WriteLine("Raw nanoseconds per invocation: " + string.Join(", ", samples.Select(value => value.ToString("R", CultureInfo.InvariantCulture))));
		}
	}

	[Test]
	public void ControlledMeasuredFinalistsCoverBothObjectSpaceBackendsAndEveryBTreePlusBackendAcrossThreeMixes() {
		var root = Path.Combine(TestContext.CurrentContext.TestDirectory, "Results");
		Assert.That(Directory.Exists(root), Is.True, "Retained independent-process measurements are required.");
		var objectSpaces = new HashSet<(ObjectSpaceLabStorage Storage, ObjectSpaceWorkloadMix Mix)>();
		var btrees = new HashSet<(BTreeStorage Storage, string Mix)>();
		foreach (var path in Directory.GetFiles(root, "*-report-full.json", SearchOption.AllDirectories)) {
			var directory = Path.GetDirectoryName(Path.GetDirectoryName(path));
			var arguments = ReadRunArguments(directory);
			if (GetArgument(arguments, "--launchCount") != "2" || GetArgument(arguments, "--iterationCount") != "10")
				continue;
			using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "run.json")));
			using var report = JsonDocument.Parse(File.ReadAllText(path));
			Assert.That(report.RootElement.GetProperty("HostEnvironmentInfo").GetProperty("HasAttachedDebugger").GetBoolean(), Is.False);
			Assert.That(report.RootElement.GetProperty("HostEnvironmentInfo").GetProperty("Configuration").GetString(), Is.EqualTo("RELEASE").IgnoreCase);
			foreach (var benchmark in report.RootElement.GetProperty("Benchmarks").EnumerateArray()) {
				// A profile merely present in run.json is not evidence that its benchmark ran successfully.
				VerifyControlledMeasurements(benchmark, arguments);
				var fullName = benchmark.GetProperty("FullName").GetString();
				var profiles = manifest.RootElement.GetProperty("Settings").GetProperty("Profiles").EnumerateArray()
					.Where(profile => fullName.EndsWith("(Profile: " + JsonSerializer.Serialize(profile.GetProperty("Name").GetString()) + ")", StringComparison.Ordinal)).ToArray();
				Assert.That(profiles, Has.Length.EqualTo(1), $"The actual measured benchmark must identify exactly one manifest profile: {fullName}");
				var method = benchmark.GetProperty("Method").GetString();
				var settings = manifest.RootElement.GetProperty("Settings");
				if (benchmark.GetProperty("Namespace").GetString() == "Sphere10.Framework.Utils.Profiler.ObjectSpaces" && method == "MixedReadWriteAndPersist") {
					var configuration = profiles[0].GetProperty("Configuration");
					var storage = Enum.Parse<ObjectSpaceLabStorage>(configuration.GetProperty("Storage").GetString());
					var mix = Enum.Parse<ObjectSpaceWorkloadMix>(settings.GetProperty("WorkloadMix").GetString());
					objectSpaces.Add((storage, mix));
					TestContext.Out.WriteLine($"Measured ObjectSpace finalist: {storage}/{mix}, {fullName}; 20 unfiltered samples. Exact profile: {profiles[0].GetRawText()}");
				} else if (benchmark.GetProperty("Namespace").GetString() == "Sphere10.Framework.Utils.Profiler.BTrees" && method == "MixedAndPersist"
					&& profiles[0].GetProperty("Family").GetString() == nameof(BTreeFamily.BTreePlus)) {
					var storage = Enum.Parse<BTreeStorage>(profiles[0].GetProperty("Storage").GetString());
					var mix = settings.GetProperty("Mix").GetString();
					btrees.Add((storage, mix));
					TestContext.Out.WriteLine($"Measured B+ tree finalist: {storage}/{mix}, {fullName}; 20 unfiltered samples. Exact profile: {profiles[0].GetRawText()}");
				}
			}
		}
		Assert.Multiple(() => {
			foreach (var storage in Enum.GetValues<ObjectSpaceLabStorage>())
				foreach (var mix in Enum.GetValues<ObjectSpaceWorkloadMix>())
					Assert.That(objectSpaces, Does.Contain((storage, mix)), $"Missing controlled measured ObjectSpace confirmation: {storage}/{mix}.");
			foreach (var storage in Enum.GetValues<BTreeStorage>())
				foreach (var mix in new[] { "balanced", "read-heavy", "write-heavy" })
					Assert.That(btrees, Does.Contain((storage, mix)), $"Missing controlled measured B+ tree confirmation: {storage}/{mix}.");
		});
	}

	[TestCase("missing-launch")]
	[TestCase("missing-batch")]
	[TestCase("missing-warmup")]
	[TestCase("duplicate-warmup")]
	[TestCase("extra-warmup-launch")]
	[TestCase("invalid-warmup-operations")]
	[TestCase("invalid-warmup-duration")]
	[TestCase("removed-outlier")]
	[TestCase("changed-result")]
	[TestCase("changed-policy")]
	public void ControlledConfirmationRejectsLostMeasurementsOrChangedPolicy(string defect) {
		using var document = CreateControlledReport(defect);
		var arguments = ControlledArguments();
		if (defect == "changed-policy")
			arguments[Array.IndexOf(arguments, "DontRemove")] = "RemoveUpper";
		Assert.That(() => VerifyControlledMeasurements(document.RootElement, arguments), Throws.InstanceOf<AssertionException>());
	}

	[TestCase(5)]
	[TestCase(30)]
	public void ControlledConfirmationReconcilesTwoWorkerLaunchesAndEveryUnfilteredBatch(int warmups) {
		using var document = CreateControlledReport(null, warmups);
		Assert.That(() => VerifyControlledMeasurements(document.RootElement, ControlledArguments(warmups)), Throws.Nothing);
	}

	private static string[] ReadRunArguments(string directory) {
		var path = Path.Combine(directory, "run.json");
		Assert.That(File.Exists(path), Is.True, "Retain the exact run manifest beside each BenchmarkDotNet results directory.");
		TestContext.AddTestAttachment(path, "Exact BenchmarkDotNet arguments and dataset");
		foreach (var name in new[] { "profiles.json", "command.ps1", "command-arguments.json", "interaction-selection.json", "execution-host.json" }) {
			var attachment = Path.Combine(directory, name);
			if (File.Exists(attachment))
				TestContext.AddTestAttachment(attachment, name);
		}
		using var manifest = JsonDocument.Parse(File.ReadAllText(path));
		var arguments = manifest.RootElement.GetProperty("BenchmarkArguments").EnumerateArray().Select(item => item.GetString()).ToArray();
		var commandPath = Path.Combine(directory, "command-arguments.json");
		if (File.Exists(commandPath)) {
			var command = JsonSerializer.Deserialize<string[]>(File.ReadAllText(commandPath));
			foreach (var option in new[] { "--launchCount", "--iterationCount", "--warmupCount", "--strategy", "--invocationCount", "--unrollFactor", "--outliers" })
				Assert.That(GetArgument(arguments, option), Is.EqualTo(GetArgument(command, option)), $"The run differs from its retained command: {option}");
			Assert.That(arguments.Contains("--noForcedGCs"), Is.EqualTo(command.Contains("--noForcedGCs")));
		}
		if (int.TryParse(GetArgument(arguments, "--launchCount"), out var launches) && launches >= 2) {
			var statusPath = Path.Combine(directory, "execution-result.json");
			Assert.That(File.Exists(statusPath), Is.True, "Retain the observed domain exit code; raw statistics alone do not establish successful oracle cleanup.");
			TestContext.AddTestAttachment(statusPath, "Observed domain process result and provenance");
			using var status = JsonDocument.Parse(File.ReadAllText(statusPath));
			Assert.That(status.RootElement.GetProperty("DomainExitCode").GetInt32(), Is.Zero);
			Assert.That(status.RootElement.GetProperty("Origin").GetString(), Is.Not.Empty);
			TestContext.Out.WriteLine("Execution result provenance: " + status.RootElement.GetProperty("Origin").GetString());
			var consolePath = Path.Combine(directory, "console.log");
			Assert.That(File.Exists(consolePath), Is.True, "Retain original worker process exit status alongside controlled measurement reports.");
			TestContext.AddTestAttachment(consolePath, "Original benchmark worker and oracle execution log");
			var workers = Regex.Matches(File.ReadAllText(consolePath), @"Benchmark Process (\d+) has exited with code (-?\d+)\.").ToArray();
			Assert.That(workers.Length, Is.GreaterThanOrEqualTo(launches));
			Assert.That(workers.All(worker => int.Parse(worker.Groups[2].Value, CultureInfo.InvariantCulture) == 0), Is.True,
				"Every recorded worker must complete its oracle-backed cleanup successfully; do not discard failed processes.");
			TestContext.Out.WriteLine($"Original console records {workers.Length} successful worker exits. The domain process exited0; retained benchmark methods validate their results outside the timed work.");
		}
		return arguments;
	}

	private static string GetArgument(string[] arguments, string option) {
		var index = Array.IndexOf(arguments, option);
		return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
	}

	private static void VerifyControlledMeasurements(JsonElement benchmark, string[] arguments) {
		// Full JSON records measurements and launch indices, but not structured job/outlier settings.
		// Verify the retained command and all actual/result samples instead of inferring policy from DisplayInfo.
		Assert.That(int.TryParse(GetArgument(arguments, "--launchCount"), out var launches), Is.True);
		Assert.That(launches, Is.GreaterThanOrEqualTo(2));
		Assert.That(int.TryParse(GetArgument(arguments, "--iterationCount"), out var iterations), Is.True);
		Assert.That(iterations, Is.GreaterThanOrEqualTo(10));
		Assert.That(int.TryParse(GetArgument(arguments, "--warmupCount"), out var warmups), Is.True);
		Assert.That(warmups, Is.GreaterThanOrEqualTo(5));
		Assert.That(GetArgument(arguments, "--strategy"), Is.EqualTo("Monitoring"));
		Assert.That(GetArgument(arguments, "--outliers"), Is.EqualTo("DontRemove"));
		Assert.That(GetArgument(arguments, "--invocationCount"), Is.EqualTo("1"));
		Assert.That(GetArgument(arguments, "--unrollFactor"), Is.EqualTo("1"));
		Assert.That(arguments, Does.Contain("--noForcedGCs"));
		var measurements = benchmark.GetProperty("Measurements").EnumerateArray().ToArray();
		var warming = measurements.Where(item => IsMeasurement(item, "Workload", "Warmup")).ToArray();
		var actual = measurements.Where(item => IsMeasurement(item, "Workload", "Actual")).ToArray();
		var results = measurements.Where(item => IsMeasurement(item, "Workload", "Result")).ToArray();
		Assert.That(measurements.Any(item => IsMeasurement(item, "Overhead", "Actual")), Is.False, "Monitoring does not subtract overhead measurements.");
		var actualLaunches = actual.GroupBy(item => item.GetProperty("LaunchIndex").GetInt32()).ToArray();
		Assert.That(actualLaunches.Length, Is.EqualTo(launches));
		Assert.That(actualLaunches.Select(launch => launch.Key).OrderBy(index => index), Is.EqualTo(Enumerable.Range(1, launches)), "Missing or unexpected worker launch index.");
		Assert.That(warming.Length, Is.EqualTo(launches * warmups), "Every declared warmup batch must be retained.");
		Assert.That(warming.Select(item => item.GetProperty("LaunchIndex").GetInt32()).Distinct().OrderBy(index => index), Is.EqualTo(Enumerable.Range(1, launches)), "Warmups must belong to exactly the declared worker launches.");
		Assert.That(actual.Length, Is.EqualTo(launches * iterations));
		Assert.That(results.Length, Is.EqualTo(actual.Length), "Every measured batch must remain in the results; outliers must not be discarded.");
		foreach (var launch in actualLaunches) {
			var warmed = warming.Where(item => item.GetProperty("LaunchIndex").GetInt32() == launch.Key).ToArray();
			Assert.That(warmed.Length, Is.EqualTo(warmups), $"Worker launch {launch.Key} has incomplete warmup batches.");
			Assert.That(warmed.Select(item => item.GetProperty("IterationIndex").GetInt32()).Distinct().Count(), Is.EqualTo(warmups), "Warmup iteration indexes must be distinct within each launch.");
			foreach (var measurement in warmed)
				NanosecondsPerOperation(measurement);
			Assert.That(launch.Count(), Is.EqualTo(iterations), $"Worker launch {launch.Key} has incomplete actual batches.");
			Assert.That(launch.Select(item => item.GetProperty("IterationIndex").GetInt32()).Distinct().Count(), Is.EqualTo(iterations));
			var retained = results.Where(item => item.GetProperty("LaunchIndex").GetInt32() == launch.Key).ToArray();
			Assert.That(retained.Length, Is.EqualTo(iterations));
			AssertSamplesEqual(launch.Select(NanosecondsPerOperation), retained.Select(NanosecondsPerOperation));
		}
		var samples = benchmark.GetProperty("Statistics").GetProperty("OriginalValues").EnumerateArray().Select(item => item.GetDouble()).ToArray();
		Assert.That(benchmark.GetProperty("Statistics").GetProperty("N").GetInt32(), Is.EqualTo(actual.Length));
		AssertSamplesEqual(results.Select(NanosecondsPerOperation), samples);
		TestContext.Out.WriteLine($"Controlled confirmation: {launches} independent workers × {iterations} actual batches, {warmups} warmups each; "
			+ "DontRemove is declared and every actual batch reconciles with the retained results and statistics. No forced GC; Monitoring; one invocation.");
	}

	private static bool IsMeasurement(JsonElement item, string mode, string stage)
		=> item.GetProperty("IterationMode").GetString() == mode && item.GetProperty("IterationStage").GetString() == stage;

	private static double NanosecondsPerOperation(JsonElement item) {
		var operations = item.GetProperty("Operations").GetInt64();
		Assert.That(operations, Is.GreaterThan(0));
		var nanoseconds = item.GetProperty("Nanoseconds").GetDouble();
		Assert.That(nanoseconds > 0 && double.IsFinite(nanoseconds), Is.True);
		return nanoseconds / operations;
	}

	private static void AssertSamplesEqual(IEnumerable<double> expectedValues, IEnumerable<double> actualValues) {
		var expected = expectedValues.OrderBy(value => value).ToArray();
		var actual = actualValues.OrderBy(value => value).ToArray();
		Assert.That(actual.Length, Is.EqualTo(expected.Length));
		for (var index = 0; index < expected.Length; index++)
			Assert.That(actual[index], Is.EqualTo(expected[index]).Within(Math.Max(0.0001, expected[index] * 1E-12)), "Retained sample does not agree with the measured batch.");
	}

	private static string[] ControlledArguments(int warmups = 5)
		=> new[] { "--launchCount", "2", "--iterationCount", "10", "--warmupCount", warmups.ToString(CultureInfo.InvariantCulture), "--strategy", "Monitoring",
			"--invocationCount", "1", "--unrollFactor", "1", "--outliers", "DontRemove", "--noForcedGCs" };

	private static JsonDocument CreateControlledReport(string defect, int warmups = 5) {
		var measurements = new List<object>();
		var samples = new List<double>();
		for (var launch = 1; launch <= 2; launch++) {
			for (var iteration = 1; iteration <= warmups; iteration++) {
				if (defect == "missing-warmup" && launch == 2 && iteration == warmups)
					continue;
				var changed = launch == 2 && iteration == warmups;
				measurements.Add(new { IterationMode = "Workload", IterationStage = "Warmup",
					LaunchIndex = defect == "extra-warmup-launch" && changed ? 3 : launch,
					IterationIndex = defect == "duplicate-warmup" && changed ? 1 : iteration,
					Operations = defect == "invalid-warmup-operations" && changed ? 0L : 1L,
					Nanoseconds = defect == "invalid-warmup-duration" && changed ? 0D : 1000000D * (launch * warmups + iteration) });
			}
			for (var iteration = 1; iteration <= 10; iteration++) {
				if (defect == "missing-launch" && launch == 2 || defect == "missing-batch" && launch == 2 && iteration == 10)
					continue;
				var elapsed = 1000000D * (launch * 10 + iteration);
				measurements.Add(new { IterationMode = "Workload", IterationStage = "Actual", LaunchIndex = launch, IterationIndex = iteration, Operations = 1L, Nanoseconds = elapsed });
				if (defect == "removed-outlier" && launch == 2 && iteration == 10)
					continue;
				var retained = defect == "changed-result" && launch == 2 && iteration == 10 ? elapsed / 2 : elapsed;
				measurements.Add(new { IterationMode = "Workload", IterationStage = "Result", LaunchIndex = launch, IterationIndex = iteration, Operations = 1L, Nanoseconds = retained });
				samples.Add(retained);
			}
		}
		return JsonDocument.Parse(JsonSerializer.Serialize(new { Measurements = measurements, Statistics = new { N = samples.Count, OriginalValues = samples } }));
	}
}

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class RecordedRecommendations {
	public static IEnumerable<TestCaseData> RetainedGroups() {
		var studies = ReadCompletedStudies().ToArray();
		if (studies.Length == 0) {
			yield return new TestCaseData("missing-evidence", "missing-group").SetName("RecordedRecommendations.CompletedEvidenceIsRequired");
			yield break;
		}
		foreach (var study in studies) {
			var number = 0;
			foreach (var recommendation in StudyEvidence.Recommend(study.Results, study.Plan.SampleCount).OrderBy(item => item.ComparisonGroup, StringComparer.Ordinal)) {
				var item = study.Results.First(result => result.Case.ComparisonGroup == recommendation.ComparisonGroup).Case;
				var backend = item.Domain == "object-spaces" ? item.ObjectSpaces.Profiles.Single().ComparisonGroup
					: $"{item.BTrees.Profiles.Single().Storage}/{item.BTrees.Profiles.Single().Family}/{item.BTrees.Profiles.Single().KeyShape}";
				var title = $"RecordedRecommendations.{study.RelativeDirectory}.{item.Domain}/{backend}/{item.Workload}/{item.Mix}"
					+ (item.Factor == null ? "" : $"/{item.Factor}") + $"/group-{++number}";
				yield return new TestCaseData(study.RelativeDirectory, recommendation.ComparisonGroup).SetName(title);
			}
		}
	}

	[TestCaseSource(nameof(RetainedGroups))]
	public void CandidateAndTiesAreDerivedFromThisWorkloadsRawSamples(string relativeDirectory, string comparisonGroup) {
		var study = ReadCompletedStudies().SingleOrDefault(item => item.RelativeDirectory == relativeDirectory);
		Assert.That(study.Plan, Is.Not.Null, "Only complete retained studies provide recommendations, and only verified measurements are eligible candidates.");
		var grouped = study.Results.Where(result => result.Case.ComparisonGroup == comparisonGroup).ToArray();
		var results = grouped.Where(result => result.IsEligible(study.Plan.SampleCount)).OrderBy(result => result.MeanNanoseconds).ToArray();
		Assert.That(results, Is.Not.Empty);
		var recommendation = StudyEvidence.Recommend(results, study.Plan.SampleCount).Single();
		var winner = results.Single(result => result.Case.Id == recommendation.WinnerId);
		var rawMeans = results.Select(result => new { result.Case.Id, Mean = result.Samples.Sum(sample => sample.Nanoseconds) / study.Plan.SampleCount }).ToArray();
		Assert.That(winner.MeanNanoseconds, Is.EqualTo(rawMeans.Min(item => item.Mean)).Within(Math.Max(0.0001, winner.MeanNanoseconds * 1E-12)));
		var overlapping = results.Where(result => result.MeanNanoseconds - result.ConfidenceHalfWidth <= winner.MeanNanoseconds + winner.ConfidenceHalfWidth)
			.Select(result => result.Case.Id).ToArray();
		Assert.That(recommendation.IndistinguishableIds, Is.EquivalentTo(overlapping));

		// The selected test result is a self-contained view of one backend/family/workload/factor comparison.
		var directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "Results", relativeDirectory);
		foreach (var name in new[] { "run-plan.json", "host.json", "results.json", "report.md", "samples.csv" })
			TestContext.AddTestAttachment(Path.Combine(directory, name), name);
		TestContext.Out.WriteLine($"Comparison: {comparisonGroup}");
		TestContext.Out.WriteLine("Lowest measured mean: " + winner.Case.Profile);
		TestContext.Out.WriteLine("Intervals overlap the fastest candidate: "
			+ string.Join(", ", recommendation.IndistinguishableIds.Select(id => results.Single(result => result.Case.Id == id).Case.Profile)));
		foreach (var excluded in grouped.Where(result => !result.IsEligible(study.Plan.SampleCount))) {
			Assert.That(string.IsNullOrWhiteSpace(excluded.Error), Is.False, "Incomplete cases without a retained failure cannot be quietly omitted.");
			Assert.That(excluded.Verified, Is.False);
			TestContext.Out.WriteLine($"EXCLUDED failed measurement {excluded.Case.Profile}: {excluded.Error}");
		}
		foreach (var result in results) {
			TestContext.Out.WriteLine($"{result.Case.Profile}: mean {result.MeanNanoseconds / 1000000:F4} ms/batch, "
				+ $"approximate 95% half-width {result.ConfidenceHalfWidth / 1000000:F4} ms; allocated {result.MeanAllocatedBytes / 1048576:F4} MiB/batch; "
				+ $"{result.Samples.Length} verified samples.");
			if (result.HasTemporalDrift)
				TestContext.Out.WriteLine($"WARNING: potential temporal drift ({result.TemporalDriftRatio:F2} times between chronological half medians). "
					+ "Confirm after longer warmup; no samples were removed.");
			TestContext.Out.WriteLine("Raw nanoseconds: " + string.Join(", ", result.Samples.Select(sample => sample.Nanoseconds.ToString("R", CultureInfo.InvariantCulture))));
			TestContext.Out.WriteLine("Exact case and dataset controls:");
			TestContext.Out.WriteLine(JsonSerializer.Serialize(result.Case, ProfilerSettings.JsonOptions));
		}
		TestContext.Out.WriteLine("These are measured screening candidates for this exact dataset and capability group. Overlapping intervals do not resolve a winner. Read separate independent-process BenchmarkDotNet confirmation output.");
	}

	[Test]
	public void RetainedMeasurementsCoverBothObjectSpaceBackendsAndEveryBTreeImplementationAcrossAllThreeMixes() {
		var completed = ReadCompletedStudies().ToArray();
		Assert.That(completed, Is.Not.Empty, "Complete measured reports are required; synthetic analyzer samples are not performance evidence.");
		var cases = completed.SelectMany(study => study.Results.Where(result => result.IsEligible(study.Plan.SampleCount)))
			.Select(result => result.Case).Where(item => item.Workload == "Mixed").ToArray();
		Assert.Multiple(() => {
			foreach (var storage in Enum.GetValues<ObjectSpaceLabStorage>())
				foreach (var mix in Enum.GetValues<ObjectSpaceWorkloadMix>())
					Assert.That(cases.Any(item => item.Domain == "object-spaces" && item.ObjectSpaces.Profiles.Single().Configuration.Storage == storage
						&& item.ObjectSpaces.WorkloadMix == mix), Is.True, $"Missing measured ObjectSpace {storage}/{mix}.");
			foreach (var storage in Enum.GetValues<BTreeStorage>())
				foreach (var family in Enum.GetValues<BTreeFamily>())
					foreach (var mix in new[] { "balanced", "read-heavy", "write-heavy" })
						Assert.That(cases.Any(item => item.Domain == "btrees" && item.BTrees.Profiles.Single().Storage == storage
							&& item.BTrees.Profiles.Single().Family == family && item.BTrees.Mix == mix), Is.True, $"Missing measured BTree {storage}/{family}/{mix}.");
		});
		TestContext.Out.WriteLine($"Verified evidence covers Memory/File ObjectSpaces and all six BTree backends × three families × three read/write mixes across {completed.Length} retained studies.");
	}

	private static IEnumerable<(string RelativeDirectory, StudyPlan Plan, StudyResult[] Results)> ReadCompletedStudies() {
		var root = Path.Combine(TestContext.CurrentContext.TestDirectory, "Results");
		if (!Directory.Exists(root))
			yield break;
		foreach (var path in Directory.GetFiles(root, "run-plan.json", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal)) {
			var directory = Path.GetDirectoryName(path);
			var resultPath = Path.Combine(directory, "results.json");
			if (!File.Exists(resultPath))
				continue;
			var plan = JsonSerializer.Deserialize<StudyPlan>(File.ReadAllText(path), ProfilerSettings.JsonOptions);
			var results = StudyEvidence.Read(resultPath);
			if (results.Length != plan.Cases.Length || !results.All(result => result.IsEligible(plan.SampleCount) || !string.IsNullOrWhiteSpace(result.Error) && !result.Verified)
				|| !results.Select(result => result.Case.Id).OrderBy(id => id, StringComparer.Ordinal)
					.SequenceEqual(plan.Cases.Select(item => item.Id).OrderBy(id => id, StringComparer.Ordinal)))
				continue;
			yield return (Path.GetRelativePath(root, directory), plan, results);
		}
	}
}
