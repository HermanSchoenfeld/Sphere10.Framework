# Sphere10.Framework.Windows.Tests

NUnit tests for [Sphere10.Framework.Windows](../../src/Sphere10.Framework.Windows/README.md): Win32 interop layouts, processes, services, sessions, security, the registry, the shell, files, ETW tracing, hardware inventory and the Windows data sources. Most fixtures call the real Windows APIs against objects the test owns, such as a child process, a temporary file, a named pipe or a registry key, and check that cancelled or invalid requests fail before any native call.

## Run the tests

Run these commands from the repository root on Windows with the .NET 10 SDK. A machine with a .NET 11 SDK also builds and runs the `net11.0` target.

```powershell
dotnet test tests/Sphere10.Framework.Windows.Tests/Sphere10.Framework.Windows.Tests.csproj -c Release -p:BuildRevision=0
```

Fixture namespaces do not include the folder names, so select fixtures by class name. For example, run the process memory and thread fixtures:

```powershell
dotnet test tests/Sphere10.Framework.Windows.Tests/Sphere10.Framework.Windows.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~WindowsProcessMemory|FullyQualifiedName~ThreadUtilTests"
```

Skip the fixtures marked as integration tests:

```powershell
dotnet test tests/Sphere10.Framework.Windows.Tests/Sphere10.Framework.Windows.Tests.csproj -c Release -p:BuildRevision=0 --filter "TestCategory!=Integration"
```

Run an explicit native-capture case by name:

```powershell
dotnet test tests/Sphere10.Framework.Windows.Tests/Sphere10.Framework.Windows.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~EtwOwnedSessionTests.NativeOwnedSessionDeliversAnOwnedChildProcessStartAndStopsOnlyItsSession"
```

## Requirements

- Windows. The assembly declares `SupportedOSPlatform("windows")`.
- `WindowsSecurityTests` creates, edits and deletes local users and groups, so it needs an elevated process.
- Several fixtures read live machine state (services, processes, WMI, the event log, performance counters and display EDID). They report `Ignore` or `Inconclusive` when a provider, IPv6 or a privilege is unavailable instead of failing.
- The cases marked `[Explicit]` are excluded from ordinary runs. `EtwOwnedSessionTests` has two `NativeCapture` cases that start a short ETW session and need ETW capture privileges. `AppxPackageSentryTests` has one `NativeIntegration` case that reads the packages registered for the current account.

## Contents

### Project root

Fixtures for the general Windows utilities and native facades.

