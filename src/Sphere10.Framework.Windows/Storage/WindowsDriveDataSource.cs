// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Storage;

/// <summary>A logical-drive inventory IDataSource. Network capacity is probed only when explicitly requested.</summary>
public sealed class WindowsDriveDataSource : ReloadableSnapshotDataSource<WindowsDriveInfo> {
	public WindowsDriveDataSource(bool probeNetworkDrives = false)
		: this(token => Tools.Windows.Drives.GetDrivesAsync(probeNetworkDrives, token)) { }

	public WindowsDriveDataSource(Func<CancellationToken, Task<WindowsDriveInfo[]>> capture)
		: base(capture) {
		DefaultSortProperty = nameof(WindowsDriveInfo.Name);
	}
}
