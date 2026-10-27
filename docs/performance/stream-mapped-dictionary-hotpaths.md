<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

Historical evidence availability: profiler capture files are no longer shipped
with the repository. Measurements below describe the original campaign, not a
new verification. Inspecting its raw data or archive-side analysis scripts
requires an external copy. Example paths use `E:/ProfilerEvidence`; substitute
your own archive root and pass it as the `ProfilerResultsRoot` NUnit parameter
when explicitly selecting recorded-capture checks.

# Stream-mapped dictionary hot-path profiling

Current-source note: the [24 October behavior review](comment-and-behavior-review.md) keeps measured seek/event/context changes and restores the three wrapper implementations, legacy paging behavior and unsupported transformations. Timing figures below identify their frozen binaries and are not new measurements of the reduced source tree.

This report records the October 22, 2026 investigation of the long-running stream-mapped dictionary integration tests. The baseline binary already contains the earlier [span and buffer optimizations](span-optimizations.md). The first profiling pass addressed additional allocations in the actual dictionary workload. All six unchanged heavy cases passed, with their summed test durations reduced from 1,229.573 to 761.980 seconds, approximately 38.0%. The second pass reduced its separate corrected-baseline matrix from 750.928 to 688.354 seconds, approximately 8.33%, with all six cases passing in both runs. The timing comparisons, sampled profiles and focused allocation regressions are separate measurements. Final CrossPlatform and Windows Release builds, targeted `OMIT_GUARD` checks and both ordinary full Core and Profiler suites passed.

The measurement host is Windows x64 with an Intel Core i9-10900K, ten physical cores and twenty logical processors, .NET SDK 10.0.402 and runtime 10.0.13. The saved first-pass baseline core assembly has SHA-256 `16D3C9D73A11E15CC2AD99A620A50674FC7B7D48CD58C7474E0EC5C662D1ACA1`; the first-pass optimized core assembly has SHA-256 `5ECE7BE70BB17B122B0582E5D9C3B6FBA556C1EB872FB18D2C9F1B49B0FD914F`.

## Workload and comparison boundary

The canonical core workload is `IntegrationTests_Heavy` in [StreamMappedDictionaryTestsBase](../../tests/Sphere10.Framework.Tests/Collections/StreamMapped/StreamMappedDictionaryTestsBase.cs), inherited by both `StreamMappedDictionaryTests` and `StreamMappedDictionaryCLKTests`. This core fixture's heavy cases are compiled only when `DEBUG` is defined. The dedicated profiler fixture described below is available in both Debug and Release, but all heavy timings and profiles reported here use Debug binaries. The build configuration and the `ClusteredStreamsPolicy.Debug` parameter are separate: each six-case matrix uses one Debug build, with `Debug`, `Default` or `Performance` storage policy.

Each case retains `MemoryStream` storage, a 32-byte cluster size, `maxItems = 250` and all 250 iterations of [AssertEx.DictionaryIntegrationTest](../../src/Sphere10.Framework.NUnit/AssertEx.cs). Keys are random strings with a unique prefix; values use the existing `TestObject` generator and serializer. Each iteration adds, updates and removes records, then checks the complete pair, key and value views against the ordinary dictionary oracle. Reaped physical slots remain part of the storage traversal. A capacity of 250 therefore represents many thousands of mutations and repeated complete reads, rather than 250 dictionary operations.

The helper seeds its operation/input `Random` with 31337. Its `Randomize()` calls use a separate thread-local framework RNG seeded from the environment, so shuffled deletion order is not bit-for-bit reproducible between hosts. The helper and its RNG behavior are unchanged. The timing rows are individual observed runs, with this workload variation and ordinary host noise; they are not statistical confidence intervals.

The comparison measures these Debug integration tests. Release builds can show different gains because JIT optimization, inlining and enum handling differ; these results are not a general throughput claim for every dictionary workload.

Baseline and optimized timing runs execute the six cases serially against separate immutable compiled outputs. The test helper, random-number consumption, iteration count, assertions, serializers, cluster size and policy values remain unchanged. In particular, the repeated oracle materialization inside the update loop is retained. No integrity policy is disabled and no test assertion or dictionary operation is removed to shorten the workload.

## Profile evidence

