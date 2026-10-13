# Parameter study: measured screening candidates

Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.
Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.
Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.

Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.

## btrees/FileStream/BTree/Int64/Mixed/balanced/records-2048/ops-100/group-8/shuffled-True/factor-FileBuffer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 1.8783 | 0.5388 | 2.0930 | 3 | lowest measured mean |
| buffer-65536 | 2.0197 | 0.8354 | 2.0930 | 3 | interval overlaps fastest |
| buffer-16384 | 2.0606 | 0.3356 | 2.0930 | 3 | interval overlaps fastest |
| buffer-1 | 5.9521 | 0.1719 | 2.0930 | 3 | slower measured mean |

## btrees/FileStream/BTree/Int64/Mixed/read-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileBuffer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| buffer-65536 | 1.0412 | 0.4173 | 1.6587 | 3 | lowest measured mean |
| buffer-16384 | 1.0616 | 0.1426 | 1.6587 | 3 | interval overlaps fastest |
| baseline | 1.3468 | 0.5926 | 1.6587 | 3 | interval overlaps fastest |
| buffer-1 | 4.4457 | 0.2531 | 1.6587 | 3 | slower measured mean |

## btrees/FileStream/BTree/Int64/Mixed/write-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileBuffer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 2.5014 | 0.0734 | 2.6942 | 3 | lowest measured mean |
| buffer-16384 | 2.6562 | 0.1482 | 2.6942 | 3 | interval overlaps fastest |
| buffer-65536 | 2.9030 | 0.3156 | 2.6942 | 3 | slower measured mean |
| buffer-1 | 7.4523 | 0.2802 | 2.6942 | 3 | slower measured mean |

## btrees/FileStream/BTreePlus/Int64/Mixed/balanced/records-2048/ops-100/group-8/shuffled-True/factor-FileBuffer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| buffer-16384 | 3.7820 | 0.1304 | 2.9700 | 3 | lowest measured mean |
| buffer-65536 | 3.7974 | 0.2300 | 2.9700 | 3 | interval overlaps fastest |
| baseline | 4.1070 | 0.2446 | 2.9700 | 3 | interval overlaps fastest |
| buffer-1 | 8.5160 | 0.8535 | 2.9700 | 3 | slower measured mean |

## btrees/FileStream/BTreePlus/Int64/Mixed/read-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileBuffer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| buffer-65536 | 1.1093 | 0.1316 | 1.7019 | 3 | lowest measured mean |
| buffer-16384 | 1.3444 | 0.1055 | 1.7019 | 3 | interval overlaps fastest |
| baseline | 1.6242 | 0.4610 | 1.7019 | 3 | interval overlaps fastest |
| buffer-1 | 4.8458 | 0.4939 | 1.7019 | 3 | slower measured mean |

## btrees/FileStream/BTreePlus/Int64/Mixed/write-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileBuffer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| buffer-16384 | 6.2513 | 0.4481 | 4.2367 | 3 | lowest measured mean |
| baseline | 6.5900 | 0.3915 | 4.2367 | 3 | interval overlaps fastest |
| buffer-65536 | 7.0740 | 2.9345 | 4.2367 | 3 | interval overlaps fastest |
| buffer-1 | 12.0460 | 0.3658 | 4.2367 | 3 | slower measured mean |

## btrees/FileStream/Lookup/Int32/Mixed/balanced/records-2048/ops-100/group-8/shuffled-True/factor-FileBuffer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| buffer-65536 | 30.6453 | 3.2539 | 33.3724 | 3 | lowest measured mean |
| buffer-16384 | 34.1915 | 1.5848 | 33.3724 | 3 | interval overlaps fastest |
| baseline | 39.0966 | 3.2907 | 33.3724 | 3 | slower measured mean |
| buffer-1 | 96.4757 | 15.7979 | 33.3724 | 3 | slower measured mean |

## btrees/FileStream/Lookup/Int32/Mixed/read-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileBuffer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| buffer-65536 | 7.8050 | 1.6486 | 12.3454 | 3 | lowest measured mean |
| buffer-16384 | 9.7370 | 0.3902 | 12.3454 | 3 | interval overlaps fastest |
| baseline | 12.6076 | 4.3016 | 12.3454 | 3 | interval overlaps fastest |
| buffer-1 | 34.7706 | 2.6434 | 12.3454 | 3 | slower measured mean |

