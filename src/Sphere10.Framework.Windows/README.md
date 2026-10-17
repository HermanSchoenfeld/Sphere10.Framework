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

## 🏗️ Core Concepts

**NT Security Objects**: Windows NT domain and local security principals (users, groups, computers).

**Security Identifiers (SIDs)**: Unique Windows identifiers for security objects.

**Local vs Remote**: Support for local machine and remote machine security operations.

**Dangling Objects**: Handle security references that couldn't be resolved to SIDs (deleted accounts).

**Privilege Management**: Grant and revoke user privileges at the system level.

**Access Control**: Windows security descriptors and ACLs.

## Windows resource abstractions

### Pageable native inventories

Concrete `IDataSource<T>` backends are provided by `WindowsProcessDataSource`
(inventory, identity, raw counters and executable path), `WindowsServiceDataSource`
(services or drivers), `WindowsNetworkEndpointDataSource` (one TCP/UDP IPv4/IPv6
owner table, optionally restricted to an owning PID),
`WindowsNetworkInterfaceDataSource`, `WindowsDriveDataSource`,
`WindowsPhysicalDiskDataSource` and `WindowsPartitionDataSource`.

These sources use Core's `ReloadableSnapshotDataSource<T>`. The first
`ReadRangeAsync`, `CountAsync` or `GetSnapshotAsync` loads the inventory on a
worker; subsequent searches, sorts and pages reuse it. `ReloadAsync` explicitly
captures a new inventory, retaining the last successful snapshot on failure or
cancellation. Existing pages remain readable while a reload runs. Synchronous
access can initialize synchronously; UI consumers must use the asynchronous APIs.
Each range reports its filtered total and clamped page. Native enumeration does
not repeat when navigating pages.

These sources have no timers, rate calculations, charts or management mutations.
The consumer owns refresh cadence and sampling history. Endpoint PID filtering
does not verify process creation time; a consumer requiring an exact process
instance must retain its `ProcessIdentity` checks around capture.

