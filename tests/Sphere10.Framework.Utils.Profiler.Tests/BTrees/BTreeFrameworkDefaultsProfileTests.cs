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
using Sphere10.Framework.Utils.Profiler.BTrees;

namespace Sphere10.Framework.Utils.Profiler.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BTreeFrameworkDefaultsProfileTests {
	[Test]
	public void SelectedDefaultsRunOnEveryBackend([Values] BTreeStorage storage, [Values] BTreeFamily family) {
		var settings = BTreeProfilerSettings.Parse(new[] { "--suite", "framework-defaults", "--storage", storage.ToString(), "--family", family.ToString(),
			"--keys", "Int64", "--records", "96", "--operations", "40", "--show-config" }, out var forwarded, out var show);
		Assert.That(show, Is.True);
		Assert.That(forwarded, Is.Empty);
		var profile = settings.Profiles.Single();
		var expectedOrder = (storage == BTreeStorage.ManagedNodes, family) switch {
			(true, BTreeFamily.BTree) => Sphere10FrameworkDefaults.BTreeOrder,
			(true, BTreeFamily.BTreePlus) => Sphere10FrameworkDefaults.BTreePlusOrder,
			(true, BTreeFamily.Lookup) => Sphere10FrameworkDefaults.BTreeLookupOrder,
			(false, BTreeFamily.BTree) => Sphere10FrameworkDefaults.StreamMappedBTreeOrder,
			(false, BTreeFamily.BTreePlus) => Sphere10FrameworkDefaults.StreamMappedBTreePlusOrder,
			_ => Sphere10FrameworkDefaults.StreamMappedBTreeLookupOrder
		};
		Assert.That(profile.Order, Is.EqualTo(expectedOrder));
		Assert.That(profile.PageSize, Is.EqualTo(Sphere10FrameworkDefaults.TransactionalPageSize));
		Assert.That(profile.CacheBytes, Is.EqualTo(Sphere10FrameworkDefaults.MaxMemoryPerCollection));
		settings.DataDirectory = Path.Combine(Path.GetTempPath(), "Sphere10-ProfilerTests", Guid.NewGuid().ToString("N"));
		settings.OutputDirectory = Path.Combine(settings.DataDirectory, "results");
		using var cleanup = Tools.Scope.DeleteDirOnDispose(settings.DataDirectory);
		using var fixture = new BTreeBenchmarkFixture(settings, profile);
		fixture.PrepareBaseline();
		fixture.MixedAndPersist();
		fixture.Verify();
		Assert.That(fixture.Count, Is.EqualTo(settings.RecordCount));
		if (storage is not (BTreeStorage.ManagedNodes or BTreeStorage.StreamMemory)) {
			fixture.ReopenAndLookup();
			fixture.Verify();
		}
	}

	[TestCase(BTreeStorage.FilePaged, BTreeFamily.BTreePlus)]
	[TestCase(BTreeStorage.ManagedNodes, BTreeFamily.Lookup)]
	public void HistoricalBaselineRemainsExplicit(BTreeStorage storage, BTreeFamily family) {
		var profile = BTreeProfile.CreateSuite("baseline", storage, family, BTreeKeyShape.Int64).Single();
		Assert.That(profile.Order, Is.EqualTo(64));
		Assert.That(profile.PageSize, Is.EqualTo(65536));
		Assert.That(profile.CacheBytes, Is.EqualTo(16777216));
	}

	[Test]
	public void ExplicitWideKeyPresetRunsAndReopens(
		[Values(BTreeStorage.StreamMemory, BTreeStorage.FileStream, BTreeStorage.FilePaged, BTreeStorage.OSMemoryMapped, BTreeStorage.TransactionalFile)] BTreeStorage storage,
		[Values(BTreeFamily.BTreePlus, BTreeFamily.Lookup)] BTreeFamily family) {
		var settings = BTreeProfilerSettings.Parse(new[] { "--suite", "wide-keys", "--storage", storage.ToString(), "--family", family.ToString(),
			"--keys", "Digest32", "--records", "96", "--operations", "40", "--show-config" }, out var forwarded, out var show);
		Assert.That(show, Is.True);
		Assert.That(forwarded, Is.Empty);
		var profile = settings.Profiles.Single();
		Assert.That(profile.Order, Is.EqualTo(family == BTreeFamily.BTreePlus
			? Sphere10FrameworkDefaults.StreamMappedWideKeyBTreePlusOrder : Sphere10FrameworkDefaults.StreamMappedWideKeyBTreeLookupOrder));
		settings.DataDirectory = Path.Combine(Path.GetTempPath(), "Sphere10-ProfilerTests", Guid.NewGuid().ToString("N"));
		settings.OutputDirectory = Path.Combine(settings.DataDirectory, "results");
		using var cleanup = Tools.Scope.DeleteDirOnDispose(settings.DataDirectory);
		using var fixture = new BTreeBenchmarkFixture(settings, profile);
		fixture.PrepareBaseline();
		fixture.MixedAndPersist();
		fixture.Verify();
		if (storage != BTreeStorage.StreamMemory) {
			fixture.ReopenAndLookup();
			fixture.Verify();
		}
	}

	[TestCase(BTreeStorage.ManagedNodes, BTreeFamily.BTreePlus, BTreeKeyShape.Digest32)]
	[TestCase(BTreeStorage.FileStream, BTreeFamily.BTree, BTreeKeyShape.Digest32)]
	[TestCase(BTreeStorage.FileStream, BTreeFamily.BTreePlus, BTreeKeyShape.Int64)]
	public void WideKeyPresetRejectsUnsupportedShapes(BTreeStorage storage, BTreeFamily family, BTreeKeyShape keyShape) {
		Assert.That(() => BTreeProfile.CreateSuite("wide-keys", storage, family, keyShape), Throws.ArgumentException);
	}
}
