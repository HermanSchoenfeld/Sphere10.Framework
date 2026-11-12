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
		/// <summary>Requests a timer that is not limited to the system clock tick (Windows 10 version 1803 and later).</summary>
		public const uint CREATE_WAITABLE_TIMER_HIGH_RESOLUTION = 0x00000002;
		public const uint TIMER_ALL_ACCESS = 0x001F0003;
		public const uint WAIT_OBJECT_0 = 0x00000000;
		public const uint WAIT_TIMEOUT = 0x00000102;

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern KernelHandle CreateWaitableTimerExW(IntPtr attributes, string name, uint flags, uint desiredAccess);

		/// <summary>A negative due time is relative, in 100 nanosecond units; a zero period fires once.</summary>
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetWaitableTimer(KernelHandle timer, ref long dueTime, int period, IntPtr completionRoutine, IntPtr completionArgument, [MarshalAs(UnmanagedType.Bool)] bool resume);

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CancelWaitableTimer(KernelHandle timer);
	}
}
