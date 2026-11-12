# Processes and Threads

Lists, opens, inspects and controls Windows processes and threads, tied to verified process and thread instances, and captures stacks, dumps and managed-runtime diagnostics.

`Tools.Windows.Processes` is the entry point. It lists processes, opens a `WindowsProcess`, captures a `ProcessIdentity`, runs identity-checked commands, sends console signals and window messages, and creates the data source, suspension owner, working-set watch and worker host of this domain. A `ProcessIdentity` pairs a PID with the process start time read through an owned handle, so a reused PID is detected. `WindowsProcess` owns a process handle and reads counters, threads, modules, command line, environment block, token, job membership, mitigation policies, memory regions, memory bytes and strings. `WindowsProcessMemoryStream` is a seekable read-only stream over one address range of another process. `Tools.Windows.Threads` reads and controls one `ThreadIdentity`, including priority, priority boost, suspend and resume counts and the Wait Chain Traversal sample. `ProcessSchedulingService` reads and sets primary-group affinity and default CPU Sets on a creation-time-verified handle.

`ProcessSuspensionService` owns the suspend increments it adds and resumes only those. Register one per application, call `ReleaseAllAsync` during application shutdown, and dispose it. `WindowsWorkingSetWatch` enables kernel working-set monitoring of a process until that process exits, and `ProcessWorkingSetDataSource` polls such a watch into a bounded fault history. The caller must confirm this persistent monitoring before starting it. `WindowsProcessDataSource` is a read-only process inventory that loads once and pages the detached rows. It derives `AsyncListDataSource` from the [core data sources](../../Sphere10.Framework/DataSource/README.md).

