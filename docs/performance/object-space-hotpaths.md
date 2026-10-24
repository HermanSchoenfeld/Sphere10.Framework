# ObjectSpace hot-path performance, 22 October 2026

Source revision note, 23 October 2026: the current source removes the `FlatMerkleTree` parent-scratch optimization described below and restores the original parent-hash calculation. Its measured allocation reduction did not justify the added scratch/pool machinery for a path representing 0.71% of the first reindexing profile's measured samples. The earlier scratch changes to `MerkleMath.VerifyProof` were also reverted, restoring its original verification logic; no direct evidence had established proof verification as a profiled bottleneck. See the [span report's revision note](span-optimizations.md). The frozen captures, allocation numbers and combined timing results remain historical evidence for their original source revision; they do not describe the current source after these removals. Other crypto/span changes and the Merkle-root publication change remain in place.

This campaign profiles and optimizes ObjectSpaces, including Merkleized memory and file stores. Its baseline already contains the preceding stream, buffer, collection, cryptography and stream-mapped-dictionary improvements in the working tree. The numbers below are incremental to that baseline.

Four implementation changes remove repeated selector compilation, unchanged Merkle-root publication, digest-array churn and a whole-tree separator rebuild. Framework defaults, schema, index membership, hash format, endianness and transaction completion policy are unchanged.

## Changes and profiling evidence

### Provisional lookup and count

[InstanceTracker](../../src/Sphere10.Framework/ObjectSpaces/InstanceTracker.cs) previously compiled an expression on every provisional lookup, including stores containing only persisted objects. The original warm-lookup trace retained 11,188 samples beneath `RunMeasuredBatch`; `LambdaCompiler.CreateDelegate` appeared in 78.99% of them inclusively. Related compilation frames overlap and must not be added together.

A direct member selector bound to its own lambda parameter now compiles only when an applicable provisional object is encountered. Empty provisional state returns immediately. Complex, captured, null and unbound expressions retain the previous compilation/error ordering; provisional search order, custom comparers and current object values remain intact. This adds no expression cache or retained closures. Counting likewise avoids scanning tracked objects when no provisionals exist, while retaining null-type rejection.

The repeat lookup trace retained 2,996 measured-batch samples and no compilation frames. The new owner-thread allocation probe for 1,000 direct-member misses allocates **0 bytes**, compared with **4,187,264 bytes** using the frozen original Core. With an unrelated provisional type present, it falls from 4,187,232 to 40,000 bytes; enumeration still has a cost in that case.

### Merkle root publication

[MerkleTreeStorageAttachment](../../src/Sphere10.Framework/ClusteredStreams/Attachments/MerkleTreeStorageAttachment.cs) now compares root bytes before assigning the mapped header property. `FlatMerkleTree.Root` returns a fresh array, so reference equality had treated unchanged content as a new root. Repeated flushes consequently rewrote headers, raised `RootChanged` and propagated updates into the spatial tree.

The comparison reads the current mapped value, preserving header-cache invalidation. Public roots still return independent arrays. Tests cover unchanged/same-content roots, actual changes, returned-array mutation, corrupted header detection after `FlushCache`, and the distinction between an empty null root and a reaped zero-hash leaf, including reopening and recycled slots.

### B+ tree separator repair

After the first fixes, a fresh Merkle reindexing trace retained 10,046 measured-batch samples. Category-index updating appeared in 68.63%, B+ removal in 32.51%, `RefreshAllSeparators` in 14.08% and the recursive `RebuildInternalSeparators` in 9.42% inclusively. These are overlapping call paths.

[BTreePlus.Remove](../../src/Sphere10.Framework/Collections/BTree/BTreePlus.cs) repaired every internal node after every successful removal. The replacement keeps recursive parent repair and refreshes only the affected node after a borrow or merge. That local repair matters: an internal merge can copy an ancestor separator whose minimum changed during deletion. Simply deleting the global pass would leave stale keys.

The change preserves the protected virtual operations and re-resolves the surviving child after them. Fourteen new cases exercise internal/leaf borrowing and merging in both directions, minimum changes, untouched sibling writes, odd/even orders, ascending/descending comparers, both byte orders, reopening and root collapse. The repeat mutation trace retained 9,845 measured-batch samples, with no global-rebuild frame. Index maintenance remains the main mutation cost.

### Bounded Merkle parent scratch (historical; removed)

