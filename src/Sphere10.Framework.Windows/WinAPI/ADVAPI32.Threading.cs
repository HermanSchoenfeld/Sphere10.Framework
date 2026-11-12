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
	public static partial class ADVAPI32 {
		public const int WctMaximumNodeCount = 16;

		[DllImport("advapi32.dll", SetLastError = true, ExactSpelling = true)]
		public static extern IntPtr OpenThreadWaitChainSession(uint flags, IntPtr callback);

		[DllImport("advapi32.dll", ExactSpelling = true)]
		public static extern void CloseThreadWaitChainSession(IntPtr session);

		[DllImport("advapi32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetThreadWaitChain(IntPtr session, UIntPtr context, uint flags, uint threadId, ref uint nodeCount,
			[Out] WAITCHAIN_NODE_INFO[] nodes, [MarshalAs(UnmanagedType.Bool)] out bool cycle);

		// SDK wct.h: the lock union requires 8-byte alignment, including on x86.
		[StructLayout(LayoutKind.Explicit, Size = 280, CharSet = CharSet.Unicode)]
		public unsafe struct WAITCHAIN_NODE_INFO {
			[FieldOffset(0)] public uint ObjectType;
			[FieldOffset(4)] public uint ObjectStatus;
			[FieldOffset(8)] public fixed char ObjectName[128];
			[FieldOffset(264)] public long Timeout;
			[FieldOffset(272)] public int Alertable;
			[FieldOffset(8)] public uint ProcessId;
			[FieldOffset(12)] public uint ThreadId;
			[FieldOffset(16)] public uint WaitTime;
			[FieldOffset(20)] public uint ContextSwitches;
		}
	}
}
