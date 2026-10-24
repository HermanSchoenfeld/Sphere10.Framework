# Comment and behavior review, 24 October 2026

This review compares the accumulated source changes with commit `d71aa957867376905031990caa1d60f72d60ad0b`, separating requested features from performance work. Existing comments are retained wherever their explanation still applies. Renamed or replaced implementations keep the explanation with the corresponding code; comments about removed mechanisms are recorded below rather than attached to unrelated replacements.

## Reverted changes

| Area | Restored behavior or implementation | Reason |
|---|---|---|
| ECDSA, Schnorr, WOTS, AMS, ordinary random generation and bit conversion | Original implementations and comments | Correctness vectors alone did not establish a measured benefit for these transformations. |
| Hash aggregation and iteration | Original full-destination aggregation input and oversized iteration-result behavior | These changed crypto behavior independently of the measured hash-chain scratch reduction. |
| Direct endian string writes, string reads and character reads | Original conversion buffers and dispatch | Their specific paths had no retained measurement. Measured character writes and serializer buffers remain. |
| `MemoryBuffer` searches, alias insertion, expansion and copying | Original search cardinality/capacity behavior, alias handling, version behavior and `CopyTo` validation timing | These included unrelated fixes. Direct scalar storage access and enumeration retain the measured allocation benefit. |
| Existing dynamic-page raw snapshot/read-write operations | Original index handling | Correcting later-page addressing was a separate bug fix. The new destination-read API translates its index locally. |
| Existing empty paged snapshots | Original zero-page rejection | Adding a destination API did not justify changing existing snapshot validation. |
| Existing extension `Visit` overloads | Original recursive traversal, null termination, shared enumeration state and lazy selector invocation | New graph-building features did not require rewriting existing traversal semantics. New `GraphNode.Visit` owns its independent iterative traversal. |
| Bounded, concurrent and nonclosing stream wrappers | Original implementations, validation order and virtual-array dispatch | The bridge cost represented about 1.07% of the sampled thread time. Retaining that change would require extra compatibility machinery to preserve behavior. |
| Stream growth, fragment resizing and transactional file-ID encoding | Original implementations | The retained byte-loop and fragment-write measurements did not exercise these paths. |
| Stream shifting | Original single-read failure contract and stream-owned index validation | Completing short reads and adding index preflight were unrelated to the measured bounded-copy algorithm. A nonnegative count check remains before sizing stack scratch. |
| Mapped properties | Original supplied-reader/writer validation timing and `FlushCache` locking behavior | Optional caching and construction helpers did not require these additional changes. |

Unused metadata inspection properties and the internal stream-decorator inspection interface were removed from the main change set. They belong with the separately isolated storage-format feature.

## Retained changes and evidence

| Change | Evidence or explicit request |
|---|---|
| Bounded node I/O, serializer buffers, padding discard and profiler read buffers | [Matched node and I/O captures](btree-span-node-io.md), including complete stored-byte comparisons. |
| Private transactional page pooling | [Page-only campaign](object-space-page-btree.md#private-transactional-page-backing), cache-accounted ownership and load/save failure coverage. |
| Cluster seeking, lazy change events, serialization contexts and guard diagnostics | [Dictionary traces and allocation comparisons](stream-mapped-dictionary-hotpaths.md). Original integrity predicates and messages remain. |
| Hash digest reuse, bounded hash-chain scratch, HashLib spans and secure erasure | [Original allocation measurements](span-optimizations.md#allocation-measurements). No aggregation or proof-math rewrite remains. |
| Fragment writes, caller-destination reads and bounded stream shifting | [Original allocation measurements](span-optimizations.md#allocation-measurements) and dictionary read-path profiles. |
| Instance tracking and unchanged Merkle-root publication | [ObjectSpace profiles](object-space-hotpaths.md). |
| Concrete restore-position value scopes | [Matched allocation evidence](evidence/20261024-restore-position-struct/comparison.json). |
| B-tree type/spec configuration, shared tree interface, compound keys, graph building, optional property caching and complete cache notifications | Explicitly requested API work, retained independently of profiling claims. |

Original B-tree shift/count/clearing comments, base-type explanations, the dirty-page save note, reader short-read explanations and the extended-memory-stream allocation note remain with their corresponding operations. The old stream-shift overlap snapshot/XOR proposal and snapshot-copy comments describe mechanisms replaced by bounded directional copying; they no longer apply. Their original text remains in the Git baseline.

## Limits

Historical captures identify their measured binaries. Their timing and allocation numbers are not measurements of this later reduced source tree. Current validation records the behavior and allocation checks executed after the reversions.

Measured span conversion still has necessary API consequences: custom encodings must supply the lower-level conversion hooks reached by span operations, and direct profiler destination reads can expose bytes written before an inner read throws. Bounded directional shifting changes block order for right overlap, including the amount of work completed before an I/O failure; it does not promise atomic copying. These limits are part of the retained mechanisms, not new fixes to existing validation or cryptographic rules.

Existing buffer-search, alias-insertion, empty-page and dynamic-page defects are tracked in the [Framework backlog](../../src/TODO.md) for separate review. The self-describing storage feature remains isolated from this source change set.
