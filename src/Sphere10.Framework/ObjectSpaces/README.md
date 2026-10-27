# Object Spaces

`Sphere10.Framework.ObjectSpaces.ObjectSpace` is a typed embedded object store over clustered streams. Each registered CLR type has a dimension with recyclable object slots, serializers and member indexes. Use [ObjectSpaceBuilder](Builder/ObjectSpaceBuilder.cs) to build a [MemoryObjectSpace](MemoryObjectSpace.cs) or transactional [FileObjectSpace](FileObjectSpace.cs).

The [WinForms tester](../../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md) contains its original ObjectSpace demonstration under **Data and collections > Data editing > ObjectSpace**. Repeatable performance measurements belong to the [ObjectSpace suite](../../../utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/README.md) in the general profiling project. Its [ObjectSpaces/Lab](../../../utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/Lab/README.md) folder contains configuration, factory and models used for profiling and correctness fixtures.

## Schema and construction

Declare dimensions, their indexes and serializers before loading. `AddDimension<T>()` reads `[Identity]`, `[UniqueIndex]`, `[Index]` and related annotations. Pass `ignoreAnnotations: true` when configuring the complete index definition explicitly. Each dimension already receives its recyclable-index store; do not add a second one. `Done()` registers an automatic serializer when the type has none.

This complete example uses explicit identifiers and a dirty flag for autosave:

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

The example expects caller-selected storage paths and a fresh compatible store. Repeating the insertion against existing data rejects the duplicate identifier/reference. Autosave does not infer every property mutation: the model must mark its configured dirty member when any persisted field changes. Here only `Total` supports later automatic edits; other field edits require explicit `Save` or setting `Dirty`.

`UsingSerializerFactory` and `UsingComparerFactory` install supplied factories. Keep application registrations local rather than modifying the global defaults. Automatic serializer layouts follow CLR members; they are not an automatic schema-migration facility. Preserve dimension order, member layouts/type registrations, indexes, endianness and allocation settings when reopening existing bytes.

## New-store defaults and existing storage

The ObjectSpace-specific constants in [Sphere10FrameworkDefaults](../Sphere10Defaults.cs) select **Balanced throughput** for new stores. Generic collection/index defaults remain separate.

| Setting | Memory | File | Constant owner |
|---|---|---|---|
| Member-index B-tree order | 256 | 256 | `ObjectSpaceIndexBTreeOrder` |
| Outer cluster bytes | 1024 | 4096 | `MemoryObjectSpaceClusterSize` / `FileObjectSpaceClusterSize` |
| Default dimension cluster bytes | 1024 | 256 | `MemoryObjectSpaceDimensionClusterSize` / `FileObjectSpaceDimensionClusterSize` |
| Outer policy | `CacheClusterHeaders \| IntegrityChecks` | `Default` | `MemoryObjectSpaceContainerPolicy` / `FileObjectSpaceContainerPolicy` |
| File page bytes | N/A | 262144 (256 KiB) | `ObjectSpacePageSize` |
| File page-cache ceiling | N/A | 16777216 (16 MiB) | `ObjectSpaceMaxMemory` |

The shared order is a power of two. The historical 128/256 comparison favors 256 for Memory with nonoverlapping reported intervals. File's lower point mean at 128 overlaps 256, which allocates slightly less; the shared default follows the resolved Memory result. File workloads can explicitly select 128 with `WithIndexBTreeOrder(128)`; neither candidate is a uniquely established File optimum.

Autosave, Merkle and graph GC remain opt-in. Index membership remains part of the caller's schema. The Memory choice favors the measured mixed throughput and lower cumulative allocations while accepting roughly **2.8 times the encoded store bytes** in the historical small-payload size check; this is not a reduction in resident-memory usage. File 256KiB/16MiB is a practical measured budget in the fast interval of the historical large-payload cache-pressure study, with overlap among other candidates. It is not a uniquely resolved optimum. Read-heavy, write-heavy, larger payloads and different key shapes can justify explicit overrides; see the [study workflow](../../../utils/Sphere10.Framework.Utils.Profiler/Studies/README.md). Original captures are stored outside the repository; measurement conclusions describe those runs rather than current-machine guarantees.

