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

namespace Sphere10.Framework;

/// <summary>
/// Manages the 256-byte header stored at the start of a clustered streams root, including standard fields and user extension properties.
/// </summary>
public class ClusteredStreamsHeader {

	public const int ByteLength = 256;

	/// <summary>The supported clustered-stream header version.</summary>
	public const byte VersionNumber = 1;

	/// <summary>The final header DWORD, containing allocated metadata bytes when metadata is present.</summary>
	public const int MetaDataSizeOffset = ByteLength - sizeof(int);

	/// <summary>The byte offset immediately after the fixed header, where metadata begins.</summary>
	public const int MetaDataOffset = ByteLength;

	public const int MerkleRootLength = 32;
	public const int MasterKeyLength = 32;

	internal const int VersionLength = sizeof(byte);
	internal const int PolicyLength = sizeof(uint);
	internal const int StreamCountLength = sizeof(long);
	internal const int StreamDescriptorsEndClusterLength = sizeof(long);
	internal const int ReservedStreamsLength = sizeof(long);
	internal const int ClusterSizeLength = sizeof(int);
	internal const int TotalClustersLength = sizeof(long);

	internal const int VersionOffset = 0;
	internal const int PolicyOffset = VersionOffset + VersionLength;
	internal const int StreamCountOffset = PolicyOffset + PolicyLength;
	internal const int StreamDescriptorsEndClusterOffset = StreamCountOffset + StreamCountLength;
	internal const int ReservedStreamsOffset = StreamDescriptorsEndClusterOffset + StreamDescriptorsEndClusterLength;
	internal const int ClusterSizeOffset = ReservedStreamsOffset + ReservedStreamsLength;
	internal const int TotalClustersOffset = ClusterSizeOffset + ClusterSizeLength;
	internal const int ExtensionPropertiesOffset = TotalClustersOffset + TotalClustersLength;


	private readonly BoundedStream _headerStream;
	private readonly Stream _extensionPropertiesStream;
	private readonly EndianBinaryReader _reader;
	private readonly EndianBinaryWriter _writer;

	private readonly ICriticalObject _lock;

	private readonly StreamMappedProperty<byte> _versionProperty;
	private readonly StreamMappedProperty<ClusteredStreamsPolicy> _policyProperty;
	private readonly StreamMappedProperty<long> _streamCountProperty;
	private readonly StreamMappedProperty<long> _streamDescriptorsClusterProperty;
	private readonly StreamMappedProperty<long> _reservedStreamsProperty;
	private readonly StreamMappedProperty<int> _clusterSizeProperty;
	private readonly StreamMappedProperty<long> _totalClustersProperty;
	private readonly IList<Action> _mappedExtensionPropertyFlushes;

	internal ClusteredStreamsHeader(ConcurrentStream rootStream, Endianness endianness) {
		Guard.ArgumentNotNull(rootStream, nameof(rootStream));
		_headerStream = rootStream.AsBounded(0, ByteLength, allowInnerResize: true); 
		_lock = rootStream;
		_reader = new EndianBinaryReader(EndianBitConverter.For(endianness), _headerStream);
		_writer = new EndianBinaryWriter(EndianBitConverter.For(endianness), _headerStream);
		_versionProperty = StreamMappedProperty.From(_headerStream, VersionOffset, VersionLength, PrimitiveSerializer<byte>.Instance, _reader, _writer, @lock: _lock, useCache: true);
		_policyProperty = StreamMappedProperty.From(_headerStream, PolicyOffset, PolicyLength, EnumSerializer<ClusteredStreamsPolicy>.Instance, _reader, _writer, @lock: _lock, useCache: true);
		_streamCountProperty = StreamMappedProperty.From(_headerStream, StreamCountOffset, StreamCountLength, PrimitiveSerializer<long>.Instance, _reader, _writer, @lock: _lock, useCache: true);
		_streamDescriptorsClusterProperty = StreamMappedProperty.From(_headerStream, StreamDescriptorsEndClusterOffset, StreamDescriptorsEndClusterLength, PrimitiveSerializer<long>.Instance, _reader, _writer, @lock: _lock, useCache: true);
		_reservedStreamsProperty = StreamMappedProperty.From(_headerStream, ReservedStreamsOffset, ReservedStreamsLength, PrimitiveSerializer<long>.Instance, _reader, _writer, @lock: _lock, useCache: true);
		_clusterSizeProperty = StreamMappedProperty.From(_headerStream, ClusterSizeOffset, ClusterSizeLength, PrimitiveSerializer<int>.Instance, _reader, _writer, @lock: _lock, useCache: true);
		_totalClustersProperty = StreamMappedProperty.From(_headerStream, TotalClustersOffset, TotalClustersLength, PrimitiveSerializer<long>.Instance, _reader, _writer, @lock: _lock, useCache: true);
		_extensionPropertiesStream = rootStream.AsBounded(ExtensionPropertiesOffset, ByteLength - VersionLength - PolicyLength - StreamCountLength - StreamDescriptorsEndClusterLength - ReservedStreamsLength - ClusterSizeLength - TotalClustersLength, allowInnerResize: false, useRelativeOffset: true);
		_mappedExtensionPropertyFlushes = new List<Action>();
	}

