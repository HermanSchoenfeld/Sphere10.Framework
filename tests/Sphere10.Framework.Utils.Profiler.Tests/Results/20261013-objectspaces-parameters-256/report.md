# Parameter study: measured screening candidates

Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.
Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.
Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.

Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-OuterCluster

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| outer-1024 | 63.0188 | 11.3347 | 165.6915 | 3 | lowest measured mean |
| outer-256 | 69.8219 | 10.6142 | 156.0164 | 3 | interval overlaps fastest |
| baseline | 91.1009 | 40.0559 | 306.9859 | 3 | interval overlaps fastest |
| outer-8192 | 106.5551 | 31.3608 | 493.3713 | 3 | slower measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-OuterCluster

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| outer-1024 | 21.7970 | 5.2838 | 39.4717 | 3 | lowest measured mean |
| outer-256 | 23.5762 | 5.8914 | 37.1340 | 3 | interval overlaps fastest |
| baseline | 26.5357 | 5.4590 | 72.8007 | 3 | interval overlaps fastest |
| outer-8192 | 31.5439 | 6.5558 | 116.1013 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-OuterCluster

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| outer-256 | 112.0106 | 14.7207 | 274.4557 | 3 | lowest measured mean |
| outer-1024 | 112.3894 | 24.0709 | 293.5949 | 3 | interval overlaps fastest |
| baseline | 146.5134 | 118.3806 | 541.1067 | 3 | interval overlaps fastest |
| outer-8192 | 188.4721 | 75.1016 | 870.4346 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-InnerCluster

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| inner-1024 | 63.3774 | 10.6879 | 270.3092 | 3 | lowest measured mean |
| baseline | 72.9407 | 16.6607 | 306.9686 | 3 | interval overlaps fastest |
| inner-4096 | 78.5563 | 19.5160 | 418.4199 | 3 | interval overlaps fastest |
| inner-8192 | 118.8388 | 36.7405 | 647.6992 | 3 | slower measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-InnerCluster

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| inner-1024 | 25.1320 | 12.3609 | 65.8460 | 3 | lowest measured mean |
| baseline | 26.7973 | 1.2650 | 72.8042 | 3 | interval overlaps fastest |
| inner-4096 | 32.5634 | 15.6129 | 107.1011 | 3 | interval overlaps fastest |
| inner-8192 | 42.9564 | 3.7450 | 166.7499 | 3 | slower measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-InnerCluster

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| inner-1024 | 105.9049 | 14.5472 | 472.2215 | 3 | lowest measured mean |
| inner-4096 | 118.6664 | 22.5388 | 730.3016 | 3 | interval overlaps fastest |
| baseline | 127.8781 | 22.1161 | 541.1074 | 3 | interval overlaps fastest |
| inner-8192 | 191.9783 | 118.1389 | 1130.0998 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Policy

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| headers-only | 68.9674 | 12.1923 | 306.9762 | 3 | lowest measured mean |
| fast-allocate | 69.7294 | 25.1332 | 306.9743 | 3 | interval overlaps fastest |
| descriptors-only | 72.7828 | 5.4228 | 306.9723 | 3 | interval overlaps fastest |
| no-caches | 74.6818 | 7.7542 | 306.9723 | 3 | interval overlaps fastest |
| baseline | 75.6209 | 18.8907 | 306.9726 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Policy

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| fast-allocate | 23.0142 | 3.3180 | 72.8041 | 3 | lowest measured mean |
| descriptors-only | 23.0512 | 1.8476 | 72.8073 | 3 | interval overlaps fastest |
| baseline | 24.5887 | 3.7039 | 72.8023 | 3 | interval overlaps fastest |
| no-caches | 26.1838 | 4.7700 | 72.8074 | 3 | interval overlaps fastest |
| headers-only | 26.8047 | 6.6599 | 72.8023 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Policy

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| descriptors-only | 113.6107 | 16.8140 | 541.1081 | 3 | lowest measured mean |
| headers-only | 118.1275 | 40.8542 | 541.1088 | 3 | interval overlaps fastest |
| baseline | 125.1837 | 2.1897 | 541.1089 | 3 | interval overlaps fastest |
| no-caches | 126.9194 | 15.4423 | 541.1079 | 3 | interval overlaps fastest |
| fast-allocate | 129.3903 | 43.8640 | 541.1089 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-False/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Policy

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| performance | 81.8491 | 26.7482 | 305.8748 | 3 | lowest measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-False/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Policy

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| performance | 28.3082 | 3.5477 | 72.4915 | 3 | lowest measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-False/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Policy

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| performance | 126.2841 | 23.2205 | 539.1800 | 3 | lowest measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-SaveMode

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 79.9206 | 26.9734 | 306.9744 | 3 | lowest measured mean |
| autosave | 87.4362 | 6.8448 | 306.9752 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-SaveMode

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 27.3213 | 1.7140 | 72.8042 | 3 | lowest measured mean |
| autosave | 29.0646 | 9.3773 | 72.7996 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-SaveMode

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| autosave | 128.6642 | 24.1220 | 541.1136 | 3 | lowest measured mean |
| baseline | 135.4667 | 27.0357 | 541.1095 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 79.7994 | 12.5077 | 306.9724 | 3 | lowest measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 26.9888 | 4.8820 | 72.8060 | 3 | lowest measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 120.4136 | 24.3143 | 541.1033 | 3 | lowest measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/merkle-SHA2_256/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-sha2-256 | 77.3194 | 19.6553 | 311.3267 | 3 | lowest measured mean |
| merkle-sha2-256-autosave | 87.0199 | 7.9930 | 311.3270 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/merkle-SHA2_256/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-sha2-256-autosave | 26.1452 | 0.9437 | 74.1607 | 3 | lowest measured mean |
| merkle-sha2-256 | 27.3249 | 3.3897 | 74.1651 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/merkle-SHA2_256/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-sha2-256 | 133.4377 | 46.7152 | 546.1536 | 3 | lowest measured mean |
| merkle-sha2-256-autosave | 133.4530 | 20.1725 | 546.1595 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/merkle-Blake2b_256/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-blake2b-256-autosave | 76.3918 | 15.6417 | 311.3133 | 3 | lowest measured mean |
| merkle-blake2b-256 | 83.5811 | 25.4720 | 311.3090 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/merkle-Blake2b_256/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-blake2b-256-autosave | 27.4511 | 5.4980 | 74.1525 | 3 | lowest measured mean |
| merkle-blake2b-256 | 27.9309 | 6.3861 | 74.1587 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/merkle-Blake2b_256/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-blake2b-256-autosave | 131.0167 | 17.3155 | 546.1406 | 3 | lowest measured mean |
| merkle-blake2b-256 | 138.5117 | 46.3520 | 546.1383 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/merkle-Blake2b_128/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-blake2b-128-autosave | 78.0294 | 19.4062 | 307.0431 | 3 | lowest measured mean |
| merkle-blake2b-128 | 84.6320 | 29.4769 | 307.0446 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/merkle-Blake2b_128/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-blake2b-128 | 28.1086 | 9.0396 | 72.8675 | 3 | lowest measured mean |
| merkle-blake2b-128-autosave | 29.1168 | 5.4820 | 72.8650 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/merkle-Blake2b_128/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-blake2b-128 | 149.2538 | 33.5857 | 539.0824 | 3 | lowest measured mean |
| merkle-blake2b-128-autosave | 156.4078 | 63.6886 | 539.0886 | 3 | interval overlaps fastest |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Indexes

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 84.7491 | 8.1130 | 306.9749 | 3 | lowest measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Indexes

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 29.9574 | 12.2892 | 72.8012 | 3 | lowest measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Indexes

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 130.5872 | 30.5711 | 541.1096 | 3 | lowest measured mean |

