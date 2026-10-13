# Collections and Storage

The [core project guide](../../../src/Sphere10.Framework/README.md) describes the collection and persistence families. Use existing implementations and decorators; see [framework reuse](framework-reuse.md), [serialization](serialization.md) and [resource scopes](resource-scopes.md).

## Typed caches and capacity

Use the typed cache contract and engine for typed keys and values. The nongeneric cache is an object specialization above the typed contract; typed caches do not implement it automatically. Use `ProjectedCache` for alternate key/value representations, following the [projection pattern](projections.md); use `ObjectCacheProjection` or `AsObjectCache()` for an object view. Entry views must forward live metadata so pinning, accounting and callbacks remain consistent. Reapers share the key/value-free `ICacheBase` contract, allowing differently typed caches to share capacity without object conversion. Follow the [cache guide](../../../src/Sphere10.Framework/Cache/README.md) for estimator units, normalized fractional reaping, actual admission deficits and `PooledCacheReaper`. Preserve dirty-page save/unload callbacks and pinned entries when changing a cache consumer. A cache budget is estimated retained size, not a whole-process memory ceiling.

## Graph and tree identity

Use dictionary-based storage keyed by entity ID for graph/tree structures. Keep node storage and candidate-head storage keyed by the same identity contract, and supply the appropriate `IEqualityComparer<TId>` for custom ID types. Choose reference identity only when the abstraction operates on actual object instances; see [application ownership](application-ownership.md#ownership-and-platform-extensions).

## ClusteredStreams

`ClusteredStreams` is a virtual file-system that multiplexes many logical `Stream`s onto a single backing stream using a cluster-based allocation model (similar to FAT). Core types:

- **[ClusteredStreams](../../../src/Sphere10.Framework/ClusteredStreams/ClusteredStreams.cs)** — manages a `ClusterMap` of fixed-size clusters, a stream descriptor list, and reserved streams. Supports `Add`, `OpenRead`, `OpenWrite`, `Remove`, `Insert`, `Swap`, `Clear(long index)` and `Clear()`. Provides pluggable [IClusteredStreamsAttachment](../../../src/Sphere10.Framework/ClusteredStreams/Attachments/IClusteredStreamsAttachment.cs) for indexes and merkle trees.
- **[ClusteredStream](../../../src/Sphere10.Framework/ClusteredStreams/ClusteredStream.cs)** — a `StreamDecorator` representing a single logical stream backed by a cluster chain. Opened via `ClusteredStreams.Open*`.
- **[ObjectStream<T>](../../../src/Sphere10.Framework/ObjectStream/ObjectStreamT.cs)** — typed view over `ObjectStream` using an `IItemSerializer<T>`, with `SaveItem` and `LoadItem` operations over per-item streams.
- **[ObjectSpace](../../../src/Sphere10.Framework/ObjectSpaces/ObjectSpaceBase.cs)** — the `Sphere10.Framework.ObjectSpaces.ObjectSpace` layer built on clustered streams, object streams and serializer/comparer factories. Provides `Get<T>`, `Save<T>`, `Delete<T>` and `New<T>`; concrete memory/file implementations supply construction.
- **Stream-mapped collections** — `IStreamMappedList<T>`, `IStreamMappedDictionary<TKey, TValue>`, `IStreamMappedHashSet<T>` all build on `ObjectStream` for persistent, list/dict/set semantics with full CRUD, serialization, and optional merkle-tree integrity.

## Collections (ExtendedList & Stream-Mapped)

Read [Collection selection and composition](collections.md) when choosing a
collection contract, implementation, adapter or decorator. It covers extended
long-index/range APIs, dictionaries and bijections, multi-key/grouped lookups,
ordering, synchronized/observable/read-only/projected views, paging, recycled
slots and stream-mapped/transactional storage. It also records concrete storage
limits, live-view/enumeration behavior and layered ownership. This topic retains
the cache, comparer, clustered-stream and B-tree layout practices below.

## Comparers & Equality

The framework provides a `ComparerFactory` registry — **not** ad-hoc `IEqualityComparer<T>` implementations:

- **[ComparerFactory](../../../src/Sphere10.Framework/Comparers/ComparerFactory.cs)** — registers equality and ordering comparers by type. Its defaults cover the listed primitive, nullable and common types; do not assume every CLR primitive or framework type has an explicit registration. Equality lookup also constructs supported collection comparers and falls back to `EqualityComparer<T>.Default`.
- **[ByteArrayEqualityComparer](../../../src/Sphere10.Framework/Comparers/ByteArrayEqualityComparer.cs)** — use for `byte[]` content equality through the framework comparer contract. Its span implementation itself uses `SequenceEqual`; the reuse rule is not a separate performance guarantee.
- **`ReferenceEqualityComparer`** — identity comparison by reference. On the current .NET target this is `System.Collections.Generic.ReferenceEqualityComparer.Instance`; the framework's [compatibility definition](../../../src/Sphere10.Framework/Comparers/ReferenceEqualityComparer.cs) is compiled only for `NETSTANDARD`.
- **[TypeEquivalenceComparer](../../../src/Sphere10.Framework/Types/TypeEquivalenceComparer.cs)** — compares generic definitions/arguments recursively and treats unspecified generic parameters as equivalent. It is not a general type-forwarding comparer.
- `new ComparerFactory(ComparerFactory.Default)` copies the current registry; then register previously unregistered domain-specific types with `RegisterEqualityComparer` or `RegisterComparer`. Registrations use dictionary insertion, so registering an existing type is not an override operation.
- `ObjectSpace` receives its comparer factory. [StreamMappedFactory](../../../src/Sphere10.Framework/Collections/StreamMapped/StreamMappedFactory.cs) accepts individual equality/ordering comparers; the comparer registry is not an injected dependency of every stream-mapped collection.

The current ordering lookup in `ComparerFactory.GetComparer` reads `_equalityComparers` rather than `_comparers` and tries to reflect `Comparer<T>.Default` as a method. Its typed lookup can therefore fail even when an ordering comparer was registered. Inspect or repair that path within the authorized task before relying on it; preserve the intended registry reuse rather than copying the defect into new code.

[IStreamMappedDictionary<TKey, TValue>](../../../src/Sphere10.Framework/Collections/StreamMapped/IStreamMappedDictionary.cs) extends the logical-entry [IStreamMappedCollection<TKey, TValue>](../../../src/Sphere10.Framework/Collections/StreamMapped/IStreamMappedCollection.cs). Its non-generic `ObjectStream` contract supports both standard key/value-pair storage and constant-length-key value storage. Persistent collections retain their existing access scopes and `OwnsContainer`/`OwnsStreams` ownership.

Collection-valued data properties expose array membership snapshots as required by [Code Styling](../code-styling.md#collection-valued-data-properties). This does not turn the `long`-indexed collection APIs into array storage or make a defensive membership copy a deep clone. [Data sources](data-sources.md) retain `int` paging over their backing collections.

For ObjectSpace schemas, indexed identity, recycled slots, file commit/rollback and B-tree configuration, read [ObjectSpace practices](object-spaces.md) and the [ObjectSpace project guide](../../../src/Sphere10.Framework/ObjectSpaces/README.md). The [ObjectSpace profiling suite](../../../utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/README.md) measures actual configuration effects rather than inferring a universal best from defaults.

## B-tree defaults and persisted layouts

Use the family-specific constants in [Sphere10FrameworkDefaults](../../../src/Sphere10.Framework/Sphere10Defaults.cs) for fresh managed or stream-mapped B-tree, B+ and grouped lookup trees. Default-order stream constructors and factory overloads require empty storage. Reopen existing trees with their original explicit order, serializer sizes and endianness. Their headers do not authenticate those choices; stride/header validation cannot identify every plausible incorrect layout. Changing a default does not migrate stored nodes.

The primary settings target Balanced workloads with numeric keys. Use the separate measured wide-key constants explicitly for serialized 32-byte B+ and lookup keys; a generic constructor does not inspect key width and silently change its order. Read the [B-tree profiler guide](../../../utils/Sphere10.Framework.Utils.Profiler/BTrees/README.md) for backend, key-width, allocation and dataset limits, and keep the original declared order for every persisted store. Generic ObjectStream index attachments retain their separate compatibility default; ObjectSpace uses its own measured index-order owner.
