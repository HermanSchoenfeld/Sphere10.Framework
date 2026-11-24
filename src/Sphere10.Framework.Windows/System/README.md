# System

Reads computer identity, firmware, hardware, operating-system and performance data, computes a hardware machine identifier, reads power plans and capabilities, and requests shutdown, restart, logoff, lock, sleep and hibernate.

`Tools.Windows.Computer` is the entry point. Its hardware queries delegate to `WindowsComputer`, which reads WMI classes such as `Win32_BIOS`, `Win32_BaseBoard`, `Win32_Processor`, `Win32_PhysicalMemory`, `Win32_PnPEntity` and `Win32_Printer` into detached records. Monitor identities come from the `root\wmi` namespace. `GetProductInfo` reads the installed Windows product through `WindowsProductKeys`: its name, edition, feature update, product ID and build from the registry, the installed key and the key kept from before an upgrade decoded from their `DigitalProductId` values with the edition and channel beside them, and the manufacturer's key from the firmware's ACPI MSDM table. A key Windows does not keep, as after a volume activation, is null. `GetSystemCounters` reads raw memory, commit, kernel-pool, process, thread, handle and CPU tick counts through `WindowsSystemInformation`. `Rename` changes the computer name only when the current name matches the expected one. `Shutdown`, `Restart`, `Logoff`, `Lock`, `Sleep` and `Hibernate` send requests that the user or another application can still veto. Every query has an async variant, and the caller owns sampling and aggregation.

`WindowsMachineIdentifierGenerator` implements the core `IMachineIdentifierGenerator` from the hardware a `MachineIdentitySources` selection names: the baseboard manufacturer, product and serial number, each processor's `ProcessorId`, the BIOS serial number, the serial number of the disk that holds the Windows system drive, and the MAC addresses of network adapters installed on the PCI or PCI Express bus (never USB, virtual or software adapters). `MachineIdentitySources.Default` is the baseboard, processor and BIOS. Each value is trimmed and upper-cased, empty values and firmware placeholders such as "To be filled by O.E.M.", "Default string", "None" and all-zero or all-F identifiers are dropped, and the remaining values are written as ordinally sorted `Source=Value` lines and hashed with SHA-256 through `Hashers`. `Generate()` returns the 64 upper-case hexadecimal digits, caches them for the life of the instance, and throws `InvalidOperationException` when no source yields a value. The same hardware and selection always give the same identifier, so compare only identifiers generated with the same selection. `BIOSMachineIdentifier` is the other implementation: it identifies the machine by the SMBIOS system UUID (`Win32_ComputerSystemProduct.UUID`) alone, composed, hashed and cached the same way, and throws `InvalidOperationException` when the firmware reports none or a placeholder. This library has no module configuration, because it must not depend on `Sphere10.Framework.Application`; the `Sphere10.Framework.Windows.Forms` module registers a singleton `WindowsMachineIdentifierGenerator` with the default sources as `IMachineIdentifierGenerator` unless the host registered its own, such as `BIOSMachineIdentifier`.

```csharp
IMachineIdentifierGenerator generator = new WindowsMachineIdentifierGenerator(MachineIdentitySources.Default | MachineIdentitySources.SystemDisk);
var machineId = generator.Generate();
var firmwareId = new BIOSMachineIdentifier().Generate();
```

