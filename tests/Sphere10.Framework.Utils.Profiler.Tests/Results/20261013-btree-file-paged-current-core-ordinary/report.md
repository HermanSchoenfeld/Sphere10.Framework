# Parameter study: measured screening candidates

Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.
Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.
Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.

Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.

## btrees/FilePaged/BTree/Int64/Mixed/balanced/records-8192/ops-100/group-8/shuffled-True/factor-CurrentCoreOrdinarySensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| current-core-order-128 | 7.5697 | 0.4013 | 20.8340 | 3 | lowest measured mean |
| current-core-order-256 | 8.1219 | 0.7557 | 21.9452 | 3 | interval overlaps fastest |
| current-core-order-64 | 8.1799 | 0.5707 | 23.1839 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTree/Int64/Mixed/read-heavy/records-8192/ops-100/group-8/shuffled-True/factor-CurrentCoreOrdinarySensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| current-core-order-128 | 6.5798 | 0.5093 | 16.7322 | 3 | lowest measured mean |
| current-core-order-256 | 7.1187 | 0.9914 | 17.0195 | 3 | interval overlaps fastest |
| current-core-order-64 | 9.2190 | 2.9825 | 18.4923 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTree/Int64/Mixed/write-heavy/records-8192/ops-100/group-8/shuffled-True/factor-CurrentCoreOrdinarySensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| current-core-order-128 | 8.3481 | 0.6106 | 24.9252 | 3 | lowest measured mean |
| current-core-order-256 | 8.6504 | 0.4376 | 26.8303 | 3 | interval overlaps fastest |
| current-core-order-64 | 8.9983 | 0.8808 | 27.8280 | 3 | interval overlaps fastest |

