# Processes and Threads

Lists, opens, inspects and controls Windows processes and threads, tied to verified process and thread instances, and captures stacks, dumps and managed-runtime diagnostics.

`ProcessSignaler` (`Tools.Windows.Processes`) is the entry point. It lists processes, opens a `WindowsProcess`, captures a `ProcessIdentity`, runs identity-checked commands, sends console signals and window messages, and creates the data source, suspension owner, working-set watch and worker host of this domain. A `ProcessIdentity` pairs a PID with the process start time read through an owned handle, so a reused PID is detected. `WindowsProcess` owns a process handle and reads counters, threads, modules, command line, environment block, token, job membership, mitigation policies, memory regions, memory bytes and strings. `WindowsProcessMemoryStream` is a seekable read-only stream over one address range of another process. `ThreadUtil` (`Tools.Windows.Threads`) reads and controls one `ThreadIdentity`, including priority, priority boost, suspend and resume counts and the Wait Chain Traversal sample. `ProcessSchedulingService` reads and sets primary-group affinity and default CPU Sets on a creation-time-verified handle.

`ProcessSuspensionService` owns the suspend increments it adds and resumes only those. Register one per application, call `ReleaseAllAsync` during application shutdown, and dispose it. `WindowsWorkingSetWatch` enables kernel working-set monitoring of a process until that process exits, and `ProcessWorkingSetDataSource` polls such a watch into a bounded fault history. The caller must confirm this persistent monitoring before starting it. `WindowsProcessDataSource` is a read-only process inventory that loads once and pages the detached rows. It derives `AsyncListDataSource` from the [core data sources](../../Sphere10.Framework/DataSource/README.md).

Several operations run in an isolated child process of the application. `ProcessSignalService`, `WindowsDumpUtil` and `FileHandleService` (in the [Files](../Files/README.md) domain) each start the application again with a worker argument through `WindowsProcessWorkerHost`, so the application calls their `TryRunWorker(args, out exitCode)` methods at the start of `Main` and exits when one returns true. `WindowsProcessWorkerSession` owns such a child with bounded stdout and stderr and asynchronous termination. `WindowsSymbolUtil` (`Tools.Windows.Symbols`) unwinds x64 thread stacks from a PSS snapshot and resolves historical addresses against local images. `ManagedRuntimeUtil` (`Tools.Windows.Managed`) reads .NET runtime, thread and optional heap data from a PSS clone or a dump with ClrMD without loading target assemblies. `WindowsDumpUtil` (`Tools.Windows.Dumps`) writes user-mode minidumps and reads memory retained in them. See managed diagnostics for the end-to-end workflow.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AttachToCommandPromptScope`](AttachToCommandPromptScope.cs) | struct | Scope that attaches the process to the foreground `cmd` or PowerShell console and frees it on disposal, posting Enter to restore the prompt |
| [`ProcessCommandService`](ProcessCommandService.cs) | static class | Runs an action against a `Process` only after rechecking that the PID still has the expected start time |
| [`ProcessIdentity`](ProcessIdentity.cs) | struct | Record struct of a PID and its start time, captured through an owned handle to detect PID reuse |
| [`ProcessSignaler`](ProcessSignaler.cs) | class | `Tools.Windows.Processes` entry point for process discovery, handles, identities, commands, signals, scheduling and service factories |
| [`ProcessSignalService`](ProcessSignalService.cs) | static class | Sends CTRL+C or CTRL+BREAK to a verified console process from an isolated worker process |
| [`ThreadIdentity`](ThreadIdentity.cs) | struct | Record struct of a thread ID and start time within a `ProcessIdentity` |
| [`ThreadUtil`](ThreadUtil.cs) | class | `Tools.Windows.Threads` reads, wait chains, priority, priority boost, suspend and resume of one verified thread |
| [`WindowsJobAccountingInfo`](WindowsJobAccountingInfo.cs) | record | Job object CPU times, page faults and process counts |
| [`WindowsModuleInfo`](WindowsModuleInfo.cs) | record | One loader-listed module with path, base and size, plus optional PE header details of its mapped image |
| [`WindowsProcess`](WindowsProcess.cs) | class | Disposable owner of a process handle that reads counters, threads, modules, command line, environment, token, job, mitigations, memory and strings |
| [`WindowsProcessCounters`](WindowsProcessCounters.cs) | record | Raw CPU time, IO bytes, memory, page faults, handles, session and priority of one process, with unavailable values as null |
| [`WindowsProcessDataSource`](WindowsProcessDataSource.cs) | class | Read-only process inventory data source with cached searchable, sortable pages |
| [`WindowsProcessEnvironmentVariable`](WindowsProcessEnvironmentVariable.cs) | record | One name and value from another process's environment block |
| [`WindowsProcessInfo`](WindowsProcessInfo.cs) | record | Toolhelp process row: PID, parent PID, executable name and thread count |
| [`WindowsProcessInventoryEntry`](WindowsProcessInventoryEntry.cs) | record | Detached inventory row with executable path, raw counters, capture times and read errors |
| [`WindowsProcessJobInfo`](WindowsProcessJobInfo.cs) | record | Whether a process is in a job, with accounting only when a job handle is available |
| [`WindowsProcessString`](WindowsProcessString.cs) | record | One ASCII or Unicode string found in process memory or its image file, with its address or offset |
| [`WindowsProcessStringScan`](WindowsProcessStringScan.cs) | record | Result of a string scan with bytes examined, skipped regions and truncation |
| [`WindowsProcessTokenInfo`](WindowsProcessTokenInfo.cs) | record | Process token SIDs, session, elevation, groups, privileges, restriction, AppContainer and virtualization state |
| [`WindowsProcessWorkerHost`](WindowsProcessWorkerHost.cs) | class | Builds argument-safe `ProcessStartInfo` to relaunch the application, or a given host, as an isolated worker |
| [`WindowsThreadInfo`](WindowsThreadInfo.cs) | record | One thread's state, wait reason, priorities, CPU time, cycles, start time and start address |
| [`WindowsTokenGroup`](WindowsTokenGroup.cs) | record | One token group SID with its attribute flags |
| [`WindowsTokenPrivilege`](WindowsTokenPrivilege.cs) | record | One token privilege name, LUID and attribute flags |