## object-spaces/Memory/PayloadOnly/IdentifierOnly/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Indexes

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| identifier-only | 26.6957 | 2.7374 | 95.3466 | 3 | lowest measured mean |

## object-spaces/Memory/PayloadOnly/IdentifierOnly/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Indexes

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| identifier-only | 15.7647 | 1.1559 | 30.0306 | 3 | lowest measured mean |

## object-spaces/Memory/PayloadOnly/IdentifierOnly/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Indexes

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| identifier-only | 38.5623 | 3.8105 | 160.6634 | 3 | lowest measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Endianness

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| big-endian | NaN | NaN | NaN | 0 | ineligible |
| baseline | 83.8396 | 42.3982 | 306.9728 | 3 | lowest measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Endianness

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| big-endian | NaN | NaN | NaN | 0 | ineligible |
| baseline | 27.6221 | 9.7385 | 72.8030 | 3 | lowest measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Endianness

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| big-endian | NaN | NaN | NaN | 0 | ineligible |
| baseline | 125.8437 | 33.9158 | 541.1090 | 3 | lowest measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-OuterCluster

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| outer-1024 | 154.7197 | 8.6077 | 462.3652 | 3 | lowest measured mean |
| baseline | 170.6987 | 22.4182 | 520.4641 | 3 | interval overlaps fastest |
| outer-256 | 215.6728 | 26.1646 | 567.6040 | 3 | slower measured mean |
| outer-8192 | 318.9289 | 41.1102 | 1711.7189 | 3 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-OuterCluster

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| outer-1024 | 46.6159 | 5.0312 | 109.9341 | 3 | lowest measured mean |
| baseline | 51.5391 | 3.6704 | 123.9123 | 3 | interval overlaps fastest |
| outer-256 | 61.7372 | 14.2983 | 134.8734 | 3 | interval overlaps fastest |
| outer-8192 | 82.3892 | 28.9338 | 407.1934 | 3 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-OuterCluster

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| outer-1024 | 281.5540 | 30.5768 | 822.0978 | 3 | lowest measured mean |
| baseline | 293.6233 | 26.5668 | 917.0089 | 3 | interval overlaps fastest |
| outer-256 | 361.7298 | 65.1337 | 998.6489 | 3 | interval overlaps fastest |
| outer-8192 | 556.0355 | 232.3374 | 3015.8508 | 3 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-InnerCluster

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| inner-1024 | 149.5902 | 43.1414 | 452.0597 | 3 | lowest measured mean |
| baseline | 167.9969 | 17.5277 | 520.4641 | 3 | interval overlaps fastest |
| inner-4096 | 196.6114 | 15.0543 | 674.4218 | 3 | interval overlaps fastest |
| inner-8192 | 244.8584 | 13.0062 | 1021.8576 | 3 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-InnerCluster

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| inner-1024 | 44.2363 | 10.1365 | 109.9120 | 3 | lowest measured mean |
| baseline | 49.7533 | 6.5171 | 123.9086 | 3 | interval overlaps fastest |
| inner-4096 | 69.8856 | 12.1559 | 172.9703 | 3 | slower measured mean |
| inner-8192 | 88.3122 | 5.5118 | 263.7467 | 3 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-InnerCluster

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| inner-1024 | 228.2004 | 29.5897 | 789.4552 | 3 | lowest measured mean |
| baseline | 279.2651 | 118.7952 | 917.0087 | 3 | interval overlaps fastest |
| inner-4096 | 309.4373 | 46.8876 | 1176.7249 | 3 | slower measured mean |
| inner-8192 | 425.6189 | 66.5902 | 1782.1179 | 3 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Policy

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| descriptors-only | 160.2149 | 45.1960 | 520.5084 | 3 | lowest measured mean |
| fast-allocate | 161.0436 | 20.8097 | 520.4639 | 3 | interval overlaps fastest |
| baseline | 164.6795 | 39.9840 | 520.4656 | 3 | interval overlaps fastest |
| no-caches | 174.9155 | 17.1927 | 520.5085 | 3 | interval overlaps fastest |
| headers-only | 184.7564 | 54.0192 | 520.4656 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Policy

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| fast-allocate | 52.3187 | 5.1908 | 123.9123 | 3 | lowest measured mean |
| headers-only | 53.4999 | 11.4349 | 123.9086 | 3 | interval overlaps fastest |
| descriptors-only | 56.6198 | 13.9197 | 123.9535 | 3 | interval overlaps fastest |
| baseline | 57.3205 | 22.9001 | 123.9106 | 3 | interval overlaps fastest |
| no-caches | 58.0861 | 21.1693 | 123.9516 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Policy

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| fast-allocate | 274.4777 | 40.2274 | 917.0072 | 3 | lowest measured mean |
| no-caches | 275.7256 | 56.2090 | 917.0109 | 3 | interval overlaps fastest |
| baseline | 286.2032 | 73.9562 | 917.0089 | 3 | interval overlaps fastest |
| descriptors-only | 286.5094 | 44.7287 | 917.0127 | 3 | interval overlaps fastest |
| headers-only | 314.3448 | 126.3370 | 917.0072 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-False/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Policy

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| performance | 180.8850 | 61.6328 | 519.3695 | 3 | lowest measured mean |

