using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sphere10.Framework;

namespace SystemExpert.Screens;

public class EventLogDataSource : BulkFetchDataSource<EventLogEntryInfo> {
	private string _logName;

	public EventLogDataSource()
		: this("Application") {
	}

	public EventLogDataSource(string logName)
		: base(() => FetchEntries(logName)) {
		_logName = logName;
	}

	public string LogName {
		get => _logName;
		set {
			_logName = value;
			Invalidate();
		}
	}

	private static IExtendedList<EventLogEntryInfo> FetchEntries(string logName) {
		var results = new List<EventLogEntryInfo>();
		try {
			using var log = new EventLog(logName);
			var entries = log.Entries;
			// Read the most recent 2000 entries for performance
			var startIndex = Math.Max(0, entries.Count - 2000);
			for (var i = entries.Count - 1; i >= startIndex; i--) {
				try {
					var entry = entries[i];
					results.Add(new EventLogEntryInfo {
						Index = entry.Index,
						Source = entry.Source,
						Level = entry.EntryType.ToString(),
						TimeGenerated = entry.TimeGenerated,
						Message = TruncateMessage(entry.Message),
						LogName = logName,
					});
				} catch {
					// Skip entries we can't read
				}
			}
		} catch {
			// Log may not exist or access denied
		}
		return results.ToExtendedList();
	}

	private static string TruncateMessage(string message) {
		if (string.IsNullOrEmpty(message))
			return "";
		// Take first line, max 200 chars for grid display
		var firstLine = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? message;
		return firstLine.Length > 200 ? firstLine[..200] + "…" : firstLine;
	}

	public override IEnumerable<EventLogEntryInfo> NewRange(int count) => throw new NotSupportedException();
	public override void CreateRange(IEnumerable<EventLogEntryInfo> entities) => throw new NotSupportedException();

	public override DataSourceItems<EventLogEntryInfo> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		var query = Future.Value.AsEnumerable();

		if (!string.IsNullOrEmpty(searchTerm)) {
			query = query.Where(e =>
				e.Source.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
				e.Level.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
				e.Message.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
		}

		query = (sortProperty, sortDirection) switch {
			("Index", SortDirection.Ascending) => query.OrderBy(e => e.Index),
			("Index", SortDirection.Descending) => query.OrderByDescending(e => e.Index),
			("Source", SortDirection.Ascending) => query.OrderBy(e => e.Source, StringComparer.OrdinalIgnoreCase),
			("Source", SortDirection.Descending) => query.OrderByDescending(e => e.Source, StringComparer.OrdinalIgnoreCase),
			("Level", SortDirection.Ascending) => query.OrderBy(e => e.Level),
			("Level", SortDirection.Descending) => query.OrderByDescending(e => e.Level),
			("TimeGenerated", SortDirection.Ascending) => query.OrderBy(e => e.TimeGenerated),
			("TimeGenerated", SortDirection.Descending) => query.OrderByDescending(e => e.TimeGenerated),
			("Message", SortDirection.Ascending) => query.OrderBy(e => e.Message, StringComparer.OrdinalIgnoreCase),
			("Message", SortDirection.Descending) => query.OrderByDescending(e => e.Message, StringComparer.OrdinalIgnoreCase),
			_ => query.OrderByDescending(e => e.Index),
		};

		var totalItems = query.Count();

		if (pageLength * page > totalItems)
			page = Math.Max(0, (int)Math.Ceiling(totalItems / (decimal)pageLength) - 1);

		return new DataSourceItems<EventLogEntryInfo> {
			Items = query.Skip(pageLength * page).Take(pageLength).ToArray(),
			Page = page,
			TotalCount = totalItems
		};
	}

	public override void RefreshRange(EventLogEntryInfo[] entities) => throw new NotSupportedException();
	public override void UpdateRange(IEnumerable<EventLogEntryInfo> entities) => throw new NotSupportedException();
	public override void DeleteRange(IEnumerable<EventLogEntryInfo> entities) => throw new NotSupportedException();
	public override Result ValidateRange(IEnumerable<(EventLogEntryInfo entity, CrudAction action)> actions) => throw new NotSupportedException();

	public override DataSourceCapabilities Capabilities =>
		DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage;
}
