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
using System.Security.Cryptography;
using System.Text.Json;
using NUnit.Framework;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces;
using Sphere10.Framework.Utils.Profiler.Studies;

namespace Sphere10.Framework.Utils.Profiler.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class RetainedPerformanceReportTests {
	[TestCase(null)]
	[TestCase("20261013-objectspaces-bdn-confirmations")]
	[Explicit("Requires historical captures supplied through ProfilerResultsRoot.")]
	[Category("RecordedPerformance")]
	public void ActualCombinedAndBenchmarkOnlyDirectoriesExposeTheirExactProfilesWithoutWriting(string relativeDirectory) {
		var root = ProfilerEvidence.GetRoot();
		var directory = relativeDirectory == null ? root : Path.Combine(root, relativeDirectory);
		Assert.That(Directory.Exists(directory), Is.True);
		var before = Snapshot(directory);
		using var output = new StringWriter(CultureInfo.InvariantCulture);
		Assert.That(RetainedPerformanceReports.Write(directory, output), Is.Zero);
		Assert.That(Snapshot(directory), Is.EqualTo(before), "Rendering retained evidence must not create, replace or rewrite files.");
		var text = output.ToString();
		Assert.That(text, Does.Contain("BenchmarkDotNet retained measurements:"));
		if (relativeDirectory == null)
			Assert.That(text, Does.Contain("Direct screening evidence:"));
		else
			Assert.That(text, Does.Not.Contain("Direct screening evidence:"));
		var count = 0;
		foreach (var path in Directory.GetFiles(directory, "*-report-full.json", SearchOption.AllDirectories)) {
			using var report = JsonDocument.Parse(File.ReadAllText(path));
			var runPath = LocateRun(path);
			using var run = JsonDocument.Parse(File.ReadAllText(runPath));
			foreach (var benchmark in report.RootElement.GetProperty("Benchmarks").EnumerateArray()) {
				var fullName = benchmark.GetProperty("FullName").GetString();
				var profiles = run.RootElement.GetProperty("Settings").GetProperty("Profiles").EnumerateArray()
					.Where(profile => fullName.EndsWith("(Profile: " + JsonSerializer.Serialize(profile.GetProperty("Name").GetString()) + ")", StringComparison.Ordinal)).ToArray();
				Assert.That(profiles, Has.Length.EqualTo(1));
				Assert.That(text, Does.Contain(fullName), "The abbreviated Parameters display must not replace the full measured identity.");
				Assert.That(text, Does.Contain($"Selected exact profile '{profiles[0].GetProperty("Name").GetString()}': {profiles[0].GetRawText()}"));
				var samples = benchmark.GetProperty("Statistics").GetProperty("OriginalValues").EnumerateArray().Select(item => item.GetDouble()).ToArray();
				var expected = FormattableString.Invariant($"Mean {samples.Average() / 1000000:F4} ms/batch (one invocation); {samples.Length} samples.");
				Assert.That(text, Does.Contain(expected));
				count++;
			}
		}
		Assert.That(count, Is.GreaterThanOrEqualTo(18), "Actual measured reports, not synthetic data, must cover the completed ObjectSpace confirmation matrix.");
		TestContext.Out.WriteLine($"Read-only report exposed {count} actual benchmark identities and exact configurations; all input hashes and modification times remained unchanged.");
	}

	[Test]
	public void LongFullNameMapsToTheExactProfileWhenParametersAreAbbreviated() {
		using var fixture = new SyntheticReport();
		fixture.Write();
		var before = Snapshot(fixture.Directory);
		using var output = new StringWriter(CultureInfo.InvariantCulture);
		Assert.That(RetainedPerformanceReports.Write(fixture.Directory, output), Is.Zero);
		Assert.That(Snapshot(fixture.Directory), Is.EqualTo(before));
		Assert.That(output.ToString(), Does.Contain(fixture.FullName));
		Assert.That(output.ToString(), Does.Contain("Selected exact profile '" + SyntheticReport.ProfileName + "'"));
		Assert.That(output.ToString(), Does.Contain("99.9%"));
		Assert.That(output.ToString(), Does.Contain("Raw nanoseconds per invocation: 2000000, 3000000, 4000000"));
		Assert.That(output.ToString(), Does.StartWith("BenchmarkDotNet retained measurements:"));
		Assert.That(output.ToString(), Does.Not.Contain("Recorded objective:"), "A directory without a selection manifest retains the existing report behavior.");
	}

