# Span inheritance review, 23 October 2026

Historical evidence availability: profiler capture files are no longer shipped
with the repository. Measurements below describe the original campaign, not a
new verification. Inspecting its raw data or archive-side analysis scripts
requires an external copy. Example paths use `E:/ProfilerEvidence`; substitute
your own archive root and pass it as the `ProfilerResultsRoot` NUnit parameter
when explicitly selecting recorded-capture checks.

Current-source note: the [24 October behavior review](comment-and-behavior-review.md) restores the original three wrapper implementations and unrelated search/paging behavior. The captured measurements below describe this earlier revision.

Source revision note: this report describes the retained inheritance-cleanup revision and its measurements. The later [complexity review](optimization-complexity-review.md) removes transport-mode helpers and makes stream array operations delegate to virtual span overloads. The independent array/span/byte extension points described below belong to this earlier revision.

The span optimization campaign used exact runtime-type checks to preserve existing subclass behavior while sending the concrete base type through direct storage operations. Those checks were too broad: a subclass that changed nothing took an allocating compatibility path, and a stream-mapped buffer could even stop filling short destination reads merely because it was subclassed.

All of those checks have been removed. There is no replacement reflection, override discovery, type cache or eligibility flag. Existing type names in exception messages and ordinary source-array/List specialization are unrelated to subclass dispatch.

## Decisions

| Area | Decision and reason |
|---|---|
| `ExtendedList<T>` scalar methods, enumeration and `CopyTo` | Revert the direct-array shortcuts. The established range-first base remains responsible for these operations, including virtual range overrides. The allocation reduction for these reverted scalar paths is deliberately given up. |
| `ExtendedList<T>` used as a bulk source | Revert its private backing-span shortcut. Materialization preserves the source's enumeration/copying contract and snapshots self-insertion. Array/List bulk sources and explicit span copies remain optimized for base classes and unchanged subclasses. |
| `BufferAdapter.ReadRange` | Use one ordinary `IBuffer.ReadSpan` projection/copy path. Both old branches already materialized a snapshot; the type distinction did not provide an allocation-free path. Empty reads follow the existing `ReadSpan` contract. |
| `BufferAdapter.AddRange` | Restore the shared snapshot and virtual scalar-add path for every instance. A callback can mutate unread source bytes, so replacing this with a direct loop would change aliased-input behavior. |
| `ExtendedMemoryStream.ToArray` | Revert the built-in buffer whitelist. Materialization follows the buffer's normal `ToArray`/copy/enumeration contract. No allocation improvement is claimed for arbitrary backing buffers. |
| `MerklePagedBuffer.ReadRange` | Revert the forwarding shortcut and use the inherited decorator's virtual `ReadSpan` contract. |
| Paged and stream-mapped destination reads | Keep direct destination copying for every subclass. The destination overload is virtual, allowing projections to customize that operation explicitly. Stream-mapped reads fill the requested destination or throw at EOF for base and inherited instances alike. |
| Stream span and single-byte I/O | Keep the shared span cores for every subclass. Bounds, locking, array argument validation and original inner-array transports remain in their existing paths. |
| Fragment-provider length | Use normal interface dispatch through `GetFragment`; the built-in provider returns a nonallocating span, and interface projections can change fragment length. |
| Ordinary B-tree key searches | Keep key-only comparisons for every subclass. `GetKeyValue` is the virtual comparison-key hook; `GetKey` supplies full returned entries. Unchanged subclasses no longer deserialize unused values during probes. |

## Subclass extension points

Array, span and single-byte stream methods are separate virtual entry points. An existing subclass that overrides only an array overload still intercepts explicit array calls; it no longer automatically intercepts span or single-byte calls. Such a subclass must override the operations whose behavior it customizes. This is an intentional change from the earlier conservative dispatch implementation, not a claim of transparent compatibility with array-only external subclasses.

