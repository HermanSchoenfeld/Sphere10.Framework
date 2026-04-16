# ObjectSpace Garbage Collection — Implementation Requirements

## Overview

### What This Is

An ObjectSpace is like a lightweight embedded object database. It stores .NET objects in "dimensions" (analogous to database tables), where each dimension holds objects of a single type. Objects can reference each other across dimensions — for example, an `Account` object may have an `Identity` property that points to an `Identity` object stored in a separate dimension.

Currently, when you serialize an `Account` that references an `Identity`, the `Identity` gets serialized inline within the `Account`'s byte stream. This means if two accounts share the same identity, it gets duplicated. Worse, there's no way to delete an `Identity` and have that deletion ripple through to all objects that reference it.

**Garbage collection for ObjectSpaces** solves this by:

1. **Externalizing cross-dimension references**: Instead of serializing a referenced dimension object inline, the serializer writes a lightweight pointer — an `ObjectSpaceObjectReference(dimensionIndex, rowIndex)`. The referenced object lives in its own dimension and is serialized independently.

2. **Tracking references globally**: The ObjectSpace maintains two global indexes — **Out-Refs** (what does this object point to?) and **In-Refs** (what points to this object?) — so it always knows the full reference graph.

3. **Collecting orphans**: When an object is deleted, its outgoing references are cleaned up. Any referenced object that is not a "root" (i.e., not explicitly created by the user) and has zero remaining in-refs can be immediately collected — no mark-and-sweep needed, because the in-ref count tells us everything.

4. **Distinguishing roots from non-roots**: Dimensions are marked as root or non-root. Root dimension objects (e.g., `Account`) are user-managed and never auto-collected. Non-root dimension objects (e.g., `Identity`) are only kept alive by incoming references; when nothing references them, they're garbage.

Objects that are **not** tracked in any dimension (plain nested objects, value types, collections, etc.) are called **component objects** — they continue to be serialized inline as they always have been. The external reference mechanism only applies to objects whose type is registered as a dimension in the ObjectSpace.

---

## Conceptual Model

- **Dimensions** are analogous to database tables. Each dimension stores objects of a single CLR type via `ObjectStream<T>` backed by `ClusteredStreams`.
- **Root vs Non-Root Dimensions**: A dimension marked as **root** (via `[Root]` attribute or builder API) contains user-managed objects that are never auto-collected by GC. Non-root dimensions contain objects that are only kept alive by references from other objects.
- **Dimension Objects vs Component Objects**: A "dimension object" is any object whose CLR type is registered as a dimension in the ObjectSpace. A "component object" is everything else (nested objects, value types, etc.) — these are always serialized inline within their parent's stream, exactly as today.
- **Object ID** = `ObjectSpaceObjectReference(short DimensionIndex, long RowIndex)` — a globally unique composite key within the ObjectSpace. The `DimensionIndex` is the ordinal position of the dimension in `ObjectSpaceDefinition.Dimensions[]`, and `RowIndex` is the item's position in the dimension's `StreamMappedRecyclableList<T>`.
- **Eager ID Assignment**: Object IDs are assigned eagerly, never lazily. `New<T>()` and `Get<T>()` are the only entry points for object activation, and both immediately assign an `ObjectSpaceObjectReference`. This means the ID is always known before serialization begins.
- **Out-refs**: for a given object, the set of other dimension objects it references.
- **In-refs**: for a given object, the set of other dimension objects that reference it.
- **In-Memory Ref Cache**: Since ObjectSpace tracks all live instances via `InstanceTracker`, the out-refs and in-refs are also cached in memory alongside the instance. When an object is serialized/saved, the in-memory ref cache is updated. As a **diagnostic check**, when saving, the persisted refs should be compared against the in-memory refs to detect inconsistencies.
- **Garbage**: a non-root dimension object whose in-refs set is empty (nothing references it). No mark-and-sweep traversal is needed — an empty in-ref set on a non-root object is sufficient to identify garbage.

---

## Current Code Inventory & Status

### Existing Infrastructure (ready to use)

