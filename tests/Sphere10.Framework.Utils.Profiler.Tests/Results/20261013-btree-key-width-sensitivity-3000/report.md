# Parameter study: measured screening candidates

Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.
Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.
Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.

Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.

## btrees/ManagedNodes/BTree/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-contender-order-128 | 0.0956 | 0.0009 | 0.0000 | 3 | lowest measured mean |
| Digest32-legacy64-order-64 | 0.1009 | 0.0024 | 0.0000 | 3 | slower measured mean |

## btrees/ManagedNodes/BTree/Int32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Int32-contender-order-128 | 0.0558 | 0.0042 | 0.0000 | 3 | lowest measured mean |
| Int32-legacy64-order-64 | 0.0600 | 0.0043 | 0.0000 | 3 | interval overlaps fastest |

## btrees/ManagedNodes/BTreePlus/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-wide-key-1024-order-1024 | 0.1169 | 0.0035 | 0.0000 | 3 | lowest measured mean |
| Digest32-contender-order-2048 | 0.1191 | 0.0126 | 0.0000 | 3 | interval overlaps fastest |
| Digest32-legacy64-order-64 | 0.3190 | 0.0466 | 0.0000 | 3 | slower measured mean |

## btrees/ManagedNodes/BTreePlus/Int32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Int32-contender-order-2048 | 0.0703 | 0.0020 | 0.0000 | 3 | lowest measured mean |
| Int32-legacy64-order-64 | 0.2560 | 0.0220 | 0.0000 | 3 | slower measured mean |

## btrees/ManagedNodes/Lookup/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-wide-key-1024-order-1024 | 0.9300 | 1.5579 | 0.0000 | 3 | lowest measured mean |
| Digest32-contender-order-2048 | 0.9403 | 1.5723 | 0.0000 | 3 | interval overlaps fastest |
| Digest32-legacy64-order-64 | 2.7747 | 2.2129 | 0.0016 | 3 | interval overlaps fastest |

## btrees/StreamMemory/BTree/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-legacy64-order-64 | 0.9309 | 1.5049 | 2.3890 | 3 | lowest measured mean |
| Digest32-contender-order-128 | 1.3109 | 1.6265 | 2.5669 | 3 | interval overlaps fastest |

## btrees/StreamMemory/BTree/Int32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Int32-contender-order-128 | 0.5011 | 0.1001 | 2.2851 | 3 | lowest measured mean |
| Int32-legacy64-order-64 | 0.5467 | 0.1688 | 2.1914 | 3 | interval overlaps fastest |

## btrees/StreamMemory/BTreePlus/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-wide-key-1024-order-1024 | 0.7410 | 0.0847 | 4.7768 | 3 | lowest measured mean |
| Digest32-legacy64-order-64 | 1.7274 | 0.1519 | 7.9874 | 3 | slower measured mean |
| Digest32-contender-order-2048 | 4.3508 | 12.7645 | 6.9422 | 3 | interval overlaps fastest |

## btrees/StreamMemory/BTreePlus/Int32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Int32-contender-order-2048 | 0.7955 | 0.0990 | 4.0953 | 3 | lowest measured mean |
| Int32-legacy64-order-64 | 2.2439 | 0.1088 | 7.5291 | 3 | slower measured mean |

## btrees/StreamMemory/Lookup/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-wide-key-1024-order-1024 | 8.8060 | 2.0969 | 57.0616 | 3 | lowest measured mean |
| Digest32-legacy64-order-64 | 18.4028 | 8.6478 | 84.1028 | 3 | interval overlaps fastest |
| Digest32-contender-order-2048 | 130.4628 | 54.6773 | 80.0122 | 3 | slower measured mean |

## btrees/FileStream/BTree/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-contender-order-128 | 2.2361 | 0.2956 | 2.5669 | 3 | lowest measured mean |
| Digest32-legacy64-order-64 | 2.6775 | 1.3103 | 2.3890 | 3 | interval overlaps fastest |

## btrees/FileStream/BTree/Int32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Int32-contender-order-128 | 1.7704 | 0.2197 | 2.2851 | 3 | lowest measured mean |
| Int32-legacy64-order-64 | 2.2071 | 0.0793 | 2.1914 | 3 | slower measured mean |

## btrees/FileStream/BTreePlus/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-wide-key-1024-order-1024 | 4.8250 | 3.2479 | 4.7768 | 3 | lowest measured mean |
| Digest32-contender-order-2048 | 6.8850 | 0.3438 | 6.9422 | 3 | interval overlaps fastest |
| Digest32-legacy64-order-64 | 23.0430 | 3.3076 | 7.9874 | 3 | slower measured mean |

## btrees/FileStream/BTreePlus/Int32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Int32-contender-order-2048 | 3.1460 | 0.1838 | 4.0953 | 3 | lowest measured mean |
| Int32-legacy64-order-64 | 23.0420 | 5.2889 | 7.5291 | 3 | slower measured mean |

## btrees/FileStream/Lookup/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-wide-key-1024-order-1024 | 47.1218 | 2.6640 | 57.0616 | 3 | lowest measured mean |
| Digest32-contender-order-2048 | 81.1893 | 5.0016 | 79.9961 | 3 | slower measured mean |
| Digest32-legacy64-order-64 | 235.3188 | 30.8502 | 84.1028 | 3 | slower measured mean |

