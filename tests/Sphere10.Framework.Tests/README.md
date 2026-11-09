# Sphere10.Framework.Tests

`Sphere10.Framework.Tests` is the NUnit project for the core
[Sphere10.Framework](../../src/Sphere10.Framework/README.md) library. It covers
caches, clustered streams, collections, crypto, data sources, ObjectSpaces,
serialization, streams, scopes, threading and the smaller helper domains. The
project also references
[Sphere10.Framework.Communications](../../src/Sphere10.Framework.Communications/README.md),
[Sphere10.Framework.Application](../../src/Sphere10.Framework.Application/README.md)
and [Sphere10.Framework.NUnit](../../src/Sphere10.Framework.NUnit/README.md).
It loads [Sphere10.Framework.Generators](../../src/Sphere10.Framework.Generators/README.md)
as an analyzer for the generated test model. Folders mirror the source domains
they exercise.

## Run the tests

Run the commands from the repository root. The project targets
`$(FrameworkTargetFrameworks)`, which is `net10.0`, plus `net11.0` when the
building SDK supports .NET 11.

```powershell
dotnet test tests/Sphere10.Framework.Tests/Sphere10.Framework.Tests.csproj -c Release -p:BuildRevision=0
```

Run one fixture, or several fixtures joined with `|`:

```powershell
dotnet test tests/Sphere10.Framework.Tests/Sphere10.Framework.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~BufferedStreamTests"
dotnet test tests/Sphere10.Framework.Tests/Sphere10.Framework.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~CacheReapingTests|FullyQualifiedName~PooledCacheReaperTests"
```

Leave out the loopback SMTP and packaging-script categories:

```powershell
dotnet test tests/Sphere10.Framework.Tests/Sphere10.Framework.Tests.csproj -c Release -p:BuildRevision=0 --filter "TestCategory!=Integration&TestCategory!=Packaging"
```

Run the explicit file-backed `BufferedStream` measurements on one target:

```powershell
dotnet test tests/Sphere10.Framework.Tests/Sphere10.Framework.Tests.csproj -c Release -p:BuildRevision=0 -f net10.0 --filter "TestCategory=Performance"
```

Quote filter expressions so the shell does not interpret `|` or `&`. Add
`--list-tests` to list the discovered cases without running them.

