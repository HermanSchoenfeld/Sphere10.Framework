# Parameter study: measured screening candidates

Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.
Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.
Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.

Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Endianness

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| big-endian | 88.8778 | 25.5975 | 306.9903 | 3 | lowest measured mean |
| baseline | 93.3042 | 17.7682 | 306.9882 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Endianness

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| big-endian | 30.7936 | 11.2891 | 72.8058 | 3 | lowest measured mean |
| baseline | 30.8438 | 6.7150 | 72.8055 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Endianness

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 133.8552 | 42.0532 | 541.1184 | 3 | lowest measured mean |
| big-endian | 148.8381 | 107.4685 | 541.1189 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Endianness

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 174.2415 | 12.9711 | 520.4602 | 3 | lowest measured mean |
| big-endian | 187.6504 | 57.0296 | 520.4602 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Endianness

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 52.5807 | 11.6001 | 123.9069 | 3 | lowest measured mean |
| big-endian | 53.3271 | 8.9375 | 123.9044 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Endianness

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| big-endian | 285.4622 | 66.8994 | 917.0068 | 3 | lowest measured mean |
| baseline | 286.0684 | 35.5351 | 917.0067 | 3 | interval overlaps fastest |

