# Parameter study: measured screening candidates

Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.
Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.
Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.

Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.

## btrees/ManagedNodes/BTree/Int64/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-64 | 0.2664 | 0.0032 | 0.0010 | 7 | lowest measured mean |
| order-128 | 0.2696 | 0.0023 | 0.0000 | 7 | interval overlaps fastest |
| order-512 | 0.2770 | 0.0044 | 0.0000 | 7 | slower measured mean |
| order-256 | 0.2784 | 0.0189 | 0.0000 | 7 | interval overlaps fastest |
| order-16 | 0.2959 | 0.0026 | 0.0015 | 7 | slower measured mean |
| order-32 | 0.2972 | 0.0077 | 0.0000 | 7 | slower measured mean |
| order-8 | 0.3507 | 0.0036 | 0.0023 | 7 | slower measured mean |

## btrees/ManagedNodes/BTree/Int64/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-64 | 0.1905 | 0.0699 | 0.0000 | 7 | lowest measured mean; potential temporal drift (2.30 times between half medians) |
| order-512 | 0.1938 | 0.0803 | 0.0000 | 7 | interval overlaps fastest; potential temporal drift (3.07 times between half medians) |
| order-256 | 0.1990 | 0.0748 | 0.0000 | 7 | interval overlaps fastest; potential temporal drift (2.29 times between half medians) |
| order-128 | 0.2075 | 0.0639 | 0.0000 | 7 | interval overlaps fastest |
| order-32 | 0.2114 | 0.0855 | 0.0000 | 7 | interval overlaps fastest; potential temporal drift (2.94 times between half medians) |
| order-16 | 0.2215 | 0.0753 | 0.0007 | 7 | interval overlaps fastest; potential temporal drift (1.92 times between half medians) |
| order-8 | 0.2621 | 0.0919 | 0.0008 | 7 | interval overlaps fastest; potential temporal drift (2.00 times between half medians) |

## btrees/ManagedNodes/BTree/Int64/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-64 | 0.1937 | 0.0056 | 0.0010 | 7 | lowest measured mean |
| order-128 | 0.1942 | 0.0049 | 0.0000 | 7 | interval overlaps fastest |
| order-256 | 0.1992 | 0.0075 | 0.0000 | 7 | interval overlaps fastest |
| order-512 | 0.2016 | 0.0059 | 0.0000 | 7 | interval overlaps fastest |
| order-32 | 0.2372 | 0.0053 | 0.0012 | 7 | slower measured mean |
| order-16 | 0.2418 | 0.0214 | 0.0016 | 7 | slower measured mean |
| order-8 | 0.3305 | 0.0047 | 0.0048 | 7 | slower measured mean |

## btrees/ManagedNodes/BTreePlus/Int64/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 0.3702 | 0.0023 | 0.0000 | 7 | lowest measured mean |
| order-256 | 0.4845 | 0.0067 | 0.0000 | 7 | slower measured mean |
| order-128 | 0.6879 | 0.0033 | 0.0000 | 7 | slower measured mean |
| order-64 | 1.1057 | 0.0120 | 0.0000 | 7 | slower measured mean |
| order-32 | 1.6048 | 0.0050 | 0.0000 | 7 | slower measured mean |
| order-16 | 3.0406 | 0.0248 | 0.0000 | 7 | slower measured mean |
| order-8 | 6.3029 | 0.0793 | 0.0043 | 7 | slower measured mean |

## btrees/ManagedNodes/BTreePlus/Int64/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 0.2273 | 0.0047 | 0.0000 | 7 | lowest measured mean |
| order-256 | 0.2509 | 0.0023 | 0.0000 | 7 | slower measured mean |
| order-128 | 0.2945 | 0.0023 | 0.0000 | 7 | slower measured mean |
| order-64 | 0.3790 | 0.0029 | 0.0000 | 7 | slower measured mean |
| order-32 | 0.5099 | 0.0057 | 0.0000 | 7 | slower measured mean |
| order-16 | 0.8188 | 0.0026 | 0.0000 | 7 | slower measured mean |
| order-8 | 1.4927 | 0.0180 | 0.0003 | 7 | slower measured mean |

## btrees/ManagedNodes/BTreePlus/Int64/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 0.4816 | 0.0179 | 0.0000 | 7 | lowest measured mean |
| order-256 | 0.6976 | 0.0314 | 0.0000 | 7 | slower measured mean |
| order-128 | 1.0504 | 0.0180 | 0.0000 | 7 | slower measured mean |
| order-64 | 1.8027 | 0.0201 | 0.0000 | 7 | slower measured mean |
| order-32 | 2.6450 | 0.0531 | 0.0006 | 7 | slower measured mean |
| order-16 | 5.0542 | 0.1083 | 0.0000 | 7 | slower measured mean |
| order-8 | 10.6680 | 0.2597 | 0.0059 | 7 | slower measured mean |

