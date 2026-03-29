using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using Sphere10.Framework;

namespace SystemExpert.Screens;

public class ServiceDataSource : BulkFetchDataSource<ServiceInfo> {

	public ServiceDataSource()
		: base(FetchServices) {
	}

	private static IExtendedList<ServiceInfo> FetchServices() {
		var wmiLookup = BuildWmiLookup();
		return ServiceController.GetServices()
			.Select(sc => ToServiceInfo(sc, wmiLookup))
			.ToExtendedList();
	}

	private static Dictionary<string, (string StartMode, string StartName)> BuildWmiLookup() {
		var lookup = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
		try {
			using var searcher = new ManagementObjectSearcher("SELECT Name, StartMode, StartName FROM Win32_Service");
			foreach (var obj in searcher.Get()) {
				var name = obj["Name"]?.ToString();
				if (name != null) {
					lookup[name] = (
						obj["StartMode"]?.ToString() ?? "Unknown",
						obj["StartName"]?.ToString() ?? "N/A"
					);
				}
			}
		} catch {
			// WMI unavailable — fall back to defaults
		}
		return lookup;
	}

	private static ServiceInfo ToServiceInfo(ServiceController sc, Dictionary<string, (string StartMode, string StartName)> wmiLookup) {
		string status;
		try { status = sc.Status.ToString(); }
		catch { status = "Unknown"; }

		var startupType = "Unknown";
		var account = "N/A";
		if (wmiLookup.TryGetValue(sc.ServiceName, out var wmiInfo)) {
			startupType = wmiInfo.StartMode;
			account = wmiInfo.StartName;
		}

		return new ServiceInfo {
			Name = sc.ServiceName,
			DisplayName = sc.DisplayName,
			Status = status,
			StartupType = startupType,
			Account = account,
		};
	}

	public override IEnumerable<ServiceInfo> NewRange(int count) => throw new NotSupportedException();
	public override void CreateRange(IEnumerable<ServiceInfo> entities) => throw new NotSupportedException();

	public override DataSourceItems<ServiceInfo> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		var query = Future.Value.AsEnumerable();

		if (!string.IsNullOrEmpty(searchTerm)) {
			query = query.Where(s =>
				s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
				s.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
				s.Status.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
		}

		query = (sortProperty, sortDirection) switch {
			("Name", SortDirection.Ascending) => query.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase),
			("Name", SortDirection.Descending) => query.OrderByDescending(s => s.Name, StringComparer.OrdinalIgnoreCase),
			("DisplayName", SortDirection.Ascending) => query.OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase),
			("DisplayName", SortDirection.Descending) => query.OrderByDescending(s => s.DisplayName, StringComparer.OrdinalIgnoreCase),
			("Status", SortDirection.Ascending) => query.OrderBy(s => s.Status, StringComparer.OrdinalIgnoreCase),
			("Status", SortDirection.Descending) => query.OrderByDescending(s => s.Status, StringComparer.OrdinalIgnoreCase),
			("StartupType", SortDirection.Ascending) => query.OrderBy(s => s.StartupType, StringComparer.OrdinalIgnoreCase),
			("StartupType", SortDirection.Descending) => query.OrderByDescending(s => s.StartupType, StringComparer.OrdinalIgnoreCase),
			("Account", SortDirection.Ascending) => query.OrderBy(s => s.Account, StringComparer.OrdinalIgnoreCase),
			("Account", SortDirection.Descending) => query.OrderByDescending(s => s.Account, StringComparer.OrdinalIgnoreCase),
			_ => query.OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase),
		};

		var totalItems = query.Count();

		if (pageLength * page > totalItems)
			page = Math.Max(0, (int)Math.Ceiling(totalItems / (decimal)pageLength) - 1);

		return new DataSourceItems<ServiceInfo> {
			Items = query.Skip(pageLength * page).Take(pageLength).ToArray(),
			Page = page,
			TotalCount = totalItems
		};
	}

	public override void RefreshRange(ServiceInfo[] entities) => throw new NotSupportedException();
	public override void UpdateRange(IEnumerable<ServiceInfo> entities) => throw new NotSupportedException();
	public override void DeleteRange(IEnumerable<ServiceInfo> entities) => throw new NotSupportedException();
	public override Result ValidateRange(IEnumerable<(ServiceInfo entity, CrudAction action)> actions) => throw new NotSupportedException();

	public override DataSourceCapabilities Capabilities =>
		DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage;
}
