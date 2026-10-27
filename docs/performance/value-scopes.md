# Value scope performance, 24 October 2026

**Historical evidence:** all measurements and passing test counts in this report
precede the final API consolidation. The current `EnterReadScope()`,
`EnterWriteScope()`, `EnterAccessScope()` and `EnterOpenPageScope()` contracts
return structs through interfaces as well as concrete references. The separate
value methods and old `IDisposable` interface returns were removed afterward.
No performance capture or test run was repeated for that consolidation.

The [original evidence archive](evidence/20261024-value-scopes/measured-evidence.zip)
and [measured harness archive](evidence/20261024-value-scopes/harness/measured-harness.zip)
preserve the exact recorded bytes independently of checkout line endings.

Subsequent source changes also keep cleanup scopes in concrete mutable fields
through UI lifetimes and profiler storage fixtures. Those source identities
differ from the measured snapshots; no new allocation or timing claim is made
for the field migrations.

Concrete synchronous scopes removed the per-acquisition scope object, callback
delegate and closure from the measured built-in lock paths. Warm monitor,
reader/writer, synchronized collection and cache-hit batches allocated zero
bytes on their calling thread. Measured storage consumers also allocated less,
although their serialization, indexing, stream and transaction work still
allocated. The then-existing `IDisposable` interface entry points retained a
boxing cost. Lower allocation did not establish that every workload ran faster.

This report compares commit `415de13c90743f96f86d6a8273d6e3c3f23f76e9` with the
measured value-scope implementation before consolidation. That baseline already
contains the earlier span, B-tree, cache and storage optimizations. These results
measure the additional scope changes; they do not describe the cumulative
improvement since the start of that work.

## Evidence and measurement boundaries

The retained [comparison JSON](evidence/20261024-value-scopes/comparison.json),
[28-workload CSV](evidence/20261024-value-scopes/comparison.csv) and
[GC stress CSV](evidence/20261024-value-scopes/gc-comparison.csv) derive from six
complete raw captures:

| Capture | Purpose | Verified measured batches |
|---|---|---:|
| [baseline-a](evidence/20261024-value-scopes/captures/baseline-a/scope-consumers.json) | First baseline host | 140 |
| [optimized-a](evidence/20261024-value-scopes/captures/optimized-a/scope-consumers.json) | First optimized host | 140 |
| [optimized-b](evidence/20261024-value-scopes/captures/optimized-b/scope-consumers.json) | Second optimized host | 140 |
| [baseline-b](evidence/20261024-value-scopes/captures/baseline-b/scope-consumers.json) | Second baseline host | 140 |
| [baseline-gc-a](evidence/20261024-value-scopes/captures/baseline-gc-a/scope-consumers.json) | Baseline GC stress | 10 |
| [optimized-gc-a](evidence/20261024-value-scopes/captures/optimized-gc-a/scope-consumers.json) | Optimized GC stress | 10 |

Normal captures ran in fresh, serial NUnit hosts in
baseline/optimized/optimized/baseline order. Each host ran two warmup batches
and five measured batches per workload. The table below combines both hosts
for ten samples per variant. GC stress uses five measured batches per variant.
The same three harness source files were compiled against each Core revision;
recompilation was necessary because concrete return signatures changed. Their
exact measured versions are retained separately from the subsequently migrated
source tree:

- [Screening harness](evidence/20261024-value-scopes/harness/ValueScopePerformanceTests.cs).
- [Consumer benchmark](evidence/20261024-value-scopes/harness/ValueScopeConsumerBenchmarks.cs).
- [BenchmarkDotNet runner](evidence/20261024-value-scopes/harness/ValueScopeConsumerBenchmarkTests.cs).

The retained environment is .NET 10.0.13, x64, Windows 10 build 19045, 20 logical
processors, workstation GC and Interactive latency mode. The stopwatch reports
10,000,000 ticks per second. Captures retain the loaded assemblies' SHA256,
versions and MVIDs, the reflected public return types, source hashes, runtime
settings, process identity and data root. The baseline and optimized Core
binaries differ; every capture of each variant identifies the same Core binary.
The JSON is the authority for exact identities and unrounded values.

