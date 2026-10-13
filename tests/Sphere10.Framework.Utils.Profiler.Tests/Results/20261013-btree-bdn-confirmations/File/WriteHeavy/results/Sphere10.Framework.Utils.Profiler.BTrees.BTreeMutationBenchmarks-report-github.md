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
| **MixedAndPersist** | **FileP(...)ol-64 [43]** | **815.42 ms** |  **9.191 ms** | **10.584 ms** | **339000.0000** |  **4000.0000** |          **-** | **3385.19 MB** |
| **MixedAndPersist** | **FileP(...)-2048 [48]** |  **99.46 ms** |  **7.201 ms** |  **8.293 ms** |  **50000.0000** |  **4000.0000** |          **-** |  **505.18 MB** |
| **MixedAndPersist** | **FileS(...)ol-64 [40]** |  **32.53 ms** |  **5.478 ms** |  **6.309 ms** |   **2000.0000** |          **-** |          **-** |   **29.45 MB** |
| **MixedAndPersist** | **FileS(...)r-128 [44]** |  **27.10 ms** |  **6.257 ms** |  **7.206 ms** |   **3000.0000** |          **-** |          **-** |   **30.73 MB** |
| **MixedAndPersist** | **FileS(...)-4096 [58]** | **101.30 ms** |  **6.435 ms** |  **7.411 ms** |  **28000.0000** | **28000.0000** | **28000.0000** |   **115.3 MB** |
| **MixedAndPersist** | **FileS(...)ol-64 [44]** | **930.72 ms** | **19.880 ms** | **22.894 ms** |  **26000.0000** |          **-** |          **-** |  **266.52 MB** |
| **MixedAndPersist** | **FileS(...)-2048 [49]** |  **76.44 ms** |  **5.322 ms** |  **6.129 ms** |   **7000.0000** |          **-** |          **-** |   **78.29 MB** |
| **MixedAndPersist** | **OSMem(...)ol-64 [48]** |  **64.22 ms** |  **4.266 ms** |  **4.912 ms** |  **26000.0000** |          **-** |          **-** |  **266.52 MB** |
| **MixedAndPersist** | **OSMem(...)-2048 [53]** |  **16.07 ms** |  **4.378 ms** |  **5.041 ms** |   **7000.0000** |          **-** |          **-** |   **78.29 MB** |
| **MixedAndPersist** | **Trans(...)ol-64 [51]** | **812.72 ms** | **15.872 ms** | **18.278 ms** | **339000.0000** |  **4000.0000** |          **-** | **3385.24 MB** |
| **MixedAndPersist** | **Trans(...)-2048 [56]** | **102.89 ms** |  **6.260 ms** |  **7.209 ms** |  **50000.0000** |  **4000.0000** |          **-** |  **505.23 MB** |
