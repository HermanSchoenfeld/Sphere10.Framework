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
- **[SnapshotDataSource<T>](../../../src/Sphere10.Framework/DataSource/SnapshotDataSource.cs)** — read-only synchronous batch source over atomically replaced array membership. Copies arrays on publication/access, searches public readable nonindexed instance properties and sorts their raw values before clamped paging. An optional `Func<object,string>` supplies value/search text without a platform dependency; default formatting is invariant. Mutation/validation operations are unsupported. Configure defaults before sharing, keep custom formatters safe for async worker queries, and remember that copied membership does not freeze mutable row objects.
- **[RefreshableSnapshotDataSource<T,TKey>](../../../src/Sphere10.Framework/DataSource/RefreshableSnapshotDataSource.cs)** — adds stable-key row refresh to that snapshot source. Supply the identity selector and synchronous refresh callback; async calls use the existing worker adapter. A null callback result removes an existing row, and a changed key rejects the whole batch. Refresh does not insert absent keys. Callbacks run outside locks; a concurrent complete snapshot publication wins over stale refresh results and the caller receives the latest published rows. Applications retain identity, formatting and command policy.
- **[ProjectedDataSource<TFrom, TTo>](../../../src/Sphere10.Framework/DataSource/ProjectedDataSource.cs)** — projects between types; operations converting an entity back to the source require the inverse mapping. Refresh-range operations copy replaced source instances back into the caller's array.

These describe the existing data-source implementations. Apply the [general sync/async adapter rules](sync-async.md#adapter-completion-and-cancellation) when extending this hierarchy, and verify existing waiting helpers before relying on their completion or synchronization-context behavior.

`CountAsync` and `CapabilitiesAsync` are task-valued properties. The contract has no `NewAsync`, `NewRangeAsync` or item-level `Read` operation and does not accept cancellation tokens. Batch names place `Range` before `Async`.

`ReadRange` takes `searchTerm`, `int pageLength`, `int page`, `sortProperty` and `SortDirection`. [DataSourceItems<T>](../../../src/Sphere10.Framework/DataSource/DataSourceItems.cs) currently contains `IEnumerable<T> Items`, `int Page` and `int TotalCount`; the source's separate `Count` property is `long`. Preserve these existing signatures, `Result` validation errors and caller-visible replacements performed by `RefreshRange`.

`DataSourceCapabilities` contains composite flags: `CanUpdate`, `CanSearch`,
`CanSort` and `CanPage` include `CanRead`. Removing one of those flags with a
complement also removes read permission. Compose the desired operation mask
explicitly and intersect it with the source capabilities; preserve `CanRead` when
temporarily disabling mutations in a readable inventory. For example, a read-only
grid can allow `CanRead | CanSearch | CanSort | CanPage`, constrained by its source.
UI busy/selection state does not replace the source's permission enforcement.

In the current [TaskExtensions](../../../src/Sphere10.Framework/Extensions/TaskExtensions.cs), `WaitSafe` queues `task.Wait` with `Task.Run` and returns without joining the worker. A sync mutation using it can return before completion and cannot synchronously propagate the pending failure. `ResultSafe` does block through the worker task's `.Result`, but the original operation has already started on the caller's context; this does not prevent a dispatcher deadlock, and `.Result` affects exception propagation. These are legacy caveats, not the intended adapter contract. Read [sync/async completion rules](sync-async.md#adapter-completion-and-cancellation) when changing a bridge, and prefer the native async operations on async consumers.

See [collections and storage](collections-and-storage.md), [fluent builders](fluent-builders.md) and the [CRUD grid skill](../../../agents/skills/crud-grid/SKILL.md) for consumers.

WinForms snapshot grids reuse the nonvisual
[CrudGridSnapshotController<T>](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudGridSnapshotController.cs)
with a typed collector and stable identity. This binds the core source behind a
concrete designer surface; source queries remain platform-neutral and caller
CSV/report policy does not belong in the controller. Verify query/copy/concurrency
behavior through [SnapshotDataSourceTests](../../../tests/Sphere10.Framework.Tests/DataSource/SnapshotDataSourceTests.cs).

Native enumeration can produce a complete collection while its `IDataSource`
supports paging. Collect once, publish membership, then perform filtering and
sorting over that snapshot before slicing the requested page. Return the filtered
total and actual clamped page, rather than making the grid trim a complete result.
Page navigation must not trigger another native collection. In WinForms, advertise
`CanPage` and use Auto or Paged to bound cell preparation and native binding;
Single remains the explicit choice to render the complete matching collection.

## Debugging foreground-thread data work

Set `Debug.MainThread = Thread.CurrentThread` once in the application's entry
point, then call `Debug.AssertNotMainThread()` inside actual queries, refresh
callbacks, native worker bodies and before synchronous waiting bridges. The
C# static extension also accepts a message and an optional detail message through
its overloads. It compares the current thread directly with that single captured
thread; there are no registrations, scopes or reporting callbacks. An unset
`MainThread` leaves the check unconfigured. Calls and their arguments are omitted
from Release consumers through `[Conditional("DEBUG")]`.

Do not assert merely because a UI event invokes an async facade: confirmation
callbacks and editor draft creation belong to that thread. Cached snapshot counts,
capabilities and publication remain valid UI metadata operations. Successfully
completed count/capability tasks return directly without an extra queued wait.

An async resource method must keep its complete operation on a worker. Dispatching
only `Open` does not protect subsequent `Length`, seek/truncate, or disposal calls:
an await can complete synchronously and continue on the original UI thread.
