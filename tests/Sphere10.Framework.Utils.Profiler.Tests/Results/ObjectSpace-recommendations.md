# ObjectSpace performance evidence and recommendation limits

This report derives its values from completed profiler runs. The current shared ObjectSpace index order is **256**, following the user preference for powers-of-two constants and the fresh Balanced 128/256 comparison below. Selected new-store defaults and exact sources are recorded in the [final selection manifest](final-default-selection.json). Rendering this report reads retained evidence and does not change Core. Timings are per complete batch; compare profiles within the same phase, dataset, backend and completion guarantees. Earlier order-169 decisions and all three-mix sensitivity remain historical evidence.

## Evidence and verification

| Evidence | Cases × measured batches | Purpose |
|---|---:|---|
| [Independent BenchmarkDotNet confirmations](20261013-objectspaces-bdn-confirmations/confirmation-matrix.json) | 18 × 20 | 1,024 records, 200 operations, fixed 128-byte payload, 1,024 category keys; three mixes, Memory and File |
| [Data-distribution sensitivity](20261013-objectspaces-dataset-transfer-256/run-plan.json) | 36 × 7 | 256 records, 100 operations, variable payload ceilings 128/1,024, HotSet 10%, 16 categories and 60% requested concentration |
| [Larger-store sensitivity](20261013-objectspaces-order-size-4096/run-plan.json) | 21 × 3 | 4,096 records/category keys, 100 operations, fixed 128-byte payload; orders 64/169/512 and the same Balanced Memory compound configuration across all mixes |
| [File cache pressure](20261013-objectspaces-file-cache-pressure-4096/run-plan.json) | 13 × 3 | 4,096 records, 100 Balanced operations, 16-KiB payload, page/cache matrix and the actual existing Core-default control |

All 714 measured batches in these four stages completed verification. BenchmarkDotNet uses two independent worker processes per profile, five warmups and ten actual batches per worker: Monitoring, invocation/unroll factor 1, `DontRemove`, no forced GC, and the existing Balanced power plan. The full JSON, exact commands, configuration, host information, original console and process statuses are retained. The three ObjectSpace `execution-result.json` files explicitly identify their after-run derivation from the observed successful guarded launcher; the original launcher is archived. No raw samples were rewritten or removed.

The assembly hashes in the original BenchmarkDotNet `execution-host.json` files identify the **launcher before the generated worker build**. They do not establish the worker DLL identities. The original commands omitted `--keepFiles`; `--outliers DontRemove` retains timing samples, not generated binaries. A later audit found the mapped worker build directory unavailable and recorded that limitation separately for [Balanced](20261013-objectspaces-bdn-confirmations/Balanced/worker-binary-fingerprints.json), [ReadHeavy](20261013-objectspaces-bdn-confirmations/ReadHeavy/worker-binary-fingerprints.json) and [WriteHeavy](20261013-objectspaces-bdn-confirmations/WriteHeavy/worker-binary-fingerprints.json). Historical worker hashes, file versions and MVIDs cannot be recovered from those launcher hashes. Build revision stamping can change binary identities without a code change, but missing binaries prevent this audit from proving that equivalence.

The direct studies are screening measurements in one process with independent baseline restoration and shuffled profile order. Their intervals below are approximate 95% Student-t intervals; three-sample intervals are particularly uncertain. BenchmarkDotNet tables use its reported 99.9% intervals. Overlap is an unresolved comparison, not proof of equal performance or a universal optimum. Ordinary user applications remained running on the i9-10900K / Windows 10 host. File workspaces were on D:, a Samsung 850 EVO SATA SSD; OS disk cache was not cleared.

In Visual Studio, run `RecordedBenchmarkDotNetConfirmations` and select a result's Output/attachments for the full sample sequence and worker checks. `RecordedRecommendations` exposes an individual comparison per dataset/backend/mix/factor, while `RecordedStudyResults` reconciles every JSONL/CSV field, clock duration, checksum and derived ranking. [Test instructions](../README.md) and the [derived machine-readable values](ObjectSpace-recommendations.json) provide the remaining details.