| Fixture | Purpose |
| --- | --- |
| [FileHandleReleaseTests](FileHandleReleaseTests.cs) | Remote file-handle release only proceeds when the captured process, object type, handle and access identity still match. |
| [MigrationSecurityScopeTests](MigrationSecurityScopeTests.cs) | Privilege and impersonation scopes, token privilege layout, credential validation and synchronous-only disposal. |
| [NamedPipeUtilTests](NamedPipeUtilTests.cs) | Passive named-pipe enumeration, cancellation and pipe-name validation. |
| [SecurityDescriptorUtilTests](SecurityDescriptorUtilTests.cs) | SID translation in security descriptors, preserved sections and registry descriptor handles. |
| [ServiceUtilTests](ServiceUtilTests.cs) | Read-only service configuration and state queries, image-path resolution, state waits, cancellation and driver enumeration. |
| [SessionUtilTests](SessionUtilTests.cs) | Terminal-services session queries, `WTSINFO` layout, identity matching, inventory and session resource scopes. |
| [VolumeCheckSentryTests](VolumeCheckSentryTests.cs) | The volume-check wrapper passes only the drive letter and rejects repair or dismount arguments. |
| [WindowsCatalogApiTests](WindowsCatalogApiTests.cs) | Network adapter, drive capacity and startup-location catalogues, and cancellation before enumeration. |
| [WindowsComputerTests](WindowsComputerTests.cs) | Computer identity, processor, operating system, firmware and product queries, including provider date parsing. |
| [WindowsDisplayTests](WindowsDisplayTests.cs) | EDID decoding and validation, display structure layouts, display inventory paging and the current desktop modes. |
| [WindowsDriveHardwareApiTests](WindowsDriveHardwareApiTests.cs) | Physical disk and partition queries, timeouts and cancellation before WMI. |
| [WindowsHookUtilTests](WindowsHookUtilTests.cs) | Mouse and keyboard hook factories keep supplied monitors borrowed and leave hooks uninstalled. |
| [WindowsMiddleLayerTests](WindowsMiddleLayerTests.cs) | Raw window, foreground, driver-tool and file-capture results carry no presentation data and reject unverified targets. |
| [WindowsMouseHookTests](WindowsMouseHookTests.cs) | Asynchronous and synchronous mouse-motion dispatch order, timestamps and restart behaviour. |
| [WindowsObjectTests](WindowsObjectTests.cs) | NT object-directory and symbolic-link reads, text budgets and access-right decoding. |
| [WindowsPowerInventoryTests](WindowsPowerInventoryTests.cs) | Power capability layout, power plans and settings, and sleep diagnostics. |
| [WindowsProcessApiTests](WindowsProcessApiTests.cs) | Process handle queries, Toolhelp enumeration, system counters, cancellation and named-pipe peers. |
| [WindowsSecurityTests](WindowsSecurityTest.cs) | `NTHost`, local users, local groups and membership through the NT security object model. |
| [WindowsUtilityFactoryTests](WindowsUtilityFactoryTests.cs) | Inventory, process, suspension and pipe-peer factories keep captures lazy and ownership independent. |

### Applications

Installed-application registration.

| Fixture | Purpose |
| --- | --- |
| [WindowsUninstallRegistrationTests](Applications/WindowsUninstallRegistrationTests.cs) | Uninstall registration reads and removal within a caller-owned registry container, and enumeration of the standard roots. |

### Audio

Wave playback.

| Fixture | Purpose |
| --- | --- |
| [WaveSoundPlayerTests](Audio/WaveSoundPlayerTests.cs) | Cache eviction by entry count and byte budget, stream ownership, invalid waves, stop, disposal and concurrent play calls. |

### DataSources

Windows inventory data sources.

| Fixture | Purpose |
| --- | --- |
| [WindowsInventoryDataSourceTests](DataSources/WindowsInventoryDataSourceTests.cs) | Service, process, endpoint, drive, interface, disk and partition sources page, filter and sort through `IDataSource`. |

### Diagnostics

Diagnostic utilities and dependency rules for the Windows library.

| Fixture | Purpose |
| --- | --- |
| [EnvironmentUtilTests](Diagnostics/EnvironmentUtilTests.cs) | Process environment variable reads, expansion, validation, edits and optimistic updates. |
| [EtwTraceUtilTests](Diagnostics/EtwTraceUtilTests.cs) | ETW property layout and read-only session enumeration. |
| [FileIconCacheTests](Diagnostics/FileIconCacheTests.cs) | Icon and package-logo loading, caching, fallbacks, scaling and cancellation. |
| [PktMonSentryTests](Diagnostics/PktMonSentryTests.cs) | Packet-monitor command arguments, read-only queries and size validation. |
| [ProcessCommandServiceTests](Diagnostics/ProcessCommandServiceTests.cs) | Process actions require a verified process identity and creation time. |
| [ProcessSignalServiceTests](Diagnostics/ProcessSignalServiceTests.cs) | Console-signal worker dispatch and request validation. |
| [WindowsDiagnosticsDependencyTests](Diagnostics/WindowsDiagnosticsDependencyTests.cs) | Reflection checks that the Windows library has no application or presentation dependencies and follows its facade and async conventions. |
| [WindowsEventLogQueryTests](Diagnostics/WindowsEventLogQueryTests.cs) | Structured event-log query construction and the historical reader. |
| [WindowsTcpCloseTests](Diagnostics/WindowsTcpCloseTests.cs) | TCP connection close identity and preflight checks on an owned loopback connection. |

