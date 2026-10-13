# Projection Adapters

Read this topic when exposing an existing serializer, collection, data source,
cache or comparer through another representation. A projection retains the
source implementation and presents a different typed view of its operations.
The source remains responsible for storage and behavior.

## Source and projected types

In `ProjectedFoo<TFrom, TTo>`, `TFrom` is the source representation and `TTo`
is the representation exposed to the consumer. Retain the typed source and
give the conversion functions explicit directions:

- `projection: TFrom -> TTo` converts source results to consumer results.
- `inverseProjection: TTo -> TFrom` converts consumer inputs to source inputs.

The operation determines the direction. A lookup key is an input even when
the operation is a read:

| Operation on the projected view | Conversion and delegation |
|---|---|
| Read, enumerate, deserialize or return a created item | Call the source, then apply `projection` to its result. |
| Add, update, serialize or calculate serialized size | Apply `inverseProjection` to the input, then call the source. |
| Lookup, contains, remove or compare an input item/key | Convert the supplied input to the source representation before delegating. |
| Count, synchronization, configuration or capabilities | Forward source state when it has the same meaning in the projected contract. |

[ProjectedSerializer](../../../src/Sphere10.Framework/Serialization/Special/ProjectedSerializer.cs)
is the central example: it implements `IItemSerializer<TTo>` around an
`IItemSerializer<TFrom>`. `Deserialize` projects the source result;
`Serialize`, `CalculateSize` and `CalculateTotalSize` inverse-project their
inputs. Size calculations and serialization therefore use the same source
layout, reader/writer and `SerializationContext`.

[TypeCollectionSerializer](../../../src/Sphere10.Framework/Serialization/Special/TypeCollectionSerializer.cs)
uses this pattern to expose types over a tagged type collection. The serializer
[AsProjection extension](../../../src/Sphere10.Framework/Serialization/Checksum/DecoratorExtensions.cs)
constructs the same adapter for an existing serializer.

## Keep the generic implementation canonical

Implement and store data in the typed abstraction. Provide an object view only
at a boundary that needs it, through an explicitly constructed projection.
Do not make the generic engine delegate through `object`, or require a generic
contract to inherit an object contract solely to make unrelated types share a
coordinator. A coordinator can depend on the common metadata/control contract
without erasing the key/value types of ordinary operations.

This existing serializer API illustrates an opt-in object view:

```csharp
using Sphere10.Framework;

IItemSerializer<int> source = PrimitiveSerializer<int>.Instance;
var objectView = source.AsProjection<int, object>(item => item, item => (int)item);
```

Typed callers retain `source`; callers of `objectView` must supply boxed
integers. Boxing/unboxing occurs at that object boundary, while the source
continues to operate on `int`. This is a conversion adapter, not a polymorphic
serializer accepting arbitrary object types. Projection can also allocate
converted values or invoke delegates; do not claim it is allocation-free.

[ProjectedCache](../../../src/Sphere10.Framework/Cache/ProjectedCache.cs)
applies the same structure to `ICache<TKey, TValue>` using independent source
key/value and destination key/value types, just as `ProjectedDictionary` does.
Its `AsProjection` extension requires both directions for both types.
[ObjectCacheProjection](../../../src/Sphere10.Framework/Cache/ObjectCacheProjection.cs)
is the explicitly named object specialization returned by `AsObjectCache()`;
ordinary typed callers keep the source contract.
[ProjectedCachedItem](../../../src/Sphere10.Framework/Cache/ProjectedCachedItem.cs)
forwards live entry metadata and value conversion; it does not copy pinning
state into an independent entry. The [cache guide](../../../src/Sphere10.Framework/Cache/README.md)
defines adapter ownership and the separate key/value-free reaper contract.

## Distinguish related patterns

- A **projection adapter** changes the exposed representation while delegating
  to the source. A mutable view normally needs both conversion directions.
  A read-only view needs only the directions its operations use; a read-only
  dictionary still needs an inverse key conversion for lookup.
- A **decorator** retains the exposed contract and layers behavior over it.
  Use the abstraction's existing decorator family for synchronization,
  observation and similar behavior. See the
  [decorator skill](../../../agents/skills/decorator-pattern/SKILL.md).
- An **inline interface bridge** forwards a specialized member through an
  inherited compatible contract on the same instance. Follow
  [interface bridges](interface-bridges.md) for those members. A representation
  adapter has its own source and conversions; it is not a reason to invert a
  typed implementation hierarchy or add object inheritance to its interface.
