# Collection Selection and Composition

Read this topic before choosing or implementing a collection, dictionary, lookup
or collection view. Start with the required semantics, then select the existing
contract, storage implementation and decorators. Read [synchronization](synchronization.md)
for shared access, [projections](projections.md) for representation changes and
[persistence and transactions](persistence-and-transactions.md) for durable work.
[Collections and storage](collections-and-storage.md) owns cache, comparer,
clustered-stream and persisted B-tree configuration practices.

## Select an existing collection

Most of these types are in `Sphere10.Framework`, even when their source folders
have more specific names. Import the actual declaring namespace; for example,
`ValueReferenceInverseDictionary` is in `Sphere10.Framework.Collections`.

| Required behavior | Existing family | Selection details |
|---|---|---|
| Mutable list with long indexes and range operations | [IExtendedList](../../../src/Sphere10.Framework/Collections/Lists/IExtendedList.cs), [ExtendedList](../../../src/Sphere10.Framework/Collections/Lists/ExtendedList.cs) | `ExtendedList<T>` is the contiguous array implementation; use a paged or stream-backed implementation when its storage limits do not fit. |
| Existing BCL list/collection through the extended contract | [ExtendedListAdapter](../../../src/Sphere10.Framework/Collections/Lists/ExtendedListAdapter.cs), [ExtendedCollectionAdapter](../../../src/Sphere10.Framework/Collections/Collections/ExtendedCollectionAdapter.cs) | These are live adapters over the supplied `IList<T>`/`ICollection<T>`, retaining the source's int capacity and behavior. |
| Unique-key lookup | `IDictionary<TKey, TValue>` and the dictionary families below | Use the equality comparer that defines domain identity. A standard dictionary is sufficient unless another behavior is needed. |
| Unique mapping in both directions | [IBijectiveDictionary](../../../src/Sphere10.Framework/Collections/Dictionaries/IBijectiveDictionary.cs), [BijectiveDictionary](../../../src/Sphere10.Framework/Collections/Dictionaries/BijectiveDictionary.cs) | `Bijection`, `TryGetKey` and `ContainsValue` provide the reverse mapping; values must be unique as well as keys. |
| Key lookup plus ordered, integer-indexed values | [DictionaryList](../../../src/Sphere10.Framework/Collections/Dictionaries/DictionaryList.cs), [IReadOnlyDictionaryList](../../../src/Sphere10.Framework/Collections/Dictionaries/IReadOnlyDictionaryList.cs) | Holds a list of values and a key-to-int-index dictionary. Removal compacts the list and updates later indexes; indexes are not stable identities. |
| Hierarchical lookup by several key components | [MultiKeyDictionary](../../../src/Sphere10.Framework/Collections/Dictionaries/MultiKeyDictionary.cs) | The two-key form exposes `[key1, key2]` and `TryGetValue(key1, key2, out value)`. Higher arities nest dictionaries; inherited `Count` counts outer keys, not total leaf entries. |
| Variable-length sequence key | [EnumerableKeyDictionary](../../../src/Sphere10.Framework/Collections/Dictionaries/EnumerableKeyDictionary.cs), [MultiKeyMap](../../../src/Sphere10.Framework/Collections/Dictionaries/MultiKeyMap.cs) | Sequence components select a tree path. `MultiKeyMap<K, V>` groups values in lists and creates a group when `TryGetValue` misses; its lookup is therefore a mutation. Materialize keys that would otherwise be single-pass enumerables. |
| One key with several values | [ExtendedLookup](../../../src/Sphere10.Framework/Collections/Lookup/ExtendedLookup.cs), [BTreeLookup](../../../src/Sphere10.Framework/Collections/BTree/BTreeLookup.cs) | `ExtendedLookup` is a mutable `ILookup` backed by extended collections. B-tree lookups provide ordered, grouped storage. Group count and total value count are different concepts. |
| Reference identity for keys/items | [ReferenceDictionary](../../../src/Sphere10.Framework/Collections/Dictionaries/ReferenceDictionary.cs), [ReferenceHashSet](../../../src/Sphere10.Framework/Collections/Sets/ReferenceHashSet.cs) | Distinct instances stay distinct even when value equality considers them equal. Use for instance tracking rather than entity IDs. |
| Several keys referring to the same value instance | [ValueReferenceInverseDictionary](../../../src/Sphere10.Framework/Collections/Dictionaries/ValueReferenceInverseDictionary.cs) | Builds an `ILookup<TValue, TKey>` by reference identity. This is a construction-time inverse snapshot, not a live bijection. |
| Sorted values, including duplicates | [ISortedList](../../../src/Sphere10.Framework/Collections/Sorted/ISortedList.cs), [SortedList](../../../src/Sphere10.Framework/Collections/Sorted/SortedList.cs) | The Sphere10 one-type-parameter sorted list uses binary-search insertion over an extended list. It differs from the BCL two-type-parameter sorted dictionary/list. |
| Ordered keys and range queries | [InMemoryBTree](../../../src/Sphere10.Framework/Collections/BTree/InMemoryBTree.cs), [InMemoryBTreePlus](../../../src/Sphere10.Framework/Collections/BTree/InMemoryBTreePlus.cs), [InMemoryBTreeLookup](../../../src/Sphere10.Framework/Collections/BTree/InMemoryBTreeLookup.cs) | Concrete managed implementations. `BTreeBase`, `BTree` and `BTreePlus` supply abstract storage algorithms rather than directly constructible stores. |
| Read/write access shared across threads | [SynchronizedList](../../../src/Sphere10.Framework/Collections/Synchronized/SynchronizedList.cs), [SynchronizedExtendedList](../../../src/Sphere10.Framework/Collections/Synchronized/SynchronizedExtendedList.cs), [SynchronizedDictionary](../../../src/Sphere10.Framework/Collections/Synchronized/SynchronizedDictionary.cs), [SynchronizedCollection](../../../src/Sphere10.Framework/Collections/Synchronized/SynchronizedCollection.cs), [SynchronizedSet](../../../src/Sphere10.Framework/Collections/Synchronized/SynchronizedSet.cs) | These expose read/write scopes over an existing collection. Compound operations and deferred results need scopes covering the whole operation; see the limitations below. |
| Observe collection operations | [ObservableCollection](../../../src/Sphere10.Framework/Collections/Observable/ObservableCollection.cs), [ObservableList](../../../src/Sphere10.Framework/Collections/Observable/ObservableList.cs), [ObservableExtendedList](../../../src/Sphere10.Framework/Collections/Observable/ObservableExtendedList.cs), [ObservableDictionary](../../../src/Sphere10.Framework/Collections/Observable/ObservableDictionary.cs) | Decorators expose pre/post operation events, `EventFilter` and suppression through `IObservableCollection`; they do not supply synchronization or automatic UI dispatch. |
| Read-only view or changed key/value representation | [ReadOnlyListAdapter](../../../src/Sphere10.Framework/Collections/Lists/ReadOnlyListAdapter.cs), [ReadOnlyDictionaryAdapter](../../../src/Sphere10.Framework/Collections/Dictionaries/ReadOnlyDictionaryAdapter.cs), [ReadOnlyListProjection](../../../src/Sphere10.Framework/Collections/Lists/ReadOnlyListProjection.cs), [ProjectedDictionary](../../../src/Sphere10.Framework/Collections/Dictionaries/ProjectedDictionary.cs) | A view retains the source. Read-only access does not freeze its membership or make its elements immutable. Projection changes representation through explicit conversions. |
| Logical list split into loadable pages | [IPagedList](../../../src/Sphere10.Framework/Collections/Paged/IPagedList.cs), [MemoryPagedList](../../../src/Sphere10.Framework/Collections/MemoryPaged/MemoryPagedList.cs), [StreamPagedList](../../../src/Sphere10.Framework/Collections/StreamPaged/StreamPagedList.cs) | Choose the actual page backend and item sizing/serialization model. `StreamPagedList` appends/removes at the end; it is not the arbitrary-insertion clustered-stream list. |
| Byte storage with file paging or transactions | [IBuffer](../../../src/Sphere10.Framework/Collections/Buffer/IBuffer.cs), [FileMappedBuffer](../../../src/Sphere10.Framework/Collections/FileMapped/FileMappedBuffer.cs), [TransactionalFileMappedBuffer](../../../src/Sphere10.Framework/Transactions/TransactionalFileMappedBuffer.cs) | Existing long-indexed byte buffers integrate through `BufferStream`; file paging alone does not add transaction semantics. |
| Serialized mutable list, dictionary or set | [StreamMappedFactory](../../../src/Sphere10.Framework/Collections/StreamMapped/StreamMappedFactory.cs) and stream-mapped contracts | Composes serializers, clustered streams and indexes. Use factory construction to obtain the required storage and attachments. |
| Reuse removed physical slots | [IRecyclableList](../../../src/Sphere10.Framework/Collections/Lists/IRecyclableList.cs), [StreamMappedRecyclableList](../../../src/Sphere10.Framework/Collections/StreamMapped/StreamMappedRecyclableList.cs) | `Add(item, out index)` returns the actual slot. Removal recycles it instead of shifting later slots; subsequent additions can reuse it. |
| File-backed collection with commit/rollback | [TransactionalList](../../../src/Sphere10.Framework/Transactions/TransactionalList.cs), [TransactionalDictionary](../../../src/Sphere10.Framework/Transactions/TransactionalDictionary.cs), [TransactionalHashSet](../../../src/Sphere10.Framework/Transactions/TransactionalHashSet.cs) | Composes stream-mapped collections with an `ITransactionalObject` backend. Detailed boundaries and ownership belong in [persistence and transactions](persistence-and-transactions.md). |
| Collection integrity roots/proofs | [MerkleListAdapter](../../../src/Sphere10.Framework/Merkle/MerkleListAdapter.cs) and stream-mapped Merkle variants | Add the supplied integrity layer to the selected storage; hashing does not itself make the store durable or transactional. |

