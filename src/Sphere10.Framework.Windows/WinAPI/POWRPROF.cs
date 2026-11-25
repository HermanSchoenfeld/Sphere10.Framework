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
	public static class POWRPROF {
		[DllImport("powrprof.dll", ExactSpelling = true)]
		public static extern uint PowerGetActiveScheme(IntPtr root, out IntPtr scheme);

		[DllImport("powrprof.dll", ExactSpelling = true)]
		public static extern uint PowerEnumerate(IntPtr root, [In] Guid[] scheme, [In] Guid[] subgroup, uint access, uint index, [Out] byte[] buffer, ref uint size);

		[DllImport("powrprof.dll", ExactSpelling = true)]
		public static extern uint PowerReadFriendlyName(IntPtr root, [In] Guid[] scheme, [In] Guid[] subgroup, [In] Guid[] setting, [Out] byte[] buffer, ref uint size);

		[DllImport("powrprof.dll", ExactSpelling = true)]
		public static extern uint PowerReadDescription(IntPtr root, [In] Guid[] scheme, [In] Guid[] subgroup, [In] Guid[] setting, [Out] byte[] buffer, ref uint size);

		[DllImport("powrprof.dll", ExactSpelling = true)]
		public static extern uint PowerReadACValueIndex(IntPtr root, [In] Guid[] scheme, [In] Guid[] subgroup, [In] Guid[] setting, out uint value);

		[DllImport("powrprof.dll", ExactSpelling = true)]
		public static extern uint PowerReadDCValueIndex(IntPtr root, [In] Guid[] scheme, [In] Guid[] subgroup, [In] Guid[] setting, out uint value);

		[DllImport("powrprof.dll", ExactSpelling = true)]
		public static extern uint PowerReadValueUnitsSpecifier(IntPtr root, [In] Guid[] subgroup, [In] Guid[] setting, [Out] byte[] buffer, ref uint size);

		[DllImport("powrprof.dll", ExactSpelling = true)]
		public static extern uint PowerReadValueMin(IntPtr root, [In] Guid[] subgroup, [In] Guid[] setting, out uint value);

		[DllImport("powrprof.dll", ExactSpelling = true)]
		public static extern uint PowerReadValueMax(IntPtr root, [In] Guid[] subgroup, [In] Guid[] setting, out uint value);

		[DllImport("powrprof.dll", ExactSpelling = true)]
		public static extern uint PowerReadValueIncrement(IntPtr root, [In] Guid[] subgroup, [In] Guid[] setting, out uint value);

		[DllImport("powrprof.dll", ExactSpelling = true)]
		public static extern uint PowerReadPossibleValue(IntPtr root, [In] Guid[] subgroup, [In] Guid[] setting, out uint type, uint index, [Out] byte[] buffer, ref uint size);

		[DllImport("powrprof.dll", ExactSpelling = true)]
		public static extern uint PowerReadPossibleFriendlyName(IntPtr root, [In] Guid[] subgroup, [In] Guid[] setting, uint index, [Out] byte[] buffer, ref uint size);

		[DllImport("powrprof.dll", ExactSpelling = true)]
		public static extern uint PowerReadSettingAttributes([In] Guid[] subgroup, [In] Guid[] setting);

		[DllImport("powrprof.dll", ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern bool GetPwrCapabilities(out SYSTEM_POWER_CAPABILITIES capabilities);

		[DllImport("powrprof.dll", ExactSpelling = true)]
		public static extern uint CallNtPowerInformation(int informationLevel, IntPtr input, uint inputLength, [Out] byte[] output, uint outputLength);

		[StructLayout(LayoutKind.Sequential)]
		public struct BATTERY_REPORTING_SCALE {
			public uint Granularity;
			public uint Capacity;
		}

		// Current Windows SDK layout (76 bytes). BOOLEAN fields are one byte, not Win32 BOOL.
		[StructLayout(LayoutKind.Sequential)]
		public struct SYSTEM_POWER_CAPABILITIES {
			public byte PowerButtonPresent;
			public byte SleepButtonPresent;
			public byte LidPresent;
			public byte SystemS1;
			public byte SystemS2;
			public byte SystemS3;
			public byte SystemS4;
			public byte SystemS5;
			public byte HiberFilePresent;
			public byte FullWake;
			public byte VideoDimPresent;
			public byte ApmPresent;
			public byte UpsPresent;
			public byte ThermalControl;
			public byte ProcessorThrottle;
			public byte ProcessorMinThrottle;
			public byte ProcessorMaxThrottle;
			public byte FastSystemS4;
			public byte Hiberboot;
			public byte WakeAlarmPresent;
			public byte AoAc;
			public byte DiskSpinDown;
			public byte HiberFileType;
			public byte AoAcConnectivitySupported;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)] public byte[] Spare;
			public byte SystemBatteriesPresent;
			public byte BatteriesAreShortTerm;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public BATTERY_REPORTING_SCALE[] BatteryScale;
			public uint AcOnLineWake;
			public uint SoftLidWake;
			public uint RtcWake;
			public uint MinDeviceWakeState;
			public uint DefaultLowLatencyWake;
		}

		[DllImport("powrprof.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern bool SetSuspendState([MarshalAs(UnmanagedType.U1)] bool hibernate, [MarshalAs(UnmanagedType.U1)] bool force, [MarshalAs(UnmanagedType.U1)] bool disableWakeEvents);

		[DllImport("powrprof.dll", ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern bool IsPwrSuspendAllowed();

		[DllImport("powrprof.dll", ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern bool IsPwrHibernateAllowed();

		[DllImport("powrprof.dll", ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern bool IsPwrShutdownAllowed();
	}
}

