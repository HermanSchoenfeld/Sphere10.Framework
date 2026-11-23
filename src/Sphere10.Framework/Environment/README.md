# Environment

Runtime and process facilities: external command execution, single-instance enforcement, the machine identifier contract, runtime queries and debugging scratch state.

`ProcessSentry` runs an owned external executable and completes only after the process exits and both redirected streams are read. `RunAsync` returns the exit code, `RunWithErrorCodeCheckAsync` throws `ProcessSentryException` on an unexpected code, and `RunCapturedAsync` passes structured arguments and returns a `ProcessExecutionResult` with the captured output. `BreakRunningProcess()` kills the running process tree, and the run task still owns completion and cleanup. `SingleApplicationInstanceScope` holds a named mutex for the entry assembly and either exits the process or throws `ApplicationAlreadyRunningException` when another instance holds it.

`IMachineIdentifierGenerator` is the contract for a stable identifier of the machine the process runs on, returned by `Generate()` as upper-case hexadecimal. Core has no implementation and registers none, so consumers take it from the service provider and handle its absence. `WindowsMachineIdentifierGenerator` (selected hardware sources) and `BIOSMachineIdentifier` (the SMBIOS system UUID) in `Sphere10.Framework.Windows` implement it from values read through WMI.

The running-process data source `ProcessDataSource` and the `KillTree` process extensions are in [Sphere10.Framework.Application](../../Sphere10.Framework.Application/Environment/README.md). The static families `Tools.Runtime`, `Tools.CodeAccessSecurity` and `Tools.Debugger` live in the `Tools` namespace. `Tools.Runtime.IsWebApp` currently always returns `false`.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ApplicationAlreadyRunningException`](ApplicationAlreadyRunningException.cs) | class | Thrown by `SingleApplicationInstanceScope` when another instance of the application is running. |
| [`IMachineIdentifierGenerator`](IMachineIdentifierGenerator.cs) | interface | Computes the machine's identifier as upper-case hexadecimal, the same on every call for an unchanged machine; platform libraries implement it. |
| [`ProcessExecutionResult`](ProcessExecutionResult.cs) | record | Exit code, standard output and standard error collected after an external process and both redirected streams complete. |
| [`ProcessSentry`](ProcessSentry.cs) | class | Runs owned external commands, completing only after process exit and output collection. |
| [`ProcessSentryException`](ProcessSentryException.cs) | class | Thrown when an external command exits with an unexpected code, carrying the file name, code and output. |
| [`SingleApplicationInstanceScope`](SingleApplicationInstanceScope.cs) | struct | Scope that holds a per-application mutex and exits or throws when another instance already holds it. |
| [`Tools.CodeAccessSecurity`](CodeAccessSecurityTool.cs) | tools | Reports whether the current application domain is fully trusted. |
| [`Tools.Debugger`](Debugger.cs) | tools | Global break conditions, counters, objects and a timestamped message list for ad hoc debugging. |
| [`Tools.Runtime`](RuntimeTool.cs) | tools | Runtime queries (WebAssembly, debug or release build, design mode, exception context, entry assembly) and command-line encoding and parsing. |
