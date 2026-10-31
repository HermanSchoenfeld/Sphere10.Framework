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
using System.Reflection;
using Sphere10.Framework.Mapping;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework;

/// <summary>Reconstructs ObjectSpaces using the clustered header's owner properties and generic metadata blob.</summary>
internal static class ObjectSpaceStorageHelper {
	// Identifies ObjectSpace ownership within the clustered header extension.
	internal const uint Magic = 0x5331304F;

	private const ObjectSpaceTraits SupportedTraits = ObjectSpaceTraits.Merkleized | ObjectSpaceTraits.AutoSave
		| ObjectSpaceTraits.GarbageCollect | ObjectSpaceTraits.MetaData | ObjectSpaceTraits.BigEndian;

	// Rebuilds the schema at the current cursor and loads an ObjectSpace over borrowed raw storage.
	internal static bool TryOpenFromStream(Stream rawStream, Endianness endianness, out ObjectSpace objectSpace) {
		// Scan the selected store without taking ownership of the caller's raw stream.
		objectSpace = null;
		if (rawStream == null)
			return false;
		var origin = rawStream.Position;
		if (!ObjectStreamStorageHelper.TryReadOwnerHeader(rawStream, endianness, Magic, (uint)SupportedTraits,
			(uint)ObjectSpaceTraits.BigEndian, out var storedTraits))
			return false;
		var traits = (ObjectSpaceTraits)storedTraits;
		if (!traits.HasFlag(ObjectSpaceTraits.MetaData))
			return false;

		// The existing descriptor table locates every child stream; metadata itself is not a reserved stream.
		var source = origin == 0 ? rawStream : ObjectStreamStorageHelper.Slice(rawStream, origin);
		var dimensions = new List<ObjectSpaceDefinition.DimensionDefinition>();
		ObjectSpaceMetaData metadata;
		SerializerFactory serializers;
		int clusterSize;
		int capacity;
		ClusteredStreamsPolicy policy;
		using (var scan = ClusteredStreams.FromStream(source, endianness)) {
			scan.Load();
			if (!TryReadMetaData(scan, out metadata)
				|| scan.Header.ReservedStreams != (traits.HasFlag(ObjectSpaceTraits.Merkleized) ? 1 : 0))
				return false;
			serializers = metadata.SerializerFactory;
			clusterSize = scan.Header.ClusterSize;
			capacity = scan.MetaDataCapacity;
			policy = scan.Header.Policy;
			for (var index = scan.Header.ReservedStreams; index < scan.Header.StreamCount; index++) {
				// A normal child view permits the existing mapped-header reader/writer pair to be constructed.
				using var child = scan.OpenWrite(index);
				if (!ObjectStreamStorageHelper.TryReadOwnerHeader(child, endianness, ObjectStreamStorageHelper.Magic,
					ObjectStreamStorageHelper.MetaDataTrait | ObjectStreamStorageHelper.BigEndianTrait,
					ObjectStreamStorageHelper.BigEndianTrait, out var childTraits)
					|| (childTraits & ObjectStreamStorageHelper.MetaDataTrait) == 0)
					return false;
				child.Position = 0;
				using var childStorage = ClusteredStreams.FromStream(child, endianness);
				childStorage.Load();
				if (!ObjectStreamStorageHelper.TryReadMetaData(childStorage, out var record)
					|| record.PreAllocateOptimization || (childStorage.Header.Policy & ~ClusteredStreamsPolicy.MetaData) != ClusteredStreamsPolicy.FastAllocate
					|| record.SerializerFactory != null
					|| !serializers.TryFindRegistration(record.ItemType, out var itemRegistration, out _)
					|| itemRegistration.TypeCode != record.ItemTypeCode)
					return false;

				// Restore physical index order, including each complete tree specification.
				var indexes = new ObjectSpaceDefinition.IndexDefinition[record.Indexes.Length];
				for (var rowIndex = 0; rowIndex < indexes.Length; rowIndex++) {
					var row = record.Indexes[rowIndex];
					Member member = null;
					if (row.MemberName != null && !TryResolveStoredMember(record.ItemType, row.MemberName, out member))
						return false;
					if (row.Kind == ObjectSpaceDefinition.IndexType.MerkleTree && row.HashFunction != metadata.HashFunction)
						return false;
					indexes[rowIndex] = new ObjectSpaceDefinition.IndexDefinition {
						Type = row.Kind,
						Name = row.Name,
						Member = member,
						NullPolicy = row.NullPolicy,
						BTreeSpec = row.BTreeSpec
					};
				}

				// Dirty tracking is construction state, represented by its mapped boolean member.
				var tracker = ObjectChangeTracker.Default;
				if (record.ChangeTrackingMemberName != null) {
					if (!TryResolveStoredMember(record.ItemType, record.ChangeTrackingMemberName, out var trackingMember)
						|| trackingMember is not PropertyMember property || !property.CanRead || !property.CanWrite || property.PropertyType != typeof(bool))
						return false;
					tracker = new ObjectChangeTracker(property);
				}
				dimensions.Add(new ObjectSpaceDefinition.DimensionDefinition {
					ObjectType = record.ItemType,
					IsRoot = record.IsRoot,
					AverageObjectSizeBytes = childStorage.Header.ClusterSize,
					ChangeTracker = tracker,
					Indexes = indexes
				});
			}
		}

		// The root's saved factory is authoritative; child records identify their type and its original code.
		if (new ObjectSpaceDefinition {
			Traits = traits,
			Dimensions = dimensions.ToArray()
		}.Validate().IsFailure)
			return false;
		foreach (var dimension in dimensions)
			if (!serializers.ContainsSerializer(dimension.ObjectType)
				|| dimension.Indexes.Where(index => index.Member != null).Any(index => !serializers.ContainsSerializer(index.Member.PropertyType)))
				return false;

		// The scan supplies ordinary builder inputs; it adds no interpretation or discovery behavior to the builder.
		var builder = new ObjectSpaceBuilder()
			.StoreMetaData(capacity)
			.UsingSerializerFactory(serializers)
			.UsingHashFunction(metadata.HashFunction)
			.WithClusterSize(clusterSize)
			.WithClusteredStreamsPolicy(policy)
			.WithEndianness(endianness);
		if (traits.HasFlag(ObjectSpaceTraits.AutoSave))
			builder.AutoSave();
		if (traits.HasFlag(ObjectSpaceTraits.GarbageCollect))
			builder.WithGarbageCollection();
		if (traits.HasFlag(ObjectSpaceTraits.Merkleized))
			builder.Merkleized();
		foreach (var dimension in dimensions) {
			var dimensionBuilder = builder.AddDimension(dimension.ObjectType, ignoreAnnotations: true)
				.WithIndexes(dimension.Indexes)
				.OptimizeAssumingAverageItemSize(dimension.AverageObjectSizeBytes.Value);
			if (dimension.IsRoot)
				dimensionBuilder.AsRoot();
			if (dimension.ChangeTracker.Member != null)
				dimensionBuilder.WithChangeTrackingVia(dimension.ChangeTracker.Member);
			dimensionBuilder.Done();
		}

		// Reset after scanning and use the existing, supplied-definition loading path over the same store.
		rawStream.Position = origin;
		var streams = new ClusteredStreams(source, clusterSize, policy,
			traits.HasFlag(ObjectSpaceTraits.Merkleized) ? 1 : 0, endianness);
		try {
			objectSpace = builder.Build(streams);
			ConfigureStorage(streams, objectSpace.Definition, objectSpace.Serializers, objectSpace.Comparers);
			objectSpace.Load();
			return true;
		} catch {
			// Partial dimensions belong to the owner; failure cleanup must not flush incomplete storage.
			if (objectSpace != null) {
				objectSpace.FlushOnDispose = false;
				objectSpace.Dispose();
			} else {
				streams.Dispose();
			}
			throw;
		}
	}

