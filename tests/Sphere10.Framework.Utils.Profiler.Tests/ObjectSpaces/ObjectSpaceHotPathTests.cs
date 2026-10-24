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
using System.Runtime.CompilerServices;
using System.Text.Json;
using NUnit.Framework;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;

namespace Sphere10.Framework.Utils.Profiler.Tests.ObjectSpaces;

[TestFixture]
[NonParallelizable]
public class ObjectSpaceHotPathTests {
	private static readonly string[] Workloads = { "WarmIdentityLookup", "CleanFlush", "ReindexAndPersist", "MixedReadWriteAndPersist" };

	public static IEnumerable<TestCaseData> SmallCases() => Cases("SmallWorkloadRoundTrips");

	public static IEnumerable<TestCaseData> TraceCases() => Cases("TraceHotPath");

	public static IEnumerable<TestCaseData> ImplementationSmallCases() => ImplementationCases("SelectedImplementationWorkloadRoundTrips");

	public static IEnumerable<TestCaseData> ImplementationTraceCases() => ImplementationCases("TraceSelectedImplementationHotPath");

	[TestCaseSource(nameof(SmallCases))]
	public void SmallWorkloadRoundTrips(ObjectSpaceLabStorage storage, string workload, bool merkleized)
		=> RunSmallWorkload(storage, workload, merkleized, null);

	[TestCaseSource(nameof(ImplementationSmallCases))]
	public void SelectedImplementationWorkloadRoundTrips(ObjectSpaceLabStorage storage, string workload, bool merkleized, BTreeType implementation)
		=> RunSmallWorkload(storage, workload, merkleized, implementation);