## btrees/ManagedNodes/Lookup/Int32/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 2.2659 | 1.8491 | 0.0000 | 7 | lowest measured mean; potential temporal drift (5.98 times between half medians) |
| order-256 | 3.0528 | 2.9486 | 0.0000 | 7 | interval overlaps fastest; potential temporal drift (7.04 times between half medians) |
| order-128 | 3.4177 | 2.9894 | 0.0021 | 7 | interval overlaps fastest; potential temporal drift (8.22 times between half medians) |
| order-64 | 4.9266 | 4.5070 | 0.0045 | 7 | interval overlaps fastest; potential temporal drift (9.85 times between half medians) |
| order-32 | 5.5702 | 6.7104 | 0.0266 | 7 | interval overlaps fastest; potential temporal drift (12.87 times between half medians) |
| order-16 | 12.7141 | 12.1034 | 0.0263 | 7 | interval overlaps fastest; potential temporal drift (12.22 times between half medians) |
| order-8 | 20.6894 | 23.7664 | 0.0603 | 7 | interval overlaps fastest; potential temporal drift (11.70 times between half medians) |

## btrees/ManagedNodes/Lookup/Int32/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-256 | 0.2974 | 0.0012 | 0.0000 | 7 | lowest measured mean |
| order-512 | 0.3049 | 0.0261 | 0.0000 | 7 | interval overlaps fastest |
| order-128 | 0.3140 | 0.0051 | 0.0000 | 7 | slower measured mean |
| order-64 | 0.3528 | 0.0017 | 0.0034 | 7 | slower measured mean |
| order-32 | 0.3827 | 0.0037 | 0.0025 | 7 | slower measured mean |
| order-16 | 0.5146 | 0.0043 | 0.0043 | 7 | slower measured mean |
| order-8 | 1.0040 | 0.0331 | 0.0096 | 7 | slower measured mean |

## btrees/ManagedNodes/Lookup/Int32/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 1.3904 | 0.0231 | 0.0000 | 7 | lowest measured mean |
| order-256 | 1.4360 | 0.0065 | 0.0000 | 7 | slower measured mean |
| order-128 | 1.6108 | 0.0056 | 0.0021 | 7 | slower measured mean |
| order-64 | 2.0071 | 0.0200 | 0.0067 | 7 | slower measured mean |
| order-32 | 2.1458 | 0.0204 | 0.0051 | 7 | slower measured mean |
| order-16 | 4.0418 | 0.0166 | 0.0481 | 7 | slower measured mean |
| order-8 | 8.2271 | 0.5245 | 0.0927 | 7 | slower measured mean |

## btrees/StreamMemory/BTree/Int64/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-64 | 2.1073 | 0.0857 | 10.7450 | 7 | lowest measured mean |
| order-128 | 2.1487 | 0.0775 | 11.2787 | 7 | interval overlaps fastest |
| order-32 | 2.1599 | 0.1168 | 10.6544 | 7 | interval overlaps fastest |
| order-16 | 2.2058 | 0.0970 | 10.7314 | 7 | interval overlaps fastest |
| order-256 | 2.2125 | 0.0855 | 11.8623 | 7 | interval overlaps fastest |
| order-8 | 2.2565 | 0.0589 | 10.9902 | 7 | slower measured mean |
| order-512 | 2.3604 | 0.0591 | 13.4596 | 7 | slower measured mean |

## btrees/StreamMemory/BTree/Int64/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-256 | 1.6097 | 0.0369 | 8.8322 | 7 | lowest measured mean |
| order-64 | 1.6466 | 0.0736 | 8.5479 | 7 | interval overlaps fastest |
| order-512 | 1.6830 | 0.0859 | 9.2307 | 7 | interval overlaps fastest |
| order-128 | 1.7274 | 0.1401 | 8.7874 | 7 | interval overlaps fastest |
| order-32 | 1.7384 | 0.1556 | 8.5490 | 7 | interval overlaps fastest |
| order-16 | 1.8027 | 0.1823 | 8.6469 | 7 | interval overlaps fastest |
| order-8 | 1.8494 | 0.1640 | 8.8001 | 7 | slower measured mean |

## btrees/StreamMemory/BTree/Int64/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-64 | 2.6470 | 0.1960 | 12.9825 | 7 | lowest measured mean |
| order-16 | 2.6601 | 0.1208 | 12.7462 | 7 | interval overlaps fastest |
| order-32 | 2.7941 | 0.1325 | 12.8458 | 7 | interval overlaps fastest |
| order-256 | 2.8064 | 0.1743 | 14.9669 | 7 | interval overlaps fastest |
| order-128 | 3.0224 | 0.6458 | 13.7940 | 7 | interval overlaps fastest |
| order-512 | 3.0335 | 0.0914 | 17.7796 | 7 | slower measured mean |
| order-8 | 3.0893 | 0.6107 | 13.2190 | 7 | interval overlaps fastest |

## btrees/StreamMemory/BTreePlus/Int64/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 2.9972 | 0.1508 | 15.1425 | 7 | lowest measured mean |
| order-256 | 3.4691 | 0.1366 | 16.7349 | 7 | slower measured mean |
| order-128 | 4.5433 | 0.1430 | 22.0873 | 7 | slower measured mean |
| order-64 | 7.1101 | 0.3003 | 34.6186 | 7 | slower measured mean |
| order-32 | 9.0374 | 0.4013 | 42.8897 | 7 | slower measured mean |
| order-16 | 14.9371 | 1.0555 | 70.1931 | 7 | slower measured mean |
| order-8 | 26.8016 | 1.4933 | 126.6403 | 7 | slower measured mean |