`Tools.Windows.PerformanceCounters` opens a `WindowsPerformanceQuery` over English PDH counter paths. The query owns its PDH handle, returns raw `WindowsPerformanceCounterSample` values and computes a formatted value from a current and an earlier sample, so it keeps no history of its own. `AlwaysOnScope` keeps the system or display awake with `SetThreadExecutionState` until it is disposed on the same thread. Display paths and EDID data are in the [Display](../Display/README.md) domain, and logical drives and disks are in the [Storage](../Storage/README.md) domain.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`WindowsManagementQuery`](WindowsManagementQuery.cs) | class | Owns a native WMI connection on one thread; created through `Tools.Windows.COM.CreateManagementQuery` |
| [`WindowsManagementObject`](WindowsManagementObject.cs) | class | Owns a WMI object, typed property values and method parameters within its query lifetime |
| [`WindowsIpmi`](WindowsIpmi.cs) | static class | Discovers Microsoft_IPMI provider paths and exchanges IPMI request/response byte arrays through native WMI |
| [`BIOSMachineIdentifier`](BIOSMachineIdentifier.cs) | class | `IMachineIdentifierGenerator` that hashes the SMBIOS system UUID into a cached upper-case SHA-256 hexadecimal identifier |
| [`MachineIdentitySources`](MachineIdentitySources.cs) | enum | Flags selecting the hardware values of a machine identifier: baseboard, processor, BIOS, system disk and PCI network adapters, with `Default` |
| [`WindowsMachineIdentifierGenerator`](WindowsMachineIdentifierGenerator.cs) | class | `IMachineIdentifierGenerator` that hashes the selected hardware values into a cached upper-case SHA-256 hexadecimal identifier |
| [`AlwaysOnScope`](AlwaysOnScope.cs) | struct | Thread-bound scope that requests system or display availability through `SetThreadExecutionState` and restores the prior state |
| [`Tools.Windows.Computer`](ComputerUtil.cs) | static class | `Tools.Windows.Computer` identity, firmware, hardware, counter and power queries, power data-source factories, rename and power-state requests |
| [`Tools.Windows.PerformanceCounters`](PerformanceCounterUtil.cs) | static class | `Tools.Windows.PerformanceCounters` factory that opens caller-owned PDH queries over English counter paths |
| [`WindowsBaseboardInfo`](WindowsBaseboardInfo.cs) | record | Baseboard manufacturer, product, version and serial number |
| [`WindowsBiosInfo`](WindowsBiosInfo.cs) | record | BIOS manufacturer, version, serial number and release date |
| [`WindowsComputer`](WindowsComputer.cs) | static class | WMI queries for computer, product, firmware, processor, memory, enclosure, video, PnP device, monitor and printer records, and computer rename |
| [`WindowsComputerInfo`](WindowsComputerInfo.cs) | record | Computer name, domain, domain membership, manufacturer and model |
| [`WindowsEnclosureInfo`](WindowsEnclosureInfo.cs) | record | System enclosure manufacturer, model, version, serial number, asset tag and chassis types |
| [`WindowsMemoryArrayInfo`](WindowsMemoryArrayInfo.cs) | record | Physical memory array location, use, device slots and maximum capacity |
| [`WindowsMemoryModuleInfo`](WindowsMemoryModuleInfo.cs) | record | One memory module with locator, bank, manufacturer, part and serial number, capacity, clock, type, form factor and widths |
| [`WindowsMonitorInfo`](WindowsMonitorInfo.cs) | record | One WMI monitor identity with manufacturer, product code, serial number, name, active flag and manufacture date |
| [`WindowsOperatingSystemInfo`](WindowsOperatingSystemInfo.cs) | record | Operating system name, version, build, architecture, serial number, install and boot times and system directories |
| [`WindowsPerformanceCounterSample`](WindowsPerformanceCounterSample.cs) | record | One raw PDH sample with counter path, instance, type, status, timestamps, raw values and time base |
| [`WindowsPerformanceQuery`](WindowsPerformanceQuery.cs) | class | Disposable PDH query that reads raw samples of its counters and calculates values from sample pairs |
| [`WindowsPlugAndPlayDeviceInfo`](WindowsPlugAndPlayDeviceInfo.cs) | record | One present PnP entity with ID, name, class, service, status, problem code and hardware and compatible IDs |
| [`WindowsPowerCapabilities`](WindowsPowerCapabilities.cs) | record | Whether the system allows suspend, hibernate and shutdown |
| [`WindowsPrinterInfo`](WindowsPrinterInfo.cs) | record | One printer with name, driver, port, status and default, network, local, shared and offline flags |
| [`WindowsProductInfo`](WindowsProductInfo.cs) | record | The installed Windows product name, edition, feature update, product ID, build and bitness, with its installed, original and firmware keys |
| [`WindowsProductKeyInfo`](WindowsProductKeyInfo.cs) | record | One registry product key with its product ID, edition and channel; the key is null when Windows keeps none |
| [`WindowsProductKeys`](WindowsProductKeys.cs) | static class | Reads `WindowsProductInfo` from the registry and the firmware's ACPI MSDM table, and decodes a `DigitalProductId` value's key |
| [`WindowsProcessorInfo`](WindowsProcessorInfo.cs) | record | One processor with ID, name, manufacturer, cores, logical processors and maximum clock |
| [`WindowsSystemCounters`](WindowsSystemCounters.cs) | record | Raw system memory, commit, kernel-pool, process, thread and handle counts and CPU idle, kernel and user ticks |
| [`WindowsSystemInformation`](WindowsSystemInformation.cs) | static class | Logical processor count across all groups and raw system counter reads |
| [`WindowsSystemProductInfo`](WindowsSystemProductInfo.cs) | record | Computer system product name, vendor, version, identifying number and UUID |
| [`WindowsVideoControllerInfo`](WindowsVideoControllerInfo.cs) | record | One video controller with name, processor, driver version and date, memory, resolution and refresh rate |

### Power

The [Power README](Power/README.md) describes the power inventories in detail. `Tools.Windows.Computer` reads power plans, the settings of one plan with their AC and DC values and policy overrides, and sleep and wake diagnostics. It also changes plans: it writes a setting's AC or DC value after checking it against the setting's values or range, activates, duplicates, renames and deletes plans. The three data sources capture once on load and then page the detached rows. `PowerCfgSentry` runs `powercfg.exe` for the read-only reports in `PowerCfgReport` and returns its localized output unparsed.

| Type | Kind | Purpose |
|---|---|---|
| [`PowerCfgReport`](Power/PowerCfgReport.cs) | enum | Read-only `powercfg` reports: requests, wake timers, last wake, available sleep states and wake-armed devices |
| [`PowerCfgSentry`](Power/PowerCfgSentry.cs) | class | `ProcessSentry` that runs one `PowerCfgReport` with a deadline and returns the raw output and exit status |
| [`WindowsPowerDiagnosticDataSource`](Power/WindowsPowerDiagnosticDataSource.cs) | class | Read-only data source over sleep, wake and power capability diagnostics |
| [`WindowsPowerDiagnosticInfo`](Power/WindowsPowerDiagnosticInfo.cs) | record | One power diagnostic value with category, name, value, source and read error |
| [`WindowsPowerPlanDataSource`](Power/WindowsPowerPlanDataSource.cs) | class | Read-only data source over the installed power plans |
| [`WindowsPowerPlanInfo`](Power/WindowsPowerPlanInfo.cs) | record | One power plan with GUID, name, description and active flag |
| [`WindowsPowerSettingDataSource`](Power/WindowsPowerSettingDataSource.cs) | class | Read-only data source over the settings of one power plan |
| [`WindowsPowerSettingChoice`](Power/WindowsPowerSettingChoice.cs) | record | One enumerated value of a power setting with its localized name |
| [`WindowsPowerSettingInfo`](Power/WindowsPowerSettingInfo.cs) | record | One power setting with subgroup, AC and DC values and labels, enumerated choices, units and range, hidden flag and policy overrides |
| [`WindowsPowerSource`](Power/WindowsPowerSource.cs) | enum | The AC (plugged in) or DC (on battery) value of a power setting |