	// Creates deferred transactional storage and configures its ObjectSpace metadata.
	internal static ClusteredStreams CreateFileStorage(Sphere10FrameworkFileDescriptor file, ObjectSpaceDefinition definition, SerializerFactory serializers, ComparerFactory comparers,
		FileAccessMode accessMode, out TransactionalStream fileStream) {
		// The existing transactional stream remains deferred and belongs to one permanent clustered container.
		Guard.ArgumentNotNull(file, nameof(file));
		Guard.ArgumentNotNull(definition, nameof(definition));
		Guard.ArgumentNotNull(serializers, nameof(serializers));
		Guard.ArgumentNotNull(comparers, nameof(comparers));
		definition.Validate().ThrowOnFailure();
		definition.ResolveIndexBTreeSpecs();
		if (definition.Traits.HasFlag(ObjectSpaceTraits.MetaData))
			ValidateComparers(comparers);
		fileStream = null;
		var rawStream = new TransactionalStream(file, accessMode.WithoutAutoLoad());
		try {
			var streams = new ClusteredStreams(rawStream, (int)file.ClusterSize, file.ContainerPolicy,
				definition.Traits.HasFlag(ObjectSpaceTraits.Merkleized) ? 1 : 0, file.Endianness) {
				OwnsStream = true
			};
			ConfigureStorage(streams, definition, serializers, comparers);
			fileStream = rawStream;
			return streams;
		} catch {
			Tools.Exceptions.ExecuteIgnoringException(rawStream.Dispose);
			throw;
		}
	}

