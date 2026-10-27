# Borrowed array scopes, 25 October 2026

`BorrowArrayScope<T>` centralizes array-pool acquisition and return in a concrete readonly `IScope<T[]>`. `Tools.Scope.BorrowArray(length)` supplies a byte rental; `BorrowArray<T>(length)` supplies other element types. Both support an explicit originating pool and an optional `clearArray` return policy. Required pools and nonnegative lengths are validated before renting.

`Item` is the actual rented array, which may exceed the requested size. `Length` and `Span` expose the requested logical range. Synchronous disposal directly returns the same array to its originating pool. Async disposal schedules that return on a worker and represents its completion and failure. Keep one owner and one normal disposal per rental; there are no lifecycle flags, shared state, automatic idempotence or adopted-array constructors. Default required-resource scopes are invalid.

```csharp
using var rental = Tools.Scope.BorrowArray(byteCount, clearArray: true);
stream.ReadExactly(rental.Span);
Consume(rental.Span);
```

See [array rental guidance](../guidelines/patterns/resource-scopes.md#array-rentals) for ownership and factory details. Clearing applies on return; the pool does not promise zero-filled contents on rent.

## Production migration boundaries

Production byte-array `ArrayPool` rent/return calls now go through `BorrowArrayScope<byte>`. Existing shared-pool configuration remains in transactional page construction. Tracking pools in tests and the profiler's deliberate raw-pool comparison retain their direct pool APIs.

- Both stream-mapped B-tree implementations retain their 1,024-byte stack threshold. Larger node records use nullable rental scopes to represent actual optional ownership; stack-sized records retain their existing `stackalloc` path.
- Stream shifting retains its 1,024-byte threshold; string serialization and endian character writing retain their 512-byte thresholds. Routing and comparison helpers return their borrowed buffers through the scope. This migration does not change these algorithms or thresholds.
- Encrypted streams retain their existing chunk sizes and clearing policy. Write paths continue sizing chunks from the physical rental's `Item.Length`, preserving the previous behavior when the pool supplies a larger bucket. Copy paths retain their requested read range. Both synchronous and asynchronous operations return the rental after use, with clearing requested.
- `MemoryBuffer` retains its optional rental until unload. It still retires storage exposed through a public span rather than returning it while that view can survive. Its ordinary return path clears the full array before returning with `clearArray: false`, preserving custom-pool behavior without double clearing. Oversized pool buckets are still cleared and returned instead of being retained against page-size cache accounting.

A scope cannot make an escaped array/span safe to reuse or prevent a copied scope from returning the same rental twice. The buffer retirement policy remains domain ownership logic; it is not generic scope lifecycle management. Cache, page pinning and transaction behavior remain covered by the existing integration and pooling tests.

## Warmed allocation and timing screening

The explicit profiler compared three bodies in one serial Release host: manual `ArrayPool<byte>.Shared` rent/`try`/`finally`/return, concrete `BorrowArrayScope<byte>`, and `Tools.Scope.BorrowArray`. Each body writes and reads the same requested array index, computes the same checksum and returns with the same clearing policy. It runs two complete warmup batches per case/mode, then five measured batches with rotating execution order.

The .NET 10.0.13 Windows x64 capture completed **90 verified samples**, covering 256-, 4,096- and 65,536-byte requests with clearing both disabled and enabled. Those samples contain **1,105,920 measured rent/use/return operations**, including **737,280 through the concrete scope or factory**. Every sample recorded **zero current-thread allocated bytes** and zero Gen0/Gen1/Gen2 collections. Resources, warmup, verification, hashing and evidence output are outside the measured batch.

| Requested bytes | Clear on return | Operations per batch | Raw median µs | Concrete scope median µs | Factory median µs |
|---|---|---:|---:|---:|---:|
| 256 | No | 16,384 | 216.2 | 295.6 | 313.8 |
| 256 | Yes | 16,384 | 334.5 | 411.3 | 435.9 |
| 4,096 | No | 16,384 | 210.3 | 287.1 | 306.2 |
| 4,096 | Yes | 16,384 | 1,007.0 | 1,134.0 | 1,124.1 |
| 65,536 | No | 4,096 | 85.9 | 101.3 | 110.5 |
| 65,536 | Yes | 4,096 | 5,914.2 | 5,850.4 | 5,697.4 |

These are short-batch screening observations from one host, **not a speedup or timing-equivalence claim**. The smaller scope/factory batches generally recorded more elapsed time than the raw body. No independent longer BenchmarkDotNet confirmation was run for this change, and no latency threshold is asserted. The capture measures warm synchronous shared-pool use; pool misses can allocate arrays, custom pools can allocate, and async return scheduling has ordinary task/closure costs. Process memory includes the NUnit host and evidence construction, so its snapshots are not a standalone scope-footprint measurement.

The [raw capture](evidence/20261025-borrow-array-scopes/array-pool-ee629b84c9d34f759f1457171a80e0ad/array-pool-scopes.json) retains all samples, conditions, source snapshots and binary identities. The [summary](evidence/20261025-borrow-array-scopes/allocation-summary.json) recomputes every group's medians and operation totals without excluding unfavorable results.

## Final validation

The Windows Release solution build completed with zero errors and ten existing PKCS package audit warnings. The profiler Release build completed with zero warnings/errors. A downstream SystemExpert source-compatibility build also completed with zero warnings/errors; this report does not claim a new downstream test run.

| Final check | Passed | Skipped |
|---|---:|---:|
| Full Core Release, final array-scope implementation and migrations | 13,865 | 102 |
| Focused Core Debug | 194 | 0 |
| Explicit array-pool performance capture | 1 | 0 |

Unlike the earlier callback consolidation's validation boundary, the full Core run here covers the final array-rental source. Focused Debug includes rental ownership, encrypted streams, B-tree spans, stream spans, transactional page pooling, string serialization and endian ownership. Tests cover exact originating-pool return, clear flags and full-capacity reference clearing, body failures, invalid inputs and zero logical length, generic element types, failed rent/return, invalid default scopes, blocked async return and fault propagation, plus warmed concrete/factory allocation budgets.

The [validation manifest](evidence/20261025-borrow-array-scopes/validation.json) identifies all logs and TRX outcomes. The [compressed logs](evidence/20261025-borrow-array-scopes/validation-logs.zip) contain nine passing build/test/capture artifacts, SHA-256 `98D7B7CA42F2F48CD7C98F165EB5E20D2D8546BF4663FC4A5B3AA580445EE6FA`.

## Retained source and reproduction

The [source archive](evidence/20261025-borrow-array-scopes/source.zip) contains **4,108 C# source, project, solution and build/configuration files**, SHA-256 `89E0415DF6D043B0FD5066201AD7795DAE55A5EABF2EB0D83D951F48740127A7`. It includes the scope, factories, production migrations, tests and profiling harness. Generated output/results and ordinary resource assets are excluded; it is a code snapshot rather than a resource-complete distribution. [Verification](evidence/20261025-borrow-array-scopes/source-verification.json) found zero missing/mismatched entries when compared with source and confirms all three capture source snapshots against that archive.

The measured Core assembly is version `3.1.3.0`, built with `BuildRevision=0`, SHA-256 `3FCC71301FAAF6FAD95D8C5EE20E29C0D3B12C2185E4F17E6101764211286D54`. [Binary identities](evidence/20261025-borrow-array-scopes/binary-identities.json) retain the final Release/Debug test and profiler assembly hashes separately from the source hashes.

To repeat, build the matching profiler test project in Release with `BuildRevision=0`. In a fresh host, explicitly select `ArrayPoolScopePerformanceTests.CaptureArrayPoolScopeConsumers`; set `SPHERE10_ARRAY_SCOPE_ARTIFACTROOT` to the output directory. The fixture fixes the three lengths, clearing variants, two warmups and five samples per group. Keep the matching source, harness and binaries together. The [earlier callback consolidation](callback-scope-consolidation.md) and its package-era artifacts remain a separate unchanged snapshot.