### Drivers

Device access.

| Fixture | Purpose |
| --- | --- |
| [WindowsDeviceTests](Drivers/WindowsDeviceTests.cs) | Device probing, opening, control-code rejection and disposal. |

### Files

File, assembly and portable-executable inspection.

| Fixture | Purpose |
| --- | --- |
| [FileToolDiscoveryTests](Files/FileToolDiscoveryTests.cs) | File-tool factories, cancellation and handle and dump worker dispatch. |
| [ManagedAssemblyFixture](Files/ManagedAssemblyFixture.cs) | Small static methods that `ManagedAssemblyTests` decompiles to check direct-call references (helper, not a fixture). |
| [ManagedAssemblyTests](Files/ManagedAssemblyTests.cs) | Metadata, decompilation, BAML-to-XAML decoding, resource inventory and project export without loading the target. |
| [PortableExecutableTests](Files/PortableExecutableTests.cs) | PE parsing of exports, resources and unwind data, mapping bounds, revision checks and malformed images. |
| [WindowsAlternateStreamTests](Files/WindowsAlternateStreamTests.cs) | Creation, reading, update and deletion of NTFS alternate data streams, with name validation, read caps and cancellation. |
| [WindowsFileInspectionServiceTests](Files/WindowsFileInspectionServiceTests.cs) | File hash, version and signature trust inspection, and native trust structure layouts. |
| [WindowsFileTests](Files/WindowsFileTests.cs) | Native file copy with progress, fail-if-exists, cancellation and callback failures. |

### Misc

Unmanaged memory.

| Fixture | Purpose |
| --- | --- |
| [UnmanagedMemoryScopeTests](Misc/UnmanagedMemoryScopeTests.cs) | Unmanaged allocation scopes, Unicode string scopes and asynchronous disposal. |

### Networking

Network interfaces and endpoints.

| Fixture | Purpose |
| --- | --- |
| [WindowsNetworkUtilTests](Networking/WindowsNetworkUtilTests.cs) | Interface counter layout, typed endpoint reads, cancellation and native read failures. |

### Packages

Appx packages.

| Fixture | Purpose |
| --- | --- |
| [AppxPackageSentryTests](Packages/AppxPackageSentryTests.cs) | Package inventory parsing, removal command encoding and identity validation. |

### Processes

Process and thread inspection and control.

| Fixture | Purpose |
| --- | --- |
| [ManagedDiagnosticsTests](Processes/ManagedDiagnosticsTests.cs) | Managed runtime snapshots, heap bounds and native stack capture. |
| [ProcessSchedulingTests](Processes/ProcessSchedulingTests.cs) | Scheduling metadata and actions stay bound to the process creation time. |
| [ProcessSuspensionTests](Processes/ProcessSuspensionTests.cs) | Suspension owners release only their own suspend increments. |
| [ThreadUtilTests](Processes/ThreadUtilTests.cs) | Thread reads, priority and boost changes, wait chains and stale identity rejection. |
| [WindowsProcessInspectionTests](Processes/WindowsProcessInspectionTests.cs) | Token, mitigation, module header, page state and address resolution queries. |
| [WindowsProcessMemoryStreamTests](Processes/WindowsProcessMemoryStreamTests.cs) | Process memory stream reads across seeks and block boundaries, and unreadable memory errors. |
| [WindowsProcessMemoryTests](Processes/WindowsProcessMemoryTests.cs) | Memory maps, slices, the UTF-8 string scanner and bounds checks. |
| [WindowsProcessWorkerSessionTests](Processes/WindowsProcessWorkerSessionTests.cs) | Worker-process protocol lines, error bounds, admission failures and cleanup of owned children. |

### Registry

Registry access.

| Fixture | Purpose |
| --- | --- |
| [RegistryUtilEditTests](Registry/RegistryUtilEditTests.cs) | Raw registry values, renames, deferred tree reads, read limits and cancellation in a caller-owned key. |

