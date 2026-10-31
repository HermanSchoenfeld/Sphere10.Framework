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
using System.Reflection;
using Sphere10.Framework.Mapping;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework;

/// <summary>Reconstructs ObjectStreams using owner words and the clustered store's generic metadata blob.</summary>
internal static class ObjectStreamStorageHelper {
	// Identifies ObjectStream ownership within the clustered header extension.
	internal const uint Magic = 0x53313053;

	// Owner traits indicate metadata presence and validate the supplied byte order.
	internal const uint MetaDataTrait = 1;

	internal const uint BigEndianTrait = 1 << 1;

	// Owner words occupy the eight extension bytes immediately before metadata capacity.
	internal const int OwnerMagicOffset = ClusteredStreamsHeader.MetaDataSizeOffset - ClusteredStreamsHeader.ExtensionPropertiesOffset - 2 * sizeof(uint);

	internal const int OwnerTraitsOffset = OwnerMagicOffset + sizeof(uint);

	// Reconstructs and loads an ObjectStream over borrowed raw storage at the current cursor.
	internal static bool TryOpenFromStream(Stream rawStream, Endianness endianness, out ObjectStream objects) {
		// Scan the selected store without taking ownership of the caller's raw stream.
		objects = null;
		if (rawStream == null)
			return false;
		var origin = rawStream.Position;
		if (!TryReadOwnerHeader(rawStream, endianness, Magic, MetaDataTrait | BigEndianTrait, BigEndianTrait, out var traits) || (traits & MetaDataTrait) == 0)
			return false;

		// Read configuration through the same generic metadata blob used by loaded storage owners.
		var source = origin == 0 ? rawStream : Slice(rawStream, origin);
		ObjectStreamMetaData metadata;
		using (var scan = ClusteredStreams.FromStream(source, endianness)) {
			scan.Load();
			if (!TryReadMetaData(scan, out metadata) || metadata.SerializerFactory == null)
				return false;
		}

		// Rewind after the scan and construct an ordinary unloaded ObjectStream before registering its indexes.
		rawStream.Position = origin;
		var streams = ClusteredStreams.FromStream(source, endianness);
		try {
			var factory = metadata.SerializerFactory;
			var serializer = factory.GetSerializer(metadata.ItemType);
			objects = (ObjectStream)Activator.CreateInstance(typeof(ObjectStream<>).MakeGenericType(metadata.ItemType), streams, serializer, metadata.PreAllocateOptimization);
			objects.OwnsStreams = true;
			AddIndexes(objects, metadata, factory);
			objects.Load();

			// The returned owner disposes its container and attachments; the raw stream remains borrowed.
			return true;
		} catch {
			// Release a partially constructed owner or its still-unowned container, then propagate the failure.
			if (objects != null)
				objects.Dispose();
			else
				streams.Dispose();
			throw;
		}
	}

