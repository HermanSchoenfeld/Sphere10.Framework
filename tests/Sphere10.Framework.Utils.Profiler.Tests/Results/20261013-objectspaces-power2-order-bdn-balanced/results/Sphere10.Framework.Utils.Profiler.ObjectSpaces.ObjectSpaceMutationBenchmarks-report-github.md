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
| Method                   | Profile              | Mean     | Error    | StdDev   | Gen0        | Allocated  |
|------------------------- |--------------------- |---------:|---------:|---------:|------------:|-----------:|
| **MixedReadWriteAndPersist** | **file-(...)r-128 [29]** | **475.9 ms** | **20.95 ms** | **24.13 ms** | **165000.0000** | **1650.01 MB** |
| **MixedReadWriteAndPersist** | **file-(...)r-256 [29]** | **505.1 ms** | **36.78 ms** | **42.36 ms** | **161000.0000** | **1607.93 MB** |
| **MixedReadWriteAndPersist** | **memor(...)r-128 [31]** | **219.0 ms** | **20.57 ms** | **23.68 ms** |  **61000.0000** |   **615.9 MB** |
| **MixedReadWriteAndPersist** | **memor(...)r-256 [31]** | **181.0 ms** |  **7.67 ms** |  **8.84 ms** |  **55000.0000** |   **556.2 MB** |
