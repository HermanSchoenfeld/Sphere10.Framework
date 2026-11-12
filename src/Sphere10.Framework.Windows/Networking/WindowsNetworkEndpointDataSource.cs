// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Networking;

/// <summary>A native TCP/UDP owner-table IDataSource for one address family, optionally limited to an owning PID.</summary>
public sealed class WindowsNetworkEndpointDataSource : AsyncListDataSource<WindowsNetworkEndpoint> {
	public WindowsNetworkEndpointDataSource(bool isTcp, AddressFamily addressFamily, uint? processId = null) {
		Guard.Argument(addressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6, nameof(addressFamily), "Specify IPv4 or IPv6.");
		Guard.Argument(!processId.HasValue || processId.Value > 0, nameof(processId), "Specify a positive owning process ID.");
		IsTcp = isTcp;
		AddressFamily = addressFamily;
		ProcessId = processId;
		DefaultSortProperty = nameof(WindowsNetworkEndpoint.LocalPort);
	}

	public bool IsTcp { get; }

	public AddressFamily AddressFamily { get; }

	public uint? ProcessId { get; }

	public override DataSourceCapabilities Capabilities => DataSourceCapabilities.ReadOnly | DataSourceCapabilities.CanNotify;

	protected override async Task<IEnumerable<WindowsNetworkEndpoint>> FetchAsync(CancellationToken cancellationToken) {
		var rows = await Tools.Windows.Network.GetEndpointsAsync(IsTcp, AddressFamily, cancellationToken).ConfigureAwait(false);
		cancellationToken.ThrowIfCancellationRequested();
		return rows.Where(row => row.IsTcp == IsTcp && row.AddressFamily == AddressFamily && (!ProcessId.HasValue || row.ProcessId == ProcessId.Value)).ToArray();
	}
}
