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
| Method          | Profile              | Mean      | Error     | StdDev    | Gen0        | Allocated  |
|---------------- |--------------------- |----------:|----------:|----------:|------------:|-----------:|
| **MixedAndPersist** | **Strea(...)ol-64 [42]** |  **40.79 ms** |  **1.912 ms** |  **2.202 ms** |  **23000.0000** |  **235.98 MB** |
| **MixedAndPersist** | **Strea(...)r-128 [46]** |  **40.81 ms** |  **0.477 ms** |  **0.549 ms** |  **24000.0000** |  **246.92 MB** |
| **MixedAndPersist** | **Strea(...)ol-64 [46]** | **389.55 ms** | **19.614 ms** | **22.588 ms** | **215000.0000** | **2144.97 MB** |
| **MixedAndPersist** | **Strea(...)-2048 [51]** |  **82.78 ms** |  **1.664 ms** |  **1.917 ms** |  **62000.0000** |  **625.58 MB** |
