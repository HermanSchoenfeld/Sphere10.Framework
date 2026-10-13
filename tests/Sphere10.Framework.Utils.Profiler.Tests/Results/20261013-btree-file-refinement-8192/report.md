# Parameter study: measured screening candidates

Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.
Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.
Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.

Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.

## btrees/FileStream/BTree/Int64/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-128 | 17.6363 | 0.3559 | 25.2708 | 3 | lowest measured mean |
| order-64 | 23.1538 | 1.0889 | 24.5841 | 3 | slower measured mean |
| order-256 | 29.7104 | 36.7429 | 26.8958 | 3 | interval overlaps fastest |

## btrees/FileStream/BTree/Int64/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-128 | 10.5110 | 0.7371 | 19.7986 | 3 | lowest measured mean |
| order-64 | 14.3860 | 0.9138 | 19.7168 | 3 | slower measured mean |
| order-256 | 14.6509 | 9.3214 | 20.2781 | 3 | interval overlaps fastest |

## btrees/FileStream/BTree/Int64/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-128 | 24.6805 | 1.2119 | 30.7330 | 3 | lowest measured mean |
| order-256 | 29.5959 | 0.2628 | 33.4759 | 3 | slower measured mean |
| order-64 | 30.3412 | 0.7509 | 29.4541 | 3 | slower measured mean |

## btrees/FileStream/BTreePlus/Int64/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-2048 | 49.3777 | 2.1404 | 50.9778 | 3 | lowest measured mean |
| order-1024 | 72.6774 | 2.6501 | 46.1563 | 3 | slower measured mean |
| order-768 | 83.5424 | 8.9929 | 46.2578 | 3 | slower measured mean |
| order-512 | 108.3420 | 7.1081 | 51.0495 | 3 | slower measured mean |
| order-256 | 206.7364 | 8.7338 | 78.2517 | 3 | slower measured mean |

## btrees/FileStream/BTreePlus/Int64/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-2048 | 22.2904 | 5.8669 | 23.6767 | 3 | lowest measured mean |
| order-1024 | 24.0864 | 1.0512 | 21.9721 | 3 | interval overlaps fastest |
| order-768 | 25.0393 | 1.0983 | 21.7722 | 3 | interval overlaps fastest |
| order-512 | 28.4298 | 0.8364 | 22.5142 | 3 | interval overlaps fastest |
| order-256 | 47.8651 | 0.7900 | 27.4499 | 3 | slower measured mean |

## btrees/FileStream/BTreePlus/Int64/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-2048 | 76.7903 | 4.9866 | 78.2928 | 3 | lowest measured mean |
| order-1024 | 121.1343 | 6.2047 | 70.3265 | 3 | slower measured mean |
| order-768 | 144.3633 | 15.7011 | 70.7671 | 3 | slower measured mean |
| order-512 | 181.5060 | 4.9443 | 79.5855 | 3 | slower measured mean |
| order-256 | 364.4100 | 1.7376 | 129.0084 | 3 | slower measured mean |

## btrees/OSMemoryMapped/BTree/Int64/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-128 | 6.2239 | 0.9488 | 25.2708 | 3 | lowest measured mean |
| order-64 | 6.3038 | 0.5008 | 24.5841 | 3 | interval overlaps fastest |
| order-256 | 6.4447 | 0.6003 | 26.8958 | 3 | interval overlaps fastest |

## btrees/OSMemoryMapped/BTree/Int64/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-128 | 5.3005 | 0.1858 | 19.7986 | 3 | lowest measured mean |
| order-256 | 5.4401 | 0.4539 | 20.2781 | 3 | interval overlaps fastest |
| order-64 | 5.9543 | 1.4653 | 19.7168 | 3 | interval overlaps fastest |

## btrees/OSMemoryMapped/BTree/Int64/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-128 | 7.1742 | 0.8970 | 30.7330 | 3 | lowest measured mean |
| order-64 | 7.5087 | 0.7354 | 29.4541 | 3 | interval overlaps fastest |
| order-256 | 7.5108 | 1.1273 | 33.4759 | 3 | interval overlaps fastest |

## btrees/OSMemoryMapped/BTreePlus/Int64/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-2048 | 10.0134 | 1.5323 | 50.9778 | 3 | lowest measured mean |
| order-1024 | 10.2679 | 1.1719 | 46.1563 | 3 | interval overlaps fastest |
| order-768 | 11.0711 | 2.2287 | 46.2578 | 3 | interval overlaps fastest |
| order-512 | 12.0599 | 0.9284 | 51.0495 | 3 | interval overlaps fastest |
| order-256 | 21.5119 | 11.0748 | 78.2517 | 3 | interval overlaps fastest |

## btrees/OSMemoryMapped/BTreePlus/Int64/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-1024 | 5.5877 | 0.1960 | 21.9721 | 3 | lowest measured mean |
| order-768 | 5.6642 | 0.2073 | 21.7722 | 3 | interval overlaps fastest |
| order-512 | 5.8803 | 0.5230 | 22.5142 | 3 | interval overlaps fastest |
| order-2048 | 6.6666 | 4.6756 | 23.6767 | 3 | interval overlaps fastest |
| order-256 | 7.3852 | 0.8277 | 27.4499 | 3 | slower measured mean |

## btrees/OSMemoryMapped/BTreePlus/Int64/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-2048 | 13.4750 | 0.5886 | 78.2928 | 3 | lowest measured mean |
| order-768 | 15.6905 | 0.9191 | 70.7671 | 3 | slower measured mean |
| order-1024 | 17.0248 | 7.2332 | 70.3265 | 3 | interval overlaps fastest |
| order-512 | 20.8748 | 8.8386 | 79.5855 | 3 | interval overlaps fastest |
| order-256 | 31.1909 | 3.2643 | 129.0084 | 3 | slower measured mean |

