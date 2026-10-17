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

/// <summary>Detached process inventory and raw counters. No rates, sampling history or display policy is applied.</summary>
public sealed record WindowsProcessInventoryEntry {
	public int ProcessId { get; init; }
	public int ParentProcessId { get; init; }
	public string ExecutableName { get; init; }
	public int ThreadCount { get; init; }
	public string ExecutablePath { get; init; } = string.Empty;
	public WindowsProcessCounters Counters { get; init; }
	public DateTime CapturedAtUtc { get; init; }
	public long CounterTimestamp { get; init; }
	public Exception ReadError { get; init; }
	public Exception ExecutablePathError { get; init; }
	public ProcessIdentity Identity => Counters?.Identity ?? new ProcessIdentity(ProcessId, null);
	public TimeSpan? CpuTime => Counters?.CpuTime;
	public ulong? ReadBytes => Counters?.ReadBytes;
	public ulong? WriteBytes => Counters?.WriteBytes;
	public ulong? OtherBytes => Counters?.OtherBytes;
	public long? WorkingSetBytes => Counters?.WorkingSetBytes;
	public long? PrivateMemoryBytes => Counters?.PrivateMemoryBytes;
	public int? HandleCount => Counters?.HandleCount;
	public int? SessionId => Counters?.SessionId;
	public ProcessPriorityClass? Priority => Counters?.Priority;
}
