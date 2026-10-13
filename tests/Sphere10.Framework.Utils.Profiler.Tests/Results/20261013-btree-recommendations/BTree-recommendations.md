# B-tree measured recommendations

The primary objective is Balanced mixed throughput on this Windows/.NET 10/SATA SSD host. The constants below are usable tested compromises; explicit backend and wide-key profiles remain available. Mixed timings are per reset batch, including the flush/commit boundary. The table records maximum leaf capacity (`Order - 1`), rather than the number of leaves, which depends on the dataset and occupancy.

## Selected constants

| Field | Order | Maximum leaf entries | Evidence |
| --- | ---: | ---: | --- |
| `Sphere10FrameworkDefaults.BTreeOrder` | 128 | 127 | [raw](../20261013-btree-bdn-managed-warm30/Balanced/results/BenchmarkRun-joined-2026-10-13-05-31-27-report-full.json) |
| `Sphere10FrameworkDefaults.BTreePlusOrder` | 2048 | 2047 | [raw](../20261013-btree-bdn-managed-warm30/Balanced/results/BenchmarkRun-joined-2026-10-13-05-31-27-report-full.json) |
| `Sphere10FrameworkDefaults.BTreeLookupOrder` | 2048 | 2047 | [raw](../20261013-btree-bdn-managed-warm30/Balanced/results/BenchmarkRun-joined-2026-10-13-05-31-27-report-full.json) |
| `Sphere10FrameworkDefaults.StreamMappedBTreeOrder` | 128 | 127 | [raw](../20261013-btree-bdn-confirmations/File/Balanced/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| `Sphere10FrameworkDefaults.StreamMappedBTreePlusOrder` | 2048 | 2047 | [raw](../20261013-btree-bdn-confirmations/File/Balanced/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| `Sphere10FrameworkDefaults.StreamMappedBTreeLookupOrder` | 2048 | 2047 | [raw](../20261013-btree-bdn-numeric-lookup-confirmations/Balanced/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeLookupMutationBenchmarks-report-full.json) |
| `Sphere10FrameworkDefaults.StreamMappedWideKeyBTreePlusOrder` | 1024 | 1023 | [raw](../20261013-btree-bdn-wide-confirmations/Balanced/results/BenchmarkRun-joined-2026-10-13-05-38-31-report-full.json) |
| `Sphere10FrameworkDefaults.StreamMappedWideKeyBTreeLookupOrder` | 1024 | 1023 | [raw](../20261013-btree-bdn-wide-confirmations/Balanced/results/BenchmarkRun-joined-2026-10-13-05-38-31-report-full.json) |

The generic stream B+/lookup choices describe numeric keys. The two explicit wide-key choices describe 32-byte keys; constructors do not infer order from serializer width or backend. The ordinary 128 choice remains within a 64/128 plateau rather than a resolved minimum. Managed B+/lookup 1024/2048 Balanced intervals overlap too. These defaults select a practical common setting; they do not establish a universal optimum.

## Why the families differ

[BTreePlus.Remove](../../../../src/Sphere10.Framework/Collections/BTree/BTreePlus.cs) rebuilds internal separators after a successful deletion. [BTreeLookup.Remove](../../../../src/Sphere10.Framework/Collections/BTree/BTreeLookup.cs) removes and re-adds subsequent group ordinals. Those implementation choices make node count significant for the tested update/recycle mixes. Higher fanout can reduce that maintenance while increasing each serialized node's size; the ordinary B-tree has different storage and mutation behavior.

For the tested Int64 key/value B+, order 2048 produces a 49,177-byte node record and order 4096 produces 98,329 bytes. Large serialized-node buffers correlate with the measured allocation/Gen2 regressions at 4096. FileStream Balanced independent confirmation is 49.699 ±5.134 ms at 2048 versus 64.941 ±5.455 ms at 4096, with 50.978 versus 71.835 MiB diagnostic allocation and 0 versus 15 Gen2 collections. The ReadHeavy pair overlaps; its difference remains unresolved.

Digest32 widens B+ records to 98,329 bytes at 2048, and lookup records to 106,521 bytes because their stored keys include a group ordinal. At 1024 those records are 49,177 and 53,273 bytes. The explicit wide-key confirmation resolves all four FilePaged/Transactional B+/lookup pairs in favor of 1024. These are observed tradeoffs for the retained workloads, rather than a rule to infer orders automatically from key size.

## Independent-process confirmation

81 profiles, 162 workers and 1,620 actual batches were retained. Each profile has two launches and ten actual batches per launch, with invocation/unroll 1, Monitoring, DontRemove and no forced GC. Numeric and wide cohorts have five warmups; the separate Managed Balanced refinement has thirty. Launcher identities, actual generated worker hashes/MVID/versions, full console output, native exit status and original build source are retained per phase. Ignored compiled DLL archives are locally available; clone-side verification uses manifests and hashes archives when present.

### 20261013-btree-bdn-confirmations/Managed/Balanced

6 profiles; wall time 41.1 s. [exact inputs](../20261013-btree-bdn-confirmations/Managed/Balanced/run.json), [worker identities](../20261013-btree-bdn-confirmations/Managed/Balanced/worker-fingerprints.json).

| Backend / family | Key | Order | Mean ±99.9% half-width ms | Diagnostic MiB/batch | Gen2 diagnostic | Leaves / occupancy |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| ManagedNodes / BTree | Int64 | 64 | 4.6349 ±0.8115 | 0.0133 | 0 | not exposed |
| ManagedNodes / BTree | Int64 | 128 | 4.9899 ±2.9150 | 0.0122 | 0 | not exposed |
| ManagedNodes / BTreePlus | Int64 | 64 | 30.1321 ±3.3141 | 0.0045 | 0 | not exposed |
| ManagedNodes / BTreePlus | Int64 | 2048 | 16.1017 ±5.8055 | 0.0000 | 0 | not exposed |
| ManagedNodes / Lookup | Int32 | 64 | 295.8654 ±19.6124 | 0.0695 | 0 | not exposed |
| ManagedNodes / Lookup | Int32 | 2048 | 59.7814 ±0.3310 | 0.0314 | 0 | not exposed |
[full samples and chronology](../20261013-btree-bdn-confirmations/Managed/Balanced/results/BenchmarkRun-joined-2026-10-13-05-04-17-report-full.json); geometry and exact profile references are indexed in the companion JSON.

### 20261013-btree-bdn-confirmations/Managed/ReadHeavy

6 profiles; wall time 29.1 s. [exact inputs](../20261013-btree-bdn-confirmations/Managed/ReadHeavy/run.json), [worker identities](../20261013-btree-bdn-confirmations/Managed/ReadHeavy/worker-fingerprints.json).

| Backend / family | Key | Order | Mean ±99.9% half-width ms | Diagnostic MiB/batch | Gen2 diagnostic | Leaves / occupancy |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| ManagedNodes / BTree | Int64 | 64 | 3.4211 ±0.6734 | 0.0020 | 0 | not exposed |
| ManagedNodes / BTree | Int64 | 128 | 2.7447 ±0.0559 | 0.0020 | 0 | not exposed |
| ManagedNodes / BTreePlus | Int64 | 64 | 8.2076 ±0.8046 | 0.0045 | 0 | not exposed |
| ManagedNodes / BTreePlus | Int64 | 2048 | 6.2769 ±2.6986 | 0.0000 | 0 | not exposed |
| ManagedNodes / Lookup | Int32 | 64 | 59.2007 ±3.0824 | 0.0325 | 0 | not exposed |
| ManagedNodes / Lookup | Int32 | 2048 | 17.5380 ±0.1915 | 0.0314 | 0 | not exposed |
[full samples and chronology](../20261013-btree-bdn-confirmations/Managed/ReadHeavy/results/BenchmarkRun-joined-2026-10-13-05-05-05-report-full.json); geometry and exact profile references are indexed in the companion JSON.

### 20261013-btree-bdn-confirmations/Managed/WriteHeavy

6 profiles; wall time 48.5 s. [exact inputs](../20261013-btree-bdn-confirmations/Managed/WriteHeavy/run.json), [worker identities](../20261013-btree-bdn-confirmations/Managed/WriteHeavy/worker-fingerprints.json).

| Backend / family | Key | Order | Mean ±99.9% half-width ms | Diagnostic MiB/batch | Gen2 diagnostic | Leaves / occupancy |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| ManagedNodes / BTree | Int64 | 64 | 5.4157 ±0.7358 | 0.0228 | 0 | not exposed |
| ManagedNodes / BTree | Int64 | 128 | 5.3712 ±0.7025 | 0.0161 | 0 | not exposed |
| ManagedNodes / BTreePlus | Int64 | 64 | 49.7430 ±4.7253 | 0.0045 | 0 | not exposed |
| ManagedNodes / BTreePlus | Int64 | 2048 | 8.7386 ±0.0796 | 0.0000 | 0 | not exposed |
| ManagedNodes / Lookup | Int32 | 64 | 553.2710 ±28.3771 | 0.1256 | 0 | not exposed |
| ManagedNodes / Lookup | Int32 | 2048 | 107.9946 ±0.7989 | 0.0628 | 0 | not exposed |
[full samples and chronology](../20261013-btree-bdn-confirmations/Managed/WriteHeavy/results/BenchmarkRun-joined-2026-10-13-05-07-44-report-full.json); geometry and exact profile references are indexed in the companion JSON.

### 20261013-btree-bdn-confirmations/StreamMemory/Balanced

4 profiles; wall time 44.7 s. [exact inputs](../20261013-btree-bdn-confirmations/StreamMemory/Balanced/run.json), [worker identities](../20261013-btree-bdn-confirmations/StreamMemory/Balanced/worker-fingerprints.json).

| Backend / family | Key | Order | Mean ±99.9% half-width ms | Diagnostic MiB/batch | Gen2 diagnostic | Leaves / occupancy |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| StreamMemory / BTree | Int64 | 64 | 35.0227 ±1.7625 | 197.1476 | 0 | 178 / 0.715 |
| StreamMemory / BTree | Int64 | 128 | 34.4877 ±0.6771 | 202.9463 | 0 | 92 / 0.693 |
| StreamMemory / BTreePlus | Int64 | 64 | 220.4486 ±1.4205 | 1239.5003 | 0 | 189 / 0.688 |
| StreamMemory / BTreePlus | Int64 | 2048 | 58.6540 ±2.4442 | 407.1245 | 0 | 6 / 0.667 |
[full samples and chronology](../20261013-btree-bdn-confirmations/StreamMemory/Balanced/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json); geometry and exact profile references are indexed in the companion JSON.

### 20261013-btree-bdn-confirmations/StreamMemory/ReadHeavy

4 profiles; wall time 38.5 s. [exact inputs](../20261013-btree-bdn-confirmations/StreamMemory/ReadHeavy/run.json), [worker identities](../20261013-btree-bdn-confirmations/StreamMemory/ReadHeavy/worker-fingerprints.json).

| Backend / family | Key | Order | Mean ±99.9% half-width ms | Diagnostic MiB/batch | Gen2 diagnostic | Leaves / occupancy |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| StreamMemory / BTree | Int64 | 64 | 27.6994 ±1.1247 | 158.1226 | 0 | 178 / 0.715 |
| StreamMemory / BTree | Int64 | 128 | 27.2555 ±0.6366 | 159.0099 | 0 | 92 / 0.693 |
| StreamMemory / BTreePlus | Int64 | 64 | 60.4959 ±1.9701 | 333.4053 | 0 | 189 / 0.688 |
| StreamMemory / BTreePlus | Int64 | 2048 | 30.0360 ±1.3349 | 188.6210 | 0 | 6 / 0.667 |
[full samples and chronology](../20261013-btree-bdn-confirmations/StreamMemory/ReadHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json); geometry and exact profile references are indexed in the companion JSON.

### 20261013-btree-bdn-confirmations/StreamMemory/WriteHeavy

4 profiles; wall time 51.2 s. [exact inputs](../20261013-btree-bdn-confirmations/StreamMemory/WriteHeavy/run.json), [worker identities](../20261013-btree-bdn-confirmations/StreamMemory/WriteHeavy/worker-fingerprints.json).

| Backend / family | Key | Order | Mean ±99.9% half-width ms | Diagnostic MiB/batch | Gen2 diagnostic | Leaves / occupancy |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| StreamMemory / BTree | Int64 | 64 | 40.7881 ±1.9122 | 235.9772 | 0 | 178 / 0.715 |
| StreamMemory / BTree | Int64 | 128 | 40.8145 ±0.4770 | 246.9155 | 0 | 92 / 0.693 |
| StreamMemory / BTreePlus | Int64 | 64 | 389.5491 ±19.6145 | 2144.9677 | 0 | 189 / 0.688 |
| StreamMemory / BTreePlus | Int64 | 2048 | 82.7811 ±1.6644 | 625.5772 | 0 | 6 / 0.667 |
[full samples and chronology](../20261013-btree-bdn-confirmations/StreamMemory/WriteHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json); geometry and exact profile references are indexed in the companion JSON.

### 20261013-btree-bdn-confirmations/File/Balanced

11 profiles; wall time 329.3 s. [exact inputs](../20261013-btree-bdn-confirmations/File/Balanced/run.json), [worker identities](../20261013-btree-bdn-confirmations/File/Balanced/worker-fingerprints.json).

| Backend / family | Key | Order | Mean ±99.9% half-width ms | Diagnostic MiB/batch | Gen2 diagnostic | Leaves / occupancy |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| FilePaged / BTreePlus | Int64 | 64 | 471.2526 ±8.0081 | 1942.1387 | 0 | 189 / 0.688 |
| FilePaged / BTreePlus | Int64 | 2048 | 74.7327 ±6.4268 | 347.1488 | 0 | 6 / 0.667 |
| FileStream / BTree | Int64 | 64 | 24.3800 ±5.8702 | 24.5841 | 0 | 178 / 0.715 |
| FileStream / BTree | Int64 | 128 | 19.9392 ±6.5171 | 25.2708 | 0 | 92 / 0.693 |
| FileStream / BTreePlus | Int64 | 4096 | 64.9411 ±5.4549 | 71.8345 | 15 | 3 / 0.667 |
| FileStream / BTreePlus | Int64 | 64 | 526.2067 ±10.9979 | 154.0367 | 0 | 189 / 0.688 |
| FileStream / BTreePlus | Int64 | 2048 | 49.6992 ±5.1342 | 50.9778 | 0 | 6 / 0.667 |
| OSMemoryMapped / BTreePlus | Int64 | 64 | 39.1599 ±5.3203 | 154.0367 | 0 | 189 / 0.688 |
| OSMemoryMapped / BTreePlus | Int64 | 2048 | 11.9408 ±5.1552 | 50.9778 | 0 | 6 / 0.667 |
| TransactionalFile / BTreePlus | Int64 | 64 | 470.8446 ±7.2107 | 1942.1887 | 0 | 189 / 0.688 |
| TransactionalFile / BTreePlus | Int64 | 2048 | 75.1871 ±6.6262 | 347.1990 | 0 | 6 / 0.667 |
[full samples and chronology](../20261013-btree-bdn-confirmations/File/Balanced/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json); geometry and exact profile references are indexed in the companion JSON.

### 20261013-btree-bdn-confirmations/File/ReadHeavy

11 profiles; wall time 287.3 s. [exact inputs](../20261013-btree-bdn-confirmations/File/ReadHeavy/run.json), [worker identities](../20261013-btree-bdn-confirmations/File/ReadHeavy/worker-fingerprints.json).

| Backend / family | Key | Order | Mean ±99.9% half-width ms | Diagnostic MiB/batch | Gen2 diagnostic | Leaves / occupancy |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| FilePaged / BTreePlus | Int64 | 64 | 130.7937 ±12.4233 | 498.3088 | 0 | 189 / 0.688 |
| FilePaged / BTreePlus | Int64 | 2048 | 44.1633 ±6.4776 | 189.7335 | 0 | 6 / 0.667 |
| FileStream / BTree | Int64 | 64 | 16.3989 ±5.5819 | 19.7168 | 0 | 178 / 0.715 |
| FileStream / BTree | Int64 | 128 | 12.7673 ±5.5125 | 19.7986 | 0 | 92 / 0.693 |
| FileStream / BTreePlus | Int64 | 4096 | 28.1410 ±5.3705 | 28.3865 | 3 | 3 / 0.667 |
| FileStream / BTreePlus | Int64 | 64 | 114.1732 ±7.0476 | 41.4637 | 0 | 189 / 0.688 |
| FileStream / BTreePlus | Int64 | 2048 | 22.9244 ±5.3587 | 23.6767 | 0 | 6 / 0.667 |
| OSMemoryMapped / BTreePlus | Int64 | 64 | 13.3292 ±5.2062 | 41.4637 | 0 | 189 / 0.688 |
| OSMemoryMapped / BTreePlus | Int64 | 2048 | 7.6457 ±5.1061 | 23.6767 | 0 | 6 / 0.667 |
| TransactionalFile / BTreePlus | Int64 | 64 | 124.3645 ±6.5031 | 498.3588 | 0 | 189 / 0.688 |
| TransactionalFile / BTreePlus | Int64 | 2048 | 47.3451 ±6.9744 | 189.7837 | 0 | 6 / 0.667 |
[full samples and chronology](../20261013-btree-bdn-confirmations/File/ReadHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json); geometry and exact profile references are indexed in the companion JSON.

### 20261013-btree-bdn-confirmations/File/WriteHeavy

11 profiles; wall time 367.5 s. [exact inputs](../20261013-btree-bdn-confirmations/File/WriteHeavy/run.json), [worker identities](../20261013-btree-bdn-confirmations/File/WriteHeavy/worker-fingerprints.json).

| Backend / family | Key | Order | Mean ±99.9% half-width ms | Diagnostic MiB/batch | Gen2 diagnostic | Leaves / occupancy |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| FilePaged / BTreePlus | Int64 | 64 | 815.4159 ±9.1908 | 3385.1863 | 0 | 189 / 0.688 |
| FilePaged / BTreePlus | Int64 | 2048 | 99.4571 ±7.2012 | 505.1784 | 0 | 6 / 0.667 |
| FileStream / BTree | Int64 | 64 | 32.5295 ±5.4785 | 29.4541 | 0 | 178 / 0.715 |
| FileStream / BTree | Int64 | 128 | 27.1045 ±6.2570 | 30.7330 | 0 | 92 / 0.693 |
| FileStream / BTreePlus | Int64 | 4096 | 101.2985 ±6.4355 | 115.2966 | 28 | 3 / 0.667 |
| FileStream / BTreePlus | Int64 | 64 | 930.7200 ±19.8805 | 266.5244 | 0 | 189 / 0.688 |
| FileStream / BTreePlus | Int64 | 2048 | 76.4358 ±5.3223 | 78.2928 | 0 | 6 / 0.667 |
| OSMemoryMapped / BTreePlus | Int64 | 64 | 64.2208 ±4.2656 | 266.5244 | 0 | 189 / 0.688 |
| OSMemoryMapped / BTreePlus | Int64 | 2048 | 16.0731 ±4.3777 | 78.2928 | 0 | 6 / 0.667 |
| TransactionalFile / BTreePlus | Int64 | 64 | 812.7248 ±15.8716 | 3385.2363 | 0 | 189 / 0.688 |
| TransactionalFile / BTreePlus | Int64 | 2048 | 102.8945 ±6.2597 | 505.2286 | 0 | 6 / 0.667 |
[full samples and chronology](../20261013-btree-bdn-confirmations/File/WriteHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json); geometry and exact profile references are indexed in the companion JSON.

### 20261013-btree-bdn-wide-confirmations/Balanced

8 profiles; wall time 386.9 s. [exact inputs](../20261013-btree-bdn-wide-confirmations/Balanced/run.json), [worker identities](../20261013-btree-bdn-wide-confirmations/Balanced/worker-fingerprints.json).

| Backend / family | Key | Order | Mean ±99.9% half-width ms | Diagnostic MiB/batch | Gen2 diagnostic | Leaves / occupancy |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| FilePaged / BTreePlus | Digest32 | 1024 | 9.0726 ±1.1237 | 30.4267 | 0 | 4 / 0.733 |
| FilePaged / BTreePlus | Digest32 | 2048 | 30.5057 ±12.7530 | 35.3255 | 4 | 2 / 0.733 |
| FilePaged / Lookup | Digest32 | 1024 | 77.8238 ±1.4603 | 382.9178 | 0 | 4 / 0.733 |
| FilePaged / Lookup | Digest32 | 2048 | 234.9326 ±35.8735 | 440.0805 | 40 | 2 / 0.733 |
| TransactionalFile / BTreePlus | Digest32 | 1024 | 11.4305 ±1.2892 | 30.4541 | 0 | 4 / 0.733 |
| TransactionalFile / BTreePlus | Digest32 | 2048 | 25.3033 ±9.4916 | 35.3741 | 4 | 2 / 0.733 |
| TransactionalFile / Lookup | Digest32 | 1024 | 82.9322 ±2.8969 | 382.9679 | 0 | 4 / 0.733 |
| TransactionalFile / Lookup | Digest32 | 2048 | 241.1394 ±26.2311 | 440.1272 | 40 | 2 / 0.733 |
[full samples and chronology](../20261013-btree-bdn-wide-confirmations/Balanced/results/BenchmarkRun-joined-2026-10-13-05-38-31-report-full.json); geometry and exact profile references are indexed in the companion JSON.

### 20261013-btree-bdn-managed-warm30/Balanced

6 profiles; wall time 45.5 s. [exact inputs](../20261013-btree-bdn-managed-warm30/Balanced/run.json), [worker identities](../20261013-btree-bdn-managed-warm30/Balanced/worker-fingerprints.json).

| Backend / family | Key | Order | Mean ±99.9% half-width ms | Diagnostic MiB/batch | Gen2 diagnostic | Leaves / occupancy |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| ManagedNodes / BTree | Int64 | 128 | 3.4953 ±0.2250 | 0.0122 | 0 | not exposed |
| ManagedNodes / BTree | Int64 | 64 | 3.5440 ±0.1578 | 0.0133 | 0 | not exposed |
| ManagedNodes / BTreePlus | Int64 | 1024 | 6.1014 ±0.0819 | 0.0000 | 0 | not exposed |
| ManagedNodes / BTreePlus | Int64 | 2048 | 6.0095 ±0.0779 | 0.0000 | 0 | not exposed |
| ManagedNodes / Lookup | Int32 | 1024 | 67.8986 ±6.0340 | 0.0000 | 0 | not exposed |
| ManagedNodes / Lookup | Int32 | 2048 | 63.7139 ±4.9530 | 0.0314 | 0 | not exposed |
[full samples and chronology](../20261013-btree-bdn-managed-warm30/Balanced/results/BenchmarkRun-joined-2026-10-13-05-31-27-report-full.json); geometry and exact profile references are indexed in the companion JSON.

### 20261013-btree-bdn-numeric-lookup-confirmations/Balanced

4 profiles; wall time 684.0 s. [exact inputs](../20261013-btree-bdn-numeric-lookup-confirmations/Balanced/run.json), [worker identities](../20261013-btree-bdn-numeric-lookup-confirmations/Balanced/worker-fingerprints.json).

| Backend / family | Key | Order | Mean ±99.9% half-width ms | Diagnostic MiB/batch | Gen2 diagnostic | Leaves / occupancy |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| FilePaged / Lookup | Int32 | 1024 | 988.4224 ±14.9802 | 4583.0496 | 0 | 13 / 0.616 |
| FilePaged / Lookup | Int32 | 2048 | 850.3561 ±43.0901 | 4194.7258 | 0 | 6 / 0.667 |
| TransactionalFile / Lookup | Int32 | 1024 | 1002.9650 ±21.9696 | 4583.0995 | 0 | 13 / 0.616 |
| TransactionalFile / Lookup | Int32 | 2048 | 830.6048 ±11.3405 | 4194.7757 | 0 | 6 / 0.667 |
[full samples and chronology](../20261013-btree-bdn-numeric-lookup-confirmations/Balanced/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeLookupMutationBenchmarks-report-full.json); geometry and exact profile references are indexed in the companion JSON.

## Backend, family and mix coverage

These 54 cells show the shared recommendation with its actual supporting cohort. `Confirmed` means measured by two independent worker launches; overlapping intervals can still leave the best order unresolved. `Screening` uses three retained samples. Each row states its dataset/batch and source; compare orders only within that exact cohort. The paired result states the lowest mean order and orders whose intervals overlap it, using the same phase and applied settings. Wider-order probes and different datasets remain separate; a minimum at a tested boundary does not establish an unmeasured optimum. FilePaged/Transactional ordinary rows explicitly identify any historical paging settings in the companion JSON.

| Backend / family | Mix | Shared order | Rows / actions | Mean ± interval ms | Paired lowest / overlapping orders | Scope | Source |
| --- | --- | ---: | ---: | ---: | --- | --- | --- |
| ManagedNodes / BTree | balanced | 128 | 32768 / 20000 | 3.4953 ±0.2250 | 128 / 64,128 | Confirmed 99.9% | [raw](../20261013-btree-bdn-managed-warm30/Balanced/results/BenchmarkRun-joined-2026-10-13-05-31-27-report-full.json) |
| ManagedNodes / BTree | read-heavy | 128 | 32768 / 20000 | 2.7447 ±0.0559 | 128 / 64,128 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/Managed/ReadHeavy/results/BenchmarkRun-joined-2026-10-13-05-05-05-report-full.json) |
| ManagedNodes / BTree | write-heavy | 128 | 32768 / 20000 | 5.3712 ±0.7025 | 128 / 64,128 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/Managed/WriteHeavy/results/BenchmarkRun-joined-2026-10-13-05-07-44-report-full.json) |
| ManagedNodes / BTreePlus | balanced | 2048 | 32768 / 20000 | 6.0095 ±0.0779 | 2048 / 1024,2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-managed-warm30/Balanced/results/BenchmarkRun-joined-2026-10-13-05-31-27-report-full.json) |
| ManagedNodes / BTreePlus | read-heavy | 2048 | 32768 / 20000 | 6.2769 ±2.6986 | 2048 / 64,2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/Managed/ReadHeavy/results/BenchmarkRun-joined-2026-10-13-05-05-05-report-full.json) |
| ManagedNodes / BTreePlus | write-heavy | 2048 | 32768 / 20000 | 8.7386 ±0.0796 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/Managed/WriteHeavy/results/BenchmarkRun-joined-2026-10-13-05-07-44-report-full.json) |
| ManagedNodes / Lookup | balanced | 2048 | 32768 / 20000 | 63.7139 ±4.9530 | 2048 / 1024,2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-managed-warm30/Balanced/results/BenchmarkRun-joined-2026-10-13-05-31-27-report-full.json) |
| ManagedNodes / Lookup | read-heavy | 2048 | 32768 / 20000 | 17.5380 ±0.1915 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/Managed/ReadHeavy/results/BenchmarkRun-joined-2026-10-13-05-05-05-report-full.json) |
| ManagedNodes / Lookup | write-heavy | 2048 | 32768 / 20000 | 107.9946 ±0.7989 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/Managed/WriteHeavy/results/BenchmarkRun-joined-2026-10-13-05-07-44-report-full.json) |
| StreamMemory / BTree | balanced | 128 | 8192 / 8000 | 34.4877 ±0.6771 | 128 / 64,128 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/StreamMemory/Balanced/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| StreamMemory / BTree | read-heavy | 128 | 8192 / 8000 | 27.2555 ±0.6366 | 128 / 64,128 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/StreamMemory/ReadHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| StreamMemory / BTree | write-heavy | 128 | 8192 / 8000 | 40.8145 ±0.4770 | 64 / 64,128 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/StreamMemory/WriteHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| StreamMemory / BTreePlus | balanced | 2048 | 8192 / 8000 | 58.6540 ±2.4442 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/StreamMemory/Balanced/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| StreamMemory / BTreePlus | read-heavy | 2048 | 8192 / 8000 | 30.0360 ±1.3349 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/StreamMemory/ReadHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| StreamMemory / BTreePlus | write-heavy | 2048 | 8192 / 8000 | 82.7811 ±1.6644 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/StreamMemory/WriteHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| StreamMemory / Lookup | balanced | 2048 | 8192 / 1000 | 91.3295 ±0.9248 | 2048 / 2048 | Screening 95% | [raw](../20261013-btree-upper-order-refinement/results.json) |
| StreamMemory / Lookup | read-heavy | 2048 | 8192 / 1000 | 30.7294 ±3.0214 | 2048 / 2048 | Screening 95% | [raw](../20261013-btree-upper-order-refinement/results.json) |
| StreamMemory / Lookup | write-heavy | 2048 | 8192 / 1000 | 152.0140 ±5.7283 | 2048 / 2048 | Screening 95% | [raw](../20261013-btree-upper-order-refinement/results.json) |
| FileStream / BTree | balanced | 128 | 8192 / 1000 | 19.9392 ±6.5171 | 128 / 64,128 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/File/Balanced/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| FileStream / BTree | read-heavy | 128 | 8192 / 1000 | 12.7673 ±5.5125 | 128 / 64,128 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/File/ReadHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| FileStream / BTree | write-heavy | 128 | 8192 / 1000 | 27.1045 ±6.2570 | 128 / 64,128 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/File/WriteHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| FileStream / BTreePlus | balanced | 2048 | 8192 / 1000 | 49.6992 ±5.1342 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/File/Balanced/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| FileStream / BTreePlus | read-heavy | 2048 | 8192 / 1000 | 22.9244 ±5.3587 | 2048 / 2048,4096 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/File/ReadHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| FileStream / BTreePlus | write-heavy | 2048 | 8192 / 1000 | 76.4358 ±5.3223 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/File/WriteHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| FileStream / Lookup | balanced | 2048 | 8192 / 1000 | 512.9006 ±17.3937 | 2048 / 2048 | Screening 95% | [raw](../20261013-btree-upper-order-refinement/results.json) |
| FileStream / Lookup | read-heavy | 2048 | 8192 / 1000 | 154.3768 ±9.7405 | 2048 / 2048 | Screening 95% | [raw](../20261013-btree-upper-order-refinement/results.json) |
| FileStream / Lookup | write-heavy | 2048 | 8192 / 1000 | 892.2655 ±13.2281 | 2048 / 2048 | Screening 95% | [raw](../20261013-btree-upper-order-refinement/results.json) |
| FilePaged / BTree | balanced | 128 | 8192 / 100 | 7.5697 ±0.4013 | 128 / 64,128,256 | Screening 95% | [raw](../20261013-btree-file-paged-current-core-ordinary/results.json) |
| FilePaged / BTree | read-heavy | 128 | 8192 / 100 | 6.5798 ±0.5093 | 128 / 64,128,256 | Screening 95% | [raw](../20261013-btree-file-paged-current-core-ordinary/results.json) |
| FilePaged / BTree | write-heavy | 128 | 8192 / 100 | 8.3481 ±0.6106 | 128 / 64,128,256 | Screening 95% | [raw](../20261013-btree-file-paged-current-core-ordinary/results.json) |
| FilePaged / BTreePlus | balanced | 2048 | 8192 / 1000 | 74.7327 ±6.4268 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/File/Balanced/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| FilePaged / BTreePlus | read-heavy | 2048 | 8192 / 1000 | 44.1633 ±6.4776 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/File/ReadHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| FilePaged / BTreePlus | write-heavy | 2048 | 8192 / 1000 | 99.4571 ±7.2012 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/File/WriteHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| FilePaged / Lookup | balanced | 2048 | 8192 / 1000 | 850.3561 ±43.0901 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-numeric-lookup-confirmations/Balanced/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeLookupMutationBenchmarks-report-full.json) |
| FilePaged / Lookup | read-heavy | 2048 | 8192 / 100 | 32.3577 ±0.5847 | 2048 / 2048 | Screening 95% | [raw](../20261013-btree-order-settings-interaction-8192/results.json) |
| FilePaged / Lookup | write-heavy | 2048 | 8192 / 100 | 147.0428 ±6.0757 | 2048 / 2048 | Screening 95% | [raw](../20261013-btree-order-settings-interaction-8192/results.json) |
| OSMemoryMapped / BTree | balanced | 128 | 8192 / 1000 | 6.2239 ±0.9488 | 128 / 64,128,256 | Screening 95% | [raw](../20261013-btree-file-refinement-8192/results.json) |
| OSMemoryMapped / BTree | read-heavy | 128 | 8192 / 1000 | 5.3005 ±0.1858 | 128 / 64,128,256 | Screening 95% | [raw](../20261013-btree-file-refinement-8192/results.json) |
| OSMemoryMapped / BTree | write-heavy | 128 | 8192 / 1000 | 7.1742 ±0.8970 | 128 / 64,128,256 | Screening 95% | [raw](../20261013-btree-file-refinement-8192/results.json) |
| OSMemoryMapped / BTreePlus | balanced | 2048 | 8192 / 1000 | 11.9408 ±5.1552 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/File/Balanced/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| OSMemoryMapped / BTreePlus | read-heavy | 2048 | 8192 / 1000 | 7.6457 ±5.1061 | 2048 / 64,2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/File/ReadHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| OSMemoryMapped / BTreePlus | write-heavy | 2048 | 8192 / 1000 | 16.0731 ±4.3777 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/File/WriteHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| OSMemoryMapped / Lookup | balanced | 2048 | 8192 / 1000 | 108.3980 ±15.7594 | 2048 / 2048 | Screening 95% | [raw](../20261013-btree-upper-order-refinement/results.json) |
| OSMemoryMapped / Lookup | read-heavy | 2048 | 8192 / 1000 | 37.7079 ±8.6550 | 2048 / 2048 | Screening 95% | [raw](../20261013-btree-upper-order-refinement/results.json) |
| OSMemoryMapped / Lookup | write-heavy | 2048 | 8192 / 1000 | 176.5305 ±11.3626 | 2048 / 2048 | Screening 95% | [raw](../20261013-btree-upper-order-refinement/results.json) |
| TransactionalFile / BTree | balanced | 128 | 8192 / 100 | 10.9271 ±0.6925 | 128 / 64,128 | Screening 95% | [raw](../20261013-btree-order-settings-interaction-8192/results.json) |
| TransactionalFile / BTree | read-heavy | 128 | 8192 / 100 | 10.2196 ±4.3881 | 128 / 64,128 | Screening 95% | [raw](../20261013-btree-order-settings-interaction-8192/results.json) |
| TransactionalFile / BTree | write-heavy | 128 | 8192 / 1000 | 76.5555 ±9.2619 | 64 / 64,128,256 | Screening 95% | [raw](../20261013-btree-file-refinement-8192/results.json) |
| TransactionalFile / BTreePlus | balanced | 2048 | 8192 / 1000 | 75.1871 ±6.6262 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/File/Balanced/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| TransactionalFile / BTreePlus | read-heavy | 2048 | 8192 / 1000 | 47.3451 ±6.9744 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/File/ReadHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| TransactionalFile / BTreePlus | write-heavy | 2048 | 8192 / 1000 | 102.8945 ±6.2597 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-confirmations/File/WriteHeavy/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeMutationBenchmarks-report-full.json) |
| TransactionalFile / Lookup | balanced | 2048 | 8192 / 1000 | 830.6048 ±11.3405 | 2048 / 2048 | Confirmed 99.9% | [raw](../20261013-btree-bdn-numeric-lookup-confirmations/Balanced/results/Sphere10.Framework.Utils.Profiler.BTrees.BTreeLookupMutationBenchmarks-report-full.json) |
| TransactionalFile / Lookup | read-heavy | 2048 | 8192 / 100 | 40.5226 ±6.3839 | 2048 / 2048 | Screening 95% | [raw](../20261013-btree-order-settings-interaction-8192/results.json) |
| TransactionalFile / Lookup | write-heavy | 2048 | 8192 / 100 | 167.4278 ±11.0715 | 2048 / 2048 | Screening 95% | [raw](../20261013-btree-order-settings-interaction-8192/results.json) |