	// Registers creation metadata and validates recorded ObjectSpace settings during activation.
	internal static void ConfigureStorage(ClusteredStreams streams, ObjectSpaceDefinition definition, SerializerFactory serializers, ComparerFactory comparers) {
		// Validate actual comparer registrations before configuring storage; applications may pass an equivalent copy of the default factory.
		Guard.ArgumentNotNull(comparers, nameof(comparers));
		if (definition.Traits.HasFlag(ObjectSpaceTraits.MetaData))
			ValidateComparers(comparers);

		// Prepare the immutable creation record at activation, after all ordinary factory registrations exist.
		var creationTraits = ObjectSpaceTraits.None;
		if (definition.Traits.HasFlag(ObjectSpaceTraits.MetaData))
			streams.ConfigureMetaData(() => {
				definition.Validate().ThrowOnFailure();
				ValidateComparers(comparers);
				creationTraits = WithEndianness(definition.Traits, streams.Endianness);
				if ((creationTraits & ~SupportedTraits) != 0)
					throw new InvalidDataException("Unknown ObjectSpace traits.");
				var dimensionSerializer = new ObjectStreamMetaDataSerializer();
				foreach (var dimension in definition.Dimensions)
					dimensionSerializer.SerializeToBytes(CreateDimensionMetaData(dimension, definition, serializers), streams.Endianness);
				return new ObjectSpaceMetaDataSerializer().SerializeToBytes(new ObjectSpaceMetaData {
					HashFunction = definition.HashFunction,
					SerializerFactory = serializers
				}, streams.Endianness);
			}, definition.MetaDataCapacity);

		streams.RegisterInitAction(wasEmpty => {
			// Legacy storage keeps its original header and layout; metadata creation never migrates it implicitly.
			if (!streams.HasMetaData) {
				definition.Traits = WithEndianness(definition.Traits & ~ObjectSpaceTraits.MetaData, streams.Endianness);
				definition.MetaDataCapacity = 0;
				return;
			}

			// Owner words use the header extension area, away from the Merkle root at offset zero.
			if (wasEmpty) {
				streams.Header.WriteExtensionProperty(ObjectStreamStorageHelper.OwnerMagicOffset, Magic, PrimitiveSerializer<uint>.Instance);
				streams.Header.WriteExtensionProperty(ObjectStreamStorageHelper.OwnerTraitsOffset, (uint)creationTraits, PrimitiveSerializer<uint>.Instance);
			} else if (streams.Header.ReadExtensionProperty(ObjectStreamStorageHelper.OwnerMagicOffset, PrimitiveSerializer<uint>.Instance) != Magic) {
				throw new InvalidDataException("Invalid ObjectSpace owner header.");
			}
			var stored = (ObjectSpaceTraits)streams.Header.ReadExtensionProperty(ObjectStreamStorageHelper.OwnerTraitsOffset, PrimitiveSerializer<uint>.Instance);
			if ((stored & ~SupportedTraits) != 0 || !stored.HasFlag(ObjectSpaceTraits.MetaData)
				|| WithEndianness(stored, streams.Endianness) != stored)
				throw new InvalidDataException("Invalid ObjectSpace traits.");
			ValidateComparers(comparers);
			if ((WithEndianness(definition.Traits, streams.Endianness) & ~ObjectSpaceTraits.MetaData) != (stored & ~ObjectSpaceTraits.MetaData))
				throw new InvalidDataException("The supplied ObjectSpace traits differ from the stored header.");
			if (!TryReadMetaData(streams, out var metadata) || metadata.HashFunction != definition.HashFunction)
				throw new InvalidDataException("The supplied ObjectSpace hash function differs from its stored metadata.");
			if (!wasEmpty && !FactoriesMatch(metadata.SerializerFactory, serializers, streams.Endianness))
				throw new InvalidDataException("The supplied serializer factory differs from its stored metadata.");
			definition.Traits = stored;
			definition.MetaDataCapacity = streams.MetaDataCapacity;
		});
	}

