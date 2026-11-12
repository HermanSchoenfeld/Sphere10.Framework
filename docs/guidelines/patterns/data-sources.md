# Data Sources

Read the [current interface](../../../src/Sphere10.Framework/DataSource/IDataSource.cs) before implementing a source. A data source is one of three kinds: **storage** that holds data, a **feature decorator** that adds one capability over any source, or a **composition** that assembles a usable source from the two. Choose the storage leaf by the backend's real primitive and add behaviour with decorators, never by committing a base lineage.

## Three kinds of data source

### Storage holds data

- **[ListDataSource<T>](../../../src/Sphere10.Framework/DataSource/ListDataSource.cs)** is the loadable list storage on the synchronous lineage: sync batch CRUD over an `IExtendedList<T>` which is either supplied or fetched on first load. It is an `ILoadableDataSource<T>` (`RequiresLoad`, `Load`, `LoadAsync(token)`, `Loading`/`Loaded`); concurrent loads wait for the first caller's fetch. Hooks are `Fetch()`, `NewMethod`, `RefreshMethod` (a changed row is stored, a null result removes the row) and `FormatValue`. Its asynchronous members run the synchronous ones on a worker (`SyncDataSourceBase`), so `LoadAsync` runs `Load` on a worker. Reads search and sort before paging through `Tools.DataSources.Query`, which sorts by a data property when the sort name matches one; otherwise rows keep the source order, so a grid column whose saved sort name no longer exists never fails the read.
- **[AsyncListDataSource<T>](../../../src/Sphere10.Framework/DataSource/AsyncListDataSource.cs)** is its dual on the asynchronous lineage (`AsyncBatchDataSourceBase`), not a subclass of it: override `FetchAsync(token)`, `RefreshMethodAsync` and the asynchronous range operations; its synchronous members, including `Load`, wait on the asynchronous ones (`// Sync-over-async pattern`, asserted off the main thread). Both construct over a list or rows (already loaded) or, from a deriving leaf, unloaded so the first load fetches. **[ActionListDataSource<T>](../../../src/Sphere10.Framework/DataSource/ActionListDataSource.cs)** and **[ActionAsyncListDataSource<T>](../../../src/Sphere10.Framework/DataSource/ActionAsyncListDataSource.cs)** let a client compose a leaf from a fetch callback without deriving one.
- **[ArrayDataSource<T>](../../../src/Sphere10.Framework/DataSource/ArrayDataSource.cs)** is the read-only store for rows a screen or control produces itself: `Data` copies the membership, `ReplaceData` publishes and notifies, `DefaultSortProperty`/`DefaultSortDirection`, `FormatValue` and `CopyItemForRead` are hooks. Mutations are unsupported.
- A dictionary store is a later storage kind; it would hold data the same way.

Storage never owns a clock, a refresh engine or a lifecycle. A leaf whose primitives are asynchronous belongs in the asynchronous lineage; it must not sit in the synchronous lineage and override asynchronous members.

### Feature decorators derive from `DataSourceDecorator`

