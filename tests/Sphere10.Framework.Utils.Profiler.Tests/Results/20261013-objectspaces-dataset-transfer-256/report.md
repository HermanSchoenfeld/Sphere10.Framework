# Parameter study: measured screening candidates

Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.
Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.
Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.

Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Variable/categories-16-60/lookup-HotSet-10/shuffled-True/factor-DatasetTransfer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| combined-candidate | 451.0184 | 26.9220 | 1893.8159 | 7 | lowest measured mean |
| order-only | 582.5920 | 55.4126 | 3035.2734 | 7 | slower measured mean |
| baseline-64 | 655.9591 | 91.3798 | 3216.7837 | 7 | slower measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Variable/categories-16-60/lookup-HotSet-10/shuffled-True/factor-DatasetTransfer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| combined-candidate | 88.7807 | 14.3207 | 306.1397 | 7 | lowest measured mean |
| order-only | 99.2085 | 8.1880 | 477.3693 | 7 | interval overlaps fastest |
| baseline-64 | 102.0293 | 15.2114 | 437.9637 | 7 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Variable/categories-16-60/lookup-HotSet-10/shuffled-True/factor-DatasetTransfer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline-64 | 1242.1090 | 83.5263 | 6063.3023 | 7 | lowest measured mean |
| order-only | 1269.7900 | 151.1065 | 5599.9005 | 7 | interval overlaps fastest |
| combined-candidate | 1897.3328 | 98.8141 | 5214.3933 | 7 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Variable/categories-16-60/lookup-HotSet-10/shuffled-True/factor-DatasetTransfer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| combined-candidate | 1585.7696 | 73.2899 | 5526.6081 | 7 | lowest measured mean |
| order-only | 1607.9746 | 147.9342 | 5186.9988 | 7 | interval overlaps fastest |
| baseline-64 | 1648.2077 | 118.6893 | 5496.8978 | 7 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Variable/categories-16-60/lookup-HotSet-10/shuffled-True/factor-DatasetTransfer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline-64 | 229.0878 | 20.6653 | 754.3672 | 7 | lowest measured mean |
| combined-candidate | 263.3341 | 11.7210 | 894.4999 | 7 | slower measured mean |
| order-only | 272.3380 | 27.1416 | 817.2798 | 7 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Variable/categories-16-60/lookup-HotSet-10/shuffled-True/factor-DatasetTransfer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-only | 2664.5228 | 199.2288 | 9555.9374 | 7 | lowest measured mean |
| combined-candidate | 2997.4187 | 169.8976 | 10222.8583 | 7 | interval overlaps fastest |
| baseline-64 | 3012.4979 | 115.1995 | 10356.0582 | 7 | slower measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-1024-Variable/categories-16-60/lookup-HotSet-10/shuffled-True/factor-DatasetTransfer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| combined-candidate | 477.9553 | 17.8290 | 1894.9749 | 7 | lowest measured mean |
| order-only | 547.0845 | 32.8761 | 2967.6998 | 7 | slower measured mean |
| baseline-64 | 625.0676 | 39.6993 | 3304.2771 | 7 | slower measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-1024-Variable/categories-16-60/lookup-HotSet-10/shuffled-True/factor-DatasetTransfer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| combined-candidate | 89.3762 | 15.3898 | 306.3618 | 7 | lowest measured mean |
| baseline-64 | 95.8401 | 3.8205 | 453.0097 | 7 | interval overlaps fastest |
| order-only | 101.5156 | 8.3082 | 467.3987 | 7 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-1024-Variable/categories-16-60/lookup-HotSet-10/shuffled-True/factor-DatasetTransfer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-only | 997.1159 | 37.8801 | 5509.7942 | 7 | lowest measured mean |
| baseline-64 | 1122.4186 | 58.4022 | 6164.1255 | 7 | slower measured mean |
| combined-candidate | 1782.6338 | 41.2583 | 5221.0271 | 7 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-1024-Variable/categories-16-60/lookup-HotSet-10/shuffled-True/factor-DatasetTransfer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-only | 1398.7439 | 46.7176 | 5052.2924 | 7 | lowest measured mean |
| baseline-64 | 1593.3591 | 75.1001 | 5656.9876 | 7 | slower measured mean |
| combined-candidate | 1604.9297 | 65.5003 | 5529.6649 | 7 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-1024-Variable/categories-16-60/lookup-HotSet-10/shuffled-True/factor-DatasetTransfer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline-64 | 239.4746 | 15.4336 | 782.5367 | 7 | lowest measured mean |
| order-only | 242.6075 | 15.4172 | 797.3367 | 7 | interval overlaps fastest |
| combined-candidate | 279.8381 | 7.9374 | 895.1602 | 7 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-1024-Variable/categories-16-60/lookup-HotSet-10/shuffled-True/factor-DatasetTransfer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-only | 2898.8705 | 314.3394 | 9376.3088 | 7 | lowest measured mean |
| combined-candidate | 2999.6527 | 124.3874 | 10228.4056 | 7 | interval overlaps fastest |
| baseline-64 | 3028.2927 | 118.0017 | 10535.4476 | 7 | interval overlaps fastest |

