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

namespace Sphere10.Framework.Tests.ObjectSpaces;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class DefaultsCompatibilityTests {
	/// <summary>Verifies ordinary reopening of default layouts using a complete default tree specification.</summary>
	[TestCase(false, false, false)]
	[TestCase(false, false, true)]
	[TestCase(true, false, false)]
	[TestCase(true, true, false)]
	[TestCase(true, false, true)]
	[TestCase(true, true, true)]
	public void DefaultLayoutReopensWithItsConcreteDefaultSpecification(bool fileBacked, bool autoLoad, bool useBuilder) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		using var memory = new MemoryStream();
		var path = Path.Combine(folder, "default.dat");
		using (var original = ConfigureStorage(CreateBuilder(false), fileBacked, path, memory, true).Build()) {
			SaveRecords(original);
			CompleteWrites(original);
		}
		var builder = ConfigureStorage(CreateBuilder(false), fileBacked, path, memory, autoLoad);
		var descriptor = Sphere10FrameworkFileDescriptor.From(path, Sphere10FrameworkDefaults.TransactionalPageFolder,
			Sphere10FrameworkDefaults.ObjectSpacePageSize, Sphere10FrameworkDefaults.ObjectSpaceMaxMemory,
			Sphere10FrameworkDefaults.FileObjectSpaceClusterSize, Sphere10FrameworkDefaults.FileObjectSpaceContainerPolicy);
		using var reopened = useBuilder ? builder.Build() : fileBacked
			? new FileObjectSpace(descriptor, builder.BuildDefinition(), Serializers(), new ComparerFactory(ComparerFactory.Default),
				autoLoad ? FileAccessMode.Default : FileAccessMode.OpenOrCreate)
			: new MemoryObjectSpace(memory, builder.BuildDefinition(), Serializers(), new ComparerFactory(ComparerFactory.Default));
		if (reopened.RequiresLoad)
			reopened.Load();
		Assert.That(reopened.Definition.IndexBTreeSpec,
			Is.EqualTo(new BTreeSpec(BTreeSpec.Default.Type, Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder)));
		AssertRecords(reopened);
	}

	/// <summary>Verifies historical mixed index layouts using their original per-index tree specifications.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public void ExplicitHistoricalLayoutReadsOldBytesAndAllMemberIndexes(bool fileBacked) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		using var memory = new MemoryStream();
		var path = Path.Combine(folder, "legacy.dat");
		using (var legacy = ConfigureStorage(CreateHistoricalBuilder(), fileBacked, path, memory, true).Build()) {
			SaveRecords(legacy);
			CompleteWrites(legacy);
		}
		using var reopened = ConfigureStorage(CreateHistoricalBuilder(), fileBacked, path, memory, true).Build();
		AssertRecords(reopened);
		Assert.That(reopened.Definition.IndexBTreeOrder, Is.EqualTo(64));
		Assert.That(reopened.Definition.Dimensions.Single().Indexes.Where(index => index.Type is ObjectSpaceDefinition.IndexType.UniqueKey or ObjectSpaceDefinition.IndexType.Index)
			.Select(index => index.BTreeSpec), Is.All.EqualTo(new BTreeSpec(BTreeType.BTreePlus, 64)));
	}

	[TestCase(8, false)]
	[TestCase(128, false)]
	[TestCase(169, false)]
	[TestCase(8, true)]
	[TestCase(128, true)]
	[TestCase(169, true)]
	public void ExplicitCustomOrderAndClustersSurviveReopen(int order, bool fileBacked) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		using var memory = new MemoryStream();
		var builder = ConfigureStorage(CreateBuilder(true).WithIndexBTreeOrder(order).WithClusterSize(2048), fileBacked, Path.Combine(folder, "custom.dat"), memory, true);
		using (var space = builder.Build()) {
			SaveRecords(space);
			CompleteWrites(space);
		}
		using var reopened = builder.Build();
		AssertRecords(reopened);
		Assert.That(reopened.Definition.IndexBTreeOrder, Is.EqualTo(order));
		Assert.That(reopened.Streams.Header.ClusterSize, Is.EqualTo(2048));
	}

	/// <summary>Verifies supplied-definition reopening and rollback retain original physical layout settings.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public void SuppliedDefinitionReopensWithOriginalSettings(bool fileBacked) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		using var memory = new MemoryStream();
		var path = Path.Combine(folder, "default.dat");
		var builder = ConfigureStorage(CreateBuilder(false), fileBacked, path, memory, true);
		ObjectSpaceDefinition definition;
		SerializerFactory serializers;
		ComparerFactory comparers;
		Sphere10FrameworkFileDescriptor descriptor = null;
		using (var space = builder.Build()) {
			definition = space.Definition;
			serializers = space.Serializers;
			comparers = space.Comparers;
			Assert.That(definition.IndexBTreeSpec, Is.EqualTo(new BTreeSpec(BTreeSpec.Default.Type, Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder)));
			Assert.That(definition.IndexBTreeOrder, Is.EqualTo(Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder));
			Assert.That(space.Streams.Header.ClusterSize, Is.EqualTo(fileBacked ? Sphere10FrameworkDefaults.FileObjectSpaceClusterSize : Sphere10FrameworkDefaults.MemoryObjectSpaceClusterSize));
			Assert.That(space.Dimensions.Single().Container.ObjectStream.Streams.Header.ClusterSize,
				Is.EqualTo(fileBacked ? Sphere10FrameworkDefaults.FileObjectSpaceDimensionClusterSize : Sphere10FrameworkDefaults.MemoryObjectSpaceDimensionClusterSize));
			SaveRecords(space);
			CompleteWrites(space);
			if (space is FileObjectSpace file)
				descriptor = file.File;
		}
		using (var reopened = fileBacked ? (ObjectSpace)new FileObjectSpace(descriptor, definition, serializers, comparers)
			: new MemoryObjectSpace(memory, definition, serializers, comparers)) {
			AssertRecords(reopened);
			if (reopened is FileObjectSpace file) {
				var record = reopened.Get((Record item) => item.Id, 42);
				record.Group = 999;
				reopened.Save(record);
				file.Rollback();
				AssertRecords(reopened);
			}
		}
		using var sameBuilder = builder.Build();
		AssertRecords(sameBuilder);
	}

	/// <summary>Verifies that loading a deferred owner does not change its builder's configuration.</summary>
	[Test]
	public void DeferredFileLoadDoesNotChangeBuilderConfiguration() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var builder = CreateBuilder(false).UseFile(Path.Combine(folder, "deferred.dat"));
		using (var space = builder.Build()) {
			Assert.That(space.RequiresLoad, Is.True);
			Assert.That(builder.BuildDefinition().IndexBTreeSpec, Is.EqualTo(space.Definition.IndexBTreeSpec));
			space.Load();
			Assert.That(builder.BuildDefinition().IndexBTreeSpec, Is.EqualTo(space.Definition.IndexBTreeSpec));
			SaveRecords(space);
			CompleteWrites(space);
		}
		using var reopened = builder.Build();
		reopened.Load();
		AssertRecords(reopened);
	}

	/// <summary>Verifies that a deferred owner retains its definition after later builder configuration changes.</summary>
	[Test]
	public void DeferredFileLoadPreservesALaterBuilderSpecificationReplacement() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var builder = CreateBuilder(false).WithIndexBTreeOrder(4).UseFile(Path.Combine(folder, "deferred.dat"));
		using var space = builder.Build();
		builder.WithIndexBTreeOrder(8);
		Assert.That(builder.BuildDefinition().IndexBTreeSpec, Is.EqualTo(new BTreeSpec(BTreeSpec.Default.Type, 8)));
		space.Load();
		Assert.That(space.Definition.IndexBTreeSpec, Is.EqualTo(new BTreeSpec(BTreeSpec.Default.Type, 4)));
		Assert.That(builder.BuildDefinition().IndexBTreeSpec, Is.EqualTo(new BTreeSpec(BTreeSpec.Default.Type, 8)));
	}

	/// <summary>Verifies that pre-load definition changes affect that store without changing its builder.</summary>
	[Test]
	public void DeferredDefinitionChangesStayLocalToThatStore() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var path = Path.Combine(folder, "deferred.dat");
		var builder = CreateBuilder(false).WithIndexBTreeOrder(4).UseFile(path);
		using (var space = (FileObjectSpace)builder.Build()) {
			space.Definition.IndexBTreeOrder = 8;
			space.Load();
			Assert.That(space.Definition.IndexBTreeSpec, Is.EqualTo(new BTreeSpec(BTreeSpec.Default.Type, 8)));
			Assert.That(builder.BuildDefinition().IndexBTreeSpec, Is.EqualTo(new BTreeSpec(BTreeSpec.Default.Type, 4)));
			SaveRecords(space);
			space.Commit();
		}
		using var reopened = CreateBuilder(false).WithIndexBTreeOrder(8).UseFile(path).AutoLoad().Build();
		AssertRecords(reopened);
	}

	/// <summary>Verifies that loading an invalid definition does not mutate the builder or write storage bytes.</summary>
	[Test]
	public void FailedLoadDoesNotChangeBuilderDefinition() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var builder = CreateBuilder(false).UseFile(Path.Combine(folder, "invalid.dat"));
		using var space = builder.Build();
		space.Definition.Dimensions = Array.Empty<ObjectSpaceDefinition.DimensionDefinition>();
		Assert.That(() => space.Load(), Throws.Exception);
		Assert.That(builder.BuildDefinition().IndexBTreeSpec, Is.EqualTo(new BTreeSpec(BTreeSpec.Default.Type, Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder)));
		Assert.That(builder.BuildDefinition().Dimensions, Has.Length.EqualTo(1));
		Assert.That(new FileInfo(Path.Combine(folder, "invalid.dat")).Length, Is.Zero);
	}

	/// <summary>Verifies pending-commit recovery with complete default index specifications.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public void ConcreteDefaultSpecificationRecoversValidPendingPages(bool autoLoad) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		byte[] pendingBytes;
		using (var memory = new MemoryStream()) {
			var builder = CreateBuilder(false).WithClusterSize(4096).WithClusteredStreamsPolicy(ClusteredStreamsPolicy.Default).UseMemoryStream(memory);
			builder.Configure<Record>().OptimizeAssumingAverageItemSize(256);
			using var original = builder.Build();
			SaveRecords(original);
			original.Flush();
			pendingBytes = memory.ToArray();
		}
		// Recreate a valid interrupted commit with no committed bytes and one staged page.
		var path = Path.Combine(folder, "pending.dat");
		File.WriteAllBytes(path, Array.Empty<byte>());
		var pages = Path.Combine(folder, "pages");
		Directory.CreateDirectory(pages);
		var descriptor = Sphere10FrameworkFileDescriptor.From(path, pages, 262144, 16777216, 4096, ClusteredStreamsPolicy.Default);
		var fileID = TransactionalFileMappedListBase<byte>.ComputeFileID(descriptor.CaseCorrectPath);
		var page = TransactionalFileMappedListBase<byte>.MarkerRepository.GeneratePageMarkerFileName(pages, fileID,
			TransactionalFileMappedListBase<byte>.PageMarkerType.UncommittedPage, 0);
		var committing = TransactionalFileMappedListBase<byte>.MarkerRepository.GenerateFileMarkerFileName(pages, fileID,
			TransactionalFileMappedListBase<byte>.FileMarkerType.Committing);
		Assert.That(pendingBytes.Length, Is.LessThanOrEqualTo(descriptor.PageSize));
		File.WriteAllBytes(page, pendingBytes);
		File.WriteAllBytes(committing, Array.Empty<byte>());
		using (var recovered = new FileObjectSpace(descriptor, CreateBuilder(false).BuildDefinition(), Serializers(), new ComparerFactory(ComparerFactory.Default),
			autoLoad ? FileAccessMode.Default : FileAccessMode.OpenOrCreate)) {
			if (!autoLoad) {
				Assert.That(recovered.RequiresLoad, Is.True);
				Assert.That(File.Exists(page), Is.True);
				Assert.That(File.Exists(committing), Is.True);
				recovered.Load();
			}
			AssertRecords(recovered);
			Assert.That(recovered.Definition.IndexBTreeSpec,
				Is.EqualTo(new BTreeSpec(BTreeSpec.Default.Type, Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder)));
			recovered.Commit();
		}
		using var reopened = new FileObjectSpace(descriptor, CreateBuilder(false).BuildDefinition(), Serializers(), new ComparerFactory(ComparerFactory.Default));
		AssertRecords(reopened);
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public void ExplicitBuilderOverridesSurviveStorageSelectionOrdering(bool fileBacked, bool selectStorageFirst) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		using var memory = new MemoryStream();
		var builder = CreateBuilder(true);
		if (selectStorageFirst)
			ConfigureStorage(builder, fileBacked, Path.Combine(folder, "override.dat"), memory, true);
		builder.WithIndexBTreeOrder(8).WithClusterSize(2048).WithClusteredStreamsPolicy(ClusteredStreamsPolicy.CacheClusterHeaders | ClusteredStreamsPolicy.IntegrityChecks)
			.WithPageSize(65536).WithMaxMemory(4194304);
		if (!selectStorageFirst)
			ConfigureStorage(builder, fileBacked, Path.Combine(folder, "override.dat"), memory, true);
		using var space = builder.Build();
		Assert.That(space.Definition.IndexBTreeOrder, Is.EqualTo(8));
		Assert.That(space.Streams.Header.ClusterSize, Is.EqualTo(2048));
		Assert.That(space.Streams.Policy, Is.EqualTo(ClusteredStreamsPolicy.CacheClusterHeaders | ClusteredStreamsPolicy.IntegrityChecks));
		if (space is FileObjectSpace file) {
			Assert.That(file.File.PageSize, Is.EqualTo(65536));
			Assert.That(file.File.MaxMemory, Is.EqualTo(4194304));
		}
	}

	/// <summary>Verifies that selecting a legacy file uses the caller's replacement layout settings.</summary>
	[Test]
	public void ReusingBuilderForDifferentLegacyFileUsesTheReplacementSpecification() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var legacyPath = Path.Combine(folder, "legacy64.dat");
		using (var legacy = CreateBuilder(true).UseFile(legacyPath).AutoLoad().Build()) {
			SaveRecords(legacy);
			CompleteWrites(legacy);
		}
		var builder = CreateBuilder(false).UseFile(Path.Combine(folder, "new-default.dat")).AutoLoad();
		using (var fresh = builder.Build()) {
			SaveRecords(fresh);
			CompleteWrites(fresh);
		}
		builder.UseFile(legacyPath);
		Assert.That(builder.BuildDefinition().IndexBTreeSpec, Is.EqualTo(new BTreeSpec(BTreeSpec.Default.Type, Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder)));
		builder.WithIndexBTreeOrder(64).WithClusterSize(4096);
		builder.Configure<Record>().OptimizeAssumingAverageItemSize(256);
		using var reopened = builder.Build();
		AssertRecords(reopened);
	}

	/// <summary>Verifies that selecting legacy memory storage uses the caller's replacement layout settings.</summary>
	[Test]
	public void ReusingBuilderForDifferentLegacyMemoryUsesTheReplacementSpecification() {
		using var legacyMemory = new MemoryStream();
		using (var legacy = CreateBuilder(true).UseMemoryStream(legacyMemory).Build()) {
			SaveRecords(legacy);
			CompleteWrites(legacy);
		}
		using var freshMemory = new MemoryStream();
		var builder = CreateBuilder(false).UseMemoryStream(freshMemory);
		using (var fresh = builder.Build()) {
			SaveRecords(fresh);
			CompleteWrites(fresh);
		}
		builder.UseMemoryStream(legacyMemory);
		Assert.That(builder.BuildDefinition().IndexBTreeSpec, Is.EqualTo(new BTreeSpec(BTreeSpec.Default.Type, Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder)));
		builder.WithIndexBTreeOrder(64).WithClusterSize(4096);
		builder.Configure<Record>().OptimizeAssumingAverageItemSize(256);
		using var reopened = builder.Build();
		AssertRecords(reopened);
	}

	/// <summary>Verifies that reselecting the same storage leaves the configured complete specification intact.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public void ReselectingStorageDoesNotChangeTheConfiguredSpecification(bool fileBacked) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		using var memory = new MemoryStream();
		var path = Path.Combine(folder, "same.dat");
		var builder = ConfigureStorage(CreateBuilder(false).WithIndexBTreeOrder(7).WithClusterSize(2048), fileBacked, path, memory, true);
		builder.Configure<Record>().OptimizeAssumingAverageItemSize(512);
		using (var space = builder.Build()) {
			SaveRecords(space);
			CompleteWrites(space);
		}
		if (fileBacked)
			builder.UseFile(Path.Combine(folder, ".", "same.dat"));
		else
			builder.UseMemoryStream(memory);
		Assert.That(builder.BuildDefinition().IndexBTreeSpec, Is.EqualTo(new BTreeSpec(BTreeSpec.Default.Type, 7)));
		using var reopened = builder.Build();
		AssertRecords(reopened);
	}

	/// <summary>Verifies that transaction-folder changes preserve index configuration and existing store bytes.</summary>
	[Test]
	public void ChangingTransactionFolderPreservesTheConcreteSpecification() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var builder = CreateBuilder(false).UseFile(Path.Combine(folder, "same.dat")).KeepUncommittedPagesAt("first-txn").AutoLoad();
		using (var space = builder.Build()) {
			SaveRecords(space);
			CompleteWrites(space);
		}
		var original = File.ReadAllBytes(Path.Combine(folder, "same.dat"));
		builder.KeepUncommittedPagesAt("different-txn");
		Assert.That(builder.BuildDefinition().IndexBTreeSpec, Is.EqualTo(new BTreeSpec(BTreeSpec.Default.Type, Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder)));
		using (var reopened = builder.Build())
			AssertRecords(reopened);
		Assert.That(File.ReadAllBytes(Path.Combine(folder, "same.dat")), Is.EqualTo(original));
	}

	[Test]
	public void OneLogicalDefinitionSupportsFreshMemoryAndFileLayoutsAndReopensBoth() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		using var memory = new MemoryStream();
		var definition = CreateBuilder(false).BuildDefinition();
		var serializers = Serializers();
		var comparers = new ComparerFactory(ComparerFactory.Default);
		using (var space = new MemoryObjectSpace(memory, definition, serializers, comparers)) {
			Assert.That(space.Streams.Header.ClusterSize, Is.EqualTo(Sphere10FrameworkDefaults.MemoryObjectSpaceClusterSize));
			Assert.That(space.Dimensions.Single().Container.ObjectStream.Streams.Header.ClusterSize, Is.EqualTo(Sphere10FrameworkDefaults.MemoryObjectSpaceDimensionClusterSize));
			SaveRecords(space);
			CompleteWrites(space);
		}
		var descriptor = Sphere10FrameworkFileDescriptor.From(Path.Combine(folder, "different-backend.dat"), Sphere10FrameworkDefaults.TransactionalPageFolder,
			Sphere10FrameworkDefaults.ObjectSpacePageSize, Sphere10FrameworkDefaults.ObjectSpaceMaxMemory,
			Sphere10FrameworkDefaults.FileObjectSpaceClusterSize, Sphere10FrameworkDefaults.FileObjectSpaceContainerPolicy);
		using (var space = new FileObjectSpace(descriptor, definition, serializers, comparers)) {
			Assert.That(space.Streams.Header.ClusterSize, Is.EqualTo(Sphere10FrameworkDefaults.FileObjectSpaceClusterSize));
			Assert.That(space.Dimensions.Single().Container.ObjectStream.Streams.Header.ClusterSize, Is.EqualTo(Sphere10FrameworkDefaults.FileObjectSpaceDimensionClusterSize));
			SaveRecords(space);
			CompleteWrites(space);
		}
		using var reopenedMemory = new MemoryObjectSpace(memory, definition, serializers, comparers);
		using var reopenedFile = new FileObjectSpace(descriptor, definition, serializers, comparers);
		AssertRecords(reopenedMemory);
		AssertRecords(reopenedFile);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void StoredClusterReaderRestoresPositionAndRejectsChangedReservedStreamCount(bool bigEndian) {
		using var memory = new MemoryStream();
		var endianness = bigEndian ? Endianness.BigEndian : Endianness.LittleEndian;
		using var streams = new ClusteredStreams(memory, 1024, ClusteredStreamsPolicy.Default, 2, endianness, true);
		memory.Position = 17;
		var original = memory.ToArray();
		Assert.That(ClusteredStreams.ReadStoredClusterSize(memory, 2, endianness), Is.EqualTo(1024));
		Assert.That(memory.Position, Is.EqualTo(17));
		Assert.That(() => ClusteredStreams.ReadStoredClusterSize(memory, 3, endianness), Throws.InvalidOperationException.With.Message.Contains("reserved streams"));
		Assert.That(memory.Position, Is.EqualTo(17));
		Assert.That(memory.ToArray(), Is.EqualTo(original));
	}

	[TestCase(1024)]
	[TestCase(4096)]
	[TestCase(2048)]
	public void FreshBuilderWithOriginalOrderReadsExistingMemoryClustersWhenSizesAreOmitted(int originalClusterSize) {
		using var memory = new MemoryStream();
		using (var original = CreateBuilder(true).WithClusterSize(originalClusterSize).UseMemoryStream(memory).Build()) {
			SaveRecords(original);
			CompleteWrites(original);
		}
		var builder = CreateBuilder(false).WithIndexBTreeOrder(64).UseMemoryStream(memory);
		using var reopened = builder.Build();
		AssertRecords(reopened);
		Assert.That(reopened.Streams.Header.ClusterSize, Is.EqualTo(originalClusterSize));
		Assert.That(reopened.Dimensions.Single().Container.ObjectStream.Streams.Header.ClusterSize, Is.EqualTo(256));
	}

	private static ObjectSpaceBuilder CreateBuilder(bool legacyLayout) {
		var builder = new ObjectSpaceBuilder();
		var dimension = builder.AddDimension<Record>(true).WithIdentifier(record => record.Id).WithUniqueIndexOn(record => record.Code)
			.WithIndexOn(record => record.Group);
		if (legacyLayout) {
			dimension.OptimizeAssumingAverageItemSize(256);
			builder.WithIndexBTreeOrder(64).WithClusterSize(4096);
		}
		dimension.Done();
		return builder;
	}

	/// <summary>Creates the historical schema with its original mixed BTree and BTreePlus index layouts.</summary>
	private static ObjectSpaceBuilder CreateHistoricalBuilder() {
		var builder = CreateBuilder(true);
		foreach (var index in builder.Configure<Record>().Indexes.Where(index => index.Member != null))
			index.BTreeSpec = new BTreeSpec(index.Type == ObjectSpaceDefinition.IndexType.Identifier ? BTreeType.BTree : BTreeType.BTreePlus, 64);
		return builder;
	}

	private static ObjectSpaceBuilder ConfigureStorage(ObjectSpaceBuilder builder, bool fileBacked, string path, MemoryStream memory, bool autoLoad) {
		if (fileBacked) {
			builder.UseFile(path);
			if (autoLoad)
				builder.AutoLoad();
		} else {
			builder.UseMemoryStream(memory);
		}
		return builder;
	}

	private static SerializerFactory Serializers() {
		var factory = new SerializerFactory(SerializerFactory.Default);
		factory.RegisterAutoBuild<Record>();
		return factory;
	}

	private static void SaveRecords(ObjectSpace space) {
		for (var index = 0; index < 220; index++) {
			var id = index * 137 % 220;
			space.Save(new Record { Id = id, Code = $"record-{id}", Group = id % 13 });
		}
	}

	private static void AssertRecords(ObjectSpace space) {
		Assert.That(space.GetAll<Record>(), Has.Length.EqualTo(220));
		Assert.That(space.Get((Record record) => record.Id, 42).Code, Is.EqualTo("record-42"));
		Assert.That(space.Get((Record record) => record.Code, "record-42").Id, Is.EqualTo(42));
		using var access = space.EnterAccessScope();
		var dimension = (ObjectSpace.Dimension<Record>)space.Dimensions.Single();
		var matches = dimension.Container.ObjectStream.GetIndexFor(record => record.Group).Values[3].Select(space.Get<Record>).ToArray();
		Assert.That(matches.Select(record => record.Id), Is.EquivalentTo(Enumerable.Range(0, 220).Where(id => id % 13 == 3)));
	}

	private static void CompleteWrites(ObjectSpace space) {
		if (space is FileObjectSpace file)
			file.Commit();
		else
			space.Flush();
	}

	public class Record {
		public long Id { get; set; }
		public string Code { get; set; }
		public int Group { get; set; }
	}
}
