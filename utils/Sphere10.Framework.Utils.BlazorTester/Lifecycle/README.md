# Plugin Lifecycle Demos

Demonstrates plugin-owned startup and shutdown tasks that run once per browser circuit's application.

`DemoPluginInitializer` derives from `AsyncApplicationInitializerBase` and `DemoPluginFinalizer` from `AsyncApplicationFinalizerBase`. Each records its plugin name in the scoped `WorkspaceStatus` from [Application](../Application/README.md), and the workspace footer shows the number of initialized plugins. All four demo plugin definitions register one of each with a plugin name. Finalization runs when the application exits or the circuit closes. The [project README](../README.md#plugin-initialization-and-shutdown) describes the lifecycle and its tests.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DemoPluginFinalizer`](DemoPluginFinalizer.cs) | class | Application finalizer that records its plugin's shutdown in `WorkspaceStatus` |
| [`DemoPluginInitializer`](DemoPluginInitializer.cs) | class | Application initializer that records its plugin's startup in `WorkspaceStatus` |
