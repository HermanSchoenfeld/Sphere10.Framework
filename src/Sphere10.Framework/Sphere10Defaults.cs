// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.


namespace Sphere10.Framework;

public static class Sphere10FrameworkDefaults {

	/// <summary>
	/// Largest node scratch buffer, in bytes, allocated on the stack by the stream-mapped B-trees.
	/// Larger node buffers are rented instead; this threshold does not change the stored node layout.
	/// </summary>
	public const int StackBufferSize = 1024;

	/// <summary>
	/// Default transfer chunk size, in bytes, for stream copying, routing, byte shifting and
	/// stream-mapped buffer operations. This controls temporary I/O buffers, not storage page size.
	/// </summary>
	public const int DefaultBufferOperationBlockSize = 32768; 
	/// <summary>
	/// Transfer chunk size, in bytes, used when committing dirty transaction page files to the
	/// backing file. This is the commit copy buffer size, independently of transaction page geometry.
	/// </summary>
	public const int TransactionalPageBufferOperationBlockSize = 262144;
	/// <summary>
	/// Transfer chunk size, in bytes, used when routing data through compression and decompression
	/// streams. This controls I/O buffering rather than the compression level or encoded block format.
	/// </summary>
	public const int OptimalCompressWriteBlockSize = 8192;

	/// <summary>
	/// Default file-page cache budget, in bytes, for paged and transactional file descriptors.
	/// Pages consume this budget as they are loaded; it is neither an initial allocation nor a
	/// limit on all managed memory used by the collection.
	/// </summary>
	public const int MaxMemoryPerCollection = int.MaxValue; // Use all available memory

	/// <summary>
	/// Default logical file page size, in bytes, for paged and transactional file descriptors.
	/// For transactional files this also determines dirty page boundaries; retain the original
	/// page size when recovering outstanding transaction pages.
	/// </summary>
	public const int TransactionalPageSize = 262144; // 256kb
	/// <summary>
	/// Default relative directory for uncommitted transaction pages and recovery markers.
	/// Transactional files resolve this directory beneath the backing file's parent directory.
	/// </summary>
	public const string TransactionalPageFolder = ".txn"; 

	/// <summary>
	/// Default cluster payload size, in bytes, for general clustered streams and file descriptors.
	/// Serialized cluster metadata occupies additional space. This size determines storage layout
	/// and must match when reopening an existing container.
	/// </summary>
	public const int ClusterSize = 4096; // 4kb
	/// <summary>
	/// Lower bound, in payload bytes, used when ObjectSpaces clamp dimension cluster-size hints
	/// or backend defaults. This is a recommendation used by ObjectSpaces, not the minimum size
	/// supported by every clustered stream.
	/// </summary>
	public const int SmallestRecommendedClusterSize = 256;
	/// <summary>
	/// Upper bound, in payload bytes, used when ObjectSpaces clamp dimension cluster-size hints
	/// or backend defaults. This bounds dimension allocation granularity rather than the maximum
	/// size of an object or stream.
	/// </summary>
	public const int LargestRecommendedClusterSize = 8192;
	/// <summary>
	/// Capacity, in serialized descriptor bytes, of the clustered-stream descriptor cache when
	/// its policy enables descriptor caching. Cache accounting excludes managed object overhead.
	/// </summary>
	public const long RecordCacheSize = 1048576; // 1mb

	/// <summary>
	/// Default fraction of cache capacity targeted for release when admitting an item requires
	/// eviction. The reaper targets at least the admission deficit, even when it exceeds this
	/// fraction; capacity units follow the cache's size estimator.
	/// </summary>
	public const double CacheReapLevel = 0.25;
	/// <summary>
	/// Default runtime policy for general clustered-stream containers and file descriptors.
	/// Enables descriptor caching, cluster-header caching and integrity checks.
	/// </summary>
	public const ClusteredStreamsPolicy ContainerPolicy = ClusteredStreamsPolicy.Default;

