// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>One active CCD target path, its exact adapter/monitor identities and optional native metadata.</summary>
public sealed record WindowsDisplayInfo {
	public string Id { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string GdiDeviceName { get; init; } = string.Empty;
	public string MonitorDevicePath { get; init; } = string.Empty;
	public string AdapterDevicePath { get; init; } = string.Empty;
	public string AdapterName { get; init; } = string.Empty;
	public string DriverVersion { get; init; } = string.Empty;
	public DateTime? DriverDate { get; init; }
	public string Connector { get; init; } = string.Empty;
	public uint ConnectorInstance { get; init; }
	public uint SourceId { get; init; }
	public uint TargetId { get; init; }
	public uint PathFlags { get; init; }
	public uint SourceStatusFlags { get; init; }
	public uint TargetStatusFlags { get; init; }
	public string Rotation { get; init; } = string.Empty;
	public string Scaling { get; init; } = string.Empty;
	public int? DesktopX { get; init; }
	public int? DesktopY { get; init; }
	public uint? DesktopWidth { get; init; }
	public uint? DesktopHeight { get; init; }
	public uint? DesktopBitsPerPixel { get; init; }
	public uint? LogicalDpi { get; init; }
	public WindowsDisplayTiming ActiveTiming { get; init; }
	public WindowsDisplayTiming PreferredTiming { get; init; }
	public bool? AdvancedColorSupported { get; init; }
	public bool? AdvancedColorEnabled { get; init; }
	public bool? AdvancedColorForceDisabled { get; init; }
	public bool? WideColorEnforced { get; init; }
	public bool? HdrSupported { get; init; }
	public bool? HdrUserEnabled { get; init; }
	public bool? WideColorSupported { get; init; }
	public bool? AdvancedColorLimitedByPolicy { get; init; }
	public string ActiveColorMode { get; init; } = string.Empty;
	public string ColorEncoding { get; init; } = string.Empty;
	public uint? BitsPerColorChannel { get; init; }
	public double? SdrWhiteLevelNits { get; init; }
	public WindowsMonitorInfo Identity { get; init; }
	public WindowsMonitorEdid Edid { get; init; }
	public WindowsDisplayMode[] Modes { get; init; } = Array.Empty<WindowsDisplayMode>();
	public string[] Availability { get; init; } = Array.Empty<string>();
}
