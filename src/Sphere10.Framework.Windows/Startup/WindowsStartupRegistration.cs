// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Startup;

/// <summary>Detached read-only launch-source provenance. Presence is not evidence that a registration has executed.</summary>
public sealed record WindowsStartupRegistration {
	public string Identity { get; init; } = string.Empty;
	public WindowsStartupSources Family { get; init; }
	public string Name { get; init; } = string.Empty;
	public string Scope { get; init; } = string.Empty;
	public string Location { get; init; } = string.Empty;
	public string Command { get; init; } = string.Empty;
	public string Executable { get; init; } = string.Empty;
	public string Arguments { get; init; } = string.Empty;
	public string WorkingDirectory { get; init; } = string.Empty;
	public string Trigger { get; init; } = string.Empty;
	public string SourceState { get; init; } = string.Empty;
	public string Availability { get; init; } = string.Empty;
	public RegistryAddress? RegistryAddress { get; init; }
	public string RegistryValueName { get; init; }
}
