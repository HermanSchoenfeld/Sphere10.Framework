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

/// <summary>Verifies the shared header and blob layout used by self-describing storage owners.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class EmbeddedStorageFormatTests {
	/// <summary>Verifies owner words, blob framing and dimension records within the existing clustered-storage layout.</summary>
	[Test]
	public void OwnerMetadataOccupiesTheGenericBlobBeforeClusters(
		[Values] Endianness endianness,
		[Values] bool merkleized,
		[Values] bool reservePadding) {
		const int reservedCapacity = 64 * 1024;
		using var raw = new MemoryStream();
		var builder = new ObjectSpaceBuilder()
			.StoreMetaData(reservePadding ? reservedCapacity : 0)
			.UsingHashFunction(CHF.SHA2_256)
			.WithClusterSize(32)
			.WithClusteredStreamsPolicy(ClusteredStreamsPolicy.Performance)
			.WithEndianness(endianness)
			.UseMemoryStream(raw)
			.AddDimension<long>(true)
				.Done();
		if (merkleized)
			builder.Merkleized();
		using var space = builder.Build();
		space.Flush();

		// The generic header owns the blob framing; object metadata consumes no reserved stream.
		var streams = space.Streams;
		var reserved = merkleized ? 1 : 0;
		Assert.That(streams.Header.Version, Is.EqualTo(ClusteredStreamsHeader.VersionNumber));
		Assert.That(streams.Header.Policy.HasFlag(ClusteredStreamsPolicy.MetaData), Is.True);
		Assert.That(streams.HasMetaData, Is.True);
		Assert.That(streams.Header.ReservedStreams, Is.EqualTo(reserved));
		Assert.That(streams.Count, Is.EqualTo(reserved + 1));
		var metadata = new ObjectSpaceMetaDataSerializer().SerializeToBytes(new ObjectSpaceMetaData {
			HashFunction = CHF.SHA2_256,
			SerializerFactory = space.Serializers
		}, endianness);
		var capacity = reservePadding ? reservedCapacity : metadata.Length;
		Assert.That(streams.MetaDataCapacity, Is.EqualTo(capacity));
		Assert.That(streams.DataOffset, Is.EqualTo(ClusteredStreamsHeader.MetaDataOffset + capacity));
		Assert.That(raw.Length, Is.EqualTo(streams.DataOffset + streams.Header.TotalClusters * (32 + ClusterSerializer.DataOffset)));

		// Owner words precede the header's metadata-size DWORD in the clustered store's byte order.
		var bytes = raw.ToArray();
		var converter = EndianBitConverter.For(endianness);
		Assert.That(converter.ToUInt32(bytes, ClusteredStreamsHeader.MetaDataSizeOffset - 2 * sizeof(uint)), Is.EqualTo(ObjectSpaceStorageHelper.Magic));
		Assert.That(converter.ToUInt32(bytes, ClusteredStreamsHeader.MetaDataSizeOffset - sizeof(uint)), Is.EqualTo((uint)space.Traits));
		Assert.That(space.Traits.HasFlag(ObjectSpaceTraits.BigEndian), Is.EqualTo(endianness == Endianness.BigEndian));
		Assert.That(converter.ToInt32(bytes, ClusteredStreamsHeader.MetaDataSizeOffset), Is.EqualTo(capacity));
		Assert.That(streams.Header.MetaDataSize, Is.EqualTo(capacity));

		// Allocated capacity and the record's used length are distinct; unused capacity is zero-filled.
		Assert.That(converter.ToInt32(bytes, ClusteredStreamsHeader.MetaDataOffset), Is.EqualTo(metadata.Length));
		Assert.That(bytes.AsSpan(ClusteredStreamsHeader.MetaDataOffset, metadata.Length).ToArray(), Is.EqualTo(metadata));
		Assert.That(bytes.AsSpan(ClusteredStreamsHeader.MetaDataOffset + metadata.Length, capacity - metadata.Length).ToArray().All(value => value == 0), Is.True);
		var stored = new ObjectSpaceMetaDataSerializer().DeserializeBytes(streams.ReadMetaData(), endianness);
		Assert.That(stored.HashFunction, Is.EqualTo(CHF.SHA2_256));
		Assert.That(stored.SerializerFactory.FindRegistration(typeof(long)).TypeCode, Is.EqualTo(space.Serializers.FindRegistration(typeof(long)).TypeCode));

		// Each dimension references the root factory instead of copying it into its own record.
		var childStreams = ((ObjectSpace.Dimension<long>)space.Dimensions[0]).Container.ObjectStream.Streams;
		var dimension = new ObjectStreamMetaDataSerializer().DeserializeBytes(childStreams.ReadMetaData(), endianness);
		Assert.That(dimension.ItemType, Is.EqualTo(typeof(long)));
		Assert.That(dimension.ItemTypeCode, Is.EqualTo(stored.SerializerFactory.FindRegistration(typeof(long)).TypeCode));
		Assert.That(dimension.SerializerFactory, Is.Null);
		Assert.That(raw.CanRead && raw.CanWrite, Is.True);
	}

	/// <summary>Verifies that negative creation budgets fail before changing or closing borrowed owner storage.</summary>
	[Test]
	public void NegativeCreationCapacityLeavesBorrowedStorageUntouched([Values] bool objectSpace) {
		// An invalid budget must not create a header or alter the caller's selected cursor.
		using var raw = new MemoryStream();
		raw.Position = 7;
		Assert.That(() => {
			if (objectSpace) {
				using var space = new ObjectSpaceBuilder()
					.StoreMetaData(-1)
					.UseMemoryStream(raw)
					.AddDimension<long>(true)
						.Done()
					.Build();
			} else {
				using var objects = new ObjectStream<long>(raw, storeMetaData: true, metaDataCapacity: -1);
			}
		}, Throws.TypeOf<ArgumentOutOfRangeException>());
		Assert.That(raw.Length, Is.Zero);
		Assert.That(raw.Position, Is.EqualTo(7));
		Assert.That(raw.CanRead && raw.CanWrite, Is.True);
	}

	/// <summary>Verifies that invalid allocation framing returns false without changing or closing borrowed storage.</summary>
	[Test]
	public void InvalidMetadataCapacityFailsWithoutChangingBorrowedStorage(
		[Values] Endianness endianness,
		[Values] bool objectSpace,
		[Values("truncated", "negative", "tooLarge")] string defect) {
		// Begin with real owner bytes so only the header's capacity field is malformed.
		using var original = new MemoryStream();
		if (objectSpace) {
			using var space = new ObjectSpaceBuilder()
				.StoreMetaData()
				.WithEndianness(endianness)
				.UseMemoryStream(original)
				.AddDimension<long>(true)
					.Done()
				.Build();
			space.Save(17L);
		} else {
			using var objects = new ObjectStream<long>(original, endianness: endianness, storeMetaData: true);
			objects.Load();
			objects.SaveItem(0, 17L, ObjectStreamOperationType.Add);
		}
		var bytes = original.ToArray();
		if (defect == "truncated")
			bytes = bytes.AsSpan(0, ClusteredStreamsHeader.MetaDataSizeOffset + sizeof(short)).ToArray();
		else
			EndianBitConverter.For(endianness).WriteTo(defect == "negative" ? -1 : int.MaxValue, bytes, ClusteredStreamsHeader.MetaDataSizeOffset);

		// Invalid framing returns false before constructing an owner or rewriting any input bytes.
		using var borrowed = new MemoryStream(bytes);
		if (objectSpace) {
			Assert.That(ObjectSpace.TryOpenFromStream(borrowed, endianness, out var opened), Is.False);
			Assert.That(opened, Is.Null);
		} else {
			Assert.That(ObjectStream.TryOpenFromStream(borrowed, endianness, out var opened), Is.False);
			Assert.That(opened, Is.Null);
		}
		Assert.That(borrowed.ToArray(), Is.EqualTo(bytes));
		Assert.That(borrowed.CanRead && borrowed.CanWrite, Is.True);
	}

	/// <summary>Verifies rejection when the supplied byte order and stored owner words disagree.</summary>
	[Test]
	public void MetadataOpeningRequiresTheSuppliedEndiannessToMatchTheOwner(
		[Values] Endianness endianness,
		[Values] bool objectSpace,
		[Values] bool corruptEndianTrait) {
		// Create valid owner bytes using the same explicit byte order throughout the store.
		using var original = new MemoryStream();
		if (objectSpace) {
			using var space = new ObjectSpaceBuilder()
				.StoreMetaData()
				.WithEndianness(endianness)
				.UseMemoryStream(original)
				.AddDimension<long>(true)
					.Done()
				.Build();
			space.Save(17L);
		} else {
			using var objects = new ObjectStream<long>(original, endianness: endianness, storeMetaData: true);
			objects.Load();
			objects.SaveItem(0, 17L, ObjectStreamOperationType.Add);
		}
		var bytes = original.ToArray();

		// Either the supplied order or the owner flag disagrees; neither case permits automatic byte-order discovery.
		var suppliedEndianness = endianness == Endianness.LittleEndian ? Endianness.BigEndian : Endianness.LittleEndian;
		if (corruptEndianTrait) {
			var converter = EndianBitConverter.For(endianness);
			var offset = ClusteredStreamsHeader.MetaDataSizeOffset - sizeof(uint);
			var endianFlag = objectSpace ? (uint)ObjectSpaceTraits.BigEndian : ObjectStreamStorageHelper.BigEndianTrait;
			converter.WriteTo(converter.ToUInt32(bytes, offset) ^ endianFlag, bytes, offset);
			suppliedEndianness = endianness;
		}
		var expected = (byte[])bytes.Clone();
		using var borrowed = new MemoryStream(bytes);

		// Rejection leaves the borrowed store open and byte-for-byte unchanged.
		if (objectSpace) {
			Assert.That(ObjectSpace.TryOpenFromStream(borrowed, suppliedEndianness, out var opened), Is.False);
			Assert.That(opened, Is.Null);
		} else {
			Assert.That(ObjectStream.TryOpenFromStream(borrowed, suppliedEndianness, out var opened), Is.False);
			Assert.That(opened, Is.Null);
		}
		Assert.That(borrowed.CanRead && borrowed.CanWrite, Is.True);
		Assert.That(borrowed.ToArray(), Is.EqualTo(expected));
	}

	/// <summary>Verifies that stores without metadata retain their original bytes and complete extension area.</summary>
	[Test]
	public void LegacyStorageKeepsItsBytesAndFullExtensionArea([Values] Endianness endianness) {
		using var raw = new MemoryStream();
		using (var streams = new ClusteredStreams(raw, 32, endianness: endianness, autoLoad: true)) {
			using (streams.EnterAccessScope())
				streams.Header.MapExtensionProperty(ClusteredStreamsHeader.ByteLength - 1 - ClusteredStreamsHeader.ExtensionPropertiesOffset, 1, PrimitiveSerializer<byte>.Instance).Value = 0xEE;
			streams.AddBytes(new byte[] { 41, 42 });
		}
		var original = raw.ToArray();
		using var reopened = ClusteredStreams.FromStream(raw, endianness, autoLoad: true);
		Assert.That(reopened.Header.Version, Is.EqualTo(ClusteredStreamsHeader.VersionNumber));
		Assert.That(reopened.Header.Policy.HasFlag(ClusteredStreamsPolicy.MetaData), Is.False);
		Assert.That(reopened.HasMetaData, Is.False);
		Assert.That(reopened.MetaDataCapacity, Is.Zero);
		Assert.That(reopened.DataOffset, Is.EqualTo(ClusteredStreamsHeader.ByteLength));
		Assert.That(raw.ToArray(), Is.EqualTo(original));
	}
}
