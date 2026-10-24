# Streamed B-tree and serializer buffer allocations, 23 October 2026

Current-source note: the [24 October behavior review](comment-and-behavior-review.md) restores unsupported optimizations and unrelated behavior changes. The matched node and serializer-buffer measurements below remain evidence for the retained paths; their captured binary identities are historical.

Both streamed trees now use `Span<byte>` for destination node reads and mutation buffers, and `ReadOnlySpan<byte>` for node writes and allocation. Node counts and child pointers are written directly into those spans. Temporary character, string and padding buffers were also removed from synchronous I/O paths.

The retained comparison measures owner-thread managed allocation. It establishes no general latency or process-memory claim.

## Matched captures

The [raw captures and comparison](../../tests/Sphere10.Framework.Utils.Profiler.Tests/Results/20261023-span-node-io) retain four fresh, serial hosts in original/optimized/optimized/original order. Every host executes the same test assembly, with one unrecorded warmup and five recorded samples per case. Each configuration therefore has ten samples per binary. The comparison uses allocation medians; preparation, verification and output snapshots are outside measurement.

| Binary | SHA-256 |
|---|---|
| Original Framework | `3995A08D58D006606ABF064F821DFE73C52A9DF436CA706E4280DFD8DA315DEF` |
| Optimized Framework | `21A30012BC17B23105167B244228AA5B231724FAA067459D8362F87C6840F7B4` |
| Shared profiling test assembly | `3EF22A8386BEA2E342DC84ED41027F397C5603C1C4CFA88553EE2AFE16E04FC3` |

The saved original DLL and PDB, original node sources, isolated test hosts and validation logs remain at `E:\Sphere10-BTree-Spec-20261023`. The original binary already contained the earlier Framework optimizations and the B-tree configuration refactor; it was saved before this node-buffer and serializer-buffer work. The measured fixtures invoke existing tree and serializer APIs. These are comparisons within an uncommitted working checkout, rather than comparisons between two published releases.

## Streamed nodes

`BTreeNodeIoPerformanceTests` exercises 512 integer keys and long values with shuffled insertion, update, complete deletion and node recycling. Both tree types, both byte orders and orders 4, 64 and 256 are covered. Construction and backing-stream capacity are prepared outside measurement.

| Tree | Order | Insert allocation reduction | Delete/recycle allocation reduction |
|---|---:|---:|---:|
| BTree | 4 | 4.49% | 5.09% |
| BTree | 64 | 14.57% | 18.11% |
| BTree | 256 | 42.52% | 48.12% |
| BTreePlus | 4 | 10.15% | 5.64% |
| BTreePlus | 64 | 22.53% | 15.37% |
| BTreePlus | 256 | 46.88% | 44.04% |

The two byte orders have the same allocation medians in these captures. Update allocations are unchanged for every configuration. Generic key/value serializer allocations remain in those operations.

All **36 type/order/endianness/workload comparisons** have identical complete stored-byte hashes and lengths across both binaries and all samples. Each sample also checks contents against an independent oracle and reopens a snapshot. The [full node comparison](../../tests/Sphere10.Framework.Utils.Profiler.Tests/Results/20261023-span-node-io/node-comparison.csv) retains allocation counts, hashes and diagnostic timings.

Whole-node scratch uses at most 1,024 stack bytes; larger nodes use an exactly sliced pool rental returned in `finally`. Copies handle overlapping entry/child shifts, and vacated slots retain their original clearing and sentinel rules. The private helpers need no byte-array overloads. The virtual tree hooks remain intact. Short node reads now throw before partially initialized scratch can be used or written back.

One redundant search optimization was removed: `GetKeyValue` previously read into stack/pool scratch only for the generic span deserializer to copy it into an owned array. It now reads directly into that required array. Removing the remaining generic serializer boundary allocations requires a destination-based serializer contract; that follow-up is recorded in the [Framework backlog](../../src/TODO.md).

## Stream and serializer I/O