	// Prepares a typed stream's container, factory and metadata without activating its final owner.
	internal static (ClusteredStreams Streams, SerializerFactory Factory, ObjectStreamMetaData MetaData, bool CreateMetaData) CreateStorage<T>(
		Stream rawStream, int clusterSize, ClusteredStreamsPolicy policy, long reservedStreams, Endianness endianness,
		bool preAllocateOptimization, bool storeMetaData, ObjectStreamMetaData.Index[] indexes
	) {
		Guard.ArgumentNotNull(rawStream, nameof(rawStream));
		Guard.Argument(Enum.IsDefined(endianness), nameof(endianness), "Unknown endianness.");

		// A supplied persistent view starts at logical zero. Discover metadata without loading the final owner early.
		if (rawStream.Length > 0) {
			using var position = rawStream.EnterRestorePositionScope();
			rawStream.Position = ClusteredStreamsHeader.PolicyOffset;
			Span<byte> policyBytes = stackalloc byte[sizeof(uint)];
			if (rawStream.ReadAtLeast(policyBytes, policyBytes.Length, throwOnEndOfStream: false) == policyBytes.Length
				&& ((ClusteredStreamsPolicy)EndianBitConverter.For(endianness).ToUInt32(policyBytes)).HasFlag(ClusteredStreamsPolicy.MetaData)) {
				// Flagged metadata must have a valid owner; a corrupt owner cannot select the legacy loading path.
				rawStream.Position = 0;
				if (!TryReadOwnerHeader(rawStream, endianness, Magic, MetaDataTrait | BigEndianTrait, BigEndianTrait, out var traits)
					|| (traits & MetaDataTrait) == 0)
					throw new InvalidDataException("Invalid ObjectStream owner header.");
				ObjectStreamMetaData metadata;
				using (var scan = ClusteredStreams.FromStream(rawStream, endianness)) {
					scan.Load();
					if (!TryReadMetaData(scan, out metadata) || metadata.SerializerFactory == null)
						throw new InvalidDataException("Invalid ObjectStream metadata.");
				}
				Guard.Argument(metadata.ItemType == typeof(T), nameof(rawStream), "Stored item type differs from the requested type.");
				return (ClusteredStreams.FromStream(rawStream, endianness), metadata.SerializerFactory, metadata, false);
			}
		}

		// Supplied-type construction retains the existing factory and index paths for both legacy and new stores.
		var factory = new SerializerFactory(SerializerFactory.Default);
		SerializerBuilder.FactoryAssemble(factory, typeof(T), true);
		factory.FinishRegistrations();
		var createMetaData = rawStream.Length == 0 && storeMetaData;
		var definition = new ObjectStreamMetaData {
			ItemType = typeof(T),
			ItemTypeCode = factory.FindRegistration(typeof(T)).TypeCode,
			PreAllocateOptimization = preAllocateOptimization,
			// The deferred blob must describe the same rows used to construct the attachments.
			Indexes = indexes?.Select(index => index == null ? null : new ObjectStreamMetaData.Index {
				Kind = index.Kind,
				Name = index.Name,
				MemberName = index.MemberName,
				NullPolicy = index.NullPolicy,
				BTreeSpec = index.BTreeSpec,
				HashFunction = index.HashFunction
			}).ToArray() ?? Array.Empty<ObjectStreamMetaData.Index>(),
			SerializerFactory = createMetaData ? factory : null
		};
		return (new ClusteredStreams(rawStream, clusterSize, policy, reservedStreams, endianness), factory, definition, createMetaData);
	}

	// Registers recorded indexes and configures metadata when creating a new stream.
	internal static void InitializeStorage(ObjectStream objects, SerializerFactory factory, ObjectStreamMetaData metadata, bool createMetaData, int metaDataCapacity) {
		// Validate and register attachments before the container writes any header or metadata bytes.
		Guard.ArgumentNotNegative(metaDataCapacity, nameof(metaDataCapacity));
		AddIndexes(objects, metadata, factory);
		if (!createMetaData)
			return;
		var streams = objects.Streams;
		streams.ConfigureMetaData(() => new ObjectStreamMetaDataSerializer().SerializeToBytes(metadata, streams.Endianness), metaDataCapacity);
		var traits = MetaDataTrait | (streams.Endianness == Endianness.BigEndian ? BigEndianTrait : 0);
		streams.RegisterInitAction(wasEmpty => {
			// Owner identity belongs in the existing extension area; the generic blob stores only the serialized record.
			if (!wasEmpty)
				return;
			streams.Header.WriteExtensionProperty(OwnerMagicOffset, Magic, PrimitiveSerializer<uint>.Instance);
			streams.Header.WriteExtensionProperty(OwnerTraitsOffset, traits, PrimitiveSerializer<uint>.Instance);
		});
	}

	// Reads and validates the ObjectStream record inside its allocated metadata blob.
	internal static bool TryReadMetaData(ClusteredStreams streams, out ObjectStreamMetaData metadata) {
		// The record's own length prefix describes its used bytes, independently of reserved metadata capacity.
		metadata = null;
		if (!streams.HasMetaData || streams.MetaDataCapacity < sizeof(int))
			return false;
		var bytes = streams.ReadMetaData();
		var bitConverter = EndianBitConverter.For(streams.Endianness);
		var length = bitConverter.ToInt32(bytes);
		if (length < sizeof(int) || length > streams.MetaDataCapacity)
			return false;
		using var record = new MemoryStream(bytes, 0, length, writable: false);
		using var recordReader = new EndianBinaryReader(bitConverter, record);
		metadata = new ObjectStreamMetaDataSerializer().Deserialize(recordReader);
		if (ValidateMetaData(metadata))
			return true;
		metadata = null;
		return false;
	}

