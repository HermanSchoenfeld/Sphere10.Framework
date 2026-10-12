---
name: data-source
description: Implement IDataSource<T> dual sync/async CRUD via the abstract base hierarchy. Batch methods use the Range suffix before Async. Trigger when creating data sources or CRUD-backed grids.
---

# DataSource Skill

`IDataSource<T>` is a full dual sync/async CRUD interface: `Create`, `Read`, `Update`, `Delete`, `Validate` (item) plus `CreateRange`, `ReadRange`, `UpdateRange`, `DeleteRange`, `ValidateRange` (batch) and `*Async` counterparts.

## Naming contract
Batch methods use the **`Range` suffix before `Async`**: `CreateRange`, `CreateRangeAsync` — never `CreateBatch` or `CreateAsyncRange`.

## Choose the right base
- `DataSourceBase<T>` — all abstract; rarely used directly.
- `SyncBatchDataSourceBase<T>` — sync-first. Override only sync batch methods; item methods delegate to batch, async wraps sync via `Task.Run`.
- `AsyncBatchDataSourceBase<T>` — async-first. Override only async batch methods; sync wraps async via `.ResultSafe()`/`.WaitSafe()`.
- `FutureListDataSource<T>` — backed by `IFuture<IExtendedList<T>>`.
- `BulkFetchDataSource<T>` — `Reloadable` future; call `Invalidate()` to force re-fetch.
- `ListDataSource<T>` — wraps an existing `IExtendedList<T>`.
- `ProjectedDataSource<TFrom, TTo>` — decorator projecting between types.

## Collections
`IExtendedCollection<T>` / `IExtendedList<T>` use `long` indexing and range operations. `RangedListBase<T>` delegates single-item ops to `*Range` counterparts — subclass and override only the range methods. Persistent variants: `StreamMappedList<T>`, `StreamMappedDictionary<TKey,TValue>`, `StreamMappedHashSet<T>`, with observable/synchronized/transactional/merkle decorators.
