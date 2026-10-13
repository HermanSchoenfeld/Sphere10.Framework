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
| Method          | Profile              | Mean       | Error    | StdDev   | Gen0        | Gen1       | Allocated |
|---------------- |--------------------- |-----------:|---------:|---------:|------------:|-----------:|----------:|
| **MixedAndPersist** | **FileP(...)-1024 [42]** |   **988.4 ms** | **14.98 ms** | **17.25 ms** | **459000.0000** | **10000.0000** |   **4.48 GB** |
| **MixedAndPersist** | **FileP(...)-2048 [42]** |   **850.4 ms** | **43.09 ms** | **49.62 ms** | **420000.0000** | **21000.0000** |    **4.1 GB** |
| **MixedAndPersist** | **Trans(...)-1024 [50]** | **1,003.0 ms** | **21.97 ms** | **25.30 ms** | **459000.0000** | **10000.0000** |   **4.48 GB** |
| **MixedAndPersist** | **Trans(...)-2048 [50]** |   **830.6 ms** | **11.34 ms** | **13.06 ms** | **420000.0000** | **21000.0000** |    **4.1 GB** |
