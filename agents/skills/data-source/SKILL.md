---
name: data-source
description: Implement generic IDataSource CRUD through the item-first or batch-first sync/async bases, using the current paging, refresh and validation contract. Trigger when creating data sources or CRUD-backed grids.
---

# DataSource Skill

Read [data-source guidance](../../../docs/guidelines/patterns/data-sources.md) for the shared CRUD, paging and delegation contract.

Read [IDataSource<T>](../../../src/Sphere10.Framework/DataSource/IDataSource.cs) before implementing it. Its current contract is:

- Item operations: `New`, `Create`, `Refresh`, `Update`, `Delete`, and `Validate(entity, CrudAction)`.
- Range operations: `NewRange(int count)`, `CreateRange`, paged `ReadRange`, in-place `RefreshRange(T[] entities)`, `UpdateRange`, `DeleteRange`, and `ValidateRange(IEnumerable<(T entity, CrudAction action)>)`.
- Async forms exist for create, refresh, update, delete, validation and paged reads. `New` and `NewRange` are synchronous only; there is no item-level `Read`, `NewAsync` or `NewRangeAsync`.
- Properties: `long Count`, `DataSourceCapabilities Capabilities`, `Task<long> CountAsync` and `Task<DataSourceCapabilities> CapabilitiesAsync`. These async members are properties, not methods.

`ReadRange` takes `searchTerm`, `int pageLength`, `int page`, `sortProperty` and `SortDirection`, and returns `DataSourceItems<T>` with `Items`, `Page` and `TotalCount`. Preserve the current paging and result signatures. The current data-source contract does not accept cancellation tokens.

## Naming contract
Batch methods use the **`Range` suffix before `Async`**: `CreateRange`, `CreateRangeAsync` — never `CreateBatch` or `CreateAsyncRange`.

## Choose the right base
- [DataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/DataSourceBase.cs) declares the neutral contract abstract.
- [SyncBatchDataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/SyncBatchDataSourceBase.cs): implement synchronous ranges, `Count` and `Capabilities`. Item methods derive from ranges; async adapters return the worker task.
- [AsyncBatchDataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/AsyncBatchDataSourceBase.cs): implement asynchronous ranges, `CountAsync`, `CapabilitiesAsync` and synchronous `NewRange`. Item methods derive from ranges.
- [SyncItemDataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/SyncItemDataSourceBase.cs): implement synchronous item methods, paged `ReadRange`, `Count` and `Capabilities`; range mutations loop over items.
- [AsyncItemDataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/AsyncItemDataSourceBase.cs): implement asynchronous item methods, paged `ReadRangeAsync`, `CountAsync`, `CapabilitiesAsync` and synchronous `New`; range mutations await item operations.

Choose the base matching the backend's real primitive. Do not duplicate the single/range forwarding or select execution direction through a flag. See [the directional hierarchy guide](../../../docs/guidelines/patterns/sync-async.md#synchronous-and-asynchronous-implementation-hierarchies).

The current async-first data-source bases still adapt synchronous calls through `WaitSafe`/`ResultSafe`. These describe existing code, not completion-guaranteed recipes: [TaskExtensions](../../../src/Sphere10.Framework/Extensions/TaskExtensions.cs) currently queues `WaitSafe` without joining it, and `ResultSafe` receives an already-started task. Follow the guide's completion, failure and synchronization-context rules when changing those bridges; prefer the native async operations on async consumers.

## Existing implementations and decoration

- [FutureListDataSource<T>](../../../src/Sphere10.Framework/DataSource/FutureListDataSource.cs) provides synchronous list-backed behavior through `IFuture<IExtendedList<T>>`; its `NewMethod` and `RefreshMethod` are extension hooks. Its default read implementation pages the list; it does not implement search or sorting merely because those parameters exist.
- [BulkFetchDataSource<T>](../../../src/Sphere10.Framework/DataSource/BulkFetchDataSource.cs) uses a reloadable future; `Invalidate()` forces retrieval on the next access.
- [ListDataSource<T>](../../../src/Sphere10.Framework/DataSource/ListDataSource.cs) wraps an existing `IExtendedList<T>`.
- [ProjectedDataSource<TFrom, TTo>](../../../src/Sphere10.Framework/DataSource/ProjectedDataSource.cs) projects source results. Operations requiring conversion back to the source type need the inverse mapping.
- [DataSourceDecorator<T, TConcrete>](../../../src/Sphere10.Framework/DataSource/DataSourceDecorator.cs) forwards the complete contract in each execution direction. Use it for additional behavior instead of rebuilding sync/async adapters in a wrapper.

Validation returns `Result`; propagate its errors according to the current implementation rather than replacing normal validation failure with an exception. `RefreshRange` may replace array entries with refreshed instances, so retain the caller-visible replacements.

## Collections
Backing `IExtendedCollection<T>` / `IExtendedList<T>` contracts use `long` counts and indices; data-source paging still uses `int`. [RangedListBase<T>](../../../src/Sphere10.Framework/Collections/Lists/RangedListBase.cs) supplies range-first list delegation. Persistent collection families include `StreamMappedList<T>`, `StreamMappedDictionary<TKey, TValue>` and `StreamMappedHashSet<T>`; reuse their existing access scopes and decorators where required by the backend.
