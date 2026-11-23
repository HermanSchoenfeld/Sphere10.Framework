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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

/// <summary>WHOIS referrals and the TCP port probe. The probe is run against listeners this fixture opens on the loopback address.</summary>
[TestFixture]
public class NetworkToolProbeTests {
	[TestCase("refer:        whois.verisign-grs.com\n", "whois.verisign-grs.com")]
	[TestCase("% IANA WHOIS server\r\nwhois:        whois.nic.io\r\nstatus: ACTIVE\r\n", "whois.nic.io")]
	[TestCase("   Registrar WHOIS Server: whois.registrar.example\r\n   Registrar URL: http://www.registrar.example\r\n", "whois.registrar.example")]
	[TestCase("ReferralServer:  whois://whois.arin.net\n", "whois.arin.net")]
	[TestCase("ReferralServer: rwhois://rwhois.example.net:4321\nOrgName: Example\n", null)]
	[TestCase("ReferralServer: whois://whois.example.net:43/\n", "whois.example.net")]
	[TestCase("Domain Name: EXAMPLE.COM\nRegistrar: Example Registrar\n", null)]
	[TestCase("Registrar WHOIS Server:\n", null)]
	[TestCase("", null)]
	public void TheServerAnAnswerRefersToIsFound(string response, string expected) => Assert.That(Tools.Network.FindWhoisReferral(response), Is.EqualTo(expected));

	[Test]
	public async Task AListeningPortIsOpenAndAReleasedPortIsClosedOrUnanswered() {
		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		using var stop = Tools.Scope.ExecuteOnDispose(listener.Stop);
		var open = ((IPEndPoint)listener.LocalEndpoint).Port;
		var released = new TcpListener(IPAddress.Loopback, 0);
		released.Start();
		var closed = ((IPEndPoint)released.LocalEndpoint).Port;
		released.Stop();

		var accepted = await Tools.Network.ProbeTcpPortAsync(IPAddress.Loopback, open, TimeSpan.FromSeconds(5));
		Assert.That((accepted.Port, accepted.State, accepted.Detail), Is.EqualTo((open, NetworkPortState.Open, string.Empty)));
		// Windows refuses a connection to its own closed port after about two seconds; a generous wait sees the refusal.
		var refused = await Tools.Network.ProbeTcpPortAsync(IPAddress.Loopback, closed, TimeSpan.FromSeconds(10));
		Assert.That((refused.Port, refused.State), Is.EqualTo((closed, NetworkPortState.Closed)));
		// A wait shorter than that reports the same port as unanswered rather than failing.
		var unanswered = await Tools.Network.ProbeTcpPortAsync(IPAddress.Loopback, closed, TimeSpan.FromMilliseconds(50));
		Assert.That(unanswered.State, Is.EqualTo(NetworkPortState.NoReply).Or.EqualTo(NetworkPortState.Closed));
	}

	[Test]
	public void AnImpossiblePortOrWaitIsRefusedAndACancelledProbeIsCancelled() {
		Assert.That(() => Tools.Network.ProbeTcpPortAsync(IPAddress.Loopback, 0, TimeSpan.FromSeconds(1)), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => Tools.Network.ProbeTcpPortAsync(IPAddress.Loopback, 80, TimeSpan.Zero), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => Tools.Network.ProbeTcpPortAsync(null, 80, TimeSpan.FromSeconds(1)), Throws.InstanceOf<ArgumentException>());
		using var cancelled = new CancellationTokenSource();
		cancelled.Cancel();
		Assert.That(() => Tools.Network.ProbeTcpPortAsync(IPAddress.Loopback, 80, TimeSpan.FromSeconds(1), cancelled.Token), Throws.InstanceOf<OperationCanceledException>());
	}
}
