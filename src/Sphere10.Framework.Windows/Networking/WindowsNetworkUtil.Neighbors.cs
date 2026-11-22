// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Buffers.Binary;
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
using Sphere10.Framework;
using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.Networking;

namespace Tools;

public static partial class Windows {

	public static partial class Network {
		// MIB_IPNET_ROW2: a 28-byte socket address, the interface index and LUID, a 32-byte hardware address with its length,
		// the state, a flags byte and the reachability time. MIB_IPNET_TABLE2 holds a count, padding to eight bytes, then the rows.
		private const int NeighborRowSize = WinAPI.IPHLPAPI.MIB_IPNET_ROW2_SIZE;
		private const int NeighborTableHeaderSize = 8;
		private const int NeighborInterfaceIndexOffset = 28;
		private const int NeighborPhysicalAddressOffset = 40;
		private const int NeighborPhysicalAddressCapacity = 32;
		private const int NeighborPhysicalLengthOffset = 72;
		private const int NeighborStateOffset = 76;
		private const int NeighborFlagsOffset = 80;

		/// <summary>Reads Windows' neighbour cache: the IP addresses on the local networks whose hardware (MAC) addresses this
		/// computer has learned, by ARP for IPv4 and neighbour discovery for IPv6. This is what <c>arp -a</c> lists.</summary>
		public static WindowsNetworkNeighbor[] GetNeighbors() {
			Debug.AssertNotMainThread();
			var error = WinAPI.IPHLPAPI.GetIpNetTable2(0, out var table);
			if (error != 0)
				throw new Win32Exception((int)error);
			try {
				var interfaces = NetworkInterface.GetAllNetworkInterfaces().Select(item => (Index: InterfaceIndexOf(item), item.Name)).Where(item => item.Index.HasValue)
					.GroupBy(item => item.Index.Value).ToDictionary(group => group.Key, group => group.First().Name);
				var count = Marshal.ReadInt32(table);
				var neighbors = new List<WindowsNetworkNeighbor>(count);
				var row = new byte[NeighborRowSize];
				for (var index = 0; index < count; index++) {
					Marshal.Copy(IntPtr.Add(table, NeighborTableHeaderSize + index * NeighborRowSize), row, 0, NeighborRowSize);
					var interfaceIndex = BinaryPrimitives.ReadInt32LittleEndian(row.AsSpan(NeighborInterfaceIndexOffset));
					neighbors.Add(ReadNeighborRow(row, interfaces.GetValueOrDefault(interfaceIndex)));
				}
				return neighbors.ToArray();
			} finally {
				WinAPI.IPHLPAPI.FreeMibTable(table);
			}
		}

		public static Task<WindowsNetworkNeighbor[]> GetNeighborsAsync(CancellationToken cancellationToken = default) => Task.Run(GetNeighbors, cancellationToken);

		/// <summary>Asks the local network which hardware (MAC) address answers for an IPv4 address, by sending an ARP request.
		/// It returns null when nothing answers, which is what an address on another network or a switched-off device gives.</summary>
		public static byte[] ResolvePhysicalAddress(IPAddress address) {
			Debug.AssertNotMainThread();
			Guard.ArgumentNotNull(address, nameof(address));
			Guard.Argument(address.AddressFamily == AddressFamily.InterNetwork, nameof(address), "ARP resolves IPv4 addresses.");
			var physical = new byte[6];
			var length = (uint)physical.Length;
			var error = WinAPI.IPHLPAPI.SendARP(BitConverter.ToUInt32(address.GetAddressBytes(), 0), 0, physical, ref length);
			// ERROR_BAD_NET_NAME and ERROR_GEN_FAILURE are how Windows reports that no device answered.
			if (error is 67 or 31)
				return null;
			if (error != 0)
				throw new Win32Exception((int)error);
			return physical.Take((int)length).ToArray();
		}

		public static Task<byte[]> ResolvePhysicalAddressAsync(IPAddress address, CancellationToken cancellationToken = default)
			=> Task.Run(() => ResolvePhysicalAddress(address), cancellationToken);

		/// <summary>Removes one entry from Windows' neighbour cache, identified by the address and interface of a neighbour that was read
		/// with <see cref="GetNeighbors"/>. Windows learns a dynamic entry again the next time the address is used. This needs administrator rights.</summary>
		public static void DeleteNeighbor(WindowsNetworkNeighbor neighbor) {
			Debug.AssertNotMainThread();
			Guard.ArgumentNotNull(neighbor, nameof(neighbor));
			Guard.ArgumentNotNull(neighbor.Address, nameof(neighbor));
			Guard.Argument(neighbor.InterfaceIndex > 0, nameof(neighbor), "The neighbour has no interface index.");
			var row = BuildNeighborRow(neighbor.Address, System.Array.Empty<byte>(), neighbor.InterfaceIndex, 0);
			ThrowOnNetworkChangeError(WinAPI.IPHLPAPI.DeleteIpNetEntry2(row), "delete the neighbour entry");
		}

		public static Task DeleteNeighborAsync(WindowsNetworkNeighbor neighbor, CancellationToken cancellationToken = default)
			=> Task.Run(() => DeleteNeighbor(neighbor), cancellationToken);

