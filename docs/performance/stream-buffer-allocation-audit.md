# Stream and page buffer allocation audit, 23 October 2026

Subsequent evidence: [matched node and I/O captures](btree-span-node-io.md#stream-and-serializer-io) now record the comparisons planned below. The [24 October behavior review](comment-and-behavior-review.md) retains those measured buffer paths and restores unsupported direct string-I/O, growth and wrapper changes.

This review covers `Collections/MemoryPaged`, `Collections/StreamPaged`, `Collections/FileMapped`, `Streams`, `IO` and `Transactions`. It distinguishes temporary conversion/copy buffers from stored data, public results and buffers whose lifetime extends beyond the call. Source inspection identifies candidates; it does not establish a measured hot path.

## Temporary buffers removed

`EndianBinaryWriter.Write(ReadOnlySpan<char>)` previously allocated a byte array for every call, including calls through `Write(char)`. It now uses at most 512 stack bytes and a pool rental returned in `finally` for larger requests. It retains the existing output length of twice the character count, including zero padding and encoding failures when that storage is insufficient. This change does not redefine that legacy character encoding contract. Checked size arithmetic retains an overflow exception before encoding or stream output.

`StreamProfiler.Read` now reads into the caller's destination and copies captured bytes directly into its private listener storage. Array reads validate their arguments and delegate to the virtual span overload. Captures remain independent of later caller-buffer mutation. The existing array behavior captures the entire requested length, including zero padding after short reads and EOF; the shared span path preserves that behavior. Nested listener scopes still copy their completed capture into the parent. `StopListening` and `PeekBytes` retain their independently owned results.

`StringSerializer` now encodes and decodes using at most 512 stack bytes and a pool rental returned in `finally` for larger payloads. Serialization still writes the declared byte-count prefix before encoding the payload. Deserialization uses the bytes actually read, including partial payloads at EOF; it does not decode uninitialized or zero-filled trailing storage. The requested serialization buffer is cleared first, preserving zero tails for encoders that produce fewer bytes than their declared count.

Conversion dispatch now uses `Encoding.GetBytes(ReadOnlySpan<char>, Span<byte>)` and the nonvirtual `Encoding.GetString(ReadOnlySpan<byte>)`, which reaches the encoding's lower-level conversion hooks. Custom encodings that intercept only `GetBytes(string)` or `GetString(byte[], int, int)` no longer intercept those calls. No production custom `Encoding` subclass was found under `src` or `utils`; the regression fixture exercises failures through lower-level virtual conversion methods, not a promise to preserve the old overload interception.

The three padding readers in `PaddedSerializer`, `ConstantSizeReferenceTypeSerializer` and `NullableSerializer` share a bounded `EndianBinaryReader.DiscardBytes` helper. It reads through a stack buffer, preserving short-read/EOF behavior and disposed, negative-length and unsupported-length validation. Large `PaddedSerializer` writes emit bounded, explicitly cleared zero chunks. Existing reference discriminators and the nullable reader's unconditional constant-size skip are unchanged; correcting that separate nullable behavior requires its own compatibility decision.

The regression fixtures are `EndianCharSpanTests`, `StreamProfilerSpanTests`, `StringSerializerSpanTests` and `PaddingDiscardSpanTests`. The explicit profiler fixture `StreamTemporaryBufferPerformanceTests` measures owner-thread allocations through existing APIs in `TemporaryBufferSamples` and `SerializerTemporaryBufferSamples`. Its `StreamTemporaryBufferArtifactRoot` test parameter selects a fresh report directory. The same test assembly can exercise saved before/after Framework binaries without requiring the new internal or span APIs. Capture-list growth, returned strings and reference tracking remain inside measurement; preparation, output capacity, owned snapshot materialization and validation remain outside. Diagnostic timings are not speed thresholds. This document records no before/after result before those captures are executed.

## Arrays retained for ownership or lifetime

| Area | Why storage remains an array |
|---|---|
| Loaded memory/file/transactional page backing | Page contents must survive the current call. Private transactional backing already uses the existing pool/escaped-span ownership policy; a stack buffer cannot replace resident page storage. |
| `PagedBufferImplementationHelper.ReadRange` and stream-page `ReadItemBytes(..., out byte[])` | These return independently owned snapshots. Caller-destination span overloads already provide the allocation-avoiding path. |
| `EndianBinaryReader.ReadBytes`, `ReadBytesOrThrow`, `ReadArray2D` | Their arrays are the requested public result. A short `ReadBytes` result also retains its existing exact returned length. |
| `FragmentedStream.ToArray`, `ExtendedMemoryStream.ToArray` | These explicitly materialize owned snapshots. |
| `ByteArrayStreamFragmentProvider`, `BitStream`, `BinaryFileCache` | Arrays are retained fragments, stream contents or cached file payloads. |
| `BackgroundOutputStream` and `BlockingStream` | Queued chunks outlive `Write`. Replacing their copies with borrowed spans would allow callers to mutate queued data. Pooling would require a separate ownership and failure-cleanup design. |
| `EncryptedStream` asynchronous writes and copy | Scratch must remain valid across `await`; stack spans cannot cross that lifetime. Existing rentals are returned through their cleanup scopes. The reusable keystream is instance state. |
| `HashingStream.GetDigest` and profiler capture results | Arrays are caller-owned outputs, not discarded I/O scratch. |
| Endian reader/writer 16-byte conversion buffers | They are allocated once per reader/writer and reused. They are not a new payload allocation for every primitive. |
| Constant-size nullable/reference serializer padding fields | Their zero arrays are allocated once with the serializer and reused on writes. Padding reads no longer allocate discarded result arrays. |

Generic `MemoryPagedBuffer` enumerable adapters must materialize arbitrary noncontiguous sequences before passing them to span operations. Their byte-array inputs already avoid that conversion. `BinarySerializedPage<TItem>` likewise passes an item-array snapshot into the graph serializer; removing it requires preserving the serialized type/graph contract, not just replacing byte scratch.

## Candidates left for a separate measured change

- Direct `EndianBinaryReader.ReadString` / `EndianBinaryWriter.Write(string)` calls and long transactional file-path encoding still allocate temporary bytes beyond their existing small stack thresholds. Pooling could replace that fallback, but these paths need their own allocation evidence and unchanged encoding/exception behavior. These are separate from the optimized `StringSerializer`. File-ID path encoding occurs during construction, not on every commit.
- `EncryptedStream.WriteByte` reaches a synchronous write that rents scratch and a cleanup scope even for one byte. A small synchronous stack path could reduce that overhead without changing the cipher, but no workload-specific benefit was established in this audit.
- `Tools.Streams.RouteStream` and `CompareStreams` already use scoped pool rentals. Those rentals are bounded transfer storage, not one new payload array per block. Span dispatch and blockwise comparison are possible follow-ups; they must preserve close flags, short-read behavior and failure handling.
- `StreamProfiler` has no internal Framework caller in the inspected source. Its removed buffer is a concrete per-read allocation site, not evidence that it dominates ObjectSpace workloads.
- `FileSystem.GetFilePart` returns an owned array and shrinks it after a short read. A caller-destination overload could avoid materialization for consumers that do not need a snapshot; replacing the returned array with stack storage is not an equivalent implementation.

`FastMarshalStructSerializer` pins arrays for general interop marshaling; a blanket `MemoryMarshal` replacement would require stronger unmanaged-layout constraints.

No additional ownership, cache, transaction or cryptographic mechanism was changed by this audit. The earlier Merkle parent-scratch and proof-verification reversions remain in effect.
