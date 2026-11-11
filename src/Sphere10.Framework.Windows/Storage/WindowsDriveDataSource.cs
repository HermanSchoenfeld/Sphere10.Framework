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

namespace Sphere10.Framework.Windows.Storage;

/// <summary>A logical-drive inventory IDataSource. Network capacity is probed only when explicitly requested.</summary>
public sealed class WindowsDriveDataSource : AsyncListDataSource<WindowsDriveInfo> {
	private readonly bool _probeNetworkDrives;

	public WindowsDriveDataSource(bool probeNetworkDrives = false) {
		_probeNetworkDrives = probeNetworkDrives;
		DefaultSortProperty = nameof(WindowsDriveInfo.Name);
	}

	public override DataSourceCapabilities Capabilities => DataSourceCapabilities.ReadOnly | DataSourceCapabilities.CanNotify;

	protected override async Task<IEnumerable<WindowsDriveInfo>> FetchAsync(CancellationToken cancellationToken)
		=> await Tools.Windows.Drives.GetDrivesAsync(_probeNetworkDrives, cancellationToken).ConfigureAwait(false);
}
