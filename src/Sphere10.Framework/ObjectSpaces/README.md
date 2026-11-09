# Object Spaces

An embedded, indexed store of typed CLR objects over clustered streams, in memory or in a transactional file.

`Sphere10.Framework.ObjectSpaces` is an embedded object store for typed CLR objects, built on clustered streams. Each registered type has a dimension with recyclable object slots, a serializer and member indexes, and the space tracks loaded objects so that each saved slot maps to one live instance. Use it when an application needs indexed, persistent objects without an external database. [ObjectSpaceBuilder](Builder/ObjectSpaceBuilder.cs) builds a [MemoryObjectSpace](MemoryObjectSpace.cs) or a transactional [FileObjectSpace](FileObjectSpace.cs), and both derive from `ObjectSpace`. The subsystem ships in the `Sphere10.Framework` package.

The [WinForms tester](../../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md) demonstrates a self-describing store on its **ObjectSpace** screen, which is in the **Data And Collections** block and under **Data Editing > ObjectSpace**. The screen creates, edits and deletes accounts and reloads the store through `ObjectSpace.TryOpenFromStream`. The general profiler's [ObjectSpace suite](../../../utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/README.md) holds repeatable measurements, and its [lab](../../../utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/Lab/README.md) supplies the models, configuration and factory used for profiling and correctness fixtures.

## Quick start

```csharp
using Sphere10.Framework;
using Sphere10.Framework.ObjectSpaces;

using var space = new ObjectSpaceBuilder()
	.UseMemoryStream()
	.AddDimension<Customer>()
		.Done()
	.Build();

var customer = space.New<Customer>();
customer.Id = 1;
customer.Email = "ana@example.com";
customer.Region = 7;
space.Save(customer);

var found = space.Get((Customer item) => item.Email, "ana@example.com");

public class Customer {
	[Identity]
	public long Id { get; set; }

	[UniqueIndex]
	public string Email { get; set; }

	[Index]
	public int Region { get; set; }
}
```

`AddDimension<T>()` reads the index annotations, and `Done()` registers an automatic serializer for the type. A memory space loads during `Build()`. `found` is the same instance as `customer`, because the space tracks object identity.

## Schema and construction

Declare dimensions, their indexes and serializers before loading:

- `AddDimension<T>()` reads the member annotations `[Identity]`, `[UniqueIndex]` and `[Index]`, and the class annotations `[Root]` and `[EqualityComparer]`. Pass `ignoreAnnotations: true` when `WithIdentifier`, `WithUniqueIndexOn` and `WithIndexOn` declare the complete index set.
- Every dimension already has its recyclable-slot store, so do not add a second one with `WithRecyclableIndexes()`.
- `Done()` registers an automatic serializer when the type has none. Automatic layouts follow the CLR members, `[Transient]` excludes a member, and the layouts are not a schema-migration facility.
- The builder registers serializers and comparers in its own copies of `SerializerFactory.Default` and `ComparerFactory.Default`, so application registrations stay local. `UsingSerializerFactory` and `UsingComparerFactory` install supplied factories instead. The builder rejects mixing a supplied factory with its own registrations, so call them before adding dimensions. Do not supply `SerializerFactory.Default` itself, because `Done()` registers missing automatic serializers in the supplied factory.
- `AutoSave()` requires every dimension to name its dirty flag with `WithChangeTrackingVia`.
- `Merkleized()` adds a Merkle attachment only to the dimensions that already exist, so call it after adding them.
- `WithGarbageCollection()` requires at least one root dimension, marked with `[Root]` or `AsRoot()`.

This file-backed example declares its indexes explicitly and uses a dirty flag for autosave:

