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
		public const ushort AF_UNSPEC = 0;
		public const uint AF_INET = 2;
		public const uint AF_INET6 = 23;
		public const uint ERROR_ACCESS_DENIED = 5;
		public const uint ERROR_INSUFFICIENT_BUFFER = 122;
		public const uint ERROR_NO_DATA = 232;
		public const uint ERROR_NOT_FOUND = 1168;
		public const byte IF_FLAG_HARDWARE = 1;
		public const byte IF_FLAG_FILTER = 2;
		public const int MAX_ADAPTER_NAME = 128;
		public const int MIB_IPNET_ROW2_SIZE = 88;
		public const uint NlnsPermanent = 6;

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

		// The neighbour (ARP and IPv6 neighbour discovery) cache. The table is released with FreeMibTable.
		// https://learn.microsoft.com/windows/win32/api/netioapi/nf-netioapi-getipnettable2
		[DllImport("iphlpapi.dll", ExactSpelling = true)]
		public static extern uint GetIpNetTable2(ushort family, out IntPtr table);

		[DllImport("iphlpapi.dll", ExactSpelling = true)]
		public static extern void FreeMibTable(IntPtr memory);

		// The row is a caller-built MIB_IPNET_ROW2 of MIB_IPNET_ROW2_SIZE bytes: the SOCKADDR_INET address at 0, the interface index
		// at 28, the LUID at 32 (zero selects the interface by index), the hardware address at 40, its length at 72 and the state at 76.
		// https://learn.microsoft.com/windows/win32/api/netioapi/nf-netioapi-createipnetentry2
		[DllImport("iphlpapi.dll", ExactSpelling = true)]
		public static extern uint CreateIpNetEntry2([In] byte[] row);

		// https://learn.microsoft.com/windows/win32/api/netioapi/nf-netioapi-deleteipnetentry2
		[DllImport("iphlpapi.dll", ExactSpelling = true)]
		public static extern uint DeleteIpNetEntry2([In] byte[] row);

		// An interface index of zero flushes every interface.
		// https://learn.microsoft.com/windows/win32/api/netioapi/nf-netioapi-flushipnettable2
		[DllImport("iphlpapi.dll", ExactSpelling = true)]
		public static extern uint FlushIpNetTable2(ushort family, uint interfaceIndex);

		// IP_ADAPTER_INDEX_MAP: the IPv4 interface index and the adapter's device name, 260 bytes.
		// https://learn.microsoft.com/windows/win32/api/ipexport/ns-ipexport-ip_adapter_index_map
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct IP_ADAPTER_INDEX_MAP {
			public uint Index;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_ADAPTER_NAME)] public string Name;
		}

		// Fills a caller-owned IP_INTERFACE_INFO: a LONG adapter count followed by IP_ADAPTER_INDEX_MAP rows.
		// https://learn.microsoft.com/windows/win32/api/iphlpapi/nf-iphlpapi-getinterfaceinfo
		[DllImport("iphlpapi.dll", ExactSpelling = true)]
		public static extern uint GetInterfaceInfo(IntPtr interfaceInfo, ref uint size);

		// https://learn.microsoft.com/windows/win32/api/iphlpapi/nf-iphlpapi-ipreleaseaddress
		[DllImport("iphlpapi.dll", ExactSpelling = true)]
		public static extern uint IpReleaseAddress(ref IP_ADAPTER_INDEX_MAP adapterInfo);

		// https://learn.microsoft.com/windows/win32/api/iphlpapi/nf-iphlpapi-iprenewaddress
		[DllImport("iphlpapi.dll", ExactSpelling = true)]
		public static extern uint IpRenewAddress(ref IP_ADAPTER_INDEX_MAP adapterInfo);

		// Sends an ARP request for an IPv4 address in network byte order and returns the hardware address that answered.
		[DllImport("iphlpapi.dll", ExactSpelling = true)]
		public static extern uint SendARP(uint destinationAddress, uint sourceAddress, [Out] byte[] physicalAddress, ref uint physicalAddressLength);

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
		public struct MIB_TCPROW {
			public uint State;
			public uint LocalAddress;
			public uint LocalPort;
			public uint RemoteAddress;
			public uint RemotePort;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MIB_TCPROW_OWNER_MODULE {
			public uint State;
			public uint LocalAddress;
			public uint LocalPort;
			public uint RemoteAddress;
			public uint RemotePort;
			public uint ProcessId;
			public long CreatedAt;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public ulong[] OwningModuleInfo;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MIB_TCPTABLE_OWNER_MODULE_HEADER {
			public uint Count;
			public MIB_TCPROW_OWNER_MODULE FirstRow;
		}

		[DllImport("iphlpapi.dll", ExactSpelling = true)]
		public static extern uint SetTcpEntry(ref MIB_TCPROW row);

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