| Component | File | Role |
|---|---|---|
| `ObjectSpace` | `ObjectSpaceBase.cs` | Core orchestrator. Has `_dimensions` (DictionaryList by Type), `_instanceTracker`, `New<T>`, `Get<T>`, `Save<T>`, `Delete<T>`, `Flush()`, `SaveModifiedObjects()`. Dimensions are built in `LoadInternal()` with reserved streams for merkle trees. `CreateItemSerializer()` is virtual and currently just calls `Serializers.GetSerializer(objectType)`. |
| `InstanceTracker` | `InstanceTracker.cs` | Bijective `Type → (long index ↔ object)` map. Tracks loaded & new instances. `TrackNew()` assigns negative indices; `Track(item, index)` sets actual row indices after save. `TryGetIndexOf(object)` returns the current index. |
| `ObjectSpaceDefinition` | `ObjectSpaceDefinition.cs` | Defines dimensions, indexes, traits. `DimensionDefinition` has `ObjectType`, `Indexes[]`, `AverageObjectSizeBytes`, `ChangeTracker`. |
| `Dimension` record | `ObjectSpaceBase.cs:484` | `record Dimension(DimensionDefinition, IStreamMappedRecylableList Container)` — runtime dimension with its backing recyclable list. |
| `ObjectSpaceDimensionBuilder<T>` | `ObjectSpaceDimensionBuilder.cs` | Fluent builder for configuring a dimension. Supports `WithIdentifier`, `WithIndexOn`, `WithUniqueIndexOn`, `WithChangeTrackingVia`, `Merkleized`, `UsingSerializer`. Calls `Done()` to return to parent builder. |
| `ObjectSpaceBuilder` | `ObjectSpaceBuilder.cs` | Fluent builder for ObjectSpace. `AddDimension<T>()` creates dimension builders. Supports annotation-driven configuration (`[Identity]`, `[Index]`, `[UniqueIndex]`, `[EqualityComparer]`). |
| `ObjectChangeTracker` | `ObjectChangeTracker.cs` | Dirty-flag tracking via a boolean property on objects. Used for AutoSave/persistence-ignorance. |
| `ObjectSpaceTraits` | `ObjectSpaceTraits.cs` | `[Flags]` enum: `None`, `Merkleized = 1 << 0`, `AutoSave = 1 << 1`. |
| `DimensionAttribute` | `DimensionAttribute.cs` | `[Dimension]` class-level attribute — currently empty marker. |
| `ReferenceSerializer<T>` | `ReferenceSerializer.cs` | Serializer wrapper handling null, not-null, context-reference discrimination. Has `_supportsExternalReferences` field (constructed from mode flag). `ClassifyReferenceType()` determines how to serialize. |
| `ReferenceSerializerMode` | `ReferenceSerializerMode.cs` | `[Flags]` enum with `SupportNull = 1 << 0`, `SupportContextReferences = 1 << 1`, `SupportExternalReferences = 1 << 1` (**BUG: same bit**). |
| `ReferenceType` enum | `ReferenceSerializer.cs:143` | `IsNull=0`, `IsNotNull=1`, `IsContextReference=2`. `IsExternalReference=3` is **commented out**. |
| `ReferenceModeAttribute` | `ReferenceModeAttribute.cs` | Attribute for properties/fields to control reference serialization mode. Has `AllowExternalReference` property that sets `SupportExternalReferences` flag. |
| `SerializationContext` | `SerialiationContext.cs` | Monolithic context tracking object status across sizing/serialization/deserialization. Uses `BijectiveDictionary<object, long>` for processed objects. |
| `ObjectSpaceReferenceSerializer<T>` | `ObjectSpaceReferenceSerializer.cs` | **Entirely commented out.** Intended to subclass `ReferenceSerializer<T>` and handle external references. |
| `ClusteredStreams` | (infrastructure) | Virtual filesystem with cluster-based allocation, attachments, reserved streams. ObjectSpace's top-level `_streams` uses reserved stream 0 for spatial merkle tree. |

### Known Bugs to Fix First

