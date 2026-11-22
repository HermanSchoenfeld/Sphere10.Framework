// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

/// <summary>Releasing and renewing DHCP leases. The ordinary cases read IP_INTERFACE_INFO memory and check that a request for an
/// interface that does not exist is refused before anything is sent; the cases that release and renew a real lease are explicit.</summary>
[TestFixture]
public class WindowsDhcpTests {
	// Names the interface index the explicit cases release and renew, so they never pick an adapter by themselves.
	private const string InterfaceVariable = "SPHERE10_DHCP_TEST_INTERFACE";

	[Test]
	public void TheAdapterTableIsReadWithIndexesAndNames() {
		const int rowSize = 4 + 2 * WinAPI.IPHLPAPI.MAX_ADAPTER_NAME;
		const int size = 4 + 2 * rowSize;
		Assert.That(Marshal.SizeOf<WinAPI.IPHLPAPI.IP_ADAPTER_INDEX_MAP>(), Is.EqualTo(rowSize));
		var buffer = Marshal.AllocHGlobal(size);
		try {
			Marshal.Copy(new byte[size], 0, buffer, size);
			Marshal.WriteInt32(buffer, 2);
			WriteRow(buffer, 4, 3, @"\DEVICE\TCPIP_{11111111-2222-3333-4444-555555555555}");
			WriteRow(buffer, 4 + rowSize, 17, @"\DEVICE\TCPIP_{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}");
			var maps = Tools.Windows.Network.ReadInterfaceInfo(buffer, size);
			Assert.That(maps.Length, Is.EqualTo(2));
			Assert.That(maps[0].Index, Is.EqualTo(3));
			Assert.That(maps[0].Name, Is.EqualTo(@"\DEVICE\TCPIP_{11111111-2222-3333-4444-555555555555}"));
			Assert.That(maps[1].Index, Is.EqualTo(17));
			Marshal.WriteInt32(buffer, 3);
			Assert.That(() => Tools.Windows.Network.ReadInterfaceInfo(buffer, size), Throws.Exception, "A count beyond the buffer is refused.");
		} finally {
			Marshal.FreeHGlobal(buffer);
		}
	}

	[Test]
	public void AnInterfaceThatDoesNotExistIsRefusedBeforeAnythingIsSent() {
		Assert.That(() => Tools.Windows.Network.ReleaseDhcpAddressAsync(int.MaxValue), Throws.InvalidOperationException.With.Message.Contains("No IPv4 network adapter"));
		Assert.That(() => Tools.Windows.Network.RenewDhcpAddressAsync(int.MaxValue), Throws.InvalidOperationException.With.Message.Contains("No IPv4 network adapter"));
		Assert.That(() => Tools.Windows.Network.ReleaseDhcpAddressAsync(0), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	[Explicit("Releases and renews the DHCP lease of the interface named by SPHERE10_DHCP_TEST_INTERFACE; the adapter loses its address meanwhile.")]
	public async Task ALeaseIsReleasedAndRenewed() {
		if (!int.TryParse(Environment.GetEnvironmentVariable(InterfaceVariable), out var interfaceIndex))
			Assert.Ignore($"Set {InterfaceVariable} to the index of a DHCP interface that may lose its address.");
		await Tools.Windows.Network.ReleaseDhcpAddressAsync(interfaceIndex);
		await Tools.Windows.Network.RenewDhcpAddressAsync(interfaceIndex);
	}

	private static void WriteRow(IntPtr buffer, int offset, int index, string name) {
		Marshal.WriteInt32(buffer, offset, index);
		var bytes = Encoding.Unicode.GetBytes(name);
		Marshal.Copy(bytes, 0, IntPtr.Add(buffer, offset + 4), bytes.Length);
	}
}
