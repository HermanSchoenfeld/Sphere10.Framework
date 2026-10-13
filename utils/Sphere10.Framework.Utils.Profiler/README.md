# Sphere10 Framework profiler

This cross-platform .NET 10 console project is the home for framework performance
studies. Its root command selects a profiling domain; it never runs every
benchmark implicitly. The project uses the repository's centrally versioned
BenchmarkDotNet package and references Core directly, with no UI dependency.

## Visual Studio

Set `Sphere10.Framework.Utils.Profiler` as the startup project and use its default
launch profile, also named `Sphere10.Framework.Utils.Profiler`. Press **Ctrl+F5**
to run it. The checked-in [launch settings](Properties/launchSettings.json) start
`dotnet run --configuration Release` and display rankings recomputed from the
retained profiler-test results. It shows the measured configurations immediately.
The report begins with the selected framework constants and their exact evidence references, then includes both screening and independent-process measurements. The profile builds and runs Release even when the solution toolbar selects Debug;
it does not require you to enter arguments in project properties.

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
prints domain help; the launch profile supplies its benchmark selection explicitly.

| Domain | Workloads and support |
| --- | --- |
| `object-spaces` | [ObjectSpace benchmarks](ObjectSpaces/README.md), storage/index configuration suites and [deterministic lab models](ObjectSpaces/Lab/README.md). |
| `btrees` | [Regular/B+ and multimap trees](BTrees/README.md), six actual storage backends, key layouts, order/page/cache/buffer sweeps and leaf geometry. |
| `study` | [Retained parameter studies](Studies/README.md), all three real workload mixes, exact replay plans, verified sample journals and evidence displayed by NUnit tests. |

Each domain owns its benchmark classes, configuration, sample models and retained
results in its subfolder. Add future framework domains alongside `ObjectSpaces/`
and register their explicit command in [Program.cs](Program.cs). Domain runners
must require a workload filter or an inspection command before execution, preserve
correctness checks outside measured regions, and document their measurement
boundaries. Keep shared runner concerns at the root when another domain needs them.

The existing ObjectSpace workloads, configuration sweeps and [measured pilot
evidence](ObjectSpaces/results/pilot-btree-file.md) are retained. Dry checks prove
execution; they do not select a preferred configuration. [Profiler tests](../../tests/Sphere10.Framework.Utils.Profiler.Tests/README.md)
cover the dispatcher, ObjectSpace and B-tree correctness, evidence eligibility,
and the recorded decisions. Run the `RecordedStudyResults` and
`RecordedBenchmarkDotNetConfirmations` fixtures in Visual Studio Test Explorer
to see the tables, samples, exact configurations and attached reports. The tests
recompute decisions from retained measurements rather than executing benchmarks
or asserting that another machine has the same speed.

`RecordedDefaultSelectionTests` checks the compiled constants against the exact measured profile/case and explains each tradeoff in its Output. `RecordedWorkerIdentityTests` reconciles worker execution paths and captured binary metadata; ignored local DLL archives are optional on a fresh clone. The [results index](../../tests/Sphere10.Framework.Utils.Profiler.Tests/Results/README.md) links the recommendations, final manifest and raw evidence.

Use the `study` domain for all three mixes: balanced (50/50 reads/writes),
read-heavy (90/10) and write-heavy (10/90). Start with order and one-factor
parameter screening, test promising combinations and larger datasets, then
confirm finalists in independent BenchmarkDotNet launches. Results stay grouped
by backend, capabilities, data shape and workload. The domains preserve the
machine's current power plan.
