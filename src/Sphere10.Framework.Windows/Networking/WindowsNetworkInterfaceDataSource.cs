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

namespace Sphere10.Framework.Windows.Networking;

/// <summary>A native network-interface inventory IDataSource, without traffic-rate sampling.</summary>
public sealed class WindowsNetworkInterfaceDataSource : ReloadableSnapshotDataSource<WindowsNetworkInterface> {
	public WindowsNetworkInterfaceDataSource()
		: this(token => Tools.Windows.Network.GetInterfacesAsync(token)) { }

	public WindowsNetworkInterfaceDataSource(Func<CancellationToken, Task<WindowsNetworkInterface[]>> capture)
		: base(capture) {
		DefaultSortProperty = nameof(WindowsNetworkInterface.Name);
	}
}
