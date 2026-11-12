# Lifecycle

Plugin initializers and finalizers that log startup and shutdown progress, and the lifetime that completes demonstration output before shutdown.

Each demonstration plugin registers two initializers and two finalizers through `AddInitializer<T>()` and `AddFinalizer<T>()` after `WithName(...)`. The Workspace plugin in `WinFormsDemoPlugin` uses the plugin and workspace tasks, and the Component plugin in `WinFormsComponentDemoPlugin` uses the component and integration tasks. Each task derives from `AsyncApplicationInitializerBase` or `AsyncApplicationFinalizerBase`, takes `IApplication` from the service provider, and logs before and after a cancellable `Task.Delay`. The application runs the initializers under the startup splash and the finalizers during shutdown.

`DemoLogOutputLifetime` is a singleton registered by the Component plugin. Screens and chart tiles in [Screens](../Screens/README.md) call its static `RegisterCurrent` to attach their log or chart output to a control, and `DemoComponentFinalizer` awaits `CompleteAsync()` so accepted writes finish before services are disposed. `RegisterCurrent` returns `null` when the framework has not started or the lifetime is not registered.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DemoComponentFinalizer`](DemoComponentFinalizer.cs) | class | Component plugin finalizer that completes registered demonstration output and logs around a 250 ms delay. |
| [`DemoComponentInitializer`](DemoComponentInitializer.cs) | class | Component plugin initializer that logs the preparation of controls and collections around a 250 ms delay. |
| [`DemoIntegrationFinalizer`](DemoIntegrationFinalizer.cs) | class | Component plugin finalizer that logs the shutdown of the integration demonstrations around a 250 ms delay. |
| [`DemoIntegrationInitializer`](DemoIntegrationInitializer.cs) | class | Component plugin initializer that logs the preparation of the integration demonstrations around a 250 ms delay. |
| [`DemoLogOutputLifetime`](DemoLogOutputLifetime.cs) | class | `LogOutputLifetime` whose static `RegisterCurrent` attaches a control's log or chart output to the registered singleton. |
| [`DemoPluginFinalizer`](DemoPluginFinalizer.cs) | class | Workspace plugin finalizer that logs around a 500 ms delay before framework services are disposed. |
| [`DemoPluginInitializer`](DemoPluginInitializer.cs) | class | Workspace plugin initializer that logs around a 500 ms delay while the startup splash is visible. |
| [`DemoWorkspaceFinalizer`](DemoWorkspaceFinalizer.cs) | class | Workspace plugin finalizer that logs the shutdown of screen hosting around a 250 ms delay. |
| [`DemoWorkspaceInitializer`](DemoWorkspaceInitializer.cs) | class | Workspace plugin initializer that logs the preparation of screen hosting around a 250 ms delay. |
