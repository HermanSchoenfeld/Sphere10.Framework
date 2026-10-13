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
| Method                   | Profile              | Mean     | Error    | StdDev   | Gen0        | Allocated |
|------------------------- |--------------------- |---------:|---------:|---------:|------------:|----------:|
| **MixedReadWriteAndPersist** | **file-baseline-64**     | **815.7 ms** | **35.59 ms** | **40.98 ms** | **284000.0000** |   **2.77 GB** |
| **MixedReadWriteAndPersist** | **file-(...)idate [23]** | **728.2 ms** | **33.45 ms** | **38.53 ms** | **257000.0000** |   **2.51 GB** |
| **MixedReadWriteAndPersist** | **file-order-only**      | **680.4 ms** | **54.38 ms** | **62.62 ms** | **238000.0000** |   **2.32 GB** |
| **MixedReadWriteAndPersist** | **memory-baseline-64**   | **371.2 ms** | **41.37 ms** | **47.64 ms** | **166000.0000** |   **1.63 GB** |
| **MixedReadWriteAndPersist** | **memor(...)idate [25]** | **553.3 ms** | **38.35 ms** | **44.17 ms** | **141000.0000** |   **1.38 GB** |
| **MixedReadWriteAndPersist** | **memory-order-only**    | **302.6 ms** | **15.84 ms** | **18.24 ms** | **140000.0000** |   **1.37 GB** |
