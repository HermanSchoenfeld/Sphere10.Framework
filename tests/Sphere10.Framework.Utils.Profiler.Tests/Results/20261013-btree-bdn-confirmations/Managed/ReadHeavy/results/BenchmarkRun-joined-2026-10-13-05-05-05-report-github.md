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
| Type                          | Method          | Profile              | Mean      | Error     | StdDev    | Allocated |
|------------------------------ |---------------- |--------------------- |----------:|----------:|----------:|----------:|
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Manag(...)ol-64 [42]** |  **3.421 ms** | **0.6734 ms** | **0.7755 ms** |    **2096 B** |
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Manag(...)r-128 [46]** |  **2.745 ms** | **0.0559 ms** | **0.0643 ms** |    **2072 B** |
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Manag(...)ol-64 [46]** |  **8.208 ms** | **0.8046 ms** | **0.9266 ms** |    **4704 B** |
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Manag(...)-2048 [51]** |  **6.277 ms** | **2.6986 ms** | **3.1077 ms** |         **-** |
| **BTreeLookupMutationBenchmarks** | **MixedAndPersist** | **Manag(...)ol-64 [43]** | **59.201 ms** | **3.0824 ms** | **3.5497 ms** |   **34104 B** |
| **BTreeLookupMutationBenchmarks** | **MixedAndPersist** | **Manag(...)-2048 [48]** | **17.538 ms** | **0.1915 ms** | **0.2205 ms** |   **32920 B** |