## object-spaces/File/PayloadOnly/Full/checks-False/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Policy

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| performance | 50.6755 | 7.5499 | 123.5957 | 3 | lowest measured mean |

## object-spaces/File/PayloadOnly/Full/checks-False/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Policy

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| performance | 263.4979 | 46.8338 | 915.0800 | 3 | lowest measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-SaveMode

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| autosave | 166.8715 | 7.0026 | 520.7167 | 3 | lowest measured mean |
| baseline | 169.2862 | 24.4927 | 520.4673 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-SaveMode

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| autosave | 51.7560 | 3.3763 | 124.1582 | 3 | lowest measured mean |
| baseline | 51.9939 | 3.8216 | 123.9086 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-SaveMode

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| autosave | 283.8612 | 49.4129 | 917.2643 | 3 | lowest measured mean |
| baseline | 297.6021 | 53.7037 | 917.0089 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 167.4930 | 40.9500 | 520.4680 | 3 | lowest measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 54.3773 | 21.6298 | 123.9106 | 3 | lowest measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 349.9663 | 139.0087 | 917.0089 | 3 | lowest measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/merkle-SHA2_256/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-sha2-256 | 172.5312 | 20.0543 | 527.5100 | 3 | lowest measured mean |
| merkle-sha2-256-autosave | 177.2545 | 26.5274 | 527.5113 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/merkle-SHA2_256/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-sha2-256 | 54.6951 | 8.4782 | 126.2596 | 3 | lowest measured mean |
| merkle-sha2-256-autosave | 57.3271 | 9.2482 | 126.2526 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/merkle-SHA2_256/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-sha2-256 | 286.7805 | 33.9766 | 924.6013 | 3 | lowest measured mean |
| merkle-sha2-256-autosave | 292.1256 | 40.0892 | 924.6082 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/merkle-Blake2b_256/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-blake2b-256 | 168.0056 | 7.2418 | 527.4945 | 3 | lowest measured mean |
| merkle-blake2b-256-autosave | 171.1515 | 24.3665 | 527.4924 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/merkle-Blake2b_256/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-blake2b-256 | 52.4912 | 1.3237 | 126.2554 | 3 | lowest measured mean |
| merkle-blake2b-256-autosave | 54.3798 | 3.9169 | 126.2503 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/merkle-Blake2b_256/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-blake2b-256-autosave | 295.2366 | 45.2783 | 924.5928 | 3 | lowest measured mean |
| merkle-blake2b-256 | 295.5608 | 43.5227 | 924.5845 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/merkle-Blake2b_128/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-blake2b-128-autosave | 165.8693 | 43.9313 | 519.0285 | 3 | lowest measured mean |
| merkle-blake2b-128 | 173.6297 | 7.8034 | 519.0332 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/merkle-Blake2b_128/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-blake2b-128 | 51.3950 | 1.6797 | 123.7103 | 3 | lowest measured mean |
| merkle-blake2b-128-autosave | 52.6662 | 5.1009 | 123.7046 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/merkle-Blake2b_128/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Merkle

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| merkle-blake2b-128-autosave | 288.6647 | 4.8883 | 910.5755 | 3 | lowest measured mean |
| merkle-blake2b-128 | 308.6270 | 54.0814 | 910.5755 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Indexes

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 185.3909 | 36.9437 | 520.4629 | 3 | lowest measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Indexes

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 55.5717 | 5.9180 | 123.9084 | 3 | lowest measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Indexes

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 290.4348 | 33.7800 | 917.0073 | 3 | lowest measured mean |

