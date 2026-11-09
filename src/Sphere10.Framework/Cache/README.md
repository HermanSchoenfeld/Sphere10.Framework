# Caches

This folder holds the core library's synchronized in-memory caches. A cache fetches a value on a miss, keeps it under expiration and capacity policies, and raises notifications as entries are fetched and removed. The types belong to the `Sphere10.Framework` namespace in the `Sphere10.Framework` package.

## Choose a cache

| Type | Use when |
|---|---|
| `ActionCache<TKey, TValue>` | A delegate fetches one value per key. |
| `BulkFetchActionCache<TKey, TValue>` | One delegate returns the whole key/value set. The cache reloads that snapshot when a requested key is missing, expired or invalidated, or only on `ForceRefresh()` when `fetchOnceOnly` is set. |
| `PreloadedCache<TKey, TValue>` | A dictionary supplied at construction is the whole source. |
| `SessionCache<K, T>` | Callers add sessions with `Set` and refresh them with `KeepAlive`, and a background job removes sessions that stay idle for longer than the timeout. |
| `TextFileCache`, `BinaryFileCache` | File contents are keyed by path and reloaded when the file changes. Both derive from `FileCacheBase<TContent>` in the `IO` folder. |
| `CacheBase<TKey, TValue>` | A custom cache implements `Fetch`, `EstimateSize` and `CheckStaleness`. |

Bulk caches hold zero-sized entries, never reap and reject `Remove`. Reading a missing or expired session throws, because a session cache has nothing to fetch.

## Example

```csharp
using var cache = new ActionCache<int, string>(
	key => $"Value {key}",
	sizeEstimator: value => value.Length,
	reapStrategy: CacheReapPolicy.LongestIdle,
	maxCapacity: 1024,
	reapLevel: 0.25);

var value = cache[42];
```

The indexer fetches on a miss and returns the stored value afterwards. `Get(key)` returns the entry as a `CachedItem<TValue>` with its fetch and access times, access count, size and `CanPurge` pin.

## Contracts and projections

`ICache<TKey, TValue>` is the typed contract. It extends `ICacheBase`, which exposes synchronization, diagnostics and capacity operations without keys, values or entries. `CacheBase<TKey, TValue>` implements it with typed storage, fetch and staleness hooks, capacity accounting and victim selection, so typed access stores and returns values without conversion.

The nongeneric `ICache`, `CacheBase` and `CachedItem` specialize the generic types for `object` keys and values. `ICache<TKey, TValue>` does not inherit `ICache`, so a typed cache reaches an `ICache` consumer only through an explicit projection:

- `cache.AsObjectCache()` or `new ObjectCacheProjection<TKey, TValue>(cache)` creates the object view.
- `cache.AsProjection(...)` or `new ProjectedCache<TKey, TValue, TProjectedKey, TProjectedValue>(...)` exposes any other key and value types through four explicit conversions, following `ProjectedDictionary`. Results use the forward conversions, and lookup keys and written values use the inverse conversions.

A projection borrows its source, so disposing it detaches its event subscriptions without disposing the cache. Each projected entry is a reusable view over its source entry, which keeps metadata such as `CanPurge` live across reads, snapshots and removal events. See the [projection pattern](../../../docs/guidelines/patterns/projections.md).

## Policies

| Policy | Values |
|---|---|
| `ExpirationPolicy` | `SinceFetchedTime` measures expiry from the end of the fetch, `SinceLastAccessedTime` measures it from the last access, and `None` disables expiry. The window is `expirationDuration`, which defaults to `TimeSpan.MaxValue`. An invalidated entry counts as expired under every policy. |
| `StaleValuePolicy` | `AssumeNeverStale` skips staleness checks. `CheckStaleOnDemand` calls `CheckStaleness` when a stored entry is read. `BackgroundCheck` is a placeholder without an implementation. |
| `NullValuePolicy` | `CacheNormally` stores `null` like any other value, `ReturnButDontCache` returns it without keeping it, and `Throw` rejects it. |
| `CacheReapPolicy` | `LeastUsed`, `Largest`, `Smallest`, `LongestIdle` (LRU), `Oldest` (FIFO) and `ASAP` order the eviction victims. `None` evicts expired entries only. |

## Capacity and batch reaping

Capacity is measured in the units that `sizeEstimator` returns, which need not be bytes or entry counts. Without an estimator, `ActionCache` counts every value as zero units, so supply one for a bounded cache. A negative estimate is rejected, and so is a value larger than the whole capacity, before any entry is evicted.

`ReapLevel` must lie strictly between zero and one. Its default, `Sphere10FrameworkDefaults.CacheReapLevel`, is `0.25`, which is a configurable policy rather than a measured optimum for every workload. Only admission under capacity pressure applies it, by releasing the greater of the admission deficit and `ceil(capacity * ReapLevel)` units. A full cache with a capacity of 100 unit-sized values therefore frees at least 25 units before it admits the next value, which leaves 76 occupied units instead of evicting on every later miss. Eviction removes whole entries, so it can overshoot the target.

Pinned entries (`CanPurge == false`) are never reaped. Pins can stop a batch from reaching its extra target, but the admitted value must still fit within capacity. `Reap(minimumRelease)` releases the requested units in estimator terms on a best-effort basis, without applying `ReapLevel` again.

Expired and invalidated entries are evicted before live ones, and `ReapPolicy` orders the rest. `LongestIdle` and `Oldest` use operation order, so the public timestamps keep their expiry and diagnostic roles.

Expiry from fetch time starts when the fetch completes, so a slow fetch still receives its full window. Last-access timestamps are refreshed after the `ItemFetched` notification.

