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
	public static partial class PSAPI {
		[DllImport("psapi.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool QueryWorkingSetEx(KERNEL32.KernelHandle process, [In, Out] WORKING_SET_EX_INFORMATION[] information, uint bytes);

		[DllImport("psapi.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool InitializeProcessForWsWatch(KERNEL32.KernelHandle process);

		[DllImport("psapi.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWsChangesEx(KERNEL32.KernelHandle process, [Out] WS_WATCH_INFORMATION_EX[] information, ref uint bytes);

		[StructLayout(LayoutKind.Sequential)]
		public struct WORKING_SET_EX_INFORMATION {
			public IntPtr VirtualAddress;

			public UIntPtr Flags;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct WS_WATCH_INFORMATION_EX {
			public IntPtr FaultingPc;

			public IntPtr FaultingVa;

			public UIntPtr FaultingThreadId;

			public UIntPtr Flags;
		}
	}
}
