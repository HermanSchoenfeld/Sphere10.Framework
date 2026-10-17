// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

public sealed record WindowsMonitorInfo {
	public string InstanceName { get; init; } = string.Empty;
	public string Manufacturer { get; init; } = string.Empty;
	public string ProductCode { get; init; } = string.Empty;
	public string SerialNumber { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public bool? Active { get; init; }
	public ushort? ManufactureWeek { get; init; }
	public ushort? ManufactureYear { get; init; }
}