Profiles use managed sampled thread-time stacks filtered to the dictionary workload. Allocation evidence comes from weighted GC allocation ticks. These are sampled estimates, distinct from the exact managed-allocation counters used by the focused regressions below. Inclusive shares overlap when a caller and its callee both appear, so the rows must not be added together or interpreted as independent portions of total allocation. Managed thread-time samples also do not directly measure end-to-end elapsed time.

The baseline allocation profiles identified these paths:

| Inclusive allocation path | KVP dictionary | CLK dictionary |
|---|---:|---:|
| `ClusterSeeker.SeekTo` | 37.13% | 49.11% |
| Paged item-byte reads | 19.82% | 17.75% |
| Fragment reads | 19.68% | 17.73% |
| `ClusterMapChangedEventArgs` construction | 8.80% | 12.19% |

The page and fragment frames describe nested parts of the same read pipeline. The profile motivated changes along that pipeline rather than treating each row as a separate allocation source. The event constructor was also significant because a small primitive write could eagerly create several empty sets and the dictionaries behind a bijection.

### Second-pass baseline

The second pass starts from a corrected immutable baseline that includes the private static-page no-op scope described below. Its core assembly SHA-256 is `545DF1BBA6E7CA235F3B1BEF054F19043A9B7ACEB424A92BCD2E159A77211DE9`. This is a separate comparison boundary from the first-pass hashes and timing table.

The frozen final Debug host records core assembly SHA-256 `FEB5AB62EFDB90F480AD25DE1C75F0EAC3411116D8E409DFAA37C199C0BCECBE` and profiler test assembly SHA-256 `83BD96EED5C8A4A5193E2FC89CAFD9C2AA552341EB5E055B5994A56CAFFA267A`. Its local provenance manifest is `profiler-pass2-final/profiling-host.json`, including captured source-file hashes, configuration, runtime, SDK and serial runner settings. These identify the frozen measurement snapshot rather than asserting that every current working-tree file has the same bytes.

The fresh workload-filtered managed profile identified the following remaining costs. Allocation shares are weighted allocation-tick estimates; exclusive sample shares describe managed sampled thread time. They are different measures and must not be combined.

| Path | Second-pass baseline evidence | Change motivated |
|---|---|---|
| Runtime identity-hash worker reached from scalar `SerializationContext` reference maps | 13.13% of exclusive thread-time samples | Keep the first reference and its status inline; allocate maps only when needed |
| Successful interpolated `Guard` diagnostics under `ClusterSeeker.SeekForward` | 52.15% of weighted allocation samples | Format internal interpolated diagnostics only when the predicate fails |
| `EnterRestorePositionSeek` | 12.34% of weighted allocation samples; 0.72% exclusive and 3.90% inclusive thread-time samples | Replace the captured closure, delegate and action disposable with one dedicated scope |
| BCL span-to-array stream bridges | Approximately 1.07% of exclusive thread-time samples | Forward spans directly through the three concrete stream wrappers |

These observations justify the four second-pass changes below. The sampled baseline profile alone does not establish an elapsed-time improvement; the separate timing comparison appears below.

### Completed second-pass profile comparison

The separate thirty-second captures are `pass2-before.nettrace` and `pass2-after.nettrace`, with parsed evidence in `pass2-before-stacks.json` and `pass2-after-stacks.json`. The profiler's managed thread samples are filtered to the full dictionary workload; they are not kernel CPU measurements. Weighted GC allocation ticks identify sampled allocation types and stacks, and each tick's byte weight also covers intervening allocations. The reported type totals are therefore sampled attribution, not an exact accounting of every allocation of that type.

| Capture summary | Corrected baseline | Final |
|---|---:|---:|
| Workload managed thread samples | 14,767 | 14,876 |
| Reported GC allocation ticks | 69,163 | 29,296 |
| Weighted bytes represented by allocation ticks | 7,393,395,328 B | 3,148,704,200 B |
| Allocation ticks missing a stack | 0 | 0 |

These fixed-duration windows execute different amounts and phases of the integration workload. Their weighted byte totals cannot establish an equal-work allocation reduction or bytes per dictionary operation. The equal-work scope and context probes below provide that separate evidence. The profiled case's elapsed duration is excluded from both timing matrices; functional checks begun after the capture window are also outside the profile measurement.

The separately profiled final CLK `Default` case completed all 250 iterations and passed in `second-pass-profiled-after.trx`. Its elapsed duration is excluded because it includes instrumentation, and subsequent functional validation overlapped the remainder of that test only after the capture window ended.

