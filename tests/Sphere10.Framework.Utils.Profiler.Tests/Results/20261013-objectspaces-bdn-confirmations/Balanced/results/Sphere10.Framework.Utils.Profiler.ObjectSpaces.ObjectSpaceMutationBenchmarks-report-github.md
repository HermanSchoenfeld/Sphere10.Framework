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
| **MixedReadWriteAndPersist** | **file-baseline-64**     | **497.2 ms** | **17.71 ms** | **20.40 ms** | **162000.0000** | **1617.42 MB** |
| **MixedReadWriteAndPersist** | **file-(...)idate [23]** | **619.4 ms** | **46.42 ms** | **53.46 ms** | **186000.0000** | **1864.31 MB** |
| **MixedReadWriteAndPersist** | **file-order-only**      | **424.3 ms** | **25.01 ms** | **28.80 ms** | **137000.0000** | **1372.33 MB** |
| **MixedReadWriteAndPersist** | **memory-baseline-64**   | **229.8 ms** | **19.70 ms** | **22.68 ms** |  **95000.0000** |  **950.59 MB** |
| **MixedReadWriteAndPersist** | **memor(...)idate [25]** | **175.7 ms** | **10.94 ms** | **12.60 ms** |  **52000.0000** |  **528.69 MB** |
| **MixedReadWriteAndPersist** | **memory-order-only**    | **212.3 ms** | **37.46 ms** | **43.14 ms** |  **81000.0000** |  **810.15 MB** |
