namespace SystemExpert.Screens;

public record ServiceInfo {
	public string Name { get; init; }
	public string DisplayName { get; init; }
	public string Status { get; init; }
	public string StartupType { get; init; }
	public string Account { get; init; }

	public override string ToString() => $"{DisplayName} ({Name})";
}