Other existing choices include `CircularList`, `BoundedList`, `UpdateOnlyList`,
`StackList`, `BinaryHeap`, `QueueAdapter` and `ProducerConsumerQueue`. Inspect
their concrete contract when that behavior is required; do not implement a
second ring, heap or producer/consumer mechanism before checking these families.

## Extended contracts and implementation direction

[IExtendedCollection<T>](../../../src/Sphere10.Framework/Collections/Collections/IExtendedCollection.cs)
extends `ICollection<T>` with a `long Count` and the extended read/write
contracts. `AddRange`, `RemoveRange(IEnumerable<T>)` and `ContainsRange` express
batch operations. The standard `ICollection<T>` and `IReadOnlyCollection<T>`
count bridges use checked conversion to int; a count above `int.MaxValue` cannot
be accessed through those contracts without overflow.

[IExtendedList<T>](../../../src/Sphere10.Framework/Collections/Lists/IExtendedList.cs)
combines extended collection, read/write list and `IList<T>` contracts:

- [IReadOnlyExtendedList<T>](../../../src/Sphere10.Framework/Collections/Lists/IReadOnlyExtendedList.cs)
  supplies `Read(long)`, `ReadRange(long, long)`, `IndexOfL` and `IndexOfRange`.
  `ReadRange` returns `IEnumerable<T>`; `IndexOfRange` returns `long[]`.
