# GitHub Actions tests

For the framework overview, installation, and project documentation, see the [main README](../README.md).

The [Build workflow](workflows/main.yml) builds the cross-platform solution once with the .NET 10 SDK, then runs independently retryable NUnit partitions using the compiled binaries. Windows runners preserve the current platform and native-library behavior.

## Adding tests

Add tests normally. [New-TestMatrix.ps1](scripts/New-TestMatrix.ps1) reads the solution, evaluates MSBuild's `IsTestProject` property, and discovers compiled tests with the installed NUnit adapter. It creates up to eight partitions per assembly/target framework, targeting approximately 1,000 discovered cases per partition. The current suite produces 17 jobs: eight core, eight CryptoEx, and one Data. Communications currently contains no active tests and is listed in the discovery summary without creating a job.

New methods, fixtures, generated/parameterized cases, and test projects added to the cross-platform solution need no YAML changes. Projects outside that solution retain their existing exclusion from this workflow. An invalid test definition or failed discovery fails the build job instead of silently losing tests.

[NUnit's native partition filter](https://github.com/nunit/nunit/blob/main/src/NUnitFramework/framework/Internal/Filters/PartitionFilter.cs) assigns each case by its full name. Every partition uses the same random seed so NUnit-generated random cases stay consistent. Explicit tests remain opt-in, and existing ignored tests and `GITHUB_ACTIONS` skips remain in effect. Keep `AssemblySelectLimit` and `ExplicitMode` in [ci.runsettings](ci.runsettings): they prevent the adapter from discarding a large filter or selecting explicit tests.

Partitions balance case counts, not measured runtime. Fixture setup can run in several partitions, and individual slow tests can still dominate a partition. Eight jobs run concurrently; a failed partition does not cancel other results. A new push on the same branch cancels the superseded workflow.

## Finding and rerunning failures

Open the failed job's summary for pass/fail/skip counts, failed test names, error details, stack traces, and the slowest tests. Download its `test-results-*` artifact for the complete `test.log`, TRX report, and exact `partition.runsettings`. Successful informational test output stays in the artifacts. The overall `build_and_test` check requires every partition and the build to succeed.

Use GitHub's **Re-run failed jobs** to repeat failed partitions without repeating successful ones. Test artifacts and compiled binaries are retained for 14 days, after which a rerun requires **Re-run all jobs** to rebuild them.

To reproduce a partition locally, run these commands from the repository root using PowerShell 7:

```powershell
$env:GITHUB_ACTIONS = 'true'
dotnet build "src/Sphere10.Framework (CrossPlatform).sln" --configuration Debug --verbosity minimal
./.github/scripts/New-TestMatrix.ps1
# Select the relevant entry from TestResults/ci-build/matrix.json:
./.github/scripts/Invoke-TestShard.ps1 -Assembly "TestResults/ci-build/assembly-2/Sphere10.Framework.CryptoEx.Tests.dll" -Shard 1 -ShardCount 8 -ResultsDirectory TestResults/local-shard
./.github/scripts/Write-TestSummary.ps1 -ResultsDirectory TestResults/local-shard
```

The scripts require fresh output directories to prevent stale binaries or results from being mistaken for this run. Supply a new `-OutputDirectory` or `-ResultsDirectory` on subsequent local runs.

To focus on one failing fixture or method without changing the workflow:

```powershell
dotnet test tests/Sphere10.Framework.Tests/Sphere10.Framework.Tests.csproj --no-build --settings .github/ci.runsettings --filter "FullyQualifiedName~YourFixture.YourMethod"
```

The runner stops a test host if a test hangs for ten minutes and saves the available diagnostics; each matrix job also has a 60-minute limit. These failures stay red. No automatic retries hide flaky tests.

## Maintaining CI

The YAML handles orchestration; the scripts handle discovery, execution, and reporting. NUnit's `DumpXmlTestDiscovery` output is used for inventory and sizing only; execution uses NUnit's partition filter, avoiding hand-maintained test lists or parsing C# source. When updating the adapter, verify discovery and partition behavior together. [NUnit adapter settings](https://docs.nunit.org/articles/vs-test-adapter/Tips-And-Tricks.html) document the shared runsettings.

Tune `-TestsPerShard` and `-MaxShardsPerAssembly` on the discovery step only when changing CI capacity; adding tests does not require these changes. The plan fails clearly if it would exceed GitHub's 256-job matrix limit.

## Diagnosing watchdog terminations

A test-host crash message can be the consequence of VSTest's blame watchdog terminating a long-running test. Inspect the artifact's `test.log` for the inactivity timeout and its `Sequence_*.xml` for unfinished cases. The September 7 failure in run 34131690660, core partition 8/8, reported 1,054 passing tests while `StreamMappedDictionaryCLKTests.IntegrationTests_Heavy(MemoryStream,Debug,250)` exceeded the ten-minute watchdog.

Local live-stack captures showed active B-tree cluster traversal and repeated descriptor maintenance during data writes. `ClusterSeeker` now retains at most 1,024 evenly spaced logical-to-physical checkpoints and uses direct flag operations instead of reflection-based enum helpers. Appends retain valid checkpoints, tail removals discard out-of-range entries, and tip migrations update their physical locations. Resizing that changes their spacing, untracked link edits and chain recreation invalidate them. The recreation case also covers a descriptor chain rebuilt after a suppressed clear. Data-only writes skip topology maintenance and unchanged cluster counts are no longer written back to the header.

Logical-cluster memoization is controlled by the file-local `#define LogicalClusterMemoizationOptimization` at the top of `src/Sphere10.Framework/ClusteredStreams/ClusterSeeker.cs`. Comment out that line and rebuild to compare against the original start/end/current traversal, with checkpoint allocation, lookup and maintenance compiled out. Restore the define and rebuild to enable it again. The checkpoint limit is the `MaxCheckpoints` constant in that file. Cluster-seeker tests check correctness and the expected cluster-read counts in either build. This switch controls memoization only; direct flag operations and data-only write handling remain active, allowing a comparison that isolates memoization. **Disabling memoization may cause long-running unit tests on GitHub Actions to exceed the timeout and fail.**

The original stress tests, iteration counts, seeds, partitioning and CI timeouts are retained. Dictionary stress tests emit progress every 25 iterations when NUnit output is enabled; CI runsettings suppress that output by default. The summary script continues to fail aborted runs.

Run the original stress workload with the normal NUnit runner:

```powershell
dotnet test tests/Sphere10.Framework.Tests/Sphere10.Framework.Tests.csproj -c Debug --filter 'FullyQualifiedName~IntegrationTests_Heavy' --blame-hang --blame-hang-timeout 10m
```
