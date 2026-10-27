# Scope abstraction migration, 24 October 2026

This is the historical comparison before the [direct-finalization simplification](scope-composition.md). Its recorded implementation and results remain preserved; use the successor report and current resource-scope guide for the final APIs.

**25 October 2026 implementation update:** `CallContext` again uses
`SynchronizedDictionary` and scoped atomic slot publication, following the
[designated synchronization pattern](../guidelines/patterns/synchronization.md#contract-and-lock-lifetime).
Search still copies key names under a short read scope and releases that scope
before yielding. The concurrent-registry implementation and timing results below
describe the archived experiment, not the current registry; those screening
measurements alone did not justify departing from the existing pattern. They have
not been rerun for this restoration.

The remaining canonical scope constructors and standard interface entry points removed their measured scope allocations. BenchmarkDotNet confirms zero-byte batches for direct scope, state-action, completed-task and synchronized-resource cases. Existing allocation-free consumers stay allocation-free, with mixed timing results: list and dictionary reads are about 3.6% slower in the benchmark capture, and synchronized resources show no clear timing gain.

The separately profiled `CallContext` registry now avoids its global reader/writer lock. Screening medians fell 59.79% for ambient reads and 64.58% for write/read round trips. Root-context time fell 2.92%; nested-context time increased 10.86%. Their allocation counts are unchanged because real context state remains shared. These context timings are screening results without separate BenchmarkDotNet confirmation.

Performance captures and final validation are complete. The full Core Release run passed 13,694 tests, and the full ordinary profiler run passed 1,253 cases. Exact counts, skips and retained logs are recorded below.

## Comparison boundary and source provenance

The baseline is the saved **first struct implementation**, before consolidating standard scope APIs and converting the remaining concrete scope classes. It is later than the original class baseline in the [historical value-scope report](value-scopes.md). It already includes earlier span, B-tree, cache and storage work and the first concrete monitor, reader/writer, page, hasher and cleanup value scopes. Their earlier gains must not be counted again.

The saved original source ZIP has SHA256 `9E9C052A28AF8EF02FF5EE19797FA9536FA82FC61051B800AB992E58D6E077A4`. The portable measured packages add identical expanded harness sources. The baseline's 1,086 Core C# files were verified byte-identical to that saved snapshot. This identifies an actual source archive, rather than a Git commit with later working changes mixed in.

| Variant | Archive entries | Compressed bytes | SHA256 |
|---|---:|---:|---|
| [Baseline source](evidence/20261024-scope-abstraction/baseline-source.zip) | 4,077 | 7,161,936 | `37EA69D5CFE522A60BFFA1826ED263C76B401F5120826D9DF2C8DA11B8471BC6` |
| [Optimized source](evidence/20261024-scope-abstraction/optimized-source.zip) | 4,103 | 7,238,722 | `FF753CEE764BA969E2362338B2890BC5B847F5A5C45478C1B1FB016502017D12` |

The [source provenance record](evidence/20261024-scope-abstraction/source-provenance.json) describes package construction. Both archives include C# sources/projects, build properties, the profiling resource and root license/readme files. Extract either into its own directory and build `tests/Sphere10.Framework.Utils.Profiler.Tests/Sphere10.Framework.Utils.Profiler.Tests.csproj`. These are profiler-build archives; unrelated native/UI assets needed for the complete Windows solution are excluded.

The final [source verification](evidence/20261024-scope-abstraction/source-verification.json) compared all 4,103 optimized archive entries with the final checkout and reported no mismatches. Core binaries in the library, Core test and profiler test outputs all have SHA256 `B6D530B72E8FF64067A7CEEE83D8527884A5B535AF28BF30F8FAEF3D43D71D46`, matching the optimized screening captures. The Debug build is a separate configuration.

The [preliminary comparison](evidence/20261024-scope-abstraction/preliminary-comparison.json) and [preliminary source](evidence/20261024-scope-abstraction/preliminary-source.zip) retain the initially regressed context implementation. Those results prompted removal of duplicate root storage and unnecessary facade dispatch, cached root classification, and profiling of the shared registry. They are excluded from final tables. An earlier three-file-harness preflight is excluded as well. Every final capture of a variant uses consistent source and Core identities.

## Implementation inventory

Concrete public scope handles are structs. Abstract reference bases retain the `StateBase` suffix where state actually needs shared identity.

| Area | Implementation and ownership |
|---|---|
| Shared synchronous lifecycle | `SyncScopeBase` stores active, disposing and completed phases inline. Defaults are inactive; failed cleanup restores retry ownership and inline reentry does not restart cleanup. |
| Shared asynchronous lifecycle | `AsyncScopeBase` keeps completed cleanup inline. Cleanup that suspends retains a shared completion owner; subsequent callers can join that completion. |
| Canonical handles | `Scope`, `Scope<T>`, `NoOpScope`, `ActionScope`, `ActionScope<T>`, `TaskScope` and `TaskScope<T>` compose common lifecycle structs. State callbacks receive their stored value directly, avoiding a fresh captured-state callback. |
| Locking | `AccessScope`, `ReadScope` and `WriteScope` retain the acquired resource. Typed `ReadScope<TScope>` / `WriteScope<TScope>` compose a selected inner scope with lock release; `SynchronizedResource` no longer subscribes to a scope-end event. |
| Standard entry points | `EnterAccessScope`, `EnterReadScope`, `EnterWriteScope` and `EnterOpenPageScope` return concrete structs through interfaces and concrete references. Parallel value methods and old `IDisposable` compatibility returns are removed. |
| Paging | `PageScope<TItem>` retains its cache pin or dynamic page directly. Nested purge-state restoration, dynamic closing, virtual dispatch, save-before-eviction and transaction behavior remain intact. |
| Hashing and escaping consumers | `HasherScope` is retained directly by borrowers and `HashingStream`. Collection/storage enumerators use `ScopeEnumerator`; escaping streams and other owners keep one acquisition in a concrete field or one closure. |
| Simple consumers | Logging duration, gates, producer/consumer helpers, file/directory cleanup and simple UI helpers retain concrete scopes and prepared state callbacks. Their underlying tasks, logging, collection and native work can still allocate. |
| Context and transactions | Action/Task context handles, delegate transaction handles, `FileTransactionScope`, `DACScope` and NHibernate `DataAccessScope` share reference state for roots, transaction ownership, rollback votes, backend connections and completion events. |
| Ambient registry | `CallContext` publishes one `AsyncLocal<object>` per name through `ConcurrentDictionary`. Existing-key reads/writes avoid the former global reader/writer lock; searches snapshot registered key names. |
| Serialization/application lifetimes | Serialization and ObjectSpace serialization contexts, plugin registration and screen/rendering handles retain shared state where reference tables, extension state, nesting and copy-safe completion require it. |
| Windows ownership | Simple native scopes use inline lifecycle state. WTS handles/memory keep a shared atomic owner to prevent copied handles freeing the same resource twice. Native rendering retains shared nesting/ownership state. |

A struct handle does not establish zero allocation for every row. Context, transaction, serialization and copy-safe native state still allocate where identity requires it. Suspended completion can allocate too. Captures measure complete operations rather than estimating savings from handle sizes.

The hash pool's return-node allocation, collection enumerators, serialization buffers, stream objects, indexes and transaction work remain separate costs. No pool redesign or storage-format feature is included in this comparison.

## Ownership and compatibility

`IScope` now contains synchronous and asynchronous disposal; `IScope<T>` also exposes its item. `ScopeEnd` has been removed. Transaction `Committing`, `Committed`, `RollingBack` and `RolledBack` events remain. Lock/resource release uses explicit composition and owner callbacks.

Return signatures and former class layouts changed: binary consumers must be rebuilt. Custom providers implement concrete struct return contracts. Consumers inheriting a former concrete scope instead compose a handle and, where shared identity is needed, derive state from an abstract state base. Generic scope decorators accept concrete struct types.

Keep one owner of inline resource scopes. Assigning one to `IScope`, `IDisposable` or `IAsyncDisposable` boxes a copy; copying a lock, page pin, hasher or unmanaged-memory owner does not transfer ownership. Context and transaction copies deliberately share state. Context handles retain the original `IContextScope RootScope` contract and call concrete state directly. Transaction typed roots wrap shared state; interface roots expose its identity. `FileTransactionScope.GetCurrent` and `DACScope.GetCurrent` return nullable whole handles when no context exists.

Synchronous cleanup stays on the caller's thread. Monitor, reader/writer and other thread-affine releases remain on the acquiring thread; an async disposal surface does not make acquisitions safe across thread switches. Changed scope and transaction adapters await cleanup rather than use `WaitSafe` or queue a `Task.Run` wait and return early. Unrelated task helpers are outside this change.

The common gate permits retry after failure and suppresses inline reentry. Shared identity does not make simultaneous first disposal thread-safe: serialize initiation and retries. Additional callers can join after the first `DisposeAsync` returns pending completion. A callback must not suspend and then await its own pending disposal: that creates a dependency on itself.

## Ambient context correctness

`CallContext` still uses `AsyncLocal`: child flows inherit parent data, while replacing it in a child does not replace the parent's binding. The concurrent registry publishes one shared slot per name. Its factory is a cached static callback; the value remains local to the execution flow. Names remain registered after clearing. Search retains the existing `StartsWith` comparison and snapshots keys at the first enumeration step, without holding a thread-affine lock across yields.

Root disposal restores its saved ambient value synchronously in the calling flow before cleanup starts. An assignment inside a suspended async method cannot restore its caller's binding after the await. Joining cleanup initiated in another flow, or disposing an already completed shared handle, also restores the joining caller's binding while cleanup ownership remains behind the shared gate.

A previous non-scope value is restored as well as null. Restoration only replaces the exact owned root, so retry after failure does not overwrite a newer root. Failed context start restores the previous binding. Cleanup remains retryable after restoration; callbacks observe the restored value. Nested disposal leaves its outer root active.

Directional adapters call internal cleanup once instead of invoking the entire disposal routine from an internal hook and duplicating root finalization. Synchronous disposal of an async context tracks and joins actual completion.

## Functional coverage and validation

Tests cover acquisition/release, defaults, repeated disposal, failure/retry, inline reentry, nesting, thread-affine release, custom providers, escaping enumerators and construction failure. Common asynchronous tests include completed, faulted, cancelled and suspended cleanup and callers joining an existing completion.

The context lifecycle fixture has 26 cases: raw ambient flow/replacement, policy rejection, identity, independent names, nested callbacks, sync/await-using disposal, previous-value restoration, pending copies, synchronous joining, fault/retry without overwriting a newer root, reentry, inherited child tasks, parallel independent roots, decorators and defaults. It also covers worker-started cleanup joined by its parent, completed shared handles and custom root providers.

Six registry cases cover concurrent first publication with child/grandchild isolation, deferred search snapshots, writes between yields, cleared names, case sensitivity and null key/prefix exceptions. Transaction coverage separately checks transaction owner versus context root, rollback votes, mixed Action/Task roots, commit/rollback/close ordering and backend ownership.

The final registry Windows solution build completed in 11.44 seconds with zero errors and ten existing PKCS package audit warnings. The Debug build completed in 14.31 seconds with zero warnings or errors. All final checks below passed against the final source; Debug uses its separate build configuration. A separate focused Release scope/context run also passed 392 tests in eight seconds.

| Final check | Passed | Skipped | Elapsed |
|---|---:|---:|---:|
| Core Release | 13,694 | 102 | 4 min 39 sec |
| Focused Debug scopes/context | 391 | 1 | 8 sec |
| Windows | 35 | 0 | 432 ms |
| WinForms | 142 | 0 | 12 sec |
| DAC | 20 | 0 | 459 ms |
| MVC | 4 | 0 | 140 ms |
| Profiler ordinary suite | 1,253 | 0 | 50 sec |

The Debug paging skip separates native scope lifecycle from an existing generic cache-key boxing path under Debug. Optimized Release and direct pin/restore have separate checks. The profiler's 63 explicit cases are excluded from its ordinary run; the measured captures above run their selected opt-in performance fixtures separately. The retained-evidence reader's 12 passing cases are included in the profiler suite. Functional durations are not benchmark results.

The [validation manifest](evidence/20261024-scope-abstraction/validation.json) retains exact outcomes, source/binary identities and log hashes. The [raw validation archive](evidence/20261024-scope-abstraction/validation-logs.zip) includes successful logs/TRX files and the earlier storage interruptions. Its SHA256 is `2BAF7E4587956374B93991482C1B989544D8B0421E234B5F495AECE8C28169B1`.

Earlier correctness and result-logging runs exhausted temporary disk space. The two affected profiler cases passed an isolated retry, followed by one clean full run of all 1,253 ordinary cases. That final run uses the console logger to avoid duplicating retained-evidence attachments; its complete console log is the final full-run result. The earlier incomplete TRX retains 25 unspecified outcomes and is identified separately in the manifest. Performance captures completed before these interruptions.

## Measurement method

Fresh serial Release hosts compile the same four sources against both variants:

- `ValueScopePerformanceTests.cs`: 36 normal workloads plus two optional GC cases.
- `ValueScopeConsumerBenchmarks.cs`: 12 selected consumer batches.
- `ValueScopeConsumerBenchmarkTests.cs`: out-of-process BenchmarkDotNet job.
- `ContextScopePerformanceTests.cs`: four synchronous context workloads.

Exact measured bytes are in [measured-harness.zip](evidence/20261024-scope-abstraction/harness/measured-harness.zip), with readable snapshots alongside. Its SHA256 is `66184EC21556FFC6E6FB42ADE1D2EE24E0D808F6514EC80FA4043B2A3D4ED266`. This preserves hashes independently of checkout line endings. Manifests retain Core/profiler/harness source hashes, loaded assembly SHA256/MVIDs, reflected return types, process/owner-thread identity, runtime/GC settings, selections and completion.

The environment is .NET 10.0.13, x64, Windows 10 build 19045, 20 logical processors, workstation GC and Interactive latency. Stopwatch frequency is 10,000,000 Hz. Both variants use the same **NTFS-compressed data volume** for file fixtures. Those timings include compression, transaction and warmed-cache behavior of small stores; they are not general disk-throughput or uncompressed-storage results.

Normal and context captures independently run baseline/optimized/optimized/baseline. Each host uses two warmup fixtures and five measured fixtures per workload; combining two hosts gives ten samples per variant. Each GC variant has five samples. All 820 batches completed semantic verification.

Setup, callbacks, warmup, checks, reopening, fixture disposal, hashing and output are excluded. Every measured sample owns fresh deterministic fixtures. Resource/tracker locks are warmed before acquisition batches. No forced collections are requested.

| Group | One counted action |
|---|---|
| Canonical scopes | Construct/dispose a concrete scope; item scopes read their item and callback scopes update prepared counters. |
| Critical/read/write | One acquisition/release through the selected concrete or interface entry. |
| Synchronized resources | Acquire the default typed scope, perform its permitted read/write, release/finalize; verify lifecycle counts and lock release. |
| Collection/cache/stream/ObjectSpace | Same boundaries as the original 28 consumers; mutation batches include flush or durable commit. |
| CallContextRead | Read one prepared ambient reference. |
| CallContextWriteRead | Alternate two prepared references and read each written value back. |
| ActionContextRoot | Enter/dispose one root; verify scope and context callback counts. |
| ActionContextNested | Enter/dispose a child under a prepared root; verify scope cleanup and zero child context finalization. |

Context comparison deliberately uses synchronous disposal in both variants. Baseline async callback defects are covered by correctness tests rather than an inequivalent comparison. Keys, markers, callbacks, registry warmup and the outer root are outside timing. Completed `TaskScope` screening actually awaits disposal; it does not model genuinely suspending cleanup.

Thread allocations cover complete synchronous work in a batch. Process allocation and GC counts can include host work. Elapsed time uses Stopwatch ticks; GC pause uses TimeSpan ticks. Tables report medians and interquartile intervals `[Q1,Q3]` using linear interpolation at `(sampleCount - 1) × percentile`. Positive reduction means less, negative time reduction means slower; zero-baseline percentages are zero and must be read with absolute counts.

BenchmarkDotNet selects the previous seven consumers plus `ScopeDirect`, `ActionScopeStateDirect`, `TaskScopeCompletedDirect`, `SynchronizedResourceRead` and `SynchronizedResourceWrite`. Each variant has two launches, three warmup and eight actual iterations per launch, one invocation/unroll factor, .NET 10 out-of-process execution and MemoryDiagnoser, without requested GC forcing or power-plan change.

One benchmark operation is a full batch. Fresh iteration fixtures and checks are outside timing. The 48 child manifests retain verified action counts and completion. All 384 raw Actual records remain available (16 per case per variant), even when BDN excludes outliers from statistics. `N` below is its retained statistical sample count. Bounds are BDN's 99.9% intervals. Parent and generated-child hashes can differ because build/PDB paths differ; consistency is checked within variants, not across build directories.

## Retained results

The [campaign directory](evidence/20261024-scope-abstraction) contains final raw captures, both benchmark variants, source packages, harness and summaries. The [reproduction guide](evidence/20261024-scope-abstraction/REPRODUCING.md) includes the executed capture/comparison/packaging scripts and separate build and capture commands:

- [Screening JSON](evidence/20261024-scope-abstraction/comparison.json), [consumer CSV](evidence/20261024-scope-abstraction/comparison.csv), [context CSV](evidence/20261024-scope-abstraction/context-comparison.csv), [GC CSV](evidence/20261024-scope-abstraction/gc-comparison.csv).
- [Benchmark JSON](evidence/20261024-scope-abstraction/bdn-comparison.json) and [CSV](evidence/20261024-scope-abstraction/bdn-comparison.csv), including per-action mean/median, allocation and confidence-bound normalization.
- [Baseline full benchmark report](evidence/20261024-scope-abstraction/bdn/baseline/results/Sphere10.Framework.Utils.Profiler.Tests.Scopes.ValueScopeConsumerBenchmarks-report-full.json) and [optimized full report](evidence/20261024-scope-abstraction/bdn/optimized/results/Sphere10.Framework.Utils.Profiler.Tests.Scopes.ValueScopeConsumerBenchmarks-report-full.json).

`RecordedScopeAbstractionResultsTests` verifies complete samples, operations, checksums, all four harness identities, variant source/Core fingerprints, benchmark jobs/completion/children and recomputed screening medians. It imposes no speed threshold. Ordinary tests copy required JSON, readable snapshots and the small harness ZIP, and attach only comparison JSON rather than duplicating every raw artifact.

| Capture | Verified measured batches |
|---|---:|
| [baseline-a](evidence/20261024-scope-abstraction/captures/baseline-a/scope-consumers.json) | 180 |
| [optimized-a](evidence/20261024-scope-abstraction/captures/optimized-a/scope-consumers.json) | 180 |
| [optimized-b](evidence/20261024-scope-abstraction/captures/optimized-b/scope-consumers.json) | 180 |
| [baseline-b](evidence/20261024-scope-abstraction/captures/baseline-b/scope-consumers.json) | 180 |
| [baseline-gc-a](evidence/20261024-scope-abstraction/captures/baseline-gc-a/scope-consumers.json) | 10 |
| [optimized-gc-a](evidence/20261024-scope-abstraction/captures/optimized-gc-a/scope-consumers.json) | 10 |
| [baseline-context-a](evidence/20261024-scope-abstraction/captures/baseline-context-a/context-scopes.json) | 20 |
| [optimized-context-a](evidence/20261024-scope-abstraction/captures/optimized-context-a/context-scopes.json) | 20 |
| [optimized-context-b](evidence/20261024-scope-abstraction/captures/optimized-context-b/context-scopes.json) | 20 |
| [baseline-context-b](evidence/20261024-scope-abstraction/captures/baseline-context-b/context-scopes.json) | 20 |

### Complete consumer screening

Interface batches remove prior boxes: 48 bytes per monitor action and 40 per read/write action. Canonical constructors remove object/callback storage. Concrete lock/cleanup, list/dictionary indexer and cache cases were already zero-byte paths in this baseline. Their earlier gains are not counted again.

<!-- BEGIN SCOPE ABSTRACTION SCREENING RESULTS -->

| Workload | Actions/batch | Before bytes | After bytes | Allocation reduction | Before ms [Q1, Q3] | After ms [Q1, Q3] | Time reduction |
|---|---:|---:|---:|---:|---:|---:|---:|
| CriticalConcrete | 16,384 | 0 | 0 | 0.00% | 0.646 [0.645, 0.648] | 0.811 [0.718, 0.878] | -25.44% |
| CriticalInterface | 16,384 | 786,432 | 0 | 100.00% | 0.947 [0.906, 1.216] | 0.798 [0.717, 0.878] | 15.65% |
| ReadConcrete | 16,384 | 0 | 0 | 0.00% | 1.010 [0.855, 1.353] | 0.961 [0.946, 0.979] | 4.82% |
| ReadInterface | 16,384 | 655,360 | 0 | 100.00% | 1.340 [1.230, 1.593] | 1.165 [1.077, 1.183] | 13.04% |
| WriteConcrete | 16,384 | 0 | 0 | 0.00% | 0.893 [0.887, 0.897] | 0.982 [0.964, 1.031] | -9.93% |
| WriteInterface | 16,384 | 655,360 | 0 | 100.00% | 1.194 [1.175, 1.212] | 1.099 [1.036, 1.137] | 7.95% |
| CleanupAction | 16,384 | 0 | 0 | 0.00% | 0.113 [0.111, 0.114] | 0.167 [0.161, 0.171] | -48.31% |
| CleanupState | 16,384 | 0 | 0 | 0.00% | 0.124 [0.123, 0.125] | 0.175 [0.172, 0.178] | -41.46% |
| BorrowHasher | 16,384 | 524,288 | 524,288 | 0.00% | 14.273 [14.005, 15.217] | 15.026 [14.989, 15.090] | -5.27% |
| ListRead | 2,048 | 0 | 0 | 0.00% | 0.155 [0.144, 0.160] | 0.178 [0.169, 0.209] | -15.18% |
| ListWrite | 2,048 | 0 | 0 | 0.00% | 0.150 [0.149, 0.151] | 0.184 [0.173, 0.187] | -23.12% |
| ListEnumeration | 2,048 | 196,608 | 196,608 | 0.00% | 1.577 [1.537, 1.606] | 1.541 [1.516, 1.565] | 2.28% |
| DictionaryRead | 2,048 | 0 | 0 | 0.00% | 0.171 [0.157, 0.177] | 0.205 [0.194, 0.226] | -19.99% |
| DictionaryWrite | 2,048 | 0 | 0 | 0.00% | 0.170 [0.168, 0.173] | 0.197 [0.190, 0.202] | -15.63% |
| DictionaryEnumeration | 2,048 | 212,992 | 212,992 | 0.00% | 1.666 [1.648, 1.678] | 1.670 [1.666, 1.692] | -0.24% |
| CacheHits | 2,048 | 0 | 0 | 0.00% | 0.365 [0.349, 0.387] | 0.404 [0.399, 0.657] | -10.53% |
| ClusterRead | 128 | 93,184 | 92,160 | 1.10% | 0.337 [0.332, 0.340] | 0.325 [0.323, 0.450] | 3.61% |
| ClusterWrite | 128 | 343,488 | 342,464 | 0.30% | 1.644 [1.599, 1.651] | 1.665 [1.654, 1.690] | -1.23% |
| ObjectStreamRead | 128 | 178,176 | 177,152 | 0.57% | 0.540 [0.537, 0.548] | 0.513 [0.511, 0.518] | 4.99% |
| ObjectStreamUpdate | 128 | 673,664 | 672,640 | 0.15% | 3.879 [3.816, 4.084] | 3.972 [3.935, 4.020] | -2.38% |
| MemoryIndexedRead | 32 | 98,856 | 98,856 | 0.00% | 1.621 [1.421, 2.931] | 1.570 [1.446, 1.754] | 3.14% |
| MemoryMerkleIndexedRead | 32 | 98,856 | 98,856 | 0.00% | 0.713 [0.568, 0.931] | 1.246 [1.116, 1.400] | -74.76% |
| FileIndexedRead | 32 | 182,664 | 182,664 | 0.00% | 1.048 [0.994, 1.205] | 0.912 [0.886, 0.953] | 13.03% |
| FileMerkleIndexedRead | 32 | 182,880 | 182,880 | 0.00% | 0.917 [0.899, 0.956] | 0.877 [0.858, 0.890] | 4.33% |
| MemoryMutationFlush | 32 | 24,756,972 | 24,757,376 | -0.00% | 92.316 [58.288, 122.476] | 56.930 [55.739, 60.673] | 38.33% |
| MemoryMerkleMutationFlush | 32 | 24,952,348 | 24,954,992 | -0.01% | 62.541 [55.184, 111.706] | 55.957 [55.850, 59.135] | 10.53% |
| FileMutationCommit | 32 | 42,108,212 | 42,108,808 | -0.00% | 230.759 [176.492, 243.404] | 176.027 [169.946, 207.041] | 23.72% |
| FileMerkleMutationCommit | 32 | 42,365,832 | 42,371,384 | -0.01% | 239.575 [234.875, 242.164] | 252.013 [245.936, 265.741] | -5.19% |
| ScopeDirect | 16,384 | 524,288 | 0 | 100.00% | 0.189 [0.178, 0.195] | 0.069 [0.067, 0.069] | 63.66% |
| ScopeItemDirect | 16,384 | 655,360 | 0 | 100.00% | 0.182 [0.175, 0.192] | 0.070 [0.070, 0.070] | 61.37% |
| ActionScopeDirect | 16,384 | 655,360 | 0 | 100.00% | 0.225 [0.222, 0.232] | 0.193 [0.191, 0.266] | 14.19% |
| ActionScopeStateDirect | 16,384 | 2,359,296 | 0 | 100.00% | 1.170 [1.048, 1.372] | 0.477 [0.459, 0.501] | 59.26% |
| TaskScopeCompletedDirect | 16,384 | 9,725,684 | 0 | 100.00% | 58.606 [48.265, 65.228] | 0.597 [0.539, 0.639] | 98.98% |
| NoOpScopeDirect | 16,384 | 524,288 | 0 | 100.00% | 0.144 [0.142, 0.147] | 0.056 [0.055, 0.056] | 61.46% |
| SynchronizedResourceRead | 16,384 | 1,572,864 | 0 | 100.00% | 6.157 [6.128, 6.621] | 6.867 [6.839, 6.927] | -11.53% |
| SynchronizedResourceWrite | 16,384 | 1,572,864 | 0 | 100.00% | 6.210 [6.175, 6.266] | 6.495 [6.391, 6.555] | -4.59% |

<!-- END SCOPE ABSTRACTION SCREENING RESULTS -->

The hasher batch still allocates 32 bytes per borrow/hash/return action for its pool return node. Enumeration retains enumerator allocations. Storage changes are generally small against remaining serializer/index/stream/transaction work; the table includes increases as well as reductions. These medians do not establish an overall application speedup.

### GC stress

Both baseline cases were already allocation-free. The new abstraction shows no additional allocation or measured GC-pause saving in these batches.

<!-- BEGIN SCOPE ABSTRACTION GC RESULTS -->

| Workload | Actions/batch | Before bytes | After bytes | Allocation reduction | Before ms [Q1, Q3] | After ms [Q1, Q3] | Time reduction |
|---|---:|---:|---:|---:|---:|---:|---:|
| CriticalGcStress | 2,000,000 | 0 | 0 | 0.00% | 76.201 [47.559, 80.872] | 66.643 [41.707, 82.355] | 12.54% |
| ReadGcStress | 2,000,000 | 0 | 0 | 0.00% | 52.641 [52.413, 52.825] | 60.977 [56.060, 64.490] | -15.84% |

<!-- END SCOPE ABSTRACTION GC RESULTS -->

Both variants recorded median Gen0 counts and total GC pause deltas of zero in each case. Slower timing medians remain visible; no new GC improvement is claimed.

### Context screening

Registry reads and write/read round trips are faster in this capture, with unchanged allocation. Root batches retain 360 bytes per action, including ambient updates; nested contexts retain 72 bytes per action for shared state. Root time changes little and nested time remains 10.86% slower. Lifecycle, restoration and identity guarantees have costs; these rows are not described as allocation-free or a uniform context speed improvement.

<!-- BEGIN SCOPE ABSTRACTION CONTEXT RESULTS -->

| Workload | Actions/batch | Before bytes | After bytes | Allocation reduction | Before ms [Q1, Q3] | After ms [Q1, Q3] | Time reduction |
|---|---:|---:|---:|---:|---:|---:|---:|
| CallContextRead | 16,384 | 0 | 0 | 0.00% | 1.559 [1.552, 1.569] | 0.627 [0.624, 0.641] | 59.79% |
| CallContextWriteRead | 16,384 | 2,752,344 | 2,752,344 | 0.00% | 6.182 [5.113, 6.294] | 2.189 [2.158, 2.272] | 64.58% |
| ActionContextRoot | 16,384 | 5,898,240 | 5,898,240 | 0.00% | 9.034 [8.793, 9.144] | 8.770 [8.451, 8.871] | 2.92% |
| ActionContextNested | 16,384 | 1,179,648 | 1,179,648 | 0.00% | 2.038 [2.030, 2.040] | 2.260 [2.249, 2.324] | -10.86% |

<!-- END SCOPE ABSTRACTION CONTEXT RESULTS -->

### BenchmarkDotNet results

Times and bytes below are per full batch. Per-action JSON/CSV figures are arithmetic normalization, not separate acquisition benchmarks. Confidence intervals and retained sample counts accompany the point estimates.

<!-- BEGIN SCOPE ABSTRACTION BENCHMARK RESULTS -->

| Workload | Actions/batch | Before mean ms [99.9% CI] | After mean ms [99.9% CI] | Before median ms | After median ms | Before/after bytes | N before/after | Mean time reduction |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| ListRead | 2,048 | 0.0524 [0.0520, 0.0529] | 0.0543 [0.0542, 0.0544] | 0.0526 | 0.0543 | 0 / 0 | 15 / 14 | -3.60% |
| DictionaryRead | 2,048 | 0.0564 [0.0557, 0.0571] | 0.0584 [0.0582, 0.0586] | 0.0564 | 0.0584 | 0 / 0 | 14 / 13 | -3.57% |
| CacheHits | 2,048 | 0.1458 [0.1444, 0.1473] | 0.1480 [0.1460, 0.1499] | 0.1454 | 0.1472 | 0 / 0 | 15 / 15 | -1.47% |
| ClusterRead | 128 | 0.0736 [0.0727, 0.0746] | 0.0743 [0.0733, 0.0754] | 0.0737 | 0.0746 | 87,040 / 86,016 | 15 / 14 | -0.97% |
| ObjectStreamRead | 128 | 0.0996 [0.0981, 0.1011] | 0.1043 [0.0997, 0.1089] | 0.0994 | 0.1026 | 159,744 / 158,720 | 15 / 14 | -4.71% |
| MemoryMerkleIndexedRead | 32 | 0.2433 [0.2397, 0.2469] | 0.2451 [0.2413, 0.2489] | 0.2454 | 0.2457 | 98,856 / 98,856 | 13 / 15 | -0.74% |
| FileMerkleMutationCommit | 32 | 86.1536 [74.0829, 98.2244] | 82.2019 [76.6782, 87.7255] | 81.5418 | 80.1073 | 42,357,320 / 42,356,808 | 16 / 16 | 4.59% |
| ScopeDirect | 16,384 | 0.1418 [0.1190, 0.1646] | 0.0093 [0.0091, 0.0095] | 0.1435 | 0.0092 | 524,288 / 0 | 16 / 15 | 93.45% |
| ActionScopeStateDirect | 16,384 | 0.4488 [0.3754, 0.5222] | 0.0800 [0.0794, 0.0806] | 0.4541 | 0.0798 | 2,359,296 / 0 | 14 / 15 | 82.17% |
| TaskScopeCompletedDirect | 16,384 | 18.7949 [18.4038, 19.1861] | 0.1902 [0.1891, 0.1913] | 18.7826 | 0.1897 | 7,855,672 / 0 | 16 / 15 | 98.99% |
| SynchronizedResourceRead | 16,384 | 5.0496 [4.9853, 5.1138] | 5.0295 [5.0153, 5.0437] | 5.0348 | 5.0268 | 1,572,864 / 0 | 14 / 13 | 0.40% |
| SynchronizedResourceWrite | 16,384 | 4.5957 [4.5715, 4.6199] | 4.6035 [4.5814, 4.6255] | 4.5944 | 4.5954 | 1,572,864 / 0 | 14 / 14 | -0.17% |

<!-- END SCOPE ABSTRACTION BENCHMARK RESULTS -->

Direct Scope, stored-state ActionScope and completed TaskScope mean batches are 93.45%, 82.17% and 98.99% shorter respectively, with zero measured allocations. The completed-task result includes removal of old asynchronous scope notification scheduling; it is not a forecast for callbacks doing substantial or genuinely suspending work.

Synchronized-resource allocation falls from 96 bytes per action to zero, while timing confidence intervals overlap and means change by under 0.4%. List/dictionary reads remain allocation-free but have about 3.6% higher means. Cache, stream and indexed-read timing changes are small or have overlapping intervals. File mutation has broad overlapping intervals and runs on compressed NTFS; its 4.59% lower mean is not established general storage-throughput gain.