## Configuration identities and preserved guarantees

The label `combined-candidate` is a role, **not one configuration shared across mixes**. Each row below identifies the actual alternative that was independently measured. Baseline and order-only profiles retain outer cluster 4,096, inner collection cluster 256, `Default` policy and explicit saving; order-only changes the index order from 64 to 169. Their File page/cache reference is 256 KiB/16 MiB. That 16-MiB profiler reference is not the historical Core cache default, which is `int.MaxValue` and is tested explicitly in the pressure stage.

| Combined alternative | Outer / inner cluster bytes | Policy | File page / cache | AutoSave |
|---|---:|---|---:|---|
| Memory Balanced | 1,024 / 1,024 | `CacheClusterHeaders, IntegrityChecks` | — | false |
| File Balanced | 1,024 / 1,024 | `CacheDescriptors, IntegrityChecks` | 256 KiB / 4 MiB | true |
| Memory ReadHeavy | 1,024 / 1,024 | `Performance, IntegrityChecks` | — | false |
| File ReadHeavy | 1,024 / 1,024 | `Performance, IntegrityChecks` | 64 KiB / 1 MiB | true |
| Memory WriteHeavy | 256 / 1,024 | `CacheDescriptors, IntegrityChecks` | — | true |
| File WriteHeavy | 1,024 / 1,024 | `Performance, IntegrityChecks` | 256 KiB / 16 MiB | true |

Every table retains `PayloadOnly` schema, Full identifier/code/category indexes, integrity checks enabled, LittleEndian, and no Merkle tree. Memory is a MemoryStream-backed ObjectSpace; File uses the framework transactional managed-page implementation. File completion is Commit/Flush of that backend, not a common hardware power-loss guarantee. Autosave alternatives are visible comparisons and **cannot justify enabling AutoSave as a default**. Disabling checks, removing indexes or Merkle guarantees is not an eligible default optimization.

## Independent-process results: 1,024 records

Each mix has 200 operations: Balanced 50% reads, ReadHeavy 90%, WriteHeavy 10%; mutations use distinct targets. Reads traverse the declared indexes even when entity instances are warmed. Allocated MiB is cumulative allocation per batch reported by MemoryDiagnoser, not working set or peak memory. Logical bytes exclude manifests and transient transaction pages.

