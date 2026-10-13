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
