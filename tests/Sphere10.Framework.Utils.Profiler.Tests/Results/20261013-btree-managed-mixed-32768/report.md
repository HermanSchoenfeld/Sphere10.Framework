# Parameter study: measured screening candidates

Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.
Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.
Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.

Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.

## btrees/ManagedNodes/BTree/Int64/Mixed/balanced/records-32768/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-128 | 0.2939 | 0.0170 | 0.0000 | 3 | lowest measured mean |
| order-512 | 0.4094 | 0.5700 | 0.0000 | 3 | interval overlaps fastest |
| order-1024 | 0.4477 | 0.6415 | 0.0000 | 3 | interval overlaps fastest |
| order-64 | 2.1312 | 6.9324 | 0.0010 | 3 | interval overlaps fastest |

## btrees/ManagedNodes/BTree/Int64/Mixed/read-heavy/records-32768/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 0.1712 | 0.0633 | 0.0000 | 3 | lowest measured mean |
| order-128 | 0.1763 | 0.0491 | 0.0000 | 3 | interval overlaps fastest |
| order-64 | 0.1783 | 0.0565 | 0.0000 | 3 | interval overlaps fastest |
| order-1024 | 0.2109 | 0.1383 | 0.0000 | 3 | interval overlaps fastest |

## btrees/ManagedNodes/BTree/Int64/Mixed/write-heavy/records-32768/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-64 | 0.2582 | 0.0274 | 0.0010 | 3 | lowest measured mean |
| order-128 | 0.2701 | 0.0510 | 0.0000 | 3 | interval overlaps fastest |
| order-512 | 0.2759 | 0.0286 | 0.0000 | 3 | interval overlaps fastest |
| order-1024 | 0.3135 | 0.0324 | 0.0000 | 3 | interval overlaps fastest |

## btrees/ManagedNodes/BTreePlus/Int64/Mixed/balanced/records-32768/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-1024 | 2.3268 | 0.1533 | 0.0000 | 3 | lowest measured mean |
| order-512 | 3.9767 | 0.1499 | 0.0000 | 3 | slower measured mean |
| order-128 | 9.8808 | 0.5080 | 0.0000 | 3 | slower measured mean |
| order-64 | 19.5431 | 7.1963 | 0.0011 | 3 | slower measured mean |

## btrees/ManagedNodes/BTreePlus/Int64/Mixed/read-heavy/records-32768/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-1024 | 0.4153 | 1.0684 | 0.0000 | 3 | lowest measured mean |
| order-512 | 1.1061 | 2.1139 | 0.0000 | 3 | interval overlaps fastest |
| order-64 | 2.9093 | 5.4595 | 0.0000 | 3 | interval overlaps fastest |
| order-128 | 3.4688 | 3.8834 | 0.0000 | 3 | interval overlaps fastest |

## btrees/ManagedNodes/BTreePlus/Int64/Mixed/write-heavy/records-32768/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-1024 | 0.5054 | 0.0611 | 0.0000 | 3 | lowest measured mean |
| order-512 | 0.7020 | 0.1998 | 0.0000 | 3 | interval overlaps fastest |
| order-128 | 1.6517 | 0.1639 | 0.0000 | 3 | slower measured mean |
| order-64 | 2.5410 | 0.8124 | 0.0011 | 3 | slower measured mean |

## btrees/ManagedNodes/Lookup/Int32/Mixed/balanced/records-32768/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 4.0403 | 0.1333 | 0.0637 | 3 | lowest measured mean |
| order-1024 | 4.0729 | 4.5488 | 0.0000 | 3 | interval overlaps fastest |
| order-128 | 7.1276 | 2.0501 | 0.0021 | 3 | slower measured mean |
| order-64 | 13.4880 | 2.8476 | 0.0236 | 3 | slower measured mean |

## btrees/ManagedNodes/Lookup/Int32/Mixed/read-heavy/records-32768/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-1024 | 0.9110 | 0.0075 | 0.0000 | 3 | lowest measured mean |
| order-512 | 1.1910 | 0.0191 | 0.0796 | 3 | slower measured mean |
| order-128 | 1.7392 | 0.4952 | 0.0021 | 3 | slower measured mean |
| order-64 | 2.6570 | 0.2615 | 0.0000 | 3 | slower measured mean |

## btrees/ManagedNodes/Lookup/Int32/Mixed/write-heavy/records-32768/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-1024 | 4.9413 | 0.0663 | 0.0000 | 3 | lowest measured mean |
| order-512 | 6.5893 | 0.2000 | 0.0637 | 3 | slower measured mean |
| order-128 | 12.5794 | 5.4970 | 0.0042 | 3 | slower measured mean |
| order-64 | 22.4559 | 5.1690 | 0.0415 | 3 | slower measured mean |

