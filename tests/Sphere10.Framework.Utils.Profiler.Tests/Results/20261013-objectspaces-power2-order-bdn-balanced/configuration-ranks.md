# ObjectSpace configuration comparison

Descriptive rankings within each workload and job. Compare error bars in the BenchmarkDotNet reports before treating nearby means as different. A fastest profile for one operation is not a general recommendation.

## MixedReadWriteAndPersist (File)

Job: Job-YYAKRU(OutlierMode=DontRemove, PowerPlanMode=00000000-0000-0000-0000-000000000000, Force=False, InvocationCount=1, IterationCount=10, LaunchCount=2, RunStrategy=Monitoring, UnrollFactor=1, WarmupCount=5)

Comparison guarantees: `File/PayloadOnly/Full/checks-True/no-merkle`

| Rank | Profile | Mean (ms/batch) | Standard error (ms) | Items/s | Storage bytes |
|---:|---|---:|---:|---:|---:|
| 1 | file-default-layout-order-128 | 475.938 | 5.395 | 420.2 |  |
| 2 | file-default-layout-order-256 | 505.062 | 9.472 | 396.0 |  |

## MixedReadWriteAndPersist (Memory)

Job: Job-YYAKRU(OutlierMode=DontRemove, PowerPlanMode=00000000-0000-0000-0000-000000000000, Force=False, InvocationCount=1, IterationCount=10, LaunchCount=2, RunStrategy=Monitoring, UnrollFactor=1, WarmupCount=5)

Comparison guarantees: `Memory/PayloadOnly/Full/checks-True/no-merkle`

| Rank | Profile | Mean (ms/batch) | Standard error (ms) | Items/s | Storage bytes |
|---:|---|---:|---:|---:|---:|
| 1 | memory-default-layout-order-256 | 181.012 | 1.976 | 1104.9 |  |
| 2 | memory-default-layout-order-128 | 218.997 | 5.296 | 913.3 |  |