## btrees/StreamMemory/BTreePlus/Int64/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 1.8339 | 0.1486 | 8.7419 | 7 | lowest measured mean |
| order-256 | 1.9823 | 0.5062 | 8.7938 | 7 | interval overlaps fastest |
| order-128 | 2.1180 | 0.2628 | 9.7081 | 7 | interval overlaps fastest |
| order-64 | 2.5424 | 0.3070 | 11.9880 | 7 | slower measured mean |
| order-32 | 2.9294 | 0.1969 | 13.4453 | 7 | slower measured mean |
| order-16 | 4.0702 | 0.2785 | 18.9113 | 7 | slower measured mean |
| order-8 | 7.3486 | 2.1591 | 30.2017 | 7 | slower measured mean |

## btrees/StreamMemory/BTreePlus/Int64/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 4.2081 | 0.1616 | 21.6774 | 7 | lowest measured mean |
| order-256 | 4.9065 | 0.1821 | 24.8174 | 7 | slower measured mean |
| order-128 | 6.8506 | 0.2532 | 34.6810 | 7 | slower measured mean |
| order-64 | 11.1913 | 0.3021 | 57.6947 | 7 | slower measured mean |
| order-32 | 14.6211 | 0.2363 | 73.3740 | 7 | slower measured mean |
| order-16 | 25.4329 | 1.1236 | 122.5436 | 7 | slower measured mean |
| order-8 | 46.7030 | 1.4907 | 224.9411 | 7 | slower measured mean |

## btrees/StreamMemory/Lookup/Int32/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 35.1854 | 3.9990 | 185.9040 | 7 | lowest measured mean |
| order-256 | 37.6194 | 1.1639 | 197.4443 | 7 | interval overlaps fastest |
| order-128 | 48.6688 | 0.6584 | 249.7029 | 7 | slower measured mean |
| order-64 | 74.2438 | 7.6419 | 359.4847 | 7 | slower measured mean |
| order-32 | 91.2752 | 3.0463 | 446.3385 | 7 | slower measured mean |
| order-16 | 143.7570 | 1.1555 | 704.3258 | 7 | slower measured mean |
| order-8 | 289.9015 | 16.8978 | 1299.5236 | 7 | slower measured mean |

## btrees/StreamMemory/Lookup/Int32/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 11.6271 | 0.2125 | 62.9656 | 7 | lowest measured mean |
| order-256 | 12.6481 | 0.7479 | 63.9823 | 7 | slower measured mean |
| order-128 | 14.4337 | 0.4849 | 72.6998 | 7 | slower measured mean |
| order-64 | 18.6735 | 0.6156 | 93.4143 | 7 | slower measured mean |
| order-32 | 22.7497 | 2.1079 | 107.5550 | 7 | slower measured mean |
| order-16 | 33.5044 | 0.8412 | 157.5754 | 7 | slower measured mean |
| order-8 | 59.3933 | 1.6040 | 274.2975 | 7 | slower measured mean |

## btrees/StreamMemory/Lookup/Int32/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 58.7016 | 3.3526 | 317.6585 | 7 | lowest measured mean |
| order-256 | 67.5547 | 4.5069 | 340.9404 | 7 | slower measured mean |
| order-128 | 91.3033 | 7.7045 | 444.6450 | 7 | slower measured mean |
| order-64 | 135.6650 | 11.2831 | 656.2028 | 7 | slower measured mean |
| order-32 | 185.7460 | 20.3242 | 801.4836 | 7 | slower measured mean |
| order-16 | 276.2395 | 8.4778 | 1300.6019 | 7 | slower measured mean |
| order-8 | 535.1209 | 23.0158 | 2406.7358 | 7 | slower measured mean |

## btrees/FileStream/BTree/Int64/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-64 | 8.5905 | 0.1614 | 10.7450 | 7 | lowest measured mean |
| order-128 | 8.7530 | 0.1063 | 11.2787 | 7 | interval overlaps fastest |
| order-256 | 10.4061 | 0.2774 | 11.8623 | 7 | slower measured mean |
| order-32 | 10.4099 | 0.1052 | 10.6544 | 7 | slower measured mean |
| order-512 | 11.1211 | 0.2125 | 13.4596 | 7 | slower measured mean |
| order-16 | 11.5072 | 0.1001 | 10.7314 | 7 | slower measured mean |
| order-8 | 16.4609 | 0.2482 | 10.9902 | 7 | slower measured mean |

## btrees/FileStream/BTree/Int64/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-64 | 5.1006 | 0.0954 | 8.5479 | 7 | lowest measured mean |
| order-128 | 5.1865 | 0.1190 | 8.7874 | 7 | interval overlaps fastest |
| order-256 | 6.1797 | 0.0689 | 8.8322 | 7 | slower measured mean |
| order-32 | 6.4756 | 0.1102 | 8.5490 | 7 | slower measured mean |
| order-512 | 6.7266 | 0.1003 | 9.2307 | 7 | slower measured mean |
| order-16 | 7.0723 | 0.0752 | 8.6469 | 7 | slower measured mean |
| order-8 | 10.5374 | 0.0532 | 8.8001 | 7 | slower measured mean |

