<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 🪟 Sphere10.Framework.Windows

**Windows platform integration layer** providing secure, type-safe access to Windows-specific APIs, security features, registry management, and system integration.

Sphere10.Framework.Windows enables **secure Windows system programming** through abstractions over Windows APIs, handling NT security objects, Windows services, event logging, and registry access while maintaining safety and compatibility.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Windows
```

## ⚡ 10-Second Example

```csharp
using Sphere10.Framework.Windows.Security;

// Get current machine information
var host = NTHost.CurrentMachine;
Console.WriteLine($"Machine: {host.Name}");
Console.WriteLine($"SID: {host.SID}");

// Get local administrator user
var admin = host.GetUserByName("Administrator");
Console.WriteLine($"Admin: {admin.Name}");
Console.WriteLine($"Admin SID: {admin.SID}");

// Get group membership
var adminGroup = host.GetGroupByName("Administrators");
var isMember = adminGroup.Members.Contains(admin);
Console.WriteLine($"Is member of Administrators: {isMember}");
```

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Windows` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|
| [Applications](Applications/README.md) | Reads, removes and launches entries from the Windows uninstall registry without deciding which applications to show or run. | [source](Applications/) |
| [Audio](Audio/README.md) | Plays WAV sound data in memory through the Windows `PlaySound` API. | [source](Audio/) |
| [BITS](BITS/README.md) | Creates, enumerates and monitors Background Intelligent Transfer Service (BITS) download and upload jobs through its COM interfaces. | [source](BITS/) |
| [Display](Display/README.md) | Reads the active display paths, modes, timings and EDID data, and provides a frame clock paced by the desktop compositor. | [source](Display/) |
| [Drivers](Drivers/README.md) | Lists installed driver packages, resolves driver file paths and opens kernel device handles for control codes. | [source](Drivers/) |
| [Environment](Environment/README.md) | Reads, validates and changes process, user and machine environment variables, and serves them as an editable data source. | [source](Environment/) |
| [EventLogs](EventLogs/README.md) | Reads and watches Windows Event Log records as detached entries, and writes Framework log output to an event log. | [source](EventLogs/) |
| [Files](Files/README.md) | Provides Windows file operations beyond the BCL: alternate data streams, stream-preserving copies, open-handle discovery, PE and managed assembly inspection, signature checks and long Unicode paths. | [source](Files/) |
| [Hooks](Hooks/README.md) | Installs low-level Windows mouse and keyboard hooks, simulates input and tracks the foreground application. | [source](Hooks/) |
| [Magnification](Magnification/README.md) | Applies colour transformations to the desktop or to single displays through the Windows Magnification API. | [source](Magnification/) |
| [Misc](Misc/README.md) | Holds general Win32 helpers that have no better domain: message-parameter parsing, rich-edit and layered-window calls, an HGlobal scope and the Windows exception type. | [source](Misc/) |
| [Networking](Networking/README.md) | Reads TCP and UDP endpoint tables, network interfaces and interface counters, closes verified TCP connections and drives the in-box `pktmon.exe` packet monitor. | [source](Networking/) |
| [Objects](Objects/README.md) | Reads NT object-manager directories and symbolic links and decodes object access masks, without changing or following any object. | [source](Objects/) |
| [Packages](Packages/README.md) | Lists and removes the current user's MSIX and AppX packages through the in-box Windows PowerShell Appx module. | [source](Packages/) |
| [Pipes](Pipes/README.md) | Lists local named pipes and reports the process at the other end of a connected pipe stream. | [source](Pipes/) |
| [Processes](Processes/README.md) | Lists, opens, inspects and controls Windows processes and threads, tied to verified process and thread instances, and captures stacks, dumps and managed-runtime diagnostics. | [source](Processes/) |
| [Registry](Registry/README.md) | Reads and edits the Windows registry by exact hive, view and key path, and captures bounded key trees with their raw values. | [source](Registry/) |
| [Scopes](Scopes/README.md) | Adds Windows scope factories to the Framework `Tools.Scope` entry point and provides the processor-group affinity scope. | [source](Scopes/) |
| [Security](Security/README.md) | Manages local users, groups and shares through the NetAPI, adjusts token privileges, impersonates accounts and reads or translates security descriptors. | [source](Security/) |
| [Services](Services/README.md) | Queries, configures and controls Windows services and drivers through the Service Control Manager, and wraps `sc.exe` and NSSM for installation. | [source](Services/) |
| [Sessions](Sessions/README.md) | Lists interactive and Remote Desktop sessions on a local or remote server and logs off, disconnects, messages or shadows a verified session. | [source](Sessions/) |
| [Shell](Shell/README.md) | Wraps Windows Shell operations: launching and opening items, file associations, known folders, the Recycle Bin, Shell namespace items and verbs, shortcuts and file icons. | [source](Shell/) |
| [Startup](Startup/README.md) | Reads where Windows starts programs at logon or boot (Run keys, startup folders, StartupApproved data, logon registry entries, scheduled tasks and WMI subscriptions) without changing them. | [source](Startup/) |
| [Storage](Storage/README.md) | Lists logical drives, physical disks and partitions, relabels volumes and runs a read-only `chkdsk` scan. | [source](Storage/) |
| [System](System/README.md) | Reads computer identity, firmware, hardware, operating-system and performance data, reads power plans and capabilities, and requests shutdown, restart, logoff, lock, sleep and hibernate. | [source](System/) |
| [ThirdParty](ThirdParty/README.md) | Holds the upstream license and notice files of the managed-analysis packages this library references, and the manifest that maps them to package versions. | [source](ThirdParty/) |
| [Tracing](Tracing/README.md) | Lists Event Tracing for Windows (ETW) sessions, starts and controls caller-owned real-time sessions, consumes their events as detached records and decodes kernel CPU-sampling events. | [source](Tracing/) |
| [WinAPI](WinAPI/README.md) | Declares the Win32 and native Windows functions, structures, constants and COM interfaces that the rest of the library calls, grouped by DLL. | [source](WinAPI/) |
| [Windowing](Windowing/README.md) | Read-only queries over other processes' native windows, verified window messaging, isolated UI Automation capture and thread DPI awareness scopes. | [source](Windowing/) |
| [buildTransitive](buildTransitive/README.md) | An MSBuild targets file, shipped in the NuGet package, that copies this package's third-party licence notices into every consuming application's build and publish output. | [source](buildTransitive/) |

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`Tools.Windows`](WindowsTool.cs) | tools | Static partial facade in the `Tools` namespace. Each domain utility is a nested static class declared in its own folder, such as `Files`, `Processes`, `Services`, `Shell` and `TraceSessions`. `Assemblies`, `Registry` and `UiAutomation` own state and are per-thread instances returned by properties |
<!-- domains:end -->

## 🏗️ Core Concepts

**NT Security Objects**: Windows NT domain and local security principals (users, groups, computers).

**Security Identifiers (SIDs)**: Unique Windows identifiers for security objects.

**Local vs Remote**: Support for local machine and remote machine security operations.

**Dangling Objects**: Handle security references that couldn't be resolved to SIDs (deleted accounts).

**Privilege Management**: Grant and revoke user privileges at the system level.

**Access Control**: Windows security descriptors and ACLs.

## Windows resource abstractions

Read-only [managed assembly/runtime analysis, native stacks and dump capture](../../docs/usage/managed-diagnostics.md)
share verified process/thread identities, bounded detached data, local diagnostics
libraries and owned PSS/DbgHelp resources. Engine provenance is recorded in
[third-party notices](THIRD-PARTY-NOTICES.md).

Read-only [power plan, setting and sleep inventories](System/Power/README.md)
expose native GUIDs, AC/DC values, policy provenance and paged snapshot sources.
They are independent of the explicit power-transition commands.

### Pageable native inventories

Concrete `IDataSource<T>` backends are provided by `WindowsProcessDataSource`
(inventory, identity, raw counters and executable path), `WindowsServiceDataSource`
(services or drivers), `WindowsNetworkEndpointDataSource` (one TCP/UDP IPv4/IPv6
owner table, optionally restricted to an owning PID),
`WindowsNetworkInterfaceDataSource`, `WindowsDriveDataSource`,
`WindowsPhysicalDiskDataSource` and `WindowsPartitionDataSource`.

These sources derive Core's `AsyncListDataSource<T>` and implement
`FetchAsync`. The first `ReadRangeAsync`, `CountAsync` or `LoadAsync` (also
`Tools.DataSources.ReadAllAsync`) loads the inventory on a worker; subsequent searches,
sorts and pages reuse it. A consumer that needs a fresh enumeration composes
`new ActionRefreshableDataSource<T>(() => new ThatDataSource())` and refreshes it: the fresh
inventory loads while the served one stays readable, and a failed or cancelled refresh
leaves the served inventory in place. Synchronous
access can initialize synchronously; UI consumers must use the asynchronous APIs.
Each range reports its filtered total and clamped page. Native enumeration does
not repeat when navigating pages.

These sources have no timers, rate calculations, charts or management mutations.
The consumer owns refresh cadence and sampling history. Endpoint PID filtering
does not verify process creation time; a consumer requiring an exact process
instance must retain its `ProcessIdentity` checks around capture.

