// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;

namespace Sphere10.Framework.Windows;

/// <summary>Loads one verified process's owner-PID endpoint tables; grid queries page the fetched rows.</summary>
public sealed class ProcessNetworkEndpointDataSource : AsyncListDataSource<NetworkEndpointInfo> {
	private readonly string _processName;

	public ProcessNetworkEndpointDataSource(ProcessIdentity identity, string processName) {
		Identity = identity;
		_processName = processName ?? string.Empty;
		DefaultSortProperty = nameof(NetworkEndpointInfo.LocalPort);
	}

	public ProcessIdentity Identity { get; }

	public override DataSourceCapabilities Capabilities => DataSourceCapabilities.ReadOnly | DataSourceCapabilities.CanNotify;

	protected override string FormatValue(object value) => DataSourceFormatting.Value(value);

	protected override async Task<IEnumerable<NetworkEndpointInfo>> FetchAsync(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		Guard.Ensure(Identity.IsVerified && Identity.ProcessId > 0, "Endpoint inspection requires a verified process creation time.");
		using var process = WindowsProcess.Open(Identity.ProcessId);
		EnsureCurrent(process);
		var rows = new List<NetworkEndpointInfo>();
		foreach (var family in new[] { AddressFamily.InterNetwork, AddressFamily.InterNetworkV6 })
			foreach (var tcp in new[] { true, false }) {
				cancellationToken.ThrowIfCancellationRequested();
				var protocol = tcp ? "TCP" : "UDP";
				var addressFamily = family == AddressFamily.InterNetwork ? "IPv4" : "IPv6";
				var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
					var source = Tools.Windows.Network.CreateEndpointDataSource(tcp, family, (uint)Identity.ProcessId);
					foreach (var endpoint in await Tools.DataSources.ReadAllAsync(source, cancellationToken).ConfigureAwait(false))
						rows.Add(new NetworkEndpointInfo {
							Identity = Identity, ProcessId = Identity.ProcessId, ProcessName = _processName, Protocol = protocol, AddressFamily = addressFamily,
							LocalAddress = endpoint.LocalAddress.ToString(), LocalPort = endpoint.LocalPort, RemoteAddress = endpoint.RemoteAddress?.ToString() ?? string.Empty,
							RemotePort = endpoint.RemotePort, State = endpoint.State?.ToString() ?? "Bound"
						});
				}, capturedException => capturedException is not OperationCanceledException).ConfigureAwait(false);
				if (error != null)
					rows.Add(new NetworkEndpointInfo { Protocol = protocol, AddressFamily = addressFamily, Availability = error.Message });
			}
		cancellationToken.ThrowIfCancellationRequested();
		// Keep the selected process handle alive through all tables, then reject any inventory that crossed its exit.
		EnsureCurrent(process);
		return rows.OrderBy(row => row.Protocol).ThenBy(row => row.AddressFamily).ThenBy(row => row.LocalPort).ToArray();
	}

	private void EnsureCurrent(WindowsProcess process)
		=> Guard.Ensure(process.Identity == Identity && !process.HasExited, "The selected process exited or its PID was reused. Select a current process.");
}
