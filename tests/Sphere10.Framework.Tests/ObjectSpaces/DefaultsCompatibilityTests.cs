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
using NUnit.Framework;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework.Tests.ObjectSpaces;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class DefaultsCompatibilityTests {
	[TestCase(false, false, false)]
	[TestCase(false, false, true)]
	[TestCase(true, false, false)]
	[TestCase(true, true, false)]
	[TestCase(true, false, true)]
	[TestCase(true, true, true)]
	public void UnknownLegacyLayoutIsRejectedBeforeDataOrMarkersChange(bool fileBacked, bool autoLoad, bool useBuilder) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		using var memory = new MemoryStream();
		var path = Path.Combine(folder, "legacy.dat");
		using (var legacy = ConfigureStorage(CreateBuilder(true), fileBacked, path, memory, true).Build()) {
			SaveRecords(legacy);
			CompleteWrites(legacy);
		}
		memory.Position = Math.Min(17, memory.Length);
		var position = memory.Position;
		var originalMemory = memory.ToArray();
		var originalFiles = SnapshotFiles(folder);
		var originalDirectories = Directory.GetDirectories(folder, "*", SearchOption.AllDirectories);
		Assert.That(() => {
			var builder = ConfigureStorage(CreateBuilder(false), fileBacked, path, memory, autoLoad);
			using var refused = useBuilder ? builder.Build() : fileBacked
				? new FileObjectSpace(LegacyDescriptor(path), builder.BuildDefinition(), Serializers(), new ComparerFactory(ComparerFactory.Default),
					autoLoad ? FileAccessMode.Default : FileAccessMode.OpenOrCreate)
				: new MemoryObjectSpace(memory, builder.BuildDefinition(), Serializers(), new ComparerFactory(ComparerFactory.Default));
		}, Throws.InvalidOperationException.With.Message.Contains("original index order"));
		Assert.That(memory.ToArray(), Is.EqualTo(originalMemory));
		Assert.That(memory.Position, Is.EqualTo(position));
		AssertFilesUnchanged(folder, originalFiles);
		Assert.That(Directory.GetDirectories(folder, "*", SearchOption.AllDirectories), Is.EquivalentTo(originalDirectories));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ExplicitHistoricalLayoutReadsOldBytesAndAllMemberIndexes(bool fileBacked) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		using var memory = new MemoryStream();
		var path = Path.Combine(folder, "legacy.dat");
		using (var legacy = ConfigureStorage(CreateBuilder(true), fileBacked, path, memory, true).Build()) {
			SaveRecords(legacy);
			CompleteWrites(legacy);
		}
		using var reopened = ConfigureStorage(CreateBuilder(true), fileBacked, path, memory, true).Build();
		AssertRecords(reopened);
		Assert.That(reopened.Definition.IndexBTreeOrder, Is.EqualTo(64));
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

	[TestCase(false)]
	[TestCase(true)]
	public void FreshDefaultDefinitionBindsAndReopensWithoutChangingOrder(bool fileBacked) {
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
			Assert.That(definition.HasExplicitIndexBTreeOrder, Is.False);
			Assert.That(definition.HasBoundIndexBTreeSpec, Is.True);
			Assert.That(definition.IndexBTreeOrder, Is.EqualTo(Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder));
			Assert.That(space.Streams.Header.ClusterSize, Is.EqualTo(fileBacked ? Sphere10FrameworkDefaults.FileObjectSpaceClusterSize : Sphere10FrameworkDefaults.MemoryObjectSpaceClusterSize));
			Assert.That(space.Dimensions.Single().Container.ObjectStream.Streams.Header.ClusterSize,
				Is.EqualTo(fileBacked ? Sphere10FrameworkDefaults.FileObjectSpaceDimensionClusterSize : Sphere10FrameworkDefaults.MemoryObjectSpaceDimensionClusterSize));
			Assert.That(() => definition.IndexBTreeOrder = definition.IndexBTreeOrder + 1, Throws.InvalidOperationException);
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

	[Test]
	public void DeferredFileLoadRetainsBuilderConfigurationOnlyAfterSuccessfulLoad() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var builder = CreateBuilder(false).UseFile(Path.Combine(folder, "deferred.dat"));
		using (var space = builder.Build()) {
			Assert.That(space.RequiresLoad, Is.True);
			Assert.That(space.Definition.HasBoundIndexBTreeSpec, Is.False);
			Assert.That(builder.BuildDefinition().HasExplicitIndexBTreeOrder, Is.False);
			space.Load();
			Assert.That(space.Definition.HasBoundIndexBTreeSpec, Is.True);
			Assert.That(builder.BuildDefinition().HasExplicitIndexBTreeOrder, Is.True);
			SaveRecords(space);
			CompleteWrites(space);
		}
		using var reopened = builder.Build();
		reopened.Load();
		AssertRecords(reopened);
	}

	[Test]
	public void FailedLoadDoesNotBindDefinitionOrBuilder() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var builder = CreateBuilder(false).UseFile(Path.Combine(folder, "invalid.dat"));
		using var space = builder.Build();
		space.Definition.Dimensions = Array.Empty<ObjectSpaceDefinition.DimensionDefinition>();
		Assert.That(() => space.Load(), Throws.Exception);
		Assert.That(space.Definition.HasBoundIndexBTreeSpec, Is.False);
		Assert.That(builder.BuildDefinition().HasExplicitIndexBTreeOrder, Is.False);
		Assert.That(new FileInfo(Path.Combine(folder, "invalid.dat")).Length, Is.Zero);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ZeroLengthCommittedFileWithPendingPagesIsGuardedBeforeRecovery(bool autoLoad) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
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
		File.WriteAllBytes(page, Enumerable.Range(0, 512).Select(value => (byte)value).ToArray());
		File.WriteAllBytes(committing, new byte[] { 19, 37 });
		var original = SnapshotFiles(folder);
		Assert.That(() => {
			using var refused = new FileObjectSpace(descriptor, CreateBuilder(false).BuildDefinition(), Serializers(), new ComparerFactory(ComparerFactory.Default),
				autoLoad ? FileAccessMode.Default : FileAccessMode.OpenOrCreate);
		}, Throws.InvalidOperationException.With.Message.Contains("original index order"));
		AssertFilesUnchanged(folder, original);
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

	[Test]
	public void ReusingBuilderForDifferentLegacyFileCannotPromoteTheNewFileOrder() {
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
		var original = SnapshotFiles(folder);
		builder.UseFile(legacyPath);
		Assert.That(builder.BuildDefinition().HasExplicitIndexBTreeOrder, Is.False);
		Assert.That(() => { using var refused = builder.Build(); }, Throws.InvalidOperationException.With.Message.Contains("original index order"));
		AssertFilesUnchanged(folder, original);
		builder.WithIndexBTreeOrder(64).WithClusterSize(4096);
		builder.Configure<Record>().OptimizeAssumingAverageItemSize(256);
		using var reopened = builder.Build();
		AssertRecords(reopened);
	}

	[Test]
	public void ReusingBuilderForDifferentLegacyMemoryCannotPromoteTheNewStreamOrder() {
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
		legacyMemory.Position = 17;
		var original = legacyMemory.ToArray();
		builder.UseMemoryStream(legacyMemory);
		Assert.That(builder.BuildDefinition().HasExplicitIndexBTreeOrder, Is.False);
		Assert.That(() => { using var refused = builder.Build(); }, Throws.InvalidOperationException.With.Message.Contains("original index order"));
		Assert.That(legacyMemory.ToArray(), Is.EqualTo(original));
		Assert.That(legacyMemory.Position, Is.EqualTo(17));
		builder.WithIndexBTreeOrder(64).WithClusterSize(4096);
		builder.Configure<Record>().OptimizeAssumingAverageItemSize(256);
		using var reopened = builder.Build();
		AssertRecords(reopened);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ReselectingTheSameStorageIdentityRetainsItsOriginalConfiguration(bool fileBacked) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		using var memory = new MemoryStream();
		var path = Path.Combine(folder, "same.dat");
		var builder = ConfigureStorage(CreateBuilder(false), fileBacked, path, memory, true);
		using (var space = builder.Build()) {
			SaveRecords(space);
			CompleteWrites(space);
		}
		if (fileBacked)
			builder.UseFile(Path.Combine(folder, ".", "same.dat"));
		else
			builder.UseMemoryStream(memory);
		Assert.That(builder.BuildDefinition().HasExplicitIndexBTreeOrder, Is.True);
		using var reopened = builder.Build();
		AssertRecords(reopened);
	}

	[Test]
	public void ChangingTransactionFolderDoesNotRetainAnImplicitOpeningConfiguration() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var builder = CreateBuilder(false).UseFile(Path.Combine(folder, "same.dat")).KeepUncommittedPagesAt("first-txn").AutoLoad();
		using (var space = builder.Build()) {
			SaveRecords(space);
			CompleteWrites(space);
		}
		var original = SnapshotFiles(folder);
		builder.KeepUncommittedPagesAt("different-txn");
		Assert.That(builder.BuildDefinition().HasExplicitIndexBTreeOrder, Is.False);
		Assert.That(() => { using var refused = builder.Build(); }, Throws.InvalidOperationException.With.Message.Contains("original index order"));
		AssertFilesUnchanged(folder, original);
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

	private static Sphere10FrameworkFileDescriptor LegacyDescriptor(string path)
		=> Sphere10FrameworkFileDescriptor.From(path, Sphere10FrameworkDefaults.TransactionalPageFolder, 262144, 16777216, 4096, ClusteredStreamsPolicy.Default);

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

	private static Dictionary<string, byte[]> SnapshotFiles(string folder)
		=> Directory.GetFiles(folder, "*", SearchOption.AllDirectories).ToDictionary(path => Path.GetRelativePath(folder, path), File.ReadAllBytes);

	private static void AssertFilesUnchanged(string folder, Dictionary<string, byte[]> snapshot) {
		var actual = SnapshotFiles(folder);
		Assert.That(actual.Keys, Is.EquivalentTo(snapshot.Keys));
		foreach (var file in snapshot)
			Assert.That(actual[file.Key], Is.EqualTo(file.Value), file.Key);
	}

	public class Record {
		public long Id { get; set; }
		public string Code { get; set; }
		public int Group { get; set; }
	}
}