- **[DataSourceDecorator<T,TConcrete>](../../../src/Sphere10.Framework/DataSource/DataSourceDecorator.cs)** forwards every member, including `Changed` and capabilities, to `InternalDataSource`; the property is protected and settable so a decorator can swap the decorated source. The interface-typed `DataSourceDecorator<T>` is the convenience form.
- **[RefreshableDataSourceBase<T,TConcrete>](../../../src/Sphere10.Framework/DataSource/RefreshableDataSourceBase.cs)** refreshes the data source itself. It is a template: a subclass creates each fresh source in the abstract `CreateDataSource()` hook, and **[ActionRefreshableDataSource<T,TConcrete>](../../../src/Sphere10.Framework/DataSource/ActionRefreshableDataSource.cs)** (and the interface-typed `ActionRefreshableDataSource<T>`) lets a client supply a factory instead. A refresh creates the fresh source, loads it when it is an `ILoadable` that requires a load (`Refresh` with `Load` on the calling thread, `RefreshAsync` with `LoadAsync(token)` on a worker), swaps `InternalDataSource` under the publication lock, disposes the old source when it is disposable and raises `Changed`. That swapped instance is the published capture; readers keep the previous source until the swap. `Current` is the served source, `Completion` the in-flight asynchronous refresh, `RequiresLoad` true until the served source is loaded (`Count`, `ReadRange` and the asynchronous reads load first). Overlapping asynchronous callers join one refresh; a caller's token ends only its wait while protected `Lifetime` cancels the work. Protected `CanPublish(latest)` is evaluated under the lock: false discards the result and refreshes again, and a source that changed meanwhile supersedes the refresh. Protected `Publish(source)` serves a source now and wins over an older refresh. `DisposeAsync` cancels the lifetime, drains the refresh (observed as cancelled, never published), disposes the served source and then runs protected `ReleaseAsync()`; `Dispose` requests that without blocking; later use throws `ObjectDisposedException`. It owns its own `Changed`, relaying the served source's event while that source advertises `CanNotify`.
- **[PollingDataSource<T,TConcrete>](../../../src/Sphere10.Framework/DataSource/PollingDataSource.cs)** decorates an `IRefreshableDataSource<T>` with the cadence and is the `IRefreshingDataSource` the application lifecycle manages. `Enabled` (false) is lifecycle admission, `AutoRefresh` (true) the user's policy and `RefreshInterval` (one second) a positive interval within the scheduler timer range, or `Timeout.InfiniteTimeSpan` for an owner-clocked source. While all three permit it, exactly one repeating job on the Framework `Scheduler` (`Scheduler.Asynchronous` unless a started scheduler is supplied) fires the inner `RefreshAsync` and observes it; the scheduler thread never waits for acquisition, overlapping ticks join the refresh in progress, and a policy or interval change replaces the job without cancelling an admitted refresh. `Refresh`, `RefreshAsync`, `Load`, `LoadAsync`, `Completion` and `IsDisposed` forward to the inner source; a failed refresh other than the lifetime's own cancellation raises `RefreshFailed` once (`OnRefreshFailed` hook, `NotifyRefreshFailed`). `StopAsync` is permanent and virtual: it disables, removes the job, disposes the inner source (cancel, drain, release) and then runs protected `ReleaseAsync()`; repeated stops share one task, `Enabled = false` and `AutoRefresh = false` stay harmless afterwards and enabling, retiming, refreshing or loading throws. `Dispose`/`DisposeAsync` map to it.
- **[ProjectedDataSource<TFrom,TTo>](../../../src/Sphere10.Framework/DataSource/ProjectedDataSource.cs)** projects between row types; operations converting an entity back to the source require the inverse mapping, and refresh-range operations copy replaced source instances back into the caller's array.

Decorators do not concern themselves with synchronous and asynchronous directions. A decorator adds the members its feature needs, synchronous or asynchronous, and works over a synchronous or an asynchronous inner source alike by calling the inner member of the same direction. It never rebuilds one direction through the other, blocks on a task or discards one; only the storage leaf adapts, through its `Sync*`/`Async*` lineage, and marks the adapter `// Sync-over-async pattern`.

### Compositions assemble the usable source

The source an application activates is a composition. The simple case is storage whose load fetches; the Framework's own `ProcessDataSource` over the BCL `Process` type is the reference:

```csharp
// Framework: storage whose load fetches the processes and whose reads scan that list.
public class ProcessDataSource : ListDataSource<Process> {
	protected override IEnumerable<Process> Fetch() {
		return Process.GetProcesses();
	}
}

// A client composition: a polled, refreshable source over fresh process lists.
var liveProcesses = new PollingDataSource<Process>(new ActionRefreshableDataSource<Process>(() => new ProcessDataSource()));
```

When the rows come out of an asynchronous pipeline, the pipeline is the fetch of an asynchronous leaf and the refreshable only creates that leaf. The leaf reaches its owner through an object reference, never a delegate:

