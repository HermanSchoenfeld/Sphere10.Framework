# Cache performance results

Raw campaign captures linked below are ignored local output. Those links resolve on machines retaining the matching capture; fresh clones include this summary, and cache evidence tests skip until captures are supplied locally.

Canonical typed cache with general projections and the explicit ObjectCacheProjection specialization measured on 13 October 2026: 19 cases, 38 successful independent worker processes and 380 retained actual samples. Every case uses two launches, five warmups and ten actual iterations per launch, Monitoring, one invocation, retained outliers and no forced GC. The power plan remained unchanged. Each phase retains exact settings, original Full JSON/CSV/console output, process exits and SHA256/MVID/version fingerprints of the actual generated workers.

Capacity is 1,024 size-estimator units. Fetches return preallocated reference values with zero simulated fetch work; these measure single-threaded cache mechanics rather than file/network latency. Hot access batches contain 65,536 hits; pressure batches contain 8,192 accesses. The mixed hot-set workload uses 80% hot accesses over 10% of preseed items. Weighted values cost 1–4 units. Allocation is cumulative per batch, not peak memory.

In Visual Studio, run `RecordedBenchmarkDotNetConfirmations` for timing/interval/raw-sample output and `RecordedCacheMetricsTests` for independently verified fetches, victims and capacity accounting. The projection-bdn phases below measure the current typed core and explicitly named object adapter. Earlier implicit-object measurements remain separately retained; measurements from different source versions are not pooled.

## 20261013-caches-access-projection-bdn

[Exact settings](20261013-caches-access-projection-bdn/run.json), [execution validation](20261013-caches-access-projection-bdn/execution-validation.json), [worker identities](20261013-caches-access-projection-bdn/worker-binary-fingerprints.json).

| Workload | Exact profile | Mean ms/batch | BDN 99.9% interval ms | Allocated MiB/batch | Fetches | Reaps | Victims | Final units |
| --- | --- | ---: | --- | ---: | ---: | ---: | ---: | ---: |
| TypedHotKeys | lru-quarter-unit | 9.495 | [9.257, 9.732] | 6.000 | 0 | 0 | 0 | 1024 |
| ObjectHotKeys | lru-quarter-unit | 15.651 | [12.375, 18.927] | 7.514 | 0 | 0 | 0 | 1024 |
| ObjectPreboxedHotKeys | lru-quarter-unit | 11.232 | [10.722, 11.743] | 6.014 | 0 | 0 | 0 | 1024 |

## 20261013-caches-unit-pressure-projection-bdn

[Exact settings](20261013-caches-unit-pressure-projection-bdn/run.json), [execution validation](20261013-caches-unit-pressure-projection-bdn/execution-validation.json), [worker identities](20261013-caches-unit-pressure-projection-bdn/worker-binary-fingerprints.json).

| Workload | Exact profile | Mean ms/batch | BDN 99.9% interval ms | Allocated MiB/batch | Fetches | Reaps | Victims | Final units |
| --- | --- | ---: | --- | ---: | ---: | ---: | ---: | ---: |
| SequentialPressure | fifo-minimal-unit | 736.763 | [692.529, 780.996] | 133.375 | 8192 | 8192 | 8192 | 1024 |
| HotSetPressure | fifo-minimal-unit | 154.587 | [146.196, 162.979] | 30.587 | 1843 | 1843 | 1843 | 1024 |
| SequentialPressure | fifo-quarter-unit | 18.709 | [16.378, 21.041] | 4.601 | 8192 | 32 | 8192 | 1024 |
| HotSetPressure | fifo-quarter-unit | 5.711 | [4.909, 6.513] | 1.675 | 1843 | 8 | 2048 | 819 |
| SequentialPressure | lru-minimal-unit | 695.579 | [681.476, 709.682] | 133.375 | 8192 | 8192 | 8192 | 1024 |
| HotSetPressure | lru-minimal-unit | 147.124 | [144.192, 150.056] | 28.094 | 1689 | 1689 | 1689 | 1024 |
| SequentialPressure | lru-quarter-unit | 18.862 | [17.003, 20.721] | 4.601 | 8192 | 32 | 8192 | 1024 |
| HotSetPressure | lru-quarter-unit | 5.338 | [4.629, 6.047] | 1.582 | 1736 | 7 | 1792 | 968 |

## 20261013-caches-weighted-pressure-projection-bdn

[Exact settings](20261013-caches-weighted-pressure-projection-bdn/run.json), [execution validation](20261013-caches-weighted-pressure-projection-bdn/execution-validation.json), [worker identities](20261013-caches-weighted-pressure-projection-bdn/worker-binary-fingerprints.json).

| Workload | Exact profile | Mean ms/batch | BDN 99.9% interval ms | Allocated MiB/batch | Fetches | Reaps | Victims | Final units |
| --- | --- | ---: | --- | ---: | ---: | ---: | ---: | ---: |
| SequentialPressure | fifo-minimal-weighted | 217.384 | [207.644, 227.124] | 42.296 | 8192 | 6042 | 8194 | 1023 |
| HotSetPressure | fifo-minimal-weighted | 43.383 | [36.314, 50.452] | 8.841 | 1843 | 1167 | 1843 | 1024 |
| SequentialPressure | fifo-quarter-weighted | 17.796 | [15.797, 19.795] | 4.656 | 8192 | 80 | 8215 | 970 |
| HotSetPressure | fifo-quarter-weighted | 5.102 | [4.430, 5.774] | 1.630 | 1844 | 18 | 1854 | 1001 |
| SequentialPressure | lru-minimal-weighted | 206.645 | [199.261, 214.028] | 42.296 | 8192 | 6042 | 8194 | 1023 |
| HotSetPressure | lru-minimal-weighted | 52.820 | [35.106, 70.533] | 7.182 | 1656 | 922 | 1656 | 1024 |
| SequentialPressure | lru-quarter-weighted | 17.639 | [15.946, 19.332] | 4.656 | 8192 | 80 | 8215 | 970 |
| HotSetPressure | lru-quarter-weighted | 5.038 | [3.999, 6.077] | 1.569 | 1675 | 17 | 1746 | 848 |

## Interpretation

Typed hits use the canonical typed contract. Object access includes the explicit adapter, stable entry-view lookup and first-view creation; its per-access integer-key boxing adds allocations. Preboxing excludes caller key boxing but retains adapter costs. The table reports actual allocations for each batch. Synchronization scopes still allocate, so typed access is not allocation-free.

Quarter-capacity reaping strongly reduces eviction-selection cost in these workloads. In unit-sized sequential LRU pressure, it performs 32 reaps instead of 8,192 while fetching the same 8,192 unique keys. Review the hot-set fetch counts to see policy-specific refills; reducing reap frequency does not guarantee the same cache-hit rate.

These compare a minimal one-unit target with 25%, across LRU/FIFO and unit/weighted sizes. They support the configurable 0.25 starting default for the declared mechanics workload, not a universal optimum. Half-capacity profiles, additional fetch costs, pools, concurrency, expiration and pinning are separate scenarios; correctness coverage does not imply measured results for them.
