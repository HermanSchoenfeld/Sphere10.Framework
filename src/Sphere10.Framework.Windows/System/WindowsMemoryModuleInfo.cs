// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

public sealed record WindowsMemoryModuleInfo {
	public string DeviceLocator { get; init; } = string.Empty;
	public string BankLabel { get; init; } = string.Empty;
	public string Manufacturer { get; init; } = string.Empty;
	public string PartNumber { get; init; } = string.Empty;
	public string SerialNumber { get; init; } = string.Empty;
	public ulong? CapacityBytes { get; init; }
	/// <summary>The provider's ConfiguredClockSpeed in MHz; zero denotes an unknown speed.</summary>
	public uint? ConfiguredClockMHz { get; init; }
	public uint? SMBIOSMemoryType { get; init; }
	public uint? FormFactor { get; init; }
	public uint? DataWidthBits { get; init; }
	public uint? TotalWidthBits { get; init; }
}