| Observed path | Corrected baseline | Final |
|---|---:|---:|
| Runtime identity-hash worker, exclusive sample share | 13.13063% | 0.02689% (four samples) |
| BCL `Stream.Read(Span<byte>)` and `Stream.Write(ReadOnlySpan<byte>)` bridges, combined exclusive sample share | 1.06995% | 0.01344% (two read samples; no write samples) |
| `PollGCWorker`, exclusive sample share | 7.44227% | 2.39984% |
| `ClusterSeeker.SeekForward`, inclusive weighted allocation | 3,855,837,728 B (52.15%) | No matching allocation stack |
| Restore-position scope creation, inclusive weighted allocation | 912,575,472 B (12.34%) | 351,214,704 B (11.15%) |

The final capture no longer attributes an allocation tick to a `SeekForward` stack. This absence is a sampled observation; the successful-guard regression independently measures zero allocation for its prepared equal-work checks. Restore-position scopes still allocate one owned object per operation: the equal-work comparison remains 128,000 to 40,000 bytes for 1,000 scopes even though their share of the capture changes less.

The largest remaining final exclusive frames include culture-aware `CompareInfo.Compare` at 12.18069%, `Monitor.Enter_Slowpath` at 6.45335%, `ClusterSeeker.SeekForward` at 4.32240% and `ClusterSeeker.SeekTo` at 3.02501%. A changed percentage is not evidence that the method itself became slower: the denominator, work completed and sampled workload phase also change.

| Leading final allocation type | Weighted attribution | Share |
|---|---:|---:|
| `ClusterMapChangedEventArgs` | 1,418,178,504 B | 45.04% |
| `byte[]` | 494,579,576 B | 15.71% |
| Restore-position scope | 351,214,704 B | 11.15% |
| `KeyValuePair<long, long>[]` | 210,757,120 B | 6.69% |
| Compiler-generated `<>c__DisplayClass118_1` | 200,390,456 B | 6.36% |
| `SerializationContext` | 85,122,384 B | 2.70% |
| `Action` | 55,861,600 B | 1.77% |
| `string` | 41,921,104 B | 1.33% |

Remaining comparisons must retain the configured culture and ordering semantics, and required monitor scopes must retain their synchronization contract. Event arguments remain independently owned because subscribers can retain them and mutate their public collections; their larger remaining share does not justify pooling or removing events without an ownership-compatible design. No comparer, locking or event policy was removed to obtain these measurements.

### Reproducing the workload

Build the core test project in Debug with `-p:BuildRevision=0`, preserving separate artifact directories for baseline and optimized binaries. Run the saved test DLL through `dotnet vstest` with this filter:

```text
FullyQualifiedName~Sphere10.Framework.Tests.StreamMappedDictionaryTests.IntegrationTests_Heavy|FullyQualifiedName~Sphere10.Framework.Tests.StreamMappedDictionaryCLKTests.IntegrationTests_Heavy
```

Use a runsettings file containing the following configuration and pass it with `--Settings:<path>`; collect per-case durations using the TRX logger. Avoid concurrent test workloads and keep profiling runs separate from elapsed-time comparisons.

```xml
<RunSettings>
  <RunConfiguration><MaxCpuCount>1</MaxCpuCount></RunConfiguration>
  <NUnit><NumberOfTestWorkers>0</NumberOfTestWorkers></NUnit>
</RunSettings>
```

For the sampled profiles, attach `dotnet-trace` 10.0.750501 to the actual testhost process during a separate Default-policy case:

```powershell
dotnet-trace collect --process-id $testHostId --profile dotnet-sampled-thread-time,gc-verbose --buffersize 512 --duration 00:00:00:30 --output dictionary.nettrace
```

Filter sampled stacks to those containing `IntegrationTests_Heavy`; otherwise idle test-runner threads can dominate the thread-time view. The thirty-second windows execute different amounts of work when the implementation becomes faster, so their allocated bytes per second cannot establish an equal-work allocation reduction.

### Profiler workload and evidence coverage

