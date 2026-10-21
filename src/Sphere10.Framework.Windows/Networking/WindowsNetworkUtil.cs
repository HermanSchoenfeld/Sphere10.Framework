// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Networking;

/// <summary>Owned managed representations of Windows IP Helper endpoint tables and interface counters.</summary>
public class WindowsNetworkUtil {
	public WindowsNetworkEndpoint[] GetEndpoints(bool tcp, AddressFamily family) => GetEndpointsCore(tcp, family, null);

	public Task<WindowsNetworkEndpoint[]> GetEndpointsAsync(bool tcp, AddressFamily family, CancellationToken cancellationToken = default)
		=> CaptureAsync(canceled => GetEndpointsCore(tcp, family, canceled), cancellationToken);

	private static WindowsNetworkEndpoint[] GetEndpointsCore(bool tcp, AddressFamily family, Func<bool> canceled) {
		Guard.Argument(family is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6, nameof(family), "Windows endpoint tables require IPv4 or IPv6.");
		var nativeFamily = family == AddressFamily.InterNetwork ? WinAPI.IPHLPAPI.AF_INET : WinAPI.IPHLPAPI.AF_INET6;
		if (canceled?.Invoke() == true)
			return null;
		var size = 0U;
		var error = Query(tcp, nativeFamily, IntPtr.Zero, ref size);
		if (error != 0 && error != WinAPI.IPHLPAPI.ERROR_INSUFFICIENT_BUFFER)
			throw new Win32Exception((int)error);
		for (var attempt = 0; attempt < 4; attempt++) {
			if (canceled?.Invoke() == true)
				return null;
			Guard.Ensure(size >= 4 && size <= 64 * 1024 * 1024, "The Windows endpoint table reported an invalid buffer size.");
			var allocated = checked((int)size);
			using var allocation = new UnmanagedMemoryScope(allocated);
			var buffer = allocation.Item;
			error = Query(tcp, nativeFamily, buffer, ref size);
			if (error == WinAPI.IPHLPAPI.ERROR_INSUFFICIENT_BUFFER)
				continue;
			if (error != 0)
				throw new Win32Exception((int)error);
			var count = Marshal.ReadInt32(buffer);
			var length = tcp
				? family == AddressFamily.InterNetwork ? Marshal.SizeOf<WinAPI.IPHLPAPI.MIB_TCPROW_OWNER_PID>() : Marshal.SizeOf<WinAPI.IPHLPAPI.MIB_TCP6ROW_OWNER_PID>()
				: family == AddressFamily.InterNetwork ? Marshal.SizeOf<WinAPI.IPHLPAPI.MIB_UDPROW_OWNER_PID>() : Marshal.SizeOf<WinAPI.IPHLPAPI.MIB_UDP6ROW_OWNER_PID>();
			Guard.Ensure(count >= 0 && count <= (allocated - 4) / length, "The Windows endpoint table reported an invalid row count.");
			var rows = new WindowsNetworkEndpoint[count];
			for (var index = 0; index < count; index++) {
				if (canceled?.Invoke() == true)
					return null;
				rows[index] = ReadRow(IntPtr.Add(buffer, 4 + index * length), tcp, family);
			}
			if (canceled?.Invoke() == true)
				return null;
			return rows;
		}
		throw new Win32Exception((int)WinAPI.IPHLPAPI.ERROR_INSUFFICIENT_BUFFER, "The Windows endpoint table changed repeatedly during enumeration.");
	}

	public WindowsNetworkInterface[] GetInterfaces() => GetInterfacesCore(NetworkInterface.GetAllNetworkInterfaces, null);

	public Task<WindowsNetworkInterface[]> GetInterfacesAsync(CancellationToken cancellationToken = default)
		=> GetInterfacesAsync(NetworkInterface.GetAllNetworkInterfaces, cancellationToken);

	internal Task<WindowsNetworkInterface[]> GetInterfacesAsync(Func<NetworkInterface[]> readInterfaces, CancellationToken cancellationToken)
		=> CaptureAsync(canceled => GetInterfacesCore(readInterfaces, canceled), cancellationToken);

	private static WindowsNetworkInterface[] GetInterfacesCore(Func<NetworkInterface[]> readInterfaces, Func<bool> canceled) {
		if (canceled?.Invoke() == true)
			return null;
		var rows = new List<WindowsNetworkInterface>();
		foreach (var adapter in readInterfaces()) {
			if (canceled?.Invoke() == true)
				return null;
			var row = new WindowsNetworkInterface {
				Id = adapter.Id, Name = adapter.Name, Description = adapter.Description,
				Status = adapter.OperationalStatus, InterfaceType = adapter.NetworkInterfaceType
			};
			Tools.Exceptions.ExecuteCapturingException<NetworkInformationException>(() => {
				row.PhysicalAddress = adapter.GetPhysicalAddress().GetAddressBytes();
				row.LinkSpeedBitsPerSecond = adapter.Speed;
			}, out var hardwareError);
			row.HardwareError = hardwareError;
			Tools.Exceptions.ExecuteCapturingException(() => {
				var properties = adapter.GetIPProperties();
				row.Addresses = properties.UnicastAddresses.Select(address => address.Address).ToArray();
				row.Gateways = properties.GatewayAddresses.Select(address => address.Address).ToArray();
				row.DnsServers = properties.DnsAddresses.ToArray();
			}, out var addressError);
			row.AddressError = addressError;
			rows.Add(row);
		}
		if (canceled?.Invoke() == true)
			return null;
		return rows.ToArray();
	}

