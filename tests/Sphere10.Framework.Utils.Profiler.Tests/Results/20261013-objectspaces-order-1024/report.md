# Parameter study: measured screening candidates

Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.
Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.
Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.

Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-1024/ops-100/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-169 | 83.0041 | 16.5731 | 381.9540 | 5 | lowest measured mean |
| order-170 | 90.3226 | 14.1431 | 386.1841 | 5 | interval overlaps fastest |
| order-64 | 104.8594 | 12.5574 | 482.9042 | 5 | interval overlaps fastest |
| order-256 | 110.4098 | 21.8185 | 533.7841 | 5 | interval overlaps fastest |
| order-128 | 113.6100 | 10.4804 | 529.8458 | 5 | slower measured mean |
| order-512 | 122.0235 | 18.2349 | 618.0687 | 5 | slower measured mean |
| order-32 | 144.6303 | 37.7757 | 681.9973 | 5 | slower measured mean |
| order-16 | 181.5048 | 21.8602 | 891.4744 | 5 | slower measured mean |
| order-8 | 318.7520 | 23.1326 | 1602.5491 | 5 | slower measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-1024/ops-100/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-169 | 28.6438 | 5.7223 | 89.9956 | 5 | lowest measured mean |
| order-170 | 30.6007 | 6.8588 | 89.8651 | 5 | interval overlaps fastest |
| order-64 | 32.7893 | 3.4040 | 123.5784 | 5 | interval overlaps fastest |
| order-512 | 33.4952 | 3.8121 | 133.5698 | 5 | interval overlaps fastest |
| order-32 | 42.1529 | 5.6810 | 151.3868 | 5 | slower measured mean |
| order-256 | 50.0538 | 6.4626 | 199.5726 | 5 | slower measured mean |
| order-128 | 52.4255 | 8.0695 | 194.0406 | 5 | slower measured mean |
| order-16 | 52.6374 | 9.1040 | 195.3761 | 5 | slower measured mean |
| order-8 | 76.8274 | 8.5330 | 336.7849 | 5 | slower measured mean |

## object-spaces/Memory/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-1024/ops-100/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-169 | 122.4332 | 8.0574 | 673.5322 | 5 | lowest measured mean |
| order-170 | 144.6750 | 9.9463 | 723.7769 | 5 | slower measured mean |
| order-64 | 162.0468 | 17.6299 | 844.4942 | 5 | slower measured mean |
| order-128 | 186.2180 | 8.4738 | 916.4166 | 5 | slower measured mean |
| order-512 | 190.1005 | 14.2297 | 1069.1630 | 5 | slower measured mean |
| order-256 | 215.2457 | 28.7828 | 1071.8308 | 5 | slower measured mean |
| order-32 | 231.7411 | 25.3425 | 1183.5863 | 5 | slower measured mean |
| order-16 | 304.8835 | 35.6373 | 1595.3911 | 5 | slower measured mean |
| order-8 | 570.8753 | 71.2278 | 2905.7056 | 5 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/Balanced/records-1024/ops-100/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-169 | 198.9189 | 18.7557 | 649.0258 | 5 | lowest measured mean |
| order-170 | 200.3854 | 17.7045 | 657.1995 | 5 | interval overlaps fastest |
| order-64 | 244.3378 | 14.1981 | 820.5592 | 5 | slower measured mean |
| order-128 | 275.5175 | 26.8822 | 899.9918 | 5 | slower measured mean |
| order-256 | 283.3518 | 13.2966 | 895.4888 | 5 | slower measured mean |
| order-512 | 300.2312 | 19.6205 | 1034.6326 | 5 | slower measured mean |
| order-32 | 335.0394 | 29.9044 | 1170.8109 | 5 | slower measured mean |
| order-16 | 452.1619 | 39.2559 | 1534.7217 | 5 | slower measured mean |
| order-8 | 794.0114 | 59.5924 | 2768.8081 | 5 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/ReadHeavy/records-1024/ops-100/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-169 | 57.7811 | 4.7010 | 153.3357 | 5 | lowest measured mean |
| order-170 | 65.4570 | 15.6130 | 153.0942 | 5 | interval overlaps fastest |
| order-64 | 74.7230 | 4.3707 | 208.0481 | 5 | slower measured mean |
| order-512 | 79.9952 | 5.3962 | 224.2489 | 5 | slower measured mean |
| order-32 | 91.1748 | 6.2141 | 258.8572 | 5 | slower measured mean |
| order-128 | 108.1130 | 11.6068 | 324.5084 | 5 | slower measured mean |
| order-16 | 110.5147 | 5.4957 | 336.7133 | 5 | slower measured mean |
| order-256 | 113.7589 | 3.6249 | 334.2475 | 5 | slower measured mean |
| order-8 | 176.4848 | 8.5322 | 581.8083 | 5 | slower measured mean |

## object-spaces/File/PayloadOnly/Full/checks-True/no-merkle/Mixed/WriteHeavy/records-1024/ops-100/payload-128-Fixed/categories-1024-0/lookup-Shuffled-10/shuffled-True

| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |
|---|---:|---:|---:|---:|---|
| order-169 | 331.4552 | 32.3553 | 1143.8928 | 5 | lowest measured mean |
| order-170 | 341.9052 | 23.0726 | 1226.5659 | 5 | interval overlaps fastest |
| order-128 | 418.6615 | 29.3892 | 1557.7250 | 5 | slower measured mean |
| order-64 | 429.9902 | 63.1843 | 1436.6322 | 5 | slower measured mean |
| order-256 | 496.0173 | 44.4797 | 1792.5001 | 5 | slower measured mean |
| order-512 | 498.3311 | 38.2865 | 1789.8660 | 5 | slower measured mean |
| order-32 | 580.2103 | 48.7952 | 2034.6164 | 5 | slower measured mean |
| order-16 | 794.1703 | 26.2393 | 2746.1436 | 5 | slower measured mean |
| order-8 | 1342.0741 | 89.2695 | 5019.8523 | 5 | slower measured mean |