	// Creates a dimension container while preserving its recorded metadata and cluster geometry.
	internal static ClusteredStreams CreateDimensionStorage(Stream dimensionStream, ObjectSpaceDefinition.DimensionDefinition dimensionDefinition,
		ObjectSpaceDefinition definition, SerializerFactory serializers, int dimensionClusterSize, Endianness endianness) {
		// Ordinary persisted geometry remains authoritative, including metadata-adjusted cluster offsets.
		if (dimensionStream.Length > 0) {
			// Validate recorded layout before constructing attachments that would otherwise need partial cleanup.
			using var position = dimensionStream.EnterRestorePositionScope();
			dimensionStream.Position = ClusteredStreamsHeader.PolicyOffset;
			Span<byte> policyBytes = stackalloc byte[sizeof(uint)];
			dimensionStream.ReadExactly(policyBytes);
			var storedPolicy = (ClusteredStreamsPolicy)EndianBitConverter.For(endianness).ToUInt32(policyBytes);
			var hasGenericMetaData = storedPolicy.HasFlag(ClusteredStreamsPolicy.MetaData);
			if (hasGenericMetaData) {
				dimensionStream.Position = 0;
				if (!ObjectStreamStorageHelper.TryReadOwnerHeader(dimensionStream, endianness, ObjectStreamStorageHelper.Magic,
					ObjectStreamStorageHelper.MetaDataTrait | ObjectStreamStorageHelper.BigEndianTrait,
					ObjectStreamStorageHelper.BigEndianTrait, out var traits)
					|| (traits & ObjectStreamStorageHelper.MetaDataTrait) == 0)
					throw new InvalidDataException("Invalid dimension ObjectStream header.");
				dimensionStream.Position = 0;
				using var scan = ClusteredStreams.FromStream(dimensionStream, endianness);
				scan.Load();
				if (!ObjectStreamStorageHelper.TryReadMetaData(scan, out var metadata) || metadata.ItemType != dimensionDefinition.ObjectType)
					throw new InvalidDataException("Invalid dimension metadata.");
				var expected = CreateDimensionMetaData(dimensionDefinition, definition, serializers);
				var metadataSerializer = new ObjectStreamMetaDataSerializer();
				if (!metadataSerializer.SerializeToBytes(metadata, endianness)
					.SequenceEqual(metadataSerializer.SerializeToBytes(expected, endianness)))
					throw new InvalidDataException("The supplied dimension configuration differs from its stored metadata.");
			} else if (definition.Traits.HasFlag(ObjectSpaceTraits.MetaData)) {
				throw new InvalidDataException("The ObjectSpace metadata trait requires metadata on every dimension stream.");
			}
			var storedClusterSize = ClusteredStreams.ReadStoredClusterSize(dimensionStream, endianness);
			if (dimensionDefinition.AverageObjectSizeBytes.HasValue)
				Guard.Ensure(storedClusterSize == dimensionClusterSize, "The explicitly configured dimension cluster size must match existing storage.");
			else
				dimensionClusterSize = storedClusterSize;
		}
		var streams = new ClusteredStreams(dimensionStream, dimensionClusterSize, ClusteredStreamsPolicy.FastAllocate,
			dimensionDefinition.Indexes.Length, endianness, false) {
			OwnsStream = true
		};
		if (definition.Traits.HasFlag(ObjectSpaceTraits.MetaData))
			streams.ConfigureMetaData(() => new ObjectStreamMetaDataSerializer().SerializeToBytes(CreateDimensionMetaData(dimensionDefinition, definition, serializers), endianness));

		streams.RegisterInitAction(wasEmpty => {
			if (!streams.HasMetaData) {
				if (definition.Traits.HasFlag(ObjectSpaceTraits.MetaData))
					throw new InvalidDataException("The ObjectSpace metadata trait requires metadata on every dimension stream.");
				return;
			}

			// Each child owns its type, index and tracking record; the factory is stored once at the ObjectSpace.
			var expectedTraits = ObjectStreamStorageHelper.MetaDataTrait | (endianness == Endianness.BigEndian ? ObjectStreamStorageHelper.BigEndianTrait : 0);
			if (wasEmpty) {
				streams.Header.WriteExtensionProperty(ObjectStreamStorageHelper.OwnerMagicOffset, ObjectStreamStorageHelper.Magic, PrimitiveSerializer<uint>.Instance);
				streams.Header.WriteExtensionProperty(ObjectStreamStorageHelper.OwnerTraitsOffset, expectedTraits, PrimitiveSerializer<uint>.Instance);
			} else if (streams.Header.ReadExtensionProperty(ObjectStreamStorageHelper.OwnerMagicOffset, PrimitiveSerializer<uint>.Instance) != ObjectStreamStorageHelper.Magic
				|| streams.Header.ReadExtensionProperty(ObjectStreamStorageHelper.OwnerTraitsOffset, PrimitiveSerializer<uint>.Instance) != expectedTraits) {
				throw new InvalidDataException("Invalid dimension ObjectStream header.");
			}
		});
		return streams;
	}