- [IWriteOnlyExtendedList<T>](../../../src/Sphere10.Framework/Collections/Lists/IWriteOnlyExtendedList.cs)
  supplies long-indexed update, insertion and removal, including their range forms.
- Indexers forward through `Read`/`Update`. The `IList<T>` indexer remains int;
  `IndexOf` checks the long result before converting it. Use `IndexOfL` when
  the result must remain long.
- `IReadOnlyExtendedCollection<T>` and `IReadOnlyExtendedList<T>` themselves
  inherit an int `Count`; keep `IExtendedCollection<T>` when long cardinality
  is required. A type's long-index signatures do not imply every inherited
  surface can represent the same size.

```csharp
using Sphere10.Framework;

IExtendedList<int> values = new ExtendedList<int>();
values.AddRange(new[] { 10, 20, 30 });
values.UpdateRange(1L, new[] { 21, 31 });
var last = values.Read(2L);
var position = values.IndexOfL(last);
values.RemoveRange(0L, 1L);
```

Select an implementation base according to its native operations:

- `ExtendedCollectionBase<T>` / `ExtendedListBase<T>` provide common contracts
  and version handling without supplying storage.
- `SingularCollectionBase<T>` / `SingularListBase<T>` implement ranges through
  single-item operations.
- `RangedCollectionBase<T>` / [RangedListBase<T>](../../../src/Sphere10.Framework/Collections/Lists/RangedListBase.cs)
  implement single-item operations through range primitives. Supply those
  primitives and remaining abstract members. The current value-based
  `RangedListBase.RemoveRange(IEnumerable<T>)` fallback throws for more than
  one item; inspect the concrete implementation before assuming that overload
  works. Indexed `RemoveRange(index, count)` is a different operation.
- `CollectionDecorator`, `ExtendedListDecorator`, `DictionaryDecorator`,
  `SetDecorator` and related families retain a supplied implementation and
  layer behavior over it. Use a decorator rather than duplicating its storage.

