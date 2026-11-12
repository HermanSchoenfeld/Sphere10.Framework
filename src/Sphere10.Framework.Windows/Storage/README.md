# Storage

Lists logical drives, physical disks and partitions, relabels volumes and runs a read-only `chkdsk` scan.

`Tools.Windows.Drives` returns `WindowsDriveInfo` rows for every logical drive with type, readiness, format, label and capacity. Network-drive capacity is queried only when `probeNetworkDrives` is true. `GetPhysicalDisks` reads `Win32_DiskDrive` and `GetPartitions` reads `Win32_DiskPartition` with their drive-letter associations, each within an optional timeout and without changing any disk. `SetVolumeLabel` changes a drive's label. Each read has an async variant, and the `Create...DataSource` methods return read-only `WindowsDriveDataSource`, `WindowsPhysicalDiskDataSource` and `WindowsPartitionDataSource` inventories that load once and page the captured rows.

`VolumeCheckSentry` runs the in-box `chkdsk.exe` on one drive root without repair, dismount, recovery or boot-scheduling flags. Per-volume label and format queries for a single root are also in `WindowsVolume` in the [Files](../Files/README.md) domain.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`VolumeCheckSentry`](VolumeCheckSentry.cs) | class | `ProcessSentry` that runs `chkdsk.exe` read-only on a drive root and returns its output |
| [`WindowsDriveDataSource`](WindowsDriveDataSource.cs) | class | Read-only logical-drive data source with opt-in network capacity probing |
| [`WindowsDriveInfo`](WindowsDriveInfo.cs) | class | One logical drive with type, readiness, format, label, capacity, free and available bytes and read error |
| [`Tools.Windows.Drives`](WindowsDriveUtil.cs) | static class | `Tools.Windows.Drives` drive, physical-disk and partition reads, volume relabelling and data-source factories |
| [`WindowsPartitionDataSource`](WindowsPartitionDataSource.cs) | class | Read-only partition data source over the bounded `Win32_DiskPartition` query |
| [`WindowsPartitionInfo`](WindowsPartitionInfo.cs) | record | One partition with disk and partition index, type, size, offset, boot flag, drive roots and read errors |
| [`WindowsPhysicalDiskDataSource`](WindowsPhysicalDiskDataSource.cs) | class | Read-only physical-disk data source over the bounded `Win32_DiskDrive` query |
| [`WindowsPhysicalDiskInfo`](WindowsPhysicalDiskInfo.cs) | record | One physical disk with index, model, interface, serial number, size, status and read error |
