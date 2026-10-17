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
	public static partial class NTDLL {
		[DllImport("ntdll.dll", ExactSpelling = true)]
		public static extern int NtQueryInformationProcess(KERNEL32.KernelHandle process, int informationClass, IntPtr information, uint informationLength, out uint returnLength);

		[DllImport("ntdll.dll", ExactSpelling = true)]
		public static extern uint RtlNtStatusToDosError(int status);

		public const int SystemExtendedHandleInformation = 64;
		public const int StatusInfoLengthMismatch = unchecked((int)0xC0000004);
		public const int StatusBufferTooSmall = unchecked((int)0xC0000023);

		// Extended native handle information is version-sensitive; callers must validate all returned bounds.
		// Layout reference: https://github.com/winsiderss/phnt/blob/master/ntexapi.h
		[DllImport("ntdll.dll", ExactSpelling = true)]
		public static extern int NtQuerySystemInformation(int informationClass, IntPtr information, uint informationLength, out uint returnLength);

		[StructLayout(LayoutKind.Sequential)]
		public struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX {
			public IntPtr Object;
			public UIntPtr ProcessId;
			public UIntPtr HandleValue;
			public uint GrantedAccess;
			public ushort CreatorBackTraceIndex;
			public ushort ObjectTypeIndex;
			public uint HandleAttributes;
			public uint Reserved;
		}
	}
}