```csharp
// Storage: one capture, fetched on load by running the owner's pipeline, or built preloaded from rows the owner already holds.
public sealed class Capture : AsyncListDataSource<Observation> {
	private readonly Probe _owner;

	internal Capture(Probe owner) {
		_owner = owner;
	}

	internal Capture(IEnumerable<Observation> rows)
		: base(rows) {
	}

	protected override async Task<IEnumerable<Observation>> FetchAsync(CancellationToken cancellationToken) {
		return await _owner.CollectAsync(cancellationToken);
	}
}

// Refreshable: a template whose hook creates the unloaded capture; the decorator loads it in the caller's direction.
public sealed class Captures : RefreshableDataSourceBase<Observation, Capture> {
	internal Captures()
		: base(new Capture(Array.Empty<Observation>()), true) {
	}

	internal Probe Owner { get; set; }

	protected override Capture CreateDataSource() {
		return new Capture(Owner);
	}
}

// Application: the polling source with the owner's policy on it.
public sealed class Probe : PollingDataSource<Observation, Captures> { … }
```

`NetworkProbeDataSource` (core) and Framework.Windows' `ProcessWorkingSetDataSource` and editable inventories follow this shape. A product derives `PollingDataSource` when it owns policy (settings, owned enrichment dependencies, stop ordering) and `RefreshableDataSourceBase` when it owns creation, retargeting or formatting (`CreateDataSource`, `CanPublish`, `Publish`). The storage leaf stays minimal. Template hooks live in the hierarchy types; `Action*` variants exist only so clients can compose without deriving. Hierarchy types do not take `Func`/`Action` constructor parameters or delegate properties.

**The rule:** a capability is a `DataSourceDecorator`, never a base-lineage commitment; storage holds data and loads itself; a refresh is a new source swapped in; compositions assemble usable sources; decorators are direction-agnostic and call the matching direction of their inner; only the storage leaf adapts one direction to the other, through `SyncDataSourceBase`/`AsyncDataSourceBase` and their batch and item specializations. Every `Sync*` type has an `Async*` dual. Nothing in this family is called a snapshot.

## DataSource Pattern (Sync/Async Hierarchy)

`IDataSource<T>` provides item and batch CRUD, refresh, validation, paging, count, capability and `Changed` notification APIs. Most operations have synchronous and asynchronous forms; `New` and `NewRange` are synchronous only. The abstract implementation hierarchy avoids duplication and follows the template pattern: foundations are abstract `*Base` types with hooks, concrete sources override those hooks, and `Action*` variants exist only so a client can compose a source from callbacks without deriving one.