	// Validates the header at the current cursor using the supplied byte order and owner identity.
	internal static bool TryReadOwnerHeader(Stream source, Endianness endianness, uint expectedMagic, uint supportedTraits, uint endianFlag, out uint traits) {
		// Read the ordinary header from the selected cursor; metadata presence is a header flag.
		Guard.Argument(Enum.IsDefined(endianness), nameof(endianness), "Unknown endianness.");
		traits = default;
		if (source == null)
			return false;
		Span<byte> bytes = stackalloc byte[ClusteredStreamsHeader.ByteLength];
		if (source.ReadAtLeast(bytes, bytes.Length, throwOnEndOfStream: false) != bytes.Length
			|| bytes[ClusteredStreamsHeader.VersionOffset] != ClusteredStreamsHeader.VersionNumber)
			return false;

		// The caller supplies byte order; the owner word and flag must agree before accepting persisted geometry.
		var converter = EndianBitConverter.For(endianness);
		var magicBytes = bytes[(ClusteredStreamsHeader.ExtensionPropertiesOffset + OwnerMagicOffset)..];
		var traitsBytes = bytes[(ClusteredStreamsHeader.ExtensionPropertiesOffset + OwnerTraitsOffset)..];
		if (converter.ToUInt32(magicBytes) != expectedMagic)
			return false;
		traits = converter.ToUInt32(traitsBytes);
		if ((traits & ~supportedTraits) != 0 || ((traits & endianFlag) != 0) != (endianness == Endianness.BigEndian))
			return false;

		// Validate the existing header fields before using them to open a configured container.
		var policy = (ClusteredStreamsPolicy)converter.ToUInt32(bytes[ClusteredStreamsHeader.PolicyOffset..]);
		if (!policy.HasFlag(ClusteredStreamsPolicy.MetaData)
			|| converter.ToInt32(bytes[ClusteredStreamsHeader.ClusterSizeOffset..]) <= 0
			|| converter.ToInt64(bytes[ClusteredStreamsHeader.ReservedStreamsOffset..]) < 0
			|| converter.ToInt64(bytes[ClusteredStreamsHeader.TotalClustersOffset..]) < 0
			|| converter.ToInt64(bytes[ClusteredStreamsHeader.StreamCountOffset..]) < 0
			|| converter.ToInt64(bytes[ClusteredStreamsHeader.StreamDescriptorsEndClusterOffset..]) < Cluster.Null)
			return false;

		// Allocation size belongs to the fixed header; the following bytes begin with the opaque record.
		var capacity = converter.ToInt32(bytes[ClusteredStreamsHeader.MetaDataSizeOffset..]);
		return capacity >= 0 && capacity <= source.Length - source.Position;
	}

