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
using NUnit.Framework;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;

namespace Sphere10.Framework.Utils.Profiler.Tests.ObjectSpaces;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ObjectSpaceWorkloadTests {
	[Test]
	public void MixedWorkloadsPersistTheirDeclaredReadWriteRatios(
		[Values] ObjectSpaceLabStorage storage,
		[Values] ObjectSpaceWorkloadMix workloadMix,
		[Values] bool autoSave
	) {
		var settings = CreateSettings(storage, 100);
		using var lifetime = Tools.Scope.DeleteDirOnDispose(settings.OutputDirectory);
		settings.WorkloadMix = workloadMix;
		settings.Profiles[0].Configuration.AutoSave = autoSave;
		settings.LookupPattern = ObjectSpaceLookupPattern.HotSet;
		settings.HotSetPercent = 1;
		settings.PayloadPattern = ObjectSpacePayloadPattern.Variable;
		settings.CategoryCount = 32;
		settings.HotCategoryPercent = 80;
		using var fixture = new ObjectSpaceBenchmarkFixture(settings, "baseline");
		fixture.PrepareMixedMutation();
		var result = fixture.MixedReadWriteAndPersist();
		fixture.VerifyMixedReadResult(result);
		fixture.VerifyMutation("mixed");
		var records = fixture.Store.GetAll<ObjectSpaceLabRecord>().ToArray();
		var expectedWrites = 100 - settings.MixedReadPercentage;
		Assert.That(records.Count(record => record.Balance == record.Id * 1.25M + 1), Is.EqualTo(expectedWrites));
		Assert.That(records.Count(record => record.Code.EndsWith("-updated", StringComparison.Ordinal)), Is.EqualTo((expectedWrites + 1) / 2));
		Assert.That(records.Select(record => record.Id).Distinct().Count(), Is.EqualTo(100));
	}

	[TestCase(ObjectSpaceLabStorage.Memory, ObjectSpaceLookupPattern.Sequential)]
	[TestCase(ObjectSpaceLabStorage.Memory, ObjectSpaceLookupPattern.Shuffled)]
	[TestCase(ObjectSpaceLabStorage.Memory, ObjectSpaceLookupPattern.HotSet)]
	[TestCase(ObjectSpaceLabStorage.File, ObjectSpaceLookupPattern.Sequential)]
	[TestCase(ObjectSpaceLabStorage.File, ObjectSpaceLookupPattern.Shuffled)]
	[TestCase(ObjectSpaceLabStorage.File, ObjectSpaceLookupPattern.HotSet)]
	public void LocalityControlsApplyToReadsWithoutDuplicatingMutationTargets(ObjectSpaceLabStorage storage, ObjectSpaceLookupPattern pattern) {
		var settings = CreateSettings(storage, 40);
		using var lifetime = Tools.Scope.DeleteDirOnDispose(settings.OutputDirectory);
		settings.LookupPattern = pattern;
		settings.HotSetPercent = 5;
		settings.CategoryCount = 4;
		settings.HotCategoryPercent = 80;
		settings.PayloadPattern = ObjectSpacePayloadPattern.Variable;
		using var fixture = new ObjectSpaceBenchmarkFixture(settings, "baseline");
		fixture.PrepareRead(false);
		fixture.VerifyReadResult(fixture.IdentityLookup());
		fixture.VerifyReadResult(fixture.PhysicalSlotLookup());
		fixture.VerifyCategoryResult(fixture.CategoryLookup());
		Assert.That(fixture.MutationIds.Distinct().Count(), Is.EqualTo(40));
		if (pattern == ObjectSpaceLookupPattern.HotSet)
			Assert.That(fixture.LookupIds.Distinct().Count(), Is.EqualTo(2));
		if (pattern == ObjectSpaceLookupPattern.Sequential)
			Assert.That(fixture.LookupIds, Is.Ordered.Ascending);
		Assert.That(fixture.Store.GetAll<ObjectSpaceLabRecord>().Select(record => record.Payload.Length).Distinct().Count(), Is.GreaterThan(1));
		Assert.That(fixture.Store.GetAll<ObjectSpaceLabRecord>().Count(record => record.Category == 0), Is.EqualTo(32));
		fixture.PrepareMutation();
		fixture.DeleteRecycleAndPersist();
		fixture.VerifyMutation("recycle");
	}

	[TestCase(ObjectSpaceLabStorage.Memory)]
	[TestCase(ObjectSpaceLabStorage.File)]
	public void MerkleAutosaveMixedChangesRoundTrip(ObjectSpaceLabStorage storage) {
		var settings = CreateSettings(storage, 24);
		using var lifetime = Tools.Scope.DeleteDirOnDispose(settings.OutputDirectory);
		settings.Profiles[0].Configuration.AutoSave = true;
		settings.Profiles[0].Configuration.Merkleized = true;
		using var fixture = new ObjectSpaceBenchmarkFixture(settings, "baseline");
		fixture.PrepareMixedMutation();
		fixture.VerifyMixedReadResult(fixture.MixedReadWriteAndPersist());
		fixture.VerifyMutation("mixed");
	}

	[Test]
	public void PayloadGrowthAndShrinkRoundTrip(
		[Values] ObjectSpaceLabStorage storage,
		[Values] bool autoSave
	) {
		var settings = CreateSettings(storage, 24);
		using var lifetime = Tools.Scope.DeleteDirOnDispose(settings.OutputDirectory);
		settings.Profiles[0].Configuration.AutoSave = autoSave;
		settings.PayloadPattern = ObjectSpacePayloadPattern.Variable;
		using var fixture = new ObjectSpaceBenchmarkFixture(settings, "baseline");
		fixture.PreparePayloadMutation();
		Assert.That(fixture.ResizePayloadAndPersist(), Is.EqualTo(24));
		fixture.VerifyMutation("payload");
	}