In the captured revision, [FlatMerkleTree.EnsureComputed](../../src/Sphere10.Framework/Merkle/FlatMerkleTree.cs) read both child digests into destination spans, hashed into a destination span and wrote that result. Scratch was allocated after recursive child calculation. Normal digests used bounded stack storage; requests exceeding 1,024 scratch bytes rented an array returned in `finally`. Public `Root`/`GetValue` ownership was unchanged, and a failed parent write left the parent dirty for retry.

This removed three digest arrays per rebuilt stream-backed parent, and one output array for a memory-backed parent. It was a secondary allocation optimization: tree calculation was only 0.71% of the first reindexing profile's measured samples. The optimization was removed on 23 October 2026; the current implementation again reads the children through `ReadSpan`, calls `MerkleMath.NodeHash` and writes the resulting parent digest.

Owner-thread allocations for **64 rebuilds of seven parents** exclude dirty marking and public root materialization:

| Hash | Stream before (bytes) | Stream after (bytes) | Reduction |
|---|---:|---:|---:|
| SHA2_256 | 389,120 | 313,856 | 19.3% |
| Blake2b_128 | 242,176 | 188,416 | 22.2% |
| Blake2b_512 | 306,688 | 188,416 | 38.6% |

The memory-backed controls had exactly the same allocation totals as their corresponding stream-backed cases after the captured change. Coordinate objects and hashing still allocated; this was not an allocation-free tree claim. The current [FlatMerkleTreeRegressionTests](../../tests/Sphere10.Framework.Tests/Merkle/FlatMerkleTreeRegressionTests.cs) retain checks for independent roots across perfect/imperfect shapes, insertion/removal, serialized round trips, root ownership and failed-write retry. The removed scratch implementation's allocation budget is no longer a current regression requirement.

## Paired measurements

Every case retains two independent worker launches, five warmup iterations and ten actual iterations per launch: **20 actual samples per case**, **600 across the 15 before/after pairs**. All outliers remain. No forced GC or power-plan change was requested. Warm reads and clean flushes use Throughput with a 250 ms target and adaptive invocation counts. Mutation batches use Monitoring with one invocation and unroll factor one; each starts from the same pristine baseline and verifies persisted data/indexes outside timing.

Small cases use **256 records / 200 operations**. Large cases and the separate confirmation use **1,024 records / 800 operations**. Payloads are 128 bytes, insertion and lookup are deterministically shuffled, categories equal record count and mixed work is 50/50 reads/writes. Profiles use PayloadOnly schema, Full indexes, SHA2_256, B-tree order 64, outer/inner clusters 4096/256, 256 KiB file pages, 16 MiB cache, Default policy and LittleEndian. `merkle-auto` uses real dirty-flag autosave; other profiles explicitly save. Graph-GC traits are disabled. These are pinned experimental settings, not new defaults.

Host: Windows 10 22H2, Intel Core i9-10900K (10 physical / 20 logical cores), .NET SDK 10.0.402, runtime .NET 10.0.13, BenchmarkDotNet 0.16.0-preview.2. File data and transaction pages used the same E: volume in both phases. The campaign serialized its measurement processes; unrelated system activity was not disabled. OS file caches were not flushed. Managed allocation excludes native allocation and resident/peak memory.

Times below are **ms per batch**, with BenchmarkDotNet's reported **99.9% confidence margin**. Allocation is **MiB per batch**. A batch of `CleanFlush` means 200 calls to `Flush`, including on file stores; it does not mean 200 durable commits. Mixed and reindex batches include completion: memory Flush or file Commit, compared only within the same backend.

