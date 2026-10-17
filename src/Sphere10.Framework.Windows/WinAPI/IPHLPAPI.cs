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

	public static class IPHLPAPI {
		public const uint AF_INET = 2;
		public const uint AF_INET6 = 23;
		public const uint ERROR_INSUFFICIENT_BUFFER = 122;
		public const byte IF_FLAG_HARDWARE = 1;
		public const byte IF_FLAG_FILTER = 2;

		// https://learn.microsoft.com/windows/win32/api/netioapi/ns-netioapi-mib_if_row2
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
		public struct MIB_IF_ROW2 {
			public ulong InterfaceLuid;
			public uint InterfaceIndex;
			public Guid InterfaceGuid;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)] public string Alias;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)] public string Description;
			public uint PhysicalAddressLength;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] PhysicalAddress;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] PermanentPhysicalAddress;
			public uint Mtu;
			public uint Type;
			public uint TunnelType;
			public uint MediaType;
			public uint PhysicalMediumType;
			public uint AccessType;
			public uint DirectionType;
			public byte InterfaceAndOperStatusFlags;
			public uint OperStatus;
			public uint AdminStatus;
			public uint MediaConnectState;
			public Guid NetworkGuid;
			public uint ConnectionType;
			public ulong TransmitLinkSpeed;
			public ulong ReceiveLinkSpeed;
			public ulong InOctets;
			public ulong InUcastPkts;
			public ulong InNUcastPkts;
			public ulong InDiscards;
			public ulong InErrors;
			public ulong InUnknownProtos;
			public ulong InUcastOctets;
			public ulong InMulticastOctets;
			public ulong InBroadcastOctets;
			public ulong OutOctets;
			public ulong OutUcastPkts;
			public ulong OutNUcastPkts;
			public ulong OutDiscards;
			public ulong OutErrors;
			public ulong OutUcastOctets;
			public ulong OutMulticastOctets;
			public ulong OutBroadcastOctets;
			public ulong OutQLen;
		}

		// Protocol-independent identity lookup avoids an IPv4 index of zero becoming fabricated zero counters.
		[DllImport("iphlpapi.dll", ExactSpelling = true)]
		public static extern uint ConvertInterfaceGuidToLuid(ref Guid interfaceGuid, out ulong interfaceLuid);

		[DllImport("iphlpapi.dll", ExactSpelling = true)]
		public static extern uint GetIfEntry2(ref MIB_IF_ROW2 row);

		public enum TCP_TABLE_CLASS {
			TCP_TABLE_BASIC_LISTENER,
			TCP_TABLE_BASIC_CONNECTIONS,
			TCP_TABLE_BASIC_ALL,
			TCP_TABLE_OWNER_PID_LISTENER,
			TCP_TABLE_OWNER_PID_CONNECTIONS,
			TCP_TABLE_OWNER_PID_ALL,
			TCP_TABLE_OWNER_MODULE_LISTENER,
			TCP_TABLE_OWNER_MODULE_CONNECTIONS,
			TCP_TABLE_OWNER_MODULE_ALL
		}

		public enum UDP_TABLE_CLASS {
			UDP_TABLE_BASIC,
			UDP_TABLE_OWNER_PID,
			UDP_TABLE_OWNER_MODULE
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MIB_TCPROW_OWNER_PID {
			public uint dwState;
			public uint dwLocalAddr;
			public uint dwLocalPort;
			public uint dwRemoteAddr;
			public uint dwRemotePort;
			public uint dwOwningPid;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MIB_TCP6ROW_OWNER_PID {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public byte[] ucLocalAddr;
			public uint dwLocalScopeId;
			public uint dwLocalPort;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public byte[] ucRemoteAddr;
			public uint dwRemoteScopeId;
			public uint dwRemotePort;
			public uint dwState;
			public uint dwOwningPid;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MIB_UDPROW_OWNER_PID {
			public uint dwLocalAddr;
			public uint dwLocalPort;
			public uint dwOwningPid;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MIB_UDP6ROW_OWNER_PID {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public byte[] ucLocalAddr;
			public uint dwLocalScopeId;
			public uint dwLocalPort;
			public uint dwOwningPid;
		}

		// The caller supplies and owns the buffer; the API returns Win32 error codes directly.
		// https://learn.microsoft.com/windows/win32/api/iphlpapi/nf-iphlpapi-getextendedtcptable
		[DllImport("iphlpapi.dll", ExactSpelling = true)]
		public static extern uint GetExtendedTcpTable(IntPtr table, ref uint size, [MarshalAs(UnmanagedType.Bool)] bool order, uint addressFamily, TCP_TABLE_CLASS tableClass, uint reserved);

		// https://learn.microsoft.com/windows/win32/api/iphlpapi/nf-iphlpapi-getextendedudptable
		[DllImport("iphlpapi.dll", ExactSpelling = true)]
		public static extern uint GetExtendedUdpTable(IntPtr table, ref uint size, [MarshalAs(UnmanagedType.Bool)] bool order, uint addressFamily, UDP_TABLE_CLASS tableClass, uint reserved);
	}
}
