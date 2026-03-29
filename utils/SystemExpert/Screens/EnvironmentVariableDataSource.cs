using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sphere10.Framework;

namespace SystemExpert.Screens;

public class EnvironmentVariableDataSource : BulkFetchDataSource<EnvironmentVariableInfo> {

	public EnvironmentVariableDataSource()
		: base(FetchVariables) {
	}

	private static IExtendedList<EnvironmentVariableInfo> FetchVariables() {
		var results = new List<EnvironmentVariableInfo>();

		AddVariables(results, EnvironmentVariableTarget.Process, "Process");
		AddVariables(results, EnvironmentVariableTarget.User, "User");
		try {
			AddVariables(results, EnvironmentVariableTarget.Machine, "Machine");
		} catch {
			// Requires elevation — silently skip
		}

		return results
			.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
			.ThenBy(v => v.Scope)
			.ToExtendedList();
	}

	private static void AddVariables(List<EnvironmentVariableInfo> results, EnvironmentVariableTarget target, string scope) {
		var vars = Environment.GetEnvironmentVariables(target);
		foreach (DictionaryEntry entry in vars) {
			results.Add(new EnvironmentVariableInfo {
				Name = entry.Key?.ToString() ?? "",
				Value = entry.Value?.ToString() ?? "",
				Scope = scope,
			});
		}
	}

	public override IEnumerable<EnvironmentVariableInfo> NewRange(int count) => throw new NotSupportedException();
	public override void CreateRange(IEnumerable<EnvironmentVariableInfo> entities) => throw new NotSupportedException();

	public override DataSourceItems<EnvironmentVariableInfo> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		var query = Future.Value.AsEnumerable();

		if (!string.IsNullOrEmpty(searchTerm)) {
			query = query.Where(v =>
				v.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
				v.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
				v.Scope.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
		}

		query = (sortProperty, sortDirection) switch {
			("Name", SortDirection.Ascending) => query.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase),
			("Name", SortDirection.Descending) => query.OrderByDescending(v => v.Name, StringComparer.OrdinalIgnoreCase),
			("Value", SortDirection.Ascending) => query.OrderBy(v => v.Value, StringComparer.OrdinalIgnoreCase),
			("Value", SortDirection.Descending) => query.OrderByDescending(v => v.Value, StringComparer.OrdinalIgnoreCase),
			("Scope", SortDirection.Ascending) => query.OrderBy(v => v.Scope),
			("Scope", SortDirection.Descending) => query.OrderByDescending(v => v.Scope),
			_ => query.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase),
		};

		var totalItems = query.Count();

		if (pageLength * page > totalItems)
			page = Math.Max(0, (int)Math.Ceiling(totalItems / (decimal)pageLength) - 1);

		return new DataSourceItems<EnvironmentVariableInfo> {
			Items = query.Skip(pageLength * page).Take(pageLength).ToArray(),
			Page = page,
			TotalCount = totalItems
		};
	}

	public override void RefreshRange(EnvironmentVariableInfo[] entities) => throw new NotSupportedException();
	public override void UpdateRange(IEnumerable<EnvironmentVariableInfo> entities) => throw new NotSupportedException();
	public override void DeleteRange(IEnumerable<EnvironmentVariableInfo> entities) => throw new NotSupportedException();
	public override Result ValidateRange(IEnumerable<(EnvironmentVariableInfo entity, CrudAction action)> actions) => throw new NotSupportedException();

	public override DataSourceCapabilities Capabilities =>
		DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage;
}
