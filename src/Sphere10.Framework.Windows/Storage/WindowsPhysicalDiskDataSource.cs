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

/// <summary>A physical-disk inventory IDataSource using the existing bounded Windows storage query.</summary>
public sealed class WindowsPhysicalDiskDataSource : ReloadableSnapshotDataSource<WindowsPhysicalDiskInfo> {
	public WindowsPhysicalDiskDataSource(TimeSpan? timeout = null)
		: this(token => Tools.Windows.Drives.GetPhysicalDisksAsync(token, timeout)) { }

	public WindowsPhysicalDiskDataSource(Func<CancellationToken, Task<WindowsPhysicalDiskInfo[]>> capture)
		: base(capture) { }
}
