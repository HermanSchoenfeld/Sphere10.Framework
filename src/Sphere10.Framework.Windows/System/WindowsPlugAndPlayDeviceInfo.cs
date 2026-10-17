// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

public sealed record WindowsPlugAndPlayDeviceInfo {
	public string DeviceId { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public string Manufacturer { get; init; } = string.Empty;
	public string DeviceClass { get; init; } = string.Empty;
	public string ClassGuid { get; init; } = string.Empty;
	public string Service { get; init; } = string.Empty;
	public string Status { get; init; } = string.Empty;
	public bool? Present { get; init; }
	public uint? ProblemCode { get; init; }
	public string[] HardwareIds { get; init; } = Array.Empty<string>();
	public string[] CompatibleIds { get; init; } = Array.Empty<string>();
}

