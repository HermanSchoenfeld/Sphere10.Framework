# Parameter study: measured screening candidates

Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.
Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.
Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.

Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.

## btrees/ManagedNodes/BTree/Int64/Read/n/a/records-100000/ops-100000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-1024 | 13.3107 | 0.4364 | 0.0000 | 5 | lowest measured mean |
| order-512 | 13.6241 | 0.4295 | 0.0000 | 5 | interval overlaps fastest |
| order-256 | 13.6739 | 0.4815 | 0.0000 | 5 | interval overlaps fastest |
| order-64 | 14.1667 | 0.3376 | 0.0000 | 5 | slower measured mean |
| order-128 | 14.2093 | 0.3678 | 0.0000 | 5 | slower measured mean |

## btrees/ManagedNodes/BTreePlus/Int64/Read/n/a/records-100000/ops-100000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 13.0671 | 0.6025 | 0.0000 | 5 | lowest measured mean |
| order-256 | 13.1965 | 0.8726 | 0.0000 | 5 | interval overlaps fastest |
| order-1024 | 13.2057 | 0.8674 | 0.0000 | 5 | interval overlaps fastest |
| order-128 | 13.2862 | 0.3880 | 0.0000 | 5 | interval overlaps fastest |
| order-64 | 14.1121 | 1.0166 | 0.0000 | 5 | interval overlaps fastest |

## btrees/ManagedNodes/Lookup/Int32/Read/n/a/records-100000/ops-100000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 46.4755 | 1.8526 | 0.0000 | 5 | lowest measured mean |
| order-1024 | 46.6123 | 1.0627 | 0.0000 | 5 | interval overlaps fastest |
| order-256 | 47.6046 | 2.9534 | 0.0000 | 5 | interval overlaps fastest |
| order-64 | 47.7116 | 0.8603 | 0.0000 | 5 | interval overlaps fastest |
| order-128 | 48.2271 | 1.6530 | 0.0000 | 5 | interval overlaps fastest |