```csharp
using System;
using System.Linq;
using Sphere10.Framework;
using Sphere10.Framework.ObjectSpaces;

public class Purchase {
	private decimal _total;

	public long Id { get; set; }

	public string Reference { get; set; }

	public int Region { get; set; }

	public decimal Total {
		get => _total;
		set {
			if (_total == value)
				return;
			_total = value;
			Dirty = true;
		}
	}

	[Transient]
	public bool Dirty { get; set; }
}

public static class PurchaseExample {
	public static Purchase[] SaveAndRead(string filePath, string pagesFolder) {
		var builder = new ObjectSpaceBuilder()
			.UseFile(filePath)
			.KeepUncommittedPagesAt(pagesFolder)
			.WithClusterSize(Sphere10FrameworkDefaults.FileObjectSpaceClusterSize)
			.WithPageSize(Sphere10FrameworkDefaults.ObjectSpacePageSize)
			.WithMaxMemory(Sphere10FrameworkDefaults.ObjectSpaceMaxMemory)
			.WithIndexBTreeOrder(Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder)
			.AutoSave();
		builder.AddDimension<Purchase>(ignoreAnnotations: true)
			.WithIdentifier(item => item.Id)
			.WithUniqueIndexOn(item => item.Reference)
			.WithIndexOn(item => item.Region)
			.WithChangeTrackingVia(item => item.Dirty)
			.OptimizeAssumingAverageItemSize(Sphere10FrameworkDefaults.FileObjectSpaceDimensionClusterSize)
			.Done();
		using var space = builder.Build();
		space.Load();
		using (space.EnterAccessScope()) {
			var purchase = space.New<Purchase>();
			purchase.Id = 1;
			purchase.Reference = "PO-0001";
			purchase.Region = 7;
			purchase.Total = 125m;
			space.Save(purchase);
			((FileObjectSpace)space).Commit();
			return space.GetAll<Purchase>().ToArray();
		}
	}
}
```

A file space loads on `Load()` unless the builder called `AutoLoad()`. The example expects caller-selected paths and a fresh store, so repeating the insertion against existing data rejects the duplicate identifier and reference. Autosave does not detect property changes by itself. The model must set its configured dirty member whenever a persisted field changes, and here only `Total` does that, so edits to other fields need an explicit `Save` or `Dirty = true`.

## Object identity, CRUD and enumeration

- `New<T>()` creates and tracks a provisional instance, and `AcceptNew(item)` tracks an existing new instance. Assign application identifiers before saving, because the identifier index does not generate them.
- `Save(item)` inserts a new or untracked instance, or updates a tracked one, and returns its physical slot. It updates the member indexes and clears the configured dirty flag.
- `Get<T>(slot)` and `TryGet<T>(slot, out item)` resolve through the identity tracker, so repeated retrieval within one loaded space returns the same instance.
- `Get` and `TryGet` with a member expression use the unique or identifier index named after that member, and they also match unsaved provisional instances. They do not run arbitrary predicate queries.
- `Delete(item)` recycles a saved slot or discards a provisional instance, so the live object count and the physical slot count differ.
- `GetAll<T>()` returns a materialized snapshot of the saved live objects in slot order. It skips recycled slots, excludes provisional instances and returns the tracked instances rather than copies.

`Count<T>()` includes tracked provisional objects, while `GetAll<T>()` includes only saved ones, so do not enumerate `0..Count-1` as storage positions. A dimension's `Container` exposes the physical `ListCount`, `RecycledCount` and live `Count` for diagnostics. Reading raw objects from the container bypasses identity tracking, so retrieve records through the space before saving or deleting them.

For a nonunique index, resolve the matching slots under the space's access scope and retrieve each record through the space:

```csharp
using System.Linq;
using Sphere10.Framework.ObjectSpaces;

public static class PurchaseQueries {
	public static Purchase[] InRegion(ObjectSpace space, int region) {
		using (space.EnterAccessScope()) {
			var dimension = (ObjectSpace.Dimension<Purchase>)space.Dimensions.Single(item => item.Definition.ObjectType == typeof(Purchase));
			var positions = dimension.Container.ObjectStream.GetIndexFor(item => item.Region).Values[region].ToArray();
			return positions.Select(space.Get<Purchase>).ToArray();
		}
	}
}
```

Select the typed dimension with generic enumeration, as the example does. `Dimensions` is backed by a `DictionaryList` whose non-generic enumerator yields key/value pairs, so `Dimensions.OfType<ObjectSpace.Dimension<T>>()` returns nothing. The member-expression index methods look an index up by member name, so an index with an explicit name needs `ObjectStream<T>.TryGetIndex<TMember>(name, out index)` or `TryGetUniqueIndex<TMember>(name, out index)`.

