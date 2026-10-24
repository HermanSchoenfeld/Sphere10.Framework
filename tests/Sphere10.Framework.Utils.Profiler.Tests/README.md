# Profiler tests

NUnit coverage for the general profiler command, ObjectSpace, B-tree and cache workloads,
parameter-study contracts and retained measurement evidence.

`StreamMappedDictionaries/StreamMappedDictionaryWorkloadTests` registers the six
`IntegrationTests_Heavy` cases: KVP and constant-length-key dictionaries on
`MemoryStream`, 32-byte clusters, 250 maximum items, 250 iterations, and the
Debug/Default/Performance policies. It reuses the canonical NUnit dictionary
integration helper and links the core fixture's payload/serializer/comparer
sources. The ordinary smoke cases use eight maximum items and three iterations.
The full cases are explicit and run serially; they include the original
correctness assertions and setup/cleanup rather than a BenchmarkDotNet kernel.
The helper seeds operation/input generation with 31337, but its existing removal
shuffles use the thread-local RNG without a public seed scope. Preserve that
behavior and record this limitation when comparing runs.

Run the matching full workload or its short verification from the Framework root:

```powershell
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Debug -p:BuildRevision=0 --settings tests/Sphere10.Framework.Utils.Profiler.Tests/StreamMappedDictionaries/serial.runsettings --filter 'FullyQualifiedName~StreamMappedDictionaryWorkloadTests.IntegrationTests_Heavy' --logger 'trx;LogFileName=heavy-six.trx'

dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Debug -p:BuildRevision=0 --filter 'FullyQualifiedName~StreamMappedDictionaryWorkloadTests.IntegrationTests_Smoke|FullyQualifiedName~HeavyRegistryContainsBothImplementationsAndAllThreePolicies|FullyQualifiedName~RecordedStreamMappedDictionaryResults'
```

`StreamMappedDictionaries/RecordedStreamMappedDictionaryResults` reads local
`Results/StreamMappedDictionaries/<campaign>/campaign.json` captures. It checks
the workload/runtime controls, declared Core binary SHA256, hashed raw TRX files,
and all six passed results by their actual test names and classes. Its output
compares elapsed times without a performance threshold. A campaign records
relative capture filenames, source provenance, and stages containing six case
mappings (`Implementation`, `Policy`, `Trx`, `TestClass`, `TestName`,
`SourceProvenance`). Each stage declares `Name`, `Configuration`, `CoreSha256`,
`SourceProvenance`, and an optional `ComparisonBase`; later optimization passes
can retain additional stages without replacing earlier captures. The manifest's
`CaptureHashes` map identifies every raw TRX by SHA256. Original superseded runs
remain retained, with any accepted clean reruns mapped explicitly per case.
Missing local campaigns skip the evidence fixture; malformed or incomplete
present campaigns fail. Raw campaign folders stay ignored local output.
`RecordedStreamMappedDictionaryResultsTests` uses synthetic TRX records to check
both exact capture shapes (original Core fixtures and the profiler fixture),
including result/definition identity matching and rejection of changed workloads,
duplicate results, failures and skipped cases. It does not run a workload.

`ProfilerCommandTests` verifies help/explicit domain selection, unknown-domain
rejection, configuration inspection and the requirement for a benchmark filter
before output artifacts are created.

`Caches/CacheProfilerTests` verifies typed/object/preboxed hot-key access and
capacity pressure against an independent LRU/FIFO linked-list oracle. It covers
minimal/quarter/half reap levels, unit/weighted estimator sizes, actual fetch and
victim sequences, capacity accounting, deterministic resets, simulated fetch
work, retained metrics and invalid controls. These are correctness fixtures;
they do not run BenchmarkDotNet or assert a timing threshold. Read the
[cache profiling guide](../../utils/Sphere10.Framework.Utils.Profiler/Caches/README.md)
for the measured batch and refill boundaries.