Likewise, a paged or stream-mapped buffer that projects snapshot bytes through `IBuffer.ReadSpan` must also override `ReadRange(long, Span<byte>)` to project destination reads. Decorators retain their shared virtual projection path. A streamed B-tree that projects comparison keys must override `GetKeyValue`; overriding `GetKey` still controls materialized entries and returned values.

Tests cover unchanged subclasses, explicit array/span overrides, length-changing fragment projections, destination projections, overlap, aliased-source callbacks, short/truncated stream reads, and key-only comparisons. Removing the Merkle forwarding shortcut exposed an empty paged-snapshot inconsistency; snapshot and destination reads now share range validation, including empty storage.

## Allocation checks and rollback cost

The reverted scalar shortcut has a real cost on the range-first `ExtendedList<Cluster>` backend. The retained earlier Core TRX reports zero bytes for 16,000 cluster-walk steps; the cleaned implementation allocates **512,000 bytes** for the same link reads. Both integrity modes allocate exactly the measured link-read baseline. An item-first `ExtendedListAdapter<Cluster>` control still allocates zero bytes for the walk, and the direct passing-Guard probe still allocates zero bytes. The Guard tests now isolate those costs instead of requiring an unrelated backend's scalar optimization.

The earlier small in-memory cluster destination-read probe likewise rises from zero to **32,000 bytes over 1,000 reads**, due to scalar cluster lookups. This cost is retained and reported. The payload-copy test preserves its slice/tail checks and separately compares equal 4,089-byte reads using caller storage and snapshots:

| Backend, 1,000 reads | Destination allocation | Snapshot allocation | Bytes saved by caller storage |
|---|---:|---:|---:|
| In-memory range-first clusters | 32,000 | 4,152,000 | 4,120,000 |
| Stream-mapped, cached | 0 | 4,120,000 | 4,120,000 |
| Stream-mapped, uncached | 0 | 4,120,000 | 4,120,000 |

The unchanged-subclass checks also show that the retained optimizations are inherited: 64 batches of 1,024-item contiguous list operations allocate zero bytes for both base and plain subclass; 8,192 single-byte stream operations allocate zero bytes for both; 32 reads into an existing 64-KiB destination allocate 21,760 bytes of paging metadata for both, with no per-read payload snapshot.

The first complete Core run passes 13,078 cases and fails three allocation assertions coupled to the reverted scalar shortcut; its original log/TRX are preserved as `core-release-full-first-failure`. After the two test methods were scoped as above, all 12 focused Guard/cluster allocation cases pass. The payload test also caps destination allocation below half the requested payload volume, so a payload-sized allocation in a shared helper cannot be hidden by equal increases in both paths. No assertion was replaced with a timing threshold or a larger unexplained allocation allowance.

The subsequent complete Release Core run passes **13,081 tests**, with **102 skipped** and zero failures. The complete Release CryptoEx suite passes **7,920 tests**. Focused Debug checks pass **376 tests**, including the final absolute payload-allocation bound. That bound also passes its three Release cases after being strengthened. The full Release profiler suite passes **1,150 tests**, including all **28 recorded ObjectSpace cases**, with 37 explicit cases recorded as `NotExecuted` in the TRX despite the runner's printed skipped counter being zero. The cross-platform Release solution build passes with zero errors and 20 NuGet audit warnings in unchanged dependencies.

The focused behavior run passes **351 tests**; these and the allocation-focused runs overlap the complete suites and are not additional independent coverage totals. All final production source was stable before the full suites and both serialized benchmark phases. The added absolute payload bound was checked separately in Release and in the subsequent Debug run. `git diff --check` passes.

## Evidence scope

The [preceding page reuse report](object-space-page-btree.md) describes its frozen source revision. Its timing and source-identity claims must not be silently attributed to this cleanup. The new dispatch comparison uses that frozen combined implementation as the before build and the cleaned implementation as the after build. Both already contain page pooling; this comparison does not isolate or remeasure pooling itself.