## object-spaces/File/PayloadOnly/IdentifierOnly/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Indexes

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| identifier-only | 62.8742 | 13.3608 | 158.7409 | 3 | lowest measured mean |

## object-spaces/File/PayloadOnly/IdentifierOnly/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Indexes

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| identifier-only | 28.8242 | 3.4836 | 50.6185 | 3 | lowest measured mean |

## object-spaces/File/PayloadOnly/IdentifierOnly/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Indexes

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| identifier-only | 99.2682 | 43.6374 | 266.8054 | 3 | lowest measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Endianness

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| big-endian | NaN | NaN | NaN | 0 | ineligible |
| baseline | 184.3702 | 52.3874 | 520.4621 | 3 | lowest measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Endianness

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| big-endian | NaN | NaN | NaN | 0 | ineligible |
| baseline | 57.3057 | 15.6674 | 123.9104 | 3 | lowest measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-Endianness

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| big-endian | NaN | NaN | NaN | 0 | ineligible |
| baseline | 281.2724 | 21.6691 | 917.0072 | 3 | lowest measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 151.4784 | 24.2988 | 520.4641 | 3 | lowest measured mean |
| page-65536 | 158.9268 | 17.2673 | 520.4846 | 3 | interval overlaps fastest |
| page-1048576 | 160.0309 | 29.3368 | 520.4661 | 3 | interval overlaps fastest |
| page-16384 | 177.4285 | 12.5867 | 525.6797 | 3 | interval overlaps fastest |
| page-4096 | 208.6497 | 7.6329 | 542.4035 | 3 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-65536 | 49.9830 | 2.3450 | 123.9365 | 3 | lowest measured mean |
| baseline | 52.5220 | 19.6894 | 123.9089 | 3 | interval overlaps fastest |
| page-1048576 | 52.7365 | 5.8053 | 123.9084 | 3 | interval overlaps fastest |
| page-16384 | 60.5284 | 16.1397 | 125.2576 | 3 | interval overlaps fastest |
| page-4096 | 84.2904 | 21.4302 | 129.6665 | 3 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 261.8981 | 18.4788 | 917.0072 | 3 | lowest measured mean |
| page-65536 | 279.1187 | 69.5397 | 917.0291 | 3 | interval overlaps fastest |
| page-1048576 | 283.3904 | 63.3606 | 917.0107 | 3 | interval overlaps fastest |
| page-16384 | 283.6577 | 29.7438 | 926.1038 | 3 | interval overlaps fastest |
| page-4096 | 360.4912 | 52.5514 | 955.1242 | 3 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| cache-4194304 | 165.3384 | 30.9852 | 520.4624 | 3 | lowest measured mean |
| cache-262144 | 168.8740 | 30.1028 | 520.4641 | 3 | interval overlaps fastest |
| cache-67108864 | 171.4066 | 9.1552 | 520.4641 | 3 | interval overlaps fastest |
| baseline | 184.9147 | 91.8044 | 520.4624 | 3 | interval overlaps fastest |
| cache-1048576 | 188.3539 | 98.7231 | 520.4624 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| cache-1048576 | 47.3323 | 4.7314 | 123.9123 | 3 | lowest measured mean |
| cache-4194304 | 47.7834 | 4.2743 | 123.9084 | 3 | interval overlaps fastest |
| cache-262144 | 50.2394 | 9.9868 | 123.9084 | 3 | interval overlaps fastest |
| baseline | 50.6918 | 2.6468 | 123.9106 | 3 | interval overlaps fastest |
| cache-67108864 | 54.2986 | 4.5943 | 123.9091 | 3 | interval overlaps fastest |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-256/ops-100/payload-128-Fixed/categories-256-0/lookup-Shuffled-10/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 286.8792 | 90.9588 | 917.0106 | 3 | lowest measured mean |
| cache-262144 | 290.1347 | 54.7948 | 917.0071 | 3 | interval overlaps fastest |
| cache-67108864 | 292.9025 | 45.2993 | 917.0072 | 3 | interval overlaps fastest |
| cache-1048576 | 310.5681 | 95.4624 | 917.0089 | 3 | interval overlaps fastest |
| cache-4194304 | 315.1946 | 98.1331 | 917.0072 | 3 | interval overlaps fastest |

