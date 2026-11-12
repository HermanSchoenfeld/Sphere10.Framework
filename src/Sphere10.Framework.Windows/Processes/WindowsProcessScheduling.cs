// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

public sealed record WindowsProcessScheduling {
	public ProcessIdentity Identity { get; init; }
	public DateTime CapturedAtUtc { get; init; }
	/// <summary>Primary-group mask; null when multi-group or access restrictions prevent a reliable mask.</summary>
	public ulong? AffinityMask { get; init; }
	public ulong? SystemAffinityMask { get; init; }
	public uint[] DefaultCpuSetIds { get; init; } = Array.Empty<uint>();
	public WindowsCpuSet[] AvailableCpuSets { get; init; } = Array.Empty<WindowsCpuSet>();
	public string AffinityAvailability { get; init; } = string.Empty;
	public string CpuSetAvailability { get; init; } = string.Empty;
}
