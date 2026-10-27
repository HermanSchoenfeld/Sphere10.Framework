# Cache profiling

The `caches` domain measures the typed cache path and the cost of admission under
capacity pressure. It uses the real `ActionCache<int,TValue>` engine with its
explicit `ObjectCacheProjection` adapter, `ReapLevel` and deterministic LRU/FIFO policies.
Expiration, invalidation, pinned entries, pools, concurrent callers and callback
refills are deliberately separate correctness scenarios, not simulated here.

## Workloads

| Workload | Timed batch |
| --- | --- |
| `TypedHotKeys` | `ICache<int,TValue>.Get(int)` against fully cached keys. |
| `ObjectHotKeys` | `ICache.Get(object)` against the identical keys; each `int` is boxed during access. |
| `ObjectPreboxedHotKeys` | The same object interface using keys boxed during setup. This separates caller boxing from adapter overhead. |
| `SequentialPressure` | Unique new keys, starting with a full cache; every access must fetch. |
| `HotSetPressure` | A deterministic mixture of repeatedly used hot keys and unique cold keys. Defaults are 80% hot accesses using 10% of the preseed items. Eviction can cause additional hot-key refills. |

The generic cache does not implement the object contract. Both object workloads
use `AsObjectCache()` to create a borrowed projection over the same typed source.
The projection is created outside timing and disposed before the source during
cleanup. Its first entry-view creation and any conversion cost during access
are part of the object workload; preboxing excludes only caller key boxing.

Each invocation is one declared batch. Every iteration creates a new cache and
preseeds its full capacity outside timing. Immutable reference values and key
sequences are constructed once outside timing. Fetch callbacks return those
preallocated values; `--fetch-work` optionally adds deterministic CPU work per
miss. This does not represent disk, network or application deserialization cost.

The hot-key comparison uses the same reference-valued cache in all three cases,
so key boxing is isolated from value boxing. It does not claim allocation-free
typed access: synchronization scope factories and other cache machinery can
allocate, and BenchmarkDotNet records allocations for the entire batch.

`LongestIdle` means LRU and `Oldest` means FIFO. Both use operation order rather
than relying on distinct wall-clock timestamps. A capacity-triggered admission
releases at least `max(deficit, ceil(capacity * ReapLevel))` estimator units.
Weighted victims can overshoot the target. The `reaping` suite compares minimal
`1/capacity`, quarter-capacity and half-capacity batches for both policies.
For capacities where floating-point multiplication would round that minimal
target above one unit, the suite lowers the fraction by one representable step;
the exact fraction and effective release target remain in the configuration.

## Capacity and verification

Capacity is the size estimator's budget; it is not the managed heap footprint.
`Unit` values cost one unit. `Weighted` values cycle through one to four units;
the last preseed value is clipped when necessary to fill the budget exactly.
The same budget therefore holds fewer weighted values, which is explicit in
the retained baseline item counts. Compare configurations within their declared
size pattern, fetch cost and access distribution.

Fetch/removal callbacks record counts, size totals and keys in preallocated
arrays during the measured batch. That instrumentation is part of its cost.
Afterward, an independent linked-list reference model replays the access
sequence and checks every fetched key and victim, value checksum, final
membership, size accounting and capacity. Full replay, array enumeration,
verification, disposal and file writes are outside measured work.

Verified per-iteration files under `cache-metrics/<run-id>/` retain capacity,
preseed/hot-key counts, actual fetches, eviction batches, victim sizes, final
occupancy and checksums. They include warmup and measured iterations without
inventing a correspondence to BenchmarkDotNet sample stages. In particular,
fewer reap calls alone do not establish a better configuration: a larger batch
may evict useful hot keys and increase fetches. The independent Full JSON report
retains timing samples and allocations; `run.json` retains exact profiles,
settings, forwarded arguments and measurement caveats.

## Run and inspect

Historical `projection-bdn` captures measured the canonical typed contract and
explicit `ObjectCacheProjection`; `final-bdn` captures measured the earlier
implementation with an implicit object interface. Those capture files are no
longer shipped with the repository. Run a new campaign or supply external files
to the [recorded-capture checks](../../../tests/Sphere10.Framework.Utils.Profiler.Tests/README.md#external-historical-captures). Keep measurements from different source versions separate; each
phase retains its own worker identity and exact settings.

Run from the Framework root, in Release without a debugger:

```powershell
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- caches --help
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- caches --suite reaping --capacity 1024 --sizes Weighted --show-config

# Execution check only; Dry timings do not select a reap level.
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- caches --suite access --operations 65536 --filter '*HotKeys*' --job Dry

# Independent launches, exact one-batch invocations, retained outliers and workers.
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- caches --suite access --operations 65536 --filter '*HotKeys*' --strategy Monitoring --launchCount 2 --warmupCount 5 --iterationCount 10 --invocationCount 1 --unrollFactor 1 --outliers DontRemove --noForcedGCs --keepFiles

dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- caches --suite reaping --capacity 1024 --operations 8192 --sizes Unit --profiles lru-minimal-unit,lru-quarter-unit,fifo-minimal-unit,fifo-quarter-unit --filter '*Pressure*' --strategy Monitoring --launchCount 2 --warmupCount 5 --iterationCount 10 --invocationCount 1 --unrollFactor 1 --outliers DontRemove --noForcedGCs --keepFiles
```

Repeat the last command with `--sizes Weighted` and the corresponding profile
suffixes for weighted-size pressure. Add `--fetch-work 128` to assess additional
refill cost on a declared CPU workload. Use `--config` with a JSON array of
`CacheProfile` objects to replay exact custom capacities, policies and levels.
`--output` selects a new artifact directory. `--show-config` and domain help
inspect settings without creating artifacts; execution requires `--filter` or
`--list flat`.

The [profiler tests](../../../tests/Sphere10.Framework.Utils.Profiler.Tests/README.md)
exercise all five workloads across both policies, three reap levels and both
size patterns, as well as reset, metrics, fetch work and invalid controls. They
assert correctness and accounting, never a timing threshold. Use the
[general profiler report](../Studies/README.md) to read retained BenchmarkDotNet
Full JSON alongside exact configurations. Keep worker fingerprints separately
when retaining measurements; the launcher identity does not identify generated
BenchmarkDotNet workers.
