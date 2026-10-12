# Data Sources

Read the [data-source skill](../../../agents/skills/data-source/SKILL.md) and the [current interface](../../../src/Sphere10.Framework/DataSource/IDataSource.cs) before implementing a source. Choose an existing item-first or batch-first base matching the backend's real primitive.

## DataSource Pattern (Sync/Async Hierarchy)

`IDataSource<T>` provides item and batch CRUD, refresh, validation, paging, count and capability APIs. Most operations have synchronous and asynchronous forms; `New` and `NewRange` are synchronous only. The abstract implementation hierarchy avoids duplication:

- **`IDataSource<T>`** — item methods are `New`, `Create`, `Refresh`, `Update`, `Delete` and `Validate`; batch methods are `NewRange`, `CreateRange`, `ReadRange`, `RefreshRange`, `UpdateRange`, `DeleteRange` and `ValidateRange`. There is no item `Read` method. Batch async names put `Range` before `Async`; count/capabilities also have task-valued properties. See the [data-source skill](../../../agents/skills/data-source/SKILL.md) for item-first and batch-first base choices.
- **[DataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/DataSourceBase.cs)** — declares item/range operations and properties abstract.
- **[SyncBatchDataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/SyncBatchDataSourceBase.cs)** — implement synchronous range primitives, including `NewRange`, and `Count`/`Capabilities`. Item operations derive from ranges; async adapters return `Task.Run` tasks for the sync work.
- **[AsyncBatchDataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/AsyncBatchDataSourceBase.cs)** — implement async range primitives, task-valued `CountAsync`/`CapabilitiesAsync`, and synchronous `NewRange`. Async item operations await ranges; existing sync operations use `ResultSafe`/`WaitSafe`.
- **[SyncItemDataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/SyncItemDataSourceBase.cs)** — implement synchronous item primitives, paged `ReadRange`, `Count` and `Capabilities`. Range mutations loop over item operations; async adapters return the worker task.
- **[AsyncItemDataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/AsyncItemDataSourceBase.cs)** — implement async item primitives, paged `ReadRangeAsync`, `CountAsync`/`CapabilitiesAsync` and synchronous `New`. Async ranges await item operations; `NewRange` enumerates synchronous `New` calls.
- **[FutureListDataSource<T>](../../../src/Sphere10.Framework/DataSource/FutureListDataSource.cs)** — sync batch implementation over `IFuture<IExtendedList<T>>`, with `NewMethod` and `RefreshMethod` hooks. Its current read pages the list; it does not apply the supplied search or sort arguments. Its delete behavior also depends on the backing list's range implementation.
- **[BulkFetchDataSource<T>](../../../src/Sphere10.Framework/DataSource/BulkFetchDataSource.cs)** — extends `FutureListDataSource<T>` with a reloadable future. `Invalidate()` forces re-fetch on the next access that evaluates the future.
- **[ListDataSource<T>](../../../src/Sphere10.Framework/DataSource/ListDataSource.cs)** — wraps an existing `IExtendedList<T>` through an explicit future.
- **[ProjectedDataSource<TFrom, TTo>](../../../src/Sphere10.Framework/DataSource/ProjectedDataSource.cs)** — projects between types; operations converting an entity back to the source require the inverse mapping. Refresh-range operations copy replaced source instances back into the caller's array.

These describe the existing data-source implementations. Apply the [general sync/async adapter rules](sync-async.md#adapter-completion-and-cancellation) when extending this hierarchy, and verify existing waiting helpers before relying on their completion or synchronization-context behavior.

`CountAsync` and `CapabilitiesAsync` are task-valued properties. The contract has no `NewAsync`, `NewRangeAsync` or item-level `Read` operation and does not accept cancellation tokens. Batch names place `Range` before `Async`.

`ReadRange` takes `searchTerm`, `int pageLength`, `int page`, `sortProperty` and `SortDirection`. [DataSourceItems<T>](../../../src/Sphere10.Framework/DataSource/DataSourceItems.cs) currently contains `IEnumerable<T> Items`, `int Page` and `int TotalCount`; the source's separate `Count` property is `long`. Preserve these existing signatures, `Result` validation errors and caller-visible replacements performed by `RefreshRange`.

In the current [TaskExtensions](../../../src/Sphere10.Framework/Extensions/TaskExtensions.cs), `WaitSafe` queues `task.Wait` with `Task.Run` and returns without joining the worker. A sync mutation using it can return before completion and cannot synchronously propagate the pending failure. `ResultSafe` does block through the worker task's `.Result`, but the original operation has already started on the caller's context; this does not prevent a dispatcher deadlock, and `.Result` affects exception propagation. These are legacy caveats, not the intended adapter contract. Read [sync/async completion rules](sync-async.md#adapter-completion-and-cancellation) when changing a bridge, and prefer the native async operations on async consumers.

See [collections and storage](collections-and-storage.md), [fluent builders](fluent-builders.md) and the [CRUD grid skill](../../../agents/skills/crud-grid/SKILL.md) for consumers.