## btrees/FileStream/BTree/Int64/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-128 | 12.2011 | 0.1102 | 13.7940 | 7 | lowest measured mean |
| order-64 | 13.3004 | 2.2639 | 12.9825 | 7 | interval overlaps fastest |
| order-256 | 14.6718 | 0.1700 | 14.9669 | 7 | slower measured mean |
| order-32 | 14.7916 | 0.2429 | 12.8458 | 7 | slower measured mean |
| order-512 | 15.9191 | 0.3570 | 17.7796 | 7 | slower measured mean |
| order-16 | 16.2630 | 1.6733 | 12.7462 | 7 | slower measured mean |
| order-8 | 22.5633 | 0.3284 | 13.2190 | 7 | slower measured mean |

## btrees/FileStream/BTreePlus/Int64/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 20.0406 | 0.1522 | 15.1425 | 7 | lowest measured mean |
| order-256 | 31.0023 | 1.6008 | 16.7349 | 7 | slower measured mean |
| order-128 | 49.9123 | 1.9522 | 22.0873 | 7 | slower measured mean |
| order-64 | 91.7007 | 1.7984 | 34.6186 | 7 | slower measured mean |
| order-32 | 137.5707 | 2.9153 | 42.8897 | 7 | slower measured mean |
| order-16 | 255.8302 | 2.8594 | 70.1931 | 7 | slower measured mean |
| order-8 | 509.8270 | 4.3678 | 126.6403 | 7 | slower measured mean |

## btrees/FileStream/BTreePlus/Int64/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 7.9347 | 0.0771 | 8.7419 | 7 | lowest measured mean |
| order-256 | 9.4823 | 0.1519 | 8.7938 | 7 | slower measured mean |
| order-128 | 12.8107 | 0.1541 | 9.7081 | 7 | slower measured mean |
| order-64 | 21.1535 | 0.1292 | 11.9880 | 7 | slower measured mean |
| order-32 | 30.5963 | 0.2214 | 13.4453 | 7 | slower measured mean |
| order-16 | 55.8599 | 0.3320 | 18.9113 | 7 | slower measured mean |
| order-8 | 107.9369 | 1.6639 | 30.2017 | 7 | slower measured mean |

## btrees/FileStream/BTreePlus/Int64/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 33.5568 | 1.7159 | 21.6774 | 7 | lowest measured mean |
| order-256 | 55.2936 | 3.2166 | 24.8174 | 7 | slower measured mean |
| order-128 | 88.7255 | 5.1377 | 34.6810 | 7 | slower measured mean |
| order-64 | 170.6003 | 13.9949 | 57.6947 | 7 | slower measured mean |
| order-32 | 244.8196 | 5.9922 | 73.3740 | 7 | slower measured mean |
| order-16 | 473.2111 | 19.2436 | 122.5436 | 7 | slower measured mean |
| order-8 | 933.4487 | 29.4818 | 224.9411 | 7 | slower measured mean |

## btrees/FileStream/Lookup/Int32/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 213.7701 | 5.4143 | 185.9040 | 7 | lowest measured mean |
| order-256 | 306.6037 | 8.3035 | 197.4443 | 7 | slower measured mean |
| order-128 | 489.1873 | 11.1798 | 249.7029 | 7 | slower measured mean |
| order-64 | 867.9155 | 17.0243 | 359.4847 | 7 | slower measured mean |
| order-32 | 1355.4858 | 30.5318 | 446.3385 | 7 | slower measured mean |
| order-16 | 2463.6857 | 13.4386 | 704.3258 | 7 | slower measured mean |
| order-8 | 5139.4279 | 38.1729 | 1299.5236 | 7 | slower measured mean |

## btrees/FileStream/Lookup/Int32/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 60.8561 | 4.5092 | 62.9656 | 7 | lowest measured mean |
| order-256 | 75.1646 | 2.6731 | 63.9823 | 7 | slower measured mean |
| order-128 | 105.3392 | 0.5902 | 72.6998 | 7 | slower measured mean |
| order-64 | 183.9900 | 5.4490 | 93.4143 | 7 | slower measured mean |
| order-32 | 273.4306 | 12.5379 | 107.5550 | 7 | slower measured mean |
| order-16 | 488.8586 | 9.0674 | 157.5754 | 7 | slower measured mean |
| order-8 | 1067.2069 | 77.0032 | 274.2975 | 7 | slower measured mean |

## btrees/FileStream/Lookup/Int32/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 381.7498 | 10.8139 | 317.6585 | 7 | lowest measured mean |
| order-256 | 550.7463 | 11.9016 | 340.9404 | 7 | slower measured mean |
| order-128 | 924.4660 | 16.1677 | 444.6450 | 7 | slower measured mean |
| order-64 | 1635.5127 | 23.0348 | 656.2028 | 7 | slower measured mean |
| order-32 | 2456.7899 | 23.9575 | 801.4836 | 7 | slower measured mean |
| order-16 | 4622.5156 | 24.1329 | 1300.6019 | 7 | slower measured mean |
| order-8 | 9609.9605 | 121.6887 | 2406.7358 | 7 | slower measured mean |

