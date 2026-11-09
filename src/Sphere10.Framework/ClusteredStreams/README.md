# Clustered Streams

Multiplexes any number of resizable logical streams onto one root `Stream` through linked chains of fixed-size clusters, with pluggable attachments for indexes and other reserved data.

`ClusteredStreams` works like a small file system inside a stream. A 256-byte `ClusteredStreamsHeader` starts the root stream, optional metadata follows it, and the cluster map follows that. Each logical stream is a bidirectionally linked chain of `Cluster` records, and a `ClusteredStreamDescriptor` records its start cluster, end cluster, size and traits. The descriptors themselves live in a chain that starts at cluster 0. `Add`, `Insert`, `Remove`, `Swap`, `Clear` and `Optimize` manage the logical streams, and `OpenRead` and `OpenWrite` return a `ClusteredStream` that reads and writes one of them. This is the storage layer beneath the `ObjectStream` types in [ObjectStream](../ObjectStream/README.md), the stream-mapped collections in [Collections](../Collections/README.md) and the [object spaces](../ObjectSpaces/README.md).

Construct a container over a root stream and call `Load()`, or pass `autoLoad: true`. An empty root stream is initialized with the given cluster size and number of reserved streams. A non-empty one is validated, and its stored cluster size must equal the constructor's `clusterSize`. `ClusteredStreams.FromStream` reads the cluster size, policy and reserved-stream count from an existing header. Every operation runs inside the root stream's access scope from `EnterAccessScope()`. An open `ClusteredStream` holds that scope until it is disposed, only one open handle per stream index is allowed, and disposing the container with a stream still open fails. The container disposes the root stream only when `OwnsStream` is `true`. `ClusteredStreamsPolicy` selects descriptor caching, cluster-header caching, fast allocation and integrity checks.

Reserved streams hold attachments. An `IClusteredStreamsAttachment` registers through `RegisterAttachment` and attaches to its reserved streams when the container loads. Register every attachment before `Load()`, because loading fails when the header declares fewer reserved streams than the attachments need. The attachments in [Attachments](Attachments) provide paged lists, stacks, B-trees, B-tree lookups, and unique-key and index maps from a value to the positions that hold it. `ClusterDiagnostics` dumps and verifies the cluster topology for tests.

```csharp
using var root = new MemoryStream();
using var streams = new ClusteredStreams(root, clusterSize: 256, autoLoad: true);
using (var stream = streams.Add())
	stream.Write(new byte[] { 1, 2, 3 });
using (var stream = streams.OpenRead(0))
	Console.WriteLine(stream.Length); // 3
```

