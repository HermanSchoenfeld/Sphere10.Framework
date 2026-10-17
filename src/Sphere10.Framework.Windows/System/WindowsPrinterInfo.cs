// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

public sealed record WindowsPrinterInfo {
	public string DeviceId { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string DriverName { get; init; } = string.Empty;
	public string PortName { get; init; } = string.Empty;
	public string Status { get; init; } = string.Empty;
	public bool? IsDefault { get; init; }
	public bool? IsNetwork { get; init; }
	public bool? IsLocal { get; init; }
	public bool? IsShared { get; init; }
	/// <summary>The provider's WorkOffline queueing mode; this is not a live connectivity test.</summary>
	public bool? IsOffline { get; init; }
	public uint? PrinterStatus { get; init; }
}

