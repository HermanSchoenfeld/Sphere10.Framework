# GitHub Actions tests

The [Build workflow](workflows/main.yml) runs one Windows job named `build_and_test`: check out the code, install .NET 10, build the cross-platform solution, and run its unit tests directly with `dotnet test`.

There are no discovery, partitioning, or reporting scripts, no compiled-test handoff, and no per-test watchdog. The workflow uses GitHub's default job timeout. Test failures and test-host crashes fail the job through the test runner's exit code.

## Build versions

The release version is `VersionPrefix` in `Directory.Build.props`. The build command passes `github.run_number` as `BuildRevision`, giving binaries a four-part file version such as `3.1.1.114`. Informational version carries that version plus the SDK's source revision metadata. NuGet package version stays `3.1.1` and assembly identity stays `3.1.1.0`.

[GitHub's workflow run number](https://docs.github.com/en/actions/reference/workflows-and-actions/variables) increases with each new run and stays the same when retrying that run. It continues across releases rather than restarting at one. Local builds default to revision zero; append `-p:BuildRevision=1` to the build command to produce `3.1.1.1`. No scripts or source-file updates are needed during CI.

## Seeing failures and test output

Open the workflow run, select `build_and_test`, then expand **Run unit tests**. The detailed console logger shows test names, failure messages, expected/actual values, stack traces, and test output directly in the GitHub log. NUnit standard output is enabled. Nothing redirects this output to a server-side log file.

The optional **test-results** artifact contains TRX reports even when tests fail. Downloading it is not necessary to read failures. Use GitHub's **Re-run failed jobs** to rebuild and rerun the job.

## Adding and running tests

Add fixtures and test cases normally; NUnit discovers them automatically. New test projects need to be added to `src/Sphere10.Framework (CrossPlatform).sln` to run in this workflow. Windows-only projects outside that solution retain their existing exclusion. Explicit tests remain opt-in, and existing `GITHUB_ACTIONS` skips remain in effect.

Run the same commands locally from the repository root:

```powershell
$env:GITHUB_ACTIONS = 'true'
dotnet build "src/Sphere10.Framework (CrossPlatform).sln" --configuration Debug --verbosity minimal
dotnet test "src/Sphere10.Framework (CrossPlatform).sln" --configuration Debug --no-build --logger "console;verbosity=detailed" --logger trx --results-directory TestResults -- NUnit.RandomSeed=1234567 NUnit.ExplicitMode=None NUnit.ConsoleOut=1
```

To focus on a failing test, use the same test command with `--filter "FullyQualifiedName~YourFixture.YourMethod"` before the standalone `--`. The fixed NUnit seed keeps generated cases reproducible.

The logging options are documented by [Microsoft](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-vstest) and [NUnit](https://docs.nunit.org/articles/vs-test-adapter/Tips-And-Tricks.html).

## Cluster memoization

The earlier partitioned runner imposed a ten-minute watchdog that could terminate a busy test host during the original dictionary stress workload. That runner and its watchdog have been removed; test iteration counts and correctness checks are unchanged.

Logical-cluster memoization remains controlled by the file-local `#define LogicalClusterMemoizationOptimization` in `src/Sphere10.Framework/ClusteredStreams/ClusterSeeker.cs`. Comment out that line and rebuild to compare against the original start/end/current traversal. The checkpoint limit is `MaxCheckpoints` in that file. Disabling memoization may increase runtime enough to hit CI job limits. Direct flag operations and data-only write handling are independent of that switch.

## Stable dependencies

`Directory.Packages.props` uses stable dependency versions for the stable `3.1.1` release. When updating packages, exclude prerelease versions so packing does not produce NU5104 warnings. Preview dependencies require an explicitly prerelease package version.