	/// <summary>
	/// Default byte order used by framework storage and serialization APIs that accept an
	/// endianness argument. Use the original byte order when reading or reopening persisted data.
	/// </summary>
	public const Endianness Endianness = Sphere10.Framework.Endianness.LittleEndian;

	/// <summary>
	/// Default digest algorithm for seed generation and ObjectSpace Merkle-tree configuration.
	/// Merkleized storage must retain its original algorithm because it determines digest sizes
	/// and the resulting tree roots.
	/// </summary>
	public const CHF HashFunction = CHF.SHA2_256;

	/// <summary>
	/// Default B-tree order for ObjectStream projection indexes and key-storage attachments.
	/// Order bounds node fanout and determines serialized node layout; reopen an index with its
	/// original order. ObjectSpaces use their separate member-index default.
	/// </summary>
	public const int IndexBTreeOrder = 64;
	/// <summary>
	/// Default attachment name for the index that records reaped ObjectStream slots available
	/// for reuse by recyclable collections and ObjectSpace dimensions.
	/// </summary>
	public const string DefaultReyclableIndexIndexName = "Recylable Index";
	/// <summary>
	/// Default attachment name for a collection's or ObjectSpace dimension's Merkle-tree index.
	/// The ObjectSpace tree that combines dimension roots uses a separate attachment name.
	/// </summary>
	public const string DefaultMerkleTreeIndexName = "Merkle Tree";
	/// <summary>
	/// Conventional attachment name callers can supply for an optional per-item checksum index.
	/// Collection factories omit that index unless an item-checksum index name is supplied.
	/// </summary>
	public const string DefaultItemChecksumIndexName = "Item Checksum";
	/// <summary>
	/// Default attachment name for key-checksum lookup indexes used by key-value-pair dictionaries
	/// and hash sets. Constant-size-key dictionaries use a separate key-storage attachment.
	/// </summary>
	public const string DefaultKeyChecksumIndexName = "Key Checksum";
	/// <summary>
	/// Default attachment name for the ordered key store used by constant-size-key dictionaries.
	/// This stores serialized keys separately from the dictionary's ObjectStream values.
	/// </summary>
	public const string DefaultKeyStoreAttachmentName = "Key Store";

	/// <summary>
	/// Default order of an in-memory B-tree when no order is supplied. Order is the maximum
	/// steady-state number of children per node; nodes hold at most order minus one keys.
	/// </summary>
	public const int BTreeOrder = 128;
	/// <summary>
	/// Default order of an in-memory B+ tree when no order is supplied. Order bounds internal
	/// node fanout; each leaf and internal node holds at most order minus one keys.
	/// </summary>
	public const int BTreePlusOrder = 2048;
	/// <summary>
	/// Default order of an in-memory B-tree lookup's backing tree when no order is supplied.
	/// The default backing type is B+ and its keys combine the lookup key with a group ordinal.
	/// </summary>
	public const int BTreeLookupOrder = 2048;
	/// <summary>
	/// Default order used to create a B-tree over an empty stream when no order is supplied.
	/// Order determines fixed node layout; opening existing storage requires its original order
	/// through an explicit-order constructor.
	/// </summary>
	public const int StreamMappedBTreeOrder = 128;
	/// <summary>
	/// Default order used to create a B+ tree over an empty stream when no order is supplied.
	/// Order determines fixed node layout; opening existing storage requires its original order
	/// through an explicit-order constructor.
	/// </summary>
	public const int StreamMappedBTreePlusOrder = 2048;
	/// <summary>
	/// Default backing-tree order used to create a B-tree lookup over an empty stream without
	/// an explicit order. Its default B+ tree stores compound keys with group ordinals; opening
	/// existing storage requires the original order.
	/// </summary>
	public const int StreamMappedBTreeLookupOrder = 2048;

