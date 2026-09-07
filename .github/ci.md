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
