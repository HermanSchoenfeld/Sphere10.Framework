# Disposables

Base classes and small adapters that implement `IDisposable` and `IAsyncDisposable` together, with a single cleanup hook in the direction the resource needs.

Derive from `SyncDisposable` when managed cleanup is synchronous, or from `AsyncDisposable` when it is asynchronous. `Disposable` declares both `DisposeInternal()` and `DisposeInternalAsync()`, and each direction-specific base implements the other hook as an adapter. `SyncDisposable` runs the synchronous hook on a worker for `DisposeAsync()`. `AsyncDisposable` blocks `Dispose()` on the asynchronous hook through `Task.Run`, outside the caller's synchronization context. Unmanaged cleanup goes in `FreeUnmanagedResources()`, which runs once after managed cleanup.

`Disposables` is a list of `IDisposable` items that disposes them in insertion order. It accepts plain actions and asynchronous callbacks through `ActionDisposable` and `TaskDisposable`, and it can ignore item failures. `DisposableResource` gives a derived type a protected `Disposables` collection that it disposes with itself. `Disposables.None` is one shared instance, so never add items to it. Scope types that release work at the end of a `using` block are in [Scopes](../Scopes/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ActionDisposable`](ActionDisposable.cs) | class | Invokes a supplied action when disposed. |
| [`AsyncDisposable`](AsyncDisposable.cs) | abstract class | Base for asynchronous managed cleanup, whose synchronous `Dispose()` waits on it outside the caller's synchronization context. |
| [`Disposable`](Disposable.cs) | abstract class | Coordinates synchronous or asynchronous managed cleanup with once-only unmanaged cleanup. |
| [`DisposableResource`](DisposableResource.cs) | class | Owns a protected `Disposables` collection and disposes it in the direction the caller chooses. |
| [`Disposables`](Disposables.cs) | class | List of disposable items, actions and asynchronous callbacks that are disposed together, optionally ignoring failures. |
| [`SyncDisposable`](SyncDisposable.cs) | abstract class | Base for synchronous managed cleanup, whose `DisposeAsync()` runs it on a worker. |
| [`TaskDisposable`](TaskDisposable.cs) | class | Awaits a supplied asynchronous action when disposed. |