### Resources

[Resources/Baml](Resources/Baml/README.md) holds the BAML, XAML and compiled fixture inputs used by `ManagedAssemblyTests`, with their provenance and rebuild recipe. The folder contains no fixtures.

### Scopes

Thread scopes.

| Fixture | Purpose |
| --- | --- |
| [ThreadGroupAffinityScopeTests](Scopes/ThreadGroupAffinityScopeTests.cs) | Processor-group affinity pinning, exact restoration and same-thread disposal. |

### Security

Accounts and security descriptors.

| Fixture | Purpose |
| --- | --- |
| [NativeAccountTests](Security/NativeAccountTests.cs) | NetAPI account enumeration paging, buffer release, user and share snapshots and creation validation. |
| [NetApiBufferScopeTests](Security/NetApiBufferScopeTests.cs) | NetAPI buffer scopes keep native buffers usable until release. |
| [SecurityAccountLookupTests](Security/SecurityAccountLookupTests.cs) | Account name and SID lookups for the current user, missing accounts and well-known SIDs. |
| [SecurityToolDescriptorTests](Security/SecurityToolDescriptorTests.cs) | File, directory and registry descriptor writes preserve ownership and write only requested sections. |

### Shell

Windows shell integration.

| Fixture | Purpose |
| --- | --- |
| [KnownFolderPathTests](Shell/KnownFolderPathTests.cs) | Known-folder reads and redirection validation on an STA worker. |
| [ShellAsyncTests](Shell/ShellAsyncTests.cs) | Shell operations on a dedicated STA worker, cancellation and failure propagation. |
| [ShellFileAssociationTests](Shell/ShellFileAssociationTests.cs) | File association lookups, identifier and verb validation, and bounded native buffers. |
| [ShellRecycleItemTests](Shell/ShellRecycleItemTests.cs) | Recycle-bin item deletion validation and cancellation. |
| [ShellUtilityTests](Shell/ShellUtilityTests.cs) | Known folders, recycle-bin queries, shell namespace enumeration, item commands and shell launch validation. |

### Startup

Startup registrations.

| Fixture | Purpose |
| --- | --- |
| [WindowsStartupRegistrationTests](Startup/WindowsStartupRegistrationTests.cs) | Startup sources, scheduled-task triggers, command parsing and shortcut inspection. |

### System

Hardware, power and performance counters.

| Fixture | Purpose |
| --- | --- |
| [ComputerHardwareInventoryTests](System/ComputerHardwareInventoryTests.cs) | Hardware inventory, memory capacity, monitor identifiers and driver dates. |
| [ComputerPowerTests](System/ComputerPowerTests.cs) | Asynchronous power adapters and shutdown-reason validation. |
| [WindowsPerformanceQueryTests](System/WindowsPerformanceQueryTests.cs) | PDH scalar and wildcard counters, time conversion, unavailable counters and native layouts. |

### Tracing

ETW sessions and event decoding.

| Fixture | Purpose |
| --- | --- |
| [EtwCpuProfilingTests](Tracing/EtwCpuProfilingTests.cs) | CPU sample payload decoding, stack timestamps and system-logger admission. |
| [EtwOwnedSessionTests](Tracing/EtwOwnedSessionTests.cs) | Owned ETW session start, commands and stop, with explicit native capture cases. |
| [EtwTraceConsumerTests](Tracing/EtwTraceConsumerTests.cs) | Event ABI layout, consumer cancellation and manifest payload decoding. |
| [EtwTraceExtendedDataTests](Tracing/EtwTraceExtendedDataTests.cs) | Extended stack data decoding and copying of callback-owned pointers. |

### Windowing

Window and DPI scopes.

| Fixture | Purpose |
| --- | --- |
| [ThreadDpiAwarenessScopeTests](Windowing/ThreadDpiAwarenessScopeTests.cs) | Nested thread DPI-awareness scopes restore the calling context and reject wrong-thread or asynchronous disposal. |
