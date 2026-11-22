// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;
using Sphere10.Framework.Windows;

namespace Tools;

public static partial class Windows {

	public static partial class Network {
		private const int InterfaceInfoHeaderSize = 4;

		/// <summary>Gives up the IPv4 address that a DHCP server leased to an interface, as <c>ipconfig /release</c> does for one adapter.
		/// The interface has no IPv4 address until it is renewed. This needs administrator rights.</summary>
		/// <exception cref="InvalidOperationException">No IPv4 adapter has that index, or the adapter does not use DHCP.</exception>
		public static void ReleaseDhcpAddress(int interfaceIndex) => ChangeDhcpLease(interfaceIndex, true);

		public static Task ReleaseDhcpAddressAsync(int interfaceIndex, CancellationToken cancellationToken = default)
			=> Task.Run(() => ReleaseDhcpAddress(interfaceIndex), cancellationToken);

		/// <summary>Asks the DHCP server to renew, or give again, an interface's IPv4 lease, as <c>ipconfig /renew</c> does for one adapter.
		/// It blocks until the server answers or Windows gives up, which can take many seconds. This needs administrator rights.</summary>
		/// <exception cref="InvalidOperationException">No IPv4 adapter has that index, or the adapter does not use DHCP.</exception>
		public static void RenewDhcpAddress(int interfaceIndex) => ChangeDhcpLease(interfaceIndex, false);

		public static Task RenewDhcpAddressAsync(int interfaceIndex, CancellationToken cancellationToken = default)
			=> Task.Run(() => RenewDhcpAddress(interfaceIndex), cancellationToken);

		/// <summary>Reads an IP_INTERFACE_INFO: a LONG adapter count followed by IP_ADAPTER_INDEX_MAP rows.</summary>
		internal static WinAPI.IPHLPAPI.IP_ADAPTER_INDEX_MAP[] ReadInterfaceInfo(IntPtr buffer, int size) {
			var length = Marshal.SizeOf<WinAPI.IPHLPAPI.IP_ADAPTER_INDEX_MAP>();
			var count = Marshal.ReadInt32(buffer);
			Guard.Ensure(count >= 0 && count <= (size - InterfaceInfoHeaderSize) / length, "The Windows adapter table reported an invalid adapter count.");
			return Enumerable.Range(0, count).Select(index => Marshal.PtrToStructure<WinAPI.IPHLPAPI.IP_ADAPTER_INDEX_MAP>(IntPtr.Add(buffer, InterfaceInfoHeaderSize + index * length))).ToArray();
		}

		private static void ChangeDhcpLease(int interfaceIndex, bool release) {
			Debug.AssertNotMainThread();
			Guard.Argument(interfaceIndex > 0, nameof(interfaceIndex), "An interface index is a positive number.");
			var action = release ? "release" : "renew";
			var maps = GetAdapterIndexMaps();
			var position = System.Array.FindIndex(maps, map => map.Index == (uint)interfaceIndex);
			if (position < 0)
				throw new InvalidOperationException($"No IPv4 network adapter has interface index {interfaceIndex}.");
			var adapter = FindIPv4Adapter(interfaceIndex);
			if (adapter is { DhcpEnabled: false })
				throw new InvalidOperationException($"The adapter {adapter.Value.Name} has a fixed IPv4 address and does not use DHCP, so there is no lease to {action}.");
			var row = maps[position];
			var error = release ? WinAPI.IPHLPAPI.IpReleaseAddress(ref row) : WinAPI.IPHLPAPI.IpRenewAddress(ref row);
			ThrowOnNetworkChangeError(error, $"{action} the DHCP address");
		}

		private static WinAPI.IPHLPAPI.IP_ADAPTER_INDEX_MAP[] GetAdapterIndexMaps() {
			var size = 0U;
			var error = WinAPI.IPHLPAPI.GetInterfaceInfo(IntPtr.Zero, ref size);
			if (error == WinAPI.IPHLPAPI.ERROR_NO_DATA)
				return System.Array.Empty<WinAPI.IPHLPAPI.IP_ADAPTER_INDEX_MAP>();
			if (error != 0 && error != WinAPI.IPHLPAPI.ERROR_INSUFFICIENT_BUFFER)
				throw new Win32Exception((int)error);
			for (var attempt = 0; attempt < 4; attempt++) {
				Guard.Ensure(size is >= InterfaceInfoHeaderSize and <= 16 * 1024 * 1024, "The Windows adapter table reported an invalid buffer size.");
				var capacity = (int)size;
				using var memory = new UnmanagedMemoryScope(capacity);
				error = WinAPI.IPHLPAPI.GetInterfaceInfo(memory.Item, ref size);
				if (error == WinAPI.IPHLPAPI.ERROR_INSUFFICIENT_BUFFER)
					continue;
				if (error == WinAPI.IPHLPAPI.ERROR_NO_DATA)
					return System.Array.Empty<WinAPI.IPHLPAPI.IP_ADAPTER_INDEX_MAP>();
				if (error != 0)
					throw new Win32Exception((int)error);
				return ReadInterfaceInfo(memory.Item, capacity);
			}
			throw new Win32Exception((int)WinAPI.IPHLPAPI.ERROR_INSUFFICIENT_BUFFER, "The Windows adapter table changed repeatedly during enumeration.");
		}

		private static (string Name, bool DhcpEnabled)? FindIPv4Adapter(int interfaceIndex) {
			foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces()) {
				IPv4InterfaceProperties properties = null;
				Tools.Exceptions.ExecuteIgnoringException(() => properties = adapter.GetIPProperties().GetIPv4Properties());
				if (properties?.Index == interfaceIndex)
					return (adapter.Name, properties.IsDhcpEnabled);
			}
			return null;
		}
	}
}