## btrees/TransactionalFile/BTree/Int64/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-128 | 58.9625 | 3.5298 | 209.7869 | 3 | lowest measured mean |
| order-64 | 64.9753 | 14.5772 | 230.6198 | 3 | interval overlaps fastest |
| order-256 | 65.6759 | 31.9802 | 220.8211 | 3 | interval overlaps fastest |

## btrees/TransactionalFile/BTree/Int64/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-128 | 51.6391 | 9.8011 | 168.2827 | 3 | lowest measured mean |
| order-64 | 53.2250 | 12.0150 | 183.9656 | 3 | interval overlaps fastest |
| order-256 | 56.6119 | 27.7003 | 171.8230 | 3 | interval overlaps fastest |

## btrees/TransactionalFile/BTree/Int64/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-64 | 75.9415 | 8.3369 | 277.2854 | 3 | lowest measured mean |
| order-128 | 76.5555 | 9.2619 | 251.2395 | 3 | interval overlaps fastest |
| order-256 | 83.2554 | 28.9985 | 269.6022 | 3 | interval overlaps fastest |

## btrees/TransactionalFile/BTreePlus/Int64/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-2048 | 88.5736 | 9.0965 | 353.0807 | 3 | lowest measured mean |
| order-1024 | 107.6672 | 13.4340 | 395.4193 | 3 | interval overlaps fastest |
| order-768 | 115.0632 | 14.3172 | 420.6886 | 3 | slower measured mean |
| order-512 | 147.4068 | 51.3744 | 499.9092 | 3 | interval overlaps fastest |
| order-256 | 232.5091 | 24.9226 | 836.8761 | 3 | slower measured mean |

## btrees/TransactionalFile/BTreePlus/Int64/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-2048 | 50.9681 | 1.3172 | 191.1184 | 3 | lowest measured mean |
| order-1024 | 55.6509 | 3.2198 | 193.9014 | 3 | slower measured mean |
| order-768 | 58.6927 | 7.5633 | 197.4196 | 3 | interval overlaps fastest |
| order-512 | 58.9665 | 4.6364 | 211.5158 | 3 | slower measured mean |
| order-256 | 91.0639 | 9.9270 | 275.0284 | 3 | slower measured mean |

## btrees/TransactionalFile/BTreePlus/Int64/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-2048 | 112.0274 | 1.7785 | 516.2725 | 3 | lowest measured mean |
| order-1024 | 156.1717 | 35.5744 | 596.6764 | 3 | slower measured mean |
| order-768 | 162.7906 | 4.7822 | 643.8901 | 3 | slower measured mean |
| order-512 | 218.2060 | 22.4807 | 788.2962 | 3 | slower measured mean |
| order-256 | 372.8715 | 26.4944 | 1398.4477 | 3 | slower measured mean |

## btrees/FilePaged/BTree/Int64/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-256 | 58.9630 | 12.1754 | 220.7058 | 3 | lowest measured mean |
| order-128 | 60.9573 | 16.4351 | 209.6719 | 3 | interval overlaps fastest |
| order-64 | 63.2503 | 5.0405 | 230.5048 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTree/Int64/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-128 | 43.6852 | 2.5259 | 168.1679 | 3 | lowest measured mean |
| order-256 | 44.5175 | 2.3840 | 171.7081 | 3 | interval overlaps fastest |
| order-64 | 48.8105 | 1.9548 | 183.8507 | 3 | slower measured mean |

## btrees/FilePaged/BTree/Int64/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-128 | 68.0825 | 5.0362 | 251.1246 | 3 | lowest measured mean |
| order-256 | 73.0800 | 7.3561 | 269.4876 | 3 | interval overlaps fastest |
| order-64 | 76.2216 | 6.2041 | 277.1705 | 3 | interval overlaps fastest |

## btrees/FilePaged/BTreePlus/Int64/Mixed/balanced/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-2048 | 84.1298 | 7.3485 | 352.9439 | 3 | lowest measured mean |
| order-1024 | 103.3329 | 7.1182 | 395.2825 | 3 | slower measured mean |
| order-768 | 123.4759 | 42.0755 | 420.5740 | 3 | interval overlaps fastest |
| order-512 | 143.6246 | 30.9690 | 499.7943 | 3 | slower measured mean |
| order-256 | 237.0459 | 7.3022 | 836.7609 | 3 | slower measured mean |

## btrees/FilePaged/BTreePlus/Int64/Mixed/read-heavy/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-768 | 54.0378 | 6.5339 | 197.3047 | 3 | lowest measured mean |
| order-2048 | 54.7954 | 6.2575 | 190.9811 | 3 | interval overlaps fastest |
| order-1024 | 57.6007 | 2.9617 | 193.7644 | 3 | interval overlaps fastest |
| order-512 | 64.5101 | 5.3952 | 211.4014 | 3 | interval overlaps fastest |
| order-256 | 84.8466 | 19.6877 | 274.9130 | 3 | slower measured mean |

## btrees/FilePaged/BTreePlus/Int64/Mixed/write-heavy/records-8192/ops-1000/group-8/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-2048 | 115.9347 | 8.1030 | 516.1355 | 3 | lowest measured mean |
| order-1024 | 153.9979 | 3.4584 | 596.5391 | 3 | slower measured mean |
| order-768 | 177.8265 | 10.3741 | 643.7750 | 3 | slower measured mean |
| order-512 | 218.2304 | 4.5140 | 788.1813 | 3 | slower measured mean |
| order-256 | 393.5184 | 19.2065 | 1398.3328 | 3 | slower measured mean |

