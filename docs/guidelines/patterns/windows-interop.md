# Windows API facades

Read this practice when consuming or adding native Windows APIs, together with
[Windows ownership layers](framework-reuse.md#windows-abstraction-layers).

## DLL facades and resource APIs

WinAPI nested types identify DLLs: USER32, KERNEL32, ADVAPI32, IPHLPAPI, PSAPI,
NTDLL and RSTRTMGR. Extend the matching existing DLL facade, not a domain-named
extern container. [USER32](../../../src/Sphere10.Framework.Windows/WinAPI/USER32.cs),
[ADVAPI32](../../../src/Sphere10.Framework.Windows/WinAPI/ADVAPI32.cs) and
[KERNEL32](../../../src/Sphere10.Framework.Windows/WinAPI/KERNEL32.cs) illustrate
window/environment, registry/token/service and process/file declarations.

Application/demo/screen code must not declare DllImport or LibraryImport or mix
WinAPI/WMI calls with presentation. Native layouts, callbacks, flags and handles
stay with their DLL binding. Verify pointer widths, Unicode entrypoints, calling
convention, boolean marshaling, SetLastError and ownership using official Microsoft
documentation and real callers.

Before adding an API, inspect the [Windows resource catalog](../../../src/Sphere10.Framework.Windows/README.md#windows-resource-abstractions).
Extend the existing instance domain utility exposed by Tools.Windows in its
owning file: RegistryUtil, ServiceUtil, ProcessSignaler, WindowsNetworkUtil,
WindowsDriveUtil and their peers. Keep discovery in WindowsTool.cs and source in
the owning domain folder; read the
[facade/folder rules](framework-utilities.md#windows-utility-facade-and-source-folders).
ServiceControlManagerSentry wraps sc.exe rather than native SCM operations; read
[Process Sentries](process-sentries.md). Do not add partial extension files.
Established WinAPI DLL partitions and designer-generated partials retain their
existing structure.

Typed resource APIs own native handles/buffers and return detached OS data or raw
errors. Their contracts have no display labels, converters/editors, navigation
parents or app serialization metadata. Application collectors compose these
middle APIs and own diagnostic projections, sampling/rates, aggregation/history,
availability text, management plans, reports and confirmation. A type used by
several screens of one app is not thereby reusable Framework code.

Keep dependency directions intact: Windows may depend on Core/provider packages,
not an executable, Application, Data or WinForms. Core's
[SnapshotDataSource<T>](../../../src/Sphere10.Framework/DataSource/SnapshotDataSource.cs)
owns generic query behavior; WinForms'
[CrudGridSnapshotController<T>](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudGridSnapshotController.cs)
owns visual binding/selection. Application export envelopes use the existing
Data CSV implementation without pulling it into native wrappers.

## Native ownership and failures

For rectangles crossing threads or windows with different DPI awareness, choose
one explicit coordinate space. [WindowsWindow.GetPhysicalBounds](../../../src/Sphere10.Framework.Windows/Windowing/WindowsWindow.cs)
returns verified outer bounds in physical desktop pixels;
`GetBounds` retains GetWindowRect's caller-virtualized coordinate contract.
[ThreadDpiAwarenessScope](../../../src/Sphere10.Framework.Windows/Windowing/ThreadDpiAwarenessScope.cs)
temporarily sets the native thread context and restores its exact previous value.
It supports synchronous disposal only: use `using`, dispose on the creating
thread in reverse nesting order, and never hold it across an `await`. Native
window creation inherits that context. [WindowHighlightScope](../../../src/Sphere10.Framework.Windows.Forms/Common/WindowHighlightScope.cs)
uses the same physical space for its per-monitor-aware HWND and cancels a second
automatic DPI resize. This keeps an app's existing DPI policy intact. See
Microsoft's [GetWindowRect](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowrect)
and [mixed-mode DPI](https://learn.microsoft.com/en-us/windows/win32/hidpi/high-dpi-improvements-for-desktop-applications)
contracts; a single scale multiplier cannot map a mixed-monitor desktop.

Use [Tools.Windows.Files](../../../src/Sphere10.Framework.Windows/Files/FileUtil.cs)
for named `$DATA` stream inventory, bounded byte reads, writes and deletion.
Keep the primary file distinct from a named stream; an empty name must never
mean delete the primary contents. The stream search owns its `FindClose` handle;
a returned FileStream belongs to its caller. Apps own encoding and confirmation.
See the [FindFirstStreamW contract](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-findfirststreamw).

Use [Tools.Windows.Computer](../../../src/Sphere10.Framework.Windows/System/ComputerUtil.cs)
for raw computer, firmware, board, CPU and OS resources. Native DTOs have no UI
labels, report text, sampling or application caches. Compose those in the app
and preserve unavailable fields. Dispose every WMI search, result collection and
resource through its own owning scope.

Keep raw native failures in the resource API, then project them at the app's
display/export boundary. Serializing an Exception directly can traverse unsupported
reflection objects such as TargetSite; exporting UI/resource graphs can also
introduce cycles. Export deliberate plain values (type, message, HResult or a
formatted app report), while the application logger retains the original error.

Use owned safe handles and disposable scopes. Native buffers, interior pointers
and borrowed source handles have different owners. Passive inspection must never
close the inspected source process/handle. An explicit force-release operation is
a separate mutation: require a complete captured process/handle identity,
revalidate that identity in the native abstraction and obtain application-level
confirmation. Keep it out of inventory reads and automatic refresh. Never retry
a destructive native close automatically: Windows `DUPLICATE_CLOSE_SOURCE` can
close its source even when the call reports failure. Report an indeterminate
outcome after failure or cancellation, and explain the remaining native handle
reuse race in the application confirmation. Copy returned data before releasing its buffer. Native IO belongs
off the UI thread with appropriate cancellation/bounds or an isolated helper for
calls that can block. A failed or partial read is not an empty successful inventory.
Generic APIs can accept a borrowed ILogger, but never resolve IApplication or an
app service to obtain it. Construction must not start monitoring, mutate the
machine, elevate or launch UI; actions occur at explicit method boundaries.

Capture native callback exceptions and rethrow after returning to managed code;
never throw across an unmanaged callback. Use [synchronization scopes](synchronization.md)
for shared state without holding a thread-affine lock over await, UI/provider
calls or callbacks. Async admission uses AsyncSynchronizedGate with disposable
scopes. Borrowed loggers remain externally owned; see [logging](logging.md).

## Host isolated workers through the consuming executable

[WindowsProcessWorkerHost](../../../src/Sphere10.Framework.Windows/Processes/WindowsProcessWorkerHost.cs)
supports an entry assembly/apphost pair or a standalone/single-file executable.
Never run a class-library DLL as the helper. Build arguments through ArgumentList,
and route FileHandleService.TryRunWorker and ProcessSignalService.TryRunWorker
before UI/framework startup. See the
[host example](../../../src/Sphere10.Framework.Windows/README.md#hosted-isolated-diagnostic-workers).

Bound helpers with deadlines, cancellation and owned-process cleanup. Kill only
the helper created by that operation, never the process being inspected. Preserve
partial data and avoid accumulating workers behind unfinished driver calls.
Console signaling uses a helper so attaching a console cannot change the GUI
process's console.

Run As Admin exit/startup coordination, parent verification and handoff protocol
belong in the app. Elevation creates a process; privilege adjustment/impersonation
does not elevate the current process. Preserve ordinary exit guards/finalizers,
abort on veto/cancellation and wait for actual parent exit before replacement UI.
Task Manager registration/restoration, expected prior state and stable product
markers are likewise app policy over registry/process primitives.

## Native account and security ownership

Use Windows.Security account/group/share resource methods for native reads and
changes. Migration copiers, ordered-host SID resolution/import, tree traversal,
retry/verification/cleanup policy and result reporting stay in the app plugin.
Raw single-host SID/name lookup belongs to SecurityTool.

Capture options before entering the worker. Native calls remain synchronous on
that worker with cooperative checkpoints between calls; a running NetAPI call
cannot be interrupted by Task cancellation.

For NetAPI enumeration, consume entriesRead rather than totalEntries and continue
ERROR_MORE_DATA with the returned resume handle. Group handles use pointer-sized
DWORD_PTR; NetUserEnum uses a 32-bit DWORD. Release each owning page, including
error/partial pages. Its interior pointers are borrowed. ConvertStringSidToSid
buffers use LocalFree, not NetApiBufferFree. Account names require Unicode bindings.
See [NetLocalGroupGetMembers](https://learn.microsoft.com/en-us/windows/win32/api/lmaccess/nf-lmaccess-netlocalgroupgetmembers),
[NetUserEnum](https://learn.microsoft.com/en-us/windows/win32/api/lmaccess/nf-lmaccess-netuserenum)
and [ConvertStringSidToSidW](https://learn.microsoft.com/en-us/windows/win32/api/sddl/nf-sddl-convertstringsidtosidw).

[SecurityDescriptorUtil](../../../src/Sphere10.Framework.Windows/Security/SecurityDescriptorUtil.cs), accessed through Tools.Windows.SecurityDescriptors,
matches exact owner/group/ACE SIDs and translates original identities once while
preserving unrelated sections. SDDL text replacement can match prefixes, miss
aliases and chain mappings. Distinguish unmapped accounts from lookup access/network
failures. Read-only searches own read handles; mutations request write access.

File migration must validate effective destinations and overlap/reparse ancestors
before cleaning. Transient credentials/passwords are never persisted or logged.
Impersonation/privilege scopes belong to the actual synchronous worker and restore
the prior effective identity. Do not alter the process token for one copy.
Check ERROR_NOT_ALL_ASSIGNED even when AdjustTokenPrivileges returns true:
[Microsoft contract](https://learn.microsoft.com/en-us/windows/win32/api/securitybaseapi/nf-securitybaseapi-adjusttokenprivileges).

## Identity and visual resources

[ProcessIdentity](../../../src/Sphere10.Framework.Windows/Processes/ProcessIdentity.cs)
captures PID plus creation time. Revalidate before operations; a PID alone does
not prove identity. [ProcessCommandService](../../../src/Sphere10.Framework.Windows/Processes/ProcessCommandService.cs)
and [WindowMessageSender](../../../src/Sphere10.Framework.Windows/Windowing/WindowMessageSender.cs)
retain native identity through dispatch. Main-window WM_QUIT posts to its owning
thread. Do not promise hard deadlines beyond a native method's timeout/flag contract.

Event-driven application watchers publish source events; UI consumers marshal
and coalesce updates instead of polling. Display filters/retention do not permit
dropping independently selected log output. Stop callbacks before disposing owned
watchers/resources.

[FileIconCache](../../../src/Sphere10.Framework.Windows/Shell/FileIconCache.cs)
loads local EXE/DLL/ICO resources on a worker, retains signed indices and lends
owned images with configurable size/capacity/loading budget. Process/application
identity adapters remain app-owned. Dispose controls before the cache.
[ExtractIconExW](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-extracticonexw)
bindings live in [WinAPI.SHELL32](../../../src/Sphere10.Framework.Windows/WinAPI/SHELL32.cs);
release each returned icon handle after copying pixels. An icon resource string
does not authorize executing or searching for a program.
