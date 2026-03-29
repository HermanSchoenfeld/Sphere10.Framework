using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sphere10.Framework;

public class ProcessDataSource : BulkFetchDataSource<Process> {

	public ProcessDataSource()
		: base(() => Process.GetProcesses().ToExtendedList()) {
	}

	public override IEnumerable<Process> NewRange(int count) => throw new NotSupportedException();

	public override void CreateRange(IEnumerable<Process> entities) => throw new NotSupportedException();

	public override DataSourceItems<Process> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		var query = Future.Value.AsEnumerable();

		if (!string.IsNullOrEmpty(searchTerm)) {
			query = query.Where(p =>
				p.ProcessName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
				p.Id.ToString().Contains(searchTerm));
		}

		query = (sortProperty, sortDirection) switch {
			("PID", SortDirection.Ascending) => query.OrderBy(p => p.Id),
			("PID", SortDirection.Descending) => query.OrderByDescending(p => p.Id),
			("Name", SortDirection.Ascending) => query.OrderBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase),
			("Name", SortDirection.Descending) => query.OrderByDescending(p => p.ProcessName, StringComparer.OrdinalIgnoreCase),
			("MemoryMB", SortDirection.Ascending) => query.OrderBy(p => p.WorkingSet64),
			("MemoryMB", SortDirection.Descending) => query.OrderByDescending(p => p.WorkingSet64),
			("ThreadCount", SortDirection.Ascending) => query.OrderBy(p => Tools.Exceptions.TryOrDefault(() => p.Threads.Count, 0)),
			("ThreadCount", SortDirection.Descending) => query.OrderByDescending(p => Tools.Exceptions.TryOrDefault(() => p.Threads.Count, 0)),
			("Priority", SortDirection.Ascending) => query.OrderBy(p => Tools.Exceptions.TryOrDefault(() => p.PriorityClass.ToString(), "N/A"), StringComparer.OrdinalIgnoreCase),
			("Priority", SortDirection.Descending) => query.OrderByDescending(p => Tools.Exceptions.TryOrDefault(() => p.PriorityClass.ToString(), "N/A"), StringComparer.OrdinalIgnoreCase),
			("Responding", SortDirection.Ascending) => query.OrderBy(p => Tools.Exceptions.TryOrDefault(() => p.Responding, false)),
			("Responding", SortDirection.Descending) => query.OrderByDescending(p => Tools.Exceptions.TryOrDefault(() => p.Responding, false)),
			_ => query.OrderBy(p => p.Id),
		};

		var totalItems = query.Count();

		if (pageLength * page > totalItems)
			page = Math.Max(0, (int)Math.Ceiling(totalItems / (decimal)pageLength) - 1);

		return new DataSourceItems<Process> {
			Items = query.Skip(pageLength * page).Take(pageLength).ToArray(),
			Page = page,
			TotalCount = totalItems
		};
	}

	public override void RefreshRange(Process[] entities) => throw new NotSupportedException();

	public override void UpdateRange(IEnumerable<Process> entities) => throw new NotSupportedException();

	public override void DeleteRange(IEnumerable<Process> entities) => throw new NotSupportedException();

	public override Result ValidateRange(IEnumerable<(Process entity, CrudAction action)> actions)=> throw new NotSupportedException();

	public override DataSourceCapabilities Capabilities =>
		DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage;

}
