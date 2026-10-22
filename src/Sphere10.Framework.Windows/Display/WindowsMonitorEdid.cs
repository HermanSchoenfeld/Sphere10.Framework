// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;

namespace Sphere10.Framework.Windows;

/// <summary>Validated EDID 1.x base-block metadata. Raw bytes are the Windows registry cache, not a fresh DDC read.</summary>
public sealed record WindowsMonitorEdid {
	public bool ValidHeader { get; init; }
	public bool[] BlockChecksums { get; init; } = Array.Empty<bool>();
	public bool Complete { get; init; }
	public bool OverridePresent { get; init; }
	public string Version { get; init; } = string.Empty;
	public bool? DigitalInput { get; init; }
	public byte? WidthCm { get; init; }
	public byte? HeightCm { get; init; }
	public double? Gamma { get; init; }
	public double? DiagonalInches { get; init; }
	public string RawHex { get; init; } = string.Empty;

	public static WindowsMonitorEdid Parse(byte[] bytes, bool overridePresent = false) {
		Guard.ArgumentNotNull(bytes, nameof(bytes));
		var header = bytes.Length >= 128 && bytes.Take(8).SequenceEqual(new byte[] { 0, 255, 255, 255, 255, 255, 255, 0 });
		var checksums = Enumerable.Range(0, bytes.Length / 128).Select(block => bytes.Skip(block * 128).Take(128).Sum(value => (int)value) % 256 == 0).ToArray();
		var valid = header && checksums[0] && bytes[18] == 1;
		var width = valid && bytes[21] > 0 && bytes[22] > 0 ? (byte?)bytes[21] : null;
		var height = valid && bytes[21] > 0 && bytes[22] > 0 ? (byte?)bytes[22] : null;
		return new WindowsMonitorEdid {
			ValidHeader = header, BlockChecksums = checksums, Complete = header && bytes.Length >= (bytes[126] + 1) * 128,
			OverridePresent = overridePresent, Version = valid ? $"{bytes[18]}.{bytes[19]}" : string.Empty,
			DigitalInput = valid ? (bytes[20] & 128) != 0 : null, WidthCm = width, HeightCm = height,
			Gamma = valid && bytes[23] != 255 ? (bytes[23] + 100) / 100.0 : null,
			DiagonalInches = width.HasValue ? Math.Sqrt(width.Value * width.Value + height.Value * height.Value) / 2.54 : null,
			RawHex = Convert.ToHexString(bytes)
		};
	}
}
