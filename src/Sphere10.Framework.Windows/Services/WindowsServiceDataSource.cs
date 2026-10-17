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

namespace Sphere10.Framework.Windows.Services;

/// <summary>A native service or driver inventory IDataSource. Reads never start, stop or configure a service.</summary>
public sealed class WindowsServiceDataSource : ReloadableSnapshotDataSource<WindowsServiceInfo> {
	public WindowsServiceDataSource(bool drivers = false)
		: this(token => drivers ? Tools.Windows.Services.GetDriversAsync(token) : Tools.Windows.Services.GetServicesAsync(token)) { }

	public WindowsServiceDataSource(Func<CancellationToken, Task<WindowsServiceInfo[]>> capture)
		: base(capture) {
		DefaultSortProperty = nameof(WindowsServiceInfo.Name);
	}
}
