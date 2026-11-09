# Profiler tests

`Sphere10.Framework.Utils.Profiler.Tests` is the NUnit project for the
[Framework profiler](../../utils/Sphere10.Framework.Utils.Profiler/README.md).
It checks the profiler commands, the ObjectSpace, B-tree, cache and
stream-mapped dictionary workloads and the parameter-study analyzer. It can also
verify recorded performance captures that you supply. Run it after you change a
profiler workload or a Framework type that those workloads exercise.

## Run the tests

Run from the Framework root:

```powershell
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0
```

An ordinary run executes the correctness fixtures and needs no interactive
desktop, browser or capture files. The project targets
`$(FrameworkTargetFrameworks)`, which is `net10.0`, plus `net11.0` when the
building SDK supports .NET 11. Add `-f net10.0` to run one target only.

| Group | Selection | Needs |
|---|---|---|
| Correctness and synthetic-evidence tests | Run by default | Nothing else |
| Timing, allocation, memory and trace diagnostics in the `Performance` category | `[Explicit]`, so a filter must select them | A serial session in a fresh test host. The sections below give each one's settings. |
| Recorded-capture checks in the `RecordedPerformance` category | `[Explicit]`, so a filter must select them | The [`ProfilerResultsRoot` parameter](#external-historical-captures) |

`IO/RestorePositionScopePerformanceTests` is the exception. It belongs to the
`Performance` category but runs by default, because it asserts that static-page
span writes in `StreamPagedList<T>` restore the caller's stream position without
allocating.

## Fixtures

| Fixture | Covers |
|---|---|
| `ProfilerCommandTests` | Root and domain help, rejection of an unknown domain, `--show-config` inspection, and the `--filter` requirement before any output is created |
| `BTrees/BTreeProfilerTests` | B-tree, B+ tree and multimap lookup behavior on the six storage backends (managed nodes, `MemoryStream`, `FileStream`, Framework paged files, OS memory-mapped files and transactional files), with key layouts, oracle-checked queries and mutations, recycled slots, rollback and reopen, leaf geometry, logical bytes against mapped capacity, and configuration rejection |
| `BTrees/BTreeTypeProfileTests` | `BTreeType` selection through `--type` and `--implementation`, lookup trees of each type on every backend, current and legacy profile JSON, and rejection of null or conflicting tree fields |
| `BTrees/BTreeFrameworkDefaultsProfileTests` | The `framework-defaults` and `wide-keys` suites against `Sphere10FrameworkDefaults` on every backend, and the unchanged `baseline` suite |
| `Caches/CacheProfilerTests` | Typed, object and preboxed hot-key access and capacity pressure against an independent LRU/FIFO linked-list oracle, covering minimal, quarter and half reap levels, unit and weighted sizes, fetch and victim sequences, capacity accounting, deterministic resets, simulated fetch work, retained metrics and invalid controls |
| `ObjectSpaces/ObjectSpaceWorkloadTests` | Workload controls, data distributions, read/write mixes, payload growth and shrink, prepared transactions, comparison groups and large index nodes in either byte order |
| `ObjectSpaces/FrameworkDefaultsProfileTests` | The `framework-defaults` ObjectSpace suite against `Sphere10FrameworkDefaults` on Memory and File stores, and the unchanged `baseline` suite |
| `ObjectSpaces/Lab/ObjectSpaceLabTests` | The lab store, as listed in [ObjectSpace lab store](#objectspace-lab-store) |
| `ObjectSpaces/Lab/ObjectSpaceBTreeManifestTests` | Lab manifest versions 1 to 3 mapped to `IndexBTreeSpec`, the current version 4 manifest, mismatches rejected before opening, and incomplete, conflicting or unsupported manifests |
| `ObjectSpaces/ObjectSpaceHotPathTests` | Hot-path workloads, described in [ObjectSpace hot paths and page memory](#objectspace-hot-paths-and-page-memory) |
| `StreamMappedDictionaries/StreamMappedDictionaryWorkloadTests` and `RecordedStreamMappedDictionaryResultsTests` | Dictionary integration workloads and their capture reader, described in [Stream-mapped dictionary workloads](#stream-mapped-dictionary-workloads) |
| `Studies/PerformanceEvidenceTests` | The study analyzer on synthetic samples, covering rejection of incomplete, failed and malformed samples, capability and dataset separation, uncertainty ties, sample statistics, temporal-drift warnings and supported-workload preflight |
| `Studies/StudySpecificationReadTests` | Replay of legacy study plans and results that store the tree order and type in older fields, without rewriting the files |
| `Studies/RetainedPerformanceReportTests` | `RetainedPerformanceReports` output from synthetic reports, including corrupt inputs that fail without changing any file |

These fixtures check correctness. They do not run BenchmarkDotNet, and their
execution time is not a performance measurement. Several recorded-capture
fixtures also contain synthetic tests of their own checks, which run by default
unless the whole fixture is explicit.

### ObjectSpace lab store

`ObjectSpaces/Lab/ObjectSpaceLabTests` covers the `PayloadOnly` and
`DeveloperLab` schema profiles and the configuration of the lab store:

- Deterministic round trips and unique lookups at B-tree orders 3, 16 and 64.
- Explicit save versus dirty-flag autosave across rehydration.
- File commit, rollback with identity-cache invalidation, and reopen.
- Rejection of incompatible or incompletely described file layouts before any
  data changes.
- Deletion and reuse, and cloned records that cannot change a tracked payload.
- Configuration validation before storage is created.
- Merkle rehydration with SHA2-256, Blake2b-256 and Blake2b-128.
- Independent record, group and event dimensions with their declared secondary
  indexes, with and without Merkle integrity.
- Explicit mapping of a version 1 manifest to the unchanged `PayloadOnly` record
  layout, and a version 2 manifest that must name its `SchemaProfile`.

## External historical captures

Historical captures are not part of the repository. They are not project items,
and builds do not copy them into the test output. The recorded-capture fixtures
read them from an external root that you pass as the `ProfilerResultsRoot` NUnit
parameter:

```powershell
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0 --filter 'FullyQualifiedName~Recorded' -- 'TestRunParameters.Parameter(name="ProfilerResultsRoot",value="E:/ProfilerEvidence")'
```

In Visual Studio, put the parameter in a run settings file and choose it with
**Test > Configure Run Settings > Select Solution Wide runsettings File**. Then
build the solution and run the fixtures from **Test Explorer**.

```xml
<RunSettings>
  <TestRunParameters>
    <Parameter name="ProfilerResultsRoot" value="E:\ProfilerEvidence" />
  </TestRunParameters>
</RunSettings>
```

Without the parameter, every recorded test reports as ignored. With it, each
fixture reads its own part of the root:

| Fixture | Reads under the root |
|---|---|
| `RecordedStudyResults` and `RecordedRecommendations` | Every study folder, identified by its `run-plan.json`, with the `host.json`, `samples.jsonl`, `samples.csv`, `results.json`, `recommendations.json` and `report.md` beside it |
| `RecordedBenchmarkDotNetConfirmations` | Every `*-report-full.json`, with the `run.json` of the run folder above its results folder |
| `RecordedWorkerIdentityTests` | Every `worker-fingerprints.json` or `worker-binary-fingerprints.json`, with the `run.json`, `console.log` and `results` reports beside it, plus `execution-result.json` for a captured audit |
| `RecordedDefaultSelectionTests` | `final-default-selection.json` at the root and the study or BenchmarkDotNet folders that it names |
| `RecordedCacheMetricsTests` | Every cache run folder, identified by a `run.json` whose settings include `HotAccessPercent` and `FetchWork`, with its `execution-result.json`, `console.log`, `results/*-report-full.json` and `cache-metrics/<RunId>/*.json` |
| `RetainedPerformanceReportTests` | The root itself and `20261013-objectspaces-bdn-confirmations` |
| `RecordedObjectSpaceHotPathTests` | `20261022-objectspace-hotpaths`, `20261022-objectspace-page-btree` and `20261022-span-dispatch` |
| `RecordedBTreeNodeIoTests` | `20261023-span-node-io` |
| `RecordedStreamMappedDictionaryResults` | `StreamMappedDictionaries/<campaign>/campaign.json` and the TRX files that it lists |

A capture that is present but malformed or incomplete fails its checks.
`RecordedObjectSpaceHotPathTests`, `RecordedCacheMetricsTests` and
`RecordedStreamMappedDictionaryResults` skip an absent campaign, whereas the
other fixtures fail when their inputs are missing. Select only the fixtures whose
captures the root contains. Keep new captures outside the checkout, and keep each
capture's original reports, logs and worker identities together.

## Reading recorded evidence

Run a recorded fixture, select a result and open its **Output** and attachments.
These fixtures read existing evidence. None of them starts a measurement,
hard-codes a fastest configuration or asserts a timing threshold, so this
machine's speed does not have to match the recorded timings.

- `RecordedStudyResults` gives one result per study folder. Its output lists
  each comparison group's candidate with its mean, approximate 95% half-width and
  overlapping candidates, followed by the report. The attachments hold the plan,
  host and clock information, sample journal, CSV, results and report. The
  fixture reconciles clock ticks with nanoseconds, requires one checksum across
  reset batches, verifies every CSV and journal field against `results.json`,
  and recomputes `recommendations.json` and `report.md` from the samples.
- `RecordedRecommendations` gives one result per comparison group, named after
  its study, domain, backend, workload, mix and parameter factor. Its output
  shows the lowest mean, the candidates whose intervals overlap it, every
  eligible profile's timing and allocation, its raw samples and the exact case
  settings. Its coverage test requires eligible mixed-workload results for
  Memory and File ObjectSpaces under every mix, and for each B-tree backend and
  family under the balanced, read-heavy and write-heavy mixes.
- `RecordedBenchmarkDotNetConfirmations` gives one result per BenchmarkDotNet
  Full JSON report, with each benchmark's sample count, mean, reported interval
  and raw nanoseconds per invocation. It rejects Dry runs as confirmation
  evidence. A run with two or more launches must also declare the controlled job
  (`--strategy Monitoring`, at least five warmups and ten iterations,
  `--outliers DontRemove`, one invocation, unroll factor 1 and `--noForcedGCs`)
  and agree with any retained `command-arguments.json`. Its
  `execution-result.json` must name its `Origin` and report a zero domain exit
  code, and its `console.log` must show successful worker exits. Every warmup,
  actual and result measurement must then reconcile with the reported
  statistics. The coverage test requires two-launch, ten-iteration finalists for
  Memory and File ObjectSpaces and for the six B+ tree backends across the three
  mixes. It matches each benchmark to its run manifest by the full profile name,
  so a profile that a configuration declares but never ran cannot satisfy
  coverage.
- `RecordedDefaultSelectionTests` gives one result per constant in
  `final-default-selection.json`. It checks that the compiled
  `Sphere10FrameworkDefaults` constant equals the setting used by its nominated
  study case or BenchmarkDotNet profile, which must have run successfully under
  the balanced mix. The nominated configuration must also keep the current
  behavior, with the `PayloadOnly` schema, full indexes, little-endian storage,
  integrity checks, and neither autosave nor Merkle integrity. Its output shows
  the value, rationale, exact measured configuration and raw samples.
  `FrameworkDefaultsProfileTests` and `BTreeFrameworkDefaultsProfileTests` run
  the current presets without any capture.
- `RecordedWorkerIdentityTests` reconciles each worker audit with the original
  worker commands and build output paths in `console.log`, the measured profiles
  and the launch count. A captured audit records each worker's SHA256, versions
  and MVIDs, and the fixture compares it with a local DLL archive when one is
  present. A historical audit may instead declare the original worker binaries
  unavailable. Their identities then stay null rather than being inferred from
  the launcher or a reused build folder. Every two-launch, ten-iteration run that
  passed `--keepFiles` needs exactly one audit with captured workers, so record
  worker identities before a later build reuses the folder. Checks in a fresh
  clone need no ignored binaries and do not compare Git-normalized project text
  with capture-time hashes.
- `RecordedCacheMetricsTests` gives one result per cache run. It reconciles
  every retained per-batch metrics file with the run settings, the exact
  profile, the original worker process and the estimator-size accounting.
  Counters and checksums must match across reset batches and independent
  workers. Its output prints the fetches, reaps, victims and final occupancy of
  each case. The metrics files include warmup and diagnostic batches and do not
  identify BenchmarkDotNet sample stages, so read timings and allocations from
  `RecordedBenchmarkDotNetConfirmations`. The
  [cache profiling guide](../../utils/Sphere10.Framework.Utils.Profiler/Caches/README.md)
  describes the measured batch and refill boundaries and the earlier cache
  captures.
- `RetainedPerformanceReportTests` renders the root and its
  `20261013-objectspaces-bdn-confirmations` folder with
  `RetainedPerformanceReports`. It checks that every measured benchmark appears
  with its full name, exact profile and mean, and that rendering changes no file.

A failed or unverified case must keep its error and its unverified status. The
fixtures display it, check its captured samples and exclude it from
recommendations and coverage. A passing check therefore validates the failure
record and does not claim that the failed configuration works. After a fix,
retain the new run separately and keep the original failure evidence.

Direct study timings are screening evidence from one process, with reset batches
and a shuffled profile order. A lowest mean is a candidate for that exact
dataset, mix, feature set and backend, and overlapping intervals remain
unresolved. The report and the recommendation outputs flag potential temporal
drift when the medians of the first and last measured halves of a case differ by
more than 1.5 times. The flag marks the mean as provisional and removes no
samples, and the absence of a flag does not prove that timings are stationary.
Neither a passing evidence fixture nor a Dry benchmark establishes a universal
best configuration. The
[study guide](../../utils/Sphere10.Framework.Utils.Profiler/Studies/README.md)
explains how to rerun a study and how to confirm finalists in independent
BenchmarkDotNet processes on the intended storage device.

```powershell
# Print retained candidates and raw finalist samples in the console.
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0 --filter 'FullyQualifiedName~RecordedStudyResults|FullyQualifiedName~RecordedRecommendations|FullyQualifiedName~RecordedBenchmarkDotNetConfirmations' --logger 'console;verbosity=detailed' -- 'TestRunParameters.Parameter(name="ProfilerResultsRoot",value="E:/ProfilerEvidence")'
```

## Stream-mapped dictionary workloads

`StreamMappedDictionaries/StreamMappedDictionaryWorkloadTests` runs the canonical
`AssertEx.DictionaryIntegrationTest` helper from `Sphere10.Framework.NUnit`
against KVP and constant-length-key (CLK) stream-mapped dictionaries. Each case
uses a `MemoryStream`, 32-byte clusters and one of the `Debug`, `Default` and
`Performance` policies. The project links the core fixture's `TestObject`,
`TestObjectSerializer` and `TestObjectEqualityComparer` sources, so the payloads
match `Sphere10.Framework.Tests`.

| Cases | Items and iterations | Selection |
|---|---|---|
| Six `IntegrationTests_Smoke` cases | 8 items, 3 iterations | Run by default |
| Six `IntegrationTests_Heavy` cases | 250 items, 250 iterations | Explicit, `Performance` category, serial |

The heavy cases keep the helper's correctness assertions, setup and cleanup.
Their duration is therefore the elapsed time of the whole correctness workload,
not a BenchmarkDotNet kernel. The helper seeds its operation and input generator
with 31337, but its removal shuffles use the thread-local `Tools.Maths.RNG`,
which has no fixed seed. Preserve that behavior and record the limitation when
you compare runs.

Run the full workload, or its short verification, from the Framework root.
`serial.runsettings` runs one test host at a time with no NUnit parallel workers.

```powershell
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Debug -p:BuildRevision=0 --settings tests/Sphere10.Framework.Utils.Profiler.Tests/StreamMappedDictionaries/serial.runsettings --filter 'FullyQualifiedName~StreamMappedDictionaryWorkloadTests.IntegrationTests_Heavy' --logger 'trx;LogFileName=heavy-six.trx'

dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Debug -p:BuildRevision=0 --filter 'FullyQualifiedName~StreamMappedDictionaryWorkloadTests.IntegrationTests_Smoke|FullyQualifiedName~HeavyRegistryContainsBothImplementationsAndAllThreePolicies|FullyQualifiedName~RecordedStreamMappedDictionaryResults'
```

`StreamMappedDictionaries/RecordedStreamMappedDictionaryResults` reads each
`StreamMappedDictionaries/<campaign>/campaign.json` under `ProfilerResultsRoot`.
The manifest records these fields:

- `SchemaVersion` 1 and a `Workload` of `MemoryStream`, 32-byte clusters, 250
  items, 250 iterations, operation seed 31337 and a `ShuffleSeedCaveat`.
- `Runtime`, `Sdk` and `Host`, with `NUnitWorkers` 0 and `MaxCpuCount` 1.
- `CaptureHashes`, which maps the relative path of every raw TRX file to its
  SHA256.
- Two or more `Stages`. Each stage declares `Name`, `Configuration` (`Debug`),
  `CoreSha256`, `SourceProvenance`, an optional `ComparisonBase` and six `Cases`.
  Each case maps `Implementation`, `Policy`, `Trx`, `TestClass`, `TestName` and
  `SourceProvenance` to one passed result.

The fixture accepts both the core fixtures' result names and this project's. It
prints each stage's elapsed times against its comparison base without a
performance threshold. A later optimization pass adds a stage instead of
replacing earlier captures. Keep superseded runs, and map any accepted clean
rerun explicitly per case. `RecordedStreamMappedDictionaryResultsTests` checks
both capture shapes with synthetic TRX records. It covers result and definition
identity matching and the rejection of changed workloads, duplicate results,
failures and skipped cases, without running a workload. The
[stream-mapped dictionary report](../../docs/performance/stream-mapped-dictionary-hotpaths.md)
describes the measured campaign.

## ObjectSpace hot paths and page memory

`ObjectSpaces/ObjectSpaceHotPathTests` exercises warm identity lookups, clean
Merkle flushes, reindexing and mixed autosave work on memory and file stores,
with plain-lookup controls. Its 32 ordinary cases check persisted data, indexes
and Merkle roots, including a non-256-bit digest and both `BTreeType` values.
The explicit `TraceHotPath` (10 cases) and `TraceSelectedImplementationHotPath`
(20 cases) run the same workloads on a larger dataset for profiler sessions.
They repeat each workload `HotPathRepeats` times and write a
`trace-workload.json` manifest under `HotPathArtifactRoot`, which defaults to
`Results/ObjectSpaceHotPaths` in the test output folder. Profile the stacks
beneath `RunMeasuredBatch`, because preparation, verification and reopening run
outside that method. BenchmarkDotNet, not these traces, supplies the statistical
timing and allocation measurements.

```powershell
# The 32 ordinary hot-path cases.
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0 --filter 'FullyQualifiedName~ObjectSpaces.ObjectSpaceHotPathTests&Name!~Trace'

# The ten TraceHotPath workloads, each repeated 100 times for a profiler session.
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0 --filter 'FullyQualifiedName~ObjectSpaceHotPathTests.TraceHotPath' -- 'TestRunParameters.Parameter(name="HotPathRepeats",value="100")'
```

The profiler's `ObjectSpaceMerkleBenchmarks.CleanFlush` measures the same
repeated clean flushes on a settled Merkle store with BenchmarkDotNet. It offers
only Merkle-enabled profiles, and its preparation and final integrity and reopen
checks run outside the benchmark method.

`ObjectSpaces/RecordedObjectSpaceHotPathTests` reads three retained
BenchmarkDotNet campaigns under `ProfilerResultsRoot` and skips any campaign that
is absent. Each case checks the original settings, the paired host and
arguments, the successful worker logs and all 20 actual samples (two launches of
ten), then prints the before and after measurements. It asserts no timing
threshold, and its descriptive comparisons do not select a universal winning
tree.

| Campaign | Recorded cases |
|---|---|
| `20261022-objectspace-hotpaths` | 15 before/after pairs (five small workloads and two larger mutation batches per store, plus a large file-reindex confirmation) |
| `20261022-objectspace-page-btree` | 7 page-only pairs on File storage, including two confirmations and a reverse-order reindex, and 4 explicit `BTreeType` captures on Memory and File stores |
| `20261022-span-dispatch` | 2 File pairs (warm lookup and clean flush) for the later span-dispatch cleanup |

The [ObjectSpace hot-path](../../docs/performance/object-space-hotpaths.md),
[page reuse and tree selection](../../docs/performance/object-space-page-btree.md)
and [span inheritance](../../docs/performance/span-inheritance-review.md)
reports hold the configurations, profiling findings and limitations. They also
explain why the dispatch pairs do not isolate page pooling.

`ObjectSpaces/FilePageMemoryTests.FilePageStartupAndFlushFootprint` is an
explicit fresh-process diagnostic. It records process and GC memory at six
checkpoints, from before construction through 2,000 clean flushes of a Merkle
file store to disposal, and writes `file-page-memory.json` under
`FilePageArtifactRoot`. Only its final labeled checkpoint follows a forced
collection. Run it once per test host, and repeat it in separate invocations to
compare startup, post-workload and post-disposal memory. It is separate from the
BenchmarkDotNet timing and allocation jobs.

```powershell
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0 --filter 'FullyQualifiedName~RecordedObjectSpaceHotPathTests' --logger 'console;verbosity=detailed' -- 'TestRunParameters.Parameter(name="ProfilerResultsRoot",value="E:/ProfilerEvidence")'
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0 --filter 'FullyQualifiedName~FilePageMemoryTests.FilePageStartupAndFlushFootprint' -- 'TestRunParameters.Parameter(name="FilePageArtifactRoot",value="E:/PageMemoryRun")'
```

## Allocation diagnostics

The node I/O and temporary-buffer fixtures are explicit, serial allocation
diagnostics. Each records owner-thread allocations for one unrecorded warmup and
five recorded samples, with preparation and verification outside the measured
region. Run the same fixture and inputs against the saved before and after
Framework binaries. They establish allocation changes, not a statistical latency
result.

| Fixture | Workloads | Output parameter and default |
|---|---|---|
| `BTrees/BTreeNodeIoPerformanceTests.StreamedNodeMutationSamples` | Insert, update and delete-and-recycle of 512 keys on `StreamMappedBTree` and `StreamMappedBTreePlus`, in both byte orders and at orders 4, 64 and 256, with content and reopen checks and a hash of the stored bytes | `BTreeNodeIoArtifactRoot`, default `BTreeNodeIo` under the NUnit work directory |
| `IO/StreamTemporaryBufferPerformanceTests` | Stream buffers (`EndianBinaryWriter` character writes and `StreamProfiler` reads with and without capture) and serializer buffers (strings, constant-size padding, null references and nullable padding) | `StreamTemporaryBufferArtifactRoot`, default `Results/StreamTemporaryBuffers` in the test output folder |

```powershell
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0 --filter 'FullyQualifiedName~BTreeNodeIoPerformanceTests' -- 'TestRunParameters.Parameter(name="BTreeNodeIoArtifactRoot",value="E:/NodeIoRun")'
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0 --filter 'FullyQualifiedName~StreamTemporaryBufferPerformanceTests' -- 'TestRunParameters.Parameter(name="StreamTemporaryBufferArtifactRoot",value="E:/BufferRun")'
```

`BTrees/RecordedBTreeNodeIoTests` reads the retained `20261023-span-node-io`
campaign under `ProfilerResultsRoot`. It checks that four captures used one test
assembly and two Framework binaries, that every summary matches its raw samples,
and that both binaries produced identical stored bytes. The
[span node I/O report](../../docs/performance/btree-span-node-io.md) describes
the campaign.

## Local scope diagnostics

The scope benchmark fixtures, their captured campaign files and reproduction
notes are kept locally and excluded from version control. Behavioral scope
regression tests remain in the ordinary test projects, such as
[`Sphere10.Framework.Tests/Scopes`](../Sphere10.Framework.Tests/Scopes). The
[value-scope](../../docs/performance/value-scopes.md),
[scope-abstraction](../../docs/performance/scope-abstraction.md) and
[scope-composition](../../docs/performance/scope-composition.md) reports
summarize the historical workload definitions, measured results and their
comparison limits.

## Related documentation

- The [Framework profiler](../../utils/Sphere10.Framework.Utils.Profiler/README.md)
  lists the profiling domains and their commands.
- The [study guide](../../utils/Sphere10.Framework.Utils.Profiler/Studies/README.md)
  covers screening, replay plans and finalist confirmation.
- The [ObjectSpace](../../utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/README.md)
  and [B-tree](../../utils/Sphere10.Framework.Utils.Profiler/BTrees/README.md)
  profiler guides describe the BenchmarkDotNet workloads. Those benchmarks do
  not replace a test harness.
- The [core ObjectSpace fixtures](../Sphere10.Framework.Tests/ObjectSpaces)
  cover core index and runtime behavior.
