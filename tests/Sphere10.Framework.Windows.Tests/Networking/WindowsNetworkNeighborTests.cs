// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Networking;

namespace Sphere10.Framework.Windows.Tests;

/// <summary>The neighbour (ARP) table and the ARP lookup. Reading the table sends nothing; the lookup is only asked for an address
/// that is refused before anything is sent.</summary>
[TestFixture]
public class WindowsNetworkNeighborTests {
	[Test]
	[Category("Integration")]
	public async Task TheNeighbourTableIsReadWithAddressesStatesAndHardwareAddressLengths() {
		var neighbors = await Tools.Windows.Network.GetNeighborsAsync();
		Assert.That(neighbors, Is.Not.Null);
		if (neighbors.Length == 0)
			Assert.Inconclusive("This computer's neighbour table is empty.");
		Assert.That(neighbors.Select(neighbor => neighbor.Address.AddressFamily), Is.All.EqualTo(AddressFamily.InterNetwork).Or.EqualTo(AddressFamily.InterNetworkV6));
		Assert.That(neighbors.Select(neighbor => neighbor.PhysicalAddress.Length), Is.All.LessThanOrEqualTo(32), "A hardware address is at most 32 bytes; Ethernet's are 6.");
		Assert.That(neighbors.Select(neighbor => neighbor.State), Is.All.Matches<WindowsNeighborState>(Enum.IsDefined));
		Assert.That(neighbors.Select(neighbor => neighbor.InterfaceIndex), Is.All.GreaterThan(0));
		// Ethernet and Wi-Fi neighbours that Windows has learned have six-byte addresses.
		Assert.That(neighbors.Where(neighbor => neighbor.State is WindowsNeighborState.Reachable or WindowsNeighborState.Stale).Select(neighbor => neighbor.PhysicalAddress.Length),
			Is.All.EqualTo(6).Or.EqualTo(0));
		TestContext.WriteLine($"{neighbors.Length} neighbours on {neighbors.Select(neighbor => neighbor.InterfaceIndex).Distinct().Count()} interfaces.");
	}

	[Test]
	public void AnArpLookupOfAnIPv6AddressOrNoAddressIsRefusedBeforeAnythingIsSent() {
		Assert.That(() => Tools.Windows.Network.ResolvePhysicalAddressAsync(IPAddress.IPv6Loopback), Throws.InstanceOf<ArgumentException>().With.Message.Contains("IPv4"));
		Assert.That(() => Tools.Windows.Network.ResolvePhysicalAddressAsync(null), Throws.InstanceOf<ArgumentException>());
	}
}