`ExtendedList<T>` exposes configurable capacity, growth size and maximum capacity,
but stores one `T[]` and contains int-sized growth calculations. Its array
constructor retains the supplied array and fixes its growth to zero; copy the
array first if callers must not share its element storage. Adapting a BCL list
does not remove its int capacity. `DictionaryList`, standard dictionary/set
contracts and B-tree cardinalities also retain int-sized internals or surfaces.
Use long APIs to preserve addresses; verify each concrete store's independent
capacity limits before describing it as an unbounded large collection.

## Dictionaries, lookup identity and ordering

Choose key equality explicitly. `ReferenceDictionary` and `ReferenceHashSet`
are appropriate for actual instance ownership/tracking; entity IDs normally use
their value equality. [InstanceTracker](../../../src/Sphere10.Framework/ObjectSpaces/InstanceTracker.cs)
uses reference dictionaries, while
[ObjectSpace](../../../src/Sphere10.Framework/ObjectSpaces/ObjectSpaceBase.cs)
uses `DictionaryList<Type, Dimension>` to combine dimension lookup with indexing.

The bidirectional dictionary retains both maps. `Bijection.Bijection` returns
the original view, and changes through either view update the other. The
[bijective dictionary tests](../../../tests/Sphere10.Framework.Tests/Collections/Dictionaries/BijectiveDictionaryTests.cs)
demonstrate this, and
[SerializationContext](../../../src/Sphere10.Framework/Serialization/SerialiationContext.cs)
uses it for object/reference IDs:

```csharp
using System.Collections.Generic;
using Sphere10.Framework;

var names = new Dictionary<string, int> {
	["one"] = 1,
	["two"] = 2
}.AsBijection();
names.Bijection.Add(3, "three");
var thirdName = names.Bijection[3];
var thirdNumber = names["three"];
```

All writes must go through a bijective view. Mutating `UnderlyingDictionary`
bypasses the inverse maintenance. The current implementation mutates the first
map before inserting into the second and does not enforce uniqueness consistently
through its indexer, so duplicate values are not a safe supported replacement
policy. Validate uniqueness before mutation and do not assume a rejected add
is atomic. `Remove(key)` currently returns false even after removing a present
entry; do not interpret that result as a verified absence. These are existing
limitations rather than the intended bidirectional pattern.

For several values per key, use `ExtendedLookup<TKey, TValue>` rather than a
bijection. `Count` is the number of groups; `GetValuesCount(key)` is long and
counts values in that group. Its indexer returns the live group or an empty
sequence; `ToDictionary()` materializes arrays. Its collection factory permits
another existing extended collection for each group.
[TypeResolver](../../../src/Sphere10.Framework/Types/TypeResolver.cs) uses this
family for types sharing a short name.

`MultiKeyDictionary` represents a path of separately compared key components,
as [ProtocolMode](../../../src/Sphere10.Framework/Protocol/ProtocolMode.cs)
does for request/response types. It is not a one-key-to-many-values dictionary.
`DictionaryChain` searches its head before its tail, updates the located link
or adds to the head, and concatenates links for counts/enumeration. Duplicate
keys in different links remain separate entries during enumeration; this is
layered lookup rather than a deduplicated merged dictionary.

`DictionaryList` keeps ordered values and keyed lookup, as the
[walkthrough tests](../../../tests/Sphere10.Framework.Tests/Collections/Dictionaries/DictionaryListTests.cs)
show. Its underlying list and int index map impose their own limits despite
its long public count. `SortedList<T>` is for ordering by a comparer rather
than insertion order. Add via `Add` or `AddRangeSequentially`, as in the
[sorted list tests](../../../tests/Sphere10.Framework.Tests/Collections/Lists/SortedListTests.cs):
its inherited `AddRange` directly forwards to the backing collection and does
not preserve sorted insertion. Retained raw access to a decorated source can
also bypass sorting and every other decorator's behavior.

B-tree `Keys` and `Values` currently materialize lists, unlike ordinary live
dictionary views. Use `FindRange(lowerInclusive, upperInclusive)` for ordered
range traversal rather than retrieving all keys and filtering them. Tree
`Order` controls node layout; it does not change the int `Count` contract.
For an XML property bag, the existing
[PropertyBag](../../../src/Sphere10.Framework/Collections/Dictionaries/PropertyBag.cs)
offers typed `Set`, `Get` and `TryGet` over named object values. It lives in
`Sphere10.Framework.Collections` and is an `IXmlSerializable` object, not an
`IDictionary` implementation. The older `IPersistedDictionary` contract is
covered in [persistence and transactions](persistence-and-transactions.md).

