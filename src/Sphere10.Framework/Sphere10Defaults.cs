// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.


namespace Sphere10.Framework;

public static class Sphere10FrameworkDefaults {

	// Streams
	public const int DefaultBufferOperationBlockSize = 32768; 
	public const int TransactionalPageBufferOperationBlockSize = 262144;
	public const int OptimalCompressWriteBlockSize = 8192;

	// Paged
	public const int MaxMemoryPerCollection = int.MaxValue; // Use all available memory

	// Transactional
	public const int TransactionalPageSize = 262144; // 256kb
	public const string TransactionalPageFolder = ".txn"; 

	// Clustered
	public const int ClusterSize = 4096; // 4kb
	public const int SmallestRecommendedClusterSize = 256;
	public const int LargestRecommendedClusterSize = 8192;
	public const long RecordCacheSize = 1048576; // 1mb

	/// <summary>Default fraction of capacity targeted for release during cache admission pressure.</summary>
	public const double CacheReapLevel = 0.25;
	public const ClusteredStreamsPolicy ContainerPolicy = ClusteredStreamsPolicy.Default;

	// Serialization
	public const Endianness Endianness = Sphere10.Framework.Endianness.LittleEndian;

	// Hashing
	public const CHF HashFunction = CHF.SHA2_256;

	// Object Streams
	public const int IndexBTreeOrder = 64;
	public const string DefaultReyclableIndexIndexName = "Recylable Index";
	public const string DefaultMerkleTreeIndexName = "Merkle Tree";
	public const string DefaultItemChecksumIndexName = "Item Checksum";
	public const string DefaultKeyChecksumIndexName = "Key Checksum";
	public const string DefaultKeyStoreAttachmentName = "Key Store";

	// Standalone trees: balanced numeric-key/value recommendations; explicit orders remain available.
	public const int BTreeOrder = 128;
	public const int BTreePlusOrder = 2048;
	public const int BTreeLookupOrder = 2048;
	public const int StreamMappedBTreeOrder = 128;
	public const int StreamMappedBTreePlusOrder = 2048;
	public const int StreamMappedBTreeLookupOrder = 2048;

	// Explicit serialized 32-byte-key/8-byte-value choices; generic constructors do not infer serializer widths.
	public const int StreamMappedWideKeyBTreePlusOrder = 1024;
	public const int StreamMappedWideKeyBTreeLookupOrder = 1024;

	// Object Spaces
	// ObjectSpace defaults selected for balanced throughput; generic collection layouts remain unchanged.
	public const int ObjectSpaceIndexBTreeOrder = 256;
	public const int MemoryObjectSpaceClusterSize = 1024;
	public const int MemoryObjectSpaceDimensionClusterSize = 1024;
	public const ClusteredStreamsPolicy MemoryObjectSpaceContainerPolicy = ClusteredStreamsPolicy.CacheClusterHeaders | ClusteredStreamsPolicy.IntegrityChecks;
	public const int FileObjectSpaceClusterSize = ClusterSize;
	public const int FileObjectSpaceDimensionClusterSize = SmallestRecommendedClusterSize;
	public const ClusteredStreamsPolicy FileObjectSpaceContainerPolicy = ContainerPolicy;
	public const int ObjectSpacePageSize = TransactionalPageSize;
	public const int ObjectSpaceMaxMemory = 16777216;
	public const string DefaultSpatialMerkleTreeIndexName = "Spatial Tree";

}


