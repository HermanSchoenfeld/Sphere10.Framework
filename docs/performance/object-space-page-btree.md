# ObjectSpace page reuse, span consolidation and tree selection, 22 October 2026

Current-source note: the [24 October behavior review](comment-and-behavior-review.md) retains measured page reuse and node I/O while restoring the original wrapper, Schnorr and transactional file-ID implementations and unrelated paging behavior. Frozen captures below remain unchanged.

Source revision note: the measurements and identities below describe the retained frozen implementation. A subsequent [span inheritance review](span-inheritance-review.md) removes subclass type gates and reverts several marginal shortcuts. The later [complexity review](optimization-complexity-review.md) removes mode-based read helpers and replaces the array-transport policy described below with array-to-span delegation. Those revisions have separate validation; these earlier numbers are not claims about every later source revision.

The current source also removes the B-tree storage marker described below. Implementation selection now comes from the builder and index constructors, with the original selection required on reopen like the order. See the [current ObjectSpace configuration contract](../../src/Sphere10.Framework/ObjectSpaces/README.md#configuration-and-performance). On 23 October 2026, the inherited `FlatMerkleTree` parent-scratch and `MerkleMath.VerifyProof` scratch changes were reverted, restoring their original implementations. See the [historical parent-scratch results and removal note](object-space-hotpaths.md#bounded-merkle-parent-scratch-historical-removed) and the [span report's revision note](span-optimizations.md). The retained captures and their source identities remain unchanged and do not measure these later removals. Other crypto/span changes, Merkle-root publication and page-cache changes remain in place.

This campaign retains seven page-only before/after pairs, four descriptive explicit-tree cases, both diagnostic trace oracles, counts-only causal diagnostics and six fresh-process footprint captures. Page-only measurements are isolated from the later combined source changes.

This follow-up addresses the file-page reload allocation identified in [the preceding ObjectSpace campaign](object-space-hotpaths.md). The page-only clean-flush batch fell from **54.930 to 25.830 ms**, with managed allocation falling from **50.572 to 0.539 MiB**. The captures do not establish a stable timing improvement for warm lookup or mutations. The original mutation means are worse and noisy; an adjacent reindex confirmation is worse with disjoint reported intervals, while the reverse-order run returns near baseline with overlapping intervals. All of those results remain visible below.

## What changed

### Private transactional page backing

The file cache continues to purge/unload pages on flush. Holding backing arrays on unloaded pages would retain memory outside the cache's loaded-page accounting, so the implementation instead gives private transactional byte pages an internal pool-owned `MemoryBuffer` mode. Unload detaches the array from the page, clears it and returns an unexposed exact-size array to `ArrayPool<byte>.Shared`. A subsequent load can rent that storage again. No public `MemoryBuffer` constructor changes its ownership policy.

The private page path borrows spans only for synchronous page I/O and destination copies. A nonempty public `AsSpan`/`ReadSpan` escape retires that backing array from pooling, preserving the caller's alias rather than allowing reuse underneath it. Snapshot reads still return owned bytes. Oversized pool buckets are returned and replaced with exact-size nonpooled arrays, keeping page-size cache accounting intact. Failed opening/loading returns or detaches page storage and restores the unloaded state; disposal releases private page backing.

The page-only Core delta is exactly these five production files, recorded with hashes in `page-only-changes.json`:

- `Collections/Buffer/MemoryBuffer.cs`
- `Collections/Buffer/IBufferPageReader.cs`
- `Collections/MemoryPaged/PagedBufferImplementationHelper.cs`
- `Collections/MemoryPaged/MemoryPagedListBase.cs`
- `Transactions/TransactionalFileMappedBuffer.cs`

The regression fixture is `TransactionalPageBufferPoolingTests`. Its owner-thread allocation probe runs **32 read/flush cycles with 262,144-byte pages**. The frozen original Core allocates **8,426,424 bytes** and fails the new budget as expected; page reuse allocates **30,464 bytes** and passes. This is allocation evidence, not a timing or retained-memory measurement.

### Array and span implementation consolidation

The combined source also makes array entry points thin adapters into shared private span cores. `BoundedStream` shares boundary/truncation logic and `ConcurrentStream` shares locking logic. Explicit legacy array transports preserve observable inner-array overrides and the original validation order. Derived span fallbacks continue to intercept virtual array overrides; the base decorator is not changed to bypass those policies. Empty bounded array reads retain their original shortcut, including avoiding buffer and position inspection. The common synchronous bounded clamp also avoids overflow near `long.MaxValue`.

`StreamMappedBuffer` uses one private read core, retaining its distinct existing contracts: an owned snapshot performs one legacy array read and returns a short result; a destination read fills completely or throws. Cluster maps share source/destination cores. Raw item reads share list notification/scope sequencing and per-page addressing/default-length logic. Their actual fill loop is centralized in `EndianBinaryReader`; its array, owned-array and strict internal reads retain array/span transport as appropriate. The pre-existing public endian span `Read` remains one underlying read, and its documentation now says so. Endian writes share disposal/transport logic. Schnorr's array-to-positive-integer helper delegates to its span implementation while preserving null, unsigned and big-endian behavior.

Meaningful coverage includes inner and derived dispatch, null/offset/count and disposal order, boundary overflow, empty operations, parent locking, short reads, truncated data, untouched destination tails, later dynamic pages, nullable whole-item lengths, page events and integer vectors. No Guard predicate or compile-out policy was changed for this consolidation. Existing hash-returning overloads, `ExtendedMemoryStream`, `FragmentedStream` and bulk buffer writes already shared their span implementations. `NonClosingStream`'s inherited array forwarding and decorator/interface projection fallbacks remain compatibility adapters; replacing them with direct destination forwarding would bypass external overrides.

### Ordered-tree implementation selection

The current API uses `BTreeType.BTree` and `BTreeType.BTreePlus`, carried with the order in an immutable `BTreeSpec`. `ObjectSpaceBuilder.WithIndexBTreeSpec` applies the specification to all member indexes on both Memory and File backends:

```csharp
var memoryBuilder = new ObjectSpaceBuilder()
	.UseMemoryStream()
	.WithIndexBTreeSpec(new BTreeSpec(BTreeType.BTreePlus, 64));
var fileBuilder = new ObjectSpaceBuilder()
	.UseFile("store.dat")
	.WithIndexBTreeSpec(new BTreeSpec(BTreeType.BTreePlus, 64));
```

In the frozen combined revision, omitting selection or passing null retains the historical mixed index layout rather than forcing one implementation across all indexes. Explicit selection is recorded in reserved header bytes; incompatible stored layout, unsupported marker version and invalid marker values are rejected. Preserve the choice when reopening. Changing an existing unmarked index to another implementation requires rebuilding into new storage. The implementation-independent `Tree` property exposes the selected ordered dictionary while legacy typed properties retain their compatibility contract.

The non-plus streamed B-tree search path reads only keys during binary search, avoiding unused value deserialization. Current code keeps the virtual comparison hook and reads directly into the owned array required by the generic serializer, avoiding scratch storage that the span serializer adapter would immediately copy into another array. Both streamed trees also accept spans in their private node helpers. These later changes are absent from the page-only timing delta below.

## Paired page-only measurements

Each of the seven before/after pairs retains two independent BenchmarkDotNet worker launches, five warmup iterations and ten actual iterations per launch: **20 actual samples per capture, 280 actual samples in total**. Four pairs are the original matrix, two are adjacent confirmations and one reverses capture order. Outlier mode is `DontRemove`; no forced GC or power-plan change was requested. Worker exit codes are zero. The full reports, CSV values and raw normalized measurements agree; no speed threshold is used to validate them.

The pinned workload uses 256 records, 200 operations per batch, 128-byte payloads, 256 categories, deterministic shuffled insertion/lookup, a balanced 50/50 mixed workload, PayloadOnly schema, Full indexes, SHA2_256, tree order 64, 4,096-byte clusters, 256 KiB file pages, 16 MiB cache, Default policy and LittleEndian. `merkle-auto` enables actual dirty-flag autosave; `merkle` explicitly saves. `CleanFlush` is a batch of 200 flush calls, not 200 commits. Mutation batches include persistence. Preparation and result verification remain outside timing.

Warm/clean cases use Throughput with a 250 ms target and adaptive invocation counts. Mutation cases use Monitoring with one invocation and unroll factor one. The source pair is frozen prior ObjectSpace code plus the footprint fixture (`E:\s10p26\b`), versus that same source with the five page files above and pooling tests (`E:\s10p26\p`). The broader combined source snapshot is `E:\s10p26\a`: its final implementation benchmarks use artifact root `h_f`; the D-source test build uses `h_a`. Those builds must not be substituted for the page-only comparison. BuildRevision is fixed to zero. All generated workers for **14 CLI groups** are retained with per-group assembly hashes: 12 page-only source-capture groups and two explicit-implementation groups.

Times are **ms per 200-operation batch ± BenchmarkDotNet's reported 99.9% confidence margin**. Managed allocation is **MiB per batch**. Intervals describe the reported samples, not a guarantee of performance on another machine.

| Workload / profile | Before ms ± margin | After ms ± margin | Point ratio before/after | Managed MiB before → after | Timing interpretation |
|---|---:|---:|---:|---:|---|
| CleanFlush / merkle | 54.930 ± 7.364 | 25.830 ± 0.601 | 2.13× | 50.572 → 0.539 | Reported intervals separate |
| WarmIdentityLookup / merkle | 7.808 ± 0.102 | 8.006 ± 0.508 | 0.98× | 4.144 → 4.144 | Unresolved; intervals overlap |
| ReindexAndPersist / merkle | 264.274 ± 10.224 | 497.836 ± 247.426 | 0.53× | 119.476 → 119.226 | Worse observed mean; noisy, overlapping intervals |
| MixedReadWriteAndPersist / merkle-auto | 138.791 ± 2.015 | 454.814 ± 338.597 | 0.31× | 62.560 → 62.310 | Worse observed mean; noisy, overlapping intervals |

The clean-flush allocation reduction is **98.93%**. Warm-lookup allocation is unchanged. Reindex and mixed allocation each fall by approximately 0.250 MiB, which must not be presented as a large mutation-allocation improvement.

The mutation timing is unstable. Original reindex after-launch means are 549.254 and 446.418 ms, with retained 1,047.915 and 1,479.438 ms actual observations. Original mixed after-launch means are 516.487 and 393.141 ms, with retained maxima of 1,554.581 and 1,465.254 ms. Both reported after intervals overlap their before intervals. These worse means remain part of the report; interval overlap alone does not prove equivalence or establish a causal regression.

The separate confirmations use the same controls and source pair. The reverse-order experiment executes the page-pool build first and baseline later; both the fixture and portable extraction validate that timestamp ordering. Original samples and outliers were not replaced.

| Additional capture | Before ms ± margin | After ms ± margin | Managed MiB before → after | Timing interpretation |
|---|---:|---:|---:|---|
| Adjacent ReindexAndPersist | 277.329 ± 51.626 | 488.932 ± 103.481 | 119.476 → 119.226 | Disjoint reported intervals; worse in this capture |
| Adjacent MixedReadWriteAndPersist | 140.783 ± 4.493 | 138.211 ± 5.202 | 62.560 → 62.312 | Intervals overlap |
| Reverse-order ReindexAndPersist | 285.469 ± 91.079 | 273.469 ± 16.415 | 119.476 → 119.226 | Intervals overlap; after build captured first |

The adjacent reindex confirmation must not be dismissed: its after interval lies wholly above the before interval. The reverse-order result then returns to overlapping values, with launch means of 289.890/257.048 ms after and 312.114/258.824 ms before. Together these captures do not support a stable mutation speedup or a stable source-caused mutation regression. They are consistent with sensitivity to capture order and/or runtime/file-system conditions; that explanation is an inference rather than a proven mechanism. The reported worse adjacent capture remains visible. There is no mutation-speed recommendation based on this sequence.

The machine uses Windows 10.0.19045, .NET 10.0.13, X64 and 20 logical processors. Before/after file data use the same E: volume. OS file caches were not flushed and unrelated system activity was not disabled. No process-footprint, peak-memory or native-allocation claim follows from BenchmarkDotNet's managed allocation column.

## Explicit tree implementations on the combined build

The final combined build runs the same 256-record / 200-operation Merkleized Full-index `ReindexAndPersist` workload with every member index explicitly configured as BTree or BTreePlus. Both Memory and File use tree order 64 and SHA2_256. All four cases retain two launches, five warmups and ten actual iterations per launch, Monitoring with one invocation/unroll factor one, all outliers and no forced GC. These **80 actual samples**, together with the seven source pairs above, make **360 actual samples** in the new campaign.

These are cross-layout descriptive cases on one combined build, not before/after comparisons of page pooling or of the key-only accessor. Memory Flush and File Commit have different completion guarantees; compare implementations within one backend.

| Backend / explicit layout | Mean ms ± reported 99.9% margin | Managed MiB per batch | Encoded store bytes |
|---|---:|---:|---:|
| Memory / BTree | 61.808 ± 1.766 | 29.369 | 148,324 |
| Memory / BTreePlus | 79.666 ± 2.103 | 44.185 | 144,211 |
| File / BTree | 341.052 ± 135.316 | 85.466 | 148,324 |
| File / BTreePlus | 504.139 ± 139.730 | 127.972 | 144,211 |

In this small memory dataset BTree has lower time and allocation, with separated reported timing intervals. File means also favor BTree, but their broad timing intervals overlap. Encoded store sizes legitimately differ across node layouts; their schema, index membership, dataset, hash and workload controls match. Four fixed-shape cases do not establish a universal preferred tree or justify changing historical defaults. `analysis/tree-implementation-comparison.json` retains all raw normalized samples and per-launch statistics, and its portable extractor validates the explicit configuration, CSV/full-report values and actual worker identities.

## Retained memory and startup cost

Three separate NUnit processes per build run the same file/Full-index/Merkle configuration and 2,000 clean flush calls. Each verifies and reopens the persisted store. Only the final checkpoint, explicitly named `DisposedAfterDiagnosticFullGC`, forces collection. These diagnostics are separate from the timing jobs.

The table uses **decimal MB**, with **median [minimum–maximum] across three processes**. Every byte value, all six checkpoints, process IDs, thread allocations and total allocations are retained in `analysis/file-page-footprint-summary.json` and the six original `file-page-memory.json` files. The process totals include NUnit, the CLR, JIT, loaded assemblies and fixture state.

| Checkpoint | Metric | Before MB: median [range] | After MB: median [range] |
|---|---|---:|---:|
| OpenedAndWarmed | Private bytes | 59.408 [53.494–59.511] | 53.772 [53.699–53.883] |
| OpenedAndWarmed | Managed estimate | 22.407 [13.617–22.412] | 13.367 [13.347–13.396] |
| OpenedAndWarmed | GC committed | 43.319 [37.519–43.319] | 37.519 [37.511–37.523] |
| After2000Flushes | Private bytes | 44.323 [43.483–45.908] | 54.731 [53.699–58.380] |
| After2000Flushes | Managed estimate | 14.413 [14.370–15.956] | 19.362 [19.340–19.388] |
| After2000Flushes | GC committed | 28.111 [27.718–29.848] | 37.519 [37.511–37.523] |
| DisposedAfterDiagnosticFullGC | Private bytes | 40.030 [39.772–41.173] | 50.586 [50.156–54.780] |
| DisposedAfterDiagnosticFullGC | Managed estimate | 12.276 [12.234–12.279] | 12.510 [12.499–12.513] |
| DisposedAfterDiagnosticFullGC | GC committed | 23.523 [23.425–24.314] | 33.989 [33.972–33.989] |
| DisposedAfterDiagnosticFullGC | Working set | 90.931 [90.743–92.049] | 100.905 [100.471–105.390] |

After disposal and diagnostic collection, the median private footprint is **10.555 MB higher**, and GC committed bytes are **10.465 MB higher**. The managed estimate is only **0.234 MB higher**, consistent with a returned 256 KiB array retained by the shared pool. That correspondence is an inference, not an exact retained-object census. `ManagedBytes` uses `GC.GetTotalMemory(false)`, while heap size/committed values describe the most recent GC. Reduced allocation changes collection/commit behavior; the higher private footprint must not be represented as 10 MB of live page arrays or hidden by reporting only managed allocation.

Opened/warmed private ranges overlap, so these three captures do not establish a startup-footprint regression or a general startup-memory improvement. After 2,000 flushes the pool build has higher private/committed memory but dramatically less allocation churn. Median owner-thread allocation between `OpenedAndWarmed` and `After2000Flushes` is 530,311,560 bytes before and 5,976,304 bytes after, including the small checkpoint instrumentation cost.

## Diagnostic stacks and remaining work

The before and after clean-flush captures use managed thread sampling and GC allocation ticks. Under `RunMeasuredBatch`, the exported before stack groups contain **33,830 byte-array allocation ticks across 12 `Array.Resize` → `MemoryPageBase.Load` variants**. The after export contains none on that path. This confirms the targeted large-array stack was removed from the retained dominant groups; it is not an equal-work allocation-rate calculation.

The analyzer exports only its top 500 allocation-stack groups globally. A zero in that subset does not prove absence from omitted groups or unsampled allocations. Whole-process byte weights include setup and verification, so no whole-process weighted-byte reduction percentage is used here. The direct allocation probe and paired BenchmarkDotNet reports support the allocation claim instead.

The after marker-qualified exported groups still include cache traits (37 ticks), boxed enum values (26), flush actions (15), byte arrays attributed to the inlined flush path (11), Merkle flush access-scope actions (10), page-array growth (9) and page-segment tuples (8). Their sampled weights are not exact sizes for all allocations covered by each tick. No parent-hash computation stack is retained beneath the clean marker; this clean workload should not be used to claim that dirty parent rebuilding is allocation-free. Setup, metadata and index-verification allocations outside the marker are separate work.

The managed workload sample counts are 3,799 before and 3,070 after. Exclusive `FileStreamHelpers.FlushToDisk` samples rise from 47.38% to 75.83%; `BufferedFileStreamStrategy.ReadSpan` falls from 21.30% to 2.96%. These are proportions of managed sampled thread time and may include time in file operations; they are not kernel CPU-time measurements. Clearing pooled buffers, cache metadata and synchronization still have costs.

The complete trace workload and oracle checks pass before and after, each performing all 1,280 requested repetitions. Whole NUnit runs take 4 m 52 s and 6 m 16 s respectively, including setup, instrumentation, verification and reopening. These total durations are retained execution evidence and are not substituted for the serialized BenchmarkDotNet timing results.

Separate read-only causal diagnostics locate the existing transactional page buffer and count its events. In all six retained iterations per build, including the labeled warmup, both builds perform **99,182 page reads, 21,446 writes, one load, two saves, two unloads and one commit**, with 148,324 loaded bytes and 296,648 saved bytes. Store length, returned operation count, open-page state and all record/checksum totals match. This constrains the explanation: the sampled workload did not silently do fewer logical page operations or skip persistence verification after pooling. Loaded/saved bytes sum page sizes and are not OS I/O/fsync counters.

Those instrumented diagnostics ran while another CryptoEx suite was active. Their raw wall/CPU fields remain in the original files, but the extracted summary deliberately excludes timing comparisons. They provide activity/equivalence evidence only. The private source is retained byte-for-byte as `diagnostics/ObjectSpacePageReindexDiagnostics.cs.txt`, SHA256 `F52956B5A27F6007BAAE3392EF2AC21E6E09D3F6B9254D736A07C60CA9888DC4`; the non-compilable suffix prevents the test SDK's source glob from compiling the private diagnostic. The original `.cs` remains on E unchanged.

## Validation and evidence

Executed page-only checks: **133 passed, zero failed**, with the expected original-Core allocation-budget failure retained separately. The new allocation probe's passing page result is extracted from the original TRX into `allocation-probes/page-pooling-allocation-probe.json`; the full TRX remains on E. Six footprint runs pass in distinct processes. Original timing workers complete with exit code zero, and the extraction scripts validate all seven paired shapes/configurations, 280 actual samples, host metadata, capture ordering, warmup counts, CSV/full-report agreement and unremoved outliers.

The final unfiltered Release Core run passes **13,036 tests**, with **102 skipped** and zero failures. The complete Release CryptoEx suite passes **7,920 tests**, with zero failures. Its crypto source is unchanged since that run; the later Core-only marker cleanup is covered by the final Core run. Earlier combined focused runs pass 3,022 Core and 70 profiler cases; these overlap the complete runs and must not be added as independent coverage totals.

The Debug checks pass **110 tests**, covering both tree implementations, deletion oracles, ObjectSpace builder/mutation/reopen/rollback, pooled page ownership/failure paths and array/span dispatch. No Guard compilation policy was changed in this follow-up. Transactional page loads now use `ReadExactly`, so short streams fail independently of conditional Guard compilation.

The full Release profiler suite passes **1,148 tests**, including all **26 `RecordedObjectSpaceHotPathTests` cases** across the two campaigns. Its TRX contains **37 `NotExecuted` explicit cases**; the runner's printed skipped counter is zero, so the per-result TRX outcomes are the source of that exclusion count. The clean-flush trace oracles and six footprint diagnostics above were executed explicitly in separate processes; the ordinary suite does not rerun statistical campaigns.

The cross-platform Release solution build succeeds with zero errors and 20 NuGet audit warnings in existing package dependencies. No package versions were changed. Final original TRX/logs and the additive `final-validation-summary.json` remain in the E campaign. The summary records per-result outcomes and original TRX hashes; it does not rewrite captures. `git diff --check` passes.

Local evidence is under `tests/Sphere10.Framework.Utils.Profiler.Tests/Results/20261022-objectspace-page-btree`. Original raw captures, source snapshots, generated workers and full validation files remain under `E:\Sphere10-ObjectSpace-PageBTree-20261022`. The local campaign stays ignored output. Copies use `workload-run.json`, `*-report-full.bdn.json`, `retained-worker-assemblies.json` and `console.txt` to avoid schema discovery by older recorded-evidence fixtures. `capture-name-mapping.json` lists each original/retained name, byte count and unchanged SHA256.

`provenance/final-worker-provenance.json` inventories all **14 actual retained worker groups × three DLLs = 42 binaries**, with SHA256, assembly/file/product versions and module MVID. It reads PE metadata without executing the worker assemblies. The same inventory compares frozen combined source `E:\s10p26\a` with current D-source bytes: **1,073 Core + 52 CryptoEx + 35 profiler `.cs`/`.csproj` files = 1,160 exact matches, zero differences**. Its stated scope excludes generated bin/obj output and does not claim that tests, documentation or every repository file were compared. The local provenance copy is byte-identical to the original E file, SHA256 `5A9CFFC0248EAF04550D5936A7F93B56FB9CCDEB39892CE8352F8AEE584C83DE`, and is included in the capture-name mapping. All **121 retained mapped payloads** pass byte-count/SHA256 replay verification; original captures remain unchanged.

The portable `analysis/analyze_page_campaign.py` accepts original or retained basenames and preserves the initial four-pair summary. The additive `analysis/analyze_extended_campaign.py` includes all seven pairs, validates capture order and reports diagnostic counts/equivalence. They recompute descriptive summaries without reading external binaries or absolute output paths from capture metadata. Both have been run successfully against the local retained campaign. Replay on a fresh machine requires new captures; an absent ignored campaign is not fabricated from the documentation.

```powershell
python tests/Sphere10.Framework.Utils.Profiler.Tests/Results/20261022-objectspace-page-btree/analysis/analyze_extended_campaign.py --campaign tests/Sphere10.Framework.Utils.Profiler.Tests/Results/20261022-objectspace-page-btree --output E:/RecomputedPageEvidence
```

`RecordedObjectSpaceHotPathTests` exposes the seven new paired cases and four explicit-implementation cases alongside the preceding campaign. It validates retained evidence and prints descriptive results without a machine-speed assertion. The portable `analysis/analyze_tree_implementations.py` similarly validates all four layouts' 80 actual samples without reading external binaries.

Run the correctness and retained-evidence checks from the Framework root:

```powershell
dotnet test tests/Sphere10.Framework.Tests -c Release -p:BuildRevision=0
dotnet test tests/Sphere10.Framework.CryptoEx.Tests -c Release -p:BuildRevision=0
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0
dotnet test tests/Sphere10.Framework.Utils.Profiler.Tests -c Release -p:BuildRevision=0 --filter 'FullyQualifiedName~RecordedObjectSpaceHotPathTests' --logger 'console;verbosity=detailed'
dotnet build 'src/Sphere10.Framework (CrossPlatform).sln' -c Release -p:BuildRevision=0
```

The original exact launch arguments and frozen sources remain in the E campaign. To measure the final combined explicit-tree configuration again, export its retained profiles and use fresh output/data directories. Repeat with the Memory capture to compare that backend. This produces a new descriptive run; it cannot reconstruct the original page-only baseline from the current combined source.

```powershell
$capture = Get-Content tests/Sphere10.Framework.Utils.Profiler.Tests/Results/20261022-objectspace-page-btree/implementation-file/workload-run.json -Raw | ConvertFrom-Json
$capture.Settings.Profiles | ConvertTo-Json -Depth 12 | Set-Content E:/replay-tree-profiles.json
dotnet run -c Release -p:BuildRevision=0 --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --config E:/replay-tree-profiles.json --profiles btree,btreeplus --records 256 --operations 200 --payload 128 --categories 256 --insertion shuffled --lookup Shuffled --mix Balanced --warmupCount 5 --iterationCount 10 --launchCount 2 --outliers DontRemove --noForcedGCs --keepFiles --data-root E:/replay-tree-data --output E:/replay-tree-results --strategy Monitoring --invocationCount 1 --unrollFactor 1 --filter '*ReindexAndPersist*'
```
