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
| Method          | Profile              | Mean      | Error    | StdDev   | Gen0        | Allocated |
|---------------- |--------------------- |----------:|---------:|---------:|------------:|----------:|
| **MixedAndPersist** | **Strea(...)ol-64 [42]** |  **35.02 ms** | **1.762 ms** | **2.030 ms** |  **19000.0000** | **197.15 MB** |
| **MixedAndPersist** | **Strea(...)r-128 [46]** |  **34.49 ms** | **0.677 ms** | **0.780 ms** |  **20000.0000** | **202.95 MB** |
| **MixedAndPersist** | **Strea(...)ol-64 [46]** | **220.45 ms** | **1.420 ms** | **1.636 ms** | **124000.0000** | **1239.5 MB** |
| **MixedAndPersist** | **Strea(...)-2048 [51]** |  **58.65 ms** | **2.444 ms** | **2.815 ms** |  **40000.0000** | **407.12 MB** |
