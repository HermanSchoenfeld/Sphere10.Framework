// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Storage;

/// <summary>Enumerates Windows drives, optionally performing capacity queries on network drives.</summary>
public class WindowsDriveUtil {
	public WindowsDriveInfo[] GetDrives(bool probeNetworkDrives) => GetDrivesCore(probeNetworkDrives, null);

	public Task<WindowsDriveInfo[]> GetDrivesAsync(bool probeNetworkDrives, CancellationToken cancellationToken = default)
		=> Task.Run(() => GetDrivesCore(probeNetworkDrives, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	private static WindowsDriveInfo[] GetDrivesCore(bool probeNetworkDrives, Action checkpoint) {
		checkpoint?.Invoke();
		var rows = new List<WindowsDriveInfo>();
		foreach (var drive in DriveInfo.GetDrives()) {
			checkpoint?.Invoke();
			var row = new WindowsDriveInfo { Name = drive.Name };
			Tools.Exceptions.ExecuteCapturingException(() => {
				row.DriveType = drive.DriveType;
				if (probeNetworkDrives || drive.DriveType != DriveType.Network) {
					row.IsReady = drive.IsReady;
					if (row.IsReady == true) {
						row.Format = drive.DriveFormat;
						row.Label = drive.VolumeLabel;
						row.CapacityBytes = drive.TotalSize;
						row.FreeBytes = drive.TotalFreeSpace;
						row.AvailableBytes = drive.AvailableFreeSpace;
					}
				}
			}, out var readError);
			row.ReadError = readError;
			rows.Add(row);
		}
		checkpoint?.Invoke();
		return rows.ToArray();
	}

	public void SetVolumeLabel(string driveName, string label) {
		Guard.ArgumentNotNullOrWhitespace(driveName, nameof(driveName));
		Guard.ArgumentNotNull(label, nameof(label));
		new DriveInfo(driveName).VolumeLabel = label;
	}

	/// <summary>Reads physical-drive properties exposed by Win32_DiskDrive without partitioning or changing a drive.</summary>
	public WindowsPhysicalDiskInfo[] GetPhysicalDisks(TimeSpan? timeout = null) => GetPhysicalDisksCore(timeout, null);

	public Task<WindowsPhysicalDiskInfo[]> GetPhysicalDisksAsync(CancellationToken cancellationToken = default, TimeSpan? timeout = null)
		=> Task.Run(() => GetPhysicalDisksCore(timeout, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	private static WindowsPhysicalDiskInfo[] GetPhysicalDisksCore(TimeSpan? timeout, Action checkpoint)
		=> ReadHardware("SELECT DeviceID,Index,Model,InterfaceType,SerialNumber,Size,Status FROM Win32_DiskDrive",
			item => new WindowsPhysicalDiskInfo {
				DeviceId = Text(item, "DeviceID"), Index = Number<uint>(item, "Index"), Model = Text(item, "Model"), InterfaceType = Text(item, "InterfaceType"),
				SerialNumber = Text(item, "SerialNumber"), SizeBytes = Number<ulong>(item, "Size"), Status = Text(item, "Status")
			}, error => new WindowsPhysicalDiskInfo { ReadError = error }, checkpoint, timeout);

	/// <summary>Reads Win32_DiskPartition identity, location, type and boot information without modifying a partition.</summary>
	public WindowsPartitionInfo[] GetPartitions(TimeSpan? timeout = null) => GetPartitionsCore(timeout, null);

	public Task<WindowsPartitionInfo[]> GetPartitionsAsync(CancellationToken cancellationToken = default, TimeSpan? timeout = null)
		=> Task.Run(() => GetPartitionsCore(timeout, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	private static WindowsPartitionInfo[] GetPartitionsCore(TimeSpan? timeout, Action checkpoint)
		=> ReadHardware("SELECT DeviceID,DiskIndex,Index,Type,Size,StartingOffset,BootPartition FROM Win32_DiskPartition",
			item => ReadPartition(item, timeout, checkpoint), error => new WindowsPartitionInfo { ReadError = error }, checkpoint, timeout);

	private static WindowsPartitionInfo ReadPartition(ManagementObject item, TimeSpan? timeout, Action checkpoint) {
		var row = new WindowsPartitionInfo {
			DeviceId = Text(item, "DeviceID"), DiskIndex = Number<uint>(item, "DiskIndex"), Index = Number<uint>(item, "Index"), Type = Text(item, "Type"),
			SizeBytes = Number<ulong>(item, "Size"), StartingOffsetBytes = Number<ulong>(item, "StartingOffset"), BootPartition = Number<bool>(item, "BootPartition")
		};
		var resolved = row;
		Tools.Exceptions.ExecuteCapturingException(() => {
			var roots = new List<string>();
			using var drives = item.GetRelated("Win32_LogicalDisk", "Win32_LogicalDiskToPartition", null, null, null, null, false,
				new System.Management.EnumerationOptions { Timeout = timeout ?? TimeSpan.FromSeconds(15), Rewindable = false });
			foreach (ManagementObject drive in drives) {
				using (drive) {
					checkpoint?.Invoke();
					var name = Text(drive, "DeviceID");
					if (name?.Length == 2 && char.IsAsciiLetter(name[0]) && name[1] == ':')
						roots.Add(name + @"\");
				}
			}
			resolved = row with { DriveRoots = roots.ToArray() };
		}, out var associationError, error => error is not OperationCanceledException);
		return associationError == null ? resolved : row with { DriveAssociationError = associationError };
	}

	private static T[] ReadHardware<T>(string query, Func<ManagementObject, T> read, Func<Exception, T> failure,
		Action checkpoint, TimeSpan? timeout) {
		checkpoint?.Invoke();
		var queryTimeout = timeout ?? TimeSpan.FromSeconds(15);
		Guard.Argument(queryTimeout > TimeSpan.Zero, nameof(timeout), "The native query timeout must be positive.");
		using var searcher = new ManagementObjectSearcher(new ManagementScope(@"\\.\root\cimv2", new ConnectionOptions { Timeout = queryTimeout }),
			new ObjectQuery(query), new System.Management.EnumerationOptions { Timeout = queryTimeout, Rewindable = false, ReturnImmediately = true, BlockSize = 64 });
		using var objects = searcher.Get();
		var result = new List<T>();
		foreach (ManagementObject item in objects) {
			using (item) {
				checkpoint?.Invoke();
				Tools.Exceptions.ExecuteCapturingException(() => result.Add(read(item)), out var readError, error => error is not OperationCanceledException);
				if (readError != null)
					result.Add(failure(readError));
			}
		}
		checkpoint?.Invoke();
		return result.ToArray();
	}

	private static string Text(ManagementObject item, string property) => item[property]?.ToString();
	private static T? Number<T>(ManagementObject item, string property)
		where T : struct
		=> item[property] == null ? null : (T)Convert.ChangeType(item[property], typeof(T), CultureInfo.InvariantCulture);
}