| Shape / store | Workload / profile | Before ms ± margin | After ms ± margin | Point ratio | Managed MiB before → after |
|---|---|---:|---:|---:|---:|
| 256 / 200 / memory | CleanFlush / merkle | 0.493 ± 0.011 | 0.219 ± 0.007 | 2.25× | 0.51 → 0.23 |
| 256 / 200 / memory | WarmIdentityLookup / merkle | 26.309 ± 0.726 | 1.914 ± 0.080 | 13.74× | 2.24 → 1.44 |
| 256 / 200 / memory | WarmIdentityLookup / plain | 22.963 ± 0.383 | 1.897 ± 0.022 | 12.11× | 2.24 → 1.44 |
| 256 / 200 / memory | ReindexAndPersist / merkle | 77.849 ± 2.913 | 75.339 ± 2.473 | unresolved | 44.88 → 40.98 |
| 256 / 200 / memory | MixedReadWriteAndPersist / merkle-auto | 51.455 ± 2.079 | 40.553 ± 1.337 | 1.27× | 23.68 → 21.35 |
| 256 / 200 / file | CleanFlush / merkle | 259.509 ± 46.724 | 50.586 ± 1.164 | 5.13× | 51.75 → 50.57 |
| 256 / 200 / file | WarmIdentityLookup / merkle | 30.147 ± 0.996 | 7.696 ± 0.223 | 3.92× | 4.95 → 4.14 |
| 256 / 200 / file | WarmIdentityLookup / plain | 27.688 ± 1.793 | 8.031 ± 0.503 | 3.45× | 4.96 → 4.15 |
| 256 / 200 / file | ReindexAndPersist / merkle | 297.784 ± 8.348 | 255.052 ± 6.665 | 1.17× | 134.43 → 119.48 |
| 256 / 200 / file | MixedReadWriteAndPersist / merkle-auto | 169.880 ± 8.304 | 141.529 ± 5.162 | 1.20× | 70.41 → 62.56 |
| 1024 / 800 / memory | ReindexAndPersist / merkle | 586.082 ± 47.887 | 363.243 ± 7.457 | 1.61× | 299.72 → 232.48 |
| 1024 / 800 / memory | MixedReadWriteAndPersist / merkle-auto | 302.363 ± 12.330 | 188.721 ± 3.237 | 1.60× | 155.39 → 120.38 |
| 1024 / 800 / file | ReindexAndPersist / merkle | 2006.200 ± 85.828 | 2653.038 ± 1004.769 | unresolved | 916.02 → 669.25 |
| 1024 / 800 / file | MixedReadWriteAndPersist / merkle-auto | 1092.372 ± 67.953 | 777.601 ± 33.228 | 1.40× | 471.05 → 346.81 |
| 1024 / 800 confirmation / file | ReindexAndPersist / merkle | 2226.956 ± 72.278 | 1554.822 ± 165.931 | 1.43× | 916.02 → 669.25 |

The small memory reindex intervals overlap, so its timing improvement is unresolved despite lower allocation. Its sub-100 ms minimum iteration warning is retained. Larger batches provide a better timing comparison.

The original large file reindex result is unstable: after-launch means were 1.957 s and 3.349 s, while the original before launches were 2.052 s and 1.961 s. Its reported after mean is worse and the intervals overlap. The separate adjacent confirmation retained the same controls and measured 2.227 s before / 1.555 s after; neither original samples nor outliers were replaced. Allocation consistently fell from 916.02 to 669.25 MiB in both captures. Treat this timing case with more caution than the stable memory results; the confirmation is not a reason to discard the noisy run. Original multimodal/outlier warnings remain in the raw reports.

## Remaining file-flush allocation

A further file-clean-flush allocation trace confirms a residual cache-lifecycle cost. Its dominant measured allocation stack is `Array.Resize` beneath `MemoryPageBase<byte>.Load`, cache lookup and `PagedBufferImplementationHelper.ReadRangeInternal`, reached from `ObjectSpace.Flush`. It retained 17,310 large byte-array allocation ticks on that stack, representing approximately one page allocation per reload.

`MemoryPagedListBase.Flush` purges the cache. Unloading clears each page's `MemoryBuffer`, discarding its backing array; the next Merkle-root read reloads a page. Thus 200 calls can allocate approximately **200 × 262,144 = 52,428,800 payload bytes**, consistent with the observed file-clean allocation. Page save/load already use spans. The existing unload contract and memory accounting are preserved; retaining buffers on unloaded pages would keep memory that the cache budget no longer counts. Removing this cost requires an explicit pooled-backing/lease design or correctly invalidated root caching, beyond a temporary span replacement.

This explains why file clean-flush time improves about 5.1× while managed allocation improves only about 2.3%. Remaining warm-lookup samples are concentrated in index traversal and access scopes. No locks, validation or integrity checks were removed to improve a benchmark.

## Tests and evidence

Seventy-five new Core cases cover tracker semantics/allocation, Merkle publication, streamed tree hashing and B+ deletion. Eleven selected regression budgets were also executed against an isolated host containing the frozen original Core; all eleven failed as intended, while the optimized Core passes them. Those expected-failure captures are kept separately from validation results.

`ObjectSpaceHotPathTests` adds twelve ordinary fixture checks (including Memory/File Blake2b_128 reopening) and ten explicit trace cases. `ObjectSpaceMerkleBenchmarks.CleanFlush` joins the existing general profiler; no second timing harness was added. `RecordedObjectSpaceHotPathTests` exposes all fifteen paired captures in the profiler test project, validates original configuration/CSV/full JSON/worker logs and all raw samples, and prints descriptive results without a machine-speed assertion. It skips when the entire ignored local campaign is absent and rejects partial local captures.

The profiler's storage-size reporting also now reads its GUID-named metadata files rather than a profile-name glob that never matched them. Storage bytes are populated in the retained CSVs.