1. **`ReferenceSerializerMode.SupportExternalReferences` has wrong bit value** (`ReferenceSerializerMode.cs:10`):
   ```csharp
   SupportContextReferences = 1 << 1,   
   SupportExternalReferences = 1 << 1,  // BUG: same bit as SupportContextReferences!
   ```
   **Fix**: Change to `SupportExternalReferences = 1 << 2`. Update `Default` accordingly.

### Scaffolded but Incomplete (need implementation)

| Component | Status | What's needed |
|---|---|---|
| `ReferenceType.IsExternalReference` | Commented out | Uncomment, implement serialize/deserialize/size logic in `ReferenceSerializer<T>` |
| `ObjectSpaceReferenceSerializer<T>` | Commented out | Uncomment, implement external reference serialization for dimension objects only |
| `[Root]` attribute | Does not exist | New annotation to mark dimension types as GC roots |
| `DimensionDefinition.IsRoot` | Does not exist | Property on dimension definition to track root status |
| Out-refs index | Does not exist | ObjectSpace-level index: `ObjectSpaceObjectReference → ObjectSpaceObjectReference[]` |
| In-refs index | Does not exist | ObjectSpace-level index: `ObjectSpaceObjectReference → ObjectSpaceObjectReference[]` |
| In-memory ref cache | Does not exist | Per-instance out-refs/in-refs cached alongside InstanceTracker entries |
| GC process | Does not exist | Reference-counting collector (empty in-refs on non-root = collect) |
| `ObjectSpaceObjectReference` | Does not exist | Value type representing `(short DimensionIndex, long RowIndex)` |

---

## Implementation Tasks

### Task 1: Fix `ReferenceSerializerMode` Bit Values

**File**: `Sphere10.Framework/Serialization/Special/ReferenceSerializerMode.cs`

- Change `SupportExternalReferences = 1 << 1` → `SupportExternalReferences = 1 << 2`.
- Update `Default = SupportNull | SupportContextReferences | SupportExternalReferences`.
- Verify no other code assumes the old bit layout.

### Task 2: Create `ObjectSpaceObjectReference` Struct + Serializer

**New file**: `Sphere10.Framework/ObjectSpaces/ObjectSpaceObjectReference.cs`

```
/// Globally unique object identifier within an ObjectSpace.
/// DimensionIndex = ordinal of the dimension (short).
/// ObjectIndex = row index within that dimension's recyclable list (long).
public readonly record struct ObjectSpaceObjectReference(short DimensionIndex, long ObjectIndex)
```

- Implement `IEquatable<ObjectSpaceObjectReference>`, `IComparable<ObjectSpaceObjectReference>`.
- Create a constant-size `IItemSerializer<ObjectSpaceObjectReference>` (10 bytes: 2 + 8).
- Register in `SerializerFactory.Default` or ensure auto-resolution works.

### Task 3: Create `[Root]` Attribute and Wire Into Dimension Definition

**New file**: `Sphere10.Framework/ObjectSpaces/Annotations/RootAttribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class RootAttribute : Attribute { }
```

**File**: `Sphere10.Framework/ObjectSpaces/ObjectSpaceDefinition.cs`

- Add `public bool IsRoot { get; set; }` to `DimensionDefinition`.

**File**: `Sphere10.Framework/ObjectSpaces/Builder/ObjectSpaceDimensionBuilder.cs`

- Add `private bool _isRoot;` field.
- Add `.AsRoot()` fluent method to mark the dimension as a root.
- In `BuildDefinition()`: set `IsRoot = _isRoot`.

**File**: `Sphere10.Framework/ObjectSpaces/Builder/ObjectSpaceBuilder.cs` — `AddDimension()`

- In the annotation-scanning block: check for `[Root]` attribute on the type and call `dimensionBuilder.AsRoot()` if present.

**File**: `Sphere10.Framework/ObjectSpaces/ObjectSpaceDefinition.cs` — `Validate()`

- Add validation: at least one root dimension must exist when GC is enabled.

### Task 4: Uncomment `ReferenceType.IsExternalReference` and Add Context Callbacks

**File**: `Sphere10.Framework/Serialization/Special/ReferenceSerializer.cs`

