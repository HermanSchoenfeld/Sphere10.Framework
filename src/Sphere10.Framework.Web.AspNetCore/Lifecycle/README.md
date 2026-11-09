# Lifecycle

The internal initializer that ends the Sphere10 Framework when the ASP.NET Core host stops.

`AspNetCoreLifecycleMonitorInitializer` is an internal `SyncApplicationInitializerBase`. Its `Initialize` method registers `Sphere10Framework.Instance.EndFramework` on `IHostApplicationLifetime.ApplicationStopped`. The project's root `ModuleConfiguration` registers it with `AddInitializer`, so a host gets this behaviour only when it adds that module in the `AddSphere10Framework` callback described in [Extensions](../Extensions/README.md). Without the module, nothing ends the Framework when the host stops.

## Contents

This folder declares no public types. Its only type is the internal `AspNetCoreLifecycleMonitorInitializer` in [AspNetCoreLifecycleMonitorInitializer.cs](AspNetCoreLifecycleMonitorInitializer.cs).