Final validation passed: complete Release Core **12,949 passed**, 102 skipped; complete Release profiler **1,099 passed**, 16 explicit cases not selected; focused Debug **749 passed**. The six retained diagnostic trace runs each completed their repeated workload and oracle checks successfully. No tests failed in these final runs. Raw TRX/logs, source verification and the separate expected-original-budget failures are retained under the local campaign's `validation` directory and `final-validation.json`.

The local raw campaign is `tests/Sphere10.Framework.Utils.Profiler.Tests/Results/20261022-objectspace-hotpaths`. Large traces, frozen source snapshots, generated workers and full logs are preserved at `E:\Sphere10-ObjectSpace-Performance-20261022`. The local campaign includes original full JSON/CSV/console logs, commands, allocation probes, stack reports and `binary-and-source-provenance.json`. Local copies use `workload-run.json`, `*-report-full.bdn.json` and `retained-worker-fingerprints.json` to distinguish their schemas from older audit campaigns. `capture-name-mapping.json` records their original filenames and unchanged SHA256 values. The campaign remains ignored output, rather than an accidental publication of generated files.

Source snapshots differ in exactly four Core implementation files; at completion of that campaign, the compiled Core/profiler source matched the final snapshot. BuildRevision was fixed to zero. Trace hosts and actual retained workers have SHA256, versions and MVIDs recorded separately. All ten final matrix worker groups, both confirmation groups, all four original large groups and the final original small group are retained. **The first five original small generated projects reused their directory before capture; their individual original generated binaries were not retained.** Frozen sources, raw build/worker commands and measurements are preserved, but later worker/launcher hashes do not reconstruct those missing original binary identities.

## Replaying

Exact profiles are versioned as `utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/profiles.hotpaths-memory.json` and `profiles.hotpaths-file.json`. Use Release without a debugger and one measurement process at a time. Choose a data volume with enough space; BenchmarkDotNet's generated worker builds can use the source project's `.bdn` directory even when report output is elsewhere. Preserve each generated worker before another CLI launch reuses that directory.

For the small memory warm/clean matrix, from the Framework root:

```powershell
dotnet run -c Release --no-launch-profile -p:BuildRevision=0 --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --config utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/profiles.hotpaths-memory.json --profiles 'plain,merkle' --records 256 --operations 200 --payload 128 --categories 256 --insertion shuffled --lookup Shuffled --mix Balanced --filter '*WarmIdentityLookup*' '*CleanFlush*' --strategy Throughput --iterationTime 250 --warmupCount 5 --iterationCount 10 --launchCount 2 --outliers DontRemove --noForcedGCs --keepFiles --data-root E:/ProfilerData --output E:/ProfilerReports/memory-warm-clean
```

For a large file mutation batch:

```powershell
dotnet run -c Release --no-launch-profile -p:BuildRevision=0 --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --config utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/profiles.hotpaths-file.json --profiles merkle --records 1024 --operations 800 --payload 128 --categories 1024 --insertion shuffled --lookup Shuffled --mix Balanced --filter '*ReindexAndPersist*' --strategy Monitoring --invocationCount 1 --unrollFactor 1 --warmupCount 5 --iterationCount 10 --launchCount 2 --outliers DontRemove --noForcedGCs --keepFiles --data-root E:/ProfilerData --output E:/ProfilerReports/file-reindex
```

Select `merkle-auto` and `*MixedReadWriteAndPersist*` for the autosave mixed case. Use the corresponding memory/file profile and 256/200/256 records/operations/categories for small mutations.

Ordinary workload checks and retained evidence:

```powershell
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0 --filter 'FullyQualifiedName~ObjectSpaces.ObjectSpaceHotPathTests&Name!~TraceHotPath'
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0 --filter 'FullyQualifiedName~RecordedObjectSpaceHotPathTests' --logger 'console;verbosity=detailed'
```

Explicit tracing selects an individual `ObjectSpaceHotPathTests.TraceHotPath` case, with `HotPathRepeats` and `HotPathArtifactRoot` in a runsettings file, `MaxCpuCount=1` and NUnit `NumberOfTestWorkers=0`. Keep creation/use/disposal on the same synchronous owner thread. Filter stacks beneath `RunMeasuredBatch`; preparation, verification and reopening are outside that marker. The traces use managed sampled thread time with GC allocation ticks, not kernel CPU time or an exact allocation census. Whole-process sampled byte totals include setup and verification and are not before/after allocation percentages; use BenchmarkDotNet and the dedicated allocation probes for those claims.
