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
| Method          | Profile              | Mean      | Error     | StdDev    | Gen0        | Gen1       | Gen2       | Allocated  |
|---------------- |--------------------- |----------:|----------:|----------:|------------:|-----------:|-----------:|-----------:|
| **MixedAndPersist** | **FileP(...)ol-64 [43]** | **471.25 ms** |  **8.008 ms** |  **9.222 ms** | **194000.0000** |  **2000.0000** |          **-** | **1942.14 MB** |
| **MixedAndPersist** | **FileP(...)-2048 [48]** |  **74.73 ms** |  **6.427 ms** |  **7.401 ms** |  **34000.0000** |  **2000.0000** |          **-** |  **347.15 MB** |
| **MixedAndPersist** | **FileS(...)ol-64 [40]** |  **24.38 ms** |  **5.870 ms** |  **6.760 ms** |   **2000.0000** |          **-** |          **-** |   **24.58 MB** |
| **MixedAndPersist** | **FileS(...)r-128 [44]** |  **19.94 ms** |  **6.517 ms** |  **7.505 ms** |   **2000.0000** |          **-** |          **-** |   **25.27 MB** |
| **MixedAndPersist** | **FileS(...)-4096 [58]** |  **64.94 ms** |  **5.455 ms** |  **6.282 ms** |  **15000.0000** | **15000.0000** | **15000.0000** |   **71.83 MB** |
| **MixedAndPersist** | **FileS(...)ol-64 [44]** | **526.21 ms** | **10.998 ms** | **12.665 ms** |  **15000.0000** |          **-** |          **-** |  **154.04 MB** |
| **MixedAndPersist** | **FileS(...)-2048 [49]** |  **49.70 ms** |  **5.134 ms** |  **5.912 ms** |   **5000.0000** |          **-** |          **-** |   **50.98 MB** |
| **MixedAndPersist** | **OSMem(...)ol-64 [48]** |  **39.16 ms** |  **5.320 ms** |  **6.127 ms** |  **15000.0000** |          **-** |          **-** |  **154.04 MB** |
| **MixedAndPersist** | **OSMem(...)-2048 [53]** |  **11.94 ms** |  **5.155 ms** |  **5.937 ms** |   **5000.0000** |          **-** |          **-** |   **50.98 MB** |
| **MixedAndPersist** | **Trans(...)ol-64 [51]** | **470.84 ms** |  **7.211 ms** |  **8.304 ms** | **194000.0000** |  **2000.0000** |          **-** | **1942.19 MB** |
| **MixedAndPersist** | **Trans(...)-2048 [56]** |  **75.19 ms** |  **6.626 ms** |  **7.631 ms** |  **34000.0000** |  **2000.0000** |          **-** |   **347.2 MB** |