Windows integration has three layers: DLL-level WinAPI facades, typed Windows
resource APIs, and the application's collectors, workflows and presentation.
The [ownership practice](../../docs/guidelines/patterns/framework-reuse.md#windows-abstraction-layers)
defines the boundary. Extend the existing domain utility in its owning class/file.

Windows utilities are discovered through [WindowsTool.cs](WindowsTool.cs), whose
thread-static properties return reusable instance utilities. Use
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
| Threads | [Tools.Windows.Threads / ThreadUtil](Processes/ThreadUtil.cs) reads an exact [ThreadIdentity](Processes/ThreadIdentity.cs), changes relative priority or dynamic priority boost, and suspends/resumes once. Both process and thread handles remain owned through dispatch; process creation, thread creation, ownership and native liveness are verified. Read returns native priority/boost/timing with nullable scheduler fields. [SuspendThread](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-suspendthread) increments the count; [ResumeThread](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-resumethread) decrements once and returns the previous count, so a previous count greater than one leaves the thread suspended. Async cancellation applies before dispatch and does not hide an accepted mutation. Confirmation, permitted targets and suspension ownership belong to the caller; disposing a screen never implicitly resumes a thread. |
| System counters | [WindowsSystemInformation](System/WindowsSystemInformation.cs) returns raw processor/memory/system counts without rates or sampling history. Applications choose aggregates using Core's Statistics. |
| Performance counters | [Tools.Windows.PerformanceCounters](System/PerformanceCounterUtil.cs) opens caller-owned [WindowsPerformanceQuery](System/WindowsPerformanceQuery.cs) resources for English PDH paths, including wildcard GPU counters. Raw samples retain supplied path, provider instance, type/status and unmodified raw fields. The query collection timestamp is converted from PDH local wall time to UTC; invalid or ambiguous daylight-saving timestamps remain unavailable. Missing providers remain nonvalid status rows. Consumers choose when to read and which sample pairs to pass to native CalculateValue; GPU identity parsing, PID verification, sampling, rates, aggregation and history remain in the app. |
| Process inspection | [WindowsProcess](Processes/WindowsProcess.cs) additionally reads bounded remote environment, token SID/group/privilege/elevation fields, job membership and bounded image-file/memory strings. Memory access opens a separately verified read-only handle; it never injects or changes a target. Remote environment uses version-sensitive NT PEB/process-parameter layouts, validates normalization/length and rechecks its pointer; inaccessible/unsupported targets fail explicitly. A 32-bit inspector rejects native 64-bit targets. Job accounting is available only for the caller's own job; another process's membership does not imply access to its job handle. Strings retain address/file offset and encoding, cap each run at 4,096 characters and disclose truncation/skipped regions. |
| Power actions | [Tools.Windows.Computer](System/ComputerUtil.cs) queries native sleep/hibernate/shutdown capabilities and exposes explicit Lock, Logoff, Sleep, Hibernate, Shutdown and Restart operations with async adapters. Shutdown/restart require a caller-supplied nonzero Windows reason. Shutdown privilege is scoped to the executing thread; the library does not elevate or confirm. Request acceptance does not guarantee the later OS transition, and cancellation cannot undo an accepted request. |
| Services/drivers | [Tools.Windows.Services / ServiceUtil](Services/ServiceUtil.cs) reads native configuration/status/dependencies and performs individual start/stop/pause/resume/start-mode operations. [WindowsServiceInfo](Services/WindowsServiceInfo.cs) retains nullable fields and native error codes. [DriverManager](Drivers/DriverManager.cs) reads raw driver packages; plans stay in the app. |
| Registry | [Tools.Windows.Registry / RegistryUtil](Registry/RegistryUtil.cs) reads/changes typed keys and values through plain [RegistryAddress](Registry/RegistryAddress.cs), [RegistryValue](Registry/RegistryValue.cs) and trees. Expected raw values reject stale edits. OpenKey transfers a caller-disposed handle. Explorer navigation, editor parsing and .reg export stay in the app. |
| Environment | [Tools.Windows.Environment / EnvironmentUtil](Environment/EnvironmentUtil.cs) reads/sets/deletes Process/User/Machine variables, verifies expected raw values, preserves persistent registry kinds and broadcasts changes. Scope warnings/confirmation belong to the caller. |
| Desktop applications | [WindowsUninstallRegistration](Applications/WindowsUninstallRegistration.cs) lists/reads/removes uninstall registrations and launches a caller-selected absolute executable. Parsing/inventory/uninstall policy stay in the app. A returned Process is caller-owned; this is not MSIX/Store management. |
| MSIX / AppX packages | [AppxPackageSentry](Packages/AppxPackageSentry.cs) queries all current-user package types through the in-box Windows PowerShell Appx module and removes an exact full package identity for that account. [WindowsAppPackageInfo](Packages/WindowsAppPackageInfo.cs) contains detached OS identity, localized metadata when available and nullable removability. App merging, dependency-removal policy and confirmations stay in the caller. |
| Network | [Tools.Windows.Network / WindowsNetworkUtil](Networking/WindowsNetworkUtil.cs) reads IPv4/IPv6 TCP/UDP owner tables, interface information and native counters. Plain types retain native identities, unsigned totals, speeds and flags. |
| Storage | [Tools.Windows.Drives / WindowsDriveUtil](Storage/WindowsDriveUtil.cs) reads logical drives/physical disks/partitions and changes labels. [WindowsVolume](Files/WindowsVolume.cs) exposes native volume queries. [VolumeCheckSentry](Storage/VolumeCheckSentry.cs) wraps read-only chkdsk.exe for a selected local root with owned cancellation and structured arguments. Callers select probing, provider deadlines, confirmation and reporting; per-row errors remain alongside successful fields. |
| Startup/events | [Tools.Windows.Startup / WindowsStartupUtil](Startup/WindowsStartupUtil.cs) reads Run locations, raw approval bytes and startup files. [Tools.Windows.EventLogs / WindowsEventLogUtil](EventLogs/WindowsEventLogUtil.cs) owns readers/records and returns detached event data with caller-supplied bounds. Interpretation/display policy stays in the app. |
| Windows/messages | [WindowsWindow](Windowing/WindowsWindow.cs) reads HWND/title/class/thread/process/visibility/minimized data. Optional process-detail omission lets callers join their existing process inventory. GetBounds verifies the captured process creation time and checks the window thread/class before and after reading [GetWindowRect](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowrect) screen coordinates; those coordinates follow the caller's DPI context and may include invisible resize borders. GetPhysicalBounds uses a synchronous ThreadDpiAwarenessScope to return the same verified outer rectangle in physical desktop pixels, independent of the caller context; it preserves the process DPI policy. [WindowMessageSender](Windowing/WindowMessageSender.cs) posts or sends with timeout, retaining process identity through main-window dispatch. The caller supplies editor/highlight policy and confirmation. |
| Console/pipes | [ProcessSignalService](Processes/ProcessSignalService.cs) uses an owned console helper. [Tools.Windows.Pipes / NamedPipeUtil](Pipes/NamedPipeUtil.cs) lists local pipe names without opening endpoints; [NamedPipePeer](Pipes/NamedPipePeer.cs) queries connected peer identity. Client capture, elevated restart and Task Manager replacement remain application workflows. |
| Trace sessions | [Tools.Windows.TraceSessions / EtwTraceUtil](Tracing/EtwTraceUtil.cs) queries accessible native ETW sessions, enables real-time delivery on an exactly identified existing session and consumes detached [EtwTraceEventRecord](Tracing/EtwTraceEventRecord.cs) records. The reader owns OpenTrace/ProcessTrace/CloseTrace and generic TDH field decoding; provider interpretation, packet attribution, bounded storage and capture/recovery policy stay in the app. |
| Logged-on sessions | [Tools.Windows.Sessions / SessionUtil](Sessions/SessionUtil.cs) queries local or named-server WTS sessions through plain [WindowsSessionInfo](Sessions/WindowsSessionInfo.cs). Message, disconnect, logoff and remote-control operations revalidate session ID/server/user/logon time. Remote control requires an existing RDP caller session and uses Windows permissions/consent. Native memory/server handles are scope-owned; tool layout and confirmation stay in the app. |
| Shell | [Tools.Windows.Shell / ShellUtility](Shell/ShellUtility.cs) launches through the Windows shell, resolves known folders, opens native property sheets, queries/empties the Recycle Bin, and reads bounded real items in shell namespaces. [WindowsShellItemInfo](Shell/WindowsShellItemInfo.cs) contains detached native fields, including available recycle origin/deletion metadata; native date conventions are retained. Queries use an owned STA worker; synchronous namespace/verb operations require STA. COM references are scoped and never returned to consumers. Item commands revalidate the selected native identity and use the exact currently offered, localized [FolderItemVerb](https://learn.microsoft.com/en-us/windows/win32/shell/folderitemverb-doit); an accepted request does not imply asynchronous completion. Returned process observation handles are caller-owned. UI operations retain the calling STA/owner HWND where the native API supports it; inventory choices, UAC policy and confirmation text belong to the app. See Microsoft's [ShellExecuteEx](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shellexecuteexw) and [Recycle Bin query](https://learn.microsoft.com/en-us/windows/win32/api/shellapi-shqueryrecyclebinw) contracts. |
| File associations | [Tools.Windows.Shell / ShellUtility](Shell/ShellUtility.cs) queries effective extension associations or an explicit fixed ProgID through [AssocQueryStringW](https://learn.microsoft.com/en-us/windows/win32/api/shlwapi/nf-shlwapi-assocquerystringw). [ShellFileAssociationInfo](Shell/ShellFileAssociationInfo.cs) retains resolved native strings and a per-field HRESULT; missing executable/command fields remain empty, including COM or packaged-app handlers. Reads avoid executable verification and registry fixups. NotifyFileAssociationsChanged sends the native [association-change notification](https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shchangenotify) after caller-owned registration edits; it does not change defaults or wait for every Shell client. Inventory, registry editing, confirmation and Windows Default Apps navigation stay in the app. |
| File inspection | [FileHandleService](Files/FileHandleService.cs) reads handles/Restart Manager users with bounded owned helpers. [WindowsFileInspectionService](Files/WindowsFileInspectionService.cs) reads version/hash/signature data. Partial captures remain explicit; an open handle is not proof of an exclusive lock. Source handles/processes are never closed. |
| File transfer | [WindowsFile](Files/WindowsFile.cs) copies one file and streams with cancellation, byte progress and worker checkpoints. Tree migration, retries, verification, cleanup policy and results stay in the application workflow. |
| Icons | [FileIconCache](Shell/FileIconCache.cs) owns worker-loaded local EXE/DLL/ICO resources and PNG/BMP/JPEG/GIF logo images with configurable size/capacity/budget and lends GetImage results. The Drawing resize helpers fit raster logos into a centered, transparent square without distorting their aspect ratio; pixels are detached from their source files. App process/application adapters stay in the app. Dispose the cache after cells stop borrowing images. |

Existing helpers may retain the root Sphere10.Framework.Windows namespace;
networking, storage, files, shell and event-log APIs use domain namespaces.
The assembly depends on Core, Drawing and Windows providers, without SystemExpert,
Application, Data or Windows Forms dependencies.

### Asynchronous Shell And Session Operations

Use `ShellUtility`'s `LaunchAsync`, `OpenShellItemAsync`, `OpenDirectoryAsync`,
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

`SessionUtil.GetCurrentSessionIdAsync` and `IsCurrentSessionRemoteAsync` include
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

### Owned native resources

Use allocator-matched scopes rather than cleanup-only try/finally blocks.
[UnmanagedMemoryScope](Misc/UnmanagedMemoryScope.cs) owns HGlobal allocations,
[NetApiBufferScope](Security/NetApiBufferScope.cs) owns NetAPI-returned memory, and
the WTS reader uses distinct memory and server-handle scopes. They never free a
borrowed security descriptor or a buffer allocated by another API. Temporary
console/worker ownership uses the existing synchronous/async scope facilities;
async cleanup drains the owned work before publishing completion or returning
partial results. See the [disposal practice](../../docs/guidelines/patterns/resource-scopes.md).

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

### Presentation and resource ownership

Constructing native APIs does not start timers, register plugins, resolve
IApplication or change machine settings. Consumers choose refresh/sampling,
confirmation and dispatch. Borrowed loggers retain their external owner; use
IApplication.Logger at the application boundary.

Core's [SnapshotDataSource<T>](../Sphere10.Framework/DataSource/SnapshotDataSource.cs)
owns generic search/sort/paging; WinForms'
[CrudGridSnapshotController<T>](../Sphere10.Framework.Windows.Forms/Crud/CrudGridSnapshotController.cs)
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
[GlobalHotKey component](../Sphere10.Framework.Windows.Forms/Components/GlobalHotKey.cs)
instead of maintaining native window registration themselves. Product settings
and toggle behavior remain in the consuming application.

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

[Tools.Windows.Files / FileUtil](Files/FileUtil.cs) exposes named `$DATA` streams
through [WindowsFile](Files/WindowsFile.cs). Enumeration excludes the unnamed
primary contents; opening a stream returns an owned `FileStream`. Creation
requires an existing primary file; empty names and stream/path separators are
rejected. Delete removes the exact named stream. Use bounded reads for unknown
streams and dispose every returned stream. Apps own encoding, editing, stale
checks and confirmation.

[Tools.Windows.Computer / ComputerUtil](System/ComputerUtil.cs) exposes detached
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
[Tools.Windows.SecurityDescriptors / SecurityDescriptorUtil](Security/SecurityDescriptorUtil.cs) translates exact
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