Several operations run in an isolated child process of the application. `ProcessSignalService`, `Tools.Windows.Dumps` and `FileHandleService` (in the [Files](../Files/README.md) domain) each start the application again with a worker argument through `WindowsProcessWorkerHost`, so the application calls their `TryRunWorker(args, out exitCode)` methods at the start of `Main` and exits when one returns true. `WindowsProcessWorkerSession` owns such a child with bounded stdout and stderr and asynchronous termination. `Tools.Windows.Symbols` unwinds x64 thread stacks from a PSS snapshot and resolves historical addresses against local images. `Tools.Windows.Managed` reads .NET runtime, thread and optional heap data from a PSS clone or a dump with ClrMD without loading target assemblies. `Tools.Windows.Dumps` writes user-mode minidumps and reads memory retained in them. See [managed diagnostics](../../../docs/usage/managed-diagnostics.md) for the end-to-end workflow.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AttachToCommandPromptScope`](AttachToCommandPromptScope.cs) | struct | Scope that attaches the process to the foreground `cmd` or PowerShell console and frees it on disposal, posting Enter to restore the prompt |
| [`ManagedDiagnosticEntry`](SpecialTypes.cs) | record | One managed diagnostic item with runtime, kind, name, address and value, such as a runtime, app domain, GC segment, module, thread, stack frame or current exception |
| [`ManagedHeapObject`](SpecialTypes.cs) | record | One captured managed heap object with address, size, type name and method table |
| [`ManagedHeapReference`](SpecialTypes.cs) | record | One reference from a source heap object to a target object |
| [`ManagedHeapRoot`](SpecialTypes.cs) | record | One GC root with kind, address, target object and pinned and interior flags |
| [`ManagedHeapType`](SpecialTypes.cs) | record | Per-type object count and total bytes of a heap capture |
| [`ManagedInspectionOptions`](ManagedInspectionOptions.cs) | class | Opt-in heap and stack capture and upper bounds for objects, references, roots, entries and frames, plus local library directories |
| [`ManagedRuntimeInfo`](ManagedRuntimeInfo.cs) | record | Detached result of a managed inspection: entries, heap objects, types, references and roots, truncation, issues and optional dump info |
| [`Tools.Windows.Managed`](ManagedRuntimeUtil.cs) | static class | `Tools.Windows.Managed` read-only ClrMD inspection of a live process through a PSS clone or of a dump file |
| [`ProcessCommandService`](ProcessCommandService.cs) | static class | Runs an action against a `Process` only after rechecking that the PID still has the expected start time |
| [`ProcessIdentity`](ProcessIdentity.cs) | struct | Record struct of a PID and its start time, captured through an owned handle to detect PID reuse |
| [`ProcessSchedulingService`](ProcessSchedulingService.cs) | static class | Reads and sets process affinity masks and default CPU Sets on a verified retained handle |
| [`Tools.Windows.Processes`](ProcessSignaler.cs) | static class | `Tools.Windows.Processes` entry point for process discovery, handles, identities, commands, signals, scheduling and service factories |
| [`ProcessSignalService`](ProcessSignalService.cs) | static class | Sends CTRL+C or CTRL+BREAK to a verified console process from an isolated worker process |
| [`ProcessSuspensionService`](ProcessSuspensionService.cs) | class | Application-owned suspension of verified processes that resumes only its own thread increments and releases them all at shutdown |
| [`ProcessWorkingSetDataSource`](ProcessWorkingSetDataSource.cs) | class | Polling data source that drains a process's working-set watch into a bounded history of `WorkingSetFaultRow` records |
| [`ThreadIdentity`](ThreadIdentity.cs) | struct | Record struct of a thread ID and start time within a `ProcessIdentity` |
| [`Tools.Windows.Threads`](ThreadUtil.cs) | static class | `Tools.Windows.Threads` reads, wait chains, priority, priority boost, suspend and resume of one verified thread |
| [`WindowsCpuSet`](SpecialTypes.cs) | record | One system CPU Set with group, logical processor, core, efficiency class and parked and allocation flags |
| [`WindowsDumpInfo`](SpecialTypes.cs) | record | An offline dump's path, length, write time and retained memory ranges |
| [`WindowsDumpMemoryRange`](SpecialTypes.cs) | record | One virtual-memory range retained in a minidump with its file offset |
| [`Tools.Windows.Dumps`](WindowsDumpUtil.cs) | static class | `Tools.Windows.Dumps` minidump capture in an isolated worker and reads of memory retained in a dump |
| [`WindowsJobAccountingInfo`](WindowsJobAccountingInfo.cs) | record | Job object CPU times, page faults and process counts |
| [`WindowsModuleInfo`](WindowsModuleInfo.cs) | record | One loader-listed module with path, base and size, plus optional PE header details of its mapped image |
| [`WindowsProcess`](WindowsProcess.cs) | class | Disposable owner of a process handle that reads counters, threads, modules, command line, environment, token, job, mitigations, memory and strings |
| [`WindowsProcessCounters`](WindowsProcessCounters.cs) | record | Raw CPU time, IO bytes, memory, page faults, handles, session and priority of one process, with unavailable values as null |
| [`WindowsProcessDataSource`](WindowsProcessDataSource.cs) | class | Read-only process inventory data source with cached searchable, sortable pages |
| [`WindowsProcessEnvironmentVariable`](WindowsProcessEnvironmentVariable.cs) | record | One name and value from another process's environment block |
| [`WindowsProcessInfo`](WindowsProcessInfo.cs) | record | Toolhelp process row: PID, parent PID, executable name and thread count |
| [`WindowsProcessInventoryEntry`](WindowsProcessInventoryEntry.cs) | record | Detached inventory row with executable path, raw counters, capture times and read errors |
| [`WindowsProcessJobInfo`](WindowsProcessJobInfo.cs) | record | Whether a process is in a job, with accounting only when a job handle is available |
| [`WindowsProcessMemoryRead`](WindowsProcessMemoryRead.cs) | record | Result of one `ReadProcessMemory` call with the bytes read and the Win32 error code |
| [`WindowsProcessMemoryRegion`](WindowsProcessMemoryRegion.cs) | record | One `VirtualQueryEx` region with raw state, protection and type flags |
| [`WindowsProcessMemoryStream`](WindowsProcessMemoryStream.cs) | class | Seekable read-only `Stream` over an address range of another process that reads only the requested bytes |
| [`WindowsProcessMetadata`](WindowsProcessMetadata.cs) | record | On-demand architecture, user and integrity SIDs and mitigation policies of a process |
| [`WindowsProcessMitigation`](WindowsProcessMitigation.cs) | record | One mitigation policy's native flags or the error that made it unavailable |
| [`WindowsProcessScheduling`](WindowsProcessScheduling.cs) | record | Affinity masks, default CPU Set IDs and available CPU Sets of a process, with availability text |
| [`WindowsProcessString`](WindowsProcessString.cs) | record | One ASCII or Unicode string found in process memory or its image file, with its address or offset |
| [`WindowsProcessStringScan`](WindowsProcessStringScan.cs) | record | Result of a string scan with bytes examined, skipped regions and truncation |
| [`WindowsProcessSuspension`](SpecialTypes.cs) | record | One suspension owned by `ProcessSuspensionService`, with the suspended thread identities |
| [`WindowsProcessSuspensionRelease`](SpecialTypes.cs) | record | Result of resuming an owned suspension: released count, remaining threads and errors |
| [`WindowsProcessTokenInfo`](WindowsProcessTokenInfo.cs) | record | Process token SIDs, session, elevation, groups, privileges, restriction, AppContainer and virtualization state |
| [`WindowsProcessWorkerHost`](WindowsProcessWorkerHost.cs) | class | Builds argument-safe `ProcessStartInfo` to relaunch the application, or a given host, as an isolated worker |
| [`WindowsProcessWorkerSession`](WindowsProcessWorkerSession.cs) | class | Async-disposable owner of one worker process with bounded line reads, captured stderr and confirmed termination |
| [`WindowsProcessWorkerSessionOptions`](WindowsProcessWorkerSessionOptions.cs) | class | Termination and stderr-drain timeouts, stderr size limit and descendant-kill setting of a worker session |
| [`WindowsResolvedAddresses`](SpecialTypes.cs) | record | Addresses resolved to stack frames against historical modules, with issues for missing or mismatched images |
| [`WindowsStackFrame`](WindowsStackFrame.cs) | record | One native stack frame with pointers, module, symbol, displacement and source line when available |
| [`WindowsSymbolModule`](SpecialTypes.cs) | record | One historical image mapping (base, size, path, timestamp, checksum) used for address resolution |
| [`Tools.Windows.Symbols`](WindowsSymbolUtil.cs) | static class | `Tools.Windows.Symbols` bounded x64 stack capture of a verified thread and offline address resolution |
| [`WindowsThreadInfo`](WindowsThreadInfo.cs) | record | One thread's state, wait reason, priorities, CPU time, cycles, start time and start address |
| [`WindowsThreadStack`](SpecialTypes.cs) | record | A bounded captured stack of one verified thread with truncation flag and issues |
| [`WindowsThreadWaitChain`](WindowsThreadWaitChain.cs) | record | One Wait Chain Traversal sample with nodes, cycle detection and truncation |
| [`WindowsThreadWaitChainNode`](SpecialTypes.cs) | record | One wait-chain node: object type, status, name and thread details |
| [`WindowsTokenGroup`](WindowsTokenGroup.cs) | record | One token group SID with its attribute flags |
| [`WindowsTokenPrivilege`](WindowsTokenPrivilege.cs) | record | One token privilege name, LUID and attribute flags |
| [`WindowsWorkingSetFault`](SpecialTypes.cs) | record | One raw working-set fault record with instruction pointer, faulting address and thread |
| [`WindowsWorkingSetPage`](WindowsWorkingSetPage.cs) | record | `QueryWorkingSetEx` flags for one address with decoded residency, sharing, protection, NUMA and page-size fields |
| [`WindowsWorkingSetSample`](SpecialTypes.cs) | record | One drain of a working-set watch with its faults and lost-record count |
| [`WindowsWorkingSetWatch`](WindowsWorkingSetWatch.cs) | class | Exclusive in-process reader of a process's working-set fault buffer, whose disposal stops only this reader |
| [`WorkingSetFaultRow`](WorkingSetFaultRow.cs) | record | One drained page fault with sequence number and observation time, as served by `ProcessWorkingSetDataSource` |