## btrees/FilePaged/BTree/Int64/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-64 | 25.2748 | 1.4221 | 92.5508 | 7 | lowest measured mean |
| order-256 | 27.2772 | 3.2193 | 98.3728 | 7 | interval overlaps fastest |
| order-32 | 28.5587 | 1.1975 | 104.0917 | 7 | slower measured mean |
| order-512 | 29.0701 | 3.2843 | 103.6384 | 7 | interval overlaps fastest |
| order-16 | 29.8858 | 2.4727 | 105.1296 | 7 | slower measured mean |
| order-128 | 31.0331 | 3.4823 | 94.6138 | 7 | slower measured mean |
| order-8 | 37.6441 | 3.0891 | 129.3262 | 7 | slower measured mean |

## btrees/FilePaged/BTree/Int64/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 22.2335 | 1.9731 | 77.7003 | 7 | lowest measured mean |
| order-256 | 22.9707 | 2.1801 | 76.0524 | 7 | interval overlaps fastest |
| order-64 | 22.9844 | 2.8769 | 74.2770 | 7 | interval overlaps fastest |
| order-128 | 23.7820 | 7.3970 | 75.8252 | 7 | interval overlaps fastest |
| order-32 | 24.3809 | 1.8170 | 82.3150 | 7 | interval overlaps fastest |
| order-16 | 24.6478 | 2.2891 | 82.9161 | 7 | interval overlaps fastest |
| order-8 | 27.7770 | 3.1957 | 99.2312 | 7 | slower measured mean |

## btrees/FilePaged/BTree/Int64/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-128 | 31.4759 | 1.7085 | 113.6458 | 7 | lowest measured mean |
| order-64 | 32.1295 | 2.7195 | 111.2373 | 7 | interval overlaps fastest |
| order-512 | 33.2234 | 1.6489 | 130.1320 | 7 | interval overlaps fastest |
| order-256 | 33.5924 | 4.9073 | 121.2842 | 7 | interval overlaps fastest |
| order-16 | 36.0871 | 1.5589 | 126.9128 | 7 | slower measured mean |
| order-32 | 38.7548 | 5.3153 | 126.4525 | 7 | slower measured mean |
| order-8 | 43.5693 | 1.6317 | 159.8079 | 7 | slower measured mean |

## btrees/FilePaged/BTreePlus/Int64/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 38.9149 | 4.9820 | 134.5838 | 7 | lowest measured mean |
| order-256 | 52.2764 | 9.9278 | 166.7125 | 7 | interval overlaps fastest |
| order-128 | 67.2103 | 4.6419 | 232.2359 | 7 | slower measured mean |
| order-64 | 118.8086 | 16.1572 | 380.0950 | 7 | slower measured mean |
| order-32 | 172.0054 | 13.7540 | 548.7700 | 7 | slower measured mean |
| order-16 | 322.5623 | 39.3435 | 1011.2451 | 7 | slower measured mean |
| order-8 | 635.0992 | 63.7527 | 2058.9155 | 7 | slower measured mean |

## btrees/FilePaged/BTreePlus/Int64/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 25.3212 | 2.8624 | 79.5458 | 7 | lowest measured mean |
| order-256 | 25.4655 | 3.7224 | 84.0184 | 7 | interval overlaps fastest |
| order-128 | 30.1555 | 3.7728 | 96.0111 | 7 | interval overlaps fastest |
| order-64 | 44.2836 | 8.7612 | 124.1227 | 7 | slower measured mean |
| order-32 | 49.5503 | 4.5387 | 162.4460 | 7 | slower measured mean |
| order-16 | 85.5064 | 19.8482 | 261.8276 | 7 | slower measured mean |
| order-8 | 140.2196 | 9.9414 | 478.6724 | 7 | slower measured mean |

## btrees/FilePaged/BTreePlus/Int64/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 54.2582 | 3.5028 | 190.8318 | 7 | lowest measured mean |
| order-256 | 68.8765 | 5.3030 | 250.9757 | 7 | slower measured mean |
| order-128 | 109.5379 | 8.2501 | 370.9690 | 7 | slower measured mean |
| order-64 | 187.3927 | 13.6795 | 641.1624 | 7 | slower measured mean |
| order-32 | 268.3951 | 13.5278 | 948.5876 | 7 | slower measured mean |
| order-16 | 485.4110 | 22.0096 | 1775.9190 | 7 | slower measured mean |
| order-8 | 1075.2284 | 42.2412 | 3669.6530 | 7 | slower measured mean |

## btrees/FilePaged/Lookup/Int32/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 427.0810 | 22.6202 | 1680.3742 | 7 | lowest measured mean |
| order-256 | 509.2815 | 24.3949 | 1951.9818 | 7 | slower measured mean |
| order-128 | 688.9366 | 19.6375 | 2596.0851 | 7 | slower measured mean |
| order-64 | 1090.4577 | 57.2375 | 3895.5445 | 7 | slower measured mean |
| order-32 | 1552.5684 | 31.0966 | 5625.6421 | 7 | slower measured mean |
| order-16 | 2680.1266 | 55.7794 | 9914.3189 | 7 | slower measured mean |
| order-8 | 5890.7039 | 298.1259 | 21046.7774 | 7 | slower measured mean |