These defaults do not migrate existing stores. A fresh builder or unbound definition opening existing bytes must declare the **original** index order. Historical default layouts used 64; custom layouts need their own original value. An index header does not store/authenticate order, so selecting 256 just because it is the new default can reinterpret old node offsets. An explicitly configured or already successfully bound logical definition supplies its chosen order; it must still match the store being opened.

Retain the original schema/member/index definitions, endianness, Merkle traits and transaction-page configuration. File opening requires the original outer cluster size in its descriptor/builder. For supplied MemoryStreams, an omitted outer cluster size reads its existing header after order eligibility is checked. Dimensions with omitted size hints read their stored cluster sizes; explicit hints must match after sanitization. Header reading preserves stream position. Dimension opening validates the reserved stream count against the actual registered attachments, including composite indexes' child stores, instead of adopting a changed schema.

For example, reopening the same Purchase schema and autosave mode from the construction example uses caller-retained original settings:

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

For a historical default file, the original order/outer/dimension values are 64/4096/256; preserve its original page size as well. The original generic cache ceiling was `int.MaxValue`, populated lazily; page-cache budget can be chosen separately from persisted node/cluster layout. For an existing MemoryStream, declare `.WithIndexBTreeOrder(originalOrder)` on the same schema builder and call `.UseMemoryStream(stream)`; omitted root/dimension sizes use the stored headers. Explicit sizes remain authoritative and must match.

Use an expandable stream when reopened data will grow. `new MemoryStream(bytes)` has fixed capacity; copy the existing bytes into a new empty `MemoryStream` and reset its position before supplying it to the builder. Spare space in an existing cluster does not guarantee that the next write fits within a fixed buffer.

A builder retains successful configuration for its matching normalized file path plus transaction-folder context, or its matching supplied MemoryStream instance. Switching targets does not promote settings from an unrelated store into a declared opening configuration. A new stream created by `.UseMemoryStream()` receives fresh defaults. File preflight rejects an unknown order before transaction construction/recovery, including committed files of length 0 with persisted logical pages; constructors/property access are not a storage-migration mechanism. Maintain application-owned versioned manifests when persisted data must outlive application/schema versions.

## Object identity, CRUD and enumeration

- `New<T>()` creates and tracks a provisional instance. `AcceptNew(item)` tracks an existing new instance. Set application identifiers before saving; the identifier index does not generate business IDs.
- `Save(item)` inserts or updates a tracked instance and returns its physical slot. It updates member indexes and clears the configured dirty flag.
- `Get<T>(slot)` and `TryGet<T>(slot, out item)` resolve through the identity tracker. Repeated retrieval within one loaded space returns the same object instance.
- Member-expression `Get`/`TryGet` use a configured unique or identifier index. They do not perform arbitrary predicate queries.
- `Delete(item)` recycles the saved slot. Live object count and physical slot count therefore differ.
- `GetAll<T>()` materializes saved live objects under an access scope, skips recycled holes and retains identity-tracked objects. It excludes unsaved provisional instances. Its snapshot contains borrowed object references, not deep clones.

`Count<T>()` includes tracked provisional objects; `GetAll<T>()` includes only saved objects. Do not enumerate `0..Count-1` as storage positions. The dimension's container exposes physical `ListCount`, `RecycledCount` and live `Count` for diagnostics. Reading its raw objects directly bypasses ObjectSpace identity tracking; use ObjectSpace retrieval for records that will be saved or deleted.

For a nonunique index, obtain the typed dimension and materialize its matching positions while holding the space's access scope:

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

Use generic enumeration of `Dimensions` as above. Its current backing dictionary-list has a different non-generic enumerator; `Dimensions.OfType<Dimension<T>>()` can therefore yield no matching dimensions. Explicitly named indexes use `ObjectStream.GetIndex<TMember>(name)` / `GetUniqueIndex<TMember>(name)` rather than member-name convenience methods.