	[Test]
	public void PreparedTransactionsVerifyEveryChangedRecord(
		[Values] bool committed,
		[Values] bool autoSave
	) {
		var settings = CreateSettings(ObjectSpaceLabStorage.File, 24);
		using var lifetime = Tools.Scope.DeleteDirOnDispose(settings.OutputDirectory);
		settings.Profiles[0].Configuration.AutoSave = autoSave;
		using var fixture = new ObjectSpaceBenchmarkFixture(settings, "baseline");
		fixture.PrepareTransaction();
		if (committed)
			fixture.Commit();
		else
			fixture.Rollback();
		fixture.VerifyTransaction(committed);
	}

	[Test]
	public void ComparisonGroupsSeparateIndexAndIntegrityGuarantees() {
		var baseline = ObjectSpaceProfile.CreateSuite("baseline", ObjectSpaceLabStorage.File)[0];
		var autosave = ObjectSpaceProfile.CreateSuite("features", ObjectSpaceLabStorage.File).Single(profile => profile.Name == "autosave");
		var performance = ObjectSpaceProfile.CreateSuite("policies", ObjectSpaceLabStorage.File).Single(profile => profile.Name == "performance");
		var identifierOnly = ObjectSpaceProfile.CreateSuite("indexes", ObjectSpaceLabStorage.File).Single(profile => profile.Name == "identifier-only");
		var hashes = ObjectSpaceProfile.CreateSuite("hashes", ObjectSpaceLabStorage.File);
		Assert.That(autosave.ComparisonGroup, Is.EqualTo(baseline.ComparisonGroup));
		Assert.That(performance.ComparisonGroup, Is.Not.EqualTo(baseline.ComparisonGroup));
		Assert.That(identifierOnly.ComparisonGroup, Is.Not.EqualTo(baseline.ComparisonGroup));
		Assert.That(hashes.Select(profile => profile.ComparisonGroup).Distinct().Count(), Is.EqualTo(hashes.Length));
	}

	[Test]
	public void LargeIndexNodesRoundTripAcrossClusterChainsInEitherByteOrder(
		[Values] ObjectSpaceLabStorage storage,
		[Values] Endianness endianness
	) {
		var settings = CreateSettings(storage, 40);
		using var lifetime = Tools.Scope.DeleteDirOnDispose(settings.OutputDirectory);
		settings.RecordCount = 256;
		settings.CategoryCount = 256;
		settings.Profiles[0].Configuration.IndexBTreeOrder = 169;
		settings.Profiles[0].Configuration.Endianness = endianness;
		using var fixture = new ObjectSpaceBenchmarkFixture(settings, "baseline");
		fixture.PrepareMixedMutation();
		fixture.VerifyMixedReadResult(fixture.MixedReadWriteAndPersist());
		fixture.VerifyMutation("mixed");
		fixture.PrepareRead(false);
		fixture.VerifyReadResult(fixture.IdentityLookup());
		fixture.VerifyReadResult(fixture.CodeLookup());
		fixture.VerifyCategoryResult(fixture.CategoryLookup());
	}

	[TestCase("--hot-set-percent", "0")]
	[TestCase("--categories", "0")]
	[TestCase("--hot-category-percent", "100")]
	[TestCase("--lookup", "999")]
	[TestCase("--payload-mode", "999")]
	[TestCase("--mix", "999")]
	public void InvalidWorkloadControlsAreRejectedBeforeStorageCreation(string argument, string value) {
		Assert.That(() => ProfilerSettings.Parse(new[] { argument, value }, out _, out _), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void WorkloadControlsAreParsedAndForwardOnlyBenchmarkArguments() {
		var args = new[] {
			"--lookup", "HotSet", "--hot-set-percent", "5", "--payload-mode", "Variable", "--categories", "64",
			"--hot-category-percent", "80", "--mix", "WriteHeavy", "--filter", "*MixedReadWriteAndPersist*"
		};
		var settings = ProfilerSettings.Parse(args, out var forwarded, out _);
		Assert.That(settings.LookupPattern, Is.EqualTo(ObjectSpaceLookupPattern.HotSet));
		Assert.That(settings.HotSetPercent, Is.EqualTo(5));
		Assert.That(settings.PayloadPattern, Is.EqualTo(ObjectSpacePayloadPattern.Variable));
		Assert.That(settings.CategoryCount, Is.EqualTo(64));
		Assert.That(settings.HotCategoryPercent, Is.EqualTo(80));
		Assert.That(settings.MixedReadPercentage, Is.EqualTo(10));
		Assert.That(forwarded, Is.EqualTo(new[] { "--filter", "*MixedReadWriteAndPersist*" }));
	}

	private static ProfilerSettings CreateSettings(ObjectSpaceLabStorage storage, int records) {
		var directory = Path.Combine(Path.GetTempPath(), $"Sphere10ObjectSpaceWorkloadTests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(directory);
		return new ProfilerSettings {
			RecordCount = records,
			OperationCount = records,
			PayloadBytes = 32,
			OutputDirectory = directory,
			DataDirectory = Path.Combine(directory, "data"),
			Profiles = ObjectSpaceProfile.CreateSuite("baseline", storage)
		};
	}
}
