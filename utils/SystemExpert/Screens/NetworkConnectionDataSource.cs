using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using Sphere10.Framework;

namespace SystemExpert.Screens;

public class NetworkConnectionDataSource : BulkFetchDataSource<NetworkConnectionInfo> {

	public NetworkConnectionDataSource()
		: base(FetchConnections) {
	}

	private static IExtendedList<NetworkConnectionInfo> FetchConnections() {
		var processNames = new Dictionary<int, string>();
		try {
			foreach (var p in Process.GetProcesses()) {
				try { processNames[p.Id] = p.ProcessName; }
				catch { /* access denied */ }
				finally { p.Dispose(); }
			}
		} catch { /* ignore */ }

		var results = new List<NetworkConnectionInfo>();
		var ipProperties = IPGlobalProperties.GetIPGlobalProperties();

		try {
			foreach (var conn in ipProperties.GetActiveTcpConnections()) {
				processNames.TryGetValue(0, out _);
				results.Add(new NetworkConnectionInfo {
					Protocol = "TCP",
					LocalAddress = conn.LocalEndPoint.Address.ToString(),
					LocalPort = conn.LocalEndPoint.Port,
					RemoteAddress = conn.RemoteEndPoint.Address.ToString(),
					RemotePort = conn.RemoteEndPoint.Port,
					State = conn.State.ToString(),
					OwningPid = 0,
					ProcessName = "",
				});
			}
		} catch { /* ignore */ }

		try {
			foreach (var listener in ipProperties.GetActiveTcpListeners()) {
				results.Add(new NetworkConnectionInfo {
					Protocol = "TCP",
					LocalAddress = listener.Address.ToString(),
					LocalPort = listener.Port,
					RemoteAddress = "*",
					RemotePort = 0,
					State = "Listening",
					OwningPid = 0,
					ProcessName = "",
				});
			}
		} catch { /* ignore */ }

		try {
			foreach (var listener in ipProperties.GetActiveUdpListeners()) {
				results.Add(new NetworkConnectionInfo {
					Protocol = "UDP",
					LocalAddress = listener.Address.ToString(),
					LocalPort = listener.Port,
					RemoteAddress = "*",
					RemotePort = 0,
					State = "",
					OwningPid = 0,
					ProcessName = "",
				});
			}
		} catch { /* ignore */ }

		return results.ToExtendedList();
	}

	public override IEnumerable<NetworkConnectionInfo> NewRange(int count) => throw new NotSupportedException();
	public override void CreateRange(IEnumerable<NetworkConnectionInfo> entities) => throw new NotSupportedException();

	public override DataSourceItems<NetworkConnectionInfo> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		var query = Future.Value.AsEnumerable();

		if (!string.IsNullOrEmpty(searchTerm)) {
			query = query.Where(c =>
				c.Protocol.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
				c.LocalAddress.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
				c.LocalPort.ToString().Contains(searchTerm) ||
				c.RemoteAddress.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
				c.RemotePort.ToString().Contains(searchTerm) ||
				c.State.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
				c.ProcessName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
		}

		query = (sortProperty, sortDirection) switch {
			("Protocol", SortDirection.Ascending) => query.OrderBy(c => c.Protocol),
			("Protocol", SortDirection.Descending) => query.OrderByDescending(c => c.Protocol),
			("LocalAddress", SortDirection.Ascending) => query.OrderBy(c => c.LocalAddress),
			("LocalAddress", SortDirection.Descending) => query.OrderByDescending(c => c.LocalAddress),
			("LocalPort", SortDirection.Ascending) => query.OrderBy(c => c.LocalPort),
			("LocalPort", SortDirection.Descending) => query.OrderByDescending(c => c.LocalPort),
			("RemoteAddress", SortDirection.Ascending) => query.OrderBy(c => c.RemoteAddress),
			("RemoteAddress", SortDirection.Descending) => query.OrderByDescending(c => c.RemoteAddress),
			("RemotePort", SortDirection.Ascending) => query.OrderBy(c => c.RemotePort),
			("RemotePort", SortDirection.Descending) => query.OrderByDescending(c => c.RemotePort),
			("State", SortDirection.Ascending) => query.OrderBy(c => c.State),
			("State", SortDirection.Descending) => query.OrderByDescending(c => c.State),
			_ => query.OrderBy(c => c.Protocol).ThenBy(c => c.LocalPort),
		};

		var totalItems = query.Count();

		if (pageLength * page > totalItems)
			page = Math.Max(0, (int)Math.Ceiling(totalItems / (decimal)pageLength) - 1);

		return new DataSourceItems<NetworkConnectionInfo> {
			Items = query.Skip(pageLength * page).Take(pageLength).ToArray(),
			Page = page,
			TotalCount = totalItems
		};
	}

	public override void RefreshRange(NetworkConnectionInfo[] entities) => throw new NotSupportedException();
	public override void UpdateRange(IEnumerable<NetworkConnectionInfo> entities) => throw new NotSupportedException();
	public override void DeleteRange(IEnumerable<NetworkConnectionInfo> entities) => throw new NotSupportedException();
	public override Result ValidateRange(IEnumerable<(NetworkConnectionInfo entity, CrudAction action)> actions) => throw new NotSupportedException();

	public override DataSourceCapabilities Capabilities =>
		DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage;
}
