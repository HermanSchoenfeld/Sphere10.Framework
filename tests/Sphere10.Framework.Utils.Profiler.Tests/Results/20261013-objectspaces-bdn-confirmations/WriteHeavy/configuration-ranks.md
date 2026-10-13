# ObjectSpace configuration comparison

Descriptive rankings within each workload and job. Compare error bars in the BenchmarkDotNet reports before treating nearby means as different. A fastest profile for one operation is not a general recommendation.

## MixedReadWriteAndPersist (File)

Job: Job-YYAKRU(OutlierMode=DontRemove, PowerPlanMode=00000000-0000-0000-0000-000000000000, Force=False, InvocationCount=1, IterationCount=10, LaunchCount=2, RunStrategy=Monitoring, UnrollFactor=1, WarmupCount=5)

Comparison guarantees: `File/PayloadOnly/Full/checks-True/no-merkle`

| Rank | Profile | Mean (ms/batch) | Standard error (ms) | Items/s | Storage bytes |
|---:|---|---:|---:|---:|---:|
| 1 | file-order-only | 680.380 | 14.003 | 294.0 | 456799 |
| 2 | file-combined-candidate | 728.220 | 8.615 | 274.6 | 1248415 |
| 3 | file-baseline-64 | 815.678 | 9.164 | 245.2 | 448573 |

## MixedReadWriteAndPersist (Memory)

Job: Job-YYAKRU(OutlierMode=DontRemove, PowerPlanMode=00000000-0000-0000-0000-000000000000, Force=False, InvocationCount=1, IterationCount=10, LaunchCount=2, RunStrategy=Monitoring, UnrollFactor=1, WarmupCount=5)

Comparison guarantees: `Memory/PayloadOnly/Full/checks-True/no-merkle`

| Rank | Profile | Mean (ms/batch) | Standard error (ms) | Items/s | Storage bytes |
|---:|---|---:|---:|---:|---:|
| 1 | memory-order-only | 302.560 | 4.079 | 661.0 | 456799 |
| 2 | memory-baseline-64 | 371.174 | 10.652 | 538.8 | 448573 |
| 3 | memory-combined-candidate | 553.290 | 9.877 | 361.5 | 1308745 |

