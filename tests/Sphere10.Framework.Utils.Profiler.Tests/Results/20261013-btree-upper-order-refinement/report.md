# Parameter study: measured screening candidates

Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.
Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.
Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.

Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.

## btrees/FileStream/BTreePlus/Int64/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-4096 | 47.7769 | 2.9417 | 71.8667 | 3 | lowest measured mean |
| upper-order-2048 | 49.9656 | 9.7661 | 50.9778 | 3 | interval overlaps fastest |

## btrees/FileStream/BTreePlus/Int64/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 24.0211 | 11.9631 | 23.6767 | 3 | lowest measured mean |
| upper-order-4096 | 29.3946 | 23.8026 | 28.3893 | 3 | interval overlaps fastest |

## btrees/FileStream/BTreePlus/Int64/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-4096 | 73.3844 | 3.5211 | 115.3430 | 3 | lowest measured mean |
| upper-order-2048 | 80.9032 | 19.3439 | 78.2928 | 3 | interval overlaps fastest |

## btrees/OSMemoryMapped/BTreePlus/Int64/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 10.7106 | 0.8134 | 50.9778 | 3 | lowest measured mean |
| upper-order-4096 | 54.4138 | 68.4376 | 71.8370 | 3 | interval overlaps fastest |

## btrees/OSMemoryMapped/BTreePlus/Int64/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 6.4697 | 1.3476 | 23.6767 | 3 | lowest measured mean |
| upper-order-4096 | 29.2364 | 27.8615 | 28.3872 | 3 | interval overlaps fastest |

## btrees/OSMemoryMapped/BTreePlus/Int64/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 15.5660 | 0.9323 | 78.2928 | 3 | lowest measured mean |
| upper-order-4096 | 39.2705 | 56.4356 | 115.3106 | 3 | interval overlaps fastest |

## btrees/TransactionalFile/BTreePlus/Int64/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 88.2002 | 27.1715 | 353.0816 | 3 | lowest measured mean |
| upper-order-4096 | 506.6820 | 40.6334 | 409.6506 | 3 | slower measured mean |

## btrees/TransactionalFile/BTreePlus/Int64/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 52.5284 | 6.2911 | 191.1189 | 3 | lowest measured mean |
| upper-order-4096 | 127.4004 | 6.3503 | 206.7006 | 3 | slower measured mean |

## btrees/TransactionalFile/BTreePlus/Int64/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 116.3405 | 11.1534 | 516.2731 | 3 | lowest measured mean |
| upper-order-4096 | 534.4863 | 140.4829 | 612.5215 | 3 | slower measured mean |

## btrees/FilePaged/BTree/Int64/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-256 | 57.1193 | 5.7869 | 220.7062 | 3 | lowest measured mean |
| upper-order-512 | 57.6860 | 3.4058 | 229.3978 | 3 | interval overlaps fastest |
| upper-order-1024 | 65.7838 | 24.6273 | 250.6416 | 3 | interval overlaps fastest |
| upper-order-2048 | 71.1184 | 33.4136 | 295.5702 | 3 | interval overlaps fastest |
| upper-order-4096 | 358.3529 | 271.9893 | 387.1282 | 3 | slower measured mean |

## btrees/FilePaged/BTreePlus/Int64/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 79.4984 | 4.1700 | 352.9439 | 3 | lowest measured mean |
| upper-order-4096 | 356.9355 | 6.4346 | 409.4937 | 3 | slower measured mean |

## btrees/FilePaged/BTreePlus/Int64/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 53.3132 | 11.6432 | 190.9816 | 3 | lowest measured mean |
| upper-order-4096 | 127.2559 | 43.4299 | 206.5087 | 3 | slower measured mean |

## btrees/FilePaged/BTreePlus/Int64/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 114.1062 | 7.8691 | 516.1358 | 3 | lowest measured mean |
| upper-order-4096 | 468.4095 | 586.1228 | 612.3890 | 3 | interval overlaps fastest |

## btrees/FileStream/Lookup/Int32/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 512.9006 | 17.3937 | 577.0644 | 3 | lowest measured mean |
| upper-order-4096 | 615.6800 | 4.2704 | 773.3513 | 3 | slower measured mean |

## btrees/FileStream/Lookup/Int32/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 154.3768 | 9.7405 | 178.9792 | 3 | lowest measured mean |
| upper-order-4096 | 212.3687 | 6.5073 | 222.5309 | 3 | slower measured mean |

## btrees/FileStream/Lookup/Int32/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 892.2655 | 13.2281 | 994.7208 | 3 | lowest measured mean |
| upper-order-4096 | 1324.3825 | 82.4528 | 1353.9579 | 3 | slower measured mean |

## btrees/OSMemoryMapped/Lookup/Int32/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 108.3980 | 15.7594 | 577.0644 | 3 | lowest measured mean |
| upper-order-4096 | 423.3454 | 45.2220 | 773.3513 | 3 | slower measured mean |

## btrees/OSMemoryMapped/Lookup/Int32/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 37.7079 | 8.6550 | 178.9792 | 3 | lowest measured mean |
| upper-order-4096 | 110.9748 | 10.2261 | 222.5309 | 3 | slower measured mean |

## btrees/OSMemoryMapped/Lookup/Int32/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 176.5305 | 11.3626 | 994.7208 | 3 | lowest measured mean |
| upper-order-4096 | 737.2809 | 19.0012 | 1353.9579 | 3 | slower measured mean |