[StreamMappedDictionaryWorkloadTests](../../tests/Sphere10.Framework.Utils.Profiler.Tests/StreamMappedDictionaries/StreamMappedDictionaryWorkloadTests.cs) provides the dedicated profiler fixture used for the second-pass timing matrix. Its six full cases are explicit: KVP and CLK, each with the three policies, retaining `MemoryStream`, 32-byte clusters, 250 maximum items and 250 iterations. It calls the unchanged canonical integration helper and links the same payload, serializer and comparer sources as the core fixture. Six ordinary smoke cases use eight maximum items and three iterations; they verify the setup without representing the full workload's elapsed time.

Select all six full cases in the saved profiler test DLL with this filter:

```text
FullyQualifiedName~Sphere10.Framework.Utils.Profiler.Tests.StreamMappedDictionaries.StreamMappedDictionaryWorkloadTests.IntegrationTests_Heavy
```

Pass the checked-in [serial.runsettings](../../tests/Sphere10.Framework.Utils.Profiler.Tests/StreamMappedDictionaries/serial.runsettings) file, which uses `MaxCpuCount = 1` and `NumberOfTestWorkers = 0`, matching the configuration above. The fixture is also nonparallelizable. Full runs and profile captures remain separate from other CPU-intensive jobs.

The frozen final Debug host passed 36 non-heavy checks: six smoke cases, one six-case registry check, one retained-capture validation and 28 synthetic parser cases. The retained evidence reader validates manifest workload controls, binary identity, hashed TRX inputs and all six successful results using their actual class/name identities. The synthetic cases cover both original core and profiler capture shapes, including result-to-definition matching and rejection of changed workloads, duplicate/missing results, failures, skipped cases and invalid durations. The six explicit heavy cases are additional runs and are not included in the 36-check count.

The original local campaign `E:/ProfilerEvidence/StreamMappedDictionaries/20261022-hotpaths/campaign.json` retains the original, first-pass, corrected second-pass baseline and final timing stages with relative raw TRX mappings and hashes. New stages are appended while the original raw captures and their hashes remain unchanged. This campaign's evidence validator requires Debug stages; that restriction does not make the profiler workload fixture Debug-only. Profile captures and parsed stack summaries are separate local artifacts with the filenames listed above; the binary/source provenance manifest is retained alongside the frozen final host.

## Changes

### Cluster seeking

[ClusterSeeker](../../src/Sphere10.Framework/ClusteredStreams/ClusterSeeker.cs) returns directly when a logical seek is already at the current cluster, while retaining the current-trait calculation and applicable integrity checks. Choosing between the chain start, end and current position uses scalar comparisons instead of allocating and sorting candidate tuples; the original stable tie order is preserved.

Short forward and backward traversals track visited clusters in bounded stack storage, with a `HashSet<long>` fallback for longer walks. Cycle detection and end-of-chain rejection remain active. Known nonzero enum flags use equivalent bit tests to avoid boxing in Debug execution. Existing checkpoints still follow append, truncation, chain recreation and physical-cluster migration events.

### Destination-based fragment and page reads

[IStreamFragmentProvider](../../src/Sphere10.Framework/Streams/IStreamFragmentProvider.cs) exposes destination-based reads and fragment-length lookup, with defaults for existing providers. [ClusteredStreamFragmentProvider](../../src/Sphere10.Framework/ClusteredStreams/ClusteredStreamFragmentProvider.cs) supplies the fixed cluster length without loading its payload and copies requested bytes directly into the caller's destination. [FragmentedStream](../../src/Sphere10.Framework/Streams/FragmentedStream.cs) uses this path for reads and materialization.

[StreamPagedList](../../src/Sphere10.Framework/Collections/StreamPaged/StreamPagedList.cs) reads item bytes into a supplied span. Single-item byte operations resolve their page directly instead of allocating a page-segment list and tuple. Existing page notifications and open-page scopes still surround the read. Static pages use a private stateless no-op disposable; dynamic pages retain their close operation.

Broader Release checks exposed a lifecycle error in the initial static-page optimization: `Disposables.None` is a shared mutable disposable collection, so disposing a page scope could dispose resources registered by another caller. The correction uses a private immutable no-op implementation whose repeated disposal has no effect. It never returns `Disposables.None` or another shared mutable scope. The targeted Release `GeneralTests` and `StreamPagedSpanTests` selection then passed all 187 cases, retaining the zero-allocation static-read path. This correction is included in the second-pass baseline.