Each measured batch owns a fresh deterministic fixture. Fixture creation,
warmup, semantic verification, reopening, disposal, source/binary hashing and
JSON output are outside the measured region. The measured delegate runs
synchronously on its owner thread. Its checksum and resulting contents are
verified afterward, including persisted mutations and Merkle integrity where
applicable. No forced garbage collections are requested.

`ThreadAllocatedBytes` covers all synchronous work performed by the batch,
including the actual consumer operation. It is not an estimate from scope
sizes. Process allocation, generation counts and total GC pause deltas are
also retained, but can include test-host work on other threads. Timing uses
`Stopwatch` ticks; GC pause duration uses `TimeSpan` ticks. They must be
converted with their respective units even though both happen to be 100 ns
in this capture.

## Complete 28-workload comparison

Every value below is a median. Bytes and milliseconds describe the **whole
batch**, not one internal lock acquisition. Positive reduction means less;
negative time reduction means the optimized screening median was slower.
Percentages are computed from unrounded medians.

**Timing is provisional screening evidence.** Several batches are shorter than
a millisecond, the two capture rounds show timing variation, and the sample
count does not provide independent BenchmarkDotNet confirmation. The table
retains slower results as well as faster results.

<!-- BEGIN VALUE SCOPE SCREENING TABLE -->
| Workload | Actions/batch | Before bytes | After bytes | Allocation reduction | Before ms | After ms | Time reduction |
|---|---:|---:|---:|---:|---:|---:|---:|
| CriticalConcrete | 16,384 | 2,097,152 | 0 | 100.00% | 0.799 | 0.643 | 19.50% |
| CriticalInterface | 16,384 | 2,097,152 | 786,432 | 62.50% | 1.010 | 0.892 | 11.66% |
| ReadConcrete | 16,384 | 1,572,864 | 0 | 100.00% | 0.992 | 0.877 | 11.56% |
| ReadInterface | 16,384 | 1,572,864 | 655,360 | 58.33% | 1.469 | 1.223 | 16.73% |
| WriteConcrete | 16,384 | 1,572,864 | 0 | 100.00% | 1.811 | 0.900 | 50.27% |
| WriteInterface | 16,384 | 1,572,864 | 655,360 | 58.33% | 1.387 | 1.255 | 9.54% |
| CleanupAction | 16,384 | 655,360 | 0 | 100.00% | 0.329 | 0.111 | 66.27% |
| CleanupState | 16,384 | 2,228,224 | 0 | 100.00% | 0.837 | 0.125 | 85.09% |
| BorrowHasher | 16,384 | 2,752,512 | 524,288 | 80.95% | 18.126 | 16.141 | 10.95% |
| ListRead | 2,048 | 196,608 | 0 | 100.00% | 0.218 | 0.154 | 29.04% |
| ListWrite | 2,048 | 196,608 | 0 | 100.00% | 0.227 | 0.153 | 32.42% |
| ListEnumeration | 2,048 | 491,520 | 196,608 | 60.00% | 1.515 | 1.536 | -1.42% |
| DictionaryRead | 2,048 | 196,608 | 0 | 100.00% | 0.431 | 0.172 | 60.07% |
| DictionaryWrite | 2,048 | 196,608 | 0 | 100.00% | 0.240 | 0.168 | 30.14% |
| DictionaryEnumeration | 2,048 | 507,904 | 212,992 | 58.06% | 2.060 | 1.649 | 19.98% |
| CacheHits | 2,048 | 196,608 | 0 | 100.00% | 0.549 | 0.359 | 34.59% |
| ClusterRead | 128 | 167,936 | 93,184 | 44.51% | 0.454 | 0.406 | 10.60% |
| ClusterWrite | 128 | 463,296 | 343,488 | 25.86% | 2.430 | 1.603 | 34.05% |
| ObjectStreamRead | 128 | 269,312 | 178,176 | 33.84% | 0.588 | 0.498 | 15.16% |
| ObjectStreamUpdate | 128 | 854,912 | 673,664 | 21.20% | 4.649 | 3.733 | 19.70% |
| MemoryIndexedRead | 32 | 119,336 | 98,856 | 17.16% | 1.635 | 1.536 | 6.05% |
| MemoryMerkleIndexedRead | 32 | 119,336 | 98,856 | 17.16% | 1.171 | 0.605 | 48.35% |
| FileIndexedRead | 32 | 293,160 | 182,664 | 37.69% | 1.690 | 1.124 | 33.52% |
| FileMerkleIndexedRead | 32 | 293,608 | 182,880 | 37.71% | 1.590 | 0.860 | 45.91% |
| MemoryMutationFlush | 32 | 25,174,744 | 24,757,632 | 1.66% | 110.539 | 55.347 | 49.93% |
| MemoryMerkleMutationFlush | 32 | 25,396,252 | 24,955,336 | 1.74% | 59.881 | 56.750 | 5.23% |
| FileMutationCommit | 32 | 61,145,460 | 42,107,552 | 31.14% | 253.462 | 208.961 | 17.56% |
| FileMerkleMutationCommit | 32 | 61,502,064 | 42,370,780 | 31.11% | 184.769 | 235.140 | -27.26% |
<!-- END VALUE SCOPE SCREENING TABLE -->