Bind UI grids to detached editable rows. Keep generation/slot checks so rows retained before rollback, reopen or slot reuse cannot update a different live record. Validate unique fields before changing a tracked instance, and remove a failed provisional create so a later autosave cannot publish it.

## Durability, scopes and ownership

Memory spaces flush into their backing memory stream. A supplied `MemoryStream` is caller-owned; the default constructor owns the stream it creates. Flush before taking a byte snapshot. Memory storage has no file transaction.

File spaces use a transactional stream with a page staging directory. `Save` and `Flush` update pending pages; `Commit()` publishes them. `Rollback()` unloads and reloads the space, invalidating previous tracked instances and their derived UI/query snapshots. Explicitly commit or roll back pending work before replacement or shutdown. File-space disposal does not flush dirty objects as a durability decision.

Transactional byte pages allocate owned `MemoryBuffer` arrays when loaded. Flush still purges the page cache; unloading clears each buffer and releases its reference to the backing array. Public spans keep their original array alive across unloads, without rental tracking or pool reuse. `WithMaxMemory` remains a cache estimate rather than a process-memory ceiling.

`FileObjectSpace.CommitAsync()` and `RollbackAsync()` currently throw `NotSupportedException`. Current dimension streams also retain access locks on the store's creation thread for their lifetime. Create, operate and dispose the store on one owner thread. A UI session dispatches commands to a dedicated worker; separate arbitrary `Task.Run` calls on an existing store can deadlock. BDN fixtures likewise create/use/dispose on their synchronous benchmark thread. An operation's access scope must remain on that thread and never cross `await`. Serialize session operations and honor cancellation between records, rather than interrupting an in-progress mutation.

Autosave saves tracked dirty objects during `Flush` and file commit. Changes visible through the identity cache are not proof of persisted bytes; verify durability by reopening. Merkle configuration installs per-dimension and root integrity attachments; flush/commit before reading a settled root.

## Configuration and performance

| Setting | Effect and comparison boundary |
|---|---|
| `WithIndexBTreeSpec(new BTreeSpec(type, order))` | Supplies a complete immutable specification for every member index. Its get-only type is always `BTreeType.BTree` or `BTreeType.BTreePlus`; construction rejects an undefined type or order below 3. If only the order is known, use `WithIndexBTreeOrder(order)` and the existing order-based index overloads. Type and order are supplied again when reopening; neither is persisted or autodetected. |
| `WithIndexBTreeType(type)` | Convenience method that creates a complete specification with the selected type and current order. Selecting only the type does not declare the original order required for opening existing storage. |
| `WithIndexBTreeOrder(order)` | Maximum child count in index B-trees/B+ trees; minimum 3, new-store ObjectSpace default 256. Steady nodes hold at most `order - 1` keys; total leaf count depends on occupancy/data. Larger nodes trade tree depth against node search, serialization and write amplification. This is an order, not a byte length or requested leaf count. |
| `WithClusterSize(bytes)` | Outer clustered-stream allocation size. Compare small/large payload and fragmentation workloads. |
| `OptimizeAssumingAverageItemSize(bytes)` | Actually chooses each dimension's inner cluster size, clipped to recommended bounds (currently 256–8192 bytes). New dimensions use their backend default when omitted; existing dimensions retain their stored size. Record the effective size. |
| `WithPageSize(bytes)` | File transactional-page granularity; affects I/O and commit amplification. Not a memory-backend tuning axis. |
| `WithMaxMemory(bytes)` | File page-cache budget. It does not cap the identity tracker or total process memory. |
| `WithClusteredStreamsPolicy(policy)` | Outer descriptor/header caching, allocation and integrity checks. Dimension containers currently use their own `FastAllocate` policy. |
| Index membership/key shape | Identifier, fixed-size unique/nonunique and checksum-based variable-key indexes incur different costs. Compare writes as well as lookups. |
| `AutoSave`, `Merkleized`, hash/endianness | Change write/flush/integrity work and persisted format. Compare supported variants independently before combining them. |

`UsingHashFunction` selects the Merkle digest. Current member-checksum indexes create their own default digest independently; changing this setting is not a checksum-index algorithm sweep.

