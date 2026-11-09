# Module configuration

The module contract that lets a library register its services before the framework builds the service provider.

`IModuleConfiguration` extends the core library's `ICoreModuleConfiguration`, which supplies `Priority`, `OnInitialize` and `OnFinalize`, with `RegisterComponents`. `Sphere10FrameworkBuilder.RegisterModules` first runs the builder's `AddServices` callbacks, then calls `RegisterComponents` on each module in descending `Priority` order. The framework calls `OnInitialize` in the same order once the provider exists. Derive library modules from `ModuleConfigurationBase` and override the hooks they need. The builder and the framework are described in the [Application guide](../README.md#-moduleconfiguration-pattern).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`IModuleConfiguration`](IModuleConfiguration.cs) | interface | `ICoreModuleConfiguration` with `RegisterComponents(IServiceCollection)` for service registration |
| [`ModuleConfigurationBase`](ModuleConfigurationBase.cs) | abstract class | `CoreModuleConfigurationBase` with an empty virtual `RegisterComponents` for library modules to override |
