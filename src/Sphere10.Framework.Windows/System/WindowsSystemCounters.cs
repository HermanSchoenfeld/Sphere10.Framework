// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

public sealed record WindowsSystemCounters {
	public long PhysicalMemoryTotalBytes { get; init; }
	public long PhysicalMemoryAvailableBytes { get; init; }
	public int ProcessCount { get; init; }
	public int ThreadCount { get; init; }
	public int HandleCount { get; init; }
	public int LogicalProcessorCount { get; init; }
	public long CommitTotalBytes { get; init; }
	public long CommitLimitBytes { get; init; }
	public long KernelPagedBytes { get; init; }
	public long KernelNonPagedBytes { get; init; }
	public ulong? IdleTicks { get; init; }
	public ulong? KernelTicks { get; init; }
	public ulong? UserTicks { get; init; }
	public ushort? ProcessorGroup { get; init; }
}