`StreamTemporaryBufferPerformanceTests` measures 4,096 operations per sample. Streams, capacity, contexts and inputs are prepared outside measurement. String reads include the returned string allocations. Captured reads include listener-list growth; owned capture snapshots are created afterward.

| Operation | Payload | Before bytes/batch | After bytes/batch | Reduction |
|---|---:|---:|---:|---:|
| Character write | 1 character | 131,072 | 0 | 100% |
| Stream profiler read | 64 bytes | 360,448 | 0 | 100% |
| Stream profiler captured read | 64 bytes | 884,984 | 524,536 | 40.73% |
| String serialize | 128 bytes | 622,592 | 0 | 100% |
| String serialize | 2,048 bytes | 8,486,912 | 0 | 100% |
| String deserialize | 128 bytes | 1,769,472 | 1,146,880 | 35.19% |
| String deserialize | 2,048 bytes | 25,362,432 | 16,875,520 | 33.46% |
| Padded serialize | 8,192-byte record | 34,111,488 | 458,752 | 98.66% |
| Padded deserialize | 8,192-byte record | 33,652,736 | 0 | 100% |
| Null reference padding read | 8,192 bytes | 35,230,144 | 1,577,408 | 95.52% |
| Nullable padding read | 4 bytes | 131,072 | 0 | 100% |

The [I/O comparison](../../tests/Sphere10.Framework.Utils.Profiler.Tests/Results/20261023-span-node-io/io-comparison.csv) and [allocation audit](stream-buffer-allocation-audit.md) describe exact boundaries and retained ownership. These zero-allocation loops reuse existing readers, writers, streams and contexts; allocating those objects or returning a serialized byte array remains separate work.

String serialization retains its size descriptors and partial-EOF decoding behavior. Encoding conversion now uses span APIs, so custom encodings that intercept only older string/array overloads need to implement the corresponding conversion hooks. No production custom encoding subclasses were found in this checkout. Character writing retains its legacy fixed-size zero padding. Profiler captures retain their own storage, nested scopes and short-read padding. Direct destination reads can expose bytes written by an underlying stream before it throws; incomplete reads do not append capture data.

## Configuration and validation

`BTreeType` replaces `BTreeImplementation`, and immutable `BTreeSpec` carries type and order together through builders, indexes, factories and attachments:

```csharp
builder.WithIndexBTreeSpec(new BTreeSpec(BTreeType.BTreePlus, 256));
```

A specification always contains a validated concrete type and order, exposed through get-only properties. When no type is selected, constructors receive the bare order and retain their historical context-specific defaults. Definitions hold an optional complete specification, the bare order and a bound flag, without duplicated bound specifications. Tree type and order are supplied again when reopening and are not persisted in tree headers. Profiler-owned schema-4 sidecars retain complete specifications or bare orders and explicitly read legacy sidecars and replay plans without rewriting them.

Focused regressions cover scratch boundaries, three-byte reads, both byte orders, mutation/recycling/reopen, serializer failures and retries, truncated nodes, encoding failures, padding and caller ownership. `RecordedBTreeNodeIoTests` independently checks raw capture identities, sample counts, medians and stored-byte equality during ordinary profiler test runs. Raw timings are retained for inspection without a speed threshold.

The 23 October span checkpoint passed 13,276 core Release tests (102 skipped), 303 focused Debug tests, and 1,213 ordinary profiler Release tests; the profiler's 60 explicit diagnostics are excluded from the ordinary run. All 23 allocation diagnostics passed in each of the four matched capture hosts. The cross-platform Release solution built with zero errors and 20 existing NuGet advisory warnings. Logs and TRX files are retained in `E:\Sphere10-BTree-Spec-20261023\validation`. A broader Debug run was stopped after 3,500 passing cases and replaced by the completed focused run; it is not counted as a successful full Debug run. Subsequent feature work is separate from the binary identities captured above.

`FlatMerkleTree.cs` and `MerkleMath.cs` match repository commit `d71aa957867376905031990caa1d60f72d60ad0b`: both disputed scratch/math optimizations remain reverted. Independent Merkle/proof/signature regression coverage remains.