`WindowsProcess.ReadMetadataAsync` explicitly reads architecture, token account
and integrity SIDs, and individually available mitigation-policy flags. It is
separate from inventory counter polling. `ProcessSchedulingService` reads and
sets primary-group affinity or process-default CPU Sets against a retained,
creation-time-verified handle. CPU Set records include group, logical CPU,
efficiency and reservation flags; mutations validate current availability before
dispatch. Empty CPU Set IDs clear defaults without changing explicit per-thread
CPU Set assignments. Consumers own confirmation and explain the hard-affinity /
soft-CPU-Set distinction and multi-group limits.

`ProcessSuspensionService` owns one native suspension increment for every
retained thread handle it successfully suspends. Repeated acquisition for an
already owned process is refused. Failed admission rolls back only acquired
increments; failed releases remain retryable. `ResumeAsync` never drains another
tool's suspension counts. Register the owner at application scope and run
`ReleaseAllAsync` as an application finalization task: it stops new acquisitions,
drains pending commands and reports incomplete releases. Screen removal does not
own this cleanup. `DisposeAsync` repeats finalization and reports remaining errors.

Thread reads expose cumulative CPU time, raw cycles and the optional Win32 start
routine. The native start query is resolved dynamically and unsupported/access
denied fields remain unavailable. `Tools.Windows.Threads.ReadWaitChainAsync`
returns a detached, bounded WCT sample with node statuses, cycle detection and
truncation. Callers own delta baselines and presentation; cycle counts must not
be converted to elapsed time. These APIs run native work on workers, retain exact
process/thread creation identities and never promote their results to a claim
that unsupported synchronization objects or architectures were inspected.