- Uncomment `IsExternalReference = 3` in the `ReferenceType` enum.
- In `ClassifyReferenceType`: after the context-reference check, if `_supportsExternalReferences` is true, call `context.ClassifyExternalReference?.Invoke(item)`. If it returns `IsExternal = true`, return `ReferenceType.IsExternalReference`.
- In `CalculateSize` / `Serialize`: for `IsExternalReference`, write the discriminator byte + the serialized `ObjectSpaceObjectReference` (10 bytes, constant size).
- In `Deserialize`: for `IsExternalReference`, read the `ObjectSpaceObjectReference` and call `context.ResolveExternalReference(ref)` to get the object instance.

**File**: `Sphere10.Framework/Serialization/SerialiationContext.cs`

- Add callback properties:
  - `Func<object, (bool IsExternal, ObjectSpaceObjectReference Reference)>? ClassifyExternalReference`
  - `Func<ObjectSpaceObjectReference, object>? ResolveExternalReference`
  - `List<ObjectSpaceObjectReference> CollectedOutRefs` — accumulated during serialization for the current root object.

**Key design point**: `ClassifyExternalReference` only returns `IsExternal = true` for objects whose CLR type is registered as a dimension in the ObjectSpace. For component objects (non-dimension types), it returns `false`, and they are serialized inline as usual. This is the sole point where "dimension object vs component object" is discriminated during serialization.

### Task 5: Implement `ObjectSpaceReferenceSerializer<T>`

**File**: `Sphere10.Framework/ObjectSpaces/ObjectSpaceReferenceSerializer.cs`

- Uncomment and implement. This is a subclass of `ReferenceSerializer<T>` that provides ObjectSpace-specific external reference handling.
- It holds a reference to the `ObjectSpace` (or an interface/callbacks extracted from it).
- Its role is to configure the `SerializationContext` callbacks (`ClassifyExternalReference`, `ResolveExternalReference`) before delegating to the base class.
- **Classification logic**: Given an object, check if its `GetType()` is a registered dimension type in the ObjectSpace. If yes → external reference. If no → component object, serialize inline (base class handles it as `IsNotNull`).
- **Resolution logic**: Given an `ObjectSpaceObjectReference`, use `ObjectSpace.Get(dimensionIndex, rowIndex)` to load or return the cached instance.

### Task 6: Eager ID Assignment + Extend InstanceTracker for ObjectSpaceObjectReference

**File**: `Sphere10.Framework/ObjectSpaces/InstanceTracker.cs`

Extend `InstanceTracker` to also track `ObjectSpaceObjectReference` per object:

- Add a parallel `Dictionary<object, ObjectSpaceObjectReference> _objectRefs` (keyed by reference equality).
- Add a parallel `Dictionary<ObjectSpaceObjectReference, object> _refToObject`.
- Add methods:
  - `void TrackRef(object item, ObjectSpaceObjectReference ref)`
  - `bool TryGetRef(object item, out ObjectSpaceObjectReference ref)`
  - `bool TryResolveRef(ObjectSpaceObjectReference ref, out object item)`
  - Update `Clear()` and `Untrack()` to clean both structures.

**File**: `Sphere10.Framework/ObjectSpaces/ObjectSpaceBase.cs`

- **`AcceptNewInternal`**: After `_instanceTracker.TrackNew(item)`, immediately compute and track the `ObjectSpaceObjectReference`:
  - `DimensionIndex` = ordinal index of the dimension for `itemType` (from `_dimensions`).
  - `ObjectIndex` = the negative index returned by `TrackNew`.
  - Call `_instanceTracker.TrackRef(item, new ObjectSpaceObjectReference(dimIdx, negativeIdx))`.
- **`SaveInternal`**: After saving (when actual `index` is known from `dimension.Container.Add`), update the tracked ref:
  - Call `_instanceTracker.TrackRef(item, new ObjectSpaceObjectReference(dimIdx, index))` (replaces the provisional negative-index ref).
- **`TryGet` (load path)**: After `_instanceTracker.Track(item, index)`, also track the ref with the real dimension index and row index.
- **Result**: The `ObjectSpaceObjectReference` for any tracked object is always known and always current. Serialization can look it up without lazy allocation.

