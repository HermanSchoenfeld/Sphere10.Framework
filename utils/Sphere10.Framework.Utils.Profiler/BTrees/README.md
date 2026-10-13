# B-tree performance profiling

The `btrees` domain profiles the framework's B-tree, B+ tree and grouped lookup
implementations independently of ObjectSpace. Run the general profiler in
Release, without a debugger. The [root profiler](../README.md) explains its
Visual Studio launch profile and output layout.

## Implementations and storage

| Storage | Implementation | Persistence and completion |
| --- | --- | --- |
| `ManagedNodes` | `InMemoryBTree`, `InMemoryBTreePlus`, `InMemoryBTreeLookup` | Managed node graph; no serialization or file persistence. |
| `StreamMemory` | Corresponding `StreamMappedBTree`, `StreamMappedBTreePlus`, `StreamMappedBTreeLookup` over `MemoryStream` | Serialized nodes in a contiguous memory buffer. |
| `FileStream` | Stream-mapped tree over a buffered `FileStream` | Stream flush; configurable file buffer. |
| `FilePaged` | Stream-mapped tree over `ExtendedMemoryStream(FileMappedBuffer)` | Framework managed page cache over a file; configurable page size and cache budget. |
| `OSMemoryMapped` | Stream-mapped tree over a real `MemoryMappedFile` view | OS view flush; logical length is independent of provisioned capacity. |
| `TransactionalFile` | Stream-mapped tree over the framework's `TransactionalStream` and `TransactionalFileMappedBuffer` | Staged managed pages; explicit commit and rollback. |

`FilePaged` and `TransactionalFile` are the framework's paged implementations.
They are not OS memory-mapped views. `OSMemoryMapped` uses the .NET OS mapping API
through a profiling adapter: capacity is provisioned before measured work, grows
only within its bounded reservation, and is truncated to logical tree length on
close. Capacity is calculated from dataset size and legal minimum node occupancy
with split/recycle headroom; it does not reserve one full node per object at high
orders. The reports include both logical bytes and provisioned capacity. Reopen
never interprets unused mapped capacity as serialized nodes.

Flush, OS view flush and transactional commit have different failure guarantees.
These workloads do not claim equivalent power-loss durability. Reopening creates
a new tree and managed page cache but leaves the operating-system file cache in
place. Results describe the tested machine, filesystem, workload, key layout and
completion operation.

## Order and leaf count

The configurable parameter is **order**, the maximum number of children of an
internal node. A steady leaf can hold at most **order minus one** entries. The
total number of leaf nodes follows from data volume, insertion order, splits and
deletions; it is diagnostic output rather than a separately configured setting.

Stream storage uses these actual fixed layouts, including one overflow slot:

| Family | Bytes per physical node |
| --- | --- |
| B-tree | `order × (key bytes + 8 value bytes + 8 child bytes) + 17` |
| B+ tree | `order × (key bytes + 8 value bytes + 8 child bytes) + 25` |
| Lookup | B+ layout with an additional four-byte group ordinal in each key. |

Profiles support `Int32`, `Int64` and `Digest32` keys with `Int64` values. The first
two cover numeric index layouts; `Digest32` uses fixed 32-byte, checksum-shaped
ordered keys and the framework's byte-array serializer/comparer. It measures
comparison and node storage, not hash calculation. Lookup stores the real
`CompoundKey<TKey,int>` layout and multiple distinct values in each group.

The order suite sweeps `8, 16, 32, 64, 128, 256, 512`. Custom profiles support
orders from 3 through 16,384. Refine around measured winners using a custom JSON
file. For example, Int64-key B+ orders 169 and 170
produce node records of 4,081 and 4,105 bytes, straddling 4 KiB. Alignment alone
does not select an optimum: the tree writes fields and whole node records, and
the file buffer/page cache, allocations, comparisons and operation mix all
affect the result.

Key width and dataset size also constrain useful orders. A 32-byte key can put a
serialized node into the large-object allocation range at an order that remains
small for numeric keys. An order equal to the dataset count can leave two leaves
at minimum occupancy; repeated delete/reinsert then repeatedly merges and splits
them. The retained studies test both effects instead of treating the largest
order or a single-leaf result as a general recommendation.

## Framework defaults and explicit wide keys

