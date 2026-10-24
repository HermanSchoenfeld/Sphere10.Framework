// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;
using Sphere10.Framework.Utils.Profiler.BTrees;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;
using Sphere10.Framework.Utils.Profiler.Studies;

namespace Sphere10.Framework.Utils.Profiler.Tests.Studies;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class StudySpecificationReadTests {

	[Test]
	public void LegacyPlanReplayPreservesItsTreeSpecificationWithoutRewritingThePlan(
		[Values("btrees", "object-spaces")] string domain,
		[Values(null, BTreeType.BTree, BTreeType.BTreePlus)] BTreeType? type,
		[Values(false, true)] bool lowercase) {
		var directory = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "run-plan.json");
		var item = CreateCase(domain, type.HasValue ? new BTreeSpec(type.Value, 17) : null);
		var plan = new StudyPlan { Cases = new[] { item }, WarmupCount = 1, SampleCount = 3 };
		var document = JsonSerializer.SerializeToNode(plan, ProfilerSettings.JsonOptions);
		UseLegacyFields(document[nameof(StudyPlan.Cases)][0], domain);
		if (lowercase)
			LowercaseProperties(document);
		File.WriteAllText(path, document.ToJsonString());
		var originalBytes = File.ReadAllBytes(path);
		var outputDirectory = Path.Combine(directory, "uncreated-output");
		var dataDirectory = Path.Combine(directory, "uncreated-data");
		using var output = new StringWriter();

		Assert.That(PerformanceStudy.Run(new[] { "--plan", path, "--show-plan", "--output", outputDirectory, "--data-root", dataDirectory }, output), Is.Zero);
		var replay = JsonSerializer.Deserialize<StudyPlan>(output.ToString(), ProfilerSettings.JsonOptions);
		var restored = replay.Cases.Single();
		var spec = domain == "btrees" ? restored.BTrees.Profiles.Single().Spec : restored.ObjectSpaces.Profiles.Single().Configuration.IndexBTreeSpec;
		Assert.That(spec, Is.EqualTo(type.HasValue ? new BTreeSpec(type.Value, 17) : null));
		Assert.That(domain == "btrees" ? restored.BTrees.Profiles.Single().Order : restored.ObjectSpaces.Profiles.Single().Configuration.IndexBTreeOrder, Is.EqualTo(17));
		Assert.That(restored.ComparisonGroup, Is.EqualTo(item.ComparisonGroup));
		Assert.That(replay.WarmupCount, Is.EqualTo(1));
		Assert.That(replay.SampleCount, Is.EqualTo(3));
		Assert.That(File.ReadAllBytes(path), Is.EqualTo(originalBytes));
		Assert.That(Directory.Exists(outputDirectory), Is.False);
		Assert.That(Directory.Exists(dataDirectory), Is.False);
	}

	[Test]
	public void LegacyResultsRetainTheirTreeSpecificationAndTypedComparisonGroup(
		[Values("btrees", "object-spaces")] string domain,
		[Values(null, BTreeType.BTree, BTreeType.BTreePlus)] BTreeType? type,
		[Values(false, true)] bool lowercase) {
		var directory = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "results.json");
		var item = CreateCase(domain, type.HasValue ? new BTreeSpec(type.Value, 17) : null);
		var result = new StudyResult {
			Case = item,
			Verified = true,
			Samples = Enumerable.Range(0, 3).Select(round => new StudySample {
				Round = round, ElapsedTicks = 100 + round, Nanoseconds = 100 + round,
				AllocatedBytes = 128, Collections = new int[3], Checksum = "42"
			}).ToArray()
		};
		var document = JsonSerializer.SerializeToNode(new[] { result }, ProfilerSettings.JsonOptions);
		UseLegacyFields(document[0][nameof(StudyResult.Case)], domain);
		if (lowercase)
			LowercaseProperties(document);
		File.WriteAllText(path, document.ToJsonString());
		var originalBytes = File.ReadAllBytes(path);

		var restored = StudyEvidence.Read(path).Single();
		var spec = domain == "btrees" ? restored.Case.BTrees.Profiles.Single().Spec : restored.Case.ObjectSpaces.Profiles.Single().Configuration.IndexBTreeSpec;
		Assert.That(spec, Is.EqualTo(type.HasValue ? new BTreeSpec(type.Value, 17) : null));
		Assert.That(domain == "btrees" ? restored.Case.BTrees.Profiles.Single().Order : restored.Case.ObjectSpaces.Profiles.Single().Configuration.IndexBTreeOrder, Is.EqualTo(17));
		Assert.That(restored.Case.ComparisonGroup, Is.EqualTo(item.ComparisonGroup));
		Assert.That(restored.IsEligible(3), Is.True);
		Assert.That(StudyEvidence.Recommend(new[] { restored }, 3).Single().ComparisonGroup, Is.EqualTo(item.ComparisonGroup));
		Assert.That(File.ReadAllBytes(path), Is.EqualTo(originalBytes));
	}

	private static StudyCase CreateCase(string domain, BTreeSpec spec) {
		var item = new StudyCase { Id = "selected", Domain = domain };
		if (domain == "btrees")
			item.BTrees = new BTreeProfilerSettings {
				RecordCount = 16, OperationCount = 4, GroupSize = 4,
				Profiles = new[] { new BTreeProfile { Name = "selected", Family = BTreeFamily.Lookup, Order = 17, Spec = spec } }
			};
		else
			item.ObjectSpaces = new ProfilerSettings {
				RecordCount = 16, OperationCount = 4,
				Profiles = new[] { new ObjectSpaceProfile { Name = "selected", Configuration = new ObjectSpaceLabConfiguration { IndexBTreeOrder = 17, IndexBTreeSpec = spec } } }
			};
		return item;
	}

	private static void UseLegacyFields(JsonNode item, string domain) {
		if (domain == "btrees") {
			var profile = item[nameof(StudyCase.BTrees)][nameof(BTreeProfilerSettings.Profiles)][0].AsObject();
			var spec = profile[nameof(BTreeProfile.Spec)];
			profile["Order"] = spec?[nameof(BTreeSpec.Order)]?.DeepClone() ?? profile["Order"]?.DeepClone();
			profile["Implementation"] = spec?[nameof(BTreeSpec.Type)]?.DeepClone();
			profile.Remove(nameof(BTreeProfile.Spec));
		} else {
			var configuration = item[nameof(StudyCase.ObjectSpaces)][nameof(ProfilerSettings.Profiles)][0][nameof(ObjectSpaceProfile.Configuration)].AsObject();
			var spec = configuration[nameof(ObjectSpaceLabConfiguration.IndexBTreeSpec)];
			configuration["IndexBTreeOrder"] = spec?[nameof(BTreeSpec.Order)]?.DeepClone() ?? configuration["IndexBTreeOrder"]?.DeepClone();
			configuration["IndexBTreeImplementation"] = spec?[nameof(BTreeSpec.Type)]?.DeepClone();
			configuration.Remove(nameof(ObjectSpaceLabConfiguration.IndexBTreeSpec));
		}
	}

	private static void LowercaseProperties(JsonNode node) {
		if (node is JsonObject dictionary)
			foreach (var property in dictionary.ToArray()) {
				dictionary.Remove(property.Key);
				LowercaseProperties(property.Value);
				dictionary[property.Key.ToLowerInvariant()] = property.Value;
			}
		else if (node is JsonArray array)
			foreach (var item in array)
				LowercaseProperties(item);
	}
}
