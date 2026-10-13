# Parameter study: measured screening candidates

Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.
Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.
Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.

Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| combined-candidate | 158.6000 | 4.9636 | 528.6959 | 7 | lowest measured mean |
| order-only | 174.5491 | 18.1614 | 810.1585 | 7 | interval overlaps fastest |
| baseline-64 | 208.6653 | 7.9305 | 950.5803 | 7 | slower measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| combined-candidate | 49.4849 | 2.4931 | 126.0190 | 7 | lowest measured mean |
| order-only | 53.6818 | 2.5111 | 180.2910 | 7 | interval overlaps fastest |
| baseline-64 | 62.6762 | 3.8049 | 225.7592 | 7 | slower measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-only | 275.9008 | 13.8839 | 1403.0719 | 7 | lowest measured mean |
| baseline-64 | 327.6621 | 22.6466 | 1665.0210 | 7 | slower measured mean |
| combined-candidate | 521.6761 | 9.1329 | 1412.1332 | 7 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-only | 393.8874 | 11.5339 | 1372.3336 | 7 | lowest measured mean |
| baseline-64 | 474.2900 | 17.5716 | 1617.4235 | 7 | slower measured mean |
| combined-candidate | 569.2263 | 16.4020 | 1864.3098 | 7 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-only | 114.6240 | 9.1425 | 307.0875 | 7 | lowest measured mean |
| baseline-64 | 128.4516 | 6.8681 | 382.3454 | 7 | interval overlaps fastest |
| combined-candidate | 141.5818 | 8.0316 | 354.2735 | 7 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-1024/ops-200/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-only | 646.4117 | 26.7299 | 2378.7503 | 7 | lowest measured mean |
| combined-candidate | 763.8194 | 22.4220 | 2567.4329 | 7 | slower measured mean |
| baseline-64 | 773.0557 | 30.3243 | 2835.5105 | 7 | slower measured mean |

