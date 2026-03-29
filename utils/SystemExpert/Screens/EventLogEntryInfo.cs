using System;

namespace SystemExpert.Screens;

public record EventLogEntryInfo {
	public long Index { get; init; }
	public string Source { get; init; }
	public string Level { get; init; }
	public DateTime TimeGenerated { get; init; }
	public string Message { get; init; }
	public string LogName { get; init; }

	public override string ToString() => $"[{Level}] {Source}: {Message}";
}
