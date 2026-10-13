// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;
using Sphere10.Framework.Utils.Profiler.BTrees;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;
using Sphere10.Framework.Utils.Profiler.Studies;

namespace Sphere10.Framework.Utils.Profiler.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class PerformanceEvidenceTests {
	[TestCase("unverified")]
	[TestCase("failed")]
	[TestCase("incomplete")]
	[TestCase("duplicate-round")]
	[TestCase("null-sample")]
	[TestCase("zero-ticks")]
	[TestCase("zero-time")]
	[TestCase("nan-time")]
	[TestCase("infinite-time")]
	[TestCase("negative-allocation")]
	[TestCase("missing-checksum")]
	[TestCase("missing-collections")]
	[TestCase("wrong-collection-count")]
	[TestCase("negative-collections")]
	public void InvalidEvidenceCannotBeatVerifiedMeasurements(string defect) {
		// Make malformed evidence faster than the valid candidate so exclusion affects the decision.
		var invalid = Result("invalid", 10, 11, 9);
		var valid = Result("valid", 100, 101, 99);
		switch (defect) {
			case "unverified": invalid.Verified = false; break;
			case "failed": invalid.Error = "Storage verification failed."; break;
			case "incomplete": invalid.Samples = invalid.Samples.Take(2).ToArray(); break;
			case "duplicate-round": invalid.Samples[1].Round = invalid.Samples[0].Round; break;
			case "null-sample": invalid.Samples[1] = null; break;
			case "zero-ticks": invalid.Samples[1].ElapsedTicks = 0; break;
			case "zero-time": invalid.Samples[1].Nanoseconds = 0; break;
			case "nan-time": invalid.Samples[1].Nanoseconds = double.NaN; break;
			case "infinite-time": invalid.Samples[1].Nanoseconds = double.PositiveInfinity; break;
			case "negative-allocation": invalid.Samples[1].AllocatedBytes = -1; break;
			case "missing-checksum": invalid.Samples[1].Checksum = null; break;
			case "missing-collections": invalid.Samples[1].Collections = null; break;
			case "wrong-collection-count": invalid.Samples[1].Collections = new[] { 0, 0 }; break;
			case "negative-collections": invalid.Samples[1].Collections = new[] { 0, -1, 0 }; break;
		}
		Assert.That(invalid.IsEligible(3), Is.False, defect);
		var recommendations = StudyEvidence.Recommend(new[] { invalid, valid }, 3);
		Assert.That(recommendations.Select(item => item.WinnerId), Is.EqualTo(new[] { "valid" }));
		Assert.That(recommendations.Single().IndistinguishableIds, Does.Not.Contain("invalid"));
	}

	[Test]
	public void OverlappingIntervalsRemainCandidatesRatherThanResolvedWinners() {
		var fastest = Result("fastest", 100, 101, 99);
		var uncertain = Result("uncertain", 80, 120, 105);
		var slower = Result("slower", 200, 201, 199);
		var recommendation = StudyEvidence.Recommend(new[] { uncertain, slower, fastest }, 3).Single();
		Assert.That(recommendation.WinnerId, Is.EqualTo("fastest"));
		Assert.That(recommendation.IndistinguishableIds, Is.EquivalentTo(new[] { "fastest", "uncertain" }));
		Assert.That(StudyEvidence.Format(new[] { fastest, uncertain, slower }, 3), Does.Contain("not resolved winners").And.Contain("interval overlaps fastest"));
	}