	private static Task<T[]> CaptureAsync<T>(Func<Func<bool>, T[]> capture, CancellationToken cancellationToken)
		=> Task.Run(() => {
			var rows = capture(() => cancellationToken.IsCancellationRequested);
			// A native call must finish and release its buffers before cancellation completes the owned operation.
			return cancellationToken.IsCancellationRequested ? Task.FromCanceled<T[]>(cancellationToken) : Task.FromResult(rows);
		}, cancellationToken);

	public WindowsInterfaceCounters GetInterfaceCounters(Guid interfaceId) {
		var error = WinAPI.IPHLPAPI.ConvertInterfaceGuidToLuid(ref interfaceId, out var luid);
		if (error != 0)
			throw new Win32Exception((int)error);
		var row = new WinAPI.IPHLPAPI.MIB_IF_ROW2 { InterfaceLuid = luid };
		error = WinAPI.IPHLPAPI.GetIfEntry2(ref row);
		if (error != 0)
			throw new Win32Exception((int)error);
		Guard.Ensure(row.InterfaceGuid == interfaceId, "Windows returned counters for a different interface GUID.");
		return new WindowsInterfaceCounters(row.InterfaceGuid, row.InterfaceLuid, row.InterfaceIndex, (OperationalStatus)row.OperStatus,
			(row.InterfaceAndOperStatusFlags & WinAPI.IPHLPAPI.IF_FLAG_HARDWARE) != 0,
			(row.InterfaceAndOperStatusFlags & WinAPI.IPHLPAPI.IF_FLAG_FILTER) != 0,
			row.ReceiveLinkSpeed, row.TransmitLinkSpeed, row.InOctets, row.OutOctets);
	}

	private static uint Query(bool tcp, uint family, IntPtr buffer, ref uint size) => tcp
		? WinAPI.IPHLPAPI.GetExtendedTcpTable(buffer, ref size, false, family, WinAPI.IPHLPAPI.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0)
		: WinAPI.IPHLPAPI.GetExtendedUdpTable(buffer, ref size, false, family, WinAPI.IPHLPAPI.UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);

	private static WindowsNetworkEndpoint ReadRow(IntPtr row, bool tcp, AddressFamily family) {
		if (tcp && family == AddressFamily.InterNetworkV6) {
			var value = Marshal.PtrToStructure<WinAPI.IPHLPAPI.MIB_TCP6ROW_OWNER_PID>(row);
			return Create(true, family, value.dwState, value.dwOwningPid, Address(value.ucLocalAddr, value.dwLocalScopeId), value.dwLocalPort, Address(value.ucRemoteAddr, value.dwRemoteScopeId), value.dwRemotePort);
		}
		if (tcp) {
			var value = Marshal.PtrToStructure<WinAPI.IPHLPAPI.MIB_TCPROW_OWNER_PID>(row);
			return Create(true, family, value.dwState, value.dwOwningPid, Address(value.dwLocalAddr), value.dwLocalPort, Address(value.dwRemoteAddr), value.dwRemotePort);
		}
		if (family == AddressFamily.InterNetworkV6) {
			var value = Marshal.PtrToStructure<WinAPI.IPHLPAPI.MIB_UDP6ROW_OWNER_PID>(row);
			return Create(false, family, 0, value.dwOwningPid, Address(value.ucLocalAddr, value.dwLocalScopeId), value.dwLocalPort, null, 0);
		}
		var ipv4 = Marshal.PtrToStructure<WinAPI.IPHLPAPI.MIB_UDPROW_OWNER_PID>(row);
		return Create(false, family, 0, ipv4.dwOwningPid, Address(ipv4.dwLocalAddr), ipv4.dwLocalPort, null, 0);
	}

	private static WindowsNetworkEndpoint Create(bool tcp, AddressFamily family, uint state, uint processId, IPAddress local, uint localPort, IPAddress remote, uint remotePort)
		=> new(tcp, family, local, Port(localPort), tcp && state != (uint)TcpState.Listen ? remote : null,
			tcp && state != (uint)TcpState.Listen ? Port(remotePort) : null, tcp ? (TcpState)state : null, processId);

	private static int Port(uint port) => unchecked((ushort)IPAddress.NetworkToHostOrder((short)(port & 0xFFFF)));
	private static IPAddress Address(uint address) => new(EndianBitConverter.Little.GetBytes(address));
	private static IPAddress Address(byte[] address, uint scope)
		=> new(address, unchecked((uint)IPAddress.NetworkToHostOrder((int)scope)));
}