[StreamMappedClusterMap](../../src/Sphere10.Framework/ClusteredStreams/ClusterMap/StreamMappedClusterMap.cs) uses small stack buffers for traits and full-width previous/next pointers. Cluster payload reads fill destination spans, and incomplete fixed-width reads are rejected. Endianness, page lifecycle and the stored cluster format are retained.

Destination buffer reads retain `IBuffer.ReadSpan` projections implemented by derived concrete buffers. Regression review also corrected the raw byte APIs on dynamic pages to translate global item indexes into page-local indexes, including the existing array read and write overloads. Derived streams positioned beyond their logical end return zero from array and span reads.

### Cluster change events

[ClusterMapChangedEventArgs](../../src/Sphere10.Framework/ClusteredStreams/ClusterMap/ClusterMapChangedEventArgs.cs) creates its mutation collections when needed. A single modified cluster remains inline until another distinct cluster or a public collection read requires a set. Internal movement queries do not create empty maps merely to discover that nothing moved.

Public collection properties keep their existing types and accessors. Repeated reads return the same mutable collection; explicit collection replacements and null assignments are retained. Events are independently owned rather than pooled, so subscribers can retain them after later writes. Cluster-map producers and the stream/seeker consumers use the new internal recording and lookup paths without changing callback order or event payload meaning.

Direct `Guard` calls remain in place, including their normal predicates and configured omission behavior. The optimization does not replace the existing validation contract or change dictionary key replacement, cache policy or integrity semantics.

### Second pass: scalar serialization contexts

[SerializationContext](../../src/Sphere10.Framework/Serialization/SerialiationContext.cs) keeps its first processed object and serialization status in fields. Scalar reference deserialization therefore avoids creating and hashing entries in the forward/inverse reference maps and status dictionary. Additional objects promote that state into the existing collections with reference-identity semantics. Finalization actions are also allocated only when registered.

The compatibility checks cover distinct value-equal objects, promotion, sizing/serialization/deserialization index reuse, null slots, sparse status updates, finalization ordering and shared-reference round trips. All eight behavior cases passed against both the corrected baseline and optimized assemblies. Both mixed-context settings produced the identical reference-graph bytes `01010673686172656402010106736861726564`, preserving shared aliases and distinct value-equal references.

### Second pass: lazy internal guard diagnostics

[Guard](../../src/Sphere10.Framework/Exceptions/Guard.cs) adds internal interpolated-string-handler overloads for `Ensure` and `Against`. Successful checks skip diagnostic expression evaluation and formatting; failing checks retain `InvalidOperationException` and ordinary interpolation formatting, alignment and current-culture behavior. Public string overloads, their default messages, direct call sites and the existing `OMIT_GUARD` conditional-call behavior remain in place. Diagnostic expressions are intentionally lazy. This change does not convert `Guard` calls to assertions or remove integrity checks.

### Second pass: restore-position scopes

[StreamExtensions](../../src/Sphere10.Framework/Extensions/StreamExtensions.cs) returns the concrete [RestorePositionScope](../../src/Sphere10.Framework/Streams/RestorePositionScope.cs) struct from both restore-position extensions. The operation retains the seekability guard, initial position capture and immediate seek order, restores through the stream's `Position` setter, and leaves stream ownership with the caller. Nested restoration, exceptions, restoration retry, repeated disposal and reentrant disposal of the same local instance are covered by the focused tests. The helper implements `IDisposable` directly; it does not inherit an asynchronous disposal adapter or call `GC.SuppressFinalize`. Ordinary `using` callers retain the value type and avoid boxing. Copies have independent disposal state, and explicit `IDisposable` assignment still boxes. Changing the public return type requires rebuilding existing compiled consumers. See [resource scopes](../guidelines/patterns/resource-scopes.md) for choosing a scope implementation.

The first 24 October 2026 disposal follow-up passed 648 focused tests in each of Release and Debug, including mapped properties, paging, cluster seeking and streamed B-tree span operations. Its allocation fixture measured 40,000 bytes for 1,000 scopes before and after replacing `SyncDisposable` with a direct `IDisposable` class. The concrete struct return is a subsequent optimization; the earlier second-pass allocation table below records the class stage.

