// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Text.Json.Serialization;

namespace Sphere10.Framework.Windows;

/// <summary>One bounded media read. Partial reads contain verified regions but may omit other media when discovery reaches its limits.</summary>
public sealed record WindowsUiAutomationMediaCapture {
	[JsonIgnore]
	public WindowsWindowInfo Window { get; init; }
	public WindowsUiAutomationMediaRegion[] Regions { get; init; } = Array.Empty<WindowsUiAutomationMediaRegion>();
	/// <summary>Known media that is out of view just now, with empty bounds: where it lies and what would show it once the content scrolls.</summary>
	public WindowsUiAutomationMediaRegion[] Upcoming { get; init; } = Array.Empty<WindowsUiAutomationMediaRegion>();
	/// <summary>Start of the geometry refresh, after provider discovery; bounds are no older than this timestamp.</summary>
	public DateTime CapturedAtUtc { get; init; }
	public bool IsAvailable { get; init; }
	public bool IsPartial { get; init; }
	public string Availability { get; init; } = string.Empty;
}