Original execution logs, TRX files, source manifests and generated benchmark workers are retained in `E:\Sphere10-Span-Dispatch-Cleanup-20261022`. The optional external recorded-capture directory is `E:/ProfilerEvidence/20261022-span-dispatch`. The explicit recorded fixture skips an absent campaign and rejects partial captures; it never launches measurements or asserts a machine-speed threshold.

To run the affected behavior and allocation checks from the Framework root:

```powershell
dotnet test tests/Sphere10.Framework.Tests -c Release -p:BuildRevision=0 --filter 'FullyQualifiedName~ExtendedListSpanTests|FullyQualifiedName~BufferSpanTests|FullyQualifiedName~StreamSpanTests|FullyQualifiedName~StreamWrapperSpanTests|FullyQualifiedName~ExtendedMemoryStreamTests|FullyQualifiedName~ByteArraySpanConsolidationTests|FullyQualifiedName~FragmentReadSpanTests|FullyQualifiedName~BTreeTypeTests|FullyQualifiedName~GuardInterpolationTests|FullyQualifiedName~ClusterMapTests.ClusterDataReadsFillCallerStorageWithoutPayloadAllocations'
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0 --filter 'FullyQualifiedName~RecordedObjectSpaceHotPathTests' --logger 'console;verbosity=detailed' -- 'TestRunParameters.Parameter(name="ProfilerResultsRoot",value="E:/ProfilerEvidence")'
```

## Before/after ObjectSpace check

The two matched File/Merkle workloads retain **80 actual samples**: two worker launches, five warmups and ten actual iterations per workload/revision. Throughput targets 250 ms with adaptive invocation counts; all outliers remain, no forced GC is requested, and every worker exits successfully. Preparation, integrity verification and reopening remain outside the measured batch.

The workload uses 256 records, 200 operations per batch, 128-byte payloads, 256 categories, shuffled insertion/lookup, Full indexes, SHA2_256, order 64, historical mixed tree selection, 4,096-byte clusters, 256-KiB pages, 16-MiB cache, Default policy, LittleEndian and explicit saving. Clean flush measures 200 flush calls, not 200 durable commits.

Times are milliseconds per batch with BenchmarkDotNet's reported 99.9% confidence margin. Managed allocation is bytes per batch.

| Workload | Before ms ± margin | After ms ± margin | Allocated bytes before → after |
|---|---:|---:|---:|
| CleanFlush | 25.748 ± 0.533 | 25.465 ± 0.525 | 564,822 → 564,822 |
| WarmIdentityLookup | 5.245 ± 0.066 | 5.478 ± 0.267 | 2,756,218 → 2,756,218 |

Both reported timing intervals overlap. These captures establish neither a timing improvement nor equivalence; they show unchanged managed allocation in these two ObjectSpace workloads. The narrower allocation probes above demonstrate inherited fast paths and separately expose the scalar rollback cost. Before/after launch means are retained: clean flush 25.364/26.132 ms before and 25.846/25.083 ms after; warm lookup 5.256/5.235 ms before and 5.739/5.218 ms after.

The source inventory compares 1,073 Core, 52 CryptoEx and 35 profiler source/project files: **1,160 per revision**, with **14 changed Core files** and no CryptoEx/profiler changes. The three root build/package inputs are byte-identical. This inventory excludes generated outputs, tests and documentation; it does not substitute for compiled-worker identities. All six actual retained Core/Profiler/generated-worker DLLs have separate SHA256, versions and MVID records, with assembly/file versions 3.1.3.0.

All **43 retained mapped payloads** pass byte-count/SHA256 verification. The portable analyzer replays both pairs and all 80 samples from the original retained copies, reconciling run settings, commands, worker exits, CSV/full-report statistics and normalized per-operation measurements. The mapped copies preserve original bytes; `.cs.txt` source suffixes keep evidence outside the C# compile glob. Launcher identities are not substituted for measured worker identities.

```powershell
python E:/ProfilerEvidence/20261022-span-dispatch/analysis/analyze_dispatch_cleanup.py --campaign E:/ProfilerEvidence/20261022-span-dispatch --output E:/RecomputedDispatchEvidence
```
