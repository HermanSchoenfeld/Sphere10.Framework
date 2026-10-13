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
using NUnit.Framework;
using Sphere10.Framework.Utils.Profiler.BTrees;

namespace Sphere10.Framework.Utils.Profiler.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BTreeProfilerTests {
	[Test]
	public void EveryImplementationRoundTripsAndRecycles(
		[Values] BTreeStorage storage,
		[Values] BTreeFamily family,
		[Values] BTreeKeyShape keyShape) {
		var profile = new BTreeProfile { Name = "test", Storage = storage, Family = family, KeyShape = keyShape, Order = 8, PageSize = 4096, CacheBytes = 8192 };
		var settings = Settings(profile);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(settings.DataDirectory);
		using var fixture = new BTreeBenchmarkFixture(settings, profile);
		fixture.PrepareBaseline();
		Assert.That(fixture.Count, Is.EqualTo(settings.RecordCount));
		Assert.That(fixture.MissingLookup(), Is.EqualTo(settings.OperationCount));
		if (family == BTreeFamily.Lookup)
			fixture.VerifyRead(nameof(BTreeLookupReadBenchmarks.GroupLookup), fixture.GroupLookup());
		else {
			fixture.VerifyRead(nameof(BTreeReadBenchmarks.WarmLookup), fixture.WarmLookup());
			fixture.VerifyRead(nameof(BTreeReadBenchmarks.RangeLookup), fixture.RangeLookup());
		}
		fixture.VerifyRead(nameof(BTreeReadBenchmarks.Scan), fixture.Scan());
		fixture.Update();
		fixture.DeleteAndRecycle();
		fixture.Mixed();
		fixture.Verify();
		fixture.Persist();
		if (storage is not (BTreeStorage.ManagedNodes or BTreeStorage.StreamMemory)) {
			fixture.ReopenAndLookup();
			fixture.Verify();
		}
		fixture.ResetEmpty();
		Assert.That(fixture.Count, Is.Zero);
		fixture.Insert();
		fixture.Persist();
		fixture.Verify();
	}

	[Test]
	public void TransactionRollbackReconstructsCachedTreeState([Values] BTreeFamily family, [Values] BTreeKeyShape keyShape) {
		var profile = new BTreeProfile { Name = "test", Storage = BTreeStorage.TransactionalFile, Family = family, KeyShape = keyShape, Order = 8, PageSize = 4096, CacheBytes = 8192 };
		var settings = Settings(profile);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(settings.DataDirectory);
		using var fixture = new BTreeBenchmarkFixture(settings, profile);
		fixture.PrepareBaseline();
		var baseline = fixture.Scan();
		fixture.Update();
		Assert.That(fixture.Scan(), Is.GreaterThan(baseline));
		fixture.RollbackPreparedChanges();
		Assert.That(fixture.Scan(), Is.EqualTo(baseline));
		fixture.Verify();
		fixture.ReopenAndLookup();
		fixture.Verify();
	}

	[TestCase("balanced")]
	[TestCase("read-heavy")]
	[TestCase("write-heavy")]
	public void MixedSequencesPreserveOracle(string mix) {
		var profile = new BTreeProfile { Name = "test", Storage = BTreeStorage.ManagedNodes, Order = 8 };
		var settings = Settings(profile);
		settings.Mix = mix;
		using var cleanup = Tools.Scope.DeleteDirOnDispose(settings.DataDirectory);
		using var fixture = new BTreeBenchmarkFixture(settings, profile);
		fixture.PrepareBaseline();
		fixture.Mixed();
		fixture.Verify();
		Assert.That(fixture.Count, Is.EqualTo(settings.RecordCount));
	}

	[Test]
	public void GeometryCountsReachableLeavesAndReportsMappingCapacity() {
		var profile = new BTreeProfile { Name = "test", Storage = BTreeStorage.OSMemoryMapped, Order = 8, KeyShape = BTreeKeyShape.Int64 };
		var settings = Settings(profile);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(settings.DataDirectory);
		using var fixture = new BTreeBenchmarkFixture(settings, profile);
		fixture.PrepareBaseline();
		var path = Directory.GetFiles(Path.Combine(settings.OutputDirectory, "storage", settings.RunId)).Single();
		using var metadata = JsonDocument.Parse(File.ReadAllText(path));
		var geometry = metadata.RootElement.GetProperty("Geometry");
		Assert.That(geometry.GetProperty("TotalLeaves").GetInt32(), Is.GreaterThan(1));
		Assert.That(geometry.GetProperty("LeafEntries").GetInt64(), Is.EqualTo(settings.RecordCount));
		Assert.That(geometry.GetProperty("MeanLeafOccupancy").GetDouble(), Is.InRange(0.5, 1));
		Assert.That(metadata.RootElement.GetProperty("LogicalTreeBytes").GetInt64(), Is.EqualTo(fixture.LogicalBytes));
		Assert.That(metadata.RootElement.GetProperty("ProvisionedMappedCapacity").GetInt64(), Is.GreaterThan(fixture.LogicalBytes));
	}