The subsequent struct follow-up passed 651 focused tests in each of Release and Debug. Both restore-position extensions measured zero allocated bytes for 1,000 warmed concrete scope operations. Two profiler cases exercise the production consumer, `StreamPagedList<long>.WriteItemBytes`: 1,000 static-page span overwrites allocate 40,000 bytes with the saved class binary and zero bytes with the struct binary, in both endiannesses. The same profiler assembly ran in separate test hosts against both binaries; input preparation and warm-up are outside measurement. Exact stored bytes, adjacent items, restored positions and stream ownership pass before the allocation assertion in both versions. The baseline intentionally fails the new zero-allocation assertion. [Recorded comparison](evidence/20261024-restore-position-struct/comparison.json) includes binary hashes and workload conditions. These are managed allocation measurements, without a latency claim.

### Second pass: stream wrapper span forwarding (subsequently reverted)

The captured second pass forwarded spans through [BoundedStream](../../src/Sphere10.Framework/Streams/BoundedStream.cs), [NonClosingStream](../../src/Sphere10.Framework/Streams/NonClosingStream.cs) and [ConcurrentStream](../../src/Sphere10.Framework/Streams/ConcurrentStream.cs). The [24 October review](comment-and-behavior-review.md) restores all three original implementations: their small profiled bridge cost did not justify the validation-order and dispatch changes or additional compatibility machinery. The retained second-pass timings describe the earlier binaries.

## Focused allocation measurements

These numbers use `GC.GetAllocatedBytesForCurrentThread()` around the synchronous operation. Inputs, destination buffers, warm-up and retained-event arrays are prepared outside the measured interval. The seek and event comparisons run the same NUnit workloads against the saved baseline and optimized core binaries in separate test hosts. A dash means that no matching before measurement was recorded.

| Workload | Before | After | Regression fixture |
|---|---:|---:|---|
| Construct 1,000 independently retained cluster change events | 648,000 B | 160,000 B | `ClusterMapChangedEventArgsTests.EventConstructionDoesNotAllocateUnusedCollections` |
| Capture 1,000 single-cluster write events without reading their sets during the write | 760,000 B | 160,000 B | `ClusterMapChangedEventArgsTests.SingleClusterWriteKeepsModifiedPayloadWithoutAllocatingASet` |
| 3,000 current/nearby cluster seeks, integrity checks disabled | 1,464,000 B | 0 B | `ClusterSeekerTests.RepeatedStationaryAndCheckpointSeeksDoNotAllocate` |
| 3,000 current/nearby cluster seeks, integrity checks enabled | 1,656,000 B | 0 B | `ClusterSeekerTests.RepeatedStationaryAndCheckpointSeeksDoNotAllocate` |
| 1,000 raw eight-byte item reads into reused destination storage | — | 0 B | `StreamPagedSpanTests.StaticItemReadsAvoidPerReadPageAndPayloadAllocations` |
| 1,000 eighteen-byte cluster reads at offset seven, using 32-byte clusters | — | 0 B for in-memory, uncached stream-mapped and cached stream-mapped maps | `ClusterMapTests.ClusterDataReadsFillCallerStorageWithoutPayloadAllocations` |

Event construction allocates approximately 75.3% fewer managed bytes in this equal-work comparison. Destination-read figures concern the prepared storage path, not arbitrary serializers, custom fragment providers or complete dictionary operations. They are neither retained-memory measurements nor application-startup measurements.

The second-pass equal-work comparisons use the corrected baseline above, with the same Debug test workloads and allocation counters. The scalar workload creates 1,000 independent contexts; it measures context/reference bookkeeping rather than full dictionary deserialization. Both early and ordinary activation modes were measured.

| Second-pass workload | Corrected baseline | After | Regression fixture |
|---|---:|---:|---|
| 1,000 scalar deserialization contexts, ordinary activation | 872,000 B | 144,000 B | `SerializationContextAllocationTests.ScalarDeserializationAvoidsReferenceMapAllocations(False)` |
| 1,000 scalar deserialization contexts, early activation | 872,000 B | 144,000 B | `SerializationContextAllocationTests.ScalarDeserializationAvoidsReferenceMapAllocations(True)` |
| 1,000 restore-position seek scopes, historical class stage | 128,000 B | 40,000 B | `StreamRestorePositionScopeTests` (subsequently updated for concrete struct returns) |
| 20,000 successful interpolated guards | — | 0 B and zero diagnostic formatting calls | `GuardInterpolationTests.PassingGuardsDoNotFormatOrAllocate` |