- A **key projection** derives a comparison, hash or index key from an item.
  It may be intentionally lossy and does not require an inverse.
  [ProjectionComparer](../../../src/Sphere10.Framework/Comparers/ProjectionComparer.cs)
  compares `TSource` by projected `TKey` values, while
  [ProjectedComparer](../../../src/Sphere10.Framework/Comparers/ProjectedComparer.cs)
  adapts an existing `IComparer<TFrom>` to consume `TTo` via the inverse.
  [ProjectionHasher](../../../src/Sphere10.Framework/Serialization/Hashers/ProjectionHasher.cs)
  and [projection indexes](../../../src/Sphere10.Framework/ObjectStream/Indexes/ProjectionIndexBase.cs)
  are key-projection examples rather than reversible object views.

## Preserve the source contract

1. Validate the source and required delegates with `Guard`. Define supported
   input types and null behavior. A source's null capability alone does not
   prove the supplied conversions accept null.
2. Choose conversions that preserve the view's intended identity and equality.
   For a writable view, inverse-projecting a projected source value must recover
   the same logical source item. Projecting an inverse-projected accepted input
   must produce its documented representation, including any normalization.
   Noninvertible projections belong in read-only views or require a deliberate
   write policy. Dictionary keys must remain unambiguous under the view's
   equality rules.
3. Cover the whole exposed contract, including range operations, indexers,
   generic and nongeneric enumeration, copying, validation and refresh results.
   Project values using the delegate; `Cast<TTo>()` only checks/casts runtime
   types and cannot perform a general conversion.
4. Keep source state authoritative. Preserve counts, paging information,
   synchronization, capabilities and mutation visibility. Do not build an
   independent store simply to present another type. Decide explicitly whether
   enumeration is lazy or materialized, and preserve the source lifetime during
   lazy reads.
5. Preserve mutable runtime identity when the contract requires it. For example,
   a projected cache entry's pinning/invalidation metadata must control the
   actual source entry. An independent copied entry is not an adequate adapter.
   If a wrapper is required, delegate its state and retain a stable association.
6. Forward each sync/async operation to the source's matching operation.
   Project actual completed results and retain cancellation and completion
   semantics. See [sync/async hierarchies](sync-async.md).
7. Preserve ownership and event semantics. State whether the adapter borrows or
   owns the source; do not dispose borrowed resources implicitly. When events
   require converted delegates, retain enough subscription state for matching
   removal, duplicate and multicast subscriptions. Direct event forwarding is
   appropriate only when the delegate contracts are compatible.

[ProjectedDictionary](../../../src/Sphere10.Framework/Collections/Dictionaries/ProjectedDictionary.cs)
and [ProjectedReadOnlyDictionary](../../../src/Sphere10.Framework/Collections/Dictionaries/ProjectedReadOnlyDictionary.cs)
show independent key/value conversions.
[ReadOnlyListProjection](../../../src/Sphere10.Framework/Collections/Lists/ReadOnlyListProjection.cs)
projects reads and enumeration without a write conversion.
[ProjectedDataSource](../../../src/Sphere10.Framework/DataSource/ProjectedDataSource.cs)
forwards CRUD, paging and async operations; operations that need a source item
reject use when its optional inverse is absent. The WinForms
[CrudGrid binding](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudGrid.cs)
uses it to expose a typed data source through an object view.

## Verify exemplars before copying

Existing classes establish the idiom, not a guarantee that every older member
implements it correctly. The current
[ProjectedExtendedList](../../../src/Sphere10.Framework/Collections/Lists/ProjectedExtendedList.cs)
uses `Cast<TTo>()` in generic enumeration, returns the source's nongeneric
enumerator, and copies into a converted temporary array in `CopyTo`.
Those paths do not perform the required general forward projection.
[ProjectedLookup](../../../src/Sphere10.Framework/Collections/Lookup/ProjectedLookup.cs)
still has an unimplemented indexer. Do not copy or recommend those paths as
complete implementations; repair them only within an authorized task.

[ProjectionMemoizer](../../../src/Sphere10.Framework/Objects/ProjectionMemoizer.cs)
stores one association per dictionary key and throws for duplicate registration
or missing removal. It is not a general identity map or a substitute for normal
event subscription semantics. Use it only when those rules match the caller.

Verify new adapters with genuinely different representations, not just an
upcast: read/enumeration/copy, inverse writes/lookups, rejected inputs, null and
equality behavior, source mutation visibility, identity, disposal and relevant
async/event behavior. Serializer tests should compare calculated size with
written bytes and verify round trips under the selected endianness. See
[serialization practices](serialization.md) and [testing practices](testing.md).