## btrees/FilePaged/Lookup/Int32/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 155.6913 | 21.1904 | 572.9442 | 7 | lowest measured mean |
| order-256 | 165.7708 | 37.0279 | 616.6692 | 7 | interval overlaps fastest |
| order-128 | 190.4212 | 9.9659 | 731.2772 | 7 | slower measured mean |
| order-64 | 262.2253 | 19.2436 | 977.8888 | 7 | slower measured mean |
| order-32 | 371.8201 | 42.4489 | 1316.7995 | 7 | slower measured mean |
| order-16 | 561.0347 | 21.1610 | 2112.8316 | 7 | slower measured mean |
| order-8 | 1178.8437 | 41.0794 | 4374.6973 | 7 | slower measured mean |

## btrees/FilePaged/Lookup/Int32/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 700.7865 | 50.9889 | 2866.9569 | 7 | lowest measured mean |
| order-256 | 859.7591 | 35.5680 | 3389.4728 | 7 | slower measured mean |
| order-128 | 1238.6080 | 35.7578 | 4664.6462 | 7 | slower measured mean |
| order-64 | 1978.2266 | 157.8174 | 7156.3376 | 7 | slower measured mean |
| order-32 | 2769.8843 | 133.5767 | 10195.8877 | 7 | slower measured mean |
| order-16 | 4914.9954 | 224.2253 | 18363.8612 | 7 | slower measured mean |
| order-8 | 10634.3119 | 505.6327 | 39381.7900 | 7 | slower measured mean |

## btrees/OSMemoryMapped/BTree/Int64/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-128 | 3.0064 | 0.2223 | 11.2787 | 7 | lowest measured mean |
| order-32 | 3.0401 | 0.1306 | 10.6544 | 7 | interval overlaps fastest |
| order-16 | 3.0802 | 0.2308 | 10.7314 | 7 | interval overlaps fastest |
| order-256 | 3.0852 | 0.2668 | 11.8623 | 7 | interval overlaps fastest |
| order-64 | 3.0951 | 0.5416 | 10.7450 | 7 | interval overlaps fastest |
| order-8 | 3.6247 | 0.7065 | 10.9902 | 7 | interval overlaps fastest |
| order-512 | 4.0984 | 0.9578 | 13.4596 | 7 | interval overlaps fastest |

## btrees/OSMemoryMapped/BTree/Int64/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-128 | 2.3410 | 0.0959 | 8.7874 | 7 | lowest measured mean |
| order-256 | 2.3955 | 0.0644 | 8.8322 | 7 | interval overlaps fastest |
| order-64 | 2.4470 | 0.1486 | 8.5479 | 7 | interval overlaps fastest |
| order-16 | 2.5174 | 0.1661 | 8.6469 | 7 | interval overlaps fastest |
| order-32 | 2.5425 | 0.1328 | 8.5490 | 7 | interval overlaps fastest |
| order-512 | 2.6897 | 0.2051 | 9.2307 | 7 | slower measured mean |
| order-8 | 2.9656 | 0.4555 | 8.8001 | 7 | slower measured mean |

## btrees/OSMemoryMapped/BTree/Int64/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-64 | 3.3953 | 0.1102 | 12.9825 | 7 | lowest measured mean |
| order-32 | 3.6818 | 0.2003 | 12.8458 | 7 | interval overlaps fastest |
| order-128 | 3.6834 | 0.2266 | 13.7940 | 7 | interval overlaps fastest |
| order-16 | 3.7794 | 0.3159 | 12.7462 | 7 | interval overlaps fastest |
| order-256 | 3.8478 | 0.3674 | 14.9669 | 7 | interval overlaps fastest |
| order-8 | 4.0465 | 0.2281 | 13.2190 | 7 | slower measured mean |
| order-512 | 4.1587 | 0.1340 | 17.7796 | 7 | slower measured mean |

## btrees/OSMemoryMapped/BTreePlus/Int64/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 4.1792 | 0.2402 | 15.1425 | 7 | lowest measured mean |
| order-256 | 4.6093 | 0.2450 | 16.7349 | 7 | interval overlaps fastest |
| order-128 | 6.1369 | 0.3093 | 22.0873 | 7 | slower measured mean |
| order-64 | 9.9276 | 0.8866 | 34.6186 | 7 | slower measured mean |
| order-32 | 12.5720 | 0.6145 | 42.8897 | 7 | slower measured mean |
| order-16 | 21.4833 | 1.6154 | 70.1931 | 7 | slower measured mean |
| order-8 | 40.9083 | 3.1362 | 126.6403 | 7 | slower measured mean |

