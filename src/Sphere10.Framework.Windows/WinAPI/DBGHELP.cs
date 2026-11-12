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
	/// <summary>Windows SDK DbgHelp ABI. Callers serialize all use of this process-wide native library.</summary>
	public static class DBGHELP {
		internal static readonly SynchronizedObject SyncRoot = new();

		[DllImport("dbghelp.dll", EntryPoint = "SymInitializeW", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SymInitialize(IntPtr process, string searchPath, [MarshalAs(UnmanagedType.Bool)] bool invadeProcess);

		[DllImport("dbghelp.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SymCleanup(IntPtr process);

		[DllImport("dbghelp.dll", ExactSpelling = true)]
		public static extern uint SymGetOptions();

		[DllImport("dbghelp.dll", ExactSpelling = true)]
		public static extern uint SymSetOptions(uint options);

		[DllImport("dbghelp.dll", EntryPoint = "SymLoadModuleExW", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern ulong SymLoadModuleEx(IntPtr process, IntPtr file, string imageName, string moduleName, ulong address, uint size, IntPtr data, uint flags);

		[DllImport("dbghelp.dll", EntryPoint = "SymFromAddrW", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SymFromAddr(IntPtr process, ulong address, out ulong displacement, IntPtr symbol);

		[DllImport("dbghelp.dll", EntryPoint = "SymGetLineFromAddrW64", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SymGetLineFromAddr(IntPtr process, ulong address, out uint displacement, ref IMAGEHLP_LINE64 line);

		[DllImport("dbghelp.dll", ExactSpelling = true)]
		public static extern IntPtr SymFunctionTableAccess64(IntPtr process, ulong address);

		[DllImport("dbghelp.dll", ExactSpelling = true)]
		public static extern ulong SymGetModuleBase64(IntPtr process, ulong address);

		[DllImport("dbghelp.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool StackWalk64(uint machine, IntPtr process, IntPtr thread, ref STACKFRAME64 frame, IntPtr context, ReadMemoryRoutine readMemory, FunctionTableRoutine functionTable, ModuleBaseRoutine moduleBase, IntPtr translateAddress);

		[DllImport("dbghelp.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MiniDumpWriteDump(KERNEL32.KernelHandle process, uint processId, Microsoft.Win32.SafeHandles.SafeFileHandle file, uint type, IntPtr exception, IntPtr userStreams, IntPtr callback);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public delegate bool ReadMemoryRoutine(IntPtr process, ulong address, IntPtr buffer, uint length, out uint bytesRead);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		public delegate IntPtr FunctionTableRoutine(IntPtr process, ulong address);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		public delegate ulong ModuleBaseRoutine(IntPtr process, ulong address);

		[StructLayout(LayoutKind.Sequential)]
		public struct ADDRESS64 {
			public ulong Offset;

			public ushort Segment;

			public uint Mode;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct KDHELP64 {
			public ulong Thread;

			public uint ThCallbackStack, ThCallbackBStore, NextCallback, FramePointer;

			public ulong KiCallUserMode, KeUserCallbackDispatcher, SystemRangeStart, KiUserExceptionDispatcher, StackBase, StackLimit;

			public uint BuildVersion, RetpolineStubFunctionTableSize;

			public ulong RetpolineStubFunctionTable;

			public uint RetpolineStubOffset, RetpolineStubSize;

			public ulong Reserved0, Reserved1;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct STACKFRAME64 {
			public ADDRESS64 AddrPC, AddrReturn, AddrFrame, AddrStack, AddrBStore;

			public IntPtr FuncTableEntry;

			public ulong Param0, Param1, Param2, Param3;

			public int Far, Virtual;

			public ulong Reserved0, Reserved1, Reserved2;

			public KDHELP64 KdHelp;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct IMAGEHLP_LINE64 {
			public uint SizeOfStruct;

			public IntPtr Key;

			public uint LineNumber;

			public IntPtr FileName;

			public ulong Address;
		}
	}
}