## btrees/FileStream/Lookup/Int32/Mixed/write-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileBuffer

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| buffer-65536 | 59.7540 | 18.4698 | 57.7642 | 3 | lowest measured mean |
| buffer-16384 | 66.2675 | 5.8768 | 57.7642 | 3 | interval overlaps fastest |
| baseline | 67.9859 | 1.4039 | 57.7642 | 3 | interval overlaps fastest |
| buffer-1 | 164.8601 | 10.7651 | 57.7642 | 3 | slower measured mean |

## btrees/FilePaged/BTree/Int64/Mixed/balanced/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-262144 | 6.7329 | 0.6209 | 18.0449 | 3 | lowest measured mean |
| page-1048576 | 6.8597 | 1.1883 | 18.0449 | 3 | interval overlaps fastest |
| page-16384 | 7.7247 | 4.4791 | 18.0531 | 3 | interval overlaps fastest |
| baseline | 8.1255 | 6.1343 | 18.0453 | 3 | interval overlaps fastest |
| page-4096 | 8.5771 | 6.8157 | 18.0750 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTree/Int64/Mixed/read-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-4096 | 6.0837 | 0.2199 | 15.1414 | 3 | lowest measured mean |
| page-16384 | 6.1034 | 0.9504 | 14.9935 | 3 | interval overlaps fastest |
| baseline | 6.1160 | 1.7995 | 14.9936 | 3 | interval overlaps fastest |
| page-262144 | 6.1701 | 0.6388 | 15.0038 | 3 | interval overlaps fastest |
| page-1048576 | 7.6336 | 4.2041 | 15.0033 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTree/Int64/Mixed/write-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-1048576 | 7.4410 | 0.4778 | 22.1654 | 3 | lowest measured mean |
| page-16384 | 8.5600 | 4.0069 | 22.1820 | 3 | interval overlaps fastest |
| page-4096 | 8.7831 | 2.2173 | 22.2446 | 3 | interval overlaps fastest |
| page-262144 | 9.0493 | 6.3441 | 22.1654 | 3 | interval overlaps fastest |
| baseline | 9.6154 | 6.2682 | 22.1679 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTree/Int64/Mixed/balanced/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| cache-4194304 | 6.6193 | 0.2413 | 18.0450 | 3 | lowest measured mean |
| cache-262144 | 6.8326 | 0.9628 | 18.0455 | 3 | interval overlaps fastest |
| baseline | 7.0036 | 0.9345 | 18.0455 | 3 | interval overlaps fastest |
| cache-65536 | 7.4451 | 0.9226 | 20.1285 | 3 | interval overlaps fastest |
| cache-1048576 | 8.1982 | 5.3915 | 18.0455 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTree/Int64/Mixed/read-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| cache-1048576 | 5.6566 | 0.4513 | 14.9934 | 3 | lowest measured mean |
| cache-4194304 | 6.6469 | 4.1278 | 14.9934 | 3 | interval overlaps fastest |
| cache-262144 | 6.8937 | 4.8679 | 14.9934 | 3 | interval overlaps fastest |
| baseline | 7.8837 | 9.0648 | 14.9936 | 3 | interval overlaps fastest |
| cache-65536 | 8.7723 | 4.5124 | 21.7584 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTree/Int64/Mixed/write-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 7.5954 | 0.5558 | 22.1679 | 3 | lowest measured mean |
| cache-4194304 | 7.7288 | 1.0122 | 22.1682 | 3 | interval overlaps fastest |
| cache-1048576 | 8.2071 | 1.5319 | 22.1682 | 3 | interval overlaps fastest |
| cache-262144 | 10.2065 | 3.6354 | 22.1679 | 3 | interval overlaps fastest |
| cache-65536 | 10.2418 | 5.0013 | 24.6430 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTreePlus/Int64/Mixed/balanced/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-1048576 | 8.6640 | 0.8758 | 26.4163 | 3 | lowest measured mean |
| page-16384 | 8.8576 | 1.2226 | 26.3714 | 3 | interval overlaps fastest |
| page-262144 | 9.0163 | 1.3668 | 26.4161 | 3 | interval overlaps fastest |
| page-4096 | 10.3928 | 2.5871 | 26.9971 | 3 | interval overlaps fastest |
| baseline | 10.7976 | 3.4123 | 26.4052 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTreePlus/Int64/Mixed/read-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-262144 | 5.9061 | 0.9425 | 15.4896 | 3 | lowest measured mean |
| baseline | 6.3167 | 0.2184 | 15.4905 | 3 | interval overlaps fastest |
| page-16384 | 6.3524 | 1.0534 | 15.4896 | 3 | interval overlaps fastest |
| page-1048576 | 6.6838 | 3.3897 | 15.4893 | 3 | interval overlaps fastest |
| page-4096 | 8.4875 | 3.6710 | 15.6207 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTreePlus/Int64/Mixed/write-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-1048576 | 12.6151 | 3.5359 | 37.3319 | 3 | lowest measured mean |
| page-4096 | 13.8731 | 4.1744 | 38.3412 | 3 | interval overlaps fastest |
| page-16384 | 14.9354 | 11.3227 | 37.2526 | 3 | interval overlaps fastest |
| page-262144 | 15.3609 | 13.1800 | 37.3317 | 3 | interval overlaps fastest |
| baseline | 15.7052 | 5.8142 | 37.3156 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTreePlus/Int64/Mixed/balanced/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| cache-1048576 | 10.2709 | 2.7097 | 26.4057 | 3 | lowest measured mean |
| cache-4194304 | 10.4185 | 7.9512 | 26.4054 | 3 | interval overlaps fastest |
| cache-262144 | 10.5304 | 2.7617 | 26.4054 | 3 | interval overlaps fastest |
| baseline | 11.7044 | 4.3297 | 26.4057 | 3 | interval overlaps fastest |
| cache-65536 | 14.0057 | 2.3453 | 39.4240 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTreePlus/Int64/Mixed/read-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 6.2308 | 0.9841 | 15.4900 | 3 | lowest measured mean |
| cache-4194304 | 6.9616 | 3.0633 | 15.4903 | 3 | interval overlaps fastest |
| cache-262144 | 7.4115 | 3.9148 | 15.4903 | 3 | interval overlaps fastest |
| cache-1048576 | 8.5621 | 9.5049 | 15.4903 | 3 | interval overlaps fastest |
| cache-65536 | 8.5994 | 3.3234 | 19.1340 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTreePlus/Int64/Mixed/write-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| cache-1048576 | 11.7389 | 0.3287 | 37.3156 | 3 | lowest measured mean |
| cache-262144 | 13.0527 | 3.8672 | 37.3158 | 3 | interval overlaps fastest |
| cache-4194304 | 13.7978 | 1.7217 | 37.3156 | 3 | slower measured mean |
| baseline | 13.8654 | 5.9617 | 37.3158 | 3 | interval overlaps fastest |
| cache-65536 | 21.5888 | 6.3388 | 58.9271 | 3 | slower measured mean |