	private static void RunSmallWorkload(ObjectSpaceLabStorage storage, string workload, bool merkleized, BTreeType? implementation) {
		var root = Path.Combine(Path.GetTempPath(), "Sphere10-ObjectSpaceHotPaths-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(root);
		using var lifetime = Tools.Scope.DeleteDirOnDispose(root);
		var settings = CreateSettings(root, storage, workload, merkleized, 16, 16);
		if (implementation.HasValue)
			settings.Profiles[0].Configuration.IndexBTreeSpec = new BTreeSpec(implementation.Value, settings.Profiles[0].Configuration.IndexBTreeOrder);
		using var fixture = new ObjectSpaceBenchmarkFixture(settings, "hot-path");
		Prepare(fixture, workload);
		Verify(fixture, workload, RunMeasuredBatch(fixture, workload));
	}

	[TestCase(ObjectSpaceLabStorage.Memory)]
	[TestCase(ObjectSpaceLabStorage.File)]
	public void CleanFlushPreservesNon256BitRootAcrossReopen(ObjectSpaceLabStorage storage) {
		var root = Path.Combine(Path.GetTempPath(), "Sphere10-ObjectSpaceDigestSmoke-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(root);
		using var lifetime = Tools.Scope.DeleteDirOnDispose(root);
		var settings = CreateSettings(root, storage, "CleanFlush", true, 16, 16);
		settings.Profiles[0].Configuration.HashFunction = CHF.Blake2b_128;
		using var fixture = new ObjectSpaceBenchmarkFixture(settings, "hot-path");
		fixture.PrepareCleanMerkle();
		fixture.VerifyCleanMerkleResult(fixture.CleanFlush());
	}

	[TestCaseSource(nameof(TraceCases))]
	[Explicit("Repeatable profiling workload; ordinary tests and BenchmarkDotNet own correctness and statistical measurement separately.")]
	[Category("Performance")]
	public void TraceHotPath(ObjectSpaceLabStorage storage, string workload, bool merkleized)
		=> RunTrace(storage, workload, merkleized, null);

	[TestCaseSource(nameof(ImplementationTraceCases))]
	[Explicit("Repeatable selected-implementation profiling workload; ordinary tests and BenchmarkDotNet own statistical measurement separately.")]
	[Category("Performance")]
	public void TraceSelectedImplementationHotPath(ObjectSpaceLabStorage storage, string workload, bool merkleized, BTreeType implementation)
		=> RunTrace(storage, workload, merkleized, implementation);

	private static void RunTrace(ObjectSpaceLabStorage storage, string workload, bool merkleized, BTreeType? implementation) {
		var repeats = int.Parse(TestContext.Parameters.Get("HotPathRepeats", "1"));
		Assert.That(repeats, Is.InRange(1, 100000));
		var artifactRoot = TestContext.Parameters.Get("HotPathArtifactRoot", Path.Combine(TestContext.CurrentContext.TestDirectory, "Results", "ObjectSpaceHotPaths"));
		var root = Path.Combine(Path.GetFullPath(artifactRoot), $"{storage}-{workload}-{merkleized}-{Guid.NewGuid():N}");
		Directory.CreateDirectory(root);
		var settings = CreateSettings(root, storage, workload, merkleized, 256, 200);
		if (implementation.HasValue)
			settings.Profiles[0].Configuration.IndexBTreeSpec = new BTreeSpec(implementation.Value, settings.Profiles[0].Configuration.IndexBTreeOrder);
		var manifestPath = Path.Combine(root, "trace-workload.json");
		File.WriteAllText(manifestPath, JsonSerializer.Serialize(new {
			Workload = workload, Repeats = repeats, Settings = settings,
			Measurement = "Trace workload only; profile stacks beneath RunMeasuredBatch. Preparation/verification are outside that marker. Statistical samples belong to the existing study/BenchmarkDotNet runners."
		}, ProfilerSettings.JsonOptions));
		TestContext.AddTestAttachment(manifestPath);
		using var fixture = new ObjectSpaceBenchmarkFixture(settings, "hot-path");
		for (var iteration = 0; iteration < repeats; iteration++) {
			Prepare(fixture, workload);
			Verify(fixture, workload, RunMeasuredBatch(fixture, workload));
			TestContext.Progress.WriteLine($"{storage}/{workload}/Merkle={merkleized}: {iteration + 1}/{repeats} verified iterations.");
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static decimal RunMeasuredBatch(ObjectSpaceBenchmarkFixture fixture, string workload) => workload switch {
		"WarmIdentityLookup" => fixture.IdentityLookup(),
		"CleanFlush" => fixture.CleanFlush(),
		"ReindexAndPersist" => fixture.UpdateAndPersist(true),
		"MixedReadWriteAndPersist" => fixture.MixedReadWriteAndPersist(),
		_ => throw new ArgumentOutOfRangeException(nameof(workload))
	};

	private static void Prepare(ObjectSpaceBenchmarkFixture fixture, string workload) {
		switch (workload) {
			case "WarmIdentityLookup": fixture.PrepareRead(true); break;
			case "CleanFlush": fixture.PrepareCleanMerkle(); break;
			case "ReindexAndPersist": fixture.PrepareMutation(); break;
			case "MixedReadWriteAndPersist": fixture.PrepareMixedMutation(); break;
			default: throw new ArgumentOutOfRangeException(nameof(workload));
		}
	}

	private static void Verify(ObjectSpaceBenchmarkFixture fixture, string workload, decimal result) {
		switch (workload) {
			case "WarmIdentityLookup": fixture.VerifyReadResult(result); break;
			case "CleanFlush": fixture.VerifyCleanMerkleResult((int)result); break;
			case "ReindexAndPersist": fixture.VerifyMutation("reindex"); break;
			case "MixedReadWriteAndPersist": fixture.VerifyMixedReadResult(result); fixture.VerifyMutation("mixed"); break;
			default: throw new ArgumentOutOfRangeException(nameof(workload));
		}
		fixture.VerifyMerkleIntegrity();
	}

	private static IEnumerable<TestCaseData> Cases(string method) {
		foreach (var storage in new[] { ObjectSpaceLabStorage.Memory, ObjectSpaceLabStorage.File }) {
			foreach (var workload in Workloads)
				yield return new TestCaseData(storage, workload, true).SetName($"{method}({storage},Merkle,{workload})");
			yield return new TestCaseData(storage, "WarmIdentityLookup", false).SetName($"{method}({storage},Plain,WarmIdentityLookup)");
		}
	}

	private static IEnumerable<TestCaseData> ImplementationCases(string method) {
		foreach (var implementation in Enum.GetValues<BTreeType>())
			foreach (var storage in new[] { ObjectSpaceLabStorage.Memory, ObjectSpaceLabStorage.File }) {
				foreach (var workload in Workloads)
					yield return new TestCaseData(storage, workload, true, implementation).SetName($"{method}({storage},Merkle,{workload},{implementation})");
				yield return new TestCaseData(storage, "WarmIdentityLookup", false, implementation).SetName($"{method}({storage},Plain,WarmIdentityLookup,{implementation})");
			}
	}

	private static ProfilerSettings CreateSettings(string root, ObjectSpaceLabStorage storage, string workload, bool merkleized, int records, int operations) {
		var profile = ObjectSpaceProfile.CreateSuite("baseline", storage)[0];
		profile.Name = "hot-path";
		profile.Configuration.Merkleized = merkleized;
		profile.Configuration.HashFunction = CHF.SHA2_256;
		profile.Configuration.AutoSave = workload == "MixedReadWriteAndPersist";
		return new ProfilerSettings {
			RecordCount = records, OperationCount = operations, PayloadBytes = 128, CategoryCount = records,
			ShuffledInsertion = true, LookupPattern = ObjectSpaceLookupPattern.Shuffled, WorkloadMix = ObjectSpaceWorkloadMix.Balanced,
			OutputDirectory = root, DataDirectory = Path.Combine(root, "data"), Profiles = new[] { profile }
		};
	}
}