## Screening campaign and sensitivity

Numeric independent confirmation covers 32 of the 54 backend/family/mix cells: all Managed families, StreamMemory ordinary/B+, FileStream ordinary/B+, and B+ on all other file backends, plus Balanced numeric lookup on FilePaged/Transactional. StreamMemory lookup, ordinary trees on the other three file backends, FileStream/OS-mapped lookup, and FilePaged/Transactional ReadHeavy/WriteHeavy lookup remain screening-only. The companion JSON retains their exact matched cases, samples, allocations, collections, settings and approximate 95% Student-t intervals. Do not pool the 8,192/100 interaction, 8,192/1,000 refinement, 3,000/100 width, 30,000/100 dataset sensitivity or changed confirmation batches.

| Retained stage | Cases | Actual samples | Workloads |
| --- | ---: | ---: | --- |
| [20261013-btrees-order-2048](../20261013-btrees-order-2048/results.json) | 378 | 2646 | Mixed |
| [20261013-btrees-parameters-2048](../20261013-btrees-parameters-2048/results.json) | 216 | 648 | Mixed |
| [20261013-btree-upper-order-refinement](../20261013-btree-upper-order-refinement/results.json) | 83 | 249 | Mixed |
| [20261013-btree-file-refinement-8192](../20261013-btree-file-refinement-8192/results.json) | 96 | 288 | Mixed |
| [20261013-btree-stream-memory-refinement-8192](../20261013-btree-stream-memory-refinement-8192/results.json) | 36 | 108 | Mixed |
| [20261013-btree-lookup-refinement-8192](../20261013-btree-lookup-refinement-8192/results.json) | 48 | 144 | Mixed |
| [20261013-btree-file-paged-current-core-ordinary](../20261013-btree-file-paged-current-core-ordinary/results.json) | 9 | 27 | Mixed |
| [20261013-btree-key-width-sensitivity-3000](../20261013-btree-key-width-sensitivity-3000/results.json) | 72 | 216 | Mixed |
| [20261013-btree-file-larger-dataset-30000](../20261013-btree-file-larger-dataset-30000/results.json) | 8 | 24 | Mixed |
| [20261013-btree-one-leaf-boundary-8192](../20261013-btree-one-leaf-boundary-8192/results.json) | 15 | 45 | Mixed |
| [20261013-btree-current-core-numeric-lookup](../20261013-btree-current-core-numeric-lookup/results.json) | 4 | 12 | Mixed |
| [20261013-btree-managed-mixed-32768](../20261013-btree-managed-mixed-32768/results.json) | 36 | 108 | Mixed |
| [20261013-btree-managed-read-100000](../20261013-btree-managed-read-100000/results.json) | 15 | 75 | Read |
| [20261013-btree-order-settings-interaction-8192](../20261013-btree-order-settings-interaction-8192/results.json) | 170 | 510 | Mixed |
| [20261013-btree-pure-diagnostics-3000](../20261013-btree-pure-diagnostics-3000/results.json) | 282 | 846 | Group, Insert, Miss, Range, Read, Recycle, Reopen, Rollback, Scan, Update |