	public byte Version { get => _versionProperty.Value; internal set => _versionProperty.Value = value; }

	public ClusteredStreamsPolicy Policy { get => _policyProperty.Value; internal set => _policyProperty.Value = value; }

	public long StreamCount { get => _streamCountProperty.Value; internal set => _streamCountProperty.Value = value; }

	public long StreamDescriptorsEndCluster { get => _streamDescriptorsClusterProperty.Value; internal set => _streamDescriptorsClusterProperty.Value = value; }

	public long ReservedStreams {
		get => _reservedStreamsProperty.Value;
		internal set {
			if (StreamCount > _reservedStreamsProperty.Value)
				throw new InvalidOperationException($"Cannot set {nameof(ReservedStreams)} to {value} as records already exist with value");

			_reservedStreamsProperty.Value = value;
		}
	}

	public int ClusterSize {
		get => _clusterSizeProperty.Value;
		set => _clusterSizeProperty.Value = value;
	}

	public long TotalClusters { get => _totalClustersProperty.Value; set => _totalClustersProperty.Value = value; }

	/// <summary>Whether an opaque metadata region follows the fixed header.</summary>
	public bool HasMetaData => Policy.HasFlag(ClusteredStreamsPolicy.MetaData);

	/// <summary>The allocated metadata size in bytes, stored inside the fixed header.</summary>
	public int MetaDataSize {
		get {
			using var accessScope = _lock.EnterAccessScope();
			if (!HasMetaData)
				return 0;

			// Read the stored field; allocation bounds are checked by CheckHeaderIntegrity.
			_headerStream.Position = MetaDataSizeOffset;
			return PrimitiveSerializer<int>.Instance.Deserialize(_reader);
		}
		internal set {
			Guard.ArgumentNotNegative(value, nameof(value));
			using var accessScope = _lock.EnterAccessScope();
			_headerStream.Position = MetaDataSizeOffset;
			PrimitiveSerializer<int>.Instance.Serialize(value, _writer);
		}
	}

	/// <summary>The first cluster byte; legacy streams retain their original offset.</summary>
	public long DataOffset => MetaDataOffset + (long)MetaDataSize;

	/// <summary>Bytes available for extensions, excluding the metadata size field when present.</summary>
	public long ExtensionPropertiesLength => ByteLength - ExtensionPropertiesOffset - (HasMetaData ? sizeof(int) : 0);

	/// <summary>Reads a fixed-size value at an offset relative to the header extension area.</summary>
	public T ReadExtensionProperty<T>(long offset, IItemSerializer<T> serializer) {
		// Fixed header fields are read directly through the established header reader.
		Guard.ArgumentNotNull(serializer, nameof(serializer));
		Guard.Argument(serializer.IsConstantSize, nameof(serializer), "Header fields require a constant-size serializer.");
		Guard.Argument(offset >= 0 && offset <= ExtensionPropertiesLength - serializer.ConstantSize, nameof(offset), "The field must fit in the extension area.");
		using var accessScope = _lock.EnterAccessScope();
		_extensionPropertiesStream.Position = offset;
		return serializer.Deserialize(_reader);
	}

	/// <summary>Writes a fixed-size value at an offset relative to the header extension area.</summary>
	public void WriteExtensionProperty<T>(long offset, T value, IItemSerializer<T> serializer) {
		// The field stays within its header allocation without creating a mapped property or cache.
		Guard.ArgumentNotNull(serializer, nameof(serializer));
		Guard.Argument(serializer.IsConstantSize, nameof(serializer), "Header fields require a constant-size serializer.");
		Guard.Argument(offset >= 0 && offset <= ExtensionPropertiesLength - serializer.ConstantSize, nameof(offset), "The field must fit in the extension area.");
		using var accessScope = _lock.EnterAccessScope();
		_extensionPropertiesStream.Position = offset;
		serializer.Serialize(value, _writer);
	}