Bind UI grids to detached editable rows. Keep slot or generation checks so that a row retained across rollback, reopen or slot reuse cannot update a different record. Validate unique fields before changing a tracked instance, and `Delete` a failed provisional instance so that a later autosave cannot publish it.

## Durability, scopes and ownership

A memory space flushes into its backing `MemoryStream` and has no file transaction. A supplied `MemoryStream` remains caller-owned, while a space built without one owns the stream it creates. Flush before taking a byte snapshot. Disposal flushes a memory space unless an exception is in flight.

A file space writes through a `TransactionalStream` that stages pages in the folder set by `KeepUncommittedPagesAt`, which defaults to `.txn` in the file's directory. `Save` and `Flush` update pending pages, and `Commit()` publishes them. `Rollback()` unloads and reloads the space, which invalidates every tracked instance and any UI or query snapshot derived from them. Disposing a file space neither flushes nor commits, so commit or roll back pending work before replacement or shutdown. `FileObjectSpace.CommitAsync()` and `RollbackAsync()` throw `NotSupportedException`.

Dimension streams hold their access locks on the creating thread for the store's lifetime. Create, operate and dispose a store on one owner thread. A UI session dispatches commands to a dedicated storage worker, because separate `Task.Run` calls on an existing store can deadlock. An access scope must stay on its thread and never cross `await`. Serialize session operations and check cancellation between records rather than interrupting a mutation.

Autosave saves tracked dirty objects during `Flush` and during a file commit. A change visible through the identity cache does not prove that bytes were persisted, so verify durability by reopening. `Merkleized()` installs a Merkle attachment for each dimension and a root tree over them, so flush or commit before reading a settled root.

## New-store defaults and existing storage

New stores take the ObjectSpace-specific constants in [Sphere10FrameworkDefaults](../Sphere10Defaults.cs), which were selected for Balanced throughput. The generic collection and index defaults are separate.

| Setting | Memory | File | Constant |
|---|---|---|---|
| Member-index B-tree order | 256 | 256 | `ObjectSpaceIndexBTreeOrder` |
| Outer cluster bytes | 1024 | 4096 | `MemoryObjectSpaceClusterSize` / `FileObjectSpaceClusterSize` |
| Default dimension cluster bytes | 1024 | 256 | `MemoryObjectSpaceDimensionClusterSize` / `FileObjectSpaceDimensionClusterSize` |
| Outer policy | `CacheClusterHeaders \| IntegrityChecks` | `Default` | `MemoryObjectSpaceContainerPolicy` / `FileObjectSpaceContainerPolicy` |
| File page bytes | N/A | 262144 (256 KiB) | `ObjectSpacePageSize` |
| File page-cache ceiling | N/A | 16777216 (16 MiB) | `ObjectSpaceMaxMemory` |

The default member-index specification combines `BTreeSpec.Default.Type` (`BTreeType.BTree`) with `ObjectSpaceIndexBTreeOrder`. Autosave, Merkle trees and graph garbage collection remain opt-in, and index membership remains part of the caller's schema.

### Reopening existing stores

The defaults apply when a store is created and never migrate an existing one. Index storage records neither the B-tree type nor its order, so a mismatched value can reinterpret old node bytes. Structural checks can reject some incompatible node layouts, but they do not authenticate the supplied type. Reopen a store with its original configuration:

- The same dimension order, serialized members and type registrations, index names, kinds and row order, endianness, Merkle traits and hash function.
- The original B-tree type and order of every member index. Use `WithIndexBTreeSpec(originalSpec)` for one shared layout. For indexes with different layouts, replace the dimension's index rows through `IObjectSpaceDimensionBuilder.WithIndexes` and give each row its original `BTreeSpec`. Recyclable-slot and Merkle attachments have no B-tree specification. Stores written with the historical defaults used order 64, with B-trees for fixed-size unique and identifier indexes and B+ trees for the other member indexes.
- The original outer cluster size and page size of a file store. A memory store reads its outer cluster size from the stored header when `WithClusterSize` is omitted.
- Dimensions without `OptimizeAssumingAverageItemSize` read their stored cluster sizes, and an explicit value must match the stored size after clipping.

