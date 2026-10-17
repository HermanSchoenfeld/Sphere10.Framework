// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

public sealed record WindowsMemoryArrayInfo {
	public string Tag { get; init; } = string.Empty;
	public uint? Location { get; init; }
	public uint? Use { get; init; }
	public uint? MemoryDevices { get; init; }
	/// <summary>The maximum capacity normalized from provider kilobytes to bytes. Unknown or unrepresentable capacities remain null.</summary>
	public ulong? MaximumCapacityBytes { get; init; }
}

