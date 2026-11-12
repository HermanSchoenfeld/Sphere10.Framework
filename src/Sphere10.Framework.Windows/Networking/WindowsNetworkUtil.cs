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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Windows.Networking;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class Windows {

	/// <summary>Owned managed representations of Windows IP Helper endpoint tables and interface counters.</summary>
	public static class Network {
		/// <summary>Creates an independent lazy owner-table inventory, retaining protocol/family/PID filtering before paging.</summary>
		public static WindowsNetworkEndpointDataSource CreateEndpointDataSource(bool isTcp, AddressFamily addressFamily, uint? processId = null)
			=> new WindowsNetworkEndpointDataSource(isTcp, addressFamily, processId);

		public static WindowsNetworkInterfaceDataSource CreateInterfaceDataSource() => new WindowsNetworkInterfaceDataSource();

		public static WindowsNetworkEndpoint[] GetEndpoints(bool tcp, AddressFamily family) => GetEndpointsCore(tcp, family, null);

		public static Task<WindowsNetworkEndpoint[]> GetEndpointsAsync(bool tcp, AddressFamily family, CancellationToken cancellationToken = default)
			=> CaptureAsync(canceled => GetEndpointsCore(tcp, family, canceled), cancellationToken);

		public static WindowsNetworkInterface[] GetInterfaces() => GetInterfacesCore(NetworkInterface.GetAllNetworkInterfaces, null);

		public static Task<WindowsNetworkInterface[]> GetInterfacesAsync(CancellationToken cancellationToken = default)
			=> GetInterfacesAsync(NetworkInterface.GetAllNetworkInterfaces, cancellationToken);

		public static WindowsInterfaceCounters GetInterfaceCounters(Guid interfaceId) {
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

		/// <summary>Captures a fresh native bind timestamp and verified process identity for an explicit IPv4 TCP close plan.</summary>
		public static Task<WindowsTcpConnectionIdentity> PrepareTcpCloseAsync(WindowsNetworkEndpoint endpoint, ProcessIdentity process, CancellationToken cancellationToken = default)
			=> Task.Run(() => PrepareTcpClose(endpoint, process, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

		/// <summary>Rechecks the full tuple, bind timestamp, process generation and state immediately before DELETE_TCB.</summary>
		/// <remarks>Windows SetTcpEntry addresses a four-tuple; its validation and deletion are separate native calls, not an atomic compare-and-close.</remarks>
		public static Task CloseTcpConnectionAsync(WindowsTcpConnectionIdentity expected, CancellationToken cancellationToken = default) => Task.Run(() => {
			Debug.AssertNotMainThread();
			Guard.ArgumentNotNull(expected, nameof(expected));
			using var process = WindowsProcess.Open(expected.Process.ProcessId);
			Guard.Ensure(process.Identity == expected.Process && !process.HasExited, "The endpoint owner exited or its PID was reused.");
			var current = PrepareTcpClose(expected.Endpoint, expected.Process, cancellationToken.ThrowIfCancellationRequested);
			VerifyTcpCloseIdentity(expected, current);
			Guard.Ensure(!process.HasExited, "The endpoint owner exited before the close operation.");
			cancellationToken.ThrowIfCancellationRequested();
			var row = new WinAPI.IPHLPAPI.MIB_TCPROW { State = 12,
				LocalAddress = EndianBitConverter.Little.ToUInt32(expected.Endpoint.LocalAddress.GetAddressBytes(), 0),
				RemoteAddress = EndianBitConverter.Little.ToUInt32(expected.Endpoint.RemoteAddress.GetAddressBytes(), 0),
				LocalPort = unchecked((ushort)IPAddress.HostToNetworkOrder((short)expected.Endpoint.LocalPort)),
				RemotePort = unchecked((ushort)IPAddress.HostToNetworkOrder((short)expected.Endpoint.RemotePort.Value)) };
			var error = WinAPI.IPHLPAPI.SetTcpEntry(ref row);
			if (error != 0)
				throw new Win32Exception((int)error, "Windows could not close this IPv4 TCP connection; elevated administrator rights are required.");
		}, cancellationToken);

		internal static Task<WindowsNetworkInterface[]> GetInterfacesAsync(Func<NetworkInterface[]> readInterfaces, CancellationToken cancellationToken)
			=> CaptureAsync(canceled => GetInterfacesCore(readInterfaces, canceled), cancellationToken);

		internal static void VerifyTcpCloseIdentity(WindowsTcpConnectionIdentity expected, WindowsTcpConnectionIdentity current) {
			Guard.ArgumentNotNull(expected, nameof(expected));
			Guard.ArgumentNotNull(current, nameof(current));
			Guard.Ensure(expected.Process.IsVerified && expected.Process == current.Process && expected.BoundAtUtc == current.BoundAtUtc
				&& expected.Endpoint == current.Endpoint, "The TCP connection identity or state changed after confirmation; no close was performed.");
		}

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

		private static WindowsTcpConnectionIdentity PrepareTcpClose(WindowsNetworkEndpoint endpoint, ProcessIdentity expectedProcess, Action checkpoint) {
			Debug.AssertNotMainThread();
			Guard.ArgumentNotNull(endpoint, nameof(endpoint));
			Guard.Argument(endpoint.IsTcp && endpoint.AddressFamily == AddressFamily.InterNetwork && endpoint.LocalAddress?.AddressFamily == AddressFamily.InterNetwork
				&& endpoint.RemoteAddress?.AddressFamily == AddressFamily.InterNetwork && endpoint.RemotePort.HasValue
				&& endpoint.State.HasValue && endpoint.State is not (TcpState.Listen or TcpState.Closed or TcpState.DeleteTcb), nameof(endpoint), "Only connected IPv4 TCP rows support Windows DELETE_TCB.");
			Guard.Argument(expectedProcess.IsVerified && endpoint.ProcessId == expectedProcess.ProcessId, nameof(expectedProcess), "A verified endpoint owner is required.");
			using var process = WindowsProcess.Open(expectedProcess.ProcessId);
			Guard.Ensure(process.Identity == expectedProcess && !process.HasExited, "The endpoint owner exited or its PID was reused.");
			uint size = 0;
			var error = WinAPI.IPHLPAPI.GetExtendedTcpTable(IntPtr.Zero, ref size, false, WinAPI.IPHLPAPI.AF_INET,
				WinAPI.IPHLPAPI.TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);
			if (error != 0 && error != WinAPI.IPHLPAPI.ERROR_INSUFFICIENT_BUFFER) throw new Win32Exception((int)error);
			for (var attempt = 0; attempt < 4; attempt++) {
				checkpoint();
				Guard.Ensure(size is >= 8 and <= 64 * 1024 * 1024, "The TCP ownership table returned an invalid bounded size.");
				var capacity = (int)size;
				using var memory = new UnmanagedMemoryScope(capacity);
				error = WinAPI.IPHLPAPI.GetExtendedTcpTable(memory.Item, ref size, false, WinAPI.IPHLPAPI.AF_INET,
					WinAPI.IPHLPAPI.TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);
				if (error == WinAPI.IPHLPAPI.ERROR_INSUFFICIENT_BUFFER) continue;
				if (error != 0) throw new Win32Exception((int)error);
				var offset = Marshal.OffsetOf<WinAPI.IPHLPAPI.MIB_TCPTABLE_OWNER_MODULE_HEADER>(nameof(WinAPI.IPHLPAPI.MIB_TCPTABLE_OWNER_MODULE_HEADER.FirstRow)).ToInt32();
				var length = Marshal.SizeOf<WinAPI.IPHLPAPI.MIB_TCPROW_OWNER_MODULE>();
				var count = Marshal.ReadInt32(memory.Item);
				Guard.Ensure(count >= 0 && count <= (capacity - offset) / length, "The TCP ownership table returned an invalid row count.");
				for (var index = 0; index < count; index++) {
					checkpoint();
					var row = Marshal.PtrToStructure<WinAPI.IPHLPAPI.MIB_TCPROW_OWNER_MODULE>(IntPtr.Add(memory.Item, offset + index * length));
					var current = Create(true, AddressFamily.InterNetwork, row.State, row.ProcessId, Address(row.LocalAddress), row.LocalPort, Address(row.RemoteAddress), row.RemotePort);
					if (current == endpoint) {
						Guard.Ensure(row.CreatedAt > 0 && !process.HasExited, "The native TCP bind timestamp or owner liveness is unavailable.");
						return new WindowsTcpConnectionIdentity(current, expectedProcess, DateTime.FromFileTimeUtc(row.CreatedAt), DateTime.UtcNow);
					}
				}
				throw new InvalidOperationException("The selected TCP connection or its state changed; refresh the endpoint inventory before preparing another close.");
			}
			throw new Win32Exception((int)WinAPI.IPHLPAPI.ERROR_INSUFFICIENT_BUFFER, "The TCP ownership table kept changing.");
		}
	}
}