The builder records no difference between explicit and default settings, so supply every original value. Opening rejects a dimension whose stored attachment count differs from the supplied schema instead of adopting the changed schema. Changing a stored index's type or order requires rebuilding the data into new storage.

This example reopens the Purchase store from the construction example with caller-retained original settings:

```csharp
public static class ExistingPurchaseStore {
	public static FileObjectSpace OpenExistingPurchases(
		string filePath, string pagesFolder, int originalOrder, int originalOuterClusterSize,
		int originalDimensionClusterSize, long originalPageSize, long pageCacheBytes, Endianness originalEndianness
	) {
		var builder = new ObjectSpaceBuilder()
			.UseFile(filePath)
			.KeepUncommittedPagesAt(pagesFolder)
			.WithIndexBTreeOrder(originalOrder)
			.WithClusterSize(originalOuterClusterSize)
			.WithPageSize(originalPageSize)
			.WithMaxMemory(pageCacheBytes)
			.WithEndianness(originalEndianness)
			.AutoSave()
			.AutoLoad();
		builder.AddDimension<Purchase>(ignoreAnnotations: true)
			.WithIdentifier(item => item.Id)
			.WithUniqueIndexOn(item => item.Reference)
			.WithIndexOn(item => item.Region)
			.WithChangeTrackingVia(item => item.Dirty)
			.OptimizeAssumingAverageItemSize(originalDimensionClusterSize)
			.Done();
		return (FileObjectSpace)builder.Build();
	}
}
```

For a store written with the historical defaults, pass 64, 4096 and 256 as the order, outer cluster size and dimension cluster size, keep the original page size, and supply per-index specifications for its mixed tree types. The example assumes one shared tree type. The page-cache budget is not part of the stored layout, so it can differ from the original.

To reopen a `MemoryStream`, declare the same schema with `.WithIndexBTreeOrder(originalOrder)` and call `.UseMemoryStream(stream)`. Omitted root and dimension sizes come from the stored headers, and explicit sizes must match them. Supply an expandable stream when the data will grow. `new MemoryStream(bytes)` has a fixed capacity, so copy the bytes into a new empty `MemoryStream` and reset its position first, because spare space in a cluster does not guarantee that the next write fits.

Every `Build()` constructs a new ObjectSpace and definition from the configured values. The builder keeps no earlier result, target history or load callbacks, so opening the same file or stream twice is the caller's responsibility. Keep application-owned, versioned manifests when stored data must outlive application or schema versions.

### Self-describing stores

`.StoreMetaData()` sets `ObjectSpaceTraits.MetaData`, which makes the space and each dimension's ObjectStream self-describing. The ObjectSpace record holds the hash function and one serializer factory for all dimensions, and each dimension record holds its item type, indexes and change tracking. `.StoreMetaData(capacity)` reserves a metadata budget in bytes, zero allocates exactly the record size, and a negative value is rejected. Metadata cannot record custom comparer registrations or delegate-based change tracking, so creation rejects them.

`ObjectSpace.TryOpenFromStream(rawStream, endianness, out objectSpace)` opens such a store without a supplied definition. It scans the metadata from the stream's current position, rebuilds the builder configuration, returns to that position and loads through the normal path. The caller supplies the original byte order, because opening does not detect it. The method returns `false` for unavailable or invalid metadata and for a mismatched byte order, while I/O and deserializer exceptions propagate. It borrows the stream, so dispose the returned space before reopening the stream.

Stores without metadata still open with a supplied definition, which neither adds metadata nor migrates their bytes. Reopening a metadata store with a supplied definition checks that definition against the stored records, including each index's `BTreeSpec` and row order, before any object loads. [Self-describing storage](../../../docs/usage/self-describing-storage.md) documents the header fields, the record formats and the difference between metadata capacity and used length.

### Default selection evidence

The defaults follow the Balanced mixed workload of the profiler's retained [study workflow](../../../utils/Sphere10.Framework.Utils.Profiler/Studies/README.md), whose original captures are not in the repository. In those runs, order 256 beat 128 for Memory with nonoverlapping intervals but overlapped it for File, and the Memory settings accepted about 2.8 times the encoded store bytes of a small-payload check in return for mixed throughput and lower allocations. The File page and cache sizes are a practical choice among overlapping candidates, so a File workload can reasonably choose `WithIndexBTreeOrder(128)` or other explicit settings.

