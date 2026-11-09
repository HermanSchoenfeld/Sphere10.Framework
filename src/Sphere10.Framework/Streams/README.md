# Streams

`Stream` implementations, stream decorators and `Tools.Streams` helpers for bounded, buffered, encrypted, hashed, concurrent, fragmented and blocking streams.

This folder holds the core library's `Stream` implementations, stream
decorators and stream helpers. They belong to the `Sphere10.Framework`
namespace in the `Sphere10.Framework` package, except the static
`Tools.Streams` helpers.

Most types here are decorators. `StreamDecorator<TStream>` forwards every
member to an inner stream, and each decorator overrides only what it changes.
A decorator usually comes in a generic form that keeps the inner stream's
type and a non-generic form over `Stream`. `StreamDecoratorExtensions`
provides fluent wrappers such as `AsBounded`, `AsReadOnly`, `AsWriteOnly`,
`AsNonClosing`, `AsConcurrent`, `AsTransactional` and `OnDispose`.
`Tools.Streams` provides helpers such as `RouteStream`, `ReadByteArray`,
`ShiftBytes`, `CompareStreams` and `GZipCompress`. The serializers in
[Serialization](../Serialization/README.md) read and write through these
streams, and `TransactionalStream` in
[Transactions](../Transactions/README.md) adds commit and rollback. The NUnit
fixtures for these types are in
[tests/Sphere10.Framework.Tests/Streams](../../../tests/Sphere10.Framework.Tests/Streams).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BackgroundOutputStream`](BackgroundOutputStream.cs) | class | Queues writes and copies them to another stream on a thread-pool thread. |
| [`BitStream`](BitStream.cs) | class | Reads and writes bits in a byte array. |
| [`BlockingStream`](BlockingStream.cs) | class | In-memory channel whose reads block until a writer adds data or calls `SetEndOfStream()`. |
| [`BoundedStream`](BoundedStream.cs) | class | `BoundedStream<Stream>` over any stream. |
| [`BoundedStream<TStream>`](BoundedStream.cs) | class | Confines reads and writes to a segment of the inner stream, with absolute or relative addressing. |
| [`BufferedStream`](BufferedStream.cs) | class | Bounded read-ahead and optional length caching, described [below](#bufferedstream). |
| [`ByteArrayStreamFragmentProvider`](ByteArrayStreamFragmentProvider.cs) | class | `IStreamFragmentProvider` over a list of byte arrays that creates new fragments with a size or generator you choose. |
| [`ConcurrentStream`](ConcurrentStream.cs) | class | Throws `InvalidOperationException` from every member unless the calling thread holds the scope from `EnterAccessScope()`. |
| [`EncryptedStream`](EncryptedStream.cs) | class | `EncryptedStream<Stream>` over any stream. |
| [`EncryptedStream<TStream>`](EncryptedStream.cs) | class | Length-preserving AES-CTR encryption with random access. It stores no header, padding or authentication tag, so authenticate the ciphertext separately. |
| [`ExtendedMemoryStream`](ExtendedMemoryStream.cs) | class | Memory stream over an `IBuffer` rather than one contiguous array. |
| [`FragmentedStream`](FragmentedStream.cs) | class | One logical stream over the fragments of an `IStreamFragmentProvider`, such as `ByteArrayStreamFragmentProvider`. |
| [`HashingStream`](HashingStream.cs) | class | Write-only stream that hashes the written bytes and returns the digest from `GetDigest()`. |
| [`IStreamFragmentProvider`](IStreamFragmentProvider.cs) | interface | Maps non-contiguous byte fragments into one logically contiguous byte range for `FragmentedStream`. |
| [`IStreamProvider`](IStreamProvider.cs) | interface | Opens read and write streams for one resource. |
| [`NonClosingStream`](NonClosingStream.cs) | class | `NonClosingStream<Stream>` over any stream. |
| [`NonClosingStream<TInner>`](NonClosingStream.cs) | class | Leaves the inner stream open when it is closed or disposed. |
| [`OnDisposeStream`](OnDisposeStream.cs) | class | `OnDisposeStream<Stream>` over any stream. |
| [`OnDisposeStream<TInner>`](OnDisposeStream.cs) | class | Runs an action with the inner stream when the decorator is disposed, then disposes as usual. |
| [`ReadOnlyStream`](ReadOnlyStream.cs) | class | `ReadOnlyStream<Stream>` over any stream. |
| [`ReadOnlyStream<TStream>`](ReadOnlyStream.cs) | class | Rejects writes with `NotSupportedException`. |
| [`StreamDecorator`](StreamDecorator.cs) | abstract class | `StreamDecorator<Stream>` for decorators that do not need the inner stream's type. |
| [`StreamDecorator<TStream>`](StreamDecorator.cs) | abstract class | Abstract base that forwards every member to an inner stream, so a subclass overrides only what it changes. |
| [`StreamDecoratorExtensions`](DecoratorExtensions.cs) | static class | Fluent `OnDispose`, `AsBounded`, `AsTransactional`, `AsNonClosing`, `ToNonClosing`, `AsReadOnly`, `AsWriteOnly` and `AsConcurrent` wrappers. |
| [`StreamMappedProperty`](StreamMappedProperty.cs) | static class | `From` and `At` factories that map a property onto a caller-owned stream. |
| [`StreamMappedProperty<T>`](StreamMappedProperty.cs) | class | Reads and writes one serialized value at a fixed stream offset, as [mapped properties](../../../docs/usage/keys-graphs-and-stream-properties.md) describes. |
| [`StreamPipeline`](StreamPipeline.cs) | class | Connects synchronous stream filters through `BlockingStream` channels. |
| [`StreamProfiler`](StreamProfiler.cs) | class | Records the bytes read between `StartListening()` and `StopListening()`. |
| [`Tools.Streams`](StreamTool.cs) | tools | Stream routing, byte-array reading, byte shifting, comparison, file writing, GZip and symmetric encryption helpers. |
| [`WriteOnlyStream`](WriteOnlyStream.cs) | class | `WriteOnlyStream<Stream>` over any stream. |
| [`WriteOnlyStream<TStream>`](WriteOnlyStream.cs) | class | Rejects reads with `NotSupportedException`. |

## BufferedStream

`Sphere10.Framework.BufferedStream` extends `StreamDecorator` with bounded
read-ahead and optional length caching. The two features are independent, so
length caching still works when read buffering is disabled. Writes go to the
source immediately. The default read buffer is 64 KiB
(`BufferedStream.DefaultBufferSize`), and `leaveOpen` defaults to `true`, so
disposal leaves the source open.

```csharp
using System.IO;
using BufferedStream = Sphere10.Framework.BufferedStream;

