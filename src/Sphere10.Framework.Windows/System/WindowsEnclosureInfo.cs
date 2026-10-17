// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

public sealed record WindowsEnclosureInfo {
	public string Manufacturer { get; init; } = string.Empty;
	public string Model { get; init; } = string.Empty;
	public string Version { get; init; } = string.Empty;
	public string SerialNumber { get; init; } = string.Empty;
	public string AssetTag { get; init; } = string.Empty;
	public ushort[] ChassisTypes { get; init; } = Array.Empty<ushort>();
}

