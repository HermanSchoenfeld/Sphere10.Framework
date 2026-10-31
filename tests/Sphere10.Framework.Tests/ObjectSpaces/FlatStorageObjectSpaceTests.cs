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

namespace Sphere10.Framework.Tests;

/// <summary>Verifies self-describing ObjectSpace reconstruction, compatibility and storage lifetime behavior.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class FlatStorageObjectSpaceTests {
	/// <summary>Verifies that direct opening restores garbage-collection traits and dimension root roles.</summary>
	[Test]
	public void RawOpenPreservesGarbageCollectionConfiguration([Values] Endianness endianness) {
		using var raw = new MemoryStream();
		var builder = new ObjectSpaceBuilder()
			.StoreMetaData()
			.WithGarbageCollection()
			.WithEndianness(endianness)
			.UseMemoryStream(raw)
			.AddDimension<long>(true)
				.AsRoot()
				.Done();
		using (var created = builder.Build())
			created.Save(17L);

		raw.Position = 0;
		Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out var opened), Is.True);
		using (opened) {
			Assert.That(opened.Traits.HasFlag(ObjectSpaceTraits.GarbageCollect), Is.True);
			Assert.That(opened.Definition.Dimensions.Select(dimension => dimension.IsRoot), Is.All.True);
			Assert.That(opened.Get<long>(0), Is.EqualTo(17L));
			var index = opened.Save(18L);
			Assert.That(opened.Get<long>(index), Is.EqualTo(18L));
		}
	}

	/// <summary>Verifies reconstructed dimensions, tracked identity, indexes and Merkle roots across further writes.</summary>
	[Test]
	public void RawOpenReconstructsDimensionsIndexesAndFurtherWrites(
		[Values(null, BTreeType.BTree, BTreeType.BTreePlus)] BTreeType? type,
		[Values] Endianness endianness,
		[Values] bool merkleized) {
		using var raw = new MemoryStream();
		byte[] root;
		using (var space = CreateBuilder(type, endianness, merkleized).UseMemoryStream(raw).Build()) {
			for (var id = 0; id < 32; id++)
				space.Save(new Record { Id = id, Code = $"code-{id}", Group = id % 4 });
			space.Save(new Audit { Id = 100, Message = "second dimension" });
			space.Delete(space.Get((Record item) => item.Id, 6L));
			var changed = space.Get((Record item) => item.Id, 11L);
			changed.Code = "changed";
			changed.Dirty = true;
			space.Flush();
			root = merkleized ? ReadRoot(space) : null;
			using var access = space.EnterAccessScope();
			Assert.That(space.Streams.Header.ReservedStreams, Is.EqualTo(merkleized ? 1 : 0));
			Assert.That(space.Streams.Header.StreamCount - space.Streams.Header.ReservedStreams, Is.EqualTo(2));
		}
		raw.Position = 0;
		Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out var loaded), Is.True);
		using (loaded) {
			Assert.That(loaded.Traits, Is.EqualTo(ObjectSpaceTraits.AutoSave | ObjectSpaceTraits.MetaData
				| (merkleized ? ObjectSpaceTraits.Merkleized : ObjectSpaceTraits.None)
				| (endianness == Endianness.BigEndian ? ObjectSpaceTraits.BigEndian : ObjectSpaceTraits.None)));
			Assert.That(loaded.Count<Record>(), Is.EqualTo(31));
			Assert.That(loaded.Get((Record item) => item.Code, "changed").Id, Is.EqualTo(11));
			Assert.That(loaded.Get((Record item) => item.Id, 11L), Is.SameAs(loaded.Get((Record item) => item.Code, "changed")));
			Assert.That(loaded.Get((Audit item) => item.Id, 100L).Message, Is.EqualTo("second dimension"));
			Assert.That(loaded.TryGet((Record item) => item.Id, 6L, out _), Is.False);
			using (loaded.EnterAccessScope()) {
				var dimension = (ObjectSpace.Dimension<Record>)loaded.Dimensions[0];
				Assert.That(dimension.Container.ObjectStream.GetIndexFor(item => item.Group).Values[3].Count(), Is.EqualTo(8));
			}
			if (merkleized)
				Assert.That(ReadRoot(loaded), Is.EqualTo(root));
			loaded.Save(new Record { Id = 99, Code = "new record", Group = 9 });
			loaded.Flush();
		}
		Assert.That(raw.CanRead && raw.CanWrite, Is.True);
		raw.Position = 0;
		Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out var reopened), Is.True);
		using var reopenedScope = reopened;
		Assert.That(reopened.Get((Record item) => item.Code, "new record").Id, Is.EqualTo(99));
	}

	/// <summary>Verifies that final index-loading errors propagate without flushing or closing borrowed storage.</summary>
	[Test]
	public void RawOpenPropagatesIndexLoadFailureAndKeepsBorrowedStorage([Values] Endianness endianness) {
		// Create a normal metadata-enabled dimension and record its identifier's physical B-tree stream.
		using var raw = new MemoryStream();
		var builder = new ObjectSpaceBuilder()
			.StoreMetaData()
			.WithIndexBTreeOrder(4)
			.WithEndianness(endianness)
			.UseMemoryStream(raw)
			.AddDimension<OnlyId>(true)
				.WithIdentifier(item => item.Id)
				.Done();
		int indexStream;
		using (var created = builder.Build()) {
			created.Save(new OnlyId { Id = 17 });
			var objects = ((ObjectSpace.Dimension<OnlyId>)created.Dimensions[0]).Container.ObjectStream;
			indexStream = objects.Streams.Attachments.Values.OfType<BTreeStorageAttachment<long, long>>().Single().BaseReservedStreamIndex;
		}

		// The root and dimension metadata remain readable; the failure occurs when the rebuilt owner loads its index.
		using (var streams = ClusteredStreams.FromStream(raw, endianness, autoLoad: true)) {
			using var dimension = streams.OpenWrite(streams.Header.ReservedStreams);
			using var objects = ClusteredStreams.FromStream(dimension, endianness, autoLoad: true);
			using var index = objects.OpenWrite(indexStream);
			index.SetLength(1);
		}
		var expected = raw.ToArray();
		raw.Position = 0;

		// Failure cleanup must propagate the attachment error without closing or flushing the borrowed storage.
		Assert.That(() => ObjectSpace.TryOpenFromStream(raw, endianness, out _),
			Throws.TypeOf<InvalidDataFormatException>().With.Message.Contains("StreamMappedBTree header"));
		Assert.That(raw.CanRead && raw.CanWrite, Is.True);
		Assert.That(raw.ToArray(), Is.EqualTo(expected));
	}

	/// <summary>Verifies that enabling the owner trait creates reconstructible metadata for every dimension.</summary>
	[Test]
	public void MetaDataTraitDescribesTheObjectSpaceAndEveryDimension([Values] Endianness endianness) {
		using var raw = new MemoryStream();
		var definition = CreateBuilder(null, endianness, false, storeMetaData: false).BuildDefinition();
		definition.Traits |= ObjectSpaceTraits.MetaData;
		var serializers = new SerializerFactory(SerializerFactory.Default).RegisterAutoBuild<Record>().RegisterAutoBuild<Audit>();
		using (var created = new MemoryObjectSpace(raw, definition, serializers, new ComparerFactory(ComparerFactory.Default), endianness: endianness)) {
			created.Save(new Record { Id = 1, Code = "trait" });
			created.Save(new Audit { Id = 2, Message = "second dimension" });
		}
		raw.Position = 0;
		Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out var loaded), Is.True);
		using var loadedScope = loaded;
		Assert.That(loaded.Traits.HasFlag(ObjectSpaceTraits.MetaData), Is.True);
		Assert.That(loaded.Get((Record item) => item.Id, 1L).Code, Is.EqualTo("trait"));
		Assert.That(loaded.Get((Audit item) => item.Id, 2L).Message, Is.EqualTo("second dimension"));
	}

	/// <summary>Verifies rejection when a metadata-enabled space contains an empty or legacy-only dimension.</summary>
	[Test]
	public void MetaDataTraitRequiresEveryDimensionRecord([Values] Endianness endianness, [Values] bool emptyDimension) {
		using var raw = new MemoryStream();
		using (var created = CreateBuilder(null, endianness, false).UseMemoryStream(raw).Build())
			created.Save(new Record { Id = 17, Code = "stored" });
		using (var streams = ClusteredStreams.FromStream(raw, endianness, autoLoad: true)) {
			using (var child = streams.OpenWrite(streams.Header.ReservedStreams)) {
				long dataOffset;
				using (var childStreams = ClusteredStreams.FromStream(child, endianness, autoLoad: true))
					dataOffset = childStreams.DataOffset;
				child.Position = 0;
				var bytes = new byte[checked((int)child.Length)];
				child.ReadExactly(bytes);
				var legacyBytes = new byte[checked(ClusteredStreamsHeader.ByteLength + bytes.Length - (int)dataOffset)];
				Array.Copy(bytes, legacyBytes, ClusteredStreamsHeader.ByteLength);
				var converter = EndianBitConverter.For(endianness);
				var policy = (ClusteredStreamsPolicy)converter.ToUInt32(legacyBytes, ClusteredStreamsHeader.PolicyOffset);
				converter.WriteTo((uint)(policy & ~ClusteredStreamsPolicy.MetaData), legacyBytes, ClusteredStreamsHeader.PolicyOffset);
				Array.Clear(legacyBytes, ClusteredStreamsHeader.MetaDataSizeOffset - 2 * sizeof(uint), 2 * sizeof(uint) + sizeof(int));
				Array.Copy(bytes, checked((int)dataOffset), legacyBytes, ClusteredStreamsHeader.ByteLength, bytes.Length - checked((int)dataOffset));
				child.SetLength(0);
				child.Position = 0;
				if (!emptyDimension)
					child.Write(legacyBytes);
			}
		}
		var expected = raw.ToArray();
		raw.Position = 0;
		Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out var loaded), Is.False);
		Assert.That(loaded, Is.Null);
		Assert.That(raw.ToArray(), Is.EqualTo(expected));
		Assert.That(raw.CanRead && raw.CanWrite, Is.True);
		Assert.That(() => CreateBuilder(null, endianness, false, storeMetaData: false).UseMemoryStream(raw).Build(), Throws.TypeOf<InvalidDataException>());
		Assert.That(raw.ToArray(), Is.EqualTo(expected));
	}

	/// <summary>Verifies physical attachment order, custom index names and complete per-index tree specifications.</summary>
	[Test]
	public void RawOpenPreservesRecordedIndexOrderNamesAndSpecifications([Values] Endianness endianness, [Values] bool merkleized) {
		using var raw = new MemoryStream();
		var definition = CreateBuilder(null, endianness, merkleized).BuildDefinition();
		var indexes = definition.Dimensions[0].Indexes.OrderBy(index => index.Type switch {
			ObjectSpaceDefinition.IndexType.UniqueKey => 0,
			ObjectSpaceDefinition.IndexType.Index => 1,
			ObjectSpaceDefinition.IndexType.Identifier => 2,
			ObjectSpaceDefinition.IndexType.MerkleTree => 3,
			ObjectSpaceDefinition.IndexType.RecyclableIndexStore => 4,
			_ => throw new ArgumentOutOfRangeException()
		}).ToArray();
		foreach (var index in indexes) {
			if (index.Type != ObjectSpaceDefinition.IndexType.MerkleTree)
				index.Name = $"record-{index.Type}";
			index.BTreeSpec = index.Type switch {
				ObjectSpaceDefinition.IndexType.Identifier => new BTreeSpec(BTreeType.BTreePlus, 5),
				ObjectSpaceDefinition.IndexType.UniqueKey => new BTreeSpec(BTreeType.BTree, 7),
				ObjectSpaceDefinition.IndexType.Index => new BTreeSpec(BTreeType.BTreePlus, 9),
				_ => null
			};
		}
		definition.Dimensions[0].Indexes = indexes;
		var serializers = new SerializerFactory(SerializerFactory.Default).RegisterAutoBuild<Record>().RegisterAutoBuild<Audit>();
		using (var created = new MemoryObjectSpace(raw, definition, serializers, new ComparerFactory(ComparerFactory.Default),
			clusterSize: 127, endianness: endianness)) {
			created.Save(new Record { Id = 1, Code = "one", Group = 7 });
			created.Save(new Record { Id = 2, Code = "two", Group = 7 });
			created.Save(new Record { Id = 3, Code = "three", Group = 8 });
			created.Delete(created.Get<Record>(1));
		}
		raw.Position = 0;
		Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out var loaded), Is.True);
		using var loadedScope = loaded;
		Assert.That(loaded.Definition.Dimensions[0].Indexes.Select(index => index.Name), Is.EqualTo(indexes.Select(index => index.Name)));
		Assert.That(loaded.Definition.Dimensions[0].Indexes.Select(index => index.Type), Is.EqualTo(indexes.Select(index => index.Type)));
		Assert.That(loaded.Definition.Dimensions[0].Indexes.Select(index => index.BTreeSpec), Is.EqualTo(indexes.Select(index => index.BTreeSpec)));
		var objects = ((ObjectSpace.Dimension<Record>)loaded.Dimensions[0]).Container.ObjectStream;
		Assert.That(objects.TryGetUniqueIndex<long>("record-Identifier", out var identifiers), Is.True);
		Assert.That(identifiers.Values[1], Is.EqualTo(0));
		Assert.That(objects.TryGetUniqueIndex<string>("record-UniqueKey", out var codes), Is.True);
		Assert.That(codes.Values["three"], Is.EqualTo(2));
		Assert.That(objects.TryGetIndex<int>("record-Index", out var groups), Is.True);
		Assert.That(groups.Values[7], Is.EqualTo(new[] { 0L }));
		Assert.That(loaded.Save(new Record { Id = 4, Code = "four", Group = 7 }), Is.EqualTo(1));
		Assert.That(loaded.Get<Record>(1).Id, Is.EqualTo(4));
		Assert.That(raw.CanRead && raw.CanWrite, Is.True);
	}

	/// <summary>Verifies that a metadata record alone cannot be opened as an ObjectSpace store.</summary>
	[Test]
	public void RawOpenRequiresTheClusteredHeaderBeforeTheMetadata([Values] Endianness endianness) {
		using var original = new MemoryStream();
		using (var created = CreateBuilder(null, endianness, false).UseMemoryStream(original).Build())
			created.Save(new Record { Id = 17, Code = "stored" });
		var bytes = original.ToArray();
		var length = EndianBitConverter.For(endianness).ToInt32(bytes, ClusteredStreamsHeader.MetaDataOffset);
		var record = bytes.AsSpan(ClusteredStreamsHeader.MetaDataOffset, length).ToArray();
		using var raw = new MemoryStream(record);
		Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out var failedOpen), Is.False);
		Assert.That(failedOpen, Is.Null);
		Assert.That(raw.ToArray(), Is.EqualTo(record));
		Assert.That(raw.CanRead && raw.CanWrite, Is.True);
	}

	/// <summary>Verifies that unsupported owner traits return false without changing the borrowed bytes.</summary>
	[Test]
	public void InvalidObjectSpaceTraitsFailWithoutChangingBorrowedStorage() {
		using var original = new MemoryStream();
		using (var space = CreateBuilder(null, Endianness.LittleEndian, false).UseMemoryStream(original).Build())
			space.Save(new Record { Id = 1, Code = "one" });
		var bytes = original.ToArray();
		EndianBitConverter.Little.WriteTo(uint.MaxValue, bytes, ClusteredStreamsHeader.MetaDataSizeOffset - sizeof(uint));
		var expected = (byte[])bytes.Clone();
		using var corrupt = new MemoryStream(bytes, writable: false);
		Assert.That(ObjectSpace.TryOpenFromStream(corrupt, Endianness.LittleEndian, out var failedOpen), Is.False);
		Assert.That(failedOpen, Is.Null);
		Assert.That(corrupt.ToArray(), Is.EqualTo(expected));
		Assert.That(corrupt.CanRead, Is.True);
	}

	/// <summary>Verifies that default construction preserves the supplied-definition legacy storage path.</summary>
	[Test]
	public void DefaultCreationRetainsLegacySchemaAndBytes([Values] Endianness endianness) {
		using var raw = new MemoryStream();
		using (var created = CreateBuilder(null, endianness, false, storeMetaData: false).UseMemoryStream(raw).Build())
			created.Save(new Record { Id = 17, Code = "legacy" });
		var expected = raw.ToArray();
		Assert.That(expected[0], Is.EqualTo(1));
		raw.Position = 0;
		Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out var failedOpen), Is.False);
		Assert.That(failedOpen, Is.Null);
		using (var reopened = CreateBuilder(null, endianness, false, storeMetaData: false).UseMemoryStream(raw).Build())
			Assert.That(reopened.Get((Record item) => item.Id, 17L).Code, Is.EqualTo("legacy"));
		Assert.That(raw.ToArray(), Is.EqualTo(expected));
		Assert.That(raw.CanRead, Is.True);
	}

	/// <summary>Verifies that stored metadata remains authoritative when reopening without a creation request.</summary>
	[Test]
	public void StoredMetadataIsOpenedWithoutOptingInAgain([Values] Endianness endianness) {
		using var raw = new MemoryStream();
		using (var created = CreateBuilder(null, endianness, false).UseMemoryStream(raw).Build())
			created.Save(new Record { Id = 17, Code = "stored" });
		var expected = raw.ToArray();
		using (var loaded = CreateBuilder(null, endianness, false, storeMetaData: false).UseMemoryStream(raw).Build()) {
			Assert.That(loaded.Get((Record item) => item.Id, 17L).Code, Is.EqualTo("stored"));
			Assert.That(loaded.Traits.HasFlag(ObjectSpaceTraits.MetaData), Is.True);
		}
		Assert.That(raw.ToArray(), Is.EqualTo(expected));
	}

	/// <summary>Verifies that factory reconstruction preserves an explicitly parameterless serializer registration.</summary>
	[Test]
	public void ParameterlessSerializerKeepsItsConstructorChoice([Values] Endianness endianness, [Values] bool storeMetaData) {
		using var raw = new MemoryStream();
		var builder = new ObjectSpaceBuilder()
			.WithIndexBTreeOrder(4)
			.WithEndianness(endianness)
			.UseMemoryStream(raw)
			.AddDimension<OnlyId>(true)
				.UsingSerializer<ConstructorSensitiveSerializer>()
				.WithIdentifier(item => item.Id)
				.Done();
		if (storeMetaData)
			builder.StoreMetaData();
		using (var created = builder.Build())
			created.Save(new OnlyId { Id = 17 });
		raw.Position = 0;
		ObjectSpace loaded;
		if (storeMetaData)
			Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out loaded), Is.True);
		else
			loaded = builder.Build();
		using var loadedScope = loaded;
		Assert.That(loaded.Get((OnlyId item) => item.Id, 17L).Id, Is.EqualTo(17));
	}

	/// <summary>Verifies constructor provenance while allowing later registrations on the caller-owned factory.</summary>
	[Test]
	public void SuppliedFactoryPreservesItsConstructorChoiceWithoutFreezingIt([Values] Endianness endianness, [Values] bool storeMetaData) {
		using var raw = new MemoryStream();
		var factory = new SerializerFactory(SerializerFactory.Default).Register<OnlyId, ConstructorSensitiveSerializer>();
		var builder = new ObjectSpaceBuilder()
			.WithIndexBTreeOrder(4)
			.WithEndianness(endianness)
			.UsingSerializerFactory(factory)
			.UseMemoryStream(raw)
			.AddDimension<OnlyId>(true)
				.WithIdentifier(item => item.Id)
				.Done();
		if (storeMetaData)
			builder.StoreMetaData();
		using (var created = builder.Build())
			created.Save(new OnlyId { Id = 17 });
		Assert.That(() => factory.RegisterEnum<UnrelatedRegistration>(), Throws.Nothing);
		Assert.That(factory.ContainsSerializer<UnrelatedRegistration>(), Is.True);
		raw.Position = 0;
		ObjectSpace loaded;
		if (storeMetaData)
			Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out loaded), Is.True);
		else
			loaded = builder.Build();
		using var loadedScope = loaded;
		Assert.That(loaded.Get((OnlyId item) => item.Id, 17L).Id, Is.EqualTo(17));
		Assert.That(loaded.Serializers.ContainsSerializer<UnrelatedRegistration>(), Is.EqualTo(!storeMetaData));
	}

	/// <summary>Verifies that direct owner construction and reopening preserve the supplied factory type code.</summary>
	[Test]
	public void DirectConstructorPreservesTheSuppliedFactoryTypeCode([Values] Endianness endianness, [Values] bool storeMetaData) {
		using var raw = new MemoryStream();
		var definition = new ObjectSpaceBuilder()
			.WithEndianness(endianness)
			.AddDimension<OnlyId>(true)
				.WithIdentifier(item => item.Id)
				.Done()
			.BuildDefinition();
		if (storeMetaData)
			definition.Traits |= ObjectSpaceTraits.MetaData;
		var serializers = new SerializerFactory(SerializerFactory.Default).SetMinTypeCode(9000).RegisterAutoBuild<OnlyId>();
		Assert.That(serializers.FindRegistration(typeof(OnlyId)).TypeCode, Is.EqualTo(9000));
		using (var created = new MemoryObjectSpace(raw, definition, serializers, new ComparerFactory(ComparerFactory.Default), endianness: endianness))
			created.Save(new OnlyId { Id = 17 });
		Assert.That(raw.CanRead && raw.CanWrite, Is.True);
		Assert.That(serializers.FindRegistration(typeof(OnlyId)).TypeCode, Is.EqualTo(9000));

		raw.Position = 0;
		ObjectSpace loaded;
		if (storeMetaData)
			Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out loaded), Is.True);
		else
			loaded = new MemoryObjectSpace(raw, definition, serializers, new ComparerFactory(ComparerFactory.Default), endianness: endianness);
		using var loadedScope = loaded;
		Assert.That(loaded.Serializers.FindRegistration(typeof(OnlyId)).TypeCode, Is.EqualTo(9000));
		Assert.That(loaded.Get((OnlyId item) => item.Id, 17L).Id, Is.EqualTo(17));
	}

	/// <summary>Verifies that unsupported comparer registrations fail before memory or file storage is created.</summary>
	[Test]
	public void DirectMetadataConstructorsRejectCustomComparerRegistrationsBeforeWriting([Values] bool fileBacked, [Values] bool equalityComparer) {
		// Direct constructors must enforce the same reconstructable-comparer boundary as the builder.
		using var raw = new MemoryStream();
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var path = Path.Combine(folder, "store.dat");
		var definition = new ObjectSpaceBuilder()
			.StoreMetaData()
			.AddDimension<OnlyId>(true)
				.WithIdentifier(item => item.Id)
				.Done()
			.BuildDefinition();
		var serializers = new SerializerFactory(SerializerFactory.Default).RegisterAutoBuild<OnlyId>();
		var comparers = new ComparerFactory(ComparerFactory.Default);
		if (equalityComparer)
			comparers.RegisterEqualityComparer(new ProjectionEqualityComparer<OnlyId, long>(item => item.Id));
		else
			comparers.RegisterComparer(Comparer<OnlyId>.Create((left, right) => right.Id.CompareTo(left.Id)));

		// The file guard also precedes construction of its transactional backend.
		if (fileBacked) {
			var descriptor = Sphere10FrameworkFileDescriptor.From(path, null);
			Assert.That(() => new FileObjectSpace(descriptor, definition, serializers, comparers), Throws.TypeOf<NotSupportedException>());
			Assert.That(File.Exists(path), Is.False);
		} else {
			Assert.That(() => new MemoryObjectSpace(raw, definition, serializers, comparers), Throws.TypeOf<NotSupportedException>());
			Assert.That(raw.Length, Is.Zero);
			Assert.That(raw.CanRead && raw.CanWrite, Is.True);
		}
	}

	/// <summary>Verifies that stored metadata enforces comparer compatibility even without a creation flag.</summary>
	[Test]
	public void StoredMetadataRejectsCustomComparersWhenTheSuppliedDefinitionOmitsMetadata([Values] bool fileBacked) {
		using var raw = new MemoryStream();
		using (var created = CreateBuilder(null, Endianness.LittleEndian, false).UseMemoryStream(raw).Build())
			created.Save(new Record { Id = 17, Code = "stored" });
		var expected = raw.ToArray();
		var definition = CreateBuilder(null, Endianness.LittleEndian, false, storeMetaData: false).BuildDefinition();
		var serializers = new SerializerFactory(SerializerFactory.Default).RegisterAutoBuild<Record>().RegisterAutoBuild<Audit>();
		var comparers = new ComparerFactory(ComparerFactory.Default);
		comparers.RegisterEqualityComparer(new ProjectionEqualityComparer<Record, long>(item => item.Id));

		// Stored traits remain authoritative even when this caller did not request metadata creation.
		if (fileBacked) {
			var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
			using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
			var path = Path.Combine(folder, "store.dat");
			File.WriteAllBytes(path, expected);
			var descriptor = Sphere10FrameworkFileDescriptor.From(path, folder, clusterSize: 127);
			using (var file = new FileObjectSpace(descriptor, definition, serializers, comparers, FileAccessMode.OpenOrCreate))
				Assert.That(() => file.Load(), Throws.TypeOf<NotSupportedException>());
			Assert.That(File.ReadAllBytes(path), Is.EqualTo(expected));
		} else {
			Assert.That(() => new MemoryObjectSpace(raw, definition, serializers, comparers), Throws.TypeOf<NotSupportedException>());
			Assert.That(raw.ToArray(), Is.EqualTo(expected));
		}
	}

	/// <summary>Verifies that deferred activation rejects comparer changes made after owner construction.</summary>
	[Test]
	public void DeferredMetadataCreationRechecksComparerRegistrationsBeforeWriting() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var path = Path.Combine(folder, "store.dat");
		var definition = new ObjectSpaceBuilder()
			.StoreMetaData()
			.AddDimension<OnlyId>(true)
				.WithIdentifier(item => item.Id)
				.Done()
			.BuildDefinition();
		var serializers = new SerializerFactory(SerializerFactory.Default).RegisterAutoBuild<OnlyId>();
		var comparers = new ComparerFactory(ComparerFactory.Default);
		using var file = new FileObjectSpace(Sphere10FrameworkFileDescriptor.From(path, folder), definition, serializers, comparers, FileAccessMode.OpenOrCreate);

		// Activation must recheck configuration that was still valid when the deferred owner was constructed.
		comparers.RegisterEqualityComparer(new ProjectionEqualityComparer<OnlyId, long>(item => item.Id));
		Assert.That(() => file.Load(), Throws.TypeOf<NotSupportedException>());
		Assert.That(File.Exists(path) && new FileInfo(path).Length > 0, Is.False);
	}

	/// <summary>Verifies that supplied definitions retain persisted dimension root and change-tracking settings.</summary>
	[Test]
	public void SuppliedDefinitionMustPreserveDimensionConstruction([Values] Endianness endianness, [Values] bool changeRoot) {
		using var raw = new MemoryStream();
		using (var created = CreateBuilder(null, endianness, false).UseMemoryStream(raw).Build())
			created.Save(new Record { Id = 17, Code = "stored" });
		var expected = raw.ToArray();

		// A different GC-root role or tracking member is a different persisted dimension contract.
		var builder = CreateBuilder(null, endianness, false).UseMemoryStream(raw);
		if (changeRoot)
			builder.Configure<Record>().AsRoot();
		else
			builder.Configure<Record>().WithChangeTrackingVia(item => item.OtherDirty);
		Assert.That(() => builder.Build(), Throws.TypeOf<InvalidDataException>());
		Assert.That(raw.ToArray(), Is.EqualTo(expected));
	}

	/// <summary>Verifies rejection of unreconstructible serializer instances while retaining explicit legacy setup.</summary>
	[Test]
	public void SerializerInstanceIsRefusedBeforeWritingAndLegacyKeepsItsSuppliedPath() {
		using var raw = new MemoryStream();
		// Creates the same supplied-instance schema for metadata rejection and legacy construction.
		ObjectSpaceBuilder CreateBuilder() => new ObjectSpaceBuilder()
			.WithIndexBTreeOrder(4)
			.UseMemoryStream(raw)
			.AddDimension<OnlyId>(true)
				.UsingSerializer(new ConstructorSensitiveSerializer())
				.WithIdentifier(item => item.Id)
				.Done();
		var builder = CreateBuilder().StoreMetaData();
		Assert.That(() => builder.Build(), Throws.TypeOf<NotSupportedException>());
		Assert.That(raw.Length, Is.Zero);
		builder = CreateBuilder();
		using (var created = builder.Build())
			created.Save(new OnlyId { Id = 17 });
		using var reopened = builder.Build();
		Assert.That(reopened.Get((OnlyId item) => item.Id, 17L).Id, Is.EqualTo(17));
	}

	/// <summary>Verifies that saved factory metadata retains actual composite member order and original type codes.</summary>
	[Test]
	public void SavedFactoryPreservesConfiguredCompositeLayout([Values] Endianness endianness) {
		// Reverse the composite's actual member order before storage captures the factory.
		using var raw = new MemoryStream();
		var factory = new SerializerFactory(SerializerFactory.Default).SetMinTypeCode(9000).RegisterAutoBuild<Audit>();
		var serializer = (CompositeSerializer<Audit>)factory.GetPureSerializer<Audit>();
		Array.Reverse(serializer.MemberBindings);
		var originalMembers = serializer.MemberBindings.Select(binding => binding.Member.Name).ToArray();
		var builder = new ObjectSpaceBuilder()
			.StoreMetaData()
			.WithEndianness(endianness)
			.UsingSerializerFactory(factory)
			.UseMemoryStream(raw)
			.AddDimension<Audit>(true)
				.WithIdentifier(item => item.Id)
				.Done();
		using (var created = builder.Build())
			created.Save(new Audit { Id = 17, Message = "configured layout" });

		// Reopening must use that saved order and type code, rather than reassemble the current CLR members.
		raw.Position = 0;
		Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out var loaded), Is.True);
		using (loaded) {
			var restoredSerializer = (CompositeSerializer<Audit>)loaded.Serializers.GetPureSerializer<Audit>();
			Assert.That(restoredSerializer.MemberBindings.Select(binding => binding.Member.Name), Is.EqualTo(originalMembers));
			Assert.That(loaded.Serializers.FindRegistration(typeof(Audit)).TypeCode, Is.EqualTo(9000));
			Assert.That(loaded.Get((Audit item) => item.Id, 17L).Message, Is.EqualTo("configured layout"));
			loaded.Save(new Audit { Id = 18, Message = "further write" });
		}
		raw.Position = 0;
		Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out var reopened), Is.True);
		using var reopenedScope = reopened;
		Assert.That(reopened.Get((Audit item) => item.Id, 18L).Message, Is.EqualTo("further write"));
	}

	/// <summary>Verifies rejection of an owner header whose required metadata allocation is missing.</summary>
	[Test]
	public void MetadataFlagRequiresItsBlobBeforeClusterStorage([Values] Endianness endianness, [Values] bool merkleized) {
		using var original = new MemoryStream();
		using (var created = CreateBuilder(null, endianness, merkleized).UseMemoryStream(original).Build())
			created.Save(new Record { Id = 17, Code = "stored" });
		var source = original.ToArray();
		var bytes = source.AsSpan(0, ClusteredStreamsHeader.ByteLength).ToArray();
		using var raw = new MemoryStream(bytes);
		Assert.That(() => CreateBuilder(null, endianness, merkleized, storeMetaData: false).UseMemoryStream(raw).Build(), Throws.TypeOf<InvalidDataException>());
		Assert.That(raw.ToArray(), Is.EqualTo(bytes));
		raw.Position = 0;
		Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out var failedOpen), Is.False);
		Assert.That(failedOpen, Is.Null);
		Assert.That(raw.ToArray(), Is.EqualTo(bytes));
	}

	/// <summary>Verifies that deferred loading and rollback retain the owner container, lock and parent scope.</summary>
	[Test]
	public void DeferredFileLoadAndRollbackRetainTheRootStorage([Values] Endianness endianness, [Values] bool merkleized) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var path = Path.Combine(folder, "store.dat");
		var pages = Path.Combine(folder, "pages");
		Directory.CreateDirectory(pages);
		var builder = CreateBuilder(BTreeType.BTreePlus, endianness, merkleized)
			.UseFile(path)
			.KeepUncommittedPagesAt(pages)
			.WithPageSize(4096)
			.WithMaxMemory(8192);
		using (var file = (FileObjectSpace)builder.Build()) {
			Assert.That(file.RequiresLoad, Is.True);
			Assert.That(File.Exists(path) && new FileInfo(path).Length > 0, Is.False);
			var parent = new CriticalObject();
			file.ParentCriticalObject = parent;
			var storage = file.Streams;
			var storageLock = file.Lock;
			file.Load();
			Assert.That(file.Streams, Is.SameAs(storage));
			Assert.That(file.Lock, Is.SameAs(storageLock));
			Assert.That(file.ParentCriticalObject, Is.SameAs(parent));
			var discarded = new Record { Id = 1, Code = "discarded" };
			file.Save(discarded);
			Assert.That(() => file.Load(), Throws.TypeOf<InvalidOperationException>().With.Message.Contains("already loaded"));
			Assert.That(file.Get((Record item) => item.Id, 1L), Is.SameAs(discarded));
			file.Rollback();
			Assert.That(file.Streams, Is.SameAs(storage));
			Assert.That(file.Lock, Is.SameAs(storageLock));
			Assert.That(file.ParentCriticalObject, Is.SameAs(parent));
			Assert.That(file.Count<Record>(), Is.Zero);
			file.Save(new Record { Id = 2, Code = "retained" });
			file.Commit();
			file.Save(new Record { Id = 3, Code = "discarded-after-commit" });
			file.Rollback();
			Assert.That(file.Streams, Is.SameAs(storage));
			Assert.That(file.Lock, Is.SameAs(storageLock));
			Assert.That(file.ParentCriticalObject, Is.SameAs(parent));
			Assert.That(file.Get((Record item) => item.Id, 2L).Code, Is.EqualTo("retained"));
			Assert.That(file.TryGet((Record item) => item.Id, 3L, out _), Is.False);
		}
		using var raw = File.Open(path, FileMode.Open, FileAccess.ReadWrite);
		Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out var loaded), Is.True);
		using var loadedScope = loaded;
		Assert.That(loaded.Get((Record item) => item.Id, 2L).Code, Is.EqualTo("retained"));
		Assert.That(loaded.TryGet((Record item) => item.Id, 1L, out _), Is.False);
	}

	/// <summary>Verifies that deferred creation captures the configuration present at first activation.</summary>
	[Test]
	public void DeferredFileMetadataUsesTheConfigurationAtFirstLoad([Values] Endianness endianness) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var path = Path.Combine(folder, "store.dat");
		using (var file = (FileObjectSpace)CreateBuilder(null, endianness, true).UseFile(path).Build()) {
			var storage = file.Streams;
			file.Definition.HashFunction = CHF.SHA2_512;
			file.Load();
			Assert.That(file.Streams, Is.SameAs(storage));
			file.Save(new Record { Id = 17, Code = "configured-before-load" });
			file.Commit();
		}

		using var raw = File.Open(path, FileMode.Open, FileAccess.ReadWrite);
		Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out var loaded), Is.True);
		using (loaded) {
			Assert.That(loaded.Definition.HashFunction, Is.EqualTo(CHF.SHA2_512));
			Assert.That(loaded.Get((Record item) => item.Id, 17L).Code, Is.EqualTo("configured-before-load"));
		}
	}

	/// <summary>Verifies metadata-backed file loading through an interrupted commit recovery.</summary>
	[Test]
	public void DeferredMetadataFileRecoversPendingCommitOnLoad([Values] Endianness endianness) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		byte[] bytes;
		using (var memory = new MemoryStream()) {
			using var original = CreateBuilder(null, endianness, true).UseMemoryStream(memory).Build();
			original.Save(new Record { Id = 17, Code = "recovered" });
			original.Flush();
			bytes = memory.ToArray();
		}

		// Stage one complete page and an interrupted operation without opening the ObjectSpace first.
		var path = Path.Combine(folder, "store.dat");
		var pages = Path.Combine(folder, "pages");
		Directory.CreateDirectory(pages);
		File.WriteAllBytes(path, Array.Empty<byte>());
		var descriptor = Sphere10FrameworkFileDescriptor.From(path, pages, 262144, 1048576, 127, ClusteredStreamsPolicy.Default, endianness);
		Assert.That(bytes.Length, Is.LessThanOrEqualTo(descriptor.PageSize));
		var fileID = TransactionalFileMappedListBase<byte>.ComputeFileID(descriptor.CaseCorrectPath);
		var page = TransactionalFileMappedListBase<byte>.MarkerRepository.GeneratePageMarkerFileName(pages, fileID,
			TransactionalFileMappedListBase<byte>.PageMarkerType.UncommittedPage, 0);
		var marker = TransactionalFileMappedListBase<byte>.MarkerRepository.GenerateFileMarkerFileName(pages, fileID,
			TransactionalFileMappedListBase<byte>.FileMarkerType.Committing);
		File.WriteAllBytes(page, bytes);
		File.WriteAllBytes(marker, Array.Empty<byte>());

		var builder = CreateBuilder(null, endianness, true);
		var serializers = new SerializerFactory(SerializerFactory.Default).RegisterAutoBuild<Record>().RegisterAutoBuild<Audit>();
		using var file = new FileObjectSpace(descriptor, builder.BuildDefinition(), serializers, new ComparerFactory(ComparerFactory.Default), FileAccessMode.OpenOrCreate);
		var storage = file.Streams;
		Assert.That(file.RequiresLoad, Is.True);
		Assert.That(File.Exists(page), Is.True);
		Assert.That(File.Exists(marker), Is.True);
		file.Load();
		Assert.That(file.Streams, Is.SameAs(storage));
		Assert.That(file.Get((Record item) => item.Id, 17L).Code, Is.EqualTo("recovered"));
		Assert.That(File.Exists(marker), Is.False);
		file.Commit();
	}

	/// <summary>Verifies transactional data, index membership and Merkle roots before direct raw-file reopening.</summary>
	[Test]
	public void FileCommitRollbackAndRawOpenPreserveData(
		[Values] BTreeType type,
		[Values] Endianness endianness,
		[Values] bool merkleized) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var path = Path.Combine(folder, "store.dat");
		var pages = Path.Combine(folder, "pages");
		Directory.CreateDirectory(pages);
		var builder = CreateBuilder(type, endianness, merkleized)
			.UseFile(path)
			.KeepUncommittedPagesAt(pages)
			.WithPageSize(4096)
			.WithMaxMemory(8192)
			.AutoLoad();
		byte[] expectedRoot;
		using (var file = (FileObjectSpace)builder.Build()) {
			for (var id = 0; id < 16; id++)
				file.Save(new Record { Id = id, Code = $"code-{id}", Group = id % 4, Payload = new string('a', 4096 + id) });
			file.Commit();
			var record = file.Get((Record item) => item.Id, 1L);
			record.Code = "pending";
			record.Dirty = true;
			file.Flush();
			file.Rollback();
			Assert.That(file.Get((Record item) => item.Id, 1L).Code, Is.EqualTo("code-1"));
			Assert.That(file.TryGet((Record item) => item.Code, "pending", out _), Is.False);
			expectedRoot = merkleized ? ReadRoot(file) : null;
		}
		var expected = File.ReadAllBytes(path);
		using var raw = File.Open(path, FileMode.Open, FileAccess.ReadWrite);
		var metadataEnd = 0;
		raw.Position = 0;
		Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out var loaded), Is.True);
		using (loaded) {
			metadataEnd = checked((int)loaded.Streams.DataOffset);
			Assert.That(loaded.Count<Record>(), Is.EqualTo(16));
			for (var id = 0; id < 16; id++) {
				var record = loaded.Get((Record item) => item.Id, (long)id);
				Assert.That(record.Code, Is.EqualTo($"code-{id}"));
				Assert.That(record.Group, Is.EqualTo(id % 4));
				Assert.That(record.Payload, Is.EqualTo(new string('a', 4096 + id)));
			}
			if (merkleized)
				Assert.That(ReadRoot(loaded), Is.EqualTo(expectedRoot));
		}
		Assert.That(raw.CanRead, Is.True);
		Assert.That(raw.Length, Is.EqualTo(expected.Length));
		raw.Position = 0;
		var actual = new byte[expected.Length];
		raw.ReadExactly(actual);
		// Writable B-tree disposal retains its existing header updates; owner metadata remains immutable.
		Assert.That(actual.AsSpan(ClusteredStreamsHeader.MetaDataSizeOffset, metadataEnd - ClusteredStreamsHeader.MetaDataSizeOffset).ToArray(), Is.EqualTo(expected.AsSpan(ClusteredStreamsHeader.MetaDataSizeOffset, metadataEnd - ClusteredStreamsHeader.MetaDataSizeOffset).ToArray()));
	}

	/// <summary>Verifies direct opening from the selected cursor while preserving an unrelated byte prefix.</summary>
	[Test]
	public void RawOpenReconstructsAPrefixedObjectSpace([Values] Endianness endianness) {
		byte[] bytes;
		using (var original = new MemoryStream()) {
			using (var created = CreateBuilder(null, endianness, false).UseMemoryStream(original).Build())
				created.Save(new Record { Id = 17, Code = "prefixed" });
			bytes = original.ToArray();
		}
		var prefix = new byte[] { 9, 8, 7, 6, 5, 4, 3 };
		using var raw = new MemoryStream();
		raw.Write(prefix);
		raw.Write(bytes);
		raw.Position = prefix.Length;
		Assert.That(ObjectSpace.TryOpenFromStream(raw, endianness, out var loaded), Is.True);
		using (loaded) {
			Assert.That(loaded.Get((Record item) => item.Id, 17L).Code, Is.EqualTo("prefixed"));
		}
		Assert.That(raw.ToArray().AsSpan(0, prefix.Length).ToArray(), Is.EqualTo(prefix));
		Assert.That(raw.CanRead && raw.CanWrite, Is.True);
	}

	/// <summary>Creates the shared two-dimension schema with the requested storage traits and index preferences.</summary>
	private static ObjectSpaceBuilder CreateBuilder(BTreeType? type, Endianness endianness, bool merkleized, bool storeMetaData = true) {
		var builder = new ObjectSpaceBuilder()
			.WithIndexBTreeOrder(4)
			.WithClusterSize(127)
			.WithEndianness(endianness)
			.AutoSave();
		if (storeMetaData)
			builder.StoreMetaData();
		if (type.HasValue)
			builder.WithIndexBTreeType(type.Value);
		builder.AddDimension<Record>(true)
			.WithIdentifier(item => item.Id)
			.WithUniqueIndexOn(item => item.Code)
			.WithIndexOn(item => item.Group)
			.WithChangeTrackingVia(item => item.Dirty)
			.Done();
		builder.AddDimension<Audit>(true)
			.WithIdentifier(item => item.Id)
			.WithChangeTrackingVia(item => item.Dirty)
			.Done();
		return merkleized ? builder.Merkleized() : builder;
	}

	/// <summary>Reads the existing ObjectSpace Merkle root through its established header extension.</summary>
	private static byte[] ReadRoot(ObjectSpace space) {
		using var access = space.EnterAccessScope();
		var size = Hashers.GetDigestSizeBytes(space.Definition.HashFunction);
		return space.Streams.Header.MapExtensionProperty(0, sizeof(bool) + size, new ConstantSizeNullableByteArraySerializer(size)).Value;
	}

	/// <summary>Provides indexed data and transient dirty flags for reconstruction and tracked-object scenarios.</summary>
	public sealed class Record {
		public long Id { get; set; }
		public string Code { get; set; }
		public int Group { get; set; }
		public string Payload { get; set; }
		[Transient]
		public bool Dirty { get; set; }
		[Transient]
		public bool OtherDirty { get; set; }
	}

	/// <summary>Provides a second indexed dimension with explicit change tracking.</summary>
	public sealed class Audit {
		public long Id { get; set; }
		public string Message { get; set; }
		[Transient]
		public bool Dirty { get; set; }
	}

	/// <summary>Provides an identifier-only payload for factory construction and compatibility scenarios.</summary>
	public sealed class OnlyId {
		public long Id { get; set; }
	}

	/// <summary>Provides an extra registration used to verify that the supplied factory remains mutable.</summary>
	public enum UnrelatedRegistration {
		Value
	}

	/// <summary>Distinguishes parameterless construction from serializer injection through its encoded mask.</summary>
	public sealed class ConstructorSensitiveSerializer : ConstantSizeItemSerializerBase<OnlyId> {
		private readonly long _mask;

		/// <summary>Creates the parameterless encoding used by explicitly parameterless factory registrations.</summary>
		public ConstructorSensitiveSerializer()
			: base(sizeof(long), false) => _mask = 42;

		/// <summary>Creates a different encoding to make constructor-injection choices observable.</summary>
		public ConstructorSensitiveSerializer(IItemSerializer<long> serializer)
			: base(sizeof(long), false) => _mask = 99;

		/// <summary>Writes the identifier using the mask selected by the constructor.</summary>
		public override void Serialize(OnlyId item, EndianBinaryWriter writer, SerializationContext context) => writer.Write(item.Id ^ _mask);

		/// <summary>Restores an identifier using the mask selected by the constructor.</summary>
		public override OnlyId Deserialize(EndianBinaryReader reader, SerializationContext context) => new() { Id = reader.ReadInt64() ^ _mask };
	}
}