using var file = File.OpenRead(path);
using var buffered = new BufferedStream(file);
var bytes = new byte[64];
var read = buffered.Read(bytes);
```

Read-ahead can move the source's position beyond the wrapper's logical
`Position`, so use the source only through the wrapper while it is active.
`FlushUnreadData()` returns unread bytes to a seekable source. The
[usage guide](../../../docs/usage/buffered-stream.md) covers ownership, cursor
behavior, the buffering controls and the earlier baseline measurements.

### Measured performance

The table compares an ordinary `FileStream` using its 4 KiB buffer with
`BufferedStream` using a 64 KiB buffer over an unbuffered `FileStream`. It was
measured at commit `586235528` on 3 November 2026 with .NET 10.0.14 on
Windows x64 with 20 logical processors and a warm OS page cache. Each value is
the median of five rounds after two warm-up rounds. Creation, buffer setup,
rewinding, output reset, validation and disposal are outside the timed loop,
and every sample validates its output.

| Workload | FileStream | Sphere10 BufferedStream | FileStream time / Sphere10 time |
|---|---:|---:|---:|
| 4 KiB reads, 64 MiB file | 79.9432 ms | 30.5887 ms | 2.61 times |
| 64 KiB reads, 64 MiB file | 24.7547 ms | 23.9356 ms | 1.03 times |
| 1 MiB reads, 64 MiB file | 20.1919 ms | 18.8083 ms | 1.07 times |
| 1-byte reads, 256 KiB | 3.2805 ms | 3.3748 ms | 0.97 times |
| 8-byte reads, 256 KiB | 0.5481 ms | 0.4537 ms | 1.21 times |
| 64-byte reads, 256 KiB | 0.2371 ms | 0.0925 ms | 2.56 times |
| 256-byte reads, 256 KiB | 0.2646 ms | 0.0548 ms | 4.83 times |
| 250,000 Length queries | 2.3167 ms | 2.3052 ms | 1.00 times |

Larger ratios favor Sphere10. The main benefit is in small-field and 4 KiB
reads, while single-byte, large-read and `Length` results are near parity. The
.NET 64 KiB `System.IO.BufferedStream` was faster for 1-, 8- and 64-byte reads
in this run. The warmed loops allocated no managed memory, which excludes setup
and the retained 64 KiB buffer. These are local diagnostic results rather than
speed guarantees or cold-disk throughput.

### Reproduce the measurements

The explicit NUnit fixture `BufferedStreamPerformanceTests`, in category
`Performance`, compares ordinary and unbuffered `FileStream`, the .NET
`BufferedStream`, and `BufferedStream` with each buffering and length-cache
switch. Because the fixture is `[Explicit]`, it runs only when the filter
selects it. Run it from the Framework repository root:

```powershell
dotnet test tests/Sphere10.Framework.Tests/Sphere10.Framework.Tests.csproj -c Release -p:BuildRevision=0 --filter FullyQualifiedName~BufferedStreamPerformanceTests
```

The fixture writes raw samples, medians and environment details to
`buffered-stream-performance.json` and `buffered-stream-performance.csv` in a
new temporary directory. The NUnit test parameter
`BufferedStreamPerformanceDirectory` selects another directory. Timings carry
no relative-speed assertions. The behavioral fixture is `BufferedStreamTests`,
and the [usage guide](../../../docs/usage/buffered-stream.md#verification)
gives its command.
