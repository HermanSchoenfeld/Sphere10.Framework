# Parameter study: measured screening candidates

Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.
Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.
Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.

Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-4096/ops-100/payload-16384-Fixed/categories-4096-0/lookup-Shuffled-10/shuffled-True/factor-FileCachePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-1048576-cache-2147483647 | 618.8822 | 59.6617 | 1429.4935 | 3 | lowest measured mean |
| page-1048576-cache-67108864 | 653.7071 | 239.8209 | 1429.4960 | 3 | interval overlaps fastest |
| page-262144-cache-16777216 | 663.7702 | 20.7498 | 1452.7699 | 3 | interval overlaps fastest |
| page-262144-cache-67108864 | 694.7992 | 93.0150 | 1435.6919 | 3 | interval overlaps fastest |
| page-262144-cache-2147483647 | 695.9742 | 189.9032 | 1433.7563 | 3 | interval overlaps fastest |
| current-core-default | 895.0231 | 114.1984 | 1994.5763 | 3 | slower measured mean |
| page-65536-cache-16777216 | 941.7358 | 84.1162 | 1457.3076 | 3 | slower measured mean |
| page-65536-cache-67108864 | 1017.4562 | 632.9793 | 1452.6008 | 3 | interval overlaps fastest |
| page-65536-cache-4194304 | 1298.1738 | 18.0904 | 1760.3025 | 3 | slower measured mean |
| page-65536-cache-2147483647 | 1358.7269 | 1931.7976 | 1451.8405 | 3 | interval overlaps fastest |
| page-1048576-cache-16777216 | 1814.5667 | 532.3822 | 4326.6798 | 3 | slower measured mean |
| page-262144-cache-4194304 | 2624.3181 | 2480.7062 | 3641.5029 | 3 | interval overlaps fastest |
| page-1048576-cache-4194304 | 8322.7067 | 1909.1083 | 21229.3798 | 3 | slower measured mean |