## btrees/OSMemoryMapped/BTreePlus/Int64/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 2.5248 | 0.0998 | 8.7419 | 7 | lowest measured mean |
| order-256 | 2.6003 | 0.1224 | 8.7938 | 7 | interval overlaps fastest |
| order-128 | 2.8405 | 0.1639 | 9.7081 | 7 | slower measured mean |
| order-64 | 3.4365 | 0.1160 | 11.9880 | 7 | slower measured mean |
| order-32 | 4.3489 | 0.2679 | 13.4453 | 7 | slower measured mean |
| order-16 | 5.7461 | 0.1980 | 18.9113 | 7 | slower measured mean |
| order-8 | 9.8526 | 0.5443 | 30.2017 | 7 | slower measured mean |

## btrees/OSMemoryMapped/BTreePlus/Int64/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 5.4695 | 0.2688 | 21.6774 | 7 | lowest measured mean |
| order-256 | 6.6423 | 0.3996 | 24.8174 | 7 | slower measured mean |
| order-128 | 9.7312 | 0.7175 | 34.6810 | 7 | slower measured mean |
| order-64 | 15.5390 | 0.6326 | 57.6947 | 7 | slower measured mean |
| order-32 | 23.1181 | 3.7416 | 73.3740 | 7 | slower measured mean |
| order-16 | 37.9764 | 3.9060 | 122.5436 | 7 | slower measured mean |
| order-8 | 72.5978 | 4.6618 | 224.9411 | 7 | slower measured mean |

## btrees/OSMemoryMapped/Lookup/Int32/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 48.5688 | 5.3198 | 185.9040 | 7 | lowest measured mean |
| order-256 | 48.8363 | 2.0793 | 197.4443 | 7 | interval overlaps fastest |
| order-128 | 66.1174 | 4.0039 | 249.7029 | 7 | slower measured mean |
| order-64 | 100.5246 | 6.2354 | 359.4847 | 7 | slower measured mean |
| order-32 | 125.6357 | 4.9103 | 446.3385 | 7 | slower measured mean |
| order-16 | 210.9167 | 7.0715 | 704.3258 | 7 | slower measured mean |
| order-8 | 420.2545 | 16.0061 | 1299.5236 | 7 | slower measured mean |

## btrees/OSMemoryMapped/Lookup/Int32/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 14.7572 | 0.7301 | 62.9656 | 7 | lowest measured mean |
| order-256 | 16.4858 | 1.3533 | 63.9823 | 7 | interval overlaps fastest |
| order-128 | 18.6512 | 0.8299 | 72.6998 | 7 | slower measured mean |
| order-64 | 25.7566 | 1.9909 | 93.4143 | 7 | slower measured mean |
| order-32 | 31.4118 | 1.8398 | 107.5550 | 7 | slower measured mean |
| order-16 | 46.5031 | 2.7541 | 157.5754 | 7 | slower measured mean |
| order-8 | 89.1415 | 3.0583 | 274.2975 | 7 | slower measured mean |

## btrees/OSMemoryMapped/Lookup/Int32/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 71.3739 | 6.0687 | 317.6585 | 7 | lowest measured mean |
| order-256 | 84.2557 | 5.0445 | 340.9404 | 7 | slower measured mean |
| order-128 | 116.3360 | 11.0557 | 444.6450 | 7 | slower measured mean |
| order-64 | 173.5052 | 5.8515 | 656.2028 | 7 | slower measured mean |
| order-32 | 229.1241 | 14.0286 | 801.4836 | 7 | slower measured mean |
| order-16 | 385.9996 | 9.7937 | 1300.6019 | 7 | slower measured mean |
| order-8 | 790.0546 | 13.9849 | 2406.7358 | 7 | slower measured mean |

## btrees/TransactionalFile/BTree/Int64/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-64 | 32.3075 | 3.8651 | 92.5992 | 7 | lowest measured mean |
| order-256 | 32.7185 | 4.8318 | 98.4212 | 7 | interval overlaps fastest |
| order-128 | 34.2989 | 3.7935 | 94.6623 | 7 | interval overlaps fastest |
| order-32 | 35.3921 | 3.4714 | 104.1401 | 7 | interval overlaps fastest |
| order-16 | 36.1483 | 3.2946 | 105.1784 | 7 | interval overlaps fastest |
| order-512 | 36.3827 | 3.5000 | 103.6868 | 7 | interval overlaps fastest |
| order-8 | 42.1550 | 4.6610 | 129.3746 | 7 | slower measured mean |

## btrees/TransactionalFile/BTree/Int64/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-256 | 24.6311 | 1.5911 | 76.1007 | 7 | lowest measured mean |
| order-64 | 24.8271 | 2.2423 | 74.3253 | 7 | interval overlaps fastest |
| order-512 | 26.1556 | 1.8566 | 77.7487 | 7 | interval overlaps fastest |
| order-32 | 27.1112 | 1.8109 | 82.3634 | 7 | interval overlaps fastest |
| order-128 | 27.1801 | 2.9533 | 75.8732 | 7 | interval overlaps fastest |
| order-16 | 28.8926 | 3.7233 | 82.9644 | 7 | interval overlaps fastest |
| order-8 | 33.2388 | 3.1164 | 99.2796 | 7 | slower measured mean |