The action counts have different meanings across these workloads:

| Workloads | One counted action and fixture |
|---|---|
| Critical/Read/Write | One synchronous scope acquisition and disposal. Historical `Interface` rows deliberately called the then-existing `IDisposable` interface method. |
| CleanupAction/CleanupState | One cleanup helper creation and disposal using a callback prepared before timing. The state case passes a static callback and a separate counter object. |
| BorrowHasher | Borrow SHA-256, hash a prepared 64-byte payload into a prepared output buffer, then return the hasher. |
| ListRead/ListWrite, DictionaryRead/DictionaryWrite | One indexer access or update over 64 prepared entries. |
| ListEnumeration/DictionaryEnumeration | One complete enumeration of all 64 entries, including enumerator creation and disposal. |
| CacheHits | One hit in a prewarmed, 64-entry typed `ActionCache<int, int>`; verification requires no additional fetch. |
| ClusterRead/ClusterWrite | Open one of 16 prepared clustered streams, read or overwrite its 64-byte payload, then close it. Cluster size is 256 bytes. |
| ObjectStreamRead/ObjectStreamUpdate | Load or update one of 16 serialized `long` items over a clustered stream. |
| Memory/File indexed reads | One shuffled, indexed identity lookup over 64 records. The Merkle variants enable SHA-256 integrity. |
| Memory/File mutations | One update/reindex action over the same store; the **batch** also includes one memory `Flush` or one durable file commit. |

The ObjectSpace fixtures use B-tree order 64, 4,096-byte clusters, 65,536-byte
pages, 1 MiB maximum memory, 32-byte payloads and explicit saves. They verify
updates after the measured flush/commit. These are warmed small-store cases,
not disk throughput, cold-cache, concurrent contention or scaling studies.

### What the allocation numbers establish

The direct monitor path falls from 128 bytes per acquisition to zero. Direct
read/write scopes fall from 96 bytes to zero. Prepared simple and state cleanup
helpers fall from 40 and 136 bytes respectively to zero. Synchronized list and
dictionary indexer operations and warmed cache hits remove their measured
96-byte allocation per action.

The historical interface calls allocated 48 bytes per monitor scope and 40 bytes
per read/write scope in this x64 capture. Their reductions came from replacing
the old scope/closure/delegate combination with one boxed value. At capture
time, separate concrete-return interface methods were covered by zero-allocation
contract tests. The final consolidation removed that dual API and changed the
standard interface methods themselves to return structs. The `CriticalInterface`,
`ReadInterface` and `WriteInterface` rows remain an unchanged record of the
pre-consolidation contract; they do not describe the current interface return
boundary. Its effect has not been remeasured.

Complete list enumeration falls from 240 to 96 bytes per traversal, and complete
dictionary enumeration from 248 to 104 bytes. The enumerator objects and inner
collection enumeration still allocate. The change removes the separately
allocated scope and disposal callback while one mutable field retains the
acquisition.

