# Persistable

The `IPersistable` contract for objects that both load and save their state, with sync, async and decorator bases.

`IPersistable` is `ILoadable` plus `ISaveable` and adds no members. `PersistableBase` derives from `LoadableBase` in [Loadable](../Loadable/README.md) and adds the saving half from [Saveable](../Saveable/README.md), so a subclass implements `LoadInternal`, `LoadInternalAsync`, `SaveInternal` and `SaveInternalAsync`. `SyncPersistableBase` derives the asynchronous members of both halves from the synchronous ones through `Task.Run`, and `AsyncPeristableBase` derives the synchronous members from the asynchronous ones as sync-over-async. The class name `AsyncPeristableBase` is spelled without the second "s" in code.

`PersistableDecorator<TPersistableImpl>` extends `LoadableDecorator<TPersistableImpl>` and forwards the saving members too. Repositories and transactional scopes that build on these contracts are in [Repository](../Repository/README.md) and [Transactions](../Transactions/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AsyncPeristableBase`](AsyncPeristableBase.cs) | abstract class | `PersistableBase` for asynchronous implementations whose synchronous load, save and hooks wait on their asynchronous counterparts on a worker. |
| [`IPersistable`](IPersistable.cs) | interface | Marker combining `ILoadable` and `ISaveable`. |
| [`PersistableBase`](PersistableBase.cs) | abstract class | `LoadableBase` that also implements `Save`, `SaveAsync`, a settable `RequiresSave` and the save notifications around abstract save members. |
| [`PersistableDecorator<TPersistableImpl>`](PersistableDecorator.cs) | class | Loadable decorator that also forwards `RequiresSave`, `Save`, `SaveAsync` and the save events to an inner `IPersistable`. |
| [`SyncPersistableBase`](SyncPersistableBase.cs) | abstract class | `PersistableBase` for synchronous implementations whose asynchronous load, save and hooks run the synchronous members through `Task.Run`. |