`Caches/RecordedCacheMetricsTests` reads retained cache measurements and prints
verified fetches, reaps, victims and final occupancy in Test Explorer output.
It checks the exact profile, estimator-size accounting and deterministic
checksums across independent workers. Read its output alongside
`RecordedBenchmarkDotNetConfirmations` for timings and allocation measurements;
cache-metrics files do not assign batches to BenchmarkDotNet sample stages.
The [current cache results](Results/cache-projection-performance-summary.md)
summarize 19 measured cases from the canonical typed core and explicit object
projection. Their three `projection-bdn` phases contain 38 successful workers
and 380 actual timing samples. The [earlier implicit-object results](Results/cache-performance-summary.md)
and their original worker evidence remain separately retained.

New benchmark campaign folders under `Results/` are ignored local output.
Existing checked-in evidence and top-level reports remain versioned. The cache
evidence fixture skips when no local cache captures are available; the workload
correctness tests run normally. Keep original reports and worker identities
together locally when inspecting a capture.

`BTrees/BTreeProfilerTests` verifies B-tree, B+ tree and multimap lookup behavior
across managed nodes, MemoryStream, FileStream, framework paged files, true OS
memory-mapped files and transactional files. It covers supported key layouts,
oracle-checked queries/mutations, recycled slots, rollback/reopen, reachable
leaf geometry, logical bytes versus mapped capacity, and configuration rejection.

`ObjectSpaces/ObjectSpaceWorkloadTests` verifies the ObjectSpace workload controls,
deterministic data distributions, read/write mixtures, payload growth/shrink,
effective parameter groups and persisted results. Its tests exercise storage
behavior; their execution time is not a performance measurement.

`ObjectSpaces/Lab/ObjectSpaceLabTests` preserves the PayloadOnly / DeveloperLab
schema and configuration coverage:

- Deterministic roundtrip and unique lookup at several real B-tree orders.
- Explicit save versus dirty-flag autosave across rehydration.
- File commit, rollback identity invalidation and reopen.
- Rejection of incompatible or unknown file layouts before data changes.
- Deletion/recycling and detached editor payload ownership.
- Configuration validation before storage creation.
- Independent Records/Groups/Events round trips and declared secondary indexes, with and without Merkle integrity.
- Version 2 manifest completeness and explicit version 1 mapping to the unchanged PayloadOnly record layout.

## Measurement evidence in Test Explorer

Open **Test > Test Explorer** in Visual Studio and build the solution. Run these
fixtures under `Sphere10.Framework.Utils.Profiler.Tests`:

1. **PerformanceEvidenceTests** checks rejection of incomplete, failed and malformed
   samples; capability/dataset separation; uncertainty ties; sample statistics;
   temporal drift diagnostics; and supported-workload preflight. These are ordinary tests of the study runner
   and analyzer, using synthetic data rather than timing assertions.
2. **RecordedStudyResults** reads the retained studies under [Results](Results).
   Select an individual result after running it and open its **Output** to see
   each measured candidate, sample count, interval and exact comparison group.
   The attached JSON, journal, CSV and Markdown files contain configurations,
   host/clock information, raw rounds and the derived report. The fixture
   reconciles clock ticks with nanoseconds, checks reset checksums, verifies every
   CSV/journal field against the raw JSON, and reproduces the displayed decisions
   from those samples.
3. **RecordedRecommendations** provides an individual Test Explorer result for
   each study, backend, B-tree family, read/write mix and parameter factor. Run the
   result of interest and open its **Output** for the candidate, uncertainty ties,
   every profile's timings/allocations, exact settings and raw samples. Its
   coverage test also requires measured evidence for Memory/File ObjectSpaces
   and all six B-tree backends, three tree families and three mixes.
