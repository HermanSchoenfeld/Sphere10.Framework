# Caches

`ICache<TKey, TValue>` is the canonical contract; it does not inherit the object-key/value `ICache`. `CacheBase<TKey, TValue>` owns typed storage, fetch/staleness hooks, capacity accounting and victim selection. The nongeneric `ICache`, `CacheBase` and `CachedItem` specialize their generic counterparts for objects. Typed access stores and returns values directly.

`ProjectedCache<TKey, TValue, TProjectedKey, TProjectedValue>` exposes any source/destination key and value types through four explicit conversions, following `ProjectedDictionary`. Source results use the forward conversions; lookup keys and written values use the inverse conversions. Construct it directly or use `AsProjection`.

Use `cache.AsObjectCache()` or `new ObjectCacheProjection<TKey, TValue>(cache)` when a consumer requires `ICache`. Conversion occurs at this explicitly named object boundary. A projection borrows its source; disposing it detaches its subscriptions without disposing the cache. Projected entries preserve live source metadata, including `CanPurge`, and reuse their view across reads, snapshots and removal events. See the [projection pattern](../../../docs/guidelines/patterns/projections.md).

Use `ActionCache<TKey, TValue>` for an individual fetch delegate, `BulkFetchActionCache` or `PreloadedCache` for whole-source refresh, and `SessionCache` for session expiry. File caches track the source's UTC modification time and length independently of fetch/expiry timestamps. Cached payloads remain borrowed: removing an entry or disposing the cache does not dispose its value. Use removal callbacks when the consumer owns resources that must be released.

## Capacity and batch reaping

Capacity is measured in the units returned by `sizeEstimator`, which need not be bytes or entry counts. The omitted `ActionCache` estimator returns zero; specify one for a bounded cache. Negative estimates are rejected, and values larger than the capacity are rejected before evicting existing entries.

```csharp
using var cache = new ActionCache<int, string>(
	key => $"Value {key}",
	sizeEstimator: value => value.Length,
	reapStrategy: CacheReapPolicy.LongestIdle,
	maxCapacity: 1024,
	reapLevel: 0.25);

var value = cache[42];
```

`ReapLevel` must be finite and strictly between zero and one. Its initial default is `0.25`; it is a configurable policy, not a measured optimum for every workload. Only capacity pressure applies the fraction. The requested release is the greater of the actual admission deficit and `ceil(capacity * ReapLevel)`. A full unit-sized capacity of 100 therefore frees at least 25 units before admitting the next unit, leaving 76 occupied units instead of reaping on every subsequent miss. Whole entries can overshoot the target.

Pinned entries (`CanPurge == false`) are excluded from reaping. The extra batch target is best effort when pinned entries limit the available victims, but admission must still fit within capacity. `Reap(minimumRelease)` performs direct best-effort victim selection in estimator units without applying the fraction again. Expired or explicitly invalidated candidates precede live candidates; the configured policy orders the remaining victims. FIFO/LRU use operation order, while public timestamps retain their expiration and diagnostic roles.

Fetch-based expiry starts when fetching completes; last-access timestamps are refreshed after fetched notifications. Slow fetches therefore receive their full expiry window. A file cache detects changes to its captured source timestamp or length, including a timestamp moving backwards, and preserves `RetainCacheOnDelete`. Its metadata check does not provide an atomic snapshot of external file writes.

Replacing a value recomputes its size and reserves any additional capacity before publishing the replacement. Removal callbacks can repopulate a cache, so admission checks actual remaining capacity after reaping rather than trusting a count of attempted removals. Cache operations synchronize their state; `CachedItems` returns a membership snapshot whose entries retain shared metadata identity. Use the cache's scopes when changing entry pinning across a multi-step operation.

`Purge()` removes the entries present at the start of the operation; entries added by removal callbacks remain stored and accounted for. Disposal prevents new admissions, attempts every original removal callback, clears membership and reports callback failures together. Callbacks that dispose a cache cannot cause the outer fetch or replacement to repopulate it.

Derived caches can override `OnItemRemoving` to prepare an entry before removal is committed. The entry remains cached, unpurged and counted against capacity until the hook returns successfully. A failure during ordinary removal, purge or reaping leaves that entry available for retry; earlier successful removals and independent callback changes remain effective. Nested removal and reaping skip the entry being prepared. Reentrant reads use that entry without refreshing it, including when it was invalidated. Replacing the same entry or starting cache disposal from the preparation hook throws `InvalidOperationException`. `ItemRemoved` retains its post-removal timing and observes the released capacity and purged entry. Disposal still attempts all original entries, clears membership and reports failures together.

