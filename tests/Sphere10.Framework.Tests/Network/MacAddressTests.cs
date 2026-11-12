// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using System.Net.NetworkInformation;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
public sealed class MacAddressTests {
	[Test]
	public void MacAddressesOmitInterfacesWithoutHardwareAddresses() {
		var addresses = Tools.Network.GetMacAddresses().ToArray();
		Assert.That(addresses, Has.None.Empty, "An interface without a hardware address (WAN miniport, virtual switch) must not contribute a blank MAC address.");
		Assert.That(addresses, Has.None.Matches<string>(address => address.Split('-').All(part => part == "00")), "An all-zero address identifies no hardware.");
		Assert.That(addresses, Is.Unique);
	}

	[Test]
	public void MacAddressesKeepEveryRealHardwareAddress() {
		// Every non-internal interface with a non-zero hardware address must still be reported.
		var expected = NetworkInterface.GetAllNetworkInterfaces()
			.Where(nic => nic.NetworkInterfaceType is not (NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel or NetworkInterfaceType.Unknown))
			.Select(nic => nic.GetPhysicalAddress().GetAddressBytes())
			.Where(bytes => bytes.Any(value => value != 0))
			.Select(Tools.Network.ToMacAddressString)
			.Distinct()
			.ToArray();
		Assert.That(Tools.Network.GetMacAddresses(), Is.EquivalentTo(expected));
	}

	[Test]
	public void DefaultMacAddressKeepsItsOriginalSaltValue() {
		// EncryptedStringAttribute salts with GetMacAddressOrDefault; it must keep the original unfiltered first address.
		var original = NetworkInterface.GetAllNetworkInterfaces()
			.Where(nic => nic.NetworkInterfaceType is not (NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel or NetworkInterfaceType.Unknown))
			.Select(nic => Tools.Network.ToMacAddressString(nic.GetPhysicalAddress().GetAddressBytes()))
			.Distinct()
			.OrderBy(address => address)
			.FirstOrDefault();
		Assert.That(Tools.Network.GetMacAddressOrDefault(), Is.EqualTo(original));
	}
}
