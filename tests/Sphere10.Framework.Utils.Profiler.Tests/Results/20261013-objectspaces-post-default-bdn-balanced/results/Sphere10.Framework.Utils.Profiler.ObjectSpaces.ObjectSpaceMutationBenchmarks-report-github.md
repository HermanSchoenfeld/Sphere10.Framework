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
| **MixedReadWriteAndPersist** | **file-(...)ne-64 [27]** | **498.3 ms** | **26.70 ms** | **30.74 ms** | **162000.0000** | **1617.42 MB** |
| **MixedReadWriteAndPersist** | **file-(...)s-169 [26]** | **427.8 ms** | **19.74 ms** | **22.73 ms** | **137000.0000** | **1372.33 MB** |
| **MixedReadWriteAndPersist** | **memor(...)ne-64 [29]** | **233.1 ms** | **24.02 ms** | **27.67 ms** |  **95000.0000** |  **950.59 MB** |
| **MixedReadWriteAndPersist** | **memor(...)s-169 [28]** | **181.0 ms** | **17.53 ms** | **20.19 ms** |  **52000.0000** |  **528.69 MB** |
