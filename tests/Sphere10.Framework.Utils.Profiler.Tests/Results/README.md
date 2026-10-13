# Retained performance evidence

The profiler retains measured configurations, original samples, host/storage context and process/binary identities here. The final defaults follow the explicitly selected **Balanced throughput** objective; workload and hardware sensitivity remain visible rather than claiming one universal optimum.

| Area | Findings and decisions | Raw evidence |
|---|---|---|
| ObjectSpace Memory and File | [Recommendations and limits](ObjectSpace-recommendations.md), [machine-readable values](ObjectSpace-recommendations.json), [final constant decisions and exact sources](final-default-selection.json) | [Original independent three-mix confirmations](20261013-objectspaces-bdn-confirmations), [distribution sensitivity](20261013-objectspaces-dataset-transfer-256), [larger stores](20261013-objectspaces-order-size-4096), [File cache pressure](20261013-objectspaces-file-cache-pressure-4096), [integrated order169 comparison](20261013-objectspaces-post-default-bdn-balanced), [power-of-two order128/256 comparison](20261013-objectspaces-power2-order-bdn-balanced) |
| B-tree implementations | [Recommendations and limits](20261013-btree-recommendations/BTree-recommendations.md), [machine-readable values](20261013-btree-recommendations/BTree-recommendations.json) | [Three-mix numeric confirmations](20261013-btree-bdn-confirmations), [longer-warmup managed comparison](20261013-btree-bdn-managed-warm30), [32-byte-key confirmation](20261013-btree-bdn-wide-confirmations), [numeric lookup confirmation](20261013-btree-bdn-numeric-lookup-confirmations), [pure workloads](20261013-btree-pure-diagnostics-3000), [key-width sensitivity](20261013-btree-key-width-sensitivity-3000) |

ObjectSpace now uses shared order 256 after the user requested power-of-two constants. The matched Memory comparison favors 256 over 128 with disjoint intervals; File 128 has a lower point mean but overlaps 256, which allocates slightly less. Its selected Memory cluster/policy configuration prioritizes Balanced throughput/allocation despite its roughly 2.8× larger encoding in the larger test. File's 256-KiB page/16-MiB cache is a stable measured budget in the fast interval, not a uniquely resolved optimum. The recommendations retain Balanced/ReadHeavy/WriteHeavy, distribution/size sensitivity and the File budget tradeoffs. Read the [ObjectSpace opening/default contract](../../../src/Sphere10.Framework/ObjectSpaces/README.md#new-store-defaults-and-existing-storage) for exact constant owners and compatible existing-store opening.

## Inspect in Visual Studio

The B-tree campaign retains 81 confirmed profiles, 162 independent workers and 1,620 actual batches. New numeric defaults are order 128 for ordinary trees and 2048 for B+/grouped lookup; explicit serialized 32-byte-key settings use 1024. Managed pairs remain overlapping fast plateaus; some backend/mix recommendations have screening evidence only. The report distinguishes those from independent confirmations and retains the large-node allocation and dataset-boundary regressions. [Path portability](20261013-btree-recommendations/path-portability-audit.json) and per-phase filename audits preserve metadata contents while avoiding long Windows checkout paths.

Build the solution, open **Test > Test Explorer**, and run the evidence fixtures in `Sphere10.Framework.Utils.Profiler.Tests`. Select a result and open **Output** and its attachments:

- `RecordedDefaultSelectionTests` checks each final numeric constant against its recorded exact profile or study case, setting pointer and rationale in `final-default-selection.json`. It does not select a new default from a smallest mean.
- `RecordedBenchmarkDotNetConfirmations` reconciles actual per-launch durations, reported confidence intervals, exact profiles and process controls. Inspect the separate integrated169 and power-of-two 128/256 ObjectSpace results, each with four profiles, 80 actual batches and eight successful workers.
- `RecordedRecommendations` exposes individual dataset/backend/family/mix/factor comparisons and their unresolved candidates.
- `RecordedStudyResults` verifies every retained raw journal/CSV/JSON round, elapsed clock ticks, checksum and derived statistic.
- `RecordedWorkerIdentityTests` checks generated worker identities and their original execution/build mappings; archived DLLs are optional local audit material.

These fixtures read existing evidence. They do not run new benchmarks or require this machine to reproduce the retained speed. Results are copied into the test output on build, so rebuild after adding evidence or updating the final manifest. The [profiler test guide](../README.md) explains the fixtures and focused CLI commands.

Set the general profiler as the Visual Studio startup project and use its [default report launch](../../../utils/Sphere10.Framework.Utils.Profiler/README.md#visual-studio) to view recorded decisions first, followed by direct screening and independent BDN measurements. [Study commands and controls](../../../utils/Sphere10.Framework.Utils.Profiler/Studies/README.md) explain replaying plans; genuine measurements require Release without a debugger and one serial execution slot.

## Interpretation and traceability

Direct studies screen candidates in one process and report approximate 95% intervals. Independent BenchmarkDotNet finalists retain two worker launches and twenty actual samples per case, with BDN's reported 99.9% intervals. Overlapping intervals remain unresolved; checks/indexes/save and backend completion guarantees define separate comparison groups. Order sets maximum child fanout and maximum steady leaf entries (`order - 1`); total leaf count is observed geometry.

Original three-mix ObjectSpace confirmations retain an explicit limitation: their launcher hashes cannot establish the now-unavailable original worker identities. The separate integrated-build comparison captures actual generated Core/Profiler/app SHA256, versions and MVIDs before directory reuse. Its ignored local DLL archives are useful locally but are not required or force-added on a fresh clone. Raw console/run/status/worker JSON remains reviewable, and new worker identities do not reconstruct historical ones.

Retain original failed cases with their attributed errors and exclude them from recommendations. The original BigEndian failures remain preserved alongside the separate verified fixed-endian run. New defaults preserve explicit parameters and existing storage layouts; measurements do not authorize silently migrating persisted nodes or transaction pages.

The repository's `.gitattributes` preserves the evidence directory's original bytes and exporter whitespace across Windows and Linux checkouts. Console logs are explicitly tracked as evidence; local worker DLLs remain ignored. Do not normalize raw reports or rewrite captures to make formatting checks pass.