The [pure workload diagnostic table](BTree-workload-diagnostics.md) exposes the retained read, miss, scan, range/group, insert, update, recycle, reopen and rollback measurements. It is a separate 3,000-row cohort and does not decide the primary mixed-workload defaults.

The 32-byte-key width cohort explicitly compares 64/1024/2048 for B+/lookup on all six backends. All five serialized lookup 1024-versus-2048 approximate 95% intervals are disjoint. Several B+ width intervals overlap with only three samples, so the added eight-profile paged wide-key confirmation tests those pairs independently. All four independently measured FilePaged/Transactional B+/lookup pairs favor 1024 with disjoint 99.9% intervals and lower allocation/Gen2 costs. The other backends remain screening-backed for wide keys.

The exact-capacity counterexample is retained at [8,192-row boundary samples](../20261013-btree-one-leaf-boundary-8192/results.json). Higher FileStream B+ probes and the 30,000-row file/transaction crosscheck remain separate cohorts; an 8,193-order single-leaf result cannot establish a general file optimum.

## Interpretation limits

- These are tested workload recommendations, not universal optima. Compare means only within identical backend/family/key/input/completion groups.
- Primary managed five-warmup chronology contains runtime transitions; thirty-warmup Balanced refinement is separate and retains all earlier samples.
- Managed ordinary 64/128 remains an unresolved plateau. Shared 128 is a conservative common candidate, not proof that 128 is uniquely best. Managed B+/lookup 1024/2048 Balanced refinement intervals also overlap.
- FileStream B+ 4096 was a separate contender. Independent confirmation resolves Balanced and WriteHeavy in favor of 2048; ReadHeavy intervals overlap. Shared 2048 also avoids measured large-node allocation/Gen2 regressions on the other serialized backends.
- Digest32 serialized B+/lookup benefits from explicit 1024 tuning. Only FilePaged/Transactional Balanced wide-key pairs receive independent-process confirmation; other wide backends remain screening-only.
- Order 8192 on 8192 records creates two minimum-size leaves and repeated merge/split under recycle: up to approximately 350 GiB cumulative allocation per batch, not resident or peak RAM. Order 8193's single-leaf advantage is dataset-specific.
- LogicalTreeBytes 0 for managed nodes means no backing stream, not zero resident footprint. PhysicalStorageBytes sums file lengths, not allocated filesystem clusters or working set.
- Batch allocations exclude seeding/provisioned capacity. BDN memory diagnostics represent an additional diagnostic batch; direct samples retain their own per-batch collection counts.
- Flush, mapped-view flush and transactional commit are different completion guarantees. Reopen retains the operating-system cache; mapped reopen includes truncation/reprovisioning.
- Post-mutation verification checks the live tree; separate persisted-reopen workloads/tests verify restart. Direct Update/Recycle include Persist; pure BDN Update/DeleteAndRecycle omit it.
- The stream format does not persist its original order/serializer width/endianness. Default changes must never reinterpret existing files; original explicit settings remain required.
