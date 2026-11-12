# Services

Queries, configures and controls Windows services and drivers through the Service Control Manager, and wraps `sc.exe` and NSSM for installation.

`Tools.Windows.Services` reads services and kernel drivers as `WindowsServiceInfo` records, each with configuration, status, dependencies and separate error codes for fields that could not be read. `ReadConfiguration` returns the `QueryServiceConfig` data as `WindowsServiceConfiguration`, and `ReadAdvancedConfiguration` returns the optional `QueryServiceConfig2` sections (delayed start, SID type, required privileges, failure actions and triggers) as `WindowsServiceAdvancedConfiguration`. It also changes the start mode, starts, stops, pauses and resumes a service within a timeout, and waits for a status without sending a control. `ResolveExecutablePath` extracts an explicit absolute executable path from a service command line without running it or searching `PATH`. `TryGetServiceController` returns a `ServiceController` that the caller owns.

`WindowsServiceDataSource` is a read-only service or driver inventory that loads once and then pages its rows, and it never starts, stops or configures a service. `ServiceControlManagerSentry` runs `sc.exe` to create and delete services, and `NssmSentry` runs `nssm.exe` from `PATH` to install, remove, start and stop an application as a service. `ServiceStatus` is a standalone status enum that no type in this library uses. Driver packages and device handles are in the [Drivers](../Drivers/README.md) domain.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`NssmSentry`](NssmSentry.cs) | class | `ProcessSentry` for `nssm.exe` that installs, removes, starts and stops services and checks whether NSSM can run |
| [`ServiceControlManagerSentry`](ServiceControlManagerSentry.cs) | class | `ProcessSentry` for `sc.exe` that creates a service with a start type and removes a service |
| [`ServiceStatus`](ServiceStatus.cs) | enum | Coarse service state from not installed through started, stopped or error |
| [`Tools.Windows.Services`](ServiceUtil.cs) | static class | `Tools.Windows.Services` service and driver reads, configuration queries, start-mode changes, state control and status waits |
| [`WindowsServiceAdvancedConfiguration`](WindowsServiceAdvancedConfiguration.cs) | record | Optional SCM configuration: delayed start, SID type, preshutdown timeout, launch protection, required privileges, failure actions, triggers and read issues |
| [`WindowsServiceConfiguration`](WindowsServiceConfiguration.cs) | record | `QueryServiceConfig` data: names, binary path, account, type, start mode, error control, load-order group, tag and dependencies |
| [`WindowsServiceConfigurationIssue`](SpecialTypes.cs) | record | An optional configuration level that could not be read, with its native error code |
| [`WindowsServiceDataSource`](WindowsServiceDataSource.cs) | class | Read-only data source over the service or the driver inventory |
| [`WindowsServiceFailureAction`](SpecialTypes.cs) | record | One service failure action type and its delay |
| [`WindowsServiceInfo`](WindowsServiceInfo.cs) | record | One service or driver with configuration, status, PID, exit code, control flags, dependencies and per-section error codes |
| [`WindowsServiceTrigger`](SpecialTypes.cs) | record | One service trigger with type, action, subtype GUID and data items |
| [`WindowsServiceTriggerData`](SpecialTypes.cs) | record | One trigger data item's type and raw bytes |