## Configuration and performance

| Setting | Effect and comparison boundary |
|---|---|
| `WithIndexBTreeSpec(new BTreeSpec(type, order))` | Replaces the shared immutable specification used by member indexes that have no per-index specification. `BTreeSpec` rejects an undefined `BTreeType` and an order below 3. Metadata stores record every index's complete specification. |
| `WithIndexBTreeType(type)` | Replaces the shared specification with `type` and the current order. |
| `WithIndexBTreeOrder(order)` | Replaces the shared specification with the current type and `order`, the maximum child count of index B-trees and B+ trees. The minimum is 3 and the new-store default is 256. A steady node holds at most `order - 1` keys, and larger nodes trade tree depth against node search, serialization and write amplification. The order is not a byte length or a leaf count. |
| `WithClusterSize(bytes)` | Outer clustered-stream allocation size. Compare small and large payloads and fragmentation workloads. |
| `OptimizeAssumingAverageItemSize(bytes)` | Sets the dimension's inner cluster size, clipped to `SmallestRecommendedClusterSize` and `LargestRecommendedClusterSize` (currently 256 to 8192 bytes). Without it, a new dimension uses its backend default and an existing dimension keeps its stored size. Record the effective size. |
| `WithPageSize(bytes)` | File transaction page size, which affects I/O and commit amplification. It has no effect on memory stores. |
| `WithMaxMemory(bytes)` | File page-cache budget. Unloaded pages release their buffers, but the budget caps neither the identity tracker nor process memory. |
| `WithClusteredStreamsPolicy(policy)` | Outer container caching, allocation and integrity checks. Dimension containers use `FastAllocate` regardless of this setting. |
| Index membership and key shape | Identifier, fixed-size and checksum-based variable-size indexes cost differently. Compare writes as well as lookups. |
| `AutoSave()`, `Merkleized()`, `UsingHashFunction`, `WithEndianness` | Change flush, integrity and encoding work. Merkle settings, the hash function and the byte order are part of the stored format. Compare each supported variant on its own before combining them. |

`UsingHashFunction` selects the Merkle digest. Member-checksum indexes use their own default checksum, so changing the hash does not change checksum-index hashing.