		/// <summary>Clears Windows' neighbour cache for one interface, or for every interface present in the cache when no index is given.
		/// Permanent entries such as broadcast addresses remain. This needs administrator rights.</summary>
		/// <remarks>An interface that disappears between reading the cache and flushing it is skipped.</remarks>
		public static void FlushNeighbors(int? interfaceIndex = null) {
			Debug.AssertNotMainThread();
			if (interfaceIndex.HasValue)
				Guard.Argument(interfaceIndex.Value > 0, nameof(interfaceIndex), "An interface index is a positive number.");
			var interfaces = interfaceIndex.HasValue ? new[] { interfaceIndex.Value } : GetNeighbors().Select(neighbor => neighbor.InterfaceIndex).Distinct().ToArray();
			foreach (var index in interfaces) {
				var error = WinAPI.IPHLPAPI.FlushIpNetTable2(WinAPI.IPHLPAPI.AF_UNSPEC, (uint)index);
				if (error == WinAPI.IPHLPAPI.ERROR_NOT_FOUND && !interfaceIndex.HasValue)
					continue;
				ThrowOnNetworkChangeError(error, "flush the neighbour cache");
			}
		}

		public static Task FlushNeighborsAsync(int? interfaceIndex = null, CancellationToken cancellationToken = default)
			=> Task.Run(() => FlushNeighbors(interfaceIndex), cancellationToken);

		/// <summary>Adds a permanent (static) entry to Windows' neighbour cache, so the IPv4 or IPv6 address resolves to the given
		/// hardware address on that interface without ARP or neighbour discovery. This needs administrator rights.</summary>
		public static void AddStaticNeighbor(IPAddress address, PhysicalAddress physicalAddress, int interfaceIndex) {
			Debug.AssertNotMainThread();
			Guard.ArgumentNotNull(address, nameof(address));
			Guard.ArgumentNotNull(physicalAddress, nameof(physicalAddress));
			Guard.Argument(address.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6, nameof(address), "A neighbour address is IPv4 or IPv6.");
			var physical = physicalAddress.GetAddressBytes();
			Guard.Argument(physical.Length is > 0 and <= NeighborPhysicalAddressCapacity, nameof(physicalAddress), "A hardware address has between 1 and 32 bytes.");
			Guard.Argument(interfaceIndex > 0, nameof(interfaceIndex), "An interface index is a positive number.");
			var row = BuildNeighborRow(address, physical, interfaceIndex, WinAPI.IPHLPAPI.NlnsPermanent);
			ThrowOnNetworkChangeError(WinAPI.IPHLPAPI.CreateIpNetEntry2(row), "add the neighbour entry");
		}

		public static Task AddStaticNeighborAsync(IPAddress address, PhysicalAddress physicalAddress, int interfaceIndex, CancellationToken cancellationToken = default)
			=> Task.Run(() => AddStaticNeighbor(address, physicalAddress, interfaceIndex), cancellationToken);

		/// <summary>Builds a MIB_IPNET_ROW2 that selects its interface by index (the LUID is left zero).</summary>
		internal static byte[] BuildNeighborRow(IPAddress address, byte[] physical, int interfaceIndex, uint state) {
			var row = new byte[NeighborRowSize];
			var span = row.AsSpan();
			var bytes = address.GetAddressBytes();
			if (address.AddressFamily == AddressFamily.InterNetworkV6) {
				BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)WinAPI.IPHLPAPI.AF_INET6);
				bytes.CopyTo(span.Slice(8));
				// A link-local address is scoped to its interface.
				var scope = address.ScopeId != 0 ? (uint)address.ScopeId : address.IsIPv6LinkLocal ? (uint)interfaceIndex : 0;
				BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(24), scope);
			} else {
				BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)WinAPI.IPHLPAPI.AF_INET);
				bytes.CopyTo(span.Slice(4));
			}
			BinaryPrimitives.WriteInt32LittleEndian(span.Slice(NeighborInterfaceIndexOffset), interfaceIndex);
			physical.CopyTo(span.Slice(NeighborPhysicalAddressOffset));
			BinaryPrimitives.WriteInt32LittleEndian(span.Slice(NeighborPhysicalLengthOffset), physical.Length);
			BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(NeighborStateOffset), state);
			return row;
		}

		internal static WindowsNetworkNeighbor ReadNeighborRow(byte[] row, string interfaceName) {
			var span = row.AsSpan();
			var family = (AddressFamily)BinaryPrimitives.ReadInt16LittleEndian(span);
			var address = new IPAddress(family == AddressFamily.InterNetworkV6 ? span.Slice(8, 16) : span.Slice(4, 4));
			var interfaceIndex = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(NeighborInterfaceIndexOffset));
			var physicalLength = Math.Clamp(BinaryPrimitives.ReadInt32LittleEndian(span.Slice(NeighborPhysicalLengthOffset)), 0, NeighborPhysicalAddressCapacity);
			var physical = span.Slice(NeighborPhysicalAddressOffset, physicalLength).ToArray();
			var state = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(NeighborStateOffset));
			return new WindowsNetworkNeighbor(address, physical, interfaceIndex, interfaceName,
				Enum.IsDefined((WindowsNeighborState)state) ? (WindowsNeighborState)state : WindowsNeighborState.Unreachable, (row[NeighborFlagsOffset] & 1) != 0);
		}

		private static int? InterfaceIndexOf(NetworkInterface item) {
			int? index = null;
			Tools.Exceptions.ExecuteIgnoringException(() => index = item.GetIPProperties().GetIPv4Properties()?.Index);
			if (!index.HasValue)
				Tools.Exceptions.ExecuteIgnoringException(() => index = item.GetIPProperties().GetIPv6Properties()?.Index);
			return index;
		}

		private static void ThrowOnNetworkChangeError(uint error, string action) {
			if (error == 0)
				return;
			if (error == WinAPI.IPHLPAPI.ERROR_ACCESS_DENIED)
				throw new UnauthorizedAccessException($"Windows refused to {action}; administrator rights are required.");
			throw new Win32Exception((int)error);
		}
	}
}
