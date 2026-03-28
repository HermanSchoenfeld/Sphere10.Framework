using System.Diagnostics;

namespace SystemExpert.Screens;

public record ProcessInfo {
	public int PID { get; init; }
	public string Name { get; init; }
	public double MemoryMB { get; init; }
	public int ThreadCount { get; init; }
	public string Priority { get; init; }
	public bool Responding { get; init; }

	public override string ToString() => $"{Name} ({PID})";

	public static ProcessInfo FromProcess(Process process) {
		return new ProcessInfo {
			PID = process.Id,
			Name = process.ProcessName,
			MemoryMB = process.WorkingSet64 / (1024.0 * 1024.0),
			ThreadCount = process.Threads.Count,
			Priority = GetPriority(process),
			Responding = GetResponding(process),
		};
	}

	private static string GetPriority(Process proc) {
		try { return proc.PriorityClass.ToString(); }
		catch { return "N/A"; }
	}

	private static bool GetResponding(Process proc) {
		try { return proc.Responding; }
		catch { return false; }
	}
}
