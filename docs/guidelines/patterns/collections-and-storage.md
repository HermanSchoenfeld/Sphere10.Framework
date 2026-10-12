# Collections and Storage

The [core project guide](../../../src/Sphere10.Framework/README.md) describes the collection and persistence families. Use existing implementations and decorators; see [framework reuse](framework-reuse.md), [serialization](serialization.md) and [resource scopes](resource-scopes.md).

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

The framework provides `long`-indexed collection interfaces and implementations:

- **[IExtendedCollection<T>](../../../src/Sphere10.Framework/Collections/Collections/IExtendedCollection.cs)** — extends `ICollection<T>` with `long Count` and range operations. Its base collection bridges use checked conversion back to `int`.
- **[IExtendedList<T>](../../../src/Sphere10.Framework/Collections/Lists/IExtendedList.cs)** — adds long indexing over the inherited [read contract](../../../src/Sphere10.Framework/Collections/Lists/IReadOnlyExtendedList.cs) and [write contract](../../../src/Sphere10.Framework/Collections/Lists/IWriteOnlyExtendedList.cs). `ReadRange(long index, long count)` returns `IEnumerable<T>`; `IndexOfRange` returns `long[]`. Base `IList<T>` indexing remains `int`.
- **[RangedListBase<T>](../../../src/Sphere10.Framework/Collections/Lists/RangedListBase.cs)** — implements single-item operations through range operations. Supply the range primitives and remaining inherited properties. Its current value-based `RemoveRange(IEnumerable<T>)` fallback throws for more than one item; inspect the concrete implementation before assuming all range forms work.
- **[ExtendedList<T>](../../../src/Sphere10.Framework/Collections/Lists/ExtendedList.cs)** — resizable contiguous array-backed list with configurable capacity, growth size and maximum capacity. Long signatures do not remove the underlying array/runtime limits.
- **[StreamMappedList<T>](../../../src/Sphere10.Framework/Collections/StreamMapped/StreamMappedList.cs)** — ordered persistent list backed by `ObjectStream<T>` and clustered streams.
- **[StreamMappedRecyclableList<T>](../../../src/Sphere10.Framework/Collections/StreamMapped/StreamMappedRecyclableList.cs)** — retains physical slots for recycling/reuse. Its [recyclable base](../../../src/Sphere10.Framework/Collections/Lists/RecyclableListBase.cs) distinguishes live `Count` from `ListCount` and `RecycledCount`.
- **[StreamMappedDictionary<TKey, TValue>](../../../src/Sphere10.Framework/Collections/StreamMapped/StreamMappedDictionary.cs)** / **[StreamMappedHashSet<T>](../../../src/Sphere10.Framework/Collections/StreamMapped/StreamMappedHashSet.cs)** — persistent dictionary/set with index lookups. Their standard dictionary/set contracts keep their existing count types; physical stream positions use `long`.
- Observable, synchronized, transactional and merkle-aware variants are existing collection families; choose the implementation/decorator actually supplied for the required abstraction.

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
