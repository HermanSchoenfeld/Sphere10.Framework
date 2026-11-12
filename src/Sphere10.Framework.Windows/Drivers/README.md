# Drivers

Lists installed driver packages, resolves driver file paths and opens kernel device handles for control codes.

`Tools.Windows.Drivers` is the entry point. `GetDriverPackages` and `GetDriverPackagesAsync` return `WindowsDriverPackageInfo` rows with device, class, version, provider, signing and INF details, and accept an optional timeout. `ResolveInfPath` and `ResolveBinaryPath` turn a registered INF name or driver image path into a validated local path. `Tools.Windows.Drivers` delegates to `DriverManager`, which holds the query and the static path rules.

`WindowsDevice` owns an opened device handle such as `\\.\Name` and sends `DeviceIoControl` codes through `Control` or `TryControl`. `ProbeAccess` reports the Windows error of opening a path without keeping the handle. Opening a device neither installs nor starts a driver. Device IO blocks the calling thread, so dispatch it from a worker. The caller owns the control codes, buffers and validation of the driver's replies. Service-level driver inventories are in the [Services](../Services/README.md) domain.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DriverManager`](DriverManager.cs) | class | Queries installed driver packages and converts INF names and registered driver image paths to local file paths |
| [`WindowsDevice`](WindowsDevice.cs) | class | Disposable owner of an opened kernel device handle that dispatches IO control codes and probes access |
| [`WindowsDriverPackageInfo`](WindowsDriverPackageInfo.cs) | record | One installed driver package: device name and ID, class, version, provider, signer, INF name and date |
| [`Tools.Windows.Drivers`](WindowsDriverUtil.cs) | static class | `Tools.Windows.Drivers` facade over `DriverManager` for package queries and path resolution |