Borrow/hash/return falls from 168 to 32 bytes per action. The remaining
32-byte allocation is the `ConcurrentStack` node allocated when returning the
hasher to its pool. This workload is **not** allocation-free; removing that
node would require a separate pool implementation and measurement.

Storage reductions include every scope reached by the real operation. They do
not mean the object payload, serializer, B-tree, file-page cache or transaction
became allocation-free. The small allocation reduction in memory mutation
batches also shows why eliminating scopes does not solve every dominant
allocation source.

### Timing limits visible in the data

The optimized screening medians are slower for list enumeration (1.42%) and
Merkle file mutation (27.26%). The ordinary memory mutation median is 49.93%
lower, while Merkle memory mutation is 5.23% lower. These differences are retained
without treating them as established storage speed changes from short captures.
Warmup, GC/test-host activity and temporal variation remain possible contributors.
The allocation reductions survive this variation; wall-time claims require
the independent-process results below.

## GC stress

Each case performs 2,000,000 concrete acquisitions per measured batch. Values
are medians of five batches, without requested forced collections.

<!-- BEGIN VALUE SCOPE GC TABLE -->
| Workload | Before bytes | After bytes | Before Gen0 | After Gen0 | Before GC pause ms | After GC pause ms | Before elapsed ms | After elapsed ms |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| CriticalGcStress | 256,000,000 | 0 | 25 | 0 | 9.772 | 0.000 | 91.147 | 76.662 |
| ReadGcStress | 192,000,000 | 0 | 18 | 0 | 7.886 | 0.000 | 114.268 | 112.416 |
<!-- END VALUE SCOPE GC TABLE -->

Median Gen1 and Gen2 counts are zero before and after for both cases. Removing
128 or 96 bytes per acquisition removes the corresponding 256 MB or 192 MB
of calling-thread allocation from each batch. No Gen0 collection or GC pause
was observed in the optimized measured regions. This is a statement about
these captured intervals, not a guarantee that the application never collects.
Generation and pause counters are process-wide and cannot attribute each
collection exclusively to the scope loop.

## Independent BenchmarkDotNet confirmation

Both final runs completed all seven cases successfully in separate .NET 10
workers. This independently measured comparison uses means and the reported
99.9% confidence intervals; it is separate from the screening medians above.

The baseline evidence includes its
[run manifest](evidence/20261024-value-scopes/bdn/baseline/run.json),
[full JSON](evidence/20261024-value-scopes/bdn/baseline/Sphere10.Framework.Utils.Profiler.Tests.Scopes.ValueScopeConsumerBenchmarks-report-full.json),
[CSV](evidence/20261024-value-scopes/bdn/baseline/Sphere10.Framework.Utils.Profiler.Tests.Scopes.ValueScopeConsumerBenchmarks-report.csv),
[formatted report](evidence/20261024-value-scopes/bdn/baseline/Sphere10.Framework.Utils.Profiler.Tests.Scopes.ValueScopeConsumerBenchmarks-report-github.md),
[benchmark log](evidence/20261024-value-scopes/bdn/baseline/Sphere10.Framework.Utils.Profiler.Tests.Scopes.ValueScopeConsumerBenchmarks-20261024-213039.log)
and [test-host result](evidence/20261024-value-scopes/bdn/baseline/test-host.log).
The optimized evidence retains the corresponding
[manifest](evidence/20261024-value-scopes/bdn/optimized/run.json),
[JSON](evidence/20261024-value-scopes/bdn/optimized/Sphere10.Framework.Utils.Profiler.Tests.Scopes.ValueScopeConsumerBenchmarks-report-full.json),
[CSV](evidence/20261024-value-scopes/bdn/optimized/Sphere10.Framework.Utils.Profiler.Tests.Scopes.ValueScopeConsumerBenchmarks-report.csv),
[formatted report](evidence/20261024-value-scopes/bdn/optimized/Sphere10.Framework.Utils.Profiler.Tests.Scopes.ValueScopeConsumerBenchmarks-report-github.md),
[benchmark log](evidence/20261024-value-scopes/bdn/optimized/Sphere10.Framework.Utils.Profiler.Tests.Scopes.ValueScopeConsumerBenchmarks-20261024-213212.log)
and [test-host result](evidence/20261024-value-scopes/bdn/optimized/test-host.log).
Each variant's directory also contains fourteen `batch-*.json` child manifests.