	// Checks that an existing dimension header has compatible reserved-stream geometry.
	internal static void ValidateDimensionStorage(Stream dimensionStream, long reservedStreams, Endianness endianness) {
		// Composite indexes register child stores, so their reserved count can exceed the schema row count.
		if (dimensionStream.Length > 0)
			ClusteredStreams.ReadStoredClusterSize(dimensionStream, reservedStreams, endianness);
	}

	// Captures a dimension's type code, indexes and supported dirty-tracking member.
	internal static ObjectStreamMetaData CreateDimensionMetaData(ObjectSpaceDefinition.DimensionDefinition dimension, ObjectSpaceDefinition definition, SerializerFactory serializers) {
		// Member-based dirty tracking can be reconstructed without persisting an executable callback.
		if (dimension.ChangeTracker.SupportsChangeTracking && dimension.ChangeTracker.Member == null)
			throw new NotSupportedException("Metadata for change tracking requires a mapped boolean property. Omit StoreMetaData for delegates.");
		foreach (var member in dimension.Indexes.Select(index => index.Member).Append(dimension.ChangeTracker.Member).Where(member => member != null))
			if (!TryResolveStoredMember(dimension.ObjectType, member.Name, out var storedMember) || !storedMember.MemberInfo.Equals(member.MemberInfo))
				throw new NotSupportedException($"The member '{member.Name}' cannot be identified unambiguously by its stored name.");

		// A dimension identifies its item registration; the ObjectSpace owns the complete factory record.
		return new ObjectStreamMetaData {
			ItemType = dimension.ObjectType,
			ItemTypeCode = serializers.FindRegistration(dimension.ObjectType).TypeCode,
			IsRoot = dimension.IsRoot,
			ChangeTrackingMemberName = dimension.ChangeTracker.Member?.Name,
			Indexes = dimension.Indexes.Select(index => new ObjectStreamMetaData.Index {
				Kind = index.Type,
				Name = index.Name,
				MemberName = index.Member?.Name,
				NullPolicy = index.NullPolicy,
				BTreeSpec = index.BTreeSpec,
				HashFunction = index.Type == ObjectSpaceDefinition.IndexType.MerkleTree ? definition.HashFunction : default
			}).ToArray()
		};
	}

