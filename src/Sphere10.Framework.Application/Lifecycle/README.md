# Initialization and finalization tasks

Startup and shutdown task contracts with synchronous and asynchronous bases, the `Tools.Application` task runners, progress reporting, automatic-start helpers and the application version type.

An initializer implements `IApplicationInitializer` and a finalizer implements `IApplicationFinalizer`. Both contracts declare a synchronous and an asynchronous method. Derive a synchronous task from `SyncApplicationInitializerBase` or `SyncApplicationFinalizerBase` and override only `Initialize` or `Finalize`, and the base runs it on a worker for the asynchronous call. Derive an asynchronous task from `AsyncApplicationInitializerBase` or `AsyncApplicationFinalizerBase` and override only the asynchronous method, and the base blocks on a worker for the synchronous call. Register process tasks with `AddInitializer<T>()` and `AddFinalizer<T>()` from [Extension methods](../Extensions/README.md), and plugin tasks through the plugin builder described in [Shared application UI model](../UI/README.md).

`Tools.Application.ExecuteInitializers` runs the sequential initializers in ascending `Priority` order, then the parallelizable ones, and reports each step through a `ProgressHandler`. `ExecuteFinalizers` runs finalizers in registration order, attempts every one and throws an `AggregateException` of the failures. Both have awaited forms. The Application module registers `IncrementUsageByOneInitializer`, and on Windows it adds `EnsureSystemDataDirGloballyAccessibleInitializer` when `Sphere10FrameworkOptions.EnsureSystemDataDirGloballyAccessible` is set. The [Application guide](../README.md#application-initialization-and-shutdown) describes the process and application lifecycles.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ApplicationFinalizerBase`](ApplicationFinalizerBase.cs) | abstract class | Finalizer base with `Priority` 100 and abstract `Finalize` and `FinalizeAsync` |
| [`ApplicationInitializerBase`](ApplicationInitializerBase.cs) | abstract class | Initializer base with `Priority` 100, `Parallelizable` false and abstract `Initialize` and `InitializeAsync` |
| [`ApplicationVersion`](Version.cs) | class | Major, minor, build and revision version with a `ProductDistribution`, `Parse` and `TryParse` of one, two or four parts, comparison operators and the `G`, `F`, `S` and `D` formats |
| [`AsyncApplicationFinalizerBase`](AsyncApplicationFinalizerBase.cs) | abstract class | Finalizer that implements `FinalizeAsync` and blocks on it on a worker for `Finalize` |
| [`AsyncApplicationInitializerBase`](AsyncApplicationInitializerBase.cs) | abstract class | Initializer that implements `InitializeAsync` and blocks on it on a worker for `Initialize` |
| [`EnsureSystemDataDirGloballyAccessibleInitializer`](EnsureSystemDataDirGloballyAccessibleInitializer.cs) | class | Windows initializer with `Priority` -1 that creates `{SystemDataDir}` and grants everyone modify access when the folder does not exist |
| [`IApplicationFinalizer`](IApplicationFinalizer.cs) | interface | Shutdown task with `Priority`, `Finalize` and `FinalizeAsync` |
| [`IApplicationInitializer`](IApplicationInitializer.cs) | interface | Startup task with `Priority`, `Parallelizable`, `Initialize` and `InitializeAsync` |
| [`IAutoRunServices`](IAutoRunServices.cs) | interface | Platform service that checks, sets and removes an application's automatic start for an `AutoRunType` scope |
| [`IncrementUsageByOneInitializer`](IncrementUsageByOneInitializer.cs) | class | Initializer with `Priority` 1 that increments the user and system usage counters through `IProductUsageServices` |
| [`ProgressHandler`](ProgressHandler.cs) | delegate | Progress callback with a title, the current task, completed and total counts and a percentage from 0 to 100 |
| [`SyncApplicationFinalizerBase`](SyncApplicationFinalizerBase.cs) | abstract class | Finalizer that implements `Finalize` and runs it on a worker for `FinalizeAsync` |
| [`SyncApplicationInitializerBase`](SyncApplicationInitializerBase.cs) | abstract class | Initializer that implements `Initialize` and runs it on a worker for `InitializeAsync` |
| [`Tools.Application`](ApplicationTool.cs) | tools | Initializer and finalizer runners in synchronous and awaited forms, plus settings value serialization and population |