The scalar allocation budgets intentionally fail against the baseline in both activation modes, while its eight compatibility cases pass. The optimized scalar workload allocates approximately 83.5% fewer bytes, and restore-position scopes allocate 68.75% fewer bytes. These allocation results are distinct from the second-pass elapsed-time comparison below and the sampled profile comparison above.

## Timing results

The table uses individual test durations from the TRX results, matching each result to its `TestDefinitions/UnitTest/TestMethod.className` through `testId` to distinguish the inherited KVP and CLK cases. Baseline results come from `TestResults/heavy-profile-baseline/heavy-six-before.trx`, except CLK `Default` and `Performance`, which use the clean repeats in `heavy-clk-clean-before.trx`. The initial CLK `Default` run overlapped profiling and the initial CLK `Performance` run overlapped an exploratory test execution; both are excluded from the comparison.

| Dictionary | Policy | Baseline elapsed | Optimized elapsed | Speedup (before/after) | Result |
|---|---|---:|---:|---:|---|
| KVP | `Debug` | 76.506 s | 51.542 s | 1.48× | Passed |
| KVP | `Default` | 73.216 s | 50.067 s | 1.46× | Passed |
| KVP | `Performance` | 68.266 s | 46.726 s | 1.46× | Passed |
| CLK | `Debug` | 395.603 s | 241.846 s | 1.64× | Passed |
| CLK | `Default` | 311.573 s | 183.960 s | 1.69× | Passed |
| CLK | `Performance` | 304.410 s | 187.838 s | 1.62× | Passed |

The six accepted baseline durations sum to 1,229.573164 seconds, compared with 761.979722 seconds in `TestResults/heavy-profile-after/heavy-six-after.trx`: a reduction of 467.593442 seconds, or 38.03%, and a 1.61× ratio of the summed durations. Totals and ratios use the unrounded TRX durations. All six optimized cases passed. The comparison uses observed per-case durations from these serial runs, rather than a statistical benchmark distribution; it does not predict Release throughput or other record sizes, cluster sizes and storage backends.

### Second-pass timing comparison

The first-pass table above predates the static-page lifecycle correction. The second-pass comparison uses the corrected baseline and frozen final optimized binaries with the unchanged six-case workload. All six cases passed in each of `second-pass-six-before.trx` and `second-pass-six-after.trx`, retained in the local campaign described above. The unrounded comparison is also retained in `second-pass-timings.json`.

| Dictionary | Policy | Corrected baseline elapsed | Final elapsed | Elapsed reduction | Speedup | Result |
|---|---|---:|---:|---:|---:|---|
| KVP | `Debug` | 50.418 s | 50.303 s | 0.23% | 1.002× | Passed |
| KVP | `Default` | 50.484 s | 48.020 s | 4.88% | 1.051× | Passed |
| KVP | `Performance` | 45.478 s | 43.852 s | 3.58% | 1.037× | Passed |
| CLK | `Debug` | 240.307 s | 218.837 s | 8.93% | 1.098× | Passed |
| CLK | `Default` | 182.736 s | 167.230 s | 8.49% | 1.093× | Passed |
| CLK | `Performance` | 181.504 s | 160.112 s | 11.79% | 1.134× | Passed |

The six corrected-baseline durations sum to 750.9277599 seconds, compared with 688.3544249 seconds for the final binary: 62.5733350 seconds less, an 8.332803545% reduction and a 1.090902786× ratio of the summed durations. Calculations use the unrounded TRX values; the table rounds elapsed times to three decimals.

The focused allocation reductions are substantially larger than these elapsed-time changes. They measure specific context or scope operations; the full dictionary workload retains its other serialization, traversal, mutation and assertion costs. These are single observed serial runs, with ordinary host noise and the existing shuffle variation, rather than repeated benchmark samples or confidence intervals. In particular, the 0.23% KVP `Debug` difference does not establish a consistent improvement at that scale. The first-pass and corrected second-pass comparisons remain separate and their percentage reductions should not be added.

The separate optimized profile comparison, artifact filenames, frozen binary identity and provenance are recorded above. Completed build, targeted validation and ordinary full Core and Profiler suite results appear below.

## Validation and evidence