## Sparse groups and capacity

For indexes, caches and grouped collections, estimate group cardinality times
initial bucket capacity times element size, in addition to the live item count.
Inspect nested factories: a lazily populated outer dictionary can retain an
oversized array for every group. Distinguish capacity limits from storage that
constructors actually allocate.

Logical removal must also release the backing store's references. Array-backed
collections must clear vacated slots for reference types and structs containing
references; changing `Count` alone leaves removed payloads reachable. Verify
removed objects become collectible while the container remains alive, as in
[ExtendedListMemoryTests](../../../tests/Sphere10.Framework.Tests/Collections/Lists/ExtendedListMemoryTests.cs).
For paging, distinguish released payload references from retained buffer capacity;
an unloaded page can still own a large empty array.

`ExtendedList<T>` initially allocates 4096 slots by default, and the default
`ExtendedLookup<TKey, TValue>` factory uses that constructor for each group.
For references on x64, that is about 32 KiB per group before array/object headers.
Configure a small growing bucket when most groups contain one or a few values.
[TypeResolver.AssemblyInfo](../../../src/Sphere10.Framework/Types/TypeResolver.cs)
uses the lookup's existing factory with capacity 1 and growth 1 for short type
names. Preserve duplicate-name growth, equality, ordering and count contracts;
fix the owning factory without changing general collection defaults unnecessarily.

