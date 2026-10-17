// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Win32Exception = System.ComponentModel.Win32Exception;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Networking;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, NonParallelizable]
public class WindowsNetworkUtilTests {
	[Test]
	public void InterfaceCounterFacadeMatchesTheWindowsSdkLayout() {
		Assert.That(Marshal.SizeOf<WinAPI.IPHLPAPI.MIB_IF_ROW2>(), Is.EqualTo(1352));
		Assert.That(Marshal.OffsetOf<WinAPI.IPHLPAPI.MIB_IF_ROW2>(nameof(WinAPI.IPHLPAPI.MIB_IF_ROW2.InterfaceIndex)).ToInt32(), Is.EqualTo(8));
		Assert.That(Marshal.OffsetOf<WinAPI.IPHLPAPI.MIB_IF_ROW2>(nameof(WinAPI.IPHLPAPI.MIB_IF_ROW2.InOctets)).ToInt32(), Is.EqualTo(1208));
		Assert.That(Marshal.OffsetOf<WinAPI.IPHLPAPI.MIB_IF_ROW2>(nameof(WinAPI.IPHLPAPI.MIB_IF_ROW2.OutOctets)).ToInt32(), Is.EqualTo(1280));
	}

	[TestCase(AddressFamily.InterNetwork, true)]
	[TestCase(AddressFamily.InterNetworkV6, true)]
	[TestCase(AddressFamily.InterNetwork, false)]
	[TestCase(AddressFamily.InterNetworkV6, false)]
	[Category("Integration")]
	public async Task TypedEndpointUtilReportsTheOwnedSocketAndExactAddressFamily(AddressFamily family, bool tcp) {
		if (family == AddressFamily.InterNetworkV6 && !Socket.OSSupportsIPv6)
			Assert.Ignore("IPv6 is unavailable on this Windows host.");
		using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		using var socket = new Socket(family, tcp ? SocketType.Stream : SocketType.Dgram, tcp ? ProtocolType.Tcp : ProtocolType.Udp);
		var address = family == AddressFamily.InterNetwork ? IPAddress.Loopback : IPAddress.IPv6Loopback;
		socket.Bind(new IPEndPoint(address, 0));
		if (tcp)
			socket.Listen(1);
		var port = ((IPEndPoint)socket.LocalEndPoint).Port;
		var row = (await Tools.Windows.Network.GetEndpointsAsync(tcp, family, lifetime.Token)).Single(value => value.LocalPort == port && value.LocalAddress.Equals(address));
		Assert.That(row.AddressFamily, Is.EqualTo(family));
		Assert.That(row.ProcessId, Is.EqualTo((uint)Environment.ProcessId));
		Assert.That(row.IsTcp, Is.EqualTo(tcp));
		Assert.That(row.RemoteAddress, Is.Null);
		Assert.That(row.RemotePort, Is.Null);
	}

	[Test, Category("Integration")]
	public void NativeInterfaceCountersKeepTheGuidLuidAndUnsignedTotals() {
		var interfaces = Tools.Windows.Network.GetInterfaces();
		var available = 0;
		foreach (var adapter in interfaces) {
			if (!Guid.TryParse(adapter.Id, out var id))
				continue;
			WindowsInterfaceCounters counters;
			try { counters = Tools.Windows.Network.GetInterfaceCounters(id); }
			catch (Win32Exception error) { TestContext.WriteLine($"Interface {id} counters unavailable: Win32 {error.NativeErrorCode}."); continue; }
			Assert.That(counters.InterfaceId, Is.EqualTo(id));
			Assert.That(counters.InterfaceIndex, Is.GreaterThan(0));
			Assert.That(counters.InterfaceLuid, Is.GreaterThan(0));
			available++;
		}
		Assert.That(available, Is.GreaterThan(0));
	}

	[Test]
	public async Task CancellationStopsBeforeNativeEndpointOrAdapterReads() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await Tools.Windows.Network.GetEndpointsAsync(true, AddressFamily.InterNetwork, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(async () => await Tools.Windows.Network.GetInterfacesAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
	}
}