	// Reads and validates the ObjectSpace record inside its allocated metadata blob.
	private static bool TryReadMetaData(ClusteredStreams streams, out ObjectSpaceMetaData metadata) {
		metadata = null;
		if (!streams.HasMetaData || streams.MetaDataCapacity < sizeof(int))
			return false;

		// Reject an impossible used length before the component codec reads its variable-sized factory table.
		var bytes = streams.ReadMetaData();
		var bitConverter = EndianBitConverter.For(streams.Endianness);
		var length = bitConverter.ToInt32(bytes);
		if (length < sizeof(int) + sizeof(ushort) || length > streams.MetaDataCapacity)
			return false;
		using var record = new MemoryStream(bytes, 0, length, writable: false);
		using var recordReader = new EndianBinaryReader(bitConverter, record);
		metadata = new ObjectSpaceMetaDataSerializer().Deserialize(recordReader);
		if (Enum.IsDefined(metadata.HashFunction) && metadata.SerializerFactory != null)
			return true;
		metadata = null;
		return false;
	}

	// Compares factory registrations, including their exact type codes and member policies.
	private static bool FactoriesMatch(SerializerFactory stored, SerializerFactory supplied, Endianness endianness) {
		// Compare the explicit registration and member contract, including exact codes and wrapper policies.
		var serializer = new SerializerFactorySerializer();
		return serializer.SerializeToBytes(stored, endianness)
			.SequenceEqual(serializer.SerializeToBytes(supplied, endianness));
	}

	// Rejects comparer registrations that cannot be reconstructed from metadata.
	private static void ValidateComparers(ComparerFactory comparers) {
		// Comparer instances have no persistent construction record in ObjectSpace metadata.
		if (!comparers.HasSameRegistrations(ComparerFactory.Default))
			throw new NotSupportedException("Metadata storage requires the default comparer registrations. Omit StoreMetaData for supplied comparers.");
	}

	// Sets the endian trait to match the configured byte order.
	private static ObjectSpaceTraits WithEndianness(ObjectSpaceTraits traits, Endianness endianness) {
		return traits.CopyAndSetFlags(ObjectSpaceTraits.BigEndian, endianness == Endianness.BigEndian);
	}

	// Resolves a recorded name to one unambiguous public field or non-indexed property.
	private static bool TryResolveStoredMember(Type type, string name, out Member member) {
		// Recorded names identify one ordinary field or property in the installed CLR type.
		member = null;
		var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public)
			.Where(member => member.Name == name && member is PropertyInfo or FieldInfo).ToArray();
		if (members.Length != 1 || members[0] is PropertyInfo property && property.GetIndexParameters().Length != 0)
			return false;
		member = members[0].ToMember();
		return true;
	}
}