The [measured consumer benchmark](evidence/20261024-value-scopes/harness/ValueScopeConsumerBenchmarks.cs)
selects seven real consumers: list reads, dictionary reads, cache hits,
cluster reads, ObjectStream reads, Merkle memory indexed reads and Merkle file
mutation/commit. Each invocation is one full batch with the same action count
and verification as the screening fixture. Fresh fixtures are created outside
timing for each iteration; verification, reopen and disposal also remain
outside timing.

The executed job uses two independent .NET 10 worker launches, three warmup
iterations and eight measured iterations per launch, one invocation and an
unroll factor of one. It requests neither forced GC nor a power-plan change.
Both variants used BenchmarkDotNet 0.16.0-preview.2, SDK 10.0.402 and .NET
10.0.13 x64 RyuJIT on the same Intel Core i9-10900K host. All fourteen benchmark
cases retain sixteen actual workload samples and six warmup samples each:
224 actual samples and 84 warmup samples across both variants. Each of the 28
child manifests records 74 semantically verified invocations, which also include
calibration and diagnostic work; that count is not the actual-sample count.

### Complete independent comparison

One operation is a **complete batch** with the action count in the screening
table. Means and errors below are in microseconds per batch. `± Error` is half
the reported 99.9% confidence interval. `N` shows the baseline/optimized sample
counts used by BenchmarkDotNet's statistics, out of sixteen raw actual samples
per variant and case. The job used its default outlier policy; all raw actual
samples remain in the full JSON even when excluded from the reported statistics.
There are 101 samples in the statistics for each variant after filtering.

<!-- BEGIN VALUE SCOPE BENCHMARKDOTNET TABLE -->
| Workload | N before/after | Before mean ± Error μs | After mean ± Error μs | Time reduction | Before bytes | After bytes | Allocation reduction |
|---|---:|---:|---:|---:|---:|---:|---:|
| CacheHits | 16/15 | 174.825 ± 3.943 | 149.747 ± 4.136 | 14.34% | 196,608 | 0 | 100.00% |
| ClusterRead | 14/16 | 84.214 ± 2.381 | 75.369 ± 3.358 | 10.50% | 161,792 | 87,040 | 46.20% |
| DictionaryRead | 14/13 | 79.414 ± 2.250 | 56.615 ± 1.024 | 28.71% | 196,608 | 0 | 100.00% |
| FileMerkleMutationCommit | 14/15 | 80,892.814 ± 3,370.873 | 81,764.800 ± 11,263.363 | -1.08% | 61,498,208 | 42,357,320 | 31.12% |
| ListRead | 15/13 | 81.007 ± 3.763 | 52.500 ± 0.765 | 35.19% | 196,608 | 0 | 100.00% |
| MemoryMerkleIndexedRead | 13/13 | 245.977 ± 1.860 | 244.431 ± 3.929 | 0.63% | 119,336 | 98,856 | 17.16% |
| ObjectStreamRead | 15/16 | 118.680 ± 3.841 | 102.350 ± 1.975 | 13.76% | 250,880 | 159,744 | 36.33% |
<!-- END VALUE SCOPE BENCHMARKDOTNET TABLE -->

These allocation values are the MemoryDiagnoser's managed bytes per batch.
Separate worker warmup and diagnostic execution can produce different storage
totals from direct NUnit calling-thread measurement. For example, the optimized
cluster read is 87,040 bytes here and 93,184 bytes in screening. Both comparisons
retain their original boundaries; their byte totals must not be mixed to derive
another reduction. The diagnostic file/Merkle batch reports six Gen0 collections
before and four after, with zero Gen1/Gen2 for both. The displayed Gen0 rate of
6,000/4,000 is per 1,000 **batches**, not per item or individual lock.

### Conclusions and timing limits

