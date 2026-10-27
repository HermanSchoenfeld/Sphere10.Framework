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

namespace Sphere10.Framework.Utils.Profiler.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
[Explicit("Requires historical captures supplied through ProfilerResultsRoot.")]
[Category("RecordedPerformance")]
public class RecordedBTreeNodeIoTests {
	private static readonly string[] _captureNames = { "baseline-a", "optimized-a", "optimized-b", "baseline-b" };
	private static readonly string[] _workloads = { "Insert", "Update", "DeleteAndRecycle" };

	private static string EvidenceDirectory => Path.Combine(ProfilerEvidence.GetRoot(), "20261023-span-node-io");

	[Test]
	public void CapturesUseOneHarnessAndTwoConsistentFrameworkBinaries() {
		var comparison = ReadComparison();
		var recorded = comparison.GetProperty("Manifests").EnumerateArray().ToArray();
		Assert.That(recorded, Has.Length.EqualTo(4));
		Assert.That(recorded.Select(item => item.GetProperty("Capture").GetString()), Is.EqualTo(_captureNames));
		Assert.That(recorded.Select(item => item.GetProperty("Binary").GetString()), Is.EqualTo(new[] { "baseline", "optimized", "optimized", "baseline" }));
		foreach (var manifest in recorded) {
			var raw = ReadReport(Path.Combine(EvidenceDirectory, "captures", manifest.GetProperty("Capture").GetString(), "manifest.json"));
			foreach (var property in new[] { "Capture", "Binary", "FrameworkSha256", "TestAssemblySha256", "ExitCode" })
				Assert.That(manifest.GetProperty(property).GetRawText(), Is.EqualTo(raw.GetProperty(property).GetRawText()), property);
			Assert.That(manifest.GetProperty("ExitCode").GetInt32(), Is.Zero);
			foreach (var property in new[] { "FrameworkSha256", "TestAssemblySha256" })
				Assert.That(manifest.GetProperty(property).GetString(), Does.Match("^[0-9A-Fa-f]{64}$"));
		}
		Assert.That(recorded.Select(item => item.GetProperty("TestAssemblySha256").GetString()).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(1));
		Assert.That(recorded.Select(item => item.GetProperty("FrameworkSha256").GetString()).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(2));
		foreach (var variant in new[] { "baseline", "optimized" })
			Assert.That(recorded.Where(item => item.GetProperty("Binary").GetString() == variant)
				.Select(item => item.GetProperty("FrameworkSha256").GetString()).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(1));
	}

