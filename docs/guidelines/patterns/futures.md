# Deferred Values and Futures

Read this topic when a value can be supplied later, loaded on demand, invalidated
or projected. Reuse Sphere10's existing futures instead of creating another lazy
holder or assuming that `IFuture` means an asynchronous Task.

## Contract and factories

[IFuture<out T>](../../../src/Sphere10.Framework/Values/IFuture.cs) exposes only
`T Value { get; }`. It has no universal IsLoaded, Reset, cancellation, await,
disposal or ownership contract. The implementation determines when loading
happens and how unavailable values/failures are reported.

[Tools.Values.Future](../../../src/Sphere10.Framework/Values/ValueTool.cs) is the
existing discovery facade:

| Factory / implementation | Actual behavior |
|---|---|
| `Explicit<T>()` / `Explicit(value)` → [ExplicitFuture<T>](../../../src/Sphere10.Framework/Values/ExplicitFuture.cs) | A value is assigned once through its concrete setter. Reading before assignment or assigning again throws. Supplying a value through the factory initializes it immediately. |
| `LazyLoad(loader)` → [LazyLoad<T>](../../../src/Sphere10.Framework/Values/LazyLoad.cs) | Runs the synchronous loader on first Value access and retains the successful result. A failed load leaves it unloaded, so a later access retries. It does not serialize competing callers. |
| `Reloadable(loader)` → [Reloadable<T>](../../../src/Sphere10.Framework/Values/Reloadable.cs) | Loads on demand under synchronized scopes. Its concrete Loaded property and Invalidate method permit reloading on the next access. Invalidation disposes a cached IDisposable or IDisposable[]; choose this only when that ownership is intended. |
| `AlwaysLoad(loader)` → [ProxyValue<T>](../../../src/Sphere10.Framework/Values/ProxyValue.cs) | Invokes the loader on every Value access, without caching or invalidation state. |
| `Projection(source, projection)` / `source.AsProjection(...)` | Re-evaluates the projection over source.Value on each access. It retains the source future's loading policy; it is not a new cached snapshot. |
| `BackgroundFetched<TSource, T>(future, loader)` → [BackgroundFetchedValue<T>](../../../src/Sphere10.Framework/Values/BackgroundFetchedValue.cs) | Starts its loader immediately on the ThreadPool. Value blocks until completion and wraps a loader failure in AggregateException. The current factory does not use its source-future argument. It supplies no cancellation/drain/disposal API. |

Use the exact current names. There is no factory named Future.Lazy,
Future.ThreadSafe or Future.Projected in the inspected source. A thread-safe
loading requirement needs the implementation's actual synchronization contract,
not a name inferred from IFuture.

## Ownership and concurrency

A future controls evaluation; it does not make the returned value immutable,
copy it or transfer/dispose its resources unless its concrete contract says so.
Access may perform synchronous IO, and BackgroundFetched.Value can block.
Keep expensive evaluation away from a UI dispatcher, and use a Task-returning API
when asynchronous completion/cancellation is part of the contract.

LazyLoad and ExplicitFuture are not synchronized. Reloadable serializes loading
and invalidation, but a returned value is not a lifetime lease: a later Invalidate
can dispose it while another consumer retains it. Coordinate invalidation with
use or choose an ownership-safe value. Its loader runs inside the write scope;
do not introduce UI reentrancy or unrelated provider/callback work under that lock.
See [synchronized scopes](synchronization.md).

For projection, inspect both source and projection costs and side effects. Cache
explicitly only when the caller needs one stable result; an AlwaysLoad projection
is appropriate when each access should reflect the source's current value.
See [projection adapters](projections.md).

Detached caller-owned data does not need defensive array clones merely to look
immutable. Decide who owns and can mutate the value. Protect a retained cache,
configuration or stale-edit baseline where mutation would break its contract;
transfer an already detached, caller-owned result without redundant constructor
and getter copies. An array copy preserves membership, not deep element immutability.

## Verified consumers

- [DirectoryFileSettingsProvider](../../../src/Sphere10.Framework.Application/Settings/DirectoryFileSettingsProvider.cs)
  accepts IFuture<string> for its base directory; a string constructor supplies
  an Explicit future. Settings paths resolve the directory when needed.
- [ApplicationBase](../../../src/Sphere10.Framework.Application/UI/ApplicationBase.cs)
  retains lazy futures for its protected initializer/finalizer inventories, rather
  than resolving tasks in every property or exposing them as public definitions.
- [ApplicationControl](../../../src/Sphere10.Framework.Windows.Forms/Application/Controls/ApplicationControl.cs)
  resolves provider-bound services lazily only after runtime attachment and refreshes
  its futures when the active provider changes. A future does not justify DI work
  in a design-mode constructor.
- [FutureListDataSource](../../../src/Sphere10.Framework/DataSource/FutureListDataSource.cs)
  delegates to a supplied future list. [BulkFetchDataSource](../../../src/Sphere10.Framework/DataSource/BulkFetchDataSource.cs)
  chooses Reloadable and exposes Invalidate for a later re-fetch.
- [RegistryTree](../../../src/Sphere10.Framework.Windows/Registry/RegistryTree.cs)
  accepts IFuture<RegistryValue[]> and IFuture<RegistrySubTree[]> for ValuesFuture
  and SubKeysFuture. Values/SubKeys evaluate those futures and return the cached
  caller-owned arrays directly. Tools.Windows.Registry.ReadTree creates cached
  reloadable address loaders that reopen/dispose keys when evaluated, without
  retaining live registry handles. Its bounded ReadTreeAsync counterpart fully
  materializes the tree with cancellation; deferred loading time excludes idle
  time between accesses. Choose lazy navigation or an eager stable capture
  according to the caller's operation, especially before exporting or comparing
  full state. RegistryValue's binary-data copy protects a separate stale-edit
  baseline; it is not a blanket reason to clone every detached tree array.

Verify first access, repeated access, failure/retry, invalidation, projection
evaluation and resource ownership for the chosen implementation. Do not assume
all implementations share those semantics or change them merely to simplify a
single consumer.
