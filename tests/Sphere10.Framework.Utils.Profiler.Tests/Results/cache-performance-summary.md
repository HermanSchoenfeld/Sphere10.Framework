# Cache performance results

Raw campaign captures linked below are ignored local output. Those links resolve on machines retaining the matching capture; fresh clones include this summary, and cache evidence tests skip until captures are supplied locally.

Historical implicit-object implementation measured on 13 October 2026: 19 cases, 38 successful independent worker processes and 380 retained actual samples. These phases predate the explicit general projection and metadata-only reaper contracts; see the [current projection results](cache-projection-performance-summary.md). Every case uses two launches, five warmups and ten actual iterations per launch, Monitoring, one invocation, retained outliers and no forced GC. The power plan remained unchanged. Each phase retains exact settings, original Full JSON/CSV/console output, process exits and SHA256/MVID/version fingerprints of the actual generated workers.

Capacity is 1,024 size-estimator units. Fetches return preallocated reference values with zero simulated fetch work; these measure single-threaded cache mechanics rather than file/network latency. Hot access batches contain 65,536 hits; pressure batches contain 8,192 accesses. The mixed hot-set workload uses 80% hot accesses over 10% of preseed items. Weighted values cost 1–4 units. Allocation is cumulative per batch, not peak memory.

In Visual Studio, run `RecordedBenchmarkDotNetConfirmations` for timing/interval/raw-sample output and `RecordedCacheMetricsTests` for independently verified fetches, victims and capacity accounting. The final phase names below distinguish these from the earlier measurements before timestamp/file-staleness corrections. Earlier evidence remains separately retained; measurements from different phases are not pooled.

## 20261013-caches-access-final-bdn

[Exact settings](20261013-caches-access-final-bdn/run.json), [execution validation](20261013-caches-access-final-bdn/execution-validation.json), [worker identities](20261013-caches-access-final-bdn/worker-binary-fingerprints.json).

| Workload | Exact profile | Mean ms/batch | BDN 99.9% interval ms | Allocated MiB/batch | Fetches | Reaps | Victims | Final units |
| --- | --- | ---: | --- | ---: | ---: | ---: | ---: | ---: |
| TypedHotKeys | lru-quarter-unit | 9.388 | [8.783, 9.993] | 6.000 | 0 | 0 | 0 | 1024 |
| ObjectHotKeys | lru-quarter-unit | 10.199 | [9.015, 11.383] | 7.500 | 0 | 0 | 0 | 1024 |
| ObjectPreboxedHotKeys | lru-quarter-unit | 10.705 | [9.158, 12.251] | 6.000 | 0 | 0 | 0 | 1024 |

## 20261013-caches-unit-pressure-final-bdn

[Exact settings](20261013-caches-unit-pressure-final-bdn/run.json), [execution validation](20261013-caches-unit-pressure-final-bdn/execution-validation.json), [worker identities](20261013-caches-unit-pressure-final-bdn/worker-binary-fingerprints.json).

| Workload | Exact profile | Mean ms/batch | BDN 99.9% interval ms | Allocated MiB/batch | Fetches | Reaps | Victims | Final units |
| --- | --- | ---: | --- | ---: | ---: | ---: | ---: | ---: |
| SequentialPressure | fifo-minimal-unit | 663.301 | [650.002, 676.599] | 133.375 | 8192 | 8192 | 8192 | 1024 |
| HotSetPressure | fifo-minimal-unit | 144.632 | [137.404, 151.860] | 30.587 | 1843 | 1843 | 1843 | 1024 |
| SequentialPressure | fifo-quarter-unit | 19.324 | [15.866, 22.781] | 4.601 | 8192 | 32 | 8192 | 1024 |
| HotSetPressure | fifo-quarter-unit | 5.513 | [4.969, 6.057] | 1.675 | 1843 | 8 | 2048 | 819 |
| SequentialPressure | lru-minimal-unit | 663.860 | [642.165, 685.554] | 133.375 | 8192 | 8192 | 8192 | 1024 |
| HotSetPressure | lru-minimal-unit | 138.754 | [134.048, 143.459] | 28.094 | 1689 | 1689 | 1689 | 1024 |
| SequentialPressure | lru-quarter-unit | 18.061 | [16.957, 19.166] | 4.601 | 8192 | 32 | 8192 | 1024 |
| HotSetPressure | lru-quarter-unit | 5.112 | [4.565, 5.659] | 1.582 | 1736 | 7 | 1792 | 968 |

## 20261013-caches-weighted-pressure-final-bdn

[Exact settings](20261013-caches-weighted-pressure-final-bdn/run.json), [execution validation](20261013-caches-weighted-pressure-final-bdn/execution-validation.json), [worker identities](20261013-caches-weighted-pressure-final-bdn/worker-binary-fingerprints.json).

| Workload | Exact profile | Mean ms/batch | BDN 99.9% interval ms | Allocated MiB/batch | Fetches | Reaps | Victims | Final units |
| --- | --- | ---: | --- | ---: | ---: | ---: | ---: | ---: |
| SequentialPressure | fifo-minimal-weighted | 192.145 | [188.749, 195.540] | 42.296 | 8192 | 6042 | 8194 | 1023 |
| HotSetPressure | fifo-minimal-weighted | 52.917 | [44.232, 61.601] | 8.841 | 1843 | 1167 | 1843 | 1024 |
| SequentialPressure | fifo-quarter-weighted | 16.790 | [15.227, 18.353] | 4.656 | 8192 | 80 | 8215 | 970 |
| HotSetPressure | fifo-quarter-weighted | 4.811 | [4.273, 5.349] | 1.630 | 1844 | 18 | 1854 | 1001 |
| SequentialPressure | lru-minimal-weighted | 189.468 | [187.408, 191.527] | 42.296 | 8192 | 6042 | 8194 | 1023 |
| HotSetPressure | lru-minimal-weighted | 41.769 | [34.562, 48.977] | 7.182 | 1656 | 922 | 1656 | 1024 |
| SequentialPressure | lru-quarter-weighted | 15.977 | [14.441, 17.513] | 4.656 | 8192 | 80 | 8215 | 970 |
| HotSetPressure | lru-quarter-weighted | 4.976 | [4.047, 5.905] | 1.569 | 1675 | 17 | 1746 | 848 |

## Interpretation

Typed hits allocate 6 MiB per 65,536 accesses versus 7.5 MiB with per-access integer-key boxing; preboxed object access also allocates 6 MiB. Synchronization scopes still allocate, so typed access is not allocation-free.

Quarter-capacity reaping strongly reduces eviction-selection cost in these workloads. In unit-sized sequential LRU pressure, it performs 32 reaps instead of 8,192 while fetching the same 8,192 unique keys. Review the hot-set fetch counts to see policy-specific refills; reducing reap frequency does not guarantee the same cache-hit rate.

These compare a minimal one-unit target with 25%, across LRU/FIFO and unit/weighted sizes. They support the configurable 0.25 starting default for the declared mechanics workload, not a universal optimum. Half-capacity profiles, additional fetch costs, pools, concurrency, expiration and pinning are separate scenarios; correctness coverage does not imply measured results for them.
