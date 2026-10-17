// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Text;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {
	public static partial class PSAPI {

		[DllImport("psapi.dll", EntryPoint = "GetProcessMemoryInfo", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetProcessMemoryInfo(KERNEL32.KernelHandle process, out PSAPI.PROCESS_MEMORY_COUNTERS_EX counters, uint size);
		[DllImport("psapi.dll", EntryPoint = "GetPerformanceInfo", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetPerformanceInfo(out PSAPI.PERFORMANCE_INFORMATION information, uint size);
		[StructLayout(LayoutKind.Sequential)]
		public struct PROCESS_MEMORY_COUNTERS_EX {
			public uint Size;
			public uint PageFaultCount;
			public UIntPtr PeakWorkingSetSize;
			public UIntPtr WorkingSetSize;
			public UIntPtr QuotaPeakPagedPoolUsage;
			public UIntPtr QuotaPagedPoolUsage;
			public UIntPtr QuotaPeakNonPagedPoolUsage;
			public UIntPtr QuotaNonPagedPoolUsage;
			public UIntPtr PagefileUsage;
			public UIntPtr PeakPagefileUsage;
			public UIntPtr PrivateUsage;
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct PERFORMANCE_INFORMATION {
			public uint Size;
			public UIntPtr CommitTotal;
			public UIntPtr CommitLimit;
			public UIntPtr CommitPeak;
			public UIntPtr PhysicalTotal;
			public UIntPtr PhysicalAvailable;
			public UIntPtr SystemCache;
			public UIntPtr KernelTotal;
			public UIntPtr KernelPaged;
			public UIntPtr KernelNonpaged;
			public UIntPtr PageSize;
			public uint HandleCount;
			public uint ProcessCount;
			public uint ThreadCount;
		}

	}
}
