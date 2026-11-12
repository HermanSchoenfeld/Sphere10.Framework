// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {
	public static partial class KERNEL32 {
		public const uint ProcessSetInformation = 0x0200;
		public const uint ProcessSetLimitedInformation = 0x2000;
		public const uint ProcessQueryInformation = 0x0400;

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DuplicateHandle(IntPtr sourceProcess, KernelHandle sourceHandle, IntPtr targetProcess, out KernelHandle targetHandle,
			uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint options);

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process2(KernelHandle process, out ushort processMachine, out ushort nativeMachine);

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetProcessMitigationPolicy(KernelHandle process, int policy, IntPtr buffer, UIntPtr length);

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetProcessAffinityMask(KernelHandle process, out UIntPtr processAffinity, out UIntPtr systemAffinity);

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetProcessAffinityMask(KernelHandle process, UIntPtr affinity);

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetProcessDefaultCpuSets(KernelHandle process, [Out] uint[] ids, uint count, out uint required);

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetProcessDefaultCpuSets(KernelHandle process, uint[] ids, uint count);

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetSystemCpuSetInformation(IntPtr buffer, uint length, out uint returnedLength, KernelHandle process, uint flags);
	}
}
