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
using Sphere10.Framework.ObjectSpaces;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;

namespace Sphere10.Framework.Utils.Profiler.Tests.ObjectSpaces;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class FrameworkDefaultsProfileTests {
	[TestCase(ObjectSpaceLabStorage.Memory)]
	[TestCase(ObjectSpaceLabStorage.File)]
	public void FrameworkDefaultsPresetAppliesTheCurrentLayoutAndPersistsMixedWork(ObjectSpaceLabStorage storage) {
		var directory = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var settings = ProfilerSettings.Parse(new[] { "--suite", "framework-defaults", "--storage", storage.ToString(),
			"--records", "80", "--operations", "40", "--payload", "32", "--output", directory, "--data-root", Path.Combine(directory, "data"), "--show-config" },
			out var forwarded, out var showConfiguration);
		Assert.That(showConfiguration, Is.True);
		Assert.That(forwarded, Is.Empty);
		Assert.That(settings.Profiles.Single().Name, Is.EqualTo("framework-defaults"));
		using var fixture = new ObjectSpaceBenchmarkFixture(settings, "framework-defaults");
		fixture.PrepareMixedMutation();
		Assert.That(fixture.Store.Definition.IndexBTreeOrder, Is.EqualTo(Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder));
		using (fixture.Store.EnterAccessScope()) {
			var streams = fixture.Store.Dimensions.Single().Container.ObjectStream.Streams;
			Assert.That(streams.Header.ClusterSize, Is.EqualTo(storage == ObjectSpaceLabStorage.Memory
				? Sphere10FrameworkDefaults.MemoryObjectSpaceDimensionClusterSize : Sphere10FrameworkDefaults.FileObjectSpaceDimensionClusterSize));
			Assert.That(((BTreeStorageAttachment<long, long>)streams.Attachments["Id.btree"]).BTree.Order, Is.EqualTo(Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder));
			Assert.That(((BTreeLookupStorageAttachment<int, long>)streams.Attachments["Code.btreeLookup"]).BTreeLookup.Order, Is.EqualTo(Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder));
			Assert.That(((BTreeLookupStorageAttachment<int, long>)streams.Attachments["Category.btreeLookup"]).BTreeLookup.Order, Is.EqualTo(Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder));
		}
		if (fixture.Store is FileObjectSpace file) {
			Assert.That(file.File.ClusterSize, Is.EqualTo(Sphere10FrameworkDefaults.FileObjectSpaceClusterSize));
			Assert.That(file.File.ContainerPolicy, Is.EqualTo(Sphere10FrameworkDefaults.FileObjectSpaceContainerPolicy));
			Assert.That(file.File.PageSize, Is.EqualTo(Sphere10FrameworkDefaults.ObjectSpacePageSize));
			Assert.That(file.File.MaxMemory, Is.EqualTo(Sphere10FrameworkDefaults.ObjectSpaceMaxMemory));
		}
		Assert.That(fixture.Store.AutoSave, Is.False);
		Assert.That(fixture.Store.Definition.Traits.HasFlag(ObjectSpaceTraits.Merkleized), Is.False);
		fixture.VerifyMixedReadResult(fixture.MixedReadWriteAndPersist());
		fixture.VerifyMutation("mixed");
	}

	[TestCase(ObjectSpaceLabStorage.Memory)]
	[TestCase(ObjectSpaceLabStorage.File)]
	public void HistoricalBaselineRemainsIndependentOfFrameworkPreset(ObjectSpaceLabStorage storage) {
		var current = ObjectSpaceProfile.CreateSuite("framework-defaults", storage).Single();
		var baseline = ObjectSpaceProfile.CreateSuite("baseline", storage).Single().Configuration;
		Assert.That(current.Configuration.IndexBTreeOrder, Is.EqualTo(Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder));
		Assert.That(baseline.IndexBTreeOrder, Is.EqualTo(64));
		Assert.That(baseline.ClusterSize, Is.EqualTo(4096));
		Assert.That(baseline.AverageRecordSizeBytes, Is.EqualTo(256));
		Assert.That(baseline.PageSize, Is.EqualTo(262144));
		Assert.That(baseline.MaxMemory, Is.EqualTo(16777216));
		Assert.That(baseline.Policy, Is.EqualTo(ClusteredStreamsPolicy.Default));
		Assert.That(baseline.AutoSave || baseline.Merkleized, Is.False);
	}
}