	/// <summary>
	/// Explicit order recommendation for stream-mapped B+ trees with serialized 32-byte keys
	/// and 8-byte values. Pass it explicitly when creating such a tree; generic constructors
	/// do not infer serializer widths or select this order automatically.
	/// </summary>
	public const int StreamMappedWideKeyBTreePlusOrder = 1024;
	/// <summary>
	/// Explicit backing-tree order recommendation for stream-mapped lookups with serialized
	/// 32-byte lookup keys and 8-byte values. Pass it explicitly; generic constructors do not
	/// infer serializer widths or select this order automatically.
	/// </summary>
	public const int StreamMappedWideKeyBTreeLookupOrder = 1024;

	/// <summary>
	/// Default B-tree order used by ObjectSpace member indexes when defining a new store.
	/// It controls node fanout and serialized layout independently of standalone tree defaults;
	/// retain the original index order when reopening an existing ObjectSpace.
	/// </summary>
	public const int ObjectSpaceIndexBTreeOrder = 256;
	/// <summary>
	/// Default root-container cluster payload size, in bytes, for a new memory ObjectSpace.
	/// When an existing memory stream is opened without an explicit size, its stored root
	/// cluster size is used instead.
	/// </summary>
	public const int MemoryObjectSpaceClusterSize = 1024;
	/// <summary>
	/// Default cluster payload size, in bytes, for a new memory ObjectSpace dimension without
	/// an average-object-size hint. Each dimension has its own clustered container; existing
	/// dimensions retain their stored size when no hint is supplied.
	/// </summary>
	public const int MemoryObjectSpaceDimensionClusterSize = 1024;
	/// <summary>
	/// Default runtime policy for memory ObjectSpace containers. Enables cluster-header caching
	/// and integrity checks, without the descriptor caching enabled by the general container policy.
	/// </summary>
	public const ClusteredStreamsPolicy MemoryObjectSpaceContainerPolicy = ClusteredStreamsPolicy.CacheClusterHeaders | ClusteredStreamsPolicy.IntegrityChecks;
	/// <summary>
	/// Default root-container cluster payload size, in bytes, selected by the ObjectSpace builder
	/// for new file stores. This shares the general cluster-size default; reopening an existing
	/// file requires the original root cluster size.
	/// </summary>
	public const int FileObjectSpaceClusterSize = ClusterSize;
	/// <summary>
	/// Default cluster payload size, in bytes, for a new file ObjectSpace dimension without
	/// an average-object-size hint. It controls dimension allocation granularity separately
	/// from the root container's cluster size.
	/// </summary>
	public const int FileObjectSpaceDimensionClusterSize = SmallestRecommendedClusterSize;
	/// <summary>
	/// Default runtime container policy selected by the ObjectSpace builder for file stores.
	/// Inherits general descriptor caching, cluster-header caching and integrity checks.
	/// </summary>
	public const ClusteredStreamsPolicy FileObjectSpaceContainerPolicy = ContainerPolicy;
	/// <summary>
	/// Default transactional file page size, in bytes, selected by the ObjectSpace builder.
	/// It determines cached page and dirty transaction page boundaries for file stores and
	/// has no paging role for the memory backend.
	/// </summary>
	public const int ObjectSpacePageSize = TransactionalPageSize;
	/// <summary>
	/// Default resident file-page cache budget, in bytes, selected by the ObjectSpace builder.
	/// This does not bound tracked object instances or total ObjectSpace memory, and does not
	/// limit the memory backend.
	/// </summary>
	public const int ObjectSpaceMaxMemory = 16777216;
	/// <summary>
	/// Default attachment name for the ObjectSpace Merkle tree that combines dimension Merkle
	/// roots. This attachment is created for Merkleized ObjectSpaces; individual dimension trees
	/// use their own Merkle-tree attachment name.
	/// </summary>
	public const string DefaultSpatialMerkleTreeIndexName = "Spatial Tree";

}