	[TestCase(3)]
	[TestCase(33)]
	[TestCase(169)]
	[TestCase(170)]
	[TestCase(512)]
	public void OSMappingHandlesSplitAndRecycleAcrossOrderBoundaries(int order) {
		var profile = new BTreeProfile { Name = "test", Storage = BTreeStorage.OSMemoryMapped, Order = order, KeyShape = BTreeKeyShape.Int64 };
		var settings = Settings(profile);
		settings.RecordCount = 1100;
		settings.OperationCount = 300;
		using var cleanup = Tools.Scope.DeleteDirOnDispose(settings.DataDirectory);
		using var fixture = new BTreeBenchmarkFixture(settings, profile);
		fixture.PrepareBaseline();
		fixture.MixedAndPersist();
		fixture.Verify();
		fixture.ReopenAndLookup();
		fixture.Verify();
		Assert.That(fixture.Count, Is.EqualTo(1100));
	}

	[Test]
	public void MixedChecksumRejectsIncorrectReads() {
		var profile = new BTreeProfile { Name = "test", Storage = BTreeStorage.ManagedNodes, Family = BTreeFamily.Lookup, Order = 8 };
		var settings = Settings(profile);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(settings.DataDirectory);
		using var fixture = new BTreeBenchmarkFixture(settings, profile);
		fixture.PrepareBaseline();
		var result = fixture.MixedAndPersist();
		Assert.That(() => fixture.VerifyMixedResult(result + 1), Throws.InvalidOperationException);
		fixture.Verify();
	}

	[Test]
	public void OrderSuiteIncludes512AndPreservesKeyLayout() {
		var settings = BTreeProfilerSettings.Parse(new[] { "--suite", "order", "--keys", "Int64", "--mix", "write-heavy", "--show-config" }, out var forwarded, out var show);
		Assert.That(show, Is.True);
		Assert.That(forwarded, Is.Empty);
		Assert.That(settings.Profiles.Select(profile => profile.Order), Is.EqualTo(new[] { 8, 16, 32, 64, 128, 256, 512 }));
		Assert.That(settings.Profiles.All(profile => profile.KeyShape == BTreeKeyShape.Int64), Is.True);
		Assert.That(settings.Mix, Is.EqualTo("write-heavy"));
	}

	[TestCase("pages", BTreeStorage.FileStream)]
	[TestCase("cache", BTreeStorage.OSMemoryMapped)]
	[TestCase("buffers", BTreeStorage.ManagedNodes)]
	public void RejectsMeaninglessStorageParameters(string suite, BTreeStorage storage) {
		Assert.That(() => BTreeProfile.CreateSuite(suite, storage, BTreeFamily.BTreePlus, BTreeKeyShape.Int32), Throws.ArgumentException);
	}

	[TestCase(3, true)]
	[TestCase(16384, true)]
	[TestCase(2, false)]
	[TestCase(16385, false)]
	public void ValidatesBoundedOrderStudyRange(int order, bool supported) {
		var profile = new BTreeProfile { Name = "boundary", Order = order };
		if (supported)
			Assert.That(() => profile.Validate(), Throws.Nothing);
		else
			Assert.That(() => profile.Validate(), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void RunnerRequiresExplicitWorkloadSelection() {
		Assert.That(() => BTreeProfiler.Run(new[] { "--suite", "baseline" }, TextWriter.Null), Throws.ArgumentException);
	}

	private static BTreeProfilerSettings Settings(BTreeProfile profile) {
		var path = Path.Combine(Path.GetTempPath(), "Sphere10-BTreeProfilerTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(path);
		return new BTreeProfilerSettings { RecordCount = 257, OperationCount = 31, GroupSize = 8, DataDirectory = path,
			OutputDirectory = Path.Combine(path, "results"), Profiles = new[] { profile } };
	}
}