Ineligible os-Memory-Endianness-big-endian-Mixed-Balanced: System.ArgumentException: Can only append from last cluster in chain (Parameter 'fromEnd')
   at Sphere10.Framework.ClusterMap.AppendClustersToEnd(Int64 fromEnd, Int64 quantity, ClusterMapChangedEventArgs event) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusterMap\ClusterMap.cs:line 74
   at Sphere10.Framework.ClusterMap.AppendClustersToEnd(Int64 fromEnd, Int64 quantity) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusterMap\ClusterMap.cs:line 59
   at Sphere10.Framework.ClusteredStreamFragmentProvider.SetTotalBytes(Int64 length) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusteredStreamFragmentProvider.cs:line 66
   at Sphere10.Framework.FragmentedStream.Write(Byte[] buffer, Int32 offset, Int32 count) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Streams\FragmentedStream.cs:line 127
   at Sphere10.Framework.StreamMappedBTree`2.CreateLeafNode() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Collections\StreamMapped\StreamMappedBTree.cs:line 187
   at Sphere10.Framework.UniqueKeyStorageAttachment`1.Add(Int64 index, TKey key) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\Attachments\UniqueKeyStorageAttachment.cs:line 119
   at Sphere10.Framework.ObjectStream.<>c__DisplayClass49_0.<SaveItemAndReturnStream>b__0() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectStream\ObjectStream.cs:line 168
   at Sphere10.Framework.ClusteredStream.Close() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusteredStream.cs:line 174
   at Sphere10.Framework.ObjectStream.SaveItemAndReturnStream(Int64 index, Object item, ObjectStreamOperationType operationType) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectStream\ObjectStream.cs:line 172
   at StreamMappedRecyclableList`1.Add(TItem item, Int64& index) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Collections\StreamMapped\StreamMappedRecyclableList.cs:line 72
   at Sphere10.Framework.ObjectSpaces.ObjectSpace.SaveInternal(Type itemType, Object item) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectSpaces\ObjectSpaceBase.cs:line 421
   at Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceBenchmarkFixture..ctor(ProfilerSettings settings, String profileName) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\ObjectSpaces\ObjectSpaceBenchmarkFixture.cs:line 65
   at Sphere10.Framework.Utils.Profiler.Studies.PerformanceStudy.StudyFixture..ctor(StudyCase item) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\Studies\PerformanceStudy.cs:line 294
   at Sphere10.Framework.Utils.Profiler.Studies.PerformanceStudy.Execute(StudyPlan plan, TextWriter output, String[] commandArguments) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\Studies\PerformanceStudy.cs:line 170
Ineligible os-Memory-Endianness-big-endian-Mixed-ReadHeavy: System.ArgumentException: Can only append from last cluster in chain (Parameter 'fromEnd')
   at Sphere10.Framework.ClusterMap.AppendClustersToEnd(Int64 fromEnd, Int64 quantity, ClusterMapChangedEventArgs event) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusterMap\ClusterMap.cs:line 74
   at Sphere10.Framework.ClusterMap.AppendClustersToEnd(Int64 fromEnd, Int64 quantity) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusterMap\ClusterMap.cs:line 59
   at Sphere10.Framework.ClusteredStreamFragmentProvider.SetTotalBytes(Int64 length) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusteredStreamFragmentProvider.cs:line 66
   at Sphere10.Framework.FragmentedStream.Write(Byte[] buffer, Int32 offset, Int32 count) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Streams\FragmentedStream.cs:line 127
   at Sphere10.Framework.StreamMappedBTree`2.CreateLeafNode() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Collections\StreamMapped\StreamMappedBTree.cs:line 187
   at Sphere10.Framework.UniqueKeyStorageAttachment`1.Add(Int64 index, TKey key) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\Attachments\UniqueKeyStorageAttachment.cs:line 119
   at Sphere10.Framework.ObjectStream.<>c__DisplayClass49_0.<SaveItemAndReturnStream>b__0() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectStream\ObjectStream.cs:line 168
   at Sphere10.Framework.ClusteredStream.Close() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusteredStream.cs:line 174
   at Sphere10.Framework.ObjectStream.SaveItemAndReturnStream(Int64 index, Object item, ObjectStreamOperationType operationType) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectStream\ObjectStream.cs:line 172
   at StreamMappedRecyclableList`1.Add(TItem item, Int64& index) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Collections\StreamMapped\StreamMappedRecyclableList.cs:line 72
   at Sphere10.Framework.ObjectSpaces.ObjectSpace.SaveInternal(Type itemType, Object item) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectSpaces\ObjectSpaceBase.cs:line 421
   at Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceBenchmarkFixture..ctor(ProfilerSettings settings, String profileName) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\ObjectSpaces\ObjectSpaceBenchmarkFixture.cs:line 65
   at Sphere10.Framework.Utils.Profiler.Studies.PerformanceStudy.StudyFixture..ctor(StudyCase item) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\Studies\PerformanceStudy.cs:line 294
   at Sphere10.Framework.Utils.Profiler.Studies.PerformanceStudy.Execute(StudyPlan plan, TextWriter output, String[] commandArguments) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\Studies\PerformanceStudy.cs:line 170
