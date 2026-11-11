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

namespace Sphere10.Framework.Windows.Networking;

/// <summary>A native network-interface inventory IDataSource, without traffic-rate sampling.</summary>
public sealed class WindowsNetworkInterfaceDataSource : AsyncListDataSource<WindowsNetworkInterface> {
	public WindowsNetworkInterfaceDataSource() => DefaultSortProperty = nameof(WindowsNetworkInterface.Name);

	public override DataSourceCapabilities Capabilities => DataSourceCapabilities.ReadOnly | DataSourceCapabilities.CanNotify;

	protected override async Task<IEnumerable<WindowsNetworkInterface>> FetchAsync(CancellationToken cancellationToken)
		=> await Tools.Windows.Network.GetInterfacesAsync(cancellationToken).ConfigureAwait(false);
}
