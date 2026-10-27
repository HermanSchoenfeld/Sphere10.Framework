# Sphere10 Framework profiler

This cross-platform .NET 10 console project is the home for framework performance
studies. Its root command selects a profiling domain; it never runs every
benchmark implicitly. The project uses the repository's centrally versioned
BenchmarkDotNet package and references Core directly, with no UI dependency.

## Visual Studio

Set `Sphere10.Framework.Utils.Profiler` as the startup project and use its default
launch profile, also named `Sphere10.Framework.Utils.Profiler`. Press **Ctrl+F5**
to run it. The checked-in [launch settings](Properties/launchSettings.json) start
`dotnet run --configuration Release` with `--help`. The default launch displays
the available commands without requiring historical capture files. The profile
builds and runs Release even when the solution toolbar selects Debug. Use
`study --report <external-capture-root>` to inspect captures you supply.

**F5** and the green Start button use the same profile. Their debugger attaches
to the `dotnet` launcher; the Release profiler and BenchmarkDotNet workers run as
child processes. Hot Reload is disabled for this launch. Use **Ctrl+F5** for
performance comparisons. Select the **ObjectSpace order study**, **BTree order
study**, **ObjectSpace parameter study** or **BTree parameter study** profile
to run fresh measurements. These use the repository drive for temporary data
and write a new uniquely named artifact directory. They can take several minutes.
The [study guide](Studies/README.md) describes dataset controls, replay plans,
screening limits and independent-process finalist confirmation.

## Command line

Run from the Framework root in Release, without a debugger:

```powershell
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- --help
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --help
```

CLI commands use `--no-launch-profile` to select their own arguments and skip the
Visual Studio launcher. Running the profiler executable without arguments still
prints domain help; the launch profile supplies its command explicitly.

| Domain | Workloads and support |
| --- | --- |
| `object-spaces` | [ObjectSpace benchmarks](ObjectSpaces/README.md), storage/index configuration suites and [deterministic lab models](ObjectSpaces/Lab/README.md). |
| `btrees` | [Regular/B+ and multimap trees](BTrees/README.md), six actual storage backends, key layouts, order/page/cache/buffer sweeps and leaf geometry. |
| `caches` | [Typed/object key access and admission pressure](Caches/README.md), LRU/FIFO policies, minimal/quarter/half eviction batches and verified fetch/victim accounting. |
| `study` | [Retained parameter studies](Studies/README.md), all three real workload mixes, exact replay plans, verified sample journals and evidence displayed by NUnit tests. |

Each domain owns its benchmark classes, configuration and sample models.
Measurement output belongs outside the source checkout. Add future framework domains alongside `ObjectSpaces/`
and register their explicit command in [Program.cs](Program.cs). Domain runners
must require a workload filter or an inspection command before execution, preserve
correctness checks outside measured regions, and document their measurement
boundaries. Keep shared runner concerns at the root when another domain needs them.

The existing ObjectSpace workloads, configuration sweeps and [measured pilot
evidence](ObjectSpaces/results/pilot-btree-file.md) are retained. Dry checks prove
execution; they do not select a preferred configuration. [Profiler tests](../../tests/Sphere10.Framework.Utils.Profiler.Tests/README.md)
cover the dispatcher, ObjectSpace, B-tree and cache correctness, evidence eligibility,
and the recorded decisions. Supply external captures and explicitly run `RecordedStudyResults` and
`RecordedBenchmarkDotNetConfirmations` in Visual Studio Test Explorer
to see the tables, samples, exact configurations and attached reports. The tests
recompute decisions from retained measurements rather than executing benchmarks
or asserting that another machine has the same speed.

`RecordedDefaultSelectionTests` checks the compiled constants against supplied measured profiles; `RecordedWorkerIdentityTests` reconciles supplied worker execution paths and binary metadata. These capture checks are explicit and use the `ProfilerResultsRoot` NUnit parameter. Historical result files are not shipped with the repository or copied during builds; see the [external-capture test instructions](../../tests/Sphere10.Framework.Utils.Profiler.Tests/README.md#external-historical-captures).

Use the `study` domain for all three mixes: balanced (50/50 reads/writes),
read-heavy (90/10) and write-heavy (10/90). Start with order and one-factor
parameter screening, test promising combinations and larger datasets, then
confirm finalists in independent BenchmarkDotNet launches. Results stay grouped
by backend, capabilities, data shape and workload. The domains preserve the
machine's current power plan.