- **`IDataSource<T>`** — item methods are `New`, `Create`, `Refresh`, `Update`, `Delete` and `Validate`; batch methods are `NewRange`, `CreateRange`, `ReadRange`, `RefreshRange`, `UpdateRange`, `DeleteRange` and `ValidateRange`. There is no item `Read` method. Batch async names put `Range` before `Async`; count/capabilities also have task-valued properties.
- **[DataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/DataSourceBase.cs)** declares item/range operations and properties abstract and owns `Changed`, protected `NotifyChanged()` (and `NotifyChanged(subscribers)` for a decorator with its own event) and the protected `OnChanged()` hook.
- **[SyncDataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/SyncDataSourceBase.cs)** — the synchronous lineage: every asynchronous member, including `CountAsync`/`CapabilitiesAsync`, runs its synchronous counterpart on a worker with `Task.Run`. Its dual is **[AsyncDataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/AsyncDataSourceBase.cs)**, the asynchronous lineage: every synchronous member waits on its asynchronous counterpart with `GetAwaiter().GetResult()` (`// Sync-over-async pattern`, asserted off the main thread; `Count`/`Capabilities` return a completed result without asserting).
- **[SyncBatchDataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/SyncBatchDataSourceBase.cs)** (from `SyncDataSourceBase`) — implement synchronous range primitives, including `NewRange`, and `Count`/`Capabilities`; item operations derive from ranges. Its dual **[AsyncBatchDataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/AsyncBatchDataSourceBase.cs)** (from `AsyncDataSourceBase`) — implement async range primitives, task-valued `CountAsync`/`CapabilitiesAsync`, and synchronous `NewRange`; async item operations await ranges.
- **[SyncItemDataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/SyncItemDataSourceBase.cs)** (from `SyncDataSourceBase`) — implement synchronous item primitives, paged `ReadRange`, `Count` and `Capabilities`; range mutations loop over item operations. Its dual **[AsyncItemDataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/AsyncItemDataSourceBase.cs)** (from `AsyncDataSourceBase`) — implement async item primitives, paged `ReadRangeAsync`, `CountAsync`/`CapabilitiesAsync` and synchronous `New`; async ranges await item operations and `NewRange` enumerates `New`.
- **[ILoadableDataSource<T>](../../../src/Sphere10.Framework/DataSource/ILoadableDataSource.cs)** marks storage that fetches its data on load (`IDataSource<T>` plus `ILoadable`, whose `LoadAsync` takes the caller's `CancellationToken`); **[IRefreshableDataSource<T>](../../../src/Sphere10.Framework/DataSource/IRefreshableDataSource.cs)** adds `Refresh`, `RefreshAsync`, `Completion`, `IsDisposed` and disposal; **[IRefreshingDataSource](../../../src/Sphere10.Framework/DataSource/IRefreshingDataSource.cs)** is the cadence contract (`Enabled`, `AutoRefresh`, `RefreshInterval`, `RefreshAsync`, `StopAsync`, `RefreshFailed`).

Apply the [general sync/async adapter rules](sync-async.md#adapter-completion-and-cancellation) when extending this hierarchy, and verify existing waiting helpers before relying on their completion or synchronization-context behavior.

`CountAsync` and `CapabilitiesAsync` are task-valued properties. The contract has no `NewAsync`, `NewRangeAsync` or item-level `Read` operation and does not accept cancellation tokens. Batch names place `Range` before `Async`.

`ReadRange` takes `searchTerm`, `int pageLength`, `int page`, `sortProperty` and `SortDirection`. [DataSourceItems<T>](../../../src/Sphere10.Framework/DataSource/DataSourceItems.cs) currently contains `IEnumerable<T> Items`, `int Page` and `int TotalCount`; the source's separate `Count` property is `long`. Preserve these existing signatures, `Result` validation errors and caller-visible replacements performed by `RefreshRange`.

`DataSourceCapabilities` contains composite flags: `CanUpdate`, `CanSearch`,
`CanSort` and `CanPage` include `CanRead`, and `ReadOnly` is the read/search/sort/page
mask a read-only inventory advertises (with `CanNotify` when it notifies). Removing
one of those flags with a complement also removes read permission. Compose the
desired operation mask explicitly and intersect it with the source capabilities;
preserve `CanRead` when temporarily disabling mutations in a readable inventory.
UI busy/selection state does not replace the source's permission enforcement.

In the current [TaskExtensions](../../../src/Sphere10.Framework/Extensions/TaskExtensions.cs), `WaitSafe` queues `task.Wait` with `Task.Run` and returns without joining the worker. A sync mutation using it can return before completion and cannot synchronously propagate the pending failure. `ResultSafe` does block through the worker task's `.Result`, but the original operation has already started on the caller's context; this does not prevent a dispatcher deadlock, and `.Result` affects exception propagation. These are legacy caveats, not the intended adapter contract. Read [sync/async completion rules](sync-async.md#adapter-completion-and-cancellation) when changing a bridge, and prefer the native async operations on async consumers. New leaves join a task with `GetAwaiter().GetResult()` off the main thread.

See [collections and storage](collections-and-storage.md), [fluent builders](fluent-builders.md) and the [WinForms project guide](../../../src/Sphere10.Framework.Windows.Forms/README.md) for CRUD grid consumers.

WinForms and Blazor grids bind the data source and observe `Changed` when it advertises `CanNotify`. The existing nonvisual
[VisualGridArrayController<T>](../../../src/Sphere10.Framework.Windows.Forms/VisualGrid/VisualGridArrayController.cs)
reads a capture source into an `ArrayDataSource<T>` projection behind a designer surface; it never triggers a loadable source's first load. Source queries remain platform-neutral; CSV/report policy belongs to the caller.

Native enumeration can produce a complete collection while its `IDataSource`
supports paging. Collect once, publish membership, then perform filtering and
sorting over that capture before slicing the requested page. Return the filtered
total and actual clamped page, rather than making the grid trim a complete result.
Page navigation must not trigger another native collection. In WinForms, advertise
`CanPage` and use Auto or Paged to bound cell preparation and native binding;
Single remains the explicit choice to render the complete matching collection.

## Notifications and source-owned acquisition

`IDataSource<T>.Changed` is the common notification contract.
`DataSourceCapabilities.CanNotify` is an independent flag which means the source
emits that event after accepted data changes. It does not grant a CRUD permission
and is deliberately separate from `DataSourceCapabilities.Default`. Test the
actual source notification capability before applying a grid's operation mask.

Raise `Changed` after successful publication or committed mutations,
outside all source/configuration and product-owner locks. A read, count query,
page change, draft or failed refresh retaining unchanged data does
not emit a data change. Emit once for a logical operation through item/range and
sync/async adapters. If an existing nontransactional batch partially commits
before failing, notify its accepted changes rather than pretending it rolled back.
`DataSourceBase.NotifyChanged()` isolates consumer failures and calls protected
`OnChanged()` before external handlers. The event is not tied to a UI dispatcher.

`DataSourceDecorator` and `ProjectedDataSource` forward notifications and capabilities
directly. `RefreshableDataSourceBase` owns one `Changed`: it relays the served source's
event and raises once per swap, so a consumer sees one notification per capture
whichever source serves it. `PollingDataSource` forwards. Decorators do not add a
second event per mutation, take ownership of a borrowed source or acquire a
collection clock. A grid subscribes once per binding, unsubscribes when
replacing/clearing/disposing, and coalesces notifications into a silent current-page
read on its own dispatcher. It preserves current filters, sort, page, column sizes
and stable selection, rejects stale pending queries, and defers display updates
during active drafts. Notification handling must not call source refresh, disable
mouse input or hold a render lock while fetching. Coordinate source notifications
with existing post-CRUD refreshes.

### Refreshable sources

A `RefreshableDataSourceBase` subclass is the data provider as well as the `IDataSource<T>`. Keep
acquisition, accepted rows, retargeting policy and notification in that owner and
share it through IoC when multiple screens consume the same resource. Query-only
projections may compose it without copying its lifecycle.

- The served source stays readable throughout a refresh; the fresh source loads on
  a worker before the short publication lock. Constructors remain dormant: creating
  the initial source never invokes collection work, and `RequiresLoad` reports that
  the first read will refresh.
- A product source that can be retargeted while a capture runs captures its
  detached inputs when it creates the fresh source and compares them in
  `CanPublish`; a result collected for a superseded request is discarded and the
  refresh runs again for the current request. Keep target identities and request
  queues in that product owner; the base has no generic request machinery.
- `Publish(source)` serves rows the owner already has (a row-level update, a reset)
  and wins over an older refresh. Copied membership retains ordinary row-object
  ownership; a source whose rows are owned mutable aggregates copies in its storage
  leaf (`CopyItemForRead` or an equivalent read hook).
- `Completion` is the admitted refresh including its publication and notification;
  an operation such as a statistics reset awaits it before taking its own gate,
  because a collector's local gate alone does not prove publication.
- `ReleaseAsync()` runs once the final refresh has drained and the served source is
  disposed; override it to release owned acquisition dependencies instead of
  wrapping `DisposeAsync`.

### Refresh policy and lifecycle

`PollingDataSource` is the only cadence engine; there is no per-source scheduler,
timer or delay loop.

- `Enabled` is lifecycle admission and defaults to false. `AutoRefresh` is the
  independent user policy and defaults to true; `RefreshInterval` defaults to one
  second. Automatic refresh runs only while both gates permit it. Disabling either
  gate retains data and lets an admitted refresh finish. Explicit `RefreshAsync`
  remains available while automatic refresh is off. Reads and startup helpers must
  not silently turn `Enabled` back on or overwrite the user's automatic-refresh
  preference.
- Overlapping manual and automatic requests join one refresh. A caller's token ends
  only its wait; the owner lifetime cancels actual work. The repeating job fires the
  refresh without waiting for it, so a slow acquisition never blocks the scheduler.
- `Timeout.InfiniteTimeSpan` means the owner supplies the clock, as a hardware source
  clocked by the shared telemetry sample does: admission, coalescing, failure
  reporting and shutdown are reused without any schedule.
- A pre-canceled caller admits no work. A failed refresh raises `RefreshFailed`,
  retains accepted data and leaves the automatic job in place; expected owner
  cancellation is cancellation, not a failure.
- Register an application-wide composition as the IoC singleton shared by its
  consumers. Application initialization applies accepted typed settings, enables the
  source, requests its initial data and logs progress/results through the application
  logger. IoC interface aliases resolve the same registered concrete instance rather
  than invoking another factory. Source constructors do not enumerate devices or open
  native sessions. Global finalization disables and awaits sources before closing
  their dependencies. Screen-specific sources use screen initialization/destruction.
- [`DataSourceLifetime`](../../../src/Sphere10.Framework/DataSource/DataSourceLifetime.cs)
  tracks active owners and pending removals. Register the polling owner before native
  startup begins, dispose its registration on removal and await `CompleteAsync` at
  application finalization. `IsCompleting` prevents a queued continuation from
  re-enabling acquisition while shutdown is already admitted. Completed
  registrations/drains release their references. Closing one view must not dispose a
  borrowed singleton.
- `StopAsync` permanently disables, removes the automatic job, disposes the inner
  refreshable (which cancels its lifetime and drains the admitted refresh; a refresh
  finishing afterwards is observed as cancelled, not published) and then runs
  `ReleaseAsync`. Repeated stops join one task. `Enabled=false` remains harmless
  during shutdown; enabling throws. Synchronous `Dispose` requests observed
  nonblocking finalization; owners await `StopAsync`/`DisposeAsync` before releasing
  resources.

`Tools.DataSources.ReadAllAsync(source, token)` loads an `ILoadableDataSource` when it
still requires a load and returns every row in natural order.
`Tools.DataSources.CaptureAsync(factory, token)` owns a temporary source, constructs
it on a worker, reads it and asynchronously releases it even when the waiting caller
is canceled. A consumer that needs a fresh enumeration of an inventory composes
`new ActionRefreshableDataSource<T>(() => new ThatDataSource())` and refreshes it.

Editable inventories are `ListDataSource`/`AsyncListDataSource` subclasses whose range
overrides perform the native change and then update their own list (an asynchronous
inventory overrides only the asynchronous ranges; its lineage supplies the synchronous
ones). The Framework.Windows inventories are templates such as
`EnvironmentVariableDataSourceBase`: a `PollingDataSource` over a nested
`RefreshableDataSourceBase` that creates a fresh `Inventory`, with abstract hooks
(`ConfirmAsync`, `Logger`) for the owner's policy and `Action*` variants for clients,
so a refresh serves a fresh inventory while edits stay in the served one. Emit `Changed` from the
actual accepted collection rather than from both a nested collection and an
operation-completed event. Advertise `CanNotify` only when that publication/mutation
contract is implemented. Keep user preferences in their typed settings owner and pass
accepted values to sources; Framework data sources do not load application settings
or own UI controls.

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
callbacks and editor draft creation belong to that thread. Cached counts,
capabilities and publication remain valid UI metadata operations. Successfully
completed count/capability tasks return directly without an extra queued wait.

An async resource method must keep its complete operation on a worker. Dispatching
only `Open` does not protect subsequent `Length`, seek/truncate, or disposal calls:
an await can complete synchronously and continue on the original UI thread.
