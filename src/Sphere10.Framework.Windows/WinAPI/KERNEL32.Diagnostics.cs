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
		[DllImport("kernel32.dll", ExactSpelling = true)]
		public static extern uint PssCaptureSnapshot(KernelHandle process, uint flags, uint contextFlags, out IntPtr snapshot);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		public static extern uint PssQuerySnapshot(IntPtr snapshot, int informationClass, out IntPtr information, uint length);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		public static extern uint PssFreeSnapshot(IntPtr process, IntPtr snapshot);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		public static extern uint PssWalkMarkerCreate(IntPtr allocator, out IntPtr marker);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		public static extern uint PssWalkMarkerFree(IntPtr marker);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		public static extern uint PssWalkSnapshot(IntPtr snapshot, int informationClass, IntPtr marker, out PSS_THREAD_ENTRY entry, uint length);

		[DllImport("kernel32.dll", ExactSpelling = true, EntryPoint = "GetProcessId")]
		public static extern uint GetSnapshotProcessId(IntPtr process);

		[DllImport("kernel32.dll", ExactSpelling = true, EntryPoint = "TerminateProcess", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool TerminateSnapshotClone(IntPtr process, uint exitCode);
		// Windows SDK ProcessSnapshot.h. ContextRecord is copied before advancing/freeing the walk marker.
		[StructLayout(LayoutKind.Sequential)]
		public struct PSS_THREAD_ENTRY {
			public uint ExitStatus;

			public IntPtr TebBaseAddress;

			public uint ProcessId;

			public uint ThreadId;

			public UIntPtr AffinityMask;

			public int Priority;

			public int BasePriority;

			public IntPtr LastSyscallFirstArgument;

			public ushort LastSyscallNumber;

			public System.Runtime.InteropServices.ComTypes.FILETIME CreateTime;

			public System.Runtime.InteropServices.ComTypes.FILETIME ExitTime;

			public System.Runtime.InteropServices.ComTypes.FILETIME KernelTime;

			public System.Runtime.InteropServices.ComTypes.FILETIME UserTime;

			public IntPtr Win32StartAddress;

			public System.Runtime.InteropServices.ComTypes.FILETIME CaptureTime;

			public uint Flags;

			public ushort SuspendCount;

			public ushort SizeOfContextRecord;

			public IntPtr ContextRecord;
		}
	}
}