Memory-paged lists save dirty pages in this preparation hook and unload them after removal. Save failures therefore retain the original loaded page and cache accounting without reinsertion. `PageSaving` and `PageSaved` run while the page remains cached; reentrant reads and changes made before the save use that resident page. If a `PageSaved` handler dirties it again, eviction throws and retains those changes for the next attempt. A nested flush can process other pages but skips an in-progress save. Transactional flush continues to stage pages; commit and rollback keep their existing publication and discard boundaries.

The page cache reserves capacity before loading a page, including during loading callbacks. `PageLoaded` and the corresponding access notification run after cache admission with the page pinned against capacity reaping. A notification failure leaves the admitted page accounted for and retryable. Explicit callback `Flush()` retains its ability to unload cached pages; if it unloads the page being opened, that open throws rather than returning an unloaded page, and can be retried after the callback completes. Nested open scopes restore the previous pin state.

## Notifications

Every cache notification follows the [event/template-hook pattern](../../../docs/guidelines/patterns/type-hierarchies.md#event-notifications-and-template-hooks): the operation calls a protected nonvirtual `Notify*` method, which calls the corresponding protected virtual `On*` hook and then the public event. The pattern applies to all four notifications, including the new pre-removal event; adding it for one requires completing the whole event family.

| Event | Lifecycle boundary |
|---|---|
| `ItemFetching` | Immediately before a miss fetches a value. |
| `ItemFetched` | After fetch completion and the applicable cache-admission policy. |
| `ItemRemoving` | Before removal commits, while membership and capacity accounting remain intact. |
| `ItemRemoved` | After membership removal, capacity release and marking the entry purged. |

An exception from either the removing hook or its event prevents ordinary eviction and allows retry. The same reentrancy guards cover both callbacks. Disposal retains its best-effort cleanup and aggregated-error behavior. Typed and object cache projections forward each notification with the corresponding live entry view; they do not take ownership of the cache or its payloads.

## Shared capacity

Use `PooledCacheReaper` when differently typed caches need one combined budget. The existing `ICacheReaper` hierarchy operates on `ICacheBase`, which exposes synchronization, diagnostics and capacity operations without keys, values or entries. Each cache uses `IsolatedCacheReaper` by default. A custom reaper can coordinate typed implementations through this shared contract without object projection.

```csharp
var reaper = new PooledCacheReaper(1024);
using var names = new ActionCache<int, string>(
	key => $"Name {key}", value => value.Length,
	CacheReapPolicy.LongestIdle, maxCapacity: 1024, reaper: reaper);
using var payloads = new ActionCache<string, byte[]>(
	key => new byte[16], value => value.Length,
	CacheReapPolicy.Oldest, maxCapacity: 1024, reaper: reaper);
```

Members register during construction and must declare the reaper's capacity in the same estimator units. They borrow one stable synchronization parent; do not change it during their lifetime. The requesting member's `ReapLevel` is applied once to the combined capacity. The reaper tries the requester first, then idle members, and rechecks actual combined consumption after callbacks. It borrows members and does not dispose them. Disposing a cache removes its membership while retaining the parent lock identity for open or waiting scopes. Manual deregistration ends managed admission: a removed member cannot add or replace values outside the shared budget. Prefer disposing members for normal cleanup.

## Validation and profiling

The [cache fixtures](../../../tests/Sphere10.Framework.Tests/Cache) cover typed/object dispatch, event subscription identity, size replacement, expiration, fractional batches, pinned entries, callbacks, concurrency and pooled budgets. The [page-cache regressions](../../../tests/Sphere10.Framework.Tests/Collections/Paged/MemoryPagedListCacheTests.cs) exercise the real dirty-page save/unload/reload consumer and large logical budgets. The [cache profiler](../../../utils/Sphere10.Framework.Utils.Profiler/Caches/README.md) compares typed/object access and actual LRU/FIFO admission costs with verified fetch and eviction counts. Reaping more headroom can reduce victim-selection frequency while increasing refetches; evaluate the application's locality, entry weights and chosen policy rather than assuming one fraction is universally fastest.