## btrees/TransactionalFile/Lookup/Int32/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 918.2294 | 88.3268 | 4248.9826 | 3 | lowest measured mean |
| upper-order-4096 | 3392.8245 | 391.6876 | 4800.1825 | 3 | slower measured mean |

## btrees/TransactionalFile/Lookup/Int32/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 311.5899 | 7.9124 | 1410.8943 | 3 | lowest measured mean |
| upper-order-4096 | 812.7795 | 282.3851 | 1543.5817 | 3 | slower measured mean |

## btrees/TransactionalFile/Lookup/Int32/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 1527.6840 | 31.3224 | 7220.6280 | 3 | lowest measured mean |
| upper-order-4096 | 5918.1770 | 911.2601 | 8215.8568 | 3 | slower measured mean |

## btrees/FilePaged/Lookup/Int32/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 973.1929 | 60.9470 | 4248.8448 | 3 | lowest measured mean |
| upper-order-4096 | 1666.4890 | 62.4125 | 4799.0972 | 3 | slower measured mean |

## btrees/FilePaged/Lookup/Int32/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 330.1097 | 24.9566 | 1410.7570 | 3 | lowest measured mean |
| upper-order-4096 | 477.9548 | 23.4497 | 1543.2271 | 3 | slower measured mean |

## btrees/FilePaged/Lookup/Int32/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 1676.0529 | 173.0975 | 7220.4800 | 3 | lowest measured mean |
| upper-order-4096 | 3001.1387 | 443.7811 | 8214.0180 | 3 | slower measured mean |

## btrees/ManagedNodes/BTreePlus/Int64/Mixed/balanced/records-32768/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 0.5483 | 0.9954 | 0.0000 | 3 | lowest measured mean |
| upper-order-4096 | 0.7058 | 1.3696 | 0.0000 | 3 | interval overlaps fastest |
| upper-order-1024 | 1.0267 | 3.1046 | 0.0000 | 3 | interval overlaps fastest |

## btrees/ManagedNodes/BTreePlus/Int64/Mixed/read-heavy/records-32768/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-1024 | 0.1533 | 0.0019 | 0.0000 | 3 | lowest measured mean |
| upper-order-2048 | 0.1624 | 0.0349 | 0.0000 | 3 | interval overlaps fastest |
| upper-order-4096 | 0.1752 | 0.0020 | 0.0000 | 3 | slower measured mean |

## btrees/ManagedNodes/BTreePlus/Int64/Mixed/write-heavy/records-32768/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-1024 | 0.4571 | 0.0126 | 0.0000 | 3 | lowest measured mean |
| upper-order-2048 | 0.4778 | 0.0758 | 0.0000 | 3 | interval overlaps fastest |
| upper-order-4096 | 0.6040 | 0.0340 | 0.0000 | 3 | slower measured mean |

## btrees/ManagedNodes/Lookup/Int32/Mixed/balanced/records-32768/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 2.6050 | 0.1174 | 0.0000 | 3 | lowest measured mean |
| upper-order-1024 | 2.7325 | 0.0689 | 0.0000 | 3 | interval overlaps fastest |
| upper-order-4096 | 3.3684 | 0.1555 | 0.0000 | 3 | slower measured mean |

## btrees/ManagedNodes/Lookup/Int32/Mixed/read-heavy/records-32768/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 0.8171 | 0.0108 | 0.0000 | 3 | lowest measured mean |
| upper-order-1024 | 0.8686 | 0.0452 | 0.0000 | 3 | interval overlaps fastest |
| upper-order-4096 | 0.9734 | 0.0130 | 0.0000 | 3 | slower measured mean |

## btrees/ManagedNodes/Lookup/Int32/Mixed/write-heavy/records-32768/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 4.3446 | 0.1400 | 0.0000 | 3 | lowest measured mean |
| upper-order-1024 | 4.5929 | 0.0399 | 0.0000 | 3 | slower measured mean |
| upper-order-4096 | 5.6562 | 0.2204 | 0.0000 | 3 | slower measured mean |

## btrees/StreamMemory/BTreePlus/Int64/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 7.9067 | 1.0073 | 50.9778 | 3 | lowest measured mean |
| upper-order-4096 | 67.0846 | 82.3714 | 71.8405 | 3 | interval overlaps fastest |

## btrees/StreamMemory/BTreePlus/Int64/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 4.0850 | 0.7267 | 23.6767 | 3 | lowest measured mean |
| upper-order-4096 | 34.8197 | 46.8333 | 28.3862 | 3 | interval overlaps fastest |

## btrees/StreamMemory/BTreePlus/Int64/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 11.4044 | 2.4239 | 78.2928 | 3 | lowest measured mean |
| upper-order-4096 | 30.3368 | 26.4012 | 115.3056 | 3 | interval overlaps fastest |

## btrees/StreamMemory/Lookup/Int32/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 91.3295 | 0.9248 | 577.0644 | 3 | lowest measured mean |
| upper-order-4096 | 1416.9128 | 155.5081 | 773.4946 | 3 | slower measured mean |

## btrees/StreamMemory/Lookup/Int32/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 30.7294 | 3.0214 | 178.9792 | 3 | lowest measured mean |
| upper-order-4096 | 381.3268 | 20.3253 | 222.5363 | 3 | slower measured mean |

## btrees/StreamMemory/Lookup/Int32/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True/factor-UpperOrderRefinement

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| upper-order-2048 | 152.0140 | 5.7283 | 994.7208 | 3 | lowest measured mean |
| upper-order-4096 | 2521.7570 | 532.0163 | 1354.1991 | 3 | slower measured mean |