The independent diagnostics confirm zero managed allocation for list reads,
dictionary reads and cache hits, and lower allocation for all four selected
storage consumers. File/Merkle mutation removes 19,140,888 managed bytes per
32-action commit batch, about 18.25 MiB, while retaining its correctness checks.
This reinforces the scope-allocation result rather than promising an
allocation-free storage operation.

For these runs, list reads, dictionary reads, cache hits, cluster reads and
ObjectStream reads have lower means and nonoverlapping reported intervals.
Their mean reductions range from 10.50% to 35.19%. These are evidence of lower
latency for those selected warmed batches on this host. Every case in both
variants carries BenchmarkDotNet's minimum-iteration-time warning: observed
iterations are below its recommended 100 ms. The six short read batches are
approximately 0.05–0.25 ms. Two launches and reported intervals do not remove
systematic effects from short iterations or measurements made at different
times, so these percentages are not general throughput guarantees.

Merkle memory indexed reads show 0.63% lower filtered mean with overlapping
intervals. Merkle file mutation/commit shows 1.08% higher filtered mean with a
much wider optimized interval, also overlapping. Neither establishes a latency
improvement or regression. The unfiltered actual-sample means are approximately
297.588 to 303.969 μs for Merkle memory reads and 81.443 to 81.506 ms for Merkle
file mutation. Keeping those samples exposes the impact of default outlier
filtering. In particular, the file/Merkle screening slowdown of 27.26% is not
reproduced as a resolved difference in this independent run. Allocation savings
are the reliable result for these two consumers.

### Launcher and measured worker identities

The two BDN run manifests identify the parent Core assemblies used by the
screening captures. Their three harness-source SHA256 values match screening
exactly. BenchmarkDotNet rebuilds the measured workers under its `.bdn` output
directory; those binaries embed different PDB paths from the external screening
build. The measured child Core SHA256/MVID therefore differs from its parent's.
The manifests retain both identities; they are **not identical binaries**.

```text
Baseline parent Core SHA256:
0497D33F0317154AA23B4536D2EBEE7BDBB74FF072C611B9EE337EDEEFC4D01D
Baseline measured child Core SHA256:
7D5DD0B145C178DCD5F59C426BD99F1206DA7BCF309133C26A83B1452C628E9A
Optimized parent Core SHA256:
E26DE98B1D1F790FBEE320A7E9478F9F99851C52AA41AC517987DD115B1F99FD
Optimized measured child Core SHA256:
8904668A6CC1213613F7C6B31A27825E5C0BBB6C16518AFF1DED342B8EBA7512
```

All fourteen children within each variant report one consistent Core hash and
MVID, plus a consistent generated harness executable hash. Baseline and optimized
children report different Core identities. The retained evidence reader checks
these boundaries, both launches, all raw sample stages, verified batch counts,
allocation diagnostics and the statistics used in the table. It does not equate
launcher identity with executed worker identity or assert timing thresholds.

## Current API and ownership after consolidation

This section describes the current source contract. The measured versions above
predate these final API names and interface signatures; their captured binary
identities and results remain historical evidence.

Built-in monitor and read/write providers return concrete `AccessScope`,
`ReadScope` and `WriteScope` values from their ordinary public entry points.
The interfaces expose those same names and concrete return types. Built-in
forwarders pass the values through the storage, collection, repository and cache
chain. There are no parallel `IDisposable` entry points, fallback adapters or
runtime `GetType()` gates. Custom providers implement the concrete return
contract and can construct a scope with their cleanup `Action`; creating that
callback can still allocate. Removing an interface boxing boundary does not
remove allocations inside custom acquisition or cleanup.

```csharp
var owner = new SynchronizedObject();
using (owner.EnterReadScope()) {
	// Concrete value result.
}
ISynchronizedObject provider = owner;
using (provider.EnterReadScope()) {
	// The same concrete result through the interface.
}
```