Native contracts: [QueryThreadCycleTime](https://learn.microsoft.com/en-us/windows/win32/api/realtimeapiset/nf-realtimeapiset-querythreadcycletime),
[NtQueryInformationThread](https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntqueryinformationthread),
[GetThreadWaitChain](https://learn.microsoft.com/en-us/windows/win32/api/wct/nf-wct-getthreadwaitchain),
[CPU Sets](https://learn.microsoft.com/en-us/windows/win32/procthread/cpu-sets) and
[GetProcessMitigationPolicy](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getprocessmitigationpolicy).

Windows integration has three layers: DLL-level WinAPI facades, typed Windows
resource APIs, and the application's collectors, workflows and presentation.
The [ownership practice](../../docs/guidelines/patterns/framework-reuse.md#windows-abstraction-layers)
defines the boundary. Extend the existing domain utility in its owning class/file.

`Tools.Windows.Shell.GetDefaultKnownFolderPath` resolves the default known-folder
path without creating it. `SetKnownFolderPath` redirects an existing absolute
directory for the current user; optional `expectedPath` rejects a stale current
mapping before mutation. It changes the Windows mapping and does not move or
delete files. Async variants use the owned Shell STA worker. Once a set operation
has dispatched, cancellation does not hide its completed result. Consumers own
folder eligibility, destination creation, confirmation and saved bookmarks. See
the native [SHSetKnownFolderPath contract](https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shsetknownfolderpath).

Windows utilities are static classes nested in the partial `Tools.Windows`
facade, each declared in its domain folder. [WindowsTool.cs](WindowsTool.cs)
holds the three utilities that own state (`Assemblies`, `Registry` and
`UiAutomation`) as per-thread instances. Use
Tools.Windows.Registry, Services, Processes, Threads, PerformanceCounters, Network, Drives, Startup, EventLogs,
Environment, SecurityDescriptors, Pipes, TraceSessions, Sessions and Shell for their domains. Implementations live in
domain folders; Misc holds general helpers without a better domain. New utility
names use Util rather than Api. See the
[facade/folder practice](../../docs/guidelines/patterns/framework-utilities.md#windows-utility-facade-and-source-folders).

APIs return detached OS data, native error codes or exceptions and own their
buffers/handles unless explicitly transferred. Plain OS data has no grid labels,
property-editor metadata, navigation parents or app serialization policy.
Diagnostic snapshots, sampling/deltas, plans, confirmations and reports stay in
the application, which composes these APIs.

| Windows resource | API and responsibility |
|---|---|
| Processes | [Tools.Windows.Processes](Processes/ProcessSignaler.cs) lists raw process information. [WindowsProcess](Processes/WindowsProcess.cs) owns an opened handle and queries identity/path/counters/command line. [ProcessIdentity](Processes/ProcessIdentity.cs) verifies PID plus creation time; [ProcessCommandService](Processes/ProcessCommandService.cs) retains identity through dispatch. The caller chooses permitted actions. |
| Threads | [Tools.Windows.Threads](Processes/ThreadUtil.cs) reads an exact [ThreadIdentity](Processes/ThreadIdentity.cs), changes relative priority or dynamic priority boost, and suspends/resumes once. Both process and thread handles remain owned through dispatch; process creation, thread creation, ownership and native liveness are verified. Read returns native priority/boost/timing with nullable scheduler fields. [SuspendThread](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-suspendthread) increments the count; [ResumeThread](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-resumethread) decrements once and returns the previous count, so a previous count greater than one leaves the thread suspended. Async cancellation applies before dispatch and does not hide an accepted mutation. Confirmation, permitted targets and suspension ownership belong to the caller; disposing a screen never implicitly resumes a thread. |
| System counters | [WindowsSystemInformation](System/WindowsSystemInformation.cs) returns raw processor/memory/system counts without rates or sampling history. Applications choose aggregates using Core's Statistics. |
| Performance counters | [Tools.Windows.PerformanceCounters](System/PerformanceCounterUtil.cs) opens caller-owned [WindowsPerformanceQuery](System/WindowsPerformanceQuery.cs) resources for English PDH paths, including wildcard GPU counters. Raw samples retain supplied path, provider instance, type/status and unmodified raw fields. The query collection timestamp is converted from PDH local wall time to UTC; invalid or ambiguous daylight-saving timestamps remain unavailable. Missing providers remain nonvalid status rows. Consumers choose when to read and which sample pairs to pass to native CalculateValue; GPU identity parsing, PID verification, sampling, rates, aggregation and history remain in the app. |
| Process inspection | [WindowsProcess](Processes/WindowsProcess.cs) additionally reads bounded remote environment, token SID/group/privilege/elevation fields, job membership and bounded image-file/memory strings. Memory access opens a separately verified read-only handle; it never injects or changes a target. Remote environment uses version-sensitive NT PEB/process-parameter layouts, validates normalization/length and rechecks its pointer; inaccessible/unsupported targets fail explicitly. A 32-bit inspector rejects native 64-bit targets. Job accounting is available only for the caller's own job; another process's membership does not imply access to its job handle. Strings retain address/file offset and encoding, cap each run at 4,096 characters and disclose truncation/skipped regions. |
| Power actions | [Tools.Windows.Computer](System/ComputerUtil.cs) queries native sleep/hibernate/shutdown capabilities and exposes explicit Lock, Logoff, Sleep, Hibernate, Shutdown and Restart operations with async adapters. Shutdown/restart require a caller-supplied nonzero Windows reason. Shutdown privilege is scoped to the executing thread; the library does not elevate or confirm. Request acceptance does not guarantee the later OS transition, and cancellation cannot undo an accepted request. |
| Services/drivers | [Tools.Windows.Services](Services/ServiceUtil.cs) reads native configuration/status/dependencies and performs individual start/stop/pause/resume/start-mode operations. [WindowsServiceInfo](Services/WindowsServiceInfo.cs) retains nullable fields and native error codes. ReadAdvancedConfigurationAsync returns detached [optional SCM configuration](Services/WindowsServiceAdvancedConfiguration.cs): recovery actions, delayed start, privileges, SID type, preshutdown timeout, launch protection and trigger data. Each unavailable information level retains its native error; the query never changes service state. ResolveExecutablePath reuses the Core command parser and requires an explicit absolute executable without PATH search or command execution. [DriverManager](Drivers/DriverManager.cs) reads raw driver packages; plans stay in the app. |
| Registry | [Tools.Windows.Registry](Registry/RegistryUtil.cs) reads/changes typed keys and values through plain [RegistryAddress](Registry/RegistryAddress.cs), [RegistryValue](Registry/RegistryValue.cs) and trees. Expected raw values reject stale edits. OpenKey transfers a caller-disposed handle. Explorer navigation, editor parsing and .reg export stay in the app. |
| Environment | [Tools.Windows.Environment](Environment/EnvironmentUtil.cs) reads/sets/deletes Process/User/Machine variables, verifies expected raw values, preserves persistent registry kinds and broadcasts changes. Scope warnings/confirmation belong to the caller. |
| Desktop applications | [WindowsUninstallRegistration](Applications/WindowsUninstallRegistration.cs) lists/reads/removes uninstall registrations and launches a caller-selected absolute executable. Parsing/inventory/uninstall policy stay in the app. A returned Process is caller-owned; this is not MSIX/Store management. |
| MSIX / AppX packages | [AppxPackageSentry](Packages/AppxPackageSentry.cs) queries all current-user package types through the in-box Windows PowerShell Appx module and removes an exact full package identity for that account. [WindowsAppPackageInfo](Packages/WindowsAppPackageInfo.cs) contains detached OS identity, localized metadata when available and nullable removability. App merging, dependency-removal policy and confirmations stay in the caller. |
| Network | [Tools.Windows.Network](Networking/WindowsNetworkUtil.cs) reads IPv4/IPv6 TCP/UDP owner tables, interface information and native counters. Plain types retain native identities, unsigned totals, speeds and flags. |
| Storage | [Tools.Windows.Drives](Storage/WindowsDriveUtil.cs) reads logical drives/physical disks/partitions and changes labels. [WindowsVolume](Files/WindowsVolume.cs) exposes native volume queries. [VolumeCheckSentry](Storage/VolumeCheckSentry.cs) wraps read-only chkdsk.exe for a selected local root with owned cancellation and structured arguments. Callers select probing, provider deadlines, confirmation and reporting; per-row errors remain alongside successful fields. |
| Startup/events | [Tools.Windows.Startup](Startup/WindowsStartupUtil.cs) reads Run locations, raw approval bytes and startup files. [Tools.Windows.EventLogs](EventLogs/WindowsEventLogUtil.cs) owns readers/records and returns detached event data with caller-supplied bounds. Interpretation/display policy stays in the app. |
| Windows/messages | [WindowsWindow](Windowing/WindowsWindow.cs) reads HWND/title/class/thread/process/visibility/minimized data plus distinct parent/owner HWNDs, style masks, message-only membership and target-awareness DPI. EnumerateAsync accepts [WindowsWindowEnumerationScope](Windowing/WindowsWindowEnumerationScope.cs) for top-level, descendant and message-only families; the existing overload still selects only top-level windows. EnumChildWindows handles desktop descendants, while FindWindowExW with HWND_MESSAGE handles message-only windows independently of visibility. Optional process-detail omission lets callers join their existing process inventory. GetBounds verifies the captured process creation time and checks the window thread/class before and after reading [GetWindowRect](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowrect) screen coordinates; those coordinates follow the caller's DPI context and may include invisible resize borders. GetPhysicalBounds uses a synchronous ThreadDpiAwarenessScope to return the same verified outer rectangle in physical desktop pixels, independent of the caller context; it preserves the process DPI policy. GetVisiblePhysicalBounds applies the same identity checks and returns [DWM extended frame bounds](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute), excluding invisible resize margins, with a physical outer-bounds fallback when DWM has no usable frame. [WindowMessageSender](Windowing/WindowMessageSender.cs) posts or sends with timeout, retaining process identity through main-window dispatch. The caller supplies editor/highlight policy and confirmation. |
| Console/pipes | [ProcessSignalService](Processes/ProcessSignalService.cs) uses an owned console helper. [Tools.Windows.Pipes](Pipes/NamedPipeUtil.cs) lists local pipe names without opening endpoints; [NamedPipePeer](Pipes/NamedPipePeer.cs) queries connected peer identity. Client capture, elevated restart and Task Manager replacement remain application workflows. |
| Trace sessions | [Tools.Windows.TraceSessions](Tracing/EtwTraceUtil.cs) queries accessible native ETW sessions, enables real-time delivery on an exactly identified existing session and consumes detached [EtwTraceEventRecord](Tracing/EtwTraceEventRecord.cs) records. The reader owns OpenTrace/ProcessTrace/CloseTrace and generic TDH field decoding; provider interpretation, packet attribution, bounded storage and capture/recovery policy stay in the app. |
| Logged-on sessions | [Tools.Windows.Sessions](Sessions/SessionUtil.cs) queries local or named-server WTS sessions through plain [WindowsSessionInfo](Sessions/WindowsSessionInfo.cs). Message, disconnect, logoff and remote-control operations revalidate session ID/server/user/logon time. Remote control requires an existing RDP caller session and uses Windows permissions/consent. Native memory/server handles are scope-owned; tool layout and confirmation stay in the app. |
| Shell | [Tools.Windows.Shell](Shell/ShellUtility.cs) launches through the Windows shell, resolves known folders, opens native property sheets, queries/empties the Recycle Bin, and reads bounded real items in shell namespaces. [WindowsShellItemInfo](Shell/WindowsShellItemInfo.cs) contains detached native fields, including available recycle origin/deletion metadata; native date conventions are retained. Queries use an owned STA worker; synchronous namespace/verb operations require STA. COM references are scoped and never returned to consumers. Item commands revalidate the selected native identity and use the exact currently offered, localized [FolderItemVerb](https://learn.microsoft.com/en-us/windows/win32/shell/folderitemverb-doit); an accepted request does not imply asynchronous completion. Returned process observation handles are caller-owned. UI operations retain the calling STA/owner HWND where the native API supports it; inventory choices, UAC policy and confirmation text belong to the app. See Microsoft's [ShellExecuteEx](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shellexecuteexw) and [Recycle Bin query](https://learn.microsoft.com/en-us/windows/win32/api/shellapi-shqueryrecyclebinw) contracts. |
| File associations | [Tools.Windows.Shell](Shell/ShellUtility.cs) queries effective extension associations or an explicit fixed ProgID through [AssocQueryStringW](https://learn.microsoft.com/en-us/windows/win32/api/shlwapi/nf-shlwapi-assocquerystringw). [ShellFileAssociationInfo](Shell/ShellFileAssociationInfo.cs) retains resolved native strings and a per-field HRESULT; missing executable/command fields remain empty, including COM or packaged-app handlers. Reads avoid executable verification and registry fixups. NotifyFileAssociationsChanged sends the native [association-change notification](https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shchangenotify) after caller-owned registration edits; it does not change defaults or wait for every Shell client. Inventory, registry editing, confirmation and Windows Default Apps navigation stay in the app. |
| File inspection | [FileHandleService](Files/FileHandleService.cs) reads handles/Restart Manager users with bounded owned helpers. [WindowsFileInspectionService](Files/WindowsFileInspectionService.cs) reads version/hash/signature data. Partial captures remain explicit; an open handle is not proof of an exclusive lock. Source handles/processes are never closed. |
| File transfer | [WindowsFile](Files/WindowsFile.cs) copies one file and streams with cancellation, byte progress and worker checkpoints. Tree migration, retries, verification, cleanup policy and results stay in the application workflow. |
| Icons | [FileIconCache](Shell/FileIconCache.cs) owns worker-loaded local EXE/DLL/ICO resources and PNG/BMP/JPEG/GIF logo images with configurable size/capacity/budget and lends GetImage results. The Drawing resize helpers fit raster logos into a centered, transparent square without distorting their aspect ratio; pixels are detached from their source files. App process/application adapters stay in the app. Dispose the cache after cells stop borrowing images. |

Existing helpers may retain the root Sphere10.Framework.Windows namespace;
networking, storage, files, shell and event-log APIs use domain namespaces.
The assembly depends on Core, Drawing and Windows providers, without SystemExpert,
Application, Data or Windows Forms dependencies.

### Editable inventories and owned captures

`EnvironmentVariableDataSourceBase` (`Sphere10.Framework.Windows`), `LocalUserDataSourceBase` and `LocalGroupDataSourceBase` (`Sphere10.Framework.Windows.Security`, over `LocalAccountsService` and `ILocalAccountsBackend`) and `AlternateDataStreamDataSource` (`Sphere10.Framework.Windows.Files`) are `PollingDataSource` templates over a nested `RefreshableDataSourceBase` that creates a fresh per-capture `Inventory`: a refresh swaps in a fresh inventory, and the inventory's asynchronous range operations validate, ask the owner's `ConfirmAsync` hook, perform the native write, update the served rows and report to the owner's `Logger`. Clients that do not derive use `ActionEnvironmentVariableDataSource`, `ActionLocalUserDataSource` and `ActionLocalGroupDataSource`. Their edit rows (`EnvironmentVariableEntry`, `LocalUserEntry`, `LocalGroupEntry`, `AlternateDataStreamEntry`) carry an edit baseline that is public for editors but `[Browsable(false), JsonIgnore]` for grids and exports.

`ProcessNetworkEndpointDataSource` loads one verified process's TCP/UDP endpoint tables as `NetworkEndpointInfo` rows. `new ProcessWorkingSetDataSource(process, maximumRecords).StartConfirmedAsync(lifetime, token)` enables the kernel working-set watch of a confirmed process (through the overridable `StartWatchAsync`/`ReadWatchAsync` hooks) and polls its drained `WorkingSetFaultRow` records; the caller may pass the application's `DataSourceLifetime` so shutdown joins an in-flight read.

### Asynchronous Shell And Session Operations

Use `Tools.Windows.Shell`'s `LaunchAsync`, `OpenShellItemAsync`, `OpenDirectoryAsync`,
`HighlightFileInExplorerAsync`, `OpenSystemToolAsync`, `OpenUriAsync`,
`ShowPropertiesAsync`, `EmptyRecycleBinAsync` and `InvokeShellItemVerbAsync`
from a UI caller. Each dispatches its complete native operation, including path
validation, to a dedicated COM-initialized STA. Capture owner HWNDs and control
values before the call; the worker never reads controls. Synchronous methods
remain available for callers that already provide the required apartment.

Shell action cancellation is checked before admission. Once dispatched, an
action returns its actual result or error even if cancellation arrives, so a
launched process observation cannot be lost and a deletion cannot be mistaken
for a canceled read. `LaunchAsync` transfers its returned observation to the
caller for disposal; disposing it does not terminate the launched program.
Windows still owns shell dialogs, UAC decisions and asynchronous operation
completion. Query cancellation retains the existing read-only contract.

The task completes when the native request returns. If a modeless property sheet
remains on that worker, the worker retains its COM apartment and pumps that
thread's native messages until its visible top-level windows close. Hidden COM
infrastructure does not keep it alive. The pump blocks for messages rather than
polling or spinning; it does not close windows because a caller canceled.
Native file execution uses `SEE_MASK_NOASYNC` before the short-lived thread can
exit, as described by Microsoft's
[Shell execution flags](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-shellexecuteinfow)
and [COM initialization contract](https://learn.microsoft.com/en-us/windows/win32/api/combaseapi/nf-combaseapi-coinitializeex).

`Tools.Windows.Sessions.GetCurrentSessionIdAsync` and `IsCurrentSessionRemoteAsync` include
the current-process/session queries in their worker boundary. A UI should retain
the completed caller identity alongside its roster rather than querying the
synchronous `CurrentSessionId` property whenever row selection or command
eligibility changes. System Expert's Logged On Sessions screen demonstrates
that captured-identity behavior.

ServiceControlManagerSentry is the separate ProcessSentry wrapper for sc.exe
Create/Delete commands; NssmSentry wraps nssm.exe. They do not implement the native
SCM utility. Read [the Sentry practice](../../docs/guidelines/patterns/process-sentries.md)
for the repaired shared command runner's completion, transcript and cancellation
contracts. [PktMonSentry](Networking/PktMonSentry.cs) wraps in-box pktmon.exe with
structured arguments, full-packet circular captures and ETL text/PCAPNG conversion.
It does not change filters or decide which global capture the app may stop.
Applications compose native ETW session/reader operations with the executable boundary and disclose
the built-in controller's global state and concurrent-controller limits.

Read-only discovery does not start a capture:

```csharp
var sessions = await Tools.Windows.TraceSessions.GetSessionsAsync(cancellationToken);
var pipeNames = await Tools.Windows.Pipes.GetNamesAsync(cancellationToken);
var help = await new PktMonSentry().ReadHelpAsync(cancellationToken);
```

`AppxPackageSentry.GetPackagesAsync` uses Windows PowerShell without profiles or
interactive input and explicitly includes Main, Framework, Resource, Bundle,
Xap and Optional package types. It returns packages registered for the current
account, including Store and sideloaded packages; it does not enumerate other
users or packages merely provisioned in the Windows image. Package identities
are full names, rather than display names or wildcard matches. A source failure
throws instead of reporting an empty successful inventory. Missing localized
metadata retains the package identity and `MetadataError`; older Windows builds
may not expose another package's display name/logo. See Microsoft's
[Get-AppxPackage](https://learn.microsoft.com/en-us/powershell/module/appx/get-appxpackage)
and [Package.DisplayName](https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.package.displayname)
contracts.

`RemovePackageAsync(fullName)` re-queries an exact current-user identity, checks
the native non-removable flag, waits for the removal command and verifies that
registration is absent. The caller owns confirmation and any policy restricting
shared framework/resource removal. This operation does not remove another user's
registration or future-user image provisioning. Inventory commands have a
one-minute deadline; removal commands have five minutes. Cancellation/deadline
termination drains the owned helper but cannot roll back a deployment already
committed by Windows. Re-query after interrupted/failed removal rather than
retrying automatically. Windows permissions and dependency restrictions remain
in force; see [Remove-AppxPackage](https://learn.microsoft.com/en-us/powershell/module/appx/remove-appxpackage).

Pipe names can include relative namespaces such as `LOCAL\name`; passive discovery
preserves all raw names, and GetPath preserves valid relative namespace segments.
Explicit path construction rejects absolute/device/drive paths, empty/traversal
segments and NUL rather than rejecting all backslashes. The caller owns connection
eligibility and protocol/client behavior. See Microsoft's
[named-pipe namespace example](https://learn.microsoft.com/en-us/windows/apps/develop/communication/interprocess-communication#pipes).

CancellationToken belongs to Task/ValueTask operation methods. Synchronous
resource methods have no token parameter; see the
[cancellation contract](../../docs/guidelines/patterns/sync-async.md#cancellation-belongs-to-asynchronous-operations).

### Network interface counters

Tools.Windows.Network.GetInterfaceCounters(Guid) resolves the current LUID from the
interface GUID and reads native 64-bit octet counters independently of IPv4.
Hardware/connector/NDIS filter flags remain OS data. The app correlates readings,
selects monotonic intervals and computes rates. First/failed/reset readings cannot
establish a rate; valid idle deltas can be zero. Loopback transfers may not be
counted; do not manufacture traffic. Native contracts:
[GUID-to-LUID](https://learn.microsoft.com/en-us/windows/win32/api/netioapi/nf-netioapi-convertinterfaceguidtoluid),
[GetIfEntry2](https://learn.microsoft.com/en-us/windows/win32/api/netioapi/nf-netioapi-getifentry2)
and [WinAPI.IPHLPAPI](WinAPI/IPHLPAPI.cs).

### Real-time ETW records

SetRealTimeAsync(expected, true, token) updates an existing session only after
matching its name, GUID, native session ID and logfile path. Adding real-time
delivery retains the existing file mode. ConsumeRealTimeAsync(expected, receive,
token, started) verifies that same identity before opening the reader. The
optional started callback runs after OpenTrace succeeds; cancellation closes and
drains only the owned reader, without stopping the producing session.

Records detach provider/event identity, UTC timestamp, native process/thread IDs,
raw user bytes and available TDH properties from the callback's native buffers.
Unsupported schemas retain raw bytes rather than inventing decoded fields. A
provider's event-header PID is not automatically the owner of data described by
that event. Consumers choose interpretation, bounded queuing, logging and drop
reporting; no application performance collector is included. A slow callback can
lose events, so copy/enqueue promptly and process data elsewhere. Native contracts:
[ControlTrace UPDATE](https://learn.microsoft.com/en-us/windows/win32/api/evntrace/nf-evntrace-controltracew)
and [real-time consumption](https://learn.microsoft.com/en-us/windows/win32/etw/consuming-events).

### Optional owned CPU profiling

`Tools.Windows.TraceSessions.StartSystemRealTimeSessionAsync` creates an additional uniquely
named/GUID multiplexed system logger. The ordinary manifest-session API retains
its original admission/mode. Reserved global logger names and SystemTraceControlGuid
are rejected; an existing session is never adopted or replaced. The caller owns
the new session and uses the same exact-identity stop/consumer contracts.

The System Profile Provider (`bfeb0324-1cee-496f-a409-2ac2b48a6322`, General
keyword `1`) requires system-logger mode and appropriate Windows profiling
rights. `SetStackTracingAsync` replaces only the selected owned system logger's
classic stack-event list. It changes neither a global profiling interval nor
registry memory policy. Admission/stack errors must remain independent coverage
results; successful ordinary capture does not establish profiling permission.

`EtwCpuProfileDecoder` reads the documented PerfInfo SampledProfile type 46 and
StackWalk type 32 into detached nullable fields and raw-clock timestamps. It
does not promote event-header writer PID/TID to the sampled actor or guess a
pointer width/unknown layout. Callers must prove source clock, event-time actor
identity and a unique timestamp/TID association before combining samples/stacks.
Multiple user/kernel fragments remain ambiguous unless independently validated.

Core's `SampledCallTree<TFrame>.Build` counts root-to-leaf observations with weight
one and contextual frame keys. It reports inclusive/exclusive counts and shares
of all supplied samples, including missing and wholly omitted depth/node-limited
samples. It does not derive CPU time from an assumed sampling interval. The
caller supplies source/actor/module context in keys and can use the checkpoint
for cancellation. Its detached node/parent rows use existing snapshot data sources
and VisualGrid details rather than requiring a new tree control.

Native contracts: [System Providers](https://learn.microsoft.com/en-us/windows/win32/etw/system-providers),
[owned multiplexed system loggers](https://learn.microsoft.com/en-us/windows/win32/etw/configuring-and-starting-a-systemtraceprovider-session),
[SampledProfile](https://learn.microsoft.com/en-us/windows/win32/etw/sampledprofile),
[StackWalk](https://learn.microsoft.com/en-us/windows/win32/etw/stackwalk-event)
and [per-session stack configuration](https://learn.microsoft.com/en-us/windows/win32/api/evntrace/nf-evntrace-tracesetinformation).

### Owned native resources

`StartRealTimeSessionAsync` creates an independently named session. Enable,
disable, rundown and stop commands verify the caller's complete session identity
before mutation; they do not take over a system/global kernel logger. The
`EtwTraceProviderOptions` overload adds an opt-in provider stack request through
`ENABLE_TRACE_PARAMETERS`. Acceptance is provider/privilege dependent and does
not guarantee a stack on each event. Consumers own fallback and coverage policy.
Detached extended data is limited to 64 items/256 KiB per event, retaining
documented stack32/stack64 match IDs and original addresses. The header writer
context remains distinct from a payload's resource actor.

`WindowsEventLogQuery` builds bounded structured XML queries for provider/event
IDs, maximum level, UTC time, writer PID, activity ID and named EventData equality.
Typed event reads preserve original XML and native property variants separately
from formatted-description failures. `WatchEventsAsync` owns an incoming-only
watcher on a worker, disposes native records after detaching them and drains on
cancellation. Applications choose retention, logging, saved preferences and the
owning screen/application lifetime. The legacy historical reader keeps its
existing XML/description policy.

`PrepareTcpCloseAsync` captures the exact IPv4 TCP tuple, native connection
creation time and verified process generation. `CloseTcpConnectionAsync`
revalidates those values and state before `SetTcpEntry(DELETE_TCB)`. This Windows
API does not provide atomic compare-and-delete; callers must disclose that race
and own confirmation. UDP, IPv6 and listeners are outside this command's supported
set. Endpoint inventory remains a separate read-only source.

Use allocator-matched scopes rather than cleanup-only try/finally blocks.
[UnmanagedMemoryScope](Misc/UnmanagedMemoryScope.cs) owns HGlobal allocations,
[NetApiBufferScope](Security/NetApiBufferScope.cs) owns NetAPI-returned memory, and
the WTS reader uses distinct memory and server-handle scopes. They never free a
borrowed security descriptor or a buffer allocated by another API. Temporary
console/worker ownership uses the existing synchronous/async scope facilities;
async cleanup drains the owned work before publishing completion or returning
partial results. See the [disposal practice](../../docs/guidelines/patterns/resource-scopes.md).

[WindowsDevice](Drivers/WindowsDevice.cs) owns an opened kernel device handle
(`\\.\Name` or `\\?\GLOBALROOT\Device\Name`): `Open` throws the unchanged Windows
error, `ProbeAccess` reports that error without keeping a handle, and
`TryControl`/`Control` dispatch one control code with caller-owned buffers.
Opening a device neither installs nor starts a driver; consumers own their
driver's codes, payloads and response validation, and dispatch from a worker.
[ThreadGroupAffinityScope](Scopes/ThreadGroupAffinityScope.cs) pins the current
native thread to one processor-group mask for a per-core read and restores the
exact previous affinity on the same thread; like the DPI scope it is synchronous
and never spans an `await`.

### Hosted isolated diagnostic workers

File/driver calls can block; console signaling must not attach the GUI process
to another console. [WindowsProcessWorkerHost](Processes/WindowsProcessWorkerHost.cs)
selects the consuming entry assembly/adjacent apphost with a dotnet fallback,
or a supplied managed assembly/apphost pair or standalone single-file executable.
Never launch a class-library DLL as a helper. Route helpers before UI startup:

~~~csharp
using System;
using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.Files;

internal static class Program {
	private static int Main(string[] args) {
		if (FileHandleService.TryRunWorker(args, out var fileExit))
			return fileExit;
		if (ProcessSignalService.TryRunWorker(args, out var signalExit))
			return signalExit;
		Console.WriteLine($"Read {Tools.Windows.Processes.GetProcesses().Length} Windows processes.");
		return 0;
	}
}
~~~

Apply the same early routing at a WinForms STA entry point. FileHandleService
accepts a host; SendConsoleAsync accepts workerHost. Deadlines/cancellation stop
only owned helpers and retain partial results. Application elevated-restart and
Task Manager helper entrypoints use generic registry/process/pipe APIs.

### Bounded UI Automation Inspection

`WindowsUiAutomationUtil.CaptureAsync` reads the selected HWND's UI Automation
Control View through the caller-configured `WindowsProcessWorkerHost`. The host
must route `WindowsUiAutomationUtil.TryRunWorker` before application/UI startup.
`Tools.Windows.UiAutomation` exposes the reusable facade; set its `WorkerHost`
when the current entry assembly is not the application's diagnostic dispatcher.
No UIAutomationClient assembly or external package reference is required.

`WindowsUiAutomationOptions` bounds nodes, depth and capture time, with optional
pattern-support reads. An isolated MTA creates native `IUIAutomation` cache
requests and walks one control node at a time. Standard properties and supported
pattern names become detached records, including per-property unavailability;
no live COM object or provider action escapes. The caller's process creation
time, HWND, native owner thread and class are checked around the read using the
same held-process/window admission as bounds inspection. Control-tree changes
can still occur during the point-in-time capture; runtime IDs are observations,
not persistent resource identities.

The owned helper streams bounded records through `WindowsProcessWorkerSession`.
Timeout or a blocked provider terminates only that helper and preserves already
received nodes with explicit partial reasons. Caller cancellation still performs
bounded cleanup and propagates cancellation. A helper whose termination could
not be confirmed prevents another UIA helper from being launched until it exits.
The shared session also owns bounded stderr draining, protocol line quotas and
idempotent cleanup; individual services retain their own operation budgets and
partial/destructive outcome policies.

The COM prefixes and IDs follow the Windows SDK `UIAutomationClient.idl`.
See Microsoft's [UIA threading guidance](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-threading),
[element retrieval and traversal](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-obtainingelements)
and [property caching](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-cachingforclients).

### Read-only launch-source provenance

`WindowsStartupRegistrationDataSource` implements `IDataSource` through the
existing reloadable snapshot source. Its `WindowsStartupSources` flags explicitly
select Winlogon/Active Setup Registry configuration, boot/logon scheduled-task
actions and persistent WMI filters/consumers/bindings in `root\subscription`.
The existing Run/RunOnce/folder APIs remain separate; this reader exposes no
task, service, Registry or subscription mutation. `Tools.Windows.Startup` serializes
additional-provider captures across consumers and dispatches complete reads to
workers. Queries subsequently page/search/sort published detached rows.

The caller sets a row budget (default 5,000; maximum 10,000) and per-WMI-query
wait timeout and cooperative enumeration deadline (default 10 seconds; maximum
30, checked between returned objects). An in-progress native provider wait can
finish after that deadline. Limit/error records distinguish partial
capture from an empty inventory. Long native fields retain an explicit truncation
marker after 16,384 characters. Task execution conditions and per-user Active
Setup state mean registration presence never proves execution. Provider field
layouts follow the installed Windows `SchedProv.mof`; see Microsoft's
[boot/logon trigger definitions](https://learn.microsoft.com/en-us/windows/win32/taskschd/trigger-types)
and [WMI subscription association](https://learn.microsoft.com/en-us/windows/win32/wmisdk/--filtertoconsumerbinding).

`Tools.Windows.Shell.ReadShortcutAsync` uses the existing owned Shell STA and the
existing `ShellLink` COM implementation to load stored metadata without calling
`IShellLink.Resolve`. `Tools.Windows.Startup.ReadShortcutTargetAsync` and `ReadTarget`
retain raw provenance while rejecting ambiguous/relative/PATH, network and
unresolved targets. Environment expansion uses the caller process and discloses
that it does not emulate another principal. Stored target text follows
[`IShellLink.GetPath` / `SLGP_RAWPATH`](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ishelllinka-getpath);
this is inspection, not target discovery, repair or launch.

### Presentation and resource ownership

Constructing native APIs does not start timers, register plugins, resolve
IApplication or change machine settings. Consumers choose refresh/sampling,
confirmation and dispatch. Borrowed loggers retain their external owner; use
IApplication.Logger at the application boundary.

Core's [ArrayDataSource<T>](../Sphere10.Framework/DataSource/ArrayDataSource.cs)
owns generic search/sort/paging; WinForms'
[VisualGridArrayController<T>](../Sphere10.Framework.Windows.Forms/VisualGrid/VisualGridArrayController.cs)
owns binding/selection retention. See the [control catalog](../../docs/guidelines/patterns/winforms-controls.md)
and [Windows practice](../../docs/guidelines/patterns/windows-interop.md).

## 🔧 Core Examples

### Accessing Windows NT Objects

```csharp
using Sphere10.Framework.Windows.Security;
using Sphere10.Framework.Windows;

// Get current machine (local host)
var currentHost = NTHost.CurrentMachine;
Console.WriteLine($"Current Machine: {currentHost.Name}");
Console.WriteLine($"Machine SID: {currentHost.SID}");

// Connect to remote machine
var remoteHost = NTHost.OpenRemote("REMOTE-COMPUTER-NAME");
Console.WriteLine($"Remote Machine: {remoteHost.Name}");
Console.WriteLine($"Remote SID: {remoteHost.SID}");

// Create NT object reference (resolved)
var ntObject = new NTRemoteObject(
    currentHost.Name,           // Host name
    "DOMAIN",                   // Domain name
    "UserName",                 // Object name
    currentHost.SID,            // SID
    WinAPI.ADVAPI32.SidNameUse.User  // Object type
);

Console.WriteLine($"NT Object: {ntObject.Domain}\\{ntObject.Name}");
```

### Local User Management

```csharp
using Sphere10.Framework.Windows.Security;

var host = NTHost.CurrentMachine;

// Get built-in users
var adminUser = host.GetUserByName("Administrator");
var guestUser = host.GetUserByName("Guest");

Console.WriteLine($"Admin: {adminUser.Name}");
Console.WriteLine($"Guest: {guestUser.Name}");

// Get user properties
var homeDir = adminUser.HomeDirectory;
var scriptPath = adminUser.ScriptPath;
var logonServer = adminUser.LogonServer;
var sid = adminUser.SID;

Console.WriteLine($"Home Directory: {homeDir}");
Console.WriteLine($"Script Path: {scriptPath}");
Console.WriteLine($"SID: {sid}");

// Update user properties
adminUser.HomeDirectory = "C:\\Users\\NewPath";
adminUser.ScriptPath = "C:\\Scripts\\Login.bat";

// Set password
adminUser.SetPassword("NewPassword123!");

// Get all local users
var allUsers = host.GetAllUsers();
foreach (var user in allUsers) {
    Console.WriteLine($"User: {user.Name} (SID: {user.SID})");
}
```

### Group Management & Membership

```csharp
using Sphere10.Framework.Windows.Security;

var host = NTHost.CurrentMachine;

// Get groups
var adminsGroup = host.GetGroupByName("Administrators");
var usersGroup = host.GetGroupByName("Users");
var guestsGroup = host.GetGroupByName("Guests");

// Get group members
var adminMembers = adminsGroup.Members;
Console.WriteLine($"Administrators group has {adminMembers.Count} members:");
foreach (var member in adminMembers) {
    Console.WriteLine($"  - {member.Name}");
}

// Check group membership
var adminUser = host.GetUserByName("Administrator");
bool isAdmin = adminsGroup.Members.Contains(adminUser);
Console.WriteLine($"Administrator is admin: {isAdmin}");

// Get groups that user belongs to
var userGroups = adminUser.Groups;
Console.WriteLine($"Administrator belongs to {userGroups.Count} groups:");
foreach (var group in userGroups) {
    Console.WriteLine($"  - {group.Name}");
}

// Get group properties
var comment = adminsGroup.Comment;
var groupSID = adminsGroup.SID;

Console.WriteLine($"Group SID: {groupSID}");
Console.WriteLine($"Comment: {comment}");

// Get all groups
var allGroups = host.GetAllGroups();
Console.WriteLine($"Total groups: {allGroups.Count}");
```

### Dangling Objects (Unresolved References)

```csharp
using Sphere10.Framework.Windows.Security;

var host = NTHost.CurrentMachine;

// Create dangling object by name (SID not resolved)
// Used when referenced user/group no longer exists
var danglingUser = new NTDanglingObject(
    host.Name,
    "DeletedUserName"
);

Console.WriteLine($"Dangling Object Name: {danglingUser.Name}");
Console.WriteLine($"Dangling Object SID: {danglingUser.SID}");  // null

// Create dangling object by SID (name unknown)
// Used when you have a SID but the account is deleted
var deletedUserSID = new SecurityIdentifier("S-1-5-21-2623811981-2622556916-1865790152-1001");
var danglingBySID = new NTDanglingObject(
    host.Name,
    deletedUserSID,
    WinAPI.ADVAPI32.SidNameUse.User
);

Console.WriteLine($"Dangling (by SID) SID: {danglingBySID.SID}");
Console.WriteLine($"Dangling (by SID) Name: {danglingBySID.Name}");  // Unknown

// Example: Cleaning up ACLs with dangling references
var acl = // ... get ACL from file/registry
var aceToRemove = acl.FindDanglingEntries().FirstOrDefault();
if (aceToRemove != null) {
    acl.RemoveAccessRule(aceToRemove);
    Console.WriteLine("Removed dangling ACE");
}
```

### Privilege Management

```csharp
using Sphere10.Framework.Windows.Security;

var host = NTHost.CurrentMachine;
var adminUser = host.GetUserByName("Administrator");

// Grant privilege
adminUser.Privileges.Add(UserPrivilege.SeNetworkLogonRight);
adminUser.Privileges.Add(UserPrivilege.SeLockMemoryPrivilege);
adminUser.Privileges.Add(UserPrivilege.SeDebugPrivilege);

Console.WriteLine("Privileges granted");

// Check if user has privilege
bool hasNetworkLogon = adminUser.Privileges.Contains(UserPrivilege.SeNetworkLogonRight);
Console.WriteLine($"Has network logon right: {hasNetworkLogon}");

// Revoke privilege
adminUser.Privileges.Remove(UserPrivilege.SeLockMemoryPrivilege);
Console.WriteLine("Privilege revoked");

// List all user privileges
var privileges = adminUser.Privileges;
Console.WriteLine($"Total privileges: {privileges.Count}");
foreach (var priv in privileges) {
    Console.WriteLine($"  - {priv}");
}

// Common privileges
var commonPrivileges = new[] {
    UserPrivilege.SeNetworkLogonRight,           // Network access
    UserPrivilege.SeInteractiveLogonRight,       // Interactive logon
    UserPrivilege.SeBackupPrivilege,             // Backup privilege
    UserPrivilege.SeRestorePrivilege,            // Restore privilege
    UserPrivilege.SeSecurityPrivilege,           // Manage audit logs
    UserPrivilege.SeDebugPrivilege,              // Debug programs
    UserPrivilege.SeTakeOwnershipPrivilege,      // Take ownership
    UserPrivilege.SeLockMemoryPrivilege          // Lock memory pages
};
```

### Access Control & Permissions

```csharp
using Sphere10.Framework.Windows.Security;
using System.Security.AccessControl;
using System.IO;

// Get security descriptor for file
var fileInfo = new FileInfo("C:\\Important\\Data.txt");
var fileSecurity = fileInfo.GetAccessControl();
var rules = fileSecurity.GetAccessRules(true, true, typeof(NTAccount));

// Display access rules
Console.WriteLine("Access Rules:");
foreach (AccessRule rule in rules) {
    var rights = rule is FileSystemAccessRule fsRule ? fsRule.FileSystemRights : AccessControlType.Allow;
    Console.WriteLine($"  Identity: {rule.IdentityReference}");
    Console.WriteLine($"  Type: {rule.AccessControlType}");
    Console.WriteLine($"  Rights: {rights}");
}

// Set permissions
var adminNTAccount = new NTAccount(NTHost.CurrentMachine.Name, "Administrator");
var fileAccessRule = new FileSystemAccessRule(
    adminNTAccount,
    FileSystemRights.FullControl,
    AccessControlType.Allow);

fileSecurity.AddAccessRule(fileAccessRule);
fileInfo.SetAccessControl(fileSecurity);
Console.WriteLine("Permissions updated");

// Remove user permissions
fileSecurity.RemoveAccessRule(fileAccessRule);
fileInfo.SetAccessControl(fileSecurity);
Console.WriteLine("Permissions removed");
```

### Windows Services

```csharp
using Sphere10.Framework.Windows;
using System.ServiceProcess;

// Get service controller
var serviceController = new ServiceController("ServiceName");

// Check service status
var status = serviceController.Status;
Console.WriteLine($"Service Status: {status}");

// Start service
if (status == ServiceControllerStatus.Stopped) {
    serviceController.Start();
    serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
    Console.WriteLine("Service started");
}

// Stop service
if (status == ServiceControllerStatus.Running) {
    serviceController.Stop();
    serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
    Console.WriteLine("Service stopped");
}

// Get service info
Console.WriteLine($"Display Name: {serviceController.DisplayName}");
Console.WriteLine($"Service Name: {serviceController.ServiceName}");

// List all services
var services = ServiceController.GetServices();
foreach (var svc in services) {
    if (svc.Status == ServiceControllerStatus.Running) {
        Console.WriteLine($"Running: {svc.DisplayName}");
    }
}
```

### Event Log Integration

```csharp
using System.Diagnostics;

// Write to Windows Event Log
var eventLog = new EventLog("Application");
eventLog.Source = "MyApp";

eventLog.WriteEntry("Application started", EventLogEntryType.Information);
eventLog.WriteEntry("Critical error occurred", EventLogEntryType.Error);
eventLog.WriteEntry("Warning message", EventLogEntryType.Warning);

// Read from Event Log
var entries = eventLog.Entries;
foreach (EventLogEntry entry in entries) {
    if (entry.Source == "MyApp") {
        Console.WriteLine($"Type: {entry.Type}");
        Console.WriteLine($"Message: {entry.Message}");
        Console.WriteLine($"Time: {entry.TimeGenerated}");
        Console.WriteLine();
    }
}

// Filter recent entries
var recentEntries = entries
    .Cast<EventLogEntry>()
    .Where(e => e.Source == "MyApp")
    .Where(e => e.TimeGenerated > DateTime.Now.AddDays(-1))
    .OrderByDescending(e => e.TimeGenerated)
    .Take(100);

foreach (var entry in recentEntries) {
    Console.WriteLine($"{entry.TimeGenerated} [{entry.Type}] {entry.Message}");
}
```

## 🛠️ Tools.* Namespace

This project extends the global Tools namespace with Windows-specific utilities:

### **Tools.WinTool**
Windows-specific operations: registry, services, events, privileges.
```csharp
// Registry operations
bool exists = Tools.WinTool.KeyExists(hostname, "HKEY_LOCAL_MACHINE\\Software\\MyApp");
var key = Tools.WinTool.OpenKey(hostname, keyPath);
var subkeys = Tools.WinTool.GetSubKeys(hostname, keyPath);

// Service management
bool running = Tools.WinTool.IsServiceRunning("MyService");
Tools.WinTool.StartService("MyService");
Tools.WinTool.StopService("MyService");

// Privileges and security
bool modified = Tools.WinTool.ModifyState(tokenHandle, "SeRestorePrivilege", true);
ushort hiword = Tools.WinTool.HIWORD(data);
Key key = Tools.WinTool.VirtualKeyToKey(virtualKeyCode);
```

### **Tools.WindowsTool**
Advanced Windows shell operations.
```csharp
Tools.WinShell.CreateShortcutForApplication(executable, shortcutPath, arguments);
string shortcutPath = Tools.WinShell.DetermineStartupShortcutFilename(appName);
```

For complete Tools reference, see [docs/tools-reference.md](../../docs/tools-reference.md)

## 🏗️ Architecture & Modules

**Security Module**: Windows NT security abstraction
- NT object model (users, groups, computers)
- SID resolution and management
- Local and remote operations
- User and group enumeration

**Privilege Module**: Windows privilege management
- Grant and revoke system privileges
- Privilege enumeration
- Privilege name mapping

**Access Control Module**: Security descriptors and ACLs
- ACL reading and modification
- Permission verification
- DACL and SACL management

**Services Module**: Windows service management
- Service enumeration and control
- Service status monitoring
- Start/stop operations

**Event Log Module**: Windows Event Log integration
- Event writing and reading
- Log filtering and searching
- Event type classification

**Registry Module**: Windows registry access (where applicable)
- Secure registry operations
- Registry value management
- Access control

## 📦 Dependencies

- **Sphere10 Framework**: Core framework
- **System.ServiceProcess.ServiceController**: Service management (.NET built-in)
- **System.Diagnostics.EventLog**: Event logging (.NET built-in)
- **System.Management**: WMI-backed inventory and management
- **System.Drawing.Common**: Windows image/icon helpers
- **System.Security.AccessControl**: Security and ACL (.NET built-in)

## ⚠️ Permissions Required

- **Most operations**: Require **Administrator privileges**
- **Reading information**: Accessing local machine info doesn't require elevation
- **Creating/modifying users**: Requires elevation
- **Changing privileges**: Requires elevation
- **Event Log writing**: May require elevation depending on event source
- **Service management**: Requires elevation

## ⚠️ Best Practices

- **Run with minimum required privileges**: Only use admin when necessary
- **Handle dangling objects gracefully**: Expect deleted users/groups
- **Clean up ACLs**: Remove dangling references from access control lists
- **Use remote carefully**: Test thoroughly on remote machines first
- **Cache lookups**: Cache user/group lookups to avoid repeated queries
- **Error handling**: NT operations can fail if users/groups don't exist
- **Thread safety**: Create separate instances for concurrent access
- **Security**: Never log or expose sensitive security information

## ✅ Status & Compatibility

- **Maturity**: Production-tested for Windows system integration
- **.NET Target**: .NET 10 (`net10.0`)
- **Platform**: Windows only (uses Windows-specific APIs)
- **Privileges**: Most operations require administrative elevation

## 📖 Related Projects

- [Sphere10.Framework](../Sphere10.Framework) - Core framework
- [Sphere10.Framework.Windows.Forms](../Sphere10.Framework.Windows.Forms) - Windows Forms UI framework
- [Sphere10.Framework.Windows.LevelDB](../Sphere10.Framework.Windows.LevelDB) - Windows LevelDB integration
- [Sphere10.Framework.Application](../Sphere10.Framework.Application) - Application framework

## ⚖️ License

Distributed under the **MIT License**.

See the LICENSE file for full details. More information: [MIT License](https://opensource.org/license/mit)

## 👤 Author

**Herman Schoenfeld** - Software Engineer

## Native interop and utility ownership

WinAPI nested types identify DLLs: KERNEL32 owns process/file/console/system
declarations, PSAPI process-memory/performance, IPHLPAPI endpoint/interface tables,
NTDLL native handles, RSTRTMGR Restart Manager, USER32 window/message/environment
broadcasts, and ADVAPI32 registry/service/token calls. Extend the DLL facade
rather than creating domain-named extern containers.

Typed domain APIs sit above the bindings. Apps own collectors, sampling/rates,
view models, display metadata and plans. Core's Statistics(0) is the reusable
accumulator; the caller selects metrics, units and lifetime. Follow the
[Windows facade practice](../../docs/guidelines/patterns/windows-interop.md).

## Global hotkey bindings

[WinAPI.USER32](WinAPI/HotKeys.cs) supplies `RegisterHotKey`, `UnregisterHotKey`,
`WM_HOTKEY`, the modifier flags and the message-only window parent. Register and
release a shortcut on its owning window thread, and use `MOD_NOREPEAT` when one
notification per press is required. WinForms consumers can use the owned
[GlobalHotKey component](../Sphere10.Framework.Windows.Forms/HotKeys/GlobalHotKey.cs)
instead of maintaining native window registration themselves. Product settings
and toggle behavior remain in the consuming application.

## Magnification and overlay layering

[WinAPI.MAGNIFICATION](WinAPI/MAGNIFICATION.cs) declares the Windows Magnification
API: initialization, the `WC_MAGNIFIER` control, window source/transform/filter
lists, per-window and full-screen color effects. [MagnificationSession](Magnification/MagnificationSession.cs)
owns the one process-level `MagInitialize`/`MagUninitialize` lifetime and applies the
full-screen color effect (`SetFullscreenColorEffect`, `ResetFullscreenColorEffect`);
disposal uninitializes, which also removes the effect. [MagnifierWindow](Magnification/MagnifierWindow.cs)
hosts a magnifier control in a passive, topmost, click-through layered popup that
re-renders `Bounds` (physical pixels) through a GDI+ `ColorMatrix` at 1:1, optionally
clipped to `SourceBounds`, with `ExcludedWindows` left out of the capture. Windows
captures a frame each time the source is set, so callers `Refresh()` once per display
frame; re-assigning an unchanged `ExcludedWindows` list costs nothing. Use both on the
UI thread that created the session, and keep the session alive longer than its windows.

The full-screen effect is the only zero-latency transformation Windows offers, and it
covers the whole desktop. Magnifier windows lag by up to one refresh, so drive them with
[DisplayFrameClock](Display/DisplayFrameClock.cs) rather than a WinForms timer, whose
15.6 ms granularity and fixed intervals fall behind the screen. The clock raises `Frame`
on its synchronization context at the compositor's refresh rate (`RefreshRate`, re-read
every second), or no faster than `MinimumInterval`. A frame stays pending until its
handler returns, so a slow handler lowers the rate without queueing a backlog or
starving input. Stop the clock while nothing needs re-rendering.

```csharp
using var session = new MagnificationSession();
var window = new MagnifierWindow(session, screenBounds, "Inverter") { ColorEffect = negative };
window.Show();
using var clock = new DisplayFrameClock();
clock.Frame += (_, _) => window.Refresh();
clock.Start();
```

[WindowsWindowLayering](Windowing/WindowsWindowLayering.cs) answers z-order
questions: `GetPassiveOverlaysAbove(referenceWindows)` returns the visible layered,
click-through windows (pointer sprites, annotation overlays) that sit above every
reference window, walking from the top of the z-order and stopping at the first
reference. A capture surface excludes those so they render once, live, while an
overlay the surface already covers stays captured rather than disappearing.

[DisplayColorEffectRenderer](Magnification/DisplayColorEffectRenderer.cs) composes all
of these into per-display colour effects. `Render(displays, fade)` takes one
`DisplayColorEffect(deviceName, bounds, matrix)` per display. While every display has
the same matrix (`GetSharedMatrix`) it uses the full-screen effect, fading over
`FadeDuration` when asked. Once displays differ, each display with a non-identity
matrix gets a magnifier window, re-captured on every display frame or every
`MinimumRefreshInterval`, with passive overlays excluded (`ExcludePassiveOverlays`).
The two paths cannot cross-fade, so switching is immediate and raises
`RenderingChanged`. `Clear(fade)` restores normal colours. A busy Magnification API
makes `Render` throw a `Win32Exception`, and a failure during a frame raises `Failed`;
either way the renderer has already restored normal colours. Create, use and dispose it
on one UI thread. Effect catalogues and which matrix each display shows remain
application policy; Lumina supplies them.

```csharp
using var renderer = new DisplayColorEffectRenderer(SynchronizationContext.Current);
var negative = ColorMatrices.CreateInversion();
renderer.Render(Screen.AllScreens.Select(screen => new DisplayColorEffect(screen.DeviceName, screen.Bounds,
	screen.Primary ? negative : ColorMatrices.Identity)), fade: false);
```

## Cached WAV feedback

`IWaveSoundPlayer` and `WaveSoundPlayer` in `Audio/` provide synchronous WAV
playback with a bounded cache of loaded samples (six entries, 4 MiB retained and
1 MiB per WAV by default; constructor overrides are available).
`Play(key, streamFactory)` owns
and closes a newly opened stream, reuses samples for the same key, and completes
when playback ends. `Stop()` can interrupt playback from another thread;
disposal stops playback and releases the cache. Call blocking playback from an
owned background worker, then stop and join that worker during shutdown.

Applications choose sample keys, resources, user preferences and preview policy.
The player does not simulate input, create UI, retry sounds or play a warm-up
sound. Windows audio-device behavior after a long idle remains device dependent.

## Reusable file, account and SID operations

[Tools.Windows.Files](Files/FileUtil.cs) exposes named `$DATA` streams
through [WindowsFile](Files/WindowsFile.cs). Enumeration excludes the unnamed
primary contents; opening a stream returns an owned `FileStream`. Creation
requires an existing primary file; empty names and stream/path separators are
rejected. Delete removes the exact named stream. Use bounded reads for unknown
streams and dispose every returned stream. Apps own encoding, editing, stale
checks and confirmation.

[Tools.Windows.Computer](System/ComputerUtil.cs) exposes detached
computer, system-product, BIOS, baseboard, processor and operating-system
resources, physical RAM modules and arrays, enclosures, video controllers, monitor
EDID identities, present Plug and Play devices and printer connections. These
queries return raw Windows resource records without presentation metadata.
Memory-array maximum capacity is converted from WMI kilobytes to bytes;
configured module clock is the provider's MHz value. Video-controller
`ReportedMemoryBytes` retains the limited 32-bit WMI AdapterRAM field and must
not be treated as authoritative modern GPU memory capacity.
Queries release WMI objects before returning typed data. Async queries
run native work on a worker and check cancellation between resources; sync
methods do not accept CancellationToken. Provider fields may be unavailable.
Apps choose report grouping, labels, caching and copy/export behavior.

Underlying provider contracts: [physical memory](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-physicalmemory),
[memory arrays](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-physicalmemoryarray),
[enclosures](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-systemenclosure),
[video controllers](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-videocontroller),
[Plug and Play entities](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-pnpentity),
[monitor identities](https://learn.microsoft.com/en-us/windows/win32/wmicoreprov/wmimonitorid) and
[printers](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-printer).

`Tools.Windows.Network.GetInterfaces[Async]` also retains detached physical
address bytes and reported link speed. Hardware-read errors are separate from
IP-address-read errors, so one unavailable part does not erase the other.

`FileHandleService.ForceRelease` / `ForceReleaseAsync` is a separate, explicit
mutation over captured process/handle identity. Passive inspection never closes
source handles. Apps confirm the source and risk; revalidation reduces, but
cannot eliminate, native handle reuse. There is no destructive automatic retry:
failure or cancellation can leave the close outcome indeterminate. Async release
uses the existing owned isolated worker.

[WindowsFile.Copy / CopyAsync](Files/WindowsFile.cs) transfer one file and streams
with overwrite selection, cancellation, byte progress and worker checkpoints.
Cancelled copies discard partial destination data; callback failures are
rethrown after returning from native code. Local/UNC paths are supported.
This API does not impose a migration layout or delete a tree.

Windows.Security supplies account/group/share resource methods, paged native
enumeration, descriptor reads/writes, raw SID lookup and effective-token scopes.
[Tools.Windows.SecurityDescriptors](Security/SecurityDescriptorUtil.cs) translates exact
owner/group/ACE identities simultaneously, preserving unrelated sections.
Account enumerators consume every page and release owning buffers; callers do
not retain interior pointers.

The File Server Migrator plugin composes these primitives. File-tree
options/results, retry/verification policy, cleaning/overlap checks, missing-account
import/default passwords, share recreation, SID walkers, progress reporting,
pause/cancel and confirmation belong to that application workflow.

Native declarations, message constants, and layouts live in `WinAPI` in this package. Use `Tools.Windows.Win32` for native message structures, cursor handles, rich-edit formatting, tab captions, and layered-window rendering. The Windows package has no dependency on Windows Forms; controls supply their native handles to these helpers.

`SoundPlayerEx` now lives in `Sphere10.Framework.Windows`. `WinAPI.USER32` owns `ICONINFO`, `MINMAXINFO`, icon/cursor functions, and window-message constants. `WinAPI.RICHEDIT` owns rich-edit constants and the Unicode `CHARFORMAT`/`FORMATRANGE` layouts; `WinAPI.COMCTL32` owns `TCITEM` and tab-message constants. `CreateCursorHandle` returns a native handle owned by the caller; release it with `WinAPI.USER32.DestroyCursor` when no longer used.

The assembly declares its Windows platform requirement. Bitmap/GDI helpers additionally declare `windows6.1`, matching their `System.Drawing.Common` dependencies. Keyboard and mouse hooks now raise their existing `Disposed` event once during explicit disposal; finalization releases the native hook without raising managed disposal events. `SoundPlayerEx.LoadStream` reads from the current position to the end, supports nonseekable streams and short reads, and leaves the input stream open.

### Process liveness

`WindowsProcess.HasExited` uses the native process handle's signaled state with a
zero-time wait. `GetProcessTimes` supplies creation and CPU times; its exit-time
output is undefined for a live process and is not used as a liveness predicate.
`WindowsProcess.Open` requests query plus synchronization access. If Windows allows
only querying, identity/path/counters remain available while `HasExited` explicitly
reports the missing synchronization permission. Identity capture and verified
commands require a live handle; they never infer a successful check from access
denial. Window and file-handle ownership checks reuse the same native liveness rule.

### Active Monitor And Display Paths

`Tools.Windows.Display.GetDisplaysAsync(token)` dispatches CCD, GDI, registry and
optional WMI metadata reads to a worker and returns `WindowsDisplayInfo[]`. It is a
read-only snapshot of active paths, with exact adapter LUID/target IDs and device
paths, current/preferred timings, color state, driver metadata, source modes and
validated cached EDID metadata. Older systems or unsupported driver queries leave
optional fields null and preserve per-query availability details. HDR support,
user enablement and active mode use the optional advanced-color v2 request; the
older advanced-color flags are not reinterpreted as HDR state.

When only names are needed, `Tools.Windows.Display.GetDisplayNamesAsync(token)` reads
the display configuration alone and returns the monitor name Windows Settings shows
for each active desktop source, keyed by its GDI device name (`\\.\DISPLAY1`, as
`Screen.DeviceName` reports it, compared ignoring case). Sources whose monitor
reports no name are omitted; a cloned source takes its first named monitor.

`WindowsDisplayDataSource` implements `IDataSource<WindowsDisplayInfo>` through
`AsyncListDataSource<T>`. Its first read collects once; sorting, filtering and page
reads reuse that inventory, and an `ActionRefreshableDataSource` over the factory serves a
fresh one on refresh. Sync
native readers assert that they are not on the main thread. Query buffers retry
boundedly when a hotplug races sizing, without publishing a failed query.

`WindowsMonitorEdid.Parse` validates the EDID 1.x base header and checksum before
decoding size/gamma. Missing extension blocks and per-block checksum failures are
reported; raw bytes remain available. The native reader reads the registry cache
and flags EDID overrides separately. It does not read DDC live, apply overrides,
change display settings or write monitor registers.

Native contracts: [QueryDisplayConfig](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-querydisplayconfig),
[display target names](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-displayconfig_target_device_name),
[video signals](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-displayconfig_video_signal_info),
and [Windows EDID overrides](https://learn.microsoft.com/en-us/windows-hardware/drivers/display/overriding-monitor-edids).

## Portable Executable And Process Memory Inspection

PE structure, bounded file/virtual-memory reads and range-scoped string extraction
are described in [Portable Executable And Process Memory Inspection](../../docs/usage/portable-executable-inspection.md).
[WindowsProcessMemoryStream](Processes/WindowsProcessMemoryStream.cs) presents one
address range of an opened process as a seekable, read-only `Stream`: each read
fetches only the requested bytes through `WindowsProcess.ReadMemoryAsync`, so a
viewer such as the hex editor can page through a large region without copying it,
and an unreadable page fails the read with the Windows error.

Approved managed-engine adapters, runtime snapshots, dumps and detached address
resolution are described in [Managed Diagnostics](../../docs/usage/managed-diagnostics.md).
Explicit module details, token restrictions, page-state queries and working-set
watch contracts are described in [Process Inspection](../../docs/usage/process-inspection.md).

## Mouse Hook Compatibility

`IMouseHook` exposes the `Scroll` event already implemented by `BaseMouseHook`.
Consumers implementing `IMouseHook` directly must provide that event; subclasses
of `BaseMouseHook` inherit its implementation.

Asynchronous `WindowsMouseHook` observations preserve native arrival order and
carry `MouseMoveEvent.MonotonicTimestampMilliseconds` captured before dispatch.
The existing wall-clock `Time` remains the processing time. Stop, restart and
uninstall discard queued observations from earlier activations; an already
running callback can finish. Native critical callbacks retain synchronous
dispatch so their subscribers can decide whether input is suppressed.
`HooksTool.IsAnyMouseButtonDown()` reads physical button state independently of
the caller's message queue, including the two extended mouse buttons.

## File And Trace Discovery

`Tools.Windows.Files.CreateHandleService(workerHost)` returns a fresh
`FileHandleService` for bounded handle/object capture, blocker discovery and
explicit verified disk-file handle release. Omitting `workerHost` preserves the
existing current-entry-assembly host selection. The application must route
`Tools.Windows.Files.TryRunHandleWorker(args, out exitCode)` before its normal
startup and exit when that dispatcher returns true. The facade does not retain
the returned service or start a capture during factory creation.

`Tools.Windows.Files.CreateInspectionService()` returns a fresh on-demand
file-inspection service; `Tools.Windows.Files.InspectAsync(path, token)` remains
the existing direct route. PE structure and bounded file/range reads remain on
the same Files utility.

`Tools.Windows.TraceSessions.TryReadCpuProfileSample(record, out sample)` and
`TryReadCpuProfileStack(record, out stack)` forward detached evidence to the
existing CPU decoder. They preserve unsupported-event results, availability,
original timestamps and raw evidence. Decoding neither opens a native consumer
nor changes a trace session or sampling policy.
