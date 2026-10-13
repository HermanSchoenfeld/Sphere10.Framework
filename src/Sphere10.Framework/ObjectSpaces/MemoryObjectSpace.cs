// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;

namespace Sphere10.Framework.ObjectSpaces;

/// <summary>
/// In-memory object space that stores data in a <see cref="MemoryStream"/> while reusing the same object space plumbing as file-backed instances.
/// </summary>
public class MemoryObjectSpace : ObjectSpace {

	public MemoryObjectSpace(
		ObjectSpaceDefinition objectSpaceDefinition,
		SerializerFactory serializerFactory,
		ComparerFactory comparerFactory,
		ClusteredStreamsPolicy clusteredStreamsPolicy = Sphere10FrameworkDefaults.MemoryObjectSpaceContainerPolicy,
		Endianness endianness = Sphere10FrameworkDefaults.Endianness
	) : this(objectSpaceDefinition, serializerFactory, comparerFactory,
		Sphere10FrameworkDefaults.MemoryObjectSpaceClusterSize,
		clusteredStreamsPolicy, endianness) {
	}

	public MemoryObjectSpace(
		ObjectSpaceDefinition objectSpaceDefinition,
		SerializerFactory serializerFactory,
		ComparerFactory comparerFactory,
		int clusterSize,
		ClusteredStreamsPolicy clusteredStreamsPolicy = Sphere10FrameworkDefaults.MemoryObjectSpaceContainerPolicy,
		Endianness endianness = Sphere10FrameworkDefaults.Endianness
	) : this(new MemoryStream(), objectSpaceDefinition, serializerFactory, comparerFactory, clusterSize, clusteredStreamsPolicy, endianness) {
		Streams.OwnsStream = true;
	}

	public MemoryObjectSpace(
		MemoryStream memoryStream,
		ObjectSpaceDefinition objectSpaceDefinition,
		SerializerFactory serializerFactory,
		ComparerFactory comparerFactory,
		ClusteredStreamsPolicy clusteredStreamsPolicy = Sphere10FrameworkDefaults.MemoryObjectSpaceContainerPolicy,
		Endianness endianness = Sphere10FrameworkDefaults.Endianness
	) : this(memoryStream, objectSpaceDefinition, serializerFactory, comparerFactory,
		ResolveClusterSize(memoryStream, objectSpaceDefinition, endianness),
		clusteredStreamsPolicy, endianness) {
	}

	public MemoryObjectSpace(
		MemoryStream memoryStream,
		ObjectSpaceDefinition objectSpaceDefinition,
		SerializerFactory serializerFactory,
		ComparerFactory comparerFactory,
		int clusterSize,
		ClusteredStreamsPolicy clusteredStreamsPolicy = Sphere10FrameworkDefaults.MemoryObjectSpaceContainerPolicy,
		Endianness endianness = Sphere10FrameworkDefaults.Endianness
	) : base(CreateStreams(memoryStream, objectSpaceDefinition, clusterSize, clusteredStreamsPolicy, endianness), objectSpaceDefinition, serializerFactory, comparerFactory,
		FileAccessMode.Default, Sphere10FrameworkDefaults.MemoryObjectSpaceDimensionClusterSize) {
		Guard.ArgumentNotNull(objectSpaceDefinition, nameof(objectSpaceDefinition));
		Guard.ArgumentNotNull(serializerFactory, nameof(serializerFactory));
		Guard.ArgumentNotNull(comparerFactory, nameof(comparerFactory));
		Stream = memoryStream;
		Load();
	}

	public MemoryStream Stream { get; }

	private static int ResolveClusterSize(MemoryStream stream, ObjectSpaceDefinition definition, Endianness endianness) {
		Guard.ArgumentNotNull(stream, nameof(stream));
		Guard.ArgumentNotNull(definition, nameof(definition));
		definition.AssertOpeningIndexOrder(stream.Length > 0);
		return stream.Length == 0 ? Sphere10FrameworkDefaults.MemoryObjectSpaceClusterSize
			: ClusteredStreams.ReadStoredClusterSize(stream, definition.Traits.HasFlag(ObjectSpaceTraits.Merkleized) ? 1 : 0, endianness);
	}

	private static ClusteredStreams CreateStreams(MemoryStream memoryStream, ObjectSpaceDefinition definition, int clusterSize, ClusteredStreamsPolicy clusteredStreamsPolicy, Endianness endianness) {
		Guard.ArgumentNotNull(memoryStream, nameof(memoryStream));
		Guard.ArgumentNotNull(definition, nameof(definition));
		definition.AssertOpeningIndexOrder(memoryStream.Length > 0);
		var objectSpaceMetaDataStreamCount = definition.Traits.HasFlag(ObjectSpaceTraits.Merkleized) ? 1 : 0;
		if (memoryStream.Length > 0)
			Guard.Ensure(ClusteredStreams.ReadStoredClusterSize(memoryStream, objectSpaceMetaDataStreamCount, endianness) == clusterSize,
				"The explicitly configured outer cluster size must match existing storage.");
		return new ClusteredStreams(memoryStream, clusterSize, clusteredStreamsPolicy, objectSpaceMetaDataStreamCount, endianness, false);
	}
}