4. **RecordedBenchmarkDotNetConfirmations** displays separately retained finalist
   measurements from independent BenchmarkDotNet worker processes. Its output
   includes raw timing samples and reported confidence intervals. It rejects Dry
   execution checks and incomplete reports as confirmation evidence. Controlled
   finalist runs reconcile at least two worker launches and ten actual batches
   per worker with every retained result sample and reported statistics. Their
   attached run manifests declare Monitoring, five warmups, `DontRemove`, one
   invocation and no forced GC; retained command files must agree. Retained
   execution status and original console logs also require successful domain
   and worker exits, including the oracle checks outside timed work. Any status
   reconstructed after execution identifies that provenance explicitly. Full JSON
   records launch indices and measurements but does not contain structured
   outlier-policy settings, so the fixture verifies both sources. Earlier pilot
   reports retain their original job controls and are identified by their output.
   Its finalist coverage test requires actual 20-sample mixed-workload results
   for Memory/File ObjectSpaces and all six B+ tree backends across three mixes.
   It matches each benchmark's complete profile name to its run manifest;
   an unused profile declared in a configuration cannot satisfy coverage.

Original ObjectSpace confirmation hashes in `execution-host.json` identify the
launcher before BenchmarkDotNet builds its separate workers. The additional
`worker-binary-fingerprints.json` audits record that those original generated
binaries are unavailable, so their hashes/versions/MVIDs cannot be inferred.
`--outliers DontRemove` preserves samples; retaining worker binaries also needs
`--keepFiles` and a phase-specific capture before later builds reuse the folder.
Future worker fingerprints must be recorded separately from launcher identities.

**RecordedDefaultSelectionTests** provides one result per final Core constant. Its Output identifies the chosen value, rationale, exact measured configuration and raw samples; it rejects an unexecuted or failed source. Constructor/default integration fixtures separately exercise current presets, reopening, rollback and refusal without mutation. **RecordedWorkerIdentityTests** checks original worker commands/build paths, actual profiles/launches, captured SHA256/versions/MVIDs and optional local DLL archives. Clone-side checks do not require ignored binaries or compare Git-normalized project text against capture-time raw-byte hashes.

Controlled confirmation checks also reconcile the exact declared warmup count with every worker's retained warmup measurements. The managed Balanced refinement uses 30 warmups to address timing transitions seen in the original five-warmup results; both runs remain separately retained. All actual batches remain in the statistics.

Running these fixtures reads existing evidence; it does not start a new
performance campaign or require this machine's timing to match recorded timing.
There are no hard-coded fastest orders or performance pass/fail thresholds.
Missing or unexplained incomplete evidence fails its fixture instead of silently
showing a recommendation. A measured case that failed setup or verification must
retain its error and unverified status. The fixture displays that failure and
checks its captured samples; it excludes the case from recommendations and
successful coverage. A passing evidence check validates the failure record and
does not claim that the failed configuration works. After a fix, retain its new
run separately and preserve the original raw failure evidence.
Results are copied into the test output during build,
so rebuild after replacing a retained report.

Direct study timings are screening evidence from one process with reset batches
and shuffled profile order. A lowest mean is a measured candidate for that exact
dataset, mix, features and backend. Overlapping intervals remain unresolved;
neither a passing evidence fixture nor a Dry benchmark establishes a universal
best configuration. Use the [general profiler](../../utils/Sphere10.Framework.Utils.Profiler/README.md)
to rerun a study or execute a new finalist comparison on the intended storage
device, preserving its full plan and raw measurements.

The report and individual recommendation outputs warn about potential temporal
drift when the medians of the first and last measured halves differ by more than
1.5 times. This detects sustained timing changes such as incomplete warmup; it
does not prove their cause or remove samples. A flagged mean is provisional.
Use sufficiently warmed independent-process confirmation before making a choice;
the absence of a warning is not proof that timings are stationary.

Run from the Framework root:

```powershell
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -p:BuildRevision=0

# Print retained candidates and raw finalist samples in the console.
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -p:BuildRevision=0 --filter 'FullyQualifiedName~RecordedStudyResults|FullyQualifiedName~RecordedRecommendations|FullyQualifiedName~RecordedBenchmarkDotNetConfirmations' --logger 'console;verbosity=detailed'
```

The [core ObjectSpace fixtures](../Sphere10.Framework.Tests/ObjectSpaces) cover core index/runtime changes. [Profiler execution checks](../../utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/README.md) use BenchmarkDotNet; they are not a replacement test harness.