	[Test]
	public void NodeSummariesMatchEveryRawConfigurationAndPreserveWholeStoredBytes() {
		// Each capture contains five samples for every phase of twelve tree configurations.
		var expected = (from type in new[] { "BTree", "BTreePlus" }
			from order in new[] { 4, 64, 256 }
			from endianness in new[] { "LittleEndian", "BigEndian" }
			from workload in _workloads
			select (type, order, endianness, workload)).ToArray();
		var samples = new List<(string variant, (string type, int order, string endianness, string workload) key, JsonElement value)>();
		foreach (var capture in _captureNames) {
			var directory = Path.Combine(EvidenceDirectory, "captures", capture);
			var variant = ReadReport(Path.Combine(directory, "manifest.json")).GetProperty("Binary").GetString();
			var paths = Directory.GetFiles(Path.Combine(directory, "nodes"), "*.json");
			Assert.That(paths, Has.Length.EqualTo(12), capture);
			foreach (var path in paths) {
				var report = ReadReport(path);
				var type = report.GetProperty("Type").GetString();
				var order = report.GetProperty("Order").GetInt32();
				var endianness = report.GetProperty("Endianness").GetString();
				Assert.That(Path.GetFileName(path), Is.EqualTo($"{type}-{order}-{endianness}.json"));
				Assert.That(report.GetProperty("RecordCount").GetInt32(), Is.EqualTo(512));
				var measurements = report.GetProperty("Measurements").EnumerateArray().ToArray();
				Assert.That(measurements, Has.Length.EqualTo(15));
				foreach (var workload in _workloads) {
					var phase = measurements.Where(item => item.GetProperty("Workload").GetString() == workload).ToArray();
					Assert.That(phase.Select(item => item.GetProperty("Sample").GetInt32()), Is.EquivalentTo(Enumerable.Range(0, 5)), path + ": " + workload);
					samples.AddRange(phase.Select(item => (variant, (type, order, endianness, workload), item)));
				}
			}
		}

		// The summary must cover exactly the raw cases, with identical storage after each mutation phase.
		var comparison = ReadComparison();
		Assert.That(comparison.GetProperty("MatchingStoredByteCases").GetInt32(), Is.EqualTo(36));
		var summaries = comparison.GetProperty("NodeComparisons").EnumerateArray().ToArray();
		Assert.That(summaries.Select(NodeKey), Is.EquivalentTo(expected));
		Assert.That(samples.Select(item => item.key).Distinct(), Is.EquivalentTo(expected));
		foreach (var summary in summaries) {
			var key = NodeKey(summary);
			var matching = samples.Where(item => item.key == key).ToArray();
			var before = matching.Where(item => item.variant == "baseline").Select(item => item.value).ToArray();
			var after = matching.Where(item => item.variant == "optimized").Select(item => item.value).ToArray();
			Assert.That(before, Has.Length.EqualTo(10), key.ToString());
			Assert.That(after, Has.Length.EqualTo(10), key.ToString());
			var hashes = matching.Select(item => item.value.GetProperty("StorageSha256").GetString()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
			var lengths = matching.Select(item => item.value.GetProperty("StreamBytes").GetInt64()).Distinct().ToArray();
			Assert.That(hashes, Has.Length.EqualTo(1), key.ToString());
			Assert.That(hashes[0], Does.Match("^[0-9A-Fa-f]{64}$"));
			Assert.That(lengths, Has.Length.EqualTo(1), key.ToString());
			Assert.That(lengths[0], Is.GreaterThan(0));
			Assert.That(summary.GetProperty("StorageSha256").GetString(), Is.EqualTo(hashes[0]).IgnoreCase);
			Assert.That(summary.GetProperty("StreamBytes").GetInt64(), Is.EqualTo(lengths[0]));
			var beforeBytes = Median(before, "AllocatedBytes");
			var afterBytes = Median(after, "AllocatedBytes");
			Assert.That(summary.GetProperty("BeforeBytes").GetDouble(), Is.EqualTo(beforeBytes));
			Assert.That(summary.GetProperty("AfterBytes").GetDouble(), Is.EqualTo(afterBytes));
			Assert.That(summary.GetProperty("AllocationReductionPercent").GetDouble(), Is.EqualTo(100 * (beforeBytes - afterBytes) / beforeBytes).Within(1e-9));
			Assert.That(summary.GetProperty("BeforeMedianTicks").GetDouble(), Is.EqualTo(Median(before, "ElapsedTicks")));
			Assert.That(summary.GetProperty("AfterMedianTicks").GetDouble(), Is.EqualTo(Median(after, "ElapsedTicks")));
		}
	}

	[Test]
	public void IoAllocationSummariesMatchAllRawWorkloadsAndSizes() {
		var expected = new (string workload, int? byteSize)[] {
			("EndianCharWrite", null), ("ProfilerRead", null), ("ProfilerCaptureRead", null),
			("StringSerialize", 128), ("StringSerialize", 2048), ("StringDeserialize", 128), ("StringDeserialize", 2048),
			("PaddedSerialize", 8192), ("PaddedDeserialize", 8192), ("NullReferencePadding", 8192), ("NullablePadding", 4)
		};
		var samples = new List<(string variant, (string workload, int? byteSize) key, JsonElement value)>();
		foreach (var capture in _captureNames) {
			var directory = Path.Combine(EvidenceDirectory, "captures", capture);
			var variant = ReadReport(Path.Combine(directory, "manifest.json")).GetProperty("Binary").GetString();
			var paths = Directory.GetFiles(Path.Combine(directory, "io"), "*.json");
			Assert.That(paths, Has.Length.EqualTo(11), capture);
			foreach (var path in paths) {
				var report = ReadReport(path);
				Assert.That(report.GetProperty("Iterations").GetInt32(), Is.EqualTo(4096));
				Assert.That(Guid.TryParse(report.GetProperty("FrameworkMvid").GetString(), out var module) && module != Guid.Empty, Is.True);
				var measurements = report.GetProperty("Samples").EnumerateArray().ToArray();
				Assert.That(measurements.Select(item => item.GetProperty("Sample").GetInt32()), Is.EquivalentTo(Enumerable.Range(0, 5)), path);
				samples.AddRange(measurements.Select(item => (variant, IoKey(report), item)));
			}
		}
		var summaries = ReadComparison().GetProperty("IoComparisons").EnumerateArray().ToArray();
		Assert.That(summaries.Select(IoKey), Is.EquivalentTo(expected));
		Assert.That(samples.Select(item => item.key).Distinct(), Is.EquivalentTo(expected));
		foreach (var summary in summaries) {
			var key = IoKey(summary);
			var matching = samples.Where(item => item.key == key).ToArray();
			var before = matching.Where(item => item.variant == "baseline").Select(item => item.value).ToArray();
			var after = matching.Where(item => item.variant == "optimized").Select(item => item.value).ToArray();
			Assert.That(before, Has.Length.EqualTo(10), key.ToString());
			Assert.That(after, Has.Length.EqualTo(10), key.ToString());
			var beforeBytes = Median(before, "AllocatedBytes");
			var afterBytes = Median(after, "AllocatedBytes");
			Assert.That(summary.GetProperty("BeforeBytes").GetDouble(), Is.EqualTo(beforeBytes));
			Assert.That(summary.GetProperty("AfterBytes").GetDouble(), Is.EqualTo(afterBytes));
			var reduction = beforeBytes == 0 ? 0 : 100 * (beforeBytes - afterBytes) / beforeBytes;
			Assert.That(summary.GetProperty("AllocationReductionPercent").GetDouble(), Is.EqualTo(reduction).Within(1e-9));
		}
	}

	private static JsonElement ReadComparison() {
		var path = Path.Combine(EvidenceDirectory, "comparison.json");
		var report = ReadReport(path);
		TestContext.AddTestAttachment(path, "Retained allocation comparison; no timing threshold is asserted");
		return report;
	}

	private static JsonElement ReadReport(string path) {
		Assert.That(File.Exists(path), Is.True, path);
		using var report = JsonDocument.Parse(File.ReadAllText(path));
		return report.RootElement.Clone();
	}

	private static (string type, int order, string endianness, string workload) NodeKey(JsonElement item)
		=> (item.GetProperty("Type").GetString(), item.GetProperty("Order").GetInt32(), item.GetProperty("Endianness").GetString(), item.GetProperty("Workload").GetString());

	private static (string workload, int? byteSize) IoKey(JsonElement item)
		=> (item.GetProperty("Workload").GetString(), item.TryGetProperty("ByteSize", out var size) && size.ValueKind != JsonValueKind.Null ? size.GetInt32() : null);

	private static double Median(IEnumerable<JsonElement> samples, string property) {
		var values = samples.Select(item => item.GetProperty(property).GetDouble()).OrderBy(value => value).ToArray();
		return (values[(values.Length - 1) / 2] + values[values.Length / 2]) / 2;
	}
}
