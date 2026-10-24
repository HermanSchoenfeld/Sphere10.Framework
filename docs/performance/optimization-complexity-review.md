# Performance implementation complexity review, 23 October 2026

Current-source note: the [24 October comment and behavior review](comment-and-behavior-review.md) restores the original cluster-trait guard behavior, wrapper implementations and other unsupported behavior changes described below. It retains nonboxing flag checks with the original predicates and diagnostics. This document records its earlier checkpoint.

Source revision note: this report records the validated complexity-cleanup snapshot. A later change removes the B-tree storage marker. Current implementation selection is supplied through builders and index constructors and must be retained on reopen; it is not persisted in tree headers. See the [current ObjectSpace configuration contract](../../src/Sphere10.Framework/ObjectSpaces/README.md#configuration-and-performance). On 23 October 2026, the `FlatMerkleTree` parent-scratch and `MerkleMath.VerifyProof` scratch changes were also reverted, restoring their original implementations. The [recorded parent-scratch allocation results](object-space-hotpaths.md#bounded-merkle-parent-scratch-historical-removed) remain historical evidence; proof verification had no direct profiled-bottleneck evidence. See the [span report's revision note](span-optimizations.md). Validation and allocation results below describe the earlier snapshot. Other crypto/span changes and Merkle-root publication remain in place.

This review covers the accumulated performance changes in collections, buffering, stream I/O, transactions, cryptography, ObjectSpaces, Merkle trees, serialization and the profiler. It follows the [inheritance review](span-inheritance-review.md). The aim is to retain useful allocation reductions while removing machinery that obscures a simple operation.

## Simplifications and fixes

- Removed every `useArray` parameter and the nullable-array transport mode in endian reads. Stream wrappers validate array arguments and delegate to their virtual span overloads. Bounds and locking are implemented once in the span overload. Endian array writes likewise use the span write operation.
- Moved the `ExtendedMemoryStream` and `FragmentedStream` read/write algorithms into their public span overloads. Array and single-byte adapters call those overloads, so derived span overrides also observe the adapted operations.
- Removed snapshot/destination mode cores from `StreamMappedBuffer`, constant/dynamic stream pages, `StreamPagedList` and `StreamMappedClusterMap`. Only useful range checking, positioning and page lookup are shared. No dummy spans, nullable mode selectors or unused result outputs remain in these paths.
- Kept snapshot and destination contracts distinct. A stream-mapped snapshot still performs its existing single read and can return fewer bytes. Its destination overload uses `ReadExactly`. Endian array reads still fill until EOF; public `Read(Span<byte>)` still performs a single read. They share the appropriate span operation rather than silently changing those contracts.
- Replaced the four branch-wrapped terminal-trait guards with two direct consistency conditions. Fixed the check for a missing END flag, which previously inspected START and could accept a singleton chain lacking END. Added invalid and valid terminal-trait cases.
- Made endian array range validation safe when `index + count` would overflow. Removed redundant capacity checking and defensive disposal probing where the concrete tree contract already guarantees disposal. Simplified pool-rental setup without changing its cleanup scope.
- Put the two internal Guard interpolation handler types in their own files, preserving names and behavior. Converted their tests to constraint assertions without losing exception, diagnostic or lazy-evaluation checks.

## Deliberate API effects

Array calls on the optimized stream wrappers now reach the inner span overload. An inner stream that inherits the [BCL span fallback](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.CoreLib/src/System/IO/Stream.cs) can therefore incur its pooled temporary copy; preserving both inner transports would require retaining the policy just removed. No performance gain is claimed for that case.

`BoundedStream`, `ConcurrentStream` and `NonClosingStream` validate array arguments before entering the span method. For malformed arrays this changes exception precedence relative to boundary/lock failures, and oversized original requests are validated before bounded truncation. Valid calls still enforce bounds and parent locking. The existing bounded zero-count read shortcut remains, including avoiding argument/position inspection.

Derived span overrides now observe array calls and, for the two buffer streams, byte operations, including byte reads at EOF. A subclass that overrides both array and span methods should account for that delegation when adding side effects. Span-only calls do not invoke array overrides. Tests exercise both dispatch directions explicitly. The preexisting fragmented-array empty-store read and zero-count write shortcuts remain; those paths can return before span dispatch.

## Complexity retained after review

Private page-pool ownership and escaped-span tracking protect aliases, clearing and cache accounting. Failed-load/save recovery and commit/rollback handling remain necessary. The reviewed snapshot retained stored B-tree layout markers and the nullable historical selection; the later marker removal leaves selection in the supplied configuration.

Lazy change-event collections, inline first serialization references and lazy instance getters remove measured allocation sites. Their state preserves public collection replacement, explicit nulls, reference identity, integrity and transaction behavior. The reviewed snapshot also retained reusable Merkle parent scratch; the later removal keeps the original calculation because its measured allocation reduction did not justify that machinery on a path accounting for 0.71% of the first reindexing profile's measured samples. The later review also restores the original `MerkleMath.VerifyProof` implementation rather than retaining scratch changes in critical verification logic without direct bottleneck evidence. Guard interpolation handlers retain conditional compilation and avoid constructing passing diagnostic strings. Bounded stack scratch in other paths, alias snapshots and source-type bulk specialization remain where they have an identified purpose.

Profiler workload reuse, configuration/layout coverage and recorded-evidence validation remain. Recorded tests check capture integrity and statistical inputs; they do not assert machine-speed thresholds. Previous timing reports retain their actual source revision and must not be attributed to this later cleanup.

## Verification

Validation results and logs for this revision are retained separately in `E:\Sphere10-Performance-Simplification-20261023`. All checks pass:

- Full Release Core: **13,091 passed**, 102 not executed, zero failures.
- Full Release CryptoEx: **7,920 passed**, zero failures.
- Focused Debug behavior/allocation checks: **425 passed**, zero failures.
- Full Release profiler tests: **1,150 passed**, 37 explicit cases not executed, zero failures. The runner prints a zero skipped counter; the not-executed count comes from the individual TRX results.
- Cross-platform Release solution build: zero errors, with 20 existing NuGet audit warnings for unchanged `System.Security.Cryptography.Pkcs` dependencies.
- Core Release build with `DefineConstants=OMIT_GUARD`: zero errors and zero warnings.
- `git diff --check`: clean.

The initial 425-case focused Release pass preceded the final removal of the byte-read EOF shortcuts; the subsequent full Core run and focused Debug run cover the final implementation and EOF interception assertions. These overlapping focused checks are not additional independent coverage totals. Source hashes for the changed implementations were checked after validation. No new statistical speedup is claimed by this cleanup.

The final Core run retains these warmed allocation checks, with inputs and caller storage prepared outside measurement:

| Probe | Managed bytes allocated |
|---|---:|
| 8,192 byte stream operations, base or unchanged subclass | 0 |
| 1,000 static raw-item destination reads | 0 |
| 20,000 passing interpolated Guards | 0, with zero formatting calls |
| 32 transactional read/flush cycles with 256-KiB pages | 30,464 |
| 32 paged reads into existing 64-KiB storage, base or unchanged subclass | 21,760 of paging metadata |

The earlier range-first scalar rollback cost remains visible: 1,000 in-memory cluster reads into caller storage allocate 32,000 bytes; cached and uncached stream-mapped cluster reads allocate zero bytes. Equal 4,089-byte snapshots allocate 4,152,000 bytes in memory and 4,120,000 bytes on either streamed backend. These probes do not measure retained process memory or establish timing improvements.
