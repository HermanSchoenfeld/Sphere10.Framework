// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Buffers.Binary;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Networking;

namespace Sphere10.Framework.Windows.Tests;

/// <summary>Adding, deleting and flushing neighbour (ARP) entries. The ordinary cases build and read MIB_IPNET_ROW2 rows in memory and
/// check that invalid requests are refused before anything is sent; the cases that change the table are explicit.</summary>
[TestFixture]
public class WindowsNeighborChangeTests {
	// 192.0.2.0/24 is TEST-NET-1, which no real network uses.
	private static readonly IPAddress TestAddress = IPAddress.Parse("192.0.2.123");
	private static readonly PhysicalAddress TestPhysicalAddress = PhysicalAddress.Parse("02-00-5E-10-00-01");

	[Test]
	public void AnIPv4RowIsBuiltWhereWindowsReadsIt() {
		var row = Tools.Windows.Network.BuildNeighborRow(TestAddress, TestPhysicalAddress.GetAddressBytes(), 7, WinAPI.IPHLPAPI.NlnsPermanent);
		Assert.That(row.Length, Is.EqualTo(88));
		Assert.That(BinaryPrimitives.ReadUInt16LittleEndian(row), Is.EqualTo(2), "AF_INET");
		Assert.That(row.Skip(4).Take(4), Is.EqualTo(TestAddress.GetAddressBytes()));
		Assert.That(BinaryPrimitives.ReadInt32LittleEndian(row.AsSpan(28)), Is.EqualTo(7));
		Assert.That(BinaryPrimitives.ReadUInt64LittleEndian(row.AsSpan(32)), Is.Zero, "A zero LUID selects the interface by index.");
		Assert.That(BinaryPrimitives.ReadInt32LittleEndian(row.AsSpan(72)), Is.EqualTo(6));
		Assert.That(BinaryPrimitives.ReadUInt32LittleEndian(row.AsSpan(76)), Is.EqualTo(6), "NlnsPermanent");
		var neighbor = Tools.Windows.Network.ReadNeighborRow(row, "Test");
		Assert.That(neighbor.Address, Is.EqualTo(TestAddress));
		Assert.That(neighbor.PhysicalAddress, Is.EqualTo(TestPhysicalAddress.GetAddressBytes()));
		Assert.That(neighbor.InterfaceIndex, Is.EqualTo(7));
		Assert.That(neighbor.InterfaceName, Is.EqualTo("Test"));
		Assert.That(neighbor.State, Is.EqualTo(WindowsNeighborState.Permanent));
		Assert.That(neighbor.IsRouter, Is.False);
	}

	[Test]
	public void AnIPv6LinkLocalRowIsScopedToItsInterface() {
		var address = IPAddress.Parse("fe80::1");
		var row = Tools.Windows.Network.BuildNeighborRow(address, TestPhysicalAddress.GetAddressBytes(), 12, WinAPI.IPHLPAPI.NlnsPermanent);
		Assert.That(BinaryPrimitives.ReadUInt16LittleEndian(row), Is.EqualTo(23), "AF_INET6");
		Assert.That(row.Skip(8).Take(16), Is.EqualTo(address.GetAddressBytes()));
		Assert.That(BinaryPrimitives.ReadUInt32LittleEndian(row.AsSpan(24)), Is.EqualTo(12));
		Assert.That(Tools.Windows.Network.ReadNeighborRow(row, null).Address, Is.EqualTo(address));
	}

	[Test]
	public void InvalidChangesAreRefusedBeforeAnythingIsSent() {
		Assert.That(() => Tools.Windows.Network.AddStaticNeighborAsync(null, TestPhysicalAddress, 1), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => Tools.Windows.Network.AddStaticNeighborAsync(TestAddress, PhysicalAddress.None, 1), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => Tools.Windows.Network.AddStaticNeighborAsync(TestAddress, TestPhysicalAddress, 0), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => Tools.Windows.Network.DeleteNeighborAsync(null), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => Tools.Windows.Network.DeleteNeighborAsync(new WindowsNetworkNeighbor(TestAddress, Array.Empty<byte>(), 0, null, WindowsNeighborState.Stale, false)),
			Throws.InstanceOf<ArgumentException>());
		Assert.That(() => Tools.Windows.Network.FlushNeighborsAsync(0), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	[Explicit("Adds and deletes a static neighbour entry for a TEST-NET-1 address; needs administrator rights.")]
	public async Task AStaticNeighbourIsAddedAndDeleted() {
		var interfaceIndex = FirstIPv4InterfaceIndex();
		await Tools.Windows.Network.AddStaticNeighborAsync(TestAddress, TestPhysicalAddress, interfaceIndex);
		var added = (await Tools.Windows.Network.GetNeighborsAsync()).SingleOrDefault(neighbor => neighbor.Address.Equals(TestAddress) && neighbor.InterfaceIndex == interfaceIndex);
		Assert.That(added, Is.Not.Null);
		Assert.That(added.State, Is.EqualTo(WindowsNeighborState.Permanent));
		Assert.That(added.PhysicalAddress, Is.EqualTo(TestPhysicalAddress.GetAddressBytes()));
		await Tools.Windows.Network.DeleteNeighborAsync(added);
		Assert.That((await Tools.Windows.Network.GetNeighborsAsync()).Where(neighbor => neighbor.Address.Equals(TestAddress) && neighbor.InterfaceIndex == interfaceIndex), Is.Empty);
	}

	[Test]
	[Explicit("Clears this computer's neighbour (ARP) cache on every interface; needs administrator rights.")]
	public async Task TheNeighbourCacheIsFlushed() {
		await Tools.Windows.Network.FlushNeighborsAsync();
		var remaining = await Tools.Windows.Network.GetNeighborsAsync();
		TestContext.WriteLine($"{remaining.Length} entries remain, {remaining.Count(neighbor => neighbor.State == WindowsNeighborState.Permanent)} of them permanent.");
	}

	private static int FirstIPv4InterfaceIndex() {
		var index = NetworkInterface.GetAllNetworkInterfaces()
			.Where(item => item.OperationalStatus == OperationalStatus.Up && item.NetworkInterfaceType != NetworkInterfaceType.Loopback && item.Supports(NetworkInterfaceComponent.IPv4))
			.Select(item => item.GetIPProperties().GetIPv4Properties()?.Index).FirstOrDefault(value => value.HasValue);
		if (!index.HasValue)
			Assert.Ignore("No IPv4 interface is up.");
		return index.Value;
	}
}
