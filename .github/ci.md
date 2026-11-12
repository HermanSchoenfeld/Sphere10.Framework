# GitHub Actions tests

The [Build workflow](workflows/main.yml) compiles the cross-platform solution on Windows with the .NET 11 SDK and both .NET 10/11 targets. **Test (net10.0)** and **Test (net11.0)** download the compiled assemblies and run them directly with `dotnet vstest`. Test does not rebuild or restore. **Wizard browser tests** runs independently.

**Validate release identity** checks the publishing contract and selects an exact version. **Pack and validate public packages** builds all packable Windows-solution libraries and exercises isolated .NET 10/11 consumers. **Publish NuGet** requires all of these checks to succeed, and runs only for eligible pushes in the intended repository. See [NuGet publishing](../docs/nuget-publishing.md) for artifact recovery and Trusted Publishing setup.

There are no discovery, partitioning, or reporting scripts and no per-test watchdog. The workflow uses GitHub's default job timeout. Test failures and test-host crashes fail the job through the test runner's exit code.

## Build versions

The next release base is `VersionPrefix` in `Directory.Build.props`, currently `3.2.0`. CI uses `BuildRevision=0`. Published package versions identify the run and attempt, while repository and informational metadata identify the full source SHA. Tags use their exact version. The numeric release base supplies assembly identity and file version.

[GitHub's run number](https://docs.github.com/en/actions/reference/workflows-and-actions/variables) increases with each new run and stays the same when retrying. The CI package suffix also includes the unique run ID and attempt. Local builds allocate a number from the gitignored `LOCAL-BUILD-NUMBER`, producing file versions such as `3.2.0.N`. Pass `-p:BuildRevision=1` for a specific number. CI leaves the local counter unchanged.

## Seeing failures and test output

Open the workflow run, select **Test**, then expand **Run unit tests**. The detailed console logger shows test names, failure messages, expected/actual values, stack traces, and test output directly in the GitHub log. NUnit standard output is enabled. Nothing redirects this output to a server-side log file.

The **test-results-net10.0** and **test-results-net11.0** artifacts contain TRX reports even when tests fail. Use **Re-run failed jobs** to retry using the existing compiled-tests artifact. Test artifacts are retained for 14 days. After that, rerun all jobs to rebuild. Required branch checks should include the build, both test targets, browser tests, release identity and package validation.

## Adding and running tests

Add fixtures and test cases normally; NUnit discovers them automatically. For a new test project, add it to `src/Sphere10.Framework (CrossPlatform).sln` and add its compiled DLL path to the Test command in the workflow. Individual test methods and fixtures need no YAML changes. Windows-only projects outside that solution retain their existing exclusion. Explicit tests remain opt-in, and existing `GITHUB_ACTIONS` skips remain in effect.

For local development, build and run tests from the repository root:

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

`Directory.Packages.props` contains the current dependency versions, including some prereleases. Review those dependencies before a stable tag. NuGet reports NU5104 when a stable package depends on a prerelease dependency. Continuous development packages are explicitly prerelease versions.