Ineligible os-Memory-Endianness-big-endian-Mixed-WriteHeavy: System.ArgumentException: Can only append from last cluster in chain (Parameter 'fromEnd')
   at Sphere10.Framework.ClusterMap.AppendClustersToEnd(Int64 fromEnd, Int64 quantity, ClusterMapChangedEventArgs event) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusterMap\ClusterMap.cs:line 74
   at Sphere10.Framework.ClusterMap.AppendClustersToEnd(Int64 fromEnd, Int64 quantity) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusterMap\ClusterMap.cs:line 59
   at Sphere10.Framework.ClusteredStreamFragmentProvider.SetTotalBytes(Int64 length) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusteredStreamFragmentProvider.cs:line 66
   at Sphere10.Framework.FragmentedStream.Write(Byte[] buffer, Int32 offset, Int32 count) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Streams\FragmentedStream.cs:line 127
   at Sphere10.Framework.StreamMappedBTree`2.CreateLeafNode() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Collections\StreamMapped\StreamMappedBTree.cs:line 187
   at Sphere10.Framework.UniqueKeyStorageAttachment`1.Add(Int64 index, TKey key) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\Attachments\UniqueKeyStorageAttachment.cs:line 119
   at Sphere10.Framework.ObjectStream.<>c__DisplayClass49_0.<SaveItemAndReturnStream>b__0() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectStream\ObjectStream.cs:line 168
   at Sphere10.Framework.ClusteredStream.Close() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusteredStream.cs:line 174
   at Sphere10.Framework.ObjectStream.SaveItemAndReturnStream(Int64 index, Object item, ObjectStreamOperationType operationType) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectStream\ObjectStream.cs:line 172
   at StreamMappedRecyclableList`1.Add(TItem item, Int64& index) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Collections\StreamMapped\StreamMappedRecyclableList.cs:line 72
   at Sphere10.Framework.ObjectSpaces.ObjectSpace.SaveInternal(Type itemType, Object item) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectSpaces\ObjectSpaceBase.cs:line 421
   at Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceBenchmarkFixture..ctor(ProfilerSettings settings, String profileName) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\ObjectSpaces\ObjectSpaceBenchmarkFixture.cs:line 65
   at Sphere10.Framework.Utils.Profiler.Studies.PerformanceStudy.StudyFixture..ctor(StudyCase item) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\Studies\PerformanceStudy.cs:line 294
   at Sphere10.Framework.Utils.Profiler.Studies.PerformanceStudy.Execute(StudyPlan plan, TextWriter output, String[] commandArguments) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\Studies\PerformanceStudy.cs:line 170
Ineligible os-File-Endianness-big-endian-Mixed-Balanced: System.ArgumentException: Can only append from last cluster in chain (Parameter 'fromEnd')
   at Sphere10.Framework.ClusterMap.AppendClustersToEnd(Int64 fromEnd, Int64 quantity, ClusterMapChangedEventArgs event) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusterMap\ClusterMap.cs:line 74
   at Sphere10.Framework.ClusterMap.AppendClustersToEnd(Int64 fromEnd, Int64 quantity) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusterMap\ClusterMap.cs:line 59
   at Sphere10.Framework.ClusteredStreamFragmentProvider.SetTotalBytes(Int64 length) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusteredStreamFragmentProvider.cs:line 66
   at Sphere10.Framework.FragmentedStream.Write(Byte[] buffer, Int32 offset, Int32 count) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Streams\FragmentedStream.cs:line 127
   at Sphere10.Framework.StreamMappedBTree`2.CreateLeafNode() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Collections\StreamMapped\StreamMappedBTree.cs:line 187
   at Sphere10.Framework.UniqueKeyStorageAttachment`1.Add(Int64 index, TKey key) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\Attachments\UniqueKeyStorageAttachment.cs:line 119
   at Sphere10.Framework.ObjectStream.<>c__DisplayClass49_0.<SaveItemAndReturnStream>b__0() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectStream\ObjectStream.cs:line 168
   at Sphere10.Framework.ClusteredStream.Close() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusteredStream.cs:line 174
   at Sphere10.Framework.ObjectStream.SaveItemAndReturnStream(Int64 index, Object item, ObjectStreamOperationType operationType) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectStream\ObjectStream.cs:line 172
   at StreamMappedRecyclableList`1.Add(TItem item, Int64& index) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Collections\StreamMapped\StreamMappedRecyclableList.cs:line 72
   at Sphere10.Framework.ObjectSpaces.ObjectSpace.SaveInternal(Type itemType, Object item) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectSpaces\ObjectSpaceBase.cs:line 421
   at Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceBenchmarkFixture..ctor(ProfilerSettings settings, String profileName) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\ObjectSpaces\ObjectSpaceBenchmarkFixture.cs:line 65
   at Sphere10.Framework.Utils.Profiler.Studies.PerformanceStudy.StudyFixture..ctor(StudyCase item) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\Studies\PerformanceStudy.cs:line 294
   at Sphere10.Framework.Utils.Profiler.Studies.PerformanceStudy.Execute(StudyPlan plan, TextWriter output, String[] commandArguments) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\Studies\PerformanceStudy.cs:line 170
