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
| Type                          | Method          | Profile              | Mean       | Error     | StdDev    | Gen0       | Gen1       | Gen2       | Allocated |
|------------------------------ |---------------- |--------------------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|----------:|
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **FileP(...)-1024 [47]** |   **9.073 ms** |  **1.124 ms** |  **1.294 ms** |  **3000.0000** |          **-** |          **-** |  **30.43 MB** |
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **FileP(...)-2048 [47]** |  **30.506 ms** | **12.753 ms** | **14.686 ms** |  **5000.0000** |  **4000.0000** |  **4000.0000** |  **35.33 MB** |
| **BTreeLookupMutationBenchmarks** | **MixedAndPersist** | **FileP(...)-1024 [44]** |  **77.824 ms** |  **1.460 ms** |  **1.682 ms** | **38000.0000** |          **-** |          **-** | **382.92 MB** |
| **BTreeLookupMutationBenchmarks** | **MixedAndPersist** | **FileP(...)-2048 [44]** | **234.933 ms** | **35.873 ms** | **41.312 ms** | **69000.0000** | **40000.0000** | **40000.0000** | **440.08 MB** |
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Trans(...)-1024 [55]** |  **11.431 ms** |  **1.289 ms** |  **1.485 ms** |  **3000.0000** |          **-** |          **-** |  **30.45 MB** |
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Trans(...)-2048 [55]** |  **25.303 ms** |  **9.492 ms** | **10.931 ms** |  **5000.0000** |  **4000.0000** |  **4000.0000** |  **35.37 MB** |
| **BTreeLookupMutationBenchmarks** | **MixedAndPersist** | **Trans(...)-1024 [52]** |  **82.932 ms** |  **2.897 ms** |  **3.336 ms** | **38000.0000** |          **-** |          **-** | **382.97 MB** |
| **BTreeLookupMutationBenchmarks** | **MixedAndPersist** | **Trans(...)-2048 [52]** | **241.139 ms** | **26.231 ms** | **30.208 ms** | **69000.0000** | **40000.0000** | **40000.0000** | **440.13 MB** |
