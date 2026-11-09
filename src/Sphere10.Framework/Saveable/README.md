# Saveable

The `ISaveable` contract for objects that write their state on demand, with base classes that pair synchronous and asynchronous saving.

An `ISaveable` exposes `RequiresSave`, `Save()`, `SaveAsync(cancellationToken)` and the `Saving` and `Saved` events. `SaveableBase` implements both entry points as notify, save, notify, and leaves `SaveInternal` and `SaveInternalAsync` to the subclass. `SyncSaveableBase` suits synchronous work and runs `SaveInternal` through `Task.Run` for the asynchronous path. `AsyncSaveableBase` suits asynchronous work and runs the synchronous path as sync-over-async on a worker. Each seals the notification hooks of the other path.

`SaveableBase.RequiresSave` is a settable flag that the base never clears, so the subclass decides when it is dirty. Asynchronous `Saving` and `Saved` handlers run on a thread-pool thread, and `SuppressNotifications` turns the events and hooks off. [Loadable](../Loadable/README.md) is the mirror contract, and [Persistable](../Persistable/README.md) combines the two.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AsyncSaveableBase`](AsyncSaveableBase.cs) | abstract class | `SaveableBase` for asynchronous implementations whose synchronous `Save` and hooks wait on their asynchronous counterparts on a worker. |
| [`ISaveable`](ISaveable.cs) | interface | Contract with `RequiresSave`, `Save`, cancellable `SaveAsync` and the `Saving` and `Saved` events. |
| [`SaveableBase`](SaveableBase.cs) | abstract class | Implements `Save` and `SaveAsync` around abstract `SaveInternal` and `SaveInternalAsync`, with a settable `RequiresSave`, notification hooks and `SuppressNotifications`. |
| [`SaveableDecorator`](SaveableDecorator.cs) | class | `SaveableDecorator<ISaveable>` for wrapping any saveable object. |
| [`SaveableDecorator<TSaveableImpl>`](SaveableDecorator.cs) | class | Forwards `RequiresSave`, `Save`, `SaveAsync` and both events to an inner `ISaveable`, with virtual members for overriding. |
| [`SyncSaveableBase`](SyncSaveableBase.cs) | abstract class | `SaveableBase` for synchronous implementations whose `SaveInternalAsync` and asynchronous hooks run the synchronous members through `Task.Run`. |