The [central constants](../../../src/Sphere10.Framework/Sphere10Defaults.cs) own new-tree settings. The primary objective is Balanced throughput with numeric keys and Int64 values:

| Family | Managed default | Stream-mapped default |
|---|---:|---:|
| Ordinary B-tree | `BTreeOrder = 128` | `StreamMappedBTreeOrder = 128` |
| B+ tree | `BTreePlusOrder = 2048` | `StreamMappedBTreePlusOrder = 2048` |
| Grouped lookup | `BTreeLookupOrder = 2048` | `StreamMappedBTreeLookupOrder = 2048` |

Default-order managed constructors and stream-mapped constructors/factories use these fields. A stream default overload requires empty storage. Reopen existing trees with the original explicit order, serializer layout and endianness; the header has no authenticated order stamp. Header/stride checks reject implausible layouts but cannot identify every coincidentally plausible incorrect explicit choice. No stored nodes are migrated when a default changes.

Serialized 32-byte keys with eight-byte values have a separate measured choice: `StreamMappedWideKeyBTreePlusOrder` and `StreamMappedWideKeyBTreeLookupOrder` are 1024. Pass the corresponding constant to the explicit-order constructor or factory. Generic constructors do not infer serializer widths. Wider values also change node size and need their own workload measurements; neither numeric nor wide-key settings are universal optima.

Use `--suite framework-defaults` to inspect/run the current primary constants, including the unchanged Core paging defaults. Use `--suite wide-keys --keys Digest32` for the explicit 32-byte-key settings on serialized B+ or Lookup backends. That preset rejects managed nodes, ordinary B-trees and other key shapes. Historical `baseline` and literal factor/order controls retain order 64, 64-KiB pages and a 16-MiB cache independently of future defaults.

```powershell
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- btrees --suite framework-defaults --storage FilePaged --show-config
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- btrees --suite wide-keys --keys Digest32 --storage TransactionalFile --family Lookup --show-config
```

See the [retained recommendations](../../../tests/Sphere10.Framework.Utils.Profiler.Tests/Results/20261013-btree-recommendations/BTree-recommendations.md) and [constant decisions](../../../tests/Sphere10.Framework.Utils.Profiler.Tests/Results/final-default-selection.json) for exact source cases, raw batches, reported intervals and backend-specific overrides. Some primary comparisons remain overlapping plateaus. Large node orders can increase allocations/GC and cause repeated merge/split work around particular dataset boundaries; total leaf count and occupancy remain diagnostic outputs.

## Workloads and correctness boundaries

The fixture builds deterministic sequential or shuffled input outside measured
operations, then validates tree invariants, every expected key/value pair and a
full scan checksum. Stream metadata is read from reachable nodes outside timing:
height, active nodes, total leaves, mean leaf occupancy and physical slots. This
does not mistake recycled slots for live leaves. Managed graph geometry is not
exposed by the production tree contract and is left absent rather than inferred.
`LeafEntries` counts entries held in leaves: B+ and Lookup store every entry there,
while ordinary B-tree entries can also reside in internal nodes.

`PhysicalStorageBytes` in retained metadata is the sum of workspace file lengths,
including provisioned mapping capacity and transaction files. It does not measure
filesystem allocated clusters, process working set or peak RAM. Use
`LogicalTreeBytes` and `ProvisionedMappedCapacity` for the corresponding layout
and mapping-capacity quantities.

| Workload | One measured invocation |
| --- | --- |
| `WarmLookup` | `OperationCount` existing-key lookups. |
| `MissingLookup` | `OperationCount` missing-key lookups. |
| `Scan` | All `RecordCount` entries. |
| `RangeLookup` | `OperationCount` inclusive ranges of up to 16 entries. |
| `GroupLookup` | `OperationCount` group lookups; each group has up to `GroupSize` values. |
| `Update` | `OperationCount` value replacements; grouped lookup removes the old pair and adds its replacement. |
| `DeleteAndRecycle` | Remove and reinsert `OperationCount` entries. |
| `MixedAndPersist` | `OperationCount` reads/updates/recycle actions, followed by the backend's explicit flush or commit. |
| `InsertAndPersist` | Insert all `RecordCount` entries into an empty tree and flush/commit. |
| `ReopenAndLookup` | Dispose/close, reopen the same file and query `OperationCount` existing entries. |
| `CommitPreparedChanges` | Commit already prepared changes; preparation is outside timing. |
| `RollbackPreparedChanges` | Roll back prepared changes and reconstruct the tree's cached header view. |

