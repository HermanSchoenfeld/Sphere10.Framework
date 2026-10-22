// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

public sealed record WindowsDisplayTiming {
	public uint ActiveWidth { get; init; }
	public uint ActiveHeight { get; init; }
	public uint TotalWidth { get; init; }
	public uint TotalHeight { get; init; }
	public ulong PixelClockHz { get; init; }
	public double? HorizontalSyncHz { get; init; }
	public double? VerticalSyncHz { get; init; }
	public string ScanLineOrdering { get; init; } = string.Empty;
}
