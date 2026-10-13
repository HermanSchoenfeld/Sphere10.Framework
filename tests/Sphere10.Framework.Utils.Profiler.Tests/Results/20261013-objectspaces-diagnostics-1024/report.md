# Parameter study: measured screening candidates

Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.
Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.
Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.

Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Read/n/a/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline-64 | 35.8881 | 1.1617 | 34.8187 | 3 | lowest measured mean |
| combined-candidate | 37.6940 | 5.3171 | 30.7189 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Miss/n/a/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| combined-candidate | 29.5037 | 9.0715 | 30.9135 | 3 | lowest measured mean |
| baseline-64 | 31.5564 | 12.0693 | 44.9357 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Scan/n/a/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline-64 | 6.7291 | 6.5748 | 26.4473 | 3 | lowest measured mean |
| combined-candidate | 6.9400 | 0.4735 | 23.9971 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Insert/n/a/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| combined-candidate | 661.1754 | 170.5513 | 2451.1880 | 3 | lowest measured mean |
| baseline-64 | 724.5643 | 92.6149 | 3453.6960 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Update/n/a/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| combined-candidate | 282.5032 | 52.4710 | 1019.1064 | 3 | lowest measured mean |
| baseline-64 | 405.5974 | 24.1723 | 1869.6136 | 3 | slower measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Recycle/n/a/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| combined-candidate | 345.2012 | 98.9528 | 1181.1512 | 3 | lowest measured mean |
| baseline-64 | 442.8519 | 117.0695 | 2044.8791 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Reopen/n/a/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline-64 | 24.5241 | 8.6730 | 118.8023 | 3 | lowest measured mean |
| combined-candidate | 33.5835 | 14.3406 | 101.2439 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Read/n/a/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline-64 | 35.7900 | 2.9447 | 59.6262 | 3 | lowest measured mean |
| order-only | 35.9790 | 3.0419 | 60.7733 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Miss/n/a/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-only | 32.7467 | 3.2596 | 57.4411 | 3 | lowest measured mean |
| baseline-64 | 43.2560 | 4.2522 | 79.2009 | 3 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Scan/n/a/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-only | 13.5266 | 0.8638 | 47.5870 | 3 | lowest measured mean |
| baseline-64 | 15.1813 | 1.5941 | 46.8403 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Insert/n/a/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline-64 | 1743.0854 | 254.3330 | 5690.5687 | 3 | lowest measured mean |
| order-only | 2114.8185 | 188.1297 | 7366.8387 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Update/n/a/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-only | 800.6486 | 53.4526 | 2684.6662 | 3 | lowest measured mean |
| baseline-64 | 956.5420 | 233.6613 | 3179.0038 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Recycle/n/a/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-only | 909.8099 | 107.2820 | 2989.8425 | 3 | lowest measured mean |
| baseline-64 | 984.6161 | 22.8776 | 3464.9452 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Reopen/n/a/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline-64 | 57.1766 | 5.5994 | 208.5454 | 3 | lowest measured mean |
| order-only | 58.1580 | 22.2822 | 207.0197 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Rollback/n/a/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline-64 | 56.9563 | 6.4345 | 208.3554 | 3 | lowest measured mean |
| order-only | 62.8048 | 13.6159 | 206.8967 | 3 | interval overlaps fastest |

