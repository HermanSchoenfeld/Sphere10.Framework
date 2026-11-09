# Repository

The `IRepository<TEntity, TIdentity>` CRUD contract with paired synchronous and asynchronous members, base classes, a dictionary adapter and decorators.

`IRepository<TEntity, TIdentity>` offers `Contains`, `TryGet`, `Create`, `Update`, `Delete` and `Clear`, each with an `*Async` twin, change events, and both `IDisposable` and `IAsyncDisposable`. `RepositoryBase` derives from `Disposable` in [Disposables](../Disposables/README.md) and supplies the events and protected `Notify*` methods. Subclasses raise those events themselves. `SyncRepositoryBase` implements the asynchronous members with `Task.Run` over the synchronous ones, and `AsyncRepositoryBase` implements the synchronous members as sync-over-async over the asynchronous ones. Each seals the disposal method of the other direction, so implement only `DisposeInternal` or `DisposeInternalAsync` respectively.

`DictionaryRepositoryAdapter` exposes any `IDictionary<TIdentity, TEntity>` as a repository, using a delegate to read an entity's identity. It throws when `Create` meets an existing identity and raises no events. `RepositoryDecorator` forwards every member, and `SynchronizedRepository` wraps each call in a read or write scope of a `SynchronizedObject` from [Threading](../Threading/README.md). Its asynchronous members hold that lock across an `await`, so they rely on the continuation returning to the same thread. `IRepositoryExtensions.Get` throws when the identity is missing. `TransactionalRepository.cs` is commented out pending a transactional-scope redesign. The loading and saving contracts are in [Persistable](../Persistable/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AsyncRepositoryBase<TEntity, TIdentity>`](AsyncRepositoryBase.cs) | abstract class | Repository base for asynchronous implementations whose synchronous members wait on the asynchronous ones on a worker. |
| [`DictionaryRepositoryAdapter<TEntity, TIdentity>`](DictionaryRepositoryAdapter.cs) | class | Dictionary adapter over an `IDictionary<TIdentity, TEntity>`, by default a new `Dictionary`. |
| [`DictionaryRepositoryAdapter<TEntity, TIdentity, TDict>`](DictionaryRepositoryAdapter.cs) | class | Synchronous repository over a dictionary of a given type, keyed by an identity delegate. |
| [`IRepository<TEntity, TIdentity>`](IRepository.cs) | interface | CRUD contract with synchronous and asynchronous members, change events and disposal. |
| [`IRepositoryExtensions`](IRepositoryExtensions.cs) | static class | Extends `IRepository` with `Get` and `GetAsync`, which throw `InvalidOperationException` for a missing identity. |
| [`RepositoryBase<TEntity, TIdentity>`](RepositoryBase.cs) | abstract class | Disposable repository base with abstract CRUD members, change events, `On*` hooks and `Notify*` raisers. |
| [`RepositoryDecorator<TEntity, TIdentity>`](RepositoryDecorator.cs) | class | Decorator over any `IRepository<TEntity, TIdentity>`. |
| [`RepositoryDecorator<TEntity, TIdentity, TConcrete>`](RepositoryDecorator.cs) | class | Decorator that forwards every member, event and disposal to an inner repository of a concrete type. |
| [`SyncRepositoryBase<TEntity, TIdentity>`](SyncRepositoryBase.cs) | abstract class | Repository base for synchronous implementations whose asynchronous members run the synchronous ones through `Task.Run`. |
| [`SynchronizedRepository<TEntity, TIdentity>`](SynchronizedRepository.cs) | class | Decorator that guards reads with a read scope and writes with a write scope of a reader-writer lock. |
