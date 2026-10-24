<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Span and buffer allocation optimizations

Current-source note: the [24 October comment and behavior review](comment-and-behavior-review.md) reverts unsupported crypto, RNG, bit-conversion, direct string-I/O, stream-growth and search transformations, along with unrelated contract changes. The original measurements below retain their captured boundaries; claims about reverted implementations are historical.

Source revision note: this report retains the original optimization pass and its measurements. The subsequent [inheritance review](span-inheritance-review.md) removes exact-type subclass gates and reverts the `ExtendedList<T>` scalar/enumeration/copy shortcuts and stream buffer whitelist. The later [complexity review](optimization-complexity-review.md) removes transport-mode helpers and makes stream array operations delegate to virtual span overloads. Original scalar allocation reductions and subclass-dispatch claims below must be read against those revision notes.

On 23 October 2026, the scratch changes in `MerkleMath.VerifyProof` were reverted to preserve its original verification implementation. No dedicated proof-verification allocation or timing measurement, or direct profiled-bottleneck evidence, had been recorded. The later `FlatMerkleTree` parent-scratch change was also removed; its [frozen allocation measurements](object-space-hotpaths.md#bounded-merkle-parent-scratch-historical-removed) remain historical evidence. Other crypto/span changes, Merkle-root publication and page-cache changes remain in place. The original measurements and validation totals below describe their captured source revision, not the current source after these removals. Irregular-preimage proof tests and deterministic AMS vectors remain as correctness regressions.

Measured on October 22, 2026 using Release builds on Windows x64, .NET 10.0.13 and SDK 10.0.402. These changes address transient allocations in extended lists, buffers, transactional files, fragmented streams and cryptography.

## Changes

- `ExtendedList<T>` handles scalar operations, enumeration and `CopyTo` directly against its backing array. Concrete span overloads support append, update, insertion and reads into caller-owned storage. Arrays, `List<T>` and concrete extended lists can supply contiguous enumerable inputs without an intermediate array. Self-insertion and overlapping sources remain safe.
- `MemoryBuffer` handles scalar operations, searches, enumeration and copying directly. Searches return the first logical index for each requested item and exclude unused capacity.
- `IBuffer.ReadRange(long, Span<byte>)` supports destination storage supplied by the caller. Its default implementation supports existing implementers. Built-in memory, paged, file and stream buffers specialize the operation; page copying stays within the page's open scope with its existing notifications.
- Paged range snapshots allocate one exact-size result instead of a growing builder followed by another copy. `ExtendedMemoryStream` uses destination reads, span writes and byte operations, and bounds zero-fill scratch space during growth. Built-in buffer materialization allocates one result array; custom buffers retain their `CopyTo` behavior.
- Fragmented writes pass only the changed slice to the fragment provider. They no longer copy an entire fragment for each update. Materialization allocates its final array once, and fragment resizing no longer builds discarded fragment-index collections.
- `Tools.Streams.ShiftBytes` copies right-overlapping ranges backwards and other ranges forwards. Scratch space is bounded by the smaller of the requested block size and byte count; small blocks use the stack and larger blocks use a scoped pool rental. Short reads are completed before writing a block. The established final stream position is retained.
- Page persistence writes spans directly and completes short reads. Transactional file IDs encode short paths and their digest into bounded stack buffers. Commit, rollback and reopened-file behavior remain covered by integration tests.
- Hash chains use bounded scratch spans and digest-sized destination slices. `HashFunctionBase` reuses its digest storage across resets. WOTS fills key/signature rows directly, and AMS assembles its signature into one final allocation. The captured revision also reused fixed-size scratch for Merkle proof verification with a dynamically sized hash fallback; that `VerifyProof` change was reverted on 23 October 2026.
- HashLib and BouncyCastle consume existing spans for supported inputs. Schnorr tagged hashes use bounded stack preimages. Random generation fills its destination directly; `SecureErase` no longer allocates another secret-sized array.
- Endian text I/O uses bounded stack buffers for small strings and character decoding, with larger string fallback storage. Malformed negative string lengths retain a catchable exception and cannot reach a negative stack allocation. Four-byte bit-conversion scratch is cleared before partial reads.

## Allocation measurements

All numbers are managed bytes allocated by the measured synchronous operation, using `GC.GetAllocatedBytesForCurrentThread()`. Inputs, output buffers, capacity reservation and warm-up are outside the measured interval. Before and after runs use separate NUnit test hosts and the same workload. Small runtime bookkeeping differences can vary between runs.

| Workload | Before | After |
|---|---:|---:|
| ExtendedList scalar/read/update/search/insert/remove/enumeration/copy, 8 × 1,024 items | 2,754,176 B | 384 B |
| ExtendedList contiguous range append/update/read, 64 × 1,024 items | — | 0 B |
| MemoryBuffer scalar/copy/enumeration workload | — | 48 B |
| FragmentedStream, 100 one-byte writes into a 64 KiB fragment | 6,556,000 B | 0 B |
| Shift a 4 MiB overlapping range with 4 KiB scratch limit | 4,198,512 B | 136 B |
| 1,000 direct Blake2b-256 digests into reused output | 56,000 B | 0 B |
| 1,000 Blake2b-256 chains of 32 hashes | approximately 2,050,000 B | 192,000 B |
| Overwrite a 1 MiB secret | 1,048,624 B | 0 B |
| Eight HashLib SHA-256 hashes of 1 MiB input slices | 8,390,336 B | 1,536 B |
| ExtendedMemoryStream, 8,192 byte operations | — | 0 B |
| Read 256 KiB of paged data into caller storage | — | 2,416 B |
| Materialize 256 KiB of paged data | — | 264,584 B |
| Materialize a 64 KiB memory stream | — | 65,560 B |

The core baseline is the binary saved before this optimization pass. The CryptoEx baseline is an available pre-task local Release binary; its computed hashes were checked against the same expected SHA-256 values. A dash means no matching baseline measurement was recorded. Baseline allocation-budget tests intentionally fail against the old binaries. These measurements establish allocation reductions, not elapsed-time, retained-memory or application-startup results.

## Compatibility and regression coverage

Existing array and enumerable methods remain supported. Fast paths retain subclass range and stream-array interception. Decorators retain overridden `ReadSpan` behavior, arbitrary custom buffer materialization retains `CopyTo`, and overlapping adapter reads preserve the original bytes. No borrowed page storage escapes its open-page scope through destination reads.

Tests cover custom comparers, logical bounds, mutation versions, owned snapshots, self-insertion, aliased spans, partially filled destination tails, empty buffers, zero-filled growth, page eviction and notifications, transactional commit/rollback/reopen, short stream reads and malformed encoded lengths. Deterministic WOTS and AMS public-key/signature hashes match the saved baseline, including both public-key modes and actual height-three proofs. Existing BIP340 vectors and ECDSA tests also pass.

Guard calls retain their original predicates and messages, including compilation omission of the entire call site. Enabling `OMIT_GUARD` also required importing `System.Diagnostics` and removing invalid `Conditional` attributes from the two value-returning cast helpers; C# cannot omit a method that supplies a value. The Release core build with `OMIT_GUARD` and six valid-input allocation smoke tests pass.

The focused regression fixtures are `ExtendedListSpanTests`, `BufferSpanTests`, `StreamSpanTests`, `CryptoSpanTests`, `ExtendedMemoryStreamTests` and `TransactionalMarkerMemoryTests`. Existing collection, storage, bit, hashing and signature fixtures provide broader coverage. Run them through the normal NUnit runner:

```powershell
dotnet test tests/Sphere10.Framework.Tests/Sphere10.Framework.Tests.csproj -c Release -p:BuildRevision=0
dotnet test tests/Sphere10.Framework.CryptoEx.Tests/Sphere10.Framework.CryptoEx.Tests.csproj -c Release -p:BuildRevision=0
dotnet build "src/Sphere10.Framework (CrossPlatform).sln" -c Release -p:BuildRevision=0
dotnet build "src/Sphere10.Framework (Win).sln" -c Release -p:BuildRevision=0
```

Executed validation:

- Full core suite: **12,700 passed, 102 skipped, zero failed**, in approximately 6.6 minutes. Results: `TestResults/span-core-final/span-core-final.trx`.
- Final-source focused regression selection: **192 passed**. Results: `TestResults/span-final-source/span-final-source.trx`. This selection overlaps the full core run and also verifies the final minor stream cleanup and normal Guard build.
- Focused CryptoEx selection: **473 passed**, including the changed adapters, ECDSA and BIP340 coverage. Results: `TestResults/cryptoex-span/cryptoex-span-final.trx`. The entire CryptoEx suite was not run.
- CrossPlatform and Windows Release solution builds: **passed**. Existing NuGet vulnerability audit warnings for transitive PKCS packages remain; dependencies were not changed by this pass.
- `OMIT_GUARD` Release core build: **passed with zero warnings**, plus **six passing** valid-input allocation smoke tests. Results: `TestResults/span-omitguard/span-omitguard.trx`.
- Final whitespace diff check: **passed**.

Stack allocations stay bounded. Large or variable-size buffers retain heap/pool fallbacks, returned arrays retain independent ownership, and compatibility fallbacks for arbitrary decorators can still allocate. The existing AMS private-key height-byte mismatch is outside this change; height-sensitive tests explicitly select and assert the parsed height.