Ineligible os-File-Endianness-big-endian-Mixed-ReadHeavy: System.ArgumentException: Can only append from last cluster in chain (Parameter 'fromEnd')
   at Sphere10.Framework.ClusterMap.AppendClustersToEnd(Int64 fromEnd, Int64 quantity, ClusterMapChangedEventArgs event) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusterMap\ClusterMap.cs:line 74
   at Sphere10.Framework.ClusterMap.AppendClustersToEnd(Int64 fromEnd, Int64 quantity) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusterMap\ClusterMap.cs:line 59
   at Sphere10.Framework.ClusteredStreamFragmentProvider.SetTotalBytes(Int64 length) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusteredStreamFragmentProvider.cs:line 66
   at Sphere10.Framework.FragmentedStream.Write(Byte[] buffer, Int32 offset, Int32 count) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Streams\FragmentedStream.cs:line 127
   at Sphere10.Framework.StreamMappedBTree`2.CreateLeafNode() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Collections\StreamMapped\StreamMappedBTree.cs:line 187
   at Sphere10.Framework.UniqueKeyStorageAttachment`1.Add(Int64 index, TKey key) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\Attachments\UniqueKeyStorageAttachment.cs:line 119
   at Sphere10.Framework.ObjectStream.<>c__DisplayClass49_0.<SaveItemAndReturnStream>b__0() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectStream\ObjectStream.cs:line 168
   at Sphere10.Framework.ClusteredStream.Close() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusteredStream.cs:line 174
   at Sphere10.Framework.ObjectStream.SaveItemAndReturnStream(Int64 index, Object item, ObjectStreamOperationType operationType) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectStream\ObjectStream.cs:line 172
   at StreamMappedRecyclableList`1.Add(TItem item, Int64& index) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Collections\StreamMapped\StreamMappedRecyclableList.cs:line 72
   at Sphere10.Framework.ObjectSpaces.ObjectSpace.SaveInternal(Type itemType, Object item) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectSpaces\ObjectSpaceBase.cs:line 421
   at Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceBenchmarkFixture..ctor(ProfilerSettings settings, String profileName) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\ObjectSpaces\ObjectSpaceBenchmarkFixture.cs:line 65
   at Sphere10.Framework.Utils.Profiler.Studies.PerformanceStudy.StudyFixture..ctor(StudyCase item) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\Studies\PerformanceStudy.cs:line 294
   at Sphere10.Framework.Utils.Profiler.Studies.PerformanceStudy.Execute(StudyPlan plan, TextWriter output, String[] commandArguments) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\Studies\PerformanceStudy.cs:line 170
Ineligible os-File-Endianness-big-endian-Mixed-WriteHeavy: System.ArgumentException: Can only append from last cluster in chain (Parameter 'fromEnd')
   at Sphere10.Framework.ClusterMap.AppendClustersToEnd(Int64 fromEnd, Int64 quantity, ClusterMapChangedEventArgs event) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusterMap\ClusterMap.cs:line 74
   at Sphere10.Framework.ClusterMap.AppendClustersToEnd(Int64 fromEnd, Int64 quantity) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusterMap\ClusterMap.cs:line 59
   at Sphere10.Framework.ClusteredStreamFragmentProvider.SetTotalBytes(Int64 length) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusteredStreamFragmentProvider.cs:line 66
   at Sphere10.Framework.FragmentedStream.Write(Byte[] buffer, Int32 offset, Int32 count) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Streams\FragmentedStream.cs:line 127
   at Sphere10.Framework.StreamMappedBTree`2.CreateLeafNode() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Collections\StreamMapped\StreamMappedBTree.cs:line 187
   at Sphere10.Framework.UniqueKeyStorageAttachment`1.Add(Int64 index, TKey key) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\Attachments\UniqueKeyStorageAttachment.cs:line 119
   at Sphere10.Framework.ObjectStream.<>c__DisplayClass49_0.<SaveItemAndReturnStream>b__0() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectStream\ObjectStream.cs:line 168
   at Sphere10.Framework.ClusteredStream.Close() in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ClusteredStreams\ClusteredStream.cs:line 174
   at Sphere10.Framework.ObjectStream.SaveItemAndReturnStream(Int64 index, Object item, ObjectStreamOperationType operationType) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectStream\ObjectStream.cs:line 172
   at StreamMappedRecyclableList`1.Add(TItem item, Int64& index) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\Collections\StreamMapped\StreamMappedRecyclableList.cs:line 72
   at Sphere10.Framework.ObjectSpaces.ObjectSpace.SaveInternal(Type itemType, Object item) in D:\Development\Sphere10\Framework\src\Sphere10.Framework\ObjectSpaces\ObjectSpaceBase.cs:line 421
   at Sphere10.Framework.Utils.Profiler.ObjectSpaces.ObjectSpaceBenchmarkFixture..ctor(ProfilerSettings settings, String profileName) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\ObjectSpaces\ObjectSpaceBenchmarkFixture.cs:line 65
   at Sphere10.Framework.Utils.Profiler.Studies.PerformanceStudy.StudyFixture..ctor(StudyCase item) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\Studies\PerformanceStudy.cs:line 294
   at Sphere10.Framework.Utils.Profiler.Studies.PerformanceStudy.Execute(StudyPlan plan, TextWriter output, String[] commandArguments) in D:\Development\Sphere10\Framework\utils\Sphere10.Framework.Utils.Profiler\Studies\PerformanceStudy.cs:line 170
