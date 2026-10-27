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
using System.Xml.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Utils.Profiler.Tests.StreamMappedDictionaries;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
[Explicit("Requires historical captures supplied through ProfilerResultsRoot.")]
[Category("RecordedPerformance")]
public class RecordedStreamMappedDictionaryResults {
	private static readonly XNamespace TrxNamespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
	private static readonly string[] RequiredCases = { "Kvp/Debug", "Kvp/Default", "Kvp/Performance", "Clk/Debug", "Clk/Default", "Clk/Performance" };
	private static readonly Regex HashPattern = new("^[0-9A-Fa-f]{64}$", RegexOptions.CultureInvariant);

	public static IEnumerable<TestCaseData> RetainedCampaigns() {
		if (!ProfilerEvidence.IsConfigured) {
			yield return new TestCaseData((string)null).Ignore(ProfilerEvidence.MissingInputMessage);
			yield break;
		}
		var root = EvidenceRoot;
		if (!Directory.Exists(root) || !Directory.EnumerateFileSystemEntries(root).Any()) {
			yield return new TestCaseData((string)null).SetName("RecordedStreamMappedDictionaryResults.NoLocalCampaign");
			yield break;
		}
		var campaigns = Directory.GetDirectories(root);
		if (campaigns.Length == 0) {
			yield return new TestCaseData("campaign.json").SetName("RecordedStreamMappedDictionaryResults.IncompleteLocalCapture");
			yield break;
		}
		foreach (var directory in campaigns.OrderBy(path => path, StringComparer.Ordinal))
			yield return new TestCaseData(Path.GetRelativePath(root, Path.Combine(directory, "campaign.json")))
				.SetName("RecordedStreamMappedDictionaryResults." + Path.GetFileName(directory));
	}