For a material allocation regression, cover representative sparse cardinality
and collisions with a bounded allocation test, as in
[TypeResolverTests](../../../tests/Sphere10.Framework.Tests/Types/TypeResolverTests.cs).
Startup/cache changes also require the
[startup memory comparison](testing.md#startup-memory-regressions).

## Compose synchronization, observation and views

Use existing constructors or the appropriate extensions in
[DecoratorExtensions](../../../src/Sphere10.Framework/Extensions/DecoratorExtensions.cs),
including `AsSynchronized`, `AsSynchronizedSet`, `AsObservable`, `AsBijection`,
`AsSorted` and `AsMerkleized`. The ordinary `IList<T>` synchronization extension
is currently named `AsSynchronized_`; the extended-list and dictionary versions
are `AsSynchronized`. For a recyclable view of a supplied extended list and
recycled-index stack, construct `RecyclableListAdapter<T>`. Overloads select the collection
contract they decorate; retain the synchronized wrapper where outer scopes
are needed. [SynchronizedDictionary](../../../src/Sphere10.Framework/Collections/Synchronized/SynchronizedDictionary.cs)
also accepts an `ISynchronizedObject` when operations must share a supplied lock.

A per-method scope does not make a multi-call check/update atomic. Hold a write
scope for the entire compound operation and a read scope for a consistent
snapshot. Ensure every consumer accesses the same decorated synchronization
domain; direct mutation of a retained backing list/dictionary bypasses it.
Follow [synchronization](synchronization.md) for parent lock composition,
recursion, lock ordering and thread affinity.

Synchronized collection/list/dictionary enumeration acquires a read scope
when obtaining the enumerator and releases it on enumerator disposal. `foreach`
provides that disposal, including on early exit. Keep enumerator use on its
acquiring thread and do not mutate through a write operation while retaining
a read-scoped enumerator. Returning `Keys` or `Values` does not retain the
property getter's read scope for later enumeration.

The current `SynchronizedSet` scopes its enumerator but directly forwards
ordinary set operations and properties without read/write scopes. Use explicit
outer scopes for its reads/writes. `SynchronizedExtendedList.ReadRange` and
`ContainsRange` dispose their method scopes before consumers enumerate the
returned sequence; keep an outer read scope while consuming a deferred source.
These wrappers cannot guarantee snapshot semantics for every source merely
from their names.

Read-only adapters and projections normally expose live membership. Convert
to an array under the appropriate scope when a snapshot is required. That
copies membership, not referenced objects. Data-object sequence properties
follow the [array-property rule](../code-styling.md#collection-valued-data-properties);
it does not replace collection APIs or force every collection view to copy.

Observable collection events describe operations through that decorator.
They are separate from `INotifyCollectionChanged`, persistence and UI dispatch.
Choose decorator order deliberately: an observable source beneath a synchronized
wrapper emits its callbacks while the outer operation holds the lock. Keep
callbacks consistent with that lock and do not treat them as outside the operation.
Representation changes use the [projection pattern](projections.md); that topic
documents the incomplete older `ProjectedExtendedList` and `ProjectedLookup`
members so they are not copied as complete exemplars.

## Paging, persistent collections and physical slots

`IPagedList<T>` is an extended list with load state, page-operation events and
`EnterOpenPageScope`. An open-page scope prevents eviction while accessing that
page; it is a page-lifetime guard, not a reader/writer lock or transaction.
`IMemoryPagedList<T>` adds loading/saving/unloading events, disposal and `Flush()`
to save dirty pages and unload cached pages. `MemoryPagedList<T>` uses an item
sizer and cannot reopen its state across process sessions. Do not infer durable
storage from memory paging or treat the page-cache budget as a process memory cap.
Verify sizing units for the chosen backend: the current page cache charges a
fixed `pageSize` for each resident page, so a generic item page's budget does
not measure all managed item allocations. File/byte buffers have a different
relationship between page size and bytes.

`StreamPagedList<T>` stores serialized pages contiguously in a supplied stream.
Choose static/dynamic layout to match serializer sizing. `StreamMappedList<T>`
instead stores an item per `ObjectStream` logical stream in `ClusteredStreams`,
supporting arbitrary insertion/removal. `StreamMappedDictionary` stores pairs;
`StreamMappedDictionaryCLK` stores values with constant-length keys in a separate
index. Both implement `IStreamMappedDictionary<TKey, TValue>` and
`IStreamMappedCollection<TKey, TValue>` for logical key/value entries. Their
common `ObjectStream` stays nongeneric because the physical layouts differ.
Standard dictionary/set count contracts remain int while physical record
positions are long. `StreamMappedHashSet` uses a digest-keyed dictionary.

Construct these families with `StreamMappedFactory.CreateList`,
`CreateRecyclableList`, `CreateDictionary` or `CreateHashSet`, supplying the
serializer/comparer/index choices required by the data. Load a store when
`RequiresLoad` is true, or use the constructor/factory's supported `autoLoad`
option. Preserve matching serializers, cluster layout and endianness when reopening.
Follow [serialization](serialization.md) and
[ObjectSpace practices](object-spaces.md) for schema/indexed object storage.

Recyclable lists distinguish live `Count`, physical `ListCount` and
`RecycledCount`; live count is physical slots minus recycled slots. They enumerate
live items while retaining holes. `Read(index)` rejects a recycled slot, `Insert`
is unsupported, and reusing a removed slot does not preserve the removed object's
identity. Use `Add(item, out index)` for the allocated slot and
`IsRecycled(index)` when traversing physical positions. Do not iterate `0..Count-1`
and assume those are the live addresses. ObjectSpace uses these slots beneath
its own identity/version tracking.

Keep backing resources alive throughout lazy enumeration and dispose enumerators.
Stream-mapped dictionary and recyclable-list enumerators hold container access
scopes for their lifetime; the ordinary `StreamMappedList` enumerator reads
items individually and does not acquire a scope around the whole traversal.
Hold the container access scope explicitly when that traversal must be consistent.
Ownership is explicit and layered: `OwnsContainer` controls
collection-to-ObjectStream disposal, `ObjectStream.OwnsStreams` controls its
clustered container, and `ClusteredStreams.OwnsStream` controls the root stream.
Factories set ownership for the layers they create; supplying an existing
container does not imply ownership of every backing resource. Transactional
decorators additionally expose `OwnsList` or `OwnsDictionary`; hash-set disposal
delegates to its dictionary. Inspect the selected overload and configure borrowed
resources accordingly.

`TransactionalList`, `TransactionalDictionary` and `TransactionalHashSet` reuse
the stream-mapped contracts and forward `Commit`/`Rollback` to their transaction
backend. Their rollback wiring reinitializes the container. Supplying a transaction
object beside an arbitrary collection is insufficient unless that backend actually
controls the collection's storage. Saving/flushing bytes and committing a transaction
are distinct operations; scope behavior and reopen verification are covered by
[persistence and transactions](persistence-and-transactions.md).

For ordered persisted data, use `StreamMappedBTree`, `StreamMappedBTreePlus` or
`StreamMappedBTreeLookup` over the selected stream/buffer backend. Memory-backed,
file-paged and transactional backends are compositions, not interchangeable
durability promises. Use the family-specific defaults for new stores and preserve
the original explicit order/serializer sizes/endianness on reopen, as specified
in [persisted B-tree layouts](collections-and-storage.md#b-tree-defaults-and-persisted-layouts).
The [B-tree profiler](../../../utils/Sphere10.Framework.Utils.Profiler/BTrees/README.md)
documents measured backend/configuration selection rather than inferring one
universal best implementation.