	// Recreates attachments in their recorded physical order using the saved factory.
	private static void AddIndexes(ObjectStream objects, ObjectStreamMetaData metadata, SerializerFactory factory) {
		// Validate the complete description before registering any attachments on the new container.
		if (!ValidateMetaData(metadata))
			throw new InvalidDataException("Invalid ObjectStream index metadata.");
		if (!factory.TryFindRegistration(metadata.ItemType, out var registration, out _) || registration.TypeCode != metadata.ItemTypeCode)
			throw new InvalidDataException("The stored item type code differs from its serializer factory registration.");
		foreach (var index in metadata.Indexes) {
			IClusteredStreamsAttachment attachment;
			if (index.Kind == ObjectSpaceDefinition.IndexType.RecyclableIndexStore)
				attachment = new RecyclableIndexIndex(objects, index.Name);
			else if (index.Kind == ObjectSpaceDefinition.IndexType.MerkleTree)
				attachment = new MerkleTreeIndex(objects, index.Name, new ObjectStreamItemHasher(objects, index.HashFunction), index.HashFunction);
			else {
				var info = metadata.ItemType.GetMember(index.MemberName, BindingFlags.Public | BindingFlags.Instance).SingleOrDefault()
					?? throw new InvalidDataException($"The indexed member {index.MemberName} is missing.");
				var member = info.ToMember();
				var keySerializer = factory.GetSerializer(member.PropertyType);
				var keyComparer = ComparerFactory.Default.GetEqualityComparer(member.PropertyType);
				var unique = index.Kind is ObjectSpaceDefinition.IndexType.Identifier or ObjectSpaceDefinition.IndexType.UniqueKey;
				var spec = index.BTreeSpec;

				// Variable keys use the existing checksum attachment; every tree receives its stored complete specification.
				if (keySerializer.IsConstantSize)
					attachment = unique ? IndexFactory.CreateUniqueMemberIndex(objects, index.Name, member, keySerializer, keyComparer, spec)
						: IndexFactory.CreateMemberIndex(objects, index.Name, member, keySerializer, keyComparer, spec);
				else
					attachment = unique ? IndexFactory.CreateUniqueMemberChecksumIndex(objects, index.Name, member, keySerializer, null, null, keyComparer, index.NullPolicy, spec)
						: IndexFactory.CreateMemberChecksumIndex(objects, index.Name, member, keySerializer, null, null, keyComparer, index.NullPolicy, spec);
			}
			objects.Streams.RegisterAttachment(attachment);
		}
	}

	// Checks the item registration, index row shapes and indexed member identities.
	private static bool ValidateMetaData(ObjectStreamMetaData metadata) {
		// Dimension records use their ObjectSpace factory; standalone records validate the same exact code here.
		if (metadata == null || metadata.ItemType == null || metadata.ItemType.ContainsGenericParameters || metadata.Indexes == null
			|| metadata.SerializerFactory != null && (!metadata.SerializerFactory.TryFindRegistration(metadata.ItemType, out var registration, out _)
				|| registration.TypeCode != metadata.ItemTypeCode))
			return false;

		// Validate index row shape and uniqueness independently of the serialized member resolution below.
		if (metadata.Indexes.Any(index => index == null || !Enum.IsDefined(index.Kind) || !Enum.IsDefined(index.NullPolicy) || string.IsNullOrEmpty(index.Name)
			|| (index.Kind is ObjectSpaceDefinition.IndexType.Identifier or ObjectSpaceDefinition.IndexType.UniqueKey or ObjectSpaceDefinition.IndexType.Index
				? index.BTreeSpec == null || string.IsNullOrEmpty(index.MemberName)
				: index.BTreeSpec != null)
			|| index.Kind == ObjectSpaceDefinition.IndexType.MerkleTree && !Enum.IsDefined(index.HashFunction))
			|| metadata.Indexes.Select(index => index.Name).Distinct(StringComparer.Ordinal).Count() != metadata.Indexes.Length)
			return false;

		// A recorded member name must identify one serializable field or non-indexed property on the item type.
		foreach (var index in metadata.Indexes.Where(index => index.Kind is ObjectSpaceDefinition.IndexType.Identifier or ObjectSpaceDefinition.IndexType.UniqueKey or ObjectSpaceDefinition.IndexType.Index)) {
			var members = metadata.ItemType.GetMember(index.MemberName, BindingFlags.Public | BindingFlags.Instance);
			if (members.Length != 1 || members[0] is not PropertyInfo and not FieldInfo
				|| members[0] is PropertyInfo property && property.GetIndexParameters().Length != 0)
				return false;
			var member = members[0].ToMember();
			if (!member.CanRead)
				return false;
		}
		return true;
	}

	// Creates a borrowed relative stream view beginning at the selected store origin.
	internal static Stream Slice(Stream stream, long offset) {
		return stream.AsBounded(offset, long.MaxValue - offset, allowResize: true, useRelativeOffset: true);
	}
}
