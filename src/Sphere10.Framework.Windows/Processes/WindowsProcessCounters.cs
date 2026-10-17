// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;

namespace Sphere10.Framework.Windows;

public sealed record WindowsProcessCounters {
	public ProcessIdentity Identity { get; init; }
	public TimeSpan? CpuTime { get; init; }
	public ulong? ReadBytes { get; init; }
	public ulong? WriteBytes { get; init; }
	public ulong? OtherBytes { get; init; }
	public long? WorkingSetBytes { get; init; }
	public long? PrivateMemoryBytes { get; init; }
	public int? HandleCount { get; init; }
	public int? SessionId { get; init; }
	public ProcessPriorityClass? Priority { get; init; }
}
