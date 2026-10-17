// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

/// <summary>Queries raw operating-system counters. Callers own sampling and aggregation.</summary>
public static class WindowsSystemInformation {
	public static int LogicalProcessorCount => checked((int)WinAPI.KERNEL32.GetActiveProcessorCount(WinAPI.KERNEL32.AllProcessorGroups));

	public static WindowsSystemCounters GetCounters() {
		var memory = new WinAPI.KERNEL32.MEMORYSTATUSEX { Length = (uint)Marshal.SizeOf<WinAPI.KERNEL32.MEMORYSTATUSEX>() };
		if (!WinAPI.KERNEL32.GlobalMemoryStatusEx(ref memory))
			throw new Win32Exception(Marshal.GetLastWin32Error());
		if (!WinAPI.PSAPI.GetPerformanceInfo(out var performance, (uint)Marshal.SizeOf<WinAPI.PSAPI.PERFORMANCE_INFORMATION>()))
			throw new Win32Exception(Marshal.GetLastWin32Error());
		var hasGroup = WinAPI.KERNEL32.GetThreadGroupAffinity(WinAPI.KERNEL32.GetCurrentThread(), out var affinity);
		var hasTimes = WinAPI.KERNEL32.GetSystemTimes(out var idle, out var kernel, out var user);
		return new WindowsSystemCounters {
			PhysicalMemoryTotalBytes = checked((long)memory.TotalPhysical), PhysicalMemoryAvailableBytes = checked((long)memory.AvailablePhysical),
			ProcessCount = checked((int)performance.ProcessCount), ThreadCount = checked((int)performance.ThreadCount), HandleCount = checked((int)performance.HandleCount),
			LogicalProcessorCount = LogicalProcessorCount, CommitTotalBytes = Bytes(performance.CommitTotal, performance.PageSize),
			CommitLimitBytes = Bytes(performance.CommitLimit, performance.PageSize), KernelPagedBytes = Bytes(performance.KernelPaged, performance.PageSize),
			KernelNonPagedBytes = Bytes(performance.KernelNonpaged, performance.PageSize), IdleTicks = hasTimes ? Ticks(idle) : null,
			KernelTicks = hasTimes ? Ticks(kernel) : null, UserTicks = hasTimes ? Ticks(user) : null, ProcessorGroup = hasGroup ? affinity.Group : null
		};
	}

	private static ulong Ticks(WinAPI.KERNEL32.FILETIME value) => ((ulong)value.DateTimeHigh << 32) | value.DateTimeLow;

	private static long Bytes(UIntPtr pages, UIntPtr size) => checked((long)(pages.ToUInt64() * size.ToUInt64()));
}
