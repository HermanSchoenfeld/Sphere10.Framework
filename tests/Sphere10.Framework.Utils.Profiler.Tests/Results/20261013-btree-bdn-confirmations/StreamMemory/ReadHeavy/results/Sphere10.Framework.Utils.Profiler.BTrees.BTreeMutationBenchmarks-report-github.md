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
| Method          | Profile              | Mean     | Error    | StdDev   | Gen0       | Allocated |
|---------------- |--------------------- |---------:|---------:|---------:|-----------:|----------:|
| **MixedAndPersist** | **Strea(...)ol-64 [42]** | **27.70 ms** | **1.125 ms** | **1.295 ms** | **15000.0000** | **158.12 MB** |
| **MixedAndPersist** | **Strea(...)r-128 [46]** | **27.26 ms** | **0.637 ms** | **0.733 ms** | **15000.0000** | **159.01 MB** |
| **MixedAndPersist** | **Strea(...)ol-64 [46]** | **60.50 ms** | **1.970 ms** | **2.269 ms** | **33000.0000** | **333.41 MB** |
| **MixedAndPersist** | **Strea(...)-2048 [51]** | **30.04 ms** | **1.335 ms** | **1.537 ms** | **18000.0000** | **188.62 MB** |