	public StreamMappedProperty<T> MapExtensionProperty<T>(long offset, int length, IItemSerializer<T> serializer)
		=> MapExtensionProperty(offset, length, serializer, false);

	public StreamMappedProperty<T> MapExtensionProperty<T>(long offset, int length, IItemSerializer<T> serializer, bool useCache = false) {
		Guard.ArgumentInRange(offset, 0, ExtensionPropertiesLength - 1, nameof(offset));
		Guard.ArgumentInRange(length, 1, ExtensionPropertiesLength - offset, nameof(length));
		Guard.ArgumentNotNull(serializer, nameof(serializer));
		var extProp = StreamMappedProperty.From(_extensionPropertiesStream, offset, length, serializer, _reader, _writer, @lock: _lock, useCache: useCache);
		_mappedExtensionPropertyFlushes.Add(extProp.FlushCache);
		return extProp;
	}

	public void Load() {
		using var accessScope = _lock.EnterAccessScope();
		Guard.Ensure(_headerStream.Length >= ByteLength, "Missing or corrupt header");
	}

	public void Create(byte version, int clusterSize, long reservedRecords) {
		Guard.Argument(version == VersionNumber, nameof(version), "Unsupported clustered-stream version.");
		Guard.ArgumentGTE(clusterSize, 1, nameof(clusterSize));
		Guard.ArgumentGTE(reservedRecords, 0, nameof(reservedRecords));

		using var accessScope = _lock.EnterAccessScope();
		Guard.Ensure(_headerStream.Length == 0, "Header must be empty");
		_headerStream.SetLength(ByteLength);
		Version = version;
		Policy = 0;
		StreamCount = 0;
		StreamDescriptorsEndCluster = Cluster.Null;
		ReservedStreams = reservedRecords;
		ClusterSize = clusterSize;
		TotalClusters = 0;
		Guard.Ensure(_headerStream.Position == ByteLength - _extensionPropertiesStream.Length);
		Load();
	}

	public void CheckHeaderIntegrity() {
		using var accessScope = _lock.EnterAccessScope();
		Guard.Ensure(Version == VersionNumber, $"Corrupt header property {nameof(Version)} value was {Version} bytes");
		Guard.Ensure(ClusterSize > 0, $"Corrupt header property {nameof(ClusterSize)} value was {ClusterSize} bytes");
		Guard.Ensure(TotalClusters >= 0, $"Corrupt header property {nameof(TotalClusters)} value was {TotalClusters} bytes");
		Guard.Ensure(ReservedStreams >= 0, $"Corrupt header property {nameof(ReservedStreams)} value was {ReservedStreams} bytes");
		Guard.Ensure(StreamCount >= 0, $"Corrupt header property {nameof(StreamCount)} value was {StreamCount} bytes");
		Guard.Ensure(StreamDescriptorsEndCluster >= Cluster.Null, $"Corrupt header property {nameof(StreamDescriptorsEndCluster)} value was {StreamDescriptorsEndCluster} bytes");

		// The declared metadata allocation must fit after the fixed header.
		if (HasMetaData) {
			var metaDataSize = MetaDataSize;
			if (metaDataSize < 0 || metaDataSize > _headerStream.InnerStream.Length - MetaDataOffset)
				throw new InvalidDataException("Invalid clustered-stream metadata capacity.");
		}
	}

	public void FlushCache() {
		_versionProperty?.FlushCache();
		_policyProperty?.FlushCache();
		_streamCountProperty?.FlushCache();
		_streamDescriptorsClusterProperty?.FlushCache();
		_reservedStreamsProperty?.FlushCache();
		_clusterSizeProperty?.FlushCache();
		_totalClustersProperty?.FlushCache();
		_mappedExtensionPropertyFlushes.ForEach(x => x.Invoke());
	}

	public override string ToString() {
		using var accessScope = _lock.EnterAccessScope();
		return $"[{nameof(ClusteredStreamsHeader)}] {nameof(Version)}: {Version}, {nameof(ClusterSize)}: {ClusterSize}, {nameof(TotalClusters)}: {TotalClusters}, {nameof(StreamCount)}: {StreamCount}, {nameof(StreamDescriptorsEndCluster)}: {StreamDescriptorsEndCluster}, {nameof(ReservedStreams)}: {ReservedStreams}, {nameof(Policy)}: {Policy}, Extension Data: {_extensionPropertiesStream.ToArray()}";
	}

}

