// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {
	public static class PDH {
		public const uint MoreData = 0x800007D2;
		public const uint NoData = 0x800007D5;
		public const uint FormatDouble = 0x00000200;
		public const uint FormatNoCap100 = 0x00008000;

		[StructLayout(LayoutKind.Sequential)]
		public struct RAW_COUNTER {
			public uint Status;
			public KERNEL32.FILETIME Timestamp;
			public long FirstValue;
			public long SecondValue;
			public uint MultiCount;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAW_COUNTER_ITEM {
			public IntPtr Name;
			public RAW_COUNTER Value;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct FORMATTED_COUNTER_VALUE {
			public uint Status;
			public double Value;
		}

		[DllImport("pdh.dll", EntryPoint = "PdhOpenQueryW", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern uint PdhOpenQuery(string dataSource, UIntPtr userData, out QueryHandle query);

		[DllImport("pdh.dll", EntryPoint = "PdhAddEnglishCounterW", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern uint PdhAddEnglishCounter(QueryHandle query, string path, UIntPtr userData, out IntPtr counter);

		[DllImport("pdh.dll", EntryPoint = "PdhGetCounterInfoW", ExactSpelling = true)]
		public static extern uint PdhGetCounterInfo(IntPtr counter, [MarshalAs(UnmanagedType.U1)] bool explain, ref uint bufferSize, IntPtr buffer);

		[DllImport("pdh.dll", ExactSpelling = true)]
		public static extern uint PdhGetCounterTimeBase(IntPtr counter, out long timeBase);

		[DllImport("pdh.dll", ExactSpelling = true)]
		public static extern uint PdhCollectQueryData(QueryHandle query);

		[DllImport("pdh.dll", ExactSpelling = true)]
		public static extern uint PdhCollectQueryDataWithTime(QueryHandle query, out long timestamp);

		[DllImport("pdh.dll", ExactSpelling = true)]
		public static extern uint PdhGetRawCounterValue(IntPtr counter, out uint type, out RAW_COUNTER value);

		[DllImport("pdh.dll", EntryPoint = "PdhGetRawCounterArrayW", ExactSpelling = true)]
		public static extern uint PdhGetRawCounterArray(IntPtr counter, ref uint bufferSize, out uint count, IntPtr buffer);

		[DllImport("pdh.dll", ExactSpelling = true)]
		public static extern uint PdhCalculateCounterFromRawValue(IntPtr counter, uint format, ref RAW_COUNTER current, IntPtr previous, out FORMATTED_COUNTER_VALUE value);

		[DllImport("pdh.dll", ExactSpelling = true)]
		public static extern uint PdhCloseQuery(IntPtr query);

		public sealed class QueryHandle : SafeHandleZeroOrMinusOneIsInvalid {
			public QueryHandle()
				: base(true) {
			}

			protected override bool ReleaseHandle() => PdhCloseQuery(handle) == 0;
		}
	}
}