Start with independent parameter sweeps, then compare shortlisted combinations with representative data and insertion order. Measure warm identity-cache hits, first-touch hydration, indexed misses, scans, indexed and nonindexed updates, slot recycling, flush, durable commit, rollback and reopen separately. Record allocations, file size, machine, runtime and the full effective configuration, because a small benchmark does not establish a universal best configuration. The [profiler guide](../../../utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/README.md) gives reproducible commands, the [study workflow](../../../utils/Sphere10.Framework.Utils.Profiler/Studies/README.md) screens and confirms candidates, and the [profiling practices](../../../docs/guidelines/patterns/object-spaces.md#profile-real-work) describe how to report them. The [lab guide](../../../utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/Lab/README.md) owns the versioned configuration sidecar that its factory checks before it opens a lab file, which is not an ObjectSpace file-format feature.

## Current boundaries

Reference-aware serializers support shared and cyclic inline components. Cross-dimension external-reference garbage collection (`WithGarbageCollection()`) is experimental, because its reference maps are neither persisted nor rehydrated, collection has physical-slot edge cases, and reopened external cycles need stronger coverage. The profiling lab and configuration-ranking suites keep it disabled, so they do not test persistent graph collection.

Custom comparers need their own index tests, because `ComparerFactory.GetComparer` currently looks up the equality-comparer registry, as [collections and storage](../../../docs/guidelines/patterns/collections-and-storage.md#comparers--equality) describes. Verify key equality, ordering and reopen behavior before relying on a domain-specific comparer.

## Tests

NUnit coverage is in [ObjectSpaces](../../../tests/Sphere10.Framework.Tests/ObjectSpaces), [B-tree collections](../../../tests/Sphere10.Framework.Tests/Collections/BTree) and the [profiling lab tests](../../../tests/Sphere10.Framework.Utils.Profiler.Tests/README.md). This command runs the fixtures in the `Sphere10.Framework.Tests.ObjectSpaces` namespace from the Framework root:

```powershell
dotnet test tests/Sphere10.Framework.Tests/Sphere10.Framework.Tests.csproj -c Release -p:BuildRevision=0 --filter FullyQualifiedName~Sphere10.Framework.Tests.ObjectSpaces
```

Read the [ObjectSpace practices](../../../docs/guidelines/patterns/object-spaces.md) before extending this subsystem.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`FileObjectSpace`](FileObjectSpace.cs) | class | Object space over a transactional file whose `Commit` and `Rollback` publish or discard staged pages, with commit and rollback events. |
| [`MemoryObjectSpace`](MemoryObjectSpace.cs) | class | Object space over a `MemoryStream` that it creates and owns, or borrows from the caller. |
| [`ObjectSpace`](ObjectSpaceBase.cs) | class | Core store that loads dimensions, tracks object identity, saves, deletes and enumerates objects, runs index lookups and optional reference collection. |
| [`ObjectSpaceDefinition`](ObjectSpaceDefinition.cs) | class | Schema of a space: traits, hash function, metadata capacity, shared index B-tree specification and dimension and index definitions, with `Validate`. |
| [`ObjectSpaceMetaData`](ObjectSpaceMetaData.cs) | class | Sealed self-describing record of a space's hash function and shared serializer factory. |
| [`ObjectSpaceMetaDataSerializer`](ObjectSpaceMetaDataSerializer.cs) | class | Sealed serializer for the length-prefixed `ObjectSpaceMetaData` record. |
| [`ObjectSpaceObjectReference`](ObjectSpaceObjectReference.cs) | record | Readonly record struct of a dimension index and object slot that identifies one object across dimensions. |
| [`ObjectSpaceObjectReferenceSerializer`](ObjectSpaceObjectReferenceSerializer.cs) | class | Sealed constant-size serializer that writes a reference as 10 bytes. |
| [`ObjectSpaceTraits`](ObjectSpaceTraits.cs) | enum | Flags for Merkle trees, autosave, garbage collection, stored metadata and big-endian storage. |

### Annotations

| Type | Kind | Purpose |
|---|---|---|
| [`DimensionAttribute`](Annotations/DimensionAttribute.cs) | class | Class attribute that marks a type as a dimension. The builder does not read it. |
| [`EqualityComparerAttribute`](Annotations/UseEqualityComparer.cs) | class | Class or struct attribute naming the equality comparer type that `AddDimension` registers for the type. |
| [`IdentityAttribute`](Annotations/IdentityAttribute.cs) | class | Member attribute that declares the dimension's identifier index, with an optional index name. |
| [`IndexAttribute`](Annotations/IndexAttribute.cs) | class | Member attribute that declares a nonunique index. |
| [`IndexAttributeBase`](Annotations/IndexAttributeBase.cs) | abstract class | Base member attribute with an optional index name and an `IndexNullPolicy`, which defaults to ignoring null keys. |
| [`RootAttribute`](Annotations/RootAttribute.cs) | class | Class attribute that marks a dimension as a garbage-collection root whose objects are never collected automatically. |
| [`UniqueIndexAttribute`](Annotations/UniqueIndexAttribute.cs) | class | Member attribute that declares a unique index. |

### Builder

| Type | Kind | Purpose |
|---|---|---|
| [`IObjectSpaceDimensionBuilder`](Builder/IObjectSpaceDimensionBuilder.cs) | interface | Untyped dimension configuration: root flag, indexes, comparers, change tracking, cluster size, definition building and `Done`. |
| [`IObjectSpaceDimensionBuilder<TBuilder>`](Builder/IObjectSpaceDimensionBuilderT.cs) | interface | Fluent dimension configuration that returns the concrete builder type and adapts it to the untyped interface. |
| [`ObjectSpaceBuilder`](Builder/ObjectSpaceBuilder.cs) | class | Fluent configuration of backend, storage sizes, B-tree layout, traits, factories and dimensions, ending in `Build` or `BuildDefinition`. |
| [`ObjectSpaceDimensionBuilder<T>`](Builder/ObjectSpaceDimensionBuilder.cs) | class | Fluent configuration of one typed dimension's serializer, comparers, identifier, indexes, Merkle and recyclable attachments, change tracking and item size. |