	[Test]
	public void RecordedDefaultDecisionsLeadTheReportAndRetainTheirExactSourcesWithoutWriting() {
		using var fixture = new SyntheticReport();
		fixture.Write();
		var manifestPath = Path.Combine(fixture.Directory, "final-default-selection.json");
		var benchmarkSource = new { Kind = "BenchmarkDotNet", RelativeDirectory = "selected-confirmation", ReportFile = "results/selected-report-full.json",
			Profile = "explicit-selected-profile", Method = "MixedAndPersist", SettingPointer = "/Order" };
		var studySource = new { Kind = "Study", RelativeDirectory = "selected-pressure-study", CaseId = "explicit-selected-case",
			SettingPointer = "/ObjectSpaces/Profiles/0/Configuration/MaxMemory" };
		var manifest = new {
			Objective = "Balanced throughput", HumanRationale = "Recorded tradeoff chosen by the user.",
			Decisions = new object[] {
				new { Constant = "ObjectSpaceIndexBTreeOrder", Value = 173, Source = benchmarkSource, Rationale = "Use the recorded candidate, irrespective of live Core values or other report means." },
				new { Constant = "ObjectSpaceMaxMemory", Value = 8192, Source = studySource, Rationale = "Retain this exact budget choice and its source case." }
			}
		};
		File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, ProfilerSettings.JsonOptions));
		var before = Snapshot(fixture.Directory);
		using var output = new StringWriter(CultureInfo.InvariantCulture);
		Assert.That(RetainedPerformanceReports.Write(fixture.Directory, output), Is.Zero);
		var text = output.ToString();
		Assert.That(text, Does.StartWith($"Recorded default selection: {manifestPath}{Environment.NewLine}Recorded objective: Balanced throughput{Environment.NewLine}"));
		Assert.That(text, Does.Contain("Recorded objective rationale: " + manifest.HumanRationale));
		Assert.That(text, Does.Contain("ObjectSpaceIndexBTreeOrder = 173; Source: " + JsonSerializer.Serialize(benchmarkSource)));
		Assert.That(text, Does.Contain("ObjectSpaceMaxMemory = 8192; Source: " + JsonSerializer.Serialize(studySource)));
		Assert.That(text, Does.Contain("Rationale: " + "Retain this exact budget choice and its source case."));
		Assert.That(text.IndexOf("ObjectSpaceMaxMemory =", StringComparison.Ordinal), Is.LessThan(text.IndexOf("BenchmarkDotNet retained measurements:", StringComparison.Ordinal)));
		Assert.That(Snapshot(fixture.Directory), Is.EqualTo(before), "Rendering the recorded decision must not evaluate its source paths, change evidence or derive replacement defaults.");
	}

	[TestCase("missing-statistics")]
	[TestCase("null-statistics")]
	[TestCase("wrong-count")]
	[TestCase("string-count")]
	[TestCase("wrong-mean")]
	[TestCase("string-mean")]
	[TestCase("empty-samples")]
	[TestCase("negative-sample")]
	[TestCase("non-numeric-sample")]
	[TestCase("missing-interval")]
	[TestCase("interval-excludes-mean")]
	[TestCase("non-numeric-interval")]
	[TestCase("unknown-profile")]
	[TestCase("ambiguous-profile")]
	[TestCase("malformed-full-name")]
	[TestCase("missing-run")]
	public void CorruptSyntheticEvidenceFailsWithItsSourceAndLeavesEveryFileUntouched(string defect) {
		using var fixture = new SyntheticReport();
		fixture.Write(defect);
		var before = Snapshot(fixture.Directory);
		using var output = new StringWriter(CultureInfo.InvariantCulture);
		var exception = Assert.Throws<InvalidDataException>(() => RetainedPerformanceReports.Write(fixture.Directory, output));
		Assert.That(exception.Message, Does.Contain(fixture.ReportPath));
		Assert.That(Snapshot(fixture.Directory), Is.EqualTo(before), "Failure reporting must not repair or rewrite captured evidence.");
	}

	[Test]
	public void SingleSampleExecutionCheckIsDisplayedWithoutInventingConfirmation() {
		using var fixture = new SyntheticReport();
		fixture.Write("single-sample");
		var before = Snapshot(fixture.Directory);
		using var output = new StringWriter(CultureInfo.InvariantCulture);
		Assert.That(RetainedPerformanceReports.Write(fixture.Directory, output), Is.Zero);
		Assert.That(output.ToString(), Does.Contain("Execution check: fewer than three samples; no configuration confirmation"));
		Assert.That(output.ToString(), Does.Not.Contain("BDN reported interval"));
		Assert.That(Snapshot(fixture.Directory), Is.EqualTo(before));
	}

	private static string LocateRun(string reportPath) {
		var directory = Path.GetDirectoryName(reportPath);
		var path = Path.Combine(directory, "run.json");
		return File.Exists(path) ? path : Path.Combine(Path.GetDirectoryName(directory), "run.json");
	}

	private static string[] Snapshot(string directory)
		=> Directory.GetFiles(directory, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal).Select(path => {
			var file = new FileInfo(path);
			return Path.GetRelativePath(directory, path) + "|" + file.Length.ToString(CultureInfo.InvariantCulture) + "|"
				+ file.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture) + "|" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
		}).ToArray();

	// Test arrangement and cleanup use a GUID-owned temporary directory. Only the report operation is asserted to be read-only.
	private sealed class SyntheticReport : IDisposable {
		public const string ProfileName = "file-combined-candidate-with-an-unabridged-name-169";
		private ActionScope<string> _cleanup;

		public SyntheticReport() {
			Directory = Path.Combine(Path.GetTempPath(), "Sphere10RetainedReportTests", Guid.NewGuid().ToString("N"));
			System.IO.Directory.CreateDirectory(Path.Combine(Directory, "results"));
			_cleanup = Tools.Scope.DeleteDirOnDispose(Directory);
		}

		public string Directory { get; }

		public string ReportPath => Path.Combine(Directory, "results", "synthetic-report-full.json");

		public string FullName => "Synthetic.MixedReadWriteAndPersist(Profile: " + JsonSerializer.Serialize(ProfileName) + ")";

		public void Write(string defect = null) {
			var samples = new object[] { 2000000D, 3000000D, 4000000D };
			if (defect == "empty-samples")
				samples = Array.Empty<object>();
			else if (defect == "negative-sample")
				samples[0] = -1D;
			else if (defect == "non-numeric-sample")
				samples[0] = "not-a-duration";
			else if (defect == "single-sample")
				samples = new object[] { 3000000D };
			var interval = new Dictionary<string, object> { ["Lower"] = 2000000D, ["Upper"] = 4000000D, ["Level"] = 12 };
			if (defect == "interval-excludes-mean")
				interval["Lower"] = 3500000D;
			else if (defect == "non-numeric-interval")
				interval["Lower"] = "not-a-duration";
			var statistics = new Dictionary<string, object> { ["OriginalValues"] = samples, ["N"] = samples.Length, ["Mean"] = 3000000D, ["ConfidenceInterval"] = interval };
			if (defect == "wrong-count")
				statistics["N"] = 999;
			else if (defect == "string-count")
				statistics["N"] = "invalid";
			else if (defect == "wrong-mean")
				statistics["Mean"] = 1000000D;
			else if (defect == "string-mean")
				statistics["Mean"] = "invalid";
			else if (defect is "missing-interval" or "single-sample")
				statistics.Remove("ConfidenceInterval");
			var name = defect == "unknown-profile" ? "Synthetic.MixedReadWriteAndPersist(Profile: \"not-declared\")"
				: defect == "malformed-full-name" ? FullName + "-unrelated-tail" : FullName;
			var benchmark = new Dictionary<string, object> { ["FullName"] = name, ["DisplayInfo"] = "Synthetic [Profile=file-(...)169]", ["Parameters"] = "Profile=file-(...)169", ["Statistics"] = statistics };
			if (defect == "missing-statistics")
				benchmark.Remove("Statistics");
			else if (defect == "null-statistics")
				benchmark["Statistics"] = null;
			File.WriteAllText(ReportPath, JsonSerializer.Serialize(new { Benchmarks = new[] { benchmark } }, ProfilerSettings.JsonOptions));
			if (defect == "missing-run")
				return;
			var profiles = new List<object> { new { Name = ProfileName, Configuration = new { Storage = "File", IndexBTreeOrder = 169 } } };
			if (defect == "ambiguous-profile")
				profiles.Add(new { Name = ProfileName, Configuration = new { Storage = "Memory", IndexBTreeOrder = 64 } });
			File.WriteAllText(Path.Combine(Directory, "run.json"), JsonSerializer.Serialize(new { Settings = new { RecordCount = 1024, OperationCount = 200, WorkloadMix = "Balanced", Profiles = profiles } }, ProfilerSettings.JsonOptions));
		}

		public void Dispose() => _cleanup.Dispose();
	}
}