`ValueActionScope` and `ValueActionScope<TState>` implement synchronous
`IDisposable` directly. A prepared/static callback avoids a per-call delegate
or captured closure; a newly capturing lambda can still allocate. `HasherScope`
owns a single pool return, `PageScope<TItem>` owns one page lifetime, and
`RestorePositionScope` restores a borrowed stream's position. Default values
own nothing. Successful repeated disposal of the **same mutable instance** is
harmless. Copying a value creates independent disposal state, so disposing two
copies can release twice or return a mutable hasher to the pool twice. Assigning
one to `IDisposable`, or binding its `Dispose` method to a delegate, captures a
boxed/copied owner.

Escaping streams and enumerators retain exactly one mutable scope owner. The
shared `ScopeEnumerator<T, TScope>` stores that scope in one field; a failed
inner-enumerator construction releases the local acquisition before propagating
the failure. Paged enumeration captures one mutable page scope and releases it
on completion or early disposal. A retained scope does not outlive its resource's
thread-affinity rules: monitor/read/write acquisitions must be disposed on the
acquiring thread and cannot span `await`.

The acquired lock identity is captured before returning. An open-hook failure
unwinds only that acquisition. Native lock release happens before the closed
hook; successful release is recorded before invoking the hook, so a failing or
reentrant closed hook cannot unlock twice. A failed thread-affine release remains
available for retry on the owner thread. Cleanup scopes similarly suppress
reentrant cleanup on the same instance and restore ownership after a cleanup
failure; retry does not undo side effects already performed by a custom callback.

Paging restores the previous cache pin state and leaves cache capacity,
dirty-page save and transaction behavior with the existing owners.
`IPagedList.EnterOpenPageScope()` returns `PageScope<TItem>` directly. Memory
paging returns its native cache-pin scope; dynamic `StreamPagedList` returns a
native close scope through its virtual method, preserving subclass dispatch.
Static pages need no cleanup. The ordinary cache's `long` key checks can box in
Debug; native pin-body checks and end-to-end cache checks have separate
measurement boundaries in the historical validation.

The Merkle paged-buffer decorator forwards `EnterOpenPageScope()` directly to
its underlying paged buffer, and cache value snapshots call `EnterReadScope()`.
These consumers retain the provider's scope lifetime without erasing the
concrete result.

Stream wrappers retain the pre-existing virtual-array transport, validation
ordering, bounded truncation, lock and ownership semantics. This scope change
does not reinstate the previously removed wrapper span-forwarding changes.
`EnterRestoreSeekPositionScope()` also retains its legacy zero-position exit;
`EnterRestorePositionScope()` captures the caller's current position. The
earlier [restore-position campaign](evidence/20261024-restore-position-struct/comparison.json)
measures that separate optimization; it is not another row in this campaign.

The richer `IScope` family, typed `SynchronizedObject<TReadScope, TWriteScope>`,
context scopes, serialization contexts, transaction scopes and asynchronous
cleanup retain their existing reference lifecycle. The generic synchronized
contract is separate from the nongeneric struct-return contract. Its end events,
extension hooks and ownership semantics are not replaced by leaf values.
Changing these interface and concrete return types requires updating provider
implementations and rebuilding binary consumers. Explicitly storing a struct
result as `IDisposable` still boxes it.

Simple cleanup APIs also keep their concrete result through ownership:
`AsyncSynchronizedGate.EnterScopeAsync()` returns
`Task<ValueActionScope<AsyncSynchronizedGate>>`; producer/consumer lock helpers
and `ILogger.LogDuration()` return concrete cleanup scopes. Await an acquisition
once and retain one owner of its result. `HashingStream` stores its borrowed
`HasherScope` directly. Notification deferral, animation, subscription, window
tracking and profiler directory lifetimes likewise use concrete mutable scopes.
The producer/consumer delay helpers retain their existing monitor thread-affinity
boundary; changing a return type does not move disposal to the acquiring worker.

Read [resource scopes](../guidelines/patterns/resource-scopes.md) and
[synchronization](../guidelines/patterns/synchronization.md) for API usage.

## Validation and reproducibility

After consolidation, the Windows Release solution build succeeded with zero
errors and ten existing PKCS NuGet audit warnings. This compiled the migrated
core, profiler, application, Blazor, WinForms and test projects. No tests or
benchmarks were executed for the consolidation.

