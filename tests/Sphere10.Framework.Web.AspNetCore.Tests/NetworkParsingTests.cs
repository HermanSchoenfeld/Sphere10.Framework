// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;

namespace Sphere10.Framework.Web.AspNetCore.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class NetworkParsingTests {

	[TestCase("192.168.1.123/24", "192.168.1.0/24")]
	[TestCase("10.1.2.3/0", "0.0.0.0/0")]
	[TestCase("10.1.2.3/32", "10.1.2.3/32")]
	[TestCase("172.31.255.255/12", "172.16.0.0/12")]
	[TestCase("2001:db8:1234:5678:abcd::1/64", "2001:db8:1234:5678::/64")]
	[TestCase("2001:db8::1/0", "::/0")]
	[TestCase("2001:db8::1/128", "2001:db8::1/128")]
	public void ParseNetworkNormalizesHostBits(string cidr, string expected) {
		var network = Tools.Web.AspNetCore.ParseNetwork(cidr);

		Assert.That(network.ToString(), Is.EqualTo(expected));
	}

	[TestCase("192.168.1.1/33")]
	[TestCase("192.168.1.1/-1")]
	[TestCase("2001:db8::1/129")]
	[TestCase("192.168.1.1")]
	[TestCase("invalid/24")]
	public void ParseNetworkRejectsInvalidNetwork(string cidr) {
		Assert.That(() => Tools.Web.AspNetCore.ParseNetwork(cidr), Throws.InstanceOf<ArgumentException>());
	}
}
