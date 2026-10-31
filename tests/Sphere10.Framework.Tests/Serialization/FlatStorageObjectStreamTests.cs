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

namespace Sphere10.Framework.Tests;

/// <summary>Verifies direct reconstruction of ObjectStreams from their stored metadata and attachments.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class FlatStorageObjectStreamTests {
	/// <summary>Verifies that invalid construction settings fail before changing borrowed storage.</summary>
	[TestCase("clusterSize")]
	[TestCase("reservedStreams")]
	[TestCase("indexOrder")]
	[TestCase("duplicateIndex")]
	[TestCase("indexKind")]
	[TestCase("nullPolicy")]
	[TestCase("treeType")]
	[TestCase("missingSpec")]
	public void InvalidConstructionLeavesBorrowedStorageUntouched(string defect) {
		using var raw = new MemoryStream();
		raw.Position = 7;
		Action construct = () => {
			var indexes = new[] {
				new ObjectStreamMetaData.Index {
					Kind = defect == "indexKind" ? (ObjectSpaceDefinition.IndexType)999 : ObjectSpaceDefinition.IndexType.Identifier,
					Name = "id", MemberName = nameof(Record.Id),
					NullPolicy = defect == "nullPolicy" ? (IndexNullPolicy)999 : default,
					BTreeSpec = defect == "missingSpec" ? null : new BTreeSpec(defect == "treeType" ? (BTreeType)999 : BTreeType.BTree, defect == "indexOrder" ? 2 : 4)
				}
			};
			if (defect == "duplicateIndex")
				indexes = new[] { indexes[0], indexes[0] };
			using var objects = new ObjectStream<Record>(raw, clusterSize: defect == "clusterSize" ? 0 : 64,
				reservedStreams: defect == "reservedStreams" ? -1 : 0, storeMetaData: true, indexes: indexes);
		};
		if (defect is "clusterSize" or "reservedStreams" or "indexOrder" or "treeType")
			Assert.That(construct, Throws.InstanceOf<ArgumentException>());
		else
			Assert.That(construct, Throws.TypeOf<InvalidDataException>());
		Assert.That(raw.Length, Is.Zero);
		Assert.That(raw.Position, Is.EqualTo(7));
		Assert.That(raw.CanRead && raw.CanWrite, Is.True);
	}

	/// <summary>Verifies reconstructed indexes, Merkle roots and subsequent writes across repeated reopening.</summary>
	[Test]
	public void RawOpenPreservesIndexesMerkleAndFurtherWrites([Values] Endianness endianness, [Values] BTreeType treeType) {
		using var raw = new MemoryStream();
		var indexes = new[] {
			new ObjectStreamMetaData.Index { Kind = ObjectSpaceDefinition.IndexType.Identifier, Name = "id", MemberName = nameof(Record.Id), BTreeSpec = new BTreeSpec(treeType, 4) },
			new ObjectStreamMetaData.Index { Kind = ObjectSpaceDefinition.IndexType.UniqueKey, Name = "name", MemberName = nameof(Record.Name), BTreeSpec = new BTreeSpec(treeType, 4) },
			new ObjectStreamMetaData.Index { Kind = ObjectSpaceDefinition.IndexType.MerkleTree, Name = "merkle", HashFunction = CHF.SHA2_256 }
		};
		byte[] root;
		using (var created = new ObjectStream<Record>(raw, clusterSize: 64, endianness: endianness, storeMetaData: true, indexes: indexes)) {
			created.Load();
			for (var id = 0; id < 12; id++)
				created.SaveItem(id, new Record { Id = id, Name = $"name-{id}" }, ObjectStreamOperationType.Add);
			root = ((MerkleTreeIndex)created.Streams.Attachments["merkle"]).MerkleTree.Root;
		}
		raw.Position = 0;
		Assert.That(ObjectStream.TryOpenFromStream(raw, endianness, out var opened), Is.True);
		using (var loaded = (ObjectStream<Record>)opened) {
			Assert.That(loaded.LoadItem(3).Name, Is.EqualTo("name-3"));
			Assert.That(loaded.TryGetUniqueIndex<long>("id", out var idIndex), Is.True);
			Assert.That(idIndex.Values[3], Is.EqualTo(3));
			Assert.That(loaded.TryGetUniqueIndex<string>("name", out var nameIndex), Is.True);
			Assert.That(nameIndex.Values["name-7"], Is.EqualTo(7));
			Assert.That(((MerkleTreeIndex)loaded.Streams.Attachments["merkle"]).MerkleTree.Root, Is.EqualTo(root));
			loaded.SaveItem(12, new Record { Id = 12, Name = "later" }, ObjectStreamOperationType.Add);
		}
		raw.Position = 0;
		Assert.That(ObjectStream.TryOpenFromStream(raw, endianness, out var reopened), Is.True);
		using (var loaded = (ObjectStream<Record>)reopened)
			Assert.That(loaded.LoadItem(12).Name, Is.EqualTo("later"));
		Assert.That(raw.CanRead && raw.CanWrite, Is.True);
	}

	/// <summary>Verifies typed reopening with the supplied byte order and recorded cluster geometry.</summary>
	[Test]
	public void PrimitiveRawConstructorReadsItsRecordedGeometry([Values] Endianness endianness, [Values] bool reservePadding) {
		using var raw = new MemoryStream();
		using (var created = new ObjectStream<long>(raw, clusterSize: 17, endianness: endianness, preAllocateOptimization: false,
			storeMetaData: true, metaDataCapacity: reservePadding ? 64 * 1024 : 0)) {
			created.Load();
			created.SaveItem(0, 123L, ObjectStreamOperationType.Add);
			var metadata = new ObjectStreamMetaDataSerializer().DeserializeBytes(created.Streams.ReadMetaData(), endianness);
			Assert.That(metadata.SerializerFactory.FindRegistration(typeof(long)).TypeCode, Is.EqualTo(metadata.ItemTypeCode));
			var recordLength = new ObjectStreamMetaDataSerializer().CalculateSize(metadata);
			Assert.That(created.Streams.MetaDataCapacity, Is.EqualTo(reservePadding ? 64 * 1024 : recordLength));
		}
		using var loaded = new ObjectStream<long>(raw, endianness: endianness);
		loaded.Load();
		Assert.That(loaded.LoadItem(0), Is.EqualTo(123L));
		Assert.That(loaded.Streams.Endianness, Is.EqualTo(endianness));
		Assert.That(loaded.Streams.Header.ClusterSize, Is.EqualTo(17));
	}

	/// <summary>Verifies that typed reopening rejects the wrong supplied byte order without changing borrowed storage.</summary>
	[Test]
	public void TypedReopenRejectsTheWrongSuppliedEndianness([Values] Endianness endianness) {
		// Persist a real metadata store whose byte order is known to the caller.
		using var raw = new MemoryStream();
		using (var created = new ObjectStream<long>(raw, clusterSize: 17, endianness: endianness, storeMetaData: true)) {
			created.Load();
			created.SaveItem(0, 123L, ObjectStreamOperationType.Add);
		}
		var expected = raw.ToArray();
		var wrongEndianness = endianness == Endianness.LittleEndian ? Endianness.BigEndian : Endianness.LittleEndian;

		// Typed construction must not silently discover the opposite byte order or load it as legacy storage.
		Assert.That(() => {
			using var invalid = new ObjectStream<long>(raw, clusterSize: 17, endianness: wrongEndianness);
			invalid.Load();
		}, Throws.InstanceOf<InvalidOperationException>());
		Assert.That(raw.CanRead && raw.CanWrite, Is.True);
		Assert.That(raw.ToArray(), Is.EqualTo(expected));

		// The same borrowed store remains readable using the supplied correct order.
		raw.Position = 0;
		Assert.That(ObjectStream.TryOpenFromStream(raw, endianness, out var reopened), Is.True);
		using var reopenedScope = reopened;
		Assert.That(((ObjectStream<long>)reopened).LoadItem(0), Is.EqualTo(123L));
	}

	/// <summary>Verifies that typed reopening cannot bypass corrupt owner words on a metadata-enabled store.</summary>
	[Test]
	public void TypedReopenRejectsCorruptMetadataOwnerWords([Values] Endianness endianness, [Values("magic", "endianness")] string defect) {
		// Matching default geometry ensures a legacy fallback could otherwise load the primitive payload successfully.
		using var original = new MemoryStream();
		using (var created = new ObjectStream<long>(original, endianness: endianness, storeMetaData: true)) {
			created.Load();
			created.SaveItem(0, 123L, ObjectStreamOperationType.Add);
		}
		var bytes = original.ToArray();
		var converter = EndianBitConverter.For(endianness);
		if (defect == "magic") {
			converter.WriteTo(0U, bytes, ClusteredStreamsHeader.MetaDataSizeOffset - 2 * sizeof(uint));
		} else {
			var offset = ClusteredStreamsHeader.MetaDataSizeOffset - sizeof(uint);
			converter.WriteTo(converter.ToUInt32(bytes, offset) ^ ObjectStreamStorageHelper.BigEndianTrait, bytes, offset);
		}
		var expected = (byte[])bytes.Clone();
		using var borrowed = new MemoryStream(bytes);

		// Metadata presence requires valid owner framing even when the item type is supplied separately.
		Assert.That(() => {
			using var objects = new ObjectStream<long>(borrowed, endianness: endianness);
			objects.Load();
		}, Throws.TypeOf<InvalidDataException>());
		Assert.That(borrowed.CanRead && borrowed.CanWrite, Is.True);
		Assert.That(borrowed.ToArray(), Is.EqualTo(expected));
	}

	/// <summary>Verifies that opening and disposing a reconstructed owner preserves the caller stream and its bytes.</summary>
	[Test]
	public void RawOpenBorrowsStreamWithoutChangingItsBytes([Values] Endianness endianness) {
		byte[] bytes;
		using (var raw = new MemoryStream()) {
			using (var created = new ObjectStream<string>(raw, endianness: endianness, storeMetaData: true)) {
				created.Load();
				created.SaveItem(0, "stored", ObjectStreamOperationType.Add);
			}
			bytes = raw.ToArray();
		}
		using var borrowed = new MemoryStream(bytes);
		borrowed.Position = 0;
		Assert.That(ObjectStream.TryOpenFromStream(borrowed, endianness, out var opened), Is.True);
		using (var loaded = (ObjectStream<string>)opened) {
			Assert.That(loaded.LoadItem(0), Is.EqualTo("stored"));
		}
		Assert.That(borrowed.CanRead && borrowed.CanWrite, Is.True);
		Assert.That(borrowed.ToArray(), Is.EqualTo(bytes));
	}

	/// <summary>Verifies that attachment-loading errors propagate while failure cleanup preserves borrowed storage.</summary>
	[Test]
	public void RawOpenPropagatesIndexLoadFailureAndKeepsBorrowedStorage([Values] Endianness endianness) {
		// Keep the owner metadata valid while locating its identifier's physical B-tree stream.
		using var raw = new MemoryStream();
		var indexes = new[] {
			new ObjectStreamMetaData.Index {
				Kind = ObjectSpaceDefinition.IndexType.Identifier,
				Name = "id",
				MemberName = nameof(Record.Id),
				BTreeSpec = new BTreeSpec(BTreeType.BTree, 4)
			}
		};
		int indexStream;
		using (var created = new ObjectStream<Record>(raw, endianness: endianness, storeMetaData: true, indexes: indexes)) {
			created.Load();
			created.SaveItem(0, new Record { Id = 17, Name = "stored" }, ObjectStreamOperationType.Add);
			indexStream = created.Streams.Attachments.Values.OfType<BTreeStorageAttachment<long, long>>().Single().BaseReservedStreamIndex;
		}

		// Metadata scanning can still succeed; normal attachment loading must reject the truncated tree header.
		using (var streams = ClusteredStreams.FromStream(raw, endianness, autoLoad: true)) {
			using var index = streams.OpenWrite(indexStream);
			index.SetLength(1);
		}
		var expected = raw.ToArray();
		raw.Position = 0;

		// Failure cleanup must propagate the attachment error without closing or flushing the borrowed storage.
		Assert.That(() => ObjectStream.TryOpenFromStream(raw, endianness, out _),
			Throws.TypeOf<InvalidDataFormatException>().With.Message.Contains("StreamMappedBTree header"));
		Assert.That(raw.CanRead && raw.CanWrite, Is.True);
		Assert.That(raw.ToArray(), Is.EqualTo(expected));
	}

	/// <summary>Verifies that ordinary construction reopens legacy bytes using their supplied cluster size.</summary>
	[Test]
	public void DefaultRawConstructorKeepsLegacyHeaderAndSuppliedGeometry() {
		using var raw = new MemoryStream();
		using (var created = new ObjectStream<long>(raw, clusterSize: 17)) {
			created.Load();
			created.SaveItem(0, 123L, ObjectStreamOperationType.Add);
		}
		var expected = raw.ToArray();
		Assert.That(expected[0], Is.EqualTo(1));
		using (var loaded = new ObjectStream<long>(raw, clusterSize: 17)) {
			loaded.Load();
			Assert.That(loaded.LoadItem(0), Is.EqualTo(123L));
		}
		Assert.That(raw.ToArray(), Is.EqualTo(expected));
	}

	/// <summary>Verifies grouped and binary-key indexes after metadata-based or supplied-configuration reopening.</summary>
	[Test]
	public void NonuniqueAndBinaryKeysKeepTheirComparersAndRecordedTreeTypes(
		[Values] Endianness endianness, [Values] bool storeMetaData) {
		using var raw = new MemoryStream();
		var indexes = new[] {
			new ObjectStreamMetaData.Index { Kind = ObjectSpaceDefinition.IndexType.Index, Name = "group", MemberName = nameof(IndexedRecord.Group), BTreeSpec = new BTreeSpec(BTreeType.BTreePlus, 4) },
			new ObjectStreamMetaData.Index { Kind = ObjectSpaceDefinition.IndexType.UniqueKey, Name = "bytes", MemberName = nameof(IndexedRecord.Key), BTreeSpec = new BTreeSpec(BTreeType.BTreePlus, 4) },
			new ObjectStreamMetaData.Index { Kind = ObjectSpaceDefinition.IndexType.MerkleTree, Name = "merkle", HashFunction = CHF.SHA2_256 }
		};
		using (var created = new ObjectStream<IndexedRecord>(raw, endianness: endianness, storeMetaData: storeMetaData, indexes: indexes)) {
			created.Load();
			created.SaveItem(0, new IndexedRecord { Group = 1, Key = new byte[] { 1, 2 } }, ObjectStreamOperationType.Add);
			created.SaveItem(1, new IndexedRecord { Group = 1, Key = new byte[] { 3, 4 } }, ObjectStreamOperationType.Add);
			Assert.That(created.TryGetIndex<int>("group", out var group), Is.True);
			Assert.That(group.Values[1], Is.EqualTo(new[] { 0L, 1L }));
			Assert.That(created.TryGetUniqueIndex<byte[]>("bytes", out var bytes), Is.True);
			Assert.That(bytes.Values[new byte[] { 1, 2 }], Is.EqualTo(0));
			Assert.That(() => created.SaveItem(2, new IndexedRecord { Group = 2, Key = new byte[] { 1, 2 } }, ObjectStreamOperationType.Add), Throws.InvalidOperationException);
			Assert.That(created.Streams.Attachments.ContainsKey("merkle"), Is.True);
		}
		ObjectStream<IndexedRecord> loaded;
		if (storeMetaData) {
			raw.Position = 0;
			Assert.That(ObjectStream.TryOpenFromStream(raw, endianness, out var opened), Is.True);
			loaded = (ObjectStream<IndexedRecord>)opened;
		} else {
			loaded = new ObjectStream<IndexedRecord>(raw, endianness: endianness, indexes: indexes);
		}
		using var loadedScope = loaded;
		if (loaded.RequiresLoad)
			loaded.Load();
		Assert.That(loaded.TryGetIndex<int>("group", out var restoredGroup), Is.True);
		Assert.That(restoredGroup.Values[1], Is.EqualTo(new[] { 0L, 1L }));
		Assert.That(loaded.TryGetUniqueIndex<byte[]>("bytes", out var restoredBytes), Is.True);
		Assert.That(restoredBytes.Values[new byte[] { 3, 4 }], Is.EqualTo(1));
	}

	/// <summary>Verifies direct opening from the selected cursor without consuming an unrelated prefix.</summary>
	[Test]
	public void RawOpenReconstructsAPrefixedObjectStream([Values] Endianness endianness) {
		byte[] bytes;
		using (var original = new MemoryStream()) {
			using (var created = new ObjectStream<Record>(original, endianness: endianness, storeMetaData: true)) {
				created.Load();
				created.SaveItem(0, new Record { Id = 17, Name = "prefixed" }, ObjectStreamOperationType.Add);
			}
			bytes = original.ToArray();
		}
		var prefix = new byte[] { 9, 8, 7, 6, 5, 4, 3 };
		using var raw = new MemoryStream();
		raw.Write(prefix);
		raw.Write(bytes);
		raw.Position = prefix.Length;
		Assert.That(ObjectStream.TryOpenFromStream(raw, endianness, out var opened), Is.True);
		using (var loaded = (ObjectStream<Record>)opened) {
			Assert.That(loaded.LoadItem(0).Name, Is.EqualTo("prefixed"));
		}
		Assert.That(raw.ToArray().AsSpan(0, prefix.Length).ToArray(), Is.EqualTo(prefix));
		Assert.That(raw.CanRead && raw.CanWrite, Is.True);
	}

	/// <summary>Provides constant-size and variable-size members for identifier and unique-index reconstruction.</summary>
	public sealed class Record {
		public long Id { get; set; }
		public string Name { get; set; }
	}

	/// <summary>Provides grouped and binary keys for comparer and index-layout round trips.</summary>
	public sealed class IndexedRecord {
		public int Group { get; set; }
		public byte[] Key { get; set; }
	}
}
