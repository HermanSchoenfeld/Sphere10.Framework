# Profiler tests

NUnit coverage for the general profiler command, ObjectSpace and B-tree workloads,
parameter-study contracts and retained measurement evidence.

`ProfilerCommandTests` verifies help/explicit domain selection, unknown-domain
rejection, configuration inspection and the requirement for a benchmark filter
before output artifacts are created.

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