| Check | Evidence recorded so far | Final-source status |
|---|---|---|
| Event payloads, collection identity/replacement, inverse mappings and retained events; existing cluster-map cases | Included in the combined focused Debug selection | Passed |
| Destination item/cluster reads, page events, short reads, dynamic page indexes and header widths | Included in the combined focused Debug selection | Passed |
| Combined event, seeker and destination-read regressions, including EOF, derived-buffer and dynamic-page corrections | 166 passed, zero failed: `TestResults/heavy-profile-after/heavy-regressions.trx` | Passed |
| Six unchanged heavy dictionary cases, serial execution | Six baseline and six optimized cases passed; uncontended CLK baseline repeats selected as described above | Passed: `TestResults/heavy-profile-after/heavy-six-after.trx` |
| Runtime/SDK and binary identity | Host, runtime, SDK, first-pass hashes, corrected baseline and frozen final host hashes recorded above | Recorded; frozen host manifest and local campaign retain provenance |
| Final CrossPlatform Release solution build | Passed in 38.78 seconds with zero errors and 20 existing NU1902/NU1903 PKCS dependency audit warnings; `final-cross-release-build.log` | Passed |
| Final Windows Release solution build | Passed in 2 minutes 18.44 seconds with zero errors and the same 20 dependency audit warnings; `final-win/build.log` | Passed |
| First-pass `OMIT_GUARD` Release core test build and targeted valid-input/corruption selection | Build passed without warnings; 25 cases passed, including truncated metadata and missing-fragment rejection | Passed for first-pass source |
| Final `OMIT_GUARD` Release core test project and selected regressions | Whole core test project built successfully; all 14 selected cases passed, zero failed; `final-omitguard.trx` | Passed for the selected cases |
| Static-page scope lifecycle correction | 187 Release `GeneralTests` and `StreamPagedSpanTests` cases passed | Passed; included in corrected second-pass baseline |
| Second-pass focused Debug checks | 396 passed, zero failed; 101 `NotExecuted` results in `second-pass-focused.trx` | Passed for the recorded second-pass source |
| Scalar context compatibility and wire bytes | Eight behavior cases passed against both assemblies; identical graph bytes for both mixed-context settings; two baseline allocation-budget failures expected | Passed |
| Lazy guard conditional omission and formatting | Debug, Release, `OMIT_GUARD` Release and explicit `DEBUG` plus `OMIT_GUARD` focused checks passed; successful interpolation allocation probe is zero | Passed for recorded handler source |
| Dedicated profiler fixture and retained evidence coverage | 36 non-heavy checks passed: six smoke, registry, retained capture and 28 synthetic parser cases | Passed in frozen final Debug host |
| Corrected second-pass comparison, serial full workload | Six explicit heavy cases passed against each binary in `second-pass-six-before.trx` and `second-pass-six-after.trx` | Passed; timings recorded above |
| Corrected baseline and final managed profiles | Thirty-second captures and parsed stack summaries retained; no allocation ticks lack stacks; comparison recorded above | Completed; sampled measurements kept separate from exact allocation probes |
| Separately profiled final CLK `Default` workload | All 250 iterations completed and passed; `second-pass-profiled-after.trx` | Passed; elapsed duration excluded from timing matrices |
| Ordinary full Core Release test suite | 12,874 passed, 102 skipped, zero failed out of 12,976 total in 9.3841 minutes; `second-pass-core-release.trx` and `second-pass-core-release.log` | Passed |
| Ordinary full Profiler Release test suite | 1,072 passed, zero failed in 5 minutes 5 seconds; six explicit heavy cases excluded as intended; all four local campaign stages validated; `profiler-full-release.trx` | Passed |

The ordinary full Core Release assembly has SHA-256 `4160194B372D5140EF01D7A8E01DCA0F35C8A3BD44BE12F773824FF70A08EE3C`. Its full-suite result and configuration are separate from the frozen Debug timing/profile host.

The final `OMIT_GUARD` Release core assembly has SHA-256 `53CC898AFE87E515C6716A28C3D8B698B195FC472CCC98EB0471C2550AF4F908`. This configuration-specific identity is separate from the frozen Debug timing/profile binary above.

The focused fixtures include `ClusterMapChangedEventArgsTests`, `ClusterSeekerTests`, `StreamPagedSpanTests`, `ClusterMapTests` and the fragment-provider read regressions. Allocation-budget failures against an older binary demonstrate a regression test's sensitivity; only the optimized-source test results should be recorded as final validation. The final full-suite results are recorded above alongside configuration-specific and earlier focused validation.