Replacing a value recomputes its size and reserves any extra capacity before it publishes the replacement. Removal callbacks can repopulate the cache, so admission checks the capacity that actually remains after reaping rather than counting attempted removals.

Cache operations are synchronized. `CachedItems` returns a membership snapshot whose entries are the live metadata objects. Hold the cache's `EnterWriteScope()` while a multi-step operation changes entry pinning.

A file cache records the file's UTC write time and length when it creates an entry, and it reloads the file when either differs, including a write time that moves backwards. `RetainCacheOnDelete` keeps serving the cached value after the file is deleted. These checks read file metadata only, so they do not give an atomic snapshot of concurrent external writes.

## Removal, purge and disposal

Cached values remain borrowed, so removing an entry or disposing the cache does not dispose its value. A consumer that owns its values releases them in an `ItemRemoved` handler, for example by calling the entry's `Dispose()`.

`Remove(key)` and `Purge()` ignore pins. `Purge()` removes the entries present when it starts, so entries that removal callbacks add stay stored and counted. Disposal blocks new admissions, attempts every original removal callback, clears membership and reports the callback failures together in an `AggregateException`. A callback that disposes the cache cannot make the outer fetch or replacement repopulate it.

A derived cache can override `OnItemRemoving` to prepare an entry before its removal commits. The entry stays cached, unpurged and counted against capacity until the hook returns. When the hook or the `ItemRemoving` event throws, an ordinary removal, purge or reap leaves that entry in place for a retry, and earlier successful removals stand. Nested removal and reaping skip the entry being prepared, and reentrant reads return it without refetching, even when it was invalidated. Replacing that entry or disposing the cache from the hook throws `InvalidOperationException`. Disposal still attempts every original entry and clears membership when a preparation fails.

## Notifications

Every cache notification follows the [event and template-hook pattern](../../../docs/guidelines/patterns/type-hierarchies.md#event-notifications-and-template-hooks). The operation calls a protected nonvirtual `Notify*` method, which calls the corresponding protected virtual `On*` hook and then the public event.

| Event | Lifecycle boundary |
|---|---|
| `ItemFetching` | Immediately before a miss fetches a value. |
| `ItemFetched` | After fetch completion and the applicable cache-admission policy. |
| `ItemRemoving` | Before removal commits, while membership and capacity accounting remain intact. |
| `ItemRemoved` | After membership removal, capacity release and marking the entry purged. |

An exception from the removing hook or its event prevents an ordinary eviction and allows a retry. The same reentrancy guards cover both callbacks. Typed and object projections forward each notification with the matching live entry view, and they take ownership of neither the cache nor its payloads.

## Shared capacity

Use `PooledCacheReaper` when differently typed caches share one budget. Reapers implement `ICacheReaper` over `ICacheBase`, so they coordinate typed caches without an object projection. A cache without a supplied reaper owns its budget through an `IsolatedCacheReaper`.

```csharp
var reaper = new PooledCacheReaper(1024);
using var names = new ActionCache<int, string>(
	key => $"Name {key}", value => value.Length,
	CacheReapPolicy.LongestIdle, maxCapacity: 1024, reaper: reaper);
using var payloads = new ActionCache<string, byte[]>(
	key => new byte[16], value => value.Length,
	CacheReapPolicy.Oldest, maxCapacity: 1024, reaper: reaper);
```

Each member registers during construction, before it holds any entries, and must declare the reaper's capacity in the same estimator units. Members borrow the reaper as their synchronization parent and must keep it for their lifetime. The requesting member applies its `ReapLevel` once to the combined capacity. The reaper reaps the requester first and then the other members, starting with the longest idle, and it rechecks the combined size after callbacks. It borrows its members and does not dispose them. Disposing a member ends its membership but keeps the parent lock, so open or waiting scopes stay valid. A member removed with `Deregister` can no longer add or replace values, so prefer disposing members.

## Memory-paged list page cache

`MemoryPagedListBase<TItem>` keeps its loaded pages in a private `CacheBase<long, IMemoryPage<TItem>>` subclass. It saves a dirty page in `OnItemRemoving` and unloads the page after removal, so a failed save keeps the loaded page and its accounting without reinsertion. `PageSaving` and `PageSaved` run while the page is still cached, and reentrant reads and earlier changes use that resident page. When a `PageSaved` handler dirties the page again, the eviction throws and keeps the changes for the next attempt. A nested flush processes the other pages and skips the page being saved.

The page cache reserves a page's capacity before it loads the page, including during loading callbacks. `PageLoaded` and the page access notification run after admission while the page is pinned against reaping, and a failure there leaves the admitted page counted and retryable. A callback can still call `Flush()`, but when that unloads the page being opened, the open throws instead of returning an unloaded page and can be retried after the callback completes. Nested open-page scopes restore the previous pin state.

## Tests and profiling

The [cache fixtures](../../../tests/Sphere10.Framework.Tests/Cache) cover typed and object dispatch, event subscription identity, size replacement, expiration, fractional batches, pinned entries, callbacks, concurrency and pooled budgets. The [page-cache regressions](../../../tests/Sphere10.Framework.Tests/Collections/Paged/MemoryPagedListCacheTests.cs) exercise the dirty-page save, unload and reload path with large logical budgets. The [cache profiler](../../../utils/Sphere10.Framework.Utils.Profiler/Caches/README.md) compares typed and object access and the cost of LRU and FIFO admission, with verified fetch and eviction counts. A larger `ReapLevel` can reduce victim selection while it increases refetches, so evaluate it against the application's locality, entry weights and policy rather than assuming that one fraction is fastest.
