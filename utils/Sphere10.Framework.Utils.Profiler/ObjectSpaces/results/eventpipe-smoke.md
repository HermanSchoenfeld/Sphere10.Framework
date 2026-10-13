# EventPipe diagnostic smoke — 12 October 2026

The optional CPU-sampling path completed successfully for the file-backed warm
ID lookup workload. BenchmarkDotNet ran its normal timing child and a separate
diagnostic child, both with successful correctness checks, and exported a
6,280,509-byte Speedscope JSON plus its underlying .nettrace. Trace binaries are
local artifacts rather than repository source; the command below reproduces them.

Environment and configuration match the [initial pilot](pilot-btree-file.md):
order 64, Full indexes, 160 records, 32-byte payloads and 64 existing ID queries
per batch. The trace is diagnostic evidence and is excluded from the pilot's
configuration comparison.

```powershell
dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces --suite baseline --storage File --records 160 --operations 64 --payload 32 --filter '*WarmIdentityLookup*' --job Short --trace
```

Inspection of evented stacks containing `ObjectSpaceBenchmarkFixture.IdentityLookup`
found B-tree reads, fragmented/paged-buffer reads and provisional-instance lookup
processing prominently in the sampled call paths. Inclusive frames overlap and
are not a partition of execution time; JIT/warmup/diagnostic phases also appear
in the trace. These observations identify follow-up areas, rather than prove the
benefit of a proposed optimization.

Source inspection explains costs that configuration changes alone cannot remove:

- [InstanceTracker.TryFindProvisional](../../../../src/Sphere10.Framework/ObjectSpaces/InstanceTracker.cs)
  compiles the supplied member expression and scans tracked instances on each
  indexed query, including queries with no outstanding provisional rows.
- [StreamMappedBTree.GetKey](../../../../src/Sphere10.Framework/Collections/StreamMapped/StreamMappedBTree.cs)
  reads separately allocated key/value byte arrays before deserialization.
- [PagedBufferImplementationHelper.ReadRange](../../../../src/Sphere10.Framework/Collections/MemoryPaged/PagedBufferImplementationHelper.cs)
  assembles the requested range with `ByteArrayBuilder` and returns its array.

No runtime performance optimization was made as part of this diagnostic. Use
the profiler's complete timing, allocation and storage reports to choose
configuration candidates, and use traces to distinguish node layout and cache
effects from expression compilation, allocation and serialization costs.