	[Test]
	public void ConfidenceIntervalUsesSampleDeviationAndSmallSampleUncertainty() {
		// For 10, 20, 30: mean=20, sample deviation=10, and t(0.975, 2)=4.302653.
		var result = Result("sample", 10, 20, 30);
		Assert.That(result.MeanNanoseconds, Is.EqualTo(20));
		Assert.That(result.StandardDeviation, Is.EqualTo(10));
		Assert.That(result.ConfidenceHalfWidth, Is.EqualTo(24.841377).Within(0.005));
		Assert.That(result.ConfidenceHalfWidth, Is.GreaterThan(result.StandardDeviation));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SustainedTemporalChangesWarnWithoutRemovingVerifiedSamples(bool slowdown) {
		var times = slowdown ? new double[] { 1, 1, 1, 1, 7, 11, 11 } : new double[] { 11, 11, 7, 1, 1, 1, 1 };
		var result = Result("drifting", times);
		Assert.That(result.TemporalDriftRatio, Is.EqualTo(11));
		Assert.That(result.HasTemporalDrift, Is.True);
		Assert.That(result.IsEligible(7), Is.True, "Verified measurements remain retained; the stability diagnostic does not discard them.");
		Assert.That(result.Samples, Has.Length.EqualTo(7));
		Assert.That(result.MeanNanoseconds, Is.EqualTo(times.Average()));
		Assert.That(StudyEvidence.Recommend(new[] { result }, 7).Single().WinnerId, Is.EqualTo("drifting"));
		Assert.That(StudyEvidence.Format(new[] { result }, 7), Does.Contain("potential temporal drift (11.00 times"));
	}

	[Test]
	public void DriftUsesChronologicalRoundsAndMedianResistsOneIsolatedSpike() {
		var drifting = Result("drifting", 11, 11, 7, 1, 1, 1, 1);
		var order = new[] { 6, 0, 4, 1, 3, 5, 2 };
		drifting.Samples = order.Select(index => drifting.Samples[index]).ToArray();
		Assert.That(drifting.TemporalDriftRatio, Is.EqualTo(11));
		var isolated = Result("isolated", 100, 1, 1, 1, 1, 1, 1);
		Assert.That(isolated.TemporalDriftRatio, Is.EqualTo(1));
		Assert.That(isolated.HasTemporalDrift, Is.False);
		Assert.That(isolated.Samples, Has.Length.EqualTo(7), "The diagnostic must preserve the slow sample too.");
	}

	[TestCase("storage")]
	[TestCase("indexes")]
	[TestCase("integrity")]
	[TestCase("merkle")]
	[TestCase("records")]
	[TestCase("operations")]
	[TestCase("payload")]
	[TestCase("categories")]
	[TestCase("lookup")]
	[TestCase("mix")]
	public void ObjectSpaceRecommendationsSeparateCapabilitiesAndDatasets(string difference) {
		var first = Result("first", 100, 101, 99);
		var second = Result("second", 10, 11, 9);
		var settings = second.Case.ObjectSpaces;
		var configuration = settings.Profiles.Single().Configuration;
		switch (difference) {
			case "storage": configuration.Storage = ObjectSpaceLabStorage.File; break;
			case "indexes": configuration.IndexProfile = ObjectSpaceLabIndexProfile.IdentifierOnly; break;
			case "integrity": configuration.Policy = ClusteredStreamsPolicy.Performance; break;
			case "merkle": configuration.Merkleized = true; break;
			case "records": settings.RecordCount++; break;
			case "operations": settings.OperationCount++; break;
			case "payload": settings.PayloadBytes++; break;
			case "categories": settings.CategoryCount++; break;
			case "lookup": settings.LookupPattern = ObjectSpaceLookupPattern.Sequential; break;
			case "mix": settings.WorkloadMix = ObjectSpaceWorkloadMix.ReadHeavy; break;
		}
		Assert.That(StudyEvidence.Recommend(new[] { first, second }, 3), Has.Length.EqualTo(2));
	}

	[TestCase("storage")]
	[TestCase("family")]
	[TestCase("keys")]
	[TestCase("groups")]
	[TestCase("records")]
	[TestCase("operations")]
	[TestCase("mix")]
	public void BTreeRecommendationsSeparateImplementationsAndWorkloadSemantics(string difference) {
		var first = Result("first", 100, 101, 99);
		var second = Result("second", 10, 11, 9);
		first.Case = TreeCase("first");
		second.Case = TreeCase("second");
		var settings = second.Case.BTrees;
		var profile = settings.Profiles.Single();
		switch (difference) {
			case "storage": profile.Storage = BTreeStorage.OSMemoryMapped; break;
			case "family": profile.Family = BTreeFamily.Lookup; break;
			case "keys": profile.KeyShape = BTreeKeyShape.Digest32; break;
			case "groups": settings.GroupSize++; break;
			case "records": settings.RecordCount++; break;
			case "operations": settings.OperationCount++; break;
			case "mix": settings.Mix = "read-heavy"; break;
		}
		Assert.That(StudyEvidence.Recommend(new[] { first, second }, 3), Has.Length.EqualTo(2));
	}

	[Test]
	public void AllFailedOrIncompleteCandidatesProduceNoRecommendation() {
		var failed = Result("failed", 100, 101, 99);
		failed.Error = "Incorrect checksum.";
		Assert.That(StudyEvidence.Recommend(new[] { failed }, 3), Is.Empty);
		Assert.That(StudyEvidence.Format(new[] { failed }, 3), Does.Contain("Ineligible failed").And.Contain("Incorrect checksum."));
	}

	[TestCase("Reopen")]
	[TestCase("Rollback")]
	public void LifecycleBatchesCountOneOperation(string workload) {
		var objects = ObjectSpaceCase("objects");
		var tree = TreeCase("tree");
		objects.Workload = tree.Workload = workload;
		Assert.That(objects.Operations, Is.EqualTo(1));
		Assert.That(tree.Operations, Is.EqualTo(1));
	}

	[Test]
	public void PlanRejectsTwoDomainsOnOneCaseBeforeExecution() {
		var item = ObjectSpaceCase("ambiguous");
		item.BTrees = TreeCase("unused").BTrees;
		Assert.That(() => Plan(item).Validate(), Throws.ArgumentException);
	}

	[Test]
	public void PlanRejectsDuplicateCaseIdsAndTooFewSamples() {
		Assert.That(() => Plan(ObjectSpaceCase("same"), ObjectSpaceCase("same")).Validate(), Throws.ArgumentException);
		var plan = Plan(ObjectSpaceCase("only"));
		plan.SampleCount = 2;
		Assert.That(() => plan.Validate(), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase("Range", 12)]
	[TestCase("Group", 6)]
	[TestCase("Rollback", 4)]
	[TestCase("Reopen", 14)]
	public void GeneratedPlansIncludeOnlyImplementationsThatSupportTheRequestedWorkload(string workload, int expectedCases) {
		var plan = PerformanceStudy.CreatePlan("all", "baseline", 80, 24, 32, new[] { workload });
		plan.OutputDirectory = "unused-study-output";
		plan.DataDirectory = "unused-study-data";
		Assert.That(() => plan.Validate(), Throws.Nothing);
		Assert.That(plan.Cases, Has.Length.EqualTo(expectedCases));
		Assert.That(plan.Cases.All(item => item.Workload == workload && item.SupportsWorkload), Is.True);
	}

	[TestCase("objects-range")]
	[TestCase("objects-group")]
	[TestCase("objects-memory-rollback")]
	[TestCase("tree-lookup-range")]
	[TestCase("tree-dictionary-group")]
	[TestCase("tree-file-rollback")]
	[TestCase("tree-managed-reopen")]
	[TestCase("tree-memory-reopen")]
	public void ExplicitUnsupportedPlansFailPreflight(string scenario) {
		var item = scenario.StartsWith("objects", StringComparison.Ordinal) ? ObjectSpaceCase("invalid") : TreeCase("invalid");
		switch (scenario) {
			case "objects-range": item.Workload = "Range"; break;
			case "objects-group": item.Workload = "Group"; break;
			case "objects-memory-rollback": item.Workload = "Rollback"; break;
			case "tree-lookup-range": item.Workload = "Range"; item.BTrees.Profiles[0].Family = BTreeFamily.Lookup; break;
			case "tree-dictionary-group": item.Workload = "Group"; break;
			case "tree-file-rollback": item.Workload = "Rollback"; break;
			case "tree-managed-reopen": item.Workload = "Reopen"; item.BTrees.Profiles[0].Storage = BTreeStorage.ManagedNodes; break;
			case "tree-memory-reopen": item.Workload = "Reopen"; item.BTrees.Profiles[0].Storage = BTreeStorage.StreamMemory; break;
		}
		Assert.That(item.SupportsWorkload, Is.False);
		Assert.That(() => Plan(item).Validate(), Throws.ArgumentException.With.Message.Contains("unsupported"));
	}

	[Test]
	public void LegacyCasesKeepTheirComparisonGroupsWhenFactorIsUndeclared() {
		var item = ObjectSpaceCase("legacy");
		var originalGroup = item.ComparisonGroup;
		var document = JsonSerializer.SerializeToNode(item, ProfilerSettings.JsonOptions).AsObject();
		document.Remove(nameof(StudyCase.Factor));
		var restored = JsonSerializer.Deserialize<StudyCase>(document, ProfilerSettings.JsonOptions);
		Assert.That(restored.Factor, Is.Null);
		Assert.That(restored.ComparisonGroup, Is.EqualTo(originalGroup));
		item.Factor = "OuterCluster";
		Assert.That(item.ComparisonGroup, Is.EqualTo(originalGroup + "/factor-OuterCluster"));
	}

	[TestCase(ObjectSpaceLabStorage.Memory)]
	[TestCase(ObjectSpaceLabStorage.File)]
	public void ObjectSpaceParameterFactorsRetainTheirOwnControlAndChangeOnlyTheirDeclaredSettings(ObjectSpaceLabStorage storage) {
		var plan = PerformanceStudy.CreatePlan("object-spaces", "parameters", 80, 24, 32, new[] { "Read" });
		var cases = plan.Cases.Where(item => item.ObjectSpaces.Profiles.Single().Configuration.Storage == storage).ToArray();
		var expected = new[] { "OuterCluster", "InnerCluster", "Policy", "SaveMode", "Merkle", "Indexes", "Endianness" }
			.Concat(storage == ObjectSpaceLabStorage.File ? new[] { "FilePage", "FileCache" } : Array.Empty<string>()).ToArray();
		Assert.That(cases.Select(item => item.Factor).Distinct(), Is.EquivalentTo(expected));
		var baseline = JsonSerializer.SerializeToNode(ObjectSpaceProfile.CreateSuite("baseline", storage).Single().Configuration, ProfilerSettings.JsonOptions).AsObject();
		foreach (var factor in cases.GroupBy(item => item.Factor)) {
			var configurations = factor.Select(item => JsonSerializer.SerializeToNode(item.ObjectSpaces.Profiles.Single().Configuration, ProfilerSettings.JsonOptions).AsObject()).ToArray();
			Assert.That(configurations.Count(configuration => JsonNode.DeepEquals(configuration, baseline)), Is.EqualTo(1), factor.Key);
			Assert.That(configurations.Select(configuration => configuration.ToJsonString()).Distinct().Count(), Is.EqualTo(configurations.Length), factor.Key);
			var allowed = factor.Key switch {
				"OuterCluster" => new[] { "ClusterSize" }, "InnerCluster" => new[] { "AverageRecordSizeBytes" }, "Policy" => new[] { "Policy" },
				"SaveMode" => new[] { "AutoSave" }, "Merkle" => new[] { "Merkleized", "HashFunction", "AutoSave" }, "Indexes" => new[] { "IndexProfile" },
				"Endianness" => new[] { "Endianness" }, "FilePage" => new[] { "PageSize" }, "FileCache" => new[] { "MaxMemory" },
				_ => throw new InvalidOperationException("Unexpected study factor.")
			};
			foreach (var configuration in configurations) {
				var changes = configuration.Where(property => !JsonNode.DeepEquals(property.Value, baseline[property.Key])).Select(property => property.Key).ToArray();
				Assert.That(changes, Is.SubsetOf(allowed), factor.Key);
			}
		}
	}

	[TestCase(BTreeStorage.FilePaged, "FilePage", 5)]
	[TestCase(BTreeStorage.TransactionalFile, "FilePage", 5)]
	[TestCase(BTreeStorage.FilePaged, "FileCache", 5)]
	[TestCase(BTreeStorage.TransactionalFile, "FileCache", 5)]
	[TestCase(BTreeStorage.FileStream, "FileBuffer", 4)]
	public void BTreeParameterFactorsDeduplicateNamedVariantsAndPreserveAValidControl(BTreeStorage storage, string factor, int expectedProfiles) {
		var plan = PerformanceStudy.CreatePlan("btrees", "parameters", 80, 24, 32, new[] { "Read" });
		var cases = plan.Cases.Where(item => item.Factor == factor && item.BTrees.Profiles.Single().Storage == storage
			&& item.BTrees.Profiles.Single().Family == BTreeFamily.BTreePlus).ToArray();
		var baseline = ProfileWithoutName(BTreeProfile.CreateSuite("baseline", storage, BTreeFamily.BTreePlus, BTreeKeyShape.Int64).Single());
		var profiles = cases.Select(item => ProfileWithoutName(item.BTrees.Profiles.Single())).ToArray();
		Assert.That(profiles, Has.Length.EqualTo(expectedProfiles));
		Assert.That(profiles.Count(profile => JsonNode.DeepEquals(profile, baseline)), Is.EqualTo(1));
		Assert.That(profiles.Select(profile => profile.ToJsonString()).Distinct().Count(), Is.EqualTo(profiles.Length));
		var allowed = factor switch { "FilePage" => nameof(BTreeProfile.PageSize), "FileCache" => nameof(BTreeProfile.CacheBytes), _ => nameof(BTreeProfile.FileBufferBytes) };
		foreach (var profile in profiles) {
			var changes = profile.Where(property => !JsonNode.DeepEquals(property.Value, baseline[property.Key])).Select(property => property.Key).ToArray();
			Assert.That(changes, Is.SubsetOf(new[] { allowed }));
		}
	}

	[TestCase(BTreeStorage.ManagedNodes)]
	[TestCase(BTreeStorage.StreamMemory)]
	[TestCase(BTreeStorage.OSMemoryMapped)]
	public void NonpagedBTreeBackendsDoNotReceiveMeaninglessParameterFactors(BTreeStorage storage) {
		var plan = PerformanceStudy.CreatePlan("btrees", "parameters", 80, 24, 32, new[] { "Read" });
		var cases = plan.Cases.Where(item => item.BTrees.Profiles.Single().Storage == storage).ToArray();
		Assert.That(cases, Has.Length.EqualTo(Enum.GetValues<BTreeFamily>().Length));
		Assert.That(cases.All(item => item.Factor == null && item.Profile == "baseline"), Is.True);
	}

	[Test]
	public void ParameterPlansKeepUniqueCaseIdsAcrossFactorsAndMixes() {
		var plan = PerformanceStudy.CreatePlan("all", "parameters", 80, 24, 32, new[] { "Read", "Mixed" });
		Assert.That(plan.Cases.Select(item => item.Id).Distinct().Count(), Is.EqualTo(plan.Cases.Length));
		Assert.That(plan.Cases.All(item => item.SupportsWorkload), Is.True);
	}

	private static JsonObject ProfileWithoutName(BTreeProfile profile) {
		var document = JsonSerializer.SerializeToNode(profile, ProfilerSettings.JsonOptions).AsObject();
		document.Remove(nameof(BTreeProfile.Name));
		return document;
	}

	private static StudyResult Result(string id, params double[] times) => new() {
		Case = ObjectSpaceCase(id), Verified = true,
		Samples = times.Select((time, round) => new StudySample { Round = round, ElapsedTicks = (long)time,
			Nanoseconds = time, AllocatedBytes = 0, Collections = new[] { 0, 0, 0 }, Checksum = "42" }).ToArray()
	};

	private static StudyCase ObjectSpaceCase(string id) => new() {
		Id = id, Domain = "object-spaces", ObjectSpaces = new ProfilerSettings {
			RecordCount = 80, OperationCount = 24, Profiles = new[] { new ObjectSpaceProfile { Name = id, Configuration = new ObjectSpaceLabConfiguration() } }
		}
	};

	private static StudyCase TreeCase(string id) => new() {
		Id = id, Domain = "btrees", BTrees = new BTreeProfilerSettings {
			RecordCount = 80, OperationCount = 24, Profiles = new[] { new BTreeProfile { Name = id, KeyShape = BTreeKeyShape.Int64 } }
		}
	};

	private static StudyPlan Plan(params StudyCase[] cases) => new() { Cases = cases, OutputDirectory = "unused-study-output", DataDirectory = "unused-study-data" };
}
