# Initial B-tree pilot — 12 October 2026

The first out-of-process ShortRun comparison **does not establish a better
B-tree order**. Order 64 has slightly lower means than 32 on this small dataset,
but their 99.9% confidence intervals overlap. Keep the current default while
using the profiler to investigate larger representative workloads and the full
order sweep. This report records observed behavior, not a new framework default.

Intel Core i9-10900K (10 physical/20 logical cores), Windows 10 22H2 build
19045.7725, .NET 10.0.12 x64, SDK 10.0.401, BenchmarkDotNet 0.15.8, workstation GC,
Release build without a debugger. Each case has three measured iterations after
three warmups, in one process launch. File storage was on the local temporary
directory's volume, with operating-system caches uncontrolled.

Configuration: Full indexes, outer clusters 4096 bytes, inner clusters 256 bytes,
file pages 256 KiB, page cache 16 MiB, default integrity/cache policy,
little-endian, explicit save, no Merkle tree, SHA2_256 as the default configured
integrity hash. Baseline contains 160 deterministic records with 32-byte payloads.
Insertion is sequential. Each warm lookup batch
queries 64 distinct existing IDs; each insertion batch accepts/saves 160 prebuilt
records into a fresh empty store and commits.

| Workload | Order | Mean/batch | 99.9% CI half-width | Allocated/batch | Seeded storage |
|---|---:|---:|---:|---:|---:|
| Warm ID lookup (64 queries) | 32 | 7.961 ms | 0.804 ms | 14.05 MiB | 82,516 B |
| Warm ID lookup (64 queries) | 64 | 7.911 ms | 0.659 ms | 13.59 MiB | 86,629 B |
| Insert and commit (160 records) | 32 | 257.723 ms | 154.088 ms | 903.15 MiB | 82,516 B |
| Insert and commit (160 records) | 64 | 252.003 ms | 90.442 ms | 941.79 MiB | 86,629 B |

The high allocation volume merits investigation with `--trace` and the cluster,
policy and page suites before focusing only on tree order. Allocation measures
total managed allocations during a batch, not peak retained memory. Small record
counts and tiny payloads do not establish production behavior. This pilot does
not evaluate orders 8, 16, 128 or 256, payload distributions, large tree heights,
alternative storage devices, or any interaction between parameters.

Exact settings, sample values, confidence intervals and allocation statistics
are preserved in [the JSON evidence](pilot-btree-file.json). The automatically
generated [ranking CSV](pilot-btree-file.csv) retains raw nanoseconds, errors,
batch counts, query/record throughput and seeded storage sizes.

```powershell
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --suite btree --profiles order-32,order-64 --storage File --records 160 --operations 64 --payload 32 --filter '*WarmIdentityLookup*' '*InsertAndPersist*' --job Short
```

Before collecting these timings, execution and round-trip validation passed for
all 15 file workloads and all 13 memory workloads using Dry jobs (80 records,
24 operations, 32-byte payloads). Dry measurements validate the runner and storage
behavior only; they do not contribute to the comparison above.
Additional Dry checks passed for reindexing, delete/recycle and rollback under
all four explicit/autosave/Merkle feature profiles, and for insert, ID lookup and
reindexing under orders 8, 16, 128 and 256. These check execution coverage across
the available order sweep without claiming timing comparisons.
The subsequent Merkle hash-suite Dry checks also passed insertion and reindexing
with SHA2_256, Blake2b_256 and Blake2b_128. These validate the selectable digest
configurations and persisted round trips; they establish no hash-performance winner.