The fixtures below cover scope ownership separately from timing. Their source
has since been migrated to the consolidated API; the historical callback fixture is retained in the linked baseline archive. The executed counts following
the table apply to the pre-consolidation versions, not to a new test run of the
current signatures:

| Fixture | Main coverage |
|---|---|
| [ValueSynchronizationScopeTests](../../tests/Sphere10.Framework.Tests/Threading/ValueSynchronizationScopeTests.cs) | Read/write nesting, hooks, failure/retry, wrong-thread release, captured parent identity, custom providers, concrete interface results and escaping collection enumerators. |
| [ValueAccessScopeTests](../../tests/Sphere10.Framework.Tests/Threading/ValueAccessScopeTests.cs) | Monitor lifetime, custom providers through storage layers, mapped properties, explicit boxing and opened clustered-stream ownership. |
| [ValueActionScopeTests (archived)](evidence/20261024-scope-abstraction/baseline-source.zip) | Historical default/null behavior, normal/exceptional cleanup, retry/reentry, copy/boxing boundaries, prepared callback allocation and file/directory cleanup. |
| [LeafValueScopeTests](../../tests/Sphere10.Framework.Tests/Threading/LeafValueScopeTests.cs) | Monitor helper, FastLock, nonreentrant lock and semaphore cleanup, including wrong-thread retry and warmed allocation. |
| [ValuePageScopeTests](../../tests/Sphere10.Framework.Tests/Collections/Paged/ValuePageScopeTests.cs) | Nested cache pins, early enumeration disposal, failed/reentrant cleanup, custom stream-list overrides and native/end-to-end allocation boundaries. |
| [HasherScopeTests](../../tests/Sphere10.Framework.Tests/Crypto/HasherScopeTests.cs) | One pool return, simultaneous borrowers, reset after unfinished/finalized input and known SHA-256 results. |
| [ActionDictionaryScopeTests](../../tests/Sphere10.Framework.Tests/Collections/Dictionaries/ActionDictionaryScopeTests.cs) | Prepared operation callbacks, enumeration lifetime/order, acquisition failure, cleanup retry/reentry and zero allocation for warmed ordinary operations. |
| [RecordedValueScopeResultsTests](../../tests/Sphere10.Framework.Utils.Profiler.Tests/Studies/RecordedValueScopeResultsTests.cs) | Capture completeness, matching source/runtime controls, parent/child Core identities, verified checksums, recomputed medians and both launches' complete BDN samples/diagnostics. No timing threshold. |

All validation below was executed before the final API consolidation. Before
the final six storage enumerator migrations, the
full Core Release suite passed 13,612 tests with 102 skipped in 4 min 46 sec.
After those migrations, focused Release validation passed 184 tests; Debug
passed 183 with one explicit Release-only cache allocation case skipped.
An additional affected Release run passed 260 tests in eight seconds after the
Merkle paged-buffer and cache enumeration value bridges were added.
The Windows solution Release build completed with zero errors and 20 existing
PKCS NuGet audit warnings. The final cross-platform Release build completed with
zero errors and ten existing PKCS NuGet audit warnings. The final profiler
Release suite passed all 1,241 ordinary cases in 52 seconds, including the 21
scope evidence cases. Both explicit benchmark runs passed all seven cases;
their successful captures are retained above.

The dedicated evidence is copied to `ScopeResults/20261024-value-scopes` in the
test output. Its scope-consumer manifest schema has its own reader; the older
CLI configuration studies continue to use their existing `Results` directory.

The [profiler test README](../../tests/Sphere10.Framework.Utils.Profiler.Tests/README.md#value-scope-consumer-measurements)
documents the explicit capture commands and controls. Reproducing these
historical contracts requires the retained measured harness and corresponding
Core revisions, not just the current migrated checkout. New captures of the
consolidated API need their own labels, source/binary identities and comparison;
they must not overwrite the historical interface rows. Ordinary test runtime is
not substituted for a performance measurement.

The subsequent [whole-abstraction comparison](scope-abstraction.md) measures the
consolidated scope families against this saved first struct implementation.
