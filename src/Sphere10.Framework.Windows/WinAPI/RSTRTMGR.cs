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
	public static partial class RSTRTMGR {

		[DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
		public static extern uint RmStartSession(out uint sessionHandle, uint flags, StringBuilder sessionKey);

		[DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
		public static extern uint RmRegisterResources(uint sessionHandle, uint fileCount, [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] files,
			uint applicationCount, IntPtr applications, uint serviceCount, IntPtr serviceNames);

		[DllImport("rstrtmgr.dll")]
		public static extern uint RmGetList(uint sessionHandle, out uint needed, ref uint count, [In, Out] RM_PROCESS_INFO[] applications, ref uint rebootReasons);

		[DllImport("rstrtmgr.dll")]
		public static extern uint RmEndSession(uint sessionHandle);

		[StructLayout(LayoutKind.Sequential)]
		public struct RM_UNIQUE_PROCESS {
			public uint ProcessId;
			public KERNEL32.FILETIME StartTime;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct RM_PROCESS_INFO {
			public RM_UNIQUE_PROCESS Process;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string ApplicationName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
			public string ServiceName;
			public uint ApplicationType;
			public uint ApplicationStatus;
			public uint SessionId;
			[MarshalAs(UnmanagedType.Bool)]
			public bool Restartable;
		}
	}
}
