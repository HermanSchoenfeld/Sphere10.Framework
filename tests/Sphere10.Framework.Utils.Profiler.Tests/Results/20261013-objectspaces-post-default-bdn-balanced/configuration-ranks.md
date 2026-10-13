# ObjectSpace configuration comparison

Descriptive rankings within each workload and job. Compare error bars in the BenchmarkDotNet reports before treating nearby means as different. A fastest profile for one operation is not a general recommendation.

## MixedReadWriteAndPersist (File)

Job: Job-YYAKRU(OutlierMode=DontRemove, PowerPlanMode=00000000-0000-0000-0000-000000000000, Force=False, InvocationCount=1, IterationCount=10, LaunchCount=2, RunStrategy=Monitoring, UnrollFactor=1, WarmupCount=5)

Comparison guarantees: `File/PayloadOnly/Full/checks-True/no-merkle`

| Rank | Profile | Mean (ms/batch) | Standard error (ms) | Items/s | Storage bytes |
|---:|---|---:|---:|---:|---:|
| 1 | file-selected-defaults-169 | 427.762 | 5.082 | 467.5 |  |
| 2 | file-historical-baseline-64 | 498.279 | 6.875 | 401.4 |  |

## MixedReadWriteAndPersist (Memory)

Job: Job-YYAKRU(OutlierMode=DontRemove, PowerPlanMode=00000000-0000-0000-0000-000000000000, Force=False, InvocationCount=1, IterationCount=10, LaunchCount=2, RunStrategy=Monitoring, UnrollFactor=1, WarmupCount=5)

Comparison guarantees: `Memory/PayloadOnly/Full/checks-True/no-merkle`

| Rank | Profile | Mean (ms/batch) | Standard error (ms) | Items/s | Storage bytes |
|---:|---|---:|---:|---:|---:|
| 1 | memory-selected-defaults-169 | 180.966 | 4.515 | 1105.2 |  |
| 2 | memory-historical-baseline-64 | 233.067 | 6.187 | 858.1 |  |

