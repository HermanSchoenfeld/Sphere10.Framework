# Parameter study: measured screening candidates

Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.
Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.
Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.

Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.

## btrees/FileStream/BTreePlus/Int64/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True/factor-OneLeafBoundary

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| boundary-order-4096 | 48.3080 | 2.2299 | 71.8639 | 3 | lowest measured mean |
| boundary-order-8193 | 54.4857 | 34.7868 | 117.4633 | 3 | interval overlaps fastest |
| boundary-order-2048 | 57.5290 | 41.3482 | 50.9778 | 3 | interval overlaps fastest |
| boundary-order-16384 | 65.1453 | 4.8062 | 211.2783 | 3 | slower measured mean |
| boundary-order-8192 | 94633.9505 | 31464.3511 | 198987.2833 | 3 | slower measured mean |

## btrees/FileStream/BTreePlus/Int64/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-OneLeafBoundary

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| boundary-order-2048 | 20.4460 | 0.5254 | 23.6767 | 3 | lowest measured mean |
| boundary-order-8193 | 22.5236 | 0.5340 | 38.1438 | 3 | slower measured mean |
| boundary-order-16384 | 26.4586 | 0.8787 | 56.9042 | 3 | slower measured mean |
| boundary-order-4096 | 26.8843 | 15.4954 | 28.3870 | 3 | interval overlaps fastest |
| boundary-order-8192 | 11448.8403 | 204.8395 | 39842.2366 | 3 | slower measured mean |

## btrees/FileStream/BTreePlus/Int64/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-OneLeafBoundary

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| boundary-order-8193 | 67.1938 | 2.0521 | 196.8320 | 3 | lowest measured mean |
| boundary-order-4096 | 72.7070 | 1.7170 | 115.3590 | 3 | slower measured mean |
| boundary-order-2048 | 73.4776 | 1.4110 | 78.2928 | 3 | slower measured mean |
| boundary-order-16384 | 93.4539 | 3.2789 | 365.6646 | 3 | slower measured mean |
| boundary-order-8192 | 102028.8118 | 899.2488 | 358432.2338 | 3 | slower measured mean |

