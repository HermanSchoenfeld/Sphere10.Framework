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
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Manag(...)ol-64 [42]** |   **5.416 ms** |  **0.7358 ms** |  **0.8473 ms** |   **5.017 ms** |   **23872 B** |
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Manag(...)r-128 [46]** |   **5.371 ms** |  **0.7025 ms** |  **0.8090 ms** |   **4.838 ms** |   **16896 B** |
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Manag(...)ol-64 [46]** |  **49.743 ms** |  **4.7253 ms** |  **5.4417 ms** |  **50.957 ms** |    **4704 B** |
| **BTreeMutationBenchmarks**       | **MixedAndPersist** | **Manag(...)-2048 [51]** |   **8.739 ms** |  **0.0796 ms** |  **0.0917 ms** |   **8.731 ms** |         **-** |
| **BTreeLookupMutationBenchmarks** | **MixedAndPersist** | **Manag(...)ol-64 [43]** | **553.271 ms** | **28.3771 ms** | **32.6791 ms** | **542.744 ms** |  **131712 B** |
| **BTreeLookupMutationBenchmarks** | **MixedAndPersist** | **Manag(...)-2048 [48]** | **107.995 ms** |  **0.7989 ms** |  **0.9200 ms** | **108.127 ms** |   **65840 B** |