## btrees/FilePaged/Lookup/Int32/Mixed/balanced/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-1048576 | 85.3914 | 7.6622 | 301.7747 | 3 | lowest measured mean |
| page-262144 | 90.1827 | 8.7373 | 301.7745 | 3 | interval overlaps fastest |
| baseline | 92.2680 | 18.8329 | 301.6935 | 3 | interval overlaps fastest |
| page-4096 | 92.9018 | 9.0737 | 306.0438 | 3 | interval overlaps fastest |
| page-16384 | 98.8207 | 7.4789 | 301.3696 | 3 | interval overlaps fastest |

## btrees/FilePaged/Lookup/Int32/Mixed/read-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-262144 | 32.3630 | 6.0855 | 112.0033 | 3 | lowest measured mean |
| page-1048576 | 35.1116 | 10.3856 | 112.0033 | 3 | interval overlaps fastest |
| page-4096 | 35.9658 | 9.0567 | 112.9883 | 3 | interval overlaps fastest |
| page-16384 | 37.3101 | 12.9986 | 111.8880 | 3 | interval overlaps fastest |
| baseline | 37.8281 | 16.6282 | 111.9768 | 3 | interval overlaps fastest |

## btrees/FilePaged/Lookup/Int32/Mixed/write-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-262144 | 150.1480 | 22.6022 | 521.9089 | 3 | lowest measured mean |
| page-1048576 | 156.1490 | 40.4669 | 521.9087 | 3 | interval overlaps fastest |
| baseline | 158.4130 | 27.7862 | 521.6462 | 3 | interval overlaps fastest |
| page-16384 | 163.4719 | 42.9309 | 521.0037 | 3 | interval overlaps fastest |
| page-4096 | 167.2415 | 71.2304 | 530.2014 | 3 | interval overlaps fastest |

