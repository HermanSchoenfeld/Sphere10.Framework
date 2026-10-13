# ObjectSpace configuration comparison

Descriptive rankings within each workload and job. Compare error bars in the BenchmarkDotNet reports before treating nearby means as different. A fastest profile for one operation is not a general recommendation.

## MixedReadWriteAndPersist (File)

Job: Job-YYAKRU(OutlierMode=DontRemove, PowerPlanMode=00000000-0000-0000-0000-000000000000, Force=False, InvocationCount=1, IterationCount=10, LaunchCount=2, RunStrategy=Monitoring, UnrollFactor=1, WarmupCount=5)

Comparison guarantees: `File/PayloadOnly/Full/checks-True/no-merkle`

| Rank | Profile | Mean (ms/batch) | Standard error (ms) | Items/s | Storage bytes |
|---:|---|---:|---:|---:|---:|
| 1 | file-order-only | 121.854 | 1.844 | 1641.3 | 456799 |
| 2 | file-combined-candidate | 142.444 | 1.661 | 1404.1 | 1248415 |
| 3 | file-baseline-64 | 143.118 | 2.045 | 1397.4 | 448573 |

## MixedReadWriteAndPersist (Memory)

Job: Job-YYAKRU(OutlierMode=DontRemove, PowerPlanMode=00000000-0000-0000-0000-000000000000, Force=False, InvocationCount=1, IterationCount=10, LaunchCount=2, RunStrategy=Monitoring, UnrollFactor=1, WarmupCount=5)

Comparison guarantees: `Memory/PayloadOnly/Full/checks-True/no-merkle`

| Rank | Profile | Mean (ms/batch) | Standard error (ms) | Items/s | Storage bytes |
|---:|---|---:|---:|---:|---:|
| 1 | memory-combined-candidate | 59.487 | 0.861 | 3362.1 | 1248415 |
| 2 | memory-order-only | 67.236 | 2.705 | 2974.6 | 456799 |
| 3 | memory-baseline-64 | 77.938 | 1.832 | 2566.2 | 448573 |

