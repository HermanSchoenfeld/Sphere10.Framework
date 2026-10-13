```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.7725/22H2/2022Update)
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-SOEIML : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

OutlierMode=DontRemove  PowerPlanMode=00000000-0000-0000-0000-000000000000  Force=False  
InvocationCount=1  IterationCount=10  LaunchCount=2  
RunStrategy=Monitoring  UnrollFactor=1  WarmupCount=30  

```
| Type                          | Method          | Profile              | Mean      | Error     | StdDev    | Allocated |
|------------------------------ |---------------- |--------------------- |----------:|----------:|----------:|----------:|
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Manag(...)0-128 [38]** |  **3.495 ms** | **0.2250 ms** | **0.2591 ms** |   **12752 B** |
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Manag(...)30-64 [37]** |  **3.544 ms** | **0.1578 ms** | **0.1817 ms** |   **13896 B** |
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Manag(...)-1024 [43]** |  **6.101 ms** | **0.0819 ms** | **0.0943 ms** |         **-** |
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Manag(...)-2048 [43]** |  **6.010 ms** | **0.0779 ms** | **0.0897 ms** |         **-** |
| **BTreeLookupMutationBenchmarks** | **MixedAndPersist** | **Manag(...)-1024 [40]** | **67.899 ms** | **6.0340 ms** | **6.9488 ms** |         **-** |
| **BTreeLookupMutationBenchmarks** | **MixedAndPersist** | **Manag(...)-2048 [40]** | **63.714 ms** | **4.9530 ms** | **5.7038 ms** |   **32920 B** |