## btrees/FilePaged/Lookup/Int32/Mixed/balanced/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| cache-4194304 | 80.1434 | 17.6875 | 301.6935 | 3 | lowest measured mean |
| cache-1048576 | 91.6235 | 35.9493 | 301.6933 | 3 | interval overlaps fastest |
| cache-262144 | 92.4655 | 15.0707 | 301.6933 | 3 | interval overlaps fastest |
| baseline | 93.8685 | 42.6915 | 301.6933 | 3 | interval overlaps fastest |
| cache-65536 | 119.1743 | 16.1779 | 409.6099 | 3 | slower measured mean |

## btrees/FilePaged/Lookup/Int32/Mixed/read-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 30.0917 | 3.2706 | 111.9768 | 3 | lowest measured mean |
| cache-262144 | 30.2750 | 6.3128 | 111.9765 | 3 | interval overlaps fastest |
| cache-1048576 | 30.4894 | 7.7590 | 111.9765 | 3 | interval overlaps fastest |
| cache-4194304 | 33.1722 | 8.1747 | 111.9768 | 3 | interval overlaps fastest |
| cache-65536 | 43.1196 | 11.6670 | 149.8507 | 3 | interval overlaps fastest |

## btrees/FilePaged/Lookup/Int32/Mixed/write-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| cache-4194304 | 131.1165 | 15.8361 | 521.6462 | 3 | lowest measured mean |
| cache-262144 | 136.1133 | 41.0448 | 521.6462 | 3 | interval overlaps fastest |
| baseline | 137.9280 | 20.2701 | 521.6462 | 3 | interval overlaps fastest |
| cache-1048576 | 139.9327 | 23.0635 | 521.6462 | 3 | interval overlaps fastest |
| cache-65536 | 208.5101 | 22.3422 | 750.2330 | 3 | slower measured mean |

## btrees/TransactionalFile/BTree/Int64/Mixed/balanced/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-1048576 | 9.2752 | 1.2667 | 18.0714 | 3 | lowest measured mean |
| page-262144 | 10.0880 | 5.3239 | 18.0710 | 3 | interval overlaps fastest |
| baseline | 10.1762 | 0.2773 | 18.0940 | 3 | interval overlaps fastest |
| page-16384 | 12.5511 | 1.3128 | 18.1677 | 3 | slower measured mean |
| page-4096 | 25.0924 | 5.2710 | 18.4954 | 3 | slower measured mean |

## btrees/TransactionalFile/BTree/Int64/Mixed/read-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-262144 | 9.3153 | 6.4304 | 14.8964 | 3 | lowest measured mean |
| page-1048576 | 9.8093 | 4.3499 | 14.8967 | 3 | interval overlaps fastest |
| baseline | 9.8631 | 3.9195 | 14.9194 | 3 | interval overlaps fastest |
| page-16384 | 13.1932 | 5.0617 | 14.9893 | 3 | interval overlaps fastest |
| page-4096 | 21.3656 | 3.4466 | 15.3234 | 3 | slower measured mean |

## btrees/TransactionalFile/BTree/Int64/Mixed/write-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-262144 | 10.4529 | 0.8416 | 22.1918 | 3 | lowest measured mean |
| page-1048576 | 11.3779 | 3.7942 | 22.1919 | 3 | interval overlaps fastest |
| baseline | 12.4261 | 3.2342 | 22.2166 | 3 | interval overlaps fastest |
| page-16384 | 14.0308 | 0.8839 | 22.2970 | 3 | slower measured mean |
| page-4096 | 25.6353 | 0.6936 | 22.6807 | 3 | slower measured mean |

## btrees/TransactionalFile/BTree/Int64/Mixed/balanced/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| cache-262144 | 10.1135 | 0.9584 | 18.0941 | 3 | lowest measured mean |
| cache-1048576 | 10.1154 | 0.9247 | 18.0936 | 3 | interval overlaps fastest |
| cache-4194304 | 10.8278 | 4.2498 | 18.0939 | 3 | interval overlaps fastest |
| baseline | 11.2166 | 4.0077 | 18.0940 | 3 | interval overlaps fastest |
| cache-65536 | 14.8134 | 0.0446 | 20.1974 | 3 | slower measured mean |

