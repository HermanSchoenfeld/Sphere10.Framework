// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>Detached UI Automation metadata, never a live provider, COM reference or action target.</summary>
public sealed record WindowsUiAutomationElement {
	public int NodeId { get; init; }
	public int? ParentNodeId { get; init; }
	public int Depth { get; init; }
	public string RuntimeId { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string ControlType { get; init; } = string.Empty;
	public string AutomationId { get; init; } = string.Empty;
	public string ClassName { get; init; } = string.Empty;
	public string Framework { get; init; } = string.Empty;
	public int? ProcessId { get; init; }
	public long? WindowHandle { get; init; }
	public string Bounds { get; init; } = string.Empty;
	public bool? Enabled { get; init; }
	public bool? Offscreen { get; init; }
	public string[] SupportedPatterns { get; init; } = Array.Empty<string>();
	public WindowsUiAutomationProperty[] Properties { get; init; } = Array.Empty<WindowsUiAutomationProperty>();
	public string Availability { get; init; } = string.Empty;
}