| Backend / mix | Profile | Mean ms | Reported 99.9% interval ms | Allocated MiB / batch | Logical MiB |
|---|---|---:|---:|---:|---:|
| Memory / Balanced | [memory-combined-candidate](20261013-objectspaces-bdn-confirmations/Balanced/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 175.666 | 164.722–186.611 | 528.69 | 1.191 |
| Memory / Balanced | [memory-order-only](20261013-objectspaces-bdn-confirmations/Balanced/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 212.343 | 174.880–249.807 | 810.15 | 0.436 |
| Memory / Balanced | [memory-baseline-64](20261013-objectspaces-bdn-confirmations/Balanced/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 229.757 | 210.059–249.455 | 950.59 | 0.428 |
| File / Balanced | [file-order-only](20261013-objectspaces-bdn-confirmations/Balanced/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 424.260 | 399.252–449.267 | 1372.33 | 0.436 |
| File / Balanced | [file-baseline-64](20261013-objectspaces-bdn-confirmations/Balanced/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 497.226 | 479.512–514.940 | 1617.42 | 0.428 |
| File / Balanced | [file-combined-candidate](20261013-objectspaces-bdn-confirmations/Balanced/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 619.372 | 572.950–665.794 | 1864.31 | 1.191 |
| Memory / ReadHeavy | [memory-combined-candidate](20261013-objectspaces-bdn-confirmations/ReadHeavy/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 59.487 | 56.142–62.831 | 126.02 | 1.191 |
| Memory / ReadHeavy | [memory-order-only](20261013-objectspaces-bdn-confirmations/ReadHeavy/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 67.236 | 56.732–77.740 | 180.30 | 0.436 |
| Memory / ReadHeavy | [memory-baseline-64](20261013-objectspaces-bdn-confirmations/ReadHeavy/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 77.938 | 70.825–85.050 | 225.77 | 0.428 |
| File / ReadHeavy | [file-order-only](20261013-objectspaces-bdn-confirmations/ReadHeavy/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 121.854 | 114.691–129.016 | 307.09 | 0.436 |
| File / ReadHeavy | [file-combined-candidate](20261013-objectspaces-bdn-confirmations/ReadHeavy/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 142.444 | 135.995–148.893 | 354.27 | 1.191 |
| File / ReadHeavy | [file-baseline-64](20261013-objectspaces-bdn-confirmations/ReadHeavy/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 143.118 | 135.175–151.060 | 382.35 | 0.428 |
| Memory / WriteHeavy | [memory-order-only](20261013-objectspaces-bdn-confirmations/WriteHeavy/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 302.560 | 286.718–318.401 | 1403.07 | 0.436 |
| Memory / WriteHeavy | [memory-baseline-64](20261013-objectspaces-bdn-confirmations/WriteHeavy/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 371.174 | 329.808–412.540 | 1665.03 | 0.428 |
| Memory / WriteHeavy | [memory-combined-candidate](20261013-objectspaces-bdn-confirmations/WriteHeavy/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 553.290 | 514.935–591.644 | 1412.13 | 1.248 |
| File / WriteHeavy | [file-order-only](20261013-objectspaces-bdn-confirmations/WriteHeavy/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 680.380 | 626.001–734.759 | 2378.75 | 0.436 |
| File / WriteHeavy | [file-combined-candidate](20261013-objectspaces-bdn-confirmations/WriteHeavy/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 728.220 | 694.765–761.675 | 2567.44 | 1.191 |
| File / WriteHeavy | [file-baseline-64](20261013-objectspaces-bdn-confirmations/WriteHeavy/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json) | 815.678 | 780.092–851.264 | 2835.48 | 0.428 |

Memory: the Balanced and ReadHeavy compound alternatives have the lowest means but overlap order-only. The WriteHeavy compound alternative is a different autosave/descriptor-cache/outer-256 configuration and clearly regresses; that result must not be attributed to the Balanced Memory configuration. Order-only is the resolved WriteHeavy candidate for this dataset.

File: order-only has the lowest mean in all three mixes. Its Balanced and ReadHeavy intervals do not overlap either competitor; in WriteHeavy it overlaps the compound alternative. Compared with the profiler order-64 reference, order-only reduces the Balanced mean from 497.226 to 424.260 ms (14.7%) and allocation from 1,617.42 to 1,372.33 MiB per batch (15.2%).

The Balanced Memory compound configuration reduces the reference mean from 229.757 to 175.666 ms (23.5%) and allocation from 950.59 to 528.69 MiB (44.4%), while increasing logical storage from 448,573 to 1,248,415 bytes. It uses 2.73 times the logical bytes of order-only (456,799). That space cost matters when choosing a Memory default.

## Variable payload / concentrated-key sensitivity

These cases are not larger-store confirmation. They retain the per-mix configurations above and change the workload to 256 records/100 operations, variable payload, HotSet 10%, 16 category keys and requested 60% concentration. The exact distribution and case IDs are in the [plan](20261013-objectspaces-dataset-transfer-256/run-plan.json); [results](20261013-objectspaces-dataset-transfer-256/results.json), [CSV](20261013-objectspaces-dataset-transfer-256/samples.csv) and [provenance](20261013-objectspaces-dataset-transfer-256/selection.json) are retained. Allocations are owner-thread totals.

| Backend / mix / payload ceiling | Lowest mean candidate | Mean ± approximate 95% half-width ms | Overlapping alternatives |
|---|---|---:|---|
| File / Balanced / 128 bytes | `combined-candidate` | 1585.770 ± 73.290 | `order-only`, `baseline-64` |
| File / Balanced / 1,024 bytes | `order-only` | 1398.744 ± 46.718 |  |
| File / ReadHeavy / 128 bytes | `baseline-64` | 229.088 ± 20.665 | `order-only` |
| File / ReadHeavy / 1,024 bytes | `baseline-64` | 239.475 ± 15.434 | `order-only` |
| File / WriteHeavy / 128 bytes | `order-only` | 2664.523 ± 199.229 | `combined-candidate` |
| File / WriteHeavy / 1,024 bytes | `order-only` | 2898.870 ± 314.339 | `combined-candidate`, `baseline-64` |
| Memory / Balanced / 128 bytes | `combined-candidate` | 451.018 ± 26.922 |  |
| Memory / Balanced / 1,024 bytes | `combined-candidate` | 477.955 ± 17.829 |  |
| Memory / ReadHeavy / 128 bytes | `combined-candidate` | 88.781 ± 14.321 | `order-only`, `baseline-64` |
| Memory / ReadHeavy / 1,024 bytes | `combined-candidate` | 89.376 ± 15.390 | `baseline-64`, `order-only` |
| Memory / WriteHeavy / 128 bytes | `baseline-64` | 1242.109 ± 83.526 | `order-only` |
| Memory / WriteHeavy / 1,024 bytes | `order-only` | 997.116 ± 37.880 |  |

The Balanced Memory compound profile is resolved as faster here for both payload ceilings; its larger encoding and lower allocation remain a tradeoff. File ReadHeavy now has order-64 as the lowest mean, but both comparisons with order-169 remain unresolved. File Balanced with the 128-byte ceiling cannot resolve any of the three alternatives. Consequently, the near-unique-category winner does not establish a winner for every selectivity or data distribution.

## Larger-store order and Balanced Memory configuration sensitivity

This stage uses 4,096 records, 100 operations, 4,096 category keys and 128-byte fixed payloads. The added `balanced-memory-candidate` is **exactly the same Balanced configuration** (order 169, outer/inner 1,024, header cache plus integrity checks, explicit save) in all three mixes. It is separate from the per-mix compound alternatives. [Plan](20261013-objectspaces-order-size-4096/run-plan.json), [results](20261013-objectspaces-order-size-4096/results.json), [CSV](20261013-objectspaces-order-size-4096/samples.csv) and [source IDs](20261013-objectspaces-order-size-4096/selection.json) retain the full evidence.

| Backend / mix | Profile | Mean ± approximate 95% half-width ms | Allocated MiB / batch | Logical MiB |
|---|---|---:|---:|---:|
| Memory / Balanced | `order-512` | 148.454 ± 54.638 | 718.05 | 1.738 |
| Memory / Balanced | `order-169` | 152.164 ± 28.890 | 733.05 | 1.675 |
| Memory / Balanced | `balanced-memory-candidate` | 160.897 ± 20.882 | 533.70 | 4.714 |
| Memory / Balanced | `order-64` | 209.503 ± 24.525 | 1045.53 | 1.714 |
| Memory / ReadHeavy | `order-169` | 35.800 ± 11.738 | 142.58 | 1.675 |
| Memory / ReadHeavy | `balanced-memory-candidate` | 37.344 ± 1.287 | 112.04 | 4.714 |
| Memory / ReadHeavy | `order-512` | 38.984 ± 5.917 | 160.21 | 1.738 |
| Memory / ReadHeavy | `order-64` | 52.210 ± 3.828 | 243.18 | 1.714 |
| Memory / WriteHeavy | `order-169` | 229.181 ± 24.569 | 1180.15 | 1.675 |
| Memory / WriteHeavy | `balanced-memory-candidate` | 240.637 ± 43.478 | 892.97 | 4.714 |
| Memory / WriteHeavy | `order-512` | 288.454 ± 27.627 | 1618.54 | 1.738 |
| Memory / WriteHeavy | `order-64` | 396.716 ± 46.798 | 1904.50 | 1.714 |
| File / Balanced | `order-512` | 336.226 ± 17.568 | 1204.28 | 1.738 |
| File / Balanced | `order-169` | 352.495 ± 19.568 | 1234.45 | 1.675 |
| File / Balanced | `order-64` | 523.194 ± 59.132 | 1796.38 | 1.714 |
| File / ReadHeavy | `order-169` | 86.543 ± 3.845 | 242.67 | 1.675 |
| File / ReadHeavy | `order-512` | 93.230 ± 6.664 | 269.59 | 1.738 |
| File / ReadHeavy | `order-64` | 143.353 ± 37.804 | 417.14 | 1.714 |
| File / WriteHeavy | `order-169` | 537.327 ± 28.014 | 1995.67 | 1.675 |
| File / WriteHeavy | `order-512` | 758.861 ± 187.991 | 2698.79 | 1.738 |
| File / WriteHeavy | `order-64` | 1100.645 ± 955.912 | 3263.83 | 1.714 |

Balanced File orders 169 and 512 overlap and both improve on 64. The larger Balanced Memory run does not resolve a unique winner: order-512 has the lowest mean and a wide interval that overlaps all four rows. The same Balanced Memory compound profile ties order-169 on ReadHeavy and WriteHeavy; it does not show the clear write regression of the distinct autosave alternative.

At this size, the Balanced compound Memory profile allocates 533.70 MiB per batch versus 733.05 for order-only, but its mean is 160.897 versus 152.164 ms and its logical storage is 4,942,924 versus 1,756,507 bytes (2.81 times). It is an allocation/space choice, not a resolved speed improvement over order-only. Three measured samples retain substantial uncertainty, especially the order-64 File WriteHeavy row. These stores remain below the 16-MiB profiler cache budget and do not identify its optimum.

## Real File cache pressure and the historical Core default

The page/cache matrix holds order 169, outer 4,096, inner 256, Default policy, explicit saving and all guarantees above. The historical Core control is order 64, page 256 KiB and `int.MaxValue` cache bound. The bound is lazy: loaded page buffers allocate on demand; a 2-GiB limit is not a 2-GiB reservation. The actual order-169 stores are 73,618,843 bytes (70.208 MiB), exceeding the 64-MiB budget as well as 4/16 MiB. The historical control is 70.248 MiB.

[Plan](20261013-objectspaces-file-cache-pressure-4096/run-plan.json), [raw results](20261013-objectspaces-file-cache-pressure-4096/results.json), [CSV](20261013-objectspaces-file-cache-pressure-4096/samples.csv), [actual-default provenance](20261013-objectspaces-file-cache-pressure-4096/selection.json), [measured storage metadata](20261013-objectspaces-file-cache-pressure-4096/storage) and [independent reconciliation](20261013-objectspaces-file-cache-pressure-4096/independent-validation.json) are retained. Initialization and verification stay outside the timed batches. Setup was expensive; 13 cases and 39 measured batches completed successfully.

| Profile | Mean ± approximate 95% half-width ms | Allocated MiB / batch |
|---|---:|---:|
| `page-1048576-cache-2147483647` | 618.882 ± 59.662 | 1429.49 |
| `page-1048576-cache-67108864` | 653.707 ± 239.821 | 1429.50 |
| `page-262144-cache-16777216` | 663.770 ± 20.750 | 1452.77 |
| `page-262144-cache-67108864` | 694.799 ± 93.015 | 1435.69 |
| `page-262144-cache-2147483647` | 695.974 ± 189.903 | 1433.76 |
| `current-core-default` | 895.023 ± 114.198 | 1994.58 |
| `page-65536-cache-16777216` | 941.736 ± 84.116 | 1457.31 |
| `page-65536-cache-67108864` | 1017.456 ± 632.979 | 1452.60 |
| `page-65536-cache-4194304` | 1298.174 ± 18.090 | 1760.30 |
| `page-65536-cache-2147483647` | 1358.727 ± 1931.798 | 1451.84 |
| `page-1048576-cache-16777216` | 1814.567 ± 532.382 | 4326.68 |
| `page-262144-cache-4194304` | 2624.318 ± 2480.706 | 3641.50 |
| `page-1048576-cache-4194304` | 8322.707 ± 1909.108 | 21229.38 |

The lowest mean is 1-MiB pages with the existing `int.MaxValue` bound, but its interval overlaps eight candidates. In particular, 256-KiB pages/16-MiB cache at 663.770 ± 20.750 ms is unresolved against 618.882 ± 59.662 ms. A smallest three-sample mean does not establish the optimal page size or justify changing a general cache constant.

The interaction is material: 1-MiB pages with only 4-MiB cache take 8,322.707 ms and allocate 21,229.38 MiB (20.73 GiB) cumulatively per 100-operation batch; with 16 MiB they still take 1,814.567 ms. Larger pages consume the bounded cache quickly. Cumulative allocations are not resident memory; cache limits also exclude tracked objects, other allocations and OS cache. A tighter cache can preserve a memory constraint at a performance cost. The existing large lazy cache bound already participates in the fastest set, so bounded-cache comparisons cannot be described as improving on an assumed 16-MiB Core default.

## Original order-169 selection for Balanced throughput

The user explicitly selected **balanced throughput** over compact Memory storage. That objective authorizes the measured Memory compound option despite its larger encoding, while retaining override parameters and all safety/persistence semantics. It does not turn a noisy smallest three-sample mean into a resolved page/cache optimum.

The original new-store selection used index order **169**, preserving index membership, checks, save semantics and endianness. Existing stores retain their saved layout. It is confirmed as the File candidate at 1,024 records, remains competitive at 4,096, and transfers reasonably across the concentrated-key cases; order-512 is tied for the larger Balanced File workload rather than established as the universal ObjectSpace order. Order means maximum child fanout; maximum steady leaf entries are order minus one, not the total leaf count.

For **Memory Balanced throughput/allocation**, the tested header-cache/outer-1,024/inner-1,024 configuration fits that explicit objective. It beats the historical reference in the independently confirmed smaller dataset and both variable-payload Balanced cases, and remains competitive at 4,096 across all three mixes. Its roughly 2.7–2.8 times the logical encoding versus order-only is an accepted, documented tradeoff. The outer-4,096/inner-256/Default-policy order-169 alternative remains an override for compact storage; the larger run does not resolve a speed benefit for the compound configuration.

For **File**, the original ObjectSpace selection used order 169, outer 4,096/inner 256, Default policy, explicit saving, page 256 KiB and a 16-MiB page-cache budget. The 256-KiB/16-MiB pressure row has a relatively stable 663.770 ± 20.750 ms mean inside the fast overlapping set for the actual 70.2-MiB store. The page size is also retained from the independently confirmed File candidate. This is a robust measured budget choice, not a uniquely optimal or unbounded-memory throughput claim. The generic collection cache ceiling remains unchanged; the bound excludes tracked objects and other allocation, and all ObjectSpace settings remain overridable. A 1-MiB page with adequate cache is another unresolved finalist for this large-payload workload. AutoSave-enabled compound timings do not authorize a behavior default.

Useful exact references for the final decision are the [Balanced Memory independent report](20261013-objectspaces-bdn-confirmations/Balanced/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json), profile `memory-combined-candidate`; [larger-store source](20261013-objectspaces-order-size-4096/results.json), case `os-Memory-balanced-memory-candidate-Mixed-Balanced-order-size-4096`; the same independent report's `file-order-only` profile; and [pressure source](20261013-objectspaces-file-cache-pressure-4096/results.json), cases `os-File-page-262144-cache-16777216-Mixed-Balanced-cache-pressure-4096` and `os-File-current-core-default-Mixed-Balanced-cache-pressure-4096`. Each case's exact settings and raw samples are also exposed by the recorded NUnit fixtures.

Every final numeric choice should link its actual case/profile ID and setting to this retained evidence and state its speed/allocation/storage objective. Existing files must reopen with their saved creation layout, including index order; new defaults do not migrate old stores. Retained plans supply their original explicit settings. Direct-study host hashes identify their executing assemblies; the historical BenchmarkDotNet launcher/worker distinction and unavailable worker identities are described above. No result establishes an optimum for arbitrary schema, payload, key distribution, transaction durability, cold OS cache or machine.

## Historical order-169 integrated-build comparison

This historical **order-169 Balanced** comparison measured the then-selected Memory/File presets against the order-64 profiler reference under the integrated implementation. The later power-of-two choice below supersedes its order selection; its raw rows remain unchanged. It contains four profiles, 80 actual measured batches and eight successful independent workers: two launches per case, five warmups and ten actual batches per launch, Monitoring, invocation/unroll 1, `DontRemove`, no forced GC and retained worker builds. It supplements the older all-three-mix and distribution/size/cache-pressure findings; it does not replace or rerank those rows.

[Exact run/settings](20261013-objectspaces-post-default-bdn-balanced/run.json), [original commands](20261013-objectspaces-post-default-bdn-balanced/command-arguments.json), [successful domain status](20261013-objectspaces-post-default-bdn-balanced/execution-result.json), [original worker console](20261013-objectspaces-post-default-bdn-balanced/console.log), [reconciliation](20261013-objectspaces-post-default-bdn-balanced/execution-validation.json), [actual worker identities](20261013-objectspaces-post-default-bdn-balanced/worker-binary-fingerprints.json) and [storage metadata](20261013-objectspaces-post-default-bdn-balanced/storage) are retained. The final-build workers have their own captured SHA256, MVID and versions; this does not reconstruct the explicitly unavailable worker binaries from the original three-mix confirmations.

| Backend | Profile | Mean ms/batch | BDN reported 99.9% interval ms | Allocated MiB/batch | Baseline logical bytes |
|---|---|---:|---:|---:|---:|
| Memory | `memory-historical-baseline-64` | 233.067 | [209.042, 257.091] | 950.59 | 448573 |
| Memory | `memory-selected-defaults-169` | 180.966 | [163.432, 198.500] | 528.69 | 1248415 |
| File | `file-historical-baseline-64` | 498.279 | [471.582, 524.976] | 1617.42 | 448573 |
| File | `file-selected-defaults-169` | 427.762 | [408.026, 447.499] | 1372.33 | 456799 |

The selected Memory mean is 22.35% lower than its matched reference. The reported intervals do not overlap in this run.

The selected File mean is 14.15% lower than its matched reference. The reported intervals do not overlap in this run.

The historical File profiler reference uses the original benchmark's 16-MiB budget; it is distinct from the old Core `int.MaxValue` cache control retained in the larger File cache-pressure study. Allocation is cumulative managed allocation, not peak RAM or working set. Storage is the encoded pristine baseline, excluding configuration manifests and transient transaction pages. File flush/commit is the selected completion boundary; OS cache is warm and power-loss guarantees are not inferred.

The user explicitly prioritized Balanced Memory throughput despite the roughly 2.8× larger encoding found in the larger dataset. That choice and the older ReadHeavy/WriteHeavy sensitivity remain part of the recommendation; this run does not establish a universal optimum for other schemas, payloads, key locality, durability or machines. Numeric confidence intervals, allocations and logical bytes above are read from this run's reports/metadata, with the complete exact configurations and raw samples added to `ObjectSpace-recommendations.json`.

Run `RecordedDefaultSelectionTests` in Visual Studio Test Explorer to check the final constants against their recorded actual profile/setting sources. `RecordedBenchmarkDotNetConfirmations` reconciles raw samples and workers; these evidence fixtures read retained results and do not start another benchmark campaign.

## Current selection: shared power-of-two order 256

The user subsequently preferred powers-of-two tuning constants. The fresh same-phase comparison changes **only index order 128 versus 256** within each previously selected backend layout. It contains four actual profiles, 80 measured batches and eight successful independent workers; every profile has two launches, five warmups and ten actual batches per launch, Monitoring, invocation/unroll 1, `DontRemove`, no forced GC and retained worker builds. Dataset controls remain 1024 records/category keys, 200 operations, 128-byte fixed payload and shuffled insertion/lookup.

[Exact settings](20261013-objectspaces-power2-order-bdn-balanced/run.json), [profile input](20261013-objectspaces-power2-order-bdn-balanced/profiles.json), [unchanged-layout provenance](20261013-objectspaces-power2-order-bdn-balanced/selection.json), [raw Full JSON](20261013-objectspaces-power2-order-bdn-balanced/results/Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceMutationBenchmarks-report-full.json), [original console](20261013-objectspaces-power2-order-bdn-balanced/console.log), [domain/worker reconciliation](20261013-objectspaces-power2-order-bdn-balanced/execution-validation.json), [worker SHA256/MVID/version capture](20261013-objectspaces-power2-order-bdn-balanced/worker-binary-fingerprints.json) and [baseline logical storage](20261013-objectspaces-power2-order-bdn-balanced/storage) retain the basis. These actual worker identities are separate from launcher hashes and do not reconstruct unavailable original three-mix workers.

| Backend | Profile | Mean ms/batch | BDN reported 99.9% interval ms | Allocated MiB/batch | Baseline logical bytes |
|---|---|---:|---:|---:|---:|
| Memory | `memory-default-layout-order-128` | 218.997 | [198.431, 239.564] | 615.897 | 1259866 |
| Memory | `memory-default-layout-order-256` | 181.012 | [173.339, 188.684] | 556.205 | 1273399 |
| File | `file-default-layout-order-128` | 475.938 | [454.986, 496.890] | 1650.013 | 465025 |
| File | `file-default-layout-order-256` | 505.062 | [468.278, 541.847] | 1607.925 | 477364 |

Memory 256 has a 17.35% lower mean than 128, and its reported interval does not overlap the 128 interval. It also allocates less (556.205 versus 615.897 MiB/batch). File 128 has the lower mean; File 256 is 6.12% higher in this run, but the reported intervals overlap. The File timing comparison remains unresolved. File 256 allocates 2.55% less (1607.925 versus 1650.013 MiB/batch).

**Select shared order 256**: it satisfies the user preference, gives the resolved Memory timing advantage and lower Memory allocation, and has lower File allocation within an unresolved File timing pair. This retains one existing shared ObjectSpace order owner rather than inventing separate backend order defaults. It does not establish that 256 is faster than 128 for File, or a universal optimum. Orders 128/256 permit at most 127/255 steady leaf entries; total leaves remain measured geometry.

| Current owner/settings | Selected value |
|---|---|
| `ObjectSpaceIndexBTreeOrder` |256|
| Memory root/dimension/policy |1024/1024, `CacheClusterHeaders, IntegrityChecks`|
| File root/dimension/policy |4096/256, `Default`|
| ObjectSpace File page/cache budget |262144/16777216 bytes|
| Full indexes / explicit save / LittleEndian / no Merkle |Preserved|

The new phase records Memory 256 baseline encoding of 1,273,399 bytes versus 1,259,866 for 128, and File 256 encoding of 477,364 versus 465,025 bytes. The original roughly 2.8× larger Memory encoding at 4096 records remains the historical user-accepted throughput/space decision context. This 1024-record phase does not repeat that larger-store experiment or include an order-64 control, so it does not replace that ratio. Allocation is cumulative managed allocation, not peak RAM; logical bytes exclude manifest/transient transaction files.

Chronology is retained rather than filtering samples. First-five to last-five mean changes within each worker are:

| Profile | Worker 1 mean change | Worker 2 mean change |
|---|---:|---:|
| `memory-default-layout-order-128` | +4.70% | -11.72% |
| `memory-default-layout-order-256` | -5.52% | -5.82% |
| `file-default-layout-order-128` | -2.50% | -3.39% |
| `file-default-layout-order-256` | -5.04% | -10.24% |

No per-worker or combined chronological half-median ratio exceeds the recorded 1.5× drift flag. This does not prove stationarity: the visible within-worker trends and retained intervals remain part of the limit, especially File 256 and Memory 128 worker 2. Full per-launch means/medians and original chronological measurements are in the derived JSON. Earlier 169 and new 128/256 timings are **not pooled or treated as same-phase comparisons**. Earlier ReadHeavy/WriteHeavy, selectivity, larger-store and cache-pressure sensitivities remain relevant; the power-of-two preference narrows the default decision rather than establishing performance for arbitrary workloads.
