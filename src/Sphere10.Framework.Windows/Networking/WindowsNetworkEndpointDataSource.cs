// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Networking;

/// <summary>A native TCP/UDP owner-table IDataSource for one address family, optionally limited to an owning PID.</summary>
public sealed class WindowsNetworkEndpointDataSource : ReloadableSnapshotDataSource<WindowsNetworkEndpoint> {
	public WindowsNetworkEndpointDataSource(bool isTcp, AddressFamily addressFamily, uint? processId = null,
		Func<CancellationToken, Task<WindowsNetworkEndpoint[]>> capture = null)
		: base(CreateCapture(isTcp, addressFamily, processId, capture)) {
		IsTcp = isTcp;
		AddressFamily = addressFamily;
		ProcessId = processId;
		DefaultSortProperty = nameof(WindowsNetworkEndpoint.LocalPort);
	}

	public bool IsTcp { get; }

	public AddressFamily AddressFamily { get; }

	public uint? ProcessId { get; }

	private static Func<CancellationToken, Task<WindowsNetworkEndpoint[]>> CreateCapture(bool isTcp, AddressFamily addressFamily, uint? processId,
		Func<CancellationToken, Task<WindowsNetworkEndpoint[]>> capture) {
		Guard.Argument(addressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6, nameof(addressFamily), "Specify IPv4 or IPv6.");
		Guard.Argument(!processId.HasValue || processId.Value > 0, nameof(processId), "Specify a positive owning process ID.");
		return async token => {
			var rows = await (capture == null ? Tools.Windows.Network.GetEndpointsAsync(isTcp, addressFamily, token) : capture(token)).ConfigureAwait(false);
			token.ThrowIfCancellationRequested();
			return rows.Where(row => row.IsTcp == isTcp && row.AddressFamily == addressFamily && (!processId.HasValue || row.ProcessId == processId.Value)).ToArray();
		};
	}
}
