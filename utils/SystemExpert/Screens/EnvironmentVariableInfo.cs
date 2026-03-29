using System;

namespace SystemExpert.Screens;

public record EnvironmentVariableInfo {
	public string Name { get; init; }
	public string Value { get; init; }
	public string Scope { get; init; }

	public override string ToString() => $"{Name}={Value} ({Scope})";
}
