# Loadable

The `ILoadable` contract for objects that load their state on demand, with base classes that pair synchronous and asynchronous loading.

An `ILoadable` exposes `RequiresLoad`, `Load()`, `LoadAsync(cancellationToken)` and the `Loading` and `Loaded` events. `LoadableBase` implements both entry points as notify, load, notify, and leaves `LoadInternal` and `LoadInternalAsync` to the subclass. Choose the base by where the real work lives. `SyncLoadableBase` is for synchronous work and runs `LoadInternal` through `Task.Run` for the asynchronous path. `AsyncLoadableBase` is for asynchronous work and runs the synchronous path as sync-over-async on a worker. Each of them seals the notification hooks of the other path, so override only the hooks of the path you implement.

`LoadableBase.LoadAsync` raises its events through `Task.Run`, so asynchronous `Loading` and `Loaded` handlers run on a thread-pool thread. `SuppressNotifications` turns both events and the `OnLoading` and `OnLoaded` hooks off, and `CheckLoaded` throws while `RequiresLoad` is true. [Saveable](../Saveable/README.md) is the mirror contract for saving, and [Persistable](../Persistable/README.md) combines the two. Object stores such as [ObjectStream](../ObjectStream/README.md) and [ObjectSpaces](../ObjectSpaces/README.md) derive from `SyncLoadableBase`.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AsyncLoadableBase`](AsyncLoadableBase.cs) | abstract class | `LoadableBase` for asynchronous implementations whose synchronous `Load` and hooks wait on their asynchronous counterparts on a worker. |
| [`ILoadable`](ILoadable.cs) | interface | Contract with `RequiresLoad`, `Load`, cancellable `LoadAsync` and the `Loading` and `Loaded` events. |
| [`LoadableBase`](LoadableBase.cs) | abstract class | Implements `Load` and `LoadAsync` around abstract `LoadInternal` and `LoadInternalAsync`, with notification hooks, `SuppressNotifications` and `CheckLoaded`. |
| [`LoadableDecorator`](LoadableDecorator.cs) | class | `LoadableDecorator<ILoadable>` for wrapping any loadable object. |
| [`LoadableDecorator<TLoadableImpl>`](LoadableDecorator.cs) | class | Forwards `RequiresLoad`, `Load`, `LoadAsync` and both events to an inner `ILoadable`, with virtual members for overriding. |
| [`NoOpLoadable`](NoOpLoadable.cs) | class | Loadable whose `Load` does nothing, whose `RequiresLoad` is false and which never raises events, exposed as `NoOpLoadable.Instance`. |
| [`SyncLoadableBase`](SyncLoadableBase.cs) | abstract class | `LoadableBase` for synchronous implementations whose `LoadInternalAsync` and asynchronous hooks run the synchronous members through `Task.Run`. |
