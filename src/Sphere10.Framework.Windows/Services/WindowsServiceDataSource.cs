// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Services;

/// <summary>A native service or driver inventory IDataSource. Reads never start, stop or configure a service.</summary>
public sealed class WindowsServiceDataSource : AsyncListDataSource<WindowsServiceInfo> {
	private readonly bool _drivers;

	public WindowsServiceDataSource(bool drivers = false) {
		_drivers = drivers;
		DefaultSortProperty = nameof(WindowsServiceInfo.Name);
	}

	public override DataSourceCapabilities Capabilities => DataSourceCapabilities.ReadOnly | DataSourceCapabilities.CanNotify;

	protected override async Task<IEnumerable<WindowsServiceInfo>> FetchAsync(CancellationToken cancellationToken)
		=> await (_drivers ? Tools.Windows.Services.GetDriversAsync(cancellationToken) : Tools.Windows.Services.GetServicesAsync(cancellationToken)).ConfigureAwait(false);
}