	[TestCaseSource(nameof(RetainedCampaigns))]
	public void CapturedStagesContainAllSixSuccessfulWorkloadsWithTheirActualIdentities(string manifestRelativePath) {
		if (manifestRelativePath == null)
			Assert.Ignore("No local stream-mapped dictionary performance campaign has been captured.");
		var manifestPath = ResolveRelative(EvidenceRoot, manifestRelativePath);
		Assert.That(File.Exists(manifestPath), Is.True, "A present campaign must contain a complete manifest.");
		TestContext.AddTestAttachment(manifestPath, "Workload, runtime, binary identities and relative raw capture mappings");
		var campaign = JsonSerializer.Deserialize<Campaign>(File.ReadAllText(manifestPath));
		Assert.That(campaign.SchemaVersion, Is.EqualTo(1));
		Assert.That(campaign.Workload.Storage, Is.EqualTo("MemoryStream"));
		Assert.That(campaign.Workload.ClusterSize, Is.EqualTo(32));
		Assert.That(campaign.Workload.MaxItems, Is.EqualTo(250));
		Assert.That(campaign.Workload.Iterations, Is.EqualTo(250));
		Assert.That(campaign.Workload.OperationSeed, Is.EqualTo(31337));
		Assert.That(campaign.Workload.ShuffleSeedCaveat, Is.Not.Null.And.Not.Empty);
		Assert.That(campaign.Runtime, Is.Not.Null.And.Not.Empty);
		Assert.That(campaign.Sdk, Is.Not.Null.And.Not.Empty);
		Assert.That(campaign.Host, Is.Not.Null.And.Not.Empty);
		Assert.That(campaign.NUnitWorkers, Is.Zero);
		Assert.That(campaign.MaxCpuCount, Is.EqualTo(1));
		Assert.That(campaign.Stages, Has.Length.GreaterThanOrEqualTo(2));
		Assert.That(campaign.Stages.Select(stage => stage.Name).Distinct().Count(), Is.EqualTo(campaign.Stages.Length));
		var directory = Path.GetDirectoryName(manifestPath);
		var documents = ReadCaptures(directory, campaign.CaptureHashes);
		var measurements = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal);
		foreach (var stage in campaign.Stages) {
			Assert.That(stage.Configuration, Is.EqualTo("Debug"));
			Assert.That(stage.CoreSha256, Does.Match(HashPattern.ToString()));
			Assert.That(stage.SourceProvenance, Is.Not.Null.And.Not.Empty);
			Assert.That(stage.Cases, Has.Length.EqualTo(6));
			Assert.That(stage.Cases.Select(item => $"{item.Implementation}/{item.Policy}"), Is.EquivalentTo(RequiredCases));
			var values = new Dictionary<string, double>(StringComparer.Ordinal);
			foreach (var capture in stage.Cases) {
				Assert.That(documents.ContainsKey(capture.Trx), Is.True, "Every referenced capture must be hashed and present.");
				Assert.That(capture.SourceProvenance, Is.Not.Null.And.Not.Empty);
				var seconds = ReadSuccessfulCase(documents[capture.Trx], capture.Implementation, capture.Policy, capture.TestClass, capture.TestName);
				values.Add($"{capture.Implementation}/{capture.Policy}", seconds);
				TestContext.WriteLine($"{stage.Name}: {capture.Implementation}/{capture.Policy}: {seconds:F6}s; Core SHA256 {stage.CoreSha256}; {capture.Trx}.");
			}
			measurements.Add(stage.Name, values);
		}
		for (var index = 1; index < campaign.Stages.Length; index++) {
			var stage = campaign.Stages[index];
			var baselineName = stage.ComparisonBase ?? campaign.Stages[index - 1].Name;
			Assert.That(measurements.ContainsKey(baselineName), Is.True);
			var baseline = measurements[baselineName];
			foreach (var key in RequiredCases)
				TestContext.WriteLine($"{baselineName} -> {stage.Name}, {key}: {baseline[key]:F6}s -> {measurements[stage.Name][key]:F6}s ({100 * (1 - measurements[stage.Name][key] / baseline[key]):F2}% less elapsed time).");
			TestContext.WriteLine($"Six-case sum: {baseline.Values.Sum():F6}s -> {measurements[stage.Name].Values.Sum():F6}s. Single-run elapsed evidence includes the unchanged NUnit correctness workload; no timing threshold or universal speed claim is asserted.");
		}
		TestContext.WriteLine(campaign.Workload.ShuffleSeedCaveat);
	}

	internal static double ReadSuccessfulCase(XDocument document, string implementation, string policy, string testClass, string testName) {
		Assert.That(RequiredCases, Does.Contain($"{implementation}/{policy}"));
		var profilerClass = typeof(StreamMappedDictionaryWorkloadTests).FullName;
		var coreClass = implementation == "Clk" ? "Sphere10.Framework.Tests.StreamMappedDictionaryCLKTests" : "Sphere10.Framework.Tests.StreamMappedDictionaryTests";
		Assert.That(testClass, Is.EqualTo(coreClass).Or.EqualTo(profilerClass));
		var implementationPrefix = testClass == profilerClass ? implementation + "," : "";
		Assert.That(testName, Is.EqualTo($"IntegrationTests_Heavy({implementationPrefix}MemoryStream,{policy},250)"));
		var definitions = document.Descendants(TrxNamespace + "UnitTest")
			.ToDictionary(item => (string)item.Attribute("id"), item => item.Element(TrxNamespace + "TestMethod"));
		var matches = document.Descendants(TrxNamespace + "UnitTestResult")
			.Where(item => (string)item.Attribute("testName") == testName &&
				definitions.TryGetValue((string)item.Attribute("testId"), out var method) &&
				(string)method?.Attribute("className") == testClass && (string)method?.Attribute("name") == testName).ToArray();
		Assert.That(matches, Has.Length.EqualTo(1), "A case mapping must identify exactly one real result and its matching definition.");
		Assert.That((string)matches[0].Attribute("outcome"), Is.EqualTo("Passed"));
		var seconds = TimeSpan.Parse((string)matches[0].Attribute("duration"), CultureInfo.InvariantCulture).TotalSeconds;
		Assert.That(seconds, Is.GreaterThan(0));
		return seconds;
	}

	private static string EvidenceRoot => Path.Combine(ProfilerEvidence.GetRoot(), "StreamMappedDictionaries");

	private static Dictionary<string, XDocument> ReadCaptures(string directory, Dictionary<string, string> hashes) {
		Assert.That(hashes, Is.Not.Null.And.Not.Empty);
		var documents = new Dictionary<string, XDocument>(StringComparer.Ordinal);
		foreach (var capture in hashes) {
			var path = ResolveRelative(directory, capture.Key);
			Assert.That(File.Exists(path), Is.True, "A present campaign must retain every declared raw capture.");
			Assert.That(capture.Value, Does.Match(HashPattern.ToString()));
			Assert.That(Convert.ToHexString(Hashers.Hash(CHF.SHA2_256, File.ReadAllBytes(path))), Is.EqualTo(capture.Value).IgnoreCase);
			TestContext.AddTestAttachment(path, "Original raw capture with verified SHA256");
			documents.Add(capture.Key, XDocument.Load(path));
		}
		return documents;
	}

	private static string ResolveRelative(string directory, string relative) {
		Assert.That(Path.IsPathRooted(relative), Is.False, "Evidence paths must be portable relative paths.");
		var path = Path.GetFullPath(Path.Combine(directory, relative));
		Assert.That(path.StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True);
		return path;
	}

	private sealed class Campaign {
		public int SchemaVersion { get; set; }
		public string Runtime { get; set; }
		public string Sdk { get; set; }
		public string Host { get; set; }
		public int NUnitWorkers { get; set; }
		public int MaxCpuCount { get; set; }
		public Workload Workload { get; set; }
		public Dictionary<string, string> CaptureHashes { get; set; }
		public Stage[] Stages { get; set; }
	}

	private sealed class Workload {
		public string Storage { get; set; }
		public int ClusterSize { get; set; }
		public int MaxItems { get; set; }
		public int Iterations { get; set; }
		public int OperationSeed { get; set; }
		public string ShuffleSeedCaveat { get; set; }
	}

	private sealed class Stage {
		public string Name { get; set; }
		public string ComparisonBase { get; set; }
		public string Configuration { get; set; }
		public string CoreSha256 { get; set; }
		public string SourceProvenance { get; set; }
		public CaseCapture[] Cases { get; set; }
	}

	private sealed class CaseCapture {
		public string Implementation { get; set; }
		public string Policy { get; set; }
		public string Trx { get; set; }
		public string TestClass { get; set; }
		public string TestName { get; set; }
		public string SourceProvenance { get; set; }
	}
}