Mutation and insertion cases reset to a pristine dataset before every measured
iteration and validate afterward. Lookup mutations use distinct expected values
and verify exact pair membership. Mixed lookup reads query the expected pair,
while `GroupLookup` enumerates the group. The mixed read checksum is checked
outside timing as well as the final tree. Small fixed-array expected-value
bookkeeping is included in mutation batches; tree validation and diagnostic
traversals are excluded. Baseline verification touches the whole tree, so mixed
measurements are warm application/page-cache workloads.

Post-batch verification checks the live tree; it does not reopen every persisted
mutation sample. Separate correctness tests exercise persisted reopening across
backends, families and key shapes, and `ReopenAndLookup` measures that operation.
The direct study's `Update` and `Recycle` include flush/commit; the BenchmarkDotNet
`Update` and `DeleteAndRecycle` methods measure mutation alone. Compare equivalent
completion boundaries. OS-mapped `ReopenAndLookup` includes truncating the old
capacity reservation and reprovisioning/remapping it, so its timing includes the
profiling adapter's resizing work.

The three mixed profiles use the same ratios as ObjectSpace:

| Mix | Reads | Updates | Delete/reinsert |
| --- | ---: | ---: | ---: |
| `balanced` | 50% | 25% | 25% |
| `read-heavy` | 90% | 5% | 5% |
| `write-heavy` | 10% | 45% | 45% |

The deterministic action schedule repeats every 100 operations. Use an operation
count divisible by 100 to obtain the exact ratios. Targets are distinct within a
batch, with identical seeded permutations across profiles.

## Run and inspect

The retained [parameter studies](../Studies/README.md) distinguish current
implementation costs. [B+ removal](../../../src/Sphere10.Framework/Collections/BTree/BTreePlus.cs)
rebuilds internal separators throughout the tree after each successful deletion.
[Lookup removal](../../../src/Sphere10.Framework/Collections/BTree/BTreeLookup.cs)
also removes and reinserts later group ordinals. Larger orders reduce the number
of nodes visited by that maintenance. This explains a measured tuning lead; it
does not establish that the largest tested order is optimal for other datasets
or read-only workloads.

Serialized field decoding creates serializer contexts and temporary buffers.
The paged-stream read path additionally builds and copies byte arrays for small
reads. Cumulative managed allocation can consequently be much larger than the
tree's resident storage. The retained reports show allocations and GC collections;
they do not attribute an exact percentage of time to an individual source method.
Use a sampling trace when investigating a particular hotspot.

Run from the Framework repository root:

```powershell
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- btrees --help
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- btrees --suite storage --show-config
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- btrees --suite order --storage FileStream --family BTreePlus --keys Int64 --records 10000 --operations 1000 --mix balanced --filter '*MixedAndPersist*' --job Short
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- btrees --suite order --storage TransactionalFile --family Lookup --keys Int32 --records 10000 --operations 1000 --filter '*GroupLookup*' --job Short
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- btrees --suite baseline --storage OSMemoryMapped --records 100 --operations 25 --filter '*ReopenAndLookup*' --job Dry
```

`--suite storage` covers all six backends. `--suite family` compares all three tree
families, with dictionary and group read workloads kept distinct. `pages` and
`cache` apply only to framework paged storage; `buffers` applies only to
`FileStream`. Inapplicable sweeps throw instead of publishing meaningless ranks.
`--profiles` selects named members of a suite. `--config` replaces the suite with
explicit [custom profiles](profiles.example.json), preserving every parameter.
Choose the disk with `--data-root`; only GUID-owned child workspaces are removed.

Execution requires an explicit `--filter`; `--list flat` lists workloads. Dry
proves execution and round-tripping. It cannot identify the optimal order.
Compare repeatable timing samples with their uncertainty, memory allocations,
logical storage and capability requirements. Keep balanced, read-heavy and
write-heavy recommendations separate when their winners differ.

Every run retains `run.json` with exact settings/arguments and machine/runtime,
BenchmarkDotNet Full JSON and CSV reports, and per-profile metadata under
`storage/<RunId>/`. The profiler test project validates actual factory storage,
reopen, transaction rollback, key layouts, range/group correctness, logical
mapping length, geometry and rejection behavior.
