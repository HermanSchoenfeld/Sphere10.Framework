```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.7725/22H2/2022Update)
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YYAKRU : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

OutlierMode=DontRemove  PowerPlanMode=00000000-0000-0000-0000-000000000000  Force=False  
InvocationCount=1  IterationCount=10  LaunchCount=2  
RunStrategy=Monitoring  UnrollFactor=1  WarmupCount=5  

```
| Method                   | Profile              | Mean      | Error     | StdDev    | Gen0       | Allocated |
|------------------------- |--------------------- |----------:|----------:|----------:|-----------:|----------:|
| **MixedReadWriteAndPersist** | **file-baseline-64**     | **143.12 ms** |  **7.942 ms** |  **9.147 ms** | **38000.0000** | **382.35 MB** |
| **MixedReadWriteAndPersist** | **file-(...)idate [23]** | **142.44 ms** |  **6.449 ms** |  **7.427 ms** | **35000.0000** | **354.27 MB** |
| **MixedReadWriteAndPersist** | **file-order-only**      | **121.85 ms** |  **7.162 ms** |  **8.248 ms** | **30000.0000** | **307.09 MB** |
| **MixedReadWriteAndPersist** | **memory-baseline-64**   |  **77.94 ms** |  **7.112 ms** |  **8.191 ms** | **22000.0000** | **225.77 MB** |
| **MixedReadWriteAndPersist** | **memor(...)idate [25]** |  **59.49 ms** |  **3.344 ms** |  **3.851 ms** | **12000.0000** | **126.02 MB** |
| **MixedReadWriteAndPersist** | **memory-order-only**    |  **67.24 ms** | **10.504 ms** | **12.096 ms** | **18000.0000** |  **180.3 MB** |