## btrees/FilePaged/BTree/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-legacy64-order-64 | 7.4970 | 0.2141 | 21.3992 | 3 | lowest measured mean |
| Digest32-contender-order-256 | 8.6442 | 5.6579 | 21.1485 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTree/Int32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Int32-contender-order-256 | 6.8534 | 0.6163 | 19.9808 | 3 | lowest measured mean |
| Int32-legacy64-order-64 | 7.6858 | 1.8398 | 21.0804 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTreePlus/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-wide-key-1024-order-1024 | 9.5109 | 0.7570 | 30.4259 | 3 | lowest measured mean |
| Digest32-legacy64-order-64 | 25.6035 | 2.1022 | 91.3499 | 3 | slower measured mean |
| Digest32-contender-order-2048 | 52.9052 | 62.0781 | 35.3209 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTreePlus/Int32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Int32-contender-order-2048 | 8.1166 | 1.2008 | 26.9639 | 3 | lowest measured mean |
| Int32-legacy64-order-64 | 25.8710 | 2.8853 | 90.7048 | 3 | slower measured mean |

## btrees/FilePaged/Lookup/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-wide-key-1024-order-1024 | 89.1546 | 11.7275 | 382.9170 | 3 | lowest measured mean |
| Digest32-legacy64-order-64 | 240.4047 | 5.1846 | 945.8969 | 3 | slower measured mean |
| Digest32-contender-order-2048 | 593.5997 | 65.1534 | 440.0471 | 3 | slower measured mean |

## btrees/OSMemoryMapped/BTree/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-contender-order-128 | 1.5204 | 0.2895 | 2.5669 | 3 | lowest measured mean |
| Digest32-legacy64-order-64 | 1.8461 | 0.6276 | 2.3890 | 3 | interval overlaps fastest |

## btrees/OSMemoryMapped/BTree/Int32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Int32-contender-order-128 | 0.8777 | 0.1939 | 2.2851 | 3 | lowest measured mean |
| Int32-legacy64-order-64 | 0.9021 | 0.1567 | 2.1914 | 3 | interval overlaps fastest |

## btrees/OSMemoryMapped/BTreePlus/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-wide-key-1024-order-1024 | 1.4678 | 0.1708 | 4.7768 | 3 | lowest measured mean |
| Digest32-legacy64-order-64 | 2.9970 | 0.4271 | 7.9874 | 3 | slower measured mean |
| Digest32-contender-order-2048 | 4.5941 | 3.1809 | 6.9422 | 3 | interval overlaps fastest |

## btrees/OSMemoryMapped/BTreePlus/Int32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Int32-contender-order-2048 | 0.9517 | 0.0627 | 4.0953 | 3 | lowest measured mean |
| Int32-legacy64-order-64 | 2.1849 | 0.0864 | 7.5291 | 3 | slower measured mean |

## btrees/OSMemoryMapped/Lookup/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-wide-key-1024-order-1024 | 11.2114 | 2.9585 | 57.0616 | 3 | lowest measured mean |
| Digest32-legacy64-order-64 | 25.3236 | 16.4488 | 84.1028 | 3 | interval overlaps fastest |
| Digest32-contender-order-2048 | 45.3882 | 10.6726 | 79.9961 | 3 | slower measured mean |

## btrees/TransactionalFile/BTree/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-contender-order-128 | 10.1039 | 1.0093 | 20.4681 | 3 | lowest measured mean |
| Digest32-legacy64-order-64 | 11.5308 | 3.8403 | 21.4259 | 3 | interval overlaps fastest |

## btrees/TransactionalFile/BTree/Int32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Int32-contender-order-128 | 9.2913 | 1.1693 | 19.2965 | 3 | lowest measured mean |
| Int32-legacy64-order-64 | 10.1404 | 3.2245 | 21.1071 | 3 | interval overlaps fastest |

## btrees/TransactionalFile/BTreePlus/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-wide-key-1024-order-1024 | 11.5076 | 0.6916 | 30.4529 | 3 | lowest measured mean |
| Digest32-legacy64-order-64 | 37.3174 | 18.0769 | 91.3769 | 3 | slower measured mean |
| Digest32-contender-order-2048 | 57.3511 | 29.8913 | 35.3673 | 3 | slower measured mean |

## btrees/TransactionalFile/BTreePlus/Int32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Int32-contender-order-2048 | 11.1417 | 4.3011 | 26.9903 | 3 | lowest measured mean |
| Int32-legacy64-order-64 | 29.8903 | 1.6330 | 90.7315 | 3 | slower measured mean |

## btrees/TransactionalFile/Lookup/Digest32/Mixed/balanced/records-3000/ops-100/group-8/shuffled-True/factor-KeyWidthSensitivity

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| Digest32-wide-key-1024-order-1024 | 94.9501 | 5.6587 | 382.9668 | 3 | lowest measured mean |
| Digest32-legacy64-order-64 | 273.0580 | 6.8667 | 945.9241 | 3 | slower measured mean |
| Digest32-contender-order-2048 | 539.5575 | 129.5191 | 440.0934 | 3 | slower measured mean |

