# BufferedStream

`Sphere10.Framework.BufferedStream` extends `StreamDecorator` with bounded
read-ahead and optional length caching. Writes are forwarded immediately.
The default read buffer is 64 KiB, and `leaveOpen` defaults to `true`.

## Performance summary

Measured on 3 November 2026: .NET 10.0.14, Windows x64, 20 logical processors,
warm OS page cache. Values are medians of five rounds after two warmups.
Ordinary FileStream uses its 4 KiB buffer; Sphere10 uses a 64 KiB buffer over an
unbuffered FileStream. Creation, eager/lazy buffer setup, rewind, output reset,
validation and disposal are outside timing. Every sample validates its output.

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

Larger ratios favor Sphere10. These measurements show the main benefit in small
field and 4 KiB reads; single-byte, large-read and Length results are near parity.
The .NET 64 KiB BufferedStream was faster for 1-, 8- and 64-byte reads in this run.
These are local diagnostic results, not speed guarantees or cold-disk throughput.
All warmed operation loops allocated zero managed bytes; this excludes setup
and does not remove the retained 64 KiB buffer allocation.

## Reproduce and use

The explicit NUnit `BufferedStreamPerformanceTests` fixture compares ordinary
and unbuffered FileStream, .NET BufferedStream, and Sphere10 with its buffering
and length-cache switches. Run it from the Framework repository root:

```powershell
dotnet test tests/Sphere10.Framework.Tests/Sphere10.Framework.Tests.csproj -c Release -p:BuildRevision=0 --filter FullyQualifiedName~BufferedStreamPerformanceTests
```

The fixture retains raw samples, JSON and CSV results in a temporary output
directory. Set the NUnit `BufferedStreamPerformanceDirectory` test parameter to
choose its location. Timings have no relative-speed assertions.

See [the usage guide](../../../docs/usage/buffered-stream.md) for ownership,
cursor behavior, buffering controls and the earlier baseline measurements.
