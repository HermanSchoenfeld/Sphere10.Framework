# ObjectSpace profiling

The `object-spaces` domain in the [general profiler](../README.md) benchmarks
the deterministic **PayloadOnly** record schema from its local ObjectSpace lab
factory. No UI assembly is required. Read the [lab guide](Lab/README.md)
for the model and configuration ownership.

The factory retains the versioned DeveloperLab schema with Records, Groups and
Events for compatibility and independent model tests. Those three-dimension
configurations are rejected by this domain: its current measured workloads and
preserved pilot results operate on Records only. Benchmarking that schema requires
deliberate multi-dimension workloads.

Run from the Framework root in **Release**, without attaching a debugger. No
domain arguments prints help. A benchmark run requires an explicit `--filter`; it never
starts every combination implicitly.

For a ready-to-run IDE execution check, use the general profiler's
[Visual Studio launch profile](../README.md#visual-studio). The CLI examples below
skip that profile so their workload and configuration arguments remain explicit.

```powershell
# Inspect available methods and exact effective configuration.
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --storage File --list flat
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --suite btree --storage File --show-config
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --suite framework-defaults --storage Memory --show-config

# Verify all file workloads on a small dataset; Dry results are execution checks.
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --storage File --records 80 --operations 24 --payload 32 --filter '*' --job Dry

# Compare seven B-tree orders for selected workloads.
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --suite btree --storage File --records 10000 --operations 1000 --filter '*IdentityLookup*' '*InsertAndPersist*' --job Short

# Change one allocation factor, retaining the other baseline settings.
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --suite clusters --storage File --records 10000 --payload 1024 --filter '*InsertAndPersist*' '*FirstTouchScan*' --job Short

# Compare integrity digests with Merkle enabled in every profile.
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --suite hashes --storage File --records 1000 --payload 1024 --filter '*InsertAndPersist*' '*ReindexAndPersist*' --job Short

# Follow up on interactions using explicitly recorded configurations.
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --config utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/profiles.example.json --records 10000 --insertion shuffled --filter '*ReindexAndPersist*' --job Medium

# Capture CPU samples separately from the comparison run.
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --suite baseline --records 10000 --filter '*InsertAndPersist*' --job Short --trace
```

The remaining arguments pass to BenchmarkDotNet; its [command-line guide](https://benchmarkdotnet.org/articles/guides/console-args.html)
describes filters and job selection. `--profiles order-32,order-64` narrows a suite.
`--output <directory>` selects the artifact folder. `--data-root <directory>`
selects the parent of temporary, individually owned workspaces, so file workloads
can target a particular storage device. Only those generated child directories
are removed. Avoid changing machine power settings, running competing disk work,
or comparing results collected with different runtime/machine conditions.

## Configuration studies

The built-in baseline is Full indexes, order 64, outer clusters 4096 bytes,
dimension clusters 256 bytes, file pages 256 KiB, file page cache 16 MiB,
little-endian, default integrity/cache policy, explicit saving, no Merkle tree,
SHA2_256 as the configured integrity hash.
These are a controlled comparison baseline, not a preferred application
configuration. Every run records its actual settings.

| Suite | Controlled values |
|---|---|
| `baseline` | One literal historical comparison profile; default suite. |
| `framework-defaults` | One profile from current Core ObjectSpace-specific constants for the selected backend; preserves Full benchmark schema, explicit saving and no Merkle. |
| `btree` | Orders 8, 16, 32, 64, 128, 256, 512, applied to identifier, constant-size and checksum index stores. |
| `btree-refinement` | Orders 169 and 170, immediately below/above a 4096-byte serialized node for the current identifier and secondary lookup layouts. Allocation-boundary follow-up, not a preselected optimum. |
| `clusters` | Outer cluster 256, 1024, 4096, 8192 bytes; separate inner size-hint sweep 256, 1024, 4096, 8192. |
| `pages` | File pages 4, 16, 64, 256, 1024 KiB; requires File storage. |
| `cache` | File page budgets 256 KiB, 1, 4, 16, 64 MiB; requires File storage. |
| `policies` | Default; default plus FastAllocate; Performance; headers only; descriptors only; neither cache. |
| `features` | Explicit save; autosave; Merkle; Merkle with autosave. |
| `hashes` | SHA2_256, Blake2b_256, Blake2b_128, with Merkle enabled consistently and Full indexes retained. |
| `indexes` | Identifier only versus Full (identifier, unique Code checksum, nonunique Category). |
| `endianness` | Little-endian versus big-endian. |

The `framework-defaults` preset resolves the [current ObjectSpace defaults](../../../src/Sphere10.Framework/ObjectSpaces/README.md#new-store-defaults-and-existing-storage) through their central constants. It currently selects order 256; Memory root/dimension 1024/1024 and header caching plus integrity, or File 4096/256 and Default policy, with File 256KiB pages/16 MiB budget. Historical `baseline`, all factor sweeps, retained run plans and the lab's explicit comparison configuration remain unchanged. Inspect `--show-config` before interpreting results. The Memory preset deliberately accepts larger encoded storage for Balanced throughput; the File budget is not a uniquely resolved performance optimum.

```powershell
# Rerun a real mixed batch using the selected current backend defaults.
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --suite framework-defaults --storage File --records 1024 --categories 1024 --operations 200 --payload 128 --mix Balanced --insertion shuffled --filter '*MixedReadWriteAndPersist*' --job Short
```

A Short job is exploratory. Use the retained study's independent confirmation settings when making a new default decision; its original measured plans/profiles preserve their explicit values rather than consulting newly changed constants.

Use `--records`, `--payload`, `--operations` and `--insertion sequential/shuffled`
to vary workload size, record size, batch length and insertion locality. Each run
uses one value for each workload control, avoiding an unmanageable implicit
Cartesian product. The seeded dataset has distinct long IDs and string Codes,
16 evenly distributed categories, decimal balances, deterministic byte payloads
and notes. Lookups visit a deterministic permutation of distinct IDs. For other
key cardinalities/skew or domain object graphs, extend the shared model/workload
deliberately; this lab does not model every application shape.

Additional workload controls support follow-up studies without changing the
serialized model or factory defaults:

| Control | Meaning |
|---|---|
| `--lookup Shuffled/Sequential/HotSet` | Read IDs use the seeded permutation, adjacent IDs, or repeat a small hot set. Default Shuffled. Mutation targets always remain distinct. |
| `--hot-set-percent 5` | HotSet reads revisit the first 5% of record IDs. This controls locality and repeated hydration/cache hits, not insertion order. |
| `--payload-mode Fixed/Variable` | Fixed uses exactly `--payload` bytes. Variable deterministically distributes lengths between zero and that maximum; the mean therefore differs from Fixed. |
| `--categories 64` | Number of nonunique category keys; default 16. Larger cardinality changes query selectivity. |
| `--hot-category-percent 80` | Approximately 80% of records use category zero; remaining rows use other categories. Zero means uniform, and the configured percentage is exact over complete 100-record blocks. |
| `--mix Balanced/ReadHeavy/WriteHeavy` | Mixed batches select 50/50, 90/10 or 10/90 reads/writes. Half the writes change secondary indexed fields; the others change balance only. |

Mix percentages are exact for a batch length divisible by 100. Shorter batches
have the corresponding deterministic approximation, spread across the batch
rather than placing all reads first. Mixed reads respect lookup locality;
writes use distinct IDs. The fixture prehydrates both target sets before timing,
then verifies the evolving read checksum and every mutation after reopening.
Category queries count queries rather than returned rows, so compare the same
cardinality/skew when assessing throughput. Variable payloads are a separate
dataset, not an allocation improvement at the same logical byte volume.

B-tree **order** controls node fanout, rather than total tree length. Steady nodes
hold at most `order - 1` keys. The stored long-key/long-position node reserves
overflow capacity and occupies `24 * order + 17` bytes; checksum/category lookup
trees have different key layouts. Higher order can reduce tree height while
increasing node search, serialization, cluster traversal and space costs. Measure
it with record sizes and page/cluster settings representative of the target.
The dimension average-size hint is clipped to 256..8192 bytes and controls its
nested cluster size. It is separate from the outer cluster size. Nested dimension
streams currently use `FastAllocate`; the outer policy sweep does not change that.

Page size and `MaxMemory` affect **file page caching**, not in-memory ObjectSpace
storage. The strong ObjectSpace identity tracker also retains hydrated records;
`MaxMemory` is not a total-process-memory cap. The Performance policy omits runtime
integrity checks; first decide whether that policy is acceptable before including
it in a configuration choice. Autosave uses dirty tracking and saves at
Flush/Commit, rather than writing on every property setter. Merkle adds integrity
maintenance cost. The `hashes` suite uses existing Core hash registrations and
varies the Merkle integrity digest, including its size/storage cost. It does not
override the unique Code index's separately constructed default checksummer.
Experimental graph garbage collection is excluded.

## Workloads and measurement boundaries

One reported invocation is a batch. Dataset creation, payload generation,
restoring a pristine snapshot and verification are outside the timed region.
The [BenchmarkDotNet setup/cleanup contract](https://benchmarkdotnet.org/articles/features/setup-and-cleanup.html)
forces one invocation per iteration for batches that require an iteration reset.
Use enough records/operations for those batches to take useful measurable time;
heed minimum-iteration-time warnings. Warm read batches can use BenchmarkDotNet's
normal adaptive invocation counts.

| Method | Timed work |
|---|---|
| `WarmIdentityLookup` | `operations` existing ID queries, after all records were hydrated. Each query still traverses the identifier B-tree; the identity cache avoids hydration rather than bypassing the index. |
| `WarmPhysicalSlotLookup` | `operations` direct physical-slot queries, with records already hydrated. This measures actual tracker cache hits without an identifier-index traversal. Slots are seeded outside measurement and must not be retained across a different store generation. |
| `MissingIdentityLookup` | `operations` distinct absent ID queries. |
| `WarmCodeLookup` | `operations` unique string checksum-index queries; Full profiles only. |
| `WarmCategoryLookup` | `operations` nonunique queries, hydrating their matching rows; Full profiles only. Throughput counts queries, not returned rows. |
| `WarmScan` | Materialize and sum all saved live records using the warmed identity tracker. |
| `CleanFlush` | `operations` calls to `Flush` on an already settled Merkle store. Only Merkle profiles participate; setup and final root/integrity/reopen checks are excluded. The batch is idempotent and supports adaptive invocation counts. |
| `FirstTouchIdentityLookup` | Existing ID queries immediately after reopening a fresh ObjectSpace. |
| `FirstTouchScan` | Materialize and sum all saved live records immediately after reopening. |
| `InsertAndPersist` | Accept and save `records` prebuilt detached records into an empty store, then Complete. |
| `UpdateAndPersist` | Change balances on `operations` hydrated records, save as required, then Complete. |
| `ReindexAndPersist` | Change balance, unique Code and nonunique Category, save as required, then Complete. Identifier-only profiles still change these fields but do not maintain their secondary indexes. |
| `MixedReadWriteAndPersist` | `operations` deterministic reads/updates in the chosen mix; half the updates reindex Code/Category. Prehydrated targets, explicit Save or real dirty-flag autosave, then Complete. |
| `ResizePayloadAndPersist` | Alternate payload growth by half the configured maximum and shrink by half the existing length, save as required, then Complete. Replacement byte arrays are generated before timing; storage rewriting and cluster allocation are timed. |
| `DeleteAndPersist` | Delete `operations` hydrated records and Complete. |
| `DeleteRecycleAndPersist` | Delete `operations` records, accept/save prebuilt replacements with new IDs, and Complete. |
| `ReopenAndHydrate` | Construct/load a fresh store, read metadata and hydrate one indexed record. Snapshot restoration and disposal are excluded. |
| `CommitPreparedChanges` | Commit already-saved/flushed uncommitted file changes; mutation preparation is excluded. |
| `RollbackPreparedChanges` | Roll back already-saved/flushed file changes, including the framework's reload; mutation preparation is excluded. |

Complete means **Flush for memory** or **Commit for file**. These have different
durability guarantees and are ranked separately. Cold/first-touch means a fresh
ObjectSpace identity tracker and transactional file page cache. Snapshot copies
and the operating-system disk cache can remain warm; this is not a claim of
physical-disk cold I/O.

Each mutation starts from a pristine committed baseline, or an empty store for
insertion. File snapshots preserve their configuration sidecar. Reset uses the
same schema, index order, cluster/policy and byte order. Correctness checks outside
measurement verify cardinalities, balance checksums and secondary-index results;
mutation checks reopen storage to verify updated keys, missing deleted keys,
replacement rows and payload round trips. Rollback checks reacquire records from
the restored view. A failed check fails that benchmark instead of producing a
usable timing rank.

The focused [ObjectSpace hot-path tests](../../../tests/Sphere10.Framework.Utils.Profiler.Tests/ObjectSpaces/ObjectSpaceHotPathTests.cs)
reuse this fixture for ordinary correctness checks and explicit repeated trace
workloads. Filter trace stacks beneath `RunMeasuredBatch` to exclude preparation
and verification. Their runsettings expose `HotPathRepeats` and
`HotPathArtifactRoot`; they do not replace BenchmarkDotNet measurements. The
[performance report](../../../docs/performance/object-space-hotpaths.md) records
the paired lookup, clean-flush and mutation campaign.
Its exact historical profiles are available as
`profiles.hotpaths-memory.json` and `profiles.hotpaths-file.json`: `plain` and
`merkle` use explicit saves; `merkle-auto` exercises dirty-flag autosave. These
pin the measured configuration and do not select new Framework defaults.

The fixture is deliberately synchronous: ObjectSpace dimension streams retain
a monitor scope for their entire loaded lifetime. Creation, use and disposal
remain on BenchmarkDotNet's execution thread. Do not migrate a loaded session
to arbitrary `Task.Run` workers or keep its monitor scopes across `await`.

## Reports and choosing a configuration

The general profiler's [study workflow](../Studies/README.md) screens exact
configuration plans and retains every measured batch. For ObjectSpace order
screening, inspect the plan first, then run it in Release on the target drive:

```powershell
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- study --domain object-spaces --suite order --records 256 --operations 100 --data-root D:/owned-profiler-data --show-plan
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- study --domain object-spaces --suite order --records 256 --operations 100 --data-root D:/owned-profiler-data
```

This screens Memory and File separately, with all three real mixed persistence
batches. The generated plan uses Full indexes, ordinary integrity checks and
near-unique category keys; it does not simultaneously test the default 16-category
dataset. Edit/replay `run-plan.json` for selectivity, locality, payload, feature
constraints or shortlisted interactions. Keep the original evidence directory
and select a new output for replay. The retained plan supplies all effective
profile settings and workload controls; the screening report carries uncertainty
and excludes failed/incomplete cases. The profiler NUnit fixtures expose recorded
evidence for review. Confirm finalists using independent BenchmarkDotNet launches
before publishing a recommended parameter set.

Order means maximum children per node, and steady leaf capacity is at most
`order - 1` entries. Total leaf count is an outcome of the dataset and occupancy,
not the tuning property. The 169/170 refinement crosses the 4096-byte serialized
node boundary for the current key layouts; it is a measurement candidate rather
than an assumed preferred value.

The default output is a unique `BenchmarkDotNet.Artifacts/objectspaces-*` folder:

- Standard BenchmarkDotNet CSV, Markdown, HTML and full JSON reports include
  timing distributions, error bars, runtime/CPU/job metadata and allocation/GC.
- `run.json` captures exact selected profiles, dataset controls, commands, data
  device path, framework/runtime versions and interpretation caveats.
- `storage/<run-id>/*.json` records seeded logical storage bytes and full settings from
  the child process. These exclude JSON sidecars and transient transaction pages.
  The run ID prevents an explicitly reused output folder's older storage sizes
  from entering the current rankings.
- `configuration-ranks.csv` and `.md` rank **successful cases within each
  workload, job, storage backend and comparison guarantees** by measured mean. Batch item counts and
  items/queries per second accompany raw nanoseconds; storage is seeded size.
- `--trace` adds [EventPipe profiling](https://benchmarkdotnet.org/articles/configs/diagnosers.html)
  artifacts for CPU hotspot investigation, using the existing BDN package.

A Dry run checks execution only. A Short run is a pilot: repeat likely candidates
with longer runs, larger realistic datasets, realistic key/payload distributions
and the target storage device. Differences within sample uncertainty do not
establish a winner. Retain application constraints on durability, integrity,
process memory and storage size before selecting the lowest mean. There is no
universal best configuration: inserts, scans, indexed reads and rollback can
favor different orders and layouts. Use custom JSON profiles to test interactions
between promising settings, then validate the application's actual operation mix.

The `ComparisonGroup` field separates index membership, runtime integrity checks,
Merkle attachment/digest and storage backend. An IdentifierOnly store, a policy
that omits integrity checks, or a smaller Merkle digest cannot become an
unqualified winner over a different guarantee. Explicit saving and dirty-flag
autosave remain comparable here: both execute actual persistence and verify the
same final data, with a correctly dirty-tracked model. Select constraints first,
then choose separate Balanced, ReadHeavy and WriteHeavy results.

The [initial file B-tree pilot](results/pilot-btree-file.md) preserves measured
order-32/order-64 results and their limits. Its overlapping confidence intervals
establish no preferred order.
The [EventPipe diagnostic smoke](results/eventpipe-smoke.md) records successful
trace collection and source-backed areas for follow-up profiling.

The `type` suite compares `BTree` and `BTreePlus` across all member indexes. `--type BTree` or `--type BTreePlus` applies that selection to the selected built-in or JSON profiles. `--order` changes the order while preserving the selected type. An optional immutable `Configuration.IndexBTreeSpec` carries both concrete values, for example `"IndexBTreeSpec": { "Type": "BTreePlus", "Order": 64 }`. When no type is selected, `IndexBTreeOrder` alone preserves the historical mixed layout. For example:

```powershell
dotnet run -c Release --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --suite type --storage Memory --filter '*ReindexAndPersist*' --job Short
```

Use fresh workspaces when switching tree types. File lab sidecars version 4 record a complete `IndexBTreeSpec` when a type is selected, or a bare `IndexBTreeOrder` for historical mixed indexes. Version 3 maps a concrete recorded implementation into a specification; versions 1 and 2 retain the original bare order and mixed layout. Reading a legacy sidecar does not rewrite it. These are application configuration sidecars; tree type and order are not persisted in Framework tree headers.

For existing profiler inputs, the JSON reader maps a concrete legacy `IndexBTreeImplementation` into a complete specification and retains a bare `IndexBTreeOrder` when no type was recorded. Repeated order fields must agree; conflicting selections are rejected. `--implementation` and suite `implementation` remain accepted aliases for retained commands. Frozen result files retain their original field names and source identities.