### Task 7: In-Memory Reference Cache

**File**: `Sphere10.Framework/ObjectSpaces/InstanceTracker.cs` (or new dedicated type)

Add per-object in-memory caches for out-refs and in-refs:

- `Dictionary<object, HashSet<ObjectSpaceObjectReference>> _outRefs` — for each tracked object, what dimension objects does it reference?
- `Dictionary<object, HashSet<ObjectSpaceObjectReference>> _inRefs` — for each tracked object, what dimension objects reference it?

These are updated:
- **On save**: After serialization, the `CollectedOutRefs` from the `SerializationContext` become the new out-refs for the saved object. Diff against previous out-refs. Update in-refs of affected targets.
- **On load**: When loading an object, deserialize it (which resolves external references), then populate the out-refs cache from the deserialized references. Update in-refs of targets.
- **On delete**: Remove the object's out-refs. For each former target, remove self from target's in-refs.

**Diagnostic check on save**: After updating the persisted out-refs/in-refs indexes (Task 8), read back the persisted values and compare with in-memory cache. Log warning or throw (configurable) on mismatch. Use `Guard.Ensure` or a `Debug.Assert`-style check.

### Task 8: Create Persisted Out-Refs and In-Refs Global Indexes

**New files**:
- `Sphere10.Framework/ObjectSpaces/ObjectSpaceOutRefsIndex.cs`
- `Sphere10.Framework/ObjectSpaces/ObjectSpaceInRefsIndex.cs`

These are **ObjectSpace-level** (not per-dimension) indexes stored as reserved streams in the top-level `ClusteredStreams`.

**Out-refs index**: Maps `ObjectSpaceObjectReference → ObjectSpaceObjectReference[]` (one source object to all dimension objects it references).

**In-refs index**: Maps `ObjectSpaceObjectReference → ObjectSpaceObjectReference[]` (one target object to all dimension objects that reference it).

Implementation approach:
- Use `StreamMappedDictionary<ObjectSpaceObjectReference, ObjectSpaceObjectReference[]>` or equivalent, stored in reserved streams.
- Alternatively, implement as custom `IClusteredStreamsAttachment` registered on the top-level `_streams`.

**Update protocol on save** (called from `SaveInternal`):
1. Serialization completes → harvest `context.CollectedOutRefs` as the new out-refs set.
2. Read previous persisted out-refs for this object (if it was previously saved, i.e., index >= 0).
3. Diff: compute added refs and removed refs.
4. For each removed ref target: remove self from target's persisted in-refs.
5. For each added ref target: add self to target's persisted in-refs.
6. Replace the persisted out-refs entry.
7. **Diagnostic**: Compare persisted refs with in-memory cache (Task 7). Assert equality.

**Update protocol on delete** (called from `DeleteInternal`):
1. Read persisted out-refs for the deleted object.
2. For each out-ref target: remove self from target's persisted in-refs.
3. Remove the persisted out-refs entry for self.
4. Read persisted in-refs for the deleted object (objects that pointed to this one).
5. For each in-ref source: note that this source now has a dangling pointer (handled by GC in Task 9).
6. Remove the persisted in-refs entry for self.
7. **Cascade check**: For each former out-ref target, if the target is in a non-root dimension and its in-refs set is now empty → collect it (call `DeleteInternal` recursively or queue for collection).

### Task 9: Implement Garbage Collection

**New file**: `Sphere10.Framework/ObjectSpaces/ObjectSpaceGarbageCollector.cs` (or inline in `ObjectSpaceBase`)

**Algorithm**: Reference-counting style — **no mark-and-sweep needed**.

When an object is deleted (or when a save updates out-refs causing a target to lose its last in-ref):
1. Check each affected target: is it in a non-root dimension?
2. If yes: is its in-refs set now empty?
3. If yes: it's garbage → delete it (recycle its row, update its own out-refs/in-refs, which may cascade further).

This is essentially a cascading delete triggered by in-ref count reaching zero on non-root objects.

