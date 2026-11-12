# Buffered streams

`Sphere10.Framework.BufferedStream` adapts PascalFramework's `TBufferedStream`
to .NET streams. It provides bounded read-ahead and optional lazy length caching.
It does not buffer writes or add synchronization.

It extends the Framework's `StreamDecorator` and uses its inner stream reference.
Copy, legacy asynchronous and close entry points retain buffering and `leaveOpen`
semantics through explicit overrides.

```csharp
using System.IO;
using BufferedStream = Sphere10.Framework.BufferedStream;

using var file = File.OpenRead(path);
using var buffered = new BufferedStream(file);
var bytes = new byte[64];
var read = buffered.Read(bytes);
```

The default buffer is 64 KiB. Read buffering and length caching are enabled by
default. `leaveOpen` defaults to `true`, retaining the Pascal implementation's
borrowed-stream behavior. Use `leaveOpen: false` to transfer disposal ownership
after successful construction. Construction does not query position or length.

## Reads, position and ownership

A read consumes available buffered bytes and makes at most one backing read.
Positive short reads return immediately. Large requests bypass the intermediate
buffer, and sequential reads support forward-only streams. Array, span, byte and
asynchronous operations observe the same logical cursor.

Read-ahead can advance `WrappedStream.Position` beyond the wrapper's logical
`Position`. Use the wrapper exclusively while it is active. `FlushUnreadData()`
returns unread bytes to a seekable source's cursor; `SynchronizePosition()` is
its equivalent name. Unsupported or failed rewinds retain unread data and
propagate the source failure. Disposal never rewinds a borrowed source.

Writes are immediate. Writes and resizes synchronize unread bytes before changing
the source; a failed rewind prevents the mutation. `Flush()` forwards the source's
flush operation without discarding buffered input.

## Buffering controls

| Member | Behavior |
|---|---|
| `EnableReadBuffering` | Enables read-ahead; disabling releases the buffer and forwards reads directly. |
| `EnableStreamLengthCaching` | Caches a successful `Length` query, including zero, independently of read buffering. |
| `EnableBuffering()` | Enables both features. |
| `DisableBuffering()` | Disables both features. |
| `BufferSize` | Retains the configured capacity when buffering is disabled. |

Disabling read buffering with unread bytes throws before changing either flag.
Consume those bytes or explicitly call `FlushUnreadData()` first. Read buffering
and length caching are independent: changing the read-buffer flag preserves the
length-cache setting and any cached length.

Writes and resizes invalidate the length cache before attempting mutation.
Invalid arguments and zero-byte writes do not. Disable length caching for a
source whose length can change independently. Rejected position/length queries
retain unread data; partial I/O failures invalidate the buffered cursor state
and preserve the original exception.

## Verification

Run the behavioral fixture with the repository's ordinary NUnit runner:

```powershell
dotnet test tests/Sphere10.Framework.Tests/Sphere10.Framework.Tests.csproj -c Release -p:BuildRevision=0 --filter FullyQualifiedName~BufferedStreamTests
```

The decorator implementation passed 186 behavioral cases, including the Pascal
implementation's 86 semantic groups and the additional .NET asynchronous, span, copy and disposal
routes. The core and test builds completed without warnings or errors.

## Measured file performance

The [Streams README](../../src/Sphere10.Framework/Streams/README.md) retains the
latest decorator measurements and a compact performance summary. The table below
preserves the original port's baseline for comparison.

The explicit NUnit diagnostic can be selected independently:

```powershell
dotnet test tests/Sphere10.Framework.Tests/Sphere10.Framework.Tests.csproj -c Release -p:BuildRevision=0 --filter FullyQualifiedName~BufferedStreamPerformanceTests
```

It writes JSON and CSV results to a unique temporary output directory. An optional
NUnit `BufferedStreamPerformanceDirectory` test parameter selects another output
directory. The fixture deletes its own temporary data file after the run.

The following baseline measurements were taken at commit `ac2fa149a`, before the
StreamDecorator refactor, on 3 November 2026 using .NET 10.0.14,
Windows x64 and 20 logical processors. Each value is the median of five measured
rounds following two warm-up rounds. All variants use synchronous
`FileAccess.Read`, `FileShare.Read` and `FileOptions.SequentialScan`, with a warm
OS page cache. The wrappers use a `FileStream` with its internal buffering
disabled; the ordinary FileStream baseline uses a 4 KiB buffer.

One stream per configuration is retained across warm-up and measurement. File
creation, constructor and lazy buffer setup, rewinding, output reset, full-byte
validation and disposal are outside the timed loop. Length caches are primed
before timing. No forced garbage collection or counting decorators are used.

| Workload | Ordinary FileStream | .NET BufferedStream, 64 KiB | Sphere10 BufferedStream, 64 KiB |
|---|---:|---:|---:|
| 4 KiB reads over 64 MiB | 58.8437 ms | 22.1186 ms | 23.1903 ms |
| 64 KiB reads over 64 MiB | 18.9871 ms | 18.7495 ms | 18.4701 ms |
| 1 MiB reads over 64 MiB | 16.0935 ms | 16.5591 ms | 15.9757 ms |
| 1-byte reads over 256 KiB | 3.6786 ms | 2.0197 ms | 2.6737 ms |
| 8-byte reads over 256 KiB | 0.5934 ms | 0.2717 ms | 0.3507 ms |
| 64-byte reads over 256 KiB | 0.2162 ms | 0.0630 ms | 0.0690 ms |
| 256-byte reads over 256 KiB | 0.2313 ms | 0.0440 ms | 0.0422 ms |
| 250,000 Length queries | 1.4208 ms | 3.0854 ms | 1.4472 ms |

Against ordinary FileStream, Sphere10 was 2.54 times faster for 4 KiB reads and
1.38–5.48 times faster for the measured small reads. Large reads and Length calls
were near parity. The .NET 64 KiB wrapper was faster for 1- and 8-byte reads;
there is no general claim that Sphere10 is faster than every buffered alternative.

All measured steady-state loops allocated zero managed bytes. This excludes
buffer setup: the Sphere10 configuration allocated 65,808 bytes during
construction, including its backing FileStream. The .NET wrapper allocated
232 bytes during construction and another 65,560 during its first warm-up read;
ordinary FileStream allocated 240 plus 4,120 bytes. These are allocation totals,
not retained-memory measurements. The diagnostic also records unbuffered
FileStream, disabled read buffering and disabled length caching configurations.

All eight performance cases passed, validating output after every sample. Timing
results are diagnostic and depend on runtime, machine, workload and cache state;
the tests enforce correctness rather than relative-speed thresholds.
