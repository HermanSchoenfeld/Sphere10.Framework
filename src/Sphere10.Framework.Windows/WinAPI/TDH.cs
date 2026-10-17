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
	public static class TDH {
		[DllImport("tdh.dll", ExactSpelling = true)]
		public static extern uint TdhGetEventInformation(IntPtr record, uint contextCount, IntPtr context, IntPtr information, ref uint bufferSize);

		[StructLayout(LayoutKind.Sequential)]
		public struct TRACE_EVENT_INFO {
			public Guid ProviderGuid;
			public Guid EventGuid;
			public ADVAPI32.EVENT_DESCRIPTOR EventDescriptor;
			public uint DecodingSource;
			public uint ProviderNameOffset;
			public uint LevelNameOffset;
			public uint ChannelNameOffset;
			public uint KeywordsNameOffset;
			public uint TaskNameOffset;
			public uint OpcodeNameOffset;
			public uint EventMessageOffset;
			public uint ProviderMessageOffset;
			public uint BinaryXMLOffset;
			public uint BinaryXMLSize;
			public uint EventNameOffset;
			public uint EventAttributesOffset;
			public uint PropertyCount;
			public uint TopLevelPropertyCount;
			public uint Flags;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct EVENT_PROPERTY_INFO {
			public uint Flags;
			public uint NameOffset;
			public ushort InType;
			public ushort OutType;
			public uint MapNameOffset;
			public ushort Count;
			public ushort Length;
			public uint Reserved;
		}
	}
}
