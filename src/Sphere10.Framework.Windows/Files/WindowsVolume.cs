// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Files;

public static class WindowsVolume {
	public static DriveType GetDriveType(string root) => new DriveInfo(root).DriveType;

	public static WindowsVolumeInfo Query(string root) {
		var drive = new DriveInfo(root);
		return drive.IsReady ? new WindowsVolumeInfo(drive.Name, drive.DriveType, true, drive.DriveFormat, drive.VolumeLabel, drive.TotalSize, drive.AvailableFreeSpace)
			: new WindowsVolumeInfo(drive.Name, drive.DriveType, false, null, null, null, null);
	}

	public static Task SetLabelAsync(string root, string expectedLabel, string proposed, CancellationToken cancellationToken = default)
		=> Task.Run(() => { cancellationToken.ThrowIfCancellationRequested(); SetLabel(root, expectedLabel, proposed); }, cancellationToken);

	public static void SetLabel(string root, string expectedLabel, string proposed) {
		var drive = new DriveInfo(root);
		Guard.Ensure(expectedLabel == null || drive.VolumeLabel == expectedLabel, "The volume label changed.");
		drive.VolumeLabel = proposed;
	}
}
