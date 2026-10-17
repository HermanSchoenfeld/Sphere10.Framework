// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

public sealed record WindowsVideoControllerInfo {
	public string DeviceId { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string Manufacturer { get; init; } = string.Empty;
	public string VideoProcessor { get; init; } = string.Empty;
	public string PnpDeviceId { get; init; } = string.Empty;
	public string DriverVersion { get; init; } = string.Empty;
	public string Status { get; init; } = string.Empty;
	public DateTime? DriverDate { get; init; }
	/// <summary>The raw uint32 WMI AdapterRAM report in bytes. Its width cannot represent modern VRAM capacities authoritatively.</summary>
	public uint? ReportedMemoryBytes { get; init; }
	public uint? HorizontalResolution { get; init; }
	public uint? VerticalResolution { get; init; }
	public uint? RefreshRateHz { get; init; }
}

