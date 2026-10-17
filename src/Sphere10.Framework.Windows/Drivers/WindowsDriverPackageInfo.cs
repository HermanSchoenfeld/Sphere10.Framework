// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

public sealed record WindowsDriverPackageInfo {
	public string DeviceName { get; init; }
	public string DeviceId { get; init; }
	public string DeviceClass { get; init; }
	public string DriverVersion { get; init; }
	public string Provider { get; init; }
	public bool? IsSigned { get; init; }
	public string Signer { get; init; }
	public string InfName { get; init; }
	public DateTime? DriverDate { get; init; }
}