| Group | Selection | Notes |
|---|---|---|
| Ordinary fixtures | Run by default | Most fixtures run their children in parallel. |
| `Integration` category | Run by default | `MailTlsIntegrationTests` only. It talks to an in-process loopback SMTP listener. |
| `Packaging` category | Run by default | `PackageScriptTests` only. It needs PowerShell, as described in [Requirements](#requirements). |
| `Performance` category | `[Explicit]`, so a filter must select it | `BufferedStreamPerformanceTests` only. It writes JSON and CSV results to a new folder under the temporary directory, or to the `BufferedStreamPerformanceDirectory` test parameter. |
| Ignored | Never run | `FastMarshalStructSerializerTests`, one `StringFormatterTests` case and one `ScopeAllocationDiagnostics` case. |

## Requirements

- A .NET 10 SDK. The tests need no interactive desktop, browser or database server.
- Many fixtures create files and folders under the operating system's temporary directory and delete them afterwards.
- `PackageScriptTests` runs `scripts/pack.ps1` and `scripts/publish.ps1`, which the project copies to `Packaging/` in the output folder. It needs `pwsh` on `PATH`, and some cases also run Windows PowerShell (`powershell`). A case whose executable is missing is ignored. A stub replaces `dotnet`, so no package is built or pushed.
- `MailTlsIntegrationTests` temporarily changes the process-wide SMTP certificate policy, so the fixture is non-parallel.
- `NetworkDiagnosticsTests` resolves `localhost` and traces the route to `127.0.0.1`. `MacAddressTests` reads the local network interfaces. Neither sends public network traffic.
- `ProcessSentryTests` starts `cmd.exe` on Windows and `/bin/sh` elsewhere.
- `TextFileCacheTests`, `SchedulerTest` and `SchedulerJobLifecycleTests` are skipped on GitHub Actions.
- `UrlIDTests` permutes ten million identifiers and takes longer than other fixtures.

## Contents

### Project root

The root holds a protocol queue fixture and an internal NUnit values attribute
(`StandardSphere10CHFValues`) that supplies the standard hash functions to other fixtures.

| Fixture | Purpose |
|---|---|
| [ProtocolOrchestratorQueueTests](ProtocolOrchestratorQueueTests.cs) | Outgoing messages wait for orchestrator startup and keep their order, and a failing message reports its envelope while the queue continues. |

### Cache

Tests for the [cache domain](../../src/Sphere10.Framework/Cache).

| Fixture | Purpose |
|---|---|
| [BulkFetchCacheTests](Cache/BulkFetchCacheTests.cs) | `BulkFetchActionCache` batch loads, stale snapshot refresh, fetch-once mode, failed forced refresh, preloaded sources and reap requests. |
| [CacheEvictionPreparationTests](Cache/CacheEvictionPreparationTests.cs) | Eviction preparation on `CacheBase` with failure and retry, reentrant remove, purge and admission, disposal during preparation, pins and a shared reaper budget. |
| [CacheFetchTimestampTests](Cache/CacheFetchTimestampTests.cs) | Fetched and last-access timestamps start after the fetch completes. |
| [CacheProjectionTests](Cache/CacheInterfaceDefaultsTests.cs) | Typed-only cache contracts, object and arbitrary projections, conversion directions, event forwarding and projection disposal. |
| [CacheNotificationTests](Cache/CacheNotificationTests.cs) | Each notification hook runs once before its event, and removing-event failures keep membership and aggregate on disposal. |
| [CacheReapingTests](Cache/CacheReapingTests.cs) | Reap levels, capacity-pressure batches, size-based reaping, reap policies, pinned and expired entries, replacement sizing, removal callbacks and concurrent misses. |
| [CacheTypedEventTests](Cache/CacheTypedEventTests.cs) | Typed and object event multicast, contravariant handlers, live entry views, disposable values and null-value policies. |
| [FileCacheSourceVersionTests](Cache/FileCacheSourceVersionTests.cs) | `TextFileCache` reloads by source version, retains deleted sources and handles `Set` on missing files. |
| [GeneralCacheTests](Cache/GeneralCacheTests.cs) | `ActionCache` capacity, removal events, bulk fetch, expiration, sizing and membership. |
| [PooledCacheReaperTests](Cache/PooledCacheReaperTests.cs) | `IsolatedCacheReaper` and `PooledCacheReaper` registration, reference-identity membership, combined capacity, pins, deregistration, disposal and concurrent members. |
| [PooledCacheTests](Cache/PooledCacheTests.cs) | Several `ActionCache` instances sharing one `PooledCacheReaper`, including exhaustion and an integration run. |
| [SessionCacheTests](Cache/SessionCacheTests.cs) | `SessionCache` item lifetime, session disposal and flush, and expiry. |
| [TextFileCacheTests](Cache/TextFileCacheTests.cs) | `TextFileCache` stale-file reload and the retain-on-delete option. |

### ClusteredStreams

Tests for the [clustered stream container](../../src/Sphere10.Framework/ClusteredStreams).
Two internal values attributes supply the policy and storage-type matrices.

| Fixture | Purpose |
|---|---|
| [ClusteredStreamOwnershipTests](ClusteredStreams/ClusteredStreamOwnershipTests.cs) | Close and finalizer ownership of opened streams, retry after failed finalizers or descriptor writes, and `ObjectStream` stream return. |
| [ClusteredStreamsConcurrentStreamsTests](ClusteredStreams/ClusteredStreamsConcurrentStreamsTests.cs) | Which reads, writes, adds, inserts, removes and clears are allowed while another stream is open. |
| [ClusteredStreamsMetaDataTests](ClusteredStreams/ClusteredStreamsMetaDataTests.cs) | Opaque metadata allocation, padding, reopening, rollback and legacy storage without metadata. |
| [ClusteredStreamsTests](ClusteredStreams/ClusteredStreamsTests.cs) | Add, insert, remove, update and clear, structure checks, corrupt-data detection and integration runs across storage types and policies. |
| [StreamContainerTestsHelper](ClusteredStreams/ClusteredStreamsTests.cs) | Static assertions that check stream descriptors and cluster chain consistency. |
| [ClusterMapChangedEventArgsTests](ClusteredStreams/ClusterMapChangedEventArgsTests.cs) | `ClusterMapChangedEventArgs` collection ownership, move and terminal tracking, and allocation. |
| [ClusterMapTests](ClusteredStreams/ClusterMapTests.cs) | `InMemoryClusterMap` and `StreamMappedClusterMap` chain allocation, expansion and removal, caller-storage reads and truncated reads. |
| [ClusterSeekerTests](ClusteredStreams/ClusterSeekerTests.cs) | `ClusterSeeker` pointer validation, shortest paths, checkpoints, cycle rejection, cache invalidation and allocation. |
| [NestedClusteredStreamsTests](ClusteredStreams/NestedClusteredStreamsTests.cs) | A clustered container stored inside a stream of another container, including reload and clear rules. |
| [ParallelClusteredStreamsTests](ClusteredStreams/ParallelClusteredStreamsTests.cs) | Parallel add, insert, remove and read, and lock retention by an opened stream. |

### Collections

Shared collection tests for the [collections domain](../../src/Sphere10.Framework/Collections).
`StreamPersistedTestsBase.cs` is commented out.

| Fixture | Purpose |
|---|---|
| [DefaultCollectionInterfaceTests](Collections/DefaultCollectionInterfaceTests.cs) | Default members of the extended collection, list, stack, queue and dictionary-list contracts dispatch to the long-indexed primitives, including remapped subclasses. |
| [StreamPersistedCollectionTestsBase](Collections/StreamPersistedCollectionTestsBase.cs) | Base class that creates memory, file-mapped and transactional backing streams for stream-persisted collection fixtures. |

### Collections/BTree

Tests for the B-tree, B+ tree and lookup implementations.

| Fixture | Purpose |
|---|---|
| [BTreeBaseTests](Collections/BTree/BTreeBaseTest.cs) | Abstract dictionary contract suite for the B-tree family, covering mutation, lookup, traversal orders and ordering. |
| [BTreeTests](Collections/BTree/BTreeTest.cs) | Abstract B-tree harness over `BTreeBaseTests`. |
| [BTreePlusTests](Collections/BTree/BTreePlusTest.cs) | Abstract B+ tree harness over `BTreeBaseTests`. |
| [InMemoryBTreeTests](Collections/BTree/InMemoryBTreeTest.cs) | Runs the B-tree suite on `InMemoryBTree`. |
| [InMemoryBTreePlusTests](Collections/BTree/InMemoryBTreePlusTest.cs) | Runs the B+ tree suite on `InMemoryBTreePlus`. |
| [StreamMappedBTreeTests](Collections/BTree/StreamMappedBTreeTest.cs) | Runs the B-tree suite on `StreamMappedBTree` and skips variable-size serializer combinations. |
| [StreamMappedBTreePlusTests](Collections/BTree/StreamMappedBTreePlusTest.cs) | Runs the B+ tree suite on `StreamMappedBTreePlus` and skips variable-size serializer combinations. |
| [BTreeDefaultOrderTests](Collections/BTree/BTreeDefaultOrderTests.cs) | Default orders and factories, reopen of legacy explicit orders, and rejection of invalid headers without mutation. |
| [BTreeDefaultReopenTransitionsTests](Collections/BTree/BTreeDefaultReopenTransitionsTests.cs) | Explicit reopen after split, merge, empty and reinserted roots. |
| [BTreeFindRangeTests](Collections/BTree/BTreeFindRangeTests.cs) | `FindRange` bounds, overlaps and ordering on in-memory and stream-mapped trees. |
| [BTreeInterfaceTests](Collections/BTree/BTreeInterfaceTests.cs) | The shared dictionary contract, inclusive ranges, lookups through the tree interface, and three- and four-component compound keys across reopen. |
| [BTreeLookupTests](Collections/BTree/BTreeLookupTests.cs) | `InMemoryBTreeLookup` and `StreamMappedBTreeLookup` multi-value add, remove, grouping and persistence. |
| [BTreePlusRemovalRegressionTests](Collections/BTree/BTreePlusRemovalRegressionTests.cs) | B+ tree separator repair on removal, root collapse and reopen against a sorted-dictionary oracle. |
| [BTreeSpecTests](Collections/BTree/BTreeSpecTests.cs) | `BTreeSpec` defaults, validation and JSON round trips. |
| [BTreeTypeTests](Collections/BTree/BTreeTypeTests.cs) | Lookup storage selected by `BTreeSpec`, reserved header bytes, historical types, search without value reads and derived-tree hooks. |
| [StreamMappedBTreeSpanTests](Collections/BTree/StreamMappedBTreeSpanTests.cs) | Short span reads, retry after serializer failures and protection against partial rewrites of truncated nodes. |

### Collections/Buffer

Tests for byte buffers.

| Fixture | Purpose |
|---|---|
| [BufferSpanTests](Collections/Buffer/BufferSpanTests.cs) | Span reads and writes on memory, paged, stream-mapped and transactional buffers, adapters and decorators, including caller-storage reads and allocations. |
| [BufferTests](Collections/Buffer/BufferTests.cs) | Page, removal, rewind and integration cases across the buffer storage types. |
| [FileMappedBufferTests](Collections/Buffer/FileMappedBufferTests.cs) | `FileMappedBuffer` save, load, growth, partial pages and read-only mode. |
| [MemoryBufferTests](Collections/Buffer/MemoryBufferTests.cs) | `MemoryBuffer` search, aliased insertion, enumerator invalidation, `CopyTo` and allocations. |
| [MerklePagedBufferTest](Collections/Buffer/MerklePagedBufferTest.cs) | `MerklePagedBuffer` roots against `SimpleMerkleTree` as pages are added and updated. |
| [PagedBufferImplementationHelperTests](Collections/Buffer/PagedBufferImplementationHelperTests.cs) | A `MemoryPagedBuffer.ReadSpan` check whose method has no `[Test]` attribute, so NUnit discovers no case. |

### Collections/Dictionaries

Tests for dictionary types and decorators.

| Fixture | Purpose |
|---|---|
| [ActionDictionaryScopeTests](Collections/Dictionaries/ActionDictionaryScopeTests.cs) | `ActionDictionaryDecorator` entry and exit callbacks around operations and enumerators, with failure and retry. |
| [BijectiveDictionaryTests](Collections/Dictionaries/BijectiveDictionaryTests.cs) | `BijectiveDictionary` add, remove and update through either direction. |
| [DictionaryListTests](Collections/Dictionaries/DictionaryListTests.cs) | `DictionaryList` walkthrough and integration. |
| [MultiKeyDictionaryTest](Collections/Dictionaries/MultiKeyDictionaryTest.cs) | `EnumerableKeyDictionary` lookups with object keys of different numeric types. |
| [ReferenceDictionaryTests](Collections/Dictionaries/ReferenceDictionaryTests.cs) | `ReferenceDictionary` add and remove by reference identity. |

### Collections/Graph

Tests for graph nodes.

| Fixture | Purpose |
|---|---|
| [GraphNodeTests](Collections/Graph/GraphNodeTests.cs) | `GraphNode` construction and visits with shared nodes, cycles, pruning, lazy enumeration, cleanup on failure and deep graphs. |

### Collections/Lists

Tests for extended and adapted lists.

| Fixture | Purpose |
|---|---|
| [ColumnarListTests](Collections/Lists/ColumnarListTests.cs) | `ColumnarList` integration run. |
| [ExtendedListMemoryTests](Collections/Lists/ExtendedListMemoryTests.cs) | `ExtendedList` releases references on clear and range removal. |
| [ExtendedListSpanTests](Collections/Lists/ExtendedListSpanTests.cs) | `ExtendedList` span reads and writes, aliased sources, enumeration checks, derived overrides and allocations. |
| [ExtendedListTests](Collections/Lists/ExtendedListTests.cs) | `ExtendedList` integration run. |
| [ListAdapterTests](Collections/Lists/ListAdapterTests.cs) | `ExtendedListAdapter` integration run. |
| [RecyclableListAdapterTests](Collections/Lists/RecyclableListAdapterTests.cs) | Runs the recyclable list suite on `RecyclableListAdapter`. |
| [RecyclableListTestsBase](Collections/Lists/RecyclableListTestsBase.cs) | Abstract walkthrough of the `IRecyclableList<T>` contract. |
| [SortedListTests](Collections/Lists/SortedListTests.cs) | `SortedList` ascending and descending ordering. |
| [UpdateOnlyListTests](Collections/Lists/UpdateOnlyListTests.cs) | `UpdateOnlyList` add, insert and remove under the fixed, by-block and minimum-required preallocation policies. |

### Collections/Observable

Tests for observable collections.

| Fixture | Purpose |
|---|---|
| [ObservableCallArgumentsTests](Collections/Observable/ObservableCallArgumentsTests.cs) | Operation arguments delivered by observable list and dictionary notifications, including replacement, reentrancy and suppression. |

### Collections/Other

Tests for other collection helpers.

| Fixture | Purpose |
|---|---|
| [BitVectorTests](Collections/Other/BitVectorTests.cs) | `BitVector` range insert, read, search, remove and update. |
| [EnumerableExtensionTests](Collections/Other/EnumerableExtensionTests.cs) | `OrderByAll` and `PartitionBySize` enumerable extensions. |

### Collections/Paged

Tests for paged lists.

| Fixture | Purpose |
|---|---|
| [MemoryPagedListCacheTests](Collections/Paged/MemoryPagedListCacheTests.cs) | `MemoryPagedList` page cache budgets, dirty-page saves before reaping, save callbacks and retry. |
| [MemoryPagedListTests](Collections/Paged/MemoryPagedListTests.cs) | `MemoryPagedList` sizing, lazy pages, iteration checks, random access and growth. |
| [ValuePageScopeTests](Collections/Paged/ValuePageScopeTests.cs) | `PageScope` pins, nesting, enumeration release and cleanup retry. |

### Collections/Queue

Tests for queues.

| Fixture | Purpose |
|---|---|
| [ProducerConsumerQueueTest](Collections/Queue/ProducerConsumerQueueTest.cs) | `ProducerConsumerQueue` producer and consumer runs, including a semaphore regression. |

### Collections/Sets

Tests for sets and Bloom filters. `SetTestsBase` is also used by the stream-mapped and transactional set fixtures.

| Fixture | Purpose |
|---|---|
| [BloomFilterTests](Collections/Sets/BloomFilterTests.cs) | `HashedBloomFilter` and `MurMur3BloomFilter` consistency and absence of false negatives. |
| [HashSetTests](Collections/Sets/HashSetTests.cs) | Runs the set suite on the .NET `HashSet<T>` as a baseline. |
| [ReferenceHashSetTests](Collections/Sets/ReferenceHashSetTests.cs) | `ReferenceHashSet` add and remove by reference identity. |
| [SetTestsBase](Collections/Sets/SetTestsBase.cs) | Abstract `ISet<T>` suite for membership, enumeration and set algebra. |

### Collections/StreamMapped

Tests for stream-mapped collections. An internal serializer (`BrokenConstantLengthSerializer`) simulates a constant-length serializer that writes the wrong size.

| Fixture | Purpose |
|---|---|
| [StreamMappedBufferTests](Collections/StreamMapped/StreamMappedBufferTests.cs) | `StreamMappedBuffer` insert, remove and integration. |
| [StreamMappedDictionaryCLKTests](Collections/StreamMapped/StreamMappedDictionaryCLKTests.cs) | Runs the dictionary suite on a constant-length-key dictionary and checks its header. |
| [StreamMappedDictionaryTests](Collections/StreamMapped/StreamMappedDictionaryTests.cs) | Runs the dictionary suite on a key-value-pair dictionary and checks keys with colliding checksums. |
| [StreamMappedDictionaryTestsBase](Collections/StreamMapped/StreamMappedDictionaryTestsBase.cs) | Abstract stream-mapped dictionary suite for add, record reuse, membership, removal and integration. |
| [StreamMappedHashSetTests](Collections/StreamMapped/StreamMappedHashSetTests.cs) | Runs the set suite on a stream-mapped hash set. |
| [StreamMappedListTests](Collections/StreamMapped/StreamMappedListTests.cs) | Stream-mapped list ranges, argument validation, null and empty items, reload and integration. |
| [StreamMappedListTestsBase](Collections/StreamMapped/StreamMappedListTestsBase.cs) | Abstract list suite with two add cases. No fixture derives from it. |
| [StreamMappedRecyclableListTests](Collections/StreamMapped/StreamMappedRecyclableListTests.cs) | Runs the recyclable list suite on a stream-mapped recyclable list. |
| [StreamMappedRecyclableListWithIndexTests](Collections/StreamMapped/StreamMappedRecyclableListWithIndexTests.cs) | Runs the recyclable list suite on a stream-mapped recyclable list with a checksum index. |

### Collections/StreamMappedMerkle

Tests for Merkle-tracked stream-mapped collections.

| Fixture | Purpose |
|---|---|
| [StreamMappedHashMerkleSetTests](Collections/StreamMappedMerkle/StreamMappedHashSetTests.cs) | Runs the set suite on `StreamMappedMerkleHashSet`. |
| [StreamMappedMerkleDictionaryCLKTests](Collections/StreamMappedMerkle/StreamMappedMerkleDictionaryCLKTests.cs) | Runs the Merkle dictionary suite with constant-length keys and checks static and dynamic key consistency. |
| [StreamMappedMerkleDictionaryTests](Collections/StreamMappedMerkle/StreamMappedMerkleDictionaryTests.cs) | Runs the Merkle dictionary suite on the key-value-list implementation and checks its header. |
| [StreamMappedMerkleDictionaryTestsBase](Collections/StreamMappedMerkle/StreamMappedMerkleDictionaryTestsBase.cs) | Abstract Merkle dictionary suite for null and empty values, membership, removal and roots. |
| [StreamMappedMerkleListTests](Collections/StreamMappedMerkle/StreamMappedMerkleListTests.cs) | Runs the Merkle list suite on `StreamMappedMerkleList` and checks lazy root flushing to the header. |
| [StreamMappedMerkleRecyclableListTests](Collections/StreamMappedMerkle/StreamMappedMerkleRecyclableListTests.cs) | Runs the recyclable list suite on `StreamMappedMerkleRecyclableList` and checks its Merkle tree. |
| [StreamMappedMerkleRecyclableListWithIndexTests](Collections/StreamMappedMerkle/StreamMappedRecyclableListWithIndexTests.cs) | The same suite with a checksum index. |

### Collections/StreamPaged

Tests for stream-paged lists. `StreamPagedListTests_Bug1.cs` is commented out.

| Fixture | Purpose |
|---|---|
| [StreamPagedListTests](Collections/StreamPaged/StreamPagedListTests.cs) | `StreamPagedList` add, update, read, fixed-size pages, raw item reads and integration across storage types. |
| [StreamPagedSpanTests](Collections/StreamPaged/StreamPagedSpanTests.cs) | Caller-storage reads, short reads, page-local indexing, allocations and repeated page-scope disposal. |

### Collections/Transactional

Tests for file transactions and transactional collections.

| Fixture | Purpose |
|---|---|
| [FileTransactionScopeTests](Collections/Transactional/FileTransactionScopeTests.cs) | `FileTransactionScope` enlistment, nested commit and rollback, and simultaneous transactions. |
| [FileTransactionTests](Collections/Transactional/FileTransactionTests.cs) | `FileTransaction` commit and rollback sequences, duplicate enlistment and resume after interruption. |
| [TransactionalDictionaryCLKTests](Collections/Transactional/TransactionalDictionaryCLKTests.cs) | Runs the transactional dictionary suite with constant-length keys and checks a broken key serializer. |
| [TransactionalDictionaryTests](Collections/Transactional/TransactionalDictionaryTests.cs) | Runs the transactional dictionary suite on the key-value-list implementation. |
| [TransactionalDictionaryTestsBase](Collections/Transactional/TransactionalDictionaryTestsBase.cs) | Abstract `TransactionalDictionary` suite for CRUD and committed, rolled-back and abandoned state. |
| [TransactionalFileMappedBufferTests](Collections/Transactional/TransactionalFileMappedBufferTests.cs) | `TransactionalFileMappedBuffer` page edits and deletes, markers, commit, rollback, resume and events. |
| [TransactionalHashSetTests](Collections/Transactional/TransactionalHashSetTests.cs) | Runs the set suite on `TransactionalHashSet`. |
| [TransactionalListTests](Collections/Transactional/TransactionalListTests.cs) | `TransactionalList` commit, rollback and reload of earlier states. |
| [TransactionalMarkerMemoryTests](Collections/Transactional/TransactionalMarkerMemoryTests.cs) | `MarkerRepository` file identifiers, sparse page markers and recovery. |
| [TransactionalPageBufferStorageTests](Collections/Transactional/TransactionalPageBufferStorageTests.cs) | Transactional page storage arrays, eviction, flush failures, reentrant loads and rollback. This fixture is non-parallel. |

### Comparers

Tests for the [comparers domain](../../src/Sphere10.Framework/Comparers).

| Fixture | Purpose |
|---|---|
| [ByteArrayComparerTests](Comparers/ByteArrayComparerTests.cs) | `ByteArrayComparer` ordering of null, empty and unequal arrays. |
| [ByteArrayEqualityComparerTests](Comparers/ByteArrayEqualityComparerTests.cs) | `ByteArrayEqualityComparer` equality of null, empty and different-length arrays. |
| [ComparerBuilderTests](Comparers/ComparerBuilderTests.cs) | `ComparerBuilder` and `EqualityComparerBuilder` composition. |
| [PackedComparerTests](Comparers/PackedComparerTests.cs) | `PackedComparer` pack, unpack, consistency and wrong-type failures. |
| [PackedEqualityComparerTests](Comparers/PackedEqualityComparerTests.cs) | `PackedEqualityComparer` pack, unpack, equality, hash codes and wrong-type failures. |

### ComponentModel

Tests for the [component model domain](../../src/Sphere10.Framework/ComponentModel).

| Fixture | Purpose |
|---|---|
| [BoundListEventTests](ComponentModel/BoundListEventTests.cs) | `BoundList` typed deletion notifications and their binding projection. |
| [DevAgeCoreReuseTests](ComponentModel/DevAgeCoreReuseTests.cs) | Shared activation, validation, comparison and number and percent conversion used by the imported binding helpers. |
| [ExtractedComponentModelTests](ComponentModel/ExtractedComponentModelTests.cs) | Calendar differences, type conversion, validators and bound-list editing. |

### Crypto

Tests for the [crypto domain](../../src/Sphere10.Framework/Crypto). `SubRootTreeTests.cs` is commented out.

| Fixture | Purpose |
|---|---|
| [CryptoSpanTests](Crypto/CryptoSpanTests.cs) | Span hashing and iteration, Blake2b digest reuse, WOTS and AMS vectors, Merkle proofs, secure erase and allocations. |
| [CryptoToolTests](Crypto/CryptoToolTests.cs) | `Tools.Crypto.FixedTimeEquals` compares whole sequences. |
| [EncryptedTests](Crypto/EncryptedTests.cs) | `Encrypted<T>` round trips, tamper rejection, value replacement and binary serializer use. |
| [HashRandomTests](Crypto/HashRandomTests.cs) | `HashRandom` seeds, determinism, output distribution, request sizes and thread safety. |
| [HasherScopeTests](Crypto/HasherScopeTests.cs) | Borrowed hasher scopes reset and return their hasher once. This fixture is non-parallel. |
| [HashersTests](Crypto/HashersTests.cs) | `Hashers.Aggregate` over empty and populated inputs. |
| [LegacyDesTests](Crypto/LegacyDesTests.cs) | DES text encryption round trip through `Tools.Crypto`. |
| [RaceConditionTests](Crypto/RaceConditionTests.cs) | Parallel borrowed hashers return correct digests for each standard hash function. |
| [SeedGeneratorTests](Crypto/SeedGeneratorTests.cs) | `SeedGenerator` entropy policies, digest lengths and span overloads. |
| [TypedCryptoInterfaceTests](Crypto/TypedCryptoInterfaceTests.cs) | Typed and base digital signature scheme contracts and configuration cloning. |
| [WAMSTests](Crypto/WAMSTests.cs) | `WAMS` and `WAMSSharp` signing, verification and key checks. |
| [WOTSSharpTests](Crypto/WOTSSharpTests.cs) | `WOTSSharp` signing and verification. |
| [WOTSTests](Crypto/WOTSTests.cs) | `WOTS` signature consistency, digests and verification. |

### DataSource

Tests for the [data source domain](../../src/Sphere10.Framework/DataSource).

| Fixture | Purpose |
|---|---|

### DateTime

Tests for date and time extensions.

| Fixture | Purpose |
|---|---|
| [ApproxEqual](DateTime/ApproxEqual.cs) | `DateTime.ApproxEqual` within and beyond a tolerance. |
| [DateTimeTests](DateTime/FormatExTests.cs) | Next day-of-week and day-of-month calculations. |

### Encoding

Tests for the [encoding domain](../../src/Sphere10.Framework/Encoding).

| Fixture | Purpose |
|---|---|
| [Base32EncodingTests](Encoding/Base32EncodingTests.cs) | `Base32Encoding` round trips of random data. |

### Environment

Tests for the [environment domain](../../src/Sphere10.Framework/Environment). `CommandLineArgsTests.cs` is commented out.

| Fixture | Purpose |
|---|---|
| [ProcessSentryTests](Environment/ProcessSentryTests.cs) | `ProcessSentry` working directory, captured output, exit codes, cancellation and borrowed writers. |

### Exceptions

Tests for the [exceptions domain](../../src/Sphere10.Framework/Exceptions).

| Fixture | Purpose |
|---|---|
| [ExceptionToolTests](Exceptions/ExceptionToolTests.cs) | `Tools.Exceptions` synchronous and asynchronous capture, filters, cancellation and legacy handlers. |
| [GuardInterpolationTests](Exceptions/GuardInterpolationTests.cs) | `Guard` interpolated messages evaluate once, format only on failure and do not allocate when passing. |

### Extensions

Tests for the [extensions domain](../../src/Sphere10.Framework/Extensions).

| Fixture | Purpose |
|---|---|
| [DebugExtensionsTests](Extensions/DebugExtensionsTests.cs) | Main-thread debug assertions and data source work kept off the captured main thread. This fixture is non-parallel. |
| [GraphTraversalTests](Extensions/GraphTraversalTests.cs) | Depth-first `Visit` order, shared visited state, pruning, lazy enumeration and cleanup. |

### FileSystem

Tests for `Tools.FileSystem`.

| Fixture | Purpose |
|---|---|
| [FileSystemTests](FileSystem/FileSystemTests.cs) | `DetermineAvailableFileName` choices. |
| [FileSystemWriteAllBytesTests](FileSystem/FileSystemWriteAllBytesTests.cs) | `WriteAllBytes` creation, replacement and borrowed input streams. |

### Filter

Tests for the [filter domain](../../src/Sphere10.Framework/Filter).

| Fixture | Purpose |
|---|---|
| [FilterTests](Filter/FilterTests.cs) | Filter operators, groups, value conversion, sorting, paging and `FilterBuilder` over in-memory rows. |

### IO

Tests for the [IO domain](../../src/Sphere10.Framework/IO).

| Fixture | Purpose |
|---|---|
| [EndianBinaryOwnershipTests](IO/EndianBinaryOwnershipTests.cs) | `EndianBinaryReader` and `EndianBinaryWriter` close or borrow the underlying stream. |
| [EndianCharSpanTests](IO/EndianCharSpanTests.cs) | Fixed-size character span writes, padding, encoding overflow and disposal. |

### Iterator

Tests for iterator helpers.

| Fixture | Purpose |
|---|---|
| [AppendIteratorTests](Iterator/AppendIteratorTests.cs) | Single-item `Union` and `Concat` extensions. |
| [MemorizingIteratorTests](Iterator/MemorizingIteratorTests.cs) | `MemorizingIterator` lazy caching, independent cursors, collection members and replayed failures. |
| [ScopeEnumeratorTests](Iterator/ScopeEnumeratorTests.cs) | Enumerators that release a scope on disposal, with retry after failure. |

### Keys

Tests for the [keys domain](../../src/Sphere10.Framework/Keys).

| Fixture | Purpose |
|---|---|
| [CompoundKeyTests](Keys/CompoundKeyTests.cs) | `CompoundKey` factories, equality, operators and `CompoundKeyComparer` ordering. |

### Logging

Tests for the [logging domain](../../src/Sphere10.Framework/Logging).

| Fixture | Purpose |
|---|---|
| [RollingFileLoggerTests](Logging/RollingFileLoggerTests.cs) | `RollingFileLogger` rolling, retention, newline counting, UTF-8 clipping and encodings. |

### Maths

Tests for the [maths domain](../../src/Sphere10.Framework/Maths).

| Fixture | Purpose |
|---|---|
| [StatisticsTests](Maths/StatisticsTests.cs) | `Statistics` population variance over long runs. |

### Memory

Tests for the [memory domain](../../src/Sphere10.Framework/Memory).

| Fixture | Purpose |
|---|---|
| [MemoryMetricTests](Memory/MemoryMetricTests.cs) | `Tools.Memory.ConvertMemoryMetric` unit conversions. |
| [MemoryToolTests](Memory/MemoryTool.cs) | `Tools.Memory.IsSerializationPrimitive` does not treat `DateTime` as a primitive. |

### Merkle

Tests for the [Merkle domain](../../src/Sphere10.Framework/Merkle). `MerkleListTestsBase` is also used by the stream-mapped Merkle list fixture.

| Fixture | Purpose |
|---|---|
| [FlatMerkleTreeRegressionTests](Merkle/FlatMerkleTreeRegressionTests.cs) | `FlatMerkleTree` roots after mutations, detached returned roots and retry after a failed write. This fixture is non-parallel. |
| [FlatMerkleTreeTests](Merkle/FlatMerkleTreeTests.cs) | `FlatMerkleTree` against `SimpleMerkleTree` for add, insert, remove, node access and empty roots. |
| [LongMerkleTreeTests](Merkle/LongMerkleTreeTests.cs) | `LongMerkleTree` against `SimpleMerkleTree` for add and node access. |
| [MerkleListAdapterTests](Merkle/MerkleListAdapterTests.cs) | Runs the Merkle list suite on `MerkleListAdapter`. |
| [MerkleListTestsBase](Merkle/MerkleListTestsBase.cs) | Abstract Merkle list suite for roots, null and empty items, inserts and integration. |
| [MerkleMathTests](Merkle/MerkleMathTests.cs) | `MerkleMath` heights, level lengths and node traits. |
| [MerkleProofTests](Merkle/MerkleProofTests.cs) | Existence, consistency and update proofs. |
| [MerkleRootPublicationTests](Merkle/MerkleRootPublicationTests.cs) | `MerkleTreeStorageAttachment` root flushing, change notifications, header caching and reopen. |
| [MerkleTreeTests](Merkle/MerkleTreeTests.cs) | Shared root and proof checks across the Merkle tree implementations. |
| [SimpleMerkleTreeTests](Merkle/SimpleMerkleTreeTests.cs) | `SimpleMerkleTree` add, insert, update and remove. |

### Misc

Tests for the [miscellaneous domain](../../src/Sphere10.Framework/Misc) and for .NET behaviour the Framework relies on.

| Fixture | Purpose |
|---|---|
| [BitsTests](Misc/BitsTests.cs) | `Bits` copy, set, read and number encoding. |
| [CompressionFactoryCompatibilityTests](Misc/CompressionFactoryCompatibilityTests.cs) | Default and legacy text and file compression pipelines interoperate, a historical compressed fixture still decrypts, and the default AES parameters are unchanged. |
| [MailTlsIntegrationTests](Misc/MailTlsIntegrationTests.cs) | `Tools.Mail` plain and STARTTLS delivery to a loopback server, certificate trust and borrowed content. |
| [MiscellaneousTests](Misc/MiscellaneousTests.cs) | Assorted Framework helpers and .NET stream, list, file and task behaviour. |
| [NetFrameworkStandardBehaviour](Misc/NetFrameworkStandardBehaviour.cs) | .NET type, stream, delegate, dictionary, file-mode and task behaviour the Framework assumes. |
| [RandomTests](Misc/RandomTests.cs) | `Random.NextRange` bounds. |
| [RecursiveDataTypeTests](Misc/RecursiveDataTypeTests.cs) | `RecursiveDataType` construction and flattening. |
| [ResultTests](Misc/ResultTests.cs) | `Result` casts and JSON round trips. |
| [RuntimeTests](Misc/RuntimeTests.cs) | `Tools.Runtime.IsInExceptionContext`. |
| [TypeExtensionTests](Misc/TypeExtensionTests.cs) | Cross-assembly type detection, partial generic definitions and C# type names. |
| [WarningRegressionTests](Misc/WarningRegressionTests.cs) | Legacy password derivation, encrypted and compressed text, query strings, URL slugs and span validation. |

### Network

Tests for the [network domain](../../src/Sphere10.Framework/Network).

| Fixture | Purpose |
|---|---|

### ObjectSpaces

Tests for the [ObjectSpaces domain](../../src/Sphere10.Framework/ObjectSpaces). Most fixtures run once per `TestTraits` combination through `TestsHelper`.

| Fixture | Purpose |
|---|---|
| [BuilderTests](ObjectSpaces/BuilderTests.cs) | `ObjectSpaceBuilder` conflicts between custom factories and registrations, and annotations. |
| [ConfigurationTests](ObjectSpaces/ConfigurationTests.cs) | Index orders, member indexes, `GetAll`, slot reuse and custom factories. |
| [DefaultsCompatibilityTests](ObjectSpaces/DefaultsCompatibilityTests.cs) | Default and historical layouts, deferred loads, builder reuse and reopen of memory and file stores. |
| [DimensionBuilderInterfaceTests](ObjectSpaces/DimensionBuilderInterfaceTests.cs) | Base and typed dimension builder contracts. |
| [FileMappedTests](ObjectSpaces/FileMappedTests.cs) | File-backed load, commit, rollback and clear. |
| [FlatStorageObjectSpaceTests](ObjectSpaces/FlatStorageObjectSpaceTests.cs) | Self-describing reconstruction, stored metadata, serializer and comparer rules, and storage lifetime. |
| [GarbageCollectionTests](ObjectSpaces/GarbageCollectionTests.cs) | Root dimensions, eager identifiers and collection of orphaned objects. |
| [GeneralTests](ObjectSpaces/GeneralTests.cs) | Construction, disposal, save, delete, clear and unique index rules. |
| [IndexBTreeSpecTests](ObjectSpaces/IndexBTreeSpecTests.cs) | Index `BTreeSpec` selection, builder independence, reopen and rollback. |
| [InstanceTrackerPerformanceTests](ObjectSpaces/InstanceTrackerPerformanceTests.cs) | `InstanceTracker` lookups, counts, provisional objects and allocations. |
| [MemoryMappedTests](ObjectSpaces/MemoryMappedTests.cs) | Memory-backed load of empty and populated stores. |
| [MerkleizedTests](ObjectSpaces/MerkleizedTests.cs) | Merkle roots change on save, and integrity checks reject corrupt roots. |
| [ObjectSpaceIntegrationTests](ObjectSpaces/ObjectSpaceIntegrationTests.cs) | CRUD and lookups over the SafeBox-style model with cross-dimension and polymorphic references. |
| [PersistenceIgnoranceTests](ObjectSpaces/PersistenceIgnoranceTests.cs) | Auto-save of changed objects without explicit saves. |
| [RehydrationTests](ObjectSpaces/RehydrationTests.cs) | The general cases repeated after reopening the store. |
| [SafeBoxTestHelper](ObjectSpaces/SafeBoxTestHelper.cs) | Static helper that builds an ObjectSpace over the SafeBox-style model. |
| [TestsHelper](ObjectSpaces/TestsHelper.cs) | Static helper that builds ObjectSpaces for each trait combination and creates sample objects. |
| [TestTraits](ObjectSpaces/TestTraits.cs) | Flags enum for memory, file, Merkle and auto-save configurations. |

### Packaging

Tests for the repository's [pack and publish scripts](../../scripts).

| Fixture | Purpose |
|---|---|
| [PackageScriptTests](Packaging/PackageScriptTests.cs) | `pack.ps1` and `publish.ps1` validation, package selection, symbols, API keys and failure handling. |

### Protocol

Tests for the [protocol domain](../../src/Sphere10.Framework/Protocol).

| Fixture | Purpose |
|---|---|
| [TypedProtocolAdapterTests](Protocol/TypedProtocolAdapterTests.cs) | Typed command, request, response, handshake and generator dispatch, and `ProtocolBuilder` composition. |

### Repository

The folder holds only `RepositoryDemoTest.cs`, which is commented out. It contains no fixture.

### Scheduler

Tests for the [scheduler domain](../../src/Sphere10.Framework/Scheduler).

| Fixture | Purpose |
|---|---|
| [JobScheduleInterfaceTests](Scheduler/JobScheduleInterfaceTests.cs) | `IJobSchedule` ordering by next start time and comparison rules. |

### Scopes

Tests for the [scopes domain](../../src/Sphere10.Framework/Scopes).

| Fixture | Purpose |
|---|---|
| [ActionScopeTests](Scopes/ActionScopeTests.cs) | `ActionScope` finalizers, async disposal and file and directory cleanup. |
| [AsyncTransactionalScopeBridgeTests](Scopes/AsyncTransactionalScopeBridgeTests.cs) | Synchronous members of async transactional scopes avoid the caller context and propagate hook failures. |
| [BorrowArrayScopeTests](Scopes/BorrowArrayScopeTests.cs) | `BorrowArrayScope` rental, return, clearing and failure handling. |
| [CallContextTests](Scopes/CallContextTests.cs) | `CallContext` slots, flow values and key search. |
| [ContextScopeLifecycleTests](Scopes/ContextScopeLifecycleTests.cs) | `ActionContextScope` and `TaskContextScope` ambient roots, nesting, disposal, failure and task isolation. |
| [ContextScopeSyncBridgeTests](Scopes/ContextScopeSyncBridgeTests.cs) | Synchronous disposal of async context scopes. |
| [ContextScopeTest](Scopes/ContextScopeTest.cs) | Context scope nesting policies on one or many threads and in async code. |
| [CoreScopesTests](Scopes/CoreScopesTests.cs) | `SyncScope`, `AsyncScope`, item and task scopes, faults, cancellation and value-task sources. |
| [DirectionalCallbackScopeTests](Scopes/DirectionalCallbackScopeTests.cs) | Synchronous and asynchronous callback scopes reject the wrong disposal direction. |
| [DisposableAsyncAdapterTests](Scopes/DisposableAsyncAdapterTests.cs) | Async disposal of synchronous resources, disposable bags and synchronized repositories. |
| [DisposableBaseTests](Scopes/DisposableBaseTests.cs) | `SyncDisposable` and `AsyncDisposable` finalize once, retry and use the right thread. |
| [ScopeAsyncAdapterTests](Scopes/ScopeAsyncAdapterTests.cs) | Async finalization of synchronous resource scopes. |
| [ScopeDecoratorConstructionTests](Scopes/ScopeDecoratorConstructionTests.cs) | The transactional scope decorator rejects a null scope. |
| [ScopeToolTests](Scopes/ScopeToolTests.cs) | `Tools.Scope` callback, lock, access, position, page, wait and hasher factories. |
| [TransactionalScopeInterfaceTests](Scopes/TransactionalScopeInterfaceTests.cs) | Transactional scope roots, nesting, voting, decorators and async adapters. |

### Serialization

Tests for the [serialization domain](../../src/Sphere10.Framework/Serialization).

| Fixture | Purpose |
|---|---|
| [AutoSizedSerializerTest](Serialization/AutoSizedSerializerTest.cs) | Auto-sized string serialization and an oversized byte descriptor. |
| [ConstantSizeCompoundKeySerializerTests](Serialization/ConstantSizeCompoundKeySerializerTests.cs) | `ConstantSizeCompoundKeySerializer` layouts and component validation. |
| [CyclicReferenceTests](Serialization/CyclicReferenceTests.cs) | Cyclic references and their calculated sizes. |
| [EmbeddedStorageFormatTests](Serialization/EmbeddedStorageFormatTests.cs) | Shared header and metadata blob layout of self-describing storage. |
| [FastMarshalStructSerializerTests](Serialization/FastMarshalStructSerializerTests.cs) | `FastMarshalStructSerializer` round trips. The fixture is ignored. |
| [FlatStorageMetaDataSerializerTests](Serialization/FlatStorageMetaDataSerializerTests.cs) | ObjectSpace and ObjectStream metadata codecs and legacy factory compatibility. |
| [FlatStorageObjectStreamTests](Serialization/FlatStorageObjectStreamTests.cs) | ObjectStream reconstruction from stored metadata and attachments. |
| [KeyValuePairSerializerTests](Serialization/KeyValuePairSerializerTests.cs) | `KeyValuePairSerializer` with empty and null values. |
| [NullableSerializerTests](Serialization/NullableSerializerTests.cs) | Nullable serialization of primitives, dates, enums and variable integers. |
| [PaddedSerializerTests](Serialization/PaddedSerializerTests.cs) | `PaddedSerializer` with an empty value. |
| [PaddingDiscardSpanTests](Serialization/PaddingDiscardSpanTests.cs) | Padding discard on short reads and end of stream. |
| [PolymorphicSerializerTests](Serialization/PolymorphicSerializerTests.cs) | Abstract, interface and list members serialized polymorphically. |
| [ReferenceSerializerTests](Serialization/ReferenceSerializerTests.cs) | Reference serializer context reuse and sealed members. |
| [SerializationAnnotationTests](Serialization/SerializationAnnotationTests.cs) | Transient, get-only, nullable and context-reference annotations. |
| [SerializationContextAllocationTests](Serialization/SerializationContextAllocationTests.cs) | `SerializationContext` reference tracking, allocations and finalization. |
| [SerializationContextDisposalTests](Serialization/SerializationContextDisposalTests.cs) | Context disposal finalizes once and retries after failure. |
| [SerializerBuilderTests](Serialization/SerializerBuilderTests.cs) | `SerializerBuilder` automatic builds, tuples, cycles, nullable enums, generics and annotations. |
| [SerializerFactorySerializerTests](Serialization/SerializerFactorySerializerTests.cs) | Serialized `SerializerFactory` registrations and reconstruction limits. |
| [SerializerFactoryTests](Serialization/SerializerFactoryTests.cs) | `SerializerFactory` resolution, open generics, hierarchies and registration rules. |
| [SerializerSerializerTests](Serialization/SerializerSerializerTests.cs) | Serialization of serializer hierarchies. |
| [StringSerializerSpanTests](Serialization/StringSerializerSpanTests.cs) | String serializer prefixes, short reads and decoding failures. |
| [TypeSerializerTests](Serialization/TypeSerializerTests.cs) | Assembly, type and type-collection serializers. |
| [VariousSerializerTests](Serialization/VariousSerializerTests.cs) | Object, collection, null, circular and subclass serialization, and activation flags. |

### Streams

Tests for the [streams domain](../../src/Sphere10.Framework/Streams).

| Fixture | Purpose |
|---|---|
| [BoundedStreamTests](Streams/BoundedStreamTests.cs) | `BoundedStream` seeking, consistency, overwrite, lengths and `ToArray`. |
| [BufferedStreamPerformanceTests](Streams/BufferedStreamPerformanceTests.cs) | Explicit file-backed `BufferedStream` timing and allocation comparison. |
| [BufferedStreamTests](Streams/BufferedStreamTests.cs) | `BufferedStream` ownership, read-ahead, cursor synchronization, length caching, async, copy and failure paths. |
| [ByteArraySpanConsolidationTests](Streams/ByteArraySpanConsolidationTests.cs) | Array and span transport through wrapper streams and endian readers and writers. |
| [EncryptedStreamTests](Streams/EncryptedStreamTests.cs) | `EncryptedStream` vectors, counter positions, async transfers, ownership and validation. |
| [ExtendedMemoryStreamTests](Streams/ExtendedMemoryStreamTests.cs) | `ExtendedMemoryStream` over each buffer type, span operations and allocations. |
| [FragmentReadSpanTests](Streams/FragmentReadSpanTests.cs) | Fragment providers used by `FragmentedStream` span reads and writes. |
| [FragmentedStreamDisposalTests](Streams/FragmentedStreamDisposalTests.cs) | `FragmentedStream` synchronous and async provider disposal. |
| [FragmentedStreamTests](Streams/FragmentedStreamTests.cs) | `FragmentedStream` reads, writes, seeking and length changes. |
| [HashingStreamTests](Streams/HashingStreamTests.cs) | `HashingStream` digests over synchronous and async writes. |
| [StreamDisposalGuardTests](Streams/StreamDisposalGuardTests.cs) | Closed blocking streams and disposed pipelines reject use. |
| [StreamExtensionsTests](Streams/StreamExtensionsTests.cs) | `ToArray` over partial-read streams. |
| [StreamMappedPropertyCacheTests](Streams/StreamMappedPropertyCacheTests.cs) | `StreamMappedProperty` caching, invalidation and failed reads and writes. |
| [StreamMappedPropertyTests](Streams/StreamMappedPropertyTests.cs) | `StreamMappedProperty` default, load, store and change. |
| [StreamProfilerSpanTests](Streams/StreamProfilerSpanTests.cs) | `StreamProfiler` captures, peeks and failed reads. |
| [StreamRestorePositionScopeTests](Streams/StreamRestorePositionScopeTests.cs) | `RestorePositionScope` nesting, failures, retry and allocations. |
| [StreamSpanTests](Streams/StreamSpanTests.cs) | Span operations on fragmented and memory streams, `ShiftBytes` and endian string reads. |
| [StreamWrapperSpanTests](Streams/StreamWrapperSpanTests.cs) | Span calls through bounded, non-closing and concurrent stream wrappers. |

### TestObjects

Shared model types used by the collection, serialization and ObjectSpace fixtures. `SafeBoxModel.cs` declares its types in the `Sphere10.Framework.Tests.SafeBox` namespace, so its `Identity` and `Account` do not clash with the plain ones.

| Fixture | Purpose |
|---|---|
| [Account](TestObjects/Account.cs) | ObjectSpace account with identity, unique number and dirty flag. |
| [AccountEqualityComparer](TestObjects/AccountEqualityComparer.cs) | Value equality for `Account`. |
| [ComplexObject](TestObjects/ComplexObject.cs) | Object with recursive, object-typed and list members for serializer tests. |
| [ComplexObjectEqualityComparer](TestObjects/ComplexObjectEqualityComparer.cs) | Value equality for `ComplexObject`. |
| [Identity](TestObjects/Identity.cs) | ObjectSpace identity with a unique key and an indexed group. |
| [NestedType](TestObjects/NestedType.cs) | Self-referencing type for cycle tests. |
| [SafeBoxObject](TestObjects/SafeBoxModel.cs) | Generated dirty-tracking base of the SafeBox-style model. |
| [Identity (SafeBox)](TestObjects/SafeBoxModel.cs) | Root identity with a name and public key. |
| [User](TestObjects/SafeBoxModel.cs) | Identity with email and display name. |
| [Group](TestObjects/SafeBoxModel.cs) | Identity with a polymorphic member array. |
| [Permission](TestObjects/SafeBoxModel.cs) | Permission granted to an identity. |
| [Account (SafeBox)](TestObjects/SafeBoxModel.cs) | Account with number, balance and owning identity. |
| [Transaction](TestObjects/SafeBoxModel.cs) | Transfer between accounts with a back-reference to its block. |
| [Block](TestObjects/SafeBoxModel.cs) | Block with a transaction array and previous hash. |
| [TestObject](TestObjects/TestObject.cs) | Small string, integer and boolean record. |
| [TestObjectEqualityComparer](TestObjects/TestObjectEqualityComparer.cs) | Value equality for `TestObject`. |
| [TestObjectSerializer](TestObjects/TestObjectSerializer.cs) | Item serializer for `TestObject`. |

### Text

Tests for the [text domain](../../src/Sphere10.Framework/Text).

| Fixture | Purpose |
|---|---|
| [FastStringBuilderTests](Text/FastStringBuilderTests.cs) | `FastStringBuilder` append, insert and chop. |
| [ParserTests](Text/ParserTests.cs) | `Tools.Parser` parsing, nullable values, safe parsing and IP addresses. |
| [StringFormatterTests](Text/StringFormatterTests.cs) | `Tools.Text.FormatEx` tokens, escapes, braces, recursive lookups and regression templates. One case is ignored. |
| [StringTests](Text/StringTests.cs) | `TrimStart` and `TrimEnd` with a string argument. |
| [TextCasingTests](Text/TextCaseConversionTests.cs) | `Tools.Text.ToCasing` conversions and punctuators. |
| [UrlIDTests](Text/UrlIDTests.cs) | `UrlID.PermuteId` over ten million identifiers. |
| [UrlToolTests](Text/UrlToolTests.cs) | `Tools.Url` query-string parsing and URL parsing with ports. |

### Threading

Tests for the [threading domain](../../src/Sphere10.Framework/Threading). `ProducerConsumerLockTests.cs` is commented out.

| Fixture | Purpose |
|---|---|
| [AsyncSynchronizedGateTests](Threading/AsyncSynchronizedGateTests.cs) | `AsyncSynchronizedGate` admission handoff and cancellation. |
| [LeafValueScopeTests](Threading/LeafValueScopeTests.cs) | Semaphore, fast, non-reentrant, producer-consumer and monitor lock scopes. |
| [SchedulerTest](Threading/SchedulerTest.cs) | `Scheduler` start times, repeats, reschedule policies, end dates, errors and serialization. This fixture is non-parallel. |
| [SchedulerTestErrorJob](Threading/SchedulerTestErrorJob.cs) | Scheduler job that throws when executed. |
| [SchedulerTestNopJob](Threading/SchedulerTestNopJob.cs) | Scheduler job that does nothing when executed. |
| [SerialThreadPoolTests](Threading/SerialThreadPoolTests.cs) | `SerialThreadPool` ordering, rescheduling and failure handling. |
| [ThreadAffineScopeTests](Threading/ThreadAffineScopeTests.cs) | Thread-affine scopes reject async disposal and capture their lock before open hooks. |
| [ThroughputOptimizerTests](Threading/ThroughputOptimizerTests.cs) | `ThroughputOptimizer` direction, sizing and optimum tracking. |
| [TypedSynchronizedObjectAdapterTests](Threading/TypedSynchronizedObjectAdapterTests.cs) | Typed synchronized object scopes and lock lifetime. |
| [ValueAccessScopeTests](Threading/ValueAccessScopeTests.cs) | `CriticalObject` access scopes, hooks, thread affinity and storage forwarders. |
| [ValueSynchronizationScopeTests](Threading/ValueSynchronizationScopeTests.cs) | `SynchronizedObject` read and write scopes, recursion, hooks and synchronized enumerators. |

### Types

Tests for the [types domain](../../src/Sphere10.Framework/Types).

| Fixture | Purpose |
|---|---|
| [CollectionTypeConverterTests](Types/CollectionTypeConverterTests.cs) | `CollectionTypeConverter` formatting and enumerator disposal. |
| [TypeResolverTests](Types/TypeResolverTests.cs) | `TypeResolver` short and full names, ambiguity and index allocation. |

### Values

Tests for the [values domain](../../src/Sphere10.Framework/Values).

| Fixture | Purpose |
|---|---|
| [CVarIntTests](Values/CVarIntTests.cs) | `CVarInt` stream and byte round trips and sizes. |
| [VarIntTests](Values/VarIntTests.cs) | `VarInt` stream and byte round trips and arithmetic operators. |
