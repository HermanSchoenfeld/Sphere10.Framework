# Parameter study: measured screening candidates

Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.
Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.
Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.

Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.

## btrees/FilePaged/Lookup/Int32/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True/factor-CurrentCoreNumericLookup

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-2048 | 866.6128 | 92.8736 | 4194.7255 | 3 | lowest measured mean |
| order-1024 | 1019.5182 | 19.4327 | 4583.0497 | 3 | slower measured mean |

## btrees/TransactionalFile/Lookup/Int32/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True/factor-CurrentCoreNumericLookup

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-2048 | 943.3661 | 7.6392 | 4194.7741 | 3 | lowest measured mean |
| order-1024 | 1189.1761 | 187.9180 | 4583.0976 | 3 | slower measured mean |