Sizes, policies and default attachment names come from `Sphere10FrameworkDefaults` in the project folder. Cluster records and descriptors use the constant-size serializers from [Serialization](../Serialization/README.md), and the root stream is wrapped by the `ConcurrentStream` described in [Streams](../Streams/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Cluster`](Cluster.cs) | class | One cluster record holding its traits, previous and next links and data payload. |
| [`ClusterDiagnostics`](ClusterDiagnostics.cs) | static class | Text dumps of a container or cluster map, and verification of cluster chains, descriptors and seek state for tests. |
| [`ClusteredStream`](ClusteredStream.cs) | class | Stream over one logical stream's cluster chain, which holds the container's access scope until it is disposed. |
| [`ClusteredStreamDescriptor`](ClusteredStreamDescriptor.cs) | struct | Start cluster, end cluster, logical size and traits of one logical stream. |
| [`ClusteredStreamDescriptorSerializer`](ClusteredStreamDescriptorSerializer.cs) | class | Fixed-width serializer for `ClusteredStreamDescriptor` records. |
| [`ClusteredStreams`](ClusteredStreams.cs) | class | Container that multiplexes logical streams onto one root stream, with stream events, metadata, reserved streams and registered attachments. |
| [`ClusteredStreamsAttachmentBase`](ClusteredStreamsAttachmentBase.cs) | abstract class | Base for an attachment that opens one reserved stream as a bounded stream and builds its structure in `AttachInternal`. |
| [`ClusteredStreamsHeader`](ClusteredStreamsHeader.cs) | class | Reads and writes the 256-byte root header with its version, cluster size, counts, policy, metadata size and user extension fields. |
| [`ClusteredStreamsPolicy`](ClusteredStreamsPolicy.cs) | enum | Flags for fast allocation, cluster-header caching, descriptor caching, integrity checks and a stored metadata region. |
| [`ClusteredStreamTraits`](ClusteredStreamTraits.cs) | enum | Flags that mark a logical stream as null or as a reaped slot available for reuse. |
| [`ClusterSerializer`](ClusterSerializer.cs) | class | Fixed-layout serializer for `Cluster` records of a given data size. |
| [`ClusterTraits`](ClusterTraits.cs) | enum | Flags that mark a cluster as the start or end of its chain. |
| [`StreamMappedDictionaryImplementation`](StreamMappedDictionaryImplementation.cs) | enum | Selects the key/value-list or constant-length-key layout for a stream-mapped dictionary, or lets the factory decide. |

### Attachments

| Type | Kind | Purpose |
|---|---|---|
| [`BTreeLookupStorageAttachment<TKey, TValue>`](Attachments/BTreeLookupStorageAttachment.cs) | class | Attachment that exposes a stream-mapped B-tree lookup on its reserved stream as an `IBTreeLookup<TKey, TValue>`. |
| [`BTreeLookupStorageAttachmentBase<TKey, TValue>`](Attachments/BTreeLookupStorageAttachmentBase.cs) | abstract class | Base that hosts a `StreamMappedBTreeLookup<TKey, TValue>` on one reserved stream according to a `BTreeSpec`. |
| [`BTreeStorageAttachment<TKey, TValue>`](Attachments/BTreeStorageAttachment.cs) | class | Attachment that exposes a stream-mapped B-tree on its reserved stream as an `IDictionary<TKey, TValue>`. |
| [`BTreeStorageAttachmentBase<TKey, TValue>`](Attachments/BTreeStorageAttachmentBase.cs) | abstract class | Base that hosts a configured stream-mapped B-tree on one reserved stream according to a `BTreeSpec`. |
| [`ClusteredStreamsAttachmentDecorator`](Attachments/ClusteredStreamsAttachmentDecorator.cs) | class | Decorator over any `IClusteredStreamsAttachment`. |
| [`ClusteredStreamsAttachmentDecorator<TInner>`](Attachments/ClusteredStreamsAttachmentDecorator.cs) | class | Decorator base that forwards every attachment member to a typed inner attachment. |
| [`CompositeStorageAttachmentBase`](Attachments/CompositeStorageAttachmentBase.cs) | class | Attachment that groups child attachments into one unit and declares no reserved streams of its own. |
| [`IClusteredStreamsAttachment`](Attachments/IClusteredStreamsAttachment.cs) | interface | Contract for a component bound to reserved streams, with its ID, stream count, attach, detach, flush and integrity check. |
| [`IndexStorageAttachment<TData>`](Attachments/IndexStorageAttachment.cs) | class | Index from a value to the positions that hold it, persisted as a paged list and a stream-mapped B-tree lookup. |
| [`InMemoryIndexStorageAttachment<TData>`](Attachments/InMemoryIndexStorageAttachment.cs) | class | Index from a value to its positions that persists values in a paged list and rebuilds the lookup in memory on attach. |
| [`InMemoryUniqueKeyStorageAttachment<TKey>`](Attachments/InMemoryUniqueKeyStorageAttachment.cs) | class | Unique key-to-position map that persists keys in a paged list and rebuilds the dictionary in memory on attach. |
| [`PagedListStorageAttachment<TData>`](Attachments/PagedListStorageAttachment.cs) | class | Attachment that exposes a stream paged list on its reserved stream as an `IExtendedList<TData>`. |
| [`PagedListStorageAttachmentBase<TData>`](Attachments/PagedListStorageAttachmentBase.cs) | abstract class | Base that hosts a `StreamPagedList<TData>` with a datum serializer on one reserved stream. |
| [`StackStorageAttachment<TData>`](Attachments/StackStorageAttachment.cs) | class | Attachment that exposes a stream paged list on its reserved stream as an `IStack<TData>`. |
| [`UniqueKeyStorageAttachment<TKey>`](Attachments/UniqueKeyStorageAttachment.cs) | class | Unique key-to-position map persisted as a paged list and a stream-mapped B-tree. |

### ClusterMap

| Type | Kind | Purpose |
|---|---|---|
| [`ClusterMap`](ClusterMap/ClusterMap.cs) | abstract class | Allocates, links, appends and removes cluster chains and raises `Changed`, without thread safety of its own. |
| [`ClusterMapChangedEventArgs`](ClusterMap/ClusterMapChangedEventArgs.cs) | class | Describes one cluster-map mutation, including the added, modified, removed and moved clusters and the chain's new terminals. |
| [`InMemoryClusterMap`](ClusterMap/InMemoryClusterMap.cs) | class | Cluster map over an in-memory `IExtendedList<Cluster>`. |
| [`StreamMappedClusterMap`](ClusterMap/StreamMappedClusterMap.cs) | class | Loadable cluster map stored as a stream paged list at an offset of the root stream, with optional caching of cluster headers. |