## btrees/TransactionalFile/BTree/Int64/Mixed/read-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| cache-262144 | 8.7793 | 0.1859 | 14.9196 | 3 | lowest measured mean |
| cache-4194304 | 9.0495 | 0.5565 | 14.9191 | 3 | interval overlaps fastest |
| baseline | 9.1628 | 1.4755 | 14.9196 | 3 | interval overlaps fastest |
| cache-1048576 | 9.5391 | 4.0352 | 14.9193 | 3 | interval overlaps fastest |
| cache-65536 | 13.3938 | 2.7382 | 17.4035 | 3 | slower measured mean |

## btrees/TransactionalFile/BTree/Int64/Mixed/write-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| cache-4194304 | 11.2564 | 1.3692 | 22.2168 | 3 | lowest measured mean |
| cache-262144 | 11.2607 | 0.8501 | 22.2165 | 3 | interval overlaps fastest |
| baseline | 11.5157 | 0.9428 | 22.2169 | 3 | interval overlaps fastest |
| cache-1048576 | 13.0431 | 2.4941 | 22.2170 | 3 | interval overlaps fastest |
| cache-65536 | 17.8248 | 0.8436 | 24.7195 | 3 | slower measured mean |

## btrees/TransactionalFile/BTreePlus/Int64/Mixed/balanced/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-262144 | 11.4719 | 1.3186 | 26.4427 | 3 | lowest measured mean |
| page-1048576 | 12.1531 | 5.1596 | 26.4423 | 3 | interval overlaps fastest |
| baseline | 12.5061 | 0.5954 | 26.4541 | 3 | interval overlaps fastest |
| page-16384 | 17.0037 | 5.2009 | 26.5087 | 3 | interval overlaps fastest |
| page-4096 | 29.8278 | 3.0775 | 27.4923 | 3 | slower measured mean |

## btrees/TransactionalFile/BTreePlus/Int64/Mixed/read-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-1048576 | 7.8706 | 0.1948 | 15.5158 | 3 | lowest measured mean |
| page-262144 | 8.1729 | 0.8408 | 15.5155 | 3 | interval overlaps fastest |
| baseline | 10.1796 | 4.4864 | 15.5387 | 3 | interval overlaps fastest |
| page-16384 | 12.5551 | 4.2552 | 15.6221 | 3 | slower measured mean |
| page-4096 | 20.2735 | 1.3858 | 16.0796 | 3 | slower measured mean |

## btrees/TransactionalFile/BTreePlus/Int64/Mixed/write-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-262144 | 15.4855 | 2.5972 | 37.3586 | 3 | lowest measured mean |
| baseline | 16.0151 | 2.2068 | 37.3643 | 3 | interval overlaps fastest |
| page-1048576 | 16.3621 | 4.2221 | 37.3586 | 3 | interval overlaps fastest |
| page-16384 | 19.1652 | 0.7158 | 37.3897 | 3 | slower measured mean |
| page-4096 | 32.0396 | 0.8368 | 38.8373 | 3 | slower measured mean |

## btrees/TransactionalFile/BTreePlus/Int64/Mixed/balanced/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| cache-1048576 | 12.3757 | 0.9769 | 26.4540 | 3 | lowest measured mean |
| cache-4194304 | 12.7972 | 1.1931 | 26.4538 | 3 | interval overlaps fastest |
| baseline | 13.7430 | 5.3406 | 26.4539 | 3 | interval overlaps fastest |
| cache-262144 | 14.5366 | 5.6681 | 26.4540 | 3 | interval overlaps fastest |
| cache-65536 | 38.7764 | 3.5174 | 39.5567 | 3 | slower measured mean |

## btrees/TransactionalFile/BTreePlus/Int64/Mixed/read-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 8.9440 | 0.8887 | 15.5385 | 3 | lowest measured mean |
| cache-1048576 | 10.7844 | 3.9315 | 15.5386 | 3 | interval overlaps fastest |
| cache-4194304 | 11.2578 | 8.4175 | 15.5389 | 3 | interval overlaps fastest |
| cache-262144 | 13.2781 | 8.3857 | 15.5389 | 3 | interval overlaps fastest |
| cache-65536 | 15.1923 | 1.2598 | 19.1988 | 3 | slower measured mean |

