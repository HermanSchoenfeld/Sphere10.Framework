# Parameter studies and verifiable evidence

The `study` command screens actual parameter configurations using the same
ObjectSpace and B-tree fixtures as the BenchmarkDotNet domains. It retains every
measured batch, exact settings and correctness outcome. Recommendations are
computed from those outputs. Tests verify the recorded evidence and show the
tables in Visual Studio Test Explorer; unit tests never silently run benchmarks.

## View results in Visual Studio

Build `Sphere10.Framework.Utils.Profiler.Tests`, supply an external capture root
with the `ProfilerResultsRoot` NUnit parameter, then explicitly run
`RecordedStudyResults` and `RecordedBenchmarkDotNetConfirmations` in **Test Explorer**. Select a test
and inspect its output and attachments. Screening output shows each parameter's
mean, approximate 95% interval, allocations and candidates whose intervals
overlap. Confirmation output includes BenchmarkDotNet's original measured values.
No assertion requires your machine to reproduce another machine's timing.

Historical captures are no longer shipped in the source repository. A supplied
external study directory contains `run-plan.json`, `host.json`, `samples.jsonl`, `samples.csv`,
`results.json`, `recommendations.json`, `report.md` and storage metadata.
Keep independent BenchmarkDotNet reports alongside those external studies.
`RecordedStudyResults` recomputes each decision from the sample durations and
reconciles the durations with the retained clock ticks and append-only journal.

Display the recomputed tables without running another measurement:

`study --report` reads both direct screening plans and independent BenchmarkDotNet Full JSON, including a directory containing only BDN reports. When `final-default-selection.json` exists at the supplied root, it prints the recorded objective, each constant's value, exact source and rationale first. The following tables retain raw samples, exact profile settings and reported confidence levels. Reporting writes no files and starts no measurements.

```powershell
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- study --report E:/ProfilerEvidence
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0 --filter 'FullyQualifiedName~Recorded' --logger 'console;verbosity=detailed' -- 'TestRunParameters.Parameter(name="ProfilerResultsRoot",value="E:/ProfilerEvidence")'
```

## Measure and replay

Run from the Framework root in Release, without a debugger. Choose a data root
on the disk being measured. The runner creates and disposes only its own GUID
workspaces; retained outputs live in the separate output directory.

```powershell
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- study --domain object-spaces --suite order --records 1024 --operations 200 --data-root D:/ProfilerData --output BenchmarkDotNet.Artifacts/my-objectspace-orders
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- study --domain object-spaces --suite parameters --records 256 --operations 100 --data-root D:/ProfilerData --output BenchmarkDotNet.Artifacts/my-objectspace-parameters
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- study --domain btrees --suite order --records 2048 --operations 512 --data-root D:/ProfilerData --output BenchmarkDotNet.Artifacts/my-btree-orders
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- study --domain btrees --suite parameters --records 2048 --operations 512 --data-root D:/ProfilerData --output BenchmarkDotNet.Artifacts/my-btree-parameters
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- study --plan BenchmarkDotNet.Artifacts/my-btree-orders/run-plan.json --data-root D:/ProfilerData --output BenchmarkDotNet.Artifacts/my-btree-replay
```

Use `--show-plan` to inspect cases before execution. Edit a copy of a retained
plan to select finalists, combine parameters or vary dataset size, key shape,
category cardinality, payload distribution, insertion order and read locality.
Replay uses the retained warmup/sample counts. Output directories cannot replace
an existing sample journal. A failed configuration is retained and excluded from
recommendations; it does not become faster by omitting failed samples.

Order sweeps cover 8–512. ObjectSpace additionally tests 169/170, around the
4 KiB serialized-node boundary for its actual long identifier and integer lookup
indexes. `Order - 1` is maximum leaf occupancy; total leaves are measured geometry.
The B-tree suite covers managed nodes, MemoryStream, FileStream, framework paging,
actual OS memory mapping and framework transactional files, for regular B-trees,
B+ trees and multimap lookup trees. Lookup keys use Int32 with Int32 ordinals;
dictionary keys use Int64. Custom plans support the other key shapes.

Parameter suites sweep one factor at a time. ObjectSpaces cover outer/inner
cluster sizing, policies, indexes, autosave/Merkle, byte order and file page/cache
sizes. B-trees cover applicable pages/cache and FileStream buffering. Combine
promising settings and measure their interaction before adopting a configuration.

## Measurement boundaries

Each group uses deterministic randomized profile order in every round, two warmup
batches and seven measured batches by default. Setup restores a baseline outside
the timer; verification checks the result outside it. Mixed batches measure reads,
updates and persistence. B-tree writes split between updates and delete/reinsert;
ObjectSpace writes alternate balance-only updates and secondary-index changes.
The three real mixes are 50/50, 90/10 and 10/90 reads/writes. Read and write ratios
are explicit inputs, rather than inferred from differently weighted timings.

The timer measures a synchronous batch on the owning thread. Allocation counters
cover that thread's managed allocations. GC collection deltas are retained.
There is no forced collection and no removal of inconvenient samples. Small
batches can be noisy: increase operations and dataset size for confirmation.
Direct runs share one process and its JIT/GC/OS state; they select screening
candidates rather than proving a universal optimum.

Rankings separate storage backend, tree semantics, key shape, dataset controls,
index membership, runtime checks and Merkle digest. Autosave and explicit save
both complete the same persisted batch. They do not imply identical intermediate
save behavior. File flush, mapped-view flush and transactional commit have
different guarantees. Reopening resets framework state without clearing OS cache.

## Confirm finalists

Retain independent-process BenchmarkDotNet measurements for competing finalists,
including the existing baseline. Use at least two process launches with ten actual
batches each and enough warmup to avoid the transitions visible in screening.
Keep all samples. The invocation is already a macro workload, so
Monitoring avoids a pilot phase that repeatedly mutates an unreset store.

```powershell
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --config finalists.json --records 1024 --operations 200 --categories 1024 --mix Balanced --insertion shuffled --filter '*MixedReadWriteAndPersist*' --strategy Monitoring --warmupCount 5 --iterationCount 10 --invocationCount 1 --unrollFactor 1 --launchCount 2 --outliers DontRemove --noForcedGCs --data-root D:/ProfilerData --output BenchmarkDotNet.Artifacts/objectspace-confirmation
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- btrees --config tree-finalists.json --records 8192 --operations 2048 --mix balanced --filter '*MixedAndPersist*' --strategy Monitoring --warmupCount 5 --iterationCount 10 --invocationCount 1 --unrollFactor 1 --launchCount 2 --outliers DontRemove --noForcedGCs --data-root D:/ProfilerData --output BenchmarkDotNet.Artifacts/btree-confirmation
```

`--invocationCount 1` means a time target cannot enlarge a batch; increase
`--operations` instead. Dry is an execution check and cannot provide performance
evidence. Retain the config array, domain `run.json`, Full JSON and CSV reports,
storage geometry, execution log and commands together. Declare changes in power
plan, disk, background load, data shape and durability. A lowest observed mean
with overlapping uncertainty remains a candidate; it is not a resolved optimum.
