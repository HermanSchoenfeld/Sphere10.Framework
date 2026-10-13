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
| Type                          | Method          | Profile              | Mean       | Error      | StdDev     | Median     | Allocated |
|------------------------------ |---------------- |--------------------- |-----------:|-----------:|-----------:|-----------:|----------:|
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Manag(...)ol-64 [42]** |   **4.635 ms** |  **0.8115 ms** |  **0.9345 ms** |   **4.441 ms** |   **13896 B** |
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Manag(...)r-128 [46]** |   **4.990 ms** |  **2.9150 ms** |  **3.3570 ms** |   **3.908 ms** |   **12752 B** |
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Manag(...)ol-64 [46]** |  **30.132 ms** |  **3.3141 ms** |  **3.8165 ms** |  **29.862 ms** |    **4704 B** |
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Manag(...)-2048 [51]** |  **16.102 ms** |  **5.8055 ms** |  **6.6856 ms** |  **19.913 ms** |         **-** |
| **BTreeLookupMutationBenchmarks** | **MixedAndPersist** | **Manag(...)ol-64 [43]** | **295.865 ms** | **19.6124 ms** | **22.5857 ms** | **299.159 ms** |   **72912 B** |
| **BTreeLookupMutationBenchmarks** | **MixedAndPersist** | **Manag(...)-2048 [48]** |  **59.781 ms** |  **0.3310 ms** |  **0.3812 ms** |  **59.720 ms** |   **32920 B** |
