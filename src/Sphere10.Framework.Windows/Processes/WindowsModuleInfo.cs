// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>Loader-listed modules; explicit details describe the captured memory image, not file trust or runtime architecture.</summary>
public sealed record WindowsModuleInfo(string Name, string FilePath, IntPtr? BaseAddress, int? SizeBytes) {
	public DateTime? CapturedAtUtc { get; init; }
	public ushort? Machine { get; init; }
	public string ImageHeaderArchitecture { get; init; } = string.Empty;
	public ushort? CoffCharacteristics { get; init; }
	public uint? CorFlags { get; init; }
	public uint? MappingType { get; init; }
	public string Classification { get; init; } = string.Empty;
	public string Availability { get; init; } = string.Empty;
}