**Public API**:
- `ObjectSpace.CollectGarbage()` — explicit full scan: iterate all objects in non-root dimensions, collect any with empty in-refs. This is a safety net / consistency check, not the primary mechanism.
- The primary mechanism is the cascading check in `DeleteInternal` and `SaveInternal` (when saves change out-refs).

**Edge cases**:
- **Cyclic references between non-root objects**: If A→B→A and both are non-root, deleting the last external in-ref to A should collect A, which removes its out-ref to B, which makes B's in-refs empty, collecting B. Implement with a processing queue to avoid stack overflow.
- **Self-references**: An object referencing itself in a non-root dimension. The self-ref counts as an in-ref, so it won't be collected until something removes it. On delete, removing self clears the self-ref, then in-refs is checked.

**Triggering**:
- Automatic: cascading from `DeleteInternal` and `SaveInternal` ref updates.
- Manual: `ObjectSpace.CollectGarbage()` for full scan.
- Configurable via `ObjectSpaceTraits.GarbageCollect` flag (see Task 10).

**Considerations**:
- GC must run within an access scope and hold the lock.
- GC must update merkle trees if the ObjectSpace is merkleized.
- GC should be idempotent.

### Task 10: Add `ObjectSpaceTraits.GarbageCollect` Flag + Builder

**File**: `Sphere10.Framework/ObjectSpaces/ObjectSpaceTraits.cs`

- Add `GarbageCollect = 1 << 2` to the flags enum.

**File**: `Sphere10.Framework/ObjectSpaces/Builder/ObjectSpaceBuilder.cs`

- Add `private bool _garbageCollect;` field.
- Add `.WithGarbageCollection()` fluent method.
- Wire into `BuildDefinition()`: set `Traits |= ObjectSpaceTraits.GarbageCollect`.

### Task 11: Reserve Streams for Ref Indexes

**File**: `Sphere10.Framework/ObjectSpaces/ObjectSpaceBase.cs` — constructor and `LoadInternal()`

Currently, the top-level `ClusteredStreams` has reserved streams, and index 0 is used for the spatial merkle tree.

- Reserve additional streams for the out-refs and in-refs indexes (e.g., indices 1 and 2, or dynamically allocated).
- During `LoadInternal()`: if GC is enabled, create/load the out-refs and in-refs `StreamMappedDictionary` instances from the reserved streams.
- Register them as `IClusteredStreamsAttachment` on `_streams` if appropriate.
- Store references to them as fields on `ObjectSpace` for use by `SaveInternal`/`DeleteInternal`.

### Task 12: Update `SaveInternal` and `DeleteInternal`

**File**: `Sphere10.Framework/ObjectSpaces/ObjectSpaceBase.cs`

**SaveInternal** changes:
1. Before serializing, configure the `SerializationContext` with `ClassifyExternalReference` and `ResolveExternalReference` callbacks that use the ObjectSpace's dimension type set and `InstanceTracker`.
2. After serialization, harvest `context.CollectedOutRefs`.
3. Update in-memory ref cache (Task 7).
4. Update persisted out-refs/in-refs indexes (Task 8 protocol).
5. Diagnostic check: compare persisted vs in-memory refs.
6. If GC enabled: check if any former out-ref targets are now orphaned → collect.

**DeleteInternal** changes:
1. Before recycling, read the object's out-refs (from in-memory cache).
2. After recycling, update persisted out-refs/in-refs (Task 8 delete protocol).
3. Update in-memory caches.
4. If GC enabled: cascade-collect orphaned non-root targets.

### Task 13: Update `CreateItemSerializer`

**File**: `Sphere10.Framework/ObjectSpaces/ObjectSpaceBase.cs`

- Override `CreateItemSerializer(Type objectType)` to wrap the resolved serializer with `ObjectSpaceReferenceSerializer<T>` for types that have reference-type properties pointing to dimension types.
- Or more simply: configure the `SerializerFactory` so that for dimension types, the `ReferenceSerializerMode` includes `SupportExternalReferences`, and the context callbacks handle the rest. The key is ensuring that when a `CompositeSerializer` serializes a member whose type is a dimension type, the wrapping `ReferenceSerializer` has external reference support enabled.
- The simplest approach: in the `SerializerFactory` setup (during `LoadInternal` or builder), for each dimension type, register a serializer that is `ObjectSpaceReferenceSerializer<T>` wrapping the auto-built `CompositeSerializer<T>`.

