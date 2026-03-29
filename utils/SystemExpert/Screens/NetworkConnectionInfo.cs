namespace SystemExpert.Screens;

public record NetworkConnectionInfo {
	public string Protocol { get; init; }
	public string LocalAddress { get; init; }
	public int LocalPort { get; init; }
	public string RemoteAddress { get; init; }
	public int RemotePort { get; init; }
	public string State { get; init; }
	public int OwningPid { get; init; }
	public string ProcessName { get; init; }

	public override string ToString() => $"{Protocol} {LocalAddress}:{LocalPort} → {RemoteAddress}:{RemotePort} ({State})";
}