## btrees/TransactionalFile/BTree/Int64/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-128 | 39.1914 | 3.3685 | 113.6944 | 7 | lowest measured mean |
| order-64 | 39.8807 | 4.5298 | 111.2858 | 7 | interval overlaps fastest |
| order-512 | 41.6101 | 4.1771 | 130.1805 | 7 | interval overlaps fastest |
| order-256 | 42.1840 | 6.2425 | 121.3327 | 7 | interval overlaps fastest |
| order-16 | 43.4810 | 4.8196 | 126.9614 | 7 | interval overlaps fastest |
| order-32 | 46.7146 | 7.5603 | 126.5009 | 7 | interval overlaps fastest |
| order-8 | 52.8020 | 4.3563 | 159.8561 | 7 | slower measured mean |

## btrees/TransactionalFile/BTreePlus/Int64/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 45.0242 | 4.7303 | 134.6324 | 7 | lowest measured mean |
| order-256 | 54.7996 | 4.7013 | 166.7610 | 7 | slower measured mean |
| order-128 | 75.2744 | 5.0475 | 232.2844 | 7 | slower measured mean |
| order-64 | 119.7509 | 7.5687 | 380.1434 | 7 | slower measured mean |
| order-32 | 178.3839 | 17.8763 | 548.8184 | 7 | slower measured mean |
| order-16 | 321.5074 | 16.1483 | 1011.2938 | 7 | slower measured mean |
| order-8 | 646.1137 | 34.8653 | 2058.9641 | 7 | slower measured mean |

## btrees/TransactionalFile/BTreePlus/Int64/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 26.1500 | 0.9087 | 79.5942 | 7 | lowest measured mean |
| order-256 | 30.3733 | 3.4144 | 84.0667 | 7 | interval overlaps fastest |
| order-128 | 35.5579 | 2.3681 | 96.0597 | 7 | slower measured mean |
| order-64 | 42.7221 | 2.5701 | 124.1717 | 7 | slower measured mean |
| order-32 | 58.8327 | 4.9734 | 162.4942 | 7 | slower measured mean |
| order-16 | 83.5142 | 6.3366 | 261.8761 | 7 | slower measured mean |
| order-8 | 153.7904 | 11.8253 | 478.7207 | 7 | slower measured mean |

## btrees/TransactionalFile/BTreePlus/Int64/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 60.2884 | 4.8274 | 190.8803 | 7 | lowest measured mean |
| order-256 | 78.2036 | 6.1802 | 251.0243 | 7 | slower measured mean |
| order-128 | 121.5317 | 5.6961 | 371.0173 | 7 | slower measured mean |
| order-64 | 202.4078 | 17.1798 | 641.2106 | 7 | slower measured mean |
| order-32 | 294.9463 | 14.4682 | 948.6360 | 7 | slower measured mean |
| order-16 | 551.9622 | 21.4875 | 1775.9675 | 7 | slower measured mean |
| order-8 | 1140.0391 | 28.8686 | 3669.7014 | 7 | slower measured mean |

## btrees/TransactionalFile/Lookup/Int32/Mixed/balanced/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 439.2571 | 19.5207 | 1680.4227 | 7 | lowest measured mean |
| order-256 | 519.5675 | 16.7080 | 1952.0302 | 7 | slower measured mean |
| order-128 | 726.4599 | 17.5928 | 2596.1335 | 7 | slower measured mean |
| order-64 | 1136.5514 | 24.9625 | 3895.5928 | 7 | slower measured mean |
| order-32 | 1638.8281 | 12.8321 | 5625.6902 | 7 | slower measured mean |
| order-16 | 2812.5538 | 30.6482 | 9914.3675 | 7 | slower measured mean |
| order-8 | 5907.3126 | 63.7783 | 21046.8255 | 7 | slower measured mean |

## btrees/TransactionalFile/Lookup/Int32/Mixed/read-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 151.9470 | 8.6940 | 572.9922 | 7 | lowest measured mean |
| order-256 | 169.1505 | 6.3896 | 616.7178 | 7 | slower measured mean |
| order-128 | 204.7342 | 4.0625 | 731.3254 | 7 | slower measured mean |
| order-64 | 285.1301 | 16.5769 | 977.9373 | 7 | slower measured mean |
| order-32 | 375.3347 | 15.3221 | 1316.8479 | 7 | slower measured mean |
| order-16 | 599.8314 | 7.5371 | 2112.8799 | 7 | slower measured mean |
| order-8 | 1315.0639 | 22.2647 | 4374.7451 | 7 | slower measured mean |

## btrees/TransactionalFile/Lookup/Int32/Mixed/write-heavy/records-2048/ops-512/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-512 | 765.8338 | 67.1381 | 2867.0052 | 7 | lowest measured mean |
| order-256 | 933.7096 | 60.6593 | 3389.5212 | 7 | slower measured mean |
| order-128 | 1378.8300 | 51.4861 | 4664.6947 | 7 | slower measured mean |
| order-64 | 2094.2256 | 74.7114 | 7156.3853 | 7 | slower measured mean |
| order-32 | 2996.2586 | 106.1752 | 10195.9363 | 7 | slower measured mean |
| order-16 | 5305.0052 | 222.4253 | 18363.9096 | 7 | slower measured mean |
| order-8 | 11515.6762 | 375.4401 | 39381.8390 | 7 | slower measured mean |

