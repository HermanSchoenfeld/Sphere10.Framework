# Live statistics without sample history

Read this topic when tracking measurements, diagnostic counters, throughput or
latency summaries without retaining individual observations. Use Sphere10's
[Statistics](../../../src/Sphere10.Framework/Maths/Statistics.cs), rather than a
parallel collection of values and custom mean/min/max calculations. Read
[synchronization](synchronization.md) when several producers/readers share the
accumulator and [resource scopes](resource-scopes.md) for scope ownership.

## Accumulate summaries, not a timeline

Construct `new Statistics(0)` explicitly. Zero is the history capacity: the
accumulator keeps summary fields and does not retain a sample timeline. Here,
"stateless statistics" means statistics without sample history; the accumulator
necessarily retains count, sums and extrema.

```csharp
using System;
using Sphere10.Framework;

var latencyMilliseconds = new Statistics(0);
if (double.IsFinite(measuredMilliseconds) && measuredMilliseconds >= 0)
	latencyMilliseconds.AddDatum(measuredMilliseconds);

var count = latencyMilliseconds.SampleCount;
double? mean = count > 0 ? latencyMilliseconds.Mean : null;
double? minimum = count > 0 ? latencyMilliseconds.Minimum : null;
double? maximum = count > 0 ? latencyMilliseconds.Maximum : null;
double? deviation = count > 0 ? latencyMilliseconds.PopulationStandardDeviation : null;
```

This fragment assumes `measuredMilliseconds` comes from a real completed
measurement. Keep one accumulator per metric and a consistent unit throughout
its lifetime. Unit names belong in the returned diagnostic snapshot or UI.
Use `AddDatum(value)` for each available observation. Missing/failed readings,
`NaN` and infinity must not become invented zeroes or enter the accumulator;
an actual measured zero is valid.

`SampleCount`, `Minimum`, `Maximum`, `Mean`, `Sum`, `SquaredSum` and
`PopulationStandardDeviation` describe all accepted readings since the last
`Reset()`. Guard empty accumulators: extrema initially equal zero and computed
means/deviations can be `NaN`; neither is an observed measurement. Show the
current value separately, so a missing current reading can remain unavailable
while the summary still describes earlier accepted readings.

These summaries do not provide percentiles, medians or a time-series graph.
Those require a different explicitly authorized sampling/history design.
An arithmetic mean of polls is not time-weighted when poll spacing varies.
Use the population deviation when describing the accumulated observations;
do not silently substitute the existing sample-variance members or invent a
second statistics implementation in the consumer.

## Synchronize a shared accumulator

`Statistics` itself is mutable and unsynchronized. Own it in a service derived
from `SynchronizedObject`, call `AddDatum`/`Reset` in a short write scope and
materialize a consistent summary in a short read scope. Do not expose the
mutable accumulator to callers. Return value snapshots/defensive arrays, using
the repository's collection-valued data-property convention.

Perform collection/IO before entering the scope. Never hold a thread-affine
read/write scope across `await`, a UI dialog or native provider calls. A reset
and all of its metric accumulators must be one write-locked operation. Consumers
that share a sampler should identify samples consistently so presenting one
reading twice does not count it twice.

SystemExpert's [LivePerformanceStatistics](../../../../SystemExpert/src/SystemExpert/Diagnostics/LivePerformanceStatistics.cs)
is an application example: `Statistics(0)` per selected metric, unavailable-reading filters,
shared scopes, UTC sample identity, immutable summary rows and atomic reset.
Its [LiveMetricSnapshot](../../../../SystemExpert/src/SystemExpert/Diagnostics/LiveMetricSnapshot.cs)
rows bind through the existing CrudGrid/snapshot controller and export through
the Data library's CSV facilities. Metric selection, display names, units and
shared-screen sampling policy belong to the application. The reusable
`Statistics` accumulator and raw Windows counter queries remain in Framework;
the application owns performance sampling intervals and delta calculations.
These example links require the sibling SystemExpert checkout.

## Lifetime and verification

Define the interval explicitly: usually "since application start" or "since
Reset statistics". Do not reset merely because a retained screen hides.
`SampleCount` is a `uint`; very long-lived producers must choose a supported
reset interval before its range is exhausted. Current population variance uses
floating-point denominator multiplication, avoiding the old 32-bit overflow
after 65,535 samples; large-magnitude sums can still lose floating-point
precision, so use meaningful units and report finite computed results.

Test actual distributions, missing readings, empty/reset behavior, duplicate
sample handling, snapshot ownership and a long-running sample count. See
[LivePerformanceStatisticsTests](../../../../SystemExpert/tests/SystemExpert.Tests/LivePerformanceStatisticsTests.cs)
for the application policy and [StatisticsTests](../../../tests/Sphere10.Framework.Tests/Maths/StatisticsTests.cs)
for the reusable accumulator's long-running variance regression.