`ObjectSpaceHotPathTests` exercises warm identity lookups, clean Merkle flushes,
reindexing and mixed autosave work in memory and file stores, with plain lookup
controls. Thirty-two ordinary cases check persisted data, indexes and Merkle
roots, including a non-256-bit digest and both explicit `BTreeType`
choices. Ten explicit `TraceHotPath` and twenty
`TraceSelectedImplementationHotPath` cases reuse the same fixture with
`HotPathRepeats` and `HotPathArtifactRoot` runsettings parameters.
Profile stacks beneath `RunMeasuredBatch`; preparation, integrity verification
and reopening are outside that marker. These cases provide trace workloads;
BenchmarkDotNet supplies the statistical timing and allocation measurements.

```powershell
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0 --filter 'FullyQualifiedName~ObjectSpaces.ObjectSpaceHotPathTests&Name!~TraceHotPath'
```

`ObjectSpaceMerkleBenchmarks.CleanFlush` measures repeated clean flushes against
an already settled Merkle store. Setup and final integrity/reopen checks remain
outside the benchmark. Select a Merkle-enabled profile when running it.

`RecordedObjectSpaceHotPathTests` checks the fifteen paired cases retained in
`Results/20261022-objectspace-hotpaths`: the small workload matrix plus larger
mutation batches and a separate file-reindex confirmation. It checks the original settings, successful worker logs and
all twenty actual samples per case, and prints the before/after measurements.
It validates retained evidence without requiring the current machine to meet
a timing threshold. See the [ObjectSpace performance report](../../docs/performance/object-space-hotpaths.md)
for configuration, profiling findings and limitations.

The same fixture adds seven page-only before/after pairs and four explicit-tree
cases from `Results/20261022-objectspace-page-btree`. Two further paired cases
in `Results/20261022-span-dispatch` compare the subsequent inheritance cleanup,
for twenty-eight recorded cases across the three campaigns. Both dispatch
captures already contain page pooling, so their comparison does not isolate that
change. The page campaign retains all repeated mutation
captures, including the worse adjacent reindex result and the reverse-order
confirmation. Explicit implementation measurements cover both Memory and File
stores. These descriptive comparisons do not select a universal winning tree.
See the [page reuse and tree selection report](../../docs/performance/object-space-page-btree.md).
The [inheritance review](../../docs/performance/span-inheritance-review.md)
records the retained/reverted optimizations and subclass extension points.

`FilePageMemoryTests.FilePageStartupAndFlushFootprint` is a separate explicit
fresh-process diagnostic. Run it once per test process, passing
`FilePageArtifactRoot` for its JSON output; repeat with separate invocations
to compare startup, post-workload and post-disposal process memory. Only its final
labeled checkpoint forces collection. This diagnostic is separate from the
BenchmarkDotNet timing/allocation jobs.

```powershell
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0 --filter 'FullyQualifiedName~RecordedObjectSpaceHotPathTests' --logger 'console;verbosity=detailed'
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0 --filter 'FullyQualifiedName~FilePageMemoryTests.FilePageStartupAndFlushFootprint' -- 'TestRunParameters.Parameter(name="FilePageArtifactRoot",value="E:/PageMemoryRun")'
```

`BTreeNodeIoPerformanceTests.StreamedNodeMutationSamples` is an explicit serial
allocation diagnostic for both streamed tree types, both byte orders, and orders
4, 64 and 256. It measures insert, update, deletion and node recycling, then
checks the independent contents, reopening and complete stored-byte hashes.
Pass `BTreeNodeIoArtifactRoot` through runsettings for its JSON output.
`StreamTemporaryBufferPerformanceTests` measures temporary stream and serializer
buffers separately; its output parameter is `StreamTemporaryBufferArtifactRoot`.
Both diagnostics retain one warmup and five samples, with preparation and
verification outside measurement. Compare the same fixture and inputs against
the saved before/after Framework binaries; these diagnostics establish allocation
changes, not a statistical latency result.
