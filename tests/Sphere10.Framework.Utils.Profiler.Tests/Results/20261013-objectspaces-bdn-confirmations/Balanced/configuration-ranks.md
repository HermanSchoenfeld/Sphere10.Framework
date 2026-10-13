# ObjectSpace configuration comparison

Descriptive rankings within each workload and job. Compare error bars in the BenchmarkDotNet reports before treating nearby means as different. A fastest profile for one operation is not a general recommendation.

## MixedReadWriteAndPersist (File)

Job: Job-YYAKRU(OutlierMode=DontRemove, PowerPlanMode=00000000-0000-0000-0000-000000000000, Force=False, InvocationCount=1, IterationCount=10, LaunchCount=2, RunStrategy=Monitoring, UnrollFactor=1, WarmupCount=5)

Comparison guarantees: `File/PayloadOnly/Full/checks-True/no-merkle`

| Rank | Profile | Mean (ms/batch) | Standard error (ms) | Items/s | Storage bytes |
|---:|---|---:|---:|---:|---:|
| 1 | file-order-only | 424.260 | 6.440 | 471.4 | 456799 |
| 2 | file-baseline-64 | 497.226 | 4.561 | 402.2 | 448573 |
| 3 | file-combined-candidate | 619.372 | 11.954 | 322.9 | 1248415 |

## MixedReadWriteAndPersist (Memory)

Job: Job-YYAKRU(OutlierMode=DontRemove, PowerPlanMode=00000000-0000-0000-0000-000000000000, Force=False, InvocationCount=1, IterationCount=10, LaunchCount=2, RunStrategy=Monitoring, UnrollFactor=1, WarmupCount=5)

Comparison guarantees: `Memory/PayloadOnly/Full/checks-True/no-merkle`

| Rank | Profile | Mean (ms/batch) | Standard error (ms) | Items/s | Storage bytes |
|---:|---|---:|---:|---:|---:|
| 1 | memory-combined-candidate | 175.666 | 2.818 | 1138.5 | 1248415 |
| 2 | memory-order-only | 212.343 | 9.647 | 941.9 | 456799 |
| 3 | memory-baseline-64 | 229.757 | 5.072 | 870.5 | 448573 |

