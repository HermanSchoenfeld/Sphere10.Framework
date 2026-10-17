// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Storage;

public sealed record WindowsPhysicalDiskInfo {
	public string DeviceId { get; init; }
	public uint? Index { get; init; }
	public string Model { get; init; }
	public string InterfaceType { get; init; }
	public string SerialNumber { get; init; }
	public ulong? SizeBytes { get; init; }
	public string Status { get; init; }
	public Exception ReadError { get; init; }
}