### Task 14: Tests

**New test file**: `tests/Sphere10.Framework.Tests/ObjectSpaces/GarbageCollectionTests.cs`

Test cases:
1. **Root attribute**: Dimension marked with `[Root]` has `IsRoot = true` in definition.
2. **Eager ID assignment**: After `New<T>()`, the object has a valid `ObjectSpaceObjectReference` immediately (before save).
3. **Component vs dimension serialization**: An object property whose type is NOT a dimension is serialized inline. A property whose type IS a dimension gets an external reference.
4. **Basic ref tracking**: Save Account (root) referencing Identity (non-root). Verify out-refs(Account) contains Identity and in-refs(Identity) contains Account.
5. **In-memory cache consistency**: After save, in-memory refs match persisted refs.
6. **Delete cascading**: Delete Account. Identity's in-refs from Account are removed. If Identity has zero in-refs and is non-root → auto-collected.
7. **Root protection**: Root dimension objects are never auto-collected even with zero in-refs.
8. **Cyclic non-root references**: A→B→A (both non-root). Delete external reference to A. Both should be collected.
9. **Diamond references**: A(root)→B(non-root), A→C(non-root), B→D(non-root), C→D. Delete A. B, C, D all collected.
10. **Save updates refs**: Change Account.Identity from Identity1 to Identity2. After save, out-refs updated. If Identity1 has no other in-refs → collected.
11. **CollectGarbage() full scan**: Manually create orphaned non-root objects, call `CollectGarbage()`, verify they're removed.
12. **Mixed with persistence ignorance**: AutoSave + GC interaction — dirty objects saved on flush, refs updated, orphans collected.
13. **Merkleized + GC**: Ensure merkle trees are updated after GC collection.
14. **Diagnostic check**: Intentionally corrupt in-memory refs, verify diagnostic detects mismatch (if feasible in test).

---

## Suggested Implementation Order

1. **Task 1** — Fix `ReferenceSerializerMode` bit values (trivial, unblocks everything).
2. **Task 2** — Create `ObjectSpaceObjectReference` struct + serializer.
3. **Task 3** — Create `[Root]` attribute + wire into dimension definition/builder.
4. **Task 4** — Uncomment `IsExternalReference`, add context callbacks.
5. **Task 6** — Eager ID assignment + extend `InstanceTracker` for ref tracking.
6. **Task 5** — Implement `ObjectSpaceReferenceSerializer<T>`.
7. **Task 7** — In-memory reference cache.
8. **Task 13** — Update `CreateItemSerializer` to use external reference serializer.
9. **Task 10** — Add GC trait flag + builder method.
10. **Task 11** — Reserve streams for ref indexes.
11. **Task 8** — Implement persisted out-refs/in-refs indexes.
12. **Task 12** — Update Save/Delete to maintain indexes + cascade GC.
13. **Task 9** — Implement GC (reference-counting + full-scan fallback).
14. **Task 14** — Tests.

---

## Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                          ObjectSpace                                  │
│                                                                       │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐         │
│  │ Dimension 0     │  │ Dimension 1     │  │ Dimension N     │  ...   │
│  │ [Root] Account  │  │ Identity        │  │ (Type)          │        │
│  │ RowIdx 0..M     │  │ RowIdx 0..K     │  │ RowIdx 0..J     │       │
│  └────────────────┘  └────────────────┘  └────────────────┘         │
│                                                                       │
│  ObjectSpaceObjectReference = (DimensionIdx, RowIdx)                  │
│  Assigned eagerly on New<T>() and Get<T>()                            │
│                                                                       │
│  ┌─────────────────────────────────────────────────────────────┐     │
│  │ InstanceTracker (in-memory)                                  │     │
│  │   object ↔ long index (per type)                             │     │
│  │   object ↔ ObjectSpaceObjectReference                        │     │
│  │   object → HashSet<ObjectSpaceObjectReference> outRefs       │     │
│  │   object → HashSet<ObjectSpaceObjectReference> inRefs        │     │
│  └─────────────────────────────────────────────────────────────┘     │
│                                                                       │
│  ┌─────────────────────────────────────────────────────────────┐     │
│  │ Persisted Global Indexes (reserved streams in ClusteredStreams) │  │
│  │   Out-Refs: ObjRef → ObjRef[]                                │     │
│  │   In-Refs:  ObjRef → ObjRef[]                                │     │
│  └─────────────────────────────────────────────────────────────┘     │
│                                                                       │
│  ┌─────────────────────────────────────────────────────────────┐     │
│  │ GC (reference-counting)                                      │     │
│  │   On delete/save: check affected targets                     │     │
│  │   Non-root + empty in-refs → collect (cascading)             │     │
│  │   CollectGarbage() for full-scan safety net                  │     │
│  └─────────────────────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────────────────────┘
```

## Serialization Flow

### Dimension Object (External Reference)
```
Serializing Account { Identity = <Identity instance>, Name = "Bob" }:

1. ObjectSpace.SaveInternal configures SerializationContext with callbacks
2. CompositeSerializer serializes Account members sequentially
3. Name property → string serializer (component, inline as usual)
4. Identity property → ReferenceSerializer wraps inner serializer
5. ReferenceSerializer.ClassifyReferenceType:
   - Item is not null ✓
   - Not a context reference (first encounter) ✓
   - _supportsExternalReferences → context.ClassifyExternalReference(identity)
   - ObjectSpace checks: Identity type IS a registered dimension → YES
   - ObjectSpace looks up InstanceTracker: identity has ObjectSpaceObjectReference(1, 5)
   - Returns (IsExternal: true, Reference: (1, 5))
6. ReferenceSerializer writes: [0x03 (IsExternalReference)] + [ObjectSpaceObjectReference(1, 5)]
7. Context records out-ref: (1, 5)
8. After serialization: ObjectSpace harvests CollectedOutRefs = [(1, 5)]
9. Updates persisted Out-Refs[Account(0, X)] = [(1, 5)]
10. Updates persisted In-Refs[Identity(1, 5)] += [Account(0, X)]
11. Updates in-memory caches to match
12. Diagnostic: verify persisted == in-memory ✓
```

### Component Object (Inline Serialization)
```
Serializing Account { Address = <Address instance> }
   (where Address is NOT a registered dimension)

1. Address property → ReferenceSerializer wraps inner serializer
2. ReferenceSerializer.ClassifyReferenceType:
   - Item is not null ✓
   - Not a context reference (first encounter) ✓
   - _supportsExternalReferences → context.ClassifyExternalReference(address)
   - ObjectSpace checks: Address type is NOT a registered dimension → NO
   - Returns (IsExternal: false)
3. Falls through to ReferenceType.IsNotNull
4. Address is serialized inline within Account's stream (existing behavior, unchanged)
```

### Deserialization
```
Deserializing Account:

1. ReferenceSerializer reads discriminator for Identity property: 0x03 (IsExternalReference)
2. Reads ObjectSpaceObjectReference(1, 5)
3. Calls context.ResolveExternalReference(ref(1, 5))
4. ObjectSpace resolves: checks InstanceTracker first (cache hit → return)
5. If not cached: calls Get on dimension 1 at row 5, loads Identity, tracks it
6. Returns the Identity instance
7. Account.Identity is set to the resolved instance
```

### GC Cascade on Delete
```
Delete Account(0, 3) which references Identity(1, 5):

1. Read Account's out-refs: [(1, 5)]
2. Recycle Account row 3 from dimension 0
3. Remove Out-Refs[Account(0, 3)]
4. Remove Account(0, 3) from In-Refs[Identity(1, 5)]
5. In-Refs[Identity(1, 5)] is now empty
6. Dimension 1 (Identity) is non-root → Identity(1, 5) is garbage
7. Recursively delete Identity(1, 5):
   a. Read Identity's out-refs (if any)
   b. Recycle Identity row 5 from dimension 1
   c. Update/remove affected refs
   d. Check cascading targets...
8. Update in-memory caches throughout
```