Keep the exact B-tree order when reopening; the eligibility guard requires a declared original value or a bound definition and does not authenticate an explicitly supplied wrong value. Existing index headers do not record/validate order; changing it can reinterpret node bytes. The profiling factory records a versioned configuration sidecar and rejects mismatched or unknown file layouts before opening them. That lab sidecar is not a new general ObjectSpace file-format guarantee.

The profiling lab's version 4 manifest records an optional complete `IndexBTreeSpec` alongside the bare `IndexBTreeOrder`. Matching duplicate orders are accepted; conflicting orders are rejected. An omitted specification uses order-only APIs. It explicitly reads earlier manifests, including version 3's separate order/type properties and version 1's PayloadOnly schema. The current benchmark workloads support PayloadOnly and reject DeveloperLab configurations; the latter remains factory/manifest test coverage. These suites measure isolated synthetic records and do not benchmark the original WinForms screen's application scenarios.

Start with independent parameter sweeps, then compare shortlisted combinations using representative data and insertion order. Separate warm identity-cache hits, first-touch hydration, indexed misses, scans, indexed/nonindexed updates, slot recycling, flush, durable commit, rollback and reopen. Capture allocations, file size, machine/runtime and full effective configuration. Prefer workload-specific results; a small benchmark does not establish a universal best configuration. See the [profiler guide](../../../utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/README.md) for reproducible commands and measurement boundaries.

The general profiler also provides a retained [parameter-study workflow](../../../utils/Sphere10.Framework.Utils.Profiler/Studies/README.md). It measures actual Balanced, ReadHeavy and WriteHeavy persistence batches separately for Memory/File, keeps raw verified samples and exact replayable plans, and exposes recorded evidence through NUnit. Lookup locality, variable/resized payloads and category selectivity/skew are explicit dataset controls. Warm member lookups still traverse their index; a separate direct-slot workload isolates tracker hits. Follow [profiling practices](../../../docs/guidelines/patterns/object-spaces.md#profile-real-work) when screening, confirming and reporting candidates; selected defaults follow the documented Balanced objective and retained evidence; future changes still require an explicit decision and compatible existing-store opening.

## Current boundaries

Reference-aware serializers support shared/cyclic inline components. Cross-dimension external-reference garbage collection remains experimental: reference maps are not durably persisted/rehydrated, collection has physical-slot edge cases, and reopened external cycles need stronger coverage. The profiling lab and configuration-ranking suites keep GC disabled; do not present them as tests of persistent graph collection.

Custom comparer behavior also needs dedicated index tests: the current ordering registry lookup has known limitations documented in [collections and storage](../../../docs/guidelines/patterns/collections-and-storage.md#comparers--equality). Verify key equality, ordering and reopen behavior before relying on a domain-specific comparer.

Relevant NUnit coverage is in [ObjectSpaces](../../../tests/Sphere10.Framework.Tests/ObjectSpaces), [B-tree collections](../../../tests/Sphere10.Framework.Tests/Collections/BTree), and [profiling lab tests](../../../tests/Sphere10.Framework.Utils.Profiler.Tests/README.md). Read the [ObjectSpace practices](../../../docs/guidelines/patterns/object-spaces.md) when extending this subsystem.

When both type and order are supplied, the builder and index constructors pass them together as an immutable `BTreeSpec`; its type is never null and it has no resolution step. When only the order is supplied, callers use the existing order-based constructors and overloads. An absent complete specification retains the historical physical defaults: ordinary BTree for fixed-size unique indexes and BTreePlus for grouped/checksum lookup storage. No type marker is written or read from index storage. Reopen with the original configuration and serializers; omission does not discover an existing store's selection. Structural checks can reject incompatible node layouts but do not authenticate the supplied type. Changing an existing store's type requires rebuilding into new storage. A successfully loaded definition fixes the supplied specification or bare order: assigning equal configuration is allowed, while changing the layout is rejected. A matching builder retains that configuration in memory when reopening its bound storage; a fresh builder must receive the original configuration.