## btrees/TransactionalFile/BTreePlus/Int64/Mixed/write-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 15.1490 | 0.6012 | 37.3641 | 3 | lowest measured mean |
| cache-1048576 | 15.9238 | 4.0528 | 37.3640 | 3 | interval overlaps fastest |
| cache-262144 | 16.2443 | 4.0131 | 37.3645 | 3 | interval overlaps fastest |
| cache-4194304 | 16.6082 | 5.1396 | 37.3642 | 3 | interval overlaps fastest |
| cache-65536 | 59.8521 | 1.5974 | 59.1258 | 3 | slower measured mean |

## btrees/TransactionalFile/Lookup/Int32/Mixed/balanced/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-1048576 | 88.9112 | 13.7017 | 301.8017 | 3 | lowest measured mean |
| page-262144 | 90.6730 | 18.3378 | 301.8011 | 3 | interval overlaps fastest |
| page-16384 | 98.6898 | 16.5680 | 301.5065 | 3 | interval overlaps fastest |
| baseline | 101.2504 | 14.1989 | 301.7415 | 3 | interval overlaps fastest |
| page-4096 | 115.4384 | 26.7776 | 306.5391 | 3 | interval overlaps fastest |

## btrees/TransactionalFile/Lookup/Int32/Mixed/read-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 35.8322 | 1.2883 | 112.0252 | 3 | lowest measured mean |
| page-1048576 | 36.2465 | 11.1876 | 112.0297 | 3 | interval overlaps fastest |
| page-262144 | 40.8278 | 14.4523 | 112.0297 | 3 | interval overlaps fastest |
| page-16384 | 41.1958 | 3.1455 | 112.0246 | 3 | slower measured mean |
| page-4096 | 57.3095 | 13.5993 | 113.4766 | 3 | slower measured mean |

## btrees/TransactionalFile/Lookup/Int32/Mixed/write-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FilePage

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| page-262144 | 150.5778 | 21.6469 | 521.9353 | 3 | lowest measured mean |
| page-1048576 | 155.9528 | 23.7220 | 521.9356 | 3 | interval overlaps fastest |
| baseline | 158.9064 | 20.3371 | 521.6944 | 3 | interval overlaps fastest |
| page-4096 | 175.1046 | 14.4017 | 530.6969 | 3 | interval overlaps fastest |
| page-16384 | 176.9353 | 18.8501 | 521.1403 | 3 | interval overlaps fastest |

## btrees/TransactionalFile/Lookup/Int32/Mixed/balanced/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| baseline | 89.1846 | 10.3740 | 301.7420 | 3 | lowest measured mean |
| cache-1048576 | 97.5486 | 33.0295 | 301.7419 | 3 | interval overlaps fastest |
| cache-262144 | 99.4874 | 38.1516 | 301.7419 | 3 | interval overlaps fastest |
| cache-4194304 | 99.5403 | 8.0086 | 301.7416 | 3 | interval overlaps fastest |
| cache-65536 | 289.3240 | 12.8847 | 410.3255 | 3 | slower measured mean |

## btrees/TransactionalFile/Lookup/Int32/Mixed/read-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| cache-4194304 | 37.8272 | 10.5244 | 112.0254 | 3 | lowest measured mean |
| baseline | 39.2434 | 10.7263 | 112.0250 | 3 | interval overlaps fastest |
| cache-262144 | 42.8580 | 18.1111 | 112.0251 | 3 | interval overlaps fastest |
| cache-1048576 | 43.1754 | 35.0446 | 112.0256 | 3 | interval overlaps fastest |
| cache-65536 | 107.9246 | 23.7404 | 150.1100 | 3 | slower measured mean |

## btrees/TransactionalFile/Lookup/Int32/Mixed/write-heavy/records-2048/ops-100/group-8/shuffled-True/factor-FileCache

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| cache-262144 | 153.1695 | 13.6529 | 521.6948 | 3 | lowest measured mean |
| baseline | 158.4203 | 25.1362 | 521.6949 | 3 | interval overlaps fastest |
| cache-4194304 | 158.5100 | 42.7760 | 521.6946 | 3 | interval overlaps fastest |
| cache-1048576 | 158.8409 | 29.0029 | 521.6948 | 3 | interval overlaps fastest |
| cache-65536 | 581.0806 | 1.1148 | 751.7307 | 3 | slower measured mean |

